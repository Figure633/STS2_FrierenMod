using MegaCrit.Sts2.Core.Entities.Cards;

namespace FrierenMod.Scripts.Cards;

public sealed class Snuggle2() : FrierenCard(0, CardType.Skill, CardRarity.Token, TargetType.Self, false), IGeneratedOnlyCard
{
    protected override bool IsPlayable => false;
    public override int MaxUpgradeLevel => 0;
}
