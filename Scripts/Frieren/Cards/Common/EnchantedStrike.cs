using BaseLib.Utils;
using FrierenMod.Scripts.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace FrierenMod.Scripts.Cards;

public sealed class EnchantedStrike() : FrierenCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];
    protected override string ArtName => nameof(StrikeFrieren);
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CalculationBaseVar(8), new ExtraDamageVar(3),
        new CalculatedDamageVar(ValueProp.Move).WithMultiplier((card, _) => card.IsMutable && card.Owner?.PlayerCombatState != null ? ManaCmd.InPile(card.Owner, PileType.Hand).Count() : 0)];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        await DamageCmd.Attack(DynamicVars.CalculatedDamage).FromCardCompatibility(this, play).Targeting(play.Target!).Execute(context);
    protected override void OnUpgrade() => DynamicVars.ExtraDamage.UpgradeValueBy(1);
}
