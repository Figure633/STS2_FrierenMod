using BaseLib.Utils;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Encounters;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace FrierenMod.Scripts.Ancients;

public struct SerieVisitDecision : INetMessage, IPacketSerializable
{
    public string Seed;
    public bool FirstVisit;
    public bool ShouldBroadcast => false;
    public bool ShouldBuffer => true;
    public NetTransferMode Mode => NetTransferMode.Reliable;
    public LogLevel LogLevel => LogLevel.VeryDebug;
    public void Serialize(PacketWriter writer) { writer.WriteString(Seed); writer.WriteBool(FirstVisit); }
    public void Deserialize(PacketReader reader) { Seed=reader.ReadString(); FirstVisit=reader.ReadBool(); }
}

// 原生访问次数决定首遇；整局决策存档，联机只由房主读取本机进度。
public static class SerieFirstVisit
{
    public static readonly SavedSpireField<Player,int> Decision = new(()=>0,"FrierenSerieFirstVisit");
    private static INetGameService? _service;
    public static bool HasVisited => (SaveManager.Instance.Progress.GetStatsForAncient(ModelDb.AncientEvent<Serie>().Id)
        ?.GetVisitsAs(ModelDb.Character<Frieren>().Id) ?? 0) > 0;
    private static void Remember(IRunState run, bool first)
    { foreach(var player in run.Players) Decision.Set(player, first?2:1); }
    private static void Receive(SerieVisitDecision message, ulong sender)
    {
        if(_service?.Type!=NetGameType.Client || RunManager.Instance.DebugOnlyGetState() is not {} run ||
            message.Seed!=run.Rng.Seed.ToString() || !run.Players.Any(p=>p.NetId==sender)) return;
        Remember(run,message.FirstVisit);
    }
    public static async Task Ensure(IRunState run,int actIndex)
    {
        if(actIndex!=2 || !FrierenEncounterEligibility.IsFrierenRun(run)) return;
        var manager=RunManager.Instance;
        if(manager.NetService.Type!=NetGameType.Client) {
            // 升级前已走过第三幕先古的旧存档，不在读档时改写其既有先古。
            if(Decision.Get(run.Players[0])==0) {
                bool underway=run is RunState {CurrentActIndex:2} saved && saved.VisitedMapCoords.Count>0;
                Remember(run,!HasVisited && !underway);
            }
            if(manager.NetService.Type==NetGameType.Host)
                manager.NetService.SendMessage(new SerieVisitDecision{Seed=run.Rng.Seed.ToString(),FirstVisit=Decision.Get(run.Players[0])==2});
        } else {
            while(Decision.Get(run.Players[0])==0) {
                if(manager.IsCleaningUp || !ReferenceEquals(manager.DebugOnlyGetState(),run)) return;
                var tree=(SceneTree)Engine.GetMainLoop();
                await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
            }
        }
        if(Decision.Get(run.Players[0])==2 && AccessTools.Field(typeof(ActModel),"_rooms").GetValue(run.Acts[actIndex]) is RoomSet rooms)
            rooms.Ancient=ModelDb.AncientEvent<Serie>();
    }
    [HarmonyPatch(typeof(RunManager),"InitializeShared")]
    private static class Register
    {
        private static void Postfix(INetGameService netService)
        {
            _service?.UnregisterMessageHandler<SerieVisitDecision>(Receive);_service=netService;
            netService.RegisterMessageHandler<SerieVisitDecision>(Receive);
        }
    }
    [HarmonyPatch(typeof(Hook),nameof(Hook.AfterMapGenerated))]
    private static class BeforeMapDisplay
    {
        private static void Postfix(IRunState runState,int actIndex,ref Task __result)=>__result=Finish(__result,runState,actIndex);
        private static async Task Finish(Task original,IRunState run,int index) { await original;await Ensure(run,index); }
    }
}
