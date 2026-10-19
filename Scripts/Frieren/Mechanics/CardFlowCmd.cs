using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace FrierenMod.Scripts.Mechanics;

public static class CardFlowCmd
{
    public static async Task SwapStrengthAndDexterity(PlayerChoiceContext context, Player player, CardModel? source)
    {
        int strength = player.Creature.GetPowerAmount<StrengthPower>();
        int dexterity = player.Creature.GetPowerAmount<DexterityPower>();
        // 一代先逐项减去旧值，再给予交换后的值；保留人工制品等减益拦截的触发顺序。
        if (strength != 0) await PowerCmd.Apply<StrengthPower>(context, player.Creature, -strength, player.Creature, source);
        if (dexterity != 0) await PowerCmd.Apply<DexterityPower>(context, player.Creature, -dexterity, player.Creature, source);
        if (dexterity != 0) await PowerCmd.Apply<StrengthPower>(context, player.Creature, dexterity, player.Creature, source);
        if (strength != 0) await PowerCmd.Apply<DexterityPower>(context, player.Creature, strength, player.Creature, source);
    }
    public static async Task MoveMana(Player player, PileType from, PileType to, int count)
    {
        foreach (var card in ManaCmd.InPile(player, from).Take(count).ToArray())
            await CardPileCmd.Add(card, to, to == PileType.Draw ? CardPilePosition.Random : CardPilePosition.Bottom);
    }

    public static async Task Generate<T>(Player player, PileType pile, bool upgraded = false) where T : CardModel
    {
        var card = player.Creature.CombatState!.CreateCard<T>(player);
        if (upgraded) card.UpgradeInternal();
        await CardPileCmd.AddGeneratedCardToCombat(card, pile, player,
            pile == PileType.Draw ? CardPilePosition.Random : CardPilePosition.Bottom);
    }

    public static async Task<IEnumerable<CardModel>> Choose(PlayerChoiceContext context, CardModel source, PileType pile, int count)
    {
        var cards = CardPile.Get(pile, source.Owner)!.Cards.Where(c => c != source).ToArray();
        count = Math.Min(count, cards.Length);
        if (count == 0) return [];
        if (count == cards.Length) return cards;
        var prefs = new CardSelectorPrefs(new LocString("frieren", "CARD_SELECT"), count);
        if (pile == PileType.Hand)
            return await CardSelectCmd.FromHand(context, source.Owner, prefs, c => c != source, source);
        return await CardSelectCmd.FromSimpleGrid(context, cards, source.Owner,
            prefs);
    }
}
