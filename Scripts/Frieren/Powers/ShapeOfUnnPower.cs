using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace FrierenMod.Scripts.Powers;

public sealed class ShapeOfUnnPower : FrierenPower, IChantListener
{
    public async Task AfterChant(PlayerChoiceContext context, CardPlay play)
    {
        if (play.Card.Owner.Creature == Owner && Owner.IsAlive) {
            await CreatureCmd.Heal(Owner, Amount);
            await PowerCmd.Remove(this);
        }
    }
}
