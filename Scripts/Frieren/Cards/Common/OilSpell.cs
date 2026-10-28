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
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(11,ValueProp.Move),new DynamicVar("Vulnerable",1)];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var firstTargets=CombatState!.Enemies.Where(c=>c.IsAlive).ToArray();
        if(firstTargets.Length==0)return;
        for(int hit=0;hit<2;hit++) {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCardCompatibility(this,play).TargetingAllOpponents(CombatState).Execute(context);
            await PowerCmd.Apply<VulnerablePower>(context,CombatState.Enemies.Where(c=>c.IsAlive).ToArray(),DynamicVars["Vulnerable"].BaseValue,Owner.Creature,this);
            if(firstTargets.Any(c=>!c.IsAlive))break;
        }
    }
    protected override void OnUpgrade()=>DynamicVars["Vulnerable"].UpgradeValueBy(1);
}
