using BaseLib.Abstracts;
using FrierenMod.Scripts.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace FrierenMod.Scripts.Powers;

// 复用原生暂时减力量的结算及人工制品交互，不带入原版“尖啸”的来源卡预览。
public sealed class BindingStrengthDownPower : TemporaryStrengthPower, ICustomPower
{
    public override AbstractModel OriginModel => ModelDb.Card<RestraintMagic>();
    protected override bool IsPositive => false;
    public string CustomPackedIconPath => ModelDb.Power<StrengthPower>().PackedIconPath;
    public string CustomBigIconPath => ModelDb.Power<StrengthPower>().ResolvedBigIconPath;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<StrengthPower>()];
}
