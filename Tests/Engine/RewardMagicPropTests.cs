using System.Text.Json;
using FrierenMod.Core;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.UI;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Unlocks;

public static class RewardMagicPropTests
{
    private static NOverlayStack? _stack;
    private static bool GetStack(ref NOverlayStack? __result) { __result = _stack; return false; }
    private static bool SkipStartup() => false;
    private static bool CreateIcon(Reward __instance, ref Control? __result)
    {
        string path = (string)AccessTools.Property(typeof(Reward), "IconPath").GetValue(__instance)!;
        __result = new TextureRect { Texture=GD.Load<Texture2D>(path), ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize };
        return false;
    }

    public static async Task<int> Run(Node root)
    {
        int count = 0;
        void Check(bool ok, string name) { if (!ok) throw new Exception(name); GD.Print("PASS 奖励魔具：" + name); count++; }
        async Task Tick() { for (int i=0;i<8;i++) await root.ToSignal(root.GetTree(), SceneTree.SignalName.ProcessFrame); }
        var player = Player.CreateForNewRun<Frieren>(UnlockState.all, 1);
        var run = RunState.CreateForTest([player], seed: "FRIEREN-REWARD-PROPS");
        RunManager.Instance.SetUpTest(run, new NetSingleplayerGameService()); LocalContext.NetId = 1;
        run.PushRoom(new RestSiteRoom());
        var deck = MagicDeckState.FromJson(JsonSerializer.Serialize(new MagicDeckSave(3,
            MagicDeckState.CreatePrototypeLoadout().Items.ToArray(), true, 3, StartingPropsGranted: true)));
        int crystal = deck.AddProp("CrystalBall").InstanceId;
        int box = deck.AddProp("MysteryBox").InstanceId;
        int horn = deck.AddProp("ShadowDragonHorn").InstanceId;
        int spare = deck.AddProp("CrystalBall").InstanceId;
        int lastBox = deck.AddProp("MysteryBox").InstanceId;
        for (int i=0;i<3;i++) deck.Add("BuildUpBody");
        MagicDeckStore.Write(player, deck);
        var patch = new Harmony("frieren.tests.reward_prop_stack");
        foreach (string method in new[] { nameof(NGame._EnterTree), nameof(NGame._Ready) })
            patch.Patch(AccessTools.Method(typeof(NGame), method), prefix: new HarmonyMethod(typeof(RewardMagicPropTests), nameof(SkipStartup)));
        // 保留真实 GlobalUi 类型、层级与魔法书 Attach 输入策略；只跳过无关的全局宿主初始化。
        foreach (var type in new[] { typeof(NGlobalUi), typeof(NOverlayStack) })
            patch.Patch(AccessTools.Method(type, "_Ready"), prefix: new HarmonyMethod(typeof(RewardMagicPropTests), nameof(SkipStartup)));
        patch.Patch(AccessTools.Method(typeof(Reward), nameof(Reward.CreateIcon)),
            prefix: new HarmonyMethod(typeof(RewardMagicPropTests), nameof(CreateIcon)));
        var game = new NGame { ProcessMode=Node.ProcessModeEnum.Disabled };
        var sceneContainer = new NSceneContainer(); game.AddChild(sceneContainer);
        AccessTools.Property(typeof(NGame), nameof(NGame.RootSceneContainer)).SetValue(game, sceneContainer);
        var input = new NInputManager(); var controller = new NControllerManager();
        AccessTools.Property(typeof(NInputManager), nameof(NInputManager.ControllerManager)).SetValue(input, controller);
        AccessTools.Property(typeof(NGame), nameof(NGame.InputManager)).SetValue(game, input);
        root.AddChild(game); AccessTools.Property(typeof(NGame), nameof(NGame.Instance)).SetValue(null, game);
        var host = new NGlobalUi { Size = root.GetViewport().GetVisibleRect().Size };
        // 提供原生屏幕上下文读取的跑局与地图；不启动真实游戏或接触玩家文件。
        SaveManager.Instance.InitPrefsDataForTest();
        var map = new NMapScreen();
        var runNode = new NRun(); var roomContainer = new NSceneContainer();
        AccessTools.Field(typeof(NRun), "_roomContainer").SetValue(runNode, roomContainer);
        AccessTools.Property(typeof(NRun), nameof(NRun.GlobalUi)).SetValue(runNode, host);
        AccessTools.Property(typeof(NSceneContainer), nameof(NSceneContainer.CurrentScene)).SetValue(sceneContainer, runNode);
        AccessTools.Property(typeof(NGlobalUi), nameof(NGlobalUi.MapScreen)).SetValue(host, map);
        _stack = new NOverlayStack { Size=host.Size };
        var backstop = new Control { Name="OverlayBackstop", MouseFilter=Control.MouseFilterEnum.Ignore };
        _stack.AddChild(backstop); AccessTools.Field(typeof(NOverlayStack), "_backstop").SetValue(_stack, backstop);
        host.AddChild(_stack);
        var capstone = new NCapstoneContainer { Size=host.Size }; capstone.AddChild(new Control { Name="CapstoneBackstop" });
        host.AddChild(capstone);
        AccessTools.Property(typeof(NGlobalUi), nameof(NGlobalUi.CapstoneContainer)).SetValue(host, capstone);
        AccessTools.Property(typeof(NGlobalUi), nameof(NGlobalUi.Overlays)).SetValue(host, _stack);
        root.AddChild(host);
        var overlays = (List<IOverlayScreen>)AccessTools.Field(typeof(NOverlayStack), "_overlays").GetValue(_stack)!;
        patch.Patch(AccessTools.PropertyGetter(typeof(NOverlayStack), nameof(NOverlayStack.Instance)),
            prefix: new HarmonyMethod(typeof(RewardMagicPropTests), nameof(GetStack)));
        var reward = new GoldReward(15, player);
        var rewards = new RewardsSet(player); rewards.Rewards.Add(reward);
        var rewardsFinished = RunManager.Instance.RewardsSetSynchronizer.BeginRewardsSet(rewards);
        var native = GD.Load<PackedScene>(NRewardsScreen.AssetPaths.Single()).Instantiate<NRewardsScreen>();
        AccessTools.Field(typeof(NRewardsScreen), "_runState").SetValue(native, run);
        AccessTools.Field(typeof(NRewardsScreen), "_rewardsSet").SetValue(native, rewards);
        AccessTools.Field(typeof(NRewardsScreen), "_isTerminal").SetValue(native, true);
        _stack.Push(native);
        MagicBookUi.Attach(host, player);
        Check(MagicDeckAnimations.IsAttached(player), "真实 GlobalUi 完成层级调整后仍保留动画监听");
        var toggle = (MagicFactorButton)host.FindChild("FrierenMagicBookButton", true, false);
        var book = capstone.GetNode<MagicBookView>("FrierenMagicBook");
        var animationLayer = capstone.GetNode<Control>("FrierenMagicDeckAnimations");
        async Task ObserveBoxAnimation(string context) {
            var pending = MagicDeckAnimations.WhenIdle(player);
            Check(!pending.IsCompleted && animationLayer.GetNodeOrNull<NCard>("UsedMagicProp") is { } used &&
                used.IsVisibleInTree() && used.CardHighlight.Modulate.G > 0.8f,
                context + "：真实动画层显示魔具绿光");
            NCard[] acquired = [];
            var elapsed = System.Diagnostics.Stopwatch.StartNew();
            while (!pending.IsCompleted && elapsed.Elapsed < TimeSpan.FromSeconds(6)) {
                await root.ToSignal(root.GetTree(), SceneTree.SignalName.ProcessFrame);
                acquired = animationLayer.GetChildren().OfType<NCard>()
                    .Where(c => c.Name.ToString().StartsWith("AcquiredMagicItem_")).ToArray();
                if (acquired.Length == 2) break;
            }
            Check(acquired.Length == 2 && acquired.All(c => c.IsVisibleInTree() &&
                c.GetGlobalTransform().Scale.X > 0.5f && c.Modulate.A > 0.9f &&
                root.GetViewport().GetVisibleRect().HasPoint(c.GlobalPosition)), context + "：两个新因子在可见区域完整展示");
            if (OS.GetCmdlineUserArgs().Contains("--ui-snapshot")) {
                await Tick();
                root.GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath(
                    "res://../../artifacts/reward-props-acquired.png"));
            }
            Vector2 start = acquired[0].GlobalPosition;
            bool moved = false;
            while (!pending.IsCompleted && elapsed.Elapsed < TimeSpan.FromSeconds(6)) {
                await root.ToSignal(root.GetTree(), SceneTree.SignalName.ProcessFrame);
                if (animationLayer.GetChildren().OfType<NCardFlyVfx>().Any() &&
                    GodotObject.IsInstanceValid(acquired[0]) && acquired[0].GlobalPosition.DistanceTo(start) > 20) {
                    moved = true; break;
                }
            }
            Check(moved, context + "：原生飞入特效实际移动新因子");
            await pending; await Tick();
            Check(animationLayer.GetChildCount()==0 && MagicDeckAnimations.IsAttached(player),
                context + "：动画收尾后仍保持监听");
        }
        void ToggleBook() => toggle.EmitSignal(NClickableControl.SignalName.Released, toggle);
        var buttons = native.GetNode<Control>("%RewardsContainer").GetChildren().ToArray();
        await Tick();
        Check(MagicPropCmd.CanUse(player, crystal) && MagicPropCmd.CanUse(player, box), "原生战利品列表允许使用非战斗限定魔具");
        Check(!MagicPropCmd.CanUse(player, horn), "战斗限定魔具在战利品界面仍不可用");
        ToggleBook(); await Tick();
        Check(book.CanProcess() && book.IsVisibleInTree(), "真实 GlobalUi 的奖励栈与魔法书输入策略允许操作");
        Check(ActiveScreenContext.Instance.IsCurrent(book) && capstone.CurrentCapstoneScreen==book,
            "魔法书通过原生上层容器接管活动屏幕，而非仅覆盖奖励画面");
        NGridCardHolder Holder(string name) => (NGridCardHolder)book.FindChild(name, true, false);
        async Task Click(NGridCardHolder holder, MouseButton button = MouseButton.Left) {
            var scroll = book.GetNode<ScrollContainer>("BookCanvas/InventoryScroll");
            if (scroll.IsAncestorOf(holder)) { scroll.EnsureControlVisible(holder.GetParent().GetParent<Control>()); await Tick(); }
            var point = holder.Hitbox.GetGlobalTransformWithCanvas() * (holder.Hitbox.Size/2);
            root.GetViewport().PushInput(new InputEventMouseButton { Position=point, GlobalPosition=point, ButtonIndex=button, Pressed=true }, true);
            await Tick();
            root.GetViewport().PushInput(new InputEventMouseButton { Position=point, GlobalPosition=point, ButtonIndex=button, Pressed=false }, true);
            await Tick();
        }
        var tool = Holder("Prop_" + crystal);
        await Click(tool, MouseButton.Right);
        Check(tool.CardNode!.CardHighlight.Modulate.A > 0, "战利品上打开魔法书后，实际右键可启用水晶球");
        if (OS.GetCmdlineUserArgs().Contains("--ui-snapshot"))
            root.GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../artifacts/reward-props-capstone.png"));
        await Click(Holder("Factor_" + deck.Items[0].InstanceId));
        await Click(Holder("Factor_" + deck.Items[1].InstanceId));
        await MagicDeckAnimations.WhenIdle(player); await Tick();
        var changed = MagicDeckStore.Read(player);
        Check(changed.Items[0].Slot==1 && changed.Items[1].Slot==0 && changed.Props.All(p=>p.InstanceId!=crystal),
            "选择目标正常交换槽位并只消耗一次魔具");
        await Click(Holder("Prop_" + box), MouseButton.Right);
        await ObserveBoxAnimation("战利品列表");
        Check(MagicDeckStore.Read(player).Items.Count==8 && MagicDeckStore.Read(player).Props.All(p=>p.InstanceId!=box),
            "战利品界面也可直接右键使用盲盒");
        Check(!reward.SuccessfullySelected && native.GetNode<Control>("%RewardsContainer").GetChildren().SequenceEqual(buttons) && overlays.Single()==native,
            "魔具使用保持奖励对象、奖励按钮及原生栈原样");
        ToggleBook(); await Tick();
        Check(native.GetNode<Control>("Rewards").Modulate.A > 0 && !native.IsComplete && _stack.Peek()==native,
            "关闭魔法书恢复同一奖励列表");
        Check(ActiveScreenContext.Instance.IsCurrent(native) && !capstone.InUse, "关闭魔法书经原生容器交还奖励界面输入");
        int gold = player.Gold;
        Check(await RunManager.Instance.RewardsSetSynchronizer.SelectLocalReward(reward) && player.Gold==gold+15,
            "返回后原有金币奖励仍能经原生同步器正常领取");
        await rewardsFinished;
        Check(MagicPropCmd.CanUse(player, spare), "奖励列表恢复后，其他非战斗限定魔具仍可用");
        var selection = new NCardRewardSelectionScreen(); overlays.Add(selection);
        Check(!MagicPropCmd.CanUse(player, spare) && !MagicPropCmd.AllowsUseOver(selection), "具体选牌界面继续保护选择流程");
        overlays.Remove(selection); selection.Free();
        native.RewardCollectedFrom((Control)buttons.Single()); await Tick();
        Check(native.IsComplete && overlays.Single()==native, "原生奖励领取完毕后仍保留等待前进的同一奖励屏");
        Check(MagicPropCmd.CanUse(player, spare) && MagicPropCmd.AllowsUseOver(native), "领完奖励等待前进时，仍允许使用魔具");
        ToggleBook(); await Tick();
        tool = Holder("Prop_" + spare);
        await Click(tool, MouseButton.Right);
        Check(tool.CardNode!.CardHighlight.Modulate.A > 0, "等待前进时实际右键仍能启用水晶球");
        await Click(Holder("Factor_" + deck.Items[0].InstanceId));
        await Click(Holder("Factor_" + deck.Items[1].InstanceId));
        await MagicDeckAnimations.WhenIdle(player); await Tick();
        changed = MagicDeckStore.Read(player);
        Check(changed.Items[0].Slot==0 && changed.Items[1].Slot==1 && changed.Props.All(p=>p.InstanceId!=spare),
            "等待前进时选满目标仍正常结算，魔具只扣一次");
        await Click(Holder("Prop_" + lastBox), MouseButton.Right);
        await ObserveBoxAnimation("奖励领完等待前进");
        Check(MagicDeckStore.Read(player).Items.Count==10 && MagicDeckStore.Read(player).Props.All(p=>p.InstanceId!=lastBox),
            "等待前进时无目标盲盒也能直接使用");
        ToggleBook(); await Tick();
        Check(native.IsComplete && overlays.Single()==native && native.GetNode<NProceedButton>("ProceedButton").IsEnabled && player.Gold==gold+15,
            "关闭魔法书保留前进按钮与奖励已领取状态，不重复领取奖励");
        // 对照当前作者存档的库存：三槽因子、一个未装载因子、两个神奇妙妙工具。
        deck = MagicDeckState.FromJson(JsonSerializer.Serialize(new MagicDeckSave(3,
            MagicDeckState.CreatePrototypeLoadout().Items.ToArray(), true, 3, StartingPropsGranted: true)));
        int inventoryFactor = deck.Add("BigFire").InstanceId;
        int swap = deck.AddProp("UnbelievableTool").InstanceId;
        deck.AddProp("UnbelievableTool");
        int mapBox = deck.AddProp("MysteryBox").InstanceId;
        MagicDeckStore.Write(player, deck); await MagicDeckAnimations.WhenIdle(player);
        AccessTools.Property(typeof(NMapScreen), nameof(NMapScreen.IsOpen)).SetValue(map, true);
        ActiveScreenContext.Instance.Update();
        Check(ActiveScreenContext.Instance.IsCurrent(map), "原生上下文识别当前地图为活动屏幕");
        ToggleBook(); await Tick();
        Check(ActiveScreenContext.Instance.IsCurrent(book) && book.CanProcess(), "地图上打开魔法书后由原生层切换活动屏幕并启用输入");
        await Click(Holder("Prop_" + swap), MouseButton.Right);
        Check(Holder("Prop_" + swap).CardNode!.CardHighlight.Modulate.A > 0, "地图上可右键启用神奇妙妙工具");
        await Click(Holder("Factor_" + deck.Items[0].InstanceId));
        await Click(Holder("Factor_" + inventoryFactor));
        await MagicDeckAnimations.WhenIdle(player); await Tick();
        changed=MagicDeckStore.Read(player);
        Check(changed.Items.Single(i=>i.InstanceId==inventoryFactor).Slot==0 && changed.Props.Count==2,
            "地图上选择槽位与库存因子后工具正确结算");
        await Click(Holder("Prop_" + mapBox), MouseButton.Right);
        await ObserveBoxAnimation("地图");
        ToggleBook(); await Tick();
        Check(ActiveScreenContext.Instance.IsCurrent(map) && map.IsOpen && !capstone.InUse,
            "关闭后回到原地图并保持奖励完成状态");
        var otherScreen = new NDeckViewScreen();
        AccessTools.Property(typeof(NCapstoneContainer), nameof(NCapstoneContainer.CurrentCapstoneScreen)).SetValue(capstone, otherScreen);
        ActiveScreenContext.Instance.Update();
        Check(!toggle.IsEnabled && !book.CanProcess(), "其他原生上层菜单打开时，魔法书仍禁止抢占输入");
        AccessTools.Property(typeof(NCapstoneContainer), nameof(NCapstoneContainer.CurrentCapstoneScreen)).SetValue(capstone, null);
        ActiveScreenContext.Instance.Update();
        Check(toggle.IsEnabled, "其他菜单退出后恢复魔法书按钮");
        otherScreen.Free();
        AccessTools.Property(typeof(NSceneContainer), nameof(NSceneContainer.CurrentScene)).SetValue(sceneContainer, null);
        overlays.Clear();
        host.QueueFree(); await Tick(); _stack=null;
        AccessTools.Property(typeof(NGame), nameof(NGame.Instance)).SetValue(null, null);
        map.Free(); roomContainer.Free(); runNode.Free();
        controller.Free(); input.Free(); game.QueueFree(); await Tick(); patch.UnpatchAll(patch.Id);
        return count;
    }
}
