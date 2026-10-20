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

public sealed class SpeakFromHeartSpell() : FrierenCard(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var mana = ManaCmd.InPile(Owner, PileType.Hand).ToArray();
        foreach (var card in mana) await CardCmd.Exhaust(context, card);
        await CardPileCmd.AutoPlayFromDrawPile(context, Owner, mana.Length * (IsUpgraded ? 2 : 1), CardPilePosition.Top, false);
    }
}
