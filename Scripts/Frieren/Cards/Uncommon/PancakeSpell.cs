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

public sealed class PancakeSpell() : FrierenCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Amount", 2)];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        if (ManaCmd.InPile(Owner, PileType.Hand).Count() < (int)DynamicVars["Amount"].BaseValue) return;
        await CardFlowCmd.MoveMana(Owner, PileType.Hand, PileType.Draw, (int)DynamicVars["Amount"].BaseValue);
        foreach (var card in await CardFlowCmd.Choose(context, this, PileType.Draw, 1))
            await CardPileCmd.Add(card, PileType.Hand);
    }
    protected override void OnUpgrade() { DynamicVars["Amount"].UpgradeValueBy(-1); }
}
