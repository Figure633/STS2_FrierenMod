using FrierenMod.Scripts.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace FrierenMod.Scripts.Cards;

public sealed class TreasureVaultSpell() : FrierenCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override bool GainsBlock => true;
    protected override bool HasEnergyCostX => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(8, ValueProp.Move), new DynamicVar("ExtraMana", 0)];
    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        bool inCombat = IsMutable && Owner?.PlayerCombatState != null && CombatState != null;
        int energy = inCombat ? Pile?.Type == PileType.Play ? EnergyCost.CapturedXValue : EnergyCost.GetAmountToSpend() : 0;
        int x = inCombat ? Hook.ModifyXValue(CombatState!, this, energy) : 0;
        description.Add("InCombat", inCombat);
        description.Add("ActualTimes", x);
        description.Add("ActualMana", x + DynamicVars["ExtraMana"].IntValue);
    }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        int x = ResolveEnergyXValue();
        for (int i = 0; i < x; i++) await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
        await ManaCmd.Generate(Owner, PileType.Draw, x + DynamicVars["ExtraMana"].IntValue);
    }
    protected override void OnUpgrade() => DynamicVars["ExtraMana"].UpgradeValueBy(1);
}
