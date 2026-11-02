using BaseLib.Config.UI;
using FrierenMod.Core;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Mechanics;
using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;

namespace FrierenMod.Scripts.UI;

// 任务板使用原生上层界面、按钮与悬停，任务和领奖完全由同步动作结算。
public sealed class CommissionScreen
{
    public MagicBookView View { get; }
    private readonly Player _player;
    private readonly Control _root, _canvas;
    private readonly NCapstoneContainer? _capstone;
    private readonly VBoxContainer _list;
    private readonly Label _heading, _summary;
    private readonly List<Control> _hoverHosts = [];
    private readonly NConfigButton _previous, _next, _close;
    private string _signature = "";
    private int _act;
    private bool _applying;
    private string _notice = "";
    private static string Text(string key) => new LocString("frieren", "COMMISSION." + key).GetFormattedText();
    private static string Format(string key, params (string Key, object Value)[] args) {
        var loc = new LocString("frieren", "COMMISSION." + key);
        foreach (var (name, value) in args) { if (value is string text) loc.Add(name, text); else loc.Add(name, Convert.ToDecimal(value)); }
        return loc.GetFormattedText();
    }
    private static Label Label(string name, string text, int size, Color color) {
        var label = new Label { Name = name, Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart,
            MouseFilter = Control.MouseFilterEnum.Ignore };
        LegacyAchievementTip.ApplyFont(label, false); label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", color); return label;
    }
    private static NConfigButton Button(string name, string text, Action action, Vector2 size) {
        var button = new NConfigButton { Name = name, CustomMinimumSize = size, Size = size };
        button.Initialize(text, action); return button;
    }
    public CommissionScreen(Control root, Player player)
    {
        _root = root; _player = player; _act = player.RunState.CurrentActIndex;
        _capstone = (root as NGlobalUi)?.CapstoneContainer;
        View = new MagicBookView { Name = "FrierenCommissionBoard", Visible = false, MouseFilter = Control.MouseFilterEnum.Stop,
            ZIndex = _capstone == null ? 100 : 0, ZAsRelative = false };
        (_capstone as Control ?? root).AddChild(View); View.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var dark = new ColorRect { Color = new("101722f5"), MouseFilter = Control.MouseFilterEnum.Stop };
        View.AddChild(dark); dark.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _canvas = new Control { Name = "CommissionCanvas", Size = new(1920, 1080), MouseFilter = Control.MouseFilterEnum.Ignore };
        View.AddChild(_canvas);
        _heading = Label("Heading", "", 42, new("f4d69a")); _heading.Position = new(190, 140); _heading.Size = new(900, 60); _canvas.AddChild(_heading);
        _summary = Label("Summary", "", 23, new("c9d5df")); _summary.Position = new(190, 205); _summary.Size = new(1510, 100); _canvas.AddChild(_summary);
        var scroll = new ScrollContainer { Name = "CommissionScroll", Position = new(190, 310), Size = new(1540, 635), HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        _canvas.AddChild(scroll);
        _list = new VBoxContainer { Name = "Quests", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _list.AddThemeConstantOverride("separation", 16); scroll.AddChild(_list);
        _previous = Button("PreviousAct", Text("PREVIOUS"), () => { _act = Math.Max(0, _act - 1); Refresh(true); }, new(210, 64));
        _previous.Position = new(1265, 140); _canvas.AddChild(_previous);
        _next = Button("NextAct", Text("NEXT"), () => { _act = Math.Min(player.RunState.CurrentActIndex, _act + 1); Refresh(true); }, new(210, 64));
        _next.Position = new(1500, 140); _canvas.AddChild(_next);
        _close = Button("CloseCommissions", Text("CLOSE"), Close, new(260, 64)); _close.Position = new(1460, 975); _canvas.AddChild(_close);
        View.DefaultFocusedControl = _close;
        var escape = new Button { Name = "CancelShortcut", Flat = true, Size = Vector2.One, Modulate = Colors.Transparent,
            Shortcut = new Shortcut { Events = [new InputEventKey { PhysicalKeycode = Key.Escape }] }, MouseFilter = Control.MouseFilterEnum.Ignore };
        _canvas.AddChild(escape); escape.Pressed += Close;
        View.VisibilityChanged += () => { if (View.Visible) NHotkeyManager.Instance?.AddBlockingScreen(View); else { NHotkeyManager.Instance?.RemoveBlockingScreen(View); ClearHover(); } };
        root.Resized += Resize;
        View.TreeExiting += () => { root.Resized -= Resize; NHotkeyManager.Instance?.RemoveBlockingScreen(View); ClearHover(); };
        var timer = new Godot.Timer { WaitTime = .2, Autostart = true }; timer.Timeout += () => { if (View.Visible) Refresh(); }; View.AddChild(timer);
        Resize();
    }
    private void Resize() {
        float scale = Math.Max(.1f, Math.Min(_root.Size.X / 1920, _root.Size.Y / 1080));
        _canvas.Scale = Vector2.One * scale; _canvas.Position = (_root.Size - _canvas.Size * scale) / 2;
    }
    public void Open() { _act = _player.RunState.CurrentActIndex; Refresh(true); if (_capstone != null) _capstone.Open(View); else View.Show(); }
    public void Close() { if (_capstone?.CurrentCapstoneScreen == View) _capstone.Close(); else View.Hide(); }
    private void ClearHover() { foreach (var control in _hoverHosts) if (GodotObject.IsInstanceValid(control)) NHoverTipSet.Remove(control); _hoverHosts.Clear(); }
    private void Hover(Control control, Func<IEnumerable<IHoverTip>> tips) {
        control.MouseFilter = Control.MouseFilterEnum.Pass; _hoverHosts.Add(control);
        control.MouseEntered += () => { if (MegaCrit.Sts2.Core.Nodes.NGame.Instance != null) NHoverTipSet.CreateAndShow(control, tips()); };
        control.MouseExited += () => NHoverTipSet.Remove(control);
    }
    public void Refresh(bool force = false)
    {
        var state = CommissionCmd.State(_player); bool canAct = !_applying && CommissionCmd.CanAct(_player);
        string signature = state.ToJson() + $"/{LocManager.Instance.Language}/{_act}/{CommissionCmd.Floor(_player)}/{canAct}/{_player.HasOpenPotionSlots}/" +
            string.Join(',', _player.Deck.Cards.Select(c => c.Id)) + string.Join(',', _player.Relics.Select(r => r.Id));
        if (!force && signature == _signature) return; _signature = signature;
        _heading.Text = Format("TITLE", ("Act", _act + 1));
        _previous.GetNode<Label>("Label").Text = Text("PREVIOUS"); _next.GetNode<Label>("Label").Text = Text("NEXT"); _close.GetNode<Label>("Label").Text = Text("CLOSE");
        _summary.Text = Format("SUMMARY", ("Count", state.ActiveCount), ("Limit", CommissionState.ActiveLimit)) + "\n" + (_notice.Length > 0 ? _notice : Text(canAct ? "RULES" : "BUSY"));
        if (_act > 0) _previous.Enable(); else _previous.Disable();
        if (_act < _player.RunState.CurrentActIndex) _next.Enable(); else _next.Disable();
        _previous.Modulate = _act > 0 ? Colors.White : new Color(1, 1, 1, .45f);
        _next.Modulate = _act < _player.RunState.CurrentActIndex ? Colors.White : new Color(1, 1, 1, .45f);
        ClearHover(); foreach (var child in _list.GetChildren()) { _list.RemoveChild(child); child.QueueFree(); }
        foreach (var quest in state.Quests.Where(q => q.Act == _act).OrderBy(q => q.OpensAt).ThenBy(q => q.Id)) AddRow(quest, state, canAct);
    }
    private void AddRow(Commission quest, CommissionState state, bool canAct)
    {
        bool unlocked = quest.OpensAt <= CommissionCmd.Floor(_player) && quest.Act == _player.RunState.CurrentActIndex;
        bool current = quest.Act == _player.RunState.CurrentActIndex;
        bool ready = current && CommissionState.Ready(quest, id => CommissionCmd.OwnsSpell(_player, id));
        var panel = new PanelContainer { Name = "Quest" + quest.Id, CustomMinimumSize = new(1500, 265) };
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = ready ? new("263d35") : new("202c3a"), BorderColor = new("68715e"),
            BorderWidthLeft = 2, BorderWidthRight = 2, BorderWidthTop = 2, BorderWidthBottom = 2, CornerRadiusTopLeft = 12, CornerRadiusTopRight = 12,
            CornerRadiusBottomLeft = 12, CornerRadiusBottomRight = 12, ContentMarginLeft = 24, ContentMarginRight = 24, ContentMarginTop = 18, ContentMarginBottom = 18 });
        _list.AddChild(panel);
        var columns = new HBoxContainer(); columns.AddThemeConstantOverride("separation", 24); panel.AddChild(columns);
        string art = quest.Template switch {
            "RESEARCH" => "events/ReadingGrimoire", "SWIFT" => "cards/RapidChant", "FLAWLESS" => "cards/PerfectDefensiveMagic",
            "ELITE" => "cards/ManaBarricade", "SUPPLIES" => "events/Package", "TEACHING" => "cards/FlowerFieldSpell",
            "CAREFUL" => "cards/DefensiveMagic", "ESCORT" or "ROAD" => "events/Kraft1", _ => "cards/MultipleOffensiveMagic" };
        columns.AddChild(new TextureRect { Name = "Illustration", Texture = ResourceLoader.Load<Texture2D>("res://FrierenMod/images/" + art + ".png"),
            CustomMinimumSize = new(260, 198), ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered, SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
            ClipContents = true, MouseFilter = Control.MouseFilterEnum.Ignore });
        var content = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; content.AddThemeConstantOverride("separation", 9); columns.AddChild(content);
        content.AddChild(Label("Title", Text("NAME." + quest.Template), 28, new("f4d69a")));
        content.AddChild(Label("Request", Text("REQUEST." + quest.Template), 18, new("bdb3a0")));
        string required = quest.Goal == CommissionGoal.Spell ? CommissionCmd.Spell(quest.Spell).Title : "";
        var goal = Label("Goal", Format("GOAL." + quest.Goal, ("Count", quest.Target), ("Spell", required)), 23, new("e8e1d4")); content.AddChild(goal);
        if (quest.Goal == CommissionGoal.Spell) Hover(goal, () => {
            var card = CommissionCmd.Spell(quest.Spell).ToMutable(); card.Owner = _player; return [new CardHoverTip(card)];
        });
        string rewardItem = quest.Reward.Kind switch {
            CommissionRewardKind.Prop => MagicPropCard.Create(_player, quest.Reward.Item).Title,
            CommissionRewardKind.Potion => CommissionCmd.Potion(quest.Reward.Item).Title.GetFormattedText(),
            CommissionRewardKind.Relic => CommissionCmd.Relic(quest.Reward.Item).Title.GetFormattedText(), _ => "" };
        string rewardText = Format("REWARD." + quest.Reward.Kind, ("Amount", quest.Reward.Amount), ("Item", rewardItem));
        if (quest.Reward.BonusGold > 0) rewardText += Format("BONUS_GOLD", ("Gold", quest.Reward.BonusGold));
        var reward = Label("Reward", rewardText, 21, new("b6d7bc")); content.AddChild(reward);
        Hover(reward, () => quest.Reward.Kind switch {
            CommissionRewardKind.Prop => [new CardHoverTip(MagicPropCard.Create(_player, quest.Reward.Item))],
            CommissionRewardKind.Relic => CommissionCmd.Relic(quest.Reward.Item).HoverTips,
            CommissionRewardKind.Potion => CommissionCmd.Potion(quest.Reward.Item).HoverTips, _ => [] });
        string status = !current && quest.Status != CommissionStatus.Claimed ? Text("EXPIRED") :
            quest.Status == CommissionStatus.Available ? (unlocked ? Text("AVAILABLE") : Format("LOCKED", ("Floor", quest.OpensAt))) :
            quest.Status == CommissionStatus.Claimed ? Text("CLAIMED") : quest.Status == CommissionStatus.Abandoned ? Text("ABANDONED") :
            ready ? Text("READY") : Format("PROGRESS", ("Current", CommissionState.ProgressOf(quest, id => CommissionCmd.OwnsSpell(_player, id))), ("Target", quest.Target));
        content.AddChild(Label("Status", status, 21, ready ? new("a7f2bd") : new("a9baca")));
        var actions = new VBoxContainer { CustomMinimumSize = new(235, 0), Alignment = BoxContainer.AlignmentMode.Center }; columns.AddChild(actions);
        void ActionButton(string name, string text, CommissionOperation op, bool enabled) {
            var button = Button(name, text, () => { _ = TaskHelper.RunSafely(Act(quest.Id, op)); }, new(235, 64));
            actions.AddChild(button); if (!enabled) { button.Disable(); button.Modulate = new Color(1, 1, 1, .45f); }
        }
        if (quest.Status == CommissionStatus.Available) ActionButton("Accept", Text("ACCEPT"), CommissionOperation.Accept, canAct && unlocked && state.ActiveCount < CommissionState.ActiveLimit);
        else if (current && quest.Status == CommissionStatus.Accepted) {
            bool fullPotion = quest.Reward.Kind == CommissionRewardKind.Potion && !_player.HasOpenPotionSlots;
            bool canReceive = CommissionCmd.CanReceive(_player, quest.Reward);
            ActionButton("Claim", Text(ready && !canReceive ? fullPotion ? "POTION_FULL" : "POTION_BLOCKED" : "CLAIM"), CommissionOperation.Claim, canAct && ready && canReceive);
            ActionButton("Abandon", Text("ABANDON"), CommissionOperation.Abandon, canAct);
        }
    }
    private async Task Act(int id, CommissionOperation operation) {
        if (_applying) return; _applying = true;
        try { _notice = await CommissionCmd.Send(_player, id, operation) ? "" : Text("RETRY"); }
        finally { _applying = false; if (GodotObject.IsInstanceValid(View)) Refresh(true); }
    }
    public static void Attach(Control root, Player player, Func<IOverlayScreen?> overlay)
    {
        var screen = new CommissionScreen(root, player);
        var button = Button("FrierenCommissionButton", Text("ENTRY"), () => {
            if (screen.View.Visible) screen.Close(); else screen.Open();
        }, new(120, 64));
        var row = root.FindChild("RightAlignedStuff", true, false) as HBoxContainer ?? root.GetNodeOrNull<HBoxContainer>("FrierenTopBarRow");
        if (row != null) { row.AddChild(button); row.MoveChild(button, 0); }
        else { root.AddChild(button); button.SetAnchorsPreset(Control.LayoutPreset.TopRight); button.Position = new(root.Size.X - 350, 8); }
        var timer = new Godot.Timer { WaitTime = .1, Autostart = true }; root.AddChild(timer);
        timer.Timeout += () => {
            button.GetNode<Label>("Label").Text = Text("ENTRY");
            bool blocked = NModalContainer.Instance?.OpenModal != null || !MagicBookUi.AllowsViewOver(overlay()) ||
                root is NGlobalUi global && global.CapstoneContainer.InUse && global.CapstoneContainer.CurrentCapstoneScreen != screen.View;
            if (blocked) button.Disable(); else button.Enable();
        };
    }
}
