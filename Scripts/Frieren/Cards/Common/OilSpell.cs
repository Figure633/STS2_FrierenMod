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

public sealed class OilSpell() : LegendaryCard(2, CardType.Attack, CardRarity.Common, TargetType.AllEnemies)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(12, ValueProp.Move), new DynamicVar("SecondDamage", 8)];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var targets = Owner.Creature.CombatState!.Enemies.Where(x => x.IsAlive).ToArray();
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).TargetingAllOpponents(CombatState!).Execute(context);
        if (targets.All(x => x.IsAlive))
            await DamageCmd.Attack(DynamicVars["SecondDamage"].BaseValue).FromCard(this).TargetingAllOpponents(CombatState!).Execute(context);
    }
    protected override void OnUpgrade() => DynamicVars["SecondDamage"].UpgradeValueBy(4);
}
