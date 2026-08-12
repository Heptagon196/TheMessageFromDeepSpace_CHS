from __future__ import annotations

import json
from collections import Counter
from pathlib import Path


HERE = Path(__file__).resolve().parent
PROJECT = HERE.parents[1]
SOURCE_PATH = HERE / "source.json"
ALIAS_PATH = PROJECT / "patch" / "Translations" / "dictionary_trigger_aliases.json"
REPORT_PATH = HERE / "semantic_audit.md"


# 结论来自逐条对照英文条件、中文别名和实际对白正文后的人工复核。
# blocker：会稳定触发错误对白或一次输入同时触发多段对白。
# error：至少一个中文别名与对白实际语义/笑点不一致。
# warning：语义大体相近，但范围过宽、表达不自然或容易误触发。
ISSUES: dict[int, tuple[str, str, str]] = {
    2: ("blocker", "`POSITIVE` 实际连到《Landlubber/旱鸭子》，中文输入“正”等会稳定触发完全无关对白。", "删除本条中文 rules；若要保留，先修正游戏数据中的对白引用。"),
    6: ("warning", "“陆/地”是过宽的 contains，容易让大量无关名称触发《Terra》。", "改成精确且独占的“泰拉/大地”等；不要使用单字 contains。"),
    19: ("warning", "单字 contains“老”会把“老师/老板”等无关名称也判为 OLD。", "改为 exact，或只保留“年老/老年/年长”。"),
    20: ("error", "“成年”只表示已成年，不等于“中年”，会触发《Middleage》中年对白。", "删除“成年”，保留“中年/中年期”。"),
    22: ("warning", "单字 contains“脑”会命中“电脑”等并触发《Brain》对白。", "优先 exact“大脑/脑”；若必须 contains，增加边界或排除逻辑。"),
    31: ("error", "`SHEEN` 在该对白中是普通名词“光泽”，不是人名音译“希恩”。", "改为“光泽/亮泽/光彩”。"),
    36: ("blocker", "“配偶”与 `WIFE` 共用，必然同时触发丈夫和妻子对白；“先生”还可能表示称谓。", "删除“配偶/先生”，只留“丈夫/老公”。"),
    37: ("blocker", "“配偶”与 `HUSBAND` 共用，必然同时触发丈夫和妻子对白。", "删除“配偶”，只留“妻子/老婆/太太”。"),
    43: ("warning", "“在……时”包含字面省略号，几乎不是玩家会输入的实际名称。", "删除该值，保留“时间/时刻”；需要句式匹配时另做规范化。"),
    50: ("blocker", "“最大”与同词条 `MOST` 共用，会同时触发《Biggest》和《Most》。", "本条保留“最大/最大的”，从 `MOST` 删除“最大”。"),
    51: ("blocker", "“最大”与 `BIGGEST` 共用，造成双触发。", "删除“最大”，保留“最多”；“最”是否保留取决于该词条的预期词性。"),
    52: ("warning", "“最低”偏向数值下界，不总是 `least` 的“最少”。", "若该词条只表达数量，删除“最低”；否则保留并在说明中限定。"),
    54: ("error", "对白明确讨论数值 0；“没有/空”并不等于零，会触发不相称对白。", "只保留“零”，可补“〇”；删除“没有/空”。"),
    55: ("blocker", "“最”和“最高级”均与 `SUPERLATIVE` 共用，一次输入触发两段语义不同的对白。", "本条只保留副词“最”；删除“最高级/最最”。"),
    56: ("blocker", "与 `MOST` 共用“最/最高级”；而对白谈的是美式毕业评选 superlatives，不是单纯语法最高级。", "使用独占的“毕业评选/班级之最/之最”，并与对白译文统一。"),
    58: ("warning", "“是/为”是系词义，现译对白采用存在义“存在”；两种 `be` 语义未完全对齐。", "锁定为“存在”，或把对白统一改回系词语义后再接受“是/为”。"),
    59: ("warning", "“组内/集合内”是位置或成员关系，现译对白使用二元关系“同群”，词性不一致。", "改为“同群/同组/同一组”；若保留集合义，应同步调整对白。"),
    66: ("error", "对白把 `prep` 解释为 preposition 的缩写；输入完整“介词”后仍谈“简称”，元语言笑点不成立。", "使用独占的中文缩写并同步对白，或不提供中文别名。"),
    71: ("warning", "“或者”符合连接符语义；“另一种/备选”偏名词，而对白展示的是 A/B/C 之间的操作符。", "只保留“或者/或”，把名词义留给 `OPTION`。"),
    77: ("blocker", "“氦秒”会被 `HELIUM` 的 contains“氦”再次命中；“埃克斯秒/外星秒”也不是 `helisec` 的同义词。", "只保留“氦秒”，并把 `HELIUM` 中文规则改为 exact。"),
    78: ("blocker", "contains“氦”会吞掉“氦秒”，稳定同时触发《Helium》和《Helisec》。", "中文别名使用 exact“氦/氦气”，不要沿用英文条件的 contains 模式。"),
    83: ("error", "对白命名并讨论“时间”；“变化参数”只是对时间作用的解释，不是同义名称。", "删除“变化参数”，保留“时间/时刻”。"),
    85: ("error", "对白笑点在 `Chemreact` 是压缩造词；输入完整“化学反应”后仍称其为混成词，语义断裂。", "设计中文缩写并同步对白，或不提供中文别名。"),
    89: ("error", "对白反复讨论 `viz` 这个缩写；输入“图/图像/可视化”后，缩写笑点和指代都不成立。", "不给本条中文别名，或设计中文缩写并重译对白。"),
    93: ("blocker", "“可视对象”与 `VOBJ` 共用，会同时触发《Visual》和《Vobj》。", "本条保留“视觉/视觉单位”；删除“可视对象”。"),
    94: ("blocker", "“可视对象”与 `VISUAL` 共用；且对白明确解释缩写 VOBJ，完整中文名称无法承接缩写笑点。", "不提供中文别名，或设计中文缩写并同步对白。"),
    95: ("error", "对白强调玩家输入的是单字符 `Z`；“高度/垂直坐标/Z 坐标”都会让“才一个字符”的对白失真。", "只依赖原版大小写兼容，不添加概念型中文别名。"),
    96: ("error", "对白明确复述玩家输入单字符 `Y`；中文概念名会与对白事实冲突。", "只保留原版 Y/y 触发。"),
    97: ("error", "对白围绕单字符 `X` 展开；“宽度/横坐标/X 坐标”不是同一次输入。", "只保留原版 X/x 触发。"),
    99: ("blocker", "“人类”与 `HUMANS` 共用，没有其它门控，会同时触发两段对白。", "用“人类文明/全人类”区分 HUMANITY，或只给其中一条中文别名。"),
    100: ("blocker", "“人类”与 `HUMANITY` 共用，必然双触发。", "本条保留“人类/人”，让 HUMANITY 使用独占译法。"),
    103: ("error", "条件表示从错误名称 METEOR 改走；“陨石”本来就是对白认可的 meteorite，纳入后会错误庆祝纠正。", "仅保留“流星”。"),
    104: ("error", "对白会纠正“这是陨石，不是流星”；若玩家输入别名“陨石”，纠正内容与输入相反。", "仅保留“流星”。"),
    106: ("blocker", "“讯息/消息/信息”与 `MSG` 完全共用，会同时触发完整词和缩写两段对白。", "这些中文全称只归 `MESSAGE`。"),
    107: ("blocker", "与 `MESSAGE` 完全共用别名；对白又依赖 MSG 的字符数笑点，中文全称无法承接。", "不提供中文别名，或另造独占缩写并同步对白。"),
    108: ("error", "对白强调单字符 F 且角色认为 F 没意义；输入“频率”后对白事实相反。", "只保留原版 F/f 触发。"),
    111: ("error", "“终点”是名词，对白讨论介词/后置词“到”，语义和词性不符。", "删除“终点”，保留“到/至”。"),
    112: ("error", "“起点”不是介词 FROM；对白还明确争论“从”和“来源”。", "删除“起点”；保留“从”，谨慎保留“来自”。"),
    113: ("error", "该条件检测旧名称 FROM；把旧名“起点”也视为“从”，会触发并不成立的“从改来源”式反应。", "只保留“从”，或限定与新名称的组合条件。"),
    114: ("error", "对白明确把 `Addcoords` 展开为“坐标相加”；“平移/移动/偏移”是可能结果，不是名称同义词。", "改为“坐标相加/加坐标”；若保留平移，应重写对白。"),
    115: ("error", "对白是 `Make(s) sense` 双关，现译为“说得通”；“制造/制作/生成/构造”均接不上。", "使用“使/让”并重新评估双关，或不提供中文别名。"),
    116: ("blocker", "“所以”与 `THEREFORE` 共用；且本对白采用“那么”，不是因果连接词的唯一用法。", "本条使用“那么/然后/接着”，删除“所以”。"),
    117: ("blocker", "“所以”与 `THEN` 共用，会同时触发《Then》和《Therefore》。", "保留“因此/所以”，让 THEN 使用独占译法。"),
    121: ("warning", "原对白赞叹符号 `|` 很简洁；输入“或者”后这句评价弱化。", "保留一字符“或/｜”，删除“或者”或接受轻微笑点损失。"),
    122: ("blocker", "“取反”与 `NOT` 共用，会同时触发位翻转和逻辑非对白。", "本条使用“翻转/位翻转”，删除“取反”。"),
    123: ("blocker", "“取反”与 `FLIP` 共用，造成双触发。", "本条使用“非/逻辑非/不是”，删除“取反”。"),
    130: ("blocker", "`SEVEN` 对白明确确认数字 7；“填空/回答”不是七，而且“回答”还会同时命中全局 `ANS`。", "只保留“七”，可补“7/柒”。"),
    133: ("error", "对白解释 `Var` 是 variable 的英文缩写；输入完整“变量”后仍出现 Var，指代不一致。", "不提供中文别名，或设计独占缩写并同步对白。"),
    134: ("blocker", "“小数”与 `FLOAT` 共用，会同时触发 Decimal 与 Float 对白。", "本条只保留“十进制/小数点”（需结合目标词义），不要与 FLOAT 共用“小数”。"),
    135: ("blocker", "“小数”与 `DECIMAL` 共用；技术上“小数”也不等于浮点数。", "只保留“浮点/浮点数”。"),
    137: ("blocker", "“乘”与符号条件 `X` 共用，会同时触发《Multiply》和《X to multiply》。", "本条保留“乘/乘法”，让 X 只接受符号。"),
    138: ("blocker", "“乘”与 `MULTIPLY` 共用；对白又明确评价单字符 X。", "只保留独占符号“×”（若游戏输入允许），否则不加中文别名。"),
    139: ("blocker", "“加/加法”与 `PLUS` 共用，会同时触发两段对白。", "本条保留“加/相加”，PLUS 使用“加上/加号”。"),
    140: ("blocker", "“加/加法”与 `ADD` 共用，造成双触发。", "删除“加/加法”，保留独占的“加上/加号/正号”。"),
    142: ("blocker", "“和/与”与 `WITH` 共用，会同时触发并列连词和伴随关系对白。", "本条使用“且/并且”，避免“和/与”。"),
    143: ("blocker", "“和/与”与 `AND` 共用，造成双触发。", "本条使用“一起/与……一起/伴随”。"),
    145: ("error", "“末数/末尾数字”表示最后一个数字；`Endnum` 对白表示数字序列结束标记。", "改为“数字结束/数终/结束数字”。"),
    146: ("warning", "“加上一个”可理解为增加一个对象，不一定是数值 +1。", "保留“加一”，把“加上一个”改为“加上 1/递增一”。"),
    147: ("blocker", "“间隔”与 `SPACE` 共用，会同时触发跳过和空间/空格对白。", "只保留“跳过/略过”。"),
    148: ("blocker", "“间隔”与 `SKIP` 共用；对白同时覆盖外太空与排版空格，并不讨论间隔动作。", "删除“间隔”，使用“空间/空格”。"),
    151: ("blocker", "对白在解释缩写 `Ans`；输入完整“答案/回答”后缩写问答失去指代，“回答”还会同时命中词条 -12 的 `SEVEN`。", "不提供中文别名，或使用独占中文缩写并同步对白。"),
    152: ("error", "对白要求玩家实际按下 ASDF 键位；“乱打的/随便打的/键盘乱敲”是解释，不是那串输入。", "保留原版 ASDF，或设计中文键盘乱按串并重写对白。"),
    153: ("blocker", "所有中文 IDFK 变体都包含“不知道”，会同时命中全局 `IDK` contains 规则。", "把 IDK 改为 exact；IDFK 也优先使用 exact 的完整变体。"),
    154: ("blocker", "contains“不知道”会吞掉所有 IDFK 中文变体，一次输入触发 IDK 与 IDFK 两组对白。", "改为 exact“不知道”，不要使用全局 contains。"),
}


SPECIAL_PASS = {
    61: "通过（“种子星”是为拼错 neutron star 设计的本地化错字笑点）。",
    84: "通过（“种子”承接 nuetron/neutron 拼错笑点）。",
    105: "通过（“按摩”承接 message/massage 与 McLuhan 双关）。",
    144: "通过（玩家把信号误命名为“苹果”，对白正是在否定这个猜测）。",
}


def key(item: dict) -> tuple[object, str, str]:
    return item.get("term_id"), item["channel"], item["english"].casefold()


def rule_text(entry: dict) -> str:
    parts = []
    for rule in entry.get("rules", []):
        values = "/".join(str(value) for value in rule.get("values", []))
        parts.append(f"{rule.get('type', '?')}:{values}")
    return "；".join(parts) or "（无中文别名）"


def escape(value: object) -> str:
    return str(value).replace("|", "\\|").replace("\n", " ")


def candidate_source(channel: str) -> str:
    return "from" if channel in {"EditEntryFromName", "EditEntryIDFromName"} else "to"


def candidate_seeds(entry: dict) -> set[str]:
    seeds: set[str] = set()
    for rule in entry.get("rules", []):
        values = [str(value) for value in rule.get("values", [])]
        if rule.get("type") == "contains_all":
            seeds.add("".join(values))
        else:
            seeds.update(values)
    return seeds


def matches(entry: dict, candidate: str) -> bool:
    normalized = candidate.strip().casefold()
    folded = candidate.casefold()
    for rule in entry.get("rules", []):
        values = [str(value).strip().casefold() for value in rule.get("values", [])]
        if rule.get("type") == "exact" and normalized in values:
            return True
        if rule.get("type") == "contains" and any(value in folded for value in values):
            return True
        if rule.get("type") == "contains_all" and values and all(value in folded for value in values):
            return True
    return False


def find_conflicts(aliases: list[dict]) -> dict[tuple[int, int], set[str]]:
    conflicts: dict[tuple[int, int], set[str]] = {}
    for left_index, left in enumerate(aliases, start=1):
        for right_index in range(left_index + 1, len(aliases) + 1):
            right = aliases[right_index - 1]
            if candidate_source(left["channel"]) != candidate_source(right["channel"]):
                continue
            left_term, right_term = left.get("term_id"), right.get("term_id")
            if left_term is not None and right_term is not None and left_term != right_term:
                continue
            candidates = candidate_seeds(left) | candidate_seeds(right)
            hits = {value for value in candidates if matches(left, value) and matches(right, value)}
            if hits:
                conflicts[(left_index, right_index)] = hits
    return conflicts


def main() -> None:
    source = json.loads(SOURCE_PATH.read_text(encoding="utf-8"))["entries"]
    aliases = json.loads(ALIAS_PATH.read_text(encoding="utf-8"))["entries"]
    alias_by_key = {key(item): item for item in aliases}
    if len(source) != 154 or len(aliases) != 154 or len(alias_by_key) != 154:
        raise RuntimeError("审计输入不再是预期的 154 条唯一规则。")

    conflicts = find_conflicts(aliases)
    if len(conflicts) != 16:
        raise RuntimeError(f"交叉命中数量发生变化：预期 16 组，实际 {len(conflicts)} 组。")

    rows = []
    counts: Counter[str] = Counter()
    for index, item in enumerate(source, start=1):
        alias = alias_by_key[(item.get("term_id"), item["channel_name"], item["english_trigger"].casefold())]
        if index in ISSUES:
            severity, finding, recommendation = ISSUES[index]
            status = {"blocker": "阻断", "error": "错误", "warning": "风险"}[severity]
            detail = f"{finding} 建议：{recommendation}"
        else:
            severity = "pass"
            status = "通过"
            detail = SPECIAL_PASS.get(index, "中文别名与实际对白的核心语义一致。")
        counts[severity] += 1
        titles = " / ".join(
            f"{dialogue['chunk_id']}《{dialogue.get('title_source', '')}→{dialogue.get('title_translation', '')}》"
            for dialogue in item.get("dialogues", [])
        )
        rows.append(
            f"| {index} | {escape(status)} | {escape(item.get('term_id', '全局'))} | "
            f"{escape(item['channel_name'])} | `{escape(item['english_trigger'])}` | "
            f"{escape(rule_text(alias))} | {escape(titles)} | {escape(detail)} |"
        )

    issue_count = counts["blocker"] + counts["error"] + counts["warning"]
    report = [
        "# 词典触发词—对白语义全量审计",
        "",
        "审计对象：`patch/Translations/dictionary_trigger_aliases.json` 的全部 154 条规则。",
        "",
        "审计方法：逐条连接游戏原始 AdvancedListener 条件、中文附加触发词、对白标题和对白全文；同时按运行时 matcher 复算同一编辑事件的交叉命中。术语判断遵循锁定词义、上下文优先和同一概念唯一译法原则。",
        "",
        "## 结论摘要",
        "",
        f"- 阻断：{counts['blocker']} 条（稳定触发无关对白，或属于 16 组无门控双触发冲突）",
        f"- 错误：{counts['error']} 条（至少一个中文别名与对白正文/元语言笑点不一致）",
        f"- 风险：{counts['warning']} 条（范围过宽、词性偏移或容易误触发）",
        f"- 通过：{counts['pass']} 条",
        f"- 需要调整：{issue_count} / 154 条",
        "",
        "说明：同一冲突组的两端都会标为“阻断”，所以阻断条数是受影响规则数，不是冲突组数。16 组冲突均未发现其它条件门控，实际运行时会同时满足。最长且唯一匹配机制可消除长短包含冲突；同长同词冲突仍需调整别名。",
        "",
        "## 16 组规则交叉命中",
        "",
        "| 规则 A | 规则 B | 可复现中文输入 |",
        "|---|---|---|",
        *[
            f"| #{left} `{escape(aliases[left - 1]['english'])}` | #{right} `{escape(aliases[right - 1]['english'])}` | {escape(' / '.join(sorted(values)))} |"
            for (left, right), values in conflicts.items()
        ],
        "",
        "## 全量逐条结果",
        "",
        "| # | 结论 | 词条 ID | 通道 | 英文条件 | 中文规则 | 对白 | 发现与建议 |",
        "|---:|---|---:|---|---|---|---|---|",
        *rows,
        "",
        "## 判定边界",
        "",
        "- 原英文触发仍有效；本报告只判断新增中文别名是否安全，不把原版已有的大小写兼容视为问题。",
        "- 缩写、字符数、键盘位置和拼写错误属于对白语义的一部分，不能只翻译缩写展开后的概念。",
        "- `contains` 按无边界子串匹配，因此“脑”会命中“电脑”，“氦”会命中“氦秒”；这类不是理论风险，而是当前 matcher 的实际行为。",
        "- 不同词条 ID 的同译词不会在一次 ID 定向编辑中互相冲突；报告只把同一编辑候选可同时满足的规则列为双触发。",
        "",
    ]
    REPORT_PATH.write_text("\n".join(report), encoding="utf-8")
    print(json.dumps({"report": str(REPORT_PATH), "counts": counts, "issues": issue_count}, ensure_ascii=False))


if __name__ == "__main__":
    main()
