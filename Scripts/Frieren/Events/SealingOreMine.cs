using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Mechanics;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace FrierenMod.Scripts.Events;

public sealed class SealingOreMine : FrierenStoryEvent
{
    public int ArtIndex {get;private set;}
    public static int ManaForRemoved(CardModel card) => card.EnergyCost.CostsX || card.Keywords.Contains(CardKeyword.Unplayable)
        ? 0 : Math.Max(0, card.EnergyCost.GetWithModifiers(CostModifiers.Local));
    protected override string[] Portraits => ["SealingOreMine", "SealingOreMineGreed", "SealingOreMinePlay"];
    public override string CustomInitialPortraitPath => $"res://FrierenMod/images/events/{Portraits[ArtIndex]}.png";
    protected override IReadOnlyList<EventOption> GenerateInitialOptions() => [
        Choice("PLAY", Play, Owner!.Deck.Cards.Any(c => c.IsRemovable)),
        CardChoice<SealingOre>("GREED", Greed)
    ];
    private async Task Play()
    {
        var selected = (await CardSelectCmd.FromDeckForRemoval(Owner!,
            new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1))).SingleOrDefault();
        if (selected == null) return;
        int count = ManaForRemoved(selected);
        await CardPileCmd.RemoveFromDeck(selected);
        for (int i = 0; i < count; i++) await AddCard<Mana>();
        ArtIndex=2;Finish("PLAY", 2);
    }
    private async Task Greed()
    {
        await AddCard<SealingOre>();
        ArtIndex=1;Finish("GREED", 1);
    }
}
