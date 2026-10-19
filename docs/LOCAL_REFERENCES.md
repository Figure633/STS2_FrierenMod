# 本地二代源码与模组参考

2026-10-19 已将参考资料放在外层工作区 `D:/program/STS2_FrierenMod/reference/`。这些目录不属于本模组 Git 仓库，不参与编译或打包。来源与提交记录见该目录的 `sources.json`。

## 二代运行代码

`STS2_v0.107.1/`：使用 ILSpyCmd 9.1.0.7988 从本机安装的 `data_sts2_windows_x86_64/sts2.dll` 反编译，共 3425 个 C# 文件。目录版本名沿用当前项目支持基线；精确输入以 `sources.json` 的 SHA256 为准。它是当前本地程序集的可读还原，包含编译生成代码，并非官方发布的原始源码，也未验证可重新编译。游戏更新后需重新导出并重新核验接口。

输入 SHA256：`A1F9E653F1E28E4076558FEE1E60D218619CB7E057B887C6417F62C62C6D7A52`。

目录按命名空间分组。后续实现优先查找下列入口：

| 需求 | 文件或目录（相对该参考目录） | 已核查的用法 |
|---|---|---|
| 抽牌与弃牌 | `MegaCrit.Sts2.Core.Models.Cards/Prepared.cs` | 等待 `CardPileCmd.Draw`，通过 `CardSelectCmd.FromHandForDiscard` 选择，再调用 `CardCmd.Discard`；沿用选择上下文 |
| 选牌与同步 | `MegaCrit.Sts2.Core.Commands/CardSelectCmd.cs` | 原生网格和三选一内部负责本地选择及远端结果同步，优先复用这些入口 |
| 选择归属 | `MegaCrit.Sts2.Core.GameActions.Multiplayer/PlayerChoiceContext.cs` | 上下文维护涉及模型的栈，嵌套触发不能丢失归属 |
| 攻击、伤害、能力 | `MegaCrit.Sts2.Core.Commands.Builders/AttackCommand.cs`、`MegaCrit.Sts2.Core.Commands/` | 查对应命令的参数、钩子和结算顺序，避免直接改底层字段 |
| 原版卡与能力 | `MegaCrit.Sts2.Core.Models.Cards/`、`MegaCrit.Sts2.Core.Models.Powers/` | 查找相近效果，优先使用已有能力和动态变量 |
| 药水获得 | `MegaCrit.Sts2.Core.Models.Cards/Alchemize.cs` | 原版使用 `PotionFactory`、战斗药水 RNG 与 `PotionCmd.TryToProcure` |
| 存档字段 | `MegaCrit.Sts2.Core.Saves.Runs/SavedProperties.cs`、`SavedPropertyAttribute.cs` | 对照原生属性存取，再结合 BaseLib 扩展字段 |
| 回合生命周期 | `MegaCrit.Sts2.Core.Combat/CombatManager.cs` | 额外回合、战斗结束和能力钩子需核查实际调用顺序 |
| 首次教程 | `MegaCrit.Sts2.Core.Nodes.Ftue/NFtue.cs` 及同目录 | 基类负责阻断输入和退出清理，可作为魔法书教程接入参考；场景和账号标记仍需另查 |

本次完成资料整理及以上代表性代码阅读，没有因此改动已实现卡牌的玩法，也没有宣称完成全量源码研究或联机验证。

## 下载的模组参考

| 本地目录 | 来源与冻结提交 | 主要借鉴内容 |
|---|---|---|
| `Downfall/` | https://github.com/lamali292/Downfall · `147c9fcddc26ac799e5530d4a7c532a90f635dd2` | 角色模块、独立卡池、公共基类；`Hermit.csproj` 引用公共工程，`HermitCode/Cards/HermitCardModel.cs` 指定自己的 Pool |
| `WatcherMod/` | https://github.com/lamali292/WatcherMod · `d5637b7b04b59cdb7c630739ffbbd84fc74deb0d` | 卡牌、姿态、命令与兼容层分工；`WatcherCode/Abstract/WatcherCardModel.cs` 集中共用变量、悬停与资源路径 |
| `Huoreimu/` | https://github.com/Andr0idCat/Huoreimu · `26302266f9240c13363040753f1c89c7f5b89c60` | `HuoreimuCode/Cards/HuoreimuCards.cs` 指定角色卡池、资源与音效；专属机制使用独立基类 |
| `BaseLib-StS2/` | https://github.com/Alchyr/BaseLib-StS2 · `c070755d11ec062f6c97c4dda1a205d975ed066a` | 当前声明 v3.4.7；重点查扩展存档、手牌上限、自定义角色及跨牌堆选择 |

BaseLib 已核查 `Patches/Utils/SavedSpireFieldPatch.cs`、`Hooks/IMaxHandSizeModifier.cs` 与 `Commands/MultiPileCardSelect.cs`。后者已有多牌堆筛选和来源标记，可供后续相关选牌功能使用。具体接口仍以实际安装程序集为准。

UID 处理也有差异：Watcher 忽略 `*.uid` 并保留个别必要例外；Downfall 忽略 `*cs.uid`；Huoreimu 仍跟踪一些 `.cs.uid`。因此不能把“优秀模组都不用 UID”作为规则，是否保留取决于场景引用和导入验证。

这些仓库保留各自许可证和 Git 来源；目前仅用于本地阅读，未构建、安装或替换现有依赖。后续引用代码需核查相应许可证。Downfall 本地快照比此前架构文档所核查提交更新，旧文档中的历史提交保持原样。
