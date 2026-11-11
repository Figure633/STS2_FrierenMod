using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;

namespace FrierenMod.Scripts.Relics;

// 一代：前两个非 Boss 宝箱各多一件遗物，75% 普通、25% 罕见。
public sealed class Matryoshka : CountingFrierenRelic
{
    public override RelicRarity Rarity => RelicRarity.Event;
    public override Task AfterObtained() { Counter = 2; return Task.CompletedTask; }
    public override bool TryModifyRewards(Player player, List<Reward> rewards, AbstractRoom? room)
    {
        if (player != Owner || room is not TreasureRoom || Counter <= 0) return false;
        Counter--; Flash();
        var rarity = Owner.PlayerRng.Rewards.NextInt(100) < 75 ? RelicRarity.Common : RelicRarity.Uncommon;
        var reward = new RelicReward(rarity,Owner); reward.Populate(); rewards.Add(reward);
        if (Counter == 0) Status = RelicStatus.Disabled;
        return true;
    }
}
