# 代码结构与新增角色

项目参考 Downfall 的角色模块与公共层分离方式，保留单个 C# 工程、统一模组入口和资源包。

```text
Scripts/
  Entry.cs                      模组初始化
  Shared/Cards/ModCard.cs        不绑定角色卡池的通用卡图基类
  Frieren/
    Character/Frieren.cs        芙莉莲定义与起始牌组
    CardPools/                  卡池、遗物池、药水池，各自独立文件
    Cards/
      Abstract/                 芙莉莲、普通咏唱、传奇魔法基类
      Basic/ Common/ Uncommon/ Rare/ Token/
                                每张卡独立文件，按实际稀有度分类
      Factors/                  因子预览模型与基类
      Props/                    魔具预览模型与基类
      Choices/                  咏唱选择模型与基类
    Interfaces/                 咏唱、传奇魔法、速度牌标记及咏唱回调
    Powers/                     每个能力独立文件及其基类
    Mechanics/                  咏唱、魔力、魔具、存档与传奇魔法生成
    Relics/ Rewards/ RestSite/   遗物、奖励和营地
    UI/ Presentation/           魔法书界面及角色音画表现
Core/Frieren/                   无引擎依赖的规则与状态
FrierenMod/                     现有运行资源、本地化与场景
Tests/Pure/ Integration/ Engine/ 规则、实际程序集和引擎测试
```

例如：快速咏唱在 `Scripts/Frieren/Cards/Common/RapidChant.cs`，童话的结尾及其伤害预览在 `Scripts/Frieren/Cards/Rare/FairyTaleEnd.cs`。起始牌组中也有普通牌，文件按卡牌实际稀有度归类。

## 身份与资源兼容

现有类型保留 `FrierenMod.Scripts.Cards` 等命名空间及类名，确保类型查找、模型身份与旧调用兼容。文件夹按角色组织，旧命名空间不是跨角色共用的声明。尤其因子与魔具工厂按类型完整名称查找，不能只改文件命名空间而不处理工厂与存档验证。

原有卡图、本地化键、场景路径和存档结构保持原值。无需为了目录美观移动资源，避免让资源清单和存档迁移成为纯整理的副作用。`model-identities.json` 保存重构前真实 ModelDb 的身份清单；新增模型时应有意识地更新清单，不能用重新生成掩盖旧模型身份改变。

## 菲伦接入约定

菲伦是明确的后续独立可选角色，尚未实现。实现时创建 `Scripts/Fern/`，在其中增加角色、内容池、卡牌、法杖等机制与展示；纯规则放 `Core/Fern/`。当前不创建空工程、假角色或未使用的注册框架。

- 菲伦卡牌基类继承 `Shared/Cards/ModCard`，自行指定卡图目录和菲伦卡池；不继承带芙莉莲卡池与提示的 `FrierenCard`。
- 新角色类型使用独立命名空间（如 `FrierenMod.Scripts.Fern.Cards`），稳定命名并验证模型 ID 不冲突；命名空间本身不保证模型 ID 唯一。
- 角色存档和战斗状态绑定所属玩家；菲伦不自动获得芙莉莲的魔法书、营地选项、音乐或初始因子。
- 共享卡牌与共用机制在实际移植时提取到 Shared；注册归属依据一代行为和二代接口确认。目录名不决定奖励池，避免重复注册增加掉率。
- 仅为确实共用的功能提取公共实现；不提前复制芙莉莲整套代码，不引入服务容器或多程序集发布链。

## 开发检查

UID 策略参考 Downfall：Git 忽略 `*.cs.uid`，C# 工程视图排除它们；Godot 仍可在本地生成。VS Code 本机工作区设置隐藏 `**/*.uid`。当前生产 C# 类型均通过程序集加载，场景没有 C# 脚本引用。GDScript 的 `.gd.uid` 和资源 `.import` 继续保留；若未来将 C# 节点直接挂入场景，应为其 UID 添加显式跟踪例外。Godot 官方一般建议提交 UID，本项目的例外仅针对这些非场景 C# 文件。

新增普通卡牌：选择角色与稀有度目录 → 新建同名文件 → 继承该角色卡牌基类 → 配套资源与双语文本 → 核对卡池和模型身份 → 执行相关验证。

`tools/import_assets.py` 递归扫描芙莉莲卡牌与能力，`tools/chant_content.py` 读取 `Core/Frieren/SpellCatalog.cs`。这些生成工具仍会重写内容，常规开发不需先运行。

### 本次重构验证（2026-10-18）

- Release 构建、49 项纯规则测试、12 项真实程序集检查通过；新增两项覆盖模型身份和公共卡牌基类的角色隔离。
- 73 个卡模型与 19 个能力模型的源码发现集合与重构前一致；拆分声明逐项比对保持原逻辑，通用卡图路径提取到 ModCard。
- 双语文本、137 条素材来源哈希及生成素材哈希检查通过。PCK 打包成功，独立挂载检查覆盖 121 张纹理、16 段音频、6 个场景、16 张文本表。
- 引擎测试由工作区 MegaDot 4.5.1-m.12 和独立 .NET 9.0.10 运行，`ENGINE_TESTS_OK: 156`，退出码 0。此验证不等于正式游戏长局试玩。
- 本机配置中的普通 Godot 打包成功，但编辑器默认配置目录存在权限报错；配置中的标准 Mono 引擎启动报告 Assemblies not found，改用工作区 MegaDot 验证。MegaDot 默认选择系统 .NET 10 时 Harmony 不兼容，通过当前进程 DOTNET_ROOT/DOTNET_ROOT_X64 指向已有 .NET 9 安装并允许跨主版本前滚后通过；未修改系统或引擎安装。
- NuGet 漏洞信息服务暂不可达，构建出现 NU1900；程序集测试的 Godot 源生成器警告与引擎测试两处可空警告仍存在。
