using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace FrierenMod.Scripts.Cards;

public abstract class LegendaryCard(int cost, CardType type, CardRarity rarity, TargetType target, bool showInLibrary = true)
    : FrierenCard(cost, type, rarity, target, showInLibrary), ILegendaryCard
{
    protected override bool IsPlayable => Owner != null && LegendaryCards.ChantsThisTurn(Owner) > 0;
}
