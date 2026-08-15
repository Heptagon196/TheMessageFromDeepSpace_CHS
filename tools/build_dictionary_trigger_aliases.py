from __future__ import annotations

import argparse
import json
from pathlib import Path
from typing import Any

from dictionary_dialogue_fixes import (
    apply_to_alias_entries,
    load_fixes,
    validate_against_source,
)
from dictionary_trigger_conflicts import (
    condition_key,
    find_conflicts,
    format_conflict,
    normalize,
    validate_no_conflicts,
)


PROJECT_DIR = Path(__file__).resolve().parents[1]


# These are additional Chinese inputs. The stock English condition is never
# removed and is deliberately not duplicated in this table.
ALIASES: dict[str, list[str]] = {
    # The stock POSITIVE condition points at the unrelated Landlubber dialogue.
    # Keep that original English behavior intact, but do not extend it to Chinese.
    "POSITIVE": [],
    "NEGATIVE": ["负", "负数", "负值", "阴性"],
    "MUTUAL": ["相互", "互相", "双向"],
    "LANDLUBBER": ["旱鸭子"],
    "TERR": ["泰拉", "大地"],
    "ROST": ["喙", "鸟嘴"],
    "COLDSIDE": ["冷面", "寒冷面"],
    "NIGHTSIDE": ["夜面", "背阳面", "阴面"],
    "DAYSIDE": ["昼面", "向阳面", "阳面"],
    "HOTSIDE": ["热面", "炎热面"],
    "OCEAN": ["海洋", "大海"],
    "SIDE": ["侧", "面", "一边"],
    "PREHISTORIC": ["史前", "远古"],
    "TEMPERATURE": ["温度"],
    "FEEL": ["感觉", "感受"],
    "OBSERVE": ["观察", "观测"],
    "PART": ["部分", "部件", "一部分"],
    "EMOTION": ["情绪", "情感"],
    "OLD": ["年老", "年长", "老年"],
    "MIDDLEAGE": ["中年", "中年期"],
    "YOUNG": ["年轻", "幼年", "幼体"],
    "BRAIN": ["脑", "大脑"],
    "CORE": ["核心", "中枢"],
    "ARM": ["手臂", "胳膊"],
    "LEG": ["腿"],
    "LIMB": ["肢体", "四肢"],
    "SEX": ["性", "性行为", "交配"],
    "KEPLER": ["开普勒"],
    "SUN": ["太阳"],
    "ERID": ["埃里德"],
    "SHEEN": ["光泽", "亮泽", "光彩"],
    "ME": ["我", "自己"],
    "ALAN": ["艾伦", "埃克斯", "艾伦·埃克斯"],
    "BAUTISTA": ["巴蒂斯塔", "布莱恩", "布莱恩·巴蒂斯塔"],
    "COLLINS": ["柯林斯", "凯莉", "凯莉·柯林斯"],
    "HUSBAND": ["丈夫", "老公"],
    "WIFE": ["妻子", "老婆", "太太"],
    "DESIRE": ["愿望", "渴望"],
    "GOAL": ["目标", "目的"],
    "HOPE": ["希望"],
    "WANT": ["想要", "想"],
    "THEN": ["然后", "接着", "下一步"],
    "TIME": ["时间", "时刻"],
    "ALMOST": ["几乎", "差不多"],
    "CLOSE": ["接近", "相近"],
    "PORTION": ["少部分", "一小部分"],
    "LIONSHARE": ["大多数", "大头", "绝大部分"],
    "CENTER": ["中心", "中央", "中间"],
    "MEAN": ["平均", "均值", "平均数"],
    "BIGGEST": ["最大", "最大的"],
    "MOST": ["最多", "最"],
    "LEAST": ["最少"],
    "SMALLEST": ["最小", "最小的"],
    "ZERO": ["零", "〇"],
    "SUPERLATIVE": ["毕业评选", "班级之最", "之最"],
    "HYCEAN": ["海氢", "海氢行星"],
    "BE": ["存在"],
    "INGROUP": ["同群", "同组", "同一组"],
    "SUBSET": ["子集"],
    "NUETRONSTAR": ["种子星"],
    "WHITEDWARF": ["白矮星"],
    "ROTATE": ["旋转", "转动"],
    "DECOMPOSE": ["分解", "拆解"],
    "DESTROY": ["摧毁", "破坏", "毁掉"],
    "PREP": [],
    "TO": ["到", "向", "朝"],
    "ACTION": ["动作", "行动"],
    "CONJUNCTION": ["连词"],
    "PREPOSITION": ["介词"],
    "ALTERNATIVE": ["或者", "或"],
    "OPTION": ["选项", "选择"],
    "WEIGHT": ["重量"],
    "DISTANCEUNIT": ["距离单位", "长度单位"],
    "METER": ["米", "公尺"],
    "HELISEC": ["氦秒"],
    "HELIUM": ["氦", "氦气"],
    "SECOND": ["秒"],
    "TIMEUNIT": ["时间单位"],
    "UNIT": ["单位", "计量单位"],
    "LIGHTSPEED": ["光速"],
    "NUETRON": ["种子"],
    "CHEMREACT": [],
    "REACT": ["反应"],
    "ELEMENT": ["元素"],
    "ATOM": ["原子"],
    "VIZ": [],
    "BALL": ["球", "球体"],
    "DOT": ["点", "圆点"],
    "PIXEL": ["像素"],
    "VISUAL": ["视觉", "视觉单位"],
    "VOBJ": [],
    "POINT": ["点"],
    "LINE": ["线", "直线"],
    "SHAPE": ["形状", "多边形"],
    "ELECTRON": ["电子"],
    "NEUTRON": ["中子"],
    "PROTON": ["质子"],
    "EARTH": ["地球"],
    "HUMANITY": ["人类文明", "全人类"],
    "HUMANS": ["人类", "人"],
    "ALIENS": ["外星人", "异星人"],
    "COMPUTER": ["电脑", "计算机"],
    "METEOR": ["流星"],
    "MASSAGE": ["按摩"],
    "MESSAGE": ["讯息", "消息", "信息"],
    "MSG": [],
    "SIGNAL": ["信号"],
    "ADDCOORDS": ["坐标相加", "加坐标"],
    "MAKE": ["使", "让"],
    "THEREFORE": ["所以", "因此", "则"],
    "LESSER": ["小于", "较小"],
    "NOTEQUAL": ["不等于", "不相等"],
    "GREATER": ["大于", "较大"],
    "FLIP": ["反转", "翻转", "位翻转"],
    "NOT": ["非", "逻辑非", "不是"],
    "INCORRECT": ["错误", "不正确"],
    "TRUE": ["真", "正确"],
    "CORRECT": ["正确", "对"],
    "FALSE": ["假", "错误", "错"],
    "PROPOSITION": ["命题"],
    "VALUE": ["值", "数值"],
    "VAR": [],
    "DECIMAL": ["十进制", "小数点"],
    "FLOAT": ["浮点", "浮点数"],
    "OCTAL": ["八进制"],
    "MULTIPLY": ["乘", "乘法"],
    "ADD": ["加", "相加"],
    "PLUS": ["加上", "加号", "正号"],
    "EQUALS": ["等于", "相等"],
    "AND": ["且", "并且"],
    "WITH": ["一起", "一同", "伴随"],
    "APPLE": ["苹果"],
    "ENDNUM": ["数字结束", "数终", "结束数字"],
    "PLUSONE": ["加一", "加上1", "加上 1", "递增一", "递增", "自增"],
    "PRESENT": ["现在"],
    "FREQ": ["频率缩写", "频率简称"],
    "SKIP": ["跳过", "略过"],
    "SPACE": ["空间", "空格"],
    "ANS": [],
}


# Same English word can mean something different for a specific term.
OVERRIDES: dict[tuple[int | None, str], list[str]] = {
    (-121, "TIME"): ["时间", "时刻"],
    (-106, "MOST"): ["最"],
    (-65, "TIME"): ["时间", "时刻"],
    (-51, "Z"): [],
    (-50, "Y"): [],
    (-49, "X"): [],
    (-42, "F"): [],
    (-41, "FROM"): ["从", "来自"],
    (-41, "TO"): ["到", "至"],
    (-40, "FROM"): ["从", "来自"],
    (-36, "THEN"): ["那么", "然后", "接着", "于是"],
    (-31, "|"): [],
    (-15, "("): ["（"],
    (-12, "SEVEN"): ["七", "7", "柒"],
    (-11, "SEVEN"): ["七"],
    (-6, "X"): ["×"],
    (-2, "_"): ["＿", "下划线"],
    (None, "?"): ["？"],
}


# A source condition can use substring matching while its Chinese aliases must
# remain exact to avoid short Chinese words swallowing longer dictionary names.
EXACT_ENTRY_KEYS: set[tuple[int | None, str]] = {
    (-184, "TERR"),
    (-151, "OLD"),
    (-150, "MIDDLEAGE"),
    (-147, "BRAIN"),
    (-99, "INGROUP"),
    (-69, "HELIUM"),
}


# A small number of conditions with the same term/English pair need distinct
# aliases depending on whether the old or new name is being inspected.
ENTRY_VALUE_OVERRIDES: dict[tuple[int | None, str, str], list[str]] = {
    (-40, "EditEntryIDFromName", "FROM"): ["从"],
}


# Key: (term ID, competing channel group, ambiguous Chinese input).
# Value: the source channel/English condition that owns that input. The build
# validates that every item here is both necessary and points at a real owner.
# Reusing an input under different term IDs (for example TO -> 到) is legal.
CONFLICT_OWNERS: dict[
    tuple[int | None, int, str], tuple[str, str]
] = {
    (-168, 1, "远古"): ("EditEntryIDToName", "ANCIENT"),
    (-119, 1, "当前"): ("EditEntryIDToName", "CURRENT"),
    (-77, 1, "或"): ("EditEntryIDToName", "OR"),
    (-67, 1, "光速"): ("EditEntryIDToName", "LIGHTSPEED"),
    (-42, 1, "频率"): ("EditEntryIDToName", "FREQUENCY"),
    (-36, 1, "所以"): ("EditEntryIDToName", "SO"),
    (-28, 1, "错误"): ("EditEntryIDToName", "INCORRECT"),
    (-27, 1, "正确"): ("EditEntryIDToName", "CORRECT"),
    (-2, 1, "加一"): ("EditEntryIDToName", "ADDONE"),
}


def direct_hypothesis_aliases(item: dict[str, Any]) -> list[str]:
    english = str(item["english_trigger"])
    values: list[str] = []
    for hypothesis in item.get("hypotheses", []):
        if str(hypothesis.get("source", "")).casefold() != english.casefold():
            continue
        translated = str(hypothesis.get("translation", "")).strip()
        if translated and translated not in values:
            values.append(translated)
    return values


def deduplicate_rules(rules: list[dict[str, Any]]) -> list[dict[str, Any]]:
    combined: dict[tuple[str, str], dict[str, Any]] = {}
    for rule in rules:
        rule_type = str(rule["type"])
        origin = str(rule.get("_origin", ""))
        key = (rule_type, origin)
        target = combined.setdefault(key, {
            "type": rule_type,
            "values": [],
            "_origin": origin,
            **({"exclude_any": list(rule["exclude_any"])}
               if rule.get("exclude_any") else {}),
        })
        known = {normalize(value) for value in target["values"]}
        for value in rule.get("values", []):
            if normalize(value) and normalize(value) not in known:
                target["values"].append(value)
                known.add(normalize(value))
    return [rule for rule in combined.values() if rule["values"]]


def apply_conflict_owners(entries: list[dict[str, Any]]) -> None:
    encountered: set[tuple[int | None, int, str]] = set()
    for conflict in find_conflicts(entries):
        key = conflict.resolution_key
        owner = CONFLICT_OWNERS.get(key)
        if owner is None:
            raise ValueError("缺少词典触发冲突消歧项：" + format_conflict(conflict))
        encountered.add(key)
        owner_key = (
            conflict.term_id,
            owner[0].casefold(),
            owner[1].casefold(),
        )
        if owner_key not in conflict.conditions:
            raise ValueError(
                f"词典触发冲突 {key!r} 指定的保留条件不存在：{owner!r}"
            )

        for entry in entries:
            entry_key = condition_key(entry)
            if entry_key not in conflict.conditions or entry_key == owner_key:
                continue
            for rule in entry.get("rules", []):
                values = rule.get("values", [])
                matching = [value for value in values if normalize(value) == key[2]]
                if not matching:
                    continue
                if str(rule.get("type", "")).casefold() == "contains_all":
                    raise ValueError(
                        "不能通过删除 contains_all 的一部分来消歧：" +
                        format_conflict(conflict)
                    )
                rule["values"] = [
                    value for value in values if normalize(value) != key[2]
                ]
            entry["rules"] = [rule for rule in entry["rules"] if rule["values"]]

    stale = set(CONFLICT_OWNERS) - encountered
    if stale:
        raise ValueError(
            "词典触发冲突消歧表含有已失效项目：" +
            ", ".join(repr(item) for item in sorted(stale, key=repr))
        )
    validate_no_conflicts(entries)


def make_rules(term_id: int | None, channel: str, english: str,
    mode: str) -> list[dict[str, Any]]:
    if term_id is None and english == "IDK":
        return [{
            "type": "contains",
            "values": ["不知道"],
            "exclude_any": ["妈", "md不知道", "tm不知道"],
            "_origin": "maintained",
        }]
    if term_id is None and english == "IDFK":
        return [
            {"type": "contains", "values": ["md不知道", "tm不知道"], "_origin": "maintained"},
            {"type": "contains_all", "values": ["妈", "不知道"], "_origin": "maintained"},
        ]
    if term_id is None and english == "ASDF":
        return []
    values = list(ENTRY_VALUE_OVERRIDES.get(
        (term_id, channel, english),
        OVERRIDES.get((term_id, english), ALIASES.get(english, [])),
    ))
    if not values:
        # Language-neutral symbols and one-letter identifiers keep working via
        # the stock English condition; there is no fabricated Chinese alias.
        return []
    rule_type = "exact" if (term_id, english) in EXACT_ENTRY_KEYS else (
        "contains" if mode == "contains" else "exact"
    )
    return [{"type": rule_type, "values": values, "_origin": "maintained"}]


def note_for(term_id: int | None, english: str, rules: list[dict[str, Any]]) -> str:
    if not rules:
        return "未提供中文附加触发；仅保留原版英文或符号条件，避免破坏对白语义或缩写笑点。"
    if term_id is None and english in {"IDK", "IDFK", "ASDF"}:
        return "全局彩蛋；中文条件为附加触发，原英文缩写仍有效。"
    if english in {"NUETRON", "NUETRONSTAR"}:
        return "故意拼错彩蛋：用同音输入错误“种子”保留原对白的纠错笑点。"
    if english == "MASSAGE":
        return "message/massage 彩蛋；中文用“按摩”承接“媒介即按摩”的对白。"
    if english in {"ALAN", "BAUTISTA", "COLLINS", "HUSBAND", "WIFE", "ME"}:
        return "人名或人物关系的中文附加触发。"
    return "中文同义词附加触发；原英文条件仍由原版及不区分大小写兼容逻辑处理。"


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--source",
        type=Path,
        default=PROJECT_DIR / "work" / "dictionary_trigger_aliases" / "source.json",
    )
    parser.add_argument(
        "--output",
        type=Path,
        default=PROJECT_DIR / "patch" / "Translations" / "dictionary_trigger_aliases.json",
    )
    parser.add_argument(
        "--ainiee-output",
        type=Path,
        default=PROJECT_DIR / "work" / "dictionary_trigger_aliases" / "translations.json",
    )
    parser.add_argument(
        "--fix-directory",
        type=Path,
        default=PROJECT_DIR / "patch" / "Fix" / "DictionaryDialogue",
    )
    args = parser.parse_args()
    source = json.loads(args.source.read_text(encoding="utf-8"))
    fixes = load_fixes(args.fix_directory)
    validate_against_source(fixes, source)
    entries: list[dict[str, Any]] = []
    ainiee: list[dict[str, Any]] = []
    source_entries = list(source["entries"]) + list(source.get("covered_entries", []))
    for index, item in enumerate(source_entries, start=1):
        term_id = item["term_id"]
        english = item["english_trigger"]
        mode = item["match_mode"]
        rules = make_rules(term_id, item["channel_name"], english, mode)
        hypothesis_values = direct_hypothesis_aliases(item)
        if hypothesis_values:
            rules.append(
                {
                    "type": "contains" if mode == "contains" else "exact",
                    "values": hypothesis_values,
                    "_origin": "hypothesis",
                }
            )
        rules = deduplicate_rules(rules)
        note = note_for(term_id, english, rules)
        entry = {
            "term_id": term_id,
            "channel": item["channel_name"],
            "english": english,
            "rules": rules,
            "dialogue_ids": item["dialogue_chunk_ids"],
            "note": note,
        }
        entries.append(entry)
        ainiee.append(
            {
                "text_index": index,
                "translated_text": json.dumps(
                    {"rules": rules, "note": note}, ensure_ascii=False, separators=(",", ":")
                ),
            }
        )
    apply_to_alias_entries(entries, fixes)
    apply_conflict_owners(entries)
    for entry in entries:
        for rule in entry["rules"]:
            rule.pop("_origin", None)
        entry["rules"] = deduplicate_rules(entry["rules"])
        for rule in entry["rules"]:
            rule.pop("_origin", None)
    ainiee = [
        {
            "text_index": index,
            "translated_text": json.dumps(
                {"rules": entry["rules"], "note": entry["note"]},
                ensure_ascii=False,
                separators=(",", ":"),
            ),
        }
        for index, entry in enumerate(entries, start=1)
    ]
    payload = {
        "format_version": 1,
        "description": "词典命名对白的中文附加触发规则；原版英文触发始终保留。",
        "matching": "同一条件的人工维护词与假说译名按 OR 合并；exclude_any 为明确配置的排除子串。构建时拒绝一次输入可同时命中多个条件的配置，运行时不做跨条件判优先级或消歧。",
        "entries": entries,
    }
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    args.ainiee_output.parent.mkdir(parents=True, exist_ok=True)
    args.ainiee_output.write_text(json.dumps(ainiee, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"output": str(args.output), "entries": len(entries)}, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
