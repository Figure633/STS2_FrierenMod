using System.Reflection;
using FrierenMod.Scripts.Cards;
using MegaCrit.Sts2.Core.Entities.Cards;
using System.Reflection.Emit;
using BaseLib.Extensions;
using FrierenMod.Scripts.Characters;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;

namespace FrierenMod.Scripts.Mechanics;

// 统一限制随机获取；模型注册、百科、旧档和芙莉莲本人的卡池不受影响。
public static class FrierenCardAvailability
{
    public static bool IsRandomCandidate(CardModel card) =>
        card.Rarity is CardRarity.Common or CardRarity.Uncommon or CardRarity.Rare &&
        card is not FactorCard and not MagicPropCard and not TimeConcept and not Flow and not IGeneratedOnlyCard;

    public static IEnumerable<CardPoolModel> Pools(IEnumerable<CardPoolModel> pools, Player player) =>
        player.Character is Frieren ? pools : pools.Where(pool => pool is not FrierenCardPool);

    public static IEnumerable<CardModel> Cards(IEnumerable<CardModel> cards, Player player) =>
        cards.Where(card => card.Pool is not FrierenCardPool || player.Character is Frieren && IsRandomCandidate(card));

    // 万花筒先随机选择角色池，必须在抽池之前排除，避免抽中空池后奖励生成失败。
    // 棱彩宝石也复用同一处规则，保留原生解锁、随机数、奖励与存档流程。
    [HarmonyPatch]
    private static class CrossCharacterPools
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(Kaleidoscope), nameof(Kaleidoscope.AfterObtained))
                .StateMachineType().GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.NonPublic)!;
            yield return AccessTools.Method(typeof(PrismaticGem), nameof(PrismaticGem.ModifyCardRewardCreationOptions));
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
        {
            var getter = AccessTools.PropertyGetter(typeof(UnlockState), nameof(UnlockState.CharacterCardPools));
            int matches = 0;
            foreach (var instruction in instructions)
            {
                yield return instruction;
                if (!instruction.Calls(getter)) continue;
                matches++;
                if (__originalMethod.DeclaringType == typeof(PrismaticGem))
                    yield return new CodeInstruction(OpCodes.Ldarg_1); // player
                else
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(__originalMethod.DeclaringType, "<>4__this"));
                    yield return CodeInstruction.Call(typeof(RelicModel), "get_Owner");
                }
                yield return CodeInstruction.Call(typeof(FrierenCardAvailability), nameof(Pools));
            }
            if (matches != 1) throw new InvalidOperationException("跨角色卡池接口发生变化：" + __originalMethod);
        }
    }

    [HarmonyPatch(typeof(CardCreationOptions), nameof(CardCreationOptions.GetPossibleCards))]
    private static class RewardCandidates
    {
        private static void Postfix(Player player, ref IEnumerable<CardModel> __result) => __result = Cards(__result, player);
    }

    [HarmonyPatch]
    private static class CombatCandidates
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(CardFactory), nameof(CardFactory.GetDistinctForCombat));
            yield return AccessTools.Method(typeof(CardFactory), nameof(CardFactory.GetForCombat));
        }
        private static void Prefix(Player player, ref IEnumerable<CardModel> cards) => cards = Cards(cards, player);
    }

    [HarmonyPatch(typeof(CardFactory), "GetFilteredTransformationOptions")]
    private static class TransformationCandidates
    {
        private static void Prefix(CardModel original, ref IEnumerable<CardModel> originalOptions) =>
            originalOptions = Cards(originalOptions, original.Owner);
    }
}
