using BaseLib.Abstracts;
using System.Runtime.CompilerServices;
using FrierenMod.Core;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Mechanics;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;

namespace FrierenMod.Scripts.Rewards;

// 一代按角色在战后生成奖励；复用 BaseLib 跑局钩子，移除或替换圣徽也不影响掉落。
public sealed class MagicItemRewardHooks() : CustomSingletonModel(HookType.Run)
{
    private readonly ConditionalWeakTable<CombatRoom, HashSet<Player>> _resolved = new();

    public override Task AfterCombatVictory(CombatRoom room)
    {
        if (room.IsPreFinished) return Task.CompletedTask;
        foreach (var player in room.CombatState.RunState.Players.Where(p => p.Character is Frieren)) {
            if (!_resolved.GetOrCreateValue(room).Add(player)) continue;
            if (room.ExtraRewards.TryGetValue(player, out var rewards) && rewards.OfType<FactorReward>().Any()) continue;
            AddReward(player, room);
        }
        return Task.CompletedTask;
    }

    private static void AddReward(Player player, CombatRoom room)
    {
        var deck = MagicDeckStore.Read(player);
        // 一代 Boss 继承普通怪物房；普通战斗有敌人逃跑时不掉落，事件战斗仍按 40%。
        bool escaped = room.ParentEventId == null && room.CombatState.EscapedCreatures.Any(c => c.IsMonster);
        bool drop = MagicRewardRules.ShouldDrop(player.PlayerRng.Rewards.NextInt(100), deck.RewardPity,
            room.RoomType == RoomType.Elite, escaped);
        deck.RewardPity += drop ? -10 : 10;
        MagicDeckStore.Write(player, deck);
        if (!drop) return;
        var reward = new FactorReward(player) { Seed = player.PlayerRng.Rewards.NextInt() };
        reward.Populate(); room.AddExtraReward(player, reward);
    }
}
