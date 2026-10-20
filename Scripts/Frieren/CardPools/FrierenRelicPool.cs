using BaseLib.Abstracts;

namespace FrierenMod.Scripts.Characters;

public sealed class FrierenRelicPool : CustomRelicPoolModel
{
    // 一代注册时即标记遗物可见，事件专属与未来角色的初始遗物也可查阅。
    public override bool SeenByDefault => true;
}
