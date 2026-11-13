# FrierenMod STS2

《杀戮尖塔2》的芙莉莲模组，移植自[一代模组](https://steamcommunity.com/sharedfiles/filedetails/?id=3164722329)。当前版本 **0.12.41**，开发中。

芙莉莲使用抽牌堆、手牌与弃牌堆中的魔力进行咏唱，可装配因子、使用魔具，包含专属卡牌、遗物、事件、委托与Boss。多人游戏双方需使用相同版本的模组和BaseLib；混合角色队伍保留原版事件和Boss池。

0.12.41 为「视觉攻击魔法」的444击杀复用原版「华丽收场」的蓄势与命中特效。

0.12.40 为「生生流转」补充战斗中的实际伤害提示，包含即将生成的魔力与伤害修正。

0.12.39 修复精准咏唱、终极咏唱忽略削弱咏唱和集中修正而计算错误X、直接取消结算的问题。

0.12.38 修复最终幕Boss战后仍生成魔法牌组奖励的问题，包含额外魔法奖励。同步士兵死亡不重选阿乌拉已公布意图的修复。

0.12.37 加强「拼命守护宝库的魔法」：升级后的每次基础格挡从8提高至9，仍额外生成1张魔力。

0.12.35 调整「小小火花」为 3（5）点伤害，「星星之火」为 13（18）点群伤，升级前后均生成 X 张「小小火花」。所有魔力牌隐藏费用，始终免费打出。

0.12.34 将「哈比夏魔法大学公开课」的委托条件提高到主牌组拥有 8 张已升级牌。

0.12.33调整黑金之翼、飞行魔法和圣雪结晶，灌铅保留消耗；修复准备架势多余入堆动画和复制体末击受力量影响，调整圣雪结晶事件构图及克拉伏特礼物代价。首幕三星委托补齐专属魔具奖励，伪作奖励仅附固定100金币。

0.12.36 将赛莉耶动作描写改为可见星号；隐藏不可打出的魔力包裹费用；阿乌拉仅在两名士兵存活时冥想，冥想时士兵必定攻击；首次解除魔法立即触发嘲讽，断头台意图时则消耗嘲讽机会并照常攻击。

## 安装

依赖 **BaseLib 3.4.7 或 3.4.8**；使用3.4.8时保留其随附的兼容助手。完整更新 `FrierenMod.dll`、`FrierenMod.pck` 和 `FrierenMod.json`，放入游戏的 `mods/FrierenMod/`，重启游戏并启用模组。

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
