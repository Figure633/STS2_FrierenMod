"""从已核对的规则生成咏唱移植的中英文本；名称和素材沿用本地一代源码。"""
import json
import re
import csv
import os
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = Path(os.environ.get("FRIEREN_STS1_SOURCE", ROOT.parents[1] / "references/FrierenMod_STS1")) / "src/main/resources/FrierenModResources"

# 一代英文长标题在二代单行标题栏会缩至 10–15 号，保留名称含义并缩短展示。
SHORT_ENGLISH_TITLES = {
    "FlowerFieldSpell": "Flower Field Spell", "HighSpeedMovement": "High-Speed Movement",
    "LightningMagic": "Destructive Lightning", "MetallicizFlower": "Metallicize Flower",
    "NatureBeauty": "Nature's Beauty", "OilSpell": "Oil-Cleaning Spell",
    "PaperPlaneSpell": "Paper Plane Spell", "SecondDefensiveMagic": "Defensive Follow-up",
}

EFFECTS = {
    "AdvancedDefensiveMagic": ("获得 {Reward} 点格挡两次。", "Gain {Reward} Block twice."),
    "BarrierMagic": ("获得 {Reward} 点不受敏捷影响的格挡与 {Reward} 层模糊。", "Gain {Reward} Block unaffected by Dexterity and {Reward} Blur."),
    "BePrepared": ("获得 {Reward} 层活力。", "Gain {Reward} Vigor."),
    "BigFire": ("将 {Reward} 张小小火加入此槽位的牌堆。", "Add {Reward} Little Fires to this slot's pile."),
    "BloodSpell": ("失去 {Reward} 点生命。", "Lose {Reward} HP."),
    "BluePrint": ("复制下一个槽位的因子。连续蓝图沿槽位循环查找；空槽或全蓝图无法发动。", "Copy the next slot's factor, following chained blueprints. An empty slot or three blueprints cannot activate."),
    "BuildUpBody": ("获得 {Reward} 点力量。", "Gain {Reward} Strength."),
    "GreatMageComposure": ("本回合接下来 {Reward} 次咏唱不消耗魔力。", "The next {Reward} Chants this turn consume no Mana."),
    "GrossedOutLook": ("所有敌人在本回合失去 {Reward} 点力量。", "All enemies lose {Reward} Strength this turn."),
    "HighSpeedMovement": ("获得 {Reward} 点敏捷。", "Gain {Reward} Dexterity."),
    "Hypnosis": ("给予所有敌人 {Cost} 层虚弱与易伤。", "Apply {Cost} Weak and Vulnerable to ALL enemies."),
    "IceArrows": ("所有敌人失去 {Reward} 点生命。", "ALL enemies lose {Reward} HP."),
    "TransferToAlchemist": ("给予所有敌人 {Reward} 层中毒。", "Apply {Reward} Poison to ALL enemies."),
    "ImpeccableIntangible": ("获得 {Reward} 层无实体。", "Gain {Reward} Intangible."),
    "MeleeMage": ("本场战斗中，名称含“打击”的牌伤害增加 {Reward}。", "Strikes deal {Reward} additional damage this combat."),
    "MetallicizFlower": ("获得 {Reward} 层金属化：回合结束时获得等量格挡。", "Gain {Reward} Metallicize: gain that much Block at the end of your turn."),
    "NatureBeauty": ("下回合获得 {Reward} 点能量。", "Gain {Reward} Energy next turn."),
    "PassOnTheTorch": ("将 {Reward} 张魔力加入下一个槽位的牌堆。", "Add {Reward} Mana to the next slot's pile."),
    "PeacefulDays": ("抽 {Reward} 张牌。", "Draw {Reward} cards."),
    "PinchAndScrape": ("接下来 {Reward} 次同调将魔力弃置而非消耗。", "The next {Reward} Synchros discard their Mana instead of Exhausting it."),
    "Sleeeep": ("下回合多抽 {Reward} 张牌。", "Draw {Reward} additional cards next turn."),
    "SpellBoost": ("接下来 {Reward} 个回合中，攻击伤害翻倍。", "Deal double attack damage during the next {Reward} turns."),
    "Strategizing": ("获得 {Reward} 点能量。", "Gain {Reward} Energy."),
    "TheLivingLegend": ("将 {Reward} 张随机传奇魔法加入此槽位的牌堆。", "Add {Reward} random Legendary Spells to this slot's pile."),
    "Transcendence": ("获得 {Reward} 层真言。每满 10 层进入神格并获得 3 能量。", "Gain {Reward} Mantra. Every 10 stacks grant Divinity and 3 Energy."),
}
# 谜题沿用一代的碎片线索；卡面和悬停提示不解释解法或结算效果。
PUZZLE_CLUES = {
    "Factor100": ("……[color=#e1c6ed]芙莉莲[/color]……", "... [color=#e1c6ed]Frieren[/color] ..."),
    "Factor010": ("……[color=#FFFE91]消灭[/color]……", "... [color=#FFFE91]kill[/color] ..."),
    "Factor001": ("……[color=#ed1c24]怪物[/color]……", "... [color=#ed1c24]monster[/color] ..."),
}
EFFECTS.update(PUZZLE_CLUES)

CARDS = {
    "RapidChant": ("咏唱 1。\n将一张魔力随机加入抽牌堆。", "Chant 1.\nShuffle 1 Mana into your Draw Pile."),
    "DefensiveMagic": ("咏唱 1。获得 {Block:diff()} 点格挡。\n将一张{Upgraded:cond:升级的|}额外的防御魔法加入手牌。", "Chant 1. Gain {Block:diff()} Block.\nAdd {Upgraded:cond:an upgraded|a} Defensive Follow-up to your Hand."),
    "ContinualChant": ("传奇魔法（本回合咏唱后可用）。将 {Intensity} 张魔力随机加入抽牌堆，然后咏唱 {Intensity}。", "Legendary Spell (requires a Chant this turn). Shuffle {Intensity} Mana into your Draw Pile, then Chant {Intensity}."),
    "FlowerFieldSpell": ("咏唱 2。将一张{Upgraded:cond:升级的|}苍月草加入手牌。", "Chant 2. Add {Upgraded:cond:an upgraded|a} Blue Moon Weed to your Hand."),
    "BlueMoonWeed": ("抽 {Cards} 张牌。", "Draw {Cards} cards."),
    "ManaBarricade": ("咏唱 2。若发动抽牌堆槽位，获得 {Block:diff()} 点格挡。", "Chant 2. If you activate the Draw Pile slot, gain {Block:diff()} Block."),
    "LureTheEnemyInDeep": ("咏唱 {Intensity}。获得 {Intensity} 点能量。若发动手牌槽位，将本牌移回手牌。", "Chant {Intensity}. Gain {Intensity} Energy. If you activate the Hand slot, return this card to your Hand."),
    "PerfectDefensiveMagic": ("咏唱 3。获得 {Block:diff()} 点格挡和 1 层缓冲。", "Chant 3. Gain {Block:diff()} Block and 1 Buffer."),
    "TrueColours": ("咏唱 4。抽 3 张牌，将 3 张魔力加入手牌。获得 3 能量，并向抽牌堆和弃牌堆各加入 3 张魔力。", "Chant 4. Draw 3 cards and add 3 Mana to your Hand. Gain 3 Energy and add 3 Mana to both your Draw and Discard Piles."),
    "FullAhead": ("每回合首次发动因子时，获得 1 力量和 1 敏捷。", "The first time you activate a factor each turn, gain 1 Strength and 1 Dexterity."),
    "Simmering": ("每次发动因子时，从抽牌堆抽取 1 张传奇魔法。", "Whenever you activate a factor, draw 1 Legendary Spell from your Draw Pile."),
    "HalfTheSky": ("造成 {Damage:diff()} 点伤害。发动因子时，将本牌从弃牌堆移回手牌。", "Deal {Damage:diff()} damage. Whenever you activate a factor, return this card from your Discard Pile to your Hand."),
    "Confrontation": ("获得 {Block:diff()} 点格挡。本牌在手牌时，每打出一张咏唱牌，使本牌本场战斗的基础格挡增加 3。", "Gain {Block:diff()} Block. Each Chant card played while this is in your Hand increases its base Block by 3 this combat."),
    "Imagination": ("本回合，咏唱不消耗魔力，无法生成魔力。\n普通卡牌改用魔力支付费用。", "This turn, Chants are free and you cannot generate Mana.\nPay normal card costs with Mana."),
    "PreparedPosture": ("造成 {Damage:diff()} 点伤害。从抽牌堆抽取 1 张咏唱牌。", "Deal {Damage:diff()} damage. Draw 1 Chant card from your Draw Pile."),
    "FolkGrimoire": ("将随机咏唱牌与随机传奇魔法各 1 张加入手牌。本回合它们的费用减少 {Discount}。", "Add a random Chant card and a random Legendary Spell to your Hand. They cost {Discount} less this turn."),
    "HellFireSummoning": ("[gold]封印[/gold]。\n造成 {Damage:diff()} 点伤害，次数为本回合打出咏唱牌数量的两倍。\n每次打出后，本回合费用增加 1。", "[gold]Sealed[/gold].\nDeal {Damage:diff()} damage twice per Chant card played this turn.\nCosts 1 more this turn after each play."),
    "Famehameha": ("传奇魔法（本回合咏唱后可用）。造成 {Damage:diff()} 点伤害。每次发动因子，使本牌本回合费用减少 1。", "Legendary Spell. Requires a Chant this turn. Deal {Damage:diff()} damage. Each factor activation reduces this card's cost by 1 this turn."),
    "FairyTaleEnd": ("传奇魔法（本回合咏唱后可用）。造成 {Damage:diff()} 点伤害。\n永久牌组中的每种传奇魔法使基础伤害翻倍（已计入）。", "Legendary Spell. Requires a Chant this turn. Deal {Damage:diff()} damage.\nEach distinct Legendary Spell in your deck doubles base damage (included)."),
    "HotTeaSpell": ("传奇魔法（本回合咏唱后可用）。获得 {Block:diff()} 点格挡，再获得等于三牌堆魔力总数的格挡。", "Legendary Spell. Requires a Chant this turn. Gain {Block:diff()} Block plus Block equal to your total Mana."),
    "MoldSpell": ("传奇魔法（本回合咏唱后可用）。造成 {Damage:diff()} 点伤害 {Hits} 次。", "Legendary Spell. Requires a Chant this turn. Deal {Damage:diff()} damage {Hits} times."),
    "OilSpell": ("传奇魔法（本回合咏唱后可用）。对所有敌人造成 {Damage:diff()} 点伤害。若没有击杀，再对所有敌人造成 {SecondDamage} 点伤害。", "Legendary Spell. Requires a Chant this turn. Deal {Damage:diff()} damage to ALL enemies. If none die, deal {SecondDamage} damage to ALL enemies."),
    "PaperPlaneSpell": ("传奇魔法（本回合咏唱后可用）。每连续同调 4 次，使接下来 1 张攻击牌伤害翻倍。", "Legendary Spell. Requires a Chant this turn. Every 4 consecutive Synchros make your next Attack deal double damage."),
    "RustCleanMagic": ("传奇魔法（本回合咏唱后可用）。移除目标首个增益（首领战除外），并移除自身首个减益。", "Legendary Spell. Requires a Chant this turn. Remove the target's first Buff (except in boss fights) and your first Debuff."),
    "LightningMagic": ("传奇魔法（本回合咏唱后可用）。将抽牌堆和弃牌堆的所有魔力移入手牌。本场战斗，同调改为对随机敌人造成 15 点伤害，不再抽牌。", "Legendary Spell. Requires a Chant this turn. Move ALL Mana from Draw and Discard to Hand. This combat, Synchro deals 15 damage to a random enemy instead of drawing."),
    "LittleFire": ("造成 {Damage:diff()} 点伤害。使本场战斗中未消耗的小小火基础伤害增加 2，本牌本回合费用增加 1。同调时从弃牌堆返回手牌。", "Deal {Damage:diff()} damage. All unexhausted Little Fires gain 2 base damage this combat. This costs 1 more this turn. Return from Discard to Hand on Synchro."),
}

POWERS = {
    "ConcentrationPower": ("专注", "Concentration", "咏唱魔力消耗减少 {Amount}。打出非魔力、非速攻牌后减少 1 层。", "Chants cost {Amount} less Mana. Lose 1 after playing a non-Mana, non-Speed card."),
    "WeakenedChantPower": ("咏唱削弱", "Weakened Chant", "本回合咏唱魔力消耗增加 {Amount}。", "Chants cost {Amount} more Mana this turn."),
    "ChantWithoutManaPower": ("无耗咏唱", "Free Chant", "本回合咏唱不消耗魔力。", "Chants consume no Mana this turn."),
    "ChantWithoutManaTimesPower": ("从容咏唱", "Composed Chant", "本回合接下来 {Amount} 次咏唱不消耗魔力。终极咏唱只消耗 1 次。", "Your next {Amount} Chants this turn consume no Mana. Final Chant uses one charge."),
    "BanManaGainPower": ("无法生成魔力", "No Mana Generation", "本回合无法生成魔力。", "You cannot generate Mana this turn."),
    "ManaInsteadOfEnergyPower": ("魔力代付", "Mana Payment", "普通卡牌显示 0 能量，实际消耗原费用对应的魔力（抽牌堆→手牌→弃牌堆）。魔力不足无法打出。X 费牌仍使用能量。", "Normal cards show 0 Energy but consume Mana equal to their cost (Draw, Hand, Discard). Insufficient Mana prevents play. X-cost cards still use Energy."),
    "SynchroWithoutManaPower": ("节俭", "Frugality", "接下来 {Amount} 次同调将魔力弃置而非消耗。", "The next {Amount} Synchros discard their Mana instead of Exhausting it."),
    "StrikeUpPower": ("近战魔法使", "Melee Mage", "打击伤害增加 {Amount}。", "Strikes deal {Amount} additional damage."),
    "MetallicizePower": ("金属化", "Metallicize", "回合结束获得 {Amount} 格挡。", "Gain {Amount} Block at the end of your turn."),
    "PhantasmalPower": ("幻影", "Phantasmal", "接下来 {Amount} 个回合获得双倍攻击伤害。", "Deal double attack damage during the next {Amount} turns."),
    "RestoreStrengthPower": ("力量恢复", "Strength Recovery", "回合结束恢复 {Amount} 力量。", "Regain {Amount} Strength at turn end."),
    "MantraPower": ("真言", "Mantra", "达到 10 层时消耗 10 层，进入神格并获得 3 能量。", "At 10, consume 10 to enter Divinity and gain 3 Energy."),
    "DivinityPower": ("神格", "Divinity", "攻击伤害为三倍，持续至下回合。", "Deal triple attack damage until your next turn."),
    "PuzzlePower": ("？？？", "???", "……", "..."),
    "FullAheadPower": ("全速前进", "Full Ahead", "每回合首次发动因子时获得 {Amount} 力量和敏捷。", "The first factor activation each turn grants {Amount} Strength and Dexterity."),
    "ThunderPower": ("雷电魔法", "Lightning Magic", "同调对随机敌人造成 15 点伤害，不再抽牌。", "Synchro deals 15 damage to a random enemy instead of drawing."),
    "SimmeringPower": ("蓄势待发", "Simmering", "每次发动因子时，从抽牌堆抽取 {Amount} 张传奇魔法。", "Each factor activation draws {Amount} Legendary Spells from your Draw Pile."),
    "PaperPlanePower": ("纸飞机", "Paper Plane", "每连续同调 4 次，获得 {Amount} 次攻击伤害翻倍。数字显示剩余强化攻击次数。", "Every 4 consecutive Synchros empower {Amount} Attacks with double damage. The number shows remaining empowered attacks."),
    "SealPower": ("封印", "Seal", "单回合打出 5 张咏唱牌后，将封印的卡牌加入手牌。", "After playing 5 Chant cards in one turn, add sealed cards to your Hand."),
}

UI = {
    "BOOK": ("魔法书 · 因子", "Magic Book · Factors"), "CLOSE": ("关闭", "Close"),
    "MANA": ("魔力", "Mana"), "COST_ONE": ("咏唱 1 需魔力", "Mana cost at Chant 1"),
    "MANA_HINT": ("青色数字表示可发动咏唱 1。\n因子详情可在魔法书中查看。", "A teal number means Chant 1 is available.\nSee the Magic Book for factor details."),
    "SLOT_UNAVAILABLE": ("当前槽位无法发动咏唱。", "This slot cannot Chant."),
    "SLOT_SHORT.Draw": ("抽", "Draw"), "SLOT_SHORT.Hand": ("手", "Hand"), "SLOT_SHORT.Discard": ("弃", "Disc."),
    "BOOK_HINT": ("选择槽位装载因子；原因子保留在库存。仅非战斗时可调整。", "Equip factors by selecting a slot. Replaced factors stay in inventory. Edit outside combat."),
    "BOOK_LOCKED": ("当前仅可查看。初始选择完成后，单人非战斗状态可调整装载。", "View only. Editing is available after initial selection, outside combat in solo runs."),
    "UNEQUIPPED": ("未装载", "Unequipped"), "EMPTY": ("未装载因子", "Empty factor slot"),
    "SLOT.Draw": ("抽牌堆", "Draw Pile"), "SLOT.Hand": ("手牌", "Hand"), "SLOT.Discard": ("弃牌堆", "Discard Pile"),
    "CHANT": ("{Slot} · 消耗 {Cost} 张魔力", "{Slot} · Consume {Cost} Mana"),
    "INITIAL": ("装载至{Slot}", "Equip to {Slot}"),
    "COLLECT": ("魔法因子 · 未装载", "Magic factor · Unequipped"),
    "FACTOR_REWARD": ("魔法因子（三选一）", "Magic Factor (choose one of three)"),
    "TIP.CHANT.title": ("咏唱", "Chant"),
    "TIP.CHANT.description": ("X 表示咏唱强度。选择一个可用槽位，消耗其牌堆中的魔力并发动因子。费用受专注等效果影响，选择时显示实际消耗。", "X is Chant intensity. Choose an available slot and consume Mana from its pile to activate its factor. Concentration and other effects modify the cost; selection shows the actual amount."),
    "TIP.LEGENDARY.title": ("传奇魔法", "Legendary Spell"),
    "TIP.LEGENDARY.description": ("本回合打出过咏唱牌后，才能打出传奇魔法。", "Playable after you have played a Chant card this turn."),
    "PUZZLE_MATCH": ("！", "@"), "PUZZLE_MISS": ("？", "?"),
    "TIP.DIVINITY.title": ("真言与神格", "Mantra & Divinity"),
    "TIP.DIVINITY.description": ("每满 10 层真言，消耗 10 层，进入神格并获得 3 能量。神格使攻击伤害变为三倍，持续至下回合。", "Every 10 Mantra are consumed to enter Divinity and gain 3 Energy. Divinity triples attack damage until your next turn."),
    "TIP.MANA_PAYMENT.title": ("魔力代付", "Mana Payment"),
    "TIP.MANA_PAYMENT.description": ("普通卡牌按原能量费用支付等量魔力，依次从抽牌堆、手牌、弃牌堆消耗。魔力不足时无法打出。X 费牌仍消耗能量。", "Pay Mana equal to the normal Energy cost, from Draw, Hand, then Discard. Insufficient Mana prevents play. X-cost cards still consume Energy."),
    "TIP.SEALED.title": ("封印", "Sealed"),
    "TIP.SEALED.description": ("战斗开始时移出牌堆。单回合打出 5 张咏唱牌后，解除封印并加入手牌。", "Removed from the pile at combat start. Play 5 Chant cards in one turn to release it into your Hand."),
    "TIP.SECOND_DEFENSE.title": ("额外的防御魔法", "Defensive Follow-up"),
    "TIP.SECOND_DEFENSE.description": ("0 能量，消耗。咏唱 1，获得与生成它的防御魔法相同的格挡；生成牌保留对应升级。", "Costs 0 Energy and Exhausts. Chant 1 and gain the same Block as the Defensive Magic that created it, retaining its upgrade."),
}

def key(name):
    return "FRIERENMOD-" + re.sub(r"(?<=[a-z0-9])(?=[A-Z])", "_", name).upper()

# 通用预览说明强度关系；实际咏唱选牌仍显示本次结算数值。
PREVIEW_EFFECTS = {
    "BePrepared": ("获得 X 层活力，额外获得 {Bonus} 层。", "Gain X Vigor, plus {Bonus} more."),
    "GrossedOutLook": ("所有敌人本回合失去 X 点力量，额外失去 {Bonus} 点。", "ALL enemies lose X Strength this turn, plus {Bonus} more."),
    "MeleeMage": ("本场战斗中，打击牌的伤害增加 X，再额外增加 {Bonus}。", "Strikes deal X additional damage this combat, plus {Bonus} more."),
    "BloodSpell": ("失去 X 点生命，额外失去 {Bonus} 点。", "Lose X HP, plus {Bonus} more."),
    "IceArrows": ("所有敌人失去咏唱强度 {Multiplier} 倍的生命。", "ALL enemies lose HP equal to {Multiplier} times the Chant intensity."),
    "TransferToAlchemist": ("给予所有敌人中毒，层数为咏唱强度的 {Multiplier} 倍。", "Apply Poison to ALL enemies equal to {Multiplier} times the Chant intensity."),
    "PinchAndScrape": ("获得节俭，层数为咏唱强度的 {Multiplier} 倍。同调时消耗 1 层，将魔力弃置而非消耗。", "Gain Frugality equal to {Multiplier} times the Chant intensity. Each Synchro uses one stack to discard its Mana instead of Exhausting it."),
    "Hypnosis": ("给予所有敌人虚弱与易伤，层数等于本次消耗的魔力。", "Apply Weak and Vulnerable to ALL enemies equal to the Mana consumed.")
}

def preview(name, attrs, index):
    effect = PREVIEW_EFFECTS.get(name, EFFECTS[name])[index]
    effect = effect.replace("{Reward}", "X").replace("{Bonus}", str(attrs.get("rewardOffset", 0))).replace("{Multiplier}", str(attrs.get("reward", 1)))
    if name == "BluePrint":
        return effect
    mult, offset = attrs.get("cost", 1), attrs.get("offset", 0)
    if index == 0:
        cost = "消耗 X 魔力" if mult == 1 else f"每点强度消耗 {mult} 魔力"
        if offset > 0:
            cost += f"，另耗 {offset}"
        elif offset < 0:
            cost += f"，少耗 {-offset}（最低 0）"
        return f"咏唱 X：{cost}。\n{effect}"
    cost = "consume X Mana" if mult == 1 else f"{mult} Mana per intensity"
    if offset > 0:
        cost += f", plus {offset} more"
    elif offset < 0:
        cost += f", reduced by {-offset} (minimum 0)"
    return f"Chant X: {cost}.\n{effect}"

def main():
    catalog = (ROOT / "Core/Frieren/SpellCatalog.cs").read_text(encoding="utf8")
    factors = {}
    for name, args in re.findall(r'F\("(\w+)"([^)]*)\)', catalog):
        attrs = dict((k, int(v)) for k, v in re.findall(r'(cost|offset|reward|rewardOffset):\s*(-?\d+)', args))
        factors[name] = attrs
    assert set(factors) == set(EFFECTS)
    for lang, original, index in [("zhs", "ZHS", 0), ("eng", "ENG", 1)]:
        directory = ROOT / "FrierenMod/localization" / lang
        originals = json.loads((SOURCE / f"localization/{original}/cards.json").read_text(encoding="utf8"))
        cards = json.loads((directory / "cards.json").read_text(encoding="utf8"))
        texts = {k: v[index] for k, v in UI.items()}
        for name, values in CARDS.items():
            cards[key(name) + ".title"] = originals.get("FrierenMod:" + name, {}).get("NAME", name)
            cards[key(name) + ".description"] = values[index].replace("传奇魔法（本回合咏唱后可用）。", "[gold]传奇魔法[/gold]。\n").replace("Legendary Spell (requires a Chant this turn). ", "[gold]Legendary Spell[/gold].\n").replace("Legendary Spell. Requires a Chant this turn. ", "[gold]Legendary Spell[/gold].\n")
        for name, attrs in factors.items():
            cards[key(name) + ".title"] = originals["FrierenMod:" + name]["NAME"]
            cards[key(name) + ".description"] = "{Context}\n{Effect}"
            effect = EFFECTS[name][index]
            texts["EFFECT." + name] = "{PuzzleMark}" if name in PUZZLE_CLUES else effect
            texts["FACTOR." + name] = preview(name, attrs, index)
        if index == 1:
            for name, title in SHORT_ENGLISH_TITLES.items():
                cards[key(name) + ".title"] = title
        powers = {}
        for name, (zhname, enname, zhdesc, endesc) in POWERS.items():
            powers[key(name) + ".title"] = (zhname, enname)[index]
            powers[key(name) + ".description"] = (zhdesc, endesc)[index]
        original_powers = json.loads((SOURCE / f"localization/{original}/powers.json").read_text(encoding="utf8"))
        for stage, name in enumerate(PUZZLE_CLUES, start=1):
            clue = original_powers["FrierenMod:" + name + "Power"]
            powers[key("PuzzlePower") + f".{stage}.title"] = clue["NAME"]
            powers[key("PuzzlePower") + f".{stage}.description"] = clue["DESCRIPTIONS"][0]
        for table, data in [("cards", cards), ("frieren", texts), ("powers", powers)]:
            (directory / f"{table}.json").write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf8")
        relics = json.loads((directory / "relics.json").read_text(encoding="utf8"))
        relics[key("HolyEmblem") + ".description"] = (
            "开局为三个槽位各选择一个初始魔法因子。每回合抽牌后，将一张魔力随机加入抽牌堆，并将一张魔力加入弃牌堆。非战斗时可通过右上角魔法书调整因子。",
            "At the start of a run, choose an initial factor for each slot. After drawing each turn, add one Mana to your Draw Pile and one to your Discard Pile. Equip factors outside combat using the Magic Book."
        )[index]
        (directory / "relics.json").write_text(json.dumps(relics, ensure_ascii=False, indent=2) + "\n", encoding="utf8")
    # 同步一代内容盘点，避免仍把已经接通的因子列为未移植。
    inventory = ROOT / "docs/content_inventory.csv"
    entries = list(csv.DictReader(inventory.read_text(encoding="utf-8-sig").splitlines()))
    implemented = set(factors) | set(CARDS) | set(POWERS) | {"HolyEmblem", "RapidChant", "DefensiveMagic", "PreciseChant", "FinalChant", "ManaExpansion", "Mana", "Flow"}
    for entry in entries:
        if entry["class"] in implemented:
            entry["status"] = "chant_port_tested" if entry["class"] != "PaperPlaneSpell" else "source_disabled_test_model"
    with inventory.open("w", encoding="utf8", newline="") as stream:
        writer = csv.DictWriter(stream, fieldnames=entries[0].keys())
        writer.writeheader(); writer.writerows(entries)
    print(f"Chant content: {len(factors)} factors, {len(CARDS)} related cards, {len(POWERS)} powers")

if __name__ == "__main__":
    main()
    from prop_content import main as write_props
    write_props()
