using BaseLib.Abstracts;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Mechanics;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace FrierenMod.Scripts.Powers;

public sealed class SimmeringPower : FrierenPower, IChantListener
{
    public async Task AfterChant(PlayerChoiceContext context, CardPlay play)
    {
        var player = Owner.Player!;
        var cards = player.PlayerCombatState!.DrawPile.Cards.Where(x => x is ILegendaryCard).Take(Amount).ToArray();
        foreach (var card in cards)
        {
            await CardPileCmd.Add(card, PileType.Draw, CardPilePosition.Top);
            await CardPileCmd.Draw(context, player);
        }
    }
}
