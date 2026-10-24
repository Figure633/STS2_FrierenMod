using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Cards;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Assets;

namespace FrierenMod.Scripts.Presentation;

// 只调整百科筛选入口，保留实际卡池、品质、类型和存档 ID。
[HarmonyPatch(typeof(NCardLibrary), nameof(NCardLibrary._Ready))]
public static class FrierenLibraryFilter
{
    public const string MagicFilterName = "FrierenMagicItemsPool";
    public static bool IsMagicItem(CardModel card) => card is FactorCard or MagicPropCard;

    public static bool IsCharacterCard(CardModel card) => !IsMagicItem(card) && card is not IGeneratedOnlyCard && card.Rarity is not
        (CardRarity.Token or CardRarity.Status or CardRarity.Curse or CardRarity.Event or CardRarity.Quest);

    // 在原版初始化前加按钮，BaseLib 的布局适配与原生焦点导航会一并处理它。
    [HarmonyPrefix]
    private static void AddMagicFilter(NCardLibrary __instance)
    {
        var misc = __instance.GetNode<NCardPoolFilter>("%MiscPool");
        if (misc.GetParent().HasNode(MagicFilterName)) return;
        var filter = PreloadManager.Cache.GetScene("res://scenes/screens/card_library/library_pool_toggle.tscn").Instantiate<NCardPoolFilter>();
        filter.Name = MagicFilterName;
        filter.Loc = new LocString("frieren", "LIBRARY_MAGIC_ITEMS");
        var image = filter.GetNode<TextureRect>("Image");
        image.Texture = GD.Load<Texture2D>("res://FrierenMod/images/ui/magic_deck.png");
        image.Material = (Material)misc.GetNode<TextureRect>("Image").Material.Duplicate();
        image.GetNode<TextureRect>("Shadow").Texture = image.Texture;
        misc.AddSibling(filter);
    }

    [HarmonyPostfix]
    [HarmonyBefore("BaseLib")]
    private static void Apply(Dictionary<NCardPoolFilter, Func<CardModel, bool>> ____poolFilters,
        Dictionary<CharacterModel, NCardPoolFilter> ____cardPoolFilters, NCardPoolFilter ____miscPoolFilter)
    {
        var misc = ____poolFilters[____miscPoolFilter];
        ____poolFilters[____miscPoolFilter] = card => !IsMagicItem(card) && (misc(card) || card is IGeneratedOnlyCard);
        if (!____cardPoolFilters.TryGetValue(ModelDb.Character<Frieren>(), out var filter)) return;
        var original = ____poolFilters[filter];
        ____poolFilters[filter] = card => original(card) && IsCharacterCard(card);
    }

    [HarmonyPostfix]
    private static void ConnectMagicFilter(NCardLibrary __instance, Dictionary<NCardPoolFilter, Func<CardModel, bool>> ____poolFilters,
        NCardPoolFilter ____miscPoolFilter)
    {
        var filter = ____miscPoolFilter.GetParent().GetNode<NCardPoolFilter>(MagicFilterName);
        ____poolFilters[filter] = IsMagicItem;
        filter.Connect(NCardPoolFilter.SignalName.Toggled, Callable.From<NCardPoolFilter>(changed =>
            __instance.Call("UpdateCardPoolFilter", changed)));
        filter.Connect(Control.SignalName.FocusEntered, Callable.From(() =>
            AccessTools.Field(typeof(NCardLibrary), "_lastHoveredControl").SetValue(__instance, filter)));
    }
}

// 因子不进入普通奖励解锁池，百科需单独允许查看；沿用 BaseLib 的默认已见机制。
[HarmonyPatch(typeof(NCardLibraryGrid), nameof(NCardLibraryGrid.RefreshVisibility))]
public static class FrierenLibraryVisibility
{
    [HarmonyPostfix]
    private static void IncludeMagicItems(HashSet<CardModel> ____unlockedCards, HashSet<ModelId> ____seenCards)
    {
        foreach (var card in ModelDb.AllCards.Where(card => card.ShouldShowInCardLibrary &&
            (FrierenLibraryFilter.IsMagicItem(card) || card is IGeneratedOnlyCard)))
        {
            ____unlockedCards.Add(card);
            ____seenCards.Add(card.Id);
            // 原生搜索和放大查看也依赖发现记录，避免出现可见却无法检索或点击的卡。
            if (!SaveManager.Instance.Progress.DiscoveredCards.Contains(card.Id))
                SaveManager.Instance.MarkCardAsSeen(card);
        }
    }
}
