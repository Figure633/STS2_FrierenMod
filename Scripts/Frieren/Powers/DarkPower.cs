using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace FrierenMod.Scripts.Powers;

public sealed class DarkPower : FrierenPower
{
    // Amount 使用 1..5 表示一代的适应阶段 0..4，避免原生移除零层能力。
    public int Adaptation => Amount - 1;
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext context, Player player)
    {
        if (player == Owner.Player && Amount < 5) await PowerCmd.ModifyAmount(context, this, 1, Owner, null);
    }
}
