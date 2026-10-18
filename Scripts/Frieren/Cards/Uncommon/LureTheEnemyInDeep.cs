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

public sealed class LureTheEnemyInDeep() : NormalChantCard(2, CardRarity.Uncommon, 2)
{
    private bool _returnToHand;
    protected override int Intensity => IsUpgraded ? 1 : 2;
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        _returnToHand = (await ChantCmd.Choose(context, play, Intensity))?.Slot == ManaSlot.Hand;
        await PlayerCmd.GainEnergy(Intensity, Owner);
    }
    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay play)
    {
        if (play.Card == this && _returnToHand)
        {
            _returnToHand = false;
            await CardPileCmd.Add(this, PileType.Hand);
        }
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
