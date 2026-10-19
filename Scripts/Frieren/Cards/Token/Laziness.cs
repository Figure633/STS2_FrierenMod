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
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace FrierenMod.Scripts.Cards;

public sealed class Laziness() : FrierenCard(0, CardType.Status, CardRarity.Token, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable, CardKeyword.Exhaust];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await Task.CompletedTask;
    }
    public override int MaxUpgradeLevel => 0;
    public override async Task BeforeSideTurnEnd(PlayerChoiceContext context, CombatSide side, IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner.Creature) && Pile?.Type == PileType.Hand)
            await CardFlowCmd.Generate<Laziness>(Owner, PileType.Discard);
    }
    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay play)
    {
        if (play.Card.Owner == Owner && ChantCmd.IsChant(play.Card) && Pile?.Type == PileType.Hand)
            await CardCmd.Exhaust(context, this);
    }
}
