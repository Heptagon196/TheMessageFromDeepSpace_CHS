#!/usr/bin/env python3
"""Reject translated dialogue PART boundaries that split a Chinese phrase."""

from __future__ import annotations

import argparse
import json
import re
from pathlib import Path


PART_RE = re.compile(r"\{PART_(\d{3})\}")
CONTROL_RE = re.compile(
    r"\{(?:SPEAKER_[^}]+|SIG_[^}]+|PLAYER_NAME)\}"
    r"|\$anim(?:[A-Za-z]\d{0,2}|\d{1,2})"
    r"|<[^>]+>"
)
CJK_RE = re.compile(r"[\u3400-\u9fff]")
NATURAL_END = frozenset("，。！？；：、…—”’）】》,.!?;:)]}")
NATURAL_START = frozenset("，。！？；：、…—”’）】》,.!?;:)]}")


def visible(text: str) -> str:
    return CONTROL_RE.sub("", text or "").strip()


def split_parts(text: str) -> list[tuple[int, str]]:
    matches = list(PART_RE.finditer(text or ""))
    parts: list[tuple[int, str]] = []
    for index, match in enumerate(matches):
        end = matches[index + 1].start() if index + 1 < len(matches) else len(text)
        parts.append((int(match.group(1)), visible(text[match.end():end])))
    return parts


def find_issues(entry: dict, clear_previous: list[bool]) -> list[str]:
    text = str(entry.get("translated_text", ""))
    matches = list(PART_RE.finditer(text))
    if len(matches) < 2 or not CJK_RE.search(text):
        return []
    parts = split_parts(text)
    issues: list[str] = []
    for boundary, ((left_index, left), (right_index, right)) in enumerate(
        zip(parts, parts[1:])
    ):
        if not left or not right:
            continue
        # A PART marker is often only a typing-delay boundary.  It becomes a
        # visible page boundary only when the following part has clearPrev.
        if boundary + 1 >= len(clear_previous) or not clear_previous[boundary + 1]:
            continue
        if left[-1] in NATURAL_END or right[0] in NATURAL_START:
            continue
        issues.append(
            f"PART_{left_index:03d}/PART_{right_index:03d} 在非标点处断开："
            f"{left[-24:]!r} | {right[:24]!r}"
        )
    return issues


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "path", nargs="?", type=Path,
        default=Path(__file__).resolve().parents[1] / "patch" / "Translations" / "dialogue.json",
    )
    parser.add_argument(
        "--metadata", type=Path,
        default=Path(__file__).resolve().parents[1] / "work" / "dialogue_part_metadata.json",
    )
    args = parser.parse_args()
    payload = json.loads(args.path.read_text(encoding="utf-8"))
    metadata = json.loads(args.metadata.read_text(encoding="utf-8")).get("entries", {})
    failures: list[str] = []
    for entry in payload.get("entries", []):
        stable_key = str(entry.get("stable_key", ""))
        clear_previous = metadata.get(stable_key)
        if clear_previous is None:
            failures.append(f"{stable_key}: 缺少 clearPrev 元数据")
            continue
        for issue in find_issues(entry, clear_previous):
            failures.append(f"{entry.get('stable_key')}: {issue}")
    if failures:
        print("对白 PART 边界校验失败：")
        print("\n".join(failures))
        return 1
    print(f"Dialogue PART boundary audit passed: {len(payload.get('entries', []))} entries.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
