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

public sealed class BreaksBarriersSpell() : FrierenCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2)];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var pile = Owner.PlayerCombatState!.DrawPile;
        var sorted = pile.Cards.OrderBy(c => ManaCmd.IsMana(c) ? -9 : c.EnergyCost.Canonical).ToArray();
        foreach (var c in sorted.Reverse()) pile.MoveToTopInternal(c);
        await CardPileCmd.Draw(context, DynamicVars.Cards.BaseValue, Owner);
    }
    protected override void OnUpgrade() { DynamicVars.Cards.UpgradeValueBy(1); }
}
