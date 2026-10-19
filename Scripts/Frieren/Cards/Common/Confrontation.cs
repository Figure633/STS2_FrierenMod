using FrierenMod.Core;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace FrierenMod.Scripts.Cards;

public sealed class Confrontation() : FrierenCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(6, ValueProp.Move)];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play) => await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
    public override Task BeforeCardPlayed(CardPlay play)
    {
        if (play.Card.Owner == Owner && ChantCmd.IsChant(play.Card) && Pile?.Type == PileType.Hand) DynamicVars.Block.BaseValue += 3;
        return Task.CompletedTask;
    }
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3);
}
