"""用手工轮廓蒙版抠出作者头像；不生成、不重画、不修改原图RGB像素。"""
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "docs/reference/frieren_avatar_original.png"
OUTPUT = ROOT / "FrierenMod/images/character/icon.png"

# 原图为502x502。沿人物外缘手工描点，以三次贝塞尔曲线保留发丝、耳尖和围巾。
PATH = [
    ("M", 56, 502),
    ("C", 61, 475, 68, 448, 80, 433),
    ("C", 74, 392, 81, 329, 91, 278),
    ("C", 71, 269, 48, 253, 41, 239),
    ("C", 40, 236, 41, 234, 46, 234),
    ("C", 64, 235, 81, 235, 99, 235),
    ("C", 104, 204, 104, 162, 124, 139),
    ("C", 129, 132, 135, 128, 141, 125),
    ("C", 146, 115, 162, 113, 178, 127),
    ("C", 212, 104, 264, 99, 313, 121),
    ("C", 331, 115, 349, 119, 361, 131),
    ("C", 382, 154, 384, 195, 388, 234),
    ("C", 404, 234, 422, 235, 438, 231),
    ("C", 441, 230, 445, 234, 444, 237),
    ("C", 432, 249, 414, 265, 393, 278),
    ("C", 399, 292, 402, 316, 404, 335),
    ("C", 412, 343, 413, 346, 412, 353),
    ("L", 409, 359), ("L", 412, 363), ("L", 409, 370),
    ("L", 410, 377), ("L", 407, 384), ("L", 409, 391),
    ("L", 403, 404), ("L", 402, 416),
    ("C", 406, 426, 408, 440, 406, 448),
    ("C", 414, 463, 418, 483, 423, 502),
    ("L", 56, 502),
]
HOLES = [
    [(152, 307), (155, 309), (158, 307), (159, 301), (163, 306),
     (169, 312), (176, 317), (181, 320), (164, 323), (152, 326)],
    [(330, 311), (336, 308), (339, 311), (339, 320), (333, 326), (329, 323)],
]


def points(path):
    result = []
    current = (0, 0)
    for command, *args in path:
        if command in ("M", "L"):
            current = tuple(args)
            result.append(current)
        else:
            start = current
            a, b, end = args[:2], args[2:4], args[4:6]
            for step in range(1, 33):
                t = step / 32
                result.append(tuple((1-t)**3*start[k] + 3*(1-t)**2*t*a[k]
                                    + 3*(1-t)*t*t*b[k] + t**3*end[k] for k in (0, 1)))
            current = tuple(end)
    return result


def main():
    original = Image.open(SOURCE).convert("RGBA")
    assert original.size == (502, 502)
    factor = 4
    mask = Image.new("L", (original.width * factor, original.height * factor))
    draw = ImageDraw.Draw(mask)
    draw.polygon([(round(x*factor), round(y*factor)) for x, y in points(PATH)], fill=255)
    for hole in HOLES:
        draw.polygon([(round(x*factor), round(y*factor)) for x, y in hole], fill=0)
    mask = mask.resize(original.size, Image.Resampling.LANCZOS)
    original.putalpha(mask)
    # 保留源像素，只裁去人物外围的空白；不缩放、补画或改色。
    original = original.crop((39, 106, 447, 502))
    original.save(OUTPUT)
    print(f"CUTOUT_OK {original.size} -> {OUTPUT}")


if __name__ == "__main__":
    main()
