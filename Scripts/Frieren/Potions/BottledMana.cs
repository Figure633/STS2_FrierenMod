using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Mechanics;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace FrierenMod.Scripts.Potions;

public sealed class BottledMana : FrierenPotion
{
    public override PotionRarity Rarity => PotionRarity.Common;
    public override string CustomPackedImagePath => "res://FrierenMod/images/potions/BottledMana.png";
    public override string CustomPackedOutlinePath => "res://FrierenMod/images/potions/BottledManaOutline.png";
    public override string CustomLargeImagePath => "res://FrierenMod/images/potions/BottledManaLarge.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(3)];
    public override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard<Mana>()];
    protected override async Task OnUse(PlayerChoiceContext context, Creature? target) =>
        await ManaCmd.Generate(Owner, PileType.Hand, DynamicVars.Cards.IntValue);
}
