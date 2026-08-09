#!/usr/bin/env python3
"""Validate an agent translation JSON against the authoritative game cache."""

from __future__ import annotations

import argparse
import json
import re
from copy import deepcopy
from pathlib import Path

import build_runtime


PART_RE = re.compile(r"\{PART_(\d{3})\}")
CONTROL_RE = re.compile(
    r"\{(?:SPEAKER_[A-Z0-9_]+|PART_\d{3}|SIG_(?:N)?\d{3}|PLAYER_NAME|DYN_\d+)\}|"
    r"\$anim(?:[A-Za-z]\d{0,2}|\d{1,2})|<[^>]+>"
)


def load(path: Path):
    return json.loads(path.read_text(encoding="utf-8"))


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("cache", type=Path)
    parser.add_argument("translations", type=Path)
    parser.add_argument(
        "--source",
        type=Path,
        help="Optional source batch; require identical index order and complete coverage.",
    )
    args = parser.parse_args()

    cache = load(args.cache)
    translations = load(args.translations)
    if not isinstance(translations, list):
        parser.error("translation JSON must be an array")

    cache_by_index = {item["text_index"]: item for item in build_runtime.iter_items(cache)}
    ids = [item.get("text_index") for item in translations]
    errors: list[str] = []
    if len(ids) != len(set(ids)):
        errors.append("translation batch contains duplicate text_index values")

    if args.source:
        source = load(args.source)
        source_ids = [item.get("text_index") for item in source]
        if ids != source_ids:
            errors.append("translation indices/order do not exactly match source batch")

    for position, translated in enumerate(translations):
        text_index = translated.get("text_index")
        original = cache_by_index.get(text_index)
        if original is None:
            errors.append(f"[{position}] unknown text_index {text_index!r}")
            continue
        if "source_text" in translated and translated["source_text"] != original["source_text"]:
            errors.append(f"[{position}] source_text mismatch for {text_index}")
        candidate = deepcopy(original)
        candidate["translation_status"] = 1
        candidate["translated_text"] = translated.get("translated_text", "")
        for issue in build_runtime.validate_item(candidate):
            errors.append(f"[{position}] {text_index}: {issue}")
        kind = original.get("extra", {}).get("game", {}).get("kind")
        if kind in {"dialogue_frame", "component_dialogue_frame"}:
            source_parts = split_parts(original["source_text"])
            translated_parts = split_parts(candidate["translated_text"])
            if len(source_parts) == len(translated_parts):
                for part_index, (source_part, translated_part) in enumerate(
                    zip(source_parts, translated_parts, strict=True)
                ):
                    if visible_text(source_part) and not visible_text(translated_part):
                        errors.append(
                            f"[{position}] {text_index}: PART_{part_index:03d} "
                            "源段非空但译段为空"
                        )

    if errors:
        print(json.dumps({"valid": False, "errors": errors}, ensure_ascii=False, indent=2))
        return 1
    print(json.dumps({"valid": True, "items": len(translations)}, ensure_ascii=False))
    return 0


def split_parts(text: str) -> list[str]:
    matches = list(PART_RE.finditer(text or ""))
    return [
        text[match.end() : matches[index + 1].start() if index + 1 < len(matches) else len(text)]
        for index, match in enumerate(matches)
    ]


def visible_text(text: str) -> str:
    return CONTROL_RE.sub("", text or "").strip()


if __name__ == "__main__":
    raise SystemExit(main())
