using FrierenMod.Core;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Rewards;
using FrierenMod.Scripts.UI;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.TopBar;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;

public static class UiTodoTests
{
    // 选牌任务与选择数据由原屏持有；覆盖它只允许隐藏/恢复，不能关闭或完成任务。
    private sealed class SelectionScreen(Control view) : IOverlayScreen
    {
        public Control? DefaultFocusedControl => view;
        public NetScreenType ScreenType => NetScreenType.CardSelection;
        public bool UseSharedBackstop => true;
        public int Hidden { get; private set; }
        public int Shown { get; private set; }
        public int Closed { get; private set; }
        public int SelectedIndex { get; set; } = 1;
        public readonly TaskCompletionSource<int> Selection = new();
        public void AfterOverlayOpened() { }
        public void AfterOverlayClosed() { Closed++; Selection.TrySetCanceled(); }
        public void AfterOverlayShown() { Shown++; view.Show(); }
        public void AfterOverlayHidden() { Hidden++; view.Hide(); }
    }

    public static async Task<int> RunFactorFeedback(Node root)
    {
        var player=Player.CreateForNewRun<Frieren>(UnlockState.all,1);
        var run=RunState.CreateForTest([player],seed:"FACTOR-FEEDBACK");
        RunManager.Instance.SetUpTest(run,new NetSingleplayerGameService()); LocalContext.NetId=1;
        var deck=new MagicDeckState();
        for(int i=0;i<3;i++){deck.PrepareDraft(_=>0);deck.SelectInitial(deck.Draft[0]);}
        var tool=deck.AddProp("UnbelievableTool");
        deck.Add("BarrierMagic"); MagicDeckStore.Write(player,deck);
        LocManager.Instance.SetLanguage("zhs");
        var ui=new Control {Size=new(1920,1080)};root.AddChild(ui);
        ui.AddChild(new ColorRect {Size=ui.Size,Color=new Color("17202c"),MouseFilter=Control.MouseFilterEnum.Ignore});
        var book=new MagicBookScreen(ui,player);book.Open();
        for(int i=0;i<4;i++)await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);
        var toolHolder=(NGridCardHolder)book.View.FindChild("Prop_"+tool.InstanceId,true,false);
        toolHolder.EmitSignal(NCardHolder.SignalName.AltPressed,toolHolder);
        var target=(NGridCardHolder)book.View.FindChild("Factor_"+deck.Items[0].InstanceId,true,false);
        target.EmitSignal(NCardHolder.SignalName.Pressed,target);
        await root.ToSignal(root.GetTree().CreateTimer(.6),SceneTreeTimer.SignalName.Timeout);
        var glow=target.CardNode!.CardHighlight;
        if(!glow.IsVisibleInTree() || glow.Modulate.A<.9f || ((ShaderMaterial)glow.Material).GetShaderParameter("width").AsSingle()<.05f)
            throw new Exception("被选中的因子没有持续外光反馈");
        if(!((Label)book.View.FindChild("BookStatus",true,false)).Text.Contains("1/2"))throw new Exception("因子目标选择未生效");
        if(OS.GetCmdlineUserArgs().Contains("--ui-snapshot"))
            root.GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../artifacts/factor-selection.png"));
        target.EmitSignal(NCardHolder.SignalName.Pressed,target);
        if(glow.Modulate.A!=0)throw new Exception("取消选中后仍保留外光");
        book.Close(); ui.QueueFree();
        for(int i=0;i<4;i++)await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);
        RunManager.Instance.CleanUp();return 3;
    }

    public static async Task<int> Run(Node root, bool topBarOnly = false)
    {
        int count = 0;
        void Check(bool value, string name) { if (!value) throw new Exception(name); GD.Print("PASS 因子界面：" + name); count++; }
        var player = Player.CreateForNewRun<Frieren>(UnlockState.all, 1);
        var run = RunState.CreateForTest([player], seed: "FRIEREN-UI-TODOS");
        RunManager.Instance.SetUpTest(run, new NetSingleplayerGameService());
        LocalContext.NetId = 1;
        var combat = new CombatState(runState: run); combat.AddPlayer(player);
        combat.AddCreature(combat.CreateCreature(ModelDb.Monster<MegaCrit.Sts2.Core.Models.Monsters.Axebot>().ToMutable(),
            CombatSide.Enemy, null));
        CombatManager.Instance.SetUpCombat(combat);
        AccessTools.Property(typeof(CombatManager), nameof(CombatManager.IsInProgress)).SetValue(CombatManager.Instance, true);
        foreach (var pile in player.PlayerCombatState!.AllPiles)
            foreach (var card in pile.Cards.ToArray()) pile.RemoveInternal(card);
        var deck = new MagicDeckState();
        for (int i = 0; i < 3; i++) { deck.PrepareDraft(_ => 0); deck.SelectInitial(deck.Draft[0]); }
        deck.GrantStartingProps();
        foreach (var gift in deck.Props.ToArray()) deck.Remove(gift.InstanceId);
        foreach (var (slot, id) in new[] { (ManaSlot.Draw,"BuildUpBody"), (ManaSlot.Hand,"BarrierMagic"), (ManaSlot.Discard,"GreatMageComposure") })
            deck.Equip(deck.Add(id).InstanceId, slot);
        var prop = deck.AddProp("MysteryBox"); MagicDeckStore.Write(player, deck);
        var uncommon = deck.Add("HighSpeedMovement");
        var rare = deck.Add("PeacefulDays");
        var tool = deck.AddProp("UnbelievableTool");
        MagicDeckStore.Write(player, deck);
        await ManaCmd.Generate(player, PileType.Draw, 7);
        await ManaCmd.Generate(player, PileType.Hand, 2);
        await ManaCmd.Generate(player, PileType.Discard, 1);
        LocManager.Instance.SetLanguage("zhs");
        var ui = new Control { Name = "UiTodoCanvas", Size = new(1920,1080) };
        root.AddChild(ui);
        ui.AddChild(new ColorRect { Size = ui.Size, Color = new Color("17202c"), MouseFilter = Control.MouseFilterEnum.Ignore });
        var row = new HBoxContainer { Name = "RightAlignedStuff", Alignment = BoxContainer.AlignmentMode.End,
            GrowHorizontal = Control.GrowDirection.Begin, MouseFilter = Control.MouseFilterEnum.Ignore };
        ui.AddChild(row);
        row.SetAnchorsPreset(Control.LayoutPreset.TopRight);
        row.OffsetLeft=-492; row.OffsetRight=-16; row.OffsetBottom=80;
        var clock = new Label { Name="TimerContainer", Text="00:13:42", CustomMinimumSize=new(160,80),
            VerticalAlignment=VerticalAlignment.Center, HorizontalAlignment=HorizontalAlignment.Right };
        row.AddChild(clock);
        var map = GD.Load<PackedScene>("res://scenes/ui/top_bar/top_bar_map_button.tscn").Instantiate<NTopBarMapButton>();
        map.Name="Map"; row.AddChild(map);
        var deckMargin = new MarginContainer { Name="DeckContainer", CustomMinimumSize=new(80,80) };
        row.AddChild(deckMargin);
        var deckButton = GD.Load<PackedScene>("res://scenes/ui/top_bar/top_bar_deck_button.tscn").Instantiate<NTopBarDeckButton>();
        deckMargin.AddChild(deckButton); deckButton.Initialize(player);
        row.AddChild(GD.Load<PackedScene>("res://scenes/ui/top_bar/top_bar_settings_button.tscn").Instantiate<NTopBarPauseButton>());
        IOverlayScreen? active = null;
        bool blocked = false;
        MagicBookUi.Attach(ui, player, () => active, () => blocked);
        var timer = ui.GetNode<Godot.Timer>("FrierenManaRefresh");
        var button = (MagicFactorButton)ui.FindChild("FrierenMagicBookButton",true,false);
        var hud = ui.GetNode<Control>("FrierenManaCounter");
        var preview = ui.GetNode<Control>("FrierenFactorPreview");
        var book = ui.GetNode<Control>("FrierenMagicBook");
        timer.EmitSignal(Godot.Timer.SignalName.Timeout);
        foreach (var (language,title) in new[] {("zhs","魔法因子"),("eng","Magic Factors")}) {
            LocManager.Instance.SetLanguage(language);
            Check(new LocString("frieren","BOOK_BUTTON").GetFormattedText() == title, language + "入口名称");
        }
        foreach (var (language,title,description) in new[] {
            ("zhs","魔法牌组","查看你拥有的[gold]魔法因子[/gold]和[gold]魔具[/gold]。"),
            ("eng","Magic Deck","View all your [gold]Magic Factors[/gold] and [gold]Magic Tools[/gold].") }) {
            LocManager.Instance.SetLanguage(language);
            var tip=(HoverTip)AccessTools.Method(typeof(MagicFactorButton),"CreateHoverTip").Invoke(null,null)!;
            Check(tip.Title==title && tip.Description==description && string.IsNullOrEmpty(button.TooltipText),
                language+"魔法牌组采用一代说明与关键词高亮，不叠加普通提示");
        }
        LocManager.Instance.SetLanguage("zhs");
        var reward = new FactorReward(player);
        string rewardIcon = (string)AccessTools.Property(typeof(MegaCrit.Sts2.Core.Rewards.Reward), "IconPath").GetValue(reward)!;
        Check(button.GetNode<TextureRect>("Control/Icon").Texture.ResourcePath == rewardIcon &&
            button.HasNode("DeckCardCount"), "顶部入口复用原生图标与右下角数量，奖励使用同一图标");
        var countLabel = button.GetNode<MegaCrit.Sts2.addons.mega_text.MegaLabel>("DeckCardCount");
        var countDeck = MagicDeckStore.Read(player);
        Check(countLabel.Text == (countDeck.Items.Count + countDeck.Props.Count).ToString(), "数量包含全部因子和魔具，包括装载中的因子");
        int extraProp = countDeck.AddProp("UnbelievableTool").InstanceId;
        MagicDeckStore.Write(player, countDeck); timer.EmitSignal(Godot.Timer.SignalName.Timeout);
        Check(countLabel.Text == (countDeck.Items.Count + countDeck.Props.Count).ToString(), "获得魔具后牌组数量更新");
        countDeck.Remove(extraProp); MagicDeckStore.Write(player, countDeck); timer.EmitSignal(Godot.Timer.SignalName.Timeout);
        Check(countLabel.Text == (countDeck.Items.Count + countDeck.Props.Count).ToString(), "消耗魔具后牌组数量恢复");
        if (topBarOnly) {
            await root.ToSignal(root.GetTree().CreateTimer(.5), SceneTreeTimer.SignalName.Timeout);
            if (OS.GetCmdlineUserArgs().Contains("--ui-snapshot"))
                root.GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../artifacts/magic-deck-count.png"));
            ui.QueueFree();
            for (int i = 0; i < 5; i++) await root.ToSignal(root.GetTree(), SceneTree.SignalName.ProcessFrame);
            RunManager.Instance.CleanUp(); return count;
        }
        foreach (var factor in SpellCatalog.All) {
            var card = FactorCard.Create(player,factor.Id);
            var expected = factor.Rarity switch { FactorRarity.Rare => CardRarity.Rare, FactorRarity.Uncommon => CardRarity.Uncommon, _ => CardRarity.Common };
            if (card.Rarity != expected || card.CanBeGeneratedInCombat || card.CanBeGeneratedByModifiers || card.ShouldShowInCardLibrary)
                throw new Exception("因子显示或生成限制错误：" + factor.Id);
        }
        Check(player.Character.CardPool.AllCards.OfType<FactorCard>().Count() == 28 &&
            !player.Character.CardPool.GetUnlockedCards(player.UnlockState,CardMultiplayerConstraint.SingleplayerOnly).OfType<FactorCard>().Any(),
            "28种因子稀有度正确，保留模型注册且排除普通奖励与生成");
        foreach (string language in new[] {"zhs","eng"}) {
            LocManager.Instance.SetLanguage(language);
            foreach (var factor in SpellCatalog.All) {
                string text=FactorCard.Create(player,factor.Id).GetDescriptionForPile(PileType.None);
                string effect=new LocString("frieren","FACTOR."+factor.Id).GetFormattedText();
                if (text!=effect) throw new Exception("奖励因子包含多余说明或空行："+language+"/"+factor.Id+"："+text);
            }
            var equipped=FactorCard.Create(player,"BuildUpBody",ManaSlot.Draw).GetDescriptionForPile(PileType.None);
            var chanting=FactorCard.Create(player,"BuildUpBody",ManaSlot.Draw,new ChantOption(ManaSlot.Draw,"BuildUpBody",2,2,2)).GetDescriptionForPile(PileType.None);
            Check(equipped.Contains('\n') && chanting.Contains('\n'), language+"奖励仅显示效果，装载与咏唱保留槽位和支付提示");
        }
        LocManager.Instance.SetLanguage("zhs");
        Check(hud.GetNode<Control>("SlotDraw").GetNode<Label>("Count").Text == "7" &&
            hud.GetNode<Control>("SlotHand").GetNode<Label>("Count").Text == "2", "三槽魔力计数：显示=" +
                hud.GetNode<Control>("SlotDraw").GetNode<Label>("Count").Text + "/" + hud.GetNode<Control>("SlotHand").GetNode<Label>("Count").Text +
                "，实际=" + ManaCmd.InPile(player,PileType.Draw).Count() + "/" + ManaCmd.InPile(player,PileType.Hand).Count());
        hud.EmitSignal(Control.SignalName.MouseEntered);
        Check(preview.Visible && preview.GetChildren().OfType<NCard>().Count() == 3, "悬停立即显示三张原生因子卡面：可见=" + preview.Visible + "，卡牌=" + preview.GetChildren().OfType<NCard>().Count());
        for (int i = 0; i < 2; i++) await root.ToSignal(root.GetTree(), SceneTree.SignalName.ProcessFrame);
        foreach (var node in preview.GetChildren().OfType<NCard>()) {
            Check(node.MouseFilter == Control.MouseFilterEnum.Ignore, node.Name + "预览不截获鼠标");
            Check(!((Control)AccessTools.Field(typeof(NCard), "_energyIcon").GetValue(node)).Visible,
                "因子预览不显示无关的0能量图标");
            var description = node.GetNode<RichTextLabel>("%DescriptionLabel");
            if (description.GetContentHeight() > description.Size.Y + 2) throw new Exception("因子预览文字溢出：" + node.Model!.Id);
        }
        hud.EmitSignal(Control.SignalName.MouseExited);
        Check(!preview.Visible, "离开计数器立即关闭预览");
        foreach (var size in new[] { new Vector2(1280,720), new Vector2(1920,1080), new Vector2(2560,1440) }) {
            ui.Size=size;
            await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);
            hud.EmitSignal(Control.SignalName.MouseEntered);
            var bounds=new Rect2(Vector2.Zero,size);
            var popupBounds=new Rect2(preview.Position,preview.Size*preview.Scale);
            Check(Math.Abs(hud.GetRect().GetCenter().Y-size.Y/2)<1 && bounds.Encloses(hud.GetRect()) && bounds.Encloses(popupBounds) &&
                bounds.Encloses(button.GetGlobalRect()) && button.GlobalPosition.Y<80 &&
                clock.GetGlobalRect().End.X<=button.GlobalPosition.X && button.GetGlobalRect().End.X<=map.GlobalPosition.X,
                "多分辨率计数器居中，时间、因子、地图顺序正确且不重叠 "+size);
        }
        ui.Size=new(1920,1080);
        button.EmitSignal(Control.SignalName.MouseEntered);
        button._GuiInput(new InputEventMouseButton {ButtonIndex=MouseButton.Left,Pressed=true});
        button._GuiInput(new InputEventMouseButton {ButtonIndex=MouseButton.Left,Pressed=false});
        Check(book.Visible && !preview.Visible && !hud.Visible, "打开因子界面隐藏计数器与悬停预览");
        button.EmitSignal(Control.SignalName.MouseExited);
        Check(!book.FindChildren("*","Label",true,false).OfType<Label>().Any(label=>label.Text==new LocString("frieren","BOOK").GetFormattedText()),
            "删除被顶部栏遮挡的魔法因子与魔具标题");
        var inventory=(GridContainer)book.FindChild("Inventory",true,false);
        var raritySorter=(NCardViewSortButton)book.FindChild("RaritySorter",true,false);
        var typeSorter=(NCardViewSortButton)book.FindChild("TypeSorter",true,false);
        int[] InventoryIds() => inventory.GetChildren().OfType<Control>().Select(cell=>int.Parse(cell.Name.ToString()[5..])).ToArray();
        int[] common=deck.Items.Where(i=>i.Slot<0 && SpellCatalog.Get(i.SpellId).Rarity==FactorRarity.Common).Select(i=>i.InstanceId).Order().ToArray();
        int[] props=deck.Props.Select(i=>i.InstanceId).Order().ToArray();
        string beforeSort=MagicDeckStore.Read(player).ToJson();
        var equippedCells=book.FindChild("EquippedFactors",true,false).GetChildren().ToArray();
        Check(InventoryIds().SequenceEqual(common.Concat([uncommon.InstanceId]).Concat(props).Concat([rare.InstanceId])),
            "默认稀有度排序按一代将魔具放在罕见与稀有因子之间");
        raritySorter.ForceClick();
        Check(InventoryIds().SequenceEqual(new[]{rare.InstanceId}.Concat(props).Concat([uncommon.InstanceId]).Concat(common)),
            "稀有度反向排序，同类条目仍保持获得顺序");
        typeSorter.ForceClick();
        Check(InventoryIds().SequenceEqual(props.Concat(common).Concat([uncommon.InstanceId,rare.InstanceId])),
            "类型排序先魔具后未装载因子");
        typeSorter.ForceClick();
        Check(InventoryIds().SequenceEqual(common.Concat([uncommon.InstanceId,rare.InstanceId]).Concat(props)),
            "类型反向排序先未装载因子后魔具");
        AccessTools.Property(typeof(CombatManager),nameof(CombatManager.IsStarting)).SetValue(CombatManager.Instance,false);
        AccessTools.Property(typeof(CombatManager),nameof(CombatManager.PlayerActionsDisabled)).SetValue(CombatManager.Instance,false);
        var toolHolder=(NGridCardHolder)book.FindChild("Prop_"+tool.InstanceId,true,false);
        toolHolder.EmitSignal(NCardHolder.SignalName.AltPressed,toolHolder);
        var selectedHolder=(NGridCardHolder)book.FindChild("Factor_"+deck.Items.First(i=>i.Slot>=0).InstanceId,true,false);
        selectedHolder.EmitSignal(NCardHolder.SignalName.Pressed,selectedHolder);
        string selectionStatus=((Label)book.FindChild("BookStatus",true,false)).Text;
        raritySorter.ForceClick();
        Check(selectionStatus.Contains("1/2") && ((Label)book.FindChild("BookStatus",true,false)).Text==selectionStatus &&
            book.FindChild(selectedHolder.Name,true,false)==selectedHolder &&
            book.FindChild("EquippedFactors",true,false).GetChildren().SequenceEqual(equippedCells) &&
            MagicDeckStore.Read(player).ToJson()==beforeSort,
            "排序保持已装载槽位、魔具待选择目标与库存原样");
        book.Hide();
        var selectionView = new Control(); ui.AddChild(selectionView);
        var selection = new SelectionScreen(selectionView); active=selection;
        string before=MagicDeckStore.Read(player).ToJson();
        timer.EmitSignal(Godot.Timer.SignalName.Timeout);
        button.EmitSignal(NClickableControl.SignalName.Released,button);
        timer.EmitSignal(Godot.Timer.SignalName.Timeout);
        Check(button.IsEnabled && book.Visible && selection.Hidden==1 && !selectionView.Visible,
            "选牌期间可打开因子界面，计时器不会强制关闭");
        var holder=(NGridCardHolder)book.FindChild("Prop_"+prop.InstanceId,true,false);
        holder.EmitSignal(NCardHolder.SignalName.AltPressed,holder);
        Check(MagicDeckStore.Read(player).ToJson()==before && ((Label)book.FindChild("BookStatus",true,false)).Text.Contains("选牌期间"),
            "选牌期间只读查看，不消耗魔具或更改因子");
        blocked=true;
        ActiveScreenContext.Instance.Update();
        Check(!button.IsEnabled && button.Modulate==MegaCrit.Sts2.Core.Helpers.StsColors.disabledTopBarButton &&
            book.ProcessMode==Node.ProcessModeEnum.Disabled && book.MouseBehaviorRecursive==Control.MouseBehaviorRecursiveEnum.Disabled,
            "原生菜单覆盖时立即禁用入口与因子界面，使用原生禁用颜色");
        button.ForceClick();
        root.GetViewport().PushInput(new InputEventKey { Keycode=Key.Escape,Pressed=true });
        await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);
        Check(book.Visible && selection.Shown==0 && !selection.Selection.Task.IsCompleted,
            "菜单期间点击和Esc不穿透到原选牌或因子界面");
        blocked=false;
        ActiveScreenContext.Instance.Update();
        Check(button.IsEnabled && button.Modulate==Colors.White && book.ProcessMode==Node.ProcessModeEnum.Inherit,
            "关闭原生菜单恢复因子界面操作");
        root.GetViewport().PushInput(new InputEventKey { Keycode=Key.Escape,Pressed=true });
        await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);
        Check(!book.Visible && selectionView.Visible && selection.Shown==1 && selection.Closed==0 &&
            !selection.Selection.Task.IsCompleted && selection.SelectedIndex==1,"关闭后恢复原选牌和选中状态，不取消选择任务");
        button.EmitSignal(NClickableControl.SignalName.Released,button);
        active=null;
        timer.EmitSignal(Godot.Timer.SignalName.Timeout);
        Check(!book.Visible && selection.Shown==1,"原选牌结束或切换后不复活旧界面");
        Check(MagicDeckStore.Read(player).ToJson()==before,"反复查看保持库存与槽位不变");

        if (OS.GetCmdlineUserArgs().Contains("--ui-snapshot")) {
            await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);
            hud.EmitSignal(Control.SignalName.MouseEntered);
            for(int i=0;i<4;i++) await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);
            root.GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../artifacts/factor-hover.png"));
            button.EmitSignal(NClickableControl.SignalName.Released,button);
            for(int i=0;i<4;i++) await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);
            root.GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../artifacts/factor-screen.png"));
            book.Hide();
            // 测试场景没有 NGame 全局宿主，使用同一个原生提示场景核对文字与布局。
            var tip=(HoverTip)AccessTools.Method(typeof(MagicFactorButton),"CreateHoverTip").Invoke(null,null)!;
            var tipView=GD.Load<PackedScene>("res://scenes/ui/hover_tip.tscn").Instantiate<Control>();
            ui.AddChild(tipView);
            tipView.GetNode<Label>("%Title").Text=tip.Title;
            tipView.GetNode<RichTextLabel>("%Description").Text=tip.Description;
            tipView.Position=new(button.GetGlobalRect().End.X-360,button.GetGlobalRect().End.Y+20);
            tipView.Size=new(360,tipView.Size.Y);
            tipView.ResetSize();
            for(int i=0;i<4;i++) await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);
            root.GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../artifacts/factor-tooltip.png"));
            tipView.QueueFree();
        }
        ui.QueueFree();
        for(int i=0;i<4;i++) await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);
        return count;
    }
}
