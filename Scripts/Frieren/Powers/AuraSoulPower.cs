namespace FrierenMod.Scripts.Powers;

public sealed class AuraSoulPower : FrierenPower
{
    public override MegaCrit.Sts2.Core.Entities.Powers.PowerType Type => MegaCrit.Sts2.Core.Entities.Powers.PowerType.Debuff;
    public override bool OwnerIsSecondaryEnemy => true;
}
