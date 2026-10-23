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
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;

namespace FrierenMod.Scripts.UI;

// 与一代一致：右键启用魔具，在同一页左键选满目标后结算；库存规则由 MagicPropRules 负责。
public sealed class MagicBookScreen
{
    public Control View { get; }
    private readonly Player _player;
    private readonly Control _canvas;
    private readonly Func<IOverlayScreen?> _currentOverlay;
    private readonly NCapstoneContainer? _capstone;
    private IOverlayScreen? _coveredOverlay;
    private readonly Control _equipped;
    private readonly ScrollContainer _scroll;
    private readonly Control _scrollContent;
    private readonly GridContainer _inventory;
    private readonly Label _status;
    private readonly NCardViewSortButton _raritySorter;
    private readonly NCardViewSortButton _typeSorter;
    private readonly Dictionary<int, (int Rarity, int Type)> _inventorySortKeys = [];
    private bool _sortByType;
    private readonly Dictionary<int, NGridCardHolder> _cards = [];
    private readonly List<int> _selected = [];
    private readonly Dictionary<int, (NCardHolder.PressedEventHandler Press, NCardHolder.AltPressedEventHandler Alt,
        NClickableControl.FocusedEventHandler Focus)> _handlers = [];
    private int? _armed;
    private string _displayed = "";
    private bool _applying;
    private bool _presentingChange;
    private bool _inputCaptured;
    private (int Id, MouseButton Button)? _pointerPress;

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

    public MagicBookScreen(Control root, Player player, Func<IOverlayScreen?>? currentOverlay = null)
    {
        _player = player;
        _currentOverlay = currentOverlay ?? (() => NOverlayStack.Instance?.Peek());
        _capstone = (root as NGlobalUi)?.CapstoneContainer;
        // 实机由原生 Capstone 管理层级，避免越过其上的教程和确认窗口。
        View = new MagicBookView { Name = "FrierenMagicBook", Visible = false, ZIndex = _capstone != null ? 0 : 100,
            ZAsRelative = false, MouseFilter = Control.MouseFilterEnum.Stop };
        (_capstone as Control ?? root).AddChild(View);
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
        _scroll = new ScrollContainer { Name = "InventoryScroll", Position = new(255, 115), Size = new(1410, 890),
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, VerticalScrollMode = ScrollContainer.ScrollMode.Auto };
        _canvas.AddChild(_scroll);
        _scrollContent = new Control { Name = "BookContents", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            MouseFilter = Control.MouseFilterEnum.Ignore };
        _scroll.AddChild(_scrollContent);
        _equipped = new Control { Name = "EquippedFactors", MouseFilter = Control.MouseFilterEnum.Ignore };
        _scrollContent.AddChild(_equipped);
        for (int i = 0; i < 3; i++)
            _scrollContent.AddChild(Caption(Text("SLOT." + (ManaSlot)i), 25, new(225 + i * 360, 2), new(240, 42)));
        var divider = new ColorRect { Position = new(5, 395), Size = new(1380, 2),
            Color = new Color("ab91686b"), MouseFilter = Control.MouseFilterEnum.Ignore };
        _scrollContent.AddChild(divider);
        _scrollContent.AddChild(Caption(Text("BOOK_INVENTORY"), 24, new(25, 405), new(480, 42)));
        NCardViewSortButton Sorter(string name, string key, float x, bool byType) {
            var sorter = GD.Load<PackedScene>("res://scenes/screens/deck_view_screen/deck_view_sort_button.tscn")
                .Instantiate<NCardViewSortButton>();
            sorter.Name = name;
            _scrollContent.AddChild(sorter);
            sorter.Position = new(x, 405); sorter.Size = new(240, 42);
            sorter.SetLabel(Text(key));
            sorter.Released += _ => {
                if (_sortByType != byType) { _sortByType = byType; sorter.IsDescending = false; }
                SortInventory();
            };
            return sorter;
        }
        _raritySorter = Sorter("RaritySorter", "SORT_RARITY", 645, false);
        _typeSorter = Sorter("TypeSorter", "SORT_TYPE", 905, true);
        _inventory = new GridContainer { Name = "Inventory", Columns = 5 };
        _inventory.AddThemeConstantOverride("h_separation", 10);
        _inventory.AddThemeConstantOverride("v_separation", 12);
        _scrollContent.AddChild(_inventory);
        _inventory.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopWide);
        _inventory.Position = new(0, 450);
        _inventory.MinimumSizeChanged += UpdateScrollHeight;
        _status = Caption(Text("BOOK_HINT"), 23, new(330, 1016), new(1260, 58));
        _status.Name = "BookStatus";
        _status.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _canvas.AddChild(_status);

        // 原生按钮会在悬停时将自身缩放恢复为 1；缩放放在父节点，避免按钮越出屏幕。
        var helpContainer = new Control { Name = "MagicTutorialButtonContainer", Position = new(1680, 155),
            Size = new(324, 64), Scale = Vector2.One * 0.65f, MouseFilter = Control.MouseFilterEnum.Ignore };
        _canvas.AddChild(helpContainer);
        var help = new BaseLib.Config.UI.NConfigButton { Name = "ReplayMagicTutorial" };
        help.Initialize(Text("TUTORIAL_HELP"), () => {
            if (!_applying && !_presentingChange) TaskHelper.RunSafely(FrierenTutorial.Show(_player, replay: true));
        });
        helpContainer.AddChild(help);

        // 原生返回按钮保留二代的贴图、悬停动画和输入提示。
        var back = GD.Load<PackedScene>("res://scenes/ui/back_button.tscn").Instantiate<MegaCrit.Sts2.Core.Nodes.CommonUi.NBackButton>();
        _canvas.AddChild(back);
        ((MagicBookView)View).DefaultFocusedControl = back;
        back.Connect(MegaCrit.Sts2.Core.Nodes.GodotExtensions.NButton.SignalName.Released, Callable.From<MegaCrit.Sts2.Core.Nodes.GodotExtensions.NButton>(_ => Back()));
        View.VisibilityChanged += () => {
            bool visible = View.IsVisibleInTree();
            if (visible && !_inputCaptured) { NHotkeyManager.Instance?.AddBlockingScreen(View); back.Enable(); }
            else if (!visible && _inputCaptured) { back.Disable(); NHotkeyManager.Instance?.RemoveBlockingScreen(View); }
            _inputCaptured = visible;
        };
        View.TreeExiting += () => { RestoreOverlay(); NHotkeyManager.Instance?.RemoveBlockingScreen(View); ReleaseCards(); };
        // 隐藏的快捷键按钮只处理 Esc，不再增加一套可见按钮皮肤。
        var escape = new Button { Name = "CancelShortcut", Flat = true, Size = Vector2.One, Modulate = Colors.Transparent,
            MouseFilter = Control.MouseFilterEnum.Ignore, FocusMode = Control.FocusModeEnum.None,
            Shortcut = new Shortcut { Events = new Godot.Collections.Array { new InputEventKey { Keycode = Key.Escape } } } };
        _canvas.AddChild(escape);
        escape.Pressed += Back;
        View.VisibilityChanged += () => { if (!View.Visible) { Cancel(); RestoreOverlay(); } };
        View.TreeExiting += () => root.Resized -= Resize;
        var pointer = new MagicBookPointerInput { Name = "FrierenBookPointer", HandleButton = HandlePointer };
        // 挂到视口末尾，先于游戏场景和其他界面的右键处理器接收输入。
        root.GetViewport().CallDeferred(Node.MethodName.AddChild, pointer);
        View.TreeExiting += () => { pointer.HandleButton = null; pointer.QueueFree(); };
    }

    public void Open()
    {
        if (View.Visible) return;
        Cancel(); Populate(resetScroll: true);
        _coveredOverlay = _currentOverlay();
        // 只暂时隐藏原生选牌屏，不移除它，也不完成或取消它的选择任务。
        if (_capstone == null) _coveredOverlay?.AfterOverlayHidden();
        if (!MagicPropCmd.AllowsUseOver(_coveredOverlay)) _status.Text = Text("BOOK_VIEW_ONLY");
        if (_capstone != null) _capstone.Open((MagicBookView)View); else View.Show();
    }
    public void Close()
    {
        Cancel();
        if (_capstone?.CurrentCapstoneScreen == View) _capstone.Close(); else View.Hide();
    }
    private void Back() {
        if (NModalContainer.Instance?.OpenModal != null) return;
        if (_armed.HasValue) Cancel(); else Close();
    }

    private void RestoreOverlay()
    {
        var previous = _coveredOverlay;
        _coveredOverlay = null;
        if (_capstone != null) return; // 原生容器负责恢复奖励/地图，避免重复调用展示回调。
        if (previous == null || previous != _currentOverlay()) return;
        if (previous is GodotObject node && !GodotObject.IsInstanceValid(node)) return;
        previous.AfterOverlayShown();
    }

    private void Cancel()
    {
        _pointerPress = null;
        _armed = null;
        _selected.Clear();
        RefreshSelection();
        _status.Text = Text("BOOK_HINT");
    }

    private bool HandlePointer(InputEventMouseButton button)
    {
        if (!View.IsVisibleInTree() || !View.CanProcess() || NModalContainer.Instance?.OpenModal != null || _applying || _presentingChange) { _pointerPress = null; return false; }
        if (button.ButtonIndex is not (MouseButton.Left or MouseButton.Right)) return false;
        bool Contains(Control control) => new Rect2(Vector2.Zero, control.Size).HasPoint(
            control.GetGlobalTransformWithCanvas().AffineInverse() * button.Position);
        var hit = _cards.Reverse().FirstOrDefault(pair => pair.Value.IsVisibleInTree() &&
            Contains(pair.Value.Hitbox) && Contains(_scroll));
        if (hit.Value == null) { _pointerPress = null; return false; }
        if (button.Pressed) {
            _pointerPress = (hit.Key, button.ButtonIndex);
            MegaCrit.Sts2.Core.Commands.SfxCmd.Play("event:/sfx/ui/clicks/ui_click");
        }
        else {
            bool clicked = _pointerPress == (hit.Key, button.ButtonIndex);
            _pointerPress = null;
            if (clicked) {
                if (button.ButtonIndex == MouseButton.Right) {
                    if (hit.Value.CardModel is MagicPropCard) Arm(hit.Key);
                } else Select(hit.Key);
            }
        }
        return true;
    }

    public void Refresh()
    {
        if (!View.Visible) return;
        if (_coveredOverlay != _currentOverlay()) { Close(); return; }
        if (_presentingChange) return;
        var deck = MagicDeckStore.Read(_player);
        if (_displayed != deck.ToJson()) { Cancel(); Populate(); }
        if (_armed.HasValue && !_applying && !MagicPropCmd.CanUse(_player, _armed.Value)) Cancel();
    }

    internal (Vector2 Position, Vector2 Scale)? AnimationPose(int id)
    {
        if (!View.IsVisibleInTree() || !_cards.TryGetValue(id, out var holder) || holder.CardNode is not { } card)
            return null;
        return new Rect2(Vector2.Zero, _scroll.Size).HasPoint(_scroll.GetGlobalTransform().AffineInverse() * card.GlobalPosition) &&
            View.GetGlobalRect().HasPoint(card.GlobalPosition) &&
            View.GetViewportRect().HasPoint(card.GlobalPosition) ? (card.GlobalPosition, card.GetGlobalTransform().Scale) : null;
    }

    internal void BeginInventoryAnimation() => _presentingChange = true;

    internal void HideAnimatedItem(int id)
    {
        if (_cards.TryGetValue(id, out var holder)) holder.Visible = false;
    }

    internal void RevealInventoryChanges()
    {
        if (View.IsVisibleInTree()) { Cancel(); Populate(); }
    }

    internal bool FocusAcquiredItem(int id)
    {
        if (!View.IsVisibleInTree() || !_cards.TryGetValue(id, out var holder)) return false;
        _scroll.EnsureControlVisible(holder.GetParent().GetParent<Control>());
        return true;
    }

    internal void FlashAcquiredItem(int id)
    {
        if (!_cards.TryGetValue(id, out var holder) || !holder.IsVisibleInTree()) return;
        holder.CardNode!.CardHighlight.Modulate = NCardHighlight.playableColor;
        holder.CardNode.CardHighlight.AnimFlash();
    }

    internal void ClearAcquiredItemFlash(int id)
    {
        if (_cards.TryGetValue(id, out var holder)) holder.CardNode?.CardHighlight.AnimHideInstantly();
    }

    internal void EndInventoryAnimation()
    {
        _presentingChange = false;
        if (GodotObject.IsInstanceValid(View) && View.IsInsideTree() && View.Visible) Populate();
    }

    internal static NCard CreateCard(CardModel model)
    {
        if (!TestMode.IsOn) return NCard.Create(model)!;
        // 测试模式禁用原生工厂；仍实例化完全相同的场景，并保留内存存档隔离。
        var card = NodePool.Get<NCard>();
        AccessTools.Property(typeof(NCard), nameof(NCard.Model)).SetValue(card, model);
        return card;
    }

    private static NGridCardHolder CreateHolder(CardModel model)
    {
        var card = CreateCard(model);
        card.MouseBehaviorRecursive = Control.MouseBehaviorRecursiveEnum.Inherited;
        card.FocusBehaviorRecursive = Control.FocusBehaviorRecursiveEnum.Inherited;
        if (!TestMode.IsOn) return NGridCardHolder.Create(card)!;
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
                holder.Hitbox.Focused -= handlers.Focus;
                if (holder.CardNode?.CardHighlight is { } highlight && GodotObject.IsInstanceValid(highlight)) {
                    highlight.AnimHideInstantly();
                    highlight.Modulate = NCardHighlight.playableColor;
                }
                holder.GetParent()?.RemoveChild(holder);
                holder.QueueFreeSafely();
            }
        }
        _cards.Clear();
        _handlers.Clear();
    }

    private void UpdateScrollHeight()
    {
        var minimum = _inventory.GetCombinedMinimumSize();
        _inventory.OffsetBottom = _inventory.OffsetTop + minimum.Y;
        _scrollContent.CustomMinimumSize = new(1390, 450 + minimum.Y);
    }

    private void Populate(bool resetScroll = false)
    {
        int scrollPosition = resetScroll ? 0 : _scroll.ScrollVertical;
        ReleaseCards();
        _inventorySortKeys.Clear();
        foreach (var node in _equipped.GetChildren().Concat(_inventory.GetChildren())) { node.GetParent().RemoveChild(node); node.QueueFree(); }
        _cards.Clear();
        var deck = MagicDeckStore.Read(_player);
        void Add(CardModel model, int id, int slot, bool prop) {
            var cell = new Control { Name = "Item_" + id, CustomMinimumSize = new(270, slot >= 0 ? 385 : 288), MouseFilter = Control.MouseFilterEnum.Pass };
            if (slot >= 0) {
                cell.Position = new(210 + slot * 360, 45);
                _equipped.AddChild(cell);
            } else {
                _inventory.AddChild(cell);
                // 一代 BY_RARITY：普通、罕见、魔具、稀有；BY_TYPE 将魔具和未装载因子分组。
                int rarity = prop ? 4 : SpellCatalog.Get(((FactorCard)model).FactorId).Rarity switch {
                    FactorRarity.Common => 2, FactorRarity.Uncommon => 3, _ => 5
                };
                _inventorySortKeys.Add(id, (rarity, prop ? -1 : 3));
            }
            // 原生卡牌悬停会从 0.8 放大到 1；父层统一缩放，预留边界避免裁切。
            var scaleRoot = new Control { Scale = Vector2.One * (slot >= 0 ? 1.03f : 0.85f), Position = new(135, slot >= 0 ? 172 : 144),
                MouseFilter = Control.MouseFilterEnum.Ignore };
            cell.AddChild(scaleRoot);
            var holder = CreateHolder(model);
            holder.Name = (prop ? "Prop_" : "Factor_") + id;
            scaleRoot.AddChild(holder);
            holder.CardNode!.UpdateVisuals(PileType.None, CardPreviewMode.Normal);
            NCardHolder.PressedEventHandler press = _ => Select(id);
            NCardHolder.AltPressedEventHandler alt = _ => { if (prop) Arm(id); };
            NClickableControl.FocusedEventHandler focus = _ => Callable.From(() => {
                if (GodotObject.IsInstanceValid(holder) && GodotObject.IsInstanceValid(_scroll) &&
                    holder.IsInsideTree() && _scroll.IsVisibleInTree() && _scroll.IsAncestorOf(holder))
                    _scroll.EnsureControlVisible(holder.Hitbox);
            }).CallDeferred();
            holder.Pressed += press;
            holder.AltPressed += alt;
            holder.Hitbox.Focused += focus;
            _handlers.Add(id, (press, alt, focus));
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
        SortInventory();
        RefreshSelection();
        UpdateScrollHeight();
        _scroll.SetDeferred(ScrollContainer.PropertyName.ScrollVertical, scrollPosition);
    }

    private void SortInventory()
    {
        bool descending = (_sortByType ? _typeSorter : _raritySorter).IsDescending;
        var ordered = _inventorySortKeys.OrderBy(pair => {
            int key = _sortByType ? pair.Value.Type : pair.Value.Rarity;
            return descending ? -key : key;
        }).ThenBy(pair => pair.Key);
        int index = 0;
        foreach (var pair in ordered) _inventory.MoveChild(_inventory.GetNode("Item_" + pair.Key), index++);
        foreach (var (sorter, active) in new[] { (_raritySorter, !_sortByType), (_typeSorter, _sortByType) }) {
            var color = new Color(active ? "f5d66f" : "d2cec3");
            sorter.GetNode<Label>("%Label").AddThemeColorOverride("font_color", color);
            sorter.GetNode<TextureRect>("%Image").Modulate = color;
        }
    }

    private void Arm(int id)
    {
        if (_applying || _presentingChange) return;
        if (!MagicPropCmd.AllowsUseOver(_currentOverlay())) { _status.Text = Text("BOOK_VIEW_ONLY"); return; }
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
        if (_applying || _presentingChange || !_armed.HasValue) return;
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
            var highlight = holder.CardNode!.CardHighlight;
            if (selected) { highlight.Modulate = new Color("9cf1ac"); highlight.AnimShow(); }
            else {
                highlight.AnimHideInstantly();
                // 原生外光 width=0 仍可能留下细边；未选中时完全隐藏此层。
                highlight.Modulate = Colors.Transparent;
            }
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
