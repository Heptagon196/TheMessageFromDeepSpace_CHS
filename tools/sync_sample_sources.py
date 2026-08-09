#!/usr/bin/env python3
"""Add authoritative source_text fields to a translation review JSON.

The resulting objects remain compatible with ainiee_translate.batch write,
which only consumes text_index and translated_text.
"""

from __future__ import annotations

import argparse
import json
from pathlib import Path


def load_json(path: Path):
    return json.loads(path.read_text(encoding="utf-8"))


def source_index(cache: dict) -> dict[int, str]:
    result: dict[int, str] = {}
    for cache_file in cache["files"].values():
        for item in cache_file["items"]:
            result[item["text_index"]] = item["source_text"]
    return result


def main() -> None:
    parser = argparse.ArgumentParser(
        description="Insert source_text into a translation review JSON."
    )
    parser.add_argument("cache", type=Path)
    parser.add_argument("translations", type=Path)
    args = parser.parse_args()

    translations = load_json(args.translations)
    sources = source_index(load_json(args.cache))

    output = []
    missing = []
    for item in translations:
        text_index = item["text_index"]
        if text_index not in sources:
            missing.append(text_index)
            continue
        output.append(
            {
                "text_index": text_index,
                "source_text": sources[text_index],
                "translated_text": item["translated_text"],
            }
        )

    if missing:
        parser.error(f"missing text_index values in cache: {missing}")

    args.translations.write_text(
        json.dumps(output, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )
    print(f"updated {len(output)} item(s): {args.translations}")


if __name__ == "__main__":
    main()
