"""仅同步已移植事件的叙事文本，将一代颜色、摇摆和抖动映射为二代原生标签。"""
from argparse import ArgumentParser
from pathlib import Path
import json
import re

EVENTS = {
    "FoodEvent": ("FOOD_EVENT", ["INITIAL", "FREE", "PACKAGE", "MEAL", "FEAST"]),
    "KraftGift": ("KRAFT_GIFT", ["INITIAL", "MEAT", "MEMBERSHIP", "RING", "LEAVE"]),
    "AnimalWell": ("ANIMAL_WELL", ["INITIAL", "FIRECRACKERS", "FLUTE", "BIRD"]),
    "MimicFight": ("MIMIC_FIGHT", ["INITIAL", "CAUGHT", "EXPLODE", "RESCUE"]),
}
COLORS = {"r": "red", "g": "green", "b": "blue", "y": "gold", "p": "purple"}


def convert(source):
    source = re.sub(r"\bNL\b", "\n", source).strip()
    output, active, emitted = [], [], []
    color = None
    index = 0
    while index < len(source):
        char = source[index]
        if char == "#" and index + 1 < len(source) and source[index + 1] in COLORS:
            color = COLORS[source[index + 1]]
            index += 2
            continue
        if char in "~@":
            effect = "sine" if char == "~" else "jitter"
            if effect in active:
                active.remove(effect)
            else:
                active.append(effect)
            index += 1
            continue
        if char.isspace():
            color = None
        desired = active + ([color] if color else [])
        common = 0
        while common < min(len(desired), len(emitted)) and desired[common] == emitted[common]:
            common += 1
        output.extend(f"[/{tag}]" for tag in reversed(emitted[common:]))
        output.extend(f"[{tag}]" for tag in desired[common:])
        emitted = desired
        output.append(char)
        index += 1
    output.extend(f"[/{tag}]" for tag in reversed(emitted))
    return "".join(output)


if __name__ == "__main__":
    parser = ArgumentParser(description=__doc__)
    parser.add_argument("source", type=Path, help="作者提供的一代源码根目录")
    parser.add_argument("--write", action="store_true", help="明确指定后写入；默认仅检查")
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[1]
    changes = 0
    for language in ["zhs", "eng"]:
        source = args.source / f"src/main/resources/FrierenModResources/localization/{language.upper()}/events.json"
        original = json.loads(source.read_text(encoding="utf-8-sig"))
        path = root / f"FrierenMod/localization/{language}/events.json"
        current = json.loads(path.read_text(encoding="utf-8-sig"))
        before = dict(current)
        for name, (entry, pages) in EVENTS.items():
            for number, page in enumerate(pages):
                key = f"FRIERENMOD-{entry}.pages.{page}.description"
                text = convert(original[f"FrierenMod:{name}"]["DESCRIPTIONS"][number])
                if current[key] != text:
                    current[key] = text
        # 开局事件保留二代流程追加的槽位说明，只还原已有片段的动画形式。
        for key, text in list(current.items()):
            if key.startswith("FRIERENMOD-READING_GRIMOIRE_EVENT."):
                effect = "jitter" if ".BOOK_0.description" in key else "sine"
                current[key] = text.replace("[wave]", f"[{effect}]").replace("[/wave]", f"[/{effect}]").replace("[shake]", "[jitter]").replace("[/shake]", "[/jitter]")
            elif ".pages.INITIAL.options." in key and key.endswith(".description") and any(
                key.startswith(f"FRIERENMOD-{entry}.") for entry, _ in EVENTS.values()
            ):
                # 二代重写过奖励数量及条件，保留其语义，只补回一代的收益与危险强调。
                if not re.search(r"\[green\](?:获得|Obtain |Gain )", text):
                    text = re.sub(r"(?:获得[^。]*。|(?:Obtain|Gain) [^.]*\.)", lambda m: f"[green]{m[0]}[/green]", text, count=1)
                if "ANIMAL_WELL" in key and ".FLUTE." in key:
                    text = text.replace("[gold]", "[green]").replace("[/gold]", "[/green]")
                if "MIMIC_FIGHT" in key:
                    for phrase in ["打开宝箱！", "一决胜负！", "Open the chest", "open the chest", "Fight the Mimic!"]:
                        if f"[red]{phrase}[/red]" not in text:
                            text = text.replace(phrase, f"[red]{phrase}[/red]")
                current[key] = text
        changes += sum(before[key] != text for key, text in current.items())
        if not args.write:
            for key, text in current.items():
                if before[key] != text:
                    print(f"NEEDS_UPDATE {language}/{key}")
        if args.write:
            path.write_text(json.dumps(current, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"EVENT_TEXT_EFFECTS {'UPDATED' if args.write else 'CHECK'}: {changes} entries")
