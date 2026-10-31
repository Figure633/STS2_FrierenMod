using FrierenMod.Scripts.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace FrierenMod.Scripts.Powers;

public sealed class SwampManaNextTurnPower : FrierenPower
{
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext context,Player player)
    {
        if(player!=Owner.Player)return;
        // 与原生延迟回合能力一样，只结算本回合开始前已有的部分。
        int due=Math.Min(AmountOnTurnStart,Amount);
        if(due<=0)return;
        AmountOnTurnStart=0;
        Flash();
        await PowerCmd.ModifyAmount(context,this,-due,Owner,null);
        await ManaCmd.Generate(player,PileType.Hand,due);
    }
}
