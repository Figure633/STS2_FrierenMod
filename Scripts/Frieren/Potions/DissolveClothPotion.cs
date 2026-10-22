using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace FrierenMod.Scripts.Potions;

public sealed class DissolveClothPotion : FrierenPotion
{
    public override PotionRarity Rarity => PotionRarity.Uncommon;
    public override TargetType TargetType => TargetType.AnyEnemy;
    public override string CustomPackedImagePath => "res://FrierenMod/images/potions/DissolveClothPotion.png";
    public override string CustomPackedOutlinePath => "res://FrierenMod/images/potions/DissolveClothPotionOutline.png";
    public override string CustomLargeImagePath => "res://FrierenMod/images/potions/DissolveClothPotionLarge.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<WeakPower>(3), new PowerVar<VulnerablePower>(3)];
    public override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<WeakPower>(), HoverTipFactory.FromPower<VulnerablePower>()];
    protected override async Task OnUse(PlayerChoiceContext context, Creature? target)
    {
        AssertValidForTargetedPotion(target);
        await CreatureCmd.LoseBlock(target, target.Block);
        await PowerCmd.Apply<VulnerablePower>(context, target, DynamicVars.Vulnerable.IntValue, Owner.Creature, null);
        await PowerCmd.Apply<WeakPower>(context, target, DynamicVars.Weak.IntValue, Owner.Creature, null);
    }
}
