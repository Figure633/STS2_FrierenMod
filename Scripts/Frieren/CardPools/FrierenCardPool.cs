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
    // 解锁池也供百科使用；随机获取限制只在 CardCreationOptions / CardFactory 入口处理。
}
