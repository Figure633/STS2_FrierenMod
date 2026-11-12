"""导入作者的十二张事件预览图；WebP无损解码为PNG，保留原图及尺寸。"""
from pathlib import Path
from PIL import Image
import hashlib
import json
import shutil

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT.parent / "png"
NAMES = ["BugKnight", "DragonBall", "elderRing", "jojo", "nier", "onepiece",
         "p5r", "sekiro", "starve", "terraria", "trail", "Zelda"]
manifest = ROOT / "tools/data/assets.json"
entries = json.loads(manifest.read_text(encoding="utf-8-sig"))
for name in NAMES:
    source = SOURCE / (name + (".webp" if name in ("p5r", "starve") else ".png"))
    image = Image.open(source)
    target = ROOT / "FrierenMod/images/events" / (("" if name == "Zelda" else "Preview") + name + ".png")
    if name == "Zelda" and target.exists() and Image.open(target).size != image.size:
        # 作者已经另存了600x675构图；优先使用该版本，不用下载的竖版覆盖。
        source = ROOT / "../开发资料/assets/author_adjusted/20261108/Zelda.png"
        source.parent.mkdir(parents=True, exist_ok=True)
        if source.exists():
            assert source.read_bytes() == target.read_bytes(), "作者又调整了Zelda，请先核对新构图"
        else:
            shutil.copy2(target, source)
        image = Image.open(source)
    if source.suffix == ".webp":
        converted = source.with_suffix(".png")
        if converted.exists():
            existing = Image.open(converted).convert("RGBA")
            assert existing.size == image.size and existing.tobytes() == image.convert("RGBA").tobytes(), converted
        else:
            image.save(converted, "PNG")
        assert Image.open(converted).convert("RGBA").tobytes() == image.convert("RGBA").tobytes()
        shutil.copy2(converted, target)
    elif image.format == "PNG":
        shutil.copy2(source, target)
    else:
        # 作者下载的十张 .png 实际是JPEG；按真实格式解码为PNG，不能只改后缀。
        if name == "Zelda" and target.exists():
            assert Image.open(target).convert("RGBA").tobytes() == image.convert("RGBA").tobytes(), "保留作者另行修改的Zelda图"
        image.save(target, "PNG")
    assert Image.open(target).convert("RGBA").tobytes() == image.convert("RGBA").tobytes()
    relative = target.relative_to(ROOT).as_posix()
    entries = [entry for entry in entries if entry["file"] != relative]
    source_label = source.relative_to(ROOT).as_posix() if source.is_relative_to(ROOT) else "../png/" + source.name
    entries.append({"file": relative, "source": source_label,
                    "source_kind": "author_provided", "modified": image.format != "PNG",
                    "processing": "保留完整构图和原尺寸；WebP与扩展名为PNG的JPEG均只转换格式；未裁切、锐化或重绘。",
                    "sha256": hashlib.sha256(target.read_bytes()).hexdigest()})
    print(name, image.size, "->", target.name)
manifest.write_text(json.dumps(entries, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
