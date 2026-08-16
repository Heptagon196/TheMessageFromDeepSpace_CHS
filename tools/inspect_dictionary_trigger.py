from __future__ import annotations

import argparse
import json
from pathlib import Path
from typing import Any, Iterable


PROJECT_ROOT = Path(__file__).resolve().parents[1]
DEFAULT_SOURCE = PROJECT_ROOT / "work" / "dictionary_trigger_aliases" / "source.json"
DEFAULT_ALIASES = PROJECT_ROOT / "patch" / "Translations" / "dictionary_trigger_aliases.json"


def load_json(path: Path) -> dict[str, Any]:
    return json.loads(path.read_text(encoding="utf-8"))


def _single_trigger_records(source: dict[str, Any]) -> Iterable[tuple[str, dict[str, Any]]]:
    # source.json deliberately separates uncovered triggers from triggers already
    # covered by a translated dictionary hypothesis. Both groups are real game
    # listeners and must be searched.
    for partition in ("entries", "covered_entries"):
        for record in source.get(partition, []):
            if isinstance(record, dict):
                yield partition, record


def _alias_key(record: dict[str, Any]) -> tuple[int | None, str, str]:
    term_id = record.get("term_id")
    # Extracted source records contain both the numeric channel and its readable
    # name, while the distributable alias file stores only the readable name.
    channel = str(record.get("channel_name") or record.get("channel") or "")
    english = str(record.get("english") or record.get("english_trigger") or "")
    return term_id, channel.casefold(), english.casefold()


def inspect_term_triggers(
    source: dict[str, Any], aliases: dict[str, Any], term_id: int
) -> dict[str, Any]:
    alias_index: dict[tuple[int | None, str, str], list[dict[str, Any]]] = {}
    for alias in aliases.get("entries", []):
        if isinstance(alias, dict):
            alias_index.setdefault(_alias_key(alias), []).append(alias)

    single_triggers: list[dict[str, Any]] = []
    seen_single: set[tuple[str, str, tuple[int, ...]]] = set()
    matched_alias_keys: set[tuple[int | None, str, str]] = set()

    for partition, record in _single_trigger_records(source):
        if record.get("term_id") != term_id:
            continue

        channel = str(record.get("channel_name", ""))
        english = str(record.get("english_trigger", ""))
        dialogue_ids = tuple(int(value) for value in record.get("dialogue_chunk_ids", []))
        identity = channel.casefold(), english.casefold(), dialogue_ids
        if identity in seen_single:
            continue
        seen_single.add(identity)

        alias_key = _alias_key(record)
        matched_alias_keys.add(alias_key)
        matching_aliases = alias_index.get(alias_key, [])
        single_triggers.append(
            {
                "source_partition": partition,
                "channel": channel,
                "match_mode": record.get("match_mode"),
                "english": english,
                "localized_rules": [
                    rule
                    for alias in matching_aliases
                    for rule in alias.get("rules", [])
                    if isinstance(rule, dict)
                ],
                "dialogue_ids": list(dialogue_ids),
                "hypotheses": record.get("hypotheses", []),
                "dialogues": record.get("dialogues", []),
            }
        )

    combination_triggers: list[dict[str, Any]] = []
    for listener in source.get("combination_listeners", []):
        if not isinstance(listener, dict):
            continue
        conditions = [
            condition
            for condition in listener.get("conditions", [])
            if isinstance(condition, dict)
        ]
        if not any(condition.get("term_id") == term_id for condition in conditions):
            continue
        combination_triggers.append(
            {
                "listener_path_id": listener.get("listener_path_id"),
                "conditions": conditions,
                "dialogue_ids": listener.get("dialogue_chunk_ids", []),
                "dialogues": listener.get("dialogues", []),
            }
        )

    alias_only: list[dict[str, Any]] = []
    for key, records in alias_index.items():
        if key[0] == term_id and key not in matched_alias_keys:
            alias_only.extend(records)

    dialogue_variants = [
        variant
        for variant in aliases.get("dialogue_variants", [])
        if isinstance(variant, dict) and variant.get("term_id") == term_id
    ]

    return {
        "term_id": term_id,
        "has_trigger": bool(single_triggers or combination_triggers or alias_only or
                            dialogue_variants),
        "single_triggers": single_triggers,
        "combination_triggers": combination_triggers,
        "alias_only_records": alias_only,
        "dialogue_variants": dialogue_variants,
    }


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="按词典词条 ID 查看所有命名对白触发条件。"
    )
    parser.add_argument("term_id", type=int, help="词典词条 ID，例如 -102")
    parser.add_argument("--source", type=Path, default=DEFAULT_SOURCE)
    parser.add_argument("--aliases", type=Path, default=DEFAULT_ALIASES)
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    result = inspect_term_triggers(
        load_json(args.source), load_json(args.aliases), args.term_id
    )
    print(json.dumps(result, ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
