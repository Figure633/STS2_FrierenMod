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
        if (____event is not FrierenStoryEvent and not ReadingGrimoireEvent and not EventArtPreview) return;
        var image = __instance.GetNode<TextureRect>("%Portrait");
        // 原生布局上下偏移-39；前景必须从顶栏(约100)下方开始，并延伸至视口底部。
        image.Show();
        image.Scale = Vector2.One; image.PivotOffset = Vector2.Zero;
        Area(image, 0, 0, 0.5f, 1);
        image.OffsetTop = 112f - __instance.OffsetTop;
        image.OffsetBottom = -__instance.OffsetBottom;
        image.StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered;
        if (____event is EventArtPreview) image.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        image.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        image.MouseFilter = Control.MouseFilterEnum.Ignore;

        var background = __instance.GetNodeOrNull<TextureRect>("FrierenEventBackdrop");
        if (background == null) {
            background = new TextureRect { Name = "FrierenEventBackdrop", ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered, MouseFilter = Control.MouseFilterEnum.Ignore };
            __instance.AddChild(background); __instance.MoveChild(background, 0);
            background.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            background.OffsetTop = -__instance.OffsetTop;
            background.OffsetBottom = -__instance.OffsetBottom;
        }
        // 页面换图也同步背景；按视口铺满，避免窄图两边出现黑带。
        background.Texture = portrait;
        // 原来左侧0.25，右侧再乘0.3约为0.075；两侧统一使用中间亮度。
        background.SelfModulate = new Color(0.16f, 0.16f, 0.16f);

        var shade = __instance.GetNodeOrNull<ColorRect>("FrierenEventTextShade");
        if (shade != null) {
            __instance.RemoveChild(shade);
            shade.QueueFree();
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
