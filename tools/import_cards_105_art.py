"""导入0.10.5作者TODO中的卡图，不修改卡牌文本或Excel；需对应版本的原始文档。"""
from pathlib import Path
from zipfile import ZipFile
from io import BytesIO
import hashlib, json, argparse, shutil
from PIL import Image, ImageOps

ROOT=Path(__file__).resolve().parents[1]
def load(p): return json.loads(p.read_text('utf-8-sig'))
def save(p,v): p.write_text(json.dumps(v,ensure_ascii=False,indent=2)+'\n','utf-8')
# 按原图哈希定位，Word新增图片或重排编号时不会错用其他卡图。
recipes={'SleepIn':('95a8315845f103e37c407558f7d521b06af50fc42f5f80a7befc4669845de07a',(.5,.5)),
 'WakeUp':('8c72ce2a99abe00d3811218a8d7baf40a9dad75ff76e1e53ee7ce255cfac6206',(.5,.15)),
 'Crying':('0b61dcbfa5f93aa0ce27d8e77bad1d61d8b49d5cfc0c8b66f2ef8efab37da0a7',(.5,.5)),
 'TreasureVaultSpell':('3f6f3ce9d7c4b566ba73fc5f2b3ae4e325ee1f44851832b6fea947127da37ec5',(.5,.5)),
 'Underestimated':('9d8e47df8106d75fee1ccb845253f83152f96cc9f5934f5f5e3efc1830ef751c',(.5,.25)),
 'StrongerWish':('c964367d8ccc7b628d2be14bf13456841f67e3775ca3eb9fb551815c7e778c45',(.5,.35)),
 'RewardWish':('6eccc9b989ee454df4bbf832d549a51259eb51095e83ea9d0f181d7bf7b5253e',(.5,.3)),
 'ManaWish':('a709653db86669dd32654fbeecd044c236bd84f7ed01d234ab686716b2601e26',(.5,.5)),
 'DomainExpansion':('219c70e8afd93120d5adb9023c582cbbab9ddfa87dd9e5354158ecf2da1a3820',(.5,.5))}
parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('--good-luck-image',type=Path,help='作者提供的好运魔法PNG原图；省略时保留现有主卡图')
args=parser.parse_args()
assets=load(ROOT/'docs/assets.json')
def record(path,source,processing):
 global assets
 rel=path.relative_to(ROOT).as_posix();assets=[a for a in assets if a['file']!=rel]
 assets.append(dict(file=rel,source=source,sha256=hashlib.sha256(path.read_bytes()).hexdigest(),modified=True,processing=processing))
with ZipFile(ROOT/'TODOs.docx') as z:
 media={hashlib.sha256(z.read(n)).hexdigest():n for n in z.namelist() if n.startswith('word/media/')}
 for key,_ in recipes.values():
  if key not in media:raise ValueError('TODO中找不到原图：'+key)
 for name,(source_hash,center) in recipes.items():
  source=media[source_hash]
  im=Image.open(BytesIO(z.read(source))).convert('RGB');target=ROOT/f'FrierenMod/images/cards/{name}.png'
  ImageOps.fit(im,(500,380),method=Image.Resampling.LANCZOS,centering=center).save(target)
  record(target,'TODOs.docx:'+source,'作者原图等比裁切为500x380；无重绘。原图SHA256='+source_hash)
 source=media['e1cf2635783166f38712e18ab94409b5a63301f0972345ad0b00ed1cb30f4d42']
 target=ROOT/'FrierenMod/images/powers/SleepInPower.png';target.write_bytes(z.read(source))
 record(target,'TODOs.docx:'+source,'作者提供的100x100能力图标，原样复制。')
if args.good_luck_image:
 if Image.open(args.good_luck_image).format!='PNG':raise ValueError('好运魔法请提供PNG原图')
 target=ROOT/'FrierenMod/images/cards/GoodLuckSpell.png';shutil.copyfile(args.good_luck_image,target)
 record(target,'作者2026-10-31上传的好运魔法PNG原图（同图已嵌入TODO）','865x487原图直接复制；无裁切、缩放或重绘。')
 next(a for a in assets if a['file']==target.relative_to(ROOT).as_posix())['modified']=False
for name,source in [('DomainExpansionPower','DomainExpansion'),('GoodLuckRewardPower','RewardWish')]:
 target=ROOT/f'FrierenMod/images/powers/{name}.png';ImageOps.fit(Image.open(ROOT/f'FrierenMod/images/cards/{source}.png'),(84,84)).save(target)
 record(target,f'FrierenMod/images/cards/{source}.png','作者卡图中心裁切为84x84能力图标。')
save(ROOT/'docs/assets.json',assets)
