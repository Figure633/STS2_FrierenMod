using System.Text.Json;
using FrierenMod.Core;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.RestSite;
using FrierenMod.Scripts.Powers;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport.ENet;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.Unlocks;

// 两个独立 Godot 进程 + 原生 ENet/消息总线/动作与选择同步器。
// 仅替代选牌屏的人工点击；CardSelectCmd、choiceId、传输与队列恢复均走原版。
public static class MultiplayerTests
{
    private sealed class LoadListener : ILoadRunLobbyListener
    {
        public void PlayerConnected(ulong id) { }
        public void RemotePlayerDisconnected(ulong id) { }
        public Task<bool> ShouldAllowRunToBegin()=>Task.FromResult(true);
        public void BeginRun() { }
        public void PlayerReadyChanged(ulong id) { }
        public void LocalPlayerDisconnected(NetErrorInfo info) { }
    }
    private static readonly List<Node> Screens = [];
    private static NOverlayStack Overlay = null!;
    private static bool CancelShop;
    private static bool GetOverlay(ref NOverlayStack __result) { __result=Overlay; return false; }
    private static bool SkipOverlay() => false;
    private static bool Choose(IReadOnlyList<CardModel> cards, ref NChooseACardSelectionScreen __result)
    {
        __result=new NChooseACardSelectionScreen(); Screens.Add(__result);
        ((TaskCompletionSource<IEnumerable<CardModel>>)AccessTools.Field(typeof(NChooseACardSelectionScreen),"_completionSource").GetValue(__result))
            .SetResult([cards[(int)(LocalContext.NetId!.Value-1)%cards.Count]]);
        return false;
    }
    private static bool Grid(IReadOnlyList<CardModel> cards, ref NSimpleCardSelectScreen __result)
    {
        __result=new NSimpleCardSelectScreen(); Screens.Add(__result);
        ((TaskCompletionSource<IEnumerable<CardModel>>)AccessTools.Field(typeof(NCardGridSelectionScreen),"_completionSource").GetValue(__result))
            .SetResult(CancelShop?[]:new[]{cards[1],cards[6]});
        return false;
    }

    public static async Task<int> Run(Node root, bool host)
    {
        int checks=0;
        void Check(bool ok,string name) { if(!ok)throw new Exception(name);checks++;GD.Print("PASS 联机："+name); }
        string dir=System.Environment.GetEnvironmentVariable("FRIEREN_MP_TEST_DIR")??throw new Exception("缺少测试目录");
        ushort port=ushort.Parse(System.Environment.GetEnvironmentVariable("FRIEREN_MP_TEST_PORT")!);
        string role=host?"host":"client",peer=host?"client":"host";
        bool mixed=OS.GetCmdlineUserArgs().Contains("--mp-mixed");
        void Publish(string name,string text) {
            string target=Path.Combine(dir,name),temporary=target+".tmp";
            File.WriteAllText(temporary,text);File.Move(temporary,target);
        }
        async Task Tick(){await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);}
        async Task Wait(Func<bool> ready,string reason) {
            ulong start=Time.GetTicksMsec();
            while(!ready()){if(Time.GetTicksMsec()-start>20000)throw new Exception("联机等待超时："+reason);await Tick();}
        }
        async Task Barrier(string name) {
            Publish(name+"-"+role+".ready","ready");
            await Wait(()=>File.Exists(Path.Combine(dir,name+"-"+peer+".ready")),name);
        }
        MessageTypes.Initialize();ActionTypes.Initialize();
        Check(ActionTypes.TryGetActionType(ActionTypes.TypeToId<NetMagicDeckAction>(),out var actionType)&&actionType==typeof(NetMagicDeckAction),"原生动作类型表发现模组类型");
        var p1=Player.CreateForNewRun<Frieren>(UnlockState.all,1);
        var p2=mixed?Player.CreateForNewRun<Ironclad>(UnlockState.all,2):Player.CreateForNewRun<Frieren>(UnlockState.all,2);
        var run=RunState.CreateForTest([p1,p2],seed:"FRIEREN_MULTIPLAYER");
        INetGameService service;
        var timer=new Godot.Timer{WaitTime=0.01,Autostart=true};
        if(host) {
            var s=new NetHostGameService();Check(s.StartENetHost(port,2)==null,"ENet主机启动");service=s;
            timer.Timeout+=s.Update;root.AddChild(timer);
            await Wait(()=>s.ConnectedPeers.Count==1,"等待客户端连接");s.SetPeerReadyForBroadcasting(2);
        }
        else {
            var s=new NetClientGameService();var transport=new ENetClient(s);s.Initialize(transport,PlatformType.None);service=s;
            Check(await transport.ConnectToHost(2,"127.0.0.1",port)==null,"ENet客户端连接");
            timer.Timeout+=s.Update;root.AddChild(timer);
        }
        await Barrier("connected");
        RunManager.Instance.SetUpTest(run,service);LocalContext.NetId=host?1ul:2ul;
        RunManager.Instance.GenerateRooms();
        // 不模拟 Steam 会话，不读写作者存档。选择屏替身只生成本地选择结果。
        Overlay=new NOverlayStack();
        var harmony=new Harmony("frieren.tests.multiplayer");
        harmony.Patch(AccessTools.PropertyGetter(typeof(NOverlayStack),nameof(NOverlayStack.Instance)),prefix:new HarmonyMethod(typeof(MultiplayerTests),nameof(GetOverlay)));
        foreach(var method in new[]{"Push","Remove"})harmony.Patch(AccessTools.Method(typeof(NOverlayStack),method),prefix:new HarmonyMethod(typeof(MultiplayerTests),nameof(SkipOverlay)));
        harmony.Patch(AccessTools.Method(typeof(NChooseACardSelectionScreen),"ShowScreen"),prefix:new HarmonyMethod(typeof(MultiplayerTests),nameof(Choose)));
        harmony.Patch(AccessTools.Method(typeof(NSimpleCardSelectScreen),"Create",[typeof(IReadOnlyList<CardModel>),typeof(CardSelectorPrefs)]),prefix:new HarmonyMethod(typeof(MultiplayerTests),nameof(Grid)));
        run.PushRoom(new RestSiteRoom());CombatTestState.SetInProgress(false);
        var me=host?p1:p2;var frierens=run.Players.Where(p=>p.Character is Frieren).ToArray();
        async Task Same(string name,Func<bool>? ready=null) {
            if(ready!=null)await Wait(ready,name);
            await Barrier(name+"-done");
            // 阶段完成后再比较两端全部玩家数据，而非只核对本机角色。
            string snapshot=JsonSerializer.Serialize(run.Players.Select(p=>new {p.NetId,deck=MagicDeckStore.Read(p).ToJson(),commissions=CommissionCmd.Read(p).ToJson(),gold=p.Gold,
                rng=CombatTestState.RngCounter(p.PlayerRng.Rewards),hp=p.Creature.CurrentHp,energy=p.PlayerCombatState?.Energy,
                piles=p.PlayerCombatState?.AllPiles.Select(pile=>new {pile.Type,cards=pile.Cards.Select(c=>c.Id.ToString()).ToArray()}).ToArray(),
                powers=p.Creature.Powers.Select(power=>new{power.Id,power.Amount,gate=power is OpenTheWaygatePower g?g.Opportunities+"/"+g.PendingTurns+"/"+g.Consecutive:null}).ToArray()}));
            Publish(name+"-"+role+".json",snapshot);
            string? remote=null;
            await Wait(()=>{
                try { remote=File.ReadAllText(Path.Combine(dir,name+"-"+peer+".json"));return true; }
                catch(IOException){return false;}
            },name+"快照发布");
            Check(remote==snapshot,name+"：两端所有玩家库存、RNG、生命和能量一致");
            await Barrier(name+"-checked");
        }
        await Barrier("run-ready");
        if(me.Character is Frieren)await MagicDeckCmd.ChooseStartingFactors(new BlockingPlayerChoiceContext(),me);
        await Same("initial",()=>frierens.All(p=>MagicDeckStore.Read(p).StartingPropsGranted));
        run.ActFloor=9;CommissionCmd.Ensure(run);
        Check(ActionTypes.TryGetActionType(ActionTypes.TypeToId<NetCommissionAction>(),out var questAction)&&questAction==typeof(NetCommissionAction),"原生动作类型表发现委托类型");
        if(me.Character is Frieren) {
            Check(await CommissionCmd.Send(me,1,CommissionOperation.Accept),"通过ENet接取本人委托");
            Check(!await CommissionCmd.Send(me,1,CommissionOperation.Accept),"重复接取被拒绝");
            Check(!await CommissionCmd.Send(run.Players.First(p=>p!=me),1,CommissionOperation.Accept),"客户端不能操作其他玩家委托");
        }
        await Same("commission-accepted",()=>frierens.All(p=>CommissionCmd.Read(p).Quests.Any(q=>q.Id==1&&q.Status==CommissionStatus.Accepted)));
        foreach(var p in frierens){var q=CommissionCmd.Read(p);q.Record("mp-original-enemies",CommissionGoal.Monsters,3);CommissionCmd.Write(p,q);}
        await Barrier("commission-ready");
        if(me.Character is Frieren) {
            int oldGold=me.Gold;
            Check(await CommissionCmd.Send(me,1,CommissionOperation.Claim)&&me.Gold==oldGold+90,"通过ENet提交本人委托并获得固定金币");
            Check(!await CommissionCmd.Send(me,1,CommissionOperation.Claim)&&me.Gold==oldGold+90,"重复提交不重复领奖");
        }
        await Same("commission-claimed",()=>frierens.All(p=>CommissionCmd.Read(p).Quests.Single(q=>q.Id==1).Status==CommissionStatus.Claimed));
        Check(frierens.All(p=>MagicDeckStore.Read(p).Items.Count==3&&MagicDeckStore.Read(p).Props.Count==2),"每位芙莉莲独立选三因子、各得两个工具");
        Check(!mixed||MagicDeckStore.Read(p2).Items.Count==0,"混合队伍其他角色不获得魔法书物品");
        var originals=frierens.ToDictionary(p=>p,p=>MagicDeckStore.Read(p).ToJson());
        var sync=RunManager.Instance.RestSiteSynchronizer;
        foreach(bool cancel in new[]{true,false}) {
            CancelShop=cancel;sync.BeginRestSite();var optionCounts=frierens.ToDictionary(p=>p,p=>sync.GetOptionsForPlayer(p).Count);await Barrier("shop-start-"+cancel);
            if(me.Character is Frieren) {
                int index=sync.GetOptionsForPlayer(me).ToList().FindIndex(o=>o is BuyMagicPropsOption);
                Check(index>=0,"多人营地提供魔具店");Check(await sync.ChooseLocalOption(index)!=cancel,"原生营地同步器处理确认/取消");
            }
            await Same("shop-"+cancel,()=>frierens.All(p=>sync.GetChosenOptionIndex(p.NetId).HasValue&&(cancel||MagicDeckStore.Read(p).Props.Count==4)));
            Check(frierens.All(p=>sync.GetOptionsForPlayer(p).Count==(cancel?optionCounts[p]:0)),"取消保留营地行动、确认消费行动");
        }
        foreach(var p in frierens){var d=MagicDeckStore.Read(p);d.AddProp("MysteryBox");MagicDeckStore.Write(p,d);}
        CancelShop=false;sync.BeginRestSite();await Barrier("shop-race-ready");
        if(me.Character is Frieren) {
            int id=MagicDeckStore.Read(me).Props.First(p=>p.PropId=="MysteryBox").InstanceId;
            var use=MagicPropCmd.UseSelected(me,id,[]);
            int index=sync.GetOptionsForPlayer(me).ToList().FindIndex(o=>o is BuyMagicPropsOption);
            var visit=sync.ChooseLocalOption(index);
            Check(await use&&await visit,"魔具请求未结束时进入商店，两项按原生动作队列顺序完成");
        }
        await Same("shop-race",()=>frierens.All(p=>sync.GetOptionsForPlayer(p).Count==0));
        foreach(var prop in MagicPropRules.All.Where(x=>!x.CombatOnly)) {
            foreach(var p in frierens) {
                var d=MagicDeckState.FromJson(originals[p]);foreach(var x in d.Props.ToArray())d.Remove(x.InstanceId);
                foreach(string id in new[]{"BePrepared","BuildUpBody","HighSpeedMovement"})d.Add(id);
                d.AddProp(prop.Id);MagicDeckStore.Write(p,d);
            }
            await Barrier("prop-start-"+prop.Id);
            if(me.Character is Frieren) {
                var d=MagicDeckStore.Read(me);var item=d.Props.Single();
                var targets=MagicPropRules.Eligible(d,item).Take(prop.Targets).ToArray();
                if(prop.Id=="UnbelievableTool")targets=[d.Items.First(i=>i.Slot>=0).InstanceId,d.Items.First(i=>i.Slot<0).InstanceId];
                Check(await MagicPropCmd.UseSelected(me,item.InstanceId,targets),"同步使用 "+prop.Id);
                Check(!await MagicPropCmd.UseSelected(me,item.InstanceId,targets),"重复点击不再次消耗 "+prop.Id);
            }
            await Same(prop.Id,()=>frierens.All(p=>!MagicDeckStore.Read(p).Props.Any(x=>x.PropId==prop.Id)));
        }
        foreach(var p in frierens){var d=MagicDeckStore.Read(p);d.AddProp("MysteryBox");MagicDeckStore.Write(p,d);}
        await Barrier("invalid-ready");
        if(me.Character is Frieren) {
            var d=MagicDeckStore.Read(me);int id=d.Props.First(x=>x.PropId=="MysteryBox").InstanceId;
            int rng=CombatTestState.RngCounter(me.PlayerRng.Rewards);
            Check(!await MagicDeckNetwork.Send(me,new NetMagicDeckAction{Prop=id,Targets=[],Snapshot="old",InCombat=false}),"过期库存请求被拒绝");
            Check(!await MagicDeckNetwork.Send(me,new NetMagicDeckAction{Prop=id,Targets=[999999],Snapshot=MagicDeckNetwork.Snapshot(me),InCombat=false}),"无效目标被拒绝");
            Check(CombatTestState.RngCounter(me.PlayerRng.Rewards)==rng&&MagicDeckStore.Read(me).ToJson()==d.ToJson(),"拒绝请求不消耗魔具和RNG");
        }
        await Same("invalid");
        var combat=new CombatState(runState:run);foreach(var p in run.Players)combat.AddPlayer(p);
        var enemy=combat.CreateCreature(ModelDb.Monster<Axebot>().ToMutable(),CombatSide.Enemy,null);combat.AddCreature(enemy);
        CombatManager.Instance.SetUpCombat(combat);CombatTestState.SetInProgress(true);
        AccessTools.Property(typeof(CombatManager),nameof(CombatManager.IsStarting)).SetValue(CombatManager.Instance,false);
        AccessTools.Property(typeof(CombatManager),nameof(CombatManager.PlayerActionsDisabled)).SetValue(CombatManager.Instance,false);
        foreach(var p in run.Players)p.PlayerCombatState!.Phase=PlayerTurnPhase.Play;
        RunManager.Instance.ActionQueueSynchronizer.SetCombatState(ActionSynchronizerCombatState.PlayPhase);
        foreach(var p in frierens){var d=MagicDeckStore.Read(p);d.AddProp("ShadowDragonHorn");MagicDeckStore.Write(p,d);}
        await Barrier("combat-ready");
        if(me.Character is Frieren) {
            var d=MagicDeckStore.Read(me);var horn=d.Props.Single(x=>x.PropId=="ShadowDragonHorn");
            Check(await MagicPropCmd.UseSelected(me,horn.InstanceId,MagicPropRules.Eligible(d,horn).Take(2).ToArray()),"战斗中黑龙角同步销毁两目标并获得能量");
        }
        await Same("combat-horn",()=>frierens.All(p=>!MagicDeckStore.Read(p).Props.Any(x=>x.PropId=="ShadowDragonHorn")));
        foreach(var p in frierens){var d=MagicDeckStore.Read(p);d.AddProp("MysteryBox");MagicDeckStore.Write(p,d);}
        await Barrier("phase-ready");
        if(me.Character is Frieren) {
            int id=MagicDeckStore.Read(me).Props.First(x=>x.PropId=="MysteryBox").InstanceId;
            Check(!await MagicDeckNetwork.Send(me,new NetMagicDeckAction{Prop=id,Targets=[],Snapshot=MagicDeckNetwork.Snapshot(me),InCombat=true,Turn=me.PlayerCombatState!.TurnNumber-1}),"过期回合请求被拒绝");
            me.PlayerCombatState!.Phase=PlayerTurnPhase.None;Check(!MagicPropCmd.CanUse(me,id),"非本人出牌阶段不能使用魔具");me.PlayerCombatState.Phase=PlayerTurnPhase.Play;
            int hp=me.Creature.CurrentHp;me.Creature.SetCurrentHpInternal(0);Check(!MagicPropCmd.CanUse(me,id),"死亡角色不能使用魔具");me.Creature.SetCurrentHpInternal(hp);
            var other=run.Players.First(p=>p!=me);Check(!MagicPropCmd.CanUse(other,id),"界面不能操作队友魔法书");
        }
        await Same("phase-guards");
        // 使用真实原生出牌动作验证队友出牌不会推进本人的连同调机会。
        NetCombatCardDb.Instance.StartCombat(run.Players);
        foreach(var p in run.Players)await PlayerCmd.GainEnergy(20,p);
        var gates=new[]{combat.CreateCard<OpenTheWaygate>(p1),combat.CreateCard<OpenTheWaygate>(p1)};
        foreach(var c in gates)await CardPileCmd.Add(c,PileType.Hand);
        await Barrier("gate-ready");
        async Task HostPlay(CardModel c) {
            if(host) {
                var action=new PlayCardAction(c,null);RunManager.Instance.ActionQueueSynchronizer.RequestEnqueue(action);await action.CompletionTask;
            }
            await Wait(()=>c.Pile?.Type!=PileType.Hand,"真实出牌："+c.Id);
        }
        await HostPlay(gates[0]);await HostPlay(gates[1]);
        Check(p1.Creature.GetPower<OpenTheWaygatePower>()!.RemainingOpportunities==2,"两张门分别提供一次机会");
        if(!mixed) {
            var otherMana=combat.CreateCard<Mana>(p2);await CardPileCmd.Add(otherMana,PileType.Hand);await Barrier("other-mana-ready");
            if(!host) {
                RunManager.Instance.ActionQueueSynchronizer.RequestEnqueue(new PlayCardAction(otherMana,null));
            }
            await Wait(()=>otherMana.Pile?.Type!=PileType.Hand,"队友同调");
            Check(p1.Creature.GetPower<OpenTheWaygatePower>()!.Consecutive==0,"队友的同调不计入本人连击");
        }
        // 每张魔力经 NetPlayCardAction 发送；避免同时向手牌塞十张以上。
        for(int i=0;i<10;i++) {
            var mana=combat.CreateCard<Mana>(p1);await CardPileCmd.Add(mana,PileType.Hand);await Barrier("mana-ready-"+i);await HostPlay(mana);
        }
        var gate=p1.Creature.GetPower<OpenTheWaygatePower>()!;
        await Same("waygate",()=>gate.PendingTurns==2);
        Check(gate.ShouldTakeExtraTurn(p1)&&!gate.ShouldTakeExtraTurn(p2),"两次额外回合只属于施法者");
        await gate.AfterTakingExtraTurn(p2);Check(gate.PendingTurns==2,"队友的额外回合不消耗本人机会");
        await gate.AfterTakingExtraTurn(p1);Check(gate.PendingTurns==1,"第一次额外回合后保留一次");
        await gate.AfterTakingExtraTurn(p1);Check(!p1.Creature.HasPower<OpenTheWaygatePower>(),"机会耗尽后两端移除图标");
        await Same("waygate-consumed");
        var save=RunManager.Instance.ToSave(null);var writer=new PacketWriter();save.Serialize(writer);var reader=new PacketReader();reader.Reset(writer.Buffer);var packet=new SerializableRun();packet.Deserialize(reader);
        var restored=RunState.FromSerializable(packet);
        Check(restored.Players.Select(p=>MagicDeckStore.Read(p).ToJson()).SequenceEqual(run.Players.Select(p=>MagicDeckStore.Read(p).ToJson())),"原生完整跑局网络存档保留各玩家魔法书及赠品标记");
        Check(restored.Players.Select(p=>CommissionCmd.Read(p).ToJson()).SequenceEqual(run.Players.Select(p=>CommissionCmd.Read(p).ToJson())),"原生完整网络存档保留委托与已领奖状态");
        await Barrier("before-reconnect");
        timer.Stop();RunManager.Instance.CleanUp();timer.QueueFree();
        await Barrier("disconnected");
        // 关闭旧连接，重新建立 ENet 会话；用原生多人载入入口恢复刚才的数据包。
        var newTimer=new Godot.Timer{WaitTime=0.01,Autostart=true};
        if(host) {
            var s=new NetHostGameService();Check(s.StartENetHost(port,2)==null,"重启ENet主机");service=s;
            newTimer.Timeout+=s.Update;root.AddChild(newTimer);
            File.WriteAllText(Path.Combine(dir,"reconnect-host-listening"),"ready");
            await Wait(()=>s.ConnectedPeers.Count==1,"重连客户端");s.SetPeerReadyForBroadcasting(2);
        }
        else {
            await Wait(()=>File.Exists(Path.Combine(dir,"reconnect-host-listening")),"重连主机监听");
            var s=new NetClientGameService();var transport=new ENetClient(s);s.Initialize(transport,PlatformType.None);service=s;
            Check(await transport.ConnectToHost(2,"127.0.0.1",port)==null,"客户端重新连接ENet");
            newTimer.Timeout+=s.Update;root.AddChild(newTimer);
        }
        await Barrier("reconnected");
        var lobby=new LoadRunLobby(service,new LoadListener(),packet);
        await RunManager.Instance.SetUpSavedMultiplayer(restored,lobby);
        LocalContext.NetId=host?1ul:2ul;
        // 独立测试不保存至磁盘，载入路径其余初始化保持原生。
        AccessTools.Property(typeof(RunManager),nameof(RunManager.ShouldSave)).SetValue(RunManager.Instance,false);
        run=restored;me=run.Players.Single(p=>p.NetId==(host?1ul:2ul));
        run.PushRoom(new RestSiteRoom());CombatTestState.SetInProgress(false);
        await Same("restored");
        if(me.Character is Frieren) {
            string old=MagicDeckStore.Read(me).ToJson();int counter=CombatTestState.RngCounter(me.PlayerRng.Rewards);
            Check(!await CommissionCmd.Send(me,1,CommissionOperation.Claim),"断线载入后不能重复领取委托报酬");
            await MagicDeckCmd.ChooseStartingFactors(new BlockingPlayerChoiceContext(),me);
            Check(MagicDeckStore.Read(me).ToJson()==old&&CombatTestState.RngCounter(me.PlayerRng.Rewards)==counter,"读档不重复选择、赠品或推进随机数");
            int id=MagicDeckStore.Read(me).Props.First(p=>p.PropId=="MysteryBox").InstanceId;
            Check(await MagicPropCmd.UseSelected(me,id,[]),"载入重连后继续使用同步魔具");
        }
        await Same("restored-used",()=>run.Players.Where(p=>p.Character is Frieren).All(p=>MagicDeckStore.Read(p).Props.Count(x=>x.PropId=="MysteryBox")==1));
        run.ActFloor=9;CommissionCmd.Ensure(run);
        if(me.Character is Frieren)Check(await CommissionCmd.Send(me,3,CommissionOperation.Accept),"重连后通过ENet接取挑战委托");
        await Same("challenge-accepted",()=>run.Players.Where(p=>p.Character is Frieren).All(p=>CommissionCmd.Read(p).Quests.Single(q=>q.Id==3).Status==CommissionStatus.Accepted));
        foreach(var p in run.Players.Where(p=>p.Character is Frieren)){var q=CommissionCmd.State(p);q.Record("mp-swift",CommissionGoal.FirstTurnBattle);CommissionCmd.Write(p,q);}
        await Barrier("challenge-ready");
        if(me.Character is Frieren){
            int oldCount=MagicDeckStore.Read(me).Props.Count;int oldGold=me.Gold;
            Check(await CommissionCmd.Send(me,3,CommissionOperation.Claim)&&MagicDeckStore.Read(me).Props.Count==oldCount+1&&me.Gold==oldGold+100,"ENet挑战报酬同步魔具和额外金币");
        }
        await Same("challenge-claimed",()=>run.Players.Where(p=>p.Character is Frieren).All(p=>CommissionCmd.Read(p).Quests.Single(q=>q.Id==3).Status==CommissionStatus.Claimed));
        if(me.Character is Frieren)Check(await CommissionCmd.Send(me,5,CommissionOperation.Accept),"ENet接取无伤精英委托");
        await Same("expiry-ready",()=>run.Players.Where(p=>p.Character is Frieren).All(p=>CommissionCmd.Read(p).Quests.Single(q=>q.Id==5).Status==CommissionStatus.Accepted));
        foreach(var p in run.Players.Where(p=>p.Character is Frieren)){var q=CommissionCmd.State(p);q.Record("mp-flawless",CommissionGoal.FlawlessElite);CommissionCmd.Write(p,q);}
        run.CurrentActIndex=1;run.ActFloor=1;CommissionCmd.Ensure(run);await Barrier("next-act");
        if(me.Character is Frieren)Check(!await CommissionCmd.Send(me,5,CommissionOperation.Claim),"ENet进入新幕后拒绝旧幕已达标领奖");
        await Same("expired",()=>run.Players.Where(p=>p.Character is Frieren).All(p=>CommissionCmd.Read(p).ActiveCount==0&&CommissionCmd.Read(p).Quests.Single(q=>q.Id==5).Status==CommissionStatus.Abandoned));
        File.WriteAllText(Path.Combine(dir,role+"-result.json"),JsonSerializer.Serialize(new{checks,mixed}));
        await Barrier("finished");
        newTimer.Stop();harmony.UnpatchAll(harmony.Id);foreach(var screen in Screens)screen.Free();Overlay.Free();
        RunManager.Instance.CleanUp();newTimer.QueueFree();return checks;
    }
}
