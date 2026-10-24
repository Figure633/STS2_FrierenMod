using FrierenMod.Core;
using FrierenMod.Scripts.Presentation;
using Godot;
using Timer = Godot.Timer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;

namespace FrierenMod.Scripts.UI;

// 对照一代 SlotBgLibrary / Slot：保留原图、四列坐标和单槽确认流程。
// 仅渲染、输入与生命周期使用 Godot / 原生 Capstone 接口。
public sealed class SlotBackLibrary
{
    public MagicBookView View { get; }
    private readonly Control _root, _canvas, _grid;
    private readonly ScrollContainer _scroll;
    private readonly Label _summary;
    private readonly ColorRect _progress;
    private readonly TextureButton _save;
    private readonly Label _saveLabel;
    private readonly Control _tip;
    private readonly Label _tipTitle, _tipCondition, _tipStatus;
    private readonly Timer _hoverTimer;
    private readonly Dictionary<string, Control> _cards = [];
    private readonly NCapstoneContainer? _capstone;
    private readonly ICapstoneScreen? _previous;
    private readonly int _slot;
    private readonly string _opening;
    private string _chosen;
    private string? _hovered;
    private bool _closing, _dragging;
    private Vector2 _dragStart;
    private int _dragScroll;
    public static string Text(string key) => new LocString("frieren", key).GetFormattedText();
    public static Texture2D Texture(string id) => GD.Load<Texture2D>($"res://FrierenMod/images/slot_backs/{id}.png");
    private static Texture2D Art(string name) => GD.Load<Texture2D>($"res://FrierenMod/images/achievement/{name}.png");

    public static SlotBackLibrary Open(Control root, int slot = 0)
    {
        if (PresentationSettings.Achievements) SlotBackStore.CollectLibraryRewards();
        var library = new SlotBackLibrary(root, slot);
        if (library._capstone != null) library._capstone.Open(library.View); else library.View.Show();
        NHotkeyManager.Instance?.AddBlockingScreen(library.View);
        return library;
    }

    private SlotBackLibrary(Control root, int slot)
    {
        _root = root; _slot = Math.Clamp(slot, 0, 2);
        _opening = _chosen = SlotBackStore.Selected(_slot);
        _capstone = (root as NGlobalUi)?.CapstoneContainer;
        _previous = _capstone?.CurrentCapstoneScreen;
        View = new MagicBookView { Name = "FrierenSlotBackLibrary", Visible = false,
            MouseFilter = Control.MouseFilterEnum.Stop, ZIndex = _capstone != null ? 0 : 200, ZAsRelative = false };
        (_capstone as Control ?? root).AddChild(View);
        View.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var dark = new ColorRect { Color = new Color(0, 0, 0, .5f), MouseFilter = Control.MouseFilterEnum.Stop };
        View.AddChild(dark); dark.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _canvas = new Control { Name = "LibraryCanvas", Size = new(1920, 1080), MouseFilter = Control.MouseFilterEnum.Ignore };
        View.AddChild(_canvas); Resize(); root.Resized += Resize;
        Picture(_canvas, "Background", Art("SlotLibraryBg"), new(0, 0), new(1920, 1100));
        _scroll = new ScrollContainer { Name = "BackScroll", Position = new(90, 110), Size = new(1740, 930),
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, FollowFocus = true };
        _canvas.AddChild(_scroll);
        _grid = new Control { Name = "BackGrid", CustomMinimumSize = new(1710, 26 * 400 + 70), MouseFilter = Control.MouseFilterEnum.Pass };
        _scroll.AddChild(_grid);
        _grid.GuiInput += Drag; _scroll.GuiInput += Drag;
        _scroll.GetVScrollBar().ValueChanged += _ => HideTip();
        Picture(_canvas, "Foreground", Art("SlotLibraryBgBoard"), new(0, 0), new(1920, 1100));
        var bar = new ColorRect { Position = new(576, 36), Size = new(768, 14), Color = Colors.White,
            MouseFilter = Control.MouseFilterEnum.Ignore };
        _canvas.AddChild(bar);
        _progress = new ColorRect { Size = new(0, 14), Color = new Color(1, .8f, .3f, .9f), MouseFilter = Control.MouseFilterEnum.Ignore };
        bar.AddChild(_progress);
        _summary = MakeLabel(_canvas, "CollectionProgress", "", new(576, 54), new(800, 45), 30);
        // 保存按钮装饰高488，但只有底部接收点击，避免遮住右列卡背。
        Picture(_canvas, "SaveDecoration", Art("graySaveButton"), new(1350, 622), new(635, 488));
        _save = new TextureButton { Name = "SaveBack", Position = new(1350, 950), Size = new(570, 130), IgnoreTextureSize = true };
        _canvas.AddChild(_save); _save.Pressed += SaveAndClose;
        _saveLabel = MakeLabel(_canvas, "SaveLabel", Text("BACK_SAVE"), new(1650, 1007), new(180, 45), 30);
        var cancel = new TextureButton { Name = "CancelBack", Position = new(720, 1000), Size = new(440, 100),
            TextureNormal = Art("cancelButton"), IgnoreTextureSize = true, StretchMode = TextureButton.StretchModeEnum.Scale };
        _canvas.AddChild(cancel); cancel.Pressed += Close;
        var cancelLabel = MakeLabel(cancel, "Caption", Text("BACK_EXIT"), new(0, 7), new(440, 45), 30);
        cancelLabel.HorizontalAlignment = HorizontalAlignment.Center;
        cancel.MouseEntered += () => cancelLabel.Modulate = new Color("ffcc4d");
        cancel.MouseExited += () => cancelLabel.Modulate = Colors.White;
        _save.MouseEntered += () => { if (!_save.Disabled) _saveLabel.Modulate = new Color("ffcc4d"); };
        _save.MouseExited += () => _saveLabel.Modulate = _save.Disabled ? Colors.Gray : Colors.White;
        View.DefaultFocusedControl = cancel;
        var escape = new Button { Name = "CancelShortcut", Flat = true, Size = Vector2.One, Modulate = Colors.Transparent,
            MouseFilter = Control.MouseFilterEnum.Ignore, FocusMode = Control.FocusModeEnum.None,
            Shortcut = new Shortcut { Events = [new InputEventKey { PhysicalKeycode = Key.Escape }] } };
        _canvas.AddChild(escape); escape.Pressed += Close;
        _save.Shortcut = new Shortcut { Events = [new InputEventKey { PhysicalKeycode = Key.Enter }] };
        _tip = new Control { Name = "BackTooltip", Visible = false, Size = new(410, 180), MouseFilter = Control.MouseFilterEnum.Ignore };
        _canvas.AddChild(_tip);
        var tipFrame = new NinePatchRect { Texture = Art("background"), Size = _tip.Size,
            RegionRect = new Rect2(23, 26, 515, 282), PatchMarginLeft = 24, PatchMarginRight = 24,
            PatchMarginTop = 30, PatchMarginBottom = 24, MouseFilter = Control.MouseFilterEnum.Ignore };
        _tip.AddChild(tipFrame);
        _tipTitle = MakeLabel(_tip, "Title", "", new(24, 16), new(362, 45), 30);
        _tipTitle.AddThemeColorOverride("font_color", new Color("ffcc4d"));
        _tipCondition = MakeLabel(_tip, "Condition", "", new(24, 64), new(362, 65), 25);
        _tipStatus = MakeLabel(_tip, "Status", "", new(24, 128), new(362, 36), 25);
        _hoverTimer = new Timer { Name = "HoverDelay", WaitTime = .2, OneShot = true };
        _canvas.AddChild(_hoverTimer); _hoverTimer.Timeout += ShowTip;
        View.TreeExiting += () => { root.Resized -= Resize; SlotBackStore.Changed -= Refresh; NHotkeyManager.Instance?.RemoveBlockingScreen(View); };
        View.VisibilityChanged += () => {
            if (!View.Visible) NHotkeyManager.Instance?.RemoveBlockingScreen(View);
            if (!View.Visible && !_closing) { _closing = true; View.QueueFree(); }
        };
        SlotBackStore.Changed += Refresh;
        Refresh();
    }

    private static TextureRect Picture(Control parent, string name, Texture2D texture, Vector2 position, Vector2 size)
    {
        var image = new TextureRect { Name = name, Texture = texture, Position = position, Size = size,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.Scale,
            MouseFilter = Control.MouseFilterEnum.Ignore };
        parent.AddChild(image); return image;
    }

    private static Label MakeLabel(Control parent, string name, string text, Vector2 position, Vector2 size, int fontSize)
    {
        var label = new Label { Name = name, Text = text, Position = position, Size = size,
            MouseFilter = Control.MouseFilterEnum.Ignore, AutowrapMode = TextServer.AutowrapMode.WordSmart };
        label.AddThemeFontOverride("font", GD.Load<Font>("res://themes/kreon_bold_shared.tres"));
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_outline_color", new Color(0, 0, 0, .8f));
        label.AddThemeConstantOverride("outline_size", 2);
        parent.AddChild(label); return label;
    }

    private void Resize()
    {
        float scale = Math.Max(.1f, Math.Min(_root.Size.X / 1920, _root.Size.Y / 1080));
        _canvas.Scale = Vector2.One * scale; _canvas.Position = (_root.Size - _canvas.Size * scale) / 2;
    }

    private void Refresh()
    {
        if (_closing) return;
        HideTip();
        var progress = SlotBackStore.Read();
        var summary = new LocString("frieren", "BACK_PROGRESS");
        summary.Add("Count", progress.Unlocked.Count); summary.Add("Total", SlotBackCatalog.Ids.Count);
        _summary.Text = summary.GetFormattedText();
        _progress.Size = new(768f * progress.Unlocked.Count / SlotBackCatalog.Ids.Count, 14);
        foreach (var child in _grid.GetChildren()) { _grid.RemoveChild(child); child.QueueFree(); }
        _cards.Clear();
        // 按收藏存档的解锁写入顺序排在前面，未解锁沿用原图册顺序。
        var ids = progress.Unlocked.Concat(SlotBackCatalog.Ids.Where(id => !progress.Unlocked.Contains(id)));
        int index = 0;
        foreach (string id in ids) {
            bool owned = progress.Unlocked.Contains(id);
            var cell = new Control { Name = "Back_" + id, Position = new(260 + index % 4 * 400, 190 + index / 4 * 400),
                Size = new(301, 461), PivotOffset = new(150.5f, 230.5f), Scale = Vector2.One * .7f,
                MouseFilter = Control.MouseFilterEnum.Ignore };
            cell.Position -= cell.PivotOffset;
            _grid.AddChild(cell); _cards.Add(id, cell); index++;
            Picture(cell, "Shadow", Art("silhouette"), new(18, 14), new(301, 461)).Modulate = new Color(0, 0, 0, .25f);
            var glow = Picture(cell, "SelectionGlow", Art("silhouette"), new(-9, -14), new(319, 489));
            glow.Modulate = new Color(.2f, .9f, 1, .6f); glow.PivotOffset = glow.Size / 2;
            var pulse = glow.CreateTween().SetLoops();
            pulse.TweenProperty(glow, "scale", Vector2.One * 1.055f, .6);
            pulse.Parallel().TweenProperty(glow, "modulate:a", .25f, .6);
            pulse.TweenProperty(glow, "scale", Vector2.One, .6);
            pulse.Parallel().TweenProperty(glow, "modulate:a", .6f, .6);
            var image = new TextureButton { Name = "SelectBack_" + id, Size = new(301, 461),
                TextureNormal = Texture(id), IgnoreTextureSize = true, StretchMode = TextureButton.StretchModeEnum.KeepAspectCentered,
                SelfModulate = new Color(1, 1, 1, owned ? 1 : .4f), MouseFilter = Control.MouseFilterEnum.Pass,
                ActionMode = BaseButton.ActionModeEnum.Press };
            cell.AddChild(image);
            image.Pressed += () => { if (owned) { _chosen = id; RefreshSelection(); } };
            image.MouseEntered += () => Hover(id, true); image.MouseExited += () => Hover(id, false);
            image.FocusEntered += () => Hover(id, true); image.FocusExited += () => Hover(id, false);
            image.GuiInput += Drag;
        }
        RefreshSelection();
    }

    private void RefreshSelection()
    {
        foreach (var (id, cell) in _cards) cell.GetNode<Control>("SelectionGlow").Visible = id == _chosen;
        bool changed = _chosen != _opening;
        _save.Disabled = !changed;
        _canvas.GetNode<TextureRect>("SaveDecoration").Texture = Art(changed ? "saveButton" : "graySaveButton");
        _saveLabel.Modulate = changed ? Colors.White : Colors.Gray;
    }

    private void Hover(string id, bool entered)
    {
        if (_closing) return;
        if (SlotBackStore.Read().Unlocked.Contains(id)) {
            var tween = _cards[id].CreateTween();
            tween.TweenProperty(_cards[id], "scale", Vector2.One * (entered ? .75f : .7f), .1);
        }
        if (entered) { HideTip(); _hovered = id; _hoverTimer.Start(); }
        else if (_hovered == id) HideTip();
    }

    private void ShowTip()
    {
        if (_closing || _hovered == null) return;
        string id = _hovered;
        bool owned = SlotBackStore.Read().Unlocked.Contains(id);
        _tipTitle.Text = Text($"BACK.{id}.title"); _tipCondition.Text = Text($"BACK.{id}.description");
        _tipStatus.Text = Text(owned ? "BACK_UNLOCKED" : "BACK_LOCKED");
        _tipStatus.AddThemeColorOverride("font_color", owned ? new Color("89ff00") : new Color("ff4b4b"));
        // 一代 TipHelper 随正文撑高；长标题也须参与排版，不能盖住条件。
        _tipTitle.Size = new(362, Math.Max(42, _tipTitle.GetMinimumSize().Y));
        _tipCondition.Position = new(24, _tipTitle.Position.Y + _tipTitle.Size.Y + 5);
        _tipCondition.Size = new(362, Math.Max(34, _tipCondition.GetMinimumSize().Y));
        _tipStatus.Position = new(24, _tipCondition.Position.Y + _tipCondition.Size.Y + 8);
        _tip.Size = new(410, _tipStatus.Position.Y + 52);
        _tip.GetChild<Control>(0).Size = _tip.Size;
        Vector2 center = _canvas.GetGlobalTransform().AffineInverse() * (_cards[id].GetGlobalTransform() * _cards[id].PivotOffset);
        _tip.Position = new(Math.Clamp(center.X + (center.X > 1200 ? -420 : 175), 25, 1485), Math.Clamp(center.Y - 200, 115, 1000 - _tip.Size.Y));
        _tip.Show();
    }

    private void HideTip() { _hovered = null; _hoverTimer?.Stop(); _tip?.Hide(); }

    private void Drag(InputEvent input)
    {
        if (input is InputEventMouseButton { ButtonIndex: MouseButton.Left } button) {
            _dragging = button.Pressed;
            if (_dragging) { _dragStart = _canvas.GetLocalMousePosition(); _dragScroll = _scroll.ScrollVertical; }
        } else if (input is InputEventMouseMotion motion && _dragging && motion.ButtonMask.HasFlag(MouseButtonMask.Left)) {
            _scroll.ScrollVertical = _dragScroll - (int)(_canvas.GetLocalMousePosition().Y - _dragStart.Y);
            HideTip();
        }
    }

    public void SaveAndClose()
    {
        if (_chosen != _opening && SlotBackStore.Select(_slot, _chosen)) Close();
    }

    public void Close()
    {
        if (_closing) return; _closing = true;
        NHotkeyManager.Instance?.RemoveBlockingScreen(View);
        if (_capstone?.CurrentCapstoneScreen == View) {
            _capstone.Close();
            if (_previous is Node node && GodotObject.IsInstanceValid(node) && !node.IsQueuedForDeletion()) _capstone.Open(_previous);
        } else View.Hide();
        View.QueueFree();
    }
}
