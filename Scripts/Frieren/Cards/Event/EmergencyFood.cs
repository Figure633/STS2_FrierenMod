using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace FrierenMod.Scripts.Cards;

public sealed class EmergencyFood() : FrierenCard(0, CardType.Skill, CardRarity.Event, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new HealVar(5)];
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain, CardKeyword.Exhaust];
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) => CreatureCmd.Heal(Owner.Creature, DynamicVars.Heal.BaseValue);
    protected override void OnUpgrade() => DynamicVars.Heal.UpgradeValueBy(2);
}
