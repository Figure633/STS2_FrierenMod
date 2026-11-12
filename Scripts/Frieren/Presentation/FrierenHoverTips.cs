using System.Text.RegularExpressions;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Mechanics;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using ModPowers = FrierenMod.Scripts.Powers;

namespace FrierenMod.Scripts.Presentation;

public static class FrierenHoverTips
{
    private static LocString TipDescription(string key)
    {
        var text = new LocString("frieren", $"TIP.{key}.description");
        text.Add("energyPrefix", ModelDb.CardPool<FrierenMod.Scripts.Characters.FrierenCardPool>().EnergyColorName);
        return text;
    }
    private static readonly string[] Keywords = ["CHANT", "LEGENDARY", "DIVINITY", "MANA_PAYMENT",
        "SEALED", "SECOND_DEFENSE", "SYNCHRO", "ACCELERATED_SYNCHRO", "ULTIMATE_SYNCHRO",
        "ULTIMATE_ACCELERATED_SYNCHRO", "BACKFIRE"];
    // 通用状态用游戏现有的能力说明，名称随语言切换，不另抄写规则。
    private static readonly Func<PowerModel>[] ReferencedPowers = [
        ModelDb.Power<WeakPower>, ModelDb.Power<VulnerablePower>, ModelDb.Power<IntangiblePower>,
        ModelDb.Power<SlowPower>, ModelDb.Power<BufferPower>, ModelDb.Power<FrailPower>,
        ModelDb.Power<StrengthPower>, ModelDb.Power<DexterityPower>, ModelDb.Power<ArtifactPower>,
        ModelDb.Power<VigorPower>, ModelDb.Power<PoisonPower>, ModelDb.Power<ThornsPower>,
        ModelDb.Power<RegenPower>, ModelDb.Power<PlatingPower>, ModelDb.Power<FocusPower>,
        ModelDb.Power<RitualPower>, ModelDb.Power<BlurPower>, ModelDb.Power<BarricadePower>, ModelDb.Power<ShrinkPower>,
        ModelDb.Power<ModPowers.ConcentrationPower>, ModelDb.Power<ModPowers.MetallicizePower>,
        ModelDb.Power<ModPowers.PhantasmalPower>, ModelDb.Power<ModPowers.StrikeUpPower>,
        ModelDb.Power<ModPowers.SynchroWithoutManaPower>, ModelDb.Power<ModPowers.ChantWithoutManaTimesPower>
    ];

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
                yield return new HoverTip(title, TipDescription(key));
        }
        // 正文只写“传说魔法”的生成牌同样需要解释它所依赖的咏唱。
        if (!keys.Contains("CHANT") && words.Contains(new LocString("frieren", "TIP.LEGENDARY.title").GetFormattedText()) &&
            !words.Contains(new LocString("frieren", "TIP.CHANT.title").GetFormattedText()))
            yield return new HoverTip(new LocString("frieren", "TIP.CHANT.title"), TipDescription("CHANT"));
        foreach (var tip in NativeReferences(description, source))
            if (!source.HoverTipsNativeKeywordsContain(tip.Id)) yield return tip;
        if(source is RustCleanMagic)
            foreach(var tip in HoverTipFactory.FromEnchantment<FrierenMod.Scripts.Enchantments.ManaSource>(readable.DynamicVars["ManaSource"].IntValue)) yield return tip;
        foreach (var preview in DerivedCardPreviews.For(source)) yield return preview;
    }

    public static IEnumerable<IHoverTip> ForText(string text)
    {
        var words = Regex.Matches(text, @"\[gold\](.*?)\[/gold\]", RegexOptions.Singleline)
            .Select(match => match.Groups[1].Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (string key in Keywords) {
            var title = new LocString("frieren", $"TIP.{key}.title");
            if (words.Contains(title.GetFormattedText()))
                yield return new HoverTip(title, TipDescription(key));
        }
        foreach (var tip in NativeReferences(text)) yield return tip;
    }

    private static bool Mentions(string text, string term) => !string.IsNullOrWhiteSpace(term) &&
        Regex.IsMatch(text, @"(?<![A-Za-z])" + Regex.Escape(term) + @"(?![A-Za-z])", RegexOptions.IgnoreCase);

    private static IEnumerable<IHoverTip> NativeReferences(string text, CardModel? source = null)
    {
        // 去除颜色及动画标签后再匹配；普通文本、嵌套标签和英文单词均可识别。
        string plain = Regex.Replace(text, @"\[[^\]]*\]", "");
        foreach (var keyword in Enum.GetValues<CardKeyword>().Where(k => k != CardKeyword.None))
            if (Mentions(plain, new LocString("card_keywords", keyword.ToString().ToUpperInvariant() + ".title").GetFormattedText()))
                yield return HoverTipFactory.FromKeyword(keyword);
        foreach (var getPower in ReferencedPowers) {
            var power = getPower();
            // 本牌另有精确到1回合的原生缩小提示，不用通用3回合说明覆盖它。
            if (source is Underestimated && power is ShrinkPower) continue;
            if (Mentions(plain, power.Title.GetFormattedText()) || power is SlowPower && Mentions(plain, "迟缓"))
                // 自定义层数能力以单层说明，避免未附着模型显示“减少0”等无效效果。
                yield return HoverTipFactory.FromPower(power, power is ModPowers.FrierenPower ? 1 : null);
        }
        var block = HoverTipFactory.Static(StaticHoverTip.Block);
        if (block is HoverTip { Title: { } title } && Mentions(plain, title)) yield return block;
    }

    private static bool HoverTipsNativeKeywordsContain(this CardModel card, string id) =>
        card.Keywords.Any(keyword => HoverTipFactory.FromKeyword(keyword).Id == id);
}
