# 二代中文增补文本核对清单

核对日期：2026-10-31。当前版本：0.10.5。扫描 1099 条中文本地化，460 条与完整一代原文一致（忽略格式、空白和标点），其余 639 条全部列出。

这是可复核的差异清单，不能仅凭措辞确认是谁写的或是否由 AI 生成。作者已确认的二代规则、新内容、原生界面所需提示和未经确认的扩写都会出现在差异中；本次不自动删除。近似一代文案用于对照，不等于已确认的映射。所有字段保留在同目录 JSON。

另列本地化之外中文字符串候选 30 处，包含诊断字符串，供按来源确认。
优先检查事件、教程、按钮的补充限制说明，再检查卡牌数值或玩法变化。用户点名的“需要可以升级的牌”见事件中的禁用条件提示。

## events.json（45 条）

| ID | 二代当前全文 | 一代对照原文 | 归一化新增/替换片段 |
|---|---|---|---|
| FRIERENMOD-READING_GRIMOIRE_EVENT.pages.INITIAL.description | “哇，好高一座塔！”<br><br>在你为又能探索新迷宫而喜悦之时，你突然发现旅行箱中的三本魔导书出现了[jitter]异样[/jitter]。可能是一边的大鲸鱼导致的…… |  “哇，好高一座塔！”，在你为又能探索新迷宫而喜悦之时，你突然发现旅行箱中的三本魔导书出现了 @异样@ 。可能是一边的大鲸鱼导致的... [events.json / FrierenMod:ReadingGrimoireEvent.DESCRIPTIONS[1]] | …… |
| FRIERENMOD-READING_GRIMOIRE_EVENT.pages.INITIAL.options.INSPECT.title | 检查魔导书 | 检查第二本... [events.json / FrierenMod:ReadingGrimoireEvent.OPTIONS[1]] | 魔导书 |
| FRIERENMOD-READING_GRIMOIRE_EVENT.pages.INITIAL.options.INSPECT.description | 依次为三个槽位选择初始[gold]魔法因子[/gold]。 | [检查魔导书] 依次选择初始 #y槽位 中的 #y魔法因子 。 [events.json / FrierenMod:ReadingGrimoireEvent.OPTIONS[0]] | 为三个槽位 |
| FRIERENMOD-READING_GRIMOIRE_EVENT.pages.BOOK_0.description | “哇，好高一座塔！”<br><br>在你为又能探索新迷宫而喜悦之时，你突然发现旅行箱中的三本魔导书出现了[jitter]异样[/jitter]。可能是一边的大鲸鱼导致的…… |  “哇，好高一座塔！”，在你为又能探索新迷宫而喜悦之时，你突然发现旅行箱中的三本魔导书出现了 @异样@ 。可能是一边的大鲸鱼导致的... [events.json / FrierenMod:ReadingGrimoireEvent.DESCRIPTIONS[1]] | …… |
| FRIERENMOD-READING_GRIMOIRE_EVENT.pages.BOOK_0.options.INSPECT.title | 检查第一本魔导书 | 检查第二本... [events.json / FrierenMod:ReadingGrimoireEvent.OPTIONS[1]] | 一 / 魔导书 |
| FRIERENMOD-READING_GRIMOIRE_EVENT.pages.BOOK_0.options.INSPECT.description | 选择一张初始[gold]魔法因子[/gold]，放入[gold]抽牌槽[/gold]。 |  选择一张放入抽牌槽  [events.json / FrierenMod:ReadingGrimoireEvent.DESCRIPTIONS[4]] | 初始魔法因子 |
| FRIERENMOD-READING_GRIMOIRE_EVENT.pages.BOOK_1.description | 你感到体内流动的[gold]魔力[/gold]正在[sine]发生变化[/sine]……<br><br>抽牌槽的因子已经装载。还有两本魔导书没有检查。 |  你感到体内流动的魔力正在 ~发生变化~ ... [events.json / FrierenMod:ReadingGrimoireEvent.DESCRIPTIONS[2]] | ……抽牌槽的因子已经装载还有两本魔导书没有检查 |
| FRIERENMOD-READING_GRIMOIRE_EVENT.pages.BOOK_1.options.INSPECT.title | 检查第二本…… | 检查第二本... [events.json / FrierenMod:ReadingGrimoireEvent.OPTIONS[1]] | …… |
| FRIERENMOD-READING_GRIMOIRE_EVENT.pages.BOOK_1.options.INSPECT.description | 选择一张初始[gold]魔法因子[/gold]，放入[gold]手牌槽[/gold]。 |  选择一张放入手牌槽  [events.json / FrierenMod:ReadingGrimoireEvent.DESCRIPTIONS[5]] | 初始魔法因子 |
| FRIERENMOD-READING_GRIMOIRE_EVENT.pages.BOOK_2.description | 你感到体内流动的[gold]魔力[/gold]正在[sine]发生变化[/sine]……<br><br>手牌槽的因子也已装载。现在，只剩下最后一本。 |  你感到体内流动的魔力正在 ~发生变化~ ... [events.json / FrierenMod:ReadingGrimoireEvent.DESCRIPTIONS[2]] | ……手牌槽的因子也已装载现在只剩下最后一本 |
| FRIERENMOD-READING_GRIMOIRE_EVENT.pages.BOOK_2.options.INSPECT.title | 检查最后一本…… | 检查最后一本... [events.json / FrierenMod:ReadingGrimoireEvent.OPTIONS[2]] | …… |
| FRIERENMOD-READING_GRIMOIRE_EVENT.pages.BOOK_2.options.INSPECT.description | 选择一张初始[gold]魔法因子[/gold]，放入[gold]弃牌槽[/gold]。 |  选择一张放入弃牌槽  [events.json / FrierenMod:ReadingGrimoireEvent.DESCRIPTIONS[6]] | 初始魔法因子 |
| FRIERENMOD-READING_GRIMOIRE_EVENT.pages.TOOLS.description | 查看魔导书后，你的部分[gold]魔力[/gold]似乎[sine]具象化[/sine]了。<br><br>三本魔导书都检查完了，旅行箱里还有一些魔法道具。 |  查看魔导书后，你的部分 #y魔力 似乎 ~具象化~ 了。你跃跃欲试地走向尖塔。 [events.json / FrierenMod:ReadingGrimoireEvent.DESCRIPTIONS[3]] | 三本魔导书都检查完了旅行箱里还有一些魔法道具 |
| FRIERENMOD-READING_GRIMOIRE_EVENT.pages.TOOLS.options.OBTAIN.title | 还有一些魔法道具？ | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-READING_GRIMOIRE_EVENT.pages.TOOLS.options.OBTAIN.description | 获得[gold]两个神奇妙妙工具[/gold]。 | [还有一些魔法道具？] 获得2张 #y魔具 。 [events.json / FrierenMod:ReadingGrimoireEvent.OPTIONS[3]] | 两个神奇妙妙工 |
| FRIERENMOD-READING_GRIMOIRE_EVENT.pages.DONE.description | 你收好了三本魔导书和[gold]两个神奇妙妙工具[/gold]，跃跃欲试地走向尖塔。 |  查看魔导书后，你的部分 #y魔力 似乎 ~具象化~ 了。你跃跃欲试地走向尖塔。 [events.json / FrierenMod:ReadingGrimoireEvent.DESCRIPTIONS[3]] | 你收好了三本 / 和两个神奇妙妙工 |
| FRIERENMOD-READING_GRIMOIRE_EVENT.pages.DONE.options.DEPART.description | 继续前往大鲸鱼的祝福。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-FOOD_EVENT.pages.FEAST.description | 你饱餐一顿，重新振作了精神。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-FOOD_EVENT.pages.INITIAL.options.FREE.title | 免费小吃 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-FOOD_EVENT.pages.INITIAL.options.FREE.description | 回复 [green]8[/green] 点生命。 | [免费小吃] #g回复  #g [events.json / FrierenMod:FoodEvent.OPTIONS[0]] | 8点生命 |
| FRIERENMOD-FOOD_EVENT.pages.INITIAL.options.PACKAGE.title | 打包了吃 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-FOOD_EVENT.pages.INITIAL.options.MEAL.title | 我吃 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-FOOD_EVENT.pages.INITIAL.options.MEAL.description | 支付 [gold]150[/gold] 金币，[green]获得草莓，回复 [green]{MealHeal}[/green] 点生命。[/green] | [我吃] #y支付150金币:  #g获得草莓，回复  #g [events.json / FrierenMod:FoodEvent.OPTIONS[3]] | {MealHeal}点生命 |
| FRIERENMOD-FOOD_EVENT.pages.INITIAL.options.FEAST.title | 我吃吃吃 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-FOOD_EVENT.pages.INITIAL.options.FEAST.description | 支付[gold]250[/gold]金币。获得[green]芒果[/green]，恢复[green]{FeastHeal}[/green]点生命。 | [我吃吃吃] #y支付250金币:  #g获得芒果，回复所有生命。 [events.json / FrierenMod:FoodEvent.OPTIONS[5]] | 恢 / {FeastHeal}点 |
| FRIERENMOD-KRAFT_GIFT.pages.INITIAL.options.MEAT.title | 给贪吃鬼的礼物 | 克拉伏特的礼物 [events.json / FrierenMod:KraftGift.NAME] | 给贪吃鬼 |
| FRIERENMOD-KRAFT_GIFT.pages.INITIAL.options.MEAT.description | [green]获得带骨肉。[/green]需要最大生命至少 [gold]80[/gold]。 | [给贪吃鬼的礼物] #g获得带骨肉。 [events.json / FrierenMod:KraftGift.OPTIONS[0]] | 需要最大生命至少80 |
| FRIERENMOD-KRAFT_GIFT.pages.INITIAL.options.MEMBERSHIP.title | 给大富豪的礼物 | 克拉伏特的礼物 [events.json / FrierenMod:KraftGift.NAME] | 给大富豪 |
| FRIERENMOD-KRAFT_GIFT.pages.INITIAL.options.MEMBERSHIP.description | [green]获得会员卡。[/green]需要持有至少 [gold]300[/gold] 金币，不消耗金币。 | [给大富豪的礼物] #g获得会员卡。 [events.json / FrierenMod:KraftGift.OPTIONS[1]] | 需要持有至少300金币不消耗金币 |
| FRIERENMOD-KRAFT_GIFT.pages.INITIAL.options.RING.title | 给囤积癖的礼物 | 克拉伏特的礼物 [events.json / FrierenMod:KraftGift.NAME] | 给囤积癖 |
| FRIERENMOD-KRAFT_GIFT.pages.INITIAL.options.RING.description | [green]获得长蛇戒指。[/green]需要牌组至少 [gold]30[/gold] 张牌。 | [给囤积癖的礼物] #g获得长蛇戒指。 [events.json / FrierenMod:KraftGift.OPTIONS[2]] | 需要牌组至少30张牌 |
| FRIERENMOD-KRAFT_GIFT.pages.INITIAL.options.LEAVE.title | 谢绝礼物 | 克拉伏特的礼物 [events.json / FrierenMod:KraftGift.NAME] | 谢绝 |
| FRIERENMOD-KRAFT_GIFT.pages.INITIAL.options.LEAVE.description | 离开。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-ANIMAL_WELL.pages.INITIAL.options.FIRECRACKERS.title | 扔强力爆竹 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-ANIMAL_WELL.pages.INITIAL.options.FIRECRACKERS.description | [green]获得火焰药水和爆炸药水各一瓶。[/green] | [扔强力爆竹] #g获得2瓶火焰药水。 [events.json / FrierenMod:AnimalWell.OPTIONS[0]] | 和爆炸药水各一瓶 |
| FRIERENMOD-ANIMAL_WELL.pages.INITIAL.options.FLUTE.title | 吹动物笛子 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-ANIMAL_WELL.pages.INITIAL.options.FLUTE.description | [green]升级[/green]一张牌。需要可以升级的牌。 | [锁定] 需要：可以升级的牌 [events.json / FrierenMod:AnimalWell.OPTIONS[4]] | 升级一张牌 |
| FRIERENMOD-ANIMAL_WELL.pages.INITIAL.options.BIRD.title | 用捕鸟魔法 | [用捕鸟魔法] #g获得捕鸟魔法 [events.json / FrierenMod:AnimalWell.OPTIONS[2]] | 用 |
| FRIERENMOD-MIMIC_FIGHT.pages.INITIAL.options.IMPULSE.title | 冲动 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-MIMIC_FIGHT.pages.INITIAL.options.REASON.title | 理智 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-MIMIC_FIGHT.pages.INITIAL.options.FIGHT.title | 战斗 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-MIMIC_FIGHT.pages.INITIAL.options.FIGHT.description | 和宝箱怪[red]一决胜负！[/red][green]获得 [gold]100[/gold] 金币、宝箱怪的头和一件稀有遗物；持有伪作时改为获得大魔法使伏拉梅的著作。[/green] | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-MIMIC_FIGHT.pages.INITIAL.options.EXPLODE.title | 爆破 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-MIMIC_FIGHT.pages.INITIAL.options.EXPLODE.description | 失去 [red]15[/red] 点生命，[green]获得卷发形态。[/green] | [爆破] #r失去15生命 , #g获得卷发形态。 [events.json / FrierenMod:MimicFight.OPTIONS[3]] | 点 |
| FRIERENMOD-MIMIC_FIGHT.pages.INITIAL.options.RESCUE.title | 逃脱 | 未找到近似原文 | 整段新增或需人工对照 |
## frieren.json（233 条）

| ID | 二代当前全文 | 一代对照原文 | 归一化新增/替换片段 |
|---|---|---|---|
| LIBRARY_MAGIC_ITEMS | 魔法因子和魔具 | 魔法因子/魔具 [UIs.json / FrierenMod:CardStyleText.TEXT[3]] | 和 |
| BOOK | 魔法因子与魔具 | 魔法因子/魔具 [UIs.json / FrierenMod:CardStyleText.TEXT[3]] | 与 |
| CLOSE | 关闭 | 未找到近似原文 | 整段新增或需人工对照 |
| COST_ONE | 咏唱 1 需魔力 | 魔力 [cards.json / FrierenMod:Mana.NAME] | 咏唱1需 |
| MANA_HINT | 青色数字表示可发动咏唱 1。<br>因子详情可在魔法书中查看。 | 未找到近似原文 | 整段新增或需人工对照 |
| SLOT_UNAVAILABLE | 当前槽位无法发动咏唱。 | 当前卡槽背景： [UIs.json / FrierenMod:SlotBgPreview.TEXT[0]] | 位无法发动咏唱 |
| PLAY.NO_MANA | 没有足够魔力激活魔法因子 | 没有足够 #r魔力 以激活 #y魔法因子 。 [UIs.json / FrierenMod:cantUseMessage.TEXT[0]] | 整段新增或需人工对照 |
| SLOT_SHORT.Hand | 手 | 手牌槽 [keywords.json / [5].PROPER_NAME] | 整段新增或需人工对照 |
| SLOT_SHORT.Discard | 弃 | 嫌弃脸 [cards.json / FrierenMod:GrossedOutLook.NAME] | 整段新增或需人工对照 |
| BOOK_HINT | 右键启用魔具，左键选择目标；选满后生效。再次右键魔具或按 Esc 取消。 | 未找到近似原文 | 整段新增或需人工对照 |
| BOOK_LOCKED | 当前仅可查看：需要完成初始因子选择；选择或同步结算期间请等待。 | 未找到近似原文 | 整段新增或需人工对照 |
| EMPTY | 未装载因子 | 未装载 [keywords.json / [2].PROPER_NAME] | 因子 |
| SLOT.Draw | 抽牌堆 | 抽牌槽 [UIs.json / FrierenMod:Slot.TEXT[0]] | 堆 |
| SLOT.Hand | 手牌 | 手牌槽 [UIs.json / FrierenMod:Slot.TEXT[1]] | 整段新增或需人工对照 |
| SLOT.Discard | 弃牌堆 | 弃牌槽 [UIs.json / FrierenMod:Slot.TEXT[2]] | 堆 |
| CHANT | 从{Slot}[gold]消耗[/gold]{Cost}张[gold]魔力[/gold]， | 未找到近似原文 | 整段新增或需人工对照 |
| INITIAL | 装载至{Slot} | 未找到近似原文 | 整段新增或需人工对照 |
| COLLECT | 魔法因子 · 未装载 | 魔法因子 [UIs.json / FrierenMod:MagicItemTip.TEXT[0]] | ·未装载 |
| FACTOR_REWARD | 魔法因子或魔具（三选一） | 魔法因子/魔具 [UIs.json / FrierenMod:CardStyleText.TEXT[3]] | 或 / 三选一 |
| TIP.CHANT.description | 选择一个装载中的因子，[gold]消耗[/gold]其牌堆所需的[gold]魔力[/gold]并发动效果。因子的 X 取[gold]咏唱[/gold]卡的数值。 | 未找到近似原文 | 整段新增或需人工对照 |
| TIP.LEGENDARY.description | 本回合打出过[gold]咏唱[/gold]牌后，才能打出[gold]传说魔法[/gold]。 | 本回合中打出过 #y咏唱 牌后才可打出。 [keywords.json / [11].DESCRIPTION] | 能 / 传说魔法 |
| TIP.DIVINITY.title | 真言与神格 | 未找到近似原文 | 整段新增或需人工对照 |
| TIP.DIVINITY.description | 每满 10 层真言，[gold]消耗[/gold] 10 层，进入[gold]神格[/gold]并获得 {energyPrefix:energyIcons(3)}。[gold]神格[/gold]使攻击伤害变为三倍，持续至下回合。 | 未找到近似原文 | 整段新增或需人工对照 |
| TIP.MANA_PAYMENT.title | 魔力代付 | 魔力 [cards.json / FrierenMod:Mana.NAME] | 代付 |
| TIP.MANA_PAYMENT.description | 普通卡牌按原能量费用支付等量[gold]魔力[/gold]，依次从抽牌堆、手牌、弃牌堆[gold]消耗[/gold]。[gold]魔力[/gold]不足时无法打出。X 费牌仍[gold]消耗[/gold]能量。 | 未找到近似原文 | 整段新增或需人工对照 |
| TIP.SEALED.description | 战斗开始时移出牌堆。单回合打出 5 张[gold]咏唱[/gold]牌后，解除[gold]封印[/gold]并加入手牌。 | 战斗开始时此牌位于所有牌堆之外，一回合内打出5张 #y咏唱 牌后加入你的手牌。 [keywords.json / [19].DESCRIPTION] | 移出 / 单 / 解除封印并 |
| TIP.SECOND_DEFENSE.title | 额外的防御魔法 | 防御魔法 [cards.json / FrierenMod:DefensiveMagic.NAME] | 额外的 |
| TIP.SECOND_DEFENSE.description | {energyPrefix:energyIcons(0)}，[gold]消耗[/gold]。[gold]咏唱[/gold] 1，获得与生成它的防御魔法相同的格挡；生成牌[gold]保留[/gold]对应升级。 | 未找到近似原文 | 整段新增或需人工对照 |
| EFFECT.AdvancedDefensiveMagic | 获得 {Block:diff()} 点格挡两次。 | 获得 !B! 点 格挡 。 [cards.json / FrierenMod:Defend_Frieren.DESCRIPTION] | { / lockdiff} / 两次 |
| FACTOR.AdvancedDefensiveMagic | [gold]咏唱[/gold] X：获得 X 点格挡两次。 |  消耗 牌堆中X张 *魔力 ，获得X点 格挡 两次。 [cards.json / FrierenMod:AdvancedDefensiveMagic.EXTENDED_DESCRIPTION[0]] | 咏唱 |
| EFFECT.BePrepared | 获得 {Reward} 层活力。 | 未找到近似原文 | 整段新增或需人工对照 |
| FACTOR.BePrepared | [gold]咏唱[/gold] X：获得 X+3层活力 | 咏唱X [keywords.json / [0].PROPER_NAME] | 获得X+3层活力 |
| EFFECT.BuildUpBody | 获得 {Reward} 点力量。 | 你每 #y同调 一张 #y魔力，获得 #b%d 点 #y力量 。 [powers.json / FrierenMod:OutpouringPower.DESCRIPTIONS[0]] | {Rewar / } |
| FACTOR.BuildUpBody | [gold]咏唱[/gold] X：获得 X 点力量。 |  消耗 牌堆中X张 *魔力 ，获得X点 力量 。 [cards.json / FrierenMod:BuildUpBody.EXTENDED_DESCRIPTION[0]] | 咏唱 |
| EFFECT.GrossedOutLook | 所有敌人在本回合失去 {Reward} 点力量。 |  消耗 牌堆中X张 *魔力 ，所有敌人在本回合内失去x+2点 力量 。 [cards.json / FrierenMod:GrossedOutLook.EXTENDED_DESCRIPTION[0]] | {Reward} |
| FACTOR.GrossedOutLook | [gold]咏唱[/gold] X：所有敌人本回合失去 X+2 点力量。 |  消耗 牌堆中X张 *魔力 ，所有敌人在本回合内失去x+2点 力量 。 [cards.json / FrierenMod:GrossedOutLook.EXTENDED_DESCRIPTION[0]] | 咏唱 / X |
| EFFECT.IceArrows | 所有敌人失去 {Reward} 点生命。 |  消耗 牌堆中X张 *魔力 ，所有敌人失去4X点生命 。 [cards.json / FrierenMod:IceArrows.EXTENDED_DESCRIPTION[0]] | {Reward} |
| FACTOR.IceArrows | [gold]咏唱[/gold] X：所有敌人失去4X点生命。 |  消耗 牌堆中X张 *魔力 ，所有敌人失去4X点生命 。 [cards.json / FrierenMod:IceArrows.EXTENDED_DESCRIPTION[0]] | 咏唱 |
| EFFECT.MeleeMage | 本场战斗中，名称含“打击”的牌伤害增加 {Reward}。 | 本场战斗中手牌上限减少 #b%d 。 [powers.json / FrierenMod:SwallowedPower.DESCRIPTIONS[0]] | 名称含打击的 / 伤害增加{Rewar / } |
| FACTOR.MeleeMage | [gold]咏唱[/gold] X：本场战斗中，打击牌的伤害增加 X+1。 | 在本场战斗中 升级 手牌中的所有牌。 [cards.json / FrierenMod:UpgradeOption.DESCRIPTION] | 咏唱X / 打击 / 伤害增加X+1 |
| EFFECT.MetalPetalSpell | 获得 {Reward} 层金属化：回合结束时获得等量格挡。 | 获得力量。在你的回合结束时，失去相等数量的力量。 [keywords.json / [13].DESCRIPTION] | {Reward}层金属化 / 获得 / 格挡 |
| FACTOR.MetalPetalSpell | [gold]咏唱[/gold] X：获得 X 层金属化：回合结束时获得等量格挡。 |  消耗 牌堆中X张 *魔力 ，获得X层 金属化 。 [cards.json / FrierenMod:MetallicizFlower.EXTENDED_DESCRIPTION[0]] | 咏唱 / 回合结束时获得等量格挡 |
| EFFECT.Sleeeep | 下回合多抽 {Reward} 张牌。 |  消耗 牌堆中X张 *魔力 ，下一回合抽X张牌。 [cards.json / FrierenMod:Sleeeep.EXTENDED_DESCRIPTION[0]] | 多 / {Reward} |
| FACTOR.Sleeeep | [gold]咏唱[/gold] X：下回合多抽 X 张牌。 |  消耗 牌堆中X张 *魔力 ，下一回合抽X张牌。 [cards.json / FrierenMod:Sleeeep.EXTENDED_DESCRIPTION[0]] | 咏唱 / 多 |
| EFFECT.TransferToAlchemist | 给予所有敌人 {Reward} 层中毒。 |  消耗 牌堆中X张 *魔力 ，给予所有敌人2X层 中毒 。 [cards.json / FrierenMod:TransferToAlchemist.EXTENDED_DESCRIPTION[0]] | {Reward} |
| FACTOR.TransferToAlchemist | [gold]咏唱[/gold] X：给予所有敌人2X层中毒。 |  消耗 牌堆中X张 *魔力 ，给予所有敌人2X层 中毒 。 [cards.json / FrierenMod:TransferToAlchemist.EXTENDED_DESCRIPTION[0]] | 咏唱 |
| EFFECT.Factor100 | ……[color=#e1c6ed]芙莉莲[/color]…… | 芙莉莲 [characters.json / FrierenMod:Frieren.NAMES[0]] | …… / …… |
| FACTOR.Factor100 | [gold]咏唱[/gold] X+1：……[color=#e1c6ed]芙莉莲[/color]…… |  消耗 牌堆中X+1张 *魔力 , ... [#e1c6ed]芙莉莲 ... [cards.json / FrierenMod:Factor100.EXTENDED_DESCRIPTION[0]] | 咏唱 / …… / …… |
| EFFECT.BarrierMagic | 获得 {Block:diff()} 点格挡与 {Reward} 层残影。 |  NL （获得 !B! 点 格挡 和 !M! 点 frierenmod:专注 ） [cards.json / FrierenMod:SorryICant.EXTENDED_DESCRIPTION[0]] | { / lockdiff} / 与{Rewa / }层残影 |
| FACTOR.BarrierMagic | [gold]咏唱[/gold] X+1：获得 X 点格挡与 X 层残影。 | 获得 !B! 点 格挡 。 [cards.json / FrierenMod:Defend_Frieren.DESCRIPTION] | 咏唱X+1 / X / 与X层残影 |
| EFFECT.BigFire | 将 {Reward} 张小小火花加入此槽位的牌堆。 |  消耗 牌堆中2X张 *魔力 ，将X张 *小小火花 放入牌堆。 [cards.json / FrierenMod:BigFire.EXTENDED_DESCRIPTION[0]] | {Reward} / 加 / 此槽位的 |
| FACTOR.BigFire | [gold]咏唱[/gold] 2X：将 X 张小小火花加入此槽位牌堆。 |  消耗 牌堆中2X张 *魔力 ，将X张 *小小火花 放入牌堆。 [cards.json / FrierenMod:BigFire.EXTENDED_DESCRIPTION[0]] | 咏唱 / 加 / 此槽位 |
| EFFECT.BloodSpell | 消耗 [red]{Reward}[/red] 点生命，在此牌堆中生成 {Reward} 张[gold]魔力[/gold]。 | 你的回合结束时， #y消耗 弃牌堆中 #b%d 张 #y魔力 。 [powers.json / FrierenMod:FlyingMagicPower.DESCRIPTIONS[0]] | {Reward}点生命在此 / 生成{Rewar / } |
| FACTOR.BloodSpell | [gold]咏唱[/gold] X：消耗 [red]X[/red] 点生命，在此牌堆中生成 X 张[gold]魔力[/gold]。仅普通[gold]咏唱[/gold]生效。 |  消耗 牌堆中2X张 *魔力 ，获得X点 [E] 。 [cards.json / FrierenMod:Strategizing.EXTENDED_DESCRIPTION[0]] | 咏唱X / X点生命在此 / 生成 / 仅普通咏唱生效 |
| EFFECT.BluePrint | 复制下一个槽位的因子。 | 激活一个槽位中的魔法因子 [UIs.json / FrierenMod:ChantOneAction.TEXT[0]] | 复制下 |
| FACTOR.BluePrint | 复制下一个槽位的因子。 | 激活一个槽位中的魔法因子 [UIs.json / FrierenMod:ChantOneAction.TEXT[0]] | 复制下 |
| EFFECT.HighSpeedMovement | 获得 {Reward} 点敏捷。 | 获得 !M! 点 敏捷 。 NL  消耗 。 [cards.json / FrierenMod:DexterityOption.DESCRIPTION] | {Reward} |
| FACTOR.HighSpeedMovement | [gold]咏唱[/gold] X：获得 X 点敏捷。 | 获得 !M! 点 敏捷 。 NL  消耗 。 [cards.json / FrierenMod:DexterityOption.DESCRIPTION] | 咏唱X / X |
| EFFECT.Hypnosis | 给予所有敌人 {Reward} 层虚弱与易伤。 | 给与所有敌人 !M! 层 易伤 。 [cards.json / FrierenMod:VulnerableOption.DESCRIPTION] | 予 / {Reward} / 虚弱与 |
| FACTOR.Hypnosis | [gold]咏唱[/gold] 2X：给予所有敌人X层虚弱与易伤。 |  消耗 牌堆中2X张 *魔力 ，给予所有敌人X层 虚弱 和 易伤 。 [cards.json / FrierenMod:Hypnosis.EXTENDED_DESCRIPTION[0]] | 咏唱 / 与 |
| EFFECT.NatureBeauty | 下回合获得 {Reward:energyIcons()}。 | 造成 !D! 点伤害。 NL 在下一回合获得 !M! 点 frierenmod:专注 。 [cards.json / FrierenMod:ChargeUp.DESCRIPTION] | {Rewa / den / gyIco / s} |
| FACTOR.NatureBeauty | [gold]咏唱[/gold] X+1：下回合获得 X{energyPrefix:energyIcons(1)}。 | 未找到近似原文 | 整段新增或需人工对照 |
| EFFECT.PassOnTheTorch | 将 {Reward} 张[gold]魔力[/gold]加入下一个槽位的牌堆。 |  将 #b%d 张 #y魔力 加入手牌。 [potions.json / FrierenMod:BottledMana.DESCRIPTIONS[0]] | {Rewar / } / 下一个槽位的 / 堆 |
| FACTOR.PassOnTheTorch | [gold]咏唱[/gold] X：将 X 张[gold]魔力[/gold]加入下一个槽位的牌堆。 |  消耗 牌堆中X张 *魔力 ，将X张 *魔力 放入下个牌堆。 [cards.json / FrierenMod:PassOnTheTorch.EXTENDED_DESCRIPTION[0]] | 咏唱 / 加 / 一 / 槽位的 |
| EFFECT.PinchAndScrape | 接下来 {Reward} 次[gold]同调[/gold]将[gold]魔力[/gold]返回手牌而非[gold]消耗[/gold]。 | 下 #b%d 次 #y同调 不再 消耗 #y魔力 。 [powers.json / FrierenMod:SynchroWithoutManaPower.DESCRIPTIONS[0]] | 接 / 来{Rewar / } / 将魔力返回手牌而非 |
| FACTOR.PinchAndScrape | [gold]咏唱[/gold] X：下2X次[gold]同调[/gold]时将[gold]魔力[/gold]返回手牌而非[gold]消耗[/gold]。 | 随机造成X+4点伤害X次，将X张 *魔力 放入手牌。 [cards.json / FrierenMod:MultipleOffensiveMagic.DESCRIPTION] | 咏唱 / 下2 / 同调时 / 返回 / 而非消耗 |
| EFFECT.SpellBoost | 接下来 {Reward} 个回合中，攻击伤害翻倍。 |  消耗 牌堆中4X张 *魔力 ，下X个回合你所有的攻击伤害翻倍。 [cards.json / FrierenMod:SpellBoost.EXTENDED_DESCRIPTION[0]] | 接 / 来{Reward} / 中 |
| FACTOR.SpellBoost | [gold]咏唱[/gold] 4X：接下来 X 个回合中，攻击伤害翻倍。 |  消耗 牌堆中4X张 *魔力 ，下X个回合你所有的攻击伤害翻倍。 [cards.json / FrierenMod:SpellBoost.EXTENDED_DESCRIPTION[0]] | 咏唱 / 接 / 来 / 中 |
| EFFECT.Factor010 | ……[color=#FFFE91]消灭[/color]…… | ...消灭... [powers.json / FrierenMod:Factor010Power.DESCRIPTIONS[0]] | …… / …… |
| FACTOR.Factor010 | [gold]咏唱[/gold] 2X+2：……[color=#FFFE91]消灭[/color]…… |  消耗 牌堆中2X+2张 *魔力 , ... [#FFFE91]消灭 ... [cards.json / FrierenMod:Factor010.EXTENDED_DESCRIPTION[0]] | 咏唱 / …… / …… |
| EFFECT.GreatMageComposure | 接下来 {Reward} 次[gold]咏唱[/gold]不[gold]消耗[/gold][gold]魔力[/gold]。 | 在本回合中，下 #b%d 次 #y咏唱 不再 消耗 #y魔力 。 [powers.json / FrierenMod:ChantWithoutManaTimesPower.DESCRIPTIONS[0]] | 接 / 来{Rewar / } |
| FACTOR.GreatMageComposure | [gold]咏唱[/gold] 4X：接下来 X 次[gold]咏唱[/gold]无需[gold]消耗[/gold][gold]魔力[/gold]。 | 在本回合中，下 #b%d 次 #y咏唱 不再 消耗 #y魔力 。 [powers.json / FrierenMod:ChantWithoutManaTimesPower.DESCRIPTIONS[0]] | 咏唱4X接 / 来X / 无需 |
| EFFECT.ImpeccableIntangible | 获得 {Reward} 层无实体。 | 获得 !M! 层 无实体 。 NL  消耗 。 [cards.json / FrierenMod:IntangibleOption.DESCRIPTION] | {Reward} |
| FACTOR.ImpeccableIntangible | [gold]咏唱[/gold] 6X：获得 X 层无实体。 |  消耗 牌堆中6X张 *魔力 ，获得X层 无实体 。 [cards.json / FrierenMod:ImpeccableIntangible.EXTENDED_DESCRIPTION[0]] | 咏唱 |
| EFFECT.PeacefulDays | 抽 {Reward} 张牌。 | 抽 !M! 张牌。 [cards.json / FrierenMod:DrawOption.DESCRIPTION] | {Reward} |
| FACTOR.PeacefulDays | [gold]咏唱[/gold] X+1：抽 X 张牌。 |  消耗 牌堆中X+1张 *魔力 ，抽X张牌 。 [cards.json / FrierenMod:PeacefulDays.EXTENDED_DESCRIPTION[0]] | 咏唱 |
| EFFECT.Strategizing | 获得 {Reward:energyIcons()}。 | 未找到近似原文 | 整段新增或需人工对照 |
| FACTOR.Strategizing | [gold]咏唱[/gold] 2X：获得 X{energyPrefix:energyIcons(1)}。 | 未找到近似原文 | 整段新增或需人工对照 |
| EFFECT.TheLivingLegend | 将 {Reward} 张随机[gold]传说魔法[/gold]加入此槽位的牌堆。 | 将2张随机的 *魔法因子 放入 你的魔法牌组。 [cards.json / FrierenMod:MysteryBox.DESCRIPTION] | {Reward} / 传说 / 加 / 此槽位 / 堆 |
| FACTOR.TheLivingLegend | [gold]咏唱[/gold] X：将 X 张随机[gold]传说魔法[/gold]加入此槽位的牌堆。 | 将2张随机的 *魔法因子 放入 你的魔法牌组。 [cards.json / FrierenMod:MysteryBox.DESCRIPTION] | 咏唱X / X / 传说 / 加 / 此槽位 / 堆 |
| EFFECT.Transcendence | 获得 {Reward} 层真言。每满 10 层进入[gold]神格[/gold]并获得 {energyPrefix:energyIcons(3)}。 | 未找到近似原文 | 整段新增或需人工对照 |
| FACTOR.Transcendence | [gold]咏唱[/gold] X：获得 X 层真言。每满 10 层进入[gold]神格[/gold]并获得 {energyPrefix:energyIcons(3)}。 | 未找到近似原文 | 整段新增或需人工对照 |
| EFFECT.Factor001 | ……[color=#ed1c24]怪物[/color]…… | ...怪物... [powers.json / FrierenMod:Factor001Power.DESCRIPTIONS[0]] | …… / …… |
| FACTOR.Factor001 | [gold]咏唱[/gold] 3X+3：……[color=#ed1c24]怪物[/color]…… |  消耗 牌堆中3X+3张 *魔力 ... [#ed1c24]怪物 ... [cards.json / FrierenMod:Factor001.EXTENDED_DESCRIPTION[0]] | 咏唱 / …… / …… |
| PROP.CrystalBall | 选择两张已装载的因子，交换它们的槽位。 | 选择2张 frierenmod:[#ff4e41]已装载 的 *魔法因子 ，使它们 frierenmod:槽位 互换。 [cards.json / FrierenMod:CrystalBall.DESCRIPTION] | 两 / 交换 / 的 |
| PROP.MysteryBox | 获得两张随机魔法因子（不含谜题）。 | 魔法因子 [UIs.json / FrierenMod:MagicItemTip.TEXT[0]] | 获得两张随机 / 不含谜题 |
| PROP.MysticBox | 选择三张未装载的因子，随机装入三个槽位；原有因子回到库存。 | 未找到近似原文 | 整段新增或需人工对照 |
| PROP.SeinNote | 移除两张未装载的因子或其他魔具，回复 10 点生命。 | 移除2张 frierenmod:[#ff4e41]未装载 的 *魔法因子 或除自己之外的 *魔具 ，回复10点生命。 [cards.json / FrierenMod:SeinNote.DESCRIPTION] | 两 / 其他 |
| PROP.ShadowDragonHorn | 仅在战斗中使用。移除两张未装载的因子或其他魔具，获得 {energyPrefix:energyIcons(2)}。 | 仅战斗阶段生效：移除2张 frierenmod:[#ff4e41]未装载 的 *魔法因子 或除自己之外的 *魔具 ，获得 [E] [E] 。 [cards.json / FrierenMod:ShadowDragonHorn.DESCRIPTION] | 在 / 中使用 / 两 / 其他 / {energyPrefixenergyIcons2} |
| PROP.TransmutationCircle | 将两张因子变为同稀有度的随机因子（不含谜题因子），保留各自槽位。 | 未找到近似原文 | 整段新增或需人工对照 |
| PROP.UnbelievableTool | 选择一张已装载和一张未装载的因子，使它们交换槽位 | 选择2张 frierenmod:[#ff4e41]已装载 的 *魔法因子 ，使它们 frierenmod:槽位 互换。 [cards.json / FrierenMod:CrystalBall.DESCRIPTION] | 一 / 和一张未装载 / 交换 |
| PROP.UnbelievableToolBox | 移除两张未装载的因子或其他魔具，获得两件神奇妙妙工具。 | 移除2张 frierenmod:[#ff4e41]未装载 的 *魔法因子 或除自己之外的 *魔具 ，获得2张 *神奇妙妙工具 。 [cards.json / FrierenMod:UnbelievableToolBox.DESCRIPTION] | 两 / 其他 / 两件 |
| BOOK_INVENTORY | 未装载因子与魔具 | 未装载 [keywords.json / [2].PROPER_NAME] | 因子与魔具 |
| BOOK_TARGETS | {Prop} · 已选 {Selected}/{Amount} · 左键选择目标，Esc 取消 | 未找到近似原文 | 整段新增或需人工对照 |
| PROP_UNAVAILABLE | 暂时无法使用：请检查材料数量、装载状态与战斗阶段。 | 未找到近似原文 | 整段新增或需人工对照 |
| FACTOR_HINT | X 表示咏唱强度。因子通过魔具调整装载，战斗中按各牌堆的魔力发动。 | 未找到近似原文 | 整段新增或需人工对照 |
| NO_PROPS | 尚无魔具。到营地选择「逛魔具店」，获得两件后在这里使用。 | 未找到近似原文 | 整段新增或需人工对照 |
| USE_PROP | 使用 | 魔具 [%s] 使用中 [UIs.json / FrierenMod:MagicDeckScreen.TEXT[1]] | 整段新增或需人工对照 |
| PROP_USED | 魔具已生效并消耗，因子与库存已更新。 | 未找到近似原文 | 整段新增或需人工对照 |
| PROP_CANCELLED | 已取消或目标不符合条件，未消耗魔具。请按说明选择目标。 | 未找到近似原文 | 整段新增或需人工对照 |
| PROP_ERROR | 使用过程中出现错误，请查看游戏日志。 | 未找到近似原文 | 整段新增或需人工对照 |
| SHOP_SELECT | 选择 {Amount} 件魔具加入魔法牌组；确认后消耗本次营地行动，取消可返回。 | 选择2张魔具加入魔法牌组 [UIs.json / FrierenMod:BuyPropOption.TEXT[1]] | {Amount}件 / 确认后消耗本次营地行动取消可返回 |
| PROP_SELECT | 使用「{Prop}」：选择 {Amount} 个目标。取消不消耗魔具。 | 未找到近似原文 | 整段新增或需人工对照 |
| INVENTORY_CONTEXT | {Slot} | 未找到近似原文 | 整段新增或需人工对照 |
| CARD_SELECT | 选择卡牌 |  [%s] 选择中 [UIs.json / FrierenMod:MagicDeckScreen.TEXT[2]] | 卡牌 |
| CRAFT.Cost | 费用：{Amount:energyIcons()}。 | 未找到近似原文 | 整段新增或需人工对照 |
| CRAFT.Chant | [gold]咏唱[/gold]{Amount}。 | 未找到近似原文 | 整段新增或需人工对照 |
| CRAFT.ManaDraw | 在抽牌堆生成{Amount}张[gold]魔力[/gold]。 | 在弃牌堆中抽 !M! 张 *魔力 。 [cards.json / FrierenMod:BackFlow.DESCRIPTION] | 抽 / 生成{Amount} |
| CRAFT.ManaHand | 在手牌生成{Amount}张[gold]魔力[/gold]。 | 在弃牌堆中抽 !M! 张 *魔力 。 [cards.json / FrierenMod:BackFlow.DESCRIPTION] | 手 / 生成{Amount} |
| CRAFT.ManaDiscard | 在弃牌堆生成{Amount}张[gold]魔力[/gold]。 | 在弃牌堆中抽 !M! 张 *魔力 。 [cards.json / FrierenMod:BackFlow.DESCRIPTION] | 生成{Amount} |
| CRAFT.Scry | 预见{Amount}。 | 未找到近似原文 | 整段新增或需人工对照 |
| CRAFT.RandomCard | 随机生成{Amount}张普通牌加入手牌，本回合费用为0。 | 将 !M! 张随机普通牌放入手中。 NL 其在本回合耗能变为0。 [cards.json / FrierenMod:GainRandomCardOption.DESCRIPTION] | 生成{Amount}张 / 加 / 牌 / 费用 |
| CRAFT.Retain | 选择[gold]保留[/gold]{Amount}张手牌。 |  选择一张放入手牌槽  [events.json / FrierenMod:ReadingGrimoireEvent.DESCRIPTIONS[5]] | 保留{Amount} |
| CRAFT.ZeroCost | 随机将{Amount}张手牌本回合费用变为0。 |  张随机牌在本回合中耗能变为0。 [UIs.json / FrierenMod:CostZeroMod.TEXT[1]] | 将{Amount}张手 / 费用 |
| CRAFT.Block | 获得{Amount}点格挡。 | 获得 !B! 点 格挡 。 [cards.json / FrierenMod:Defend_Frieren.DESCRIPTION] | {Amount} |
| CRAFT.Damage | 造成{Amount}点伤害。 | 造成 !D! 点伤害。 [cards.json / FrierenMod:Strike_Frieren.DESCRIPTION] | {Amount} |
| CRAFT.DamageTriple | 造成{Amount}点伤害3次。 | 造成 !D! 点伤害三次。 [cards.json / FrierenMod:DamageThirdOption.DESCRIPTION] | {Amount} / 3 |
| CRAFT.DamageAll | 对所有敌人造成{Amount}点伤害。 | 对所有敌人造成 !D! 点伤害。 [cards.json / FrierenMod:DamageAllOption.DESCRIPTION] | {Amount} |
| CRAFT.Energy | 获得{Amount:energyIcons()}。 | 未找到近似原文 | 整段新增或需人工对照 |
| CRAFT.Execute | 若目标生命不超过{Amount}，消灭它。 | 未找到近似原文 | 整段新增或需人工对照 |
| CRAFT.Draw | 抽{Amount}张牌。 | 抽 !M! 张牌。 [cards.json / FrierenMod:DrawOption.DESCRIPTION] | {Amount} |
| CRAFT.Dexterity | 获得{Amount}点敏捷。[gold]消耗[/gold]。 | 获得 !M! 点 敏捷 。 NL  消耗 。 [cards.json / FrierenMod:DexterityOption.DESCRIPTION] | {Amount} |
| CRAFT.Vulnerable | 给予所有敌人{Amount}层易伤。 | 给与所有敌人 !M! 层 易伤 。 [cards.json / FrierenMod:VulnerableOption.DESCRIPTION] | 予 / {Amount} |
| CRAFT.Intangible | 获得{Amount}层无实体。[gold]消耗[/gold]。 | 获得 !M! 层 无实体 。 NL  消耗 。 [cards.json / FrierenMod:IntangibleOption.DESCRIPTION] | {Amount} |
| CRAFT.UpgradeHand | 升级所有手牌。 | [吹动物笛子] #g升级一张牌。 [events.json / FrierenMod:AnimalWell.OPTIONS[1]] | 所有手 |
| CRAFT.DoubleStrength | 力量翻倍。[gold]消耗[/gold]。 | 将你的 力量 翻倍。 NL  消耗 。 [cards.json / FrierenMod:StrengthOption.DESCRIPTION] | 整段新增或需人工对照 |
| CRAFT.RemoveDebuffs | 移除自身所有负面效果。 | 移除你身上的所有负面效果。 [cards.json / FrierenMod:RemoveDebuffOption.DESCRIPTION] | 自 |
| CRAFT.UpgradeAll | 升级本场战斗所有非[gold]消耗[/gold]牌。[gold]消耗[/gold]。 | 在本场战斗中 升级 你的所有牌。 NL  消耗 。 [cards.json / FrierenMod:UpgradeAllOption.DESCRIPTION] | 升级 / 非消耗 |
| TIP.ULTIMATE_SYNCHRO.description | 随机对1个敌人造成15点伤害。[gold]消耗[/gold]。（受力量等效果影响） | 随机对1个敌人造成15点伤害。 #y消耗 。 [keywords.json / [8].DESCRIPTION] | 受力量等效果影响 |
| TIP.BACKFIRE.description | 每次打出后，本牌在本回合的费用增加 1。 | 你在这个回合每打出一张卡牌，该敌人本回合受到的伤害增加 10% 。 [keywords.json / [18].DESCRIPTION] | 次 / 后本 / 在 / 费用 |
| HISTORY_BOOK_HINT | 历史魔法牌组 · 仅供查看 | 魔法牌组 [UIs.json / FrierenMod:MagicDeckPanel.TEXT[0]] | 历史 / ·仅供查看 |
| BOOK_VIEW_ONLY | 选牌期间可查看因子与魔具。关闭后继续选牌。 | 查看你拥有的 #y魔法因子 和 #y魔具 。 [UIs.json / FrierenMod:MagicDeckPanel.TEXT[1]] | 选牌期间可 / 与 / 关闭后继续选牌 |
| MAGIC_ITEM_ADDED | 已将{Card}加入魔法牌组。 | 选择2张魔具加入魔法牌组 [UIs.json / FrierenMod:BuyPropOption.TEXT[1]] | 已将{Card} |
| PUZZLE_WRONG | 这好像不太对… | 这好像不太对... [UIs.json / FrierenMod:Slot2.TEXT_DICT.7001] | … |
| TUTORIAL_TITLE | 芙莉莲的魔法书 | 芙莉莲 [characters.json / FrierenMod:Frieren.NAMES[0]] | 的魔法书 |
| TUTORIAL_PAGE | {Current} / {Total} | 未找到近似原文 | 整段新增或需人工对照 |
| TUTORIAL_SKIP | 跳过教程 | 教程 [UIs.json / FrierenMod:FTUE.EXTRA_TEXT[2]] | 跳过 |
| TUTORIAL_DONE | 开始冒险 | 新的开始 [events.json / FrierenMod:ReadingGrimoireEvent.NAME] | 冒险 |
| TUTORIAL_HELP | 查看教程 | 教程 [UIs.json / FrierenMod:FTUE.EXTRA_TEXT[2]] | 查看 |
| TUTORIAL_BODY_1 | 魔力是咏唱消耗的卡牌资源，与能量分别计算。<br><br>圣徽在你的每回合开始时，向抽牌堆和弃牌堆各放入1张魔力。<br><br>左侧的魔力计数依次对应抽牌堆、手牌、弃牌堆，三处分别计算。 | 每个回合开始时，在抽牌堆和弃牌堆各放入 #b1 张 #y魔力 。 [relics.json / FrierenMod:HolyEmblem.DESCRIPTIONS[0]] | 魔力是咏唱消耗的卡牌资源与能量分别计算圣徽在你的 / 向 / 左侧的魔力计数依次对应抽牌堆手牌弃牌堆三处分别计算 |
| TUTORIAL_BODY_2 | 打出带咏唱效果的牌后，选择一个可用槽位。<br><br>只消耗该槽位对应牌堆中的魔力，然后触发魔法因子的效果。<br><br>魔力不足的因子仍会显示，但无法选择。 | 未找到近似原文 | 整段新增或需人工对照 |
| TUTORIAL_BODY_3 | 点击右上角的紫色书本，查看你拥有的魔法因子和魔具。<br><br>右键启用魔具，再按提示左键选择目标；选满后生效。再次右键魔具或按 Esc 取消。<br><br>战后可能获得新的因子和魔具。需要帮助时，可在魔法书中点击“教程”重看。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-READING_GRIMOIRE_EVENT.pages.INITIAL.description | “哇，好高一座塔！”<br><br>在你为又能探索新迷宫而喜悦之时，你突然发现旅行箱中的三本魔导书出现了[shake]异样[/shake]。可能是一边的大鲸鱼导致的…… |  “哇，好高一座塔！”，在你为又能探索新迷宫而喜悦之时，你突然发现旅行箱中的三本魔导书出现了 @异样@ 。可能是一边的大鲸鱼导致的... [events.json / FrierenMod:ReadingGrimoireEvent.DESCRIPTIONS[1]] | …… |
| FRIERENMOD-READING_GRIMOIRE_EVENT.pages.INITIAL.options.INSPECT.title | 检查魔导书 | 检查第二本... [events.json / FrierenMod:ReadingGrimoireEvent.OPTIONS[1]] | 魔导书 |
| FRIERENMOD-READING_GRIMOIRE_EVENT.pages.INITIAL.options.INSPECT.description | 依次为三个槽位选择初始[gold]魔法因子[/gold]。 | [检查魔导书] 依次选择初始 #y槽位 中的 #y魔法因子 。 [events.json / FrierenMod:ReadingGrimoireEvent.OPTIONS[0]] | 为三个槽位 |
| FRIERENMOD-READING_GRIMOIRE_EVENT.pages.BOOK_0.description | “哇，好高一座塔！”<br><br>在你为又能探索新迷宫而喜悦之时，你突然发现旅行箱中的三本魔导书出现了[shake]异样[/shake]。可能是一边的大鲸鱼导致的…… |  “哇，好高一座塔！”，在你为又能探索新迷宫而喜悦之时，你突然发现旅行箱中的三本魔导书出现了 @异样@ 。可能是一边的大鲸鱼导致的... [events.json / FrierenMod:ReadingGrimoireEvent.DESCRIPTIONS[1]] | …… |
| FRIERENMOD-READING_GRIMOIRE_EVENT.pages.BOOK_0.options.INSPECT.title | 检查第一本魔导书 | 检查第二本... [events.json / FrierenMod:ReadingGrimoireEvent.OPTIONS[1]] | 一 / 魔导书 |
| FRIERENMOD-READING_GRIMOIRE_EVENT.pages.BOOK_0.options.INSPECT.description | 选择一张初始[gold]魔法因子[/gold]，放入[gold]抽牌槽[/gold]。 |  选择一张放入抽牌槽  [events.json / FrierenMod:ReadingGrimoireEvent.DESCRIPTIONS[4]] | 初始魔法因子 |
| FRIERENMOD-READING_GRIMOIRE_EVENT.pages.BOOK_1.description | 你感到体内流动的[gold]魔力[/gold]正在[wave]发生变化[/wave]……<br><br>抽牌槽的因子已经装载。还有两本魔导书没有检查。 |  你感到体内流动的魔力正在 ~发生变化~ ... [events.json / FrierenMod:ReadingGrimoireEvent.DESCRIPTIONS[2]] | ……抽牌槽的因子已经装载还有两本魔导书没有检查 |
| FRIERENMOD-READING_GRIMOIRE_EVENT.pages.BOOK_1.options.INSPECT.title | 检查第二本…… | 检查第二本... [events.json / FrierenMod:ReadingGrimoireEvent.OPTIONS[1]] | …… |
| FRIERENMOD-READING_GRIMOIRE_EVENT.pages.BOOK_1.options.INSPECT.description | 选择一张初始[gold]魔法因子[/gold]，放入[gold]手牌槽[/gold]。 |  选择一张放入手牌槽  [events.json / FrierenMod:ReadingGrimoireEvent.DESCRIPTIONS[5]] | 初始魔法因子 |
| FRIERENMOD-READING_GRIMOIRE_EVENT.pages.BOOK_2.description | 你感到体内流动的[gold]魔力[/gold]正在[wave]发生变化[/wave]……<br><br>手牌槽的因子也已装载。现在，只剩下最后一本。 |  你感到体内流动的魔力正在 ~发生变化~ ... [events.json / FrierenMod:ReadingGrimoireEvent.DESCRIPTIONS[2]] | ……手牌槽的因子也已装载现在只剩下最后一本 |
| FRIERENMOD-READING_GRIMOIRE_EVENT.pages.BOOK_2.options.INSPECT.title | 检查最后一本…… | 检查最后一本... [events.json / FrierenMod:ReadingGrimoireEvent.OPTIONS[2]] | …… |
| FRIERENMOD-READING_GRIMOIRE_EVENT.pages.BOOK_2.options.INSPECT.description | 选择一张初始[gold]魔法因子[/gold]，放入[gold]弃牌槽[/gold]。 |  选择一张放入弃牌槽  [events.json / FrierenMod:ReadingGrimoireEvent.DESCRIPTIONS[6]] | 初始魔法因子 |
| FRIERENMOD-READING_GRIMOIRE_EVENT.pages.TOOLS.description | 查看魔导书后，你的部分[gold]魔力[/gold]似乎[wave]具象化[/wave]了。<br><br>三本魔导书都检查完了，旅行箱里还有一些魔法道具。 |  查看魔导书后，你的部分 #y魔力 似乎 ~具象化~ 了。你跃跃欲试地走向尖塔。 [events.json / FrierenMod:ReadingGrimoireEvent.DESCRIPTIONS[3]] | 三本魔导书都检查完了旅行箱里还有一些魔法道具 |
| FRIERENMOD-READING_GRIMOIRE_EVENT.pages.TOOLS.options.OBTAIN.title | 还有一些魔法道具？ | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-READING_GRIMOIRE_EVENT.pages.TOOLS.options.OBTAIN.description | 获得[gold]两个神奇妙妙工具[/gold]。 | [还有一些魔法道具？] 获得2张 #y魔具 。 [events.json / FrierenMod:ReadingGrimoireEvent.OPTIONS[3]] | 两个神奇妙妙工 |
| FRIERENMOD-READING_GRIMOIRE_EVENT.pages.DONE.description | 你收好了三本魔导书和[gold]两个神奇妙妙工具[/gold]，跃跃欲试地走向尖塔。 |  查看魔导书后，你的部分 #y魔力 似乎 ~具象化~ 了。你跃跃欲试地走向尖塔。 [events.json / FrierenMod:ReadingGrimoireEvent.DESCRIPTIONS[3]] | 你收好了三本 / 和两个神奇妙妙工 |
| FRIERENMOD-READING_GRIMOIRE_EVENT.pages.DONE.options.DEPART.description | 继续前往大鲸鱼的祝福。 | 未找到近似原文 | 整段新增或需人工对照 |
| BACK.0009.description | 被猎人杀手击败。 | 第一次被击败。 [UIs.json / FrierenMod:Slot.TEXT_DICT.0008] | 猎人杀手 |
| BACK.1001.description | 芙莉莲在进阶0或以上通关时，有概率解锁。 | A0以上碎心有几率解锁。 [UIs.json / FrierenMod:Slot.TEXT_DICT.1] | 芙莉莲在进阶 / 或 / 通关时 / 概 |
| BACK.1002.description | 芙莉莲在进阶0或以上通关时，有概率解锁。 | A0以上碎心有几率解锁。 [UIs.json / FrierenMod:Slot.TEXT_DICT.1] | 芙莉莲在进阶 / 或 / 通关时 / 概 |
| BACK.1003.description | 芙莉莲在进阶0或以上通关时，有概率解锁。 | A0以上碎心有几率解锁。 [UIs.json / FrierenMod:Slot.TEXT_DICT.1] | 芙莉莲在进阶 / 或 / 通关时 / 概 |
| BACK.1004.description | 芙莉莲在进阶0或以上通关时，有概率解锁。 | A0以上碎心有几率解锁。 [UIs.json / FrierenMod:Slot.TEXT_DICT.1] | 芙莉莲在进阶 / 或 / 通关时 / 概 |
| BACK.1005.description | 芙莉莲在进阶0或以上通关时，有概率解锁。 | A0以上碎心有几率解锁。 [UIs.json / FrierenMod:Slot.TEXT_DICT.1] | 芙莉莲在进阶 / 或 / 通关时 / 概 |
| BACK.1006.description | 芙莉莲在进阶0或以上通关时，有概率解锁。 | A0以上碎心有几率解锁。 [UIs.json / FrierenMod:Slot.TEXT_DICT.1] | 芙莉莲在进阶 / 或 / 通关时 / 概 |
| BACK.1007.description | 芙莉莲在进阶0或以上通关时，有概率解锁。 | A0以上碎心有几率解锁。 [UIs.json / FrierenMod:Slot.TEXT_DICT.1] | 芙莉莲在进阶 / 或 / 通关时 / 概 |
| BACK.1008.description | 芙莉莲在进阶0或以上通关时，有概率解锁。 | A0以上碎心有几率解锁。 [UIs.json / FrierenMod:Slot.TEXT_DICT.1] | 芙莉莲在进阶 / 或 / 通关时 / 概 |
| BACK.1009.description | 芙莉莲在进阶0或以上通关时，有概率解锁。 | A0以上碎心有几率解锁。 [UIs.json / FrierenMod:Slot.TEXT_DICT.1] | 芙莉莲在进阶 / 或 / 通关时 / 概 |
| BACK.1010.description | 芙莉莲在进阶0或以上通关时，有概率解锁。 | A0以上碎心有几率解锁。 [UIs.json / FrierenMod:Slot.TEXT_DICT.1] | 芙莉莲在进阶 / 或 / 通关时 / 概 |
| BACK.1011.description | 芙莉莲在进阶0或以上通关时，有概率解锁。 | A0以上碎心有几率解锁。 [UIs.json / FrierenMod:Slot.TEXT_DICT.1] | 芙莉莲在进阶 / 或 / 通关时 / 概 |
| BACK.1012.description | 芙莉莲在进阶0或以上通关时，有概率解锁。 | A0以上碎心有几率解锁。 [UIs.json / FrierenMod:Slot.TEXT_DICT.1] | 芙莉莲在进阶 / 或 / 通关时 / 概 |
| BACK.1013.description | 芙莉莲在进阶0或以上通关时，有概率解锁。 | A0以上碎心有几率解锁。 [UIs.json / FrierenMod:Slot.TEXT_DICT.1] | 芙莉莲在进阶 / 或 / 通关时 / 概 |
| BACK.1014.description | 芙莉莲在进阶0或以上通关时，有概率解锁。 | A0以上碎心有几率解锁。 [UIs.json / FrierenMod:Slot.TEXT_DICT.1] | 芙莉莲在进阶 / 或 / 通关时 / 概 |
| BACK.1015.description | 芙莉莲在进阶0或以上通关时，有概率解锁。 | A0以上碎心有几率解锁。 [UIs.json / FrierenMod:Slot.TEXT_DICT.1] | 芙莉莲在进阶 / 或 / 通关时 / 概 |
| BACK.1016.description | 芙莉莲在进阶0或以上通关时，有概率解锁。 | A0以上碎心有几率解锁。 [UIs.json / FrierenMod:Slot.TEXT_DICT.1] | 芙莉莲在进阶 / 或 / 通关时 / 概 |
| BACK.1017.description | 芙莉莲在进阶0或以上通关时，有概率解锁。 | A0以上碎心有几率解锁。 [UIs.json / FrierenMod:Slot.TEXT_DICT.1] | 芙莉莲在进阶 / 或 / 通关时 / 概 |
| BACK.1018.description | 芙莉莲在进阶0或以上通关时，有概率解锁。 | A0以上碎心有几率解锁。 [UIs.json / FrierenMod:Slot.TEXT_DICT.1] | 芙莉莲在进阶 / 或 / 通关时 / 概 |
| BACK.2001.description | 芙莉莲在进阶5或以上通关时，有概率解锁。 | A0以上碎心有几率解锁。 [UIs.json / FrierenMod:Slot.TEXT_DICT.1] | 芙莉莲在进阶5或 / 通关时 / 概 |
| BACK.2002.description | 芙莉莲在进阶5或以上通关时，有概率解锁。 | A0以上碎心有几率解锁。 [UIs.json / FrierenMod:Slot.TEXT_DICT.1] | 芙莉莲在进阶5或 / 通关时 / 概 |
| BACK.2003.description | 芙莉莲在进阶5或以上通关时，有概率解锁。 | A0以上碎心有几率解锁。 [UIs.json / FrierenMod:Slot.TEXT_DICT.1] | 芙莉莲在进阶5或 / 通关时 / 概 |
| BACK.2004.description | 芙莉莲在进阶5或以上通关时，有概率解锁。 | A0以上碎心有几率解锁。 [UIs.json / FrierenMod:Slot.TEXT_DICT.1] | 芙莉莲在进阶5或 / 通关时 / 概 |
| BACK.2005.description | 芙莉莲在进阶5或以上通关时，有概率解锁。 | A0以上碎心有几率解锁。 [UIs.json / FrierenMod:Slot.TEXT_DICT.1] | 芙莉莲在进阶5或 / 通关时 / 概 |
| BACK.2006.description | 芙莉莲在进阶5或以上通关时，有概率解锁。 | A0以上碎心有几率解锁。 [UIs.json / FrierenMod:Slot.TEXT_DICT.1] | 芙莉莲在进阶5或 / 通关时 / 概 |
| BACK.2007.description | 芙莉莲在进阶5或以上通关时，有概率解锁。 | A0以上碎心有几率解锁。 [UIs.json / FrierenMod:Slot.TEXT_DICT.1] | 芙莉莲在进阶5或 / 通关时 / 概 |
| BACK.2008.description | 芙莉莲在进阶5或以上通关时，有概率解锁。 | A0以上碎心有几率解锁。 [UIs.json / FrierenMod:Slot.TEXT_DICT.1] | 芙莉莲在进阶5或 / 通关时 / 概 |
| BACK.2009.description | 芙莉莲在进阶5或以上通关时，有概率解锁。 | A0以上碎心有几率解锁。 [UIs.json / FrierenMod:Slot.TEXT_DICT.1] | 芙莉莲在进阶5或 / 通关时 / 概 |
| BACK.2010.description | 芙莉莲在进阶5或以上通关时，有概率解锁。 | A0以上碎心有几率解锁。 [UIs.json / FrierenMod:Slot.TEXT_DICT.1] | 芙莉莲在进阶5或 / 通关时 / 概 |
| BACK.2011.description | 芙莉莲在进阶5或以上通关时，有概率解锁。 | A0以上碎心有几率解锁。 [UIs.json / FrierenMod:Slot.TEXT_DICT.1] | 芙莉莲在进阶5或 / 通关时 / 概 |
| BACK.2012.description | 芙莉莲在进阶5或以上通关时，有概率解锁。 | A0以上碎心有几率解锁。 [UIs.json / FrierenMod:Slot.TEXT_DICT.1] | 芙莉莲在进阶5或 / 通关时 / 概 |
| BACK.2013.description | 芙莉莲在进阶5或以上通关时，有概率解锁。 | A0以上碎心有几率解锁。 [UIs.json / FrierenMod:Slot.TEXT_DICT.1] | 芙莉莲在进阶5或 / 通关时 / 概 |
| BACK.2014.description | 芙莉莲在进阶5或以上通关时，有概率解锁。 | A0以上碎心有几率解锁。 [UIs.json / FrierenMod:Slot.TEXT_DICT.1] | 芙莉莲在进阶5或 / 通关时 / 概 |
| BACK.2015.description | 芙莉莲在进阶5或以上通关时，有概率解锁。 | A0以上碎心有几率解锁。 [UIs.json / FrierenMod:Slot.TEXT_DICT.1] | 芙莉莲在进阶5或 / 通关时 / 概 |
| BACK.2016.description | 芙莉莲在进阶5或以上通关时，有概率解锁。 | A0以上碎心有几率解锁。 [UIs.json / FrierenMod:Slot.TEXT_DICT.1] | 芙莉莲在进阶5或 / 通关时 / 概 |
| BACK.2017.description | 芙莉莲在进阶5或以上通关时，有概率解锁。 | A0以上碎心有几率解锁。 [UIs.json / FrierenMod:Slot.TEXT_DICT.1] | 芙莉莲在进阶5或 / 通关时 / 概 |
| BACK.2018.description | 芙莉莲在进阶5或以上通关时，有概率解锁。 | A0以上碎心有几率解锁。 [UIs.json / FrierenMod:Slot.TEXT_DICT.1] | 芙莉莲在进阶5或 / 通关时 / 概 |
| BACK.3001.description | 芙莉莲在进阶10通关时，有概率解锁。 | A10以上碎心有几率解锁。 [UIs.json / FrierenMod:Slot.TEXT_DICT.2] | 芙莉莲在进阶 / 通关时 / 概 |
| BACK.3002.description | 芙莉莲在进阶10通关时，有概率解锁。 | A10以上碎心有几率解锁。 [UIs.json / FrierenMod:Slot.TEXT_DICT.2] | 芙莉莲在进阶 / 通关时 / 概 |
| BACK.3003.description | 芙莉莲在进阶10通关时，有概率解锁。 | A10以上碎心有几率解锁。 [UIs.json / FrierenMod:Slot.TEXT_DICT.2] | 芙莉莲在进阶 / 通关时 / 概 |
| BACK.3004.description | 芙莉莲在进阶10通关时，有概率解锁。 | A10以上碎心有几率解锁。 [UIs.json / FrierenMod:Slot.TEXT_DICT.2] | 芙莉莲在进阶 / 通关时 / 概 |
| BACK.3005.description | 芙莉莲在进阶10通关时，有概率解锁。 | A10以上碎心有几率解锁。 [UIs.json / FrierenMod:Slot.TEXT_DICT.2] | 芙莉莲在进阶 / 通关时 / 概 |
| BACK.3006.description | 芙莉莲在进阶10通关时，有概率解锁。 | A10以上碎心有几率解锁。 [UIs.json / FrierenMod:Slot.TEXT_DICT.2] | 芙莉莲在进阶 / 通关时 / 概 |
| BACK.3007.description | 芙莉莲在进阶10通关时，有概率解锁。 | A10以上碎心有几率解锁。 [UIs.json / FrierenMod:Slot.TEXT_DICT.2] | 芙莉莲在进阶 / 通关时 / 概 |
| BACK.3008.description | 芙莉莲在进阶10通关时，有概率解锁。 | A10以上碎心有几率解锁。 [UIs.json / FrierenMod:Slot.TEXT_DICT.2] | 芙莉莲在进阶 / 通关时 / 概 |
| BACK.3009.description | 芙莉莲在进阶10通关时，有概率解锁。 | A10以上碎心有几率解锁。 [UIs.json / FrierenMod:Slot.TEXT_DICT.2] | 芙莉莲在进阶 / 通关时 / 概 |
| BACK.3010.description | 芙莉莲在进阶10通关时，有概率解锁。 | A10以上碎心有几率解锁。 [UIs.json / FrierenMod:Slot.TEXT_DICT.2] | 芙莉莲在进阶 / 通关时 / 概 |
| BACK.3011.description | 芙莉莲在进阶10通关时，有概率解锁。 | A10以上碎心有几率解锁。 [UIs.json / FrierenMod:Slot.TEXT_DICT.2] | 芙莉莲在进阶 / 通关时 / 概 |
| BACK.3012.description | 芙莉莲在进阶10通关时，有概率解锁。 | A10以上碎心有几率解锁。 [UIs.json / FrierenMod:Slot.TEXT_DICT.2] | 芙莉莲在进阶 / 通关时 / 概 |
| BACK.3013.description | 芙莉莲在进阶10通关时，有概率解锁。 | A10以上碎心有几率解锁。 [UIs.json / FrierenMod:Slot.TEXT_DICT.2] | 芙莉莲在进阶 / 通关时 / 概 |
| BACK.3014.description | 芙莉莲在进阶10通关时，有概率解锁。 | A10以上碎心有几率解锁。 [UIs.json / FrierenMod:Slot.TEXT_DICT.2] | 芙莉莲在进阶 / 通关时 / 概 |
| BACK.3015.description | 芙莉莲在进阶10通关时，有概率解锁。 | A10以上碎心有几率解锁。 [UIs.json / FrierenMod:Slot.TEXT_DICT.2] | 芙莉莲在进阶 / 通关时 / 概 |
| BACK.3016.description | 芙莉莲在进阶10通关时，有概率解锁。 | A10以上碎心有几率解锁。 [UIs.json / FrierenMod:Slot.TEXT_DICT.2] | 芙莉莲在进阶 / 通关时 / 概 |
| BACK.3017.description | 芙莉莲在进阶10通关时，有概率解锁。 | A10以上碎心有几率解锁。 [UIs.json / FrierenMod:Slot.TEXT_DICT.2] | 芙莉莲在进阶 / 通关时 / 概 |
| BACK.3018.description | 芙莉莲在进阶10通关时，有概率解锁。 | A10以上碎心有几率解锁。 [UIs.json / FrierenMod:Slot.TEXT_DICT.2] | 芙莉莲在进阶 / 通关时 / 概 |
| BACK.4006.title | 我最讨厌debuff了！ | 我最讨厌邪咒了！ [UIs.json / FrierenMod:Slot2.TEXT_DICT.4006] | debuff |
| BACK.4006.description | 一次打出清洁铜像生锈的魔法，移除至少3种负面能力。 | 打出清洁铜像生锈的魔法后移除了邪咒。 [UIs.json / FrierenMod:Slot.TEXT_DICT.4006] | 一次 / 至少3种负面能力 |
| BACK.5001.description | 击败Boss阿乌拉。 | 自尽吧，阿乌拉 [UIs.json / FrierenMod:Slot2.TEXT_DICT.5001] | 击败Boss |
| BACK.5007.description | 在宝箱怪战斗中触发吃人，获得好黑啊。 | 未找到近似原文 | 整段新增或需人工对照 |
| BACK.5010.description | 通关时，三个槽位都是稀有因子。 | 胜利时，槽位中都是罕见因子。 [UIs.json / FrierenMod:Slot.TEXT_DICT.5010] | 通关 / 三个 / 稀有 |
| BACK.5012.description | 通关时，本局游戏时间超过1小时。 | 胜利时，游戏时间超过2小时。 [UIs.json / FrierenMod:Slot.TEXT_DICT.5012] | 通关 / 本局 / 1 |
| BACK.6001.description | 本场战斗中未受到任何生命伤害并击败宝箱怪。 | 未找到近似原文 | 整段新增或需人工对照 |
| BACK.7001.description | 三个谜题因子装在正确槽位后，同一回合按弃牌堆、手牌、抽牌堆的顺序咏唱。 | 未找到近似原文 | 整段新增或需人工对照 |
| BACK.7002.description | 按二代的槽位和顺序要求，成功完成三段谜题。 | 未找到近似原文 | 整段新增或需人工对照 |
| SNUGGLE_DISCARD | 弃掉至多{MaxCount}张牌；每张魔力获得1点能量。 | 丢弃最多 !M! 张 *魔力 获得等量 [E] 。 [cards.json / FrierenMod:Snuggle1.DESCRIPTION] | 掉至 / { / axCount}张牌每 / 1点能 |
| BACK_LIBRARY | 成就与卡背 | 当前卡槽背景： [UIs.json / FrierenMod:SlotBgPreview.TEXT[0]] | 成就与 |
| BACK_SEARCH | 搜索 ID、名称或解锁条件 | 未找到近似原文 | 整段新增或需人工对照 |
| BACK_OWNED_ONLY | 只看已解锁 |  NL #g已解锁！  [UIs.json / FrierenMod:Slot.TEXT[4]] | 只看 |
| BACK_PROGRESS | 收藏进度 {Count} / {Total} | 未找到近似原文 | 整段新增或需人工对照 |
| BACK_CHOOSE_SLOT | 选择要更换背景的槽位 | 未找到近似原文 | 整段新增或需人工对照 |
| AURA.PLAYER | 玩家魔力 | 魔力 [cards.json / FrierenMod:Mana.NAME] | 玩家 |
| AURA.COUNTDOWN | 服从天平 · 第{Turn}回合 | 服从的天平 [relics.json / FrierenMod:ScaleOfObedience.NAME] | ·第{Turn}回合 |
| AURA.JUDGMENT | 服从的天平 · 判定 | 服从的天平 [relics.json / FrierenMod:ScaleOfObedience.NAME] | ·判定 |
| WAYGATE_EXTRA_TURN | 获得额外回合！ |  NL 获得  [UIs.json / FrierenMod:GainRandomCardMod.TEXT[0]] | 额外回合 |
## ancients.json（18 条）

| ID | 二代当前全文 | 一代对照原文 | 归一化新增/替换片段 |
|---|---|---|---|
| THE_ARCHITECT.talk.FRIERENMOD-FRIEREN.0-0.ancient | 你想从这座塔里得到什么？ | 你干了什么！ [UIs.json / FrierenMod:Slot2.TEXT_DICT.EXCEPTION] | 想从这座塔里得到 |
| THE_ARCHITECT.talk.FRIERENMOD-FRIEREN.0-0.next | 回应 | [免费小吃] #g回复  #g [events.json / FrierenMod:FoodEvent.OPTIONS[0]] | 应 |
| THE_ARCHITECT.talk.FRIERENMOD-FRIEREN.0-1.char | 没见过的魔法。还有离开这里的方法。 | 未找到近似原文 | 整段新增或需人工对照 |
| THE_ARCHITECT.talk.FRIERENMOD-FRIEREN.0-1.next | 继续 | 永续 [cards.json / FrierenMod:Perpetual.NAME] | 继 |
| THE_ARCHITECT.talk.FRIERENMOD-FRIEREN.0-2.ancient | 那你还得再找找。 | 未找到近似原文 | 整段新增或需人工对照 |
| THE_ARCHITECT.talk.FRIERENMOD-FRIEREN.0-endattack | Both | 未找到近似原文 | 整段新增或需人工对照 |
| THE_ARCHITECT.talk.FRIERENMOD-FRIEREN.1-0r.ancient | 又是你。还没学会放弃吗？ | 未找到近似原文 | 整段新增或需人工对照 |
| THE_ARCHITECT.talk.FRIERENMOD-FRIEREN.1-0r.next | 回应 | [免费小吃] #g回复  #g [events.json / FrierenMod:FoodEvent.OPTIONS[0]] | 应 |
| THE_ARCHITECT.talk.FRIERENMOD-FRIEREN.1-1r.char | 上次的魔法，我还没弄明白。 | 干练的魔法使 [cards.json / FrierenMod:SkilledMage.NAME] | 上次 / 我还没弄明白 |
| THE_ARCHITECT.talk.FRIERENMOD-FRIEREN.1-1r.next | 继续 | 永续 [cards.json / FrierenMod:Perpetual.NAME] | 继 |
| THE_ARCHITECT.talk.FRIERENMOD-FRIEREN.1-2r.ancient | 我没有义务教你。 | 未找到近似原文 | 整段新增或需人工对照 |
| THE_ARCHITECT.talk.FRIERENMOD-FRIEREN.1-endattack | Both | 未找到近似原文 | 整段新增或需人工对照 |
| THE_ARCHITECT.talk.FRIERENMOD-FRIEREN.2-0r.char | 原来如此。你的魔法，我稍微看懂了一点。 | 未找到近似原文 | 整段新增或需人工对照 |
| THE_ARCHITECT.talk.FRIERENMOD-FRIEREN.2-0r.next | 继续 | 永续 [cards.json / FrierenMod:Perpetual.NAME] | 继 |
| THE_ARCHITECT.talk.FRIERENMOD-FRIEREN.2-1r.ancient | 看懂了又如何？ | 未找到近似原文 | 整段新增或需人工对照 |
| THE_ARCHITECT.talk.FRIERENMOD-FRIEREN.2-1r.next | 回应 | [免费小吃] #g回复  #g [events.json / FrierenMod:FoodEvent.OPTIONS[0]] | 应 |
| THE_ARCHITECT.talk.FRIERENMOD-FRIEREN.2-2r.char | 至少，下一次不会白来。 | 未找到近似原文 | 整段新增或需人工对照 |
| THE_ARCHITECT.talk.FRIERENMOD-FRIEREN.2-endattack | Both | 未找到近似原文 | 整段新增或需人工对照 |
## cards.json（183 条）

| ID | 二代当前全文 | 一代对照原文 | 归一化新增/替换片段 |
|---|---|---|---|
| FRIERENMOD-STRIKE_FRIEREN.description | 造成 {Damage:diff()} 点伤害。 | 造成 !D! 点伤害。 [cards.json / FrierenMod:Strike_Frieren.DESCRIPTION] | { / amagediff} |
| FRIERENMOD-DEFEND_FRIEREN.description | 获得 {Block:diff()} 点格挡。 | 获得 !B! 点 格挡 。 [cards.json / FrierenMod:Defend_Frieren.DESCRIPTION] | { / lockdiff} |
| FRIERENMOD-FLOW.description | 造成 {Damage:diff()} 点伤害。将抽牌堆和弃牌堆各一张[gold]魔力[/gold]移入手牌。 | 造成 !D! 点伤害。 NL 将抽牌堆和弃牌堆各一张 *魔力 放入手中。 [cards.json / FrierenMod:Flow.DESCRIPTION] | { / amagediff} / 移 / 牌 |
| FRIERENMOD-RAPID_CHANT.description | [gold]咏唱[/gold] 1。<br>将一张[gold]魔力[/gold]加入抽牌堆。 |  frierenmod:咏唱 !CX! 。 NL 将一张 *魔力 放入抽牌堆。 [cards.json / FrierenMod:RapidChant.DESCRIPTION] | 1 / 加 |
| FRIERENMOD-DEFENSIVE_MAGIC.description | [gold]咏唱[/gold] 1。获得 {Block:diff()} 点格挡。<br>将一张{Upgraded:cond:[gold]额外的防御魔法+[/gold]\|[gold]额外的防御魔法[/gold]}加入手牌。 |  frierenmod:咏唱 !CX! 。 NL 获得 !B! 点 格挡 。将另一张 *防御魔法+ 放入手牌。 [cards.json / FrierenMod:DefensiveMagic.UPGRADE_DESCRIPTION] | 1 / { / lockdiff} / {Upgradedcond额外的 / \|额外的防御魔法}加 |
| FRIERENMOD-SECOND_DEFENSIVE_MAGIC.title | 防御魔法·追加 | 防御魔法 [cards.json / FrierenMod:SecondDefensiveMagic.NAME] | ·追加 |
| FRIERENMOD-SECOND_DEFENSIVE_MAGIC.description | [gold]咏唱[/gold] 1。获得 {Block:diff()} 点格挡。 |  frierenmod:咏唱 !CX! 。 NL 获得 !B! 点格挡。 NL  消耗 。 [cards.json / FrierenMod:SecondDefensiveMagic.DESCRIPTION] | 1 / { / lockdiff} |
| FRIERENMOD-MANA.description | [gold]{Synchro}[/gold]。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-PRECISE_CHANT.description | 选择一个可用槽位，以其牌堆的[gold]魔力[/gold]计算最大[gold]咏唱[/gold]强度并发动因子效果。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-FINAL_CHANT.description | 尽可能[gold]咏唱[/gold]所有牌堆的所有[gold]魔力[/gold]。 | 尽可能 frierenmod:咏唱 所有牌堆的所有的 *魔力 。 [cards.json / FrierenMod:FinalChant.DESCRIPTION] | 整段新增或需人工对照 |
| FRIERENMOD-MANA_EXPANSION.description | {Upgraded:cond:将抽牌堆、手牌和弃牌堆\|将抽牌堆和弃牌堆}中的[gold]魔力[/gold]数量翻倍。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-DRAW_CHANT_CHOICE.title | 抽牌堆咏唱 | 从抽牌堆膨胀 [cards.json / FrierenMod:DoubleManaInDrawPile.NAME] | 咏唱 |
| FRIERENMOD-DRAW_CHANT_CHOICE.description | [gold]消耗[/gold]抽牌堆中的 {ManaCost} 张[gold]魔力[/gold]。{Effect} |  消耗 牌堆中2X张 *魔力 ，获得X点 [E] 。 [cards.json / FrierenMod:Strategizing.EXTENDED_DESCRIPTION[0]] | 抽 / 的{ManaCost} / {Effect} |
| FRIERENMOD-HAND_CHANT_CHOICE.title | 手牌咏唱 | 高速咏唱 [cards.json / FrierenMod:RapidChant.NAME] | 手牌 |
| FRIERENMOD-HAND_CHANT_CHOICE.description | [gold]消耗[/gold]手牌中的 {ManaCost} 张[gold]魔力[/gold]。{Effect} | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-DISCARD_CHANT_CHOICE.title | 弃牌堆咏唱 | 从弃牌堆膨胀 [cards.json / FrierenMod:DoubleManaInDiscardPile.NAME] | 咏唱 |
| FRIERENMOD-DISCARD_CHANT_CHOICE.description | [gold]消耗[/gold]弃牌堆中的 {ManaCost} 张[gold]魔力[/gold]。{Effect} | 在弃牌堆中抽 !M! 张 *魔力 。 [cards.json / FrierenMod:BackFlow.DESCRIPTION] | 消耗 / 的{ / anaCost} / {Effect} |
| FRIERENMOD-CHANT_EFFECT.AdvancedDefensiveMagic | 改良的防御魔法：获得 {Reward} 点基础格挡两次。 |  消耗 牌堆中X张 *魔力 ，获得X点 格挡 两次。 [cards.json / FrierenMod:AdvancedDefensiveMagic.EXTENDED_DESCRIPTION[0]] | 改良的防御 / 法 / {Reward} / 基础 |
| FRIERENMOD-CHANT_EFFECT.BuildUpBody | 强化肉体：获得 {Reward} 点力量。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-CHANT_EFFECT.HighSpeedMovement | 高速移动魔法：获得 {Reward} 点敏捷。 | 他山之石：高速移动魔法 [cards.json / FrierenMod:HighSpeedMovement.NAME] | 获得{Reward}点敏捷 |
| FRIERENMOD-CONTINUAL_CHANT.description | [gold]传说魔法[/gold]。<br>将 {Intensity:diff()} 张[gold]魔力[/gold]加入抽牌堆，然后[gold]咏唱[/gold] {Intensity:diff()}。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-FLOWER_FIELD_SPELL.description | [gold]咏唱[/gold] 2。将一张{Upgraded:cond:[gold]苍月草+[/gold]\|[gold]苍月草[/gold]}加入手牌。 |  frierenmod:咏唱 !CX! 。 NL 将1张 *苍月草+ 放入手牌。 [cards.json / FrierenMod:FlowerFieldSpell.UPGRADE_DESCRIPTION] | 2 / 一 / {Upgradedcond / \|苍月草}加 |
| FRIERENMOD-BLUE_MOON_WEED.description | 抽 {Cards:diff()} 张牌。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-MANA_BARRICADE.description | [gold]咏唱[/gold] 2。获得{Block:diff()}点格挡。若在抽牌堆咏唱，再获得{BonusBlock:diff()}点格挡。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-LURE_THE_ENEMY_IN_DEEP.description | [gold]咏唱[/gold] {Intensity:diff()}。获得 {Intensity:diff()}{energyPrefix:energyIcons(1)}。若在手牌[gold]咏唱[/gold]，将本牌移回手牌。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-PERFECT_DEFENSIVE_MAGIC.description | [gold]咏唱[/gold] 3。获得 {Block:diff()} 点格挡和 1 层缓冲。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-TRUE_COLOURS.description | [gold]咏唱[/gold] 4。抽 3 张牌，将 3 张[gold]魔力[/gold]加入手牌。获得 {energyPrefix:energyIcons(3)}，并向抽牌堆和弃牌堆各加入 3 张[gold]魔力[/gold]。 |  frierenmod:咏唱 !CX! 。 NL 获得 [E] [E] [E] ，抽3张牌，在抽牌堆、手牌、弃牌堆各放入3张 *魔力 。 NL  消耗 。 [cards.json / FrierenMod:TrueColours.DESCRIPTION] | 4 / 将3张魔力加入手牌获得{energyPrefixenergyIcons3}并向 / 和 / 加 |
| FRIERENMOD-FULL_AHEAD.description | 每回合第一次[gold]咏唱[/gold]后，获得 1 力量和 1 敏捷。 | 每回合第一次 frierenmod:咏唱 后，获得 1 力量 和 1 敏捷 。 [cards.json / FrierenMod:FullAhead.DESCRIPTION] | 整段新增或需人工对照 |
| FRIERENMOD-SIMMERING.description | 每次[gold]咏唱[/gold]时，从抽牌堆抽取 1 张[gold]传说魔法[/gold]。 |  frierenmod:咏唱 后，抽1张 frierenmod:传说魔法 。 [cards.json / FrierenMod:Simmering.DESCRIPTION] | 每次 / 时从 / 牌堆抽取 |
| FRIERENMOD-ENVY.description | 造成 {Damage:diff()} 点伤害。你每次[gold]咏唱[/gold]时，将本牌从弃牌堆移回手牌。 |  造成!D!点伤害。你每次 frierenmod:咏唱 时， 将这张牌从弃牌堆放回你的手牌。 [cards.json / FrierenMod:HalfTheSky.DESCRIPTION] | { / amagediff} / 本 / 移 |
| FRIERENMOD-CONFRONTATION.description | 获得{Block:diff()}点格挡。若本回合[gold]咏唱[/gold]过，额外获得{BonusBlock:diff()}点格挡。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-IMAGINATION.description | 本回合，[gold]咏唱[/gold]不[gold]消耗[/gold][gold]魔力[/gold]，无法生成[gold]魔力[/gold]。<br>卡牌改用[gold]魔力[/gold]支付费用。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-READY_STANCE.description | 造成{Damage:diff()}点伤害。从抽牌堆或弃牌堆抽取1张[gold]高速咏唱[/gold]（优先抽牌堆）。 | 造成 !D! 点伤害。 NL 将抽牌堆和弃牌堆各一张 *魔力 放入手中。 [cards.json / FrierenMod:Flow.DESCRIPTION] | { / amagediff} / 从 / 或 / 抽取1 / 高速咏唱优先抽牌堆 |
| FRIERENMOD-FOLK_GRIMOIRE.description | 将随机[gold]咏唱[/gold]牌与随机[gold]传说魔法[/gold]各 1 张加入手牌。本回合它们的费用减少 {Discount:diff()}。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-HELL_FIRE_SUMMONING.description | [gold]封印[/gold]。[gold]回火[/gold]。<br>造成 {BaseDamage:diff()} 点伤害，次数为本回合打出[gold]咏唱[/gold]牌数量的两倍。{InCombat:cond:<br>（造成 {Damage:diff()} 点伤害 {Hits} 次）\|} | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-FAMEHAMEHA.description | [gold]传说魔法[/gold]。<br>造成{Damage:diff()}点伤害。每次[gold]咏唱[/gold]时，使本牌本回合费用减少1。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-FAIRY_TALE_END.description | [gold]传说魔法[/gold]。<br>造成 {BaseDamage:diff()} 点伤害。<br>永久牌组中的每种[gold]传说魔法[/gold]使基础伤害翻倍。<br>（造成 {Damage:diff()} 点伤害） |  frierenmod:传说魔法 。 NL 造成 !D! 点伤害。牌组中每一种 *传说魔法 ，此牌的基础伤害就翻一倍。 NL 消耗 。 [cards.json / FrierenMod:FairyTaleEnd.DESCRIPTION] | {Base / amagediff} / 永久 / 的 / 使 / 造成{Damagediff}点伤害 |
| FRIERENMOD-HOT_TEA_SPELL.description | [gold]传说魔法[/gold]。<br>获得 {Block:diff()} 点格挡，再获得等于三牌堆[gold]魔力[/gold]总数的格挡。{InCombat:cond:<br>（获得{CalculatedBlock:diff()}点格挡）\|} | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-MOLD_SPELL.description | [gold]传说魔法[/gold]。<br>造成 {Damage:diff()} 点伤害 {Hits:diff()} 次。 |  frierenmod:传说魔法 。 NL 造成 !D! 点伤害 !M! 次。  [cards.json / FrierenMod:MoldSpell.DESCRIPTION] | { / amagediff} / {Hitsdiff} |
| FRIERENMOD-OIL_SPELL.description | [gold]传说魔法[/gold]。<br>对所有敌人造成{Damage:diff()}点伤害并给予{Vulnerable:diff()}层[gold]易伤[/gold]。若没有击杀，重复一次。 |  frierenmod:传说魔法 。 NL 对所有敌人造成 !D! 点伤害。 如果没有敌人死亡，则再造成 !M! 点。 [cards.json / FrierenMod:OilSpell.DESCRIPTION] | { / amagediff} / 并给予{Vulnerablediff}层易伤若 / 击杀重复一次 |
| FRIERENMOD-PAPER_PLANE_SPELL.description | [gold]传说魔法[/gold]。<br>本回合最高连续[gold]同调[/gold]次数{Upgraded:cond:大于或等于\|大于}本场战斗此前回合的最高记录时，你的攻击伤害翻倍。<br>（本回合：{Current}，此前最高：{Previous}） | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-RUST_CLEAN_MAGIC.description | [gold]传说魔法[/gold]。<br>移除你的所有减益，每移除一个获得{Gold:diff()}金币。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-LIGHTNING_MAGIC.description | [gold]传说魔法[/gold]。<br>将抽牌堆和弃牌堆的所有[gold]魔力[/gold]移入手牌。本场战斗，[gold]同调[/gold]变为[gold]终极同调[/gold]。 |  frierenmod:传说魔法 。 NL 将抽牌堆和弃牌堆中的 *魔力 放入手牌直到上限。本场战斗中 frierenmod:同调 变为 frierenmod:终极同调 。 [cards.json / FrierenMod:LightningMagic.DESCRIPTION] | 所有 / 移 |
| FRIERENMOD-LITTLE_FIRE.description | [gold]回火[/gold]。造成 {Damage:diff()} 点伤害。本场战斗中现有的小小火花基础伤害增加 2。[gold]同调[/gold]时从弃牌堆返回手牌。 |  frierenmod:回火 。造成 !D! 点伤害。在本场战斗中所有 *小小火花 伤害增加2。 frierenmod:同调 时，将这张牌从弃牌堆放回手牌。 [cards.json / FrierenMod:LittleFire.DESCRIPTION] | { / amagediff} / 现 / 的 / 基础 / 返 |
| FRIERENMOD-ADVANCED_DEFENSIVE_MAGIC.description | {Context}{Effect} | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-BE_PREPARED.description | {Context}{Effect} | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-BUILD_UP_BODY.description | {Context}{Effect} | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-GROSSED_OUT_LOOK.description | {Context}{Effect} | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-ICE_ARROWS.description | {Context}{Effect} | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-MELEE_MAGE.description | {Context}{Effect} | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-METAL_PETAL_SPELL.description | {Context}{Effect} | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-SLEEEEP.description | {Context}{Effect} | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-TRANSFER_TO_ALCHEMIST.description | {Context}{Effect} | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-FACTOR100.description | {Context}{Effect} | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-BARRIER_MAGIC.description | {Context}{Effect} | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-BIG_FIRE.description | {Context}{Effect} | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-BLOOD_SPELL.description | {Context}{Effect} | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-BLUE_PRINT.description | {Context}{Effect} | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-HIGH_SPEED_MOVEMENT.description | {Context}{Effect} | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-HYPNOSIS.description | {Context}{Effect} | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-NATURE_BEAUTY.description | {Context}{Effect} | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-PASS_ON_THE_TORCH.description | {Context}{Effect} | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-PINCH_AND_SCRAPE.description | {Context}{Effect} | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-SPELL_BOOST.description | {Context}{Effect} | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-FACTOR010.description | {Context}{Effect} | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-GREAT_MAGE_COMPOSURE.description | {Context}{Effect} | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-IMPECCABLE_INTANGIBLE.description | {Context}{Effect} | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-PEACEFUL_DAYS.description | {Context}{Effect} | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-STRATEGIZING.description | {Context}{Effect} | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-THE_LIVING_LEGEND.description | {Context}{Effect} | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-TRANSCENDENCE.description | {Context}{Effect} | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-FACTOR001.description | {Context}{Effect} | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-CRYSTAL_BALL.title | 魔法水晶球 | 魔法水晶球【蓝色品质】 [cards.json / FrierenMod:CrystalBall.NAME] | 整段新增或需人工对照 |
| FRIERENMOD-CRYSTAL_BALL.description | 选择两张已装载的因子，交换它们的槽位。 | 选择2张 frierenmod:[#ff4e41]已装载 的 *魔法因子 ，使它们 frierenmod:槽位 互换。 [cards.json / FrierenMod:CrystalBall.DESCRIPTION] | 两 / 交换 / 的 |
| FRIERENMOD-MYSTERY_BOX.description | 获得两张随机魔法因子（不含谜题因子），放入未装载库存。 | 将2张随机的 *魔法因子 放入 你的魔法牌组。 [cards.json / FrierenMod:MysteryBox.DESCRIPTION] | 获得两 / 不含谜题因子 / 未装载库存 |
| FRIERENMOD-MYSTIC_BOX.description | 选择三张未装载的因子，随机装入三个槽位；原有因子退回库存。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-SEIN_NOTE.description | 移除两张未装载的因子或其他魔具，回复 10 点生命。 | 移除2张 frierenmod:[#ff4e41]未装载 的 *魔法因子 或除自己之外的 *魔具 ，回复10点生命。 [cards.json / FrierenMod:SeinNote.DESCRIPTION] | 两 / 其他 |
| FRIERENMOD-SHADOW_DRAGON_HORN.description | 仅在战斗中使用。移除两张未装载的因子或其他魔具，获得 {energyPrefix:energyIcons(2)}。 | 仅战斗阶段生效：移除2张 frierenmod:[#ff4e41]未装载 的 *魔法因子 或除自己之外的 *魔具 ，获得 [E] [E] 。 [cards.json / FrierenMod:ShadowDragonHorn.DESCRIPTION] | 在 / 中使用 / 两 / 其他 / {energyPrefixenergyIcons2} |
| FRIERENMOD-TRANSMUTATION_CIRCLE.description | 将两张因子变为同稀有度的随机因子（不含谜题因子），[gold]保留[/gold]各自槽位。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-UNBELIEVABLE_TOOL.description | 选择一张已装载和一张未装载的因子，使它们交换槽位 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-UNBELIEVABLE_TOOL_BOX.description | 移除两张未装载的因子或其他魔具，获得两件神奇妙妙工具。 | 移除2张 frierenmod:[#ff4e41]未装载 的 *魔法因子 或除自己之外的 *魔具 ，获得2张 *神奇妙妙工具 。 [cards.json / FrierenMod:UnbelievableToolBox.DESCRIPTION] | 两 / 其他 / 两件 |
| FRIERENMOD-ACCESSORIES_SPELL.description | [gold]传说魔法[/gold]。战斗胜利时额外获得1个随机遗物奖励。 |  固有 。 frierenmod:传说魔法 。 NL 战斗结束时，获得一件随机遗物。 NL  虚无 。 [cards.json / FrierenMod:AccessoriesSpell.DESCRIPTION] | 胜利 / 额外 / 1个 / 奖励 |
| FRIERENMOD-AIR_SLASH.description | 造成{Damage:diff()}点伤害{Hits:diff()}次。下回合，[gold]同调[/gold]变为[gold]加速同调[/gold]。 | 造成 !D! 点伤害。 NL 本回合中， frierenmod:同调 变为 frierenmod:加速同调 ，但无法从卡牌中获取 *魔力 。 [cards.json / FrierenMod:AirSlash.DESCRIPTION] | { / amagediff} / {Hitsdiff}次下 |
| FRIERENMOD-APEX_MAGIC.description | 若抽牌堆、手牌和弃牌堆各恰好有4张[gold]魔力[/gold]，消灭所有敌人。 | 若你的手牌、抽牌堆、弃牌堆分别只有4张 *魔力 ，将所有敌人生命值变为0。 [cards.json / FrierenMod:ApexMagic.DESCRIPTION] | 手牌和 / 各恰好 / 消灭 |
| FRIERENMOD-APPREHEND.description | 造成{Damage:diff()}点伤害。抽{Cards:diff()}张牌，然后丢弃其中的[gold]魔力[/gold]。 | 造成 !D! 点伤害。 NL 抽 !M! 张牌。 NL 丢弃其中的 *魔力 。 [cards.json / FrierenMod:Apprehend.DESCRIPTION] | { / amagediff} / {Cardsdiff} / 然后 |
| FRIERENMOD-BABY_SLEEPING.description | 获得 {Block:diff()} 点格挡。[gold]保留[/gold]一张手牌，若弃牌堆没有[gold]魔力[/gold]，[gold]保留[/gold]所有手牌。 | 获得 !B! 点格挡。 保留 1张手牌。 NL 若弃牌堆没有 *魔力 ， 保留 所有手牌。 [cards.json / FrierenMod:BabySleeping.DESCRIPTION] | { / lockdiff} / 一 |
| FRIERENMOD-BACK_FLOW.description | 将弃牌堆的4张[gold]魔力[/gold]移入手牌。 | 在弃牌堆中抽 !M! 张 *魔力 。 [cards.json / FrierenMod:BackFlow.DESCRIPTION] | 将 / 的4 / 移入手牌 |
| FRIERENMOD-BREAKS_BARRIERS_SPELL.description | 整理抽牌堆：[gold]魔力[/gold]在最上方，其余牌按费用从低到高排列。抽{Cards:diff()}张牌。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-BUTT_SLAM.description | 造成等同于你当前格挡的伤害（{CalculatedDamage:diff()}点）。在弃牌堆生成2张[gold]魔力[/gold]。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-CLEAR_MIND.description | 丢弃至多{Amount:diff()}张手牌。在手牌生成{Amount:diff()}张[gold]魔力[/gold]。本回合[gold]同调[/gold]改为[gold]加速同调[/gold]。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-SUSTAINED_BARRAGE.description | 对所有敌人造成{Damage:diff()}点伤害3次。出牌时若抽牌堆有[gold]魔力[/gold]，[gold]消耗[/gold]本牌。 | 对所有敌人造成 !D! 点伤害3次。若抽牌堆有 *魔力 ， 消耗 此牌。 [cards.json / FrierenMod:ContinuousShooting.DESCRIPTION] | { / amagediff} / 出牌时 / 本 |
| FRIERENMOD-CUSTOM_LEGENDARY_SPELL.description | {Effects} | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-DARK.description | 抽{Cards:diff()}张牌。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-DISPERSE.description | 在抽牌堆、手牌、弃牌堆分别生成1、2、3张[gold]魔力[/gold]。 | 将1张 *魔力 放入抽牌堆 NL 将2张 *魔力 放入手牌。 NL 将3张 *魔力 放入弃牌堆。 [cards.json / FrierenMod:Disperse.UPGRADE_DESCRIPTION] | 在 / 弃牌堆分别生成12 |
| FRIERENMOD-ELEMENTS_BOMBING.description | [gold]消耗[/gold]所有手牌，每[gold]消耗[/gold]一张就随机生成一张{Upgraded:cond:升级的\|}元素牌加入手牌。 |  消耗 所有手牌。每 消耗 一张牌，在你的手牌中增加一张 升级 的随机 *元素 牌 。 NL 消耗 。 [cards.json / FrierenMod:ElementsBombing.UPGRADE_DESCRIPTION] | 就随机生成 / {Upgradedcond / \|} / 加入手牌 |
| FRIERENMOD-FIRE.description | 造成{Damage:diff()}点伤害两次。 | 造成 !D! 点伤害两次。 NL 消耗 。 NL 虚无 。 [cards.json / FrierenMod:Fire.DESCRIPTION] | { / amagediff} |
| FRIERENMOD-FIRE_SPELL.description | 当前每有一个敌人，将一张{Upgraded:cond:[gold]小小火花+[/gold]\|[gold]小小火花[/gold]}加入手牌。 | 当前每有一个敌人，将1张 *小小火花+ 放入你的手牌。 NL 消耗 。 [cards.json / FrierenMod:FireSpell.UPGRADE_DESCRIPTION] | 一 / {Upgradedcond / \|小小火花}加 |
| FRIERENMOD-FIVE_MINUTES_MORE.description | 获得{Energy:energyIcons()}。将一张懒懒散散{Upgraded:cond:加入手牌\|随机加入抽牌堆}。 | 获得 [E] [E] ，将一张 *懒懒散散 放入抽牌堆。 [cards.json / FrierenMod:FiveMinutesMore.DESCRIPTION] | {EnergyenergyIcons} / {Upgradedcond加入手牌\|随机加 / } |
| FRIERENMOD-FLYING_MAGIC.description | 获得3层无实体。每回合结束时，[gold]消耗[/gold]弃牌堆的{Amount:diff()}张[gold]魔力[/gold]。 | 获得3层 无实体 ，在每回合结束时 消耗 弃牌堆 !M! 张 *魔力 。 [cards.json / FrierenMod:FlyingMagic.DESCRIPTION] | 的{Amountdiff} |
| FRIERENMOD-VIOLENT_EXORCISM.description | 丢弃所有非[gold]魔力[/gold]手牌。每丢弃一张，获得{Block:diff()}点格挡并造成{Damage:diff()}点伤害。 | 丢弃所有非 *魔力 牌。 NL 每丢弃一张牌，造成 !D! 点伤害，获得 !B! 点 格挡 。 [cards.json / FrierenMod:Free.DESCRIPTION] | 手 / 获得{Blockdiff}点格挡并 / { / amagediff} |
| FRIERENMOD-FRIEREN_SUITCASE.description | 手牌上限改为13张。打出牌后若手牌已满，获得{Amount:diff()}点格挡。 | 本场战斗中手牌上限提高至13。 NL 使用卡牌后若手牌已满，获得 !M! 点格挡。 [cards.json / FrierenMod:FrierenSuitcase.DESCRIPTION] | 改为 / 张打出 / {Amountdiff} |
| FRIERENMOD-GRAND_CROSS.description | 造成{Damage:diff()}点伤害。若本回合打出过[gold]咏唱[/gold]牌，本牌费用变为0。 |  造成!D!点伤害。如果你本回合 frierenmod:咏唱 过，这张牌在本回合耗能变为0。 [cards.json / FrierenMod:GrandCross.DESCRIPTION] | { / amagediff} / 若 / 打出过 / 牌费用 |
| FRIERENMOD-GRAPE_SPELL.title | 让葡萄变酸的魔法 | 把甜葡萄变成酸葡萄的魔法 [cards.json / FrierenMod:GrapeSpell.NAME] | 让 |
| FRIERENMOD-GRAPE_SPELL.description | [gold]消耗[/gold]2张手牌。在手牌生成2张[gold]魔力[/gold]。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-GREEN_APPLE.description | 获得{Block:diff()}点格挡两次。 |  获得 !B! 点 格挡 2次。 NL 消耗 。 [cards.json / FrierenMod:GreenApple.DESCRIPTION] | { / lockdiff} / 两 |
| FRIERENMOD-HIGH_DENSITY_MANA.description | [gold]{Synchro}[/gold]。[gold]消耗[/gold]时获得{Amount:diff()}{energyPrefix:energyIcons(1)}。连续[gold]同调[/gold]3次后，若本牌在抽牌堆，将其移入手牌。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-ICE.description | 获得{Block:diff()}点格挡。 | 获得 !B! 点格挡。 NL 消耗 。 NL 虚无 。 [cards.json / FrierenMod:Ice.DESCRIPTION] | { / lockdiff} |
| FRIERENMOD-KISS.description | 给予99层易伤和99层虚弱。 | 给予99层 易伤 和99层 虚弱 。 NL  消耗 。 NL  虚无 。  [cards.json / FrierenMod:Kiss.DESCRIPTION] | 整段新增或需人工对照 |
| FRIERENMOD-LAZINESS.description | 回合结束时若仍在手牌，将一张懒懒散散加入弃牌堆。打出[gold]咏唱[/gold]牌时[gold]消耗[/gold]本牌。 |  不能被打出 。 NL  frierenmod:咏唱 一次 以 消耗 此牌 。 NL 在你的回合结束且在手牌中时，将一张复制放入弃牌堆。 [cards.json / FrierenMod:Laziness.DESCRIPTION] | 时若仍 / 懒懒散散加 / 打出咏唱牌时消耗本牌 |
| FRIERENMOD-LEGENDARY_CRAFT_CHOICE.title | 魔法构筑选项 | 防御魔法 [cards.json / FrierenMod:DefensiveMagic.NAME] | 构筑选项 |
| FRIERENMOD-LEGENDARY_CRAFT_CHOICE.description | {Effect} | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-LIGHT.description | 回复{Amount:diff()}点生命。 | 回复 !M! 点生命。 NL 消耗 。 NL 虚无 。 [cards.json / FrierenMod:Light.DESCRIPTION] | {Amountdiff} |
| FRIERENMOD-LOCK_TARGET.description | 造成{Damage:diff()}点伤害。抽1张牌。若抽牌堆[gold]魔力[/gold]不少于弃牌堆，重复一次。{InCombat:cond:<br>（造成{Damage:diff()}点伤害{CalculatedHits}次）\|} | 造成 !D! 点伤害，抽1张牌。若抽牌堆中 *魔力 不少于弃牌堆中的，再触发一次。 [cards.json / FrierenMod:LockTarget.DESCRIPTION] | { / amagediff} / 重复 / {InCombatcond造成{Damagediff}点伤害{CalculatedHits}次\|} |
| FRIERENMOD-MAGE_STYLE_FINISHER.description | 本回合每[gold]同调[/gold]一次，造成一次{Damage:diff()}点伤害。{InCombat:cond:<br>（造成{Damage:diff()}点伤害{CalculatedHits}次）\|} | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-MANA_COLLAPSE.description | 造成{Damage:diff()}点伤害。若这是本回合前10张打出的牌，在手牌生成2张[gold]魔力[/gold]，并将本牌洗入抽牌堆。 | 造成 !D! 点伤害。若本回合内打出小于10张牌，将 !M! 张 *魔力 放入手中，将此牌放入抽牌堆。 [cards.json / FrierenMod:ManaCollapse.DESCRIPTION] | { / amagediff} / 这是 / 前 / 打出的 / 在手牌生成2 / 并 / 本 / 洗 |
| FRIERENMOD-MANA_CONCEALMENT.description | 将抽牌堆的所有[gold]魔力[/gold]移入弃牌堆。 | 将抽牌堆中的 *魔力 全部放入弃牌堆。 NL 消耗 。 NL 虚无 。 [cards.json / FrierenMod:ManaConcealment.DESCRIPTION] | 所有 / 移 |
| FRIERENMOD-MANA_DETECTION.description | 造成{Damage:diff()}点伤害。此敌人每受到一次攻击，攻击者的弃牌堆生成1张[gold]魔力[/gold]。 | 造成 !D! 点伤害。 NL 每当目标敌人受到攻击时，将 !M! 张 *魔力 放入弃牌堆。 NL  消耗 。 [cards.json / FrierenMod:ManaDetection.DESCRIPTION] | { / amagediff} / 此 / 每 / 一次 / 攻击者的弃牌堆生成1 |
| FRIERENMOD-MANA_PARCEL.description | 抽到本牌时，[gold]消耗[/gold]自身，在手牌生成{Amount:diff()}张[gold]魔力[/gold]。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-MULTIPLE_OFFENSIVE_MAGIC.description | 随机造成X+4点伤害{Upgraded:cond:X+1\|X}次，将X张[gold]魔力[/gold]放入手牌。{InCombat:cond:<br>（造成{Damage:diff()}点伤害{Hits:diff()}次）\|} | 随机造成X+4点伤害X+1次，将X张 *魔力 放入手牌。 [cards.json / FrierenMod:MultipleOffensiveMagic.UPGRADE_DESCRIPTION] | {Upgradedcond / \|X} / {InCombatcond造成{Damagediff}点伤害{Hitsdiff}次\|} |
| FRIERENMOD-OPEN_THE_WAYGATE.description | 连续[gold]同调[/gold]{Amount:diff()}次后，在本回合结束后获得一个额外回合。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-ORDINARY_OFFENSIVE_MAGIC.description | 造成[gold]消耗[/gold]牌堆[gold]魔力[/gold]数量两倍的伤害。{InCombat:cond:<br>（造成{CalculatedDamage:diff()}点伤害）\|} | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-OUTPOURING.description | 本回合每次[gold]同调[/gold]，获得1点力量。 | 打出此牌后，你在本回合内每 frierenmod:同调 一张 *魔力，获得 !M! 点 力量 。 NL  消耗 。 [cards.json / FrierenMod:Outpouring.DESCRIPTION] | 次 / 1 |
| FRIERENMOD-PAJAMAS_FORM.description | 回合结束时[gold]消耗[/gold]你的所有[gold]魔力[/gold]，每张对所有敌人造成{DamagePerMana:diff()}点伤害。 | 在每回合结束时，消耗所有 *魔力 ，对所有敌人造成消耗的 *魔力 数量4倍的伤害。 [cards.json / FrierenMod:PajamasForm.DESCRIPTION] | 你的 / 每张 / {DamagePerManadiff}点 |
| FRIERENMOD-PANCAKE_SPELL.title | 顺利翻薄饼的魔法 | 翻薄饼时会很顺利的魔法 [cards.json / FrierenMod:PancakeSpell.NAME] | 顺利 |
| FRIERENMOD-PANCAKE_SPELL.description | 从抽牌堆中选择一张牌和你手牌中的 {Amount:diff()} 张[gold]魔力[/gold]交换。 | 从抽牌堆中选择一张牌和你手牌中的 !M! 张 *魔力 交换。 [cards.json / FrierenMod:PancakeSpell.DESCRIPTION] | {Amountdiff} |
| FRIERENMOD-RECALL.description | 选择[gold]消耗[/gold]牌堆的{Amount:diff()}张牌移入手牌，本回合[gold]保留[/gold]它们。 | 将消耗牌堆中的 !M! 张牌放入你的手牌，并将其 保留 。 NL 消耗 。 [cards.json / FrierenMod:Recall.DESCRIPTION] | 选择 / {Amountdiff} / 移 / 本回合 / 它们 |
| FRIERENMOD-RECYCLE.description | 每有一张从抽牌堆或手牌流向弃牌堆的[gold]魔力[/gold]，获得{RecyclePower:diff()}点格挡。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-RED_APPLE_SPELL.description | 本回合交换[gold]力量[/gold]和[gold]敏捷[/gold]。将一张{Upgraded:cond:[gold]青苹果+[/gold]\|[gold]青苹果[/gold]}加入手牌。 | 交换你的的 力量 和 敏捷 层数， 将一张 *青苹果+ 放入手牌。 NL 回合结束时，换回。 [cards.json / FrierenMod:RedAppleSpell.UPGRADE_DESCRIPTION] | 本回合 / {Upgradedcond / \|青苹果}加 |
| FRIERENMOD-RESTRAINT_MAGIC.description | 敌人本回合失去{StrengthLoss:diff()}点[gold]力量[/gold]，并获得[gold]迟缓[/gold]。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-RINGLET_FORM.description | 每[gold]消耗[/gold]一张[gold]魔力[/gold]，获得{Amount:diff()}点格挡。 | 你每 消耗 一张 *魔力 ，获得 !M! 点 格挡 。 [cards.json / FrierenMod:RingletForm.DESCRIPTION] | {Amountdiff} |
| FRIERENMOD-RITES_PREPARATION.description | 接下来的{Amount:diff()}次[gold]同调[/gold]，对所有敌人造成5点伤害。 | 下 !M! 次 frierenmod:同调 会对所有敌人造成5点伤害。 [cards.json / FrierenMod:RitesPreparation.DESCRIPTION] | 接 / 来的{A / unt / iff}次 |
| FRIERENMOD-SPEAK_FROM_HEART_SPELL.title | 说出心声的魔法 | 变出刨冰的魔法 [cards.json / FrierenMod:ShavedIceSpell.NAME] | 说 / 心声 |
| FRIERENMOD-SPEAK_FROM_HEART_SPELL.description | [gold]消耗[/gold]手牌中的所有[gold]魔力[/gold]。每[gold]消耗[/gold]一张就打出抽牌堆顶的 {Upgraded:cond:2\|1} 张牌。 |  消耗 手中所有的 *魔力 ，每 消耗 1张就打出你抽牌堆随机位置的 !M! 张牌。 [cards.json / FrierenMod:SayThinkingSpell.DESCRIPTION] | 牌 / 的 / 一 / 顶 / {Upgradedcond2\|1} |
| FRIERENMOD-SEE_THROUGH_PHANTOM.description | 造成{Damage:diff()}点伤害。敌人意图为攻击时给予{Amount:diff()}层虚弱，否则给予{Amount:diff()}层易伤。 | 造成 !D! 点伤害，若敌方意图为攻击则给予 !M! 层 虚弱 ，否则给予 !M! 层 易伤 。 [cards.json / FrierenMod:SeeThroughPhantom.DESCRIPTION] | { / amagediff} / 人 / 时 / {Amountdiff} / {Amountdiff} |
| FRIERENMOD-SERIE_GRIMOIRE.description | 选择费用，并从三组{Upgraded:cond:3\|2}个选项中各选择一种效果，创造一张[gold]传说魔法[/gold]加入手牌。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-SHAVED_ICE_SPELL.description | 将{Amount:diff()}张[gold]魔力[/gold]加入抽牌堆。 | 将 !M! 张 *魔力 放入抽牌堆。 [cards.json / FrierenMod:ShavedIceSpell.DESCRIPTION] | {Amountdiff} / 加 |
| FRIERENMOD-SWAMP_SPELL.description | 抽取抽牌堆中的{Amount:diff()}张[gold]魔力[/gold]。此效果每少抽1张，下个回合在手牌中生成1张。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-THUNDER.description | 对所有敌人造成{Damage:diff()}点伤害。 | 对所有敌人造成 !D! 点伤害。 NL 消耗 。 NL 虚无 。 [cards.json / FrierenMod:Thunder.DESCRIPTION] | { / amagediff} |
| FRIERENMOD-TIME_CONCEPT.description | 两回合后的回合开始时，恢复你现在的牌堆、生命、格挡、能量、能力、遗物及药水。{Upgraded:cond:\|将一张悔恨加入手牌。} | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-TONGUE_TWISTER_SPELL.title | 说绕口令不会大舌头的魔法 | 说绕口令时绝对不会大舌头的魔法 [cards.json / FrierenMod:TongueTwisterSpell.NAME] | 整段新增或需人工对照 |
| FRIERENMOD-TONGUE_TWISTER_SPELL.description | 抽{Cards:diff()}张牌。手牌中每有1张[gold]魔力[/gold]，获得{Block:diff()}点格挡。 | 抽 !M! 张牌，每抽到一张 *魔力 ，获得 !B! 点 格挡 。 [cards.json / FrierenMod:TongueTwisterSpell.DESCRIPTION] | {Cardsdiff} / 手牌中 / 有1 / { / lockdiff} |
| FRIERENMOD-TRICK.description | 每回合开始时，将一张[gold]魔力[/gold]抑制加入手牌。 | 在你的回合开始时，将一张 *魔力隐藏 放入手牌。 [cards.json / FrierenMod:Trick.DESCRIPTION] | 每 / 抑制加 |
| FRIERENMOD-HIMMEL.description | 芙莉莲……<br>回合结束时，若本牌在手牌中，将一张[gold]后悔莫及[/gold]加入手牌。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-REPENT.description | 选择弃牌堆中的{Cards:diff()}张牌移入手牌，本回合它们的耗能为0。 |  保留 。 NL 选择弃牌堆的两张牌放入你的手牌，它们在本回合的耗能变为0。 NL 消耗 。 [cards.json / FrierenMod:Repent.DESCRIPTION] | 中 / {Cardsdiff} / 移 / 它们 |
| FRIERENMOD-LONGEVITY_PAIN.description | 连续[gold]同调[/gold]10次后，将一张[gold]寿命论[/gold]加入手牌。<br>（还剩{Remaining}次。） |  不能被打出 。连续 frierenmod:同调 10次后将 *寿命论 放入手中。 NL 这个 *诅咒 无法逃脱。 [cards.json / FrierenMod:LongevityPain.DESCRIPTION] | 一张 / 加 / 牌还剩{Remaining}次 |
| FRIERENMOD-FATALISM.description | 所有角色失去999点生命。 | 所有角色失去999生命。 [cards.json / FrierenMod:Fatalism.DESCRIPTION] | 点 |
| FRIERENMOD-EMERGENCY_FOOD.description | 回复 [green]{Heal}[/green] 点生命。 |  保留 。 NL 回复 !M! 生命。 NL 消耗 。 [cards.json / FrierenMod:EmergencyFood.DESCRIPTION] | {Heal}点 |
| FRIERENMOD-BIRD_CAPTURING_SPELL.description | 击晕一个带翅膀的敌人 [blue]{Amount}[/blue] 回合。 |  *击晕 一个鸟类，或者长的像鸟类的敌人3回合。 NL 消耗 。 [cards.json / FrierenMod:BirdCapturingSpell.DESCRIPTION] | 带翅膀 / {Amount} |
| FRIERENMOD-UPSIDE_DOWN.description | 每[gold]同调[/gold]一次，交换抽牌堆和弃牌堆，并失去 [red]2[/red] 点生命。 |  不能被打出 。 NL 每 frierenmod:同调 一次，交换你的抽牌堆和弃牌堆并失去2点生命。 [cards.json / FrierenMod:UpsideDown.DESCRIPTION] | 整段新增或需人工对照 |
| FRIERENMOD-MANA_AFFINITY.description | 你的[gold]魔力[/gold]获得[gold]保留[/gold]。 |  *魔力 拥有 保留 。 [cards.json / FrierenMod:ManaAffinity.DESCRIPTION] | 你的 / 获得 |
| FRIERENMOD-SNUGGLE.description | 选择：<br>弃掉至多{Amount}张牌，每弃掉1张[gold]魔力[/gold]，获得{energyPrefix:energyIcons(1)}。<br>或在手牌生成1张[gold]魔力[/gold]。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-SNUGGLE1.description | 弃掉至多{Amount}张牌，每弃掉1张[gold]魔力[/gold]，获得{energyPrefix:energyIcons(1)}。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-SNUGGLE2.description | 在手牌生成1张[gold]魔力[/gold]。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-AURA_DISPEL.title | 解除魔法 | 解析结界的魔法 [cards.json / FrierenMod:BreaksBarriersSpell.NAME] | 除 |
| FRIERENMOD-AURA_DISPEL.description | 消灭一个被操纵的士兵，触发其[gold]灵魂解放[/gold]。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-ENDLESS_FLOW.title | 生生流转 | 流转 [cards.json / FrierenMod:Flow.NAME] | 生生 |
| FRIERENMOD-ENDLESS_FLOW.description | 向抽牌堆、手牌和弃牌堆各加入1张[gold]魔力[/gold]。对所有敌人造成等同于你的魔力总数的伤害。 | 在每回合结束时，消耗所有 *魔力 ，对所有敌人造成消耗的 *魔力 数量4倍的伤害。 [cards.json / FrierenMod:PajamasForm.DESCRIPTION] | 向抽牌堆手牌和弃牌堆各加入1张 / 等同于你 / 总 |
| FRIERENMOD-FRIEREN_THE_SLAYER.title | 葬送的芙莉莲 | 蓝图芙莉莲 [cards.json / FrierenMod:BluePrint.NAME] | 葬送的 |
| FRIERENMOD-FRIEREN_THE_SLAYER.description | 每回合开始时，从所有中文卡名含“魔法”的{Upgraded:cond:升级\|}卡牌中选择1张加入手牌。本回合可免费打出。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-KINDLING_SPARK.title | 星星之火 | 星星 [cards.json / FrierenMod:DrawOption.NAME] | 之火 |
| FRIERENMOD-KINDLING_SPARK.description | 对所有敌人造成{Damage:diff()}点伤害。将{Upgraded:cond:X+1\|X}张[gold]小小火花[/gold]加入手牌。{InCombat:cond:<br>（加入{ActualSparks}张小小火花）\|} | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-CRYING.title | 嗷嗷哭 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-CRYING.description | 获得{Block:diff()}点格挡，给予{Weak:diff()}层[gold]虚弱[/gold]。交换此卡的格挡值和虚弱层数。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-UNDERESTIMATED.title | 真是被看扁了啊 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-UNDERESTIMATED.description | 当你没有[gold]缩小[/gold]时，获得1层[gold]缩小[/gold]。下回合获得{Energy:energyIcons()}并多抽1张牌。 |  不能被打出 。 NL 当你抽到这张牌时，获得 !M! 点 frierenmod:专注 并抽1张牌，然后 消耗 。 [cards.json / FrierenMod:Meditation.DESCRIPTION] | 没有缩小 / 1层缩小下回合获得{En / gy / ergyIc / ns} / 多 |
| FRIERENMOD-ENCHANTED_STRIKE.title | 附魔打击 | 打击 [cards.json / FrierenMod:Strike_Frieren.NAME] | 附魔 |
| FRIERENMOD-ENCHANTED_STRIKE.description | 造成{CalculationBase}点伤害。你的手牌中每有一张[gold]魔力[/gold]，伤害增加{ExtraDamage:diff()}点。（造成{CalculatedDamage:diff()}点伤害） | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-TREASURE_VAULT_SPELL.title | 拼命守护宝库的魔法 | 干练的魔法使 [cards.json / FrierenMod:SkilledMage.NAME] | 拼命守护宝库 |
| FRIERENMOD-TREASURE_VAULT_SPELL.description | 获得{Block:diff()}点格挡X次。在抽牌堆生成{Upgraded:cond:X+1\|X}张[gold]魔力[/gold]。{InCombat:cond:<br>（获得{Block:diff()}点格挡{ActualTimes}次，生成{ActualMana}张魔力）\|} | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-DOMAIN_EXPANSION.title | 领域展开 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-DOMAIN_EXPANSION.description | 每回合开始时，将一张[gold]高密度魔力[/gold]加入手牌。 | 在你的回合开始时，将一张 *魔力隐藏 放入手牌。 [cards.json / FrierenMod:Trick.DESCRIPTION] | 每 / 高密度 / 加 |
| FRIERENMOD-SLEEP_IN.title | 赖床 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-SLEEP_IN.description | 获得{Block:diff()}点格挡和1层[gold]堡垒[/gold]。将一张[gold]起床！[/gold]加入抽牌堆。起床！被打出或消耗之前，你不能打出其他牌。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-WAKE_UP.title | 起床！ | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-WAKE_UP.description | 失去[gold]堡垒[/gold]，获得赖床期间累计剩余的能量（{StoredEnergy}点能量）。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-GOOD_LUCK_SPELL.title | 变好运的魔法 | 变出刨冰的魔法 [cards.json / FrierenMod:ShavedIceSpell.NAME] | 好运 |
| FRIERENMOD-GOOD_LUCK_SPELL.description | [gold]传说魔法[/gold]。<br>选择：升级牌组中随机一张牌；额外掉落一次魔法牌组奖励；在手牌中添加{Mana:diff()}张[gold]魔力[/gold]。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-STRONGER_WISH.title | 我要变得更强！ | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-STRONGER_WISH.description | 升级牌组中随机一张牌。 |  NL （牌组中有 [cards.json / FrierenMod:FairyTaleEnd.EXTENDED_DESCRIPTION[0]] | 升级 / 随机一张牌 |
| FRIERENMOD-REWARD_WISH.title | 我要更多奖励！ | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-REWARD_WISH.description | 本场战斗胜利后，额外掉落一次魔法牌组奖励。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-MANA_WISH.title | 我要更多魔力！ | 魔力 [cards.json / FrierenMod:Mana.NAME] | 我要更多 |
| FRIERENMOD-MANA_WISH.description | 在手牌中添加{Mana:diff()}张[gold]魔力[/gold]。 | 在弃牌堆中抽 !M! 张 *魔力 。 [cards.json / FrierenMod:BackFlow.DESCRIPTION] | 手 / 添加{ / anadiff} |
## characters.json（9 条）

| ID | 二代当前全文 | 一代对照原文 | 归一化新增/替换片段 |
|---|---|---|---|
| FRIERENMOD-FRIEREN.description | 漫长旅途中的精灵魔法使。<br>积蓄魔力，咏唱魔法，收集旅途中的魔法因子与魔具。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-FRIEREN.pronounObject | 她 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-FRIEREN.pronounSubject | 她 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-FRIEREN.pronounPossessive | 她的 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-FRIEREN.possessiveAdjective | 她的 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-FRIEREN.cardsModifierTitle | 芙莉莲卡牌 | 芙莉莲 [characters.json / FrierenMod:Frieren.NAMES[0]] | 卡牌 |
| FRIERENMOD-FRIEREN.cardsModifierDescription | 加入芙莉莲的卡牌。 | 芙莉莲 [characters.json / FrierenMod:Frieren.NAMES[0]] | 加入 / 的卡牌 |
| FRIERENMOD-FRIEREN.eventDeathPrevention | 旅途还没有结束。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-FRIEREN.unlockText | 踏上新的旅途。 | 未找到近似原文 | 整段新增或需人工对照 |
## encounters.json（1 条）

| ID | 二代当前全文 | 一代对照原文 | 归一化新增/替换片段 |
|---|---|---|---|
| FRIERENMOD-AURA_ENCOUNTER.title | 断头台阿乌拉 | 自尽吧，阿乌拉 [UIs.json / FrierenMod:Slot2.TEXT_DICT.5001] | 断头台 |
## monsters.json（12 条）

| ID | 二代当前全文 | 一代对照原文 | 归一化新增/替换片段 |
|---|---|---|---|
| FRIERENMOD-MIMIC.chew | （咀嚼的声音） | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-AURA.name | 阿乌拉 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-AURA_SOLDIER.name | 被操纵的士兵 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-AURA.taunt | 哈哈哈，葬送的芙莉莲也不过如此。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-AURA.moves.SCALES.title | 服从天平 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-AURA.moves.GUILLOTINE.title | 断头台 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-AURA.moves.STAGGER.title | 天平反制 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-AURA.moves.TAUNT.title | 嘲讽 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-AURA.moves.SUMMON.title | 操纵士兵 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-AURA.moves.BARRAGE.title | 连击魔法 | 普通攻击魔法SP [monsters.json / FrierenMod:Spiegel_Frieren.MOVES[0]] | 连 |
| FRIERENMOD-AURA_SOLDIER.moves.STRIKE.title | 斩击 | 重击 [monsters.json / FrierenMod:Mimic.MOVES[1]] | 斩 |
| FRIERENMOD-AURA_SOLDIER.moves.GUARD.title | 护卫 | 未找到近似原文 | 整段新增或需人工对照 |
## potions.json（3 条）

| ID | 二代当前全文 | 一代对照原文 | 归一化新增/替换片段 |
|---|---|---|---|
| FRIERENMOD-BOTTLED_MANA.description | 将{Cards}张[gold]魔力[/gold]加入手牌。 |  将 #b%d 张 #y魔力 加入手牌。 [potions.json / FrierenMod:BottledMana.DESCRIPTIONS[0]] | {Car / s} |
| FRIERENMOD-DISSOLVE_CLOTH_POTION.description | 移除目标的所有格挡，给予{VulnerablePower}层[gold]易伤[/gold]和{WeakPower}层[gold]虚弱[/gold]。 | 去除对象的格挡并给予 #b%d 层 #y易伤 和 #y虚弱 。 [potions.json / FrierenMod:DissolveClothPotion.DESCRIPTIONS[0]] | 移 / 目标 / 所有 / {VulnerablePower} / {WeakPower}层 |
| FRIERENMOD-EMPEROR_WINE.description | 从3张随机稀有牌中选择1张加入手牌，本回合耗能为0。 | 从 #b3 张随机的稀有牌中选择 #b1 张加入你的手牌。这张牌在本回合耗能变为0 。 [potions.json / FrierenMod:EmperorWine.DESCRIPTIONS[0]] | 整段新增或需人工对照 |
## powers.json（97 条）

| ID | 二代当前全文 | 一代对照原文 | 归一化新增/替换片段 |
|---|---|---|---|
| FRIERENMOD-CONCENTRATION_POWER.description | [gold]咏唱[/gold][gold]魔力[/gold][gold]消耗[/gold]减少 {Amount}。打出非[gold]魔力[/gold]、非速攻牌后减少 1 层。 |  打出非 #y魔力 牌后失去1点该能力。 [powers.json / FrierenMod:ConcentrationPower.DESCRIPTIONS[0]] | 咏唱魔力消耗减少{Amount} / 非速攻 / 减少 / 层 |
| FRIERENMOD-WEAKENED_CHANT_POWER.description | 本回合[gold]咏唱[/gold][gold]魔力[/gold][gold]消耗[/gold]增加 {Amount}。 | 在本回合中， #y咏唱 消耗的 #y魔力 增加 #b%d 张 。 [powers.json / FrierenMod:WeakenedChantPower.DESCRIPTIONS[0]] | 魔力 / {Amount} |
| FRIERENMOD-CHANT_WITHOUT_MANA_POWER.title | 无耗咏唱 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-CHANT_WITHOUT_MANA_POWER.description | 本回合[gold]咏唱[/gold]不[gold]消耗[/gold][gold]魔力[/gold]。 | 在本回合中，你的 #y咏唱 不再 消耗 #y魔力 。 [powers.json / FrierenMod:ChantWithoutManaPower.DESCRIPTIONS[0]] | 整段新增或需人工对照 |
| FRIERENMOD-CHANT_WITHOUT_MANA_TIMES_POWER.title | 从容咏唱 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-CHANT_WITHOUT_MANA_TIMES_POWER.description | 接下来 {Amount} 次[gold]咏唱[/gold]不[gold]消耗[/gold][gold]魔力[/gold]。终极[gold]咏唱[/gold]只[gold]消耗[/gold] 1 次。 | 在本回合中，下 #b%d 次 #y咏唱 不再 消耗 #y魔力 。 [powers.json / FrierenMod:ChantWithoutManaTimesPower.DESCRIPTIONS[0]] | 接 / 来{Amount} / 终极咏唱只消耗1次 |
| FRIERENMOD-BAN_MANA_GAIN_POWER.title | 无法生成魔力 | 在本回合中，你无法从卡牌中获取 #y魔力 。 [powers.json / FrierenMod:BanManaGainPower.DESCRIPTIONS[0]] | 生成 |
| FRIERENMOD-BAN_MANA_GAIN_POWER.description | 本回合无法生成[gold]魔力[/gold]。 | 在本回合中，你无法从卡牌中获取 #y魔力 。 [powers.json / FrierenMod:BanManaGainPower.DESCRIPTIONS[0]] | 生成 |
| FRIERENMOD-MANA_INSTEAD_OF_ENERGY_POWER.title | 魔力代付 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-MANA_INSTEAD_OF_ENERGY_POWER.description | 普通卡牌显示 {energyPrefix:energyIcons(0)}，实际[gold]消耗[/gold]原费用对应的[gold]魔力[/gold]（抽牌堆→手牌→弃牌堆）。[gold]魔力[/gold]不足无法打出。X 费牌仍使用能量。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-SYNCHRO_WITHOUT_MANA_POWER.title | 节俭 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-SYNCHRO_WITHOUT_MANA_POWER.description | 接下来 {Amount} 次[gold]同调[/gold]将[gold]魔力[/gold]返回手牌而非[gold]消耗[/gold]。高密度魔力回手时仍回复能量。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-STRIKE_UP_POWER.title | 近战魔法使 | 魔法 [powers.json / FrierenMod:ChantWithoutManaPower.NAME] | 近战 / 使 |
| FRIERENMOD-STRIKE_UP_POWER.description | 打击伤害增加 {Amount}。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-METALLICIZE_POWER.title | 金属化 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-METALLICIZE_POWER.description | 回合结束获得 {Amount} 格挡。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-PHANTASMAL_POWER.title | 幻影 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-PHANTASMAL_POWER.description | 接下来 {Amount} 个回合获得双倍攻击伤害。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-RESTORE_STRENGTH_POWER.title | 力量恢复 | 力量融合 [powers.json / FrierenMod:StrengthFusionPower.NAME] | 恢复 |
| FRIERENMOD-RESTORE_STRENGTH_POWER.description | 回合结束恢复 {Amount} 力量。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-MANTRA_POWER.title | 真言 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-MANTRA_POWER.description | 达到 10 层时[gold]消耗[/gold] 10 层，进入[gold]神格[/gold]并获得 {energyPrefix:energyIcons(3)}。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-DIVINITY_POWER.title | 神格 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-DIVINITY_POWER.description | 攻击伤害为三倍，持续至下回合。 | 伤害融合 [powers.json / FrierenMod:DamageFusionPower.NAME] | 攻击 / 为三倍持续至下回 |
| FRIERENMOD-PUZZLE_POWER.description | …… | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-FULL_AHEAD_POWER.description | 每回合第一次[gold]咏唱[/gold]后，获得 {Amount} 力量和敏捷。 | 每回合第一次 #y咏唱 后，获得 #b%d #y力量 和 #b%d #y敏捷 。 [powers.json / FrierenMod:FullAheadPower.DESCRIPTIONS[0]] | {Amount} |
| FRIERENMOD-THUNDER_POWER.title | 雷电魔法 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-THUNDER_POWER.description | [gold]同调[/gold]对随机敌人造成 15 点伤害，不再抽牌。（受力量等效果影响） | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-SIMMERING_POWER.title | 蓄势待发 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-SIMMERING_POWER.description | 每次[gold]咏唱[/gold]时，从抽牌堆抽取 {Amount} 张[gold]传说魔法[/gold]。 |  #y咏唱 后，抽 #b%d 张 #y传说魔法 。 [powers.json / FrierenMod:SimmeringPower.DESCRIPTIONS[0]] | 每次 / 时从 / 牌堆抽取{Amount} |
| FRIERENMOD-PAPER_PLANE_POWER.title | 纸飞机 | 让纸飞机飞得很远的魔法 [powers.json / FrierenMod:PaperPlaneSpellPower.NAME] | 整段新增或需人工对照 |
| FRIERENMOD-PAPER_PLANE_POWER.description | 本回合最高连续[gold]同调[/gold]次数{AllowEqual:cond:大于或等于\|大于}本场战斗此前回合的最高记录时，攻击伤害翻倍。<br>本回合：{Current}；此前最高：{Previous}。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-SEAL_POWER.description | 单回合打出 5 张[gold]咏唱[/gold]牌后，将[gold]封印[/gold]的卡牌加入手牌。 | 一回合内 #y咏唱 #b%d 次后打破封印，将 #b%d 张 #y地狱业火魔法 加入你的手牌。 [powers.json / FrierenMod:SealPower.DESCRIPTIONS[0]] | 单 / 打出5张 / 牌 / 将 / 的卡牌 |
| FRIERENMOD-ACCELERATE_FLOW_POWER.description | 本回合[gold]同调[/gold]抽2张牌；雷电魔法的伤害效果仍然[gold]保留[/gold]。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-OUTPOURING_POWER.title | 涌流 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-OUTPOURING_POWER.description | 本回合每次[gold]同调[/gold]获得{Amount}点力量。 | 你每 #y同调 一张 #y魔力，获得 #b%d 点 #y力量 。 [powers.json / FrierenMod:OutpouringPower.DESCRIPTIONS[0]] | 本回合 / 次 / {Amount} |
| FRIERENMOD-PERPETUAL_POWER.description | 回合开始时在弃牌堆生成每层2张[gold]魔力[/gold]，无视禁止生成效果。 | 在你的回合开始时，将 #b%d 张 #y魔力 置入弃牌堆。  [powers.json / FrierenMod:PerpetualPower.DESCRIPTIONS[0]] | 在弃牌堆生成每层2 / 无视禁止生成效果 |
| FRIERENMOD-RINGLET_FORM_POWER.title | 纵卷发形态 | 卷发形态 [powers.json / FrierenMod:RingletFormPower.NAME] | 纵 |
| FRIERENMOD-RINGLET_FORM_POWER.description | 每[gold]消耗[/gold]一张[gold]魔力[/gold]，获得{Amount}点格挡。 | 你每 #y消耗 一张 #y魔力，获得 #b%d 点 #y格挡 。 [powers.json / FrierenMod:RingletFormPower.DESCRIPTIONS[0]] | {Amount} |
| FRIERENMOD-RITES_PREPARATION_POWER.description | 接下来{Amount}次[gold]同调[/gold]对所有敌人造成5点伤害。 | 下 #b%d 张 #y同调 的 #y魔力 会对所有敌人造成6点伤害。 [powers.json / FrierenMod:RitesPreparationPower.DESCRIPTIONS[0]] | 接 / 来{Amount}次 / 5 |
| FRIERENMOD-TRICK_POWER.title | 小伎俩 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-TRICK_POWER.description | 回合开始时在手牌生成一张[gold]魔力[/gold]抑制。 | 在你的回合开始时，将一张 #y魔力隐藏 置入手牌。 [powers.json / FrierenMod:TrickPower.DESCRIPTIONS[0]] | 在手牌生成 / 抑制 |
| FRIERENMOD-PAJAMAS_FORM_POWER.description | 回合结束时[gold]消耗[/gold]三个牌堆的所有[gold]魔力[/gold]，每张对所有敌人造成{DamagePerMana}点伤害。 | 在每回合结束时，消耗所有 #y魔力 ，对所有敌人造成消耗的 #y魔力 数量的伤害的 #b%d 倍。 NL （将造成 #b%d 点伤害）。 [powers.json / FrierenMod:PajamasFormPower.DESCRIPTIONS[0]] | 三个牌堆的 / 每张 / {DamagePerMana} |
| FRIERENMOD-FLYING_MAGIC_POWER.description | 回合结束时[gold]消耗[/gold]弃牌堆的{Amount}张[gold]魔力[/gold]。 | 你的回合结束时， #y消耗 弃牌堆中 #b%d 张 #y魔力 。 [powers.json / FrierenMod:FlyingMagicPower.DESCRIPTIONS[0]] | 的{Amount} |
| FRIERENMOD-SWITCH_STRENGTH_AND_DEXTERITY_POWER.title | 力量敏捷交换 | 交换 [powers.json / FrierenMod:SwitchStrengthAndDexterityPower.NAME] | 力量敏捷 |
| FRIERENMOD-SWITCH_STRENGTH_AND_DEXTERITY_POWER.description | 回合结束时再次交换力量和敏捷。 | 在你的回合结束时，交换你的 #y力量 和 #y敏捷 层数。 [powers.json / FrierenMod:SwitchStrengthAndDexterityPower.DESCRIPTIONS[0]] | 再次 |
| FRIERENMOD-MANA_RETURN_POWER.title | 魔力回流 | 魔力返还 [powers.json / FrierenMod:ManaReturnPower.NAME] | 回流 |
| FRIERENMOD-MANA_RETURN_POWER.description | 每受到一次攻击，在攻击者的弃牌堆生成{Amount}张[gold]魔力[/gold]。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-OPEN_THE_WAYGATE_POWER.description | 剩余同调机会：{Remaining}；已获得的额外回合：{Pending}。{HasOpportunity:cond:<br>下一次需连续同调{Threshold}次，当前{Consecutive}次。\|}所有机会和额外回合用完后失去此能力。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-FRIEREN_SUITCASE_POWER.title | 芙莉莲的手提箱 | 芙莉莲的旅行箱 [powers.json / FrierenMod:FrierenSuitcasePower.NAME] | 手提 |
| FRIERENMOD-FRIEREN_SUITCASE_POWER.description | 手牌上限改为13。打出牌后若手牌已满，获得{Amount}点格挡。 | 本场战斗中手牌上限提高至13。 NL 使用卡牌后若手牌已满，获得 #b%d 点格挡。 [powers.json / FrierenMod:FrierenSuitcasePower.DESCRIPTIONS[0]] | 改为 / 打出 / {Amount} |
| FRIERENMOD-GAIN_RELIC_POWER.title | 饰品魔法 | 寻找遗失饰品的魔法 [powers.json / FrierenMod:GainRelicPower.NAME] | 整段新增或需人工对照 |
| FRIERENMOD-GAIN_RELIC_POWER.description | 战斗胜利后额外获得{Amount}个随机遗物奖励。 | 战斗结束时，获得 #b%d 件随机遗物。 [powers.json / FrierenMod:GainRelicPower.DESCRIPTIONS[0]] | 胜利后额外 / {Amount}个 / 奖励 |
| FRIERENMOD-TIME_TRAVEL_POWER.title | 时间回溯 | 精灵的时间观念 [powers.json / FrierenMod:TimeTravelPower.NAME] | 回溯 |
| FRIERENMOD-TIME_TRAVEL_POWER.description | {Amount}回合后，恢复施法时的自身状态。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-MIMIC_POWER.description | 再打出 [blue]{Amount}[/blue] 张牌后，强制结束回合，移除抽牌堆、手牌及弃牌堆的所有[gold]魔力[/gold]，并获得[gold]好黑啊[/gold]。 | 当你打出 #b%d 张牌后，你的回合会强制结束，移除你所有 #y魔力 并获得 #y好黑啊 。 [powers.json / FrierenMod:MimicPower.DESCRIPTIONS[0]] | 再 / {Amount} / 回合 / 抽牌堆手牌及弃牌堆的 |
| FRIERENMOD-DARK_POWER.description | 在宝箱怪肚子里看不清了……每回合逐渐适应黑暗。 | 在宝箱怪肚子里看不清了... [powers.json / FrierenMod:DarkPower.DESCRIPTIONS[0]] | ……每回合逐渐适应黑暗 |
| FRIERENMOD-SWALLOWED_POWER.description | 本场战斗中，手牌上限降低 [blue]{Amount}[/blue]。 | 本场战斗中手牌上限减少 #b%d 。 [powers.json / FrierenMod:SwallowedPower.DESCRIPTIONS[0]] | 降低{Amount} |
| FRIERENMOD-BIRD_CAPTURED_POWER.description | 接下来 [blue]{Amount}[/blue] 回合无法行动。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-COPY_POWER.description | 受到未被格挡的伤害时，获得等量[gold]魔力[/gold]。玩家获得[gold]格挡[/gold]时，获得相同数量的格挡。生命降至 [red]100[/red] 或以下时，将使出全力！ | 受到未被格挡的伤害时，获得等量 #r魔力 层数 。 NL 玩家获得 #y格挡 时，获得相同数量的 #y格挡 。 NL 血量降到100以下时将会逼出复制体的全力！ [powers.json / FrierenMod:CopyPower.DESCRIPTIONS[0]] | 生命 / 至 / 或 / 使 |
| FRIERENMOD-ENEMY_MANA_POWER.description | 当前有 [blue]{StoredMana}[/blue] 层魔力。施放魔法时消耗对应层数。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-SPELL_CASTER_POWER.description | 魔力足够时，依次施放以下魔法，最多两轮：<br>{Spells} | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-SPIEGEL_MALLEABLE_POWER.title | 柔韧 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-SPIEGEL_MALLEABLE_POWER.description | 受到未被格挡的攻击伤害时，获得 [blue]{Amount}[/blue] 点格挡；本回合每触发一次，下一次多获得 [blue]1[/blue] 点格挡。 | 受到未被格挡的伤害时，获得等量 #r魔力 层数 。 NL 玩家获得 #y格挡 时，获得相同数量的 #y格挡 。 NL 血量降到100以下时将会逼出复制体的全力！ [powers.json / FrierenMod:CopyPower.DESCRIPTIONS[0]] | 攻击 / {Amount}点格挡本回合每触发一次下一次多 / 1点 |
| FRIERENMOD-SPELL_CASTER_POWER.spells.0 | {Cost}魔力：誓死守护宝库的魔法，获得壁垒与{Block}点格挡。 | 誓死守护宝库的魔法 NL  [powers.json / FrierenMod:SpellCasterPower.DESCRIPTIONS[0]] | {Cost}魔力 / 获得壁垒与{Block}点格挡 |
| FRIERENMOD-SPELL_CASTER_POWER.spells.1 | {Cost}魔力：高速咏唱SP，获得{Ritual}层仪式。 | 高速咏唱SP NL  [powers.json / FrierenMod:SpellCasterPower.DESCRIPTIONS[2]] | {Cost}魔力 / 获得{Ritual}层仪式 |
| FRIERENMOD-SPELL_CASTER_POWER.spells.2 | {Cost}魔力：操纵石像的魔法SP，获得1层人工制品。 | 操纵石像的魔法SP NL  [powers.json / FrierenMod:SpellCasterPower.DESCRIPTIONS[4]] | {Cost}魔力 / 获得1层人工制品 |
| FRIERENMOD-SPELL_CASTER_POWER.spells.3 | {Cost}魔力：地狱业火魔法SP，在玩家抽牌堆放入{Burns}张灼伤。 | 花费 #b%d 层 #r魔力 ，在敌人抽牌堆中放入 #b%d 张 #y灼伤 。 NL  [powers.json / FrierenMod:SpellCasterPower.DESCRIPTIONS[7]] | {Cost} / 地狱业火魔法SP / 玩家 / {Burns} |
| FRIERENMOD-SPELL_CASTER_POWER.spells.4 | {Cost}魔力：破灭之雷魔法SP，使玩家获得2层易伤与虚弱。 | 破灭之雷魔法SP NL  [powers.json / FrierenMod:SpellCasterPower.DESCRIPTIONS[8]] | {Cost}魔力 / 使玩家获得2层易伤与虚弱 |
| FRIERENMOD-SPELL_CASTER_POWER.spells.5 | {Cost}魔力：防御魔法完全体SP，获得{Buffer}层缓冲。 | 防御魔法完全体SP NL  [powers.json / FrierenMod:SpellCasterPower.DESCRIPTIONS[10]] | {Cost}魔力 / 获得{Buffer}层缓冲 |
| FRIERENMOD-SPELL_CASTER_POWER.spells.6 | {Cost}魔力：捕鸟魔法，召唤1只潮湿邪教徒。 | 花费 #b%d 层 #r魔力 ，召唤 #b1 只 #y咔咔 。 NL  [powers.json / FrierenMod:SpellCasterPower.DESCRIPTIONS[13]] | {Cost} / 捕鸟魔法 / 潮湿邪教徒 |
| FRIERENMOD-SPELL_CASTER_POWER.spells.7 | {Cost}魔力：飞行魔法SP，获得1层无实体。（仅首次施放，第二轮跳过且不消耗魔力） | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-SPELL_CASTER_POWER.spells.5.enhanced | {Cost}魔力：防御魔法完全体SP，获得{Buffer}层缓冲。 | 防御魔法完全体SP NL  [powers.json / FrierenMod:SpellCasterPower.DESCRIPTIONS[10]] | {Cost}魔力 / 获得{Buffer}层缓冲 |
| FRIERENMOD-MANA_AFFINITY_POWER.description | 魔力获得[gold]保留[/gold]。 |  #y魔力 拥有 #y保留 。 [powers.json / FrierenMod:ManaAffinityPower.DESCRIPTIONS[0]] | 获得 |
| FRIERENMOD-AURA_ELEGANCE_POWER.title | 优雅 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-AURA_ELEGANCE_POWER.description | 当前魔力：[blue]{Mana}[/blue]。受到未被格挡的伤害时，优先消耗等量魔力；不足部分扣除生命。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-AURA_SCALES_POWER.title | 服从天平 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-AURA_SCALES_POWER.description | 每3回合结束时比较双方魔力总量。阿乌拉更高：你下回合少抽2张牌，她下次必定攻击。你更高：她眩晕1回合，随后将生命与生命上限降至1，并将失去的生命转为魔力。平局无效果。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-AURA_ARROGANCE_POWER.title | 自大的魔族 | 直视的魔眼 [powers.json / FrierenMod:MysticEyePower.NAME] | 自大 / 族 |
| FRIERENMOD-AURA_ARROGANCE_POWER.description | 你首次打出解除魔法后，阿乌拉的下次行动变为嘲讽。天平结算回合及其裁决优先，嘲讽顺延。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-AURA_SOUL_POWER.title | 灵魂解放 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-AURA_SOUL_POWER.description | 死亡时，阿乌拉失去15魔力，在你的弃牌堆生成10张魔力。 | 你的回合结束时， #y消耗 弃牌堆中 #b%d 张 #y魔力 。 [powers.json / FrierenMod:FlyingMagicPower.DESCRIPTIONS[0]] | 死亡时阿乌拉失去15魔力在 / 生成10 |
| FRIERENMOD-AURA_SUBMISSION_POWER.title | 服从 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-AURA_SUBMISSION_POWER.description | 下回合少抽[blue]{Amount}[/blue]张牌。阿乌拉即将使用断头台。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-RECYCLE_POWER.description | 每有一张从抽牌堆或手牌流向弃牌堆的[gold]魔力[/gold]，获得{Amount}点格挡。 | 你每 #y消耗 一张 #y魔力，获得 #b%d 点 #y格挡 。 [powers.json / FrierenMod:RingletFormPower.DESCRIPTIONS[0]] | 有 / 从抽牌堆或手牌流向弃牌堆的 / {Amount} |
| FRIERENMOD-SWAMP_MANA_NEXT_TURN_POWER.title | 沼泽脱困 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-SWAMP_MANA_NEXT_TURN_POWER.description | 下个自己的回合开始、正常抽牌后，向手牌生成{Amount}张[gold]魔力[/gold]。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-FRIEREN_THE_SLAYER_POWER.title | 葬送的芙莉莲 | ...芙莉莲... [powers.json / FrierenMod:Factor100Power.DESCRIPTIONS[0]] | 葬送的 |
| FRIERENMOD-FRIEREN_THE_SLAYER_POWER.description | 每回合开始时，从所有中文卡名含“魔法”的卡牌中选择{Amount}张加入手牌，其中{UpgradedChoices}张升级。本回合可免费打出。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-ACCELERATE_FLOW_NEXT_TURN_POWER.title | 下回合加速同调 | 本回合中你的 #y同调 变为 #y加速同调 。 [powers.json / FrierenMod:AccelerateFlowPower.DESCRIPTIONS[0]] | 下 |
| FRIERENMOD-ACCELERATE_FLOW_NEXT_TURN_POWER.description | 下回合，[gold]同调[/gold]变为[gold]加速同调[/gold]。 | 本回合中你的 #y同调 变为 #y加速同调 。 [powers.json / FrierenMod:AccelerateFlowPower.DESCRIPTIONS[0]] | 下 |
| FRIERENMOD-DOMAIN_EXPANSION_POWER.title | 领域展开 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-DOMAIN_EXPANSION_POWER.description | 每回合开始时，将{Amount}张[gold]高密度魔力[/gold]加入手牌。 | 在你的回合开始时，将一张 #y魔力隐藏 置入手牌。 [powers.json / FrierenMod:TrickPower.DESCRIPTIONS[0]] | 每 / {Amount} / 高密度 / 加 |
| FRIERENMOD-SLEEP_IN_POWER.title | 赖床 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-SLEEP_IN_POWER.description | 起床！被打出或消耗之前，你不能打出其他牌。已累计{StoredEnergy}点剩余能量。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-GOOD_LUCK_REWARD_POWER.title | 我要更多奖励！ | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-GOOD_LUCK_REWARD_POWER.description | 本场战斗胜利后，额外掉落{Amount}次魔法牌组奖励。 | 未找到近似原文 | 整段新增或需人工对照 |
## relics.json（28 条）

| ID | 二代当前全文 | 一代对照原文 | 归一化新增/替换片段 |
|---|---|---|---|
| FRIERENMOD-HOLY_EMBLEM.title | 圣徽 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-HOLY_EMBLEM.flavor | 旅途中的魔法积累。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-EAR_ACCESSORY.description | 战斗开始时，将2张[gold]魔力[/gold]加入手牌。 | 战斗开始时，将 #b2 张 #y魔力 加入你的手牌。 [relics.json / FrierenMod:EarAccessory.DESCRIPTIONS[0]] | 整段新增或需人工对照 |
| FRIERENMOD-FAKE_FLAMME_GRIMOIRE.description | 每场战斗的第1回合，你打出[gold]传说魔法[/gold]不需要提前[gold]咏唱[/gold]。 | 在第一个回合，你打出 #y传说魔法 不需要提前 #y咏唱 。 [relics.json / FrierenMod:FakeFlammeGrimoire.DESCRIPTIONS[0]] | 每场战斗的 / 1 |
| FRIERENMOD-HAIR_ACCESSORY.description | 每当[gold]魔力[/gold]被消耗时，获得1层[gold]专注[/gold]。 | 每当 #y魔力 消耗后，获得1点 #y专注 。 [relics.json / FrierenMod:HairAccessory.DESCRIPTIONS[0]] | 被 / 时 / 层 |
| FRIERENMOD-HAITAA_WAND.description | 已废弃。无法获得，不再产生效果。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-HIMMEL_GRAVESTONE.description | 每回合开始时，获得{energyPrefix:energyIcons(1)}。拾取时，将一张[gold]长寿种的痛苦[/gold]加入牌组。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-ICICLE_CHERRY_BLOSSOM.description | 每回合开始时，获得{energyPrefix:energyIcons(1)}。卡牌奖励中的每张牌有60%的概率被替换为[gold]魔力[/gold]。 | 在每回合开始时获得 [E] 。在卡牌奖励画面，可供选择的牌有 #b60 %的概率被替换为 #y魔力 。 [relics.json / FrierenMod:IcicleCherryBlossom.DESCRIPTIONS[0]] | {energyPrefixenergyIcons1} / 中 / 每张 |
| FRIERENMOD-IMMOVABLE_CLOAK.description | 只要本场战斗中你没有失去过生命，每回合多抽1张牌。 | 只要本场战斗你没有失去过生命，每回合开始多抽 #b1 张牌。 [relics.json / FrierenMod:ImmovableCloak.DESCRIPTIONS[0]] | 中 |
| FRIERENMOD-MERCURY_PUDDING.description | 战斗胜利时，若你的生命值不高于10，回复最大生命值的50%。仅能生效一次。 | 战斗结束时，若你的血量在10点及以下，回复你最大生命值的 #b50% （仅能起效一次）。 [relics.json / FrierenMod:MercuryPudding.DESCRIPTIONS[0]] | 胜利 / 生命值不高于 / 生 |
| FRIERENMOD-MICKEY_HOUSE.description | 每次进入事件房间，获得一个[gold]神奇妙妙工具[/gold]。 | 每次进入？房间时获得一个 #y神奇妙妙工具 。 [relics.json / FrierenMod:MickeyHouse.DESCRIPTIONS[0]] | 事件 |
| FRIERENMOD-MIMIC_HEAD.description | 每次打开非Boss宝箱时，额外获得100金币。 | 你每次打开一个非boss宝箱，里面都会有额外的 #b100 金币! [relics.json / FrierenMod:MimicHead.DESCRIPTIONS[0]] | B / 时 / 获得 |
| FRIERENMOD-MIRRORED_LOTUS_RING.description | 每场战斗的第4回合开始时，将一张[gold]辛美尔[/gold]加入手牌。 | 在第 #b4 回合开始时，将一张 #y辛美尔 置入你的手牌。 [relics.json / FrierenMod:MirroredLotusRing.DESCRIPTIONS[0]] | 每场战斗的 / 加 |
| FRIERENMOD-OLD_HAIR_ACCESSORY.description | 每[gold]同调[/gold]4次，抽1张牌。计数每场战斗重置。 | 你每 #y同调 #b4 次 ，抽 #b1 张牌。 [relics.json / FrierenMod:OLDHairAccessory.DESCRIPTIONS[0]] | 计数每场战斗重置 |
| FRIERENMOD-SCALE_OF_OBEDIENCE.description | 在普通战斗中，每回合开始时获得{energyPrefix:energyIcons(1)}，抽1张牌。 | 在普通战斗中，你在每回合开始时获得 [E] ，抽 #b1 张牌。 [relics.json / FrierenMod:ScaleOfObedience.DESCRIPTIONS[0]] | {energyPrefixenergyIcons1} |
| FRIERENMOD-STATUE_OF_HIMMEL.description | 前三回合开始时，若你没有[gold]人工制品[/gold]，获得1层[gold]人工制品[/gold]。 | 在前三个回合开始时，若你没有 #y人工制品 ，则获得 #b1 层 #y人工制品 。 [relics.json / FrierenMod:StatueOfHimmel.DESCRIPTIONS[0]] | 整段新增或需人工对照 |
| FRIERENMOD-STILLE.description | 战斗开始时，获得2点[gold]敏捷[/gold]。第3回合结束时，失去2点[gold]敏捷[/gold]。 | 战斗开始时获得 #b2 点 #y敏捷 ,在第三回合结束时失去 #b2 点 #y敏捷 。 [relics.json / FrierenMod:Stille.DESCRIPTIONS[0]] | 3 |
| FRIERENMOD-UBEL_STAFF.description | 每打出6张[gold]咏唱[/gold]牌，获得{energyPrefix:energyIcons(1)}。计数在战斗间保留。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-COMFORTABLE_BED.description | 每回合开始时，获得{energyPrefix:energyIcons(1)}。战斗开始时，向抽牌堆和弃牌堆各加入一张[gold]懒懒散散[/gold]。 | 每回合开始时获得 [E] 。在战斗开始时，将一张 #r懒懒散散 放入你的抽牌堆和弃牌堆。 [relics.json / FrierenMod:ComfortableBed.DESCRIPTIONS[0]] | {energyPrefixenergyIcons1} / 向 / 各加入一张懒懒散散 |
| FRIERENMOD-UPGRADED_HOLY_EMBLEM.title | 升级圣徽 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-UPGRADED_HOLY_EMBLEM.description | 每个回合开始时，在抽牌堆、手牌和弃牌堆各放入1张[gold]魔力[/gold]。 | 每个回合开始时，在抽牌堆和弃牌堆各放入 #b1 张 #y魔力 。 [relics.json / FrierenMod:HolyEmblem.DESCRIPTIONS[0]] | 手牌 |
| FRIERENMOD-UPGRADED_HOLY_EMBLEM.flavor | 旅途中的魔法积累。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-KRAFT_SERPENT_RING.title | 长蛇戒指 | 镜莲花戒指 [relics.json / FrierenMod:MirroredLotusRing.NAME] | 长蛇 |
| FRIERENMOD-KRAFT_SERPENT_RING.description | 每回合抽牌阶段额外抽 [blue]1[/blue] 张牌。 | 你每 #y同调 #b4 次 ，抽 #b1 张牌。 [relics.json / FrierenMod:OLDHairAccessory.DESCRIPTIONS[0]] | 回合抽牌阶段额外 |
| FRIERENMOD-KRAFT_SERPENT_RING.flavor | 克拉伏特送给你的饯别礼。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-AURA_COMPANION.title | 阿乌拉的陪伴 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-AURA_COMPANION.description | 阿乌拉会在战斗中陪伴你。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-AURA_COMPANION.flavor | 阿乌拉，跟上。 | 未找到近似原文 | 整段新增或需人工对照 |
## rest_site_ui.json（2 条）

| ID | 二代当前全文 | 一代对照原文 | 归一化新增/替换片段 |
|---|---|---|---|
| OPTION_FRIEREN_MAGIC_SHOP.description | 选择两件魔具加入魔法牌组，然后在魔法书中使用。占用一次营地行动。 | 选择2张魔具加入魔法牌组 [UIs.json / FrierenMod:BuyPropOption.TEXT[1]] | 两件 / 然后在魔法书中使用占用一次营地行动 |
| OPTION_FRIEREN_MAGIC_SHOP.descriptionDisabled | 需要先完成初始因子选择。 | 未找到近似原文 | 整段新增或需人工对照 |
## settings_ui.json（8 条）

| ID | 二代当前全文 | 一代对照原文 | 归一化新增/替换片段 |
|---|---|---|---|
| FRIERENMOD-ORIGINAL_MUSIC.title | 芙莉莲动漫精选bgm | 芙莉莲 [characters.json / FrierenMod:Frieren.NAMES[0]] | 动漫精选bgm |
| FRIERENMOD-ORIGINAL_MUSIC.hover.desc | 使用芙莉莲动漫精选BGM：事件、常规战斗、Boss与精英、商店及战败。关闭后恢复二代音乐。 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-ACHIEVEMENTS.title | 成就解锁 |  NL #g已解锁！  [UIs.json / FrierenMod:Slot.TEXT[4]] | 成就 |
| FRIERENMOD-ACHIEVEMENTS.hover.desc | 启用芙莉莲成就及卡槽背景解锁。关闭不影响已收藏背景的使用。 | 新卡槽背景解锁！ [UIs.json / FrierenMod:Slot.TEXT[6]] | 启用芙莉莲成就及 / 关闭不影响已收藏背景的使用 |
| FRIERENMOD-BACK_LIBRARY.title | 成就与卡背收藏 | 未找到近似原文 | 整段新增或需人工对照 |
| FRIERENMOD-BACK_LIBRARY_OPEN.title | 查看与选择 |  [%s] 选择中 [UIs.json / FrierenMod:MagicDeckScreen.TEXT[2]] | 查看与 |
| FRIERENMOD-ALLOW_OTHER_ACT_TWO_BOSSES.title | 允许遇到非芙莉莲模组Boss | 欢迎游玩芙莉莲模组！ [UIs.json / FrierenMod:Slot2.TEXT_DICT.0001] | 允许遇到非 / Boss |
| FRIERENMOD-ALLOW_OTHER_ACT_TWO_BOSSES.hover.desc | 关闭时，芙莉莲新跑局的二层Boss固定为阿乌拉，三层固定为复制体。进阶10双Boss中复制体的位置随机。开启后恢复混合Boss池。仅限全员芙莉莲队伍，已生成的跑局不变。 | 未找到近似原文 | 整段新增或需人工对照 |

## 代码与场景中的中文字符串候选

这里还包含异常、日志和开发诊断文字；仅列出候选，不把它们都当成玩家可见文案。注释排除，具体是否显示须按所在调用判断。

| 来源 | 字符串 |
|---|---|
| Scripts/Entry.cs:26 | FrierenMod {typeof(Entry).Assembly.GetName().Version?.ToString(3)}: 已加载。 |
| Scripts/Frieren/Mechanics/ChantCmd.cs:47 | Frieren: 没有可用咏唱槽位；检查装载与各牌堆魔力。 |
| Scripts/Frieren/Mechanics/ChantCmd.cs:81 | Frieren: {option.Slot} {option.SpellId} X={option.Intensity} 支付={option.Cost} |
| Scripts/Frieren/Mechanics/FactorEffects.cs:75 | 因子未结算：{option.SpellId} |
| Scripts/Frieren/Mechanics/FrierenCardAvailability.cs:62 | 跨角色卡池接口发生变化： |
| Scripts/Frieren/Mechanics/MagicDeckCmd.cs:148 | Frieren: 初始槽位 {deck.DraftSlot} 装载 {factor.FactorId} |
| Scripts/Frieren/Mechanics/ManaPayment.cs:25 | 用于代付能量的魔力不足。 |
| Scripts/Frieren/Presentation/ManaAnimationSpeed.cs:103 | 正式版牌堆补间接口改变，需核查咏唱速度 |
| Scripts/Frieren/Presentation/SlotBackStore.cs:31 | FrierenMod 卡背收藏读取失败，保留原存档： |
| Scripts/Frieren/Presentation/SlotBackStore.cs:43 | FrierenMod 卡背收藏保存失败： |
| Scripts/Frieren/UI/MagicBookScreen.cs:498 | Frieren 魔具使用失败： |
| Scripts/Frieren/UI/MagicDeckAnimations.cs:41 | 魔法牌组动画未能启动： |
| Scripts/Frieren/UI/MagicDeckAnimations.cs:144 | 魔法牌组动画未完成： |
| Scripts/Shared/Compatibility/GameApi.cs:52 | 找不到原生消耗特效 |
| Core/Frieren/MagicDeckState.cs:39 | 初始因子选项已失效。 |
| Core/Frieren/MagicDeckState.cs:58 | 魔法因子不存在。 |
| Core/Frieren/MagicDeckState.cs:83 | 魔法牌组条目不存在。 |
| Core/Frieren/MagicDeckState.cs:89 | 魔法因子不存在。 |
| Core/Frieren/MagicDeckState.cs:120 | Magic Deck 存档为空。 |
| Core/Frieren/MagicDeckState.cs:121 | 不支持的 Magic Deck 版本：{save.Version} |
| Core/Frieren/MagicDeckState.cs:123 | 魔法物品稀有度补偿无效。 |
| Core/Frieren/MagicDeckState.cs:125 | Magic Deck 存档包含无效条目。 |
| Core/Frieren/MagicDeckState.cs:134 | Magic Deck 存档包含重复实例或槽位。 |
| Core/Frieren/MagicDeckState.cs:137 | 初始因子选择进度无效。 |
| Core/Frieren/MagicDeckState.cs:140 | 初始因子选项无效。 |
| Core/Frieren/MagicDeckState.cs:142 | 魔具条目无效。 |
| Core/Frieren/MagicDeckState.cs:145 | 因子与魔具实例编号重复。 |
| Core/Frieren/MagicDeckState.cs:147 | 魔法牌组编号计数无效。 |
| Core/Frieren/MagicPropRules.cs:15 | 未知魔具： |
| Core/Frieren/SpellCatalog.cs:42 | 未知魔法因子：{id} |
