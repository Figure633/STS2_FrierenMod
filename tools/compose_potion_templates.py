"""按一代原生瓶型和配色组合药水图层，导出二代的固定图片资源。"""
from argparse import ArgumentParser
from hashlib import sha256
from io import BytesIO
from pathlib import Path
from zipfile import ZipFile
import json

from PIL import Image, ImageChops

ROOT = Path(__file__).resolve().parents[1]
# 对应一代模组构造函数的 PotionSize/PotionColor。
# 颜色和图层顺序来自原版 AbstractPotion.initializeColor/render。
RECIPES = {
    "BottledMana": {"shape": "heart", "palette": "SWIFT", "liquid": "0d429dff", "spots": "00ffffff"},
    "EmperorWine": {"shape": "sphere", "palette": "ENERGY", "liquid": "ffd700ff"},
    "DissolveClothPotion": {"shape": "bottle", "palette": "WEAK", "liquid": "ee82eeff", "hybrid": "b03060ff"},
}

def fit_to_native(icon, outline):
    # 原版二代图标的可见瓶身占满80像素；一代64像素模板自带留白，需统一裁边后等比适配。
    bounds = icon.getchannel("A").getbbox()
    width, height = bounds[2]-bounds[0], bounds[3]-bounds[1]
    scale = 80 / max(width, height)
    size = (round(width * scale), round(height * scale))
    result = []
    for source in (icon, outline):
        canvas = Image.new("RGBA", (80,80))
        canvas.alpha_composite(source.crop(bounds).resize(size, Image.Resampling.LANCZOS), ((80-size[0])//2, (80-size[1])//2))
        result.append(canvas)
    return result


def compose(jar_path: Path):
    records = []
    destination = ROOT / "FrierenMod/images/potions"
    with ZipFile(jar_path) as jar:
        for key, recipe in RECIPES.items():
            prefix = f"images/potion/{recipe['shape']}/"
            sources = []

            def read_layer(name):
                path = prefix + name + ".png"
                data = jar.read(path)
                sources.append({"path": path, "sha256": sha256(data).hexdigest()})
                return Image.open(BytesIO(data)).convert("RGBA")

            icon = Image.new("RGBA", (64, 64))
            for name in ("liquid", "hybrid", "spots"):
                if name in recipe:
                    layer = read_layer(name)
                    tint = tuple(bytes.fromhex(recipe[name]))
                    icon = Image.alpha_composite(icon, ImageChops.multiply(layer, Image.new("RGBA", layer.size, tint)))
            icon = Image.alpha_composite(icon, read_layer("body"))
            outline = read_layer("outline")
            icon, outline = fit_to_native(icon, outline)
            for suffix, image in (("", icon), ("Outline", outline)):
                target = destination / (key + suffix + ".png")
                image.save(target)
                records.append({
                    "file": target.relative_to(ROOT).as_posix(),
                    "source": "desktop-1.0.jar", "source_kind": "local_sts1_game_template",
                    "template": recipe, "layers": sources[:-1] if not suffix else [sources[-1]],
                    "composition": "liquid → optional hybrid → optional spots → body; outline uses native template",
                    "sha256": sha256(target.read_bytes()).hexdigest(), "modified": True,
                })
            print(f"TEMPLATE_OK {key} shape={recipe['shape']} palette={recipe['palette']}")
    return records


if __name__ == "__main__":
    parser = ArgumentParser(description=__doc__)
    parser.add_argument("jar", type=Path, help="本机一代 desktop-1.0.jar")
    parser.add_argument("--records", type=Path, help="写出本次资源来源记录")
    args = parser.parse_args()
    records = compose(args.jar)
    if args.records:
        args.records.write_text(json.dumps(records, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
