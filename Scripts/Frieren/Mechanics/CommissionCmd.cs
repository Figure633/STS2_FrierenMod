using BaseLib.Utils;
using FrierenMod.Core;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Shared.Compatibility;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using System.Security.Cryptography;
using System.Text;

namespace FrierenMod.Scripts.Mechanics;

public static class CommissionCmd
{
    public static readonly SavedSpireField<Player, string> Data = new(() => "", "FrierenCommissions");
    private static readonly Type[] Spells = [typeof(DefensiveMagic), typeof(MoldSpell), typeof(OilSpell), typeof(FlowerFieldSpell)];
    private static readonly Type[] Potions = [typeof(FirePotion), typeof(BlockPotion), typeof(StrengthPotion), typeof(EnergyPotion)];
    private static readonly Type[] Relics = [typeof(Anchor), typeof(BagOfPreparation), typeof(Lantern), typeof(Vajra), typeof(OddlySmoothStone)];
    private static readonly HashSet<Player> Pending = [];
    public static bool IsBusy(Player player) => Pending.Contains(player);
    public static CommissionState Read(Player player)
    {
        var json = Data.Get(player);
        return string.IsNullOrEmpty(json) ? new() : CommissionState.FromJson(json);
    }
    public static void Write(Player player, CommissionState state) => Data.Set(player, state.ToJson());
    public static CommissionState State(Player player)
    {
        var state = Read(player);
        if (player.Character is not Frieren) return state;
        int act = player.RunState.CurrentActIndex;
        if (state.GeneratedActs.Contains(act)) return state;
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes($"frieren-commissions-v1/{player.RunState.Rng.StringSeed}/{player.NetId}/{act}"));
        var rng = GameApi.CreateRng(BitConverter.ToUInt32(hash, 0));
        // 不依赖生成时的瞬时库存；旧局两端首次查看的时机不同也必须生成相同委托。
        if (state.EnsureAct(act, rng.NextInt, Spells.Select(t => t.Name).ToArray(), MagicPropRules.All.Select(p => p.Id).ToArray(),
                Potions.Select(t => t.Name).ToArray(), Relics.Select(t => t.Name).ToArray())) Write(player, state);
        return state;
    }
    public static CardModel Spell(string id) => ModelDb.GetById<CardModel>(ModelDb.GetId(Spells.Single(t => t.Name == id)));
    public static RelicModel Relic(string id) => ModelDb.GetById<RelicModel>(ModelDb.GetId(Relics.Single(t => t.Name == id)));
    public static PotionModel Potion(string id) => ModelDb.GetById<PotionModel>(ModelDb.GetId(Potions.Single(t => t.Name == id)));
    public static bool OwnsSpell(Player player, string id) => player.Deck.Cards.Any(c => c.Id == Spell(id).Id);
    public static int Floor(Player player) => Math.Max(1, player.RunState is RunState state ? state.ActFloor : player.RunState.MapPointHistory.ElementAtOrDefault(player.RunState.CurrentActIndex)?.Count ?? 1);
    public static bool CanAct(Player player, bool input = true)
    {
        var manager = RunManager.Instance; var combat = CombatManager.Instance;
        return player.Character is Frieren && ReferenceEquals(manager.DebugOnlyGetState(), player.RunState) && !manager.IsCleaningUp &&
            !player.Creature.IsDead && !combat.IsStarting && !combat.IsEnding && (!input || !MagicPropCmd.IsBusy(player)) &&
            (!input || (!Pending.Contains(player) && !manager.NetService.IsGameLoading && LocalContext.IsMe(player) &&
                !manager.ActionExecutor.IsRunning && MagicBookUiAllowsInput())) &&
            (!combat.IsInProgress || (!combat.PlayerActionsDisabled && !combat.IsEnemyTurnStarted &&
                !combat.EndingPlayerTurnPhaseOne && !combat.EndingPlayerTurnPhaseTwo &&
                (player.RunState.Players.Count == 1 || player.PlayerCombatState?.Phase == PlayerTurnPhase.Play)));
    }
    private static bool MagicBookUiAllowsInput() => MagicPropCmd.AllowsUseOver(NOverlayStack.Instance?.Peek());
    public static bool CanReceive(Player player, CommissionReward reward) => reward.Kind != CommissionRewardKind.Potion ||
        player.HasOpenPotionSlots && Hook.ShouldProcurePotion(player.RunState, player.Creature.CombatState, Potion(reward.Item).ToMutable(), player);
    public static async Task<bool> Send(Player player, int id, CommissionOperation operation)
    {
        if (!CanAct(player) || !Pending.Add(player)) return false;
        try {
            int revision = State(player).Revision;
            return await MagicDeckNetwork.SendCommission(player, new() { Quest = id, Operation = operation, Revision = revision });
        } finally { Pending.Remove(player); }
    }
    public static async Task<bool> Execute(Player player, int id, CommissionOperation operation, int revision)
    {
        if (!CanAct(player, false) || !Enum.IsDefined(operation)) return false;
        var state = State(player);
        if (revision != state.Revision) return false;
        if (operation == CommissionOperation.Accept) {
            if (!state.Accept(id, player.RunState.CurrentActIndex, Floor(player))) return false;
            Write(player, state); return true;
        }
        if (operation == CommissionOperation.Abandon) {
            if (!state.Abandon(id)) return false;
            Write(player, state); return true;
        }
        var quest = state.Quests.SingleOrDefault(q => q.Id == id);
        if (quest == null || !CommissionState.Ready(quest, spell => OwnsSpell(player, spell))) return false;
        var reward = quest.Reward;
        PotionModel? potion = reward.Kind == CommissionRewardKind.Potion ? Potion(reward.Item).ToMutable() : null;
        if (!CanReceive(player, reward)) return false;
        // 原生动作串行执行，先锁定领取状态；状态与奖励归属同一玩家及原生存档检查点。
        if (!state.Claim(id, spell => OwnsSpell(player, spell))) return false;
        Write(player, state);
        switch (reward.Kind) {
            case CommissionRewardKind.Gold: await PlayerCmd.GainGold(reward.Amount, player); break;
            case CommissionRewardKind.Prop:
                var deck = MagicDeckStore.Read(player); deck.AddProp(reward.Item); MagicDeckStore.Write(player, deck); break;
            case CommissionRewardKind.Relic:
                var relic = Relic(reward.Item);
                if (player.Relics.Any(r => r.Id == relic.Id)) await PlayerCmd.GainGold(reward.Amount, player);
                else await RelicCmd.Obtain(relic.ToMutable(), player);
                break;
            case CommissionRewardKind.Potion:
                var result = await PotionCmd.TryToProcure(potion!, player);
                if (!result.success) { state.Quests.Single(q => q.Id == id).Status = CommissionStatus.Accepted; state.Revision++; Write(player, state); return false; }
                break;
        }
        return true;
    }

    public static void MonsterDied(ICombatState? combat, Creature creature, bool prevented)
    {
        if (combat == null || prevented || creature.IsAlive || creature.Side != CombatSide.Enemy ||
            combat.RunState.CurrentRoom is not CombatRoom { RoomType: RoomType.Monster } room ||
            !room.Encounter.HaveMonstersBeenGenerated) return;
        int originalIndex = room.Encounter.MonstersWithSlots.ToList().FindIndex(pair =>
            ReferenceEquals(pair.Item1, creature.Monster) || pair.Item2 != null && pair.Item2 == creature.SlotName && pair.Item1.Id == creature.Monster?.Id);
        if (originalIndex < 0) return;
        foreach (var player in combat.Players.Where(p => p.Character is Frieren && p.Creature.IsAlive)) {
            var state = State(player);
            if (state.Record($"{player.RunState.CurrentActIndex}/{player.RunState.TotalFloor}/{room.ModelId}/original-{originalIndex}", CommissionGoal.Monsters)) Write(player, state);
        }
    }
    public static void Victory(IRunState run, CombatRoom room)
    {
        if (room.RoomType != RoomType.Elite) return;
        foreach (var player in room.CombatState.Players.Where(p => p.Character is Frieren && p.Creature.IsAlive)) {
            var state = State(player);
            if (state.Record($"elite/{run.CurrentActIndex}/{run.TotalFloor}/{room.ModelId}", CommissionGoal.Elites)) Write(player, state);
        }
    }
    public static void Ensure(IRunState run) { foreach (var player in run.Players.Where(p => p.Character is Frieren)) State(player); }
}

[HarmonyPatch(typeof(Hook), nameof(Hook.AfterDeath))]
internal static class CommissionDeathHook
{
    private static void Postfix(ICombatState? combatState, Creature creature, bool wasRemovalPrevented, ref Task __result) => __result = After(__result, combatState, creature, wasRemovalPrevented);
    private static async Task After(Task original, ICombatState? combat, Creature creature, bool prevented) { await original; CommissionCmd.MonsterDied(combat, creature, prevented); }
}
[HarmonyPatch(typeof(Hook), nameof(Hook.AfterCombatVictory))]
internal static class CommissionVictoryHook
{
    private static void Postfix(IRunState runState, CombatRoom room, ref Task __result) => __result = After(__result, runState, room);
    private static async Task After(Task original, IRunState run, CombatRoom room) { await original; CommissionCmd.Victory(run, room); }
}
[HarmonyPatch(typeof(Hook), nameof(Hook.AfterRoomEntered))]
internal static class CommissionRoomHook
{
    private static void Postfix(IRunState runState, ref Task __result) => __result = After(__result, runState);
    private static async Task After(Task original, IRunState run) { await original; CommissionCmd.Ensure(run); }
}
[HarmonyPatch(typeof(Hook), nameof(Hook.AfterActEntered))]
internal static class CommissionActHook
{
    private static void Postfix(IRunState runState, ref Task __result) => __result = After(__result, runState);
    private static async Task After(Task original, IRunState run) { await original; CommissionCmd.Ensure(run); }
}
