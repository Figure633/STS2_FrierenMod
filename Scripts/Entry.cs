using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using FrierenMod.Scripts.Mechanics;

namespace FrierenMod.Scripts;

// 必须要加的属性，用于注册Mod。字符串和初始化函数命名一致。
[ModInitializer("Init")]
public class Entry
{
    // 初始化函数
    public static void Init()
    {
        // 打patch（即修改游戏代码的功能）用
        // 传入参数随意，只要不和其他人撞车即可
        var harmony = new Harmony("sts2.arkalin.frierenmod");
        _ = MagicDeckStore.Data;
        BaseLib.Utils.CustomLocTableManager.Register("frieren");
        BaseLib.Config.ModConfigRegistry.Register("FrierenMod", new Presentation.PresentationSettings());
        harmony.PatchAll();
        Log.Info("FrierenMod 0.5.2: 三幕通关结算修复版已加载。");
    }
}
