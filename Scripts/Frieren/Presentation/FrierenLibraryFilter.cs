using FrierenMod.Scripts.Characters;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;

namespace FrierenMod.Scripts.Presentation;

// 保留模型卡池和存档 ID，只调整百科分类；衍生牌由原生杂项分类按稀有度收录。
[HarmonyPatch(typeof(NCardLibrary), nameof(NCardLibrary._Ready))]
public static class FrierenLibraryFilter
{
    public static bool IsCharacterCard(CardModel card) => card.Rarity is not
        (CardRarity.Token or CardRarity.Status or CardRarity.Curse or CardRarity.Event or CardRarity.Quest);

    [HarmonyPostfix]
    private static void Apply(Dictionary<NCardPoolFilter, Func<CardModel, bool>> ____poolFilters,
        Dictionary<CharacterModel, NCardPoolFilter> ____cardPoolFilters)
    {
        if (!____cardPoolFilters.TryGetValue(ModelDb.Character<Frieren>(), out var filter)) return;
        var original = ____poolFilters[filter];
        ____poolFilters[filter] = card => original(card) && IsCharacterCard(card);
    }
}
