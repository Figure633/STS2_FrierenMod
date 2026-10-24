using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace FrierenMod.Scripts.Powers;

// 二代没有一代“可塑性”；沿用伤害后、回合开始的原生钩子。
public sealed class SpiegelMalleablePower : FrierenPower
{
    private int _hits;
    public override string CustomPackedIconPath => "res://FrierenMod/images/powers/CopyPower.png";
    public override async Task AfterDamageReceived(PlayerChoiceContext context, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? source)
    {
        if (target != Owner || !props.IsPoweredAttack() || result.UnblockedDamage <= 0 || Owner.IsDead) return;
        await CreatureCmd.GainBlock(Owner, Amount + _hits++, ValueProp.Unpowered, null);
    }
    public override Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState state)
    {
        if (participants.Contains(Owner)) _hits = 0;
        return Task.CompletedTask;
    }
}
