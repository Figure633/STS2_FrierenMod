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
        panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomRight);
        panel.OffsetLeft = -545; panel.OffsetRight = -100; panel.OffsetTop = -345; panel.OffsetBottom = -95;
        var title = new Label { Text = SlotBackLibrary.Text("BACK_LIBRARY"), Position = new(0, 0), Size = new(440, 36),
            MouseFilter = Control.MouseFilterEnum.Ignore };
        title.AddThemeFontSizeOverride("font_size", 23); panel.AddChild(title);
        var previews = new TextureButton[3];
        for (int i = 0; i < 3; i++) {
            int slot = i;
            var button = new TextureButton { Name = "SlotBack" + i, Position = new(10 + i * 145, 45), Size = new(105, 161),
                IgnoreTextureSize = true, StretchMode = TextureButton.StretchModeEnum.KeepAspectCentered };
            panel.AddChild(button); previews[i] = button;
            button.Pressed += () => SlotBackLibrary.Open(screen, slot);
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
