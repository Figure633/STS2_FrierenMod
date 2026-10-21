using Godot;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;

namespace FrierenMod.Scripts.UI;

// 与原生卡组查看使用同一界面层，打开时由原版切换活动屏幕并暂停下层交互。
public partial class MagicBookView : Control, ICapstoneScreen
{
    public NetScreenType ScreenType => NetScreenType.DeckView;
    public bool UseSharedBackstop => true;
    public Control? DefaultFocusedControl { get; set; }
    public void AfterCapstoneOpened() => Show();
    public void AfterCapstoneClosed() => Hide();
}
