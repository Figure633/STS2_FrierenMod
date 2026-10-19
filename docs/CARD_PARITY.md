# 一代芙莉莲卡牌核对与补齐

核对日期：2026-10-19。一代规则基线为 `8604c9cd072dba6ae1bf8f47986fdc291b26c3cc`，逐项检查 `cards/white`、`cards/whitePurple`，以及对应 Action、Power、卡牌修饰器和临时牌。可机读清单见 [frieren_card_parity.json](inventory/frieren_card_parity.json)，程序集测试会检查全部有效卡牌仍有非 Token 模型。

## 核对结果

- 两个源目录共81个类：77张有效牌、4张停用牌。
- 有效牌中30张已有实现，本次补入47张，包含7张芙莉莲/菲伦共享牌在芙莉莲侧的实现。
- 另补9个临时牌模型和1个构筑选项模型；总卡模型由73增至130，包含因子、魔具、选择预览和兼容模型，不能当作奖励池张数。
- 补入14个能力模型，总能力模型33个。新增牌和能力各自独立文件，沿用角色模块目录。
- `FrushDown`、`PaperPlaneSpell` 在一代标记 `AutoAdd.Ignore`；`MageStyleChoke`、`RockGolemSpell` 的源码已整体注释。其余三个停用类不注册；已有纸飞机仍仅保留 Token 兼容模型。

## 本次补入的有效牌

| 一代类 | 二代文件 |
|---|---|
| `AccessoriesSpell` | [AccessoriesSpell.cs](../Scripts/Frieren/Cards/Rare/AccessoriesSpell.cs) |
| `AirSlash` | [AirSlash.cs](../Scripts/Frieren/Cards/Uncommon/AirSlash.cs) |
| `ApexMagic` | [ApexMagic.cs](../Scripts/Frieren/Cards/Rare/ApexMagic.cs) |
| `Apprehend` | [Apprehend.cs](../Scripts/Frieren/Cards/Uncommon/Apprehend.cs) |
| `BackFlow` | [BackFlow.cs](../Scripts/Frieren/Cards/Common/BackFlow.cs) |
| `BreaksBarriersSpell` | [BreaksBarriersSpell.cs](../Scripts/Frieren/Cards/Uncommon/BreaksBarriersSpell.cs) |
| `ButtSlam` | [ButtSlam.cs](../Scripts/Frieren/Cards/Uncommon/ButtSlam.cs) |
| `ContinuousShooting` | [ContinuousShooting.cs](../Scripts/Frieren/Cards/Uncommon/ContinuousShooting.cs) |
| `ElementsBombing` | [ElementsBombing.cs](../Scripts/Frieren/Cards/Rare/ElementsBombing.cs) |
| `FireSpell` | [FireSpell.cs](../Scripts/Frieren/Cards/Uncommon/FireSpell.cs) |
| `FiveMinutesMore` | [FiveMinutesMore.cs](../Scripts/Frieren/Cards/Uncommon/FiveMinutesMore.cs) |
| `Free` | [Free.cs](../Scripts/Frieren/Cards/Uncommon/Free.cs) |
| `FrierenSuitcase` | [FrierenSuitcase.cs](../Scripts/Frieren/Cards/Uncommon/FrierenSuitcase.cs) |
| `GrandCross` | [GrandCross.cs](../Scripts/Frieren/Cards/Common/GrandCross.cs) |
| `HighDensityMana` | [HighDensityMana.cs](../Scripts/Frieren/Cards/Uncommon/HighDensityMana.cs) |
| `Kiss` | [Kiss.cs](../Scripts/Frieren/Cards/Uncommon/Kiss.cs) |
| `LockTarget` | [LockTarget.cs](../Scripts/Frieren/Cards/Common/LockTarget.cs) |
| `MageStyleFinisher` | [MageStyleFinisher.cs](../Scripts/Frieren/Cards/Uncommon/MageStyleFinisher.cs) |
| `ManaCollapse` | [ManaCollapse.cs](../Scripts/Frieren/Cards/Uncommon/ManaCollapse.cs) |
| `ManaParcel` | [ManaParcel.cs](../Scripts/Frieren/Cards/Uncommon/ManaParcel.cs) |
| `MultipleOffensiveMagic` | [MultipleOffensiveMagic.cs](../Scripts/Frieren/Cards/Common/MultipleOffensiveMagic.cs) |
| `OpenTheWaygate` | [OpenTheWaygate.cs](../Scripts/Frieren/Cards/Rare/OpenTheWaygate.cs) |
| `OrdinaryOffensiveMagic` | [OrdinaryOffensiveMagic.cs](../Scripts/Frieren/Cards/Uncommon/OrdinaryOffensiveMagic.cs) |
| `Outpouring` | [Outpouring.cs](../Scripts/Frieren/Cards/Uncommon/Outpouring.cs) |
| `PajamasForm` | [PajamasForm.cs](../Scripts/Frieren/Cards/Uncommon/PajamasForm.cs) |
| `PancakeSpell` | [PancakeSpell.cs](../Scripts/Frieren/Cards/Uncommon/PancakeSpell.cs) |
| `Perpetual` | [Perpetual.cs](../Scripts/Frieren/Cards/Uncommon/Perpetual.cs) |
| `Recall` | [Recall.cs](../Scripts/Frieren/Cards/Rare/Recall.cs) |
| `Recycle` | [Recycle.cs](../Scripts/Frieren/Cards/Common/Recycle.cs) |
| `RedAppleSpell` | [RedAppleSpell.cs](../Scripts/Frieren/Cards/Uncommon/RedAppleSpell.cs) |
| `RestraintMagic` | [RestraintMagic.cs](../Scripts/Frieren/Cards/Uncommon/RestraintMagic.cs) |
| `RingletForm` | [RingletForm.cs](../Scripts/Frieren/Cards/Rare/RingletForm.cs) |
| `RitesPreparation` | [RitesPreparation.cs](../Scripts/Frieren/Cards/Uncommon/RitesPreparation.cs) |
| `SayThinkingSpell` | [SayThinkingSpell.cs](../Scripts/Frieren/Cards/Uncommon/SayThinkingSpell.cs) |
| `SeeThroughPhantom` | [SeeThroughPhantom.cs](../Scripts/Frieren/Cards/Common/SeeThroughPhantom.cs) |
| `SerieGrimoire` | [SerieGrimoire.cs](../Scripts/Frieren/Cards/Rare/SerieGrimoire.cs) |
| `SwampSpell` | [SwampSpell.cs](../Scripts/Frieren/Cards/Common/SwampSpell.cs) |
| `TimeConcept` | [TimeConcept.cs](../Scripts/Frieren/Cards/Rare/TimeConcept.cs) |
| `TongueTwisterSpell` | [TongueTwisterSpell.cs](../Scripts/Frieren/Cards/Common/TongueTwisterSpell.cs) |
| `Trick` | [Trick.cs](../Scripts/Frieren/Cards/Uncommon/Trick.cs) |
| `BabySleeping` | [BabySleeping.cs](../Scripts/Frieren/Cards/Common/BabySleeping.cs) |
| `ClearMind` | [ClearMind.cs](../Scripts/Frieren/Cards/Uncommon/ClearMind.cs) |
| `Disperse` | [Disperse.cs](../Scripts/Frieren/Cards/Common/Disperse.cs) |
| `FlyingMagic` | [FlyingMagic.cs](../Scripts/Frieren/Cards/Rare/FlyingMagic.cs) |
| `GrapeSpell` | [GrapeSpell.cs](../Scripts/Frieren/Cards/Uncommon/GrapeSpell.cs) |
| `ManaDetection` | [ManaDetection.cs](../Scripts/Frieren/Cards/Uncommon/ManaDetection.cs) |
| `ShavedIceSpell` | [ShavedIceSpell.cs](../Scripts/Frieren/Cards/Common/ShavedIceSpell.cs) |

## 配套机制与适配

- 普通魔力和高密度魔力通过同一资源身份支付咏唱；消耗高密度魔力返还能量。加速同调和雷电同调可共同生效，相关连锁只统计所属玩家。
- 连续射击的消耗关键词、魔力崩坏的回抽牌堆在原生出牌去向判定时确定，不能等伤害结算后才决定。
- 手提箱使用 BaseLib 的玩家手牌上限接口，至少13张，不修改全局上限。额外回合沿用二代原生机制；多人时按施法玩家的额外回合规则执行。
- 元素轰炸依一代源码消耗整手牌，按25/25/25/15/11（随机整数0至100）的权重生成冰、火、雷、暗、光，升级后生成升级元素。不是从五种元素中手动选两种。
- 赛丽艾的魔导书先选0至3费，再从三个对应费用的效果池各选一种。普通每层展示2种，升级展示3种；四个费用选项采用原生单选网格。自定义牌保存费用和效果组合，并正确改变攻击类型、目标及消耗属性。
- 时间概念在第二个后续回合开始时恢复施法者的牌堆位置、生命上限/当前生命、格挡、能量、能力、遗物及药水。卡牌保留对象身份，因此期间升级不会撤销，与一代持有卡牌引用的方式一致。未升级时加入原生悔恨。
- 时间回溯不回滚敌人、RNG、共享战斗历史、魔法书库存或永久牌组。尤其不会照搬一代将旧出牌历史再次追加的做法。第三方能力与遗物的内部可变字段依赖其原生克隆实现，尚未对所有第三方组合做兼容验收。
- 一代部分过长卡名缩短显示标题以适应二代卡框；效果说明保留。旧背景皮肤解锁、特殊演出和成就不随本次卡牌补齐迁入。
- 菲伦独占卡牌与角色仍属于后续角色开发，本次未注册空角色。

## 资源与验证入口

新增70张图片直接取自用户提供的 `STS1_素材/FrierenMod_STS1_source code`，保留原文件，逐项路径和SHA-256写入 `docs/assets.json`。没有将本地素材误标为冻结Git提交中的图片。

`Tests/Engine/MissingCardTests.cs` 检查资源流转、实际抽牌/消耗回调、同调与能力连锁、条件回牌堆、X费、自定义魔法选牌与存档、时间回溯。`CardUiAudit` 检查中英原生卡面排版。自动验证不等于人工完整长局及联机验收；结果见 [TEST_REPORT.md](TEST_REPORT.md)。
