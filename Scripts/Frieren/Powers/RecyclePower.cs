using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace FrierenMod.Scripts.Powers;

public sealed class RecyclePower : TurnOnlyPower
{
    private readonly Dictionary<CardPlay, int> _pending = [];
    public override Task BeforeCardPlayed(CardPlay play)
    {
        if (play.Card.Owner == Owner.Player) _pending[play] = Amount;
        return Task.CompletedTask;
    }
    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay play)
    {
        if (_pending.Remove(play, out int amount))
            await CreatureCmd.GainBlock(Owner, amount, ValueProp.Unpowered, null, fast: true);
    }
}
