using FrierenMod.Scripts.Cards;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Rewards;

namespace FrierenMod.Scripts.Events;

public sealed class AnimalWell : FrierenStoryEvent
{
    protected override string[] Portraits => ["AnimalWell", "AnimalWell1", "AnimalWell2", "AnimalWell3"];
    protected override IReadOnlyList<EventOption> GenerateInitialOptions() => [
        Choice("FIRECRACKERS", Firecrackers), Choice("FLUTE", Flute, Owner!.Deck.Cards.Any(c => c.IsUpgradable)),
        Choice("BIRD", Bird)
    ];
    private async Task Firecrackers()
    {
        // 一代实际给火焰与爆炸药水各一瓶；使用原生奖励界面处理满药水槽。
        await RewardsCmd.OfferCustom(Owner!, [new PotionReward(ModelDb.Potion<FirePotion>().ToMutable(), Owner!),
            new PotionReward(ModelDb.Potion<ExplosiveAmpoule>().ToMutable(), Owner!)]);
        Finish("FIRECRACKERS", 1);
    }
    private async Task Flute()
    {
        foreach (var card in await CardSelectCmd.FromDeckForUpgrade(Owner!, new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt, 1)))
            CardCmd.Upgrade(card);
        Finish("FLUTE", 2);
    }
    private async Task Bird() { await AddCard<BirdCapturingSpell>(); Finish("BIRD", 3); }
}
