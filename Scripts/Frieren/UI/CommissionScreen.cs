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
    private void ClearHover() { foreach (var control in _hoverHosts) if (GodotObject.IsInstanceValid(control)) CommissionHoverTips.Remove(control); _hoverHosts.Clear(); }
    private void Hover(Control control, Func<IEnumerable<IHoverTip>> tips) {
        control.MouseFilter = Control.MouseFilterEnum.Pass; _hoverHosts.Add(control);
        control.MouseEntered += () => { if (MegaCrit.Sts2.Core.Nodes.NGame.Instance != null) CommissionHoverTips.Show(control, tips()); };
        control.MouseExited += () => CommissionHoverTips.Remove(control);
    }
    public void Refresh(bool force = false)
    {
        var state = CommissionCmd.State(_player); bool canAct = !_applying && CommissionCmd.CanAct(_player);
        string signature = state.ToJson() + $"/{LocManager.Instance.Language}/{_act}/{CommissionCmd.Floor(_player)}/{canAct}/{_player.HasOpenPotionSlots}/" +
            string.Join(',', _player.Deck.Cards.Select(c => c.Id+"/"+c.CurrentUpgradeLevel))+"/"+CommissionCargo.Freight(_player) + string.Join(',', _player.Relics.Select(r => r.Id));
        if (!force && signature == _signature) return; _signature = signature;
        _heading.Text = Format("TITLE", ("Act", _act + 1));
        _previous.GetNode<Label>("Label").Text = Text("PREVIOUS"); _next.GetNode<Label>("Label").Text = Text("NEXT"); _close.GetNode<Label>("Label").Text = Text("CLOSE");
        _summary.Text = Format("SUMMARY", ("Count", state.ActiveCount), ("Limit", CommissionState.ActiveLimit)) + "\n" + (_notice.Length > 0 ? _notice : Text(canAct ? state.Quests.Any(q=>q.Completed&&q.Status==CommissionStatus.Accepted)?"CARRY_NOTICE":"RULES" : "BUSY"));
        if (_act > 0) _previous.Enable(); else _previous.Disable();
        if (_act < _player.RunState.CurrentActIndex) _next.Enable(); else _next.Disable();
        _previous.Modulate = _act > 0 ? Colors.White : new Color(1, 1, 1, .45f);
        _next.Modulate = _act < _player.RunState.CurrentActIndex ? Colors.White : new Color(1, 1, 1, .45f);
        ClearHover(); foreach (var child in _list.GetChildren()) { _list.RemoveChild(child); child.QueueFree(); }
        foreach (var quest in state.Quests.Where(q => q.Act == _act && !q.Legacy).OrderBy(q => q.OpensAt).ThenBy(q => q.Id)) AddRow(quest, state, canAct);
    }
    public static string GoalText(Player player, Commission q)
    {
        if(q.Completed&&q.Status==CommissionStatus.Accepted)return Text("CARRY_READY");
        if(q.Goal==CommissionGoal.Cargo&&q.Status==CommissionStatus.Claimed)return Format("DELIVERED",("Gold",q.PaidFreight));
        var tutor=CommissionCmd.TutorCounts(player);
        int current=q.Spell switch {
            "rare-pair"=>player.Deck.Cards.Where(c=>c.Rarity==MegaCrit.Sts2.Core.Entities.Cards.CardRarity.Rare).Select(c=>c.Id).Distinct().Count(),
            "legendary-three"=>player.Deck.Cards.Where(c=>c is ILegendaryCard).Select(c=>c.Id).Distinct().Count(),
            "upgraded-five"=>player.Deck.Cards.Count(c=>c.IsUpgraded),
            "zero-three"=>player.Deck.Cards.Where(c=>c.EnergyCost.Canonical==0&&!c.EnergyCost.CostsX&&c is not IManaCard).Select(c=>c.Id).Distinct().Count(),
            "power-three"=>player.Deck.Cards.Where(c=>c.Type==MegaCrit.Sts2.Core.Entities.Cards.CardType.Power).Select(c=>c.Id).Distinct().Count(),
            "distinct-twelve"=>player.Deck.Cards.Select(c=>c.Id).Distinct().Count(),_=>0};
        var required=MegaCrit.Sts2.Core.Models.ModelDb.AllCards.FirstOrDefault(c=>c.GetType().Name==q.Spell);
        string goal=Format("GOAL."+q.Template,("Spell",required?.Title??""),("Mana",tutor.Mana),("Chants",tutor.Chants),("Current",current),
            ("Count",CommissionCargo.Goods(player).Length),("Gold",CommissionCargo.Freight(player)));
        if(q.Goal==CommissionGoal.EmpoweredElite)goal+="\n"+CommissionCmd.AffixText(q.Affix,q.Act,player.RunState.Players.Count);
        return goal;
    }
    public static string RewardText(Player player,Commission q)
    {
        var r=q.Reward;
        string item=r.Kind switch {
            CommissionRewardKind.Prop=>MagicPropCard.Create(player,r.Item).Title,
            CommissionRewardKind.Relic=>CommissionCmd.Relic(r.Item).Title.GetFormattedText(),
            CommissionRewardKind.Potion=>CommissionCmd.Potion(r.Item).Title.GetFormattedText(),
            CommissionRewardKind.Card=>CommissionCmd.Spell(r.Item).Title+"+",_=>""};
        string text=Format("REWARD."+(r.FallbackProp.Length>0?"BossRelic":r.Kind.ToString()),("Item",item),("Amount",r.Amount));
        if(r.FallbackProp.Length>0)text+=Format("FALLBACK_PROP",("Item",MagicPropCard.Create(player,r.FallbackProp).Title));
        if(r.BonusGold>0)text+=Format("BONUS_GOLD",("Gold",r.BonusGold));return text;
    }
    public static IEnumerable<IHoverTip> RewardTips(Player player,Commission q)
    {
        var r=q.Reward;
        if(r.Kind==CommissionRewardKind.Prop)return [new CardHoverTip(MagicPropCard.Create(player,r.Item))];
        if(r.Kind==CommissionRewardKind.Relic)return CommissionCmd.Relic(r.Item).HoverTips.Concat(r.FallbackProp.Length>0?[new CardHoverTip(MagicPropCard.Create(player,r.FallbackProp))]:Array.Empty<IHoverTip>());
        if(r.Kind==CommissionRewardKind.Potion)return CommissionCmd.Potion(r.Item).HoverTips;
        if(r.Kind==CommissionRewardKind.RareChoice)return (r.Choices??[]).Select(id=>{var c=CommissionCmd.Spell(id).ToMutable();c.Owner=player;return (IHoverTip)new CardHoverTip(c);});
        if(r.Kind==CommissionRewardKind.Card){var c=CommissionCmd.Spell(r.Item).ToMutable();c.Owner=player;MegaCrit.Sts2.Core.Commands.CardCmd.Upgrade(c);return [new CardHoverTip(c)];}
        return [];
    }
    public static string CurrentSummary(Player player)
    {
        var state=CommissionCmd.State(player);var q=state.Active??state.Quests.FirstOrDefault(q=>q.Completed&&q.Status==CommissionStatus.Accepted);
        return q==null?Text("NO_ACTIVE"):
            Text("NAME."+q.Template)+" "+new string('★',q.Stars)+"\n"+GoalText(player,q)+"\n"+RewardText(player,q)+"\n"+Text(CommissionState.Ready(q,id=>CommissionCmd.OwnsSpell(player,id))?"READY":"IN_PROGRESS");
    }
    public static bool NeedsAttention(Player player)=>CommissionCmd.State(player) is var state&&
        (state.Quests.Any(q=>CommissionState.Ready(q,id=>CommissionCmd.OwnsSpell(player,id))) ||
            state.Active==null&&state.Quests.Any(q=>!q.Legacy&&q.Act==state.CurrentAct&&q.Status==CommissionStatus.Available));
    private void AddRow(Commission q,CommissionState state,bool canAct)
    {
        bool current=q.Act==_player.RunState.CurrentActIndex;
        bool ready=CommissionState.Ready(q,id=>CommissionCmd.OwnsSpell(_player,id));
        var panel=new PanelContainer{Name="Quest"+q.Id,CustomMinimumSize=new(1500,285)};
        panel.AddThemeStyleboxOverride("panel",new StyleBoxFlat{BgColor=ready?new("263d35"):new("202c3a"),BorderColor=q.Stars==3?new("bf9654"):new("68715e"),
            BorderWidthLeft=2,BorderWidthRight=2,BorderWidthTop=2,BorderWidthBottom=2,CornerRadiusTopLeft=12,CornerRadiusTopRight=12,CornerRadiusBottomLeft=12,CornerRadiusBottomRight=12,
            ContentMarginLeft=24,ContentMarginRight=24,ContentMarginTop=18,ContentMarginBottom=18});_list.AddChild(panel);
        var columns=new HBoxContainer();columns.AddThemeConstantOverride("separation",24);panel.AddChild(columns);
        string art=q.Template switch {
            "HUNT"=>"cards/ManaBarricade","GRAPES"=>"cards/GrapeSpell","STATUE"=>"cards/MoldSpell","WEDDING"=>"cards/FlowerFieldSpell",
            "TEA"=>"cards/HotTeaSpell","FESTIVAL"=>"cards/TongueTwisterSpell","COPPER"=>"cards/RustCleanMagic","SUMMER"=>"cards/ShavedIceSpell",
            "LOST"=>"cards/AccessoriesSpell","SLEEP"=>"cards/BabySleeping","CARGO"=>"events/Kraft1",_=>"events/ReadingGrimoire"};
        columns.AddChild(new TextureRect{Name="Illustration",Texture=ResourceLoader.Load<Texture2D>("res://FrierenMod/images/"+art+".png"),CustomMinimumSize=new(235,179),
            ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,StretchMode=TextureRect.StretchModeEnum.KeepAspectCovered,SizeFlagsVertical=Control.SizeFlags.ShrinkCenter,ClipContents=true,MouseFilter=Control.MouseFilterEnum.Ignore});
        var content=new VBoxContainer{SizeFlagsHorizontal=Control.SizeFlags.ExpandFill};content.AddThemeConstantOverride("separation",8);columns.AddChild(content);
        content.AddChild(Label("Title",Text("NAME."+q.Template)+"  "+new string('★',q.Stars),27,new("f4d69a")));
        content.AddChild(Label("Request",Text("REQUEST."+q.Template),18,new("bdb3a0")));
        var goal=Label("Goal",GoalText(_player,q),21,new("e8e1d4"));content.AddChild(goal);
        if(MegaCrit.Sts2.Core.Models.ModelDb.AllCards.Any(c=>c.GetType().Name==q.Spell))Hover(goal,()=>{var c=CommissionCmd.Spell(q.Spell).ToMutable();c.Owner=_player;return [new CardHoverTip(c)];});
        if(q.Goal==CommissionGoal.Cargo)Hover(goal,()=>{var c=CommissionCargo.Goods(_player).FirstOrDefault();if(c==null){c=(ValuableCargo)MegaCrit.Sts2.Core.Models.ModelDb.Card<ValuableCargo>().ToMutable();c.Owner=_player;}return [new CardHoverTip(c)];});
        var reward=Label("Reward",RewardText(_player,q),21,new("b6d7bc"));content.AddChild(reward);
        if(q.Reward.Kind==CommissionRewardKind.RareChoice){
            var choices=new HBoxContainer{Name="CardChoices"};choices.AddThemeConstantOverride("separation",20);content.AddChild(choices);
            foreach(var id in q.Reward.Choices??[]){
                var card=CommissionCmd.Spell(id).ToMutable();card.Owner=_player;
                var name=Label("CardChoice",card.Title,19,new("f4d69a"));name.SizeFlagsHorizontal=Control.SizeFlags.ExpandFill;choices.AddChild(name);
                Hover(name,()=>[new CardHoverTip(card)]);
            }
            Hover(reward,()=>[new HoverTip(new LocString("frieren","COMMISSION.RARE_SELECT"),string.Join("\n",(q.Reward.Choices??[]).Select(id=>CommissionCmd.Spell(id).Title)))]);
        }else Hover(reward,()=>RewardTips(_player,q));
        string status=!current&&!ready&&q.Status!=CommissionStatus.Claimed?Text("EXPIRED"):q.Status switch {
            CommissionStatus.Available=>Text("AVAILABLE"),CommissionStatus.Claimed=>Text("CLAIMED"),CommissionStatus.Abandoned=>Text("ABANDONED"),
            _=>ready?Text("READY"):q.Goal==CommissionGoal.Cargo?Text("AUTO_DELIVERY"):q.Goal==CommissionGoal.EmpoweredElite?Text(q.EliteBattle.Length>0?"HUNT_BOUND":"HUNT_WAIT"):Text("IN_PROGRESS")};
        content.AddChild(Label("Status",status,21,ready?new("a7f2bd"):new("a9baca")));
        var actions=new VBoxContainer{CustomMinimumSize=new(235,0),Alignment=BoxContainer.AlignmentMode.Center};columns.AddChild(actions);
        void Action(string name,string text,CommissionOperation op,bool enabled){var b=Button(name,text,()=>{_=TaskHelper.RunSafely(Act(q.Id,op));},new(235,64));actions.AddChild(b);if(!enabled){b.Disable();b.Modulate=new(1,1,1,.45f);}}
        if(q.Status==CommissionStatus.Available)Action("Accept",Text("ACCEPT"),CommissionOperation.Accept,current&&canAct&&state.ActiveCount==0);
        else if((current||ready)&&q.Status==CommissionStatus.Accepted){
            if(q.Goal!=CommissionGoal.Cargo)Action("Claim",Text(ready&&!CommissionCmd.CanReceive(_player,q.Reward)?"POTION_FULL":"CLAIM"),CommissionOperation.Claim,canAct&&ready&&CommissionCmd.CanReceive(_player,q.Reward));
            if(current)Action("Abandon",Text("ABANDON"),CommissionOperation.Abandon,canAct);
        }
    }
    private async Task Act(int id, CommissionOperation operation) {
        if (_applying) return; _applying = true;
        try {
            var q=CommissionCmd.State(_player).Quests.Single(x=>x.Id==id);
            if(operation==CommissionOperation.Claim&&q.Reward.Kind is CommissionRewardKind.RareChoice or CommissionRewardKind.Card)Close();
            _notice=await CommissionCmd.Send(_player,id,operation)?"":Text("RETRY");
        }
        finally { _applying = false; if (GodotObject.IsInstanceValid(View)) Refresh(true); }
    }
    public static void Attach(Control root,Player player,Func<IOverlayScreen?> overlay)
    {
        var screen=new CommissionScreen(root,player);var seen=new HashSet<int>();bool hovered=false;string hoverText="";
        var button=Button("FrierenCommissionButton",Text("ENTRY"),()=>{if(screen.View.Visible)screen.Close();else screen.Open();},new(120,64));
        button.MouseEntered+=()=>{hovered=true;hoverText="";};button.MouseExited+=()=>{hovered=false;CommissionHoverTips.Remove(button);};
        var row=root.FindChild("RightAlignedStuff",true,false) as HBoxContainer??root.GetNodeOrNull<HBoxContainer>("FrierenTopBarRow");
        if(row!=null){row.AddChild(button);row.MoveChild(button,0);}else{root.AddChild(button);button.SetAnchorsPreset(Control.LayoutPreset.TopRight);button.Position=new(root.Size.X-350,8);}
        var timer=new Godot.Timer{WaitTime=.1,Autostart=true};root.AddChild(timer);
        timer.Timeout+=()=>{
            var state=CommissionCmd.State(player);
            var completed=state.Quests.Where(q=>!q.Legacy&&q.Act==state.CurrentAct&&q.Status==CommissionStatus.Claimed).Select(q=>q.Id).ToArray();
            if(screen.View.Visible)seen.UnionWith(completed);
            bool attention=!screen.View.Visible&&(NeedsAttention(player)||completed.Any(id=>!seen.Contains(id)));
            button.GetNode<Label>("Label").Text=Text("ENTRY")+(attention?" !":"");
            bool blocked=NModalContainer.Instance?.OpenModal!=null||!MagicBookUi.AllowsViewOver(overlay())||root is NGlobalUi global&&global.CapstoneContainer.InUse&&global.CapstoneContainer.CurrentCapstoneScreen!=screen.View;
            if(blocked)button.Disable();else button.Enable();
            float pulse=(float)(.5+.5*Math.Sin(Time.GetTicksMsec()/260d));button.Modulate=attention?new Color(1,.75f+.25f*pulse,.45f+.55f*pulse):Colors.White;
            if(hovered&&MegaCrit.Sts2.Core.Nodes.NGame.Instance!=null){var text=CurrentSummary(player);if(text!=hoverText){hoverText=text;CommissionHoverTips.Remove(button);CommissionHoverTips.Show(button,[new HoverTip(new LocString("frieren","COMMISSION.CURRENT"),text)]);}}
        };
        button.TreeExiting+=()=>CommissionHoverTips.Remove(button);
    }
}
