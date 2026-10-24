using FrierenMod.Scripts.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace FrierenMod.Scripts.Powers;

public sealed class MimicPower : FrierenPower
{
    public override async Task AfterCardPlayedLate(PlayerChoiceContext context, CardPlay play)
    {
        if (Owner.IsDead || play.Card.Owner.Creature.CombatState != Owner.CombatState) return;
        Flash();
        if (Amount > 1) { await PowerCmd.Decrement(this); return; }
        Monsters.Mimic.Chew(Owner);
        await PowerCmd.Remove(this);
        foreach (var player in Owner.CombatState!.Players)
        {
            await PowerCmd.Apply<DarkPower>(context, player.Creature, 1, Owner, null);
            var state = player.PlayerCombatState!;
            await CardPileCmd.RemoveFromCombat(state.DrawPile.Cards.Concat(state.Hand.Cards).Concat(state.DiscardPile.Cards).OfType<Mana>().ToArray());
            PlayerCmd.EndTurn(player, canBackOut: false);
        }
    }
}
