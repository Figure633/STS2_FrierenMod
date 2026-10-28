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

public sealed class ManaBarricade() : NormalChantCard(1, CardRarity.Common, 2)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [..base.CanonicalVars, new BlockVar(8, ValueProp.Move), new BlockVar("BonusBlock",6,ValueProp.Move)];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var option = await ChantCmd.Choose(context, play, Intensity);
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
        if (option?.Slot == ManaSlot.Draw) await CreatureCmd.GainBlock(Owner.Creature, (BlockVar)DynamicVars["BonusBlock"], play);
    }
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(2);
}
