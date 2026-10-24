"""检查资源、文本、版本及打包边界，不访问游戏进程。"""
import hashlib
import json
import re
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
def load(path):
    def unique(pairs):
        result = {}
        for key, value in pairs:
            assert key not in result, f"duplicate JSON key: {key}"
            result[key] = value
        return result
    return json.loads(path.read_text(encoding="utf-8-sig"), object_pairs_hook=unique)

for table in ["cards", "characters", "relics", "potions", "frieren", "powers", "settings_ui", "rest_site_ui", "ancients", "events", "monsters", "encounters"]:
    eng = load(ROOT / f"FrierenMod/localization/eng/{table}.json")
    zhs = load(ROOT / f"FrierenMod/localization/zhs/{table}.json")
    assert eng.keys() == zhs.keys(), f"localization key mismatch: {table}"
    assert all(eng.values()) and all(zhs.values()), f"empty localization: {table}"
    for language, texts in [("eng", eng), ("zhs", zhs)]:
        for key, text in texts.items():
            if table == "cards" and key == "FRIERENMOD-MULTIPLE_OFFENSIVE_MAGIC.description":
                # 作者指定 X 费正文保留 X+4 和升级后的 X+1，其他程序公式仍禁止。
                text = re.sub(r"X\+(?:4|1)(?!\d)", "X", text)
            if table == "frieren" and key.startswith("FACTOR."):
                # 因子保留 X+1、2X 等咏唱费用或效果规则，仍禁止泄漏程序函数。
                text = re.sub(r"\d*X(?:[+-]\d+)?", "X", text)
            assert not re.search(r"\b(?:max|min|Math\.\w+)\s*\(|\b\d+X\b|\bX[+-]\d", text), f"formula exposed in {language}/{table}/{key}"
    print(f"PASS {table}: {len(eng)} bilingual keys")
for item in load(ROOT / "docs/assets.json"):
    path = ROOT / item["file"]
    assert path.is_file(), str(path)
    assert hashlib.sha256(path.read_bytes()).hexdigest() == item["sha256"], str(path)
print(f"PASS asset provenance: {len(load(ROOT / 'docs/assets.json'))} file hashes")
for item in load(ROOT / "docs/generated_assets.json")["assets"]:
    path = ROOT / item["target"]
    assert hashlib.sha256(path.read_bytes()).hexdigest() == item["sha256"], str(path)
print(f"PASS generated asset provenance: {len(load(ROOT / 'docs/generated_assets.json')['assets'])} file hashes and saved prompts")
manifest = load(ROOT / "FrierenMod.json")
project_version = ET.parse(ROOT / "FrierenMod.csproj").findtext("./PropertyGroup/Version")
assert project_version and manifest["version"] == project_version, "manifest/project version mismatch"
assert manifest["min_game_version"] == "0.107.1"
assert manifest["dependencies"] == [{"id": "BaseLib", "min_version": "3.4.7"}]
assert not list((ROOT / "FrierenMod").rglob("*.dll"))
assert not list((ROOT / "FrierenMod").rglob("*.cs"))
print("PASS manifest and resource boundaries")
