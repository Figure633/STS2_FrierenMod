using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Models;
using FrierenMod.Scripts.Mechanics;

namespace FrierenMod.Scripts.Powers;

public sealed class RecyclePower : FrierenPower
{
    public override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? clonedBy)
    {
        if (card.Owner == Owner.Player && ManaCmd.IsMana(card) && oldPileType is PileType.Draw or PileType.Hand && card.Pile?.Type == PileType.Discard)
            await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null, fast: true);
    }
}
