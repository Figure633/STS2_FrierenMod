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
using HarmonyLib;

public static class MagicDeckAnimationTests
{
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
        await Snapshot("magic-prop-green-pulse");
        bool seenFlight = false;
        Vector2? flightStart = null;
        var elapsed = System.Diagnostics.Stopwatch.StartNew();
        while (elapsed.Elapsed < TimeSpan.FromSeconds(8) && !use.IsCompleted) {
            await Tick();
            var flying = layer.GetChildren().OfType<NCard>().Where(c => c.Name.ToString().StartsWith("AcquiredMagicItem")).ToArray();
            if (flying.Length == 2 && !seenFlight) {
                seenFlight = true; flightStart = flying[0].GlobalPosition;
                Check(layer.GetNodeOrNull<NCard>("UsedMagicProp") == null, "绿光及消耗完成后再展示两个新因子");
                await Snapshot("magic-factors-obtained");
            }
            if (seenFlight && flying.Length == 2 && flying[0].GlobalPosition.DistanceTo(flightStart!.Value) > 20) {
                Check(layer.GetChildren().OfType<NCardFlyVfx>().Any() &&
                    layer.GetChildren().OfType<NCardTrailVfx>().Any() && flying[0].Body.Scale.X < 1,
                    "原版飞入节点与粒子拖尾运行，卡面按原版规则缩小");
                await Snapshot("magic-native-flight");
                break;
            }
        }
        Check(seenFlight, "盲盒产生两个飞入节点：" + string.Join(",", layer.GetChildren().Select(c => c.Name.ToString())) + " completed=" + use.IsCompleted);
        await use;
        for (int i=0;i<8;i++) await Tick();
        Check(layer.GetChildCount() == 0 && !MagicPropCmd.IsBusy(player), "动画结束释放卡牌节点和忙碌状态");
        await RightClick(circle);
        circleHolder = (NGridCardHolder)ui.FindChild("Prop_" + circle, true, false);
        Check(circleHolder.CardNode!.CardHighlight.Modulate.A > 0 &&
            circleHolder.CardNode.MouseBehaviorRecursive == Control.MouseBehaviorRecursiveEnum.Inherited,
            "动画结束并复用卡牌后，实际右键仍可选中另一魔具");
        await RightClick(circle);
        toggle.EmitSignal(NClickableControl.SignalName.Released, toggle);
        Check(!player.Deck.Cards.OfType<FrierenMod.Scripts.Cards.FactorCard>().Any(), "表现卡不进入普通牌组");
        deck = MagicDeckStore.Read(player); deck.Equip(deck.Items.Last().InstanceId, ManaSlot.Hand);
        MagicDeckStore.Write(player, deck);
        Check(layer.GetChildCount() == 0, "槽位调整不会误播获得动画");
        var selector = new TestCardSelector(); using var selected = CardSelectCmd.UseSelector(selector);
        selector.PrepareToSelect([0]);
        Check(await MagicDeckCmd.ChooseReward(new BlockingPlayerChoiceContext(), player,
            ["AdvancedDefensiveMagic", "BuildUpBody", "HighSpeedMovement"]), "奖励选择成功");
        Check(layer.GetChildren().OfType<NCard>().Count() == 1, "奖励获得自动飞入魔法牌组");
        selector.PrepareToSelect([1, 6]);
        Check(await MagicPropCmd.VisitShop(player), "魔具店八选二成功");
        await MagicDeckAnimations.WhenIdle(player);
        for (int i=0;i<8;i++) await Tick();
        Check(layer.GetChildCount() == 0 && MagicDeckStore.Read(player).Props.Count == 3, "连续奖励和商店动画排队且不丢物品");
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
