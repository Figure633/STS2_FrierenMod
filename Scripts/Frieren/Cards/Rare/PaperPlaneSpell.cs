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

// 作者新设计：记录每个玩家回合最高连续同调，原生历史提供上回合数据。
public sealed class PaperPlaneSpell() : LegendaryCard(1, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play) {
        var power=Owner.Creature.GetPower<PaperPlanePower>() ?? await PowerCmd.Apply<PaperPlanePower>(context,Owner.Creature,1,Owner.Creature,this);
        if(power!=null && IsUpgraded)power.AllowEqual=true;
    }
    protected override void AddExtraArgsToDescription(MegaCrit.Sts2.Core.Localization.LocString description) {
        base.AddExtraArgsToDescription(description);
        description.Add("Current",PaperPlanePower.TurnRecord(Owner));
        description.Add("Previous",PaperPlanePower.TurnRecord(Owner,true));
    }
    protected override void OnUpgrade() { }
}
