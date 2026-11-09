"""仅更新本次委托/魔具指定键，保留作者其他中文与JSON排版。"""
from pathlib import Path
import json,re

def update(lang,table,changes):
    p=Path('FrierenMod/localization')/lang/(table+'.json')
    s=p.read_text(encoding='utf-8')
    for key,value in changes.items():
        pattern=r'("'+re.escape(key)+r'"\s*:\s*)"(?:[^"\\]|\\.)*"'
        s,n=re.subn(pattern,lambda m:m[1]+json.dumps(value,ensure_ascii=False),s,count=1)
        if not n:
            pos=s.rfind('}');s=s[:pos].rstrip()+',\n  '+json.dumps(key)+': '+json.dumps(value,ensure_ascii=False)+'\n}\n'
    json.loads(s);p.write_text(s,encoding='utf-8')

for lang in ['zhs','eng']:
    zh=lang=='zhs'
    vial='只能在战斗中使用。回复30%最大生命，逃离非Boss战斗。使用后移除。' if zh else 'Can only be used in combat. Heal 30% of your maximum HP and escape a non-Boss combat. Removed after use.'
    pot='可反复使用。花费30金币，将1个随机普通魔具加入你的魔法牌组。' if zh else 'Reusable. Spend 30 Gold to add a random common magic item to your magic deck.'
    royal='从所有可获取因子中挑选1个加入魔法牌组，包含谜题。使用后移除。' if zh else 'Choose 1 obtainable factor, including puzzles, to add to your magic deck. Removed after use.'
    frieren={
        'PROP.MagicExamVial':vial,'PROP.MagicPot':pot,'PROP.RoyalGrimoire':royal,
        'COMMISSION.REWARD.Potion':'报酬：一瓶药水' if zh else 'Reward: 1 Potion',
        'COMMISSION.BASE_GOLD':'；固定{Gold}金币' if zh else '; {Gold} fixed Gold',
        'COMMISSION.EXTRA_PROP':'，另得魔具“{Item}”' if zh else ', plus {Item}',
        'COMMISSION.NAME.GRANNY':'顽固婆婆的委托' if zh else "The Stubborn Granny's Commission",
        'COMMISSION.REQUEST.GRANNY':'如果把精英魔物讨伐掉的话，要我给你奖励也不是不可以' if zh else "If you defeat an Elite monster, I suppose I could give you a reward.",
        'COMMISSION.GOAL.GRANNY':'接取后，赢得1场精英战斗。' if zh else 'After accepting, win 1 Elite combat.',
        'COMMISSION.NAME.NAMUSAN':'南无三，何等无慈悲' if zh else 'Namu San! How Merciless!',
        'COMMISSION.REQUEST.NAMUSAN':'Domo，芙莉莲=san，实际达人！请指教在下魔法！' if zh else 'Domo, Frieren-san. A true master! Please teach me magic!',
        'COMMISSION.GOAL.NAMUSAN':'接取后，在第1回合赢得1场战斗。' if zh else 'After accepting, win a combat on turn 1.',
        'COMMISSION.NAME.STARK':'修塔尔克的请求' if zh else "Stark's Request",
        'COMMISSION.REQUEST.STARK':'我已经受够当魔物诱饵的日子了芙莉莲，再受伤我就回老家了。' if zh else "I've had enough of being monster bait, Frieren. If I get hurt again, I'm going home.",
        'COMMISSION.GOAL.STARK':'接取后，累计赢得3场无伤战斗（当前{Current}场）。' if zh else 'After accepting, win 3 combats without losing HP (currently {Current}).',
        'COMMISSION.NAME.GUIDE':'冒险者小队招聘导游' if zh else 'Adventuring Party Seeks a Guide',
        'COMMISSION.REQUEST.GUIDE':'听说有问号的地方就有宝藏！我们需要一个资深导游带路' if zh else "They say question marks lead to treasure! We need an experienced guide.",
        'COMMISSION.GOAL.GUIDE':'在本幕中累计进入4个问号房，已进入{Current}个。' if zh else 'Enter 4 ? rooms this Act. You have entered {Current}.',
        'COMMISSION.GOAL.LESSON':'主牌组拥有4张已升级卡牌（当前{Current}张）。' if zh else 'Have 4 upgraded cards in your deck (currently {Current}).',
    }
    update(lang,'frieren',frieren)
    update(lang,'cards',{
        'FRIERENMOD-MAGIC_EXAM_VIAL.title':'魔法等级考试的小瓶' if zh else 'Mage Exam Vial',
        'FRIERENMOD-MAGIC_EXAM_VIAL.description':vial,
        'FRIERENMOD-MAGIC_POT.title':'魔法陶罐' if zh else 'Magic Pot',
        'FRIERENMOD-MAGIC_POT.description':pot,
        'FRIERENMOD-ROYAL_GRIMOIRE.description':royal,
    })
    update(lang,'relics',{
        'FRIERENMOD-MAGIC_SHOP_MEMBERSHIP.title':'魔具店会员卡' if zh else 'Magic Shop Membership',
        'FRIERENMOD-MAGIC_SHOP_MEMBERSHIP.description':'逛魔具店不消耗营地行动。' if zh else 'Visiting the magic shop does not consume your Rest Site action.',
        'FRIERENMOD-MAGIC_SHOP_MEMBERSHIP.flavor':'',
        'FRIERENMOD-GAMBLER_COIN.title':'赌徒金币' if zh else "Gambler's Coin",
        'FRIERENMOD-GAMBLER_COIN.description':'失去所有金币。将1张[gold]老实赚钱，很枯燥吧？[/gold]加入你的[gold]牌组[/gold]。' if zh else 'Lose all your Gold. Add [gold]Honest Work Is Boring, Right?[/gold] to your [gold]deck[/gold].',
        'FRIERENMOD-GAMBLER_COIN.flavor':'',
    })
print('AUTHOR_REVIEW_LOCALIZATION_OK')
