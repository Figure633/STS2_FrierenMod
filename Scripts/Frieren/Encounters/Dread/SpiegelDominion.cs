using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Dread;
using MegaCrit.Sts2.Core.ValueProps;

namespace FrierenMod.Scripts.Encounters.Dread;

public sealed class SpiegelDominion : DominionModel, ICustomModel
{
    public override async Task AfterBlockGained(Creature creature, decimal amount, ValueProp props, CardModel? source)
    {
        if (!creature.IsPlayer || creature.Player?.PlayerCombatState?.TurnNumber != 1 || amount <= 0 ||
            creature.CombatState is not { } combat) return;

        foreach (var enemy in combat.Enemies.Where(enemy => enemy.IsAlive).ToArray())
            await CreatureCmd.GainBlock(enemy, amount, ValueProp.Unpowered, null);
    }
}
