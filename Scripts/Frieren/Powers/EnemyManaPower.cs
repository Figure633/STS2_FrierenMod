using MegaCrit.Sts2.Core.Localization;

namespace FrierenMod.Scripts.Powers;

public sealed class EnemyManaPower : FrierenPower
{
    // 保留1层用于表示0魔力，避免原生引擎移除0层能力。
    public int StoredMana => Math.Max(0, Amount - 1);
    public override int DisplayAmount => StoredMana;
    public override LocString Description { get { var text = base.Description; text.Add("StoredMana", StoredMana); return text; } }
}
