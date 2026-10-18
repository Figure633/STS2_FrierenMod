"""Build a reproducible static source inventory, not a runtime registration proof."""
from pathlib import Path
import collections
import json
import os
import re
import subprocess
import sys

sys.stdout.reconfigure(encoding="utf-8")

ROOT = Path(__file__).resolve().parents[1]
SOURCE = Path(os.environ.get("FRIEREN_STS1_SOURCE", ROOT.parent / "reference" / "FrierenMod_STS1"))
OUT = ROOT / "docs" / "inventory"
OUT.mkdir(parents=True, exist_ok=True)

def git(*args):
    return subprocess.check_output(["git", "-C", str(SOURCE), *args], text=True, encoding="utf-8").strip()

def without_comments(text):
    pattern = r'"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\'|//[^\n]*|/\*[\s\S]*?\*/'
    return re.sub(pattern, lambda m: "" if m[0].startswith(("//", "/*")) else m[0], text)

sha = git("rev-parse", "HEAD")
base = SOURCE / "src/main/java/FrierenMod"
locales = {}
errors = []
for path in sorted((SOURCE / "src/main/resources/FrierenModResources/localization").glob("*/*.json")):
    try:
        locales.setdefault(path.parent.name, {})[path.stem] = json.loads(path.read_text(encoding="utf-8-sig"))
    except (ValueError, UnicodeError) as exc:
        errors.append({"path": path.relative_to(SOURCE).as_posix(), "error": str(exc)})

records = []
categories = {"cards": "cards", "relics": "relics", "potions": "potions", "powers": "powers", "Characters": "characters", "events": "events", "monsters": "monsters"}
registered_card_dirs = {"white", "purple", "tempCards", "optionCards", "magicItems"}
for folder, category in categories.items():
    for path in sorted((base / folder).rglob("*.java")):
        raw = path.read_text(encoding="utf-8-sig")
        code = without_comments(raw)
        decl = re.search(r"public\s+(abstract\s+)?class\s+(\w+)", code)
        class_name = decl[2] if decl else path.stem
        group = path.relative_to(base / folder).parent.as_posix()
        source_path = path.relative_to(SOURCE).as_posix()
        source_line = next((i for i, line in enumerate(raw.splitlines(), 1) if re.search(r"public\s+(?:abstract\s+)?class\s+" + re.escape(class_name) + r"\b", line) and not line.lstrip().startswith("//")), 1)
        excluded = False
        if not decl:
            state, excluded = "无活动类声明/注释存档", True
        elif decl[1]:
            state, excluded = "抽象基类", True
        elif "@AutoAdd.Ignore" in code:
            state, excluded = "显式忽略", True
        elif category == "cards":
            if any(group.startswith(prefix) for prefix in registered_card_dirs):
                state = "自动扫描目录候选"
            else:
                state = "不在直接扫描目录/需追踪引用"
        elif category == "characters":
            state = "默认关闭" if class_name == "Stark" else "已注册且默认启用"
            excluded = class_name == "Stark"
        elif category == "relics":
            state = "自动扫描目录候选"
        elif category == "potions":
            state = "显式注册"
        elif category == "events":
            state = "显式注册" if class_name in {"FoodEvent", "KraftGift", "AnimalWell", "MimicFight"} else "专用流程/需追踪引用"
        elif category == "monsters":
            state = "注册入口已调用/生成条件需复核"
        else:
            state = "由效果引用/需追踪可达性"
        old_id = "FrierenMod:" + class_name
        loc = locales.get("ZHS", {}).get(category, {}).get(old_id, {})
        name = loc.get("NAME") or (loc.get("NAMES") or [""])[0]
        id_verified = bool(re.search(r"makeID\(\s*" + re.escape(class_name) + r"\.class\.getSimpleName\(\)\s*\)", code))
        tags = sorted(set(re.findall(r"(?:Enum|CardTags)\.(\w+)", code)))
        info = re.search(r"new CardInfo\(ID,\s*([^,]+),\s*CardType\.(\w+),\s*([^,]+),\s*CardRarity\.(\w+),\s*CardTarget\.(\w+)", code)
        disposition = "不纳入默认移植" if excluded else "需行为核对后移植"
        if category == "cards" and group.split("/")[0] in {"orange", "testCards"}:
            disposition = "先追踪调用；不得计为独立卡池"
        if category == "cards" and group == "whitePurple":
            disposition = "按white前缀扫描；核对双色二次注册"
        if class_name == "StarkRelic":
            disposition = "注册与关闭角色不一致，先核对获取条件"
        records.append({
            "category": category, "group": group, "class": class_name,
            "legacy_id_candidate": old_id, "id_expression_verified": id_verified,
            "name_zhs": name, "localization_zhs": loc,
            "registration_evidence": state, "disposition": disposition,
            "implementation_status": "not_started", "tags_observed": tags,
            "card_info_literal": dict(zip(["cost", "type", "color", "rarity", "target"], info.groups())) if info else None,
            "source": source_path, "source_line": source_line,
            "source_url": f"https://github.com/FrierenModForSlayTheSpire/FrierenMod_STS1/blob/{sha}/{source_path}#L{source_line}",
        })

trees = git("-c", "core.quotepath=false", "ls-tree", "-r", "--name-only", "HEAD").splitlines()
resources = [p for p in trees if p.startswith("src/main/resources/")]
local_assets = [p for p in sorted((ROOT.parent / "STS1_素材").rglob("*")) if p.is_file()]
local_source = ROOT.parent / "STS1_素材" / "FrierenMod_STS1_source code"
comparison = {"same_java": 0, "different_java": [], "missing_java": []}
for path in sorted(base.rglob("*.java")):
    rel = path.relative_to(SOURCE)
    other = local_source / rel
    if not other.exists():
        comparison["missing_java"].append(rel.as_posix())
    elif path.read_text(encoding="utf-8-sig") == other.read_text(encoding="utf-8-sig"):
        comparison["same_java"] += 1
    else:
        comparison["different_java"].append(rel.as_posix())
locale_gaps = {}
for lang, sets in locales.items():
    locale_gaps[lang] = {}
    for kind, baseline in locales.get("ZHS", {}).items():
        if isinstance(baseline, dict):
            actual = sets.get(kind, {})
            locale_gaps[lang][kind] = {"missing_vs_zhs": sorted(set(baseline) - set(actual)), "extra_vs_zhs": sorted(set(actual) - set(baseline))}
summary = {
    "audit_date": "2026-10-17", "source_commit": sha,
    "source_manifest_version": json.loads((SOURCE / "src/main/resources/ModTheSpire.json").read_text(encoding="utf-8-sig"))["version"],
    "java_file_count": len(list(base.rglob("*.java"))),
    "content_file_count": len(records),
    "categories": dict(collections.Counter(r["category"] for r in records)),
    "card_groups": dict(collections.Counter(r["group"] for r in records if r["category"] == "cards")),
    "registration_states": dict(collections.Counter(r["registration_evidence"] for r in records)),
    "resource_file_count": len(resources),
    "resource_extensions": dict(collections.Counter(Path(p).suffix.lower() for p in resources)),
    "local_asset_file_count": len(local_assets),
    "local_asset_bytes": sum(p.stat().st_size for p in local_assets),
    "local_asset_extensions": dict(collections.Counter(p.suffix.lower() for p in local_assets)),
    "localization_counts": {lang: {kind: len(data) for kind, data in sets.items()} for lang, sets in locales.items()},
    "localization_parse_errors": errors,
    "local_source_comparison": comparison,
    "package_filter_basis": "BaseMod master AutoAdd.PackageFilter.accept uses startsWith; exact historical runtime jar not verified",
    "limitations": ["文件数不是游戏可获取内容数", "自动扫描不证明运行时注册成功", "ID仅匹配常见表达式，未匹配者需人工核对", "本地素材仅枚举路径与大小，未打开文档或审查图片", "未下载远程大型美术音频文件", "词条未验证翻译质量或语义一致性"]
}
for name, data in [("summary.json", summary), ("content.json", records), ("locale_gaps.json", locale_gaps), ("resource_paths.json", resources), ("local_assets.json", [{"path": p.relative_to(ROOT.parent).as_posix(), "bytes": p.stat().st_size} for p in local_assets])]:
    (OUT / name).write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

def cell(value):
    return str(value).replace("|", "\\|").replace("\n", " ").replace("\r", "")

lines = ["# 一代内容静态清单", "", f"来源提交：`{sha}`。生成工具：`tools/audit_sts1.py`。", "", "文件级清单，不等于可获取内容总数。状态来自默认开关、注册目录和忽略标记；未运行一代游戏。每一项的代码、中文文本和基础卡牌字面量保存在 [content.json](content.json)。所有条目的二代实现状态均为未开始。", "", "按 [BaseMod维护仓库的PackageFilter实现](https://github.com/daviscook477/BaseMod/blob/master/mod/src/main/java/basemod/AutoAdd.java)，包名使用前缀匹配，因此white扫描包含whitePurple；这些双色牌还定义了二次注册。未核验历史运行时BaseMod.jar，仍按候选计数。辅助房间、抽象类、历史注释文件也列出，避免漏审。", ""]
for category in categories.values():
    items = [r for r in records if r["category"] == category]
    lines += [f"## {category}（{len(items)} 个文件）", "", "| 类 / 来源 | 中文名 | 组 | 静态证据 | 处置 |", "|---|---|---|---|---|"]
    for r in items:
        lines.append(f"| [{r['class']}]({r['source_url']}) | {cell(r['name_zhs']) or '—'} | {r['group']} | {r['registration_evidence']} | {r['disposition']} |")
    lines.append("")
(OUT / "README.md").write_text("\n".join(lines), encoding="utf-8")
print(json.dumps(summary, ensure_ascii=False, indent=2))
