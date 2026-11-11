using BaseLib.Abstracts;

namespace FrierenMod.Scripts.Characters;

public sealed class FrierenPotionPool : CustomPotionPoolModel
{
    public override IEnumerable<MegaCrit.Sts2.Core.Models.PotionModel> GetUnlockedPotions(MegaCrit.Sts2.Core.Unlocks.UnlockState unlockState) =>
        base.GetUnlockedPotions(unlockState).Where(p => p.Rarity != MegaCrit.Sts2.Core.Entities.Potions.PotionRarity.Event);
}
