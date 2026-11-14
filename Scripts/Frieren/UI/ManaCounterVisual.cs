using Godot;

namespace FrierenMod.Scripts.UI;

// 教程与实战共用原图坐标，避免数字与贴图各自缩放、排列。
internal static class ManaCounterVisual
{
    // 一代原始绘制点为贴图中的 (128,48)、(128,128)、(128,208)。
    public static float CenterY(int slot) => (48 + 80 * slot) * 264f / 256;

    public static TextureRect Layer(string name) => new() {
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        Texture = new AtlasTexture {
            Atlas = GD.Load<Texture2D>($"res://FrierenMod/images/ui/{name}.png"),
            Region = new(88, 0, 80, 256)
        },
        Size = new(68, 264),
        MouseFilter = Control.MouseFilterEnum.Ignore
    };

    public static Label Count(string text) {
        var label = new Label { Name = "Count", Text = text, Size = new(68, 36),
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore };
        label.AddThemeFontSizeOverride("font_size", 27);
        label.AddThemeColorOverride("font_outline_color", Colors.Black);
        label.AddThemeConstantOverride("outline_size", 7);
        return label;
    }
}
