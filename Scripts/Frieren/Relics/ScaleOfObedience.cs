using FrierenMod.Scripts.Mechanics;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace FrierenMod.Scripts.Relics;

public sealed class ScaleOfObedience : FrierenRelic
{
    public override RelicRarity Rarity => RelicRarity.Ancient;
    public override async Task BeforeSideTurnEnd(PlayerChoiceContext context, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Player || !participants.Contains(Owner.Creature) || ManaCmd.Total(Owner) == 0) return;
        Flash();
        await PowerCmd.Apply<EnergyNextTurnPower>(context, Owner.Creature, 1, Owner.Creature, null);
    }
}
