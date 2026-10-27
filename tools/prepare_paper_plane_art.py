"""按作者TODO原图裁切纸飞机卡面，保留原图颜色。"""
import hashlib
import json
from pathlib import Path
from zipfile import ZipFile
from PIL import Image
from io import BytesIO

root=Path(__file__).resolve().parents[1]
with ZipFile(root/'TODOs.docx')as archive:
    data=archive.read('word/media/image3.png')
image=Image.open(BytesIO(data)).convert('RGBA')
assert image.size==(1734,1112),'TODO配图已变化，请先核对纸飞机原图'
width=round(image.height*500/380)
left=(image.width-width)//2
image=image.crop((left,0,left+width,image.height)).resize((500,380),Image.Resampling.LANCZOS)
target=root/'FrierenMod/images/cards/PaperPlaneSpell.png'
image.save(target)
assets_file=root/'docs/assets.json'
assets=json.loads(assets_file.read_text(encoding='utf-8'))
entry=next(a for a in assets if a['file']=='FrierenMod/images/cards/PaperPlaneSpell.png')
entry.update(source='TODOs.docx:word/media/image3.png',source_sha256=hashlib.sha256(data).hexdigest(),
    sha256=hashlib.sha256(target.read_bytes()).hexdigest(),modified=True,
    conversion='tools/prepare_paper_plane_art.py; centered 500:380 crop, original colors')
entry.pop('source_commit',None)
assets_file.write_text(json.dumps(assets,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print('PAPER_PLANE_ART_OK 500x380')
