using BaseLib.Abstracts;
using FrierenMod.Core;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Presentation;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Unlocks;

public static class LibraryTests
{
    public static Task<int> Run(Node root)
    {
        int count = 0;
        void Check(bool valid, string name) { if (!valid) throw new Exception(name); count++; GD.Print("PASS 百科分类：" + name); }
        var pool = ModelDb.CardPool<FrierenCardPool>();
        var items = pool.AllCards.Where(FrierenLibraryFilter.IsMagicItem).ToArray();
        Check(items.OfType<FactorCard>().Count() == SpellCatalog.All.Count && items.OfType<MagicPropCard>().Count() == 8,
            "完整登记全部因子和八种魔具");
        Check(items.All(c => c.ShouldShowInCardLibrary), "全部因子和魔具进入原生百科候选列表");

        var character = new NCardPoolFilter(); var misc = new NCardPoolFilter(); var other = new NCardPoolFilter();
        Func<CardModel, bool> originalOther = card => card.Pool is not FrierenCardPool;
        var filters = new Dictionary<NCardPoolFilter, Func<CardModel, bool>> {
            [character] = card => pool.AllCardIds.Contains(card.Id),
            [misc] = card => card.Rarity is CardRarity.Token or CardRarity.Status or CardRarity.Curse or CardRarity.Event or CardRarity.Quest,
            [other] = originalOther
        };
        var apply = AccessTools.Method(typeof(FrierenLibraryFilter), "Apply");
        Check(Harmony.GetPatchInfo(AccessTools.Method(typeof(NCardLibrary), nameof(NCardLibrary._Ready)))!.Postfixes
            .Any(p => p.PatchMethod == apply), "百科初始化已注册分类补丁");
        apply.Invoke(null, [filters, new Dictionary<CharacterModel, NCardPoolFilter> { [ModelDb.Character<Frieren>()] = character }, misc]);
        foreach (var card in items)
            Check(filters[misc](card) && !filters[character](card), "仅收录于衍生牌分类：" + card.GetType().Name);
        Check(ReferenceEquals(filters[other], originalOther), "其他角色分类保留原过滤器");
        Check(filters[character](ModelDb.Card<RapidChant>()) && filters[character](ModelDb.Card<HighDensityMana>()) &&
            !filters[misc](ModelDb.Card<RapidChant>()) && !filters[misc](ModelDb.Card<HighDensityMana>()), "普通角色卡分类不变");
        Check(filters[misc](ModelDb.Card<Mana>()) && !filters[character](ModelDb.Card<Mana>()) &&
            filters[misc](ModelDb.Card<Dazed>()) && filters[misc](ModelDb.Card<Injury>()), "魔力和原生状态诅咒继续收录");
        Check(!ModelDb.Card<PaperPlaneSpell>().ShouldShowInCardLibrary && !ModelDb.Card<TimeConcept>().ShouldShowInCardLibrary &&
            !ModelDb.Card<DrawChantChoice>().ShouldShowInCardLibrary && !ModelDb.Card<LegendaryCraftChoice>().ShouldShowInCardLibrary,
            "废弃牌和选择占位模型继续隐藏");

        var oldUnlocks = pool.GetUnlockedCards(UnlockState.all, CardMultiplayerConstraint.None).Select(c => c.Id).ToHashSet();
        var oldSeen = SaveManager.Instance.Progress.DiscoveredCards.ToHashSet();
        var grid = new NCardLibraryGrid();
        grid.RefreshVisibility();
        var visibility = AccessTools.Method(typeof(NCardLibraryGrid), "GetCardVisibility");
        Check(items.All(c => (ModelVisibility)visibility.Invoke(grid, [c])! == ModelVisibility.Visible),
            "原生百科网格完整显示，不是锁定或未见卡背");
        Check(items.All(c => SaveManager.Instance.Progress.DiscoveredCards.Contains(c.Id)), "原生名称搜索和放大查看的发现条件满足");
        Check(SaveManager.Instance.Progress.DiscoveredCards.Except(oldSeen).All(id => items.Any(c => c.Id == id) ||
            ModelDb.GetById<CardModel>(id) is IGeneratedOnlyCard),
            "不提前发现其他卡牌");
        Check(pool.GetUnlockedCards(UnlockState.all, CardMultiplayerConstraint.None).Select(c => c.Id).ToHashSet().SetEquals(oldUnlocks) &&
            !pool.GetUnlockedCards(UnlockState.all, CardMultiplayerConstraint.None).OfType<FactorCard>().Any(),
            "百科可见不改变普通奖励解锁池");
        var discovered = SaveManager.Instance.Progress.DiscoveredCards.ToHashSet();
        grid.RefreshVisibility();
        Check(SaveManager.Instance.Progress.DiscoveredCards.ToHashSet().SetEquals(discovered), "重开百科不重复添加发现记录");
        foreach (string language in new[] { "zhs", "eng" }) {
            LocManager.Instance.SetLanguage(language);
            Check(items.All(c => !string.IsNullOrWhiteSpace(c.Title) && !c.Title.Contains("FRIERENMOD-")), "全部卡名可检索：" + language);
            Check(items.All(c => ((ICustomTypeTextCard)c).GetTypeModifiers().Single().GetFormattedText() ==
                new LocString("frieren", c is FactorCard ? "FACTOR_TYPE" : "PROP_TYPE").GetFormattedText()),
                "保留魔法因子和魔具类型：" + language);
            Check(items.All(c => {
                string description = c.GetDescriptionForPile(PileType.None);
                return !string.IsNullOrWhiteSpace(description) && !description.Contains("{Effect}") && !description.Contains("{Context}");
            }), "无主百科模型展示完整规则：" + language);
        }
        Check(items.OfType<FactorCard>().All(c => c.Rarity == (SpellCatalog.Get(c.FactorId).Rarity switch {
            FactorRarity.Common => CardRarity.Common, FactorRarity.Uncommon => CardRarity.Uncommon, _ => CardRarity.Rare
        })), "因子品质与原目录一致，可继续按稀有度筛选排序");
        Check(items.OfType<MagicPropCard>().All(c => c.Rarity == CardRarity.Token), "魔具保留衍生牌稀有度");
        LocManager.Instance.SetLanguage("eng");
        character.Free(); misc.Free(); other.Free(); grid.Free();
        return Task.FromResult(count);
    }
}
