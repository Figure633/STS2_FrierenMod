using System.Text.Json;
using FrierenMod.Core;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Relics;
using FrierenMod.Scripts.UI;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Unlocks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;

public static class StartingSetupTests
{
    private static NMapScreen? _map;
    private static int _mapCloses, _mapOpens;
    private static bool GetMap(ref NMapScreen? __result){__result=_map;return false;}
    private static bool CloseMap(NMapScreen __instance){_mapCloses++;AccessTools.Property(typeof(NMapScreen),nameof(NMapScreen.IsOpen)).SetValue(__instance,false);return false;}
    private static bool OpenMap(NMapScreen __instance,ref NMapScreen __result){_mapOpens++;AccessTools.Property(typeof(NMapScreen),nameof(NMapScreen.IsOpen)).SetValue(__instance,true);__result=__instance;return false;}
    public static async Task<int> Run(Node root)
    {
        int count=0;
        void Check(bool ok,string name){if(!ok)throw new Exception(name);GD.Print("PASS 开局恢复："+name);count++;}
        async Task Tick(){for(int i=0;i<8;i++)await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);}
        void Start(Player p) {
            var run=RunState.CreateForTest([p],seed:"FRIEREN-STARTING-SETUP");
            RunManager.Instance.SetUpTest(run,new NetSingleplayerGameService());LocalContext.NetId=1;
        }
        Player Copy(Player p) => Player.FromSerializable(JsonSerializer.Deserialize<SerializablePlayer>(
            JsonSerializer.Serialize(p.ToSerializable(),JsonSerializationUtility.Options),JsonSerializationUtility.Options)!);
        var context=new BlockingPlayerChoiceContext();
        var selector=new TestCardSelector();using var choices=CardSelectCmd.UseSelector(selector);
        SaveManager.Instance.InitPrefsDataForTest();

        for(int slot=0;slot<3;slot++) {
            var player=Player.CreateForNewRun<Frieren>(UnlockState.all,1);Start(player);
            for(int i=0;i<slot;i++)selector.PrepareToSelect(new[]{i});
            var pending=selector.SetupForAsyncIndexSelection();
            Task old=MagicDeckCmd.ChooseStartingFactors(context,player);
            var before=MagicDeckStore.Read(player);
            Check(!old.IsCompleted && before.DraftSlot==slot && before.Draft.Length==3,"第"+slot+"槽选择等待时保存完整选项");
            var restored=Copy(player);
            Check(MagicDeckStore.Read(restored).ToJson()==before.ToJson(),"真实玩家序列化保留第"+slot+"槽库存和选项");
            RunManager.Instance.CleanUp();await Tick();
            Check(old.IsCompletedSuccessfully,"退出跑局取消第"+slot+"槽旧等待，释放选择锁");
            selector.Cleanup();Start(restored);
            pending.TrySetResult(new[]{0});await Tick();
            Check(MagicDeckStore.Read(restored).ToJson()==before.ToJson(),"过期回调不会写入读档后的玩家");
            for(int i=slot;i<3;i++)selector.PrepareToSelect(new[]{0});
            var host=new Control{Size=new(1920,1080)};root.AddChild(host);
            MagicBookUi.Attach(host,restored,()=>null,()=>false);
            var timer=host.GetNode<Godot.Timer>("FrierenManaRefresh");
            RunManager.Instance.NetService.SetGameLoading(true);
            await restored.GetRelic<HolyEmblem>()!.AfterActEntered();
            await restored.GetRelic<HolyEmblem>()!.AfterPlayerTurnStart(context,restored);
            timer.EmitSignal(Godot.Timer.SignalName.Timeout);
            Check(MagicDeckStore.Read(restored).DraftSlot==slot,"加载期间章节、首回合和界面兜底均不阻塞转场");
            RunManager.Instance.NetService.SetGameLoading(false);
            timer.EmitSignal(Godot.Timer.SignalName.Timeout);await Tick();
            var completed=MagicDeckStore.Read(restored);
            Check(completed.SetupCompleted && completed.Items.Count==3 && completed.Items.Single(i=>i.Slot==slot).SpellId==before.Draft[0],
                "界面恢复后从原第"+slot+"槽继续，不重掷选项或重复因子");
            Check(completed.StartingPropsGranted && completed.Props.Count==2 && completed.Props.All(p=>p.PropId=="UnbelievableTool"),
                "三槽完成后只送两个妙妙工具");
            string once=completed.ToJson();
            await MagicDeckCmd.ChooseStartingFactors(context,restored);
            await restored.GetRelic<HolyEmblem>()!.AfterActEntered();
            timer.EmitSignal(Godot.Timer.SignalName.Timeout);
            Check(MagicDeckStore.Read(restored).ToJson()==once,"重复调用、章节和界面刷新不重复赠送");
            foreach(var prop in completed.Props.ToArray())completed.Remove(prop.InstanceId);
            MagicDeckStore.Write(restored,completed);
            var afterUse=Copy(restored);
            host.QueueFree();await Tick();RunManager.Instance.CleanUp();await Tick();Start(afterUse);
            MagicDeckCmd.TryResumeStartingFactors(afterUse);
            Check(MagicDeckStore.Read(afterUse).StartingPropsGranted && MagicDeckStore.Read(afterUse).Props.Count==0,
                "消耗赠品并读档后不会补发");
            RunManager.Instance.CleanUp();await Tick();selector.Cleanup();
        }

        var legacyPlayer=Player.CreateForNewRun<Frieren>(UnlockState.all,1);Start(legacyPlayer);
        var legacy=MagicDeckState.FromJson(JsonSerializer.Serialize(new MagicDeckSave(3,
            MagicDeckState.CreatePrototypeLoadout().Items.ToArray(),true,3)));
        legacy.AddProp("MysteryBox");MagicDeckStore.Write(legacyPlayer,legacy);
        await RelicCmd.Remove(legacyPlayer.GetRelic<HolyEmblem>()!);
        await MagicDeckCmd.ChooseStartingFactors(context,legacyPlayer);
        var migrated=MagicDeckStore.Read(legacyPlayer);
        Check(migrated.Items.Count==3 && migrated.Props.Count==3 && migrated.Props.Count(p=>p.PropId=="UnbelievableTool")==2,
            "没有圣徽的旧档也能一次性补赠，保留原有因子和魔具");
        string migratedJson=migrated.ToJson();
        await MagicDeckCmd.ChooseStartingFactors(context,legacyPlayer);
        Check(MagicDeckStore.Read(legacyPlayer).ToJson()==migratedJson,"旧档补赠标记持久化");
        RunManager.Instance.CleanUp();await Tick();selector.Cleanup();

        var live=Player.CreateForNewRun<Frieren>(UnlockState.all,1);Start(live);
        var mapPatch=new Harmony("frieren.tests.initial_map");
        mapPatch.Patch(AccessTools.PropertyGetter(typeof(NMapScreen),nameof(NMapScreen.Instance)),prefix:new HarmonyMethod(typeof(StartingSetupTests),nameof(GetMap)));
        mapPatch.Patch(AccessTools.Method(typeof(NMapScreen),nameof(NMapScreen.Close)),prefix:new HarmonyMethod(typeof(StartingSetupTests),nameof(CloseMap)));
        mapPatch.Patch(AccessTools.Method(typeof(NMapScreen),nameof(NMapScreen.Open)),prefix:new HarmonyMethod(typeof(StartingSetupTests),nameof(OpenMap)));
        _map=new NMapScreen();AccessTools.Property(typeof(NMapScreen),nameof(NMapScreen.IsOpen)).SetValue(_map,true);
        var first=selector.SetupForAsyncIndexSelection();
        selector.PrepareToSelect(new[]{0});selector.PrepareToSelect(new[]{0});
        var selecting=MagicDeckCmd.ChooseStartingFactors(context,live);
        Check(!_map.IsOpen && _mapCloses==1,"使用原生地图关闭接口，让初始选牌获得输入焦点");
        var inventory=MagicDeckStore.Read(live);inventory.AddProp("MysteryBox");MagicDeckStore.Write(live,inventory);
        first.SetResult(new[]{0});await selecting;
        Check(MagicDeckStore.Read(live).Props.Count==3 && MagicDeckStore.Read(live).Props.Any(p=>p.PropId=="MysteryBox"),
            "等待选牌期间新增的库存不会被旧快照覆盖");
        Check(_map.IsOpen && _mapOpens==1,"初始选择完成后经原生接口恢复地图");
        _map.Free();_map=null;mapPatch.UnpatchAll(mapPatch.Id);
        RunManager.Instance.CleanUp();await Tick();selector.Cleanup();

        var fallback=Player.CreateForNewRun<Frieren>(UnlockState.all,1);Start(fallback);
        var combat=new CombatState(runState:fallback.RunState);combat.AddPlayer(fallback);
        combat.AddCreature(combat.CreateCreature(ModelDb.Monster<MegaCrit.Sts2.Core.Models.Monsters.Axebot>().ToMutable(),CombatSide.Enemy,null));
        CombatManager.Instance.SetUpCombat(combat);
        AccessTools.Property(typeof(CombatManager),nameof(CombatManager.IsInProgress)).SetValue(CombatManager.Instance,true);
        RunManager.Instance.NetService.SetGameLoading(true);
        await fallback.GetRelic<HolyEmblem>()!.AfterPlayerTurnStart(context,fallback);
        Check(MagicDeckStore.Read(fallback).Draft.Length==0 && ManaCmd.InPile(fallback,PileType.Draw).Count()==1 &&
            ManaCmd.InPile(fallback,PileType.Discard).Count()==1,"加载中的真实首回合不等待选牌，圣徽仍正常生成魔力");
        RunManager.Instance.NetService.SetGameLoading(false);
        MagicDeckCmd.TryResumeStartingFactors(fallback);
        Check(MagicDeckStore.Read(fallback).Draft.Length==0,"战斗初始化未结束时不抢先弹选牌");
        AccessTools.Property(typeof(CombatManager),nameof(CombatManager.IsStarting)).SetValue(CombatManager.Instance,false);
        AccessTools.Property(typeof(CombatManager),nameof(CombatManager.PlayerActionsDisabled)).SetValue(CombatManager.Instance,false);
        var originalGame=NGame.Instance;
        var game=new NGame();var transition=new NTransition();
        AccessTools.Property(typeof(NGame),nameof(NGame.Transition)).SetValue(game,transition);
        AccessTools.Property(typeof(NGame),nameof(NGame.Instance)).SetValue(null,game);
        AccessTools.Property(typeof(NTransition),nameof(NTransition.InTransition)).SetValue(transition,true);
        MagicDeckCmd.TryResumeStartingFactors(fallback);
        Check(MagicDeckStore.Read(fallback).Draft.Length==0,"原生淡入遮罩未结束时不弹出初始选牌");
        AccessTools.Property(typeof(NGame),nameof(NGame.Instance)).SetValue(null,originalGame);
        transition.Free();game.Free();
        for(int i=0;i<3;i++)selector.PrepareToSelect(new[]{0});
        MagicDeckCmd.TryResumeStartingFactors(fallback);await Tick();
        Check(MagicDeckStore.Read(fallback).SetupCompleted && MagicDeckStore.Read(fallback).Props.Count==2,
            "首回合兜底在初始化结束后完成选牌和一次性赠品");
        return count;
    }
}
