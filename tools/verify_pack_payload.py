"""核验Godot 4.5 PCK目录、逐文件内容及媒体排重；只读工程和发行包。"""
from pathlib import Path
import hashlib
import re
import struct
import sys

ROOT = Path(__file__).resolve().parents[1]
MEDIA_TYPES = {"CompressedTexture2D", "AudioStreamOggVorbis", "AudioStreamMP3", "AudioStreamWAV"}

def digest(stream, size):
    result = hashlib.md5()
    while size:
        block = stream.read(min(size, 1048576))
        assert block, "文件内容提前结束"
        result.update(block)
        size -= len(block)
    return result.digest()

def read_entries(path):
    """返回包内路径到(绝对偏移、字节数、MD5)的映射，不读取工程原文件。"""
    with Path(path).open("rb") as pack:
        return _read_entries(pack)

def _read_entries(pack):
    header = struct.unpack("<6I2Q", pack.read(40))
    assert header[:5] == (0x43504447, 3, 4, 5, 1), f"不支持的PCK格式：{header}"
    assert header[5] == 2, "仅支持未加密、独立目录的PCK"
    pack.seek(header[7])
    count = struct.unpack("<I", pack.read(4))[0]
    entries = {}
    for _ in range(count):
        length = struct.unpack("<I", pack.read(4))[0]
        name = pack.read(length).rstrip(b"\0").decode("utf-8").removeprefix("res://")
        offset, size = struct.unpack("<2Q", pack.read(16))
        checksum = pack.read(16)
        flags = struct.unpack("<I", pack.read(4))[0]
        assert name not in entries and flags == 0, (name, flags)
        assert offset + header[6] + size <= header[7], name
        entries[name] = (offset + header[6], size, checksum)
    return entries

def verify(path):
    entries = read_entries(path)
    with Path(path).open("rb") as pack:
        for name, (offset, size, checksum) in entries.items():
            source = (ROOT / name).resolve()
            assert source.is_relative_to(ROOT) and source.is_file(), name
            assert source.stat().st_size == size, f"文件大小改变：{name}"
            pack.seek(offset)
            assert digest(pack, size) == checksum, f"包内内容损坏：{name}"
            with source.open("rb") as original:
                assert digest(original, size) == checksum, f"包内内容与工程不一致：{name}"

    omitted = 0
    saved = 0
    for name in entries:
        if not name.endswith(".import"):
            continue
        text = (ROOT / name).read_text(encoding="utf-8-sig")
        media_type = re.search(r'^type="([^"]+)"', text, re.M)
        if not media_type or media_type[1] not in MEDIA_TYPES:
            continue
        source = name.removesuffix(".import")
        destinations = set(re.findall(r'"res://(\.godot/imported/[^"]+)"', text))
        assert destinations and destinations.issubset(entries), f"导入副本缺失：{source}"
        assert source not in entries, f"重复原文件仍在包内：{source}"
        omitted += 1
        saved += (ROOT / source).stat().st_size
    assert omitted > 0, "未检查到排重媒体"
    print(f"PACK_PAYLOAD_OK: files={len(entries)} byte_identical={len(entries)} omitted_media={omitted} saved_source_bytes={saved}")

if __name__ == "__main__":
    verify(Path(sys.argv[1]) if len(sys.argv) > 1 else ROOT / "artifacts/FrierenMod/FrierenMod.pck")
