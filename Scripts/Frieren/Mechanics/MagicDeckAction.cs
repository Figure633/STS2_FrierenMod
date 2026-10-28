using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace FrierenMod.Scripts.Mechanics;

public sealed class MagicDeckAction(Player player, NetMagicDeckAction data) : GameAction
{
    public override ulong OwnerId => player.NetId;
    public override GameActionType ActionType => data.Setup ? GameActionType.Any : data.Shop ? GameActionType.NonCombat :
        data.InCombat ? GameActionType.CombatPlayPhaseOnly : GameActionType.NonCombat;
    protected override async Task ExecuteAction()
    {
        bool success = false;
        try {
            var context = new GameActionPlayerChoiceContext(this);
            success = data.Setup
                ? await MagicDeckCmd.ExecuteNetworkSetup(context, player)
                : data.Shop ? await MagicPropCmd.VisitShopCore(player, context) : await MagicPropCmd.ExecuteSynchronized(player, data);
        }
        finally { Complete(success); }
    }
    private void Complete(bool success)
    {
        if(data.Shop)MagicDeckNetwork.CompleteShop(player,success);
        MagicDeckNetwork.Complete(player,data.Request,success);
    }
    protected override void CancelAction() => Complete(false);
    public override INetAction ToNetAction() => data;
}
