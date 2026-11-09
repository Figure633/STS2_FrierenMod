using FrierenMod.Scripts.Events;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Localization;

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
        if (____event is EventArtPreview or HollowKnightAdventure) image.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        image.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        image.MouseFilter = Control.MouseFilterEnum.Ignore;

        var background = __instance.GetNodeOrNull<TextureRect>("FrierenEventBackdrop");
        if (background == null) {
            background = new TextureRect { Name = "FrierenEventBackdrop", ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered, MouseFilter = Control.MouseFilterEnum.Ignore };
            __instance.AddChild(background); __instance.MoveChild(background, 0);
            __instance.Resized += () => {
                if (GodotObject.IsInstanceValid(__instance) && !__instance.IsQueuedForDeletion())
                    Compose(__instance, ____event);
            };
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
        Compose(__instance, ____event);
    }

    private static void Compose(NEventLayout layout, EventModel model)
    {
        var background = layout.GetNode<TextureRect>("FrierenEventBackdrop");
        if (background.Texture == null || layout.Size.X <= 0 || layout.Size.Y <= 0) return;
        var composition = EventArtComposition.For(model);
        var origin = new Vector2(0, -layout.OffsetTop);
        var backdropBase = layout.GetNodeOrNull<ColorRect>("FrierenEventBackdropBase");
        if (backdropBase == null) {
            backdropBase = new ColorRect { Name = "FrierenEventBackdropBase", Color = new Color(0.015f, 0.017f, 0.022f),
                MouseFilter = Control.MouseFilterEnum.Ignore };
            layout.AddChild(backdropBase);
        }
        backdropBase.Position = origin; backdropBase.Size = layout.Size;
        layout.MoveChild(backdropBase, 0); layout.MoveChild(background, 1);
        background.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopLeft);
        background.StretchMode = TextureRect.StretchModeEnum.Scale;
        var rect = composition.BackgroundRect(layout.Size, background.Texture.GetSize());
        background.Position = rect.Position + origin; background.Size = rect.Size;
        background.SelfModulate = composition.FullScene ? Colors.White : new Color(0.16f, 0.16f, 0.16f);
        layout.GetNode<TextureRect>("%Portrait").Visible = !composition.FullScene;

        var shade = layout.GetNodeOrNull<TextureRect>("FrierenFullSceneShade");
        if (shade == null) {
            shade = new TextureRect { Name = "FrierenFullSceneShade", MouseFilter = Control.MouseFilterEnum.Ignore,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, Texture = new GradientTexture2D {
                    Width = 256, Height = 1, FillFrom = Vector2.Zero, FillTo = Vector2.Right,
                    Gradient = new Gradient { Offsets = [0f, 0.35f, 0.65f, 1f],
                        Colors = [new Color(0, 0, 0, 0.05f), new Color(0, 0, 0, 0.05f),
                            new Color(0, 0, 0, 0.75f), new Color(0, 0, 0, 0.75f)] } } };
            layout.AddChild(shade);
        }
        layout.MoveChild(shade, 2); shade.Position = origin; shade.Size = layout.Size;
        shade.Visible = composition.FullScene;

        var credit = layout.GetNodeOrNull<Label>("FrierenEventArtCredit");
        if (credit == null) {
            credit = new Label { Name = "FrierenEventArtCredit", MouseFilter = Control.MouseFilterEnum.Ignore,
                HorizontalAlignment = HorizontalAlignment.Right, ZIndex = 10 };
            credit.AddThemeFontSizeOverride("font_size", 18);
            credit.AddThemeColorOverride("font_color", new Color(0.92f, 0.86f, 0.72f));
            credit.AddThemeColorOverride("font_shadow_color", Colors.Black);
            credit.AddThemeConstantOverride("shadow_offset_x", 2); credit.AddThemeConstantOverride("shadow_offset_y", 2);
            layout.AddChild(credit);
        }
        credit.Visible = model is EventArtPreview or HollowKnightAdventure;
        Area(credit, 0, 1, 0.5f, 1);
        credit.OffsetLeft = 18; credit.OffsetRight = -18;
        credit.OffsetTop = -34 - layout.OffsetBottom; credit.OffsetBottom = -8 - layout.OffsetBottom;
        if (credit.Visible) {
            var text = new LocString("frieren", "EVENT_ART_CREDIT");
            text.Add("Artist", composition.Artist ?? new LocString("frieren", "EVENT_ART_CREDIT_UNKNOWN").GetFormattedText());
            credit.Text = text.GetFormattedText();
        }
    }

    private static void Area(Control control, float left, float top, float right, float bottom)
    {
        control.AnchorLeft = left; control.AnchorTop = top; control.AnchorRight = right; control.AnchorBottom = bottom;
        control.OffsetLeft = 0; control.OffsetTop = 0; control.OffsetRight = 0; control.OffsetBottom = 0;
    }
}
