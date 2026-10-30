using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace FrierenMod.Scripts.Cards;

public sealed class ManaWish() : FrierenCard(0, CardType.Skill, CardRarity.Token, TargetType.Self, false)
{
    protected override bool IsPlayable => false;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Mana", 6)];
    protected override void OnUpgrade() => DynamicVars["Mana"].UpgradeValueBy(2);
}
