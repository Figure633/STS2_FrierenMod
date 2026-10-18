using FrierenMod.Core;
using FrierenMod.Scripts.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using FrierenMod.Scripts.Powers;

namespace FrierenMod.Scripts.Mechanics;

public static class ManaCmd
{
    public static int Total(Player owner) => Enum.GetValues<ManaSlot>().Sum(slot => InPile(owner, Pile(slot)).Count());
    public static PileType Pile(ManaSlot slot) => slot switch
    {
        ManaSlot.Draw => PileType.Draw,
        ManaSlot.Hand => PileType.Hand,
        ManaSlot.Discard => PileType.Discard,
        _ => throw new ArgumentOutOfRangeException(nameof(slot))
    };

    public static IEnumerable<Mana> InPile(Player owner, PileType pile) =>
        CardPile.Get(pile, owner)?.Cards.OfType<Mana>() ?? [];

    public static async Task Generate(Player owner, PileType pile, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (owner.Creature.CombatState == null || owner.Creature.GetPower<BanManaGainPower>() != null) return;
        for (int i = 0; i < count; i++)
        {
            var mana = owner.Creature.CombatState.CreateCard<Mana>(owner);
            await CardPileCmd.AddGeneratedCardToCombat(mana, pile, owner,
                pile == PileType.Draw ? CardPilePosition.Random : CardPilePosition.Bottom);
        }
    }

    public static async Task<bool> Spend(PlayerChoiceContext context, Player owner, ManaSlot slot, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        var cards = InPile(owner, Pile(slot)).Take(count).ToArray();
        if (cards.Length < count) return false;
        foreach (var card in cards) await CardCmd.Exhaust(context, card);
        return true;
    }
}
