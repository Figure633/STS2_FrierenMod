using FrierenMod.Scripts.Presentation;
using FrierenMod.Scripts.Relics;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Cards;

namespace FrierenMod.Scripts.Events;

public sealed class SnowCrystalLand : FrierenStoryEvent
{
    protected override string[] Portraits => ["SnowCrystalLand", "SnowCrystalTouched"];
    public override Task AfterEventStarted() { FrierenAchievements.Unlock(Owner!,"8019"); return Task.CompletedTask; }
    protected override IReadOnlyList<EventOption> GenerateInitialOptions() => [
        new(this,Owner!.Deck.Cards.Count(c => c.IsTransformable && c.Type != CardType.Quest) >= 2 ? Touch : null,
            $"{Id.Entry}.pages.INITIAL.options.TOUCH",HoverTipFactory.FromCardWithCardHoverTips<Doubt>()),
        new(this,Protect,$"{Id.Entry}.pages.INITIAL.options.PROTECT",HoverTipFactory.FromRelic<Matryoshka>())
    ];
    private async Task Touch()
    {
        var cards = (await CardSelectCmd.FromDeckForTransformation(Owner!,new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt,2))).ToArray();
        if (cards.Length != 2) return;
        // 与原版 MorphicGrove 的两张变化相同，逐张交给原生随机变化命令。
        foreach(var card in cards) await CardCmd.TransformToRandom(card,Owner!.PlayerRng.Transformations,CardPreviewStyle.EventLayout);
        await CardPileCmd.AddCurseToDeck<Doubt>(Owner!);
        Finish("TOUCH",1);
    }
    private async Task Protect() { await RelicCmd.Obtain<Matryoshka>(Owner!); Finish("PROTECT",0); }
}
