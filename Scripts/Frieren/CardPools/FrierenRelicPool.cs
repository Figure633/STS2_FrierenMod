using BaseLib.Abstracts;

namespace FrierenMod.Scripts.Characters;

public sealed class FrierenRelicPool : CustomRelicPoolModel
{
    public override string BigEnergyIconPath => "res://FrierenMod/images/energy/card_icon.png";
    public override string TextEnergyIconPath => "res://FrierenMod/images/energy/text_icon.png";
    // 一代注册时即标记遗物可见，事件专属与未来角色的初始遗物也可查阅。
    public override bool SeenByDefault => true;
}
