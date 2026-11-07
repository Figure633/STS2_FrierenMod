"""根据作者直接更新的遗物PNG生成Atlas与原生悬停轮廓；不修改原PNG。"""
from pathlib import Path
from PIL import Image,ImageFilter
import argparse,json,hashlib,re
ROOT=Path(__file__).resolve().parents[1]
parser=argparse.ArgumentParser();parser.add_argument('name');args=parser.parse_args()
if not re.fullmatch('[A-Za-z][A-Za-z0-9]*',args.name):raise ValueError('遗物名须为模型类名')
folder=ROOT/'FrierenMod/images/relics';source=folder/(args.name+'.png');before=source.read_bytes();image=Image.open(source).convert('RGBA')
alpha=image.getchannel('A');left,top,right,bottom=alpha.getbbox();outline=Image.new('RGBA',image.size,(255,255,255,0));outline.putalpha(alpha.filter(ImageFilter.MaxFilter(3)));outline.save(folder/(args.name+'Outline.png'))
for name in [args.name,args.name+'Outline']:
 bounds=alpha.getbbox() if name==args.name else outline.getchannel('A').getbbox();l,t,r,b=bounds
 (folder/(name+'.tres')).write_text('[gd_resource type="AtlasTexture" load_steps=2 format=3]\n[ext_resource type="Texture2D" path="res://FrierenMod/images/relics/'+name+'.png" id="1"]\n[resource]\natlas = ExtResource("1")\nregion = Rect2('+', '.join(map(str,[l,t,r-l,b-t]))+')\nfilter_clip = true\n',encoding='utf-8')
file=ROOT/'docs/assets.json';data=json.loads(file.read_text(encoding='utf-8'))
for name in [args.name,args.name+'Outline']:
 relative='FrierenMod/images/relics/'+name+'.png';entry=next((r for r in data if r['file']==relative),None)
 if entry is None:entry={'file':relative};data.append(entry)
 entry.update({'source':('作者直接更新 '+source.relative_to(ROOT).as_posix()) if name==args.name else source.relative_to(ROOT).as_posix(),'source_kind':'author_provided' if name==args.name else 'derived_from_author_artwork','modified':name!=args.name,'processing':'作者PNG原样保留；仅依据Alpha更新Atlas区域。' if name==args.name else 'tools/update_author_relic_icon.py；由作者PNG的Alpha生成白色轮廓，不改原图。','sha256':hashlib.sha256((folder/(name+'.png')).read_bytes()).hexdigest()})
file.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
assert source.read_bytes()==before
print('AUTHOR_RELIC_UNCHANGED',args.name,image.size,alpha.getbbox())
