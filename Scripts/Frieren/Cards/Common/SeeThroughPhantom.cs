using FrierenMod.Core;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace FrierenMod.Scripts.Cards;

public sealed class SeeThroughPhantom() : FrierenCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(8, ValueProp.Move), new DynamicVar("Amount", 1)];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(play.Target!).Execute(context);
        if (play.Target!.Monster?.NextMove?.Intents.Any(i => i is MegaCrit.Sts2.Core.MonsterMoves.Intents.AttackIntent) == true)
            await PowerCmd.Apply<WeakPower>(context, play.Target, (int)DynamicVars["Amount"].BaseValue, Owner.Creature, this);
        else await PowerCmd.Apply<VulnerablePower>(context, play.Target, (int)DynamicVars["Amount"].BaseValue, Owner.Creature, this);
    }
    protected override void OnUpgrade() { DynamicVars.Damage.UpgradeValueBy(2); DynamicVars["Amount"].UpgradeValueBy(1); }
}
