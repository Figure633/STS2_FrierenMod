"""按作者最新两张原图排先古插画，主体放在原生正文遮挡区上方。"""
from pathlib import Path
import hashlib
import json
from PIL import Image, ImageOps, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
SOURCES = ROOT / '../开发资料/assets/card_sources'
SIZE = (598, 842)
assets_path = ROOT / 'tools/data/assets.json'
assets = json.loads(assets_path.read_text('utf-8-sig'))


def record(path, source, processing, modified=True):
    relative = path.relative_to(ROOT).as_posix()
    item = next((a for a in assets if a['file'] == relative), None)
    if item is None:
        item = {'file': relative}
        assets.append(item)
    item.update(source=source, processing=processing, modified=modified,
                sha256=hashlib.sha256(path.read_bytes()).hexdigest())
    item.pop('crop', None)


def save(name, image, processing):
    path = ROOT / f'FrierenMod/images/cards/{name}.png'
    image.save(path)
    record(path, f'../开发资料/assets/card_sources/{name}.png', processing)


flow = Image.open(SOURCES / 'EndlessFlow.png').convert('RGB')
# 整张对称原图保留在上半幅；左右仅镜像延展边缘花草，不裁掉或翻转中央人物。
height = 430
width = round(flow.width * height / flow.height)
main = flow.resize((width, height), Image.Resampling.LANCZOS)
left = (SIZE[0] - width) // 2
upper = Image.new('RGB', (SIZE[0], height))
upper.paste(ImageOps.mirror(main.crop((0, 0, left, height))), (0, 0))
upper.paste(main, (left, 0))
right = SIZE[0] - left - width
upper.paste(ImageOps.mirror(main.crop((width-right, 0, width, height))), (left+width, 0))
canvas = ImageOps.fit(flow.crop((0, round(flow.height*.68), flow.width, flow.height)), SIZE,
                      method=Image.Resampling.LANCZOS)
canvas.paste(upper, (0, 0))
save('EndlessFlow', canvas, '原图完整等比缩至上半幅598x430内居中，保留上下左右对称构图与中央人物；左右仅镜像延展花草边缘；下方以原图底部花草铺底，留给原生正文遮挡。无主体拉伸或重绘。')

slayer = Image.open(SOURCES / 'FrierenTheSlayer.png').convert('RGB')
# 原图满宽等比缩放后上移80px，眼睛约y210、下巴约y365，均在类型牌/正文上方。
main = slayer.resize((598, round(slayer.height*598/slayer.width)), Image.Resampling.LANCZOS)
canvas = ImageOps.fit(slayer.crop((0, round(slayer.height*.76), slayer.width, slayer.height)), SIZE,
                      method=Image.Resampling.LANCZOS)
mask = Image.new('L', main.size, 255)
draw = ImageDraw.Draw(mask)
for y in range(main.height-80, main.height):
    draw.line((0, y, main.width, y), fill=round(255*(main.height-1-y)/79))
canvas.paste(main, (0, -80), mask)
save('FrierenTheSlayer', canvas, '原图等比缩放为598px宽，上移80px；脸部位于上半幅约y125至365，避开类型牌和正文。仅正文遮挡区内把原衣服/花草背景接续铺底，不改人物像素或五官。')

for name, filename in [('EndlessFlow','codex-clipboard-9d611e36-7e7e-4550-a9f9-5593674ed368.png'),
                       ('FrierenTheSlayer','codex-clipboard-2a4b9a06-da24-498e-8d0c-b7891ea885a2.png')]:
    record(SOURCES / f'{name}.png', '作者2026-10-31上传：'+filename, '作者原图，原样保存，不装入游戏资源包。', False)
assets_path.write_text(json.dumps(assets, ensure_ascii=False, indent=2)+'\n', 'utf-8')

# 可叠到修图软件最上层的透明参考图；导出卡图时隐藏。
guide = Image.new('RGBA', SIZE, (0,0,0,0))
draw = ImageDraw.Draw(guide)
font = ImageFont.truetype('C:/Windows/Fonts/msyh.ttc', 21)
draw.rectangle((0,0,597,120), fill=(210,45,45,65))
draw.rectangle((0,390,597,841), fill=(210,45,45,90))
draw.rectangle((65,135,533,370), outline=(20,200,100,230), width=4)
draw.text((18,20),'标题可能遮挡',font=font,fill=(255,255,255,255))
draw.text((85,145),'脸和重要元素尽量放在此区域',font=font,fill=(10,100,45,255))
draw.text((18,480),'下半部分会被正文挡住',font=font,fill=(255,255,255,255))
draw.text((18,520),'这是参考层，导出卡图前隐藏',font=font,fill=(255,255,255,255))
guide.save(SOURCES / '先古卡遮挡参考层.png')
