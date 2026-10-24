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

public sealed class SynchroWithoutManaPower : FrierenPower
{
    public override (PileType, CardPilePosition) ModifyCardPlayResultPileTypeAndPosition(CardModel card, bool isAutoPlay, ResourceInfo resources, PileType pileType, CardPilePosition position) =>
        card.Owner == Owner.Player && card is IManaCard ? (PileType.Hand, position) : (pileType, position);
    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay play)
    {
        if (play.Card.Owner != Owner.Player || play.Card is not IManaCard) return;
        // 原生出牌去向在结算前已确定；最后一层也须保留高密度魔力的能量收益。
        if (play.ResultPile == PileType.Hand && play.Card is HighDensityMana dense)
            await PlayerCmd.GainEnergy(dense.DynamicVars["Amount"].BaseValue, Owner.Player!);
        await PowerCmd.Decrement(this);
    }
}
