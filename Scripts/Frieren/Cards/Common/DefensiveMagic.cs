using FrierenMod.Scripts.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace FrierenMod.Scripts.Cards;

public sealed class DefensiveMagic() : FrierenCard(1, CardType.Skill, CardRarity.Common, TargetType.Self), IChantCard
{
    protected override bool IsPlayable => Owner != null && ChantCmd.Options(Owner, 1).Count > 0;
    protected override void AddExtraArgsToDescription(MegaCrit.Sts2.Core.Localization.LocString description) => description.Add("Upgraded", IsUpgraded);
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(3, ValueProp.Move)];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await ChantCmd.Choose(context, play, 1);
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
        var card = Owner.Creature.CombatState!.CreateCard<SecondDefensiveMagic>(Owner);
        if (IsUpgraded) card.UpgradeInternal();
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, Owner);
    }
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(2);
}
