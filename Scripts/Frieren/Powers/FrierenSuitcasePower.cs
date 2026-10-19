using BaseLib.Hooks;
using BaseLib.Patches.Hooks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace FrierenMod.Scripts.Powers;

public sealed class FrierenSuitcasePower : FrierenPower, IMaxHandSizeModifier
{
    public int ModifyMaxHandSize(Player player, int current) => player == Owner.Player ? Math.Max(current, 13) : current;
    public override async Task AfterCardPlayedLate(PlayerChoiceContext context, CardPlay play)
    {
        if (play.Card.Owner == Owner.Player && Owner.Player.PlayerCombatState!.Hand.Cards.Count >= MaxHandSizePatch.GetMaxHandSize(Owner.Player, CardPile.MaxCardsInHand))
            await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null);
    }
}
