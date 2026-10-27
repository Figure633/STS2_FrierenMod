using MegaCrit.Sts2.Core.Entities.Powers;
namespace FrierenMod.Scripts.Powers;

public sealed class BanManaGainPower : TurnOnlyPower
{
    public override PowerType Type => PowerType.Debuff;
}
