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

public sealed class BackFlow() : FrierenCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await CardFlowCmd.MoveMana(Owner, PileType.Discard, PileType.Hand, 4);
    }
    protected override void OnUpgrade() { EnergyCost.UpgradeBy(-1); }
}
