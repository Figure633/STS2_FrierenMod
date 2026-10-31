using MegaCrit.Sts2.Core.Entities.Powers;

namespace FrierenMod.Scripts.Powers;

// 小把戏及其衍生牌已弃用；保留模型 ID 读取旧档，不再生成弃用卡。
public sealed class TrickPower : FrierenPower
{
    public override PowerType Type => PowerType.None;
    public override PowerStackType StackType => PowerStackType.None;
}
