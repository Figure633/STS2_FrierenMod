"""只读核对当前卡池、卡图形状、卡背资源及文档引用，结果写入临时目录。"""
from pathlib import Path
from collections import Counter,defaultdict
from PIL import Image,ImageDraw,ImageFont
import hashlib,json,re,argparse
import numpy as np

ROOT=Path(__file__).resolve().parents[1]
parser=argparse.ArgumentParser();parser.add_argument('--output',type=Path,default=ROOT/'artifacts/content-audit-current');parser.add_argument('--native',action='store_true',help='核对该目录内新生成的原生卡面截图');args=parser.parse_args()
OUT=args.output;OUT.mkdir(parents=True,exist_ok=True)
def load(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def save(p,data):p.write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
cards=[c for c in load(ROOT/'artifacts/card-catalog-current.json') if not c['upgraded']]
assert all('randomCandidate'in c and 'portraitPath'in c for c in cards),'请先运行增强后的 export-card-catalog'
pool=[c for c in cards if c['randomCandidate'] and c['showInLibrary']]
counts={r:dict(Counter(c['type'] for c in pool if c['rarity']==r)) for r in ['Common','Uncommon','Rare']}
pools=load(ROOT/'artifacts/card-pools-current.json')
comparison=[dict(model=p['model'],total=len(p['cards']),types=dict(Counter(c['type'] for c in p['cards']))) for p in pools]
source=ROOT.parent/'FrierenMod_STS1_source code/src/main'
java=defaultdict(list)
for path in (source/'java/FrierenMod/cards').rglob('*.java'):java[path.stem].append(path)
assets={Path(x['file']).name:x for x in load(ROOT/'docs/assets.json') if '/images/cards/'in x['file']}
artcards=defaultdict(list)
for c in cards:
 path=c['portraitPath'].replace('res://','');artcards[path].append(c)
arts=[]
for relative,models in sorted(artcards.items()):
 path=ROOT/relative
 if not path.is_file() or path.suffix!='.png':continue
 image=Image.open(path).convert('RGBA');a=np.array(image);rgb=a[:,:,:3].astype(float);alpha=a[:,:,3]
 h,w=alpha.shape;black=(rgb.max(axis=2)<22)&(alpha>245)
 def strip(values):
  n=0
  for b in values:
   if not b:break
   n+=1
  return n
 rowblack=black.mean(axis=1)>.97;colblack=black.mean(axis=0)>.97
 bars=[strip(rowblack),strip(rowblack[::-1]),strip(colblack),strip(colblack[::-1])]
 dh,dw=max(1,h//7),max(1,w//7)
 corners=[float(x.mean()) for x in [black[:dh,:dw],black[:dh,-dw:],black[-dh:,:dw],black[-dh:,-dw:]]]
 transparent=float((alpha<245).mean());blackcorners=sum(c>.72 for c in corners)
 stem=path.stem;provenance=assets.get(path.name,{})
 original=Path(str(provenance.get('source','')).replace('\\','/')).stem
 original=re.sub('_p$','',original)
 candidates=[stem,original]+[m['model'] for m in models]
 original_types=[];origfiles=[]
 for name in dict.fromkeys(candidates):
  for jp in java.get(name,[]):
   text=jp.read_text(encoding='utf-8-sig',errors='replace');types=re.findall(r'CardType\.(ATTACK|POWER|SKILL|STATUS|CURSE)',text)
   if types:original_types.append(types[0]);origfiles.append(jp.relative_to(source).as_posix())
 oldshape=bool(set(original_types)&{'ATTACK','POWER'})
 suspicious=(transparent>.008 or blackcorners>=2 or max(bars)>0 or abs(w/h-250/190)>.015)
 # 先古卡采用完整竖幅，不作为普通横幅黑边候选。
 ordinary=any(m['rarity']!='Ancient' for m in models)
 record=dict(file=relative,size=[w,h],models=[dict(model=m['model'],name=m['name'],type=m['type'],kind=m['kind'],rarity=m['rarity'],retired=m['retired'],showInLibrary=m['showInLibrary']) for m in models],
  alphaFraction=round(transparent,4),blackCorners=[round(x,3) for x in corners],bars=bars,originalTypes=sorted(set(original_types)),originalFiles=origfiles,
  source=provenance.get('source',''),processing=provenance.get('processing',''),legacyAttackOrPower=oldshape,suspicious=suspicious and ordinary)
 arts.append(record)
suspects=[a for a in arts if a['suspicious']]
fontpath=Path('C:/Windows/Fonts/msyh.ttc');font=ImageFont.truetype(str(fontpath),16);small=ImageFont.truetype(str(fontpath),13)
def sheets(records,prefix):
 for start in range(0,len(records),12):
  part=records[start:start+12];out=Image.new('RGB',(1200,((len(part)+3)//4)*270),'#d2d8df');draw=ImageDraw.Draw(out)
  for n,item in enumerate(part):
   x=n%4*300;y=n//4*270
   im=Image.open(ROOT/item['file']).convert('RGBA');im.thumbnail((285,205))
   # 棋盘格显示旧图的透明缺角，避免把透明误当成黑色。
   for xx in range(0,285,15):
    for yy in range(0,205,15):draw.rectangle((x+xx,y+yy,x+xx+14,y+yy+14),fill='#ffffff'if(xx//15+yy//15)%2 else'#a9adb3')
   out.paste(im,(x+(285-im.width)//2,y),im)
   label=item['models'][0]
   draw.text((x+5,y+209),label['name'][:17],font=font,fill='black')
   draw.text((x+5,y+232),Path(item['file']).stem[:34],font=small,fill='black')
   draw.text((x+5,y+251),'/'.join(item['originalTypes'])+' -> '+','.join(sorted({m['type']for m in item['models']})),font=small,fill='black')
  out.save(OUT/f'{prefix}-{start//12+1:02}.png')
sheets(suspects,'suspects')
save(OUT/'art-audit.json',arts)
save(OUT/'cards.json',cards);save(OUT/'rarities.json',dict(normalPool=counts,comparison=comparison,allModels=dict(Counter(c['rarity']for c in cards))))
save(OUT/'suspect-models.json',[m['model']for a in suspects for m in a['models']if m['showInLibrary']])
native_models=[m['model']for a in arts for m in a['models']if m['showInLibrary']and m['rarity']!='Ancient'and(m['type']in ['Attack','Power']or max(a['bars'])>=2)]
save(OUT/'native-models.json',list(dict.fromkeys(native_models)))
if args.native:
 native=[]
 for r in load(OUT/'native-rects.json'):
  image=Image.open(OUT/r['file']).convert('RGB');sx=image.width/r['viewport'][0];sy=image.height/r['viewport'][1];x,y,w,h=r['rect']
  box=(round(x*sx),round(y*sy),round((x+w)*sx),round((y+h)*sy))
  rendered=np.array(image.crop(box));original=np.array(Image.open(ROOT/r['portrait'].replace('res://','')).convert('RGBA').resize((box[2]-box[0],box[3]-box[1])))
  mask=np.zeros(original.shape[:2],dtype=bool);mask[3:-3,3:-3]=True
  transparent=(original[:,:,3]<20)&mask;dark=rendered.max(axis=2)<27
  native.append(dict(model=r['model'],name=r['name'],transparentPixels=int(transparent.sum()),blackGapPixels=int((transparent&dark).sum())))
 save(OUT/'native-gap-analysis.json',native)
ids=re.findall(r'"(\d{4})"',(ROOT/'Core/Frieren/SlotBackCatalog.cs').read_text(encoding='utf-8'))
installed={p.stem:p for p in (ROOT/'FrierenMod/images/slot_backs').glob('*.png')}
installed_hashes={hashlib.sha256(p.read_bytes()).hexdigest()for p in installed.values()}
oldfolder=source/'resources/FrierenModResources/img/UI/slotBg'
if not oldfolder.is_dir():
 candidates=[p for p in(source/'resources/FrierenModResources/img/UI').rglob('*.png')if re.fullmatch(r'\d{4}',p.stem)]
else:candidates=list(oldfolder.glob('*.png'))
old={p.stem:p for p in candidates};same=[id for id,p in installed.items()if id in old and p.read_bytes()==old[id].read_bytes()]
labels=load(ROOT/'FrierenMod/localization/zhs/frieren.json')
newbacks=[]
for folder in ['outputs/slot-back-preview-20261029','outputs/slot-backs-final-20261029']:
 for p in (ROOT/folder).rglob('*.png'):
  if Image.open(p).size==(301,461):
   digest=hashlib.sha256(p.read_bytes()).hexdigest()
   newbacks.append(dict(file=p.relative_to(ROOT).as_posix(),sha256=digest,installed=digest in installed_hashes))
prepared=[r for r in newbacks if r['file'].startswith(('outputs/slot-backs-final-20261029/基础圆角/','outputs/slot-backs-final-20261029/金色花纹/'))]
achievementfiles=list((ROOT/'Scripts').rglob('*.cs'))
hooks='\n'.join(p.read_text(encoding='utf-8-sig') for p in achievementfiles if 'Achievement' in p.name)
unlocks=sorted(set(re.findall(r'Unlock\([^\n]*?"(\d{4})"',hooks)))
references={id:[]for id in ids}
for folder in ['Scripts','Core']:
 for p in(ROOT/folder).rglob('*.cs'):
  if p.name=='SlotBackCatalog.cs':continue
  for id in set(re.findall(r'"(\d{4})"',p.read_text(encoding='utf-8-sig')))&set(ids):references[id].append(p.relative_to(ROOT).as_posix())
save(OUT/'back-references.json',references)
save(OUT/'backs.json',dict(registeredIds=ids,resourceIds=sorted(installed),missing=sorted(set(ids)-installed.keys()),unregistered=sorted(installed.keys()-set(ids)),oldResourceCount=len(old),sameAsSts1=same,
 labels={id:dict(title=labels.get('BACK.'+id+'.title',''),condition=labels.get('BACK.'+id+'.description',''))for id in ids},hookIds=unlocks,newArtwork=newbacks,preparedArtwork=prepared))
print(json.dumps(dict(cardModels=len(cards),normalPool=len(pool),rarities=counts,comparison=comparison,artFiles=len(arts),suspectArt=len(suspects),registeredBacks=len(ids),sameAsSts1=len(same),pendingBackArtwork=sum(not a['installed']for a in prepared)),ensure_ascii=False))
