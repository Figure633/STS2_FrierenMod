using BaseLib.Config.UI;
using FrierenMod.Core;
using FrierenMod.Scripts.Presentation;
using Godot;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;

namespace FrierenMod.Scripts.UI;

// 复用原生 Capstone / 返回按钮与 BaseLib 按钮，收藏本身不进入跑局卡牌或网络状态。
public sealed class SlotBackLibrary
{
    public MagicBookView View { get; }
    private readonly Control _root;
    private readonly Control _canvas;
    private readonly GridContainer _grid;
    private readonly Label _summary;
    private readonly Label _selection;
    private readonly string[] _draft;
    private readonly NCapstoneContainer? _capstone;
    private readonly ICapstoneScreen? _previous;
    private int _slot;
    private string _search = "";
    private bool _ownedOnly;
    private bool _closing;
    public static string Text(string key) => new LocString("frieren", key).GetFormattedText();
    public static Texture2D Texture(string id) => GD.Load<Texture2D>($"res://FrierenMod/images/slot_backs/{id}.png");

    public static SlotBackLibrary Open(Control root, int slot = 0)
    {
        if (PresentationSettings.Achievements) SlotBackStore.CollectLibraryRewards();
        var library = new SlotBackLibrary(root, slot);
        if (library._capstone != null) library._capstone.Open(library.View); else library.View.Show();
        NHotkeyManager.Instance?.AddBlockingScreen(library.View);
        if (library.View.DefaultFocusedControl is { FocusMode: not Control.FocusModeEnum.None } focused)
            focused.GrabFocus();
        return library;
    }

    private SlotBackLibrary(Control root, int slot)
    {
        _root = root; _slot = Math.Clamp(slot, 0, 2); _draft = (string[])SlotBackStore.Read().Selected.Clone();
        _capstone = (root as NGlobalUi)?.CapstoneContainer;
        _previous = _capstone?.CurrentCapstoneScreen;
        View = new MagicBookView { Name = "FrierenSlotBackLibrary", Visible = false, MouseFilter = Control.MouseFilterEnum.Stop,
            ZIndex = _capstone != null ? 0 : 200, ZAsRelative = false };
        (_capstone as Control ?? root).AddChild(View);
        View.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        View.AddChild(new ColorRect { Color = new Color("10151bf8"), MouseFilter = Control.MouseFilterEnum.Stop });
        View.GetChild<Control>(0).SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _canvas = new Control { Name = "LibraryCanvas", Size = new(1920, 1080), MouseFilter = Control.MouseFilterEnum.Ignore };
        View.AddChild(_canvas); Resize(); root.Resized += Resize;
        AddLabel(Text("BACK_LIBRARY"), new(155, 25), new(1000, 50), 34);
        _summary = AddLabel("", new(1230, 30), new(510, 45), 24);
        for (int i = 0; i < 3; i++) {
            int index = i;
            Button(Text("SLOT." + (ManaSlot)i), new(155 + i * 340, 92), () => { _slot = index; RefreshSelection(); });
        }
        var search = new LineEdit { Name = "BackSearch", Position = new(1210, 96), Size = new(505, 54),
            PlaceholderText = Text("BACK_SEARCH") };
        search.AddThemeFontSizeOverride("font_size", 23); _canvas.AddChild(search);
        search.TextChanged += text => { _search = text; Populate(); };
        _selection = AddLabel("", new(155, 165), new(1210, 40), 23);
        var owned = new CheckButton { Name = "OwnedOnly", Text = Text("BACK_OWNED_ONLY"), Position = new(1380, 165), Size = new(340, 42) };
        owned.AddThemeFontSizeOverride("font_size", 23);
        _canvas.AddChild(owned); owned.Toggled += value => { _ownedOnly = value; Populate(); };
        var scroll = new ScrollContainer { Name = "BackScroll", Position = new(150, 220), Size = new(1580, 720),
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        _canvas.AddChild(scroll);
        _grid = new GridContainer { Name = "BackGrid", Columns = 4, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _grid.AddThemeConstantOverride("h_separation", 18); _grid.AddThemeConstantOverride("v_separation", 20); scroll.AddChild(_grid);
        Button(Text("BACK_SAVE"), new(1380, 975), SaveAndClose);
        var back = GD.Load<PackedScene>("res://scenes/ui/back_button.tscn").Instantiate<NBackButton>();
        _canvas.AddChild(back);
        back.Position = new(45, 1000);
        back.Connect(MegaCrit.Sts2.Core.Nodes.GodotExtensions.NButton.SignalName.Released,
            Callable.From<MegaCrit.Sts2.Core.Nodes.GodotExtensions.NButton>(_ => Close()));
        View.DefaultFocusedControl = back; back.Enable();
        View.TreeExiting += () => { root.Resized -= Resize; SlotBackStore.Changed -= Refresh; NHotkeyManager.Instance?.RemoveBlockingScreen(View); };
        View.VisibilityChanged += () => {
            if (!View.Visible) NHotkeyManager.Instance?.RemoveBlockingScreen(View);
            if (!View.Visible && !_closing) { _closing = true; View.QueueFree(); }
        };
        SlotBackStore.Changed += Refresh;
        Refresh();
    }

    private void Resize()
    {
        float scale = Math.Max(0.1f, Math.Min(_root.Size.X / 1920, _root.Size.Y / 1080));
        _canvas.Scale = Vector2.One * scale; _canvas.Position = (_root.Size - _canvas.Size * scale) / 2;
    }

    private Label AddLabel(string text, Vector2 position, Vector2 size, int fontSize)
    {
        var label = new Label { Text = text, Position = position, Size = size, MouseFilter = Control.MouseFilterEnum.Ignore,
            VerticalAlignment = VerticalAlignment.Center, AutowrapMode = TextServer.AutowrapMode.WordSmart };
        label.AddThemeFontOverride("font", GD.Load<Font>("res://themes/kreon_bold_shared.tres"));
        label.AddThemeFontSizeOverride("font_size", fontSize); label.AddThemeColorOverride("font_color", new Color("eee7d8"));
        _canvas.AddChild(label); return label;
    }

    private void Button(string text, Vector2 position, Action action)
    {
        var host = new Control { Position = position, Size = new(324, 64), MouseFilter = Control.MouseFilterEnum.Ignore };
        _canvas.AddChild(host);
        var button = new NConfigButton(); button.Initialize(text, action); host.AddChild(button);
    }

    private void Refresh()
    {
        if (_closing) return;
        var progress = SlotBackStore.Read();
        var summary = new LocString("frieren", "BACK_PROGRESS");
        summary.Add("Count", progress.Unlocked.Count); summary.Add("Total", SlotBackCatalog.Ids.Count);
        _summary.Text = summary.GetFormattedText();
        RefreshSelection(); Populate();
    }

    private void RefreshSelection() => _selection.Text = Text("SLOT." + (ManaSlot)_slot) + " · " + _draft[_slot] + " · " + Text($"BACK.{_draft[_slot]}.title");

    private void Populate()
    {
        foreach (var child in _grid.GetChildren()) { _grid.RemoveChild(child); child.QueueFree(); }
        var unlocked = SlotBackStore.Read().Unlocked;
        foreach (var id in SlotBackCatalog.Ids.OrderByDescending(unlocked.Contains).ThenBy(id => id)) {
            string title = Text($"BACK.{id}.title"), condition = Text($"BACK.{id}.description");
            bool owned = unlocked.Contains(id);
            if ((_ownedOnly && !owned) || !(id + title + condition).Contains(_search, StringComparison.OrdinalIgnoreCase)) continue;
            var cell = new Control { Name = "Back_" + id, CustomMinimumSize = new(365, 392), MouseFilter = Control.MouseFilterEnum.Pass };
            _grid.AddChild(cell);
            var image = new TextureButton { Name = "SelectBack_" + id, Position = new(90, 3), Size = new(180, 276),
                TextureNormal = owned ? Texture(id) : GD.Load<Texture2D>("res://FrierenMod/images/achievement/silhouette.png"),
                IgnoreTextureSize = true, StretchMode = TextureButton.StretchModeEnum.KeepAspectCentered,
                Disabled = !owned, TooltipText = id + " · " + title + "\n" + condition };
            cell.AddChild(image); image.Pressed += () => { _draft[_slot] = id; RefreshSelection(); };
            var label = new Label { Text = id + " · " + title, Position = new(5, 284), Size = new(355, 47),
                HorizontalAlignment = HorizontalAlignment.Center, AutowrapMode = TextServer.AutowrapMode.WordSmart,
                MouseFilter = Control.MouseFilterEnum.Ignore };
            label.AddThemeFontSizeOverride("font_size", 21); cell.AddChild(label);
            var description = new Label { Text = (owned ? Text("BACK_UNLOCKED") : Text("BACK_LOCKED")) + "\n" + condition,
                Position = new(6, 334), Size = new(352, 56), AutowrapMode = TextServer.AutowrapMode.WordSmart,
                MouseFilter = Control.MouseFilterEnum.Ignore, ClipText = true, TooltipText = condition };
            description.AddThemeFontSizeOverride("font_size", 17); cell.AddChild(description);
        }
    }

    public void SaveAndClose()
    {
        if (SlotBackStore.SelectAll(_draft)) Close();
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
