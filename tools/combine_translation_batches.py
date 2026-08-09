#!/usr/bin/env python3
"""Combine already validated agent outputs into one checkpoint write batch."""

from __future__ import annotations

import argparse
import json
from pathlib import Path


def load(path: Path):
    return json.loads(path.read_text(encoding="utf-8"))


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("manifest", type=Path)
    parser.add_argument("output", type=Path)
    parser.add_argument("groups", nargs="+", type=int)
    args = parser.parse_args()

    manifest = load(args.manifest)
    root = args.manifest.parent
    by_group = {entry["group"]: entry for entry in manifest["groups"]}
    combined = []
    seen: set[int] = set()
    for group_number in args.groups:
        if group_number not in by_group:
            parser.error(f"group {group_number} is not in manifest")
        entry = by_group[group_number]
        source = load(root / entry["source"])
        translated = load(root / entry["translation"])
        source_ids = [item["text_index"] for item in source]
        translated_ids = [item["text_index"] for item in translated]
        if source_ids != translated_ids:
            parser.error(f"group {group_number} index/order mismatch")
        if len(translated) != entry["count"]:
            parser.error(
                f"group {group_number} count mismatch: {len(translated)} != {entry['count']}"
            )
        overlap = seen.intersection(translated_ids)
        if overlap:
            parser.error(f"group {group_number} duplicates indices: {sorted(overlap)[:5]}")
        seen.update(translated_ids)
        combined.extend(translated)

    args.output.write_text(
        json.dumps(combined, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
    )
    print(
        json.dumps(
            {"groups": args.groups, "items": len(combined), "output": str(args.output)},
            ensure_ascii=False,
        )
    )


if __name__ == "__main__":
    main()
