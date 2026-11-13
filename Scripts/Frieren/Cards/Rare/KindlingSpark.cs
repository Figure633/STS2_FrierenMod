using BaseLib.Utils;
using FrierenMod.Scripts.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace FrierenMod.Scripts.Cards;

public sealed class KindlingSpark() : FrierenCard(0, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
{
    protected override bool HasEnergyCostX => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(13, ValueProp.Move)];

    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        bool inCombat = IsMutable && Owner?.PlayerCombatState != null && CombatState != null;
        description.Add("InCombat", inCombat);
        int energy = inCombat ? Pile?.Type == PileType.Play ? EnergyCost.CapturedXValue : EnergyCost.GetAmountToSpend() : 0;
        description.Add("ActualSparks", inCombat ? Hook.ModifyXValue(CombatState!, this, energy) : 0);
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        int count = ResolveEnergyXValue();
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCardCompatibility(this, play).TargetingAllOpponents(CombatState!).Execute(context);
        for (int i = 0; i < count; i++) await CardFlowCmd.Generate<LittleFire>(Owner, PileType.Hand);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(5);
}
