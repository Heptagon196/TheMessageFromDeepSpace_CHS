from __future__ import annotations

import json
from dataclasses import dataclass
from pathlib import Path
from typing import Any


@dataclass(frozen=True)
class DictionaryDialogueFix:
    dialogue_chunk_id: int
    channel: str
    english: str
    original_term_id: int
    replacement_term_id: int
    note: str


def load_fixes(directory: Path) -> list[DictionaryDialogueFix]:
    if not directory.is_dir():
        raise ValueError(f"词典对白修正目录不存在：{directory}")
    fixes: list[DictionaryDialogueFix] = []
    seen: set[tuple[int, str, str]] = set()
    for path in sorted(directory.glob("*.json"), key=lambda item: item.name.casefold()):
        raw: dict[str, Any] = json.loads(path.read_text(encoding="utf-8"))
        allowed = {
            "dialogue_chunk_id", "channel", "english", "original_term_id",
            "replacement_term_id", "note",
        }
        unknown = set(raw) - allowed
        if unknown:
            raise ValueError(f"{path.name} 含未知字段：{sorted(unknown)!r}")
        required = allowed - {"note"}
        missing = required - set(raw)
        if missing:
            raise ValueError(f"{path.name} 缺少字段：{sorted(missing)!r}")
        chunk_id = raw["dialogue_chunk_id"]
        channel = raw["channel"]
        english = raw["english"]
        old_id = raw["original_term_id"]
        new_id = raw["replacement_term_id"]
        if not isinstance(chunk_id, int) or chunk_id <= 0:
            raise ValueError(f"{path.name}: dialogue_chunk_id 必须是正整数")
        if path.stem != str(chunk_id):
            raise ValueError(f"{path.name}: 文件名必须等于 dialogue_chunk_id")
        if not isinstance(channel, str) or not channel.strip():
            raise ValueError(f"{path.name}: channel 不能为空")
        if not isinstance(english, str) or not english.strip():
            raise ValueError(f"{path.name}: english 不能为空")
        if not isinstance(old_id, int) or not isinstance(new_id, int) or old_id == new_id:
            raise ValueError(f"{path.name}: 原始和替换词条 ID 必须是不同整数")
        key = (chunk_id, channel.casefold(), english.casefold())
        if key in seen:
            raise ValueError(f"{path.name}: 修正规则重复：{key!r}")
        seen.add(key)
        fixes.append(DictionaryDialogueFix(
            chunk_id, channel, english, old_id, new_id, str(raw.get("note", "")),
        ))
    return fixes


def validate_against_source(fixes: list[DictionaryDialogueFix], source: dict[str, Any]) -> None:
    entries = list(source.get("entries", [])) + list(source.get("covered_entries", []))
    known_term_ids = {entry.get("term_id") for entry in entries}
    for fix in fixes:
        candidates = [
            entry for entry in entries
            if entry.get("term_id") == fix.original_term_id
            and str(entry.get("channel_name", "")).casefold() == fix.channel.casefold()
            and str(entry.get("english_trigger", "")).casefold() == fix.english.casefold()
            and fix.dialogue_chunk_id in entry.get("dialogue_chunk_ids", [])
        ]
        if len(candidates) != 1:
            raise ValueError(
                f"对白 {fix.dialogue_chunk_id} 的原始条件校验失败：应唯一匹配 "
                f"term={fix.original_term_id}, channel={fix.channel}, english={fix.english!r}，"
                f"实际 {len(candidates)} 条"
            )
        if fix.replacement_term_id not in known_term_ids:
            raise ValueError(
                f"对白 {fix.dialogue_chunk_id} 的替换词条 ID "
                f"{fix.replacement_term_id} 不存在于提取数据"
            )
        already_fixed = [
            entry for entry in entries
            if entry.get("term_id") == fix.replacement_term_id
            and str(entry.get("channel_name", "")).casefold() == fix.channel.casefold()
            and str(entry.get("english_trigger", "")).casefold() == fix.english.casefold()
            and fix.dialogue_chunk_id in entry.get("dialogue_chunk_ids", [])
        ]
        if already_fixed:
            raise ValueError(
                f"对白 {fix.dialogue_chunk_id} 的提取数据已经是修正后的条件，"
                "请删除已失效的修正规则"
            )


def apply_to_alias_entries(entries: list[dict[str, Any]],
                           fixes: list[DictionaryDialogueFix]) -> None:
    for fix in fixes:
        candidates = [
            entry for entry in entries
            if entry.get("term_id") == fix.original_term_id
            and str(entry.get("channel", "")).casefold() == fix.channel.casefold()
            and str(entry.get("english", "")).casefold() == fix.english.casefold()
            and fix.dialogue_chunk_id in entry.get("dialogue_ids", [])
        ]
        if len(candidates) != 1:
            raise ValueError(
                f"无法把对白 {fix.dialogue_chunk_id} 的条件修正同步到中文触发表"
            )
        original = candidates[0]
        original["dialogue_ids"] = [
            item for item in original["dialogue_ids"] if item != fix.dialogue_chunk_id
        ]
        corrected = {
            **original,
            "term_id": fix.replacement_term_id,
            "dialogue_ids": [fix.dialogue_chunk_id],
            "note": (original.get("note", "") + " " +
                     f"对白 {fix.dialogue_chunk_id} 的原版词条 ID 错误已由 Fix 规则修正。"),
        }
        entries.append(corrected)
        if not original["dialogue_ids"]:
            entries.remove(original)
