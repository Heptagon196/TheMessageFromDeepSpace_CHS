#!/usr/bin/env python3
"""Normalize primary Chinese quotation marks in cache and formal batches."""

from __future__ import annotations

import argparse
import json
from pathlib import Path
from typing import Any, Iterable


PROJECT = Path(__file__).resolve().parents[1]


def normalize_primary_quotes(text: str) -> str:
    """Promote top-level ‘ ’ to “ ” while preserving valid nested quotes."""

    depth = 0
    output: list[str] = []
    for char in text or "":
        if char == "“":
            depth += 1
            output.append(char)
        elif char == "”":
            depth = max(0, depth - 1)
            output.append(char)
        elif char == "‘" and depth == 0:
            output.append("“")
        elif char == "’" and depth == 0:
            output.append("”")
        else:
            output.append(char)
    return "".join(output)


def iter_cache_items(cache: dict[str, Any]) -> Iterable[dict[str, Any]]:
    for file_data in cache.get("files", {}).values():
        yield from file_data.get("items", [])


def write_json(path: Path, value: Any, *, compact: bool = False) -> None:
    if compact:
        rendered = json.dumps(value, ensure_ascii=False, separators=(",", ":"))
    else:
        rendered = json.dumps(value, ensure_ascii=False, indent=2)
    path.write_text(rendered + "\n", encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--cache", type=Path, default=PROJECT / "work" / "cache.json"
    )
    parser.add_argument(
        "--manifest",
        type=Path,
        default=PROJECT / "work" / "formal_batches" / "manifest.json",
    )
    parser.add_argument("--write", action="store_true")
    parser.add_argument("--expect", type=int)
    args = parser.parse_args()

    manifest = json.loads(args.manifest.read_text(encoding="utf-8"))
    batch_dir = args.manifest.parent
    batch_plans: list[tuple[Path, list[dict[str, Any]], int]] = []
    builder_plans: list[tuple[Path, str, int]] = []
    formal_changes = 0

    for group in manifest["groups"]:
        translation_path = batch_dir / group["translation"]
        translations = json.loads(translation_path.read_text(encoding="utf-8"))
        replacements: list[tuple[str, str]] = []
        changed = 0
        for item in translations:
            old = item.get("translated_text") or ""
            new = normalize_primary_quotes(old)
            if new == old:
                continue
            item["translated_text"] = new
            replacements.append((old, new))
            changed += 1
        if not changed:
            continue

        builder_path = batch_dir / f"build_trans_{int(group['group']):02}.py"
        builder = builder_path.read_text(encoding="utf-8")
        builder_replacements = 0
        for old, new in replacements:
            old_literal = json.dumps(old, ensure_ascii=False)
            new_literal = json.dumps(new, ensure_ascii=False)
            count = builder.count(old_literal)
            if count == 0:
                raise RuntimeError(
                    f"{builder_path.name} 中找不到待替换的完整译文: {old!r}"
                )
            builder = builder.replace(old_literal, new_literal)
            builder_replacements += count

        batch_plans.append((translation_path, translations, changed))
        builder_plans.append((builder_path, builder, builder_replacements))
        formal_changes += changed

    cache = json.loads(args.cache.read_text(encoding="utf-8"))
    cache_changes = 0
    for item in iter_cache_items(cache):
        old = item.get("translated_text") or ""
        new = normalize_primary_quotes(old)
        if new == old:
            continue
        item["translated_text"] = new
        cache_changes += 1

    if args.expect is not None and (
        formal_changes != args.expect or cache_changes != args.expect
    ):
        raise RuntimeError(
            f"变更数与预期不符: formal={formal_changes}, "
            f"cache={cache_changes}, expected={args.expect}"
        )

    print(
        json.dumps(
            {
                "formal_entries": formal_changes,
                "cache_entries": cache_changes,
                "batch_files": len(batch_plans),
                "builder_occurrences": sum(plan[2] for plan in builder_plans),
                "write": args.write,
            },
            ensure_ascii=False,
        )
    )
    if not args.write:
        return 0

    for path, translations, _ in batch_plans:
        write_json(path, translations)
    for path, builder, _ in builder_plans:
        path.write_text(builder, encoding="utf-8")
    write_json(args.cache, cache, compact=True)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
