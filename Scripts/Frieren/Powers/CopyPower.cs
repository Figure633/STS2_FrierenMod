using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace FrierenMod.Scripts.Powers;

public sealed class CopyPower : FrierenPower
{
    public override async Task AfterDamageReceived(PlayerChoiceContext context, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? source)
    {
        if (target != Owner || Owner.IsDead || result.UnblockedDamage <= 0) return;
        await PowerCmd.Apply<EnemyManaPower>(context, Owner, result.UnblockedDamage, Owner, null);
        if (Owner.GetPower<SpellCasterPower>() is { } caster) await caster.Resolve(context);
    }
    public override Task AfterBlockGained(Creature creature, decimal amount, ValueProp props, CardModel? source) =>
        creature.IsPlayer && creature.CombatState == Owner.CombatState && Owner.IsAlive
            ? CreatureCmd.GainBlock(Owner, amount, ValueProp.Unpowered, null) : Task.CompletedTask;
}
