"""生成魔具与营地按钮的中英文本；名称沿用一代，描述适配二代交互。"""
import json
import csv
import re
import os
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = Path(os.environ.get("FRIEREN_STS1_SOURCE", ROOT.parents[1] / "references/FrierenMod_STS1")) / "src/main/resources/FrierenModResources"
PROPS = {
    "CrystalBall": ("选择两张已装载的因子，交换它们的槽位。", "Swap the slots of two equipped factors."),
    "MysteryBox": ("获得两张随机魔法因子（不含谜题因子），放入未装载库存。", "Gain two random factors (excluding puzzle factors) in your unequipped inventory."),
    "MysticBox": ("选择三张未装载的因子，随机装入三个槽位；原有因子退回库存。", "Choose three unequipped factors and equip them in random slots. Return the previous factors to inventory."),
    "SeinNote": ("移除两张未装载的因子或其他魔具，回复 10 点生命。", "Remove two unequipped factors or other tools. Heal 10 HP."),
    "ShadowDragonHorn": ("仅在战斗中使用。移除两张未装载的因子或其他魔具，获得 2 点能量。", "Combat only. Remove two unequipped factors or other tools. Gain 2 Energy."),
    "TransmutationCircle": ("将两张因子变为同稀有度的随机因子（不含谜题因子），保留各自槽位。", "Transform two factors into random factors of the same rarity (excluding puzzle factors), keeping their slots."),
    "UnbelievableTool": ("选择一张已装载和一张未装载的因子，用库存因子替换已装载因子；原有因子退回库存。", "Swap an equipped factor with an unequipped factor. Keep the previous factor in inventory."),
    "UnbelievableToolBox": ("移除两张未装载的因子或其他魔具，获得两件神奇妙妙工具。", "Remove two unequipped factors or other tools. Gain two Unbelievable Tools.")
}
TEXT = {
    "BOOK_BUTTON": ("魔法书", "Magic Book"),
    "BOOK": ("魔法书 · 因子与魔具", "Magic Book · Factors & Tools"),
    "BOOK_HINT": ("右键启用魔具，左键选择目标；选满后生效。再次右键魔具或按 Esc 取消。", "Right-click a tool, then left-click its targets. It activates when all targets are chosen. Right-click again or press Esc to cancel."),
    "BOOK_INVENTORY": ("未装载因子与魔具", "Unequipped Factors & Tools"),
    "BOOK_TARGETS": ("{Prop} · 已选 {Selected}/{Amount} · 左键选择目标，Esc 取消", "{Prop} · {Selected}/{Amount} selected · Left-click targets; Esc cancels"),
    "PROP_UNAVAILABLE": ("暂时无法使用：请检查材料数量、装载状态与战斗阶段。", "Unavailable: check target requirements and the current combat phase."),
    "FACTOR_HINT": ("X 表示咏唱强度。因子通过魔具调整装载，战斗中按各牌堆的魔力发动。", "X is Chant intensity. Use tools to change equipped factors; each slot uses its own pile's Mana."),
    "BOOK_LOCKED": ("当前仅可查看：需要完成初始因子选择，且暂只支持单人；选择操作中请等待。", "Read only: finish initial factor selection; tool use currently supports solo runs only. Wait for any active selection."),
    "FACTORS": ("魔法因子", "Factors"),
    "PROPS": ("魔具", "Tools"),
    "NO_PROPS": ("尚无魔具。到营地选择「逛魔具店」，获得两件后在这里使用。", "No tools yet. Choose Magic Shop at a camp to acquire two tools."),
    "USE_PROP": ("使用", "Use"),
    "PROP_USED": ("魔具已生效并消耗，因子与库存已更新。", "Tool consumed. Factors and inventory updated."),
    "PROP_CANCELLED": ("已取消或目标不符合条件，未消耗魔具。请按说明选择目标。", "Cancelled or invalid targets. Tool not consumed. Follow the target requirements."),
    "PROP_ERROR": ("使用过程中出现错误，请查看游戏日志。", "An error occurred. Please check the game log."),
    "SHOP_SELECT": ("选择 {Amount} 件魔具加入魔法牌组；确认后消耗本次营地行动，取消可返回。", "Choose {Amount} tools. Confirm to spend this camp action, or cancel to return."),
    "PROP_SELECT": ("使用「{Prop}」：选择 {Amount} 个目标。取消不消耗魔具。", "Use {Prop}: choose {Amount} targets. Cancel without consuming the tool."),
    "INVENTORY_CONTEXT": ("{Slot}", "{Slot}")
}

def main():
    for index, (lang, source) in enumerate([("zhs", "ZHS"), ("eng", "ENG")]):
        folder = ROOT / "FrierenMod/localization" / lang
        original = json.loads((SOURCE / f"localization/{source}/Cards.json").read_text(encoding="utf8"))
        cards = json.loads((folder / "cards.json").read_text(encoding="utf8"))
        texts = json.loads((folder / "frieren.json").read_text(encoding="utf8"))
        for name, descriptions in PROPS.items():
            key = "FRIERENMOD-" + re.sub(r"(?<!^)(?=[A-Z])", "_", name).upper()
            cards[key + ".title"] = original["FrierenMod:" + name]["NAME"]
            if index == 1 and name == "TransmutationCircle":
                cards[key + ".title"] = "Scientific Transmutation"
            cards[key + ".description"] = descriptions[index]
            texts["PROP." + name] = descriptions[index]
        texts.update({k: v[index] for k, v in TEXT.items()})
        rest = {
            "OPTION_FRIEREN_MAGIC_SHOP.name": ("逛魔具店", "Magic Shop")[index],
            "OPTION_FRIEREN_MAGIC_SHOP.description": ("选择两件魔具加入魔法牌组，然后在魔法书中使用。占用一次营地行动。", "Choose two tools for your Magic Book. Uses one camp action.")[index],
            "OPTION_FRIEREN_MAGIC_SHOP.descriptionDisabled": ("需要完成初始因子选择；魔具交互暂只支持单人。", "Finish initial factor selection. Tool interactions currently support solo runs only.")[index]
        }
        relics = json.loads((folder / "relics.json").read_text(encoding="utf8"))
        relics["FRIERENMOD-HOLY_EMBLEM.description"] = (
            "开局为三个槽位各选择一个初始魔法因子。每回合抽牌后，将一张魔力随机加入抽牌堆，并将一张魔力加入弃牌堆。通过魔法书查看因子并使用魔具调整装载。",
            "Choose an initial factor for each slot. After drawing each turn, add one Mana to your Draw Pile and one to your Discard Pile. View factors and use tools in the Magic Book."
        )[index]
        for table, data in [("cards", cards), ("frieren", texts), ("rest_site_ui", rest), ("relics", relics)]:
            (folder / f"{table}.json").write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf8")
    inventory = ROOT / "docs/content_inventory.csv"
    with inventory.open(encoding="utf8", newline="") as stream:
        entries = list(csv.DictReader(stream))
    for row in entries:
        if row["class"] in PROPS or row["class"] in {"BuyPropOption", "BuyPropOptionPatch", "CampfireBuyPropEffect"}:
            row["status"] = "magic_shop_port_tested"
        elif row["class"] == "MagicDeckScreen":
            row["status"] = "adapted_magic_book_interactions"
    with inventory.open("w", encoding="utf8", newline="") as stream:
        writer = csv.DictWriter(stream, fieldnames=["source", "class", "category", "status"])
        writer.writeheader(); writer.writerows(entries)

if __name__ == "__main__":
    main()
