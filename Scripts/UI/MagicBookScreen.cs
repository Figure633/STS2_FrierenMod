using FrierenMod.Core;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Mechanics;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Pooling;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.TestSupport;

namespace FrierenMod.Scripts.UI;

// 与一代一致：右键启用魔具，在同一页左键选满目标后结算；库存规则由 MagicPropRules 负责。
public sealed class MagicBookScreen
{
    public Control View { get; }
    private readonly Player _player;
    private readonly Control _canvas;
    private readonly Control _equipped;
    private readonly GridContainer _inventory;
    private readonly Label _status;
    private readonly Dictionary<int, NGridCardHolder> _cards = [];
    private readonly List<int> _selected = [];
    private readonly Dictionary<int, (NCardHolder.PressedEventHandler Press, NCardHolder.AltPressedEventHandler Alt)> _handlers = [];
    private int? _armed;
    private string _displayed = "";
    private bool _applying;
    private bool _inputCaptured;

    private static string Text(string key) => new LocString("frieren", key).GetFormattedText();

    private static Label Caption(string text, int size, Vector2 position, Vector2 bounds)
    {
        var label = new Label { Text = text, Position = position, Size = bounds,
            HorizontalAlignment = HorizontalAlignment.Center, MouseFilter = Control.MouseFilterEnum.Ignore };
        label.AddThemeFontOverride("font", GD.Load<Font>("res://themes/kreon_bold_shared.tres"));
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", new Color("fff1d5"));
        label.AddThemeColorOverride("font_outline_color", new Color("17120e"));
        label.AddThemeConstantOverride("outline_size", 5);
        return label;
    }

    public MagicBookScreen(Control root, Player player)
    {
        _player = player;
        View = new Control { Name = "FrierenMagicBook", Visible = false, ZIndex = 6, MouseFilter = Control.MouseFilterEnum.Stop };
        root.AddChild(View);
        View.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _canvas = new Control { Name = "BookCanvas", Size = new Vector2(1920, 1080), MouseFilter = Control.MouseFilterEnum.Ignore };
        View.AddChild(_canvas);
        void Resize() {
            float scale = Math.Min(root.Size.X / 1920, root.Size.Y / 1080);
            _canvas.Scale = Vector2.One * scale;
            _canvas.Position = (root.Size - _canvas.Size * scale) / 2;
        }
        root.Resized += Resize;
        Resize();
        _canvas.AddChild(Caption(Text("BOOK"), 38, new(300, 45), new(1320, 55)));
        _equipped = new Control { Name = "EquippedFactors", MouseFilter = Control.MouseFilterEnum.Ignore };
        _canvas.AddChild(_equipped);
        for (int i = 0; i < 3; i++)
            _canvas.AddChild(Caption(Text("SLOT." + (ManaSlot)i), 25, new(480 + i * 360, 117), new(240, 42)));
        var divider = new ColorRect { Position = new(260, 510), Size = new(1400, 2),
            Color = new Color("ab91686b"), MouseFilter = Control.MouseFilterEnum.Ignore };
        _canvas.AddChild(divider);
        _canvas.AddChild(Caption(Text("BOOK_INVENTORY"), 24, new(400, 520), new(1120, 36)));
        var scroll = new ScrollContainer { Name = "InventoryScroll", Position = new(255, 565), Size = new(1410, 385),
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, VerticalScrollMode = ScrollContainer.ScrollMode.Auto };
        _canvas.AddChild(scroll);
        _inventory = new GridContainer { Name = "Inventory", Columns = 5, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _inventory.AddThemeConstantOverride("h_separation", 10);
        _inventory.AddThemeConstantOverride("v_separation", 30);
        scroll.AddChild(_inventory);
        _status = Caption(Text("BOOK_HINT"), 23, new(330, 978), new(1260, 78));
        _status.Name = "BookStatus";
        _status.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _canvas.AddChild(_status);

        // 原生返回按钮保留二代的贴图、悬停动画和输入提示。
        var back = GD.Load<PackedScene>("res://scenes/ui/back_button.tscn").Instantiate<MegaCrit.Sts2.Core.Nodes.CommonUi.NBackButton>();
        _canvas.AddChild(back);
        back.Connect(MegaCrit.Sts2.Core.Nodes.GodotExtensions.NButton.SignalName.Released, Callable.From<MegaCrit.Sts2.Core.Nodes.GodotExtensions.NButton>(_ => Back()));
        View.VisibilityChanged += () => {
            bool visible = View.IsVisibleInTree();
            if (visible && !_inputCaptured) { NHotkeyManager.Instance?.AddBlockingScreen(View); back.Enable(); }
            else if (!visible && _inputCaptured) { back.Disable(); NHotkeyManager.Instance?.RemoveBlockingScreen(View); }
            _inputCaptured = visible;
        };
        View.TreeExiting += () => { NHotkeyManager.Instance?.RemoveBlockingScreen(View); ReleaseCards(); };
        // 隐藏的快捷键按钮只处理 Esc，不再增加一套可见按钮皮肤。
        var escape = new Button { Name = "CancelShortcut", Flat = true, Size = Vector2.One, Modulate = Colors.Transparent,
            MouseFilter = Control.MouseFilterEnum.Ignore, FocusMode = Control.FocusModeEnum.None,
            Shortcut = new Shortcut { Events = new Godot.Collections.Array { new InputEventKey { Keycode = Key.Escape } } } };
        _canvas.AddChild(escape);
        escape.Pressed += Back;
        View.VisibilityChanged += () => { if (!View.Visible) Cancel(); };
        View.TreeExiting += () => root.Resized -= Resize;
    }

    public void Open() { Cancel(); Populate(); View.Show(); }
    public void Close() { Cancel(); View.Hide(); }
    private void Back() { if (_armed.HasValue) Cancel(); else Close(); }

    private void Cancel()
    {
        _armed = null;
        _selected.Clear();
        RefreshSelection();
        _status.Text = Text("BOOK_HINT");
    }

    public void Refresh()
    {
        if (!View.Visible) return;
        var deck = MagicDeckStore.Read(_player);
        if (_displayed != deck.ToJson()) { Cancel(); Populate(); }
        if (_armed.HasValue && !_applying && !MagicPropCmd.CanUse(_player, _armed.Value)) Cancel();
    }

    private static NGridCardHolder CreateHolder(CardModel model)
    {
        if (!TestMode.IsOn) return NGridCardHolder.Create(NCard.Create(model)!)!;
        // 测试模式禁用原生工厂；仍实例化完全相同的场景，并保留内存存档隔离。
        var card = NodePool.Get<NCard>();
        AccessTools.Property(typeof(NCard), nameof(NCard.Model)).SetValue(card, model);
        var holder = NodePool.Get<NGridCardHolder>();
        holder.Call(NCardHolder.MethodName.SetCard, card);
        holder.Call(NGridCardHolder.MethodName.UpdateCardModel);
        holder.Scale = holder.SmallScale;
        return holder;
    }

    private void ReleaseCards()
    {
        foreach (var (id, holder) in _cards) {
            // 先从树中移除，再交还原生对象池；避免父节点释放时销毁已归还的卡牌。
            if (GodotObject.IsInstanceValid(holder)) {
                var handlers = _handlers[id];
                holder.Pressed -= handlers.Press;
                holder.AltPressed -= handlers.Alt;
                holder.GetParent()?.RemoveChild(holder);
                holder.QueueFreeSafely();
            }
        }
        _cards.Clear();
        _handlers.Clear();
    }

    private void Populate()
    {
        ReleaseCards();
        foreach (var node in _equipped.GetChildren().Concat(_inventory.GetChildren())) { node.GetParent().RemoveChild(node); node.QueueFree(); }
        _cards.Clear();
        var deck = MagicDeckStore.Read(_player);
        void Add(CardModel model, int id, int slot, bool prop) {
            var cell = new Control { Name = "Item_" + id, CustomMinimumSize = new(270, 385), MouseFilter = Control.MouseFilterEnum.Pass };
            if (slot >= 0) {
                cell.Position = new(465 + slot * 360, 160);
                _equipped.AddChild(cell);
            } else _inventory.AddChild(cell);
            // 原生卡牌悬停会从 0.8 放大到 1；父层统一缩放，预留边界避免裁切。
            var scaleRoot = new Control { Scale = Vector2.One * 0.9f, Position = new(135, slot >= 0 ? 172 : 192),
                MouseFilter = Control.MouseFilterEnum.Ignore };
            cell.AddChild(scaleRoot);
            var holder = CreateHolder(model);
            holder.Name = (prop ? "Prop_" : "Factor_") + id;
            scaleRoot.AddChild(holder);
            holder.CardNode!.UpdateVisuals(PileType.None, CardPreviewMode.Normal);
            NCardHolder.PressedEventHandler press = _ => Select(id);
            NCardHolder.AltPressedEventHandler alt = _ => { if (prop) Arm(id); };
            holder.Pressed += press;
            holder.AltPressed += alt;
            _handlers.Add(id, (press, alt));
            _cards.Add(id, holder);
        }
        foreach (var item in deck.Items.OrderBy(i => i.Slot < 0 ? 3 : i.Slot)) {
            var card = FactorCard.Create(_player, item.SpellId, item.Slot < 0 ? null : (ManaSlot)item.Slot);
            card.InventoryInstanceId = item.InstanceId;
            Add(card, item.InstanceId, item.Slot, false);
        }
        foreach (var prop in deck.Props) Add(MagicPropCard.Create(_player, prop.PropId), prop.InstanceId, -1, true);
        if (_inventory.GetChildCount() == 0) {
            var empty = Caption(Text("NO_PROPS"), 23, Vector2.Zero, new(1380, 160));
            empty.CustomMinimumSize = new(1380, 160); empty.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            _inventory.AddChild(empty);
        }
        _displayed = deck.ToJson();
        RefreshSelection();
    }

    private void Arm(int id)
    {
        if (_applying) return;
        if (_armed == id) { Cancel(); return; }
        if (!MagicPropCmd.CanUse(_player, id)) { _status.Text = Text("PROP_UNAVAILABLE"); return; }
        _armed = id; _selected.Clear();
        UpdatePrompt();
        RefreshSelection();
        var prop = MagicDeckStore.Read(_player).Props.Single(p => p.InstanceId == id);
        if (MagicPropRules.Get(prop.PropId).Targets == 0) _ = Apply();
    }

    private void Select(int id)
    {
        if (_applying || !_armed.HasValue) return;
        var deck = MagicDeckStore.Read(_player);
        var prop = deck.Props.SingleOrDefault(p => p.InstanceId == _armed.Value);
        if (prop == null || !MagicPropCmd.CanUse(_player, prop.InstanceId)) { Cancel(); return; }
        if (_selected.Remove(id)) { UpdatePrompt(); RefreshSelection(); return; }
        if (!MagicPropRules.Eligible(deck, prop).Contains(id)) return;
        _selected.Add(id);
        UpdatePrompt();
        RefreshSelection();
        if (_selected.Count == MagicPropRules.Get(prop.PropId).Targets) _ = Apply();
    }

    private void UpdatePrompt()
    {
        var prop = MagicDeckStore.Read(_player).Props.Single(p => p.InstanceId == _armed);
        var text = new LocString("frieren", "BOOK_TARGETS");
        text.Add("Prop", MagicPropCard.Create(_player, prop.PropId).Title);
        text.Add("Selected", _selected.Count);
        text.Add("Amount", MagicPropRules.Get(prop.PropId).Targets);
        _status.Text = text.GetFormattedText();
    }

    private void RefreshSelection()
    {
        var deck = MagicDeckStore.Read(_player);
        var prop = deck.Props.SingleOrDefault(p => p.InstanceId == _armed);
        var eligible = prop == null ? [] : MagicPropRules.Eligible(deck, prop);
        foreach (var (id, holder) in _cards) {
            bool selected = _armed == id || _selected.Contains(id);
            holder.Modulate = prop != null && !selected && !eligible.Contains(id) ? new Color("777777") : Colors.White;
            if (selected) { holder.CardNode!.CardHighlight.Modulate = new Color("9cf1ac"); holder.CardNode.CardHighlight.AnimShow(); }
            else holder.CardNode!.CardHighlight.AnimHideInstantly();
        }
    }

    private async Task Apply()
    {
        if (!_armed.HasValue || _applying) return;
        _applying = true;
        int prop = _armed.Value;
        var targets = _selected.ToArray();
        try {
            bool success = await MagicPropCmd.UseSelected(_player, prop, targets);
            if (!GodotObject.IsInstanceValid(View) || !View.IsInsideTree()) return;
            Cancel(); Populate();
            _status.Text = Text(success ? "PROP_USED" : "PROP_CANCELLED");
        } catch (Exception ex) {
            MegaCrit.Sts2.Core.Logging.Log.Error("Frieren 魔具使用失败：" + ex);
            if (GodotObject.IsInstanceValid(View) && View.IsInsideTree()) { Cancel(); _status.Text = Text("PROP_ERROR"); }
        } finally { _applying = false; }
    }
}
