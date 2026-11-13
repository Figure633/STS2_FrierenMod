using FrierenMod.Scripts.Events;
using FrierenMod.Scripts.RestSite;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace FrierenMod.Scripts.Mechanics;

public struct DragonCampReady : INetMessage, IPacketSerializable
{
    public int Floor;
    public int Stage;
    public ulong Collector;
    public bool ShouldBroadcast => true;
    public bool ShouldBuffer => true;
    public NetTransferMode Mode => NetTransferMode.Reliable;
    public LogLevel LogLevel => LogLevel.VeryDebug;
    public void Serialize(PacketWriter writer) { writer.WriteInt(Floor); writer.WriteInt(Stage); writer.WriteULong(Collector); }
    public void Deserialize(PacketReader reader) { Floor=reader.ReadInt(); Stage=reader.ReadInt(); Collector=reader.ReadULong(); }
}

// 营地不能恢复嵌套房间。用原生房间切换和检查点，单独保存每名玩家剩余营地行动。
public static class DragonSummoning
{
    private static INetGameService? _service;
    private static readonly Dictionary<(int,int,ulong),HashSet<ulong>> Ready = [];
    private static int _campActions;
    private static readonly HashSet<Player> SpentCampAction = [];
    private static bool _returnQueued;
    public static bool IsTransitioning { get; private set; }
    public static Task Pending { get; private set; } = Task.CompletedTask;
    public static Player? Collector(IRunState run) => run.Players.FirstOrDefault(p => DragonBallStore.Read(p).Summoning);

    public static void Begin(Player owner)
    {
        if (IsTransitioning || owner.RunState.CurrentRoom is not RestSiteRoom || !DragonBallStore.CanSummon(owner)) return;
        // 同一个营地多个集齐者按原生收到的第一次选择锁定，整个队伍只进入一次。
        var state=DragonBallStore.Read(owner); state.Summoning=true; DragonBallStore.Write(owner,state);
        IsTransitioning=true; _returnQueued=false;
        Pending=Enter(owner); TaskHelper.RunSafely(Pending);
    }
    private static async Task Frame()
    {
        if (Godot.Engine.GetMainLoop() is Godot.SceneTree tree) await tree.ToSignal(tree,Godot.SceneTree.SignalName.ProcessFrame);
        else await Task.Yield();
    }
    private static async Task Barrier(Player owner,int stage)
    {
        var run=owner.RunState; var manager=RunManager.Instance;
        bool Busy()=>_campActions>0 || manager.ActionExecutor.IsRunning || run.Players.Any(MagicPropCmd.IsBusy);
        while (Busy()) await Frame();
        if (run.Players.Count<=1 || manager.IsSingleplayerOrFakeMultiplayer) return;
        var key=(run.TotalFloor,stage,owner.NetId);
        if(!Ready.TryGetValue(key,out var ready)) Ready[key]=ready=[];
        ready.Add(manager.NetService.NetId);
        manager.NetService.SendMessage(new DragonCampReady{Floor=run.TotalFloor,Stage=stage,Collector=owner.NetId});
        // Reliable 消息按发出顺序送达：所有 ready 之前的选项及选牌结果均会先处理。
        while (run.Players.Any(p=>(manager.RunLobby?.PlayerIds.Contains(p.NetId) ?? true) && !ready.Contains(p.NetId)) || Busy()) {
            if(manager.IsCleaningUp || !ReferenceEquals(manager.DebugOnlyGetState(),run)) return;
            await Frame();
        }
        manager.CombatStateSynchronizer.StartSync(); await manager.CombatStateSynchronizer.WaitForSync();
        Ready.Remove(key);
    }
    private static async Task Enter(Player owner)
    {
        await Frame(); await Barrier(owner,0);
        var manager=RunManager.Instance; var run=owner.RunState;
        if(manager.IsCleaningUp || !ReferenceEquals(manager.DebugOnlyGetState(),run)) return;
        foreach(var player in run.Players) {
            var state=DragonBallStore.Read(player); state.CampFloor=run.TotalFloor;
            state.CampOptions=manager.RestSiteSynchronizer.GetOptionsForPlayer(player).Where(o=>o is not SummonDragonOption).Select(o=>o.OptionId).ToArray();
            DragonBallStore.Write(player,state);
        }
        using var loading=new NetLoadingHandle(manager.NetService);
        await manager.EnterRoom(new EventRoom(ModelDb.Event<SummonDragon>()));
        IsTransitioning=false;
        await SaveManager.Instance.SaveRun(run.CurrentRoom);
    }
    public static void ReturnToCamp(Player owner)
    {
        if(_returnQueued) return;
        _returnQueued=true; IsTransitioning=true;
        Pending=Return(owner); TaskHelper.RunSafely(Pending);
    }
    private static async Task Return(Player owner)
    {
        await Frame();
        var manager=RunManager.Instance; var run=owner.RunState;
        await manager.EventSynchronizer.AwaitPendingOptionTasks();
        await Barrier(owner,1);
        if(manager.IsCleaningUp || !ReferenceEquals(manager.DebugOnlyGetState(),run)) return;
        using var loading=new NetLoadingHandle(manager.NetService);
        await manager.EnterRoom(new RestSiteRoom());
        var state=DragonBallStore.Read(owner); state.Summoning=false; DragonBallStore.Write(owner,state);
        IsTransitioning=false; _returnQueued=false;
        await SaveManager.Instance.SaveRun(run.CurrentRoom);
    }
    public static void RestoreOptions(Player player,List<RestSiteOption> options)
    {
        var state=DragonBallStore.Read(player);
        if(state.CampFloor==player.RunState.TotalFloor && state.CampOptions != null)
            options.RemoveAll(o=>!state.CampOptions.Contains(o.OptionId));
        if(DragonBallStore.CanSummon(player) && Collector(player.RunState)==null &&
            player == player.RunState.Players.Where(DragonBallStore.CanSummon).MinBy(p=>p.NetId) && !options.OfType<SummonDragonOption>().Any())
            options.Add(new SummonDragonOption(player));
    }
    private static void Receive(DragonCampReady message,ulong sender)
    {
        if(RunManager.Instance.DebugOnlyGetState() is not {} run || message.Floor!=run.TotalFloor || !run.Players.Any(p=>p.NetId==sender)) return;
        var key=(message.Floor,message.Stage,message.Collector);
        if(!Ready.TryGetValue(key,out var ready)) Ready[key]=ready=[];
        ready.Add(sender);
    }
    [HarmonyPatch(typeof(RunManager),"InitializeShared")]
    private static class Register
    {
        private static void Postfix(INetGameService netService)
        {
            _service?.UnregisterMessageHandler<DragonCampReady>(Receive); _service=netService;
            netService.RegisterMessageHandler<DragonCampReady>(Receive);
            Ready.Clear(); _campActions=0; IsTransitioning=false; _returnQueued=false; Pending=Task.CompletedTask;
        }
    }
    // beta 会在行动耗尽时立即完成营地，须先保留免费召唤入口。
    [HarmonyPatch(typeof(MegaCrit.Sts2.Core.Hooks.Hook), "ShouldDisableRemainingRestSiteOptions")]
    private static class KeepSummonAvailable
    {
        private static void Postfix(Player player, ref bool __result)
        {
            if (!__result || Collector(player.RunState) != null || !DragonBallStore.CanSummon(player) ||
                player != player.RunState.Players.Where(DragonBallStore.CanSummon).MinBy(p => p.NetId)) return;
            SpentCampAction.Add(player);
            __result = false;
        }
    }
    [HarmonyPatch(typeof(RestSiteSynchronizer),"ChooseOption")]
    private static class TrackChoices
    {
        private static void Prefix() => _campActions++;
        private static void Postfix(ref Task<bool> __result) => __result=Complete(__result);
        private static async Task<bool> Complete(Task<bool> task)
        {
            try { return await task; }
            finally {
                _campActions--;
                if(RunManager.Instance.DebugOnlyGetState() is {} run && run.CurrentRoom is RestSiteRoom) {
                    foreach(var player in run.Players) {
                        var options=(List<RestSiteOption>)RunManager.Instance.RestSiteSynchronizer.GetOptionsForPlayer(player);
                        if (SpentCampAction.Remove(player)) options.RemoveAll(option => option is not SummonDragonOption);
                        // 普通休息耗尽行动，或逛店刚买到第七颗时，仍可免费召唤。
                        if(Collector(run)==null && DragonBallStore.CanSummon(player) &&
                            player==run.Players.Where(DragonBallStore.CanSummon).MinBy(p=>p.NetId) && !options.OfType<SummonDragonOption>().Any())
                            options.Add(new SummonDragonOption(player));
                        var state=DragonBallStore.Read(player);
                        if(state.CampFloor!=run.TotalFloor || state.CampOptions==null) continue;
                        state.CampOptions=RunManager.Instance.RestSiteSynchronizer.GetOptionsForPlayer(player).Where(o=>o is not SummonDragonOption).Select(o=>o.OptionId).ToArray();
                        DragonBallStore.Write(player,state);
                    }
                }
            }
        }
    }
    [HarmonyPatch(typeof(RestSiteSynchronizer),nameof(RestSiteSynchronizer.ChooseLocalOption))]
    private static class BlockNewChoices
    {
        private static bool Prefix(ref Task<bool> __result)
        { if(!IsTransitioning)return true; __result=Task.FromResult(false); return false; }
    }
    [HarmonyPatch(typeof(NRestSiteRoom),"OnProceedButtonReleased")]
    private static class BlockProceed { private static bool Prefix()=>!IsTransitioning; }
    [HarmonyPatch(typeof(PotionModel),nameof(PotionModel.EnqueueManualUse))]
    private static class BlockNewPotions { private static bool Prefix()=>!IsTransitioning; }
}
