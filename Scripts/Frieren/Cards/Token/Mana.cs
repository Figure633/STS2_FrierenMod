using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using FrierenMod.Scripts.Mechanics;

namespace FrierenMod.Scripts.Cards;

// 普通魔力与高密度魔力共用同调结算。
public sealed class Mana() : FrierenCard(0, CardType.Status, CardRarity.Token, TargetType.Self, false), IManaCard
{
    public override int MaxUpgradeLevel => 0;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) => SynchroCmd.Play(context, this);
}
