using FrierenMod.Scripts.Shared.Cards;
using BaseLib.Utils;
using FrierenMod.Scripts.Characters;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using Godot;
using FrierenMod.Scripts.Presentation;

namespace FrierenMod.Scripts.Cards;

[Pool(typeof(FrierenCardPool))]
public abstract class FrierenCard(int cost, CardType type, CardRarity rarity, TargetType target,
    bool showInLibrary = true) : ModCard(cost, type, rarity, target, showInLibrary)
{
    protected override string CardArtDirectory => "res://FrierenMod/images/cards";
    public override Texture2D? CustomFrame => FrierenCardFrames.Texture(this);
    public override Material? CreateCustomFrameMaterial => FrierenCardFrames.Material(this);
    protected override IEnumerable<IHoverTip> ExtraHoverTips => FrierenHoverTips.For(this);
}
