using System.Text.RegularExpressions;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Mechanics;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace FrierenMod.Scripts.Presentation;

public static class FrierenHoverTips
{
    private static readonly string[] Keywords = ["CHANT", "LEGENDARY", "DIVINITY", "MANA_PAYMENT",
        "SEALED", "SECOND_DEFENSE", "SYNCHRO", "ACCELERATED_SYNCHRO", "ULTIMATE_SYNCHRO",
        "ULTIMATE_ACCELERATED_SYNCHRO", "BACKFIRE"];

    public static IEnumerable<IHoverTip> For(CardModel source)
    {
        // 只涂金色不会生成原生说明。正文引用规则的牌也须提供对应 HoverTip，
        // 例如 FullAhead 并非咏唱牌，而传说魔法的解锁条件同样需要解释咏唱。
        var readable = source.IsMutable ? source : source.ToMutable();
        string description = readable.GetDescriptionForPile(PileType.None);
        var words = Regex.Matches(description, @"\[gold\](.*?)\[/gold\]", RegexOptions.Singleline)
            .Select(match => match.Groups[1].Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var keys = new HashSet<string>();
        if (ChantCmd.IsChant(source) || source is FactorCard or ILegendaryCard) keys.Add("CHANT");
        if (source is ILegendaryCard) keys.Add("LEGENDARY");
        if (source is Transcendence) keys.Add("DIVINITY");
        if (source is Imagination) keys.Add("MANA_PAYMENT");
        if (source is HellFireSummoning) keys.Add("SEALED");
        if (source is DefensiveMagic) keys.Add("SECOND_DEFENSE");
        if (source is IManaCard) keys.Add(SynchroPresentation.Key(source));
        if (source is AirSlash or ClearMind) keys.Add("ACCELERATED_SYNCHRO");
        if (source is LightningMagic) keys.Add("ULTIMATE_SYNCHRO");
        if (source is LittleFire or HellFireSummoning) keys.Add("BACKFIRE");
        foreach (string key in Keywords) {
            var title = new LocString("frieren", $"TIP.{key}.title");
            if (keys.Contains(key) || words.Contains(title.GetFormattedText()))
                yield return new HoverTip(title, new LocString("frieren", $"TIP.{key}.description"));
        }
        // 正文只写“传说魔法”的生成牌同样需要解释它所依赖的咏唱。
        if (!keys.Contains("CHANT") && words.Contains(new LocString("frieren", "TIP.LEGENDARY.title").GetFormattedText()) &&
            !words.Contains(new LocString("frieren", "TIP.CHANT.title").GetFormattedText()))
            yield return new HoverTip(new LocString("frieren", "TIP.CHANT.title"), new LocString("frieren", "TIP.CHANT.description"));
        foreach (var tip in NativeReferences(words))
            if (!source.HoverTipsNativeKeywordsContain(tip.Id)) yield return tip;
        foreach (var preview in DerivedCardPreviews.For(source)) yield return preview;
    }

    public static IEnumerable<IHoverTip> ForText(string text)
    {
        var words = Regex.Matches(text, @"\[gold\](.*?)\[/gold\]", RegexOptions.Singleline)
            .Select(match => match.Groups[1].Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (string key in Keywords) {
            var title = new LocString("frieren", $"TIP.{key}.title");
            if (words.Contains(title.GetFormattedText()))
                yield return new HoverTip(title, new LocString("frieren", $"TIP.{key}.description"));
        }
        foreach (var tip in NativeReferences(words)) yield return tip;
    }

    private static IEnumerable<IHoverTip> NativeReferences(HashSet<string> words)
    {
        foreach (var keyword in new[] { CardKeyword.Exhaust, CardKeyword.Retain })
            if (words.Contains(new LocString("card_keywords", keyword.ToString().ToUpperInvariant() + ".title").GetFormattedText()))
                yield return HoverTipFactory.FromKeyword(keyword);
    }

    private static bool HoverTipsNativeKeywordsContain(this CardModel card, string id) =>
        card.Keywords.Any(keyword => HoverTipFactory.FromKeyword(keyword).Id == id);
}
