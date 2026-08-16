#!/usr/bin/env python3
"""Apply several manual translation overrides, then validate/build once."""

from __future__ import annotations

import argparse
import contextlib
import io
import json
import os
from pathlib import Path

import build_runtime
import update_translation as single


PROJECT_DIR = Path(__file__).resolve().parents[1]


def main() -> int:
    parser = argparse.ArgumentParser(description="批量维护人工译文源，只执行一次全量构建。")
    parser.add_argument("batch", type=Path, help="[{text_index, translated_text}, ...]")
    parser.add_argument("--no-install", action="store_true")
    args = parser.parse_args()

    cache = single.load_json(single.DEFAULT_CACHE)
    rows = single.load_json(args.batch)
    if not isinstance(rows, list) or not rows:
        parser.error("批次必须是非空 JSON 数组")
    overrides = (single.load_json(single.DEFAULT_OVERRIDES)
                 if single.DEFAULT_OVERRIDES.is_file()
                 else {"format_version": 1, "entries": []})
    touched_categories: set[str] = set()
    expected: dict[str, str] = {}
    for row in rows:
        item = single.find_item(cache, int(row["text_index"]))
        translated = single.compose_translation(item, str(row["translated_text"]))
        candidate = dict(item)
        candidate["translation_status"] = 1
        candidate["translated_text"] = translated
        issues = build_runtime.validate_item(candidate)
        if issues:
            parser.error(f"{row['text_index']} 译文结构校验失败：{'；'.join(issues)}")
        overrides = single.upsert_override(overrides, item, translated)
        game = item.get("extra", {}).get("game", {})
        touched_categories.add(build_runtime.category_for(str(game.get("kind", ""))))
        expected[str(game.get("stable_key", ""))] = translated

    stage = PROJECT_DIR / "build" / "translation-batch-update-stage"
    staged_overrides = stage / "manual_translation_overrides.json"
    staged_runtime = stage / "runtime"
    staged_report = stage / "validation-report.json"
    single.write_json_atomic(staged_overrides, overrides)
    original_argv = os.sys.argv
    try:
        os.sys.argv = ["build_runtime.py", "--cache", str(single.DEFAULT_CACHE),
                       "--overrides", str(staged_overrides), "--output", str(staged_runtime),
                       "--report", str(staged_report), "--strict"]
        with contextlib.redirect_stdout(io.StringIO()):
            result = build_runtime.main()
    finally:
        os.sys.argv = original_argv
    if result != 0:
        parser.exit(1, f"完整运行时构建失败，详见 {staged_report}\n")

    found: dict[str, str] = {}
    for category in touched_categories:
        file_name = single.RUNTIME_FILE_NAMES[category]
        payload = single.load_json(staged_runtime / file_name)
        for entry in payload.get("entries", []):
            key = str(entry.get("stable_key", ""))
            if key in expected:
                found[key] = str(entry.get("translated_text", ""))
    if found != expected:
        parser.exit(1, "生成文件没有逐项包含预期译文，已停止发布。\n")

    single.write_json_atomic(single.DEFAULT_OVERRIDES, overrides)
    for category in touched_categories:
        file_name = single.RUNTIME_FILE_NAMES[category]
        source = staged_runtime / file_name
        single.replace_file_atomic(source, single.PROJECT_TRANSLATIONS / file_name)
        if not args.no_install:
            from project_config import resolve_game_root
            destination = resolve_game_root() / "DeepSpaceChinese" / "Translations" / file_name
            single.replace_file_atomic(source, destination)
    print(json.dumps({"updated": len(rows), "stable_keys": sorted(expected)},
                     ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
