using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Mechanics;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace FrierenMod.Scripts.Powers;

public sealed class SwitchStrengthAndDexterityPower : FrierenPower
{
    public override async Task BeforeSideTurnEnd(PlayerChoiceContext context, CombatSide side, IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner)) return;
        for (int i = 0; i < Amount; i++) await CardFlowCmd.SwapStrengthAndDexterity(context, Owner.Player!, null);
        await PowerCmd.Remove(this);
    }
}
