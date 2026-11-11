using FrierenMod.Scripts.Mechanics;
using MegaCrit.Sts2.Core.Localization;

namespace FrierenMod.Scripts.Cards;

public abstract class DragonBall : MagicPropCard
{
    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        description.Add("Remaining", IsMutable && Owner != null ? 7 - DragonBallStore.Held(Owner) : 6);
    }
}
