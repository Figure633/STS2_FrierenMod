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

public sealed class ClearMind() : FrierenCard(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Amount", 2)];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await ManaCmd.Generate(Owner, PileType.Hand, (int)DynamicVars["Amount"].BaseValue);
        await PowerCmd.Apply<AccelerateFlowPower>(context, Owner.Creature, 1, Owner.Creature, this);
    }
    protected override void OnUpgrade() { DynamicVars["Amount"].UpgradeValueBy(1); }
}
