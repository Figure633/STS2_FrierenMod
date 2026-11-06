namespace FrierenMod.Scripts.Powers;

public sealed class AuraScalesPower : FrierenPower
{
    public override MegaCrit.Sts2.Core.Localization.LocString Description {
        get { var text = base.Description; text.Add("EnergyLoss", Owner?.Monster is Monsters.Aura aura ? aura.SubmissionEnergy : 1); return text; }
    }
    public override MegaCrit.Sts2.Core.Entities.Powers.PowerType Type => MegaCrit.Sts2.Core.Entities.Powers.PowerType.None;
}
