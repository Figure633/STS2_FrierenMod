"""从作者提供的一代源码提取卡背与成就原文，保留实际判定与适配问题。"""
from pathlib import Path
import json
import re

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT.parent / "FrierenMod_STS1_source code/src/main"
JAVA = SOURCE / "java/FrierenMod"
RES = SOURCE / "resources/FrierenModResources"
ui = json.loads((RES / "localization/ZHS/UIs.json").read_text(encoding="utf-8"))
titles = ui["FrierenMod:Slot2"]["TEXT_DICT"]
descriptions = ui["FrierenMod:Slot"]["TEXT_DICT"]

actual = {
    "0001": "初始解锁；默认装载到抽牌槽。", "0002": "初始解锁；默认装载到手牌槽。", "0003": "初始解锁；默认装载到弃牌槽。",
    "0004": "芙莉莲第一次打出带 CHANT 标签的牌。", "0005": "芙莉莲第一次打出带 LEGENDARY_SPELL 标签的牌。",
    "0006": "神奇妙妙工具成功交换两个因子的装载位置。", "0007": "芙莉莲复制体正常死亡时解锁。",
    "0008": "芙莉莲进入战败界面，且存在敌人组。", "0009": "进入战败界面时，敌人组中包含 Chosen（被拣选者）；不要求它是最后一击来源。",
    "0010": "进入战败界面时，敌人组中包含芙莉莲复制体。",
    "4001": "芙莉莲打出 LightningMagic。", "4002": "芙莉莲打出 HellFireSummoning。",
    "4003": "打出视觉攻击魔法时，抽牌堆、手牌、弃牌堆各恰好有4张魔力，触发全体即死。",
    "4004": "捕鸟魔法选中鸟类敌人，成功施加眩晕。",
    "4005": "童话的结尾在 use() 中计算出的 damage >= 1000；判定在实际伤害结算之前。",
    "4006": "清洁铜像生锈的魔法即将移除的第一个负面能力正好是 HexPower（邪咒）。",
    "4007": "芙莉莲打出 Fatalism。", "4008": "普通攻击魔法计算出的 damage >= 100；判定在实际伤害结算之前。",
    "4009": "睡衣形态回合结束结算时，抽牌堆、手牌、弃牌堆的魔力合计 >= 30。",
    "4010": "打出寻找遗失饰品的魔法。", "4011": "水星布丁执行 usedUp()；正常流程为战斗胜利时生命 <= 10，消耗布丁并治疗。",
    "4012": "镜莲花戒指的战斗回合计数达到4，生成辛美尔。", "4013": "赞因的笔记成功消耗2个未装载的魔法物品并治疗10点。",
    "4014": "执行 SerieGrimoireAction 创建自定义传说魔法。", "4015": "芙莉莲打出的牌同时具有 LIMIT_OVER_SYNCHRO 与 ACCEL_SYNCHRO 标签。",
    "5001": "芙莉莲回合开始时，三堆魔力总数 >= 100。", "5002": "芙莉莲回合开始时，当前格挡恰好等于999。",
    "5003": "芙莉莲回合开始时，正面能力类型数 >= 5；排除 GetPlayerBlockPower。",
    "5004": "芙莉莲回合开始时，同时持有 HimmelGravestone、StatueOfHimmel、MirroredLotusRing。",
    "5005": "芙莉莲回合开始时，弃牌堆 Laziness 张数 >= 4。", "5006": "芙莉莲回合开始时，抽牌堆 Dazed 张数 >= 8。",
    "5007": "芙莉莲回合开始时，BaseMod.MAX_HAND_SIZE 恰好等于0。",
    "5008": "芙莉莲触发原版 onFinalBossVictoryLogic 钩子时，主牌组张数 >= 40。",
    "5009": "同一Boss胜利钩子中，魔法牌组张数 >= 30。",
    "5010": "同一Boss胜利钩子中，三个装载因子的 magicItemRarity 都是 RARE（稀有）。",
    "5011": "同一Boss胜利钩子中，三个装载因子的 magicItemRarity 都是 COMMON（普通）。",
    "5012": "同一Boss胜利钩子中，CardCrawlGame.playtime >= 7200秒（含恰好2小时）。",
    "5013": "同一Boss胜利钩子中，三个装载因子的 cardID 去重后只有1种。",
    "5014": "同一Boss胜利钩子中，魔法牌组中 UnbelievableTool 数量 >= 5。",
    "5015": "芙莉莲回合开始时，力量能力存在且层数 >= 30。",
    "6001": "宝箱怪死亡时，本场战斗已打出的牌数 <= 5（含5）。", "6002": "复制体正常死亡时，GameActionManager.turn <= 3（含3）。",
    "7001": "芙莉莲回合开始时，三个装载因子都是 BluePrint（蓝图）。",
    "7002": "一代 Factor001Power 回合结束判定检查 Factor001、Factor010、Factor001（重复检查001，未检查100），满足后解锁并击杀敌人。",
    "9001": "打开卡背收藏库时，赠送纪念卡背后，当前已收集数量 >= 50。",
    "9002": "打开卡背收藏库时，当前已收集数量 >= 100；判定在9001之后。",
    "9003": "打开收藏库时，分别至少有1张辛美尔组、海塔组和艾泽组卡背；具体ID集合见备注。",
    "9997": "打开卡背收藏库时自动赠送，无日期限制。", "9998": "打开卡背收藏库时自动赠送，无日期限制。", "9999": "打开卡背收藏库时自动赠送。",
}
notes = {
    "0009": "中文原文为“被被拣选者击败”。二代当前无 Chosen；不自行指定替代敌人。",
    "4005": "原说明“1000点以上”；代码为 >=1000，且是计算伤害，不是扣血后的实际伤害。",
    "4006": "二代有原生 HexPower / Hexed（SpectralKnight会施加），不是缺失内容；两代邪咒机制不同，需适配移除及判定。",
    "4008": "原说明“100点以上”；代码为 >=100，且是计算伤害。",
    "4009": "原说明“30张以上”；代码 >=30。",
    "5001": "原说明“100张以上”；代码 >=100。",
    "5003": "排除的是一代内部用于读取格挡的能力；二代需忽略对应内部实现。",
    "5008": "不是每场普通战斗胜利；原挂钩为 onFinalBossVictoryLogic，二代需选择对应Boss结算入口。",
    "5009": "一代魔法牌组包含因子与魔具，二代应合计 Items 与 Props。",
    "5010": "重要：中文说明写“罕见因子”，源码实际判定 RARE（稀有），二者冲突，需决定沿用说明还是代码。",
    "5012": "原说明“超过2小时”；代码 >=7200秒。",
    "5014": "原说明“拥有5张”，代码是 >=5；包含未装载的魔具。",
    "7001": "不是缺失内容：二代已有 BluePrint 因子及三槽装载机制。",
    "7002": "重要：一代源码漏检100；二代现有谜题要求按槽位和顺序完成三段。需决定是否保留现行正确三段判定。",
    "9001": "间接受限：若54张碎心奖励保持锁定，非碎心卡背总共仅50张，其中9001、9002自身尚未解锁，阈值无法自然达到。",
    "9002": "间接受限：缺少54张碎心奖励时，无法自然收集到100张。",
    "9003": "辛美尔：2007/2008/3002/4012/9003；海塔：4013；艾泽：1008/5015。可经4012、4013、5015达成，不必依赖碎心卡背。",
}

locations = {}
for path in JAVA.rglob("*.java"):
    for number, line in enumerate(path.read_text(encoding="utf-8").splitlines(), 1):
        for id in re.findall(r'unlockANewSlot\("(\d{4})"\)', line):
            locations[id] = f"{path.relative_to(JAVA).as_posix()}:{number}"

ids = sorted(p.stem for p in (RES / "img/UI/slotBg").glob("*.png"))
rows = []
for id in ids:
    group = id[0] if id[0] in "123" else None
    key = id if id in titles else group or "EXCEPTION"
    condition = descriptions.get(id, descriptions.get(key, descriptions["EXCEPTION"]))
    title = titles.get(id, titles.get(key, titles["EXCEPTION"]))
    status = "条件可对接（尚未接入）"
    note = notes.get(id, "保留原触发逻辑；成就系统尚在移植，不能视为已经实装。")
    if group:
        asc = {"1": "任何进阶等级", "2": "进阶 >= 10", "3": "进阶恰好等于20"}[group]
        rule = f"芙莉莲击败 CorruptHeart 时，{asc}，从本组尚未解锁的卡背中随机解锁1张；组内18张，集齐后不重复。高进阶可同时获得较低组奖励。"
        status = "内容暂缺（待决定）"
        note = "二代当前没有腐化之心，最高进阶为10，原版A20条件不可达；54张卡背的替代条件均待作者决定。" if group == "3" else "二代当前没有腐化之心；原碎心奖励入口不可达，暂不改变条件。"
        location = "patches/AchievementPatch.java:23-36；gameHelpers/SlotBgHelper.java:177-193"
    else:
        rule = actual[id]
        location = locations.get(id, "gameHelpers/SlotBgHelper.java:19-20（默认已解锁）")
    if id == "0009": status = "内容暂缺（待决定）"
    if id in ("9001", "9002"): status = "受暂缺内容影响（待决定）"
    if id in ("5010", "7002"): status = "原说明或实现冲突（待决定）"
    rows.append(dict(id=id, title=title, image=str((RES / f"img/UI/slotBg/{id}.png").resolve()), description=condition,
                     actual=rule, status=status, note=note, decision="", source=location))

assert len(rows) == 104 and len({r['id'] for r in rows}) == 104
assert sum(r['status'] == '内容暂缺（待决定）' for r in rows) == 55
out = ROOT / 'artifacts/achievement-review/records.json'
out.parent.mkdir(parents=True, exist_ok=True)
out.write_text(json.dumps(rows, ensure_ascii=False, indent=2), encoding='utf-8')
print(f'RECORDS_OK total={len(rows)} direct_missing=55 indirect=2 rule_conflicts=2')
