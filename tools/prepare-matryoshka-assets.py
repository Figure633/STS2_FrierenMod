"""从一代JAR读取原版套娃，缩去留白后等比放大到常规遗物大小。"""
import argparse
import hashlib
import json
from pathlib import Path
from zipfile import ZipFile
from io import BytesIO
from PIL import Image

parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('jar',type=Path)
args=parser.parse_args()
root=Path(__file__).resolve().parents[1]
output=root/'FrierenMod/images/relics'
records=[]
with ZipFile(args.jar) as jar:
    for suffix,source in [('', 'images/relics/matryoshka.png'),('Outline','images/relics/outline/matryoshka.png')]:
        original=jar.read(source)
        image=Image.open(BytesIO(original)).convert('RGBA').crop((33,36,99,98))
        scale=min(120/image.width,112/image.height)
        image=image.resize((round(image.width*scale),round(image.height*scale)),Image.Resampling.LANCZOS)
        canvas=Image.new('RGBA',(128,128));canvas.alpha_composite(image,((128-image.width)//2,(128-image.height)//2))
        target=output/f'Matryoshka{suffix}.png';canvas.save(target)
        records.append({'file':str(target.relative_to(root)).replace('\\','/'),'sourceJarEntry':source,'sourceSha256':hashlib.sha256(original).hexdigest(),'processing':'共同裁框(33,36,99,98)，等比放大至120x112以内，居中128x128','sha256':hashlib.sha256(target.read_bytes()).hexdigest()})
(output/'Matryoshka.provenance.json').write_text(json.dumps({'source':'Slay the Spire desktop-1.0.jar','assets':records},indent=2)+'\n',encoding='utf-8')
print('MATRYOSHKA_OK: 原图与轮廓等比放大，来源与像素指纹已记录。')
