using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.UI;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;

namespace FrierenMod.Scripts.Presentation;

[HarmonyPatch(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.SelectCharacter))]
public static class SlotBackSelectionEntry
{
    private static void Postfix(NCharacterSelectScreen __instance, CharacterModel characterModel) =>
        Attach(__instance, characterModel is Frieren);

    public static void Attach(Control screen, bool frieren)
    {
        if (screen.GetNodeOrNull<Control>("FrierenSlotBackSelection") is { } existing) {
            existing.Visible = frieren; return;
        }
        if (!frieren) return;
        var panel = new Control { Name = "FrierenSlotBackSelection", MouseFilter = Control.MouseFilterEnum.Ignore };
        screen.AddChild(panel);
        // 一代选人页的三槽预览位于右上，按1920×1080统一缩放。
        panel.Size = new(330, 220);
        void Layout() {
            float scale = Math.Min(screen.Size.X / 1920, screen.Size.Y / 1080);
            panel.Scale = Vector2.One * scale;
            panel.Position = new(screen.Size.X - 390 * scale, 65 * scale);
        }
        Layout(); screen.Resized += Layout;
        panel.TreeExiting += () => screen.Resized -= Layout;
        panel.AddChild(new ColorRect { Name = "ReadabilityBackground", Position = new(-20, -20), Size = new(370, 240),
            Color = new Color(0, 0, 0, .5f), MouseFilter = Control.MouseFilterEnum.Ignore });
        var title = new Label { Text = SlotBackLibrary.Text("BACK_LIBRARY"), Position = new(0, 0), Size = new(330, 36),
            MouseFilter = Control.MouseFilterEnum.Ignore };
        title.AddThemeFontSizeOverride("font_size", 23); panel.AddChild(title);
        var previews = new TextureButton[3];
        for (int i = 0; i < 3; i++) {
            int slot = i;
            var button = new TextureButton { Name = "SlotBack" + i, Position = new(10 + i * 110, 45), Size = new(75, 115),
                IgnoreTextureSize = true, StretchMode = TextureButton.StretchModeEnum.KeepAspectCentered };
            panel.AddChild(button); previews[i] = button;
            button.PivotOffset = button.Size / 2;
            var glow = new Panel { Name = "HoverGlow", Position = new(-5, -5), Size = button.Size + new Vector2(10, 10),
                Visible = false, ShowBehindParent = true, MouseFilter = Control.MouseFilterEnum.Ignore };
            glow.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(0, 0, 0, 0),
                BorderColor = new Color("95ff83"), BorderWidthLeft = 3, BorderWidthRight = 3, BorderWidthTop = 3, BorderWidthBottom = 3,
                CornerRadiusTopLeft = 7, CornerRadiusTopRight = 7, CornerRadiusBottomLeft = 7, CornerRadiusBottomRight = 7,
                ShadowColor = new Color(.35f, 1, .25f, .6f), ShadowSize = 9 });
            button.AddChild(glow);
            Tween? hoverTween = null;
            void Highlight(bool highlighted) {
                hoverTween?.Kill(); glow.Visible = highlighted;
                hoverTween = button.CreateTween();
                hoverTween.TweenProperty(button, "scale", Vector2.One * (highlighted ? 1.12f : 1), .12);
            }
            button.MouseEntered += () => Highlight(true);
            button.MouseExited += () => Highlight(button.HasFocus());
            button.FocusEntered += () => Highlight(true);
            button.FocusExited += () => Highlight(button.GetRect().HasPoint(panel.GetLocalMousePosition()));
            button.Pressed += () => SlotBackLibrary.Open(screen, slot);
            var label = new Label { Text = SlotBackLibrary.Text("CHANT_SLOT." + (Core.ManaSlot)i),
                Position = new(i * 110, 167), Size = new(98, 35), HorizontalAlignment = HorizontalAlignment.Center,
                MouseFilter = Control.MouseFilterEnum.Ignore };
            label.AddThemeFontSizeOverride("font_size", 20); panel.AddChild(label);
        }
        void Refresh() {
            for (int i = 0; i < 3; i++) {
                string id = SlotBackStore.Selected(i);
                previews[i].TextureNormal = SlotBackLibrary.Texture(id);
                previews[i].TooltipText = SlotBackLibrary.Text("SLOT." + (Core.ManaSlot)i) + " · " + id + "\n" + SlotBackLibrary.Text("BACK." + id + ".title");
            }
        }
        Refresh(); SlotBackStore.Changed += Refresh;
        panel.TreeExiting += () => SlotBackStore.Changed -= Refresh;
    }
}
