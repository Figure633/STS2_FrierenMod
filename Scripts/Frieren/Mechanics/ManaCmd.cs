using FrierenMod.Core;
using FrierenMod.Scripts.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using FrierenMod.Scripts.Powers;
using FrierenMod.Scripts.Presentation;
using MegaCrit.Sts2.Core.Models;

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

    public static bool IsMana(CardModel card) => card is IManaCard;
    public static IEnumerable<CardModel> InPile(Player owner, PileType pile) =>
        CardPile.Get(pile, owner)?.Cards.Where(IsMana) ?? [];

    public static async Task Generate(Player owner, PileType pile, int count, bool ignoreBan = false, float previewTime = 1.2f)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (owner.Creature.CombatState == null || (!ignoreBan && owner.Creature.GetPower<BanManaGainPower>() != null)) return;
        for (int i = 0; i < count; i++)
        {
            var mana = owner.Creature.CombatState.CreateCard<Mana>(owner);
            await CardFlowCmd.AddGenerated(mana, pile, owner, previewTime);
        }
    }

    public static async Task<bool> Spend(PlayerChoiceContext context, Player owner, ManaSlot slot, int count, bool fastAnimation = false)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        var cards = InPile(owner, Pile(slot)).Take(count).ToArray();
        if (cards.Length < count) return false;
        using var animation = fastAnimation ? ManaExhaustAnimation.Begin(cards) : null;
        foreach (var card in cards) await CardCmd.Exhaust(context, card);
        return true;
    }
}
