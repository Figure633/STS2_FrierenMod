using FrierenMod.Scripts.Cards;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace FrierenMod.Scripts.Presentation;

public static class MagicPropPresentation
{
    // 一代 PROP 使用 card_banner_prop_small/large.png 的紫色横幅。
    // 复用二代已有紫色材质，只更改外观，魔具仍属于衍生牌。
    public const string BannerMaterialPath = "res://materials/cards/banners/card_banner_curse_mat.tres";
}

[HarmonyPatch(typeof(NCard), "GetTitleLabelOutlineColor")]
internal static class MagicPropTitleOutline
{
    private static bool Prefix(NCard __instance, ref Color __result)
    {
        if (__instance.Model is not MagicPropCard) return true;
        __result = StsColors.cardTitleOutlineCurse;
        return false;
    }
}
