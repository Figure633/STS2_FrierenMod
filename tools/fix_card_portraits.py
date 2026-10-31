"""复现作者2026-10-31点名的七张卡图修正；只裁剪/缩放及延展原透明角的背景像素。"""
from io import BytesIO
from pathlib import Path
from zipfile import ZipFile
import hashlib
import json
import subprocess
from PIL import Image, ImageOps

ROOT = Path(__file__).resolve().parents[1]
NORMAL = (500, 380)  # 原生 Portrait: 250x190, KeepAspectCentered
ANCIENT = (598, 842)  # 原生 AncientPortrait: 598x842, Scale
assets_path = ROOT / 'docs/assets.json'
assets = json.loads(assets_path.read_text('utf-8-sig'))


def save(name, image, source, processing, crop):
    target = ROOT / f'FrierenMod/images/cards/{name}.png'
    image.save(target)
    relative = target.relative_to(ROOT).as_posix()
    record = next(a for a in assets if a['file'] == relative)
    record.update(source=source, sha256=hashlib.sha256(target.read_bytes()).hexdigest(),
                  modified=True, processing=processing, crop=list(crop))


def extend_transparent_background(image):
    # 一代原能力图的椭圆外是透明像素，转RGB会错误地变成黑角。
    # 保留所有可见原像素；仅把每行透明边角延展为最近的原背景像素。
    # 不缩进两轴、不移动人物、不生成或绘制新内容。
    image = image.convert('RGBA')
    pixels = image.load()
    for y in range(image.height):
        opaque = [x for x in range(image.width) if pixels[x, y][3] == 255]
        if not opaque:
            raise ValueError('裁剪包含整行透明像素，请调整构图')
        left, right = opaque[0], opaque[-1]
        for x in range(left):
            pixels[x, y] = pixels[left, y]
        for x in range(right + 1, image.width):
            pixels[x, y] = pixels[right, y]
    return image.convert('RGB')


# 用固定Git快照保留作者原始图及Word裁剪，不依赖后续文档的图片编号。
with ZipFile(BytesIO(subprocess.check_output(['git', 'show', 'af59100:TODOs.docx'], cwd=ROOT))) as word:
    for name, media, word_crop, size in [
        ('RingletForm', 'image3.png', (.21572, .03226, 0, .07103), NORMAL),
    ]:
        image = Image.open(BytesIO(word.read('word/media/' + media))).convert('RGB')
        w, h = image.size
        l, t, r, b = word_crop
        box = (round(w*l), round(h*t), round(w*(1-r)), round(h*(1-b)))
        image = ImageOps.fit(image.crop(box), size, method=Image.Resampling.LANCZOS)
        save(name, image, 'TODOs.docx@af59100:word/media/' + media,
             f'先应用作者Word裁剪{word_crop}，再等比裁剪为{size[0]}x{size[1]}；保留脸部完整，无拉伸。', box)

originals = ROOT.parent / 'FrierenMod_STS1_source code/src/main/resources/FrierenModResources/img/cards'
# 每张单独构图；飞行保留两个人，饰品保留法杖和人物，仪式保留完整头部与手中书笔。
for name, box in {
    'AccessoriesSpell': (150, 35, 500, 301),
    'FlyingMagic': (35, 20, 485, 362),
    'RitesPreparation': (110, 5, 500, 301),
}.items():
    path = originals / f'{name}_p.png'
    image = extend_transparent_background(Image.open(path).crop(box))
    image = ImageOps.fit(image, NORMAL, method=Image.Resampling.LANCZOS)
    save(name, image, path.relative_to(originals.parents[5]).as_posix(),
         '按主体单独等比裁剪为500x380；保留人物与原场景相对位置；仅透明边角延展最近原背景像素，避免转RGB形成黑角，无重绘。', box)

source = '0b0af3f:FrierenMod/images/cards/GoodLuckSpell.png'
image = Image.open(BytesIO(subprocess.check_output(['git', 'show', source], cwd=ROOT))).convert('RGB')
image = ImageOps.fit(image, NORMAL, method=Image.Resampling.LANCZOS, centering=(.5, .35))
save('GoodLuckSpell', image, '作者2026-10-31上传PNG原图@' + source,
     '原图等比裁剪为500x380，优先保留两张脸；与普通画框比例一致，不留横向黑边。', (112, 0, 753, 487))
assets_path.write_text(json.dumps(assets, ensure_ascii=False, indent=2) + '\n', 'utf-8')
subprocess.run([__import__('sys').executable, str(ROOT / 'tools/prepare_ancient_art.py')], check=True)
