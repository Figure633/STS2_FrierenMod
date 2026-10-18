using FrierenMod.Scripts.Mechanics;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace FrierenMod.Scripts.Cards;

public sealed class PreciseChant() : FrierenCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self), IChantCard
{
    protected override bool IsPlayable => Owner != null && ChantCmd.Options(Owner, null).Count > 0;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        await ChantCmd.Choose(context, play, null);
    protected override void OnUpgrade() => RemoveKeyword(CardKeyword.Exhaust);
}
