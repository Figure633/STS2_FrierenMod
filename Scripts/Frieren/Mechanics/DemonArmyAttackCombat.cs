using System.Runtime.CompilerServices;
using FrierenMod.Scripts.Events;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;

namespace FrierenMod.Scripts.Mechanics;

public static class DemonArmyAttackCombat
{
    private static readonly ConditionalWeakTable<ICombatState, HashSet<ulong>> Granted = new();
    public static bool IsEventBattle(CombatRoom room) => room.ParentEventId == ModelDb.Event<DemonArmyAttack>().Id;

    public static List<Reward> Rewards(Player player, CombatRoom room) => room.Encounter.RoomType switch {
        RoomType.Boss => [new RelicReward(RelicRarity.Rare, player), new PotionReward(player), new PotionReward(player), new GoldReward(150, player)],
        RoomType.Elite => [new RelicReward(player), new PotionReward(player), new GoldReward(50, player)],
        _ => [new RelicReward(RelicRarity.Common, player), new GoldReward(50, player)]
    };

    public static async Task Start(ICombatState? combat)
    {
        if (combat?.RunState.CurrentRoom is not CombatRoom room || !IsEventBattle(room) || !ReferenceEquals(room.CombatState, combat)) return;
        var granted = Granted.GetOrCreateValue(combat);
        var context = new ThrowingPlayerChoiceContext();
        foreach (var player in combat.Players.Where(p => p.Creature.IsAlive)) {
            if (!granted.Add(player.NetId)) continue;
            switch (room.Encounter.RoomType) {
                case RoomType.Boss:
                    await PowerCmd.Apply<BarricadePower>(context, player.Creature, 1, player.Creature, null);
                    await PowerCmd.Apply<PlatingPower>(context, player.Creature, 8, player.Creature, null);
                    break;
                case RoomType.Elite:
                    await PowerCmd.Apply<AfterimagePower>(context, player.Creature, 1, player.Creature, null);
                    break;
                default:
                    await PowerCmd.Apply<ArtifactPower>(context, player.Creature, 1, player.Creature, null);
                    break;
            }
        }
    }

    // 保留原版Boss模型、槽位、背景和AI；事件战斗不触发第三幕Boss的终局与过幕流程。
    [HarmonyPatch(typeof(CombatRoom), nameof(CombatRoom.RoomType), MethodType.Getter)]
    private static class EventRoomType
    {
        private static void Postfix(CombatRoom __instance, ref RoomType __result)
        {
            if (__result == RoomType.Boss && IsEventBattle(__instance)) __result = RoomType.Monster;
        }
    }

    [HarmonyPatch(typeof(Hook), nameof(Hook.BeforeCombatStart))]
    private static class OpeningBuffs
    {
        private static void Postfix(ICombatState? combatState, ref Task __result) => __result = After(__result, combatState);
        private static async Task After(Task original, ICombatState? combat) { await original; await Start(combat); }
    }

    // 覆盖该事件的默认战利品，领取、药水栏处理、遗物抽取与存档继续使用原生流程。
    [HarmonyPatch(typeof(RewardsSet), "GenerateRewardsFor")]
    private static class Bounties
    {
        private static bool Prefix(Player player, AbstractRoom room, ref List<Reward> __result)
        {
            if (room is not CombatRoom combat || !IsEventBattle(combat)) return true;
            __result = Rewards(player, combat);
            return false;
        }
    }
}
