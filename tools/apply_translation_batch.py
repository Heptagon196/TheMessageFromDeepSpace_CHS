#!/usr/bin/env python3
"""Apply validated translations to the authoritative cache by text_index."""

from __future__ import annotations

import argparse
import json
from pathlib import Path

import build_runtime


def load(path: Path):
    return json.loads(path.read_text(encoding="utf-8"))


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("cache", type=Path)
    parser.add_argument("translations", type=Path)
    parser.add_argument("--indices", nargs="+", type=int)
    args = parser.parse_args()

    cache = load(args.cache)
    translations = load(args.translations)
    selected = set(args.indices) if args.indices else {
        item["text_index"] for item in translations
    }
    by_index = {
        item["text_index"]: item["translated_text"]
        for item in translations
        if item["text_index"] in selected
    }
    if set(by_index) != selected:
        missing = sorted(selected - set(by_index))
        raise SystemExit(f"translations missing selected indices: {missing}")

    seen: set[int] = set()
    for item in build_runtime.iter_items(cache):
        text_index = item.get("text_index")
        if text_index not in by_index:
            continue
        if text_index in seen:
            raise SystemExit(f"duplicate cache text_index: {text_index}")
        item["translated_text"] = by_index[text_index]
        item["translation_status"] = 1
        seen.add(text_index)
    if seen != selected:
        missing = sorted(selected - seen)
        raise SystemExit(f"cache missing selected indices: {missing}")

    temporary = args.cache.with_suffix(args.cache.suffix + ".tmp")
    temporary.write_text(
        json.dumps(cache, ensure_ascii=False, separators=(",", ":")),
        encoding="utf-8",
    )
    temporary.replace(args.cache)
    print(json.dumps({"updated": len(seen), "cache": str(args.cache)}, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
