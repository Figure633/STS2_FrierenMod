# 作者交接：代码导览与待办

截至 2026-10-18，游戏功能版本为 0.5.2。当前代码在原二代骨架之上完成本地移植，未提交或推送。此文描述当前快照，不承诺一代全量行为已还原。

## 基线与溯源

- 二代仓库：https://github.com/FrierenModForSlayTheSpire/FrierenMod_STS2 ，基线 `985668e9ced12e0be563c1ea00edd674f6e6c05d`。
- 一代参考：https://github.com/FrierenModForSlayTheSpire/FrierenMod_STS1 ，基线 `8604c9cd072dba6ae1bf8f47986fdc291b26c3cc`。
- 一代源码声明 2.3.0；当时安装的 Workshop JAR 声明 2.3.1。类名和资源比较见 `baseline.json`，该比较不代表逐方法一致。
- 当前完整工作树已入包，包括原仓库未跟踪的新源码。不要仅按基线提交号重建本次内容。
- 支持基线：StS2 v0.107.1 / BaseLib v3.4.7 / Windows / .NET SDK 9.0.306 / Godot 4.5.1。

## 代码地图

目录已于 2026-10-18 按角色模块整理，新增内容请先读 [代码结构](CODE_STRUCTURE.md)。现有类型命名空间和模型身份保留。

| 层次 | 文件/目录 | 职责 |
|---|---|---|
| 入口与角色 | Scripts/Entry.cs、Scripts/Frieren/Character/Frieren.cs | 模型/补丁注册，角色数据、起始牌组与展示 |
| 纯规则 | Core/Frieren/ChantRules.cs、SpellCatalog.cs | 咏唱费用、强度、因子目录及规则 |
| 魔法书状态 | Core/Frieren/MagicDeckState.cs | 因子、槽位、魔具、开局草稿、奖励补偿计数与序列化 |
| 魔具规则 | Core/Frieren/MagicPropRules.cs | 八种魔具的目标约束和效果，非法操作保持状态不变 |
| 游戏结算 | Scripts/Frieren/Mechanics/ChantCmd.cs、FactorEffects.cs、Mana*.cs | 调用二代原生命令，支付魔力、结算因子及回调 |
| 开局与奖励 | Scripts/Frieren/Mechanics/MagicDeckCmd.cs、Scripts/Frieren/Rewards/FactorReward.cs | 三槽各一次三选一、独立因子奖励 |
| 存档 | Scripts/Frieren/Mechanics/MagicDeckStore.cs | 玩家 SavedSpireField，兼容旧圣徽字段；移除圣徽仍保留数据 |
| 魔具命令 | Scripts/Frieren/Mechanics/MagicPropCmd.cs | 单人操作阶段限制、防重复提交、奖励 RNG、治疗/能量命令 |
| 卡牌与能力 | Scripts/Frieren/Cards/、Scripts/Frieren/Powers/ | 原生卡模型、预览、悬停、咏唱触发；Cards/Rare/FairyTaleEnd.cs 包含童话伤害预览 |
| 常驻 UI | Scripts/Frieren/UI/MagicBookUi.cs | 紧凑三槽魔力计数、悬停说明、魔法书入口及遮罩 |
| 魔法书界面 | Scripts/Frieren/UI/MagicBookScreen.cs | 原生 NCard 与网格，右键启用魔具、左键选目标、选满生效 |
| 营地 | Scripts/Frieren/RestSite/BuyMagicPropsOption.cs | 原生营地行动，八选二魔具；取消不耗行动 |
| 展示 | Scripts/Frieren/Presentation/、FrierenMod/scenes/ | 音乐切换、动作音效、死亡配乐、Godot 角色场景 |
| 文本和资源 | FrierenMod/localization/{zhs,eng}/、images/、audio/ | 双语卡面、悬停、建筑师对话及资源 |
| 验证 | Tests/Pure/、Integration/、Engine/ | 纯规则、实际程序集接口、真实引擎和 UI/结算流程 |

### 关键数据约定

`MagicDeckState` 当前存档格式为 **v3**，读取 v1/v2/v3；槽位编号沿用抽牌堆、手牌、弃牌堆顺序。因子和魔具共享实例 ID 序列。开局选择草稿及进度保存，避免读档重新刷选择。

随机玩法使用玩家奖励 RNG；音乐按种子独立选曲，不消耗卡牌/奖励 RNG。圣徽负责首次章节开局选择及首回合兜底。下一步新增开局赠品时，应扩展可迁移状态，不能每次调用开局入口都重新赠送。

## 优先待办：开局与首次教程（尚未实现）

用户已提出并同意恢复。调查结论来自上述冻结一代源码：

| 需求 | 一代行为和位置 | 二代落点 |
|---|---|---|
| 开局赠品 | `src/main/java/FrierenMod/events/ReadingGrimoireEvent.java`：三次初始因子选择后，赠送 1 个 UnbelievableTool 和 1 个随机 PROP | MagicDeckCmd 完成开局流程；MagicDeckState 记录只赠送一次 |
| 首次教学用魔具 | 同事件首次游玩分支赠送 Tutorial；`cards/magicItems/props/Tutorial.java` 标记 NEVER_DROP，选择一张其他魔法物品作为练习，不改变目标 | 独立教程魔具规则与预览，不放入普通掉落或营地八种魔具池 |
| 三页新手引导 | `utils/FTUEUtils.java`、`patches/TipTrackerPatch.java`、`localization/ZHS/UIs.json` 的 FTUE 条目：魔力计数、咏唱支付、因子和魔具 | 结合二代原生教程 UI；账号/档案首次标记、可重看、可关闭 |
| 清楚知道开局选了什么 | 一代 ReadingGrimoireEvent 的抽/手/弃选择标题 | 给每轮选择明确槽位，完成后显示三槽选择结果；魔法书中可回查 |

一代 Java 路径均相对于一代仓库；本地化文件在 `src/main/resources/FrierenModResources/` 下。

建议验收：
- 新开局完整选择后各赠一次；中途退出继续、首回合兜底、进入下一幕均不重复赠送。
- 旧 v1/v2/v3 档采用明确补发策略，存读档后既不重复赠送，也不丢失已消耗状态。
- 每轮清楚显示“抽牌堆/手牌/弃牌堆”，完成摘要与实际装载完全一致。
- 首次三页教程介绍当前真实功能；一代“黄色高亮”和“战斗掉魔具”不能原样照搬，二代目前可用槽是青色、战斗奖励仅因子。
- 教程魔具选择其他物品成功后仅消耗自身；取消、选择自身和无效目标不消耗。
- 教程可跳过/重看，关闭后恢复操作；避免常驻大说明挡住角色和战场；中英与多分辨率检查。

开始实施前仍需核对一代首次弹出时机与 NEVER_DROP 的随机池过滤。当前没有新教程代码、测试或素材产物，不要将以上建议视为已经实现。

## 必须保留的体验决定

- 三幕之后保留二代建筑师互动、原生反击与“胜利……？”结果页；暂不使用一代终幕背景、胜利语音或第四章流程。
- 建筑师对话在双语 `ancients.json`。原生末尾不允许通用对话回退；缺少芙莉莲专属对话会造成 WinRun 空引用。不要删除这组注册。
- 谜题卡只保留一代文字碎片、符号及费用，不在卡面、悬停或状态说明揭示效果/答案。
- 卡面不出现 max(0,x) 等代码公式；非谜题效果应给玩家可理解的数值。“童话的结尾”伤害预览要与实际牌组倍率和结算一致。
- 魔力 HUD 仅常驻三个计数与短标签；长说明放悬停。魔法书沿一代魔具操作逻辑，使用二代原生卡牌交互。
- 杀人魔法语音不应绑定所有攻击/施法。当前对应专用牌尚未迁入，保留资源但不从通用动作触发。
- 魔具操作/营地商店当前仅支持单人；不能仅移除禁用检查就宣称支持联机。

## 已知差异与后续验收

目前 28 种因子、八种魔具、73 个卡模型、19 个能力模型；模型数包含预览、临时及兼容模型，不等于正常奖励池牌数。自定义遗物只迁入圣徽。菲伦、一代事件/Boss、其余遗物/药水、部分卡牌及语音、成就和背景皮肤未移植。

`docs/content_inventory.csv` 可定位一代类，但应结合当前代码核对；`tools/audit_baseline.py` 是早期来源审计脚本，包含旧实现清单和固定工作区假设，**不要作为日常生成任务运行，否则会覆盖后来整理的清单**。

已有营地原生命令及部分遗物兼容测试，未覆盖所有二代事件/遗物组合。正式游戏完整长局、真实建筑师气泡/演出、时间线解锁、返回菜单完整切场、云同步、联机及主观音频感受仍需人工验收。自动测试的真实对象调用不等同于这些实机覆盖。

“进化的防御魔法”结算会按每次获得格挡应用敏捷；槽位预览是否完整反映战斗增益仍需继续核对，勿仅凭卡面基础值判断效果。

## 资产编辑与重新生成

普通构建直接使用包内已转换的素材。以下仅在需要从一代重建时使用：

```powershell
$env:FRIEREN_STS1_SOURCE = "D:/Sources/FrierenMod_STS1"
python "./tools/import_assets.py"
```

一代源码需保留 Git 元数据供来源记录读取，并使用上述冻结提交。当前转换脚本使用 Python 标准库。这些命令会重新写入相应资源及来源清单，应在自己的备份/版本控制副本中进行并检查差异。

`chant_content.py`、`prop_content.py` 同样接受该来源路径，但属于批量文本生成器，运行会重写相应文本/清单；目前已保存的 JSON 是本次交接的有效内容，不要求作者先运行生成器。谜题保密、童话数值、建筑师对话等均应在生成后复测。

137 条一代来源记录见 `assets.json`；两张生成图片的提示词、引用说明和哈希见 `generated_assets.json`。地图参考中的原生摄政王图标未随包分发，当前生成结果已包含。普通 .NET 生产模组不需要旧 Spine 运行时；角色 Idle 已烘焙为 Godot 网格动画。

## 继续开发的最短路径

先按根目录说明跑通构建，再试玩新局 → 因子选择 → 咏唱 → 营地逛店 → 魔具操作；随后优先补完本页的开局/教程待办。每次修改卡面或图片需重打 PCK，修改 DLL 后也应成套更新版本匹配的文件。测试只使用内存档案；不把真实玩家存档、游戏 DLL 或日志中的私人目录带入源码包。
