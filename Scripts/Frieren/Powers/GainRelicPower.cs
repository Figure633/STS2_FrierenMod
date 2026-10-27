using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;

namespace FrierenMod.Scripts.Powers;

public sealed class GainRelicPower : FrierenPower
{
    private bool _granted;
    // 玩家战斗能力在 AfterCombatVictory 前已清空，必须在战斗结束钩子存入奖励。
    public override Task AfterCombatEnd(CombatRoom room)
    {
        if (_granted || Owner.Player == null || !Owner.IsAlive) return Task.CompletedTask;
        _granted = true;
        for (int i = 0; i < Amount; i++) room.AddExtraReward(Owner.Player!, new RelicReward(Owner.Player!));
        return Task.CompletedTask;
    }
}
