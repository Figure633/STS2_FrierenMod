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

public sealed class ContinualChant() : NormalChantCard(0, CardRarity.Uncommon, 3), ILegendaryCard
{
    protected override int Intensity => IsUpgraded ? 4 : 3;
    protected override bool IsPlayable => Owner != null && LegendaryCards.ChantsThisTurn(Owner) > 0 &&
        (Owner.Creature.GetPower<BanManaGainPower>() == null || base.IsPlayable);
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await ManaCmd.Generate(Owner, PileType.Draw, Intensity);
        await ChantCmd.Choose(context, play, Intensity);
    }
}
