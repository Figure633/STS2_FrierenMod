using FrierenMod.Scripts.Characters;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Players;
using FrierenMod.Scripts.Mechanics;
using HarmonyLib;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace FrierenMod.Scripts.Events;

[HarmonyPatch(typeof(RunManager), "CreateRoom")]
public static class ReadingGrimoireEntrance
{
    // 0 未进入，1 已进入未出发，2 已出发；保证第三本选完及领取工具后的读档仍回到本事件。
    public static readonly SavedSpireField<Player, int> Stage = new(() => 0, "FrierenOpeningEvent");
    private static void Postfix(RunManager __instance, MapPointType mapPointType, ref AbstractRoom __result)
    {
        var run = __instance.DebugOnlyGetState();
        if (run == null || run.CurrentActIndex != 0 || run.Players.Count != 1 || mapPointType != MapPointType.Ancient ||
            __result is not EventRoom { CanonicalEvent: Neow } || run.Players[0].Character is not Frieren ||
            Stage.Get(run.Players[0]) == 2 ||
            (MagicDeckStore.Read(run.Players[0]).SetupCompleted && Stage.Get(run.Players[0]) != 1)) return;
        Stage.Set(run.Players[0], 1);
        __result = new EventRoom(ModelDb.Event<ReadingGrimoireEvent>());
    }

    [HarmonyPatch(typeof(ActModel), nameof(ActModel.MarkRoomVisited))]
    private static class PreserveRandomEventPool
    {
        // 前置事件不是地图中的一次普通事件；涅奥照常登记一次访问。
        private static bool Prefix(RoomType roomType) => roomType != RoomType.Event ||
            RunManager.Instance.DebugOnlyGetState()?.CurrentRoom is not EventRoom { CanonicalEvent: ReadingGrimoireEvent };
    }

    // 开局标记与实际房间必须一致；原生未解锁涅奥时不会生成这个先古。
    private static void EnsureFirstRunNeowRoom(RunManager manager)
    {
        var run = manager.DebugOnlyGetState();
        if (run is not { GameMode: GameMode.Standard, Players.Count: 1, Acts.Count: > 0 } ||
            run.Players[0].Character is not Frieren || !run.ExtraFields.StartedWithNeow) return;
        if (AccessTools.Field(typeof(ActModel), "_rooms").GetValue(run.Acts[0]) is RoomSet { HasAncient: false } rooms)
            rooms.Ancient = ModelDb.AncientEvent<Neow>();
    }

    [HarmonyPatch(typeof(RunManager), nameof(RunManager.GenerateRooms))]
    private static class GenerateFirstRunNeow
    {
        private static void Postfix(RunManager __instance) => EnsureFirstRunNeowRoom(__instance);
    }

    [HarmonyPatch(typeof(ActModel), nameof(ActModel.ValidateRoomsAfterLoad))]
    private static class RestoreFirstRunNeow
    {
        private static void Postfix(ActModel __instance)
        {
            var manager = RunManager.Instance;
            if (manager.DebugOnlyGetState() is { Acts.Count: > 0 } run && ReferenceEquals(run.Acts[0], __instance))
                EnsureFirstRunNeowRoom(manager);
        }
    }

    [HarmonyPatch(typeof(RunManager), "SetStartedWithNeowFlag")]
    private static class FrierenFirstRun
    {
        // 原版档案的首局会跳过涅奥；芙莉莲仍需先检查魔导书，再继续祝福。
        private static void Postfix(RunManager __instance)
        {
            var run = __instance.DebugOnlyGetState();
            if (run is { CurrentActIndex: 0, GameMode: GameMode.Standard } && run.Players.Count == 1 && run.Players[0].Character is Frieren)
                run.ExtraFields.StartedWithNeow = true;
        }
    }
}
