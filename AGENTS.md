# 开发协作约定

- 用户主要提供想法和功能方向；开发、调试、必要验证及日常 Git 操作由代理主动完成。
- 交流、开发说明及提交说明使用中文。提交采用 Conventional Commits，例如 `feat:添加新卡牌xxx`、`fix:修复咏唱费用计算`、`docs:更新开发说明`。
- 每次修改前检查 Git 状态，保留用户已有修改；按独立功能或修复提交，仅暂存本次相关文件。不得擅自丢弃修改、重写历史或强制推送。
- 用户已明确授权：每次完成修改并通过相应验证后，自动 commit 并 push 到当前分支对应的远端，无需重复询问。推送失败应排查并说明实际状态；不得将本地提交成功表述为远端已同步。此授权不包含发布版本或创意工坊上传。
- 遇到影响玩法方向、需求冲突或无法从源码确认的问题及时沟通；一般实现选择自行推进。

## 当前待办与版本管理

- 用户维护的根目录 `TODOs.docx` 是当前需求入口（2026-10-19）；执行顺序为先修 bug、再新增内容、最后做平衡调整。旧 `docs/HANDOFF.md` 和外层 `TODO.txt` 仅作补充，冲突时以用户最新要求为准，不能把旧方案当作已确认的新需求。
- 中文文本优先对照一代源码和中文本地化逐项核对，其他语言后做；无法确认且影响玩法或原意的问题向用户询问，不自行改写设计。
- 卡名以根目录 `卡名对照表.xlsx` 和可检索索引 `docs/inventory/card_names.jsonl` 为准。交流前用代码 ID 检索核对中文名，未核实时直接用代码 ID，不凭英文或效果编造中文卡名。`BabySleeping` 的中文是“婴儿般的睡眠”，英文是 `Sleeping Like a Baby`。
- 对照表中的一代原名和二代当前名分别保留；“需对齐”表示发现名称差异，不代表已经修复。四个二代选择预览没有一代同 ID，暂用二代现名并标记待确认。修改卡名后同步刷新索引和表格，不能将旧表当作当前运行文本。
- 作者正在核对卡名表，核对完成前不得重新生成或覆盖 `卡名对照表.xlsx`。作者确认后，表格只保留 ID、二代中文名、二代英文名、卡面效果（中文）四列；此次确认前先处理其他 bug。
- 新待办明确要求开局赠送两个神奇妙妙工具，取代旧交接中“一个工具加一个随机魔具”的方案；仍须保证存读档、兜底入口和跨幕不重复赠送。
- 每批实际修改模组代码、玩法、文本或资源时必须递增版本号。当前仍处于 0.x 开发期，常规修复使用补丁号递增（例如 0.5.2 → 0.5.3），较大功能批次再递增次版本；未经用户明确确认不得升至 1.0。
- 同批更新 `FrierenMod.csproj` 的 `Version`、`FrierenMod.json` 的 `version` 及 README 当前版本说明，保持构建暂存清单、DLL 和 PCK 配套。纯待办整理、协作约定等文档改动不冒充模组功能更新，无需递增功能版本。

## 仓库与参考

- 当前开发仓库是本目录 `FrierenMod_STS2`。外层工作区不是 Git 仓库；其他快照目录不作为默认修改对象。
- 先看 `README.md`、`docs/HANDOFF.md` 和当前源码。文档可能保留快照制作时的状态（例如“未提交”），实际状态以 Git 和验证结果为准。
- 一代参考源码在 `../reference/FrierenMod_STS1`，素材副本在 `../STS1_素材`。保持参考与原始素材不变。
- 任何一代行为移植前，阅读对应 Java 实现、注册/补丁入口、本地化与资源，以及当前二代实现；不能只凭卡面描述推测运行规则。

## 一代源码导航

相对一代 `src/main/java/FrierenMod/`：

- `ModManager.java`、`utils/RegisterHelper.java`：ModTheSpire 初始化、BaseMod 生命周期订阅、内容与存档注册。
- `Characters/`、`cards/`、`powers/`、`relics/`、`potions/`、`events/`、`monsters/`：角色与各类内容；注册池和配置开关需一并核对。
- `actions/ChantAction.java`、`gameHelpers/CombatHelper.java`、`cards/magicItems/`：普通/精准/终极咏唱、魔力判定、因子与魔具；通过一代 ActionQueue 结算。
- `patches/fields/MagicDeckField.java`、`ui/slot/`、`ui/screens/MagicDeckScreen.java`：玩家魔法书收藏、三槽及交互。
- `gameHelpers/SaveFileHelper.java`、`gameHelpers/DataObject.java`、`ModManager.java`：存档与恢复；不能照搬 Java 对象到二代。
- `patches/`、`cardMods/`、`rewards/`：原生行为扩展、卡牌修饰、奖励接入。
- `events/ReadingGrimoireEvent.java`、`utils/FTUEUtils.java`：开局因子、赠品与首次引导。
- `src/main/resources/FrierenModResources/`：本地化、图片、动画及音频。逐项清单见本仓库 `docs/inventory/` 与 `docs/content_inventory.csv`。

## 二代实现与验证

- 后续必须加入菲伦（Fern）作为独立可选角色；本次架构重构需支持芙莉莲、菲伦及共享内容，不能把公共卡牌基类、状态或资源绑定到芙莉莲卡池。
- 用户认可观者移植与 Huoreimu 作为架构参考。多角色补充参考 `https://github.com/lamali292/Downfall`：角色独立模块与公共层分离。具体架构取舍见 `docs/ARCHITECTURE_REFERENCES.md`。

- 当前采用 Downfall 式角色模块边界，保留单一工程。代码导航见 `docs/CODE_STRUCTURE.md`；新增卡牌和能力各自独立文件，普通卡牌按实际稀有度分类。
- `Core/Frieren/` 为芙莉莲纯规则；`Scripts/Frieren/` 为角色实现；`Scripts/Shared/` 仅放真实共用基础功能；`FrierenMod/` 为运行资源。菲伦实现时增加对应 Fern 模块，不提前创建空框架。
- 现有命名空间、模型 ID 与资源路径有兼容意义，不能随文件移动机械改名。公共基类不得带角色专属 Pool 属性。
- 二代采用 C#、Godot、BaseLib；沿用原生命令与异步流程，不复制一代全局状态或 ActionQueue。
- 本地二代反编译源码与四个模组参考已放在 `../reference/`；入口和核查结论见 `docs/LOCAL_REFERENCES.md`，精确来源见 `../reference/sources.json`。实现前优先查原版相近效果及当前 BaseLib 接口，参考目录不参与本模组编译和提交。
- 构建入口为 `tools/build.ps1`，构建安装入口为 `tools/build-and-install.ps1`。先读脚本参数和本地配置再调用。
- 根据变更执行相应检查；文本/资源修改需要重新打包 PCK。编译、自动测试通过不能表述为已经完成游戏实测。
- 不随意运行会覆盖清单或文本的审计/生成脚本，尤其 `tools/audit_baseline.py`；遵循 `docs/HANDOFF.md` 中的说明。
