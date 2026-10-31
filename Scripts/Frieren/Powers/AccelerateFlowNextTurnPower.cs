using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace FrierenMod.Scripts.Powers;

public sealed class AccelerateFlowNextTurnPower : FrierenPower
{
    public override PowerStackType StackType => PowerStackType.None;
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext context, Player player)
    {
        if (player != Owner.Player) return;
        await PowerCmd.Apply<AccelerateFlowPower>(context, Owner, 1, Owner, null);
        await PowerCmd.Remove(this);
    }
}
