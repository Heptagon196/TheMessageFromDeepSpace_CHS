from __future__ import annotations

import argparse
import json
import os
import sys
from collections import defaultdict
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


PROJECT_DIR = Path(__file__).resolve().parents[1]
GAME_ROOT = PROJECT_DIR.parent
DATA_DIR = GAME_ROOT / "The Message From Deep Space_Data"
CHANNEL_NAMES = {
    14: "EditEntryFromName",
    15: "EditEntryToName",
    16: "EditEntryIDToName",
    17: "EditEntryIDFromName",
    19: "DictEntryIs",
    20: "EditEntryIDContains",
}


def load_unitypy():
    dependency_dir = os.environ.get("TMFDS_PYTHON_PACKAGES")
    if not dependency_dir:
        bundled = PROJECT_DIR / "build" / "puzzle-inspector-python"
        dependency_dir = str(
            bundled if bundled.exists() else PROJECT_DIR / "tools" / "python-packages"
        )
    sys.path.insert(0, dependency_dir)
    import UnityPy
    from UnityPy.helpers.TypeTreeGenerator import TypeTreeGenerator

    return UnityPy, TypeTreeGenerator


def cache_items(cache: dict[str, Any]):
    for cache_file in cache.get("files", {}).values():
        yield from cache_file.get("items", [])


def dialogue_catalog(cache: dict[str, Any]) -> tuple[dict[int, int], dict[int, dict[str, Any]]]:
    path_to_chunk: dict[int, int] = {}
    chunks: dict[int, dict[str, Any]] = {}
    for item in cache_items(cache):
        game = item.get("extra", {}).get("game", {})
        if game.get("kind") not in {"dialogue_title", "dialogue_frame"}:
            continue
        chunk_id = game.get("chunk_id")
        path_id = game.get("asset_path_id")
        if not isinstance(chunk_id, int) or not isinstance(path_id, int):
            continue
        path_to_chunk[path_id] = chunk_id
        chunk = chunks.setdefault(
            chunk_id,
            {
                "chunk_id": chunk_id,
                "chunk_name": game.get("chunk_name", ""),
                "title_source": "",
                "title_translation": "",
                "frames": [],
            },
        )
        if game.get("kind") == "dialogue_title":
            chunk["title_source"] = item.get("source_text", "")
            chunk["title_translation"] = item.get("translated_text", "")
        else:
            chunk["frames"].append(
                {
                    "frame_index": game.get("frame_index", -1),
                    "source": item.get("source_text", ""),
                    "translation": item.get("translated_text", ""),
                }
            )
    for chunk in chunks.values():
        chunk["frames"].sort(key=lambda value: value["frame_index"])
    return path_to_chunk, chunks


def hypothesis_translations(cache: dict[str, Any]) -> dict[str, str]:
    result: dict[str, str] = {}
    for item in cache_items(cache):
        game = item.get("extra", {}).get("game", {})
        field_path = game.get("field_path", "")
        if game.get("kind") != "component_string" or not field_path.startswith("hypos["):
            continue
        result[field_path] = item.get("translated_text", "")
    return result


def compact_context(chunks: list[dict[str, Any]]) -> str:
    lines: list[str] = []
    for chunk in chunks:
        title = chunk.get("title_source") or chunk.get("chunk_name") or str(chunk["chunk_id"])
        lines.append(f"对白 {chunk['chunk_id']}《{title}》")
        for frame in chunk.get("frames", []):
            source = frame.get("source", "")
            translation = frame.get("translation", "")
            if source:
                lines.append(f"原文：{source}")
            if translation:
                lines.append(f"现译：{translation}")
    return "\n".join(lines)


def main() -> int:
    parser = argparse.ArgumentParser(
        description="提取词典命名对白条件、对应假说和实际触发对白。"
    )
    parser.add_argument(
        "--out",
        default=str(PROJECT_DIR / "work" / "dictionary_trigger_aliases"),
        help="输出工作目录",
    )
    args = parser.parse_args()
    output_dir = Path(args.out).resolve()
    output_dir.mkdir(parents=True, exist_ok=True)

    cache = json.loads((PROJECT_DIR / "work" / "cache.json").read_text(encoding="utf-8"))
    path_to_chunk, chunks = dialogue_catalog(cache)
    translated_hypotheses = hypothesis_translations(cache)

    UnityPy, TypeTreeGenerator = load_unitypy()
    generator = TypeTreeGenerator("6000.0.73f1")
    generator.load_local_game(str(GAME_ROOT))
    env = UnityPy.load(str(DATA_DIR / "level0"))
    env.typetree_generator = generator

    hypotheses_by_term: dict[int, list[dict[str, str]]] = defaultdict(list)
    listeners: list[dict[str, Any]] = []
    for obj in env.objects:
        if obj.type.name != "MonoBehaviour":
            continue
        try:
            tree = obj.parse_as_dict()
        except Exception:
            continue
        hypos = tree.get("hypos")
        if isinstance(hypos, list):
            for index, hypothesis in enumerate(hypos):
                if not isinstance(hypothesis, dict) or not isinstance(hypothesis.get("termID"), int):
                    continue
                term_id = hypothesis["termID"]
                for field in ("aGuess", "bGuess", "cGuess"):
                    source = str(hypothesis.get(field, ""))
                    if not source:
                        continue
                    hypotheses_by_term[term_id].append(
                        {
                            "field_path": f"hypos[{index}].{field}",
                            "source": source,
                            "translation": translated_hypotheses.get(
                                f"hypos[{index}].{field}", ""
                            ),
                        }
                    )
        conditions = tree.get("conditions")
        dialogue_refs = tree.get("dc")
        if not isinstance(conditions, list) or not isinstance(dialogue_refs, list):
            continue
        dictionary_conditions = [
            condition
            for condition in conditions
            if isinstance(condition, dict)
            and condition.get("listenChannel") in CHANNEL_NAMES
            and condition.get("strValue") is not None
        ]
        if not dictionary_conditions:
            continue
        dialogue_ids = []
        for pointer in dialogue_refs:
            path_id = pointer.get("m_PathID", 0) if isinstance(pointer, dict) else 0
            chunk_id = path_to_chunk.get(path_id)
            if chunk_id is not None and chunk_id not in dialogue_ids:
                dialogue_ids.append(chunk_id)
        listeners.append(
            {
                "listener_path_id": obj.path_id,
                "conditions": conditions,
                "dictionary_conditions": dictionary_conditions,
                "dialogue_chunk_ids": dialogue_ids,
            }
        )

    grouped: dict[tuple[int | None, int, str], dict[str, Any]] = {}
    for listener in listeners:
        for condition in listener["dictionary_conditions"]:
            channel = int(condition["listenChannel"])
            term_id = None if channel in (14, 15) else int(condition.get("value", 0))
            trigger = str(condition.get("strValue", ""))
            key = (term_id, channel, trigger.casefold())
            record = grouped.setdefault(
                key,
                {
                    "term_id": term_id,
                    "channel": channel,
                    "channel_name": CHANNEL_NAMES[channel],
                    "match_mode": "contains" if channel == 20 else "exact",
                    "english_trigger": trigger,
                    "hypotheses": hypotheses_by_term.get(term_id, []) if term_id is not None else [],
                    "listener_path_ids": [],
                    "dialogue_chunk_ids": [],
                    "other_conditions": [],
                },
            )
            record["listener_path_ids"].append(listener["listener_path_id"])
            for chunk_id in listener["dialogue_chunk_ids"]:
                if chunk_id not in record["dialogue_chunk_ids"]:
                    record["dialogue_chunk_ids"].append(chunk_id)
            record["other_conditions"].append(
                [
                    value
                    for value in listener["conditions"]
                    if value is not condition
                ]
            )

    records = []
    for record in grouped.values():
        sources = {item["source"].casefold() for item in record["hypotheses"]}
        record["covered_by_hypothesis_alias"] = (
            record["term_id"] is not None
            and record["english_trigger"].casefold() in sources
        )
        record["dialogues"] = [
            chunks[chunk_id]
            for chunk_id in record["dialogue_chunk_ids"]
            if chunk_id in chunks
        ]
        records.append(record)
    records.sort(
        key=lambda value: (
            value["covered_by_hypothesis_alias"],
            value["term_id"] is None,
            value["term_id"] or 0,
            value["english_trigger"],
        )
    )
    uncovered = [value for value in records if not value["covered_by_hypothesis_alias"]]

    audit_path = output_dir / "source.json"
    audit_path.write_text(
        json.dumps(
            {
                "format_version": 1,
                "game_version": "0.10",
                "total_conditions": len(records),
                "uncovered_conditions": len(uncovered),
                "entries": uncovered,
            },
            ensure_ascii=False,
            indent=2,
        )
        + "\n",
        encoding="utf-8",
    )

    cache_entries = []
    for index, record in enumerate(uncovered, start=1):
        hypotheses = "；".join(
            f"{item['source']}→{item['translation']}" for item in record["hypotheses"]
        ) or "（无可用假说）"
        source_text = (
            f"词条ID：{record['term_id'] if record['term_id'] is not None else '全局'}\n"
            f"匹配方式：{record['match_mode']}\n"
            f"英文触发词：{record['english_trigger']}\n"
            f"该词条假说：{hypotheses}\n"
            f"触发上下文：\n{compact_context(record['dialogues'])}"
        )
        cache_entries.append(
            {
                "text_index": index,
                "translation_status": 0,
                "model": "",
                "source_text": source_text,
                "translated_text": "",
                "text_to_detect": source_text,
                "lang_code": None,
                "extra": {
                    "trigger": {
                        "term_id": record["term_id"],
                        "channel": record["channel"],
                        "match_mode": record["match_mode"],
                        "english_trigger": record["english_trigger"],
                        "dialogue_chunk_ids": record["dialogue_chunk_ids"],
                    }
                },
            }
        )

    translation_cache = {
        "project_id": "tmfds-dictionary-trigger-aliases-zh-cn",
        "project_type": "Mtool",
        "project_name": "The Message from Deep Space 中文词典触发别名",
        "project_create_time": datetime.now(timezone.utc).isoformat(),
        "input_path": str(audit_path),
        "stats_data": {
            "total_requests": 0,
            "error_requests": 0,
            "start_time": 0.0,
            "total_line": 0,
            "line": 0,
            "token": 0,
            "total_completion_tokens": 0,
            "time": 0.0,
        },
        "files": {
            "dictionary_trigger_aliases": {
                "storage_path": "dictionary_trigger_aliases",
                "encoding": "utf-8",
                "file_project_type": "Mtool",
                "line_ending": "\n",
                "items": cache_entries,
                "language_stats": [],
                "lc_language_stats": [],
                "extra": {"source": "Unity AdvancedListener conditions"},
            }
        },
        "detected_encoding": "utf-8",
        "detected_line_ending": "\n",
        "extra": {"format_version": 1, "game_version": "0.10"},
    }
    cache_path = output_dir / "cache.json"
    cache_path.write_text(
        json.dumps(translation_cache, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )
    print(
        json.dumps(
            {
                "audit": str(audit_path),
                "cache": str(cache_path),
                "total": len(records),
                "uncovered": len(uncovered),
            },
            ensure_ascii=False,
        )
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
