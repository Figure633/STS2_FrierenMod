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

// 比较本回合最高连续同调与此前回合的战斗最高记录。
public sealed class PaperPlaneSpell() : LegendaryCard(3, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play) {
        var power=Owner.Creature.GetPower<PaperPlanePower>() ?? await PowerCmd.Apply<PaperPlanePower>(context,Owner.Creature,1,Owner.Creature,this);
        if(power!=null && IsUpgraded)power.AllowEqual=true;
    }
    protected override void AddExtraArgsToDescription(MegaCrit.Sts2.Core.Localization.LocString description) {
        base.AddExtraArgsToDescription(description);
        description.Add("Current",IsMutable ? PaperPlanePower.TurnRecord(Owner) : 0);
        description.Add("Previous",IsMutable ? PaperPlanePower.BattleRecord(Owner) : 0);
    }
    protected override void OnUpgrade() { }
}
