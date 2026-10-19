using FrierenMod.Scripts.Shared.Cards;
using BaseLib.Utils;
using FrierenMod.Scripts.Characters;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;

namespace FrierenMod.Scripts.Cards;

[Pool(typeof(FrierenCardPool))]
public abstract class FrierenCard(int cost, CardType type, CardRarity rarity, TargetType target,
    bool showInLibrary = true) : ModCard(cost, type, rarity, target, showInLibrary)
{
    protected override string CardArtDirectory => "res://FrierenMod/images/cards";
    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get {
            // 重复的规则解释放到原生悬停提示，卡面保留本牌效果。
            HoverTip Tip(string key) => new(new LocString("frieren", $"TIP.{key}.title"), new LocString("frieren", $"TIP.{key}.description"));
            if (FrierenMod.Scripts.Mechanics.ChantCmd.IsChant(this) || this is FactorCard) yield return Tip("CHANT");
            if (this is ILegendaryCard) yield return Tip("LEGENDARY");
            if (this is Transcendence) yield return Tip("DIVINITY");
            if (this is Imagination) yield return Tip("MANA_PAYMENT");
            if (this is HellFireSummoning) yield return Tip("SEALED");
            if (this is DefensiveMagic) yield return Tip("SECOND_DEFENSE");
        }
    }
}
