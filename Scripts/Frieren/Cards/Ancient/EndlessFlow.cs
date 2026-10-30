using BaseLib.Utils;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace FrierenMod.Scripts.Cards;

public sealed class EndlessFlow() : FrierenCard(0, CardType.Attack, CardRarity.Ancient, TargetType.AllEnemies)
{
    protected override string ArtName => nameof(EndlessFlow);
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CalculationBaseVar(0), new ExtraDamageVar(1),
        new CalculatedDamageVar(ValueProp.Move).WithMultiplier((card, _) => card.IsMutable && card.Owner?.Creature.CombatState != null
            ? ManaCmd.Total(card.Owner) + (card.Owner.Creature.HasPower<BanManaGainPower>() ? 0 : 3) : 3)];

    protected override void AddExtraArgsToDescription(MegaCrit.Sts2.Core.Localization.LocString description)
    {
        base.AddExtraArgsToDescription(description);
        DynamicVars.CalculatedDamage.EnchantedValue = DynamicVars.CalculatedDamage.Calculate(null);
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        foreach (var pile in new[] { PileType.Draw, PileType.Hand, PileType.Discard })
            await ManaCmd.Generate(Owner, pile, 1);
        await DamageCmd.Attack(ManaCmd.Total(Owner)).FromCardCompatibility(this, play).TargetingAllOpponents(CombatState!).Execute(context);
    }

    protected override void OnUpgrade() => AddKeyword(CardKeyword.Innate);
}
