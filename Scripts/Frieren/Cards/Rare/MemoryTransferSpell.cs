using BaseLib.Utils;
using FrierenMod.Scripts.Mechanics;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace FrierenMod.Scripts.Cards;

public sealed class MemoryTransferSpell() : LegendaryCard(1,CardType.Attack,CardRarity.Rare,TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars=>[new DamageVar(14,ValueProp.Move),new DynamicVar("Reduction",1)];
    protected override async Task OnPlay(PlayerChoiceContext context,CardPlay play)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCardCompatibility(this,play).Targeting(play.Target!).Execute(context);
        var cards=await CardSelectCmd.FromHand(context,Owner,new CardSelectorPrefs(SelectionScreenPrompt,1),null,this);
        foreach(var card in cards)card.EnergyCost.AddThisTurn(-DynamicVars["Reduction"].IntValue);
        Owner.PlayerCombatState?.Hand.InvokeContentsChanged();
    }
    protected override void OnUpgrade() { DynamicVars["Reduction"].UpgradeValueBy(1); DynamicVars.Damage.UpgradeValueBy(3); }
}
