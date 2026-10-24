using BaseLib.Hooks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace FrierenMod.Scripts.Powers;

public sealed class SwallowedPower : FrierenPower, IMaxHandSizeModifier
{
    public override PowerType Type => PowerType.Debuff;
    public int ModifyMaxHandSizeLate(Player player, int current) => player == Owner.Player ? Math.Max(0, current - Amount) : current;
}
