from __future__ import annotations

from dataclasses import dataclass
from itertools import permutations
from typing import Any, Iterable


def channel_group(channel: str) -> int:
    value = str(channel or "").casefold()
    if value in {"editentryfromname", "editentryidfromname"}:
        return 0
    if value in {"editentrytoname", "editentryidtoname", "editentryidcontains"}:
        return 1
    if value == "dictentryis":
        return 2
    return -1


def normalize(value: str) -> str:
    return str(value or "").strip().casefold()


def condition_key(entry: dict[str, Any]) -> tuple[int | None, str, str]:
    return (
        entry.get("term_id"),
        str(entry.get("channel", "")).casefold(),
        str(entry.get("english", "")).casefold(),
    )


def rule_match_length(rule: dict[str, Any], candidate: str) -> int:
    values = [str(value) for value in rule.get("values", []) if str(value)]
    rule_type = str(rule.get("type", "")).casefold()
    folded = candidate.casefold()
    if any(
        str(value).casefold() in folded
        for value in rule.get("exclude_any", [])
        if str(value)
    ):
        return 0
    if rule_type == "contains_all":
        if not values or any(value.casefold() not in folded for value in values):
            return 0
        return sum(len(value) for value in values)
    if rule_type == "contains":
        return max(
            (len(value) for value in values if value.casefold() in folded),
            default=0,
        )
    if rule_type == "exact":
        stripped = normalize(candidate)
        return max(
            (len(value.strip()) for value in values if normalize(value) == stripped),
            default=0,
        )
    return 0


def entry_match_length(entry: dict[str, Any], candidate: str) -> int:
    return max(
        (rule_match_length(rule, candidate) for rule in entry.get("rules", [])),
        default=0,
    )


@dataclass(frozen=True)
class Conflict:
    term_id: int | None
    channel_group: int
    candidate: str
    conditions: tuple[tuple[int | None, str, str], ...]

    @property
    def resolution_key(self) -> tuple[int | None, int, str]:
        return (self.term_id, self.channel_group, normalize(self.candidate))


def _scopes(entries: Iterable[dict[str, Any]]) -> list[int | None]:
    term_ids = sorted(
        {int(entry["term_id"]) for entry in entries if entry.get("term_id") is not None}
    )
    return [None, *term_ids]


def _relevant(entry: dict[str, Any], term_id: int | None, group: int) -> bool:
    if channel_group(str(entry.get("channel", ""))) != group:
        return False
    entry_term = entry.get("term_id")
    if term_id is None:
        return entry_term is None
    return entry_term is None or entry_term == term_id


def _atomic_candidates(entries: Iterable[dict[str, Any]]) -> set[str]:
    values: set[str] = set()
    for entry in entries:
        for rule in entry.get("rules", []):
            rule_values = [str(value).strip() for value in rule.get("values", [])]
            values.update(value for value in rule_values if value)
            if str(rule.get("type", "")).casefold() == "contains_all":
                for ordered in permutations(rule_values):
                    combined = "".join(ordered).strip()
                    if combined:
                        values.add(combined)
    return values


def find_conflicts(entries: list[dict[str, Any]]) -> list[Conflict]:
    """Find aliases that match multiple source conditions in one naming event."""
    conflicts: dict[tuple[int | None, int, str], Conflict] = {}
    for term_id in _scopes(entries):
        for group in range(3):
            relevant = [entry for entry in entries if _relevant(entry, term_id, group)]
            if len(relevant) < 2:
                continue
            for candidate in sorted(_atomic_candidates(relevant)):
                matched = [
                    (entry_match_length(entry, candidate), entry)
                    for entry in relevant
                ]
                winners = {
                    condition_key(entry)
                    for length, entry in matched
                    if length > 0
                }
                if len(winners) < 2:
                    continue
                conflict = Conflict(
                    term_id,
                    group,
                    candidate,
                    tuple(sorted(winners, key=repr)),
                )
                conflicts[conflict.resolution_key] = conflict
    return sorted(conflicts.values(), key=lambda item: repr(item.resolution_key))


def format_conflict(conflict: Conflict) -> str:
    conditions = ", ".join(
        f"{channel}/{english}" for _, channel, english in conflict.conditions
    )
    return (
        f"term_id={conflict.term_id}, group={conflict.channel_group}, "
        f"input={conflict.candidate!r}: {conditions}"
    )


def validate_no_conflicts(entries: list[dict[str, Any]]) -> None:
    conflicts = find_conflicts(entries)
    if conflicts:
        details = "\n".join(f"  - {format_conflict(item)}" for item in conflicts)
        raise ValueError(
            "词典中文触发配置仍可能让一次输入同时命中多个条件：\n" + details
        )
