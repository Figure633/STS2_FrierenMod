using BaseLib.Abstracts;
using FrierenMod.Scripts.Mechanics;
using HarmonyLib;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;

namespace FrierenMod.Scripts.Rewards;

public sealed class DragonBallRewardHooks() : CustomSingletonModel(HookType.Run)
{
    public override Task AfterCombatVictory(CombatRoom room)
    {
        if (room.IsPreFinished || !room.Encounter.ShouldGiveRewards || room.CombatState.EscapedCreatures.Any(c => c.IsMonster)) return Task.CompletedTask;
        foreach (var player in room.CombatState.RunState.Players) {
            if (DragonBallStore.Next(player) == 0) continue;
            var state = DragonBallStore.Read(player);
            string key = CommissionCmd.BattleKey(player.RunState,room);
            if (state.LastRewardRoom == key) continue;
            state.LastRewardRoom = key;
            bool guaranteed = room.RoomType == RoomType.Boss && player.RunState.CurrentActIndex < 2;
            bool drop = guaranteed || player.PlayerRng.Rewards.NextInt(100) < state.DropChance;
            if (!guaranteed) state.DropChance = drop ? 10 : Math.Min(100,state.DropChance + 5);
            DragonBallStore.Write(player,state);
            if (drop) { var reward = new DragonBallReward(player); reward.Populate(); room.AddExtraReward(player,reward); }
        }
        return Task.CompletedTask;
    }

    [HarmonyPatch(typeof(RewardsSet), "GenerateRewardsFor")]
    private static class ChestReward
    {
        private static void Postfix(RewardsSet __instance, AbstractRoom room, List<Reward> __result)
        {
            if (room is not TreasureRoom || DragonBallStore.Next(__instance.Player) == 0 || __result.OfType<DragonBallReward>().Any()) return;
            var reward = new DragonBallReward(__instance.Player); reward.Populate(); __result.Add(reward);
        }
    }
}
