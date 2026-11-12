using BaseLib.Abstracts;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Mechanics;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace FrierenMod.Scripts.Enchantments;

public sealed class ManaSource : CustomEnchantmentModel
{
    protected override string CustomIconPath => "res://FrierenMod/images/enchantments/ManaSource.png";
    public override bool HasExtraCardText => true;
    public override bool ShowAmount => true;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromCardWithCardHoverTips<Mana>();
    // 原生附魔排除状态牌；作者指定的魔力是唯一例外，仍不覆盖已有附魔。
    public override bool CanEnchant(CardModel card) => card is Mana ? card.Enchantment == null : base.CanEnchant(card);
    public override async Task OnPlay(PlayerChoiceContext context, CardPlay? play)
    {
        if (play == null || play.Card != Card || Status != EnchantmentStatus.Normal) return;
        // 在异步生成前禁用，重放与嵌套出牌都不会重复触发。
        Status = EnchantmentStatus.Disabled;
        await ManaCmd.Generate(Card.Owner, PileType.Hand, (int)Amount);
    }
}
