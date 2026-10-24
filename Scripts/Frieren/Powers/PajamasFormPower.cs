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

public sealed class PajamasFormPower : FrierenPower
{
    public override async Task BeforeSideTurnEnd(PlayerChoiceContext context, CombatSide side, IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner)) return;
        var cards = new[] { PileType.Draw, PileType.Hand, PileType.Discard }
            .SelectMany(p => ManaCmd.InPile(Owner.Player!, p)).ToArray();
        if (cards.Length >= 30) Presentation.FrierenAchievements.Unlock(Owner.Player!, "4009");
        foreach (var card in cards) await CardCmd.Exhaust(context, card);
        if (cards.Length > 0)
            await CreatureCmd.Damage(context, Owner.CombatState!.Enemies.Where(c => c.IsAlive), 4 * Amount * cards.Length, ValueProp.Unpowered, Owner);
    }
}
