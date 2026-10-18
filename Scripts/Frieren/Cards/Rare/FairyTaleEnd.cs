using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace FrierenMod.Scripts.Cards;

public sealed class FairyTaleEnd() : LegendaryCard(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new FairyTaleDamageVar()];
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        await DamageCmd.Attack(((FairyTaleDamageVar)DynamicVars.Damage).Calculate(this)).FromCard(this).Targeting(play.Target!).Execute(context);
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2);

    // 预览与出牌共用牌组倍率；基础值不在预览时改写，避免反复查看造成重复翻倍。
    private sealed class FairyTaleDamageVar() : DamageVar(6, ValueProp.Move)
    {
        public decimal Calculate(CardModel card)
        {
            int count = card.IsMutable ? card.Owner?.Deck.Cards.OfType<ILegendaryCard>()
                .Select(x => x.GetType()).Distinct().Count() ?? 0 : 0;
            return Math.Min(99999m, BaseValue * (decimal)Math.Pow(2, Math.Min(count, 16)));
        }

        public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode,
            MegaCrit.Sts2.Core.Entities.Creatures.Creature? target, bool runGlobalHooks)
        {
            // 使用原生伤害预览处理力量、虚弱、易伤和附魔，保持目标预览与实际命中一致。
            var damage = new DamageVar(Calculate(card), Props);
            damage.UpdateCardPreview(card, previewMode, target, runGlobalHooks);
            EnchantedValue = damage.EnchantedValue;
            PreviewValue = damage.PreviewValue;
        }
    }
}
