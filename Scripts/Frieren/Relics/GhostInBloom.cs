using FrierenMod.Scripts.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace FrierenMod.Scripts.Relics;

public sealed class GhostInBloom : FrierenRelic
{
    public override RelicRarity Rarity => RelicRarity.Event;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromCardWithCardHoverTips<BlueMoonWeed>();
    public override async Task AfterShuffle(PlayerChoiceContext context, Player shuffler)
    {
        if (shuffler != Owner) return;
        Flash();
        await CardPileCmd.AddGeneratedCardToCombat(Owner.Creature.CombatState!.CreateCard<BlueMoonWeed>(Owner),
            PileType.Draw, Owner);
    }
}
