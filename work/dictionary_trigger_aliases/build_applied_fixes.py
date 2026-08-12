from __future__ import annotations

import importlib.util
import json
import subprocess
from pathlib import Path


HERE = Path(__file__).resolve().parent
PROJECT = HERE.parents[1]
ALIAS_PATH = PROJECT / "patch" / "Translations" / "dictionary_trigger_aliases.json"
SOURCE_PATH = HERE / "source.json"
AUDIT_SCRIPT = HERE / "build_semantic_audit.py"
OUTPUT_PATH = HERE / "applied_fixes.md"


def load_audit_module():
    spec = importlib.util.spec_from_file_location("dictionary_alias_audit", AUDIT_SCRIPT)
    module = importlib.util.module_from_spec(spec)
    assert spec.loader is not None
    spec.loader.exec_module(module)
    return module


def rule_text(entry: dict) -> str:
    if not entry.get("rules"):
        return "（无中文别名）"
    return "；".join(
        f"{rule['type']}:" + "/".join(str(value) for value in rule.get("values", []))
        for rule in entry["rules"]
    )


def escape(value: object) -> str:
    return str(value).replace("|", "\\|").replace("\n", " ")


def match_length(entry: dict, candidate: str) -> int:
    longest = 0
    normalized = candidate.strip().casefold()
    folded = candidate.casefold()
    for rule in entry.get("rules", []):
        values = [str(value).strip().casefold() for value in rule.get("values", [])]
        if rule.get("type") == "contains_all" and values and all(
            value in folded for value in values
        ):
            longest = max(longest, sum(len(value) for value in values))
        elif rule.get("type") == "contains":
            longest = max(longest, *(len(value) for value in values if value in folded), 0)
        elif rule.get("type") == "exact" and normalized in values:
            longest = max(longest, len(normalized))
    return longest


def main() -> None:
    current_root = json.loads(ALIAS_PATH.read_text(encoding="utf-8"))
    old_root = json.loads(
        subprocess.check_output(
            ["git", "show", "HEAD:patch/Translations/dictionary_trigger_aliases.json"],
            cwd=PROJECT,
            text=True,
            encoding="utf-8",
        )
    )
    source = json.loads(SOURCE_PATH.read_text(encoding="utf-8"))["entries"]
    audit = load_audit_module()
    conflicts = audit.find_conflicts(current_root["entries"])
    ambiguous = {
        pair: values
        for pair, values in conflicts.items()
        if any(
            match_length(current_root["entries"][pair[0] - 1], value) ==
            match_length(current_root["entries"][pair[1] - 1], value)
            for value in values
        )
    }

    extra_reasons = {
        5: ("删除生硬直译“陆地佬”，与现有对白统一为自然中文“旱鸭子”。", "只保留“旱鸭子”。"),
    }
    rows: list[str] = []
    changed = 0
    for index, (old, new, context) in enumerate(
        zip(old_root["entries"], current_root["entries"], source), start=1
    ):
        if old.get("rules") == new.get("rules"):
            continue
        changed += 1
        if index in audit.ISSUES:
            _, finding, recommendation = audit.ISSUES[index]
        else:
            finding, recommendation = extra_reasons[index]
        rows.append(
            f"| {index} | {escape(context.get('term_id', '全局'))} | "
            f"`{escape(context['english_trigger'])}` | {escape(rule_text(old))} | "
            f"{escape(rule_text(new))} | {escape(finding)} {escape(recommendation)} |"
        )

    report = [
        "# 词典对白中文触发修正对照表",
        "",
        f"- 实际修改规则：{changed} / {len(current_root['entries'])} 条",
        f"- 修改后规则层原始包含重叠：{len(conflicts)} 组",
        f"- 唯一最长匹配后仍有歧义：{len(ambiguous)} 组",
        "- 运行时选择策略：同一次编辑取唯一最长中文命中；并列最长拒绝中文附加触发。",
        "- 无中文别名的条目仍保留原版英文、符号及大小写不敏感兼容触发。",
        "",
        "| # | 词条 ID | 英文条件 | 修正前 | 修正后 | 原因 |",
        "|---:|---:|---|---|---|---|",
        *rows,
        "",
    ]
    OUTPUT_PATH.write_text("\n".join(report), encoding="utf-8")
    print(json.dumps({"report": str(OUTPUT_PATH), "changed": changed,
                      "raw_overlaps": len(conflicts),
                      "ambiguous_after_longest": len(ambiguous)}, ensure_ascii=False))


if __name__ == "__main__":
    main()
