using FrierenMod.Core;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace FrierenMod.Scripts.Cards;

public sealed class TimeConcept() : FrierenCard(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var snapshot = PlayerTimeSnapshot.Capture(Owner, this);
        var power = await PowerCmd.Apply<TimeTravelPower>(context, Owner.Creature, 2, Owner.Creature, this);
        power!.Snapshot = snapshot;
        power.Upgraded = IsUpgraded;
    }
}
