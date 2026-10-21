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
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
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

    public static void Attach(Control root, Player player, Func<IOverlayScreen?>? currentOverlay = null,
        Func<bool>? inputBlocked = null)
    {
        currentOverlay ??= () => NOverlayStack.Instance?.Peek();
        inputBlocked ??= () => root is NGlobalUi global &&
            (global.CapstoneContainer.InUse || NModalContainer.Instance?.OpenModal != null);
        var toggle = MagicFactorButton.Create(root);

        var hud = new Control { Name = "FrierenManaCounter",
            Size = new Vector2(108, 264), MouseFilter = Control.MouseFilterEnum.Pass };
        root.AddChild(hud);
        hud.SetAnchorsPreset(Control.LayoutPreset.CenterLeft);
        hud.OffsetLeft = 20;
        hud.OffsetRight = 128;
        hud.OffsetTop = -132;
        hud.OffsetBottom = 132;
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

        var backdrop = new ColorRect { Name = "FrierenBookBackdrop", Visible = false, ZIndex = 99, ZAsRelative = false,
            Color = new Color(0.035f, 0.028f, 0.023f, 0.94f), MouseFilter = Control.MouseFilterEnum.Stop };
        root.AddChild(backdrop);
        backdrop.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var preview = new ManaFactorPreview(root, hud, player);
        foreach (var area in hoverAreas) {
            area.MouseEntered += preview.Show;
            area.MouseExited += () => {
                if (!hud.GetGlobalRect().HasPoint(hud.GetGlobalMousePosition())) preview.Hide();
            };
        }
        hud.MouseEntered += preview.Show;
        hud.MouseExited += preview.Hide;
        var screen = new MagicBookScreen(root, player, currentOverlay);
        var book = screen.View;
        var animations = new Control { Name = "FrierenMagicDeckAnimations", ZIndex = 101,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            MouseBehaviorRecursive = Control.MouseBehaviorRecursiveEnum.Disabled,
            FocusBehaviorRecursive = Control.FocusBehaviorRecursiveEnum.Disabled };
        root.AddChild(animations);
        animations.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        MagicDeckAnimations.Bind(player, animations, toggle, screen);
        book.VisibilityChanged += () => {
            toggle.SetBookOpen(book.Visible);
            backdrop.Visible = book.Visible;
            hud.Visible = CombatManager.Instance.IsInProgress && !book.Visible && currentOverlay() == null;
            if (!hud.Visible) preview.Hide();
        };
        backdrop.GuiInput += input => {
            if (input is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) {
                screen.Close(); backdrop.AcceptEvent();
            }
        };
        toggle.Released += _ => {
            if (inputBlocked()) return;
            if (book.Visible) screen.Close(); else screen.Open();
        };
        void RefreshInput()
        {
            bool blocked = inputBlocked();
            toggle.SetEnabled(!blocked);
            foreach (var view in new[] { hud, book, backdrop }) {
                view.MouseBehaviorRecursive = blocked ? Control.MouseBehaviorRecursiveEnum.Disabled : Control.MouseBehaviorRecursiveEnum.Inherited;
                view.FocusBehaviorRecursive = blocked ? Control.FocusBehaviorRecursiveEnum.Disabled : Control.FocusBehaviorRecursiveEnum.Inherited;
                view.ProcessMode = blocked ? Node.ProcessModeEnum.Disabled : Node.ProcessModeEnum.Inherit;
            }
            if (blocked) preview.Hide();
        }
        ActiveScreenContext.Instance.Updated += RefreshInput;
        root.TreeExiting += () => ActiveScreenContext.Instance.Updated -= RefreshInput;
        var timer = new Godot.Timer { Name = "FrierenManaRefresh", WaitTime = 0.1, Autostart = true };
        timer.Timeout += () =>
        {
            RefreshInput();
            bool overlay = currentOverlay() != null;
            hud.Visible = CombatManager.Instance.IsInProgress && !overlay && !book.Visible;
            if (!hud.Visible) preview.Hide();
            var deck = MagicDeckStore.Read(player);
            foreach (var slot in Enum.GetValues<ManaSlot>())
            {
                int mana = ManaCmd.InPile(player, ManaCmd.Pile(slot)).Count();
                var spell = deck.ResolveSlot(slot);
                int cost = spell == null ? 0 : ChantRules.Cost(1, spell,
                    player.Creature.GetPower<ConcentrationPower>()?.Amount ?? 0,
                    player.Creature.GetPower<WeakenedChantPower>()?.Amount ?? 0,
                    player.Creature.GetPower<ChantWithoutManaPower>() != null || player.Creature.GetPower<ChantWithoutManaTimesPower>() != null);
                manaNumbers[(int)slot].Text = mana.ToString();
                manaNumbers[(int)slot].Modulate = spell != null && mana >= cost ? new Color("b9f4e2") : Colors.White;
            }
            manaFill.Modulate = Enum.GetValues<ManaSlot>().Any(s => ManaCmd.InPile(player, ManaCmd.Pile(s)).Any()) ? Colors.White : new Color("777777");
            screen.Refresh();
            preview.Refresh();
        };
        root.AddChild(timer);
        if (root is NGlobalUi globalUi) {
            // 因子查看位于选牌层之上、原生菜单遮罩之下，暂停时自然变暗且不会挡住菜单。
            foreach (var view in new[] { hud, root.GetNode<Control>("FrierenFactorPreview"), backdrop, book, animations }) {
                view.ZIndex = 0; view.ZAsRelative = true;
                root.MoveChild(view, globalUi.CapstoneContainer.GetIndex());
            }
        }
        RefreshInput();
    }
}
