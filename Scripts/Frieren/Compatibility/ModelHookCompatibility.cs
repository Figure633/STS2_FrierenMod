using System.Reflection;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Powers;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace FrierenMod.Scripts.Compatibility;

internal readonly record struct ModelLocation(Player player, PileType pileType, CardPilePosition position);

// 两版虚方法的签名不同。对本模组使用基类默认实现的模型补上效果，
// 原版仍负责遍历监听者、合并 modifier、伤害预览以及最终移动牌堆。
internal static class ModelHookCompatibility
{
    private static readonly MethodInfo? NewInitial = AccessTools.Method(typeof(CardModel), "GetResultLocationForCardPlay");
    private static readonly MethodInfo? OldInitial = AccessTools.Method(typeof(CardModel), "GetResultPileTypeForCardPlay");
    private static readonly MethodInfo? NewLocation = AccessTools.Method(typeof(AbstractModel), "ModifyCardPlayResultLocation");
    private static readonly MethodInfo? OldLocation = AccessTools.Method(typeof(AbstractModel), "ModifyCardPlayResultPileTypeAndPosition");

    private static ModelLocation Read(object location) => new(
        (Player)AccessTools.Field(location.GetType(),"player").GetValue(location)!,
        (PileType)AccessTools.Field(location.GetType(),"pileType").GetValue(location)!,
        (CardPilePosition)AccessTools.Field(location.GetType(),"position").GetValue(location)!);
    private static object Write(object original, ModelLocation value) =>
        Activator.CreateInstance(original.GetType(), value.player, value.pileType, value.position)!;
    private static ModelLocation Change(AbstractModel listener, CardModel card, bool automatic, ResourceInfo resources, ModelLocation location) => listener switch {
        ManaCollapse collapse => collapse.ChangePlayLocation(card,automatic,resources,location),
        SynchroWithoutManaPower thrift => thrift.ChangePlayLocation(card,automatic,resources,location),
        _ => location
    };

    [HarmonyPatch]
    private static class ManaInitialMain
    {
        private static bool Prepare() => OldInitial != null;
        private static MethodBase TargetMethod() => OldInitial!;
        private static void Postfix(CardModel __instance, ref PileType __result)
        { if (__instance is IManaCard && __result==PileType.Discard) __result=PileType.Exhaust; }
    }
    [HarmonyPatch]
    private static class ManaInitialBeta
    {
        private static bool Prepare() => NewInitial != null;
        private static MethodBase TargetMethod() => NewInitial!;
        private static void Postfix(CardModel __instance, ref object __result)
        {
            if (__instance is not IManaCard) return;
            var location=Read(__result);
            if(location.pileType==PileType.Discard) __result=Write(__result,location with {pileType=PileType.Exhaust});
        }
    }
    [HarmonyPatch]
    private static class LocationMain
    {
        private static bool Prepare() => OldLocation != null;
        private static MethodBase TargetMethod() => OldLocation!;
        private static void Postfix(AbstractModel __instance, CardModel card, bool isAutoPlay, ResourceInfo resources,
            ref (PileType,CardPilePosition) __result)
        {
            if (__instance is not ManaCollapse and not SynchroWithoutManaPower) return;
            var result=Change(__instance,card,isAutoPlay,resources,new(card.Owner,__result.Item1,__result.Item2));
            __result=(result.pileType,result.position);
        }
    }
    [HarmonyPatch]
    private static class LocationBeta
    {
        private static bool Prepare() => NewLocation != null;
        private static MethodBase TargetMethod() => NewLocation!;
        private static void Postfix(AbstractModel __instance, CardModel card, bool isAutoPlay, ResourceInfo resources, ref object __result)
        {
            if (__instance is not ManaCollapse and not SynchroWithoutManaPower) return;
            var before=Read(__result);var after=Change(__instance,card,isAutoPlay,resources,before);
            if(after!=before)__result=Write(__result,after);
        }
    }
    [HarmonyPatch]
    private static class DamageMultipliers
    {
        private static MethodBase TargetMethod() => AccessTools.Method(typeof(AbstractModel),"ModifyDamageMultiplicative");
        private static void Postfix(AbstractModel __instance, object[] __args, ref decimal __result)
        {
            if (__instance is not DivinityPower and not PaperPlanePower) return;
            var target=(Creature?)__args[0];var amount=(decimal)__args[1];var props=(ValueProp)__args[2];
            var dealer=(Creature?)__args[3];var card=(CardModel?)__args[4];
            var play=__args.Length>5?(CardPlay?)__args[5]:null;
            __result *= __instance is DivinityPower divine ? divine.DamageMultiplier(target,amount,props,dealer,card,play)
                : ((PaperPlanePower)__instance).DamageMultiplier(target,amount,props,dealer,card,play);
        }
    }
    [HarmonyPatch]
    private static class DamageBonuses
    {
        private static MethodBase TargetMethod() => AccessTools.Method(typeof(AbstractModel),"ModifyDamageAdditive");
        private static void Postfix(AbstractModel __instance, object[] __args, ref decimal __result)
        {
            if(__instance is StrikeUpPower strike)
                __result+=strike.DamageBonus((Creature?)__args[0],(decimal)__args[1],(ValueProp)__args[2],
                    (Creature?)__args[3],(CardModel?)__args[4],__args.Length>5?(CardPlay?)__args[5]:null);
        }
    }
}
