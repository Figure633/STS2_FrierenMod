using Godot;
using HarmonyLib;
using MegaCrit.sts2.Core.Nodes.TopBar;

namespace FrierenMod.Scripts.UI;

[HarmonyPatch(typeof(NTopBarBossIcon), nameof(NTopBarBossIcon.RefreshBossIcon))]
public static class FrierenBossIcon
{
    private static void Postfix(TextureRect ____bossIcon, TextureRect ____bossIconOutline,
        TextureRect? ____secondBossIcon, TextureRect? ____secondBossIconOutline)
    {
        Fit(____bossIcon, ____bossIconOutline);
        if (____secondBossIcon != null && ____secondBossIconOutline != null) Fit(____secondBossIcon, ____secondBossIconOutline);
    }

    private static void Fit(TextureRect icon, TextureRect outline)
    {
        if (icon.Texture is not { } texture || !texture.ResourcePath.Contains("/monsters/Spiegel_node")) return;
        // 顶栏空间很小，聚焦原图中心镜面；地图继续使用完整碎镜图。
        var region = new Rect2(490, 430, 490, 650);
        icon.Texture = new AtlasTexture { Atlas = texture, Region = region };
        if (outline.Texture is { } border) outline.Texture = new AtlasTexture { Atlas = border, Region = region };
        foreach (var view in new[] { icon, outline }) {
            view.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
            view.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        }
    }
}
