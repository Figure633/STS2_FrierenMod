using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Mechanics;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace FrierenMod.Scripts.Powers;

public sealed class OutpouringPower : TurnOnlyPower
{
    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay play)
    {
        if (play.Card.Owner == Owner.Player && ManaCmd.IsMana(play.Card))
            await PowerCmd.Apply<StrengthPower>(context, Owner, Amount, Owner, play.Card);
    }
}
