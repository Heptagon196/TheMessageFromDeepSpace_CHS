from __future__ import annotations

import argparse
import json
from pathlib import Path
from typing import Any


PROJECT_DIR = Path(__file__).resolve().parents[1]


# These are additional Chinese inputs. The stock English condition is never
# removed and is deliberately not duplicated in this table.
ALIASES: dict[str, list[str]] = {
    "POSITIVE": ["正", "正数", "正值", "阳性"],
    "NEGATIVE": ["负", "负数", "负值", "阴性"],
    "MUTUAL": ["相互", "互相", "双向"],
    "LANDLUBBER": ["旱鸭子", "陆地佬"],
    "TERR": ["陆", "地"],
    "ROST": ["喙", "鸟嘴"],
    "COLDSIDE": ["冷面", "寒冷面"],
    "NIGHTSIDE": ["夜面", "背阳面"],
    "DAYSIDE": ["昼面", "向阳面"],
    "HOTSIDE": ["热面", "炎热面"],
    "OCEAN": ["海洋", "大海"],
    "SIDE": ["侧", "面", "一边"],
    "PREHISTORIC": ["史前", "远古"],
    "TEMPERATURE": ["温度"],
    "FEEL": ["感觉", "感受"],
    "OBSERVE": ["观察", "观测"],
    "PART": ["部分", "部件", "一部分"],
    "EMOTION": ["情绪", "情感"],
    "OLD": ["老", "年长", "老年"],
    "MIDDLEAGE": ["中年", "成年"],
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
    "SHEEN": ["希恩"],
    "ME": ["我", "自己"],
    "ALAN": ["艾伦", "埃克斯", "艾伦·埃克斯"],
    "BAUTISTA": ["巴蒂斯塔", "布莱恩", "布莱恩·巴蒂斯塔"],
    "COLLINS": ["柯林斯", "凯莉", "凯莉·柯林斯"],
    "HUSBAND": ["丈夫", "老公", "先生", "配偶"],
    "WIFE": ["妻子", "老婆", "太太", "配偶"],
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
    "MOST": ["最多", "最", "最大"],
    "LEAST": ["最少", "最低"],
    "SMALLEST": ["最小", "最小的"],
    "ZERO": ["零", "没有", "空"],
    "SUPERLATIVE": ["最高级", "最"],
    "HYCEAN": ["海氢", "海氢行星"],
    "BE": ["是", "为"],
    "INGROUP": ["组内", "集合内"],
    "SUBSET": ["子集"],
    "NUETRONSTAR": ["种子星"],
    "WHITEDWARF": ["白矮星"],
    "ROTATE": ["旋转", "转动"],
    "DECOMPOSE": ["分解", "拆解"],
    "DESTROY": ["摧毁", "破坏", "毁掉"],
    "PREP": ["介词"],
    "TO": ["到", "向", "朝"],
    "ACTION": ["动作", "行动"],
    "CONJUNCTION": ["连词"],
    "PREPOSITION": ["介词"],
    "ALTERNATIVE": ["或者", "另一种", "备选"],
    "OPTION": ["选项", "选择"],
    "WEIGHT": ["重量"],
    "DISTANCEUNIT": ["距离单位", "长度单位"],
    "METER": ["米", "公尺"],
    "HELISEC": ["氦秒", "埃克斯秒", "外星秒"],
    "HELIUM": ["氦"],
    "SECOND": ["秒"],
    "TIMEUNIT": ["时间单位"],
    "UNIT": ["单位", "计量单位"],
    "LIGHTSPEED": ["光速"],
    "NUETRON": ["种子"],
    "CHEMREACT": ["化学反应"],
    "REACT": ["反应"],
    "ELEMENT": ["元素"],
    "ATOM": ["原子"],
    "VIZ": ["图", "图像", "可视化"],
    "BALL": ["球", "球体"],
    "DOT": ["点", "圆点"],
    "PIXEL": ["像素"],
    "VISUAL": ["图像", "视觉对象", "可视对象"],
    "VOBJ": ["可视对象"],
    "EARTH": ["地球"],
    "HUMANITY": ["人类"],
    "HUMANS": ["人类", "人"],
    "ALIENS": ["外星人", "异星人"],
    "COMPUTER": ["电脑", "计算机"],
    "METEOR": ["陨石", "流星"],
    "MASSAGE": ["按摩"],
    "MESSAGE": ["讯息", "消息", "信息"],
    "MSG": ["讯息", "消息", "信息"],
    "SIGNAL": ["信号"],
    "ADDCOORDS": ["平移", "移动", "偏移"],
    "MAKE": ["制造", "制作", "生成", "构造"],
    "THEREFORE": ["所以", "因此"],
    "LESSER": ["小于", "较小"],
    "NOTEQUAL": ["不等于", "不相等"],
    "GREATER": ["大于", "较大"],
    "FLIP": ["反转", "翻转", "取反"],
    "NOT": ["非", "不是", "取反"],
    "INCORRECT": ["错误", "不正确"],
    "TRUE": ["真", "正确"],
    "CORRECT": ["正确", "对"],
    "FALSE": ["假", "错误", "错"],
    "PROPOSITION": ["命题"],
    "VALUE": ["值", "数值"],
    "VAR": ["变量"],
    "DECIMAL": ["小数", "十进制"],
    "FLOAT": ["浮点", "浮点数", "小数"],
    "OCTAL": ["八进制"],
    "MULTIPLY": ["乘", "乘法"],
    "ADD": ["加", "加法"],
    "PLUS": ["加", "加号", "加法"],
    "EQUALS": ["等于", "相等"],
    "AND": ["和", "与", "并且"],
    "WITH": ["和", "与", "一起"],
    "APPLE": ["苹果"],
    "ENDNUM": ["末数", "末尾数字"],
    "PLUSONE": ["加一", "加上一个"],
    "SKIP": ["跳过", "间隔"],
    "SPACE": ["空格", "间隔"],
    "ANS": ["答案", "回答"],
}


# Same English word can mean something different for a specific term.
OVERRIDES: dict[tuple[int | None, str], list[str]] = {
    (-121, "TIME"): ["时间", "时刻", "在……时"],
    (-106, "MOST"): ["最", "最高级", "最最"],
    (-65, "TIME"): ["时间", "变化参数"],
    (-51, "Z"): ["高度", "垂直坐标", "Z 坐标"],
    (-50, "Y"): ["深度", "纵坐标", "Y 坐标"],
    (-49, "X"): ["宽度", "横坐标", "X 坐标"],
    (-42, "F"): ["频率"],
    (-41, "FROM"): ["从", "来自"],
    (-41, "TO"): ["到", "至", "终点"],
    (-40, "FROM"): ["从", "来自", "起点"],
    (-36, "THEN"): ["所以", "那么", "于是"],
    (-31, "|"): ["或", "或者", "｜"],
    (-15, "("): ["（"],
    (-12, "SEVEN"): ["七", "填空", "回答"],
    (-11, "SEVEN"): ["七"],
    (-6, "X"): ["乘", "乘号", "×"],
    (-2, "_"): ["＿", "下划线"],
    (None, "?"): ["？"],
}


def make_rules(term_id: int | None, english: str, mode: str) -> list[dict[str, Any]]:
    if term_id is None and english == "IDK":
        return [{"type": "contains", "values": ["不知道"]}]
    if term_id is None and english == "IDFK":
        return [
            {"type": "contains", "values": ["md不知道", "tm不知道"]},
            {"type": "contains_all", "values": ["妈", "不知道"]},
        ]
    if term_id is None and english == "ASDF":
        return [{"type": "exact", "values": ["乱打的", "随便打的", "键盘乱敲"]}]
    values = OVERRIDES.get((term_id, english), ALIASES.get(english, []))
    if not values:
        # Language-neutral symbols and one-letter identifiers keep working via
        # the stock English condition; there is no fabricated Chinese alias.
        return []
    return [{"type": "contains" if mode == "contains" else "exact", "values": values}]


def note_for(term_id: int | None, english: str, rules: list[dict[str, Any]]) -> str:
    if term_id is None and english in {"IDK", "IDFK", "ASDF"}:
        return "全局彩蛋；中文条件为附加触发，原英文缩写仍有效。"
    if english in {"NUETRON", "NUETRONSTAR"}:
        return "故意拼错彩蛋：用同音输入错误“种子”保留原对白的纠错笑点。"
    if english == "MASSAGE":
        return "message/massage 彩蛋；中文用“按摩”承接“媒介即按摩”的对白。"
    if english in {"ALAN", "BAUTISTA", "COLLINS", "HUSBAND", "WIFE", "ME"}:
        return "人名或人物关系的中文附加触发。"
    if not rules:
        return "语言无关的符号或标识；仅保留原版触发。"
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
    args = parser.parse_args()
    source = json.loads(args.source.read_text(encoding="utf-8"))
    entries: list[dict[str, Any]] = []
    ainiee: list[dict[str, Any]] = []
    for index, item in enumerate(source["entries"], start=1):
        term_id = item["term_id"]
        english = item["english_trigger"]
        mode = item["match_mode"]
        rules = make_rules(term_id, english, mode)
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
    payload = {
        "format_version": 1,
        "description": "词典命名对白的中文附加触发规则；原版英文触发始终保留。",
        "matching": "同一条目的 rules 按 OR 组合；contains_all 内部按 AND 组合。",
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
