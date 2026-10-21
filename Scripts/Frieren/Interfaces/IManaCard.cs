using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Localization;

namespace FrierenMod.Scripts.Cards;

// 魔力是一种资源身份；普通魔力和高密度魔力都可支付咏唱。
public interface IManaCard : ICustomTypeTextCard
{
    IEnumerable<LocString> ICustomTypeTextCard.GetTypeModifiers() => [new("frieren", "MANA_TYPE")];
}
