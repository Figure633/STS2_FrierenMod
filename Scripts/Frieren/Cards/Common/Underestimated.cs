using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace FrierenMod.Scripts.Cards;

public sealed class Underestimated() : FrierenCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override IEnumerable<MegaCrit.Sts2.Core.HoverTips.IHoverTip> ExtraHoverTips =>
        base.ExtraHoverTips.Append(MegaCrit.Sts2.Core.HoverTips.HoverTipFactory.FromPower<ShrinkPower>());
    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(2)];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        if (!Owner.Creature.HasPower<ShrinkPower>())
            await PowerCmd.Apply<ShrinkPower>(context, Owner.Creature, 1, Owner.Creature, this);
        await PowerCmd.Apply<EnergyNextTurnPower>(context, Owner.Creature, DynamicVars.Energy.BaseValue, Owner.Creature, this);
        await PowerCmd.Apply<DrawCardsNextTurnPower>(context, Owner.Creature, 1, Owner.Creature, this);
    }
    protected override void OnUpgrade() => DynamicVars.Energy.UpgradeValueBy(1);
}
