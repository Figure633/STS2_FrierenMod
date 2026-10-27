using MegaCrit.Sts2.Core.Entities.Relics;

namespace FrierenMod.Scripts.Relics;

public sealed class AuraCompanion : FrierenRelic
{
    public override RelicRarity Rarity => RelicRarity.Event;
    protected override string BigIconPath => PackedIconPath;
}
