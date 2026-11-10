using BaseLib.Utils;
using FrierenMod.Scripts.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace FrierenMod.Scripts.Cards;

public sealed class ArcaneNet() : FrierenCard(10,CardType.Attack,CardRarity.Rare,TargetType.AllEnemies)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new DamageVar(19,ValueProp.Move)];
    public override bool TryModifyEnergyCostInCombat(CardModel card,decimal cost,out decimal modifiedCost)
    {
        modifiedCost=cost;
        if(card!=this)return false;
        modifiedCost=Math.Max(0,cost-SynchroCmd.CountThisCombat(Owner));
        return modifiedCost!=cost;
    }
    protected override async Task OnPlay(PlayerChoiceContext context,CardPlay play)=>
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCardCompatibility(this,play).TargetingAllOpponents(CombatState!).Execute(context);
    protected override void OnUpgrade()=>DynamicVars.Damage.UpgradeValueBy(5);
}
