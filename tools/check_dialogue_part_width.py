from __future__ import annotations

import argparse
import json
import re
import unicodedata
from pathlib import Path


PART_RE = re.compile(r"\{PART_(\d{3})\}")
SPEAKER_RE = re.compile(r"\{SPEAKER_[A-Z0-9_]+\}")
ANIMATION_RE = re.compile(r"\$anim(?:[A-Za-z]\d{0,2}|\d{1,2})")
TAG_RE = re.compile(r"<[^>]+>")


def visible_text(value: str) -> str:
    return TAG_RE.sub("", ANIMATION_RE.sub("", value))


def width_units(value: str) -> float:
    total = 0.0
    for char in visible_text(value):
        if char in "\r\n":
            continue
        total += 1.0 if unicodedata.east_asian_width(char) in {"W", "F"} else 0.5
    return total


def main() -> int:
    parser = argparse.ArgumentParser(description="检查指定对白每个 PART 的近似单行显示策略")
    parser.add_argument("dialogue_json", type=Path)
    parser.add_argument("stable_key")
    parser.add_argument("--max-units", type=float, required=True)
    parser.add_argument("--shrink-threshold", type=float, default=1.5)
    args = parser.parse_args()

    data = json.loads(args.dialogue_json.read_text(encoding="utf-8"))
    entry = next((item for item in data["entries"] if item["stable_key"] == args.stable_key), None)
    if entry is None:
        raise SystemExit(f"找不到条目：{args.stable_key}")
    body = SPEAKER_RE.sub("", entry["translated_text"], count=1)
    matches = list(PART_RE.finditer(body))
    failures: list[tuple[int, float, str]] = []
    shrinks: list[tuple[int, float, str]] = []
    for position, match in enumerate(matches):
        start = match.end()
        end = matches[position + 1].start() if position + 1 < len(matches) else len(body)
        text = body[start:end]
        width = width_units(text)
        if width > args.max_units * args.shrink_threshold:
            failures.append((int(match.group(1)), width, visible_text(text)))
        elif width > args.max_units:
            shrinks.append((int(match.group(1)), width, visible_text(text)))
    if failures:
        for part, width, text in failures:
            print(
                f"PAGINATE {args.stable_key} PART_{part:03d}: {width:g} > "
                f"{args.max_units * args.shrink_threshold:g}: {text}"
            )
        return 1
    for part, width, text in shrinks:
        scale = args.max_units / width * 100
        print(f"SHRINK {args.stable_key} PART_{part:03d}: {width:g}, font {scale:.1f}%: {text}")
    if not shrinks:
        print(f"UNCHANGED {args.stable_key}: all PARTs <= {args.max_units:g}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
