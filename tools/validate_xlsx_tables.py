"""检查Excel原生表列ID及范围；只读，不修写工作簿。"""
import re
import sys
from xml.etree import ElementTree as ET
from zipfile import ZipFile

NS = {"s": "http://schemas.openxmlformats.org/spreadsheetml/2006/main"}


def column_number(address):
    letters = re.fullmatch(r"\$?([A-Z]+)\$?\d+", address)
    if not letters:
        raise ValueError(f"无法识别表格地址：{address}")
    value = 0
    for char in letters[1]:
        value = value * 26 + ord(char) - ord("A") + 1
    return value


def validate(path):
    table_count = column_count = 0
    with ZipFile(path) as archive:
        if archive.testzip() is not None:
            raise ValueError("ZIP条目校验失败")
        content = ET.fromstring(archive.read("[Content_Types].xml"))
        for part in content:
            if not part.get("ContentType", "").endswith("spreadsheetml.table+xml"):
                continue
            name = part.attrib["PartName"].lstrip("/")
            table = ET.fromstring(archive.read(name))
            columns = table.find("s:tableColumns", NS)
            if columns is None:
                raise ValueError(f"{name}缺少列定义")
            ids = [int(col.attrib["id"]) for col in columns]
            if len(ids) != len(set(ids)) or any(i < 1 for i in ids):
                raise ValueError(f"{name}存在重复或无效的表格列ID：{ids}")
            if int(columns.attrib["count"]) != len(columns):
                raise ValueError(f"{name}声明列数与实际不符")
            bounds = table.attrib["ref"].split(":")
            width = column_number(bounds[-1]) - column_number(bounds[0]) + 1
            if width != len(columns):
                raise ValueError(f"{name}范围宽度与列定义不符")
            table_count += 1
            column_count += len(columns)
    print(f"XLSX_TABLES_OK tables={table_count} columns={column_count}")


if __name__ == "__main__":
    try:
        if len(sys.argv) != 2:
            raise ValueError("用法：python tools/validate_xlsx_tables.py <工作簿.xlsx>")
        validate(sys.argv[1])
    except (ValueError, KeyError, ET.ParseError) as error:
        print(f"XLSX_TABLES_FAIL: {error}", file=sys.stderr)
        sys.exit(1)
