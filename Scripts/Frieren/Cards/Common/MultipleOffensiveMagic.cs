using FrierenMod.Core;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace FrierenMod.Scripts.Cards;

public sealed class MultipleOffensiveMagic() : FrierenCard(0, CardType.Attack, CardRarity.Common, TargetType.AllEnemies)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new XDamageVar(), new XAmountVar("Hits", 0), new XAmountVar("Mana", 0)];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        int x = ResolveEnergyXValue();
        int hits = x + DynamicVars["Hits"].IntValue;
        if (hits > 0) await DamageCmd.Attack(x + DynamicVars.Damage.BaseValue).FromCard(this).TargetingRandomOpponents(CombatState!).WithHitCount(hits).Execute(context);
        await ManaCmd.Generate(Owner, PileType.Hand, x);
    }
    protected override bool HasEnergyCostX => true;
    protected override void OnUpgrade() => DynamicVars["Hits"].UpgradeValueBy(1);

    private static int PreviewX(CardModel card)
    {
        if (!card.IsMutable || card.Owner?.PlayerCombatState == null || card.CombatState == null) return 0;
        int energy = card.Pile?.Type == PileType.Play ? card.EnergyCost.CapturedXValue : card.EnergyCost.GetAmountToSpend();
        return Hook.ModifyXValue(card.CombatState, card, energy);
    }

    // 与原生伤害预览共用增益、减益及目标修正；不改写基础值，也不提前捕获出牌 X。
    private sealed class XDamageVar() : DamageVar(4, ValueProp.Move)
    {
        public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
        {
            var damage = new DamageVar(BaseValue + PreviewX(card), Props);
            damage.UpdateCardPreview(card, previewMode, target, runGlobalHooks);
            EnchantedValue = damage.EnchantedValue;
            PreviewValue = damage.PreviewValue;
        }
    }

    private sealed class XAmountVar(string name, int amount) : DynamicVar(name, amount)
    {
        public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
        {
            // X 本身不作为强化效果染色，只有升级额外次数使用原生绿色高亮。
            EnchantedValue = PreviewValue = BaseValue + PreviewX(card);
        }
    }
}
