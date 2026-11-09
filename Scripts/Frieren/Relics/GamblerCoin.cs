using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Relics;
using FrierenMod.Scripts.Cards;
using MegaCrit.Sts2.Core.HoverTips;

namespace FrierenMod.Scripts.Relics;
public sealed class GamblerCoin : FrierenRelic
{
    public override RelicRarity Rarity => RelicRarity.Ancient;
    public override bool HasUponPickupEffect => true;
    protected override string BigIconPath => PackedIconPath;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Concat(HoverTipFactory.FromCardWithCardHoverTips<HonestLiving>());
    public override async Task AfterObtained()
    {
        await PlayerCmd.LoseGold(Owner.Gold,Owner,GoldLossType.Lost);
        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(Owner.RunState.CreateCard<HonestLiving>(Owner),PileType.Deck));
    }
}
