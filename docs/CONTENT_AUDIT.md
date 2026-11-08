# 卡池、卡图与卡背核对

2026年11月8日，玩法版本0.12.5。依据实际初始化后的卡牌模型、当前卡图、原生卡面截图、一代完整源码及卡背成品目录核对。升级不另计。本次清理文档和临时依赖链接，没有修改玩法、卡牌数值或游戏内图片。

## 常规卡池

按当前随机获取规则筛选，排除因子、魔具、基础／先古／事件、仅生成牌和弃用牌。共有79张规则候选，其中包含状态牌“高密度魔力”。原生奖励、商店等入口还会按各自要求筛选类型，79不表示每个入口都能抽出这79张，也不代表抽取概率。

| 稀有度 | 攻击 | 技能 | 能力 | 状态 | 合计 |
|---|---:|---:|---:|---:|---:|
|普通（白）|8|11|1|0|20|
|罕见（蓝）|11|17|7|1|36|
|稀有（金）|5|10|8|0|23|
|合计|24|38|16|1|79|

攻击24张，占规则候选30.4%；若只计攻击／技能／能力，分母为78张，攻击占30.8%。普通攻击8张、蓝色攻击11张、金色攻击5张。输出因子不属于攻击卡，不混入这些数量。

| 角色 | 普通 | 罕见 | 稀有 | 总数 | 攻击 | 技能 | 能力 | 状态 | 金色攻击 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
|铁甲战士|20|36|26|82|34|29|19|0|7|
|静默猎手|20|36|26|82|24|40|18|0|5|
|储君|20|36|26|82|29|35|18|0|9|
|亡灵契约师|20|36|26|82|32|33|17|0|8|
|故障机器人|20|36|26|82|28|35|19|0|8|
|芙莉莲|20|36|23|79|24|38|16|1|5|

攻击总量与静默猎手相同，少于其他四名原版角色。普通、罕见候选总数分别与原版的20、36张一致，差额主要在稀有卡：23对26。仅凭数量不能判定缺少攻击或牌池强弱；这套角色也通过因子与能力输出。

金色攻击5张：视觉攻击魔法、童话的结尾、地狱业火魔法、老实赚钱，很枯燥吧？、星星之火。

### 常规候选完整名单

| 稀有度 | 类型 | 卡名 | 模型 |
|---|---|---|---|
|普通（白）|攻击|附魔打击|EnchantedStrike|
|普通（白）|攻击|一眼盯砧，鉴定为芙了|Envy|
|普通（白）|攻击|大十字|GrandCross|
|普通（白）|攻击|定叫他有来无回|LockTarget|
|普通（白）|攻击|多重攻击魔法|MultipleOffensiveMagic|
|普通（白）|攻击|清除顽固油渍的魔法|OilSpell|
|普通（白）|攻击|准备架势|ReadyStance|
|普通（白）|攻击|识破幻影|SeeThroughPhantom|
|普通（白）|能力|仪式的准备|RitesPreparation|
|普通（白）|技能|婴儿般的睡眠|BabySleeping|
|普通（白）|技能|对峙|Confrontation|
|普通（白）|技能|嗷嗷哭|Crying|
|普通（白）|技能|分散|Disperse|
|普通（白）|技能|开出花田的魔法|FlowerFieldSpell|
|普通（白）|技能|魔力壁垒|ManaBarricade|
|普通（白）|技能|高速咏唱|RapidChant|
|普通（白）|技能|变出刨冰的魔法|ShavedIceSpell|
|普通（白）|技能|拔出无底沼泽的魔法|SwampSpell|
|普通（白）|技能|说绕口令不会大舌头的魔法|TongueTwisterSpell|
|普通（白）|技能|真是被看扁了啊|Underestimated|
|罕见（蓝）|攻击|空气斩|AirSlash|
|罕见（蓝）|攻击|无情铁手|Apprehend|
|罕见（蓝）|攻击|全身撞姬|ButtSlam|
|罕见（蓝）|攻击|芙派气功|Famehameha|
|罕见（蓝）|攻击|小小火花|LittleFire|
|罕见（蓝）|攻击|魔法使流终结技|MageStyleFinisher|
|罕见（蓝）|攻击|离别的馈赠|ManaCollapse|
|罕见（蓝）|攻击|魔力探知|ManaDetection|
|罕见（蓝）|攻击|消灭霉菌的魔法|MoldSpell|
|罕见（蓝）|攻击|普通攻击魔法|OrdinaryOffensiveMagic|
|罕见（蓝）|攻击|持续散射|SustainedBarrage|
|罕见（蓝）|能力|芙莉莲的旅行箱|FrierenSuitcase|
|罕见（蓝）|能力|全速前进|FullAhead|
|罕见（蓝）|能力|魔力亲和|ManaAffinity|
|罕见（蓝）|能力|睡衣形态|PajamasForm|
|罕见（蓝）|能力|永续|Perpetual|
|罕见（蓝）|能力|再循环|Recycle|
|罕见（蓝）|能力|暗流涌动|Simmering|
|罕见（蓝）|技能|加速流转|ClearMind|
|罕见（蓝）|技能|连环咏唱|ContinualChant|
|罕见（蓝）|技能|终极咏唱|FinalChant|
|罕见（蓝）|技能|再睡五分钟|FiveMinutesMore|
|罕见（蓝）|技能|民间的魔导书|FolkGrimoire|
|罕见（蓝）|技能|让葡萄变酸的魔法|GrapeSpell|
|罕见（蓝）|技能|变出热茶的魔法|HotTeaSpell|
|罕见（蓝）|技能|飞吻(杀伤力极强!)|Kiss|
|罕见（蓝）|技能|诱敌深入|LureTheEnemyInDeep|
|罕见（蓝）|技能|魔力包裹|ManaParcel|
|罕见（蓝）|技能|倾泻|Outpouring|
|罕见（蓝）|技能|顺利翻薄饼的魔法|PancakeSpell|
|罕见（蓝）|技能|将红苹果变成青苹果的魔法|RedAppleSpell|
|罕见（蓝）|技能|拘束魔法|RestraintMagic|
|罕见（蓝）|技能|贴贴|Snuggle|
|罕见（蓝）|技能|说出心声的魔法|SpeakFromHeartSpell|
|罕见（蓝）|技能|拼命守护宝库的魔法|TreasureVaultSpell|
|罕见（蓝）|状态|高密度魔力|HighDensityMana|
|稀有（金）|攻击|视觉攻击魔法|ApexMagic|
|稀有（金）|攻击|童话的结尾|FairyTaleEnd|
|稀有（金）|攻击|地狱业火魔法|HellFireSummoning|
|稀有（金）|攻击|老实赚钱，很枯燥吧？|HonestLiving|
|稀有（金）|攻击|星星之火|KindlingSpark|
|稀有（金）|能力|寻找遗失饰品的魔法|AccessoriesSpell|
|稀有（金）|能力|领域展开|DomainExpansion|
|稀有（金）|能力|飞行魔法|FlyingMagic|
|稀有（金）|能力|变好运的魔法|GoodLuckSpell|
|稀有（金）|能力|打开时空之门|OpenTheWaygate|
|稀有（金）|能力|让纸飞机飞得很远的魔法|PaperPlaneSpell|
|稀有（金）|能力|卷发形态|RingletForm|
|稀有（金）|能力|赖床|SleepIn|
|稀有（金）|技能|元素轰炸|ElementsBombing|
|稀有（金）|技能|魔法&想象力|Imagination|
|稀有（金）|技能|破灭之雷魔法|LightningMagic|
|稀有（金）|技能|魔力膨胀|ManaExpansion|
|稀有（金）|技能|防御魔法完全体|PerfectDefensiveMagic|
|稀有（金）|技能|精准咏唱|PreciseChant|
|稀有（金）|技能|追忆|Recall|
|稀有（金）|技能|清洁铜像生锈的魔法|RustCleanMagic|
|稀有（金）|技能|赛莉耶的魔导书|SerieGrimoire|
|稀有（金）|技能|真面目|TrueColours|

### 全部注册模型的稀有度

包括弃用旧档模型、因子、魔具、衍生和隐藏选择卡，共163个模型；其中147个声明可在百科显示。这个口径与79张常规候选不同。

| 稀有度 | 全部模型数 | 常规候选数 |
|---|---:|---:|
|基础|4|0|
|普通（白）|31|20|
|罕见（蓝）|51|36|
|稀有（金）|38|23|
|先古|2|0|
|事件|2|0|
|衍生／选择|33|0|
|诅咒|2|0|

可获取因子28种：普通9、罕见11、稀有8，另保留1个弃用因子模型。因子数量不加到攻击／技能／能力的79张中。

不在常规候选中的模型按稀有度列出如下。包含只用于选择的模型，不能把这一组全当作可掉落牌。

**基础：** 防御（DefendFrieren）；防御魔法（DefensiveMagic）；流转（Flow）；打击（StrikeFrieren）。

**普通（白）：** 进化的防御魔法（AdvancedDefensiveMagic）；逆流（BackFlow）；伺机而动（BePrepared）；强身健体（BuildUpBody）；谜题: ！？？（Factor100）；嫌弃脸（GrossedOutLook）；他山之石：释放冰箭的魔法（IceArrows）；物理魔法使（MeleeMage）；他山之石：花瓣金属化魔法（MetalPetalSpell）；养精蓄锐（Sleeeep）；转职药剂师（TransferToAlchemist）。

**罕见（蓝）：** 结界魔法（BarrierMagic）；大大火灾（BigFire）；他山之石：黑金之翼魔法（BlackGoldWings）；蓝图芙莉莲（BluePrint）；解析结界的魔法（BreaksBarriersSpell）；谜题: ？！？（Factor010）；放出火焰的魔法（FireSpell）；他山之石：高速移动魔法（HighSpeedMovement）；他山之石：精神操作魔法（Hypnosis）；宴会魔法：花鸟风月（NatureBeauty）；薪火相传（PassOnTheTorch）；省吃俭用（PinchAndScrape）；魔力增幅（SpellBoost）；欺骗魔族的小把戏（Trick）；物理超度（ViolentExorcism）。

**稀有（金）：** 解除魔法（AuraDispel）；他山之石：驭血魔法（BloodSpell）；自定义魔法（CustomLegendarySpell）；谜题: ？？！（Factor001）；寿命论（Fatalism）；大魔法使的余裕（GreatMageComposure）；辛美尔（Himmel）；出水芙蓉（ImpeccableIntangible）；岁月静好（PeacefulDays）；后悔莫及（Repent）；运筹帷幄（Strategizing）；活着的传说（TheLivingLegend）；精灵的时间观念（TimeConcept）；遁入智瞳（Transcendence）；起床！（WakeUp）。

**先古：** 生生流转（EndlessFlow）；葬送的芙莉莲（FrierenTheSlayer）。

**事件：** 捕鸟魔法（BirdCapturingSpell）；应急食物（EmergencyFood）。

**衍生／选择：** 苍月草（BlueMoonWeed）；魔法水晶球（CrystalBall）；暗元素（Dark）；弃牌堆咏唱（DiscardChantChoice）；抽牌堆咏唱（DrawChantChoice）；精灵的时砂漏（ElvenHourglass）；火元素（Fire）；青苹果（GreenApple）；手牌咏唱（HandChantChoice）；冰元素（Ice）；懒懒散散（Laziness）；超小份魔法（LegendaryCraftChoice）；光元素（Light）；魔力（Mana）；魔力隐藏（ManaConcealment）；我要更多魔力！（ManaWish）；盲盒（MysteryBox）；神秘魔术箱（MysticBox）；不死鸟的羽笔（PhoenixQuill）；我要更多奖励！（RewardWish）；王室秘藏的魔导书（RoyalGrimoire）；封魔矿（SealingOre）；防御魔法·追加（SecondDefensiveMagic）；赞因的笔记（SeinNote）；暗黑龙的角（ShadowDragonHorn）；和芙莉莲贴贴（Snuggle1）；和修塔尔克贴贴（Snuggle2）；我要变得更强！（StrongerWish）；雷元素（Thunder）；某科学的炼成阵（TransmutationCircle）；神奇妙妙工具（UnbelievableTool）；妙妙工具箱（UnbelievableToolBox）；贵重的商品（ValuableCargo）。

**诅咒：** 长寿种的痛苦（LongevityPain）；颠倒（UpsideDown）。

## 卡图边缘

共核对155个当前模型使用的独立卡图路径，并把当前可见攻击／能力卡及顶部黑条候选共52个模型放进原生卡框截图检查。检查的是当前未升级模型，未替换任何卡图。

26张一代攻击卡图仍含旧的底部透明切角，但当前攻击卡框覆盖了这部分；52张原生截图中，没有发现这些旧透明区域形成明显黑色露底。不能把“透明切角”直接统计成“有黑边”。当前能力卡也未发现旧椭圆透明边缘造成的明显黑角。再循环现为能力牌、沿用旧技能图，本次原生卡面未见露黑。

### 原图仍有顶部黑色条带的候选

下表为逐像素检测的原图黑色顶行。当前横幅可遮住全部或大部分；这些是清理原图时优先复查的候选，不能等同于游戏里都存在明显黑边。

| 卡名 | 原图顶部黑条 | 当前用途 | 来源／说明 |
|---|---:|---|---|
|无情铁手|4px|Attack|一代攻击图沿用；4px细条。|
|解析结界的魔法|4px|Skill|已弃用并隐藏，不影响当前正常获取。|
|自定义魔法|2px|Skill|生成的自定义魔法展示图；旧技能图。|
|应急食物|7px|Skill|技能图，7px细条。|
|他山之石：高速移动魔法|19px|因子|因子图，19px，是这批较明显的原图残留。|
|他山之石：花瓣金属化魔法|2px|因子|因子图，2px细条。|
|高速咏唱|18px|Skill|技能图，18px，是这批较明显的原图残留。|

三张谜题图的黑底为符号图设计的一部分，本次未归为需要裁掉的动画黑条。

旧攻击切角仍保留的26张：

空气斩、视觉攻击魔法、无情铁手、全身撞姬、一眼盯砧，鉴定为芙了、童话的结尾、芙派气功、火元素、流转、大十字、地狱业火魔法、小小火花、定叫他有来无回、魔法使流终结技、离别的馈赠、魔力探知、消灭霉菌的魔法、多重攻击魔法、清除顽固油渍的魔法、普通攻击魔法、准备架势、识破幻影、打击、持续散射、雷元素、物理超度。

该清单含火／雷元素等衍生牌及已弃用的物理超度，不能直接作为正常可获取攻击数量。能力图中，寻找遗失饰品的魔法、飞行魔法、仪式的准备等此前已经过透明边缘处理；当前文件为不透明矩形。

截图证据位于 `artifacts/repository-audit-20261108/native-01.png` 至 `native-09.png`，原图接触表为同目录 `suspects-*.png`。像素结果为 `art-audit.json`；原生框下的透明区域检查为 `native-gap-analysis.json`。临时图片不替代作者对实际游戏窗口的目视验收。

## 卡背与成就

已注册104个卡背ID，游戏目录有104张对应PNG，没有缺文件或漏注册的旧卡背。104张都与一代对应文件逐字节相同。当前代码入口覆盖41个基础／特殊条件，收藏与通关奖励另由 SlotBackProgress 处理；静态入口存在不等于每项已完成游戏实测。

| 分组 | 数量 | 获取方式 |
|---|---:|---|
| 0001～0003 | 3 | 初始默认 |
| 0004～0010 | 7 | 初次咏唱、传说魔法、魔法书、战斗与死亡等条件 |
| 1001～1018、2001～2018、3001～3018 | 54 | 通关按进阶0／5／10组别抽取未解锁卡背 |
| 4001～4015、5001～5015、6001～6002、7001～7002 | 34 | 具体卡牌、构筑、战斗和谜题成就 |
| 9001～9003 | 3 | 收藏条件 |
| 9997～9999 | 3 | 打开收藏库补发的纪念卡背 |

### 尚未装进游戏的新图

19款构图，每款基础圆角／金色花纹两版，共38张最终成品，均未与现有ID分配，也没有写入游戏卡背目录。预览、最终版打包文件夹里的重复副本不重复计数；原作者素材继续保留。

[19款未接入卡背预览](../artifacts/repository-audit-20261108/pending-backs.png)，展示每款的金色花纹版；基础圆角版采用同一构图。

| 编号 | 构图 | 基础圆角 | 金色花纹 |
|---|---|---|---|
|01|芙莉莲·微笑|[未接入](../outputs/slot-backs-final-20261029/基础圆角/01-芙莉莲·微笑.png)|[未接入](../outputs/slot-backs-final-20261029/金色花纹/01-芙莉莲·微笑.png)|
|02|芙莉莲·晴空|[未接入](../outputs/slot-backs-final-20261029/基础圆角/02-芙莉莲·晴空.png)|[未接入](../outputs/slot-backs-final-20261029/金色花纹/02-芙莉莲·晴空.png)|
|03|辛美尔|[未接入](../outputs/slot-backs-final-20261029/基础圆角/03-辛美尔.png)|[未接入](../outputs/slot-backs-final-20261029/金色花纹/03-辛美尔.png)|
|04|芙莉莲·花冠|[未接入](../outputs/slot-backs-final-20261029/基础圆角/04-芙莉莲·花冠.png)|[未接入](../outputs/slot-backs-final-20261029/金色花纹/04-芙莉莲·花冠.png)|
|05|海塔|[未接入](../outputs/slot-backs-final-20261029/基础圆角/05-海塔.png)|[未接入](../outputs/slot-backs-final-20261029/金色花纹/05-海塔.png)|
|06|芙莉莲·水光|[未接入](../outputs/slot-backs-final-20261029/基础圆角/06-芙莉莲·水光.png)|[未接入](../outputs/slot-backs-final-20261029/金色花纹/06-芙莉莲·水光.png)|
|07|芙莉莲·温泉|[未接入](../outputs/slot-backs-final-20261029/基础圆角/07-芙莉莲·温泉.png)|[未接入](../outputs/slot-backs-final-20261029/金色花纹/07-芙莉莲·温泉.png)|
|08|芙莉莲·得意|[未接入](../outputs/slot-backs-final-20261029/基础圆角/08-芙莉莲·得意.png)|[未接入](../outputs/slot-backs-final-20261029/金色花纹/08-芙莉莲·得意.png)|
|09|芙莉莲·冰棒|[未接入](../outputs/slot-backs-final-20261029/基础圆角/09-芙莉莲·冰棒.png)|[未接入](../outputs/slot-backs-final-20261029/金色花纹/09-芙莉莲·冰棒.png)|
|10|芙莉莲·坠落|[未接入](../outputs/slot-backs-final-20261029/基础圆角/10-芙莉莲·坠落.png)|[未接入](../outputs/slot-backs-final-20261029/金色花纹/10-芙莉莲·坠落.png)|
|11|弗兰梅|[未接入](../outputs/slot-backs-final-20261029/基础圆角/11-弗兰梅.png)|[未接入](../outputs/slot-backs-final-20261029/金色花纹/11-弗兰梅.png)|
|12|芙莉莲·回眸|[未接入](../outputs/slot-backs-final-20261029/基础圆角/12-芙莉莲·回眸.png)|[未接入](../outputs/slot-backs-final-20261029/金色花纹/12-芙莉莲·回眸.png)|
|13|雪原旅途|[未接入](../outputs/slot-backs-final-20261029/基础圆角/13-雪原旅途.png)|[未接入](../outputs/slot-backs-final-20261029/金色花纹/13-雪原旅途.png)|
|14|芙莉莲·冬日|[未接入](../outputs/slot-backs-final-20261029/基础圆角/14-芙莉莲·冬日.png)|[未接入](../outputs/slot-backs-final-20261029/金色花纹/14-芙莉莲·冬日.png)|
|15|赛丽艾|[未接入](../outputs/slot-backs-final-20261029/基础圆角/15-赛丽艾.png)|[未接入](../outputs/slot-backs-final-20261029/金色花纹/15-赛丽艾.png)|
|16|菲伦·花冠|[未接入](../outputs/slot-backs-final-20261029/基础圆角/16-菲伦·花冠.png)|[未接入](../outputs/slot-backs-final-20261029/金色花纹/16-菲伦·花冠.png)|
|17|修塔尔克·魔族装扮|[未接入](../outputs/slot-backs-final-20261029/基础圆角/17-修塔尔克·魔族装扮.png)|[未接入](../outputs/slot-backs-final-20261029/金色花纹/17-修塔尔克·魔族装扮.png)|
|18|修塔尔克·决意|[未接入](../outputs/slot-backs-final-20261029/基础圆角/18-修塔尔克·决意.png)|[未接入](../outputs/slot-backs-final-20261029/金色花纹/18-修塔尔克·决意.png)|
|19|菲伦·整理头发|[未接入](../outputs/slot-backs-final-20261029/基础圆角/19-菲伦·整理头发.png)|[未接入](../outputs/slot-backs-final-20261029/金色花纹/19-菲伦·整理头发.png)|

### 二代新成就

没有新增超出一代104个ID的独立成就／卡背编号，也没有接入这38张新图。已有二代改版条件，不宜说成条件完全照搬：

- 5001“自尽吧，阿乌拉”：当前击败阿乌拉；一代同ID原为魔力总量达到100。
- 5012：当前本局超过1小时通关；一代原为2小时。
- 6001：当前无生命损伤击败宝箱怪。
- 7001：当前按弃牌堆、手牌、抽牌堆顺序完成三因子；一代同ID原为三个蓝图。

目前没有委托、强化精英、封魔矿、黑金之翼、777金币／负债的专属新成就。这里只统计现有实现，不代作者添加新的目标或奖励。

当前34个专项条件：

| ID | 成就 | 当前条件 |
|---|---|---|
|4001|我受够这些繁文缛节了！|打出破灭之雷魔法。|
|4002|来自地狱的业火！|打出地狱业火魔法。|
|4003|444！|打出视觉攻击魔法后使所有敌人生命值变为0。|
|4004|咔咔发出谴责|打出捕鸟魔法后使敌人眩晕。|
|4005|传说魔法的上限|打出童话的结尾后造成1000点以上伤害。|
|4006|我最讨厌debuff了！|一次打出清洁铜像生锈的魔法，移除至少3种负面能力。|
|4007|不可逃避的命运|打出寿命论。|
|4008|杀人魔法的威力|打出普通攻击魔法后造成了100点以上伤害。|
|4009|别睡啦！|睡衣形态时一次性消耗了30张以上魔力。|
|4010|开始发育|打出寻找遗失饰品的魔法。|
|4011|再来一份！|吃掉了水星布丁。|
|4012|一阵强劲的音乐响起|触发镜莲花戒指的效果。|
|4013|温柔的酒肉和尚|使用赞因的笔记。|
|4014|师祖的认可|创建一张自定义传说魔法。|
|4015|终极同调+加速同调？|终极加速同调一次。|
|5001|自尽吧，阿乌拉|击败Boss阿乌拉。|
|5002|“好厚的防御，感觉能切开”|回合开始后，拥有999点格挡。|
|5003|启动用了几回合？|回合开始后，拥有5种不同的正面效果。|
|5004|mua!|回合开始后，同时拥有辛美尔雕像、辛美尔之墓、镜莲花戒指。|
|5005|呜...让我再睡一会...|回合开始后，弃牌堆中有4张以上懒懒散散。|
|5006|脑瓜子嗡嗡的|回合开始后，抽牌堆中有8张以上眩晕。|
|5007|宝箱怪的肚子里好暖和|在宝箱怪战斗中触发吃人，获得好黑啊。|
|5008|有收藏癖的魔法使|胜利时，牌组中牌数为40张或以上。|
|5009|因为太谨慎所以全留着了|胜利时，魔法牌组中牌数为30张或以上。|
|5010|初窥门径|通关时，三个槽位都是稀有因子。|
|5011|喂！|胜利时，槽位中都是普通因子。|
|5012|呱！是白色鸡煲，快退口牙！|通关时，本局游戏时间超过1小时。|
|5013|你是怎么做到的？|胜利时，槽位中所有因子相同。|
|5014|我这的宝贝可不少！|胜利时，魔法牌组中拥有5张妙妙工具。|
|5015|小身材，大斧头！|回合开始时，拥有30点以上力量。|
|6001|打破魔咒|本场战斗中未受到任何生命伤害并击败宝箱怪。|
|6002|果然只是个赝品么|3回合内击败水镜恶魔：芙莉莲的复制体。|
|7001|这好像不太对...|三个谜题因子装在正确槽位后，同一回合按弃牌堆、手牌、抽牌堆的顺序咏唱。|
|7002|不愧是我！|按二代的槽位和顺序要求，成功完成三段谜题。|

## 本次文件清理

删除27份过时的初期计划、0.5.2交接、旧进度、旧测试报告、10月重复统计及过期源码快照清单，共740,415字节（约0.71MiB）。旧内容在Git历史中可恢复。修复了README、协作入口和源码打包工具对它们的引用；临时文本／体积审计改写入artifacts。

同时移除了15个旧审计临时目录中的 node_modules 依赖链接，未删除链接目标。作者Word／Excel、原图、PSD、外层参考仓库、待采用卡背成品和旧的未提交文档输出均未删除。保留构建验证读取的资产指纹、77张牌对应表及一代原始证据JSON。

删除清单：

- `docs/01-一代基线审计.md`
- `docs/02-需求文档.md`
- `docs/03-机制与交互规范.md`
- `docs/04-技术与开发规范.md`
- `docs/05-实施与验收计划.md`
- `docs/06-决策与风险台账.md`
- `docs/HANDOFF.md`
- `docs/HANDOFF_TESTS.md`
- `docs/TEST_REPORT.md`
- `docs/ART_AUDIO_PORT.md`
- `docs/REST_EVENT_COMPAT.md`
- `docs/CARD_NAMES.md`
- `docs/PORTING_REFERENCE.md`
- `docs/STS1_REMAINING_CONTENT.md`
- `docs/inventory/README.md`
- `先读我-作者接手.md`
- `TODOs进度.md`
- `SOURCE_SNAPSHOT.json`
- `SHA256SUMS.txt`
- `docs/review-20261030/card-statistics.json`
- `docs/review-20261030/card-statistics.md`
- `docs/review-20261030/package-size.md`
- `docs/review-20261030/power-icons.md`
- `docs/review-20261030/spell-pool.md`
- `docs/review-20261030/text-differences.json`
- `docs/review-20261030/text-differences.md`
- `docs/CARD_PARITY.md`
