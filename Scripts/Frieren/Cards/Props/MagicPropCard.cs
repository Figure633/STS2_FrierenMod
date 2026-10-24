using FrierenMod.Core;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace FrierenMod.Scripts.Cards;

// 魔具只用于魔法牌组选取与预览，永不进入普通战斗卡池。
public abstract class MagicPropCard() : FrierenCard(-1, CardType.Skill, CardRarity.Event, TargetType.Self), ICustomTypeTextCard
{
    public string PropId => GetType().Name;
    public override int MaxUpgradeLevel => 0;
    protected override bool IsPlayable => false;
    public IEnumerable<LocString> GetTypeModifiers() => [new("frieren", "PROP_TYPE")];
    public static MagicPropCard Create(Player player, string id)
    {
        _ = MagicPropRules.Get(id);
        var type = typeof(MagicPropCard).Assembly.GetType(typeof(MagicPropCard).Namespace + "." + id, true)!;
        var card = (MagicPropCard)ModelDb.GetById<CardModel>(ModelDb.GetId(type)).ToMutable();
        card.Owner = player;
        return card;
    }
}
