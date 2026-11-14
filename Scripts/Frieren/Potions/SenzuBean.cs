using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Nodes.Potions;

namespace FrierenMod.Scripts.Potions;

public sealed class SenzuBean : FrierenPotion
{
    public override PotionRarity Rarity => PotionRarity.Event;
    public override PotionUsage Usage => PotionUsage.AnyTime;
    public override TargetType TargetType => TargetType.AnyPlayer;
    public override bool CanBeGeneratedInCombat => false;
    public override string CustomPackedImagePath => "res://FrierenMod/images/potions/SenzuBean.png";
    public override string CustomPackedOutlinePath => "res://FrierenMod/images/potions/SenzuBeanOutline.png";
    protected override async Task OnUse(PlayerChoiceContext context, Creature? target)
    {
        AssertValidForTargetedPotion(target);
        foreach (var power in target!.Powers.Where(p => p.TypeForCurrentAmount == PowerType.Debuff).ToArray()) await PowerCmd.Remove(power);
        await CreatureCmd.Heal(target, target.MaxHp);
    }
    public static bool Blocks(PotionModel potion) => potion is not SenzuBean && potion.Owner != null &&
        potion.Owner.PotionSlots.Any(p => p is SenzuBean);

    // 原生目标选择器已支持战斗外的队友头像；为仙豆开放此入口，仍走原生药水动作同步。
    [HarmonyPatch(typeof(PotionModel), nameof(PotionModel.CanThrowAtAlly))]
    private static class ThrowAtTeammate
    {
        private static void Postfix(PotionModel __instance, ref bool __result)
        {
            if (__instance is SenzuBean && __instance.IsMutable && __instance.Owner?.RunState.Players.Count > 1)
                __result = true;
        }
    }

    [HarmonyPatch(typeof(PotionModel), nameof(PotionModel.IsValidTarget))]
    private static class ValidateUse
    {
        private static bool Prefix(PotionModel __instance, ref bool __result)
        { if (!Blocks(__instance)) return true; __result = false; return false; }
    }
    [HarmonyPatch(typeof(PotionModel), nameof(PotionModel.OnUseWrapper))]
    private static class GuardUse
    {
        private static bool Prefix(PotionModel __instance, ref Task __result)
        { if (!Blocks(__instance)) return true; __result = Task.CompletedTask; return false; }
    }
    [HarmonyPatch(typeof(FairyInABottle), nameof(FairyInABottle.ShouldDie))]
    private static class GuardFairy
    {
        private static bool Prefix(FairyInABottle __instance, ref bool __result)
        { if (!Blocks(__instance)) return true; __result = true; return false; }
    }
    [HarmonyPatch(typeof(NPotionPopup), "RefreshButtons")]
    private static class DisableOtherPotions
    {
        private static void Postfix(NPotionPopup __instance, NPotionPopupButton ____useButton)
        {
            if (AccessTools.Property(typeof(NPotionPopup), "Potion").GetValue(__instance) is PotionModel potion && Blocks(potion))
                ____useButton.Disable();
        }
    }
}
