using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace FrierenMod.Scripts.Powers;

public sealed class SealingOrePower : FrierenPower
{
    public override PowerType Type => PowerType.Debuff;
    public override string CustomPackedIconPath => ModelDb.Power<DarkShacklesPower>().PackedIconPath;
    public override string CustomBigIconPath => ModelDb.Power<DarkShacklesPower>().ResolvedBigIconPath;
}
