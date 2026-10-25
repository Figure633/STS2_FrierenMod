# 一代内容在二代的接入核对

核对日期：2026年10月25日。二代版本：0.8.0。

目前最大的未接入部分是菲伦完整角色。此外，历史战绩中的魔法牌组展示、部分语音、卡名上方的魔法读音和若干设置开关尚未接入。芙莉莲的主要内容模型已覆盖，部分演出和规则采用二代实现，不能等同于逐项复刻完成。

本次以作者提供的 `../FrierenMod_STS1_source code` 中562个Java文件为范围，核查注册入口、默认配置、本地化、调用位置及当前 `Scripts/`、`Core/`、资源目录。旧 `docs/inventory/content.json` 的 `not_started` 是历史快照，不作为当前结论；类名变化、合并实现和原版替代分别追踪。本次是源码与接入核对，没有启动一代逐场景对照，也未对所有卡牌组合进行效果回归。

## 尚未接入的有效内容

| 内容 | 一代证据 | 二代现状 |
|---|---|---|
| 菲伦独立角色 | `Characters/Fern.java`、`ModManager.receiveEditCharacters`，`Config.FERN_ENABLE=true` | 没有独立角色、起始牌组、角色选择、战斗动画及菲伦模块。发饰模型已有，但仍保留初始遗物身份，不能视为菲伦已接入。 |
| 菲伦专属卡池 | `cards/purple` 共64类，其中 `MysticEye` 被 `@AutoAdd.Ignore` 排除，63张为注册候选 | 仅贴贴、魔力亲和两张已经改为芙莉莲双色牌；其余61张未接入，逐项见下表。 |
| 菲伦配套系统 | `ui/panels/FernPanel.java`、`cardMods/FusionMod.java`、`powers/FusionPower/`、`actions/MakeSpecializedOffensiveMagicAction.java` | 奇袭、融合、菲伦面板及由菲伦机制生成特化普攻魔法的整条流程未接入。八种具体融合能力未接入；不把抽象基类算作缺失玩法。现有专注能力用于芙莉莲咏唱，并不等于菲伦系统。 |
| 菲伦衍生牌 | `cards/tempCards/Cheering.java`、`GetWell.java`、`Pouting.java`、`SpecializedOffensiveMagic.java` | 四张尚无对应模型，随菲伦相关牌与机制一起补齐。 |
| 历史战绩展示魔法牌组 | `patches/MagicDeckHistoryPatch.java` 将 `magic_deck`、`loaded_factors` 写入并绘制在跑局历史中 | 当前有跑局存档中的魔法书数据；尚无历史战绩页面的魔法书列表和三个装载槽展示。跑局存档可恢复与历史页面可查看是两个功能。 |
| 芙莉莲三张胜利图的播放入口 | `Characters/Frieren.getCutscenePanels()` 顺序使用 VICTORY_1、VICTORY_2、VICTORY_3 | 二代 `images/character/victory1.png` 至 `victory3.png` 已打包，但当前角色和结算代码没有播放调用；尚未接上二代适用的结算场景。 |
| 三处芙莉莲语音 | `cards/white/Kiss.java` 调用 `kiss.mp3`；`cards/whitePurple/BabySleeping.java` 调用 `BabySleeping.mp3`；`relics/MirroredLotusRing.java` 调用 `Himmel.mp3` | 对应卡牌、遗物效果已实现，三个音频未打包，对应效果中也未播放。 |
| 菲伦语音 | `AudioPatch.java` 注册 `I_am_fern.mp3`、`FumeshroomForm.mp3`、`SocialDancing.mp3`、`SoSmall.mp3` | 四段未接入，依赖菲伦角色或卡牌。 |
| 卡名上方的魔法读音 | `utils/TopTextHelper.java`、`patches/CardStylePatch.RenderTopTextPatches` | 七个读音没有对应展示：Zoltraak、Jutragerme、Volzanbell、Jubelade、Jilwer、Nephtear、Baruterie。对应普通攻击、雷、业火、金属花瓣、高速移动、冰箭、血魔法的规则模型已存在。 |
| 部分原版模组设置 | `utils/Config.java`、`ui/panels/ConfigPanel.java` | 当前设置仅提供一代音乐、成就开关及收藏入口；特殊音效／特效、槽位类型显示、谜题因子掉落、复制体遭遇开关尚未提供。谜题因子及复制体本身均已接入。 |
| 封印牌收向人物的专门演出 | `effects/SealEffect.java` 在封印时将牌缩小并移向人物 | `SealPower` 与封印牌结算已有；没有对应的专门收牌演出。 |

上述文件路径在“一代证据”列均相对于一代的 `src/main/java/FrierenMod/`。菲伦清单的中文名来自一代本地化，尚不是二代卡名定稿。

## 已适配但与一代不同的部分

| 项目 | 当前处理 | 是否算遗漏 |
|---|---|---|
| 一代终幕与结局音乐 | 主菜单曲 `STS_MenuTheme_NewMix_v1.ogg`、第四幕曲 `STS_Act4_BGM_v2.mp3`、心脏首领曲 `STS_Boss4_v6.mp3`、片尾曲 `STS_Credits_v5.ogg` 未接入。当前建筑师与胜利房间保留二代原生音乐。 | 主菜单曲确实尚未接入；其他三首需要二代对应场景的使用方案，不能直接套用一代第四幕。 |
| 复制体召唤 | 一代召唤邪教徒；二代由 `SpellCasterPower` 召唤原版潮湿邪教徒，沿用其动画、仪式和AI。 | 已有召唤功能，召唤对象为二代适配。 |
| 复制体防御魔法完全体SP | 依作者最新要求只给缓冲：普通1层、强化2层。 | 金属化、柔韧是明确移除的效果，不是待补内容。 |
| 谜题成功／视觉攻击演出 | `PuzzlePower` 使用原版 Hyperbeam 蓄力、光束、音效与震屏；建筑师攻击等使用二代VFX。 | 玩法已接入，具体演出不等同于一代LibGDX效果。 |
| 魔具标题横幅 | `MagicPropPresentation` 借用二代紫色标题材质，而非直接使用一代 `card_banner_prop_small/large.png`。 | 已有外观适配，尚非原素材逐项复刻。 |
| 魔法书获取、使用和消耗动画 | `UI/MagicDeckAnimations.cs` 已提供获取、移动、使用与消耗反馈。 | 不因一代 `FastMagicItemObtainEffect` 等Java类名未出现就判定功能未移植；画面细节仍需并排核对。 |
| 开局与教程 | 已接入开局事件、三槽初始选择、两个神奇妙妙工具、独立教程与重看入口。 | 一代的 `Tutorial` 魔具模型被教程界面替代，两个开局工具是作者指定方案。 |
| 自定义传说魔法及选项 | `LegendaryCraftCmd`、`Core/Frieren/LegendaryCraft`、`LegendaryCraftChoice` 合并一代多层选项类。 | 选项类不再一类一文件，不算缺牌。 |
| 时间回溯等 | `TimeConcept` 等使用二代状态和原生命令；边界说明见 `CARD_PARITY.md`。 | 不能以模型存在推断所有内部行为逐方法相同，也不应当作完全未接入。 |
| 多人魔具及营地购买 | 当前主动限制单人，尚未完成对应网络同步。 | 属于二代新增兼容工作；一代没有多人模式，不列为一代内容缺失。 |

## 已存在的主要内容

| 范围 | 本次核对结果 |
|---|---|
| 芙莉莲及原七张双色牌 | `frieren_card_parity.json` 中77张有效牌的对应文件全部存在，其中包含原七张共享牌；另加贴贴、魔力亲和两张，共79个这一范围的牌模型。77不包含因子、魔具、全部衍生牌，也不表示全部实机验收。 |
| 因子 | 一代28个因子均有对应目录和规则；`MetallicizFlower` 对应二代 `MetalPetalSpell`，旧存档ID有迁移。 |
| 魔具 | 八种常规魔具已有规则、获取、使用和营地入口；教程改用独立界面。 |
| 随机事件 | FoodEvent、KraftGift、AnimalWell、MimicFight 四个均有注册与角色限制；开局 ReadingGrimoireEvent 另有接入。 |
| 怪物 | Mimic、Spiegel_Frieren 均有二代怪物与遭遇实现，后者类名为 SpiegelFrieren。 |
| 药水 | BottledMana、DissolveClothPotion、EmperorWine 三种均存在。 |
| 遗物 | 一代未明确废弃的相关遗物均能找到二代模型；HairAccessory 留作初始遗物，不进入芙莉莲普通池。原Boss遗物的稀有度按作者决定适配。 |
| 卡槽背景与成就 | 104张背景资源、收藏入口、悬停提示、选择保存、音效弹窗和对应解锁调用已接入。背景数是资源数，不等于104个互相独立的事件触发器；含默认、成组通关随机奖励及收藏奖励。 |
| 战斗反馈 | 普通攻击魔法语音、因子/魔具独立卡框、魔法牌组数量、同调反馈、神格效果和芙莉莲动画均已有实现。不能依据同名Java效果类缺失就判为没有。 |

## 不计入当前遗漏的内容

- 修塔尔克：一代 `STARK_ENABLE=false`，仅有角色原型、两张基础牌和占位遗物，不是完整发布内容。
- `HaitaaWand`：作者已决定废弃，只保留二代旧档ID。
- `FrushDown`、`PaperPlaneSpell`、`MysticEye`：一代有忽略注册标记；`MageStyleChoke`、卡牌版 `RockGolemSpell` 和两张 `testCards/Exchange*` 为注释／停用代码。敌人法术中的同名 `RockGolemSpell` 是另一项有效功能，在二代由施法列表给予人工制品实现。
- `OLDHairAccessory`：一代旧忽略版本，二代已有 `OldHairAccessory` 适配，不是漏掉。
- `win.mp3`：二代已打包但未播放；一代也只找到音频注册，没有实际播放调用，不能据此认定漏掉一段一代有效演出。
- 一代几份备用音乐文件没有在 `AudioPatch` 的有效选曲中使用，不能按文件数量差全部当作缺失BGM。
- 移除时间吞噬者、天鹅绒项圈等一代原版内容的兼容开关，需要二代存在对应对象才有意义，不直接照搬。
- 时间线、先古卡、新BGM曲名提示、永久捕鸟随从及成就解锁异画库属于二代需求／扩展；不作为一代源码中已实现内容计算。

## 菲伦尚未接入的61张牌

一代 `cards/purple` 的63个有效候选中，移除已经移植的 `Snuggle`、`ManaAffinity` 后，余下如下；`MysticEye` 不在本表。

| 一代ID后缀 | 一代中文名 |
|---|---|
| `AggrieveStrike` | 委屈打击 |
| `Backstab` | 背刺 |
| `BoldReversal` | 回马枪 |
| `Catwalk` | 猫步 |
| `ChargeUp` | 普通蓄力 |
| `CleanClothSpell` | 彻底清除衣服污渍的魔法 |
| `CloseRaid` | 近身奇袭 |
| `ColdBlood` | 冷酷无情 |
| `Confidence` | 胸有成竹 |
| `ContinuousOffensiveMagic` | 连环普攻魔法 |
| `CounterAttack` | 防御反击 |
| `CriticalHit` | 会心一击 |
| `Defend_Fern` | 防御 |
| `DinnerTime` | 晚餐时间 |
| `EmergencyDefense` | 应急防御 |
| `FernFixFrieren` | 富妇抚芙服 |
| `FernRushdown` | 菲伦下山 |
| `Fight` | 加把劲骑士 |
| `FlyOnTheWall` | 暗中观察 |
| `FumeshroomForm` | 大喷菇形态 |
| `FusionRaid` | 奇袭的三击 |
| `FusionSlash` | 融合的一击 |
| `Gamble` | 豪赌 |
| `GetSick` | 发烧了！ |
| `GirlishHeart` | 少女心 |
| `Glaring` | 眩光 |
| `HuddleForWarmth` | 抱团取暖 |
| `InjuryTime` | 伤停补时 |
| `LieInWait` | 伺机待发 |
| `LittlePunch` | 小拳拳 |
| `MasterAndApprentice` | 师与徒 |
| `MasterHelpApprentice` | 师徒协力 |
| `Meditation` | 冥想 |
| `MemoryOfChildhood` | 童年的回忆 |
| `Metamorphose` | 化蝶 |
| `MoodyGirl` | 阴晴不定大姐姐 |
| `NoMercy` | 毫不留情 |
| `OneInHundred` | 百里挑一 |
| `PureMind` | 明镜止水之心 |
| `RaidPreparation` | 奇袭准备 |
| `RaidRace` | 竞速赛 |
| `RecollectionOfBenefactor` | 心系故人 |
| `Reviewing` | 复盘 |
| `Rewind` | 倒带 |
| `SaturationOffensiveMagic` | 饱和攻击魔法 |
| `SeeThroughClothSpell` | 把衣服变透明的魔法 |
| `SeriousSeriesSpell` | 认真系列，认真一击 |
| `Sit` | 神坐 |
| `SkilledMage` | 干练的魔法使 |
| `SkimReading` | 快速检阅 |
| `SleepingBeauty` | 睡美人 |
| `SocialDancing` | 交际舞 |
| `SorryICant` | 对不起，做不到 |
| `SoSmall` | 好小 |
| `SpellToMapADungeon` | 绘制地图的魔法 |
| `Strike_Fern` | 打击 |
| `StudyTime` | 请莲大学习 |
| `Surveillance` | 观察敌情 |
| `Swoop` | 以头抢地 |
| `TrainingByEating` | 加练一组甜点 |
| `WithNoStop` | 疾速飞驰 |
