using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Relics;
using MegaCrit.Sts2.Core.Unlocks;

namespace FrierenMod.Scripts.Characters;

public sealed class FrierenCardPool : CustomCardPoolModel
{
    public override string Title => "Frieren";
    public override Color DeckEntryCardColor => new("e5e0f0");
    public override bool IsColorless => false;
    public override string BigEnergyIconPath => "res://FrierenMod/images/energy/card_icon.png";
    public override string TextEnergyIconPath => "res://FrierenMod/images/energy/text_icon.png";
    // 保留因子模型注册及存档 ID；普通奖励、商店和生成入口只取得可玩卡牌。
    protected override IEnumerable<CardModel> FilterThroughEpochs(UnlockState unlockState, IEnumerable<CardModel> cards) =>
        base.FilterThroughEpochs(unlockState, cards).Where(card => card is not FactorCard and not TimeConcept);
}
