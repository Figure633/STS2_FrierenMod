using FrierenMod.Core;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Powers;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Runs;

namespace FrierenMod.Scripts.UI;

[HarmonyPatch(typeof(NRun), nameof(NRun._Ready))]
public static class MagicBookUi
{
    private static string Text(string key) => new LocString("frieren", key).GetFormattedText();
    private static Label Label(string text, int size = 20)
    {
        var label = new Label { Text = text, MouseFilter = Control.MouseFilterEnum.Ignore };
        label.AddThemeFontSizeOverride("font_size", size);
        return label;
    }

    public static void Postfix(NRun __instance, RunState ____state)
    {
        var player = LocalContext.GetMe(____state);
        if (player?.Character is not Frieren) return;
        Attach(__instance.GlobalUi, player);
    }

    public static void Attach(Control root, Player player)
    {
        var toggle = new Button { Name = "FrierenMagicBookButton", Text = Text("BOOK_BUTTON"), Flat = true, ZIndex = 4 };
        toggle.Icon = GD.Load<Texture2D>("res://FrierenMod/images/ui/magic_deck.png");
        toggle.AddThemeConstantOverride("icon_max_width", 42);
        toggle.AddThemeFontOverride("font", GD.Load<Font>("res://themes/kreon_bold_shared.tres"));
        toggle.AddThemeFontSizeOverride("font_size", 23);
        toggle.AddThemeColorOverride("font_color", new Color("fff1d5"));
        root.AddChild(toggle);
        toggle.SetAnchorsPreset(Control.LayoutPreset.TopRight);
        toggle.OffsetLeft = -240;
        toggle.OffsetTop = 140;
        toggle.OffsetRight = -40;
        toggle.OffsetBottom = 184;

        var hud = new Control { Name = "FrierenManaCounter", Position = new Vector2(20, 285),
            Size = new Vector2(108, 264), MouseFilter = Control.MouseFilterEnum.Ignore };
        root.AddChild(hud);
        var slots = Enum.GetValues<ManaSlot>();
        var manaNumbers = new Label[slots.Length];
        var hoverAreas = new Control[slots.Length];
        TextureRect Layer(string name)
        {
            var layer = new TextureRect {
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                Texture = new AtlasTexture { Atlas = GD.Load<Texture2D>($"res://FrierenMod/images/ui/{name}.png"), Region = new Rect2(88, 0, 80, 256) },
                Size = new Vector2(68, 264), MouseFilter = Control.MouseFilterEnum.Ignore };
            hud.AddChild(layer);
            return layer;
        }
        var manaFill = Layer("MPLayer0");
        Layer("MPWrap");
        for (int i = 0; i < slots.Length; i++)
        {
            var hover = new Control { Name = "Slot" + slots[i], Position = new Vector2(0, 88 * i),
                Size = new Vector2(108, 88), MouseFilter = Control.MouseFilterEnum.Pass };
            hud.AddChild(hover);
            hoverAreas[i] = hover;
            manaNumbers[i] = Label("0", 27);
            manaNumbers[i].Name = "Count";
            manaNumbers[i].Position = new Vector2(0, 20);
            manaNumbers[i].Size = new Vector2(68, 36);
            manaNumbers[i].HorizontalAlignment = HorizontalAlignment.Center;
            manaNumbers[i].AddThemeColorOverride("font_outline_color", Colors.Black);
            manaNumbers[i].AddThemeConstantOverride("outline_size", 7);
            hover.AddChild(manaNumbers[i]);
            var caption = Label(Text("SLOT_SHORT." + slots[i]), 16);
            caption.Position = new Vector2(70, 28);
            caption.AddThemeColorOverride("font_outline_color", Colors.Black);
            caption.AddThemeConstantOverride("outline_size", 5);
            hover.AddChild(caption);
        }

        var backdrop = new ColorRect { Name = "FrierenBookBackdrop", Visible = false, ZIndex = 5,
            Color = new Color(0.035f, 0.028f, 0.023f, 0.94f), MouseFilter = Control.MouseFilterEnum.Stop };
        root.AddChild(backdrop);
        backdrop.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var screen = new MagicBookScreen(root, player);
        var book = screen.View;
        book.VisibilityChanged += () => {
            backdrop.Visible = book.Visible;
            hud.Visible = CombatManager.Instance.IsInProgress && !book.Visible && (NOverlayStack.Instance?.ScreenCount ?? 0) == 0;
        };
        backdrop.GuiInput += input => {
            if (input is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) {
                screen.Close(); backdrop.AcceptEvent();
            }
        };
        toggle.Pressed += () => { if (book.Visible) screen.Close(); else screen.Open(); };
        var names = SpellCatalog.All.ToDictionary(x => x.Id, x => FactorCard.Create(player, x.Id).Title);
        var timer = new Godot.Timer { Name = "FrierenManaRefresh", WaitTime = 0.1, Autostart = true };
        timer.Timeout += () =>
        {
            bool overlay = (NOverlayStack.Instance?.ScreenCount ?? 0) > 0;
            toggle.Disabled = overlay;
            if (overlay) screen.Close();
            hud.Visible = CombatManager.Instance.IsInProgress && !overlay && !book.Visible;
            var deck = MagicDeckStore.Read(player);
            foreach (var slot in Enum.GetValues<ManaSlot>())
            {
                var factor = deck.InSlot(slot);
                int mana = ManaCmd.InPile(player, ManaCmd.Pile(slot)).Count();
                string name = factor == null ? Text("EMPTY") : names[factor.Id];
                var spell = deck.ResolveSlot(slot);
                int cost = spell == null ? 0 : ChantRules.Cost(1, spell,
                    player.Creature.GetPower<ConcentrationPower>()?.Amount ?? 0,
                    player.Creature.GetPower<WeakenedChantPower>()?.Amount ?? 0,
                    player.Creature.GetPower<ChantWithoutManaPower>() != null || player.Creature.GetPower<ChantWithoutManaTimesPower>() != null);
                hoverAreas[(int)slot].TooltipText = $"{Text($"SLOT.{slot}")} · {Text("MANA")}: {mana}\n{name}\n" +
                    (spell == null ? Text("SLOT_UNAVAILABLE") : $"{Text("COST_ONE")}: {cost}") + "\n" + Text("MANA_HINT");
                manaNumbers[(int)slot].Text = mana.ToString();
                manaNumbers[(int)slot].Modulate = spell != null && mana >= cost ? new Color("b9f4e2") : Colors.White;
            }
            manaFill.Modulate = Enum.GetValues<ManaSlot>().Any(s => ManaCmd.InPile(player, ManaCmd.Pile(s)).Any()) ? Colors.White : new Color("777777");
            screen.Refresh();
        };
        root.AddChild(timer);
    }
}
