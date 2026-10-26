using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace FrierenMod.Scripts.Powers;

public sealed class AuraSubmissionPower : FrierenPower
{
    public override PowerType Type => PowerType.Debuff;
    public override decimal ModifyHandDraw(Player player, decimal count) => player == Owner.Player && AmountOnTurnStart != 0 ? Math.Max(0, count - Amount) : count;
    public override Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState) =>
        participants.Contains(Owner) && AmountOnTurnStart != 0 ? PowerCmd.Remove(this) : Task.CompletedTask;
}
