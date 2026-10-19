using FrierenMod.Core;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace FrierenMod.Scripts.Cards;

public sealed class GreenApple() : FrierenCard(0, CardType.Skill, CardRarity.Token, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(3, ValueProp.Move)];
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        for (int i = 0; i < 2; i++) await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
    }
    protected override void OnUpgrade() { DynamicVars.Block.UpgradeValueBy(2); }
}
