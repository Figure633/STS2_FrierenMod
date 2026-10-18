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

public sealed class FullAheadPower : FrierenPower, IChantListener
{
    private bool _triggered;
    public async Task AfterChant(PlayerChoiceContext context, CardPlay play)
    {
        if (_triggered) return;
        _triggered = true;
        await PowerCmd.Apply<StrengthPower>(context, Owner, Amount, Owner, play.Card);
        await PowerCmd.Apply<DexterityPower>(context, Owner, Amount, Owner, play.Card);
    }
    public override Task AfterPlayerTurnStart(PlayerChoiceContext context, Player player)
    {
        if (player == Owner.Player) _triggered = false;
        return Task.CompletedTask;
    }
}
