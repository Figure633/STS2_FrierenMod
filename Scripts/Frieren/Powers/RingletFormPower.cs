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

public sealed class RingletFormPower : FrierenPower
{
    public override async Task AfterCardExhausted(PlayerChoiceContext context, CardModel card, bool causedByEthereal)
    {
        if (card.Owner == Owner.Player && ManaCmd.IsMana(card))
            await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null);
    }
}
