"""落实2026-11-06作者改稿的本地化与新增素材，不生成或覆盖作者需求文档。"""
from pathlib import Path
import json
import re
import shutil
import hashlib
from PIL import Image, ImageOps

ROOT = Path(__file__).resolve().parents[1]
BACKUP = ROOT/'artifacts/author-review-20261106/input'
BACKUP.mkdir(parents=True,exist_ok=True)
for relative in ['TODOs.docx','卡名对照表.xlsx','FrierenMod/localization/zhs/powers.json','FrierenMod/localization/zhs/relics.json']:
    target=BACKUP/relative
    if not target.exists():
        target.parent.mkdir(parents=True,exist_ok=True)
        shutil.copy2(ROOT/relative,target)

def update(language,table,changes):
    path=ROOT/f'FrierenMod/localization/{language}/{table}.json'
    data=json.loads(path.read_text(encoding='utf-8-sig'))
    data.update(changes)
    path.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')

update('zhs','frieren',{
 'CRAFT.RetainAll':'保留你的全部手牌{Amount}回合。',
 'CRAFT.RandomCard':'随机生成{Amount}张芙莉莲牌加入[gold]手牌[/gold]，本回合费用为0。',
 'CRAFT.DamageQuad':'造成{Amount}点伤害4次。',
 'CRAFT.DamageSeven':'造成{Amount}点伤害7次。',
 'CRAFT.DoubleStats':'力量与敏捷翻倍。[gold]消耗[/gold]。',
 'CRAFT.ReduceStrength':'所有敌人失去{Amount}点力量。[gold]消耗[/gold]。',
 'FACTOR.GrossedOutLook':'[gold]咏唱[/gold] X：所有敌人本回合失去X+1点力量。',
 'FACTOR.BlackGoldWings':'[gold]咏唱[/gold] 2X：对所有敌人造成6X点伤害。获得6X点格挡。',
 'EFFECT.BlackGoldWings':'对所有敌人造成{Damage:diff()}点伤害。获得{Block:diff()}点格挡。',
 'SEALING_ORE.REMOVE':'选择一张牌移除。',
 'SEALING_ORE.MERCHANT':'嚯嚯，终于买到封魔石啦，标价1000金币！',
})
update('eng','frieren',{
 'CRAFT.RetainAll':'Retain your entire hand for {Amount} turns.',
 'CRAFT.RandomCard':'Add {Amount} random Frieren cards to your [gold]hand[/gold]. They cost 0 this turn.',
 'CRAFT.DamageQuad':'Deal {Amount} damage 4 times.', 'CRAFT.DamageSeven':'Deal {Amount} damage 7 times.',
 'CRAFT.DoubleStats':'Double your Strength and Dexterity. [gold]Exhaust[/gold].',
 'CRAFT.ReduceStrength':'ALL enemies lose {Amount} Strength. [gold]Exhaust[/gold].',
 'FACTOR.GrossedOutLook':'[gold]Chant[/gold] X: ALL enemies lose X+1 Strength this turn.',
 'FACTOR.BlackGoldWings':'[gold]Chant[/gold] 2X: Deal 6X damage to ALL enemies. Gain 6X Block.',
 'EFFECT.BlackGoldWings':'Deal {Damage:diff()} damage to ALL enemies. Gain {Block:diff()} Block.',
 'SEALING_ORE.REMOVE':'Choose a card to remove.',
 'SEALING_ORE.MERCHANT':'Ho ho, finally some anti-magic stone! Priced at 1,000 Gold!',
})
for language,name,description,ore,ore_description,royal in [
 ('zhs','他山之石：黑金之翼魔法','{Context}{Effect}','封魔矿','使用后消耗。\n持有时无法[gold]咏唱[/gold]。\n抵达商店时自动出售，获得300金币。','从所有可获取因子中挑选1个加入魔法牌组。\n委托专属魔具，使用后移除。'),
 ('eng','Black-and-Gold Wings','{Context}{Effect}','Anti-Magic Ore','Consumed on use.\nYou cannot [gold]Chant[/gold] while carrying this.\nAutomatically sold for 300 Gold upon reaching a merchant.','Choose 1 obtainable factor to add to your magic deck.\nCommission-exclusive magic tool. Consumed on use.')]:
 update(language,'cards',{
  'FRIERENMOD-BLACK_GOLD_WINGS.title':name,'FRIERENMOD-BLACK_GOLD_WINGS.description':description,
  'FRIERENMOD-SEALING_ORE.title':ore,'FRIERENMOD-SEALING_ORE.description':ore_description,
  'FRIERENMOD-ROYAL_GRIMOIRE.description':royal,
 })
update('zhs','cards',{'FRIERENMOD-FRIEREN_THE_SLAYER.description':'每回合开始时，挑选1张本场战斗未选过的{Upgraded:cond:升级的|}“魔法”加入[gold]手牌[/gold]。'})
update('eng','cards',{'FRIERENMOD-FRIEREN_THE_SLAYER.description':'At the start of each turn, choose 1 {Upgraded:cond:upgraded |}spell you have not chosen this combat and add it to your [gold]hand[/gold].'})
update('zhs','powers',{
 'FRIERENMOD-OPEN_THE_WAYGATE_POWER.description':'一回合内[gold]连续同调[/gold]{Threshold}张[gold]魔力[/gold]后，结束你的回合并额外获得一回合，失去一层此能力。当前连续同调{Consecutive}次。',
 'FRIERENMOD-SEALING_ORE_POWER.title':'封魔矿','FRIERENMOD-SEALING_ORE_POWER.description':'你无法[gold]咏唱[/gold]。',
 'FRIERENMOD-AURA_SUBMISSION_POWER.description':'下回合失去[blue]{Amount}[/blue]点能量。阿乌拉即将发动断头台。',
 'FRIERENMOD-AURA_SCALES_POWER.description':'每3回合，比较双方[gold]魔力[/gold]。若[red]阿乌拉[/red]更高：你下回合失去{EnergyLoss}点能量，她下次必定攻击。若你更高：她[gold]眩晕[/gold]1回合，随后将生命与生命上限降至1，并将失去的生命转为[gold]魔力[/gold]。平局无效果。',
 'FRIERENMOD-FRIEREN_THE_SLAYER_POWER.description':'每回合开始时，从本场战斗未选过的“魔法”中挑选{Amount}张加入[gold]手牌[/gold]。{UpgradedChoices:cond:>0?其中{}张升级。|}',
})
update('eng','powers',{
 'FRIERENMOD-SEALING_ORE_POWER.title':'Anti-Magic Ore','FRIERENMOD-SEALING_ORE_POWER.description':'You cannot [gold]Chant[/gold].',
 'FRIERENMOD-AURA_SUBMISSION_POWER.description':'Lose [blue]{Amount}[/blue] Energy next turn. Aura is about to use Guillotine.',
 'FRIERENMOD-AURA_SCALES_POWER.description':'Every 3 turns, compare [gold]Mana[/gold]. If Aura has more, you lose {EnergyLoss} Energy next turn and she attacks next. If you have more, she is [gold]Stunned[/gold] for 1 turn, then her HP and Max HP become 1 and the lost HP becomes Mana. Ties have no effect.',
 'FRIERENMOD-FRIEREN_THE_SLAYER_POWER.description':'At the start of each turn, choose {Amount} spells not yet chosen this combat to add to your [gold]hand[/gold]. {UpgradedChoices:cond:>0?{} of them are upgraded.|}',
})
update('zhs','events',{
 'FRIERENMOD-FOOD_EVENT.pages.INITIAL.options.FREE.description':'回复[green]15[/green]点生命。',
 'FRIERENMOD-FOOD_EVENT.pages.INITIAL.options.MEAL.description':'支付[gold]150[/gold]金币，获得[green]水星布丁[/green]，回复[green]{MealHeal}[/green]点生命。',
 'FRIERENMOD-FOOD_EVENT.pages.INITIAL.options.MEAL_STRAWBERRY.title':'我吃',
 'FRIERENMOD-FOOD_EVENT.pages.INITIAL.options.MEAL_STRAWBERRY.description':'支付[gold]150[/gold]金币，获得[green]草莓[/green]，回复[green]{MealHeal}[/green]点生命。',
 'FRIERENMOD-FOOD_EVENT.pages.INITIAL.options.FEAST.description':'支付[gold]250[/gold]金币。获得[green]芒果[/green]，回复全部生命。',
 'FRIERENMOD-SEALING_ORE_MINE.title':'封魔石矿山',
 'FRIERENMOD-SEALING_ORE_MINE.pages.INITIAL.description':'你来到了一片遍布封魔石的矿山。封魔石极其珍贵，所有商人都会像[jitter]疯了一样[/jitter]抢着购买！\n\n而宝藏往往伴随着风险。封魔石会让附近的魔法失效，它对魔法使来说是最大的天敌。你会怎么做？',
 'FRIERENMOD-SEALING_ORE_MINE.pages.INITIAL.options.PLAY.title':'这是什么？玩一下',
 'FRIERENMOD-SEALING_ORE_MINE.pages.INITIAL.options.PLAY.description':'[green]移除[/green]一张牌。每有1耗能，向主牌组添加1张[gold]魔力[/gold]。',
 'FRIERENMOD-SEALING_ORE_MINE.pages.INITIAL.options.GREED.title':'贪！',
 'FRIERENMOD-SEALING_ORE_MINE.pages.INITIAL.options.GREED.description':'获得魔具[green]封魔矿[/green]。',
 'FRIERENMOD-SEALING_ORE_MINE.pages.PLAY.description':'你拿起一块封魔石试了试。身上的一页魔法悄然消失，化作了纯粹的魔力。',
 'FRIERENMOD-SEALING_ORE_MINE.pages.GREED.description':'你挑了一块最漂亮的封魔石放进行李。\n\n[gold]下一个商人一定会喜欢它。[/gold]',
})
update('eng','events',{
 'FRIERENMOD-FOOD_EVENT.pages.INITIAL.options.FREE.description':'Heal [green]15[/green] HP.',
 'FRIERENMOD-FOOD_EVENT.pages.INITIAL.options.MEAL.description':'Pay [gold]150[/gold] Gold. Gain [green]Mercury Pudding[/green] and heal [green]{MealHeal}[/green] HP.',
 'FRIERENMOD-FOOD_EVENT.pages.INITIAL.options.MEAL_STRAWBERRY.title':'A Meal',
 'FRIERENMOD-FOOD_EVENT.pages.INITIAL.options.MEAL_STRAWBERRY.description':'Pay [gold]150[/gold] Gold. Gain a [green]Strawberry[/green] and heal [green]{MealHeal}[/green] HP.',
 'FRIERENMOD-FOOD_EVENT.pages.INITIAL.options.FEAST.description':'Pay [gold]250[/gold] Gold. Gain a [green]Mango[/green] and heal to full HP.',
 'FRIERENMOD-SEALING_ORE_MINE.title':'Anti-Magic Stone Mine',
 'FRIERENMOD-SEALING_ORE_MINE.pages.INITIAL.description':'You arrive at a mine filled with precious anti-magic stone. Merchants go [jitter]wild[/jitter] for it!\n\nBut treasure comes with risk: these stones suppress magic, making them a mage\'s worst enemy. What will you do?',
 'FRIERENMOD-SEALING_ORE_MINE.pages.INITIAL.options.PLAY.title':'What is this? Let me try',
 'FRIERENMOD-SEALING_ORE_MINE.pages.INITIAL.options.PLAY.description':'[green]Remove[/green] a card. Add 1 [gold]Mana[/gold] to your deck for each Energy it costs.',
 'FRIERENMOD-SEALING_ORE_MINE.pages.INITIAL.options.GREED.title':'Greed!',
 'FRIERENMOD-SEALING_ORE_MINE.pages.INITIAL.options.GREED.description':'Gain [green]Anti-Magic Ore[/green].',
 'FRIERENMOD-SEALING_ORE_MINE.pages.PLAY.description':'A page of magic fades away, leaving pure Mana behind.',
 'FRIERENMOD-SEALING_ORE_MINE.pages.GREED.description':'You pack a beautiful stone into your luggage.\n\nThe next merchant will surely love it.',
})
# 正文中的三牌堆术语沿用全模组统一金色规则；保留作者其他措辞。
for table in ['powers','events','frieren']:
 p=ROOT/f'FrierenMod/localization/zhs/{table}.json';data=json.loads(p.read_text(encoding='utf-8'))
 for key,value in data.items():
  if key.endswith('.title'):continue
  for term in ['抽牌堆','手牌','弃牌堆']:
   value=re.sub(r'(?<!\[gold\])'+term+r'(?!\[/gold\])','[gold]'+term+'[/gold]',value)
  data[key]=value
 p.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
# 作者原图按已有画框比例裁剪，保留原图副本，禁止重绘。
sources=ROOT/'assets/author_adjusted/20261106';sources.mkdir(parents=True,exist_ok=True)
for name,model in [('黑金之翼.png','BlackGoldWings'),('封魔石.png','SealingOre')]:
 source=ROOT.parent/'png'/name;shutil.copy2(source,sources/name)
 image=Image.open(source).convert('RGB')
 ImageOps.fit(image,(500,380),method=Image.Resampling.LANCZOS).save(ROOT/f'FrierenMod/images/cards/{model}.png')
shutil.copy2(sources/'封魔石.png',ROOT/'FrierenMod/images/events/SealingOreMine.png')
provenance=ROOT/'docs/assets.json'
entries=json.loads(provenance.read_text(encoding='utf-8'))
for target,source in [('cards/BlackGoldWings.png','黑金之翼.png'),('cards/SealingOre.png','封魔石.png'),('events/SealingOreMine.png','封魔石.png')]:
 relative='FrierenMod/images/'+target
 entry={'file':relative,'source':'assets/author_adjusted/20261106/'+source,'source_kind':'author_provided','modified':target.startswith('cards/'),'processing':'作者原图；卡图按500×380原生画框等比处理，事件保留完整原图。','sha256':hashlib.sha256((ROOT/relative).read_bytes()).hexdigest()}
 entries=[e for e in entries if e['file']!=relative];entries.append(entry)
provenance.write_text(json.dumps(entries,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print('AUTHOR_CONTENT_0120_OK')
