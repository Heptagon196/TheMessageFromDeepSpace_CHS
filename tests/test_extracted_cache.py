from __future__ import annotations

import hashlib
import json
import re
from collections import Counter
from pathlib import Path


PROJECT = Path(__file__).resolve().parents[1]
CACHE_PATH = PROJECT / "work" / "cache.json"


def iter_items(cache: dict):
    for cache_file in cache["files"].values():
        yield from cache_file["items"]


cache = json.loads(CACHE_PATH.read_text(encoding="utf-8"))
items = list(iter_items(cache))

assert len(items) == 12_509

text_indices = [item["text_index"] for item in items]
stable_keys = [item["extra"]["game"]["stable_key"] for item in items]
assert len(text_indices) == len(set(text_indices)), "text_index 必须全局唯一"
assert len(stable_keys) == len(set(stable_keys)), "stable_key 必须全局唯一"

for item in items:
    game = item["extra"]["game"]
    actual_hash = hashlib.sha256(item["source_text"].encode("utf-8")).hexdigest()
    assert game["source_sha256"] == actual_hash, game["stable_key"]

kind_counts = Counter(item["extra"]["game"]["kind"] for item in items)
assert kind_counts == {
    "dialogue_frame": 7_099,
    "dialogue_title": 1_227,
    "ui_text": 1_779,
    "component_string": 1_005,
    "component_dialogue_frame": 23,
    "achievement_name": 36,
    "achievement_description": 39,
    "display_value": 1_279,
    "ui_template": 13,
    "ui_fragment": 9,
}

component_items = [
    item for item in items if item["extra"]["game"]["kind"] == "component_string"
]
component_paths = Counter(item["extra"]["game"]["field_path"] for item in component_items)
assert sum(path.startswith("actSections[") for path in component_paths) == 13
assert sum(path.startswith("numberStrings[") for path in component_paths) == 61
assert sum(path.startswith("hypos[") for path in component_paths) == 639
assert component_paths["progressLogData.actSection"] == 42
assert component_paths["progressLogData.nextActName"] == 41
assert component_paths["progressLogData.actName"] == 42
assert component_paths["pilotProf.speakerName"] == 1
assert component_paths["qopilotProf.speakerName"] == 1

frame_items = [
    item
    for item in items
    if item["extra"]["game"]["kind"] == "component_dialogue_frame"
]
frame_paths = Counter(item["extra"]["game"]["field_path"] for item in frame_items)
assert sum(path.startswith("nameIsTakenPrompts[") for path in frame_paths) == 20
assert frame_paths["autoLogStartFrame"] == 1
assert frame_paths["autoLogEndFrame"] == 1
assert frame_paths["inactivityExitFrame"] == 1

display_items = [
    item for item in items if item["extra"]["game"]["kind"] == "display_value"
]
display_paths = Counter(item["extra"]["game"]["field_path"] for item in display_items)
assert display_paths == {
    "title": 982,
    "puzzleGroupName": 145,
    "elementName": 92,
    "universalAbundanceRank": 3,
    "songTitle": 11,
    "isotopes[0].unit": 14,
    "isotopes[1].unit": 13,
    "isotopes[2].unit": 14,
    "isotopes[3].unit": 4,
    "isotopes[4].unit": 1,
}

template_ids = {
    item["extra"]["game"]["template_id"]
    for item in items
    if item["extra"]["game"]["kind"] == "ui_template"
}
assert template_ids == {
    "atomic-mass",
    "atomic-number",
    "calculator-invalid-log",
    "calculator-too-large",
    "duplicate-dictionary-name",
    "graph-parse-failed",
    "idea-log-entry",
    "invalid-transmission",
    "missing-visual",
    "name-dictionary-term",
    "puzzle-group",
    "save-path",
    "universal-abundance",
}

for item in items:
    dyn_tokens = [int(value) for value in re.findall(r"\{DYN_(\d+)\}", item["source_text"])]
    if dyn_tokens:
        first_occurrences = list(dict.fromkeys(dyn_tokens))
        assert first_occurrences == list(range(max(dyn_tokens) + 1)), item["extra"]["game"]["stable_key"]

for item in component_items:
    game = item["extra"]["game"]
    if game["field_path"] == "fileSuffix":
        assert item["translation_status"] == 7
        assert game["exclude_reason"] == "internal_file_suffix"

translatable = [item for item in items if item["translation_status"] == 0]
for item in translatable:
    source = item["source_text"]
    assert not re.search(r"\bSIGNAL_-?\d+\b", source), item["extra"]["game"]["stable_key"]

print(
    "Extracted-cache audit passed: 12,509 unique items, recovered component data, "
    "dynamic UI, display data and protected gameplay state."
)
