using FrierenMod.Scripts.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace FrierenMod.Scripts.Cards;

public sealed class LongevityPain() : FrierenCard(-1, CardType.Curse, CardRarity.Curse, TargetType.None)
{
    private int _remaining = 10;
    [SavedProperty]
    public int Remaining {
        get => _remaining;
        set { AssertMutable(); _remaining = value; DynamicVars["Remaining"].BaseValue = value; }
    }
    public override int MaxUpgradeLevel => 0;
    public override bool CanBeGeneratedInCombat => false;
    public override bool CanBeGeneratedByModifiers => false;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable, CardKeyword.Eternal];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Remaining", 10)];
    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay play)
    {
        if (play.Card.Owner != Owner || Pile?.Type != PileType.Hand) return;
        Remaining = ManaCmd.IsMana(play.Card) ? Remaining - 1 : 10;
        if (Remaining > 0) return;
        Remaining = 10; await CardFlowCmd.Generate<Fatalism>(Owner, PileType.Hand);
    }
    public override async Task AfterCardExhausted(PlayerChoiceContext context, CardModel card, bool causedByEthereal)
    {
        if (card == this) await CardFlowCmd.Generate<LongevityPain>(Owner, PileType.Hand);
    }
    public override Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? clonedBy)
    {
        if (card == this && Pile?.Type != PileType.Hand) Remaining = 10;
        return Task.CompletedTask;
    }
}
