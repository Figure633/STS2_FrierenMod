using FrierenMod.Core;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace FrierenMod.Scripts.Cards;

public sealed class HellFireSummoning() : FrierenCard(0, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(8, ValueProp.Move)];
    public override async Task BeforeCombatStart()
    {
        if (Pile?.Type != PileType.Draw) return;
        var power = Owner.Creature.GetPower<SealPower>();
        if (power == null)
            power = await PowerCmd.Apply<SealPower>(new ThrowingPlayerChoiceContext(), Owner.Creature, 1, Owner.Creature, this);
        power!.Cards.Add(this);
        Pile.RemoveInternal(this);
    }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        int hits = LegendaryCards.ChantsThisTurn(Owner) * 2;
        if (hits > 0) await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(play.Target!).WithHitCount(hits).Execute(context);
        EnergyCost.AddThisTurn(1);
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2);
}
