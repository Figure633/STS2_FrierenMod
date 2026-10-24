using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace FrierenMod.Scripts.Powers;

public sealed class BirdCapturedPower : FrierenPower
{
    public override PowerType Type => PowerType.Debuff;
    public override string CustomPackedIconPath => "res://FrierenMod/images/powers/SwallowedPower.png";
    public override Task AfterApplied(Creature? applier, CardModel? cardSource) => Stun();
    private Task Stun() => Owner.Monster?.NextMove.Id is { } move && move != "STUNNED"
        ? CreatureCmd.Stun(Owner, move) : Task.CompletedTask;
    public override Task BeforeSideTurnStart(PlayerChoiceContext context, CombatSide side, IReadOnlyList<Creature> participants, ICombatState state) =>
        participants.Contains(Owner) ? Stun() : Task.CompletedTask;
    public override async Task AfterSideTurnEnd(PlayerChoiceContext context, CombatSide side, IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner)) await PowerCmd.Decrement(this);
    }
}
