#!/usr/bin/env python3
"""Split untranslated game strings into context-aware, agent-safe source batches."""

from __future__ import annotations

import argparse
import json
import os
from collections import defaultdict
from pathlib import Path
from typing import Any, Iterable


def iter_items(cache: dict[str, Any]) -> Iterable[dict[str, Any]]:
    for cache_file in cache["files"].values():
        yield from cache_file["items"]


def compact(item: dict[str, Any]) -> dict[str, Any]:
    game = item["extra"]["game"]
    context = {
        key: game[key]
        for key in (
            "kind",
            "stable_key",
            "chunk_id",
            "chunk_name",
            "speaker",
            "object_path",
            "object_name",
            "field_path",
            "template_id",
            "fragment_id",
        )
        if key in game
    }
    return {
        "text_index": item["text_index"],
        "source_text": item["source_text"],
        "context": context,
    }


def split_dialogue(items: list[dict[str, Any]], group_count: int):
    by_chunk: dict[int, list[dict[str, Any]]] = defaultdict(list)
    for item in items:
        game = item["extra"]["game"]
        by_chunk[int(game["chunk_id"])].append(item)
    chunks = []
    for chunk_id in sorted(by_chunk):
        chunk_items = sorted(
            by_chunk[chunk_id],
            key=lambda item: (
                0 if item["extra"]["game"]["kind"] == "dialogue_title" else 1,
                item["extra"]["game"].get("frame_index", -1),
            ),
        )
        chunks.append((chunk_id, chunk_items))

    groups = []
    cursor = 0
    remaining_items = sum(len(values) for _, values in chunks)
    for group_number in range(group_count):
        groups_left = group_count - group_number
        target = remaining_items / groups_left
        selected = []
        count = 0
        while cursor < len(chunks):
            chunk_id, values = chunks[cursor]
            chunks_after = len(chunks) - cursor - 1
            if selected and count >= target and chunks_after >= groups_left - 1:
                break
            selected.append((chunk_id, values))
            count += len(values)
            cursor += 1
        remaining_items -= count
        flattened = [item for _, values in selected for item in values]
        groups.append(
            (
                f"dialogue_chunks_{selected[0][0]}_{selected[-1][0]}",
                flattened,
                {
                    "category": "dialogue_and_titles",
                    "chunk_start": selected[0][0],
                    "chunk_end": selected[-1][0],
                },
            )
        )
    assert cursor == len(chunks)
    return groups


def split_source_clusters(items: list[dict[str, Any]], group_count: int, prefix: str):
    clusters: dict[str, list[dict[str, Any]]] = defaultdict(list)
    for item in items:
        clusters[item["source_text"]].append(item)
    bins: list[list[dict[str, Any]]] = [[] for _ in range(group_count)]
    sizes = [0] * group_count
    for _, cluster in sorted(clusters.items(), key=lambda pair: (-len(pair[1]), pair[0])):
        target = min(range(group_count), key=lambda index: sizes[index])
        bins[target].extend(cluster)
        sizes[target] += len(cluster)
    return [
        (
            f"{prefix}_{index + 1}",
            sorted(values, key=lambda item: item["extra"]["game"]["stable_key"]),
            {"category": prefix},
        )
        for index, values in enumerate(bins)
    ]


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("cache", type=Path)
    parser.add_argument("output", type=Path)
    parser.add_argument("--dialogue-groups", type=int, default=14)
    args = parser.parse_args()

    cache = json.loads(args.cache.read_text(encoding="utf-8"))
    untranslated = [item for item in iter_items(cache) if item["translation_status"] == 0]
    args.output.mkdir(parents=True, exist_ok=True)
    existing = list(args.output.glob("src_*.json"))
    if existing:
        parser.error(f"output already contains {len(existing)} source batch(es): {args.output}")

    dialogue = [
        item
        for item in untranslated
        if item["extra"]["game"]["kind"] in {"dialogue_frame", "dialogue_title"}
    ]
    ui_core = [
        item
        for item in untranslated
        if item["extra"]["game"]["kind"]
        in {
            "ui_text",
            "achievement_name",
            "achievement_description",
            "ui_template",
            "ui_fragment",
        }
    ]
    display = [
        item
        for item in untranslated
        if item["extra"]["game"]["kind"] == "display_value"
    ]
    hypotheses = [
        item
        for item in untranslated
        if item["extra"]["game"]["kind"] == "component_string"
        and item["extra"]["game"].get("field_path", "").startswith("hypos[")
    ]
    system_other = [
        item
        for item in untranslated
        if item["extra"]["game"]["kind"]
        in {"component_string", "component_dialogue_frame"}
        and item not in hypotheses
    ]

    groups = split_dialogue(dialogue, args.dialogue_groups)
    groups.append(("ui_core", sorted(ui_core, key=lambda item: item["extra"]["game"]["stable_key"]), {"category": "ui_core"}))
    groups.extend(split_source_clusters(display, 2, "display_values"))
    groups.append(("system_hypotheses", sorted(hypotheses, key=lambda item: item["extra"]["game"]["stable_key"]), {"category": "system_hypotheses"}))
    groups.append(("system_other", sorted(system_other, key=lambda item: item["extra"]["game"]["stable_key"]), {"category": "system_other"}))

    grouped_ids = [item["text_index"] for _, values, _ in groups for item in values]
    expected_ids = [item["text_index"] for item in untranslated]
    assert len(grouped_ids) == len(set(grouped_ids))
    assert set(grouped_ids) == set(expected_ids)

    manifest = []
    for index, (name, values, metadata) in enumerate(groups, start=1):
        filename = f"src_{index:02d}_{name}.json"
        path = args.output / filename
        path.write_text(
            json.dumps([compact(item) for item in values], ensure_ascii=False, indent=2) + "\n",
            encoding="utf-8",
        )
        manifest.append(
            {
                "group": index,
                "name": name,
                "source": filename,
                "translation": f"trans_{index:02d}.json",
                "newterms": f"newterms_{index:02d}.txt",
                "count": len(values),
                **metadata,
            }
        )

    (args.output / "manifest.json").write_text(
        json.dumps(
            {
                "cache": Path(os.path.relpath(args.cache.resolve(), args.output.resolve())).as_posix(),
                "untranslated": len(untranslated),
                "groups": manifest,
            },
            ensure_ascii=False,
            indent=2,
        )
        + "\n",
        encoding="utf-8",
    )
    print(json.dumps(manifest, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
