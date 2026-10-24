using BaseLib.Abstracts;
using FrierenMod.Scripts.Monsters;
using FrierenMod.Scripts.Relics;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;

namespace FrierenMod.Scripts.Encounters;

public sealed class MimicEncounter() : CustomEncounterModel(RoomType.Elite, false)
{
    public override bool IsValidForAct(ActModel act) => false;
    public override IEnumerable<MonsterModel> AllPossibleMonsters => [ModelDb.Monster<Mimic>()];
    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() => [(ModelDb.Monster<Mimic>().ToMutable(), null)];
    public override bool HasScene => false;
    public static List<Reward> Rewards(Player player) => [new GoldReward(100, player),
        new RelicReward(ModelDb.Relic<MimicHead>().ToMutable(), player),
        player.GetRelic<FakeFlammeGrimoire>() != null ? new RelicReward(ModelDb.Relic<FlammeGrimoire>().ToMutable(), player) : new RelicReward(RelicRarity.Rare, player)];
}

// 原生事件战斗接口只支持追加奖励；在生成默认奖励的单一入口替换本战斗的基础奖励，仍经过原生领取、存档和奖励钩子。
[HarmonyPatch(typeof(RewardsSet), "GenerateRewardsFor")]
public static class MimicEncounterRewards
{
    [HarmonyPrefix]
    private static bool Prefix(Player player, AbstractRoom room, ref List<Reward> __result)
    {
        if (room is not CombatRoom { Encounter: MimicEncounter }) return true;
        __result = MimicEncounter.Rewards(player); return false;
    }
}
