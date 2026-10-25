using Godot;
using MegaCrit.Sts2.Core.Localization;

namespace FrierenMod.Scripts.UI;

// 一代 TipHelper 的原生三段背景与阴影；不使用成就解锁弹窗的图片。
public partial class LegacyAchievementTip : PanelContainer
{
    public override void _Ready() { Resized += QueueRedraw; }
    public override void _Draw() {
        void Box(Vector2 offset, Color color) {
            foreach (var (part, y, height) in new[] { ("Top", 0f, 32f), ("Mid", 32f, Size.Y - 64), ("Bot", Size.Y - 32, 32f) })
                DrawTextureRect(GD.Load<Texture2D>($"res://FrierenMod/images/achievement/tip{part}.png"),
                    new Rect2(offset + new Vector2(0, y), new Vector2(Size.X, height)), false, color);
        }
        Box(new(9, 14), new Color(0, 0, 0, .3f)); Box(Vector2.Zero, Colors.White);
    }

    public static void ApplyFont(Label label, bool title)
    {
        bool chinese = LocManager.Instance.Language == "zhs";
        string file = chinese ? (title ? "SourceHanSerifSC-Bold.otf" : "NotoSansMonoCJKsc-Regular.otf")
            : (title ? "Kreon-Bold.ttf" : "Kreon-Regular.ttf");
        int size = title ? 23 : 22;
        var font = GD.Load<Font>($"res://FrierenMod/fonts/{file}");
        int trim = (title ? 32 : 26) - (int)Mathf.Ceil(font.GetHeight(size));
        var variation = new FontVariation { BaseFont = font, SpacingTop = trim / 2, SpacingBottom = trim - trim / 2 };
        label.AddThemeFontOverride("font", variation);
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeConstantOverride("line_spacing", 0);
        label.AddThemeConstantOverride("outline_size", title ? 2 : 0);
        label.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, .33f));
        label.AddThemeConstantOverride("shadow_offset_x", 2);
        label.AddThemeConstantOverride("shadow_offset_y", 2);
    }
}
