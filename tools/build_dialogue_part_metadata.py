#!/usr/bin/env python3
"""Extract compact clearPrev metadata used by the PART-boundary audit."""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path


TOOLS_DIR = Path(__file__).resolve().parent
sys.path.insert(0, str(TOOLS_DIR))
from extract import extract_dialogue, make_generator  # noqa: E402
from project_config import PROJECT_DIR  # noqa: E402


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--output",
        type=Path,
        default=PROJECT_DIR / "work" / "dialogue_part_metadata.json",
    )
    args = parser.parse_args()

    groups, _ = extract_dialogue(make_generator(), {})
    entries: dict[str, list[bool]] = {}
    for items in groups.values():
        for item in items:
            game = item.get("extra", {}).get("game", {})
            if game.get("kind") != "dialogue_frame":
                continue
            entries[str(game["stable_key"])] = [
                bool(part.get("clear_previous", False))
                for part in game.get("parts", [])
            ]

    payload = {
        "format_version": 1,
        "game_version": "0.10",
        "entries": dict(sorted(entries.items())),
    }
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(
        json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
    )
    print(f"Wrote {args.output}: {len(entries)} dialogue frames")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
