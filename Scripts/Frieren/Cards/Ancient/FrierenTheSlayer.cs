using FrierenMod.Scripts.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace FrierenMod.Scripts.Cards;

public sealed class FrierenTheSlayer() : FrierenCard(1, CardType.Power, CardRarity.Ancient, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Innate];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var power = await PowerCmd.Apply<FrierenTheSlayerPower>(context, Owner.Creature, 1, Owner.Creature, this);
        if (power != null && IsUpgraded) power.UpgradedChoices++;
    }
    protected override void OnUpgrade() { }
}
