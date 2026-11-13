using FrierenMod.Scripts.Shared.Compatibility;
using System.Text.Json;
using FrierenMod.Core;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.UI;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Unlocks;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Vfx.Cards;
using HarmonyLib;

public static class MagicDeckAnimationTests
{
    private static int ExhaustsCreated;
    private static void CreateExhaust(ref bool __state)
    {
        __state = TestMode.IsOn;
        TestMode.IsOn = false;
    }
    private static void RestoreTestMode(bool __state, CanvasItem? __result)
    {
        TestMode.IsOn = __state;
        if (__result != null) ExhaustsCreated++;
    }
    private static bool CreateTrail(Control card, string characterTrailPath, ref NCardTrailVfx __result)
    {
        // 原生 TestMode 跳过视觉工厂；测试仍使用原版场景、脚本和粒子，保留存档隔离。
        __result = GD.Load<PackedScene>(characterTrailPath).Instantiate<NCardTrailVfx>();
        __result.Set(NCardTrailVfx.PropertyName._nodeToFollow, card);
        return false;
    }
    public static async Task<int> Run(Node root)
    {
        int count = 0;
        var visualFactory = new Harmony("frieren.tests.native_magic_flight");
        visualFactory.Patch(AccessTools.Method(typeof(NCardTrailVfx), nameof(NCardTrailVfx.Create)),
            prefix: new HarmonyMethod(typeof(MagicDeckAnimationTests), nameof(CreateTrail)));
        visualFactory.Patch(GameApi.ExhaustCreate,
            prefix: new HarmonyMethod(typeof(MagicDeckAnimationTests), nameof(CreateExhaust)),
            postfix: new HarmonyMethod(typeof(MagicDeckAnimationTests), nameof(RestoreTestMode)));
        void Check(bool ok, string name) { if (!ok) throw new Exception(name); GD.Print("PASS 动画：" + name); count++; }
        async Task Tick() => await root.ToSignal(root.GetTree(), SceneTree.SignalName.ProcessFrame);
        async Task Snapshot(string name) {
            await Tick();
            if (OS.GetCmdlineUserArgs().Contains("--ui-snapshot"))
                root.GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../artifacts/" + name + ".png"));
        }
        var player = Player.CreateForNewRun<Frieren>(UnlockState.all, 1);
        var run = RunState.CreateForTest([player], seed: "FRIEREN-ANIMATIONS");
        RunManager.Instance.SetUpTest(run, new NetSingleplayerGameService()); LocalContext.NetId = 1;
        run.PushRoom(new RestSiteRoom());
        var deck = MagicDeckState.FromJson(JsonSerializer.Serialize(new MagicDeckSave(3,
            MagicDeckState.CreatePrototypeLoadout().Items.ToArray(), true, 3, StartingPropsGranted: true)));
        int box = deck.AddProp("MysteryBox").InstanceId;
        int circle = deck.AddProp("TransmutationCircle").InstanceId;
        MagicDeckStore.Write(player, deck);
        var canvas = new CanvasLayer { Transform = new Transform2D(0, Vector2.One * 0.85f, 0, new Vector2(40, 20)) };
        root.AddChild(canvas);
        var ui = new Control { Size = root.GetViewport().GetVisibleRect().Size }; canvas.AddChild(ui);
        MagicBookUi.Attach(ui, player, () => null, () => false);
        var layer = ui.GetNode<Control>("FrierenMagicDeckAnimations");
        Check(MagicDeckAnimations.IsAttached(player) && layer.MouseBehaviorRecursive == Control.MouseBehaviorRecursiveEnum.Disabled,
            "表现层绑定本地玩家并穿透输入");
        string before = MagicDeckStore.Read(player).ToJson();
        Check(!await MagicPropCmd.UseSelected(player, circle, []) && layer.GetChildCount() == 0 &&
            MagicDeckStore.Read(player).ToJson() == before, "目标无效时不播放、不扣库存");
        var toggle = (MagicFactorButton)ui.FindChild("FrierenMagicBookButton", true, false);
        toggle.EmitSignal(NClickableControl.SignalName.Released, toggle);
        await Tick();
        async Task RightClick(int id) {
            var holder = (NGridCardHolder)ui.FindChild("Prop_" + id, true, false);
            var point = holder.Hitbox.GetGlobalTransformWithCanvas() * (holder.Hitbox.Size / 2);
            root.GetViewport().PushInput(new InputEventMouseButton { Position=point, GlobalPosition=point,
                ButtonIndex=MouseButton.Right, Pressed=true }, true);
            await Tick();
            root.GetViewport().PushInput(new InputEventMouseButton { Position=point, GlobalPosition=point,
                ButtonIndex=MouseButton.Right, Pressed=false }, true);
            for (int i=0;i<8;i++) await Tick();
        }
        await RightClick(circle);
        var circleHolder = (NGridCardHolder)ui.FindChild("Prop_" + circle, true, false);
        Check(circleHolder.CardNode!.CardHighlight.Modulate.A > 0, "完整界面实际右键可选中魔具");
        await RightClick(circle);
        var source = (NGridCardHolder)ui.FindChild("Prop_" + box, true, false);
        Vector2 sourcePosition = source.CardNode!.GlobalPosition;
        await RightClick(box);
        var use = MagicDeckAnimations.WhenIdle(player);
        await Tick();
        var pulse = layer.GetNode<NCard>("UsedMagicProp");
        Check(pulse.GlobalPosition.DistanceTo(sourcePosition) < 1, "在魔具原位置播放绿光，不重复显示到屏幕中央");
        Check(!use.IsCompleted && MagicPropCmd.IsBusy(player) && pulse.CardHighlight.Modulate.G > 0.8f,
            "盲盒成功使用后本体闪绿光，动画期间防止重复使用");
        Check(MagicDeckStore.Read(player).Items.Count == 5 && MagicDeckStore.Read(player).Props.Count == 1,
            "动画开始前库存已经准确提交一次");
        Check(!source.IsVisibleInTree(), "消耗副本播放时隐藏旧魔具，不留下可复用的原卡面");
        await Snapshot("magic-prop-green-pulse");
        bool seenExhaust = false, seenFly = false, capturedNew = false;
        var seenNew = new HashSet<int>();
        var clearedNew = new HashSet<int>();
        var addedIds = MagicDeckStore.Read(player).Items.TakeLast(2).Select(i=>i.InstanceId).ToArray();
        var elapsed = System.Diagnostics.Stopwatch.StartNew();
        while (elapsed.Elapsed < TimeSpan.FromSeconds(8) && !use.IsCompleted) {
            await Tick();
            seenFly |= layer.GetChildren().OfType<NCardFlyVfx>().Any();
            if (!seenExhaust && layer.GetChildren().Where(GameApi.IsExhaust).Any()) {
                seenExhaust = true;
                Check(layer.GetChildren().Where(GameApi.IsExhaust).SelectMany(e=>e.FindChildren("*", "", true, false)).OfType<GpuParticles2D>().Any(p=>p.Emitting),
                    "魔具原位置运行原版消耗粒子");
                var settle=layer.CreateTween(); settle.TweenInterval(0.16); await settle.AwaitFinished(layer);
                await Snapshot("magic-prop-native-exhaust");
            }
            foreach (int id in addedIds) {
                if (ui.FindChild("Factor_"+id,true,false) is NGridCardHolder holder && holder.IsVisibleInTree() &&
                    holder.CardNode!.CardHighlight.Modulate==NCardHighlight.playableColor &&
                    ui.FindChild("InventoryScroll",true,false) is ScrollContainer scroll &&
                    scroll.GetGlobalRect().HasPoint(holder.CardNode.GlobalPosition)) {
                    float width = (float)AccessTools.Method(typeof(NCardHighlight), "GetShaderParameter").Invoke(holder.CardNode.CardHighlight, null)!;
                    if (width > 0.01f) seenNew.Add(id);
                    else if (seenNew.Contains(id)) clearedNew.Add(id);
                }
            }
            if (seenNew.Count==2 && !capturedNew) { await Snapshot("magic-factors-obtained"); capturedNew=true; }
        }
        Check(seenExhaust && seenNew.Count==2 && !seenFly, "两个新因子直接在库存中依次闪光，不飞回魔法书图标");
        await use;
        for (int i=0;i<8;i++) await Tick();
        Check(addedIds.All(id => ui.FindChild("Factor_"+id,true,false) is NGridCardHolder holder &&
            (float)AccessTools.Method(typeof(NCardHighlight), "GetShaderParameter").Invoke(holder.CardNode!.CardHighlight, null)! < .001f),
            "两个因子完成队列并重新排版后均无残留蓝光");
        Check(layer.GetChildren().All(GameApi.IsExhaust) && !MagicPropCmd.IsBusy(player) && elapsed.Elapsed<TimeSpan.FromSeconds(1.6), "获得闪光结束即释放操作锁，原生粒子尾部独立收尾且盲盒等待少于1.6秒");
        await RightClick(circle);
        circleHolder = (NGridCardHolder)ui.FindChild("Prop_" + circle, true, false);
        Check(circleHolder.CardNode!.CardHighlight.Modulate.A > 0 &&
            circleHolder.CardNode.MouseBehaviorRecursive == Control.MouseBehaviorRecursiveEnum.Inherited,
            "动画结束并复用卡牌后，实际右键仍可选中另一魔具");
        await RightClick(circle);
        var materialIds=MagicDeckStore.Read(player).Items.Take(2).Select(i=>i.InstanceId).ToArray();
        int previousExhausts=ExhaustsCreated;
        var materialUse=MagicPropCmd.UseSelected(player,circle,materialIds);
        int maxExhausts=0; elapsed.Restart();
        while(!materialUse.IsCompleted && elapsed.Elapsed<TimeSpan.FromSeconds(8)) {
            await Tick(); maxExhausts=Math.Max(maxExhausts,layer.GetChildren().Where(GameApi.IsExhaust).Count());
        }
        // TestMode 会跳过原生特效尾部等待，统计创建次数而不依赖尾部存活时长。
        Check(await materialUse && ExhaustsCreated-previousExhausts==3 && maxExhausts>0 && materialIds.All(id=>MagicDeckStore.Read(player).Items.All(i=>i.InstanceId!=id)) &&
            layer.GetChildren().All(GameApi.IsExhaust), "上次粒子未结束仍可使用炼成，三个消耗并行结算及回收各一次");
        toggle.EmitSignal(NClickableControl.SignalName.Released, toggle);
        elapsed.Restart();while(layer.GetChildCount()>0&&elapsed.Elapsed<TimeSpan.FromSeconds(5))await Tick();
        Check(layer.GetChildCount()==0,"原生消耗尾部自然结束并释放全部节点");
        Check(!player.Deck.Cards.OfType<FrierenMod.Scripts.Cards.FactorCard>().Any(), "表现卡不进入普通牌组");
        deck = MagicDeckStore.Read(player); deck.Equip(deck.Items.Last().InstanceId, ManaSlot.Hand);
        MagicDeckStore.Write(player, deck);
        Check(layer.GetChildCount() == 0, "槽位调整不会误播获得动画");
        var selector = new TestCardSelector(); using var selected = CombatTestState.UseSelector(selector);
        selector.PrepareToSelect([0]);
        Check(await MagicDeckCmd.ChooseReward(new BlockingPlayerChoiceContext(), player,
            ["AdvancedDefensiveMagic", "BuildUpBody", "HighSpeedMovement"]), "奖励选择成功");
        Check(layer.GetChildren().OfType<NCard>().Count() == 1, "奖励获得自动飞入魔法牌组");
        elapsed.Restart();
        while (!layer.GetChildren().OfType<NCardFlyVfx>().Any() && elapsed.Elapsed < TimeSpan.FromSeconds(3)) await Tick();
        Check(layer.GetChildren().OfType<NCardFlyVfx>().Any() && layer.GetChildren().OfType<NCardTrailVfx>().Any(),
            "魔法书外的奖励仍运行原版飞入和粒子拖尾");
        selector.PrepareToSelect([1, 6]);
        Check(await MagicPropCmd.VisitShop(player), "魔具店八选二成功");
        await MagicDeckAnimations.WhenIdle(player);
        for (int i=0;i<8;i++) await Tick();
        Check(layer.GetChildCount() == 0 && MagicDeckStore.Read(player).Props.Count == 2, "连续奖励和商店动画排队且不丢物品");
        run.ActFloor=9;
        var commissions=CommissionCmd.State(player);int questId=CommissionState.QuestId(0,1);
        commissions.Quests[0]=new(){Id=questId,Act=0,Template="HUNT",Stars=3,Goal=CommissionGoal.EmpoweredElite,Reward=new(CommissionRewardKind.Prop,"RoyalGrimoire",1)};
        commissions.Accept(questId,0,9);commissions.Active!.Progress=1;CommissionCmd.Write(player,commissions);
        var commissionButton=(BaseLib.Config.UI.NConfigButton)ui.FindChild("FrierenCommissionButton",true,false);
        commissionButton.EmitSignal(NButton.SignalName.Released,commissionButton);await Tick();
        var board=ui.GetNode<Control>("FrierenCommissionBoard");
        Check(board.IsVisibleInTree()&&layer.ZIndex>board.ZIndex&&!layer.ZAsRelative,"委托窗口打开时，领奖动画使用独立前景层");
        int commissionProps=MagicDeckStore.Read(player).Props.Count;int commissionGold=player.Gold;
        var claim=(BaseLib.Config.UI.NConfigButton)board.GetNode<Control>("CommissionCanvas/CommissionScroll/Quests/Quest"+questId).FindChild("Claim",true,false);
        // 本场景没有原生房间动作循环，直接执行领奖后核对表现层队列。
        Check(await CommissionCmd.Execute(player, questId, CommissionOperation.Claim, commissions.Revision), "委托领奖成功");
        Task claimAnimation = MagicDeckAnimations.WhenIdle(player);
        Check(!claimAnimation.IsCompleted && MagicDeckStore.Read(player).Props.Count == commissionProps + 1,
            "委托领奖先入库，再等待本机动画");
        Check(!await CommissionCmd.Execute(player, questId, CommissionOperation.Claim, CommissionCmd.State(player).Revision),
            "委托已领取时拒绝重复提交");
        elapsed.Restart();bool commissionFlight=false;
        while(!claimAnimation.IsCompleted && elapsed.Elapsed < TimeSpan.FromSeconds(5)) {
            await Tick();
            if(!commissionFlight && layer.GetChildren().OfType<NCardFlyVfx>().Any()) {
                commissionFlight=true;
                Check(board.IsVisibleInTree() && layer.GetChildren().OfType<NCardTrailVfx>().Any(),
                    "委托魔具在窗口前方使用原生飞入和粒子拖尾");
                await Snapshot("commission-reward-flight");
            }
        }
        Check(commissionFlight && claimAnimation.IsCompleted && layer.GetChildCount() == 0 &&
            player.Gold == commissionGold + commissions.Quests.Single(q=>q.Id==questId).Reward.BonusGold,
            "委托飞入完成后回收节点，物品与额外金币各发一次");
        commissionButton.EmitSignal(NButton.SignalName.Released,commissionButton);await Tick();
        deck = MagicDeckStore.Read(player); deck.Add("BuildUpBody"); MagicDeckStore.Write(player, deck);
        Task pending = MagicDeckAnimations.WhenIdle(player);
        elapsed.Restart();
        while (!layer.GetChildren().OfType<NCardFlyVfx>().Any() && elapsed.Elapsed < TimeSpan.FromSeconds(3)) await Tick();
        Check(layer.GetChildren().OfType<NCardFlyVfx>().Any(), "退出检查发生于原生飞入特效运行期间");
        canvas.QueueFree();
        for (int i = 0; i < 8; i++) await Tick();
        Check(pending.IsCompleted && !MagicDeckAnimations.IsAttached(player), "退出界面时终止动画并解除绑定，不悬挂异步流程");
        visualFactory.UnpatchAll(visualFactory.Id);
        return count;
    }
}
