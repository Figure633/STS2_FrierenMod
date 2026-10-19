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

public sealed class TongueTwisterSpell() : FrierenCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2), new BlockVar(5, ValueProp.Move)];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var drawn = (await CardPileCmd.Draw(context, DynamicVars.Cards.BaseValue, Owner)).ToArray();
        foreach (var mana in drawn.Where(ManaCmd.IsMana))
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
    }
    protected override void OnUpgrade() { DynamicVars.Cards.UpgradeValueBy(1); DynamicVars.Block.UpgradeValueBy(1); }
}
