namespace FrierenMod.Scripts.Powers;

public sealed class AuraScalesPower : FrierenPower
{
    public override MegaCrit.Sts2.Core.Localization.LocString Description {
        get {
            var text = base.Description;
            text.Add(new MegaCrit.Sts2.Core.Localization.DynamicVars.EnergyVar("EnergyLoss", IsMutable && Owner?.Monster is Monsters.Aura aura ? aura.SubmissionEnergy : 1) {
                ColorPrefix = MegaCrit.Sts2.Core.Helpers.EnergyIconHelper.GetPrefix(MegaCrit.Sts2.Core.Models.ModelDb.Card<Cards.RapidChant>())
            });
            return text;
        }
    }
    public override MegaCrit.Sts2.Core.Entities.Powers.PowerType Type => MegaCrit.Sts2.Core.Entities.Powers.PowerType.None;
}
