"""从作者提供的读书坐姿图手工抠图，无生成、补画或原版游戏图片复制。"""
from argparse import ArgumentParser
from hashlib import sha256
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "docs/reference/frieren-rest-source.png"
SOURCE_SHA256 = "960648c4fa4f2414a08b980b6e0278398db5e78cbb0336b52ae3d0da999a0186"
# 在 482×608 作者原图上沿人物与书本外轮廓手工勾选，去掉床和背景。
OUTLINE = [
    (208,44),(224,43),(241,49),(250,57),(259,72),(263,89),(265,111),(264,125),
    (257,139),(253,147),(260,171),(263,184),(265,202),(261,220),(259,236),(264,250),
    (275,262),(291,253),(315,250),(343,247),(318,268),(292,292),(277,305),(286,309),
    (282,316),(269,318),(294,319),(313,325),(330,341),(340,362),(345,387),
    (350,415),(359,451),(368,478),(366,488),(355,496),(350,498),(348,506),(330,508),
    (327,522),(330,540),(333,552),(332,565),(328,571),(323,570),(320,574),(316,573),
    (313,575),(309,572),(306,565),(305,551),(304,538),(302,520),(302,509),(291,511),
    (279,507),(275,514),(275,531),(277,542),(278,553),(281,570),(281,584),(278,589),
    (274,592),(269,591),(267,586),(265,581),(263,579),(260,582),(255,576),(253,563),
    (252,551),(248,546),(247,539),(251,532),(255,524),(256,507),(244,503),(241,491),
    (227,493),(225,489),(228,465),(231,435),(235,411),(241,393),(243,386),(229,381),
    (211,378),(192,372),(176,369),(161,362),(154,351),(149,336),(149,311),(150,291),
    (150,275),(147,269),(143,260),(142,245),(141,229),(141,217),(135,212),(132,206),
    (135,191),(139,173),(145,161),(150,158),(144,151),(143,139),(147,116),(151,96),
    (154,79),(159,63),(167,51),(176,49),(185,53),(197,46),
]


def compose():
    assert sha256(SOURCE.read_bytes()).hexdigest() == SOURCE_SHA256, "原画已改变，需重新核对抠图轮廓"
    image = Image.open(SOURCE).convert("RGBA")
    mask = Image.new("L", (image.width * 4, image.height * 4))
    ImageDraw.Draw(mask).polygon([(round(x * 4), round(y * 4)) for x, y in OUTLINE], fill=255)
    mask = mask.resize(image.size, Image.Resampling.LANCZOS)
    # 清除抗锯齿滤镜远端极低透明度残点，不改变人物颜色。
    mask = mask.point(lambda a: 0 if a < 12 else (255 if a > 243 else a))
    image.putalpha(mask)
    image = image.crop((120, 35, 380, 600)).resize((208, 452), Image.Resampling.LANCZOS)
    canvas = Image.new("RGBA", (256, 480))
    canvas.alpha_composite(image, (24, 14))
    return canvas


if __name__ == "__main__":
    parser = ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, default=ROOT / "artifacts/frieren-rest-original.png")
    args = parser.parse_args()
    args.output.parent.mkdir(parents=True, exist_ok=True)
    compose().save(args.output)
    print(f"REST_ORIGINAL_ART_OK {args.output}")

