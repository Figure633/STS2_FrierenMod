using MegaCrit.Sts2.Core.Entities.Powers;

namespace FrierenMod.Scripts.Powers;

// 线索按实际获得顺序保留，单个图标不显示层数；与顺序状态一起在回合结束移除。
public abstract class PuzzleCluePower : TurnOnlyPower
{
    public override PowerStackType StackType => PowerStackType.Single;
}
