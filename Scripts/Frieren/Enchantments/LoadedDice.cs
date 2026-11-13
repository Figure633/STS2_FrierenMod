using BaseLib.Abstracts;
using FrierenMod.Scripts.Cards;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace FrierenMod.Scripts.Enchantments;

public sealed class LoadedDice : CustomEnchantmentModel
{
    protected override string CustomIconPath => "res://FrierenMod/images/enchantments/LoadedDice.svg";
    public override bool HasExtraCardText => true;
    public override bool CanEnchant(CardModel card) => card is HonestLiving && base.CanEnchant(card);
}
