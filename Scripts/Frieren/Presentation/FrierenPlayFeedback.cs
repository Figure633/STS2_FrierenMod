using System.Reflection;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Mechanics;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace FrierenMod.Scripts.Presentation;

// 通过原生失败原因与阻止者传递卡牌上下文；鼠标、手柄和 BaseLib 选目标共用这条路径。
// 不修改 CanPlay 的结果，也不保存“当前卡牌”等全局状态。
public static class FrierenPlayFeedback
{
    private static string? MessageKey(CardModel card)
    {
        if (card is not FrierenCard || !card.IsMutable || card.Owner?.Creature.CombatState == null) return null;
        if (card is ILegendaryCard && LegendaryCards.ChantsThisTurn(card.Owner) == 0) return "PLAY.NO_CHANT";
        return ChantCmd.IsChant(card) ? "PLAY.NO_MANA" : null;
    }

    [HarmonyPatch(typeof(CardModel), nameof(CardModel.CanPlay),
        [typeof(UnplayableReason), typeof(AbstractModel)], [ArgumentType.Out, ArgumentType.Out])]
    private static class AttachCardReason
    {
        private static void Postfix(CardModel __instance, bool __result, UnplayableReason reason, ref AbstractModel? preventer)
        {
            // 只有本牌规则单独阻止出牌时才接管文案；能量、星星、不可打出及其他阻止效果均走原版。
            if (!__result && reason == UnplayableReason.BlockedByCardLogic && preventer == null && MessageKey(__instance) != null)
                preventer = __instance;
        }
    }

    [HarmonyPatch]
    private static class ExplainCardReason
    {
        private static MethodBase TargetMethod() => AccessTools.Method(
            typeof(CardModel).Assembly.GetType("MegaCrit.Sts2.Core.Entities.Cards.UnplayableReasonExtensions", throwOnError: true),
            "GetPlayerDialogueLine");

        private static void Postfix(UnplayableReason reason, AbstractModel? preventer, ref LocString? __result)
        {
            if (reason == UnplayableReason.BlockedByCardLogic && preventer is CardModel card && MessageKey(card) is { } key)
                __result = new LocString("frieren", key);
        }
    }
}
