"""从作者填写表读取已确认的成就名和条件；不改动表格。"""
from pathlib import Path
import json,re
import openpyxl
root=Path(__file__).resolve().parents[1]
table=root/'outputs/slot-backs-final-20261109/二代新增卡槽背景-解锁条件填写表.xlsx'
rows=[r for r in openpyxl.load_workbook(table,read_only=True).worksheets[0].iter_rows(min_row=6,values_only=True) if r[5]]
translations=[
('A Wish on Ice','Finish 2 runs as Frieren.'),
('What Is This Place?','Enter any Adventure Story event.'),
('Battle Fanatics','Equip Hypnotic Spell and Black-and-Gold Wings at the same time.'),
("A Toast to Those Who Would Kill Me",'Win 5 runs as Frieren.'),
('Running Away Works','Escape a combat using the Magic Exam Vial.'),
('Frieren the Slayer','Obtain the Ancient card Frieren the Slayer.'),
('Float Like a Butterfly, Sting Like a Bee','Defeat a Boss on the first turn.'),
('You Have a Gift for Being a Priest','Heal 20 HP in a single combat.'),
("It Is What Himmel Would Have Done",'Complete 20 commissions in total.'),
('The Taciturn Warrior','Win a run within 30 minutes.'),
('The Heroes in Our Memories','Collect 20 slot backgrounds.'),
("Journey's End, at the Golden Land",'Win 10 runs as Frieren.'),
('Time to Get a Little Serious','Complete an empowered Elite commission.'),
('RPG Enjoyer','Complete a commission.'),
('Frieren 2 Has Arrived!','Open the slot background collection.'),
('Money?!','Have more than 999 Gold.'),
('We Call That a Gambling Legend','Win 777 Gold by playing Making an Honest Living Is Boring, Right?'),
('My Prized Apprentice','Discover every Legendary Magic card in the Card Library.'),
('A Stunning View Ahead','Enter the Sacred Snow Crystal event.'),
('Endless Flow','Obtain the Ancient card Endless Flow.'),
('The Bond Between Master and Apprentice','Win a run with at least 3 shared Frieren/Fern cards in your deck.'),
('Even a Lion Hunts Rabbits at Full Strength','Defeat Hunter-Killer.'),
('A Slightly Soiled Hem','Defeat an Infested Prism.')]
assert len(rows)==len(translations)==23
for lang in ('zhs','eng'):
 p=root/'FrierenMod/localization'/lang/'frieren.json';s=p.read_text(encoding='utf-8')
 for r,en in zip(rows,translations):
  for suffix,value in [('title',r[7] if lang=='zhs' else en[0]),('description',r[6] if lang=='zhs' else en[1])]:
   key=f'BACK.{r[5]}.{suffix}'
   pattern=r'("'+re.escape(key)+r'"\s*:\s*)"(?:[^"\\]|\\.)*"'
   s,n=re.subn(pattern,lambda m:m[1]+json.dumps(value,ensure_ascii=False),s,count=1)
   assert n==1,key
 json.loads(s);p.write_text(s,encoding='utf-8')
p=root/'docs/inventory/sts2_slot_backs.json';j=json.loads(p.read_text(encoding='utf-8'))
j['note']='作者已填写23组成就名与条件；22组已接入，8019圣雪结晶按作者回复等待新增事件后接入。仍不进入一代随机通关池。'
for item,r in zip(j['images'],rows):
 assert item['id']==r[5]
 item.update(achievementName=r[7],condition=r[6],unlockStatus='pending-event' if r[5]=='8019' else 'implemented')
p.write_text(json.dumps(j,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print('SLOT_BACK_CONDITIONS_OK: 23组作者文案，中英文本及来源索引已更新，表格未改写。')
