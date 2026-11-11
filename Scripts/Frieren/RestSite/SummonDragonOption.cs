using BaseLib.Abstracts;
using FrierenMod.Scripts.Mechanics;
using MegaCrit.Sts2.Core.Entities.Players;

namespace FrierenMod.Scripts.RestSite;

public sealed class SummonDragonOption(Player owner) : CustomRestSiteOption(owner)
{
    public override string OptionId => "FRIEREN_SUMMON_DRAGON";
    public override string CustomIconPath => "res://FrierenMod/images/events/SummonDragon.png";
    public override bool IsEnabled => DragonBallStore.CanSummon(Owner) && !DragonSummoning.IsTransitioning;
    public override Task<bool> OnSelect()
    {
        if (IsEnabled) DragonSummoning.Begin(Owner);
        return Task.FromResult(false); // 原生同步器不消耗休息次数。
    }
}
