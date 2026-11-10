# 代理文本来源核对

2026年11月10日，基于0.12.13工作区、一代完整中文本地化、作者现有及历史Word、作者本次背景解锁表和Git记录核对。0.12.15已落实作者本轮对矿山两个分支及两种圣徽的改稿；其他待核对文本保留。

本次能明确找到的代理旁白和占位句主要集中在开局拆页、美食结束页、矿山分支结束页、少数遗物风味文字与二代角色介绍。完整逐键索引见 [文本来源索引](inventory/text_authorship.jsonl)，共1395条中文与1395条英文文本，含原文、文件行号、判断理由和留空的作者处理意见字段。索引中的“未找到逐句确认”不等于“玩法未经授权”。

## 当前正式内容中优先核对的句子

| 位置 | 文本键 | 当前全文 | 来源判断 | 作者处理意见 |
|---|---|---|---|---|
| [characters:4](../FrierenMod/localization/zhs/characters.json#L4) | FRIERENMOD-FRIEREN.description | 精灵族魔法使，在搜集魔法的旅途中偶遇尖塔。<br>以魔力为核心运转，使用强大的魔法打败魔族。 | 代理适配改写：用于二代角色选择页；一代角色介绍并非此两句，未找到逐句确认。 |  |
| [characters:11](../FrierenMod/localization/zhs/characters.json#L11) | FRIERENMOD-FRIEREN.eventDeathPrevention | 旅途还没有结束。 | 代理补写：二代事件免死提示。 |  |
| [characters:12](../FrierenMod/localization/zhs/characters.json#L12) | FRIERENMOD-FRIEREN.unlockText | 踏上新的旅途。 | 代理补写：二代角色解锁提示。 |  |
| [events:9](../FrierenMod/localization/zhs/events.json#L9) | FRIERENMOD-READING_GRIMOIRE_EVENT.pages.BOOK_1.description | 你感到体内流动的[gold]魔力[/gold]正在[sine]发生变化[/sine]……<br><br>抽牌槽的因子已经装载。还有两本魔导书没有检查。 | 部分代理补写：前句“你感到体内流动的魔力正在发生变化”来自一代；新增“抽牌槽的因子已经装载。还有两本魔导书没有检查。” |  |
| [events:12](../FrierenMod/localization/zhs/events.json#L12) | FRIERENMOD-READING_GRIMOIRE_EVENT.pages.BOOK_2.description | 你感到体内流动的[gold]魔力[/gold]正在[sine]发生变化[/sine]……<br><br>[gold]手牌[/gold]槽的因子也已装载。现在，只剩下最后一本。 | 部分代理补写：前句来自一代；新增“手牌槽的因子也已装载。现在，只剩下最后一本。” |  |
| [events:15](../FrierenMod/localization/zhs/events.json#L15) | FRIERENMOD-READING_GRIMOIRE_EVENT.pages.TOOLS.description | 查看魔导书后，你的部分[gold]魔力[/gold]似乎[sine]具象化[/sine]了。<br><br>三本魔导书都检查完了，旅行箱里还有一些魔法道具。 | 部分代理补写：魔力具象化来自一代；新增“三本魔导书都检查完了，旅行箱里还有一些魔法道具。” |  |
| [events:18](../FrierenMod/localization/zhs/events.json#L18) | FRIERENMOD-READING_GRIMOIRE_EVENT.pages.DONE.description | 你收好了三本魔导书和[gold]两个神奇妙妙工具[/gold]，跃跃欲试地走向尖塔。 | 代理改写：为拆分开局流程重写的结束句；两个工具由作者指定，旁白措辞由代理写。 |  |
| [events:26](../FrierenMod/localization/zhs/events.json#L26) | FRIERENMOD-FOOD_EVENT.pages.FEAST.description | 你饱餐一顿，重新振作了精神。 | 代理改写：替换了一代贵族餐厅、狼吞虎咽、生命真谛的结束段；没有找到作者要求替换此段旁白。 |  |
| [events:83](../FrierenMod/localization/zhs/events.json#L83) | FRIERENMOD-SEALING_ORE_MINE.pages.PLAY.description | 你拿起一块[gold]封魔石[/gold]往里注入[gold]魔力[/gold]。比钻石还坚硬的[gold]封魔石[/gold]突然发出耀眼的光芒之后[gold]炸裂[/gold]开来，[gold]魔力[/gold]满溢而出。余兴节目结束，你继续踏上了旅程。 | 作者2026年11月10日改稿 | 0.12.15 已落实，待游戏实测 |
| [events:84](../FrierenMod/localization/zhs/events.json#L84) | FRIERENMOD-SEALING_ORE_MINE.pages.GREED.description | 你挑了一块[gold]最漂亮的封魔石[/gold]放进行李，虽然他会[red]影响你的咏唱[/red]，<br><br>[gold]但前方的商人一定会出个好价钱。[/gold] | 作者2026年11月10日改稿 | 0.12.15 已落实，待游戏实测 |
| [monsters:20](../FrierenMod/localization/zhs/monsters.json#L20) | FRIERENMOD-AURA.taunt | 哈哈哈，葬送的芙莉莲也不过如此。 | 待确认来源：二代阿乌拉对话；没有找到此句的一代原文或独立作者原稿。 |  |
| [relics:4](../FrierenMod/localization/zhs/relics.json#L4) | FRIERENMOD-HOLY_EMBLEM.flavor | 相当久远前的魔法使资格证明，但管理魔法的团体经常更换，芙莉莲对此头疼不已。 | 作者2026年11月10日改稿 | 0.12.15 已落实，待游戏实测 |
| [relics:61](../FrierenMod/localization/zhs/relics.json#L61) | FRIERENMOD-UPGRADED_HOLY_EMBLEM.flavor | 因为你的优异表现，这个魔法使资格证明已经得到了所有魔法团体的认可。 | 作者2026年11月10日改稿 | 0.12.15 已落实，待游戏实测 |
| [relics:64](../FrierenMod/localization/zhs/relics.json#L64) | FRIERENMOD-KRAFT_SERPENT_RING.flavor | 克拉伏特送给你的饯别礼。 | 代理补写：二代兼容遗物的简短收尾；奖励玩法有作者要求，但这句不等于经作者逐字确认。 |  |
| [relics:67](../FrierenMod/localization/zhs/relics.json#L67) | FRIERENMOD-AURA_COMPANION.flavor | 阿乌拉因为输给了自己的服从魔法，成为了你最忠诚的小提琴手。 |

## 旧设计及兼容文本


| 位置 | 文本键 | 当前全文 | 来源判断 | 作者处理意见 |
|---|---|---|---|---|

## 当前委托对白

作者最新核对表明确要求沿用frieren.json中的名称和文本，因此本批按原句保留。这里仍列全当前17项对白，方便逐条看；不因曾提交过就自动标成“逐句已验收”。

| 位置 | 文本键 | 当前全文 | 来源判断 | 作者处理意见 |
|---|---|---|---|---|
| [frieren:511](../FrierenMod/localization/zhs/frieren.json#L511) | COMMISSION.REQUEST.HUNT | “有股异常的魔力笼罩了强敌。任务危险指数3颗星，请阁下小心。” | 作者指定沿用当前frieren.json：这是采用当前文案的依据；不冒充每句都有独立逐字审阅记录。 |  |
| [frieren:514](../FrierenMod/localization/zhs/frieren.json#L514) | COMMISSION.REQUEST.GRAPES | “都说吃不到葡萄说葡萄酸，我倒看看酸葡萄有多酸！” | 作者指定沿用当前frieren.json：这是采用当前文案的依据；不冒充每句都有独立逐字审阅记录。 |  |
| [frieren:517](../FrierenMod/localization/zhs/frieren.json#L517) | COMMISSION.REQUEST.STATUE | “勇者辛美尔在我爷爷的爷爷那一代拯救了村子，先代立的雕像已经留存了几百年。” | 作者指定沿用当前frieren.json：这是采用当前文案的依据；不冒充每句都有独立逐字审阅记录。 |  |
| [frieren:520](../FrierenMod/localization/zhs/frieren.json#L520) | COMMISSION.REQUEST.WEDDING | “我想在花田里说出那句话。” | 作者指定沿用当前frieren.json：这是采用当前文案的依据；不冒充每句都有独立逐字审阅记录。 |  |
| [frieren:526](../FrierenMod/localization/zhs/frieren.json#L526) | COMMISSION.REQUEST.FESTIVAL | “领主的名字太拗口了，今晚我可不能咬到舌头！” | 作者指定沿用当前frieren.json：这是采用当前文案的依据；不冒充每句都有独立逐字审阅记录。 |  |
| [frieren:532](../FrierenMod/localization/zhs/frieren.json#L532) | COMMISSION.REQUEST.SUMMER | “新来的员工不知道刨冰是用冰做出来的，神了！” | 作者指定沿用当前frieren.json：这是采用当前文案的依据；不冒充每句都有独立逐字审阅记录。 |  |
| [frieren:535](../FrierenMod/localization/zhs/frieren.json#L535) | COMMISSION.REQUEST.LOST | “希望别又在我家旺财的肚子里” | 作者指定沿用当前frieren.json：这是采用当前文案的依据；不冒充每句都有独立逐字审阅记录。 |  |
| [frieren:541](../FrierenMod/localization/zhs/frieren.json#L541) | COMMISSION.REQUEST.TUTOR | “上次的蓝发老师可是水王级魔法使，只可惜被那个黄毛拐跑了” | 作者指定沿用当前frieren.json：这是采用当前文案的依据；不冒充每句都有独立逐字审阅记录。 |  |
| [frieren:544](../FrierenMod/localization/zhs/frieren.json#L544) | COMMISSION.REQUEST.CARGO | “物品：电脑配件。用户备注：私密发货。擅自打开脑子会被遭雷劈。” | 作者指定沿用当前frieren.json：这是采用当前文案的依据；不冒充每句都有独立逐字审阅记录。 |  |
| [frieren:547](../FrierenMod/localization/zhs/frieren.json#L547) | COMMISSION.REQUEST.EXHIBITION | “村里的魔法使都在北方诸国参军去了，但用魔法压轴可是我们村子的惯例。” | 作者指定沿用当前frieren.json：这是采用当前文案的依据；不冒充每句都有独立逐字审阅记录。 |  |
| [frieren:550](../FrierenMod/localization/zhs/frieren.json#L550) | COMMISSION.REQUEST.LIBRARY | “传说魔法见者不曾言闻者不可见，好像能毁灭整个世界！” | 作者指定沿用当前frieren.json：这是采用当前文案的依据；不冒充每句都有独立逐字审阅记录。 |  |
| [frieren:553](../FrierenMod/localization/zhs/frieren.json#L553) | COMMISSION.REQUEST.LESSON | “请带上认真修炼过的魔法，练到可以反着咏唱的程度。” | 作者指定沿用当前frieren.json：这是采用当前文案的依据；不冒充每句都有独立逐字审阅记录。 |  |
| [frieren:562](../FrierenMod/localization/zhs/frieren.json#L562) | COMMISSION.REQUEST.VARIETY | “临时招募魔法使助手。这是我最后一次魔术秀，让我的魔法使梦想成真吧。” | 作者指定沿用当前frieren.json：这是采用当前文案的依据；不冒充每句都有独立逐字审阅记录。 |  |
| [frieren:638](../FrierenMod/localization/zhs/frieren.json#L638) | COMMISSION.REQUEST.GRANNY | 如果把精英魔物讨伐掉的话，要我给你奖励也不是不可以 | 作者指定沿用当前frieren.json：这是采用当前文案的依据；不冒充每句都有独立逐字审阅记录。 |  |
| [frieren:641](../FrierenMod/localization/zhs/frieren.json#L641) | COMMISSION.REQUEST.NAMUSAN | Domo，芙莉莲=san，实际达人！请指教在下魔法！ | 作者指定沿用当前frieren.json：这是采用当前文案的依据；不冒充每句都有独立逐字审阅记录。 |  |
| [frieren:644](../FrierenMod/localization/zhs/frieren.json#L644) | COMMISSION.REQUEST.STARK | 我已经受够当魔物诱饵的日子了芙莉莲，再受伤我就回老家了。 | 作者指定沿用当前frieren.json：这是采用当前文案的依据；不冒充每句都有独立逐字审阅记录。 |  |
| [frieren:647](../FrierenMod/localization/zhs/frieren.json#L647) | COMMISSION.REQUEST.GUIDE | 听说有问号的地方就有宝藏！我们需要一个资深导游带路 | 作者指定沿用当前frieren.json：这是采用当前文案的依据；不冒充每句都有独立逐字审阅记录。 |  |

## 效果说明与界面用语

卡牌、能力、因子、魔具、奖励、选牌提示的大量文字由代理按已授权效果整理成二代格式；有些与作者原稿不逐字相同。这一类没有一概判为擅自设计。索引逐条列出尚无逐字原稿匹配的内容，可以按table与status筛选。原版选牌提示的复用不增加新设定；代码内硬编码中文另核查后仅有日志、报错和筛选用“魔法”，未找到遗漏的硬编码剧情段落。

## 英文文本

英文分为一代英文移植与代理翻译／二代适配。中文获准不代表英文每句话均已被作者审阅；没有查到整套英文逐句验收的记录。本次23组背景名称的英文也是代理翻译。英文原文保留在eng各表，可在中文定稿后逐表核对；本次没有为了消除审计项而改写英文剧情。

## 临时预览文本

12张插画标题、上一张／下一张／离开以及“普通／淡入／飞入／省略号”等说明由代理为用户要求的预览写成，只用于控制台测试事件，不在正式随机池。它们是界面演示文本，不应冒充作者正式剧情。

## 基线来源判断统计 0.12.13

| 判断 | 中文键数 |
|---|---:|
| 未找到逐字来源／确认 | 787 |
| 一代原文匹配 | 466 |
| 作者本次填写并明确要求实装 | 46 |
| 作者指定沿用当前frieren.json | 34 |
| 代理撰写的临时预览界面 | 20 |
| 现有作者需求文档有对应 | 8 |
| 代理补写／已停用委托 | 8 |
| 代理补写 | 7 |
| 作者工作区JSON修订 | 5 |
| 部分代理补写 | 3 |
| 代理旧设计／已退出新获取 | 3 |
| 代理改写 | 2 |
| 待确认来源 | 2 |
| 留空，无新增风味文字 | 2 |
| 代理适配改写 | 1 |
| 代理补写／旧兼容项 | 1 |

没有把未找到本地批准记录解释为“用户从未批准”。聊天批准可能未逐句持久化；可疑处保留当前内容，由作者在上表或索引的authorDecision中填写意见。已有整体玩法核对文档仍见 [代理新增内容核对](AGENT_ADDITIONS_REVIEW.md)。
