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

public sealed class TrueColours() : NormalChantCard(1, CardRarity.Rare, 4)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        if (await ChantCmd.Choose(context, play, Intensity) != null)
        {
            await CardPileCmd.Draw(context, 3, Owner);
            await ManaCmd.Generate(Owner, PileType.Hand, 3);
        }
        await PlayerCmd.GainEnergy(3, Owner);
        await ManaCmd.Generate(Owner, PileType.Draw, 3);
        await ManaCmd.Generate(Owner, PileType.Discard, 3);
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
