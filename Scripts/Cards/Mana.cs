using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using FrierenMod.Scripts.Powers;

namespace FrierenMod.Scripts.Cards;

// 一代 Mana 的 SYNCHRO 提供基础抽一张、消耗；高级同调由后续专项移植处理。
public sealed class Mana() : FrierenCard(0, CardType.Status, CardRarity.Token, TargetType.Self, false)
{
    public override int MaxUpgradeLevel => 0;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        if (Owner.Creature.GetPower<ThunderPower>() != null)
            await DamageCmd.Attack(15).FromCard(this).TargetingRandomOpponents(CombatState!).Execute(context);
        else await CardPileCmd.Draw(context, DynamicVars.Cards.BaseValue, Owner);
        foreach (var fire in Owner.PlayerCombatState!.DiscardPile.Cards.OfType<LittleFire>().ToArray())
            await CardPileCmd.Add(fire, PileType.Hand);
    }
}
