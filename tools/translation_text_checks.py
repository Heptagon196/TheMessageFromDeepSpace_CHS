#!/usr/bin/env python3
"""Shared deterministic checks for user-visible Chinese translation text."""

from __future__ import annotations

import json
import re
from pathlib import Path


ASCII_ELLIPSIS_RE = re.compile(r"\.{3,}")
INVISIBLE_CONTROL_RE = re.compile(
    r"(?:"
    r"\{(?:SPEAKER_[A-Z0-9_]+|PART_\d{3})\}"
    r"|\$anim(?:[A-Za-z]\d{0,2}|\d{1,2})"
    r"|</?[A-Za-z][^>]*>"
    r")"
)
ACCIDENTAL_PUNCTUATION_RE = re.compile(
    r"(?:[。，、；：]{2,}|[。，、；：][！？]|[！？][。，、；：])"
)
DYNAMIC_SIGNAL_RE = re.compile(r"\{SIG_(?:N)?\d{3}\}")
CHINESE_CONTEXT_CHARS = "，。！？；：、…—～（）【】《》“”‘’"
ASCII_NATURAL_PUNCTUATION = ".,;:!?"
DISALLOWED_GARBLED_SUBTITLE = "[听不清]"
DICTIONARY_SLOGAN_TRANSLATIONS = {
    "happy dictionary": "幸福词典",
    "happy dictionaries": "幸福词典",
    "happy life": "幸福人生",
    "unhappy dictionary": "不幸词典",
    "unhappy dictionaries": "不幸词典",
    "unhappy life": "不幸人生",
}
DICTIONARY_SLOGAN_RE = re.compile(
    r"(?<![A-Za-z])(?:"
    + "|".join(re.escape(source) for source in
               sorted(DICTIONARY_SLOGAN_TRANSLATIONS, key=len, reverse=True))
    + r")(?![A-Za-z])",
    re.IGNORECASE,
)
GLOSSARY_PATH = Path(__file__).resolve().parents[1] / "work" / "glossary.locked.json"


def _load_enforced_glossary_terms() -> tuple[tuple[tuple[str, ...], str], ...]:
    glossary = json.loads(GLOSSARY_PATH.read_text(encoding="utf-8"))
    return tuple(
        (
            tuple(
                source.lower()
                for source in (item.get("src", ""), *item.get("aliases", []))
                if source
            ),
            str(item["dst"]),
        )
        for item in glossary.get("terms", [])
        if item.get("enforce") and item.get("dst")
    )


ENFORCED_GLOSSARY_TERMS = _load_enforced_glossary_terms()


def validate_chinese_quotes(text: str) -> list[str]:
    """Validate Chinese quote hierarchy without rejecting Latin apostrophes.

    Chinese primary quotations use “ ”.  The single pair ‘ ’ is reserved for
    a quotation nested inside an open primary quotation.  Straight quotes are
    rejected unless an apostrophe is embedded in a Latin word such as
    ``O'Brien``.
    """

    issues: list[str] = []
    stack: list[tuple[str, int]] = []
    text = text or ""

    for index, char in enumerate(text):
        previous = text[index - 1] if index > 0 else ""
        following = text[index + 1] if index + 1 < len(text) else ""
        latin_apostrophe = (
            char in {"'", "’"}
            and previous.isascii()
            and previous.isalnum()
            and following.isascii()
            and following.isalnum()
        )
        if latin_apostrophe:
            continue

        if char == '"' or char == "'":
            issues.append(f"位置 {index} 使用了半角引号 {char!r}")
        elif char == "“":
            if stack:
                issues.append(f"位置 {index} 的双引号嵌套层级不正确")
            stack.append(("double", index))
        elif char == "”":
            if not stack or stack[-1][0] != "double":
                issues.append(f"位置 {index} 的右双引号没有对应的左双引号")
            else:
                stack.pop()
        elif char == "‘":
            if not stack or stack[-1][0] != "double":
                issues.append(f"位置 {index} 的一级引用错误使用了单引号")
            stack.append(("single", index))
        elif char == "’":
            if not stack or stack[-1][0] != "single":
                issues.append(f"位置 {index} 的右单引号没有对应的嵌套左单引号")
            else:
                stack.pop()

    for kind, index in stack:
        label = "双引号" if kind == "double" else "单引号"
        issues.append(f"位置 {index} 的左{label}没有闭合")
    return issues


def validate_dialogue_ellipsis(text: str) -> list[str]:
    """Require standard paired Chinese ellipses in translated dialogue."""

    text = text or ""
    issues: list[str] = []
    if ASCII_ELLIPSIS_RE.search(text):
        issues.append("对白省略号必须使用中文双省略号 ……，不能使用 ASCII 三点 ...")
    if "…" in text.replace("……", ""):
        issues.append("中文省略号必须成对使用 ……，不能只写一个 …")
    return issues


def validate_duplicate_punctuation(text: str) -> list[str]:
    """Reject accidental repeated punctuation as it appears on screen.

    Speaker, dialogue-part, animation and TMP rich-text tags are invisible at
    runtime.  Removing them before validation catches strings such as
    ``没错。$animD19。`` which render as ``没错。。``.  Paired ellipses,
    em dashes and expressive question/exclamation runs remain valid.
    """

    visible_text = INVISIBLE_CONTROL_RE.sub("", text or "")
    issues: list[str] = []
    for match in ACCIDENTAL_PUNCTUATION_RE.finditer(visible_text):
        run = match.group(0)
        issues.append(
            f"位置 {match.start()} 存在异常连续标点 {run!r}"
        )
    return issues


def validate_natural_chinese_punctuation(text: str) -> list[str]:
    """Reject halfwidth sentence punctuation in Chinese natural language.

    Signal placeholders are treated as a Chinese word because their runtime
    value may be Chinese. Decimal points, filenames and intentional ASCII
    three-dot system ellipses remain valid.
    """

    visible_text = DYNAMIC_SIGNAL_RE.sub("词", text or "")
    visible_text = INVISIBLE_CONTROL_RE.sub("", visible_text)
    issues: list[str] = []

    def is_chinese_context(char: str) -> bool:
        return (
            "\u3400" <= char <= "\u4dbf"
            or "\u4e00" <= char <= "\u9fff"
            or "\uf900" <= char <= "\ufaff"
            or char in CHINESE_CONTEXT_CHARS
        )

    def is_ascii_emoticon_colon(index: int) -> bool:
        if visible_text[index] != ":":
            return False
        tail = visible_text[index : index + 4]
        return bool(re.match(r":(?:-?[()DdPp/\\]|->)", tail))

    def is_quoted_literal_punctuation(index: int) -> bool:
        if index <= 0 or index + 1 >= len(visible_text):
            return False
        return (
            visible_text[index - 1] in "“‘\"'"
            and visible_text[index + 1] in "”’\"'"
        )

    def is_numbered_list_marker(index: int) -> bool:
        return (
            visible_text[index] == "."
            and index > 0
            and visible_text[index - 1].isdigit()
            and index + 1 < len(visible_text)
            and visible_text[index + 1].isspace()
        )

    for index, char in enumerate(visible_text):
        if char not in ASCII_NATURAL_PUNCTUATION:
            continue
        if char == "." and (
            index > 0 and visible_text[index - 1] == "."
            or index + 1 < len(visible_text) and visible_text[index + 1] == "."
        ):
            continue
        if (
            is_ascii_emoticon_colon(index)
            or is_quoted_literal_punctuation(index)
            or is_numbered_list_marker(index)
        ):
            continue
        previous = index - 1
        while previous >= 0 and visible_text[previous] in " \t":
            previous -= 1
        following = index + 1
        while following < len(visible_text) and visible_text[following] in " \t":
            following += 1
        if (
            previous >= 0 and is_chinese_context(visible_text[previous])
            or following < len(visible_text)
            and is_chinese_context(visible_text[following])
        ):
            issues.append(
                f"位置 {index} 的中文自然语言使用了半角标点 {char!r}"
            )
    return issues


def validate_garbled_subtitle(text: str) -> list[str]:
    """Keep the localized transcription cue consistent across dialogue."""

    if DISALLOWED_GARBLED_SUBTITLE in (text or ""):
        return ["含混语音标注必须统一为 [含混的嘟囔]，不能使用 [听不清]"]
    return []


def validate_locked_glossary_terms(source: str, translated: str) -> list[str]:
    """Require glossary entries marked ``enforce`` in final translations."""

    visible_source = INVISIBLE_CONTROL_RE.sub("", source or "").lower()
    visible_translation = INVISIBLE_CONTROL_RE.sub("", translated or "")
    issues: list[str] = []
    for source_variants, required_target in ENFORCED_GLOSSARY_TERMS:
        matched = next(
            (
                variant
                for variant in source_variants
                if re.search(
                    rf"(?<![a-z0-9_]){re.escape(variant)}(?![a-z0-9_])",
                    visible_source,
                )
            ),
            None,
        )
        if matched and required_target not in visible_translation:
            issues.append(
                f"锁定术语 {matched!r} 必须译为包含 {required_target!r}"
            )
    return issues


def validate_dictionary_slogan(source: str, translated: str) -> list[str]:
    """Keep the recurring happy/unhappy dictionary slogan consistent.

    Dialogue control tokens can split the two halves between PARTs, and the
    game also lets different speakers finish the slogan in separate frames.
    Checking each fixed phrase independently covers both forms.
    """

    visible_source = INVISIBLE_CONTROL_RE.sub("", source or "")
    visible_translation = INVISIBLE_CONTROL_RE.sub("", translated or "")
    expected = [
        DICTIONARY_SLOGAN_TRANSLATIONS[match.group(0).lower()]
        for match in DICTIONARY_SLOGAN_RE.finditer(visible_source)
    ]
    if not expected:
        return []

    actual_sequence: list[tuple[int, str]] = []
    for canonical in set(expected):
        start = 0
        while True:
            found = visible_translation.find(canonical, start)
            if found < 0:
                break
            actual_sequence.append((found, canonical))
            start = found + len(canonical)
    actual = [value for _, value in sorted(actual_sequence)]

    issues: list[str] = []
    if actual != expected:
        issues.append(
            "词典口号的规范片段顺序或次数不一致："
            f"应为 {expected!r}，实际为 {actual!r}"
        )
    return issues
