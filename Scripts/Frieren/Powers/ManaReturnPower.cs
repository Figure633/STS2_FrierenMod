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

public sealed class ManaReturnPower : FrierenPower
{
    public override PowerType Type => PowerType.Debuff;
    public override async Task AfterDamageReceived(PlayerChoiceContext context, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target == Owner && dealer?.Player != null && dealer != Owner && props.HasFlag(ValueProp.Move))
            await ManaCmd.Generate(dealer.Player, PileType.Discard, Amount, ignoreBan: true);
    }
}
