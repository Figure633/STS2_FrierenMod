# FrierenMod STS2

《杀戮尖塔2》的芙莉莲模组，移植自[一代模组](https://steamcommunity.com/sharedfiles/filedetails/?id=3164722329)。当前版本 **0.12.31**，开发中。

芙莉莲使用抽牌堆、手牌与弃牌堆中的魔力进行咏唱，可装配因子、使用魔具，包含专属卡牌、遗物、事件、委托与Boss。多人游戏双方需使用相同版本的模组和BaseLib；混合角色队伍保留原版事件和Boss池。

0.12.31实装作者选定的赛莉耶简笔图标，地图沿用原生先古状态配色；补齐其他角色遇见赛莉耶时的中英文通用对话。芙莉莲仍按0、1、2、3、2、3的顺序触发专属对话。

## 安装

依赖 **BaseLib 3.4.7**。完整更新 `FrierenMod.dll`、`FrierenMod.pck` 和 `FrierenMod.json`，放入游戏的 `mods/FrierenMod/`，重启游戏并启用模组。

## 构建

使用 .NET SDK 9.0.306、Godot 4.5.1和游戏程序集。将 `local.props.example` 复制为 `local.props`，配置本机游戏、BaseLib及Godot路径。

```powershell
./tools/build.ps1
./tools/build.ps1 -Pack -GodotPath "D:/Tools/Godot/Godot_v4.5.1-stable_win64_console.exe"
./tools/install.ps1
```

构建结果位于 `artifacts/FrierenMod/`；安装前需退出游戏。基础构建包含规则、集成、接口及资源检查。引擎检查另加 `-EngineTests -GodotMonoPath <Godot .NET路径>`，会使用内存测试存档。自动检查不能代替实际游戏验收。

源码位于 `Core/` 和 `Scripts/`，运行资源位于 `FrierenMod/`，测试位于 `Tests/`，构建与资源处理工具位于 `tools/`。常规构建不需要作者的需求文档或素材原稿。

## 来源与许可

一代素材与改编内容遵循 [LICENSE.STS1](LICENSE.STS1)。神格相关代码与视觉改编自WatcherMod，遵循 [LICENSE.WATCHER](LICENSE.WATCHER)。运行资源的来源及指纹保留在 `tools/data/assets.json` 和 `tools/data/generated_assets.json`。
