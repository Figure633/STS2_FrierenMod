using FrierenMod.Scripts.Events;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Events;

namespace FrierenMod.Scripts.Presentation;

// 保留原版布局、选项按钮和同步流程，仅调整本角色事件已有的图片与文字区域。
[HarmonyPatch(typeof(NEventLayout), nameof(NEventLayout.SetPortrait))]
public static class FrierenEventLayout
{
    private static void Postfix(NEventLayout __instance, EventModel ____event, Texture2D portrait)
    {
        if (____event is not FrierenStoryEvent and not ReadingGrimoireEvent) return;
        var image = __instance.GetNode<TextureRect>("%Portrait");
        // 原生左侧肖像是多余的第三张图：保留铺满背景与顶层事件内容。
        image.Hide();
        image.Scale = Vector2.One; image.PivotOffset = Vector2.Zero;
        Area(image, 0.035f, 0.17f, 0.465f, 0.94f);
        image.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        image.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        image.MouseFilter = Control.MouseFilterEnum.Ignore;

        var background = __instance.GetNodeOrNull<TextureRect>("FrierenEventBackdrop");
        if (background == null) {
            background = new TextureRect { Name = "FrierenEventBackdrop", ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered, MouseFilter = Control.MouseFilterEnum.Ignore,
                SelfModulate = new Color(0.25f, 0.25f, 0.25f) };
            __instance.AddChild(background); __instance.MoveChild(background, 0);
            background.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            background.OffsetTop = -__instance.OffsetTop;
            background.OffsetBottom = -__instance.OffsetBottom;
        }
        // 页面换图也同步背景；按视口铺满，避免窄图两边出现黑带。
        background.Texture = portrait;

        var shade = __instance.GetNodeOrNull<ColorRect>("FrierenEventTextShade");
        if (shade == null) {
            shade = new ColorRect { Name = "FrierenEventTextShade", Color = new Color(0.025f, 0.03f, 0.045f, 0.7f),
                MouseFilter = Control.MouseFilterEnum.Ignore };
            __instance.AddChild(shade); __instance.MoveChild(shade, 1);
            Area(shade, 0.49f, 0, 1, 1);
            shade.OffsetTop = -__instance.OffsetTop;
            shade.OffsetBottom = -__instance.OffsetBottom;
        }
        var column = __instance.GetNode<VBoxContainer>("VBoxContainer");
        Area(column, 0.52f, 0.16f, 0.965f, 0.16f);
    }

    private static void Area(Control control, float left, float top, float right, float bottom)
    {
        control.AnchorLeft = left; control.AnchorTop = top; control.AnchorRight = right; control.AnchorBottom = bottom;
        control.OffsetLeft = 0; control.OffsetTop = 0; control.OffsetRight = 0; control.OffsetBottom = 0;
    }
}
