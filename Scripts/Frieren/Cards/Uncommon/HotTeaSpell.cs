using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace FrierenMod.Scripts.Cards;

public sealed class HotTeaSpell() : LegendaryCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(12, ValueProp.Move),
        new CalculationBaseVar(0), new CalculationExtraVar(1),
        new CalculatedBlockVar(ValueProp.Move).WithMultiplier((card, _) => card.DynamicVars.Block.BaseValue + ManaCmd.Total(card.Owner))];
    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        // 魔力数量属于卡牌自身公式；只有敏捷、脆弱等外部修正改变数字颜色。
        DynamicVars.CalculatedBlock.EnchantedValue = DynamicVars.CalculatedBlock.Calculate(null);
    }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue + ManaCmd.Total(Owner), ValueProp.Move, play);
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3);
}
