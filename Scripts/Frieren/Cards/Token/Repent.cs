using FrierenMod.Scripts.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace FrierenMod.Scripts.Cards;

public sealed class Repent() : FrierenCard(0, CardType.Skill, CardRarity.Token, TargetType.Self)
{
    public override bool CanBeGeneratedInCombat => false;
    public override bool CanBeGeneratedByModifiers => false;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain, CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2)];
    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1);
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        foreach (var card in await CardFlowCmd.Choose(context, this, PileType.Discard, DynamicVars.Cards.IntValue))
        {
            card.SetToFreeThisTurn(); await CardPileCmd.Add(card, PileType.Hand);
        }
    }
}
