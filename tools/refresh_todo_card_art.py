"""按作者 TODO 嵌图裁切卡面；不重绘、不生成像素。"""
from pathlib import Path
from zipfile import ZipFile
from io import BytesIO
import json,hashlib,re
from PIL import Image,ImageOps
ROOT=Path(__file__).resolve().parents[1]
RECIPES={'EndlessFlow':('image1.png',None),'FrierenTheSlayer':('image2.jpeg',None),'RingletForm':('image3.png',(0,40,2560,1470)),'PajamasForm':('image4.png',None),'OpenTheWaygate':('image5.png',(0,75,2560,1410)),'FrierenSuitcase':('image6.png',(0,80,2560,1520)),'Simmering':('image7.png',(0,80,2560,1520)),'Perpetual':('image8.png',(0,80,2560,1520))}
assets_path=ROOT/'docs/assets.json';assets=json.loads(assets_path.read_text('utf-8-sig'))
def record(path,source,processing):
 global assets
 relative=path.relative_to(ROOT).as_posix();old=next((a for a in assets if a['file']==relative),{})
 old.update(file=relative,source=source,sha256=hashlib.sha256(path.read_bytes()).hexdigest(),modified=True,processing=processing)
 assets=[a for a in assets if a['file']!=relative];assets.append(old)
with ZipFile(ROOT/'TODOs.docx') as z:
 for name,(media,box) in RECIPES.items():
  im=Image.open(BytesIO(z.read('word/media/'+media))).convert('RGB')
  if box:im=im.crop(box)
  im=ImageOps.fit(im,(500,380),method=Image.Resampling.LANCZOS);target=ROOT/f'FrierenMod/images/cards/{name}.png';im.save(target)
  record(target,'TODOs.docx:word/media/'+media,'作者原图，去截图边缘后按500x380等比中心裁切；不生成或重绘。')
 im=Image.open(ROOT/'FrierenMod/images/cards/FrierenTheSlayer.png');target=ROOT/'FrierenMod/images/powers/FrierenTheSlayerPower.png';ImageOps.fit(im,(84,84),method=Image.Resampling.LANCZOS).save(target);record(target,'TODOs.docx:word/media/image2.jpeg','卡面原图中心裁切为84x84能力图标。')
# 其他能力沿用原有图片，裁去横向黑条与一代能力卡的椭圆黑角。
models={'BackFlow'}
for p in (ROOT/'Scripts/Frieren/Cards').rglob('*.cs'):
 if re.search(r':\s*(?:FrierenCard|LegendaryCard)\([^\n]*CardType.Power',p.read_text('utf-8-sig')):models.add(p.stem)
for name in sorted(models-set(RECIPES)):
 target=ROOT/f'FrierenMod/images/cards/{name}.png'
 if not target.exists():continue
 old=next((a for a in assets if a['file']==target.relative_to(ROOT).as_posix()),{})
 original=ROOT.parent/'FrierenMod_STS1_source code'/old.get('source','')
 im=Image.open(original if original.is_file() else target).convert('RGB');w,h=im.size
 def dark(y):return max(max(im.getpixel((x,y))) for x in range(w))<=24
 top=0;bottom=h
 while top<bottom and dark(top):top+=1
 while bottom>top and dark(bottom-1):bottom-=1
 # 不裁掉超过图片1/3的内容，避免将场景暗区误当边框。
 if bottom-top<h*2/3:continue
 cropped=im.crop((0,top,w,bottom));cw,ch=cropped.size
 black_corners=all(max(cropped.getpixel((x,y)))<=24 for x,y in [(0,0),(cw-1,0),(0,ch-1),(cw-1,ch-1)])
 if black_corners:
  # 椭圆内接矩形（两轴各取66%）在四角也保留实图，不补画黑角。
  cropped=cropped.crop((round(cw*.17),round(ch*.17),round(cw*.83),round(ch*.83)))
 if name=='BackFlow':cropped=cropped.crop((0,24,cropped.width,cropped.height))
 if top==0 and bottom==h and not black_corners and name!='BackFlow':continue
 ImageOps.fit(cropped,(w,h),method=Image.Resampling.LANCZOS).save(target)
 record(target,old.get('source','一代现有卡面'),f'去完整黑边与椭圆黑角（{black_corners}），逆流另去顶端播放器提示；保持尺寸{w}x{h}，无重绘。')
 print(name,top,h-bottom,'ellipse=',black_corners)
assets_path.write_text(json.dumps(assets,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
