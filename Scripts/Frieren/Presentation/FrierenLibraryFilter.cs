using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Cards;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using MegaCrit.Sts2.Core.Saves;

namespace FrierenMod.Scripts.Presentation;

// 保留卡池、品质和存档 ID；因子、魔具与普通衍生牌均归入原生杂项分类。
[HarmonyPatch(typeof(NCardLibrary), nameof(NCardLibrary._Ready))]
public static class FrierenLibraryFilter
{
    public static bool IsMagicItem(CardModel card) => card is FactorCard or MagicPropCard;

    public static bool IsCharacterCard(CardModel card) => !IsMagicItem(card) && card.Rarity is not
        (CardRarity.Token or CardRarity.Status or CardRarity.Curse or CardRarity.Event or CardRarity.Quest);

    [HarmonyPostfix]
    private static void Apply(Dictionary<NCardPoolFilter, Func<CardModel, bool>> ____poolFilters,
        Dictionary<CharacterModel, NCardPoolFilter> ____cardPoolFilters, NCardPoolFilter ____miscPoolFilter)
    {
        var misc = ____poolFilters[____miscPoolFilter];
        ____poolFilters[____miscPoolFilter] = card => misc(card) || IsMagicItem(card);
        if (!____cardPoolFilters.TryGetValue(ModelDb.Character<Frieren>(), out var filter)) return;
        var original = ____poolFilters[filter];
        ____poolFilters[filter] = card => original(card) && IsCharacterCard(card);
    }
}

// 因子不进入普通奖励解锁池，百科需单独允许查看；沿用 BaseLib 的默认已见机制。
[HarmonyPatch(typeof(NCardLibraryGrid), nameof(NCardLibraryGrid.RefreshVisibility))]
public static class FrierenLibraryVisibility
{
    [HarmonyPostfix]
    private static void IncludeMagicItems(HashSet<CardModel> ____unlockedCards, HashSet<ModelId> ____seenCards)
    {
        foreach (var card in ModelDb.AllCards.Where(card => card.ShouldShowInCardLibrary && FrierenLibraryFilter.IsMagicItem(card)))
        {
            ____unlockedCards.Add(card);
            ____seenCards.Add(card.Id);
            // 原生搜索和放大查看也依赖发现记录，避免出现可见却无法检索或点击的卡。
            if (!SaveManager.Instance.Progress.DiscoveredCards.Contains(card.Id))
                SaveManager.Instance.MarkCardAsSeen(card);
        }
    }
}
