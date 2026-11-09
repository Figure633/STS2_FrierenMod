using FrierenMod.Scripts.Enchantments;
using FrierenMod.Scripts.Relics;
using FrierenMod.Scripts.Shared.Compatibility;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;

namespace FrierenMod.Scripts.Events;

public sealed class HollowKnightAdventure : FrierenStoryEvent
{
    protected override string[] Portraits => ["PreviewBugKnight"];
    protected override IReadOnlyList<EventOption> GenerateInitialOptions() => [
        new(this, Radiance, $"{Id.Entry}.pages.INITIAL.options.RADIANCE", HoverTipFactory.FromRelic<KnightSecret>()),
        new EventOption(this, Owner!.Deck.Cards.Any(ModelDb.Enchantment<MarkOfPride>().CanEnchant) ? PathOfPain : null,
            $"{Id.Entry}.pages.INITIAL.options.PATH", HoverTipFactory.FromEnchantment<MarkOfPride>()).ThatDoesDamage(9),
        CardChoice<BorrowedTime>("SILKSONG", Silksong)
    ];

    private async Task Radiance()
    {
        await RelicCmd.Obtain<KnightSecret>(Owner!);
        Finish("RADIANCE", 0);
    }

    private async Task PathOfPain()
    {
        await GameApi.Damage(new ThrowingPlayerChoiceContext(), Owner!.Creature, 9,
            ValueProp.Unblockable | ValueProp.Unpowered, null, null);
        if (Owner.Creature.IsDead) return;
        var card = (await CardSelectCmd.FromDeckForEnchantment(Owner, ModelDb.Enchantment<MarkOfPride>(), 1,
            new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 1))).FirstOrDefault();
        if (card != null) {
            CardCmd.Enchant<MarkOfPride>(card, 1);
            var vfx = NCardEnchantVfx.Create(card);
            if (vfx != null) NRun.Instance?.GlobalUi.CardPreviewContainer.AddChildSafely(vfx);
        }
        Finish("PATH", 0);
    }

    private async Task Silksong() { await AddCard<BorrowedTime>(); Finish("SILKSONG", 0); }
}
