using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Mechanics;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;

namespace FrierenMod.Scripts.Potions;

public sealed class BottledMana : FrierenPotion
{
    public override PotionRarity Rarity => PotionRarity.Common;
    // 一代使用原版药瓶轮廓；二代沿用同色原生药瓶。
    public override string CustomPackedImagePath => ModelDb.Potion<SwiftPotion>().ImagePath;
    public override string? CustomPackedOutlinePath => ModelDb.Potion<SwiftPotion>().OutlinePath;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(3)];
    public override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard<Mana>()];
    protected override async Task OnUse(PlayerChoiceContext context, Creature? target) =>
        await ManaCmd.Generate(Owner, PileType.Hand, DynamicVars.Cards.IntValue);
}
