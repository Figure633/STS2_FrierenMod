"""将作者待办中的“老实赚钱”原图按原生横幅等比裁切，不重绘。"""
from pathlib import Path
from zipfile import ZipFile
from io import BytesIO
from lxml import etree as E
from PIL import Image, ImageOps
import hashlib, json

ROOT=Path(__file__).resolve().parents[1]
NS={'w':'http://schemas.openxmlformats.org/wordprocessingml/2006/main',
    'a':'http://schemas.openxmlformats.org/drawingml/2006/main',
    'r':'http://schemas.openxmlformats.org/officeDocument/2006/relationships'}
with ZipFile(ROOT/'../开发资料/TODOs.docx') as z:
    tree=E.fromstring(z.read('word/document.xml'))
    rels={r.get('Id'):r.get('Target') for r in E.fromstring(z.read('word/_rels/document.xml.rels'))}
    paragraphs=tree.findall('.//w:p',NS)
    i=next(i for i,p in enumerate(paragraphs) if '新卡：老实赚' in ''.join(p.xpath('.//w:t/text()',namespaces=NS)))
    illustration=next(p for p in paragraphs[i:i+3] if p.find('.//a:blip',NS) is not None)
    if any(dict(c.attrib) for c in illustration.findall('.//a:srcRect',NS)):
        raise ValueError('作者添加了新的Word裁剪位置，请先核对构图。')
    source='word/'+rels[illustration.find('.//a:blip',NS).get('{'+NS['r']+'}embed')]
    data=z.read(source)
image=Image.open(BytesIO(data)).convert('RGB')
target=ROOT/'FrierenMod/images/cards/HonestLiving.png'
# 原图脸贴近上边缘，保留顶部，裁去下方部分衣摆。
ImageOps.fit(image,(500,380),method=Image.Resampling.LANCZOS,centering=(.5,0)).save(target)
path=ROOT/'tools/data/assets.json';assets=json.loads(path.read_text(encoding='utf-8-sig'))
relative=target.relative_to(ROOT).as_posix();assets=[a for a in assets if a['file']!=relative]
assets.append({'file':relative,'source':'../开发资料/TODOs.docx:'+source,'source_kind':'author_provided',
    'modified':True,'processing':'作者原图等比裁切500x380，顶端对齐保留头脸，下方裁去衣摆；无重绘、锐化或新增内容。原图SHA256='+hashlib.sha256(data).hexdigest(),
    'sha256':hashlib.sha256(target.read_bytes()).hexdigest()})
path.write_text(json.dumps(assets,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print('HONEST_LIVING_ART_OK',image.size,source)
