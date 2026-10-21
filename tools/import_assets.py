"""从冻结的一代检出复制素材，并生成二代展示场景及可追溯的资产清单。"""
import hashlib
import json
import shutil
import subprocess
import re
import os
from pathlib import Path
from convert_frieren_visuals import bake

ROOT = Path(__file__).resolve().parents[1]
SOURCE = Path(os.environ.get("FRIEREN_STS1_SOURCE", ROOT.parents[1] / "references/FrierenMod_STS1"))
RES = SOURCE / "src/main/resources/FrierenModResources"
CARD_ART = {name: name for path in sorted((ROOT / "Scripts/Frieren/Cards").rglob("*.cs"))
            for name in re.findall(r"public sealed class (\w+)", path.read_text(encoding="utf8"))}
CARD_ART.update({"StrikeFrieren": "Strike_Frieren", "DefendFrieren": "Defend_Frieren",
                 "SecondDefensiveMagic": "DefensiveMagic", "DrawChantChoice": "AdvancedDefensiveMagic",
                 "HandChantChoice": "BuildUpBody", "DiscardChantChoice": "HighSpeedMovement"})

def main():
    sha = subprocess.check_output(["git", "-C", str(SOURCE), "rev-parse", "HEAD"], text=True).strip()
    records = []
    def copy(source, target):
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(source, target)
        records.append({"file": target.relative_to(ROOT).as_posix(),
                        "source": source.relative_to(SOURCE).as_posix(), "source_commit": sha,
                        "sha256": hashlib.sha256(target.read_bytes()).hexdigest(),
                        "modified": False, "redistribution_review": "pending; local prototype only"})
    for target, source in CARD_ART.items():
        image = RES / f"img/cards/{source}_p.png"
        if not image.exists():
            image = RES / f"img/cards/{source}.png"
        if not image.exists():
            print(f"Placeholder artwork: {target} -> FinalChant")
            image = RES / "img/cards/FinalChant_p.png"
        copy(image, ROOT / f"FrierenMod/images/cards/{target}.png")
    for path in sorted((ROOT / "Scripts/Frieren/Powers").rglob("*.cs")):
        for name in re.findall(r"public sealed class (\w+)", path.read_text(encoding="utf8")):
            source = RES / f"img/powers/{name}_84.png"
            if not source.exists():
                source = RES / "img/relics/HolyEmblem.png"
            copy(source, ROOT / f"FrierenMod/images/powers/{name}.png")
    copy(RES / "img/relics/HolyEmblem.png", ROOT / "FrierenMod/images/relics/HolyEmblem.png")
    # 一代没有单独的轮廓图；实验版复用原图，后续替换为正式描边素材。
    copy(RES / "img/relics/HolyEmblem.png", ROOT / "FrierenMod/images/relics/HolyEmblemOutline.png")
    character = {
        "atlas": "character_ArmatureFrieren", "select": "Character_Portrait",
        "icon": "Character_Button", "corpse": "corpse_character",
        "rest": "shoulder", "rest_sleep": "shoulder2",
        "victory1": "Victory1", "victory2": "Victory2", "victory3": "Victory3",
    }
    for target, source in character.items():
        copy(RES / f"img/char/Frieren/{source}.png", ROOT / f"FrierenMod/images/character/{target}.png")
    for name in ["MPLayer0", "MPLayerDark0", "MPWrap", "MPWrapGolden", "GainMPVFX"]:
        copy(RES / f"img/UI/manaPanel/{name}.png", ROOT / f"FrierenMod/images/ui/{name}.png")
    for name in ["magic_deck", "magic_item_reward"]:
        copy(RES / f"img/UI/icons/{name}.png", ROOT / f"FrierenMod/images/ui/{name}.png")
    copy(RES / "img/UI/campfire/buyProp.png", ROOT / "FrierenMod/images/ui/buy_prop.png")
    for name in ["layer1", "layer2", "layer3", "layer4", "layer1d", "layer2d", "layer3d", "vfx"]:
        copy(RES / f"img/UI/orb/Frieren/{name}.png", ROOT / f"FrierenMod/images/energy/{name}.png")
    # 沿用一代卡牌费用与正文的小能量徽记；底部能量球的红色旋转层直接引用二代原版。
    for source, target in [("card_orb", "card_icon"), ("small_orb", "text_icon")]:
        copy(RES / f"img/cardStyles/Frieren/orb/{source}.png", ROOT / f"FrierenMod/images/energy/{target}.png")
    # 仅迁入当前芙莉莲玩法有播放入口的音频；不混入菲伦、未实现事件和旧主菜单。
    tracks = ["STS_Level1_NewMix_v1.mp3", "STS_Level1-2_v2.mp3", "STS_Level2_NewMix_v1.mp3",
              "STS_Level2-2_v2.mp3", "STS_Level3_v2.mp3", "STS_Level3-2_v2.mp3",
              "STS_Merchant_NewMix_v1.ogg", "STS_Shrine_NewMix_v1.ogg", "STS_EliteBoss_NewMix_v1.ogg",
              "STS_Boss1_NewMix_v1.mp3", "STS_Boss2_NewMix_v1.mp3", "STS_Boss3_NewMix_v1.ogg",
              "I_am_frieren.mp3", "normal_attack.mp3", "Frieren_DeathStinger.mp3", "win.mp3"]
    for name in tracks:
        copy(RES / f"audio/sound/{name}", ROOT / f"FrierenMod/audio/{name}")
    converted = bake()
    inputs = [RES / f"img/char/Frieren/character_ArmatureFrieren.{ext}" for ext in ["json", "atlas"]]
    records.append({"file": converted.relative_to(ROOT).as_posix(),
                    "source": [p.relative_to(SOURCE).as_posix() for p in inputs], "source_commit": sha,
                    "source_sha256": [hashlib.sha256(p.read_bytes()).hexdigest() for p in inputs],
                    "sha256": hashlib.sha256(converted.read_bytes()).hexdigest(), "modified": True,
                    "conversion": "Spine 3.8 Idle -> 24 fps Godot Polygon2D; original speed 1.2; synthetic action reactions",
                    "redistribution_review": "pending; local prototype only"})
    copy(SOURCE / "LICENSE", ROOT / "LICENSE.STS1")
    (ROOT / "docs/assets.json").write_text(json.dumps(records, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"Imported {len(records)} files from {sha}")

if __name__ == "__main__":
    main()
