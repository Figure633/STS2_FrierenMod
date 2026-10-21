using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Runs;

namespace FrierenMod.Scripts.Relics;

// 作者已废弃的一代角色切换原型遗物：保留旧档 ID，不再获取或生效。
public sealed class HaitaaWand : FrierenRelic
{
    public override RelicRarity Rarity => RelicRarity.None;
    public override bool IsAllowed(IRunState runState) => false;
    public override bool IsAllowedInShops => false;
}
