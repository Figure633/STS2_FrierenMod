using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace FrierenMod.Scripts.Mechanics;

public static class SynchroCmd
{
    public static int CountThisTurn(Player player) => CombatManager.Instance.History.CardPlaysStarted.Count(e =>
        e.Actor == player.Creature && e.HappenedThisTurn(player.Creature.CombatState) && ManaCmd.IsMana(e.CardPlay.Card));

    public static int Consecutive(Player player) => CombatManager.Instance.History.CardPlaysStarted
        .Where(e => e.Actor == player.Creature && e.HappenedThisTurn(player.Creature.CombatState))
        .Reverse().TakeWhile(e => ManaCmd.IsMana(e.CardPlay.Card)).Count();

    public static async Task Play(PlayerChoiceContext context, CardModel card)
    {
        var owner = card.Owner;
        bool accelerated = owner.Creature.HasPower<AccelerateFlowPower>();
        bool thunder = owner.Creature.HasPower<ThunderPower>();
        if (thunder)
            await DamageCmd.Attack(15).FromCard(card).TargetingRandomOpponents(owner.Creature.CombatState!).Execute(context);
        if (accelerated || !thunder) await CardPileCmd.Draw(context, accelerated ? 2 : 1, owner);
        foreach (var fire in owner.PlayerCombatState!.DiscardPile.Cards.OfType<LittleFire>().ToArray())
            await CardPileCmd.Add(fire, PileType.Hand);
        if (Consecutive(owner) >= 3)
            foreach (var mana in owner.PlayerCombatState.DrawPile.Cards.OfType<HighDensityMana>().ToArray())
                await CardPileCmd.Add(mana, PileType.Hand);
    }
}
