from __future__ import annotations

import importlib.util
import struct
from pathlib import Path


PROJECT = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location(
    "extraction_rules", PROJECT / "tools" / "extraction_rules.py"
)
assert spec and spec.loader
rules = importlib.util.module_from_spec(spec)
spec.loader.exec_module(rules)


source, metadata = rules.protect_component_source(
    "METEOR_OS vXXX\nTRANSLATION\nDDD CCC TTT",
    "bootupText_s",
)
assert source == (
    "METEOR_OS v{DYN_0}\n{PLAYER_NAME}\n{DYN_1} {DYN_2} {DYN_3}"
)
assert metadata["runtime_tokens"] == {
    "XXX": "{DYN_0}",
    "DDD": "{DYN_1}",
    "CCC": "{DYN_2}",
    "TTT": "{DYN_3}",
}
assert metadata["player_token_literal"] == "TRANSLATION"

source, metadata = rules.protect_component_source(
    "Copied X to Clipboard", "clipboard_s"
)
assert source == "Copied {DYN_0} to Clipboard"
assert metadata["runtime_tokens"] == {"X": "{DYN_0}"}

assert rules.exclusion_reason("fileSuffix", ".save") == "internal_file_suffix"
assert rules.exclusion_reason("bootupText_s", "METEOR_OS ONLINE") is None

console_message_fields = {
    "loadingSignalMsg",
    "tokenizingSignalMsg",
    "correctInput",
    "wrongInput",
    "sendingSignalMsg",
    "recompilingMsg",
    "updatingMsg",
    "winResponseLine",
    "loadingTxt",
    "failedToRetrieveResponse",
}
assert console_message_fields <= getattr(
    rules, "EXPLICIT_COMPONENT_STRING_FIELDS", set()
), "ConsoleLoaderMessage 的运行时提示字段必须显式进入提取器"


def aligned_string(value: str) -> bytes:
    encoded = value.encode("utf-8")
    data = struct.pack("<i", len(encoded)) + encoded
    return data + b"\0" * ((-len(data)) % 4)


values = ["Act 0", "Act I", "Grand Finale"]
blob = b"head" + struct.pack("<i", len(values)) + b"".join(
    aligned_string(value) for value in values
)
recovered = rules.recover_string_array(blob, "Act 0", expected_count=3)
assert recovered == values

templates = {template["source_text"] for template in rules.UI_TEMPLATES}
assert "Transmission: {DYN_0}, Time: {DYN_1}" in templates
assert "Atomic Number: {DYN_0}" in templates
assert "No {DYN_0} in {DYN_1} {DYN_2}" in templates

fragments = {fragment["source_text"] for fragment in rules.UI_FRAGMENTS}
assert "Half-Life: Almost Instantaneous" in fragments
assert "Stable: yes" in fragments
assert "_UNDEF" in fragments

print("Extraction-rules self-test passed: locked tokens, exclusions, raw arrays and UI templates.")
