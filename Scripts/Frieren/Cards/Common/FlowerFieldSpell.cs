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

public sealed class FlowerFieldSpell() : NormalChantCard(1, CardRarity.Common, 2)
{
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        if (await ChantCmd.Choose(context, play, Intensity) == null) return;
        var card = CombatState!.CreateCard<BlueMoonWeed>(Owner);
        if (IsUpgraded) card.UpgradeInternal();
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, Owner);
    }
}
