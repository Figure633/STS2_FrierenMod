"""打包可独立接手的源码快照；只新增压缩包，不包含本机配置、缓存或游戏依赖。"""
import argparse
import hashlib
import json
from datetime import datetime, timezone
from pathlib import Path
import zipfile

ROOT = Path(__file__).resolve().parents[1]
TOP = "FrierenMod_STS2"
ROOT_FILES = {
    ".editorconfig", ".gitattributes", ".gitignore", "FrierenMod.csproj",
    "FrierenMod.json", "FrierenMod.sln", "GameReferences.props", "global.json",
    "icon.svg", "icon.svg.import", "LICENSE.STS1", "local.props.example",
    "project.godot", "README.md", "先读我-作者接手.md",
}
SOURCE_DIRS = ("Core", "Scripts", "FrierenMod", "Tests", "docs", "tools")
EXCLUDED_DIRS = {".git", ".godot", ".cache", "bin", "obj", "artifacts", "__pycache__", "backups"}
EXCLUDED_SUFFIXES = {".pyc", ".pyo", ".dll", ".pdb", ".pck", ".zip", ".save", ".bak", ".log", ".translation"}


def source_files():
    files = [ROOT / name for name in ROOT_FILES]
    for directory in SOURCE_DIRS:
        for path in (ROOT / directory).rglob("*"):
            relative = path.relative_to(ROOT)
            if any(part in EXCLUDED_DIRS for part in relative.parts):
                continue
            if path.is_symlink():
                raise ValueError(f"拒绝打包符号链接：{relative}")
            if not path.is_file():
                continue
            if path.suffix.lower() in EXCLUDED_SUFFIXES:
                continue
            if path.name in {"local.props", "release_info.json"} or path.name.startswith(".env"):
                continue
            if relative.as_posix().startswith("docs/") and path.name.endswith(".csv.import"):
                continue
            files.append(path)
    for path in files:
        if not path.is_file() or path.is_symlink():
            raise ValueError(f"缺少源码文件或发现链接：{path}")
    return sorted(files, key=lambda path: path.relative_to(ROOT).as_posix())


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, help="新 ZIP 的路径；拒绝覆盖")
    args = parser.parse_args()
    manifest = json.loads((ROOT / "FrierenMod.json").read_text(encoding="utf-8-sig"))
    version = manifest["version"]
    output = (args.output or ROOT.parent / f"FrierenMod_STS2_source_{version}_{datetime.now():%Y%m%d-%H%M%S}.zip").resolve()
    if output.exists() or output.with_suffix(output.suffix + ".sha256").exists():
        raise FileExistsError(f"目标已存在，请使用新文件名：{output}")
    files = source_files()
    baseline = json.loads((ROOT / "docs/baseline.json").read_text(encoding="utf-8-sig"))
    snapshot = {
        "format": 1, "mod_version": version,
        "created_utc": datetime.now(timezone.utc).isoformat(),
        "kind": "complete source working-tree snapshot; no git history; not an upstream commit",
        "source_file_count": len(files),
        "sts2_upstream": {"url": "https://github.com/FrierenModForSlayTheSpire/FrierenMod_STS2", "commit": baseline["target_upstream_commit"]},
        "sts1_reference": {"url": "https://github.com/FrierenModForSlayTheSpire/FrierenMod_STS1", "commit": baseline["source_commit"]},
        "dependencies": {"game": "v0.107.1", "BaseLib": "v3.4.7", "dotnet_sdk": "9.0.306", "Godot": "4.5.1"},
        "pending": ["starting tool + random prop grant", "starting factor guidance", "first-play tutorial"],
        "excluded": ["git history", "build/import caches", "game and BaseLib binaries", "personal settings and saves", "raw machine logs"],
        "integrity": "SHA256SUMS.txt covers every other file, including SOURCE_SNAPSHOT.json",
    }
    checksums = []
    output.parent.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(output, "x", compression=zipfile.ZIP_DEFLATED, compresslevel=6) as archive:
        for path in files:
            relative = path.relative_to(ROOT).as_posix()
            content = path.read_bytes()
            archive.writestr(f"{TOP}/{relative}", content)
            checksums.append(f"{hashlib.sha256(content).hexdigest()}  {relative}")
        content = (json.dumps(snapshot, ensure_ascii=False, indent=2) + "\n").encode("utf-8")
        archive.writestr(f"{TOP}/SOURCE_SNAPSHOT.json", content)
        checksums.append(f"{hashlib.sha256(content).hexdigest()}  SOURCE_SNAPSHOT.json")
        archive.writestr(f"{TOP}/SHA256SUMS.txt", "\n".join(checksums) + "\n")
    # 对 ZIP 中每个实际字节重新校验，避免只验证打包前源文件。
    with zipfile.ZipFile(output) as archive:
        names = archive.namelist()
        assert len(names) == len(set(names)) == len(checksums) + 1
        for line in checksums:
            digest, relative = line.split("  ", 1)
            assert hashlib.sha256(archive.read(f"{TOP}/{relative}")).hexdigest() == digest, relative
    digest = hashlib.sha256(output.read_bytes()).hexdigest()
    checksum_file = output.with_suffix(output.suffix + ".sha256")
    with checksum_file.open("x", encoding="utf-8") as stream:
        stream.write(f"{digest}  {output.name}\n")
    print(json.dumps({"archive": str(output), "source_files": len(files), "archive_entries": len(checksums) + 1,
                      "bytes": output.stat().st_size, "sha256": digest}, ensure_ascii=False))


if __name__ == "__main__":
    main()
