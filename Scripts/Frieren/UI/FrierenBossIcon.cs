using Godot;
using HarmonyLib;
using MegaCrit.sts2.Core.Nodes.TopBar;

namespace FrierenMod.Scripts.UI;

[HarmonyPatch(typeof(NTopBarBossIcon), nameof(NTopBarBossIcon.RefreshBossIcon))]
public static class FrierenBossIcon
{
    private static void Postfix(NTopBarBossIcon __instance, TextureRect ____bossIcon, TextureRect ____bossIconOutline,
        TextureRect? ____secondBossIcon, TextureRect? ____secondBossIconOutline)
    {
        __instance.CustomMinimumSize=Vector2.Zero;
        ____bossIcon.SizeFlagsHorizontal=Control.SizeFlags.ShrinkCenter;
        Fit(____bossIcon, ____bossIconOutline);
        if (____secondBossIcon != null && ____secondBossIconOutline != null) {
            Fit(____secondBossIcon, ____secondBossIconOutline);
            if (____secondBossIcon.Visible && (____bossIcon.Texture?.ResourcePath.Contains("/monsters/Spiegel_") == true ||
                ____secondBossIcon.Texture?.ResourcePath.Contains("/monsters/Spiegel_") == true))
            {
                ____secondBossIcon.Position = new Vector2(48,14);
                __instance.CustomMinimumSize = new Vector2(96,44);
                ____bossIcon.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
            }
        }
    }

    private static void Fit(TextureRect icon, TextureRect outline)
    {
        if (icon.Texture is not { } texture || !texture.ResourcePath.Contains("/monsters/Spiegel_node")) return;
        // 原版主图、次图均为44像素；使用独立小图，避免大幅Atlas参与最小尺寸计算。
        icon.Texture = GD.Load<Texture2D>("res://FrierenMod/images/monsters/Spiegel_topbar.png");
        outline.Texture = GD.Load<Texture2D>("res://FrierenMod/images/monsters/Spiegel_topbar_outline.png");
        foreach (var view in new[] { icon, outline }) {
            view.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
            view.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
            view.CustomMinimumSize = new Vector2(44,44);
        }
    }
}
