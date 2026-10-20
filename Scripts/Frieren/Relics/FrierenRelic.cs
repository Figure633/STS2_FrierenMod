using BaseLib.Abstracts;
using BaseLib.Utils;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Presentation;
using FrierenMod.Scripts.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace FrierenMod.Scripts.Relics;

[Pool(typeof(FrierenRelicPool))]
public abstract class FrierenRelic : CustomRelicModel
{
    public override string PackedIconPath => $"res://FrierenMod/images/relics/{GetType().Name}.tres";
    protected override string PackedIconOutlinePath => $"res://FrierenMod/images/relics/{GetType().Name}Outline.tres";
    protected override string BigIconPath => $"res://FrierenMod/images/relics/{GetType().Name}.png";
    protected override IEnumerable<IHoverTip> ExtraHoverTips => FrierenHoverTips.ForText(DynamicDescription.GetRawText())
        .Concat(this switch {
            MirroredLotusRing => HoverTipFactory.FromCardWithCardHoverTips<Himmel>(),
            HimmelGravestone => HoverTipFactory.FromCardWithCardHoverTips<LongevityPain>(),
            ComfortableBed => HoverTipFactory.FromCardWithCardHoverTips<Laziness>(),
            EarAccessory or IcicleCherryBlossom => HoverTipFactory.FromCardWithCardHoverTips<Mana>(),
            _ => []
        });
}

public abstract class CountingFrierenRelic : FrierenRelic
{
    private int _counter;
    [SavedProperty]
    public int Counter {
        get => _counter;
        set { AssertMutable(); _counter = value; UpdateDisplay(); }
    }
    protected virtual void UpdateDisplay() => InvokeDisplayAmountChanged();
    public override bool ShowCounter => true;
    public override int DisplayAmount => Counter;
}

// 一代 Boss 遗物暂按作者决定进入稀有池，使用原生能量上限钩子。
public abstract class EnergyFrierenRelic : FrierenRelic
{
    public override RelicRarity Rarity => RelicRarity.Rare;
    public override decimal ModifyMaxEnergy(Player player, decimal amount) => player == Owner ? amount + 1 : amount;
}
