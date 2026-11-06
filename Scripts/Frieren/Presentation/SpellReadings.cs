using FrierenMod.Scripts.Cards;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace FrierenMod.Scripts.Presentation;

// 独立小字继承原生标题字体和卡面缩放，不改变牌名及其检索、存档ID。
[HarmonyPatch(typeof(NCard), nameof(NCard.UpdateVisuals))]
public static class SpellReadings
{
    public static string Reading(CardModel? card) => card switch {
        OrdinaryOffensiveMagic => "Zoltraak", BloodSpell => "Balterie", MetalPetalSpell => "Jubelade",
        IceArrows => "Nephtear", HighSpeedMovement => "Jilwer", BlackGoldWings => "Digardnacht", _ => ""
    };
    private static void Postfix(NCard __instance)
    {
        if (!__instance.IsNodeReady() || __instance.GetNodeOrNull<Label>("%TitleLabel") is not {} title) return;
        var parent = title.GetParent();
        var label = parent.GetNodeOrNull<Label>("FrierenSpellReading");
        var reading = Reading(__instance.Model);
        if (label == null && reading.Length > 0) {
            label = new Label { Name = "FrierenSpellReading", MouseFilter = Control.MouseFilterEnum.Ignore,
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
                Position = new Vector2(title.Position.X - 25, title.Position.Y - 19), Size = new Vector2(title.Size.X + 50, 20) };
            label.AddThemeFontOverride("font", title.GetThemeFont("font"));
            label.AddThemeFontSizeOverride("font_size", 14);
            label.AddThemeColorOverride("font_color", new Color("fff2cf"));
            label.AddThemeColorOverride("font_outline_color", Colors.Black);
            label.AddThemeConstantOverride("outline_size", 3);
            parent.AddChild(label);
        }
        if (label != null) { label.Text = reading; label.Visible = reading.Length > 0; }
    }
}
