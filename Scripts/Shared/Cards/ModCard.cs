using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;

namespace FrierenMod.Scripts.Shared.Cards;

// 通用展示约定不绑定角色或卡池；具体角色基类负责指定资源目录和 Pool。
public abstract class ModCard(int cost, CardType type, CardRarity rarity, TargetType target,
    bool showInLibrary = true) : CustomCardModel(cost, type, rarity, target, showInLibrary)
{
    protected abstract string CardArtDirectory { get; }
    protected virtual string ArtName => GetType().Name;
    public override string PortraitPath => $"{CardArtDirectory}/{ArtName}.png";
    public override string CustomPortraitPath => PortraitPath;
    public override string BetaPortraitPath => PortraitPath;
    protected override void AddExtraArgsToDescription(LocString description) => description.Add("Upgraded", IsUpgraded);
}
