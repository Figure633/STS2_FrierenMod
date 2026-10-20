using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace FrierenMod.Scripts.Cards;

public sealed class Fatalism() : FrierenCard(10, CardType.Skill, CardRarity.Token, TargetType.Self)
{
    public override int MaxUpgradeLevel => 0;
    public override bool CanBeGeneratedInCombat => false;
    public override bool CanBeGeneratedByModifiers => false;
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        await CreatureCmd.Damage(context, Owner.Creature.CombatState!.Creatures.Where(c => c.IsAlive).ToArray(),
            999, ValueProp.Unblockable | ValueProp.Unpowered, Owner.Creature);
}
