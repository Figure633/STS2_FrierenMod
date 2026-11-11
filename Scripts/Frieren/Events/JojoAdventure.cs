using FrierenMod.Scripts.Enchantments;
using FrierenMod.Scripts.Relics;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace FrierenMod.Scripts.Events;

public sealed class JojoAdventure : FrierenStoryEvent
{
    protected override string[] Portraits => ["Previewjojo"];
    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return [
            new(this, Owner!.Deck.Cards.Any(ModelDb.Enchantment<HamonOverdrive>().CanEnchant) ? Mask : null,
                $"{Id.Entry}.pages.INITIAL.options.MASK", HoverTipFactory.FromEnchantment<HamonOverdrive>()),
            new(this, Owner.Relics.Any(r => r is GhostInBloom) ? null : Diamond,
                $"{Id.Entry}.pages.INITIAL.options.DIAMOND", HoverTipFactory.FromRelic<GhostInBloom>()),
            CardChoice<Spinner>("SPIN", Spin)
        ];
    }
    private async Task Mask()
    {
        var selected = (await CardSelectCmd.FromDeckForEnchantment(Owner!, ModelDb.Enchantment<HamonOverdrive>(), 1,
            new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 1))).FirstOrDefault();
        if (selected != null) {
            CardCmd.Enchant<HamonOverdrive>(selected, 1);
            var vfx = NCardEnchantVfx.Create(selected);
            if (vfx != null) NRun.Instance?.GlobalUi.CardPreviewContainer.AddChildSafely(vfx);
        }
        await CreatureCmd.Heal(Owner!.Creature, 8);
        Finish("MASK", 0);
    }
    private async Task Diamond() { await RelicCmd.Obtain<GhostInBloom>(Owner!); Finish("DIAMOND", 0); }
    private async Task Spin() { await AddCard<Spinner>(); Finish("SPIN", 0); }
}
