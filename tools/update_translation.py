#!/usr/bin/env python3
"""Update one translation source, validate it, build runtime JSON, and install it."""

from __future__ import annotations

import argparse
import contextlib
import io
import json
import os
import shutil
from pathlib import Path
from typing import Any

import build_runtime


PROJECT_DIR = Path(__file__).resolve().parents[1]
DEFAULT_CACHE = PROJECT_DIR / "work" / "cache.json"
DEFAULT_OVERRIDES = build_runtime.DEFAULT_MANUAL_OVERRIDES
PROJECT_TRANSLATIONS = PROJECT_DIR / "patch" / "Translations"
RUNTIME_FILE_NAMES = {
    "dialogue": "dialogue.json",
    "titles": "titles.json",
    "ui": "ui.json",
    "system": "system.json",
}


def load_json(path: Path) -> Any:
    return json.loads(path.read_text(encoding="utf-8"))


def find_item(cache: dict[str, Any], text_index: int) -> dict[str, Any]:
    matches = [item for item in build_runtime.iter_items(cache) if item.get("text_index") == text_index]
    if len(matches) != 1:
        raise ValueError(f"text_index {text_index} 应唯一存在，实际找到 {len(matches)} 条。")
    return matches[0]


def compose_translation(item: dict[str, Any], visible_or_raw: str) -> str:
    game = item.get("extra", {}).get("game", {})
    kind = str(game.get("kind", ""))
    if kind not in {"dialogue_frame", "component_dialogue_frame"}:
        return visible_or_raw
    if build_runtime.TOKEN_PATTERNS["speaker"].search(visible_or_raw):
        return visible_or_raw
    source = str(item.get("source_text", ""))
    speakers = build_runtime.TOKEN_PATTERNS["speaker"].findall(source)
    parts = build_runtime.TOKEN_PATTERNS["part"].findall(source)
    if len(speakers) != 1 or len(parts) != 1:
        raise ValueError(
            "多段对白必须传入包含原 SPEAKER/PART 标记的完整译文；"
            "单段对白可只传可见中文。"
        )
    return f"{speakers[0]}{parts[0]}{visible_or_raw}"


def upsert_override(
    payload: dict[str, Any], item: dict[str, Any], translated_text: str,
    *, allow_added_player_name: bool = False,
) -> dict[str, Any]:
    entries = payload.setdefault("entries", [])
    if payload.get("format_version") != 1 or not isinstance(entries, list):
        raise ValueError("人工译文修订文件格式无效。")
    text_index = int(item["text_index"])
    game = item.get("extra", {}).get("game", {})
    replacement = {
        "text_index": text_index,
        "source_sha256": game.get("source_sha256", ""),
        "translated_text": translated_text,
    }
    if allow_added_player_name:
        replacement["allow_added_player_name"] = True
    found = False
    output: list[dict[str, Any]] = []
    for entry in entries:
        if entry.get("text_index") == text_index:
            if found:
                raise ValueError(f"人工译文修订中重复出现 text_index {text_index}。")
            output.append(replacement)
            found = True
        else:
            output.append(entry)
    if not found:
        output.append(replacement)
    output.sort(key=lambda entry: int(entry["text_index"]))
    return {"format_version": 1, "entries": output}


def write_json_atomic(path: Path, payload: Any) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_name(path.name + ".tmp")
    temporary.write_text(
        json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
    )
    os.replace(temporary, path)


def replace_file_atomic(source: Path, destination: Path) -> None:
    destination.parent.mkdir(parents=True, exist_ok=True)
    temporary = destination.with_name(destination.name + ".tmp")
    shutil.copy2(source, temporary)
    # Parse the exact staged bytes before exposing them to the game.
    json.loads(temporary.read_text(encoding="utf-8"))
    os.replace(temporary, destination)


def run(args: argparse.Namespace) -> dict[str, Any]:
    cache = load_json(args.cache)
    item = find_item(cache, args.text_index)
    translated_text = compose_translation(item, args.text)
    override_payload = (
        load_json(args.overrides)
        if args.overrides.is_file()
        else {"format_version": 1, "entries": []}
    )
    existing_override = next(
        (
            entry
            for entry in override_payload.get("entries", [])
            if entry.get("text_index") == args.text_index
        ),
        {},
    )
    allow_added_player_name = bool(
        args.allow_added_player_name
        or existing_override.get("allow_added_player_name") is True
    )
    candidate = dict(item)
    candidate["translation_status"] = 1
    candidate["translated_text"] = translated_text
    if allow_added_player_name:
        candidate["_manual_allow_added_player_name"] = True
    issues = build_runtime.validate_item(candidate)
    if issues:
        raise ValueError("译文结构校验失败：" + "；".join(issues))

    updated_overrides = upsert_override(
        override_payload,
        item,
        translated_text,
        allow_added_player_name=allow_added_player_name,
    )

    # Keep staging under the ignored build directory. Python's TemporaryDirectory
    # can inherit unusable ACLs under Program Files on Windows; this stable path
    # is deliberately reused and every published file is still atomically replaced.
    temporary = PROJECT_DIR / "build" / "translation-update-stage"
    staged_overrides = temporary / "manual_translation_overrides.json"
    staged_output = temporary / "runtime"
    staged_report = temporary / "validation-report.json"
    temporary.mkdir(parents=True, exist_ok=True)
    write_json_atomic(staged_overrides, updated_overrides)

    # Validate every runtime entry, not just the edited one. Nothing is
    # installed unless this complete build succeeds.
    original_argv = os.sys.argv
    try:
        os.sys.argv = [
            "build_runtime.py",
            "--cache",
            str(args.cache),
            "--overrides",
            str(staged_overrides),
            "--output",
            str(staged_output),
            "--report",
            str(staged_report),
            "--strict",
        ]
        with contextlib.redirect_stdout(io.StringIO()):
            result = build_runtime.main()
    finally:
        os.sys.argv = original_argv
    if result != 0:
        raise RuntimeError(f"完整运行时构建失败，详见 {staged_report}")

    category = build_runtime.category_for(str(item.get("extra", {}).get("game", {}).get("kind", "")))
    file_name = RUNTIME_FILE_NAMES[category]
    staged_runtime = staged_output / file_name
    runtime_payload = load_json(staged_runtime)
    stable_key = item.get("extra", {}).get("game", {}).get("stable_key")
    runtime_matches = [
        entry for entry in runtime_payload.get("entries", []) if entry.get("stable_key") == stable_key
    ]
    if len(runtime_matches) != 1 or runtime_matches[0].get("translated_text") != translated_text:
        raise RuntimeError("生成文件未包含预期译文，已停止安装。")

    # The maintained override is the source of truth. Publish generated JSON
    # only after the entire staged build and target-entry check pass.
    write_json_atomic(args.overrides, updated_overrides)
    replace_file_atomic(staged_runtime, PROJECT_TRANSLATIONS / file_name)
    installed_to: Path | None = None
    if not args.no_install:
        from project_config import resolve_game_root

        game_root = args.game_root.resolve() if args.game_root else resolve_game_root()
        installed_to = game_root / "DeepSpaceChinese" / "Translations" / file_name
        replace_file_atomic(staged_runtime, installed_to)

    return {
        "text_index": args.text_index,
        "stable_key": stable_key,
        "translated_text": translated_text,
        "runtime_file": file_name,
        "installed_to": str(installed_to) if installed_to else None,
    }


def main() -> int:
    parser = argparse.ArgumentParser(
        description="一条命令修改译文源、全量校验、生成运行时 JSON 并安装到游戏。"
    )
    parser.add_argument("text_index", type=int, help="提取缓存中的 text_index")
    parser.add_argument("text", help="新译文；单段对白可省略 SPEAKER/PART 标记")
    parser.add_argument("--cache", type=Path, default=DEFAULT_CACHE)
    parser.add_argument("--overrides", type=Path, default=DEFAULT_OVERRIDES)
    parser.add_argument("--game-root", type=Path, help="覆盖项目配置中的游戏目录")
    parser.add_argument(
        "--allow-added-player-name",
        action="store_true",
        help="允许译文相较原文新增一个 {PLAYER_NAME} 运行时占位符",
    )
    parser.add_argument("--no-install", action="store_true", help="只更新项目，不覆盖游戏")
    args = parser.parse_args()
    try:
        result = run(args)
    except (OSError, ValueError, RuntimeError, json.JSONDecodeError) as exc:
        parser.exit(1, f"错误：{exc}\n")
    print(json.dumps(result, ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
