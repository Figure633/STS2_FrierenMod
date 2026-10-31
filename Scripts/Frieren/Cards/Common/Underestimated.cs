using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace FrierenMod.Scripts.Cards;

public sealed class Underestimated() : FrierenCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    private const int ShrinkTurns = 1;
    protected override IEnumerable<IHoverTip> ExtraHoverTips {
        get {
            foreach (var tip in base.ExtraHoverTips) yield return tip;
            // 原版普通缩小说明把3回合写死，FromPower(amount)也不会替换。
            // 使用原生能力悬停的标题/图标，只为本卡提供准确时长，不改全局缩小。
            var power = ModelDb.Power<ShrinkPower>();
            var description = new LocString("frieren", "TIP.SHRINK_ONE_TURN.description");
            power.DynamicVars.AddTo(description);
            description.Add("Amount", ShrinkTurns);
            yield return new HoverTip(power, description.GetFormattedText(), false);
        }
    }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(2)];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        if (!Owner.Creature.HasPower<ShrinkPower>())
            await PowerCmd.Apply<ShrinkPower>(context, Owner.Creature, ShrinkTurns, Owner.Creature, this);
        await PowerCmd.Apply<EnergyNextTurnPower>(context, Owner.Creature, DynamicVars.Energy.BaseValue, Owner.Creature, this);
        await PowerCmd.Apply<DrawCardsNextTurnPower>(context, Owner.Creature, 1, Owner.Creature, this);
    }
    protected override void OnUpgrade() => DynamicVars.Energy.UpgradeValueBy(1);
}
