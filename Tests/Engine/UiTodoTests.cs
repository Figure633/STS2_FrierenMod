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
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
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

    public static async Task<int> Run(Node root)
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
        foreach (var (slot, id) in new[] { (ManaSlot.Draw,"BuildUpBody"), (ManaSlot.Hand,"BarrierMagic"), (ManaSlot.Discard,"GreatMageComposure") })
            deck.Equip(deck.Add(id).InstanceId, slot);
        var prop = deck.AddProp("MysteryBox"); MagicDeckStore.Write(player, deck);
        await ManaCmd.Generate(player, PileType.Draw, 7);
        await ManaCmd.Generate(player, PileType.Hand, 2);
        await ManaCmd.Generate(player, PileType.Discard, 1);
        LocManager.Instance.SetLanguage("zhs");
        var ui = new Control { Name = "UiTodoCanvas", Size = new(1920,1080) };
        root.AddChild(ui);
        ui.AddChild(new ColorRect { Size = ui.Size, Color = new Color("17202c"), MouseFilter = Control.MouseFilterEnum.Ignore });
        IOverlayScreen? active = null;
        MagicBookUi.Attach(ui, player, () => active);
        var timer = ui.GetNode<Godot.Timer>("FrierenManaRefresh");
        var button = ui.GetNode<Button>("FrierenMagicBookButton");
        var hud = ui.GetNode<Control>("FrierenManaCounter");
        var preview = ui.GetNode<Control>("FrierenFactorPreview");
        var book = ui.GetNode<Control>("FrierenMagicBook");
        timer.EmitSignal(Godot.Timer.SignalName.Timeout);
        foreach (var (language,title) in new[] {("zhs","魔法因子"),("eng","Magic Factors")}) {
            LocManager.Instance.SetLanguage(language);
            Check(new LocString("frieren","BOOK_BUTTON").GetFormattedText() == title, language + "入口名称");
        }
        LocManager.Instance.SetLanguage("zhs");
        button.Text = new LocString("frieren","BOOK_BUTTON").GetFormattedText();
        var reward = new FactorReward(player);
        string rewardIcon = (string)AccessTools.Property(typeof(MegaCrit.Sts2.Core.Rewards.Reward), "IconPath").GetValue(reward)!;
        Check(button.Icon.ResourcePath == rewardIcon, "奖励与右上角入口使用同一图标");
        foreach (var factor in SpellCatalog.All) {
            var card = FactorCard.Create(player,factor.Id);
            var expected = factor.Rarity switch { FactorRarity.Rare => CardRarity.Rare, FactorRarity.Uncommon => CardRarity.Uncommon, _ => CardRarity.Common };
            if (card.Rarity != expected || card.CanBeGeneratedInCombat || card.CanBeGeneratedByModifiers || card.ShouldShowInCardLibrary)
                throw new Exception("因子显示或生成限制错误：" + factor.Id);
        }
        Check(player.Character.CardPool.AllCards.OfType<FactorCard>().Count() == 28 &&
            !player.Character.CardPool.GetUnlockedCards(player.UnlockState,CardMultiplayerConstraint.SingleplayerOnly).OfType<FactorCard>().Any(),
            "28种因子稀有度正确，保留模型注册且排除普通奖励与生成");
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
                bounds.Encloses(button.GetRect()) && button.Position.Y<80, "多分辨率居中与界面边界 "+size);
        }
        ui.Size=new(1920,1080);
        button.EmitSignal(BaseButton.SignalName.Pressed);
        Check(book.Visible && !preview.Visible && !hud.Visible, "打开因子界面隐藏计数器与悬停预览");
        book.Hide();
        var selectionView = new Control(); ui.AddChild(selectionView);
        var selection = new SelectionScreen(selectionView); active=selection;
        string before=MagicDeckStore.Read(player).ToJson();
        timer.EmitSignal(Godot.Timer.SignalName.Timeout);
        button.EmitSignal(BaseButton.SignalName.Pressed);
        timer.EmitSignal(Godot.Timer.SignalName.Timeout);
        Check(!button.Disabled && book.Visible && selection.Hidden==1 && !selectionView.Visible,
            "选牌期间可打开因子界面，计时器不会强制关闭");
        var holder=(NGridCardHolder)book.FindChild("Prop_"+prop.InstanceId,true,false);
        holder.EmitSignal(NCardHolder.SignalName.AltPressed,holder);
        Check(MagicDeckStore.Read(player).ToJson()==before && ((Label)book.FindChild("BookStatus",true,false)).Text.Contains("选牌期间"),
            "选牌期间只读查看，不消耗魔具或更改因子");
        root.GetViewport().PushInput(new InputEventKey { Keycode=Key.Escape,Pressed=true });
        await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);
        Check(!book.Visible && selectionView.Visible && selection.Shown==1 && selection.Closed==0 &&
            !selection.Selection.Task.IsCompleted && selection.SelectedIndex==1,"关闭后恢复原选牌和选中状态，不取消选择任务");
        button.EmitSignal(BaseButton.SignalName.Pressed);
        active=null;
        timer.EmitSignal(Godot.Timer.SignalName.Timeout);
        Check(!book.Visible && selection.Shown==1,"原选牌结束或切换后不复活旧界面");
        Check(MagicDeckStore.Read(player).ToJson()==before,"反复查看保持库存与槽位不变");

        if (OS.GetCmdlineUserArgs().Contains("--ui-snapshot")) {
            await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);
            hud.EmitSignal(Control.SignalName.MouseEntered);
            for(int i=0;i<4;i++) await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);
            root.GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../artifacts/factor-hover.png"));
            button.EmitSignal(BaseButton.SignalName.Pressed);
            for(int i=0;i<4;i++) await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);
            root.GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../artifacts/factor-screen.png"));
        }
        ui.QueueFree();
        for(int i=0;i<4;i++) await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);
        return count;
    }
}
