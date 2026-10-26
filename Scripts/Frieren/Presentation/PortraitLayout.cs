using FrierenMod.Scripts.Characters;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.RunHistoryScreen;
using MegaCrit.Sts2.Core.Runs;

namespace FrierenMod.Scripts.Presentation;

// 历史记录原生图标默认按纹理最小尺寸撑开；芙莉莲在既定容器内等比居中。
[HarmonyPatch(typeof(NRunHistoryPlayerIcon), nameof(NRunHistoryPlayerIcon.LoadRun))]
public static class PortraitLayout
{
    private const string OriginalLayout = "FrierenPortraitOriginalLayout";

    private static void Postfix(RunHistoryPlayer player, TextureRect ____icon)
    {
        if (player.Character == ModelDb.Character<Frieren>().Id) {
            if (!____icon.HasMeta(OriginalLayout))
                ____icon.SetMeta(OriginalLayout, new Vector2I((int)____icon.ExpandMode, (int)____icon.StretchMode));
            ____icon.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
            ____icon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
            ____icon.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        } else if (____icon.HasMeta(OriginalLayout)) {
            var original = ____icon.GetMeta(OriginalLayout).AsVector2I();
            ____icon.ExpandMode = (TextureRect.ExpandModeEnum)original.X;
            ____icon.StretchMode = (TextureRect.StretchModeEnum)original.Y;
            ____icon.RemoveMeta(OriginalLayout);
            ____icon.ResetSize();
        }
    }
}
