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
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
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
            MagicDeckState.CreatePrototypeLoadout().Items.ToArray(), true, 3)));
        int crystal = deck.AddProp("CrystalBall").InstanceId;
        int box = deck.AddProp("MysteryBox").InstanceId;
        int horn = deck.AddProp("ShadowDragonHorn").InstanceId;
        int spare = deck.AddProp("CrystalBall").InstanceId;
        for (int i=0;i<3;i++) deck.Add("BuildUpBody");
        MagicDeckStore.Write(player, deck);
        var patch = new Harmony("frieren.tests.reward_prop_stack");
        foreach (string method in new[] { nameof(NGame._EnterTree), nameof(NGame._Ready) })
            patch.Patch(AccessTools.Method(typeof(NGame), method), prefix: new HarmonyMethod(typeof(RewardMagicPropTests), nameof(SkipStartup)));
        patch.Patch(AccessTools.Method(typeof(Reward), nameof(Reward.CreateIcon)),
            prefix: new HarmonyMethod(typeof(RewardMagicPropTests), nameof(CreateIcon)));
        var game = new NGame { ProcessMode=Node.ProcessModeEnum.Disabled };
        var input = new NInputManager(); var controller = new NControllerManager();
        AccessTools.Property(typeof(NInputManager), nameof(NInputManager.ControllerManager)).SetValue(input, controller);
        AccessTools.Property(typeof(NGame), nameof(NGame.InputManager)).SetValue(game, input);
        root.AddChild(game); AccessTools.Property(typeof(NGame), nameof(NGame.Instance)).SetValue(null, game);
        var host = new Control { Size = root.GetViewport().GetVisibleRect().Size }; root.AddChild(host);
        _stack = new NOverlayStack();
        var overlays = (List<IOverlayScreen>)AccessTools.Field(typeof(NOverlayStack), "_overlays").GetValue(_stack)!;
        patch.Patch(AccessTools.PropertyGetter(typeof(NOverlayStack), nameof(NOverlayStack.Instance)),
            prefix: new HarmonyMethod(typeof(RewardMagicPropTests), nameof(GetStack)));
        var reward = new GoldReward(15, player);
        var rewards = new RewardsSet(player); rewards.Rewards.Add(reward);
        var native = GD.Load<PackedScene>(NRewardsScreen.AssetPaths.Single()).Instantiate<NRewardsScreen>();
        AccessTools.Field(typeof(NRewardsScreen), "_runState").SetValue(native, run);
        AccessTools.Field(typeof(NRewardsScreen), "_rewardsSet").SetValue(native, rewards);
        host.AddChild(native); overlays.Add(native); native.AfterOverlayShown();
        var screen = new MagicBookScreen(host, player);
        var buttons = native.GetNode<Control>("%RewardsContainer").GetChildren().ToArray();
        await Tick();
        Check(MagicPropCmd.CanUse(player, crystal) && MagicPropCmd.CanUse(player, box), "原生战利品列表允许使用非战斗限定魔具");
        Check(!MagicPropCmd.CanUse(player, horn), "战斗限定魔具在战利品界面仍不可用");
        screen.Open(); await Tick();
        NGridCardHolder Holder(string name) => (NGridCardHolder)screen.View.FindChild(name, true, false);
        async Task Click(NGridCardHolder holder, MouseButton button = MouseButton.Left) {
            var point = holder.Hitbox.GetGlobalTransformWithCanvas() * (holder.Hitbox.Size/2);
            root.GetViewport().PushInput(new InputEventMouseButton { Position=point, GlobalPosition=point, ButtonIndex=button, Pressed=true }, true);
            await Tick();
            root.GetViewport().PushInput(new InputEventMouseButton { Position=point, GlobalPosition=point, ButtonIndex=button, Pressed=false }, true);
            await Tick();
        }
        var tool = Holder("Prop_" + crystal);
        await Click(tool, MouseButton.Right);
        Check(tool.CardNode!.CardHighlight.Modulate.A > 0, "战利品上打开魔法书后，实际右键可启用水晶球");
        await Click(Holder("Factor_" + deck.Items[0].InstanceId));
        await Click(Holder("Factor_" + deck.Items[1].InstanceId));
        var changed = MagicDeckStore.Read(player);
        Check(changed.Items[0].Slot==1 && changed.Items[1].Slot==0 && changed.Props.All(p=>p.InstanceId!=crystal),
            "选择目标正常交换槽位并只消耗一次魔具");
        await Click(Holder("Prop_" + box), MouseButton.Right);
        Check(MagicDeckStore.Read(player).Items.Count==8 && MagicDeckStore.Read(player).Props.All(p=>p.InstanceId!=box),
            "战利品界面也可直接右键使用盲盒");
        Check(!reward.SuccessfullySelected && native.GetNode<Control>("%RewardsContainer").GetChildren().SequenceEqual(buttons) && overlays.Single()==native,
            "魔具使用保持奖励对象、奖励按钮及原生栈原样");
        screen.Close(); await Tick();
        Check(native.GetNode<Control>("Rewards").Modulate.A > 0 && !native.IsComplete && _stack.Peek()==native,
            "关闭魔法书恢复同一奖励列表");
        int gold = player.Gold;
        Check(await reward.SelectUnsynchronized() && player.Gold==gold+15, "返回后原有金币奖励仍能正常领取");
        Check(MagicPropCmd.CanUse(player, spare), "奖励列表恢复后，其他非战斗限定魔具仍可用");
        var selection = new NCardRewardSelectionScreen(); overlays.Add(selection);
        Check(!MagicPropCmd.CanUse(player, spare) && !MagicPropCmd.AllowsUseOver(selection), "具体选牌界面继续保护选择流程");
        overlays.Remove(selection); selection.Free();
        AccessTools.Property(typeof(NRewardsScreen), nameof(NRewardsScreen.IsComplete)).SetValue(native, true);
        Check(!MagicPropCmd.CanUse(player, spare) && !MagicPropCmd.AllowsUseOver(native), "已完成的奖励列表不允许继续操作");
        overlays.Clear(); _stack.Free(); _stack=null;
        host.QueueFree(); await Tick();
        AccessTools.Property(typeof(NGame), nameof(NGame.Instance)).SetValue(null, null);
        controller.Free(); input.Free(); game.QueueFree(); await Tick(); patch.UnpatchAll(patch.Id);
        return count;
    }
}
