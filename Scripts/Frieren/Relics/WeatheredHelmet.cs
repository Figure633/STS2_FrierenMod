using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace FrierenMod.Scripts.Relics;

public sealed class WeatheredHelmet : FrierenRelic
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;
    public static int PrintedEnergyCost(CardModel card)
    {
        if (card.EnergyCost.CostsX) return 0;
        // 只看百科中此升级等级的费用，排除免费打出、附魔及局内降费。
        var printed = card.CanonicalInstance.ToMutable();
        for (int i = 0; i < card.CurrentUpgradeLevel; i++) {
            printed.UpgradeInternal(); printed.FinalizeUpgradeInternal();
        }
        return printed.EnergyCost.GetWithModifiers(CostModifiers.None);
    }
    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay play)
    {
        if (!CombatManager.Instance.IsInProgress || play.Card.Owner != Owner || PrintedEnergyCost(play.Card) < 3) return;
        Flash();
        await PowerCmd.Apply<BufferPower>(context, Owner.Creature, 1, Owner.Creature, null);
    }
}
