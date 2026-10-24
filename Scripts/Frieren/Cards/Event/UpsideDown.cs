using FrierenMod.Scripts.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace FrierenMod.Scripts.Cards;

public sealed class UpsideDown() : FrierenCard(-1, CardType.Curse, CardRarity.Curse, TargetType.None)
{
    public override int MaxUpgradeLevel => 0;
    public override bool CanBeGeneratedInCombat => false;
    public override bool CanBeGeneratedByModifiers => false;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable];
    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay play)
    {
        if (Pile?.Type != PileType.Hand || play.Card.Owner != Owner || !ManaCmd.IsMana(play.Card)) return;
        var state = Owner.PlayerCombatState!;
        var draw = state.DrawPile.Cards.ToArray(); var discard = state.DiscardPile.Cards.ToArray();
        // 先快照再逐张通过原生换堆，保留牌对象与牌堆变更通知；不视为抽牌或弃牌。
        foreach (var card in draw) await CardPileCmd.Add(card, PileType.Discard, CardPilePosition.Top);
        foreach (var card in discard) await CardPileCmd.Add(card, PileType.Draw, CardPilePosition.Top);
        await CreatureCmd.Damage(context, Owner.Creature, 2, ValueProp.Unblockable | ValueProp.Unpowered, null, this);
    }
}
