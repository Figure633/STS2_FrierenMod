using FrierenMod.Core;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace FrierenMod.Scripts.Cards;

public sealed class OrdinaryOffensiveMagic() : FrierenCard(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CalculationBaseVar(0), new ExtraDamageVar(2),
        new CalculatedDamageVar(ValueProp.Move).WithMultiplier((card, _) => ManaCmd.InPile(card.Owner, PileType.Exhaust).Count())];
    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        DynamicVars.CalculatedDamage.EnchantedValue = DynamicVars.CalculatedDamage.Calculate(null);
    }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        SfxCmd.Play("res://FrierenMod/audio/normal_attack.mp3");
        Presentation.FrierenAchievements.CheckDamage(this, play.Target, 2 * ManaCmd.InPile(Owner, PileType.Exhaust).Count(), 100, "4008");
        await DamageCmd.Attack(2 * ManaCmd.InPile(Owner, PileType.Exhaust).Count()).FromCard(this).Targeting(play.Target!).Execute(context);
    }
    protected override void OnUpgrade() { EnergyCost.UpgradeBy(-1); }
}
