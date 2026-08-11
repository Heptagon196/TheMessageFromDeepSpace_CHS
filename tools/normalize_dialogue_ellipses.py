#!/usr/bin/env python3
"""Normalize dialogue ellipses in the AiNiee cache without creating backups."""

from __future__ import annotations

import argparse
import json
import re
from pathlib import Path
from typing import Any, Iterable


PROJECT = Path(__file__).resolve().parents[1]
ASCII_ELLIPSIS_RE = re.compile(r"\.{3,}")
CONTROL_TOKEN_RE = re.compile(
    r"\{(?:SPEAKER_[A-Z0-9_]+|PART_\d{3}|SIG_(?:N)?\d{3}|PLAYER_NAME|DYN_\d+)\}|"
    r"\$anim(?:[A-Za-z]\d{0,2}|\d{1,2})|<[^>]+>"
)


def iter_items(cache: dict[str, Any]) -> Iterable[dict[str, Any]]:
    for file_data in cache.get("files", {}).values():
        yield from file_data.get("items", [])


def is_ellipsis_only_source(text: str) -> bool:
    visible = CONTROL_TOKEN_RE.sub("", text or "").strip()
    return bool(visible) and re.fullmatch(r"\.{3,}", visible) is not None


def normalize_ascii_ellipsis(text: str) -> str:
    return ASCII_ELLIPSIS_RE.sub("……", text or "")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--cache", type=Path, default=PROJECT / "work" / "cache.json")
    parser.add_argument("--write", action="store_true")
    parser.add_argument("--expect", type=int)
    args = parser.parse_args()

    cache = json.loads(args.cache.read_text(encoding="utf-8"))
    changes: list[str] = []
    for item in iter_items(cache):
        game = item.get("extra", {}).get("game", {})
        kind = game.get("kind")
        if kind not in {"dialogue_frame", "component_dialogue_frame"}:
            continue
        if kind == "component_dialogue_frame" and game.get("field_path") == "autoLogStartFrame":
            continue

        status = int(item.get("translation_status", 0))
        if status in (1, 2):
            old = item.get("translated_text") or ""
            new = normalize_ascii_ellipsis(old)
        elif kind == "dialogue_frame" and is_ellipsis_only_source(item.get("source_text") or ""):
            old = item.get("translated_text") or ""
            new = normalize_ascii_ellipsis(item.get("source_text") or "")
            item["translation_status"] = 1
            item["model"] = "manual-punctuation-normalization"
        else:
            continue

        if new == old:
            continue
        item["translated_text"] = new
        changes.append(str(game.get("stable_key", "")))

    if args.expect is not None and len(changes) != args.expect:
        raise RuntimeError(f"变更数与预期不符: {len(changes)} != {args.expect}")
    print(json.dumps({"changed": len(changes), "write": args.write}, ensure_ascii=False))
    if not args.write:
        return 0

    temporary = args.cache.with_suffix(args.cache.suffix + ".tmp")
    temporary.write_text(
        json.dumps(cache, ensure_ascii=False, separators=(",", ":")) + "\n",
        encoding="utf-8",
    )
    temporary.replace(args.cache)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
