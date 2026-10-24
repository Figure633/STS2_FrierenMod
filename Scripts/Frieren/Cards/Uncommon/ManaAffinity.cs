using FrierenMod.Scripts.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace FrierenMod.Scripts.Cards;

public sealed class ManaAffinity() : FrierenCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        PowerCmd.Apply<ManaAffinityPower>(context, Owner.Creature, 1, Owner.Creature, this);

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
