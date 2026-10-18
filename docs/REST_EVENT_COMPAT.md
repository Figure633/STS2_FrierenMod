# 营地、遗物与事件兼容核查

2026-10-17，0.4.0；基线 StS2 v0.107.1、BaseLib v3.4.7。一代源码提交 `8604c9cd072dba6ae1bf8f47986fdc291b26c3cc`。

## 当前范围

| 项目 | 实现与验证边界 |
|---|---|
| 营地美术与音乐 | 已迁入静态肩背图及音乐，使用 BaseLib 转换成二代 NRestSiteCharacter；原生场景加载通过 |
| 二代休息、锻造 | 原生选项生成、实际回血、快速咏唱升级及取消锻造通过命令级测试；未做完整界面实机验收 |
| 二代营地遗物 | 皇家枕头追加 15 点治疗；铲子添加/移除挖掘选项通过测试。尚未覆盖挖掘领取及所有营地遗物组合 |
| 二代营地治疗队友 | 0.3.0 的 Hitbox 错误忽略鼠标；0.3.1 恢复接收输入并检查原生悬停信号连接。未完成双人实机验收 |
| 二代原生事件 | 事件共用的 MimicRestSiteHeal 命令及皇家枕头加成通过测试；不能据此推断所有事件兼容 |
| 一代专属营地选项 | 已移植「逛魔具店」：原生网格八选二，取消返回，成功占用营地行动；原图按钮与中英文本已接入。通过真实营地同步器验证 |
| 因子与魔具交互 | 八种可获得魔具全部实现。魔法书提供因子/魔具页签和使用按钮，目标选择显示因子编号与槽位；使用成功才消耗，非法选择不修改库存 |
| 一代遗物 | 仅圣徽已移植；其余专属遗物仍缺失。美术资源入包不代表对应遗物已注册 |
| 一代事件 | FoodEvent、KraftGift、AnimalWell、MimicFight 未移植。一代 ReadingGrimoireEvent 的开局因子选择功能已有二代三次三选一替代，但原事件页面与演出未移植 |
| 数据保留 | v3 玩家字段保存因子、魔具、槽位与递增实例编号，兼容 v1/v2。随原生楼层检查点保存，房间中途退出沿用原生回退；不单独落盘收益以免重复领营地物品 |

## 实现与测试依据

- `FrierenMod/scenes/frieren_rest.tscn`：Hitbox 使用 `mouse_filter = 0`；装饰层仍忽略输入。
- `Tests/Engine/PresentationTests.cs`：场景转换、命中框尺寸、鼠标过滤及原生悬停信号连接。
- `Tests/Engine/RestSiteTests.cs`：独立 TestMode 内存局，走二代 RestSiteOption、RelicCmd、CardSelectCmd、PlayerCmd 实现。
- `Scripts/Relics/HolyEmblem.cs`：当前唯一自定义遗物。
- `Scripts/RestSite/BuyMagicPropsOption.cs`：角色专属按钮，与原生营地同步器消费行动。
- `Core/MagicPropRules.cs`、`Scripts/Mechanics/MagicPropCmd.cs`：八种魔具规则与原生选牌/回血/能量命令。
- `Tests/Pure/MagicPropTests.cs`、`Tests/Engine/MagicShopTests.cs`：13 项规则、19 项引擎检查。
- 一代 `patches/BuyPropOptionPatch.java`、`effects/CampfireBuyPropEffect.java`、`utils/RegisterHelper.java`、`patches/ReadingGrimoireEventPatch.java`：原营地与事件入口。

休息、锻造、营地行动消费、选牌、回血与能量继续使用二代原生命令与钩子。纯数据层集中处理魔具目标约束和库存变化，先验证再在副本上结算，避免取消后误消耗。已有 172 项自动检查不代表全遗物、全事件或完整跑局通过验收。

## 魔具映射

| 魔具 | 交互 |
|---|---|
| 魔法水晶球 | 两张已装载因子交换槽位 |
| 神奇妙妙工具 | 一张库存因子替换一张已装载因子，旧因子留在库存 |
| 盲盒 | 随机获得两张非谜题因子 |
| 神秘魔术箱 | 三张库存因子随机装入三个槽位，旧因子留在库存 |
| 某科学的炼成阵 | 两张因子转换为同稀有度随机因子，保留槽位 |
| 赞因的笔记 | 移除两张库存因子/其他魔具，治疗 10 |
| 暗黑龙的角 | 战斗中移除两张库存因子/其他魔具，获得 2 能量 |
| 妙妙工具箱 | 移除两张库存因子/其他魔具，获得两个神奇妙妙工具 |

0.3.x 免费换槽下拉框已移除，恢复通过魔具调整装载。魔具交互目前限单人，战斗开场、结束回合、敌方阶段和行动结算中禁用。原作 Tutorial 的 NEVER_DROP、皮肤解锁与成就不属于本次营地可获得池；随机结果不要求与 Java 同种子逐次相同。普通战斗奖励暂仍只给因子，未扩成一代因子/魔具混合掉落池。

后续仍需迁入专属遗物和事件；二代原生事件的删牌、变牌、升级、生命变化和遗物交换需要另行覆盖。
