"""从作者Word中指定事件标题后提取第一幅原图，保留来源与文件指纹。"""
from pathlib import Path
from zipfile import ZipFile
from lxml import etree as ET
import argparse,hashlib,json,shutil

ROOT=Path(__file__).resolve().parents[1]
parser=argparse.ArgumentParser()
parser.add_argument('--document',default='TODOs.docx')
parser.add_argument('--title',required=True)
parser.add_argument('--model',required=True)
parser.add_argument('--date',required=True)
args=parser.parse_args()
assert args.model.isidentifier() and args.date.isdigit()
ns={'w':'http://schemas.openxmlformats.org/wordprocessingml/2006/main','a':'http://schemas.openxmlformats.org/drawingml/2006/main','r':'http://schemas.openxmlformats.org/officeDocument/2006/relationships'}
with ZipFile(ROOT/args.document) as z:
    root=ET.fromstring(z.read('word/document.xml'))
    relations={r.get('Id'):r.get('Target') for r in ET.fromstring(z.read('word/_rels/document.xml.rels'))}
    paragraphs=root.xpath('//w:body/w:p',namespaces=ns)
    start=next(i for i,p in enumerate(paragraphs) if ''.join(p.xpath('.//w:t/text()',namespaces=ns)).strip()==args.title)
    paragraph=next(p for p in paragraphs[start+1:] if p.xpath('.//a:blip',namespaces=ns))
    # 有Word裁剪时应先核对构图，不默默丢弃裁剪要求。
    assert all(not any(int(v) for v in rect.attrib.values()) for rect in paragraph.xpath('.//a:srcRect',namespaces=ns)), '此图片含作者裁剪，请先按Word构图处理'
    embed=paragraph.xpath('.//a:blip',namespaces=ns)[0].get('{'+ns['r']+'}embed')
    data=z.read('word/'+relations[embed])
source=ROOT/'assets/author_adjusted'/args.date/(args.model+'.png')
source.parent.mkdir(parents=True,exist_ok=True);source.write_bytes(data)
target=ROOT/'FrierenMod/images/events'/(args.model+'.png');shutil.copy2(source,target)
provenance=ROOT/'docs/assets.json';entries=json.loads(provenance.read_text(encoding='utf-8'))
relative=target.relative_to(ROOT).as_posix()
entries=[e for e in entries if e['file']!=relative]
entries.append({'file':relative,'source':source.relative_to(ROOT).as_posix(),'source_kind':'author_provided','modified':False,'processing':'从作者Word事件条目提取完整原图，沿用原生事件左侧等比铺满。','sha256':hashlib.sha256(data).hexdigest()})
provenance.write_text(json.dumps(entries,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print('EVENT_ART_EXTRACTED',args.model,len(data))
