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
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace FrierenMod.Scripts.Powers;

public sealed class OpenTheWaygatePower : FrierenPower
{
    private int _consecutive;
    private bool _ready;
    public override PowerStackType StackType => PowerStackType.None;
    public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay play)
    {
        if (play.Card.Owner == Owner.Player)
        {
            _consecutive = ManaCmd.IsMana(play.Card) ? _consecutive + 1 : 0;
            if (_consecutive >= Amount) _ready = true;
        }
        return Task.CompletedTask;
    }
    public override bool ShouldTakeExtraTurn(Player player) => player == Owner.Player && _ready;
    public override async Task AfterTakingExtraTurn(Player player)
    {
        if (player == Owner.Player && _ready) await PowerCmd.Remove(this);
    }
    public override Task AfterPlayerTurnStart(PlayerChoiceContext context, Player player)
    {
        if (player == Owner.Player) _consecutive = 0;
        return Task.CompletedTask;
    }
}
