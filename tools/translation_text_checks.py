#!/usr/bin/env python3
"""Shared deterministic checks for user-visible Chinese translation text."""

from __future__ import annotations


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
