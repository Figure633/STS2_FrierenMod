using MegaCrit.Sts2.Core.HoverTips;

namespace FrierenMod.Scripts.Cards;

public sealed class BirdCapturingGrimoire : MagicPropCard
{
    protected override string ArtName => "BirdCapturingSpell";
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips
        .Concat(HoverTipFactory.FromCardWithCardHoverTips<BirdCapturingSpell>());
}
