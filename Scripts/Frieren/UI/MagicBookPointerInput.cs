using Godot;

namespace FrierenMod.Scripts.UI;

// 输入节点位于视口末尾；卡面仍绘制在菜单下方，鼠标不依赖卡牌悬停焦点。
public partial class MagicBookPointerInput : Node
{
    public Func<InputEventMouseButton, bool>? HandleButton { get; set; }
    public override void _Input(InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouseButton button && HandleButton?.Invoke(button) == true)
            GetViewport().SetInputAsHandled();
    }
}
