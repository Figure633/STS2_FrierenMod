using MegaCrit.Sts2.Core.Entities.Cards;

namespace FrierenMod.Scripts.Cards;

public sealed class StrongerWish() : FrierenCard(0, CardType.Skill, CardRarity.Token, TargetType.Self, false)
{
    public override int MaxUpgradeLevel => 0;
    protected override bool IsPlayable => false;
}
