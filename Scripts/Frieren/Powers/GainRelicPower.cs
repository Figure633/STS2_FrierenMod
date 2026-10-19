using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;

namespace FrierenMod.Scripts.Powers;

public sealed class GainRelicPower : FrierenPower
{
    public override Task AfterCombatVictory(CombatRoom room)
    {
        for (int i = 0; i < Amount; i++) room.AddExtraReward(Owner.Player!, new RelicReward(Owner.Player!));
        return Task.CompletedTask;
    }
}
