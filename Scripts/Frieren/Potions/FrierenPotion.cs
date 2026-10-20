using BaseLib.Abstracts;
using BaseLib.Utils;
using FrierenMod.Scripts.Characters;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Potions;

namespace FrierenMod.Scripts.Potions;

[Pool(typeof(FrierenPotionPool))]
public abstract class FrierenPotion : CustomPotionModel
{
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.Self;
}
