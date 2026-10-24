using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace FrierenMod.Scripts.Cards;

public sealed class Snuggle1() : FrierenCard(0, CardType.Skill, CardRarity.Token, TargetType.Self, false), IGeneratedOnlyCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Amount", 1)];
    protected override bool IsPlayable => false;
    public override int MaxUpgradeLevel => 0;
}
