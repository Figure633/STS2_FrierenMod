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

public sealed class SealPower : FrierenPower, IChantListener
{
    private List<CardModel>? _cards;
    public List<CardModel> Cards => _cards ??= [];
    public override int DisplayAmount => Owner?.Player is { PlayerCombatState: not null } player
        ? LegendaryCards.ChantsThisTurn(player) : 0;
    public async Task AfterChant(PlayerChoiceContext context, CardPlay play)
    {
        InvokeDisplayAmountChanged();
        if (DisplayAmount < 5) return;
        foreach (var card in Cards.ToArray()) await CardPileCmd.Add(card, PileType.Hand);
        Cards.Clear();
        await PowerCmd.Remove(this);
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext context, Player player)
    {
        if (player == Owner.Player) InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }
}
