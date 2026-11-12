using FrierenMod.Scripts.Enchantments;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace FrierenMod.Scripts.Relics;

public sealed class RottingSageSkull : FrierenRelic
{
    public override RelicRarity Rarity => RelicRarity.Ancient;
    public override bool HasUponPickupEffect => true;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromEnchantment<ManaSource>(3);
    public override async Task AfterObtained()
    {
        var selected = await CardSelectCmd.FromDeckForEnchantment(Owner, ModelDb.Enchantment<ManaSource>(), 3,
            new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 3));
        foreach (var card in selected) {
            CardCmd.Enchant<ManaSource>(card, 3);
            var vfx = NCardEnchantVfx.Create(card);
            if (vfx != null) NRun.Instance?.GlobalUi.CardPreviewContainer.AddChild(vfx);
        }
    }
}
