"""恢复作者动图原帧，只抠除与边缘连通的背景；不缩放或修改RGB。"""
from pathlib import Path
from collections import deque
from PIL import Image, ImageFilter
import json,hashlib

ROOT=Path(__file__).resolve().parents[1]
source=ROOT/'FrierenMod/images/pets/aura/00.png'
image=Image.open(source).convert('RGBA');pixels=image.load();width,height=image.size
background=image.getpixel((0,0))[:3]
pending=deque([(x,0) for x in range(width)]+[(x,height-1) for x in range(width)]+[(0,y) for y in range(height)]+[(width-1,y) for y in range(height)])
seen=set()
while pending:
    x,y=pending.popleft()
    if (x,y) in seen or not (0<=x<width and 0<=y<height):continue
    seen.add((x,y));color=pixels[x,y]
    # 背景带少量纹理；窄色差避免穿过浅色头发或皮肤。
    r,g,b=color[:3]
    ground_shadow=y>=220 and 80<b<=g<=r<205 and r-g<20 and g-b<30
    if sum((color[c]-background[c])**2 for c in range(3))>20**2 and not ground_shadow:continue
    pixels[x,y]=(*color[:3],0)
    pending.extend([(x-1,y),(x+1,y),(x,y-1),(x,y+1)])
original=Image.open(source).convert('RGBA')
assert image.convert('RGB').tobytes()==original.convert('RGB').tobytes()
for point in [(155,100),(140,50),(145,170),(168,228)]:
    assert image.getpixel(point)[3]==original.getpixel(point)[3],('主体被误删',point)
folder=ROOT/'FrierenMod/images/relics';image.save(folder/'AuraCompanion.png')
outline=Image.new('RGBA',image.size,(255,255,255,0));outline.putalpha(image.getchannel('A').filter(ImageFilter.MaxFilter(3)));outline.save(folder/'AuraCompanionOutline.png')
bounds=image.getchannel('A').getbbox();left,top,right,bottom=bounds
for name in ['AuraCompanion','AuraCompanionOutline']:
    (folder/(name+'.tres')).write_text('[gd_resource type="AtlasTexture" load_steps=2 format=3]\n[ext_resource type="Texture2D" path="res://FrierenMod/images/relics/'+name+'.png" id="1"]\n[resource]\natlas = ExtResource("1")\nregion = Rect2('+', '.join(map(str,[left,top,right-left,bottom-top]))+')\nfilter_clip = true\n',encoding='utf-8')
provenance=ROOT/'tools/data/assets.json';records=json.loads(provenance.read_text(encoding='utf-8'))
for name in ['AuraCompanion','AuraCompanionOutline']:
    relative='FrierenMod/images/relics/'+name+'.png';records=[r for r in records if r['file']!=relative]
    records.append({'file':relative,'source':source.relative_to(ROOT).as_posix(),'source_kind':'original_animation_frame_alpha_only','modified':True,'processing':'tools/restore_aura_companion_icon.py；原帧270×249，只处理连通背景Alpha；主体RGB逐像素不变，无缩放、锐化或重绘。Outline为同一Alpha轮廓。','sha256':hashlib.sha256((ROOT/relative).read_bytes()).hexdigest()})
provenance.write_text(json.dumps(records,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
generated=ROOT/'tools/data/generated_assets.json';data=json.loads(generated.read_text(encoding='utf-8'))
retired=[r for r in data['assets'] if r['target']=='FrierenMod/images/relics/AuraCompanion.png']
for r in retired:r['status']='0.12.3已撤换；旧版本可从Git历史查阅'
data.setdefault('retired_assets',[]).extend(retired)
data['assets']=[r for r in data['assets'] if r['target']!='FrierenMod/images/relics/AuraCompanion.png']
generated.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print('AURA_ORIGINAL_RGB_PRESERVED',width,height,bounds)
