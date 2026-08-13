from __future__ import annotations

import copy
import importlib.util
import sys
from pathlib import Path


PROJECT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(PROJECT / "tools"))
spec = importlib.util.spec_from_file_location(
    "update_translation", PROJECT / "tools" / "update_translation.py"
)
update_translation = importlib.util.module_from_spec(spec)
assert spec.loader is not None
spec.loader.exec_module(update_translation)


dialogue = {
    "text_index": 42,
    "source_text": "{SPEAKER_AKERS}{PART_000}[garbled]",
    "extra": {
        "game": {
            "kind": "dialogue_frame",
            "stable_key": "dialogue:1/frame:0",
            "source_sha256": "abc",
        }
    },
}
assert (
    update_translation.compose_translation(dialogue, "[含混的嘟囔]")
    == "{SPEAKER_AKERS}{PART_000}[含混的嘟囔]"
)

multi = copy.deepcopy(dialogue)
multi["source_text"] += "{PART_001}more"
try:
    update_translation.compose_translation(multi, "不能猜如何分段")
except ValueError as exc:
    assert "多段对白" in str(exc)
else:
    raise AssertionError("多段对白不应静默丢失 PART 结构")

payload = {"format_version": 1, "entries": []}
updated = update_translation.upsert_override(
    payload, dialogue, "{SPEAKER_AKERS}{PART_000}[含混的嘟囔]"
)
assert updated["entries"] == [
    {
        "text_index": 42,
        "source_sha256": "abc",
        "translated_text": "{SPEAKER_AKERS}{PART_000}[含混的嘟囔]",
    }
]
replaced = update_translation.upsert_override(updated, dialogue, "changed")
assert len(replaced["entries"]) == 1
assert replaced["entries"][0]["translated_text"] == "changed"

print("Single-command translation update tests passed.")
