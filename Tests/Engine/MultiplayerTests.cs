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
        public void PlayerConnected(LoadRunLobbyPlayer player) { }
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
        bool factors=cards.All(c=>c is FactorCard);
        if(factors && (cards.Count!=SpellCatalog.Available.Count() || !cards.OfType<FactorCard>().Any(c=>c.FactorId=="HighSpeedMovement")))
            throw new Exception("王室魔导书必须展示全部可获得因子及谜题");
        ((TaskCompletionSource<IEnumerable<CardModel>>)AccessTools.Field(typeof(NCardGridSelectionScreen),"_completionSource").GetValue(__result))
            .SetResult(factors||cards.Count==3?new[]{cards[(int)(LocalContext.NetId!.Value-1)%cards.Count]}:CancelShop?[]:new[]{cards[1],cards[6]});
        return false;
    }

    private static readonly List<string> CombatDiagnostics=[];
    private static void InspectCompletion(Player player, uint request, bool result)
    {
        if(CombatManager.Instance.IsInProgress)CombatDiagnostics.Add($"local={LocalContext.NetId} player={player.NetId} request={request} result={result} phase={player.PlayerCombatState?.Phase} deck={MagicDeckStore.Read(player).ToJson()}");
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
            while(!ready()){if(Time.GetTicksMsec()-start>20000)throw new Exception("联机等待超时："+reason+" 诊断："+string.Join(";",CombatDiagnostics));await Tick();}
        }
        async Task Barrier(string name) {
            Publish(name+"-"+role+".ready","ready");
            await Wait(()=>File.Exists(Path.Combine(dir,name+"-"+peer+".ready")),name);
        }
        async Task<bool> QuestSend(int id,CommissionOperation operation){await Wait(()=>CommissionCmd.CanAct(runPlayer()),"等待委托操作阶段");return await CommissionCmd.Send(runPlayer(),id,operation);}
        Player runPlayer()=>RunManager.Instance.DebugOnlyGetState()!.Players.Single(p=>p.NetId==(host?1ul:2ul));
        MessageTypes.Initialize();ActionTypes.Initialize();
        Check(ActionTypes.TryGetActionType(ActionTypes.TypeToId<NetMagicDeckAction>(),out var actionType)&&actionType==typeof(NetMagicDeckAction),"原生动作类型表发现模组类型");
        var p1=Player.CreateForNewRun<Frieren>(UnlockState.all,1);
        var p2=mixed?Player.CreateForNewRun<Ironclad>(UnlockState.all,2):Player.CreateForNewRun<Frieren>(UnlockState.all,2);
        var run=RunState.CreateForTest([p1,p2],seed:"FRIEREN_MULTIPLAYER");
        INetGameService service;
        var timer=new Godot.Timer{WaitTime=0.01,Autostart=true};
        if(host) {
            var s=new NetHostGameService(MegaCrit.Sts2.Core.Multiplayer.PeerVersionInfo.LocalDefault());Check(s.StartENetHost(port,2)==null,"ENet主机启动");service=s;
            timer.Timeout+=s.Update;root.AddChild(timer);
            await Wait(()=>s.ConnectedPeers.Count==1,"等待客户端连接");s.SetPeerReadyForBroadcasting(2);
        }
        else {
            var s=new NetClientGameService(MegaCrit.Sts2.Core.Multiplayer.PeerVersionInfo.LocalDefault());var transport=new ENetClient(s);s.Initialize(transport,PlatformType.None);service=s;
            Check(await transport.ConnectToHost(2,"127.0.0.1",port)==null,"ENet客户端连接");
            timer.Timeout+=s.Update;root.AddChild(timer);
        }
        await Barrier("connected");
        RunManager.Instance.SetUpTest(run,service);LocalContext.NetId=host?1ul:2ul;
        RunManager.Instance.GenerateRooms();
        if(OS.GetCmdlineUserArgs().Contains("--mp-dragon")){
            var stats=SaveManager.Instance.Progress.GetOrCreateAncientStats(ModelDb.AncientEvent<FrierenMod.Scripts.Ancients.Serie>().Id);
            stats.CharStats.Clear();stats.CharStats.Add(new AncientCharacterStats{Character=p1.Character.Id,Wins=host?0:5});
            await Barrier("serie-first-start");await FrierenMod.Scripts.Ancients.SerieFirstVisit.Ensure(run,2);await Barrier("serie-first-done");
            Check(run.Acts[2].Ancient is FrierenMod.Scripts.Ancients.Serie&&FrierenMod.Scripts.Ancients.SerieFirstVisit.Decision.Get(p1)==2,"访问记录不同仍按房主首次必遇赛莉耶");
            foreach(var p in run.Players)FrierenMod.Scripts.Ancients.SerieFirstVisit.Decision.Set(p,0);
            stats.CharStats[0].Wins=host?1:0;
            var rooms=(RoomSet)AccessTools.Field(typeof(ActModel),"_rooms").GetValue(run.Acts[2])!;rooms.Ancient=ModelDb.AncientEvent<MegaCrit.Sts2.Core.Models.Events.Tanx>();
            await Barrier("serie-repeat-start");await FrierenMod.Scripts.Ancients.SerieFirstVisit.Ensure(run,2);await Barrier("serie-repeat-done");
            Check(rooms.Ancient is MegaCrit.Sts2.Core.Models.Events.Tanx&&FrierenMod.Scripts.Ancients.SerieFirstVisit.Decision.Get(p1)==1,"房主已访问时客户端首次记录不强制改图");
            checks+=await DragonBallTests.RunParty(root,run,host,Barrier,Wait);
            File.WriteAllText(Path.Combine(dir,role+"-result.json"),JsonSerializer.Serialize(new{checks,dragon=true}));
            await Barrier("finished");timer.Stop();RunManager.Instance.CleanUp();timer.QueueFree();return checks;
        }
        // 不模拟 Steam 会话，不读写作者存档。选择屏替身只生成本地选择结果。
        Overlay=new NOverlayStack();
        var harmony=new Harmony("frieren.tests.multiplayer");
        harmony.Patch(AccessTools.Method(typeof(MagicDeckNetwork),nameof(MagicDeckNetwork.Complete)),prefix:new HarmonyMethod(typeof(MultiplayerTests),nameof(InspectCompletion)));
        harmony.Patch(AccessTools.PropertyGetter(typeof(NOverlayStack),nameof(NOverlayStack.Instance)),prefix:new HarmonyMethod(typeof(MultiplayerTests),nameof(GetOverlay)));
        foreach(var method in new[]{"Push","Remove"})harmony.Patch(AccessTools.Method(typeof(NOverlayStack),method),prefix:new HarmonyMethod(typeof(MultiplayerTests),nameof(SkipOverlay)));
        harmony.Patch(AccessTools.Method(typeof(NChooseACardSelectionScreen),"ShowScreen"),prefix:new HarmonyMethod(typeof(MultiplayerTests),nameof(Choose)));
        harmony.Patch(AccessTools.Method(typeof(NSimpleCardSelectScreen),"Create",[typeof(IReadOnlyList<CardModel>),typeof(CardSelectorPrefs)]),prefix:new HarmonyMethod(typeof(MultiplayerTests),nameof(Grid)));
        run.PushRoom(new RestSiteRoom());run.AppendToMapPointHistory(MegaCrit.Sts2.Core.Map.MapPointType.RestSite,RoomType.RestSite,null);CombatTestState.SetInProgress(false);
        MegaCrit.Sts2.Core.Rewards.RewardsSet.testSelector=async set=>{
            var rewardSelector=new MegaCrit.Sts2.Core.TestSupport.TestCardSelector();
            rewardSelector.PrepareToSelectCardRewardAtIndex((int)LocalContext.NetId!.Value-1);
            using var selection=CombatTestState.UseSelector(rewardSelector);
            foreach(var reward in set.Rewards)await RunManager.Instance.RewardsSetSynchronizer.SelectLocalReward(reward);
        };
        var me=host?p1:p2;var frierens=run.Players.Where(p=>p.Character is Frieren).ToArray();
        async Task Same(string name,Func<bool>? ready=null) {
            if(ready!=null)await Wait(ready,name);
            await Barrier(name+"-done");
            // 阶段完成后再比较两端全部玩家数据，而非只核对本机角色。
            string snapshot=JsonSerializer.Serialize(run.Players.Select(p=>new {p.NetId,deck=MagicDeckStore.Read(p).ToJson(),commissions=CommissionCmd.Read(p).ToJson(),gold=p.Gold,
                deckCards=p.Deck.Cards.Select(c=>new{Id=c.Id.ToString(),c.CurrentUpgradeLevel,fare=c is ValuableCargo v?v.Fare:-1,stop=c is ValuableCargo cargo?cargo.LastStop:""}),
                rng=CombatTestState.RngCounter(p.PlayerRng.Rewards),debt=DebtStore.Read(p),rolls=run.Rng.Niche.ToSerializable().counter,hp=p.Creature.CurrentHp,energy=p.PlayerCombatState?.Energy,
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
        if(OS.GetCmdlineUserArgs().Contains("--mp-turn-guard")) {
            foreach(var p in run.Players) {
                var deck=new MagicDeckState();
                for(int i=0;i<3;i++){deck.PrepareDraft(_=>0);deck.SelectInitial(deck.Draft[0]);}
                deck.GrantStartingProps();deck.AddProp("ShadowDragonHorn");MagicDeckStore.Write(p,deck);
            }
            var turnCombat=new CombatState(runState:run);foreach(var p in run.Players)turnCombat.AddPlayer(p);
            var target=turnCombat.CreateCreature(ModelDb.Monster<Axebot>().ToMutable(),CombatSide.Enemy,null);turnCombat.AddCreature(target);
            CombatManager.Instance.SetUpCombat(turnCombat);CombatTestState.SetInProgress(true);
            object turnState=AccessTools.Property(typeof(CombatManager),nameof(CombatManager.IsStarting)).SetMethod!=null
                ? CombatManager.Instance : AccessTools.Field(typeof(CombatManager),"_turnState").GetValue(CombatManager.Instance);
            AccessTools.Property(turnState.GetType(),nameof(CombatManager.IsStarting)).SetValue(turnState,false);
            AccessTools.Property(typeof(CombatManager),nameof(CombatManager.PlayerActionsDisabled)).SetValue(CombatManager.Instance,false);
            foreach(var p in run.Players)p.PlayerCombatState!.Phase=PlayerTurnPhase.Play;
            RunManager.Instance.ActionQueueSynchronizer.SetCombatState(Enum.Parse<ActionSynchronizerCombatState>("PlayPhase"));
            RunManager.Instance.ActionExecutor.Unpause();
            int energy=p2.PlayerCombatState!.Energy;
            await Barrier("turn-guard-ready");
            if(host) {
                var end=new EndPlayerTurnAction(p1,p1.PlayerCombatState!.TurnNumber);
                RunManager.Instance.ActionQueueSynchronizer.RequestEnqueue(end);await end.CompletionTask;
            }
            await Wait(()=>CombatManager.Instance.IsPlayerReadyToEndTurn(p1),"房主结束回合动作同步");
            Check(CombatManager.Instance.PlayerActionsDisabled==host,"只有已结束回合的房主禁用本机输入");
            await Barrier("turn-guard-host-ended");
            Check(CommissionCmd.CanAct(p2,false),"房主结束回合不阻止队友委托结算");
            Check(!CommissionCmd.CanAct(p1,false),"已结束回合者不能结算新的委托");
            if(!host) {
                var deck=MagicDeckStore.Read(p2);var horn=deck.Props.Single(p=>p.PropId=="ShadowDragonHorn");
                Check(await MagicPropCmd.UseSelected(p2,horn.InstanceId,MagicPropRules.Eligible(deck,horn).Take(2).ToArray()),"队友通过真实网络动作使用黑龙角");
            }
            await Same("turn-guard-horn",()=>!MagicDeckStore.Read(p2).Props.Any(p=>p.PropId=="ShadowDragonHorn"));
            Check(p2.PlayerCombatState.Energy==energy+2,"两端都给尚未结束回合的队友2能量");
            Check(MagicDeckStore.Read(p1).Props.Any(p=>p.PropId=="ShadowDragonHorn"),"不消耗房主魔具");
            var rejected=new NetMagicDeckAction {Prop=MagicDeckStore.Read(p1).Props.Single(p=>p.PropId=="ShadowDragonHorn").InstanceId,
                Targets=MagicDeckStore.Read(p1).Props.Where(p=>p.PropId=="UnbelievableTool").Select(p=>p.InstanceId).ToArray(),
                Snapshot=MagicDeckNetwork.Snapshot(p1),InCombat=true,Turn=p1.PlayerCombatState!.TurnNumber};
            if(host)Check(!await MagicDeckNetwork.Send(p1,rejected),"同步端拒绝已结束回合者的迟到魔具请求");
            await Same("turn-guard-rejected");
            File.WriteAllText(Path.Combine(dir,role+"-result.json"),JsonSerializer.Serialize(new{checks,turnGuard=true}));
            await Barrier("finished");timer.Stop();harmony.UnpatchAll(harmony.Id);Overlay.Free();
            RunManager.Instance.CleanUp();timer.QueueFree();return checks;
        }
        await Barrier("run-ready");
        if(me.Character is Frieren)await MagicDeckCmd.ChooseStartingFactors(new BlockingPlayerChoiceContext(),me);
        await Same("initial",()=>frierens.All(p=>MagicDeckStore.Read(p).StartingPropsGranted));
        run.ActFloor=9;CommissionCmd.Ensure(run);int villageId=CommissionState.QuestId(0,2);
        foreach(var p in frierens){var q=CommissionCmd.State(p);q.Quests[1]=new(){Id=villageId,Act=0,Template="GRAPES",Goal=CommissionGoal.Deck,Spell="GrapeSpell",Reward=new(CommissionRewardKind.Gold,"",80)};CommissionCmd.Write(p,q);await CardPileCmd.Add(run.CreateCard<GrapeSpell>(p),PileType.Deck);}
        Check(ActionTypes.TryGetActionType(ActionTypes.TypeToId<NetCommissionAction>(),out var questAction)&&questAction==typeof(NetCommissionAction),"原生动作类型表发现委托类型");
        if(me.Character is Frieren){
            Check(await QuestSend(villageId,CommissionOperation.Accept),"通过ENet接取本人村民委托");
            Check(!await CommissionCmd.Send(me,CommissionState.QuestId(0,1),CommissionOperation.Accept),"已经接一项后不能再接第二项");
            Check(!await CommissionCmd.Send(run.Players.First(p=>p!=me),villageId,CommissionOperation.Accept),"客户端不能操作其他玩家委托");
        }
        await Same("commission-accepted",()=>frierens.All(p=>CommissionCmd.Read(p).Active?.Id==villageId));
        if(me.Character is Frieren){int oldGold=me.Gold;Check(await QuestSend(villageId,CommissionOperation.Claim)&&me.Gold==oldGold+80,"通过ENet提交村民委托并获得80金币");Check(!await CommissionCmd.Send(me,villageId,CommissionOperation.Claim)&&me.Gold==oldGold+80,"重复提交不重复领奖");}
        await Same("commission-claimed",()=>frierens.All(p=>CommissionCmd.Read(p).Quests.Single(q=>q.Id==villageId).Status==CommissionStatus.Claimed));
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
        foreach(var prop in MagicPropRules.All.Concat(MagicPropRules.CommissionOnly).Where(x=>!x.CombatOnly).Append(MagicPropRules.Get("SealingOre"))) {
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
        enemy.SetMaxHpInternal(9999);enemy.SetCurrentHpInternal(9999);
        CombatManager.Instance.SetUpCombat(combat);CombatTestState.SetInProgress(true);
        AccessTools.Property(typeof(CombatManager),nameof(CombatManager.IsStarting)).SetValue(CombatManager.Instance,false);
        AccessTools.Property(typeof(CombatManager),nameof(CombatManager.PlayerActionsDisabled)).SetValue(CombatManager.Instance,false);
        foreach(var p in run.Players)p.PlayerCombatState!.Phase=PlayerTurnPhase.Play;
        RunManager.Instance.ActionQueueSynchronizer.SetCombatState(Enum.Parse<ActionSynchronizerCombatState>("PlayPhase"));
        foreach(var p in frierens){var d=MagicDeckStore.Read(p);d.AddProp("ShadowDragonHorn");MagicDeckStore.Write(p,d);}
        await Barrier("combat-ready");
        if(me.Character is Frieren) {
            var d=MagicDeckStore.Read(me);var horn=d.Props.Single(x=>x.PropId=="ShadowDragonHorn");
            await Wait(()=>MagicPropCmd.CanUse(me,horn.InstanceId),"等待黑龙角输入阶段");
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
        // 前面的十次同调会把手牌抽满；先腾出位置，避免测试牌被原生满手规则送进弃牌堆。
        foreach(var held in p1.PlayerCombatState!.Hand.Cards.ToArray())await CardPileCmd.Add(held,PileType.Discard);
        foreach(bool jackpot in new[]{false,true}) {
            var rng=run.Rng.Niche;int minimum=jackpot?5:4;
            int nextCounter=rng.ToSerializable().counter;
            while(true){
                var probe=new MegaCrit.Sts2.Core.Random.Rng(rng.ToSerializable());
                for(int advance=rng.ToSerializable().counter;advance<nextCounter;advance++)probe.NextInt(8);
                var rolls=Enumerable.Range(0,3).Select(_=>probe.NextInt(minimum,8)).ToArray();
                if(rolls.All(n=>n==7)==jackpot)break;
                if(++nextCounter>rng.ToSerializable().counter+20000)throw new Exception("未找到联机投点测试序列");
            }
            while(rng.ToSerializable().counter<nextCounter)rng.NextInt(8);
            var honest=combat.CreateCard<HonestLiving>(p1);if(jackpot)CardCmd.Upgrade(honest);
            int oldGold=p1.Gold,oldDebt=DebtStore.Read(p1);
            await CardPileCmd.Add(honest,PileType.Hand);
            Check(honest.Pile?.Type==PileType.Hand&&honest.CanPlay(),"联机测试牌实际进入手牌且可以打出");
            await Barrier("honest-ready-"+jackpot);await HostPlay(honest);
            await Same("honest-result-"+jackpot,()=>honest.Pile?.Type==PileType.Exhaust);
            Check(p1.Gold==oldGold+(jackpot?777:0)&&DebtStore.Read(p1)==oldDebt+(jackpot?0:7)&&DebtStore.Read(p2)==0,
                "原生ENet出牌同步随机金币／负债，仅属于施法者："+jackpot);
            Check(rng.ToSerializable().counter==nextCounter+3,"双端三次独立投点计数相同："+jackpot);
        }
        var save=RunManager.Instance.ToSave(null);var writer=new PacketWriter();save.Serialize(writer);var reader=new PacketReader();reader.Reset(writer.Buffer);var packet=new SerializableRun();packet.Deserialize(reader);
        var restored=RunState.FromSerializable(packet);
        Check(restored.Players.Select(p=>MagicDeckStore.Read(p).ToJson()).SequenceEqual(run.Players.Select(p=>MagicDeckStore.Read(p).ToJson())),"原生完整跑局网络存档保留各玩家魔法书及赠品标记");
        Check(restored.Players.Select(p=>CommissionCmd.Read(p).ToJson()).SequenceEqual(run.Players.Select(p=>CommissionCmd.Read(p).ToJson())),"原生完整网络存档保留委托与已领奖状态");
        Check(restored.Players.Select(DebtStore.Read).SequenceEqual(run.Players.Select(DebtStore.Read)),"原生完整网络存档保留各玩家独立负债");
        await Barrier("before-reconnect");
        timer.Stop();RunManager.Instance.CleanUp();timer.QueueFree();
        await Barrier("disconnected");
        // 关闭旧连接，重新建立 ENet 会话；用原生多人载入入口恢复刚才的数据包。
        var newTimer=new Godot.Timer{WaitTime=0.01,Autostart=true};
        if(host) {
            var s=new NetHostGameService(MegaCrit.Sts2.Core.Multiplayer.PeerVersionInfo.LocalDefault());Check(s.StartENetHost(port,2)==null,"重启ENet主机");service=s;
            newTimer.Timeout+=s.Update;root.AddChild(newTimer);
            File.WriteAllText(Path.Combine(dir,"reconnect-host-listening"),"ready");
            await Wait(()=>s.ConnectedPeers.Count==1,"重连客户端");s.SetPeerReadyForBroadcasting(2);
        }
        else {
            await Wait(()=>File.Exists(Path.Combine(dir,"reconnect-host-listening")),"重连主机监听");
            var s=new NetClientGameService(MegaCrit.Sts2.Core.Multiplayer.PeerVersionInfo.LocalDefault());var transport=new ENetClient(s);s.Initialize(transport,PlatformType.None);service=s;
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
        run.PushRoom(new RestSiteRoom());run.AppendToMapPointHistory(MegaCrit.Sts2.Core.Map.MapPointType.RestSite,RoomType.RestSite,null);CombatTestState.SetInProgress(false);

        await Same("restored");
        if(me.Character is Frieren) {
            string old=MagicDeckStore.Read(me).ToJson();int counter=CombatTestState.RngCounter(me.PlayerRng.Rewards);
            Check(!await CommissionCmd.Send(me,villageId,CommissionOperation.Claim),"断线载入后不能重复领取委托报酬");
            await MagicDeckCmd.ChooseStartingFactors(new BlockingPlayerChoiceContext(),me);
            Check(MagicDeckStore.Read(me).ToJson()==old&&CombatTestState.RngCounter(me.PlayerRng.Rewards)==counter,"读档不重复选择、赠品或推进随机数");
            int id=MagicDeckStore.Read(me).Props.First(p=>p.PropId=="MysteryBox").InstanceId;
            Check(await MagicPropCmd.UseSelected(me,id,[]),"载入重连后继续使用同步魔具");
        }
        await Same("restored-used",()=>run.Players.Where(p=>p.Character is Frieren).All(p=>MagicDeckStore.Read(p).Props.Count(x=>x.PropId=="MysteryBox")==1));
        run.ActFloor=9;CommissionCmd.Ensure(run);int huntId=CommissionState.QuestId(0,1),cargoId=CommissionState.QuestId(0,3);
        var rareChoices=ModelDb.CardPool<FrierenCardPool>().AllCards.Where(c=>c.Rarity==CardRarity.Rare&&FrierenCardAvailability.IsRandomCandidate(c)).Take(3).Select(c=>c.GetType().Name).ToArray();
        foreach(var p in run.Players.Where(p=>p.Character is Frieren)){
            var q=CommissionCmd.State(p);q.Quests[0]=new(){Id=huntId,Act=0,Template="HUNT",Stars=3,Goal=CommissionGoal.EmpoweredElite,Affix=0,Reward=new(CommissionRewardKind.RareChoice,"",1,Choices:rareChoices)};
            q.Quests[2]=new(){Id=cargoId,Act=0,Template="CARGO",Stars=2,Goal=CommissionGoal.Cargo,Reward=new(CommissionRewardKind.Freight,"",1)};q.Revision++;CommissionCmd.Write(p,q);
        }
        await Barrier("new-contracts");
        if(me.Character is Frieren)Check(await QuestSend(huntId,CommissionOperation.Accept),"重连后ENet接取三星强化精英");
        await Same("hunt-accepted",()=>run.Players.Where(p=>p.Character is Frieren).All(p=>CommissionCmd.Read(p).Active?.Id==huntId));
        var eliteModel=ModelDb.AllEncounters.First(e=>e.RoomType==RoomType.Elite).ToMutable();eliteModel.GenerateMonstersWithSlots(run);
        var eliteRoom=new CombatRoom(eliteModel,run);run.PushRoom(eliteRoom);var eliteCombat=eliteRoom.CombatState;foreach(var p in run.Players)eliteCombat.AddPlayer(p);
        var originalEnemy=eliteModel.MonstersWithSlots[0];var eliteEnemy=eliteCombat.CreateCreature(originalEnemy.Item1,CombatSide.Enemy,originalEnemy.Item2);eliteCombat.AddCreature(eliteEnemy);int originalHp=eliteEnemy.MaxHp;
        CombatManager.Instance.SetUpCombat(eliteCombat);CombatTestState.SetInProgress(true);
        AccessTools.Property(typeof(CombatManager),nameof(CombatManager.IsStarting)).SetValue(CombatManager.Instance,false);
        AccessTools.Property(typeof(CombatManager),nameof(CombatManager.PlayerActionsDisabled)).SetValue(CombatManager.Instance,false);
        foreach(var p in run.Players)p.PlayerCombatState!.Phase=PlayerTurnPhase.Play;
        RunManager.Instance.ActionExecutor.Unpause();RunManager.Instance.ActionQueueSynchronizer.SetCombatState(Enum.Parse<ActionSynchronizerCombatState>("PlayPhase"));
        await CommissionCmd.Empower(eliteCombat);await CommissionCmd.Empower(eliteCombat);
        Check(eliteEnemy.MaxHp==originalHp&&eliteEnemy.GetPower<CommissionElitePower>()?.Amount==1,"双端精英强化一次，多人接取不叠加");
        CommissionCmd.Victory(run,eliteRoom);await Same("hunt-ready",()=>run.Players.Where(p=>p.Character is Frieren).All(p=>CommissionCmd.Read(p).Active?.Progress==1));
        if(me.Character is Frieren){int oldCards=me.Deck.Cards.Count;Check(await QuestSend(huntId,CommissionOperation.Claim)&&me.Deck.Cards.Count==oldCards+1&&me.Deck.Cards.Last().GetType().Name==rareChoices[(int)me.NetId-1],"原生选择同步器完成金卡三选一");}
        await Same("hunt-claimed",()=>run.Players.Where(p=>p.Character is Frieren).All(p=>CommissionCmd.Read(p).Quests.Single(q=>q.Id==huntId).Status==CommissionStatus.Claimed));
        CombatManager.Instance.Reset(true);run.PushRoom(new RestSiteRoom());RunManager.Instance.ActionExecutor.Unpause();
        if(me.Character is Frieren)Check(await QuestSend(cargoId,CommissionOperation.Accept),"ENet接取护送并加入货物");
        await Same("cargo-accepted",()=>run.Players.Where(p=>p.Character is Frieren).All(p=>CommissionCargo.Goods(p).Length==1));
        run.AppendToMapPointHistory(MegaCrit.Sts2.Core.Map.MapPointType.RestSite,RoomType.RestSite,null);await CommissionCmd.Entered(run);
        foreach(var p in run.Players.Where(p=>p.Character is Frieren)){
            CommissionCargo.Damaged(p.Creature,new DamageResult(p.Creature,MegaCrit.Sts2.Core.ValueProps.ValueProp.Move){UnblockedDamage=1});
            await CardPileCmd.Add(run.CloneCard(CommissionCargo.Goods(p).Single()),PileType.Deck);
        }
        await Same("cargo-copied",()=>run.Players.Where(p=>p.Character is Frieren).All(p=>CommissionCargo.Goods(p).Length==2&&CommissionCargo.Freight(p)==36));
        var shop=new MerchantRoom();run.PushRoom(shop);run.AppendToMapPointHistory(MegaCrit.Sts2.Core.Map.MapPointType.Shop,RoomType.Shop,null);int beforeDelivery=me.Gold;
        await CommissionCmd.Entered(run,shop);await CommissionCmd.Entered(run,shop);
        Check(me.Character is not Frieren||me.Gold==beforeDelivery+76,"双份复制货物各结算38运费，重复商人通知不重付");
        await Same("cargo-sold",()=>run.Players.Where(p=>p.Character is Frieren).All(p=>CommissionCargo.Goods(p).Length==0&&CommissionCmd.Read(p).Active==null));
        // 再建立一项独立过幕夹具，复核在途货物的自动归还和过期输入。
        foreach(var p in run.Players.Where(p=>p.Character is Frieren)){var q=CommissionCmd.State(p);q.Quests[2]=new(){Id=cargoId,Act=0,Template="CARGO",Stars=2,Goal=CommissionGoal.Cargo,Reward=new(CommissionRewardKind.Freight,"",1)};q.Revision++;CommissionCmd.Write(p,q);}
        await Barrier("cargo-expiry-fixture");
        if(me.Character is Frieren)Check(await QuestSend(cargoId,CommissionOperation.Accept),"ENet接取在途护送夹具");
        await Same("cargo-in-transit",()=>run.Players.Where(p=>p.Character is Frieren).All(p=>CommissionCargo.Goods(p).Length==1));
        run.CurrentActIndex=1;run.ActFloor=1;run.PushRoom(new RestSiteRoom());await CommissionCmd.Entered(run);await Barrier("next-act");
        if(me.Character is Frieren)Check(!await CommissionCmd.Send(me,cargoId,CommissionOperation.Claim),"跨幕拒绝旧护送任务提交");
        await Same("expired",()=>run.Players.Where(p=>p.Character is Frieren).All(p=>CommissionCargo.Goods(p).Length==0&&CommissionCmd.Read(p).ActiveCount==0&&CommissionCmd.Read(p).Quests.Single(q=>q.Id==cargoId).Status==CommissionStatus.Abandoned));
        run.CurrentActIndex=2;
        var army=ModelDb.Event<FrierenMod.Scripts.Events.DemonArmyAttack>();
        Check(army.IsAllowed(run)==!mixed,"魔王军事件只对全芙莉莲第三幕开放");
        if(!mixed){
            harmony.Patch(AccessTools.Method(typeof(RunManager),nameof(RunManager.EnterRoomWithoutExitingCurrentRoom)),prefix:new HarmonyMethod(typeof(DemonArmyAttackTests),nameof(DemonArmyAttackTests.CaptureEnterRoom)));
            for(int branch=0;branch<3;branch++){
                run.PushRoom(new EventRoom(army));
                var events=RunManager.Instance.EventSynchronizer;events.BeginEvent(army);
                await Wait(()=>events.Events.Count==2&&events.Events.All(e=>e.CurrentOptions.Count==3),"魔王军共享事件开始");
                var local=(FrierenMod.Scripts.Events.DemonArmyAttack)events.GetLocalEvent();
                Check(events.IsShared&&events.Events.Cast<FrierenMod.Scripts.Events.DemonArmyAttack>().All(e=>e.SelectedBoss.Id==local.SelectedBoss.Id),"各玩家显示相同随机Boss："+branch);
                DemonArmyAttackTests.CapturedRoom=null;await Barrier("army-vote-ready-"+branch);
                events.ChooseLocalOption(branch);
                await Wait(()=>DemonArmyAttackTests.CapturedRoom!=null,"ENet共享投票进入事件战斗");
                await events.AwaitPendingOptionTasks();
                var selectedRoom=DemonArmyAttackTests.CapturedRoom!;
                Check(selectedRoom.ParentEventId==army.Id&&selectedRoom.RoomType!=RoomType.Boss,"共享战斗保留父事件且不触发终局："+branch);
                string detail=JsonSerializer.Serialize(new{boss=local.SelectedBoss.Id.ToString(),actual=selectedRoom.Encounter.Id.ToString(),type=selectedRoom.RoomType.ToString(),reward=DemonArmyAttackCombat.Rewards(me,selectedRoom).Select(r=>r.GetType().Name).ToArray()});
                string key="army-selected-"+branch;Publish(key+"-"+role+".json",detail);await Barrier(key);
                Check(File.ReadAllText(Path.Combine(dir,key+"-"+peer+".json"))==detail,"ENet两端选择同一遭遇及悬赏："+branch);
                await Same("army-event-"+branch);
            }
        }
        File.WriteAllText(Path.Combine(dir,role+"-result.json"),JsonSerializer.Serialize(new{checks,mixed}));
        await Barrier("finished");
        newTimer.Stop();harmony.UnpatchAll(harmony.Id);foreach(var screen in Screens)screen.Free();Overlay.Free();
        RunManager.Instance.CleanUp();newTimer.QueueFree();return checks;
    }
}
