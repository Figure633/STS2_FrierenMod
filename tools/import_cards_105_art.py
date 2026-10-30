"""导入0.10.5作者TODO中的卡图，不修改卡牌文本或Excel；需对应版本的原始文档。"""
from pathlib import Path
from zipfile import ZipFile
from io import BytesIO
import hashlib, json, re
from PIL import Image, ImageOps

ROOT=Path(__file__).resolve().parents[1]
def load(p): return json.loads(p.read_text('utf-8-sig'))
def save(p,v): p.write_text(json.dumps(v,ensure_ascii=False,indent=2)+'\n','utf-8')
# TODO 原图独立提取，不重新运行旧版批量裁图，保留既有卡面。
recipes={'SleepIn':('image1.jpeg',(.5,.5)),'WakeUp':('image2.jpeg',(.5,.15)),'Crying':('image4.jpeg',(.5,.5)),
 'TreasureVaultSpell':('image5.jpeg',(.5,.5)),'Underestimated':('image6.jpeg',(.5,.25)),
 'StrongerWish':('image7.jpeg',(.5,.35)),'RewardWish':('image8.jpeg',(.5,.3)), 'ManaWish':('image9.jpeg',(.5,.5)),
 'DomainExpansion':('image12.jpeg',(.5,.5)), 'GoodLuckSpell':('image7.jpeg',(.5,.35))}
assets=load(ROOT/'docs/assets.json')
def record(path,source,processing):
 global assets
 rel=path.relative_to(ROOT).as_posix();assets=[a for a in assets if a['file']!=rel]
 assets.append(dict(file=rel,source=source,sha256=hashlib.sha256(path.read_bytes()).hexdigest(),modified=True,processing=processing))
with ZipFile(ROOT/'TODOs.docx') as z:
 for name,(source,center) in recipes.items():
  im=Image.open(BytesIO(z.read('word/media/'+source))).convert('RGB');target=ROOT/f'FrierenMod/images/cards/{name}.png'
  ImageOps.fit(im,(500,380),method=Image.Resampling.LANCZOS,centering=center).save(target)
  record(target,'TODOs.docx:word/media/'+source,'作者原图等比裁切为500x380；无重绘。')
 target=ROOT/'FrierenMod/images/powers/SleepInPower.png';target.write_bytes(z.read('word/media/image3.png'))
 record(target,'TODOs.docx:word/media/image3.png','作者提供的100x100能力图标，原样复制。')
for name,source in [('DomainExpansionPower','DomainExpansion'),('GoodLuckRewardPower','RewardWish')]:
 target=ROOT/f'FrierenMod/images/powers/{name}.png';ImageOps.fit(Image.open(ROOT/f'FrierenMod/images/cards/{source}.png'),(84,84)).save(target)
 record(target,f'FrierenMod/images/cards/{source}.png','作者卡图中心裁切为84x84能力图标。')
save(ROOT/'docs/assets.json',assets)
