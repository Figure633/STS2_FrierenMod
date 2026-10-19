using FrierenMod.Scripts.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace FrierenMod.Scripts.Powers;

public sealed class TimeTravelPower : FrierenPower
{
    public override PowerStackType StackType => PowerStackType.None;
    public PlayerTimeSnapshot? Snapshot { get; set; }
    public bool Upgraded { get; set; }
    public override async Task AfterPlayerTurnStartEarly(PlayerChoiceContext context, Player player)
    {
        if (player != Owner.Player || Snapshot == null) return;
        if (Amount > 1) { await PowerCmd.Decrement(this); return; }
        var snapshot = Snapshot;
        await PowerCmd.Remove(this);
        await snapshot.Restore(context, player, Upgraded);
    }
}
