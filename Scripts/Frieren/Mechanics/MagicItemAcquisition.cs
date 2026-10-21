using FrierenMod.Scripts.Cards;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace FrierenMod.Scripts.Mechanics;

// 因子与魔具的卡模型用于展示；实际获得时一律存入对应玩家的魔法牌组。
public static class MagicItemAcquisition
{
    private static bool IsMagicItem(CardModel card) => card is FactorCard or MagicPropCard;

    public static bool Receive(CardModel card)
    {
        if (!IsMagicItem(card) || card.HasBeenRemovedFromState) return false;
        var deck = MagicDeckStore.Read(card.Owner);
        if (card is FactorCard factor) deck.Add(factor.FactorId);
        else deck.AddProp(((MagicPropCard)card).PropId);
        MagicDeckStore.Write(card.Owner, deck);
        card.RemoveFromState();
        return true;
    }

    [HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.Add), new[] {
        typeof(IEnumerable<CardModel>), typeof(CardPile), typeof(CardPilePosition), typeof(AbstractModel), typeof(bool) })]
    private static class RedirectPileAdd
    {
        private static bool Prefix(IEnumerable<CardModel> cards, CardPile newPile, CardPilePosition position,
            AbstractModel? clonedBy, bool skipVisuals, ref Task<IReadOnlyList<CardPileAddResult>> __result)
        {
            var items = cards.ToArray();
            if (!items.Any(IsMagicItem)) return true;
            __result = Add(items, newPile, position, clonedBy, skipVisuals);
            return false;
        }

        private static async Task<IReadOnlyList<CardPileAddResult>> Add(CardModel[] cards, CardPile pile,
            CardPilePosition position, AbstractModel? clonedBy, bool skipVisuals)
        {
            var results = new List<CardPileAddResult>();
            foreach (var card in cards) {
                if (!IsMagicItem(card)) { results.Add(await CardPileCmd.Add(card, pile, position, clonedBy, skipVisuals)); continue; }
                var old = card.Pile;
                results.Add(new CardPileAddResult { cardAdded = card, oldPile = old, success = Receive(card) });
            }
            return results;
        }
    }

    // 原生控制台会先检查手牌及战斗状态；魔法物品需在这之前分流，战斗外同样可获得。
    [HarmonyPatch(typeof(CardConsoleCmd), nameof(CardConsoleCmd.Process))]
    private static class RedirectConsole
    {
        private static bool Prefix(Player? issuingPlayer, string[] args, ref CmdResult __result)
        {
            if (issuingPlayer == null || !RunManager.Instance.IsInProgress || args.Length == 0) return true;
            var model = ModelDb.AllCards.FirstOrDefault(c => c.Id.Entry.Equals(args[0], StringComparison.OrdinalIgnoreCase));
            if (model == null || !IsMagicItem(model)) return true;
            var card = model.ToMutable(); card.Owner = issuingPlayer;
            bool added = Receive(card);
            var text = new LocString("frieren", "MAGIC_ITEM_ADDED"); text.Add("Card", model.Title);
            __result = new CmdResult(added, text.GetFormattedText());
            return false;
        }
    }
}
