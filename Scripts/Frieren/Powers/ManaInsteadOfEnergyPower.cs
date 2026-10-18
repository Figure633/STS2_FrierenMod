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

public sealed class ManaInsteadOfEnergyPower : TurnOnlyPower
{
    private bool _calculating;
    public int Cost(CardModel card)
    {
        if (card.EnergyCost.CostsX) return 0;
        _calculating = true;
        try { return card.EnergyCost.GetAmountToSpend(); }
        finally { _calculating = false; }
    }
    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        bool replace = !_calculating && card.Owner == Owner.Player && !card.EnergyCost.CostsX;
        modifiedCost = replace ? 0 : originalCost;
        return replace;
    }
    public override bool ShouldPlay(CardModel card, AutoPlayType autoPlayType) =>
        card.Owner != Owner.Player || autoPlayType != AutoPlayType.None || ManaCmd.Total(card.Owner) >= Cost(card);
}
