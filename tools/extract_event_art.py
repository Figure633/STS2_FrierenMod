"""从作者Word中指定事件标题后提取第一幅原图，保留来源与文件指纹。"""
from pathlib import Path
from zipfile import ZipFile
from lxml import etree as ET
import argparse,hashlib,json,shutil,subprocess,os
from PIL import Image
from io import BytesIO

ROOT=Path(__file__).resolve().parents[1]
parser=argparse.ArgumentParser()
parser.add_argument('--document',default='../开发资料/TODOs.docx')
parser.add_argument('--title',required=True)
parser.add_argument('--model',required=True)
parser.add_argument('--date',required=True)
parser.add_argument('--apply-word-crop',action='store_true')
parser.add_argument('--bounds',help='可选原图像素范围left,top,width,height，须位于Word裁剪内')
parser.add_argument('--node',help='执行确定性PNG转换的Node路径；sharp通过SHARP_MODULE指定')
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
    crops=paragraph.xpath('.//a:srcRect',namespaces=ns)
    has_crop=any(any(int(v) for v in rect.attrib.values()) for rect in crops)
    assert not has_crop or args.apply_word_crop, '此图片含作者裁剪，请显式指定--apply-word-crop并核对构图'
    embed=paragraph.xpath('.//a:blip',namespaces=ns)[0].get('{'+ns['r']+'}embed')
    media=relations[embed];data=z.read('word/'+media)
size=Image.open(BytesIO(data)).size
l,t,r,b=(int(crops[0].get(k,'0'))/100000 for k in ('l','t','r','b')) if crops and args.apply_word_crop else (0,0,0,0)
left,top=round(size[0]*l),round(size[1]*t)
right,bottom=round(size[0]*(1-r)),round(size[1]*(1-b))
rect={'left':left,'top':top,'width':right-left,'height':bottom-top}
if args.bounds:
    x,y,w,h=map(int,args.bounds.split(','));assert x>=left and y>=top and w>0 and h>0 and x+w<=right and y+h<=bottom,'手工范围超出作者Word裁剪'
    rect={'left':x,'top':y,'width':w,'height':h}
source=ROOT/'../开发资料/assets/author_adjusted'/args.date/(args.model+Path(media).suffix)
source.parent.mkdir(parents=True,exist_ok=True);source.write_bytes(data)
target=ROOT/'FrierenMod/images/events'/(args.model+'.png')
modified=has_crop or bool(args.bounds) or source.suffix.lower()!='.png'
if modified:
    assert args.node,'裁切或JPEG转PNG需指定--node（sharp运行时）'
    js="const sharp=require(process.env.SHARP_MODULE||'sharp');sharp(process.argv[1]).extract(JSON.parse(process.argv[3])).png().toFile(process.argv[2]).catch(e=>{console.error(e);process.exitCode=1})"
    subprocess.run([args.node,'-e',js,str(source),str(target),json.dumps(rect)],check=True)
else:shutil.copy2(source,target)
provenance=ROOT/'tools/data/assets.json';entries=json.loads(provenance.read_text(encoding='utf-8'))
relative=target.relative_to(ROOT).as_posix()
entries=[e for e in entries if e['file']!=relative]
entries.append({'file':relative,'source':source.relative_to(ROOT).as_posix(),'source_kind':'author_provided','modified':modified,'processing':'按作者Word裁剪提取；手工收紧播放器边缘，不重画、不锐化。' if modified else '作者Word完整原图直接复制。','source_rect':rect,'sha256':hashlib.sha256(target.read_bytes()).hexdigest()})
provenance.write_text(json.dumps(entries,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print('EVENT_ART_EXTRACTED',args.model,len(data))
