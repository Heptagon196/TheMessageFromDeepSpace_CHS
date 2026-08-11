#!/usr/bin/env python3
"""Shared deterministic checks for user-visible Chinese translation text."""

from __future__ import annotations

import re


ASCII_ELLIPSIS_RE = re.compile(r"\.{3,}")
INVISIBLE_CONTROL_RE = re.compile(
    r"(?:"
    r"\{(?:SPEAKER_[A-Z0-9_]+|PART_\d{3})\}"
    r"|\$anim(?:[A-Za-z]\d{0,2}|\d{1,2})"
    r"|</?[A-Za-z][^>]*>"
    r")"
)
ACCIDENTAL_PUNCTUATION_REPEAT_RE = re.compile(r"([。，、；：])\1+")


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
    for match in ACCIDENTAL_PUNCTUATION_REPEAT_RE.finditer(visible_text):
        issues.append(
            f"位置 {match.start()} 存在重复中文标点 {match.group(0)!r}"
        )
    return issues
