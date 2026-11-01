using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Combat;

namespace FrierenMod.Scripts.Mechanics;

// 本地等待状态不参与玩法；客户端请求的原始 Action 不会执行，故按请求编号接收同步动作的结果。
public static class MagicDeckNetwork
{
    private sealed class Pending
    {
        public uint Next;
        public readonly Dictionary<uint, TaskCompletionSource<bool>> Requests = [];
    }
    private static readonly ConditionalWeakTable<Player, Pending> PendingByPlayer = new();
    private sealed class ShopResults
    {
        public readonly Queue<bool> Results=[];
        public readonly Queue<TaskCompletionSource<bool>> Waiters=[];
    }
    private static readonly ConditionalWeakTable<Player,ShopResults> Shops=new();
    public static Task<bool> WaitForShop(Player player)
    {
        var shop=Shops.GetOrCreateValue(player);
        if(shop.Results.TryDequeue(out bool result))return Task.FromResult(result);
        var completion=new TaskCompletionSource<bool>();shop.Waiters.Enqueue(completion);return completion.Task;
    }
    public static void CompleteShop(Player player,bool result)
    {
        if(LocalContext.IsMe(player))return;
        var shop=Shops.GetOrCreateValue(player);
        if(shop.Waiters.TryDequeue(out var completion))completion.TrySetResult(result);
        else shop.Results.Enqueue(result);
    }
    public static string Snapshot(Player player) => Convert.ToHexString(SHA256.HashData(
        Encoding.UTF8.GetBytes(MagicDeckStore.Read(player).ToJson())));
    public static Task<bool> Send(Player player, NetMagicDeckAction data) => SendAction(player, request => {
        data.Request = request; return new MagicDeckAction(player, data);
    });
    public static Task<bool> SendCommission(Player player, NetCommissionAction data) => SendAction(player, request => {
        data.Request = request; data.InCombat = CombatManager.Instance.IsInProgress; data.Floor = player.RunState.TotalFloor;
        return new CommissionAction(player, data);
    });
    private static async Task<bool> SendAction(Player player, Func<uint, GameAction> create)
    {
        var manager = RunManager.Instance;
        if (!LocalContext.IsMe(player) || !ReferenceEquals(manager.DebugOnlyGetState(), player.RunState) ||
            manager.IsCleaningUp || !manager.NetService.IsConnected ||
            manager.NetService.Type == MegaCrit.Sts2.Core.Multiplayer.Game.NetGameType.Replay) return false;
        var pending = PendingByPlayer.GetOrCreateValue(player);
        uint request = ++pending.Next;
        var completion = new TaskCompletionSource<bool>();
        pending.Requests.Add(request, completion);
        void Disconnected(MegaCrit.Sts2.Core.Entities.Multiplayer.NetErrorInfo _) => completion.TrySetResult(false);
        manager.NetService.Disconnected += Disconnected;
        try {
            manager.ActionQueueSynchronizer.RequestEnqueue(create(request));
            return await completion.Task;
        }
        finally {
            manager.NetService.Disconnected -= Disconnected;
            pending.Requests.Remove(request);
        }
    }
    public static void Complete(Player player, uint request, bool result)
    {
        if (PendingByPlayer.TryGetValue(player, out var pending) && pending.Requests.TryGetValue(request, out var completion))
            completion.TrySetResult(result);
    }
    [HarmonyPatch(typeof(RunManager), nameof(RunManager.CleanUp))]
    private static class CancelPending
    {
        private static void Prefix(RunManager __instance)
        {
            if (__instance.DebugOnlyGetState() is not { } run) return;
            foreach (var player in run.Players) {
                if (PendingByPlayer.TryGetValue(player, out var pending))
                    foreach (var completion in pending.Requests.Values.ToArray()) completion.TrySetResult(false);
                if(Shops.TryGetValue(player,out var shop)) {
                    while(shop.Waiters.TryDequeue(out var completion))completion.TrySetResult(false);
                    shop.Results.Clear();
                }
            }
        }
    }
}
