using FrierenMod.Scripts.Cards;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace FrierenMod.Scripts.Powers;

public sealed class ManaAffinityPower : FrierenPower
{
    public override PowerStackType StackType => PowerStackType.Single;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(CardKeyword.Retain)];

    // 复用原生动态关键字接口，覆盖已有及后来生成的魔力；移除能力后自动恢复。
    public override bool TryModifyKeywordsInCombat(CardModel card, ISet<CardKeyword> keywords) =>
        card.Owner?.Creature == Owner && card is IManaCard && keywords.Add(CardKeyword.Retain);
}
