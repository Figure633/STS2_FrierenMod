using BaseLib.Abstracts;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Mechanics;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Hooks;

namespace FrierenMod.Scripts.RestSite;

public sealed class BuyMagicPropsOption(Player owner) : CustomRestSiteOption(owner)
{
    public override string OptionId => "FRIEREN_MAGIC_SHOP";
    public override string CustomIconPath => "res://FrierenMod/images/ui/buy_prop.tres";
    public override bool IsEnabled => MagicDeckStore.Read(Owner).SetupCompleted;
    public override LocString Description => new("rest_site_ui", "OPTION_" + OptionId + (IsEnabled ? ".description" : ".descriptionDisabled"));
    public override Task<bool> OnSelect() => MagicPropCmd.VisitShop(Owner);
}

// 专属营地功能属于角色；移除圣徽不应使这个按钮消失。
[HarmonyPatch(typeof(RestSiteOption), nameof(RestSiteOption.Generate))]
public static class FrierenRestSiteOptions
{
    public static void Postfix(Player player, List<RestSiteOption> __result)
    {
        if (player.Character is Frieren && !__result.OfType<BuyMagicPropsOption>().Any())
            __result.Add(new BuyMagicPropsOption(player));
        DragonSummoning.RestoreOptions(player,__result);
    }
}

// 原生同步器仍将逛店记为成功并移除该选项，只保留其他营地行动。
[HarmonyPatch(typeof(Hook), nameof(Hook.ShouldDisableRemainingRestSiteOptions))]
internal static class MagicShopMembershipChoice
{
    private static bool Prefix(Player player,ref bool __result)
    {
        if(!MagicPropCmd.ConsumeFreeShopChoice(player))return true;
        __result=false;return false;
    }
}
