using FrierenMod.Scripts.Mechanics;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace FrierenMod.Scripts.Cards;

public sealed class PreciseChant() : FrierenCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self), IChantCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Mana", 4)];
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var choices = ChantChoice.ManaGenerationChoices(Owner, DynamicVars["Mana"].IntValue);
        if (await CardSelectCmd.FromChooseACardScreen(context, choices, Owner) is not ChantChoice selected) return;
        await ManaCmd.Generate(Owner, ManaCmd.Pile(selected.Slot), selected.GenerationAmount);
        await ChantCmd.Choose(context, play, null);
    }
    protected override void OnUpgrade() => RemoveKeyword(CardKeyword.Exhaust);
}
