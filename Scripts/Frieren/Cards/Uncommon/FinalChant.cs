using FrierenMod.Scripts.Mechanics;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace FrierenMod.Scripts.Cards;

public sealed class FinalChant() : FrierenCard(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self), IChantCard
{
    protected override bool IsPlayable => Owner != null && ChantCmd.Options(Owner, null).Count > 0;
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        await ChantCmd.Final(context, play);
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
