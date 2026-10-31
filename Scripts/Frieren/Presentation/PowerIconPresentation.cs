using FrierenMod.Scripts.Powers;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace FrierenMod.Scripts.Presentation;

// 只在显示层轻微提亮，不改作者原图、透明度、原生脉冲材质或层数文字。
public static class PowerIconPresentation
{
    public static readonly Color IconTint = new(1.08f, 1.08f, 1.08f, 1f);
    private const string OriginalTint = "FrierenPowerOriginalTint";
    public static bool IsModIcon(Texture2D? texture) =>
        texture?.ResourcePath.StartsWith("res://FrierenMod/images/powers/", StringComparison.Ordinal) == true;

    [HarmonyPatch(typeof(NPower), "Reload")]
    private static class CombatIcon
    {
        private static void Postfix(PowerModel? ____model, TextureRect? ____icon, CpuParticles2D? ____powerFlash)
        {
            if (____icon == null) return;
            Apply(____icon, ____model is FrierenPower);
            if (____powerFlash != null) Apply(____powerFlash, ____model is FrierenPower);
        }
    }

    [HarmonyPatch(typeof(NHoverTipSet), "Init")]
    private static class HoverIcon
    {
        private static void Postfix(NHoverTipSet __instance) => ApplyToHoverIcons(__instance);
    }

    public static void ApplyToHoverIcons(Node root)
    {
        foreach (var child in root.GetChildren()) {
            if (child is TextureRect icon) Apply(icon, IsModIcon(icon.Texture));
            ApplyToHoverIcons(child);
        }
    }

    private static void Apply(CanvasItem icon, bool brighten)
    {
        if (brighten) {
            if (!icon.HasMeta(OriginalTint)) icon.SetMeta(OriginalTint, icon.SelfModulate);
            icon.SelfModulate = icon.GetMeta(OriginalTint).AsColor() * IconTint;
        } else if (icon.HasMeta(OriginalTint)) {
            icon.SelfModulate = icon.GetMeta(OriginalTint).AsColor();
            icon.RemoveMeta(OriginalTint);
        }
    }
}
