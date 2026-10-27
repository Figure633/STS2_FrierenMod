# 正式版与测试版兼容

0.9.4 使用同一份 `FrierenMod.dll` 和资源包支持以下已核查版本，依赖 BaseLib 3.4.7。清单最低版本为0.107.1，不表示对所有未来版本自动兼容。

| 游戏版本 | sts2.dll SHA-256 | 已验证范围 |
|---|---|---|
| 正式版0.107.1 | `A1F9E653F1E28E4076558FEE1E60D218619CB7E057B887C6417F62C62C6D7A52` | 成员签名、虚方法覆盖、真实初始化、内存存档、核心战斗与差异专项；界面使用本机正式版PCK |
| 测试版0.111.0 | `0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9` | 同一DLL的成员签名、虚方法覆盖、真实初始化、内存存档、43项核心战斗及9项差异专项；完整测试版界面与长局仍待游戏实测 |

测试版运行时来自本机此前安装版本的测试依赖副本；本轮本机已切回正式版。引擎战斗验证加载正式版共用资源，不能把它表述为测试版完整资源和流程验收。游戏程序集只保存在忽略的本地工作区，不随模组分发。

0.9.5复查此前测试版启动日志：10月27日18:22的日志中，本模组0.9.3在旧牌堆补间补丁初始化失败，该接口已于0.9.4适配；18:25的日志中本模组已加载，随后另有HakureiReimu初始化失败及其界面脚本缺失。现有证据不能保证完整模组组合可启动。按作者安排，保留双版本适配，完整测试版启动与界面验收暂缓至工坊发布前处理。

## 参考后采用的做法

- [BaseLib：BetaMainCompatibility](https://github.com/Alchyr/BaseLib-StS2/blob/master/Utils/BetaMainCompatibility.cs)：复用 `FromCardCompatibility`、`VariableMethod`，按实际签名选择调用；不凭版本字符串猜测接口。
- [Watcher：CompatibilityCreatureCmd](https://github.com/lamali292/WatcherMod/blob/master/WatcherCode/Compatibility/CompatibilityCreatureCmd.cs) 与 [CardCmdCompatibility](https://github.com/lamali292/WatcherMod/blob/master/WatcherCode/Compatibility/CardCmdCompatibility.cs)：伤害参数和消耗返回类型不同，都可能导致二进制调用失败；必须集中适配。
- [Downfall：CardPlayLocationCompat](https://github.com/lamali292/Downfall/blob/5a885eb4e66c749c1e483f84bb205cda0d000483/DownfallCode/Compatibility/CardPlayLocationCompat.cs)：避免在通用模型中硬编码只存在于新版的 `CardLocation` 类型，按接口选择去向钩子。

本模组的通用命令适配位于 `Scripts/Shared/Compatibility/GameApi.cs`；芙莉莲专属虚方法适配位于 `Scripts/Frieren/Compatibility/ModelHookCompatibility.cs`。原版负责遍历监听者、预览、修饰者登记与最终移动牌堆。伤害适配只补充本模组特定能力，保留其他角色及模组的原生覆盖方法。

原生消耗动画分别使用正式版自动播放的 `NExhaustVfx` 和测试版显式播放的 `NCardExhaustVfx`，同时遵守各自的卡牌释放所有权。魔力加速分别适配旧版牌堆补间和新版公开补间工厂。随机种子和随机数构造采用本版游戏的实现；不承诺两个游戏版本之间随机序列完全相同。

## 复核方式

`Tests/Compatibility` 仅读取元数据，检查最终DLL对游戏的成员引用与基类虚方法覆盖，不执行模块初始化、不接触存档：

```powershell
dotnet run --project Tests/Compatibility/CompatibilityTests.csproj -- <FrierenMod.dll> <游戏data目录> <BaseLib目录>
```

`Tests/Engine` 以正式版公共API编译，使用反射处理测试夹具本身的版本差异。运行 `version-compat-only` 检查神格、打击加成、纸飞机、魔力崩坏、高密度魔力、节俭、格挡和种子；运行 `combat-api-only` 复核完整核心战斗链。

`tools/run-engine-check.ps1` 可通过 `-RuntimeDataDir` 和 `-ReleaseInfoPath` 指定另一版本的本地运行时，每次运行都显式复制所选依赖，避免上次测试版程序集污染下一次正式版检查。完整UI验证还需对应版本的游戏PCK。编译和自动检查通过不等同于长局验收。
