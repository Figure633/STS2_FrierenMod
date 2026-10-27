using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace FrierenMod.Scripts.Powers;

public sealed class AuraSubmissionPower : FrierenPower
{
    private bool _drawResolved;
    public override PowerType Type => PowerType.Debuff;
    public override decimal ModifyHandDraw(Player player, decimal count) => player == Owner.Player && AmountOnTurnStart != 0 && !_drawResolved ? Math.Max(0, count - Amount) : count;
    public override Task AfterModifyingHandDraw()
    {
        _drawResolved = true;
        return Task.CompletedTask;
    }
    // 选择界面可暂停开场抽牌，不能在 AfterSideTurnStart 提前移除。
    public override Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants) =>
        side == CombatSide.Player && participants.Contains(Owner) && _drawResolved ? PowerCmd.Remove(this) : Task.CompletedTask;
}
