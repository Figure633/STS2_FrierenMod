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

public sealed class PaperPlanePower : FrierenPower
{
    private int _consecutive;
    private int _attacks;
    public override int DisplayAmount => _attacks;
    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource) =>
        dealer == Owner && props.HasFlag(ValueProp.Move) && _attacks > 0 ? 2 : 1;
    public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay play)
    {
        if (play.Card.Owner != Owner.Player) return Task.CompletedTask;
        if (play.Card.Type == CardType.Attack) _attacks = Math.Max(0, _attacks - 1);
        if (play.Card is IManaCard)
        {
            if (++_consecutive == 4) { _attacks += Amount; _consecutive = 0; }
        }
        else _consecutive = 0;
        return Task.CompletedTask;
    }
}
