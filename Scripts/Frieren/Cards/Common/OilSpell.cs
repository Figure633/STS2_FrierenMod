using BaseLib.Utils;
using FrierenMod.Scripts.Mechanics;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace FrierenMod.Scripts.Cards;

public sealed class OilSpell() : LegendaryCard(2, CardType.Attack, CardRarity.Common, TargetType.AllEnemies)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(10,ValueProp.Move),new DamageVar("SecondDamage",10,ValueProp.Move)];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var firstTargets=CombatState!.Enemies.Where(c=>c.IsAlive).ToArray();
        if(firstTargets.Length==0)return;
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCardCompatibility(this,play).TargetingAllOpponents(CombatState).Execute(context);
        if(firstTargets.Any(c=>!c.IsAlive))return;
        await DamageCmd.Attack(DynamicVars["SecondDamage"].BaseValue).FromCardCompatibility(this,play).TargetingAllOpponents(CombatState).Execute(context);
        await PowerCmd.Apply<VulnerablePower>(context,CombatState.Enemies.Where(c=>c.IsAlive).ToArray(),2,Owner.Creature,this);
    }
    protected override void OnUpgrade()=>DynamicVars["SecondDamage"].UpgradeValueBy(2);
}
