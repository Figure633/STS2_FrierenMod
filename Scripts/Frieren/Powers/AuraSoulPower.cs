namespace FrierenMod.Scripts.Powers;

public sealed class AuraSoulPower : FrierenPower
{
    public override MegaCrit.Sts2.Core.Localization.LocString Description {
        get {
            var text = base.Description;
            text.Add("ManaLoss", 15 * (IsMutable && Owner?.CombatState is { } combat ? Math.Max(1, combat.Players.Count) : 1));
            return text;
        }
    }
    public override MegaCrit.Sts2.Core.Entities.Powers.PowerType Type => MegaCrit.Sts2.Core.Entities.Powers.PowerType.Debuff;
    public override bool OwnerIsSecondaryEnemy => true;
}
