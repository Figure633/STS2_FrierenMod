"""输出源码/JAR 元数据、类路径和资源哈希差异。此检查不等同于字节码行为一致性。"""
import csv
import hashlib
import json
import re
import subprocess
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WORKSPACE = ROOT.parents[1]
SOURCE = WORKSPACE / "references/FrierenMod_STS1"
JAVA = SOURCE / "src/main/java"
RES = SOURCE / "src/main/resources"
JAR = Path("C:/Program Files (x86)/Steam/steamapps/workshop/content/646570/3164722329/FrierenMod.jar")

def commit(path):
    return subprocess.check_output(["git", "-C", str(path), "rev-parse", "HEAD"], text=True).strip()

def main():
    sources = {p.relative_to(JAVA).with_suffix("").as_posix(): p for p in JAVA.rglob("*.java")}
    with zipfile.ZipFile(JAR) as jar:
        classes = {p[:-6] for p in jar.namelist() if p.startswith("FrierenMod/") and p.endswith(".class") and "$" not in p}
        paths = {p.relative_to(RES).as_posix(): p for p in (RES / "FrierenModResources").rglob("*") if p.is_file()}
        jar_paths = {p for p in jar.namelist() if p.startswith("FrierenModResources/") and not p.endswith("/")}
        different = [p for p in sorted(paths.keys() & jar_paths) if paths[p].read_bytes() != jar.read(p)]
        report = {"source_commit": commit(SOURCE), "target_upstream_commit": commit(ROOT),
                  "baselib_reference_commit": commit(WORKSPACE / "references/BaseLib-StS2"),
                  "template_reference_commit": commit(WORKSPACE / "references/ModTemplate-StS2"),
                  "source_mod_info": json.loads((RES / "ModTheSpire.json").read_text(encoding="utf-8")),
                  "jar_mod_info": json.loads(jar.read("ModTheSpire.json")),
                  "jar_sha256": hashlib.sha256(JAR.read_bytes()).hexdigest(),
                  "source_only_classes": sorted(sources.keys() - classes), "jar_only_classes": sorted(classes - sources.keys()),
                  "resources_different": different, "source_only_resources": sorted(paths.keys() - jar_paths),
                  "jar_only_resources": sorted(jar_paths - paths.keys()),
                  "scope": "metadata/class names/resource bytes only; method bodies and numerical rules not compared"}
    (ROOT / "docs/baseline.json").write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    implemented = {"Strike_Frieren", "Defend_Frieren", "Flow", "DefensiveMagic", "RapidChant", "Mana",
                   "SecondDefensiveMagic", "PreciseChant", "FinalChant", "ManaExpansion",
                   "AdvancedDefensiveMagic", "BuildUpBody", "HighSpeedMovement", "HolyEmblem"}
    with (ROOT / "docs/content_inventory.csv").open("w", newline="", encoding="utf-8-sig") as stream:
        writer = csv.writer(stream)
        writer.writerow(["source", "class", "category", "status"])
        for name, path in sorted(sources.items()):
            if not re.search(r"/(cards|relics|potions|powers|events|monsters)/", name):
                continue
            status = "prototype_subset" if path.stem in implemented else "not_ported"
            if "/orange/" in name or "/testCards/" in name: status = "out_of_scope"
            writer.writerow([name + ".java", path.stem, "/".join(name.split("/")[1:-1]), status])
    print(f"Baseline: {len(sources)} Java files, {len(different)} differing resources, "
          f"{len(report['source_only_classes'])} source-only / {len(report['jar_only_classes'])} JAR-only classes")

if __name__ == "__main__":
    main()
