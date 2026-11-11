"""从一代游戏本地 JAR 复制原版套娃与轮廓，不修改游戏文件。"""
import argparse
import hashlib
import json
from pathlib import Path
from zipfile import ZipFile

parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('jar',type=Path)
args=parser.parse_args()
root=Path(__file__).resolve().parents[1]
output=root/'FrierenMod/images/relics'
records=[]
with ZipFile(args.jar) as jar:
    for suffix,source in [('', 'images/relics/matryoshka.png'),('Outline','images/relics/outline/matryoshka.png')]:
        data=jar.read(source)
        target=output/f'Matryoshka{suffix}.png';target.write_bytes(data)
        records.append({'file':str(target.relative_to(root)).replace('\\','/'),'sourceJarEntry':source,'sha256':hashlib.sha256(data).hexdigest()})
(output/'Matryoshka.provenance.json').write_text(json.dumps({'source':'Slay the Spire desktop-1.0.jar','assets':records},indent=2)+'\n',encoding='utf-8')
print('套娃原版图标及轮廓已复制，来源与像素指纹已记录。')
