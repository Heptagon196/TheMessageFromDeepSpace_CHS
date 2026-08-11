#!/usr/bin/env python3
"""Synchronize code-authored UI template definitions into the translation cache."""

from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
from typing import Any, Iterable

from extraction_rules import UI_TEMPLATES


PROJECT_DIR = Path(__file__).resolve().parents[1]


def iter_items(cache: dict[str, Any]) -> Iterable[dict[str, Any]]:
    for file_data in cache.get("files", {}).values():
        yield from file_data.get("items", [])


def deterministic_index(stable_key: str, used: dict[int, str]) -> int:
    index = int(hashlib.sha256(stable_key.encode("utf-8")).hexdigest()[:8], 16)
    index &= 0x7FFFFFFF
    if index == 0:
        index = 1
    while index in used and used[index] != stable_key:
        index = 1 if index == 0x7FFFFFFF else index + 1
    used[index] = stable_key
    return index


def template_game(template: dict[str, Any], source: str) -> dict[str, Any]:
    template_id = str(template["template_id"])
    game: dict[str, Any] = {
        "kind": "ui_template",
        "stable_key": f"ui-template:{template_id}",
        "source_sha256": hashlib.sha256(source.encode("utf-8")).hexdigest(),
        "template_id": template_id,
        "original_text": source,
        "protect_player_name": False,
    }
    if template.get("translate_display_values", False):
        game["translate_display_values"] = True
    return game


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--cache", type=Path, default=PROJECT_DIR / "work" / "cache.json")
    args = parser.parse_args()

    cache = json.loads(args.cache.read_text(encoding="utf-8"))
    templates_file = cache.get("files", {}).get("ui.templates")
    if not isinstance(templates_file, dict) or not isinstance(templates_file.get("items"), list):
        raise SystemExit("cache is missing files.ui.templates.items")

    all_items = list(iter_items(cache))
    used = {
        int(item["text_index"]): str(item.get("extra", {}).get("game", {}).get("stable_key", ""))
        for item in all_items
    }
    by_key = {
        str(item.get("extra", {}).get("game", {}).get("stable_key", "")): item
        for item in templates_file["items"]
    }
    added = 0
    updated = 0
    for template in UI_TEMPLATES:
        source = str(template["source_text"])
        game = template_game(template, source)
        stable_key = game["stable_key"]
        existing = by_key.get(stable_key)
        if existing is None:
            existing = {
                "text_index": deterministic_index(stable_key, used),
                "translation_status": 0,
                "model": "",
                "source_text": source,
                "translated_text": "",
                "text_to_detect": source,
                "lang_code": None,
                "extra": {"game": game},
            }
            templates_file["items"].append(existing)
            by_key[stable_key] = existing
            added += 1
            continue

        source_changed = existing.get("source_text") != source
        existing["source_text"] = source
        existing["text_to_detect"] = source
        existing["extra"]["game"] = game
        if source_changed:
            existing["translation_status"] = 0
            existing["translated_text"] = ""
            existing["model"] = ""
        updated += 1

    templates_file["items"].sort(
        key=lambda item: str(item.get("extra", {}).get("game", {}).get("stable_key", ""))
    )
    temporary = args.cache.with_suffix(args.cache.suffix + ".tmp")
    temporary.write_text(
        json.dumps(cache, ensure_ascii=False, separators=(",", ":")), encoding="utf-8"
    )
    temporary.replace(args.cache)
    print(json.dumps({"added": added, "updated": updated}, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
