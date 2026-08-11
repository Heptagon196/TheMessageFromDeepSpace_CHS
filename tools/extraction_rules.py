from __future__ import annotations

import struct
from typing import Any


# These fields are runtime templates. The game replaces the literal markers after
# the localization patch has supplied the translated template, so translators must
# see locked placeholders instead of ordinary English-looking text.
RUNTIME_FIELD_TOKENS: dict[str, list[tuple[str, str]]] = {
    "invalidBaseDigit_s": [("X", "{DYN_0}")],
    "clipboard_s": [("X", "{DYN_0}")],
    "clipboardPaste_s": [("X", "{DYN_0}")],
    "bootupText_s": [
        ("XXX", "{DYN_0}"),
        ("DDD", "{DYN_1}"),
        ("CCC", "{DYN_2}"),
        ("TTT", "{DYN_3}"),
    ],
    "shutdownText_s": [
        ("XXX", "{DYN_0}"),
        ("DDD", "{DYN_1}"),
        ("CCC", "{DYN_2}"),
        ("TTT", "{DYN_3}"),
    ],
}

PLAYER_TOKEN_FIELDS = {
    "bootupText_s": "TRANSLATION",
    "shutdownText_s": "TRANSLATION",
}

INTERNAL_FIELDS = {
    "fileSuffix": "internal_file_suffix",
}


# User-facing strings whose field names do not contain the generic extraction
# hints. Keep this explicit so arbitrary gameplay "input" fields stay excluded.
EXPLICIT_COMPONENT_STRING_FIELDS = {
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


# Text assembled directly in Assembly-CSharp rather than stored in a Unity asset.
# DYN markers are captured from the live string and copied into the translation.
UI_TEMPLATES: list[dict[str, Any]] = [
    {"template_id": "calculator-invalid-log", "source_text": "<= 0 invalid logarithm"},
    {"template_id": "calculator-too-large", "source_text": "A too large (>{DYN_0})"},
    {"template_id": "graph-parse-failed", "source_text": "Failed to parse function"},
    {
        "template_id": "idea-log-entry",
        "source_text": "Transmission: {DYN_0}, Time: {DYN_1}",
    },
    {"template_id": "save-path", "source_text": "Path: {DYN_0}"},
    {
        "template_id": "puzzle-group",
        "source_text": "GROUP {DYN_0} - {DYN_1}",
        "translate_display_values": True,
    },
    {"template_id": "atomic-number", "source_text": "Atomic Number: {DYN_0}"},
    {"template_id": "atomic-mass", "source_text": "Atomic Mass: {DYN_0}"},
    {
        "template_id": "universal-abundance",
        "source_text": "Universal Abundance: {DYN_0}",
    },
    {
        "template_id": "missing-visual",
        "source_text": "No {DYN_0} in {DYN_1} {DYN_2}",
    },
    {
        "template_id": "name-signal",
        "source_text": "NAME SIGNAL {DYN_0}",
    },
    {
        "template_id": "invalid-transmission",
        "source_text": "{DYN_0} {DYN_1} is invalid ",
    },
    {
        "template_id": "duplicate-dictionary-name",
        "source_text": "{DYN_0} already \"{DYN_1}\"",
    },
    {
        "template_id": "name-dictionary-term",
        "source_text": "NAME {DYN_0} IN DICTIONARY",
    },
]


# Dynamic TMP components whose generated contents contain game-authored display
# values (currently puzzle-group names). All other dynamic text is opaque: it can
# contain file paths, program names, compiler output, or player-authored text and
# must never receive dictionary-style substitutions.
DISPLAY_VALUE_UI_PATHS = {
    "Progress Log (Canvas) (start inactive)/FULL PROGRESS LOG (start inactive)[1]/TRANSMISSION GROUP LOG[6]",
}


# Fragments used to construct longer periodic-table strings. They are translated
# inside the completed TMP string, after which bilingual formatting is applied once.
UI_FRAGMENTS: list[dict[str, str]] = [
    {"fragment_id": "half-life-instant", "source_text": "Half-Life: Almost Instantaneous"},
    {"fragment_id": "abundance-trace", "source_text": "Abundance: trace"},
    {"fragment_id": "stable-yes", "source_text": "Stable: yes"},
    {"fragment_id": "stable-no", "source_text": "Stable: no"},
    {"fragment_id": "isotope", "source_text": "Isotope: "},
    {"fragment_id": "neutrons", "source_text": "Neutrons: "},
    {"fragment_id": "abundance", "source_text": "Abundance: "},
    {"fragment_id": "half-life", "source_text": "Half-Life: "},
    {"fragment_id": "element-preview", "source_text": " | Element "},
    {"fragment_id": "undefined-suffix", "source_text": "_UNDEF"},
]


def protect_component_source(text: str, field_path: str) -> tuple[str, dict[str, Any]]:
    """Protect field-specific runtime substitutions and return runtime metadata."""
    field_name = field_path.split("[", 1)[0].split(".", 1)[0]
    source = text
    metadata: dict[str, Any] = {"protect_player_name": False}
    mappings = RUNTIME_FIELD_TOKENS.get(field_name, [])
    if mappings:
        runtime_tokens: dict[str, str] = {}
        for literal, placeholder in mappings:
            source = source.replace(literal, placeholder)
            runtime_tokens[literal] = placeholder
        metadata["runtime_tokens"] = runtime_tokens
    player_literal = PLAYER_TOKEN_FIELDS.get(field_name)
    if player_literal:
        source = source.replace(player_literal, "{PLAYER_NAME}")
        metadata["player_token_literal"] = player_literal
    return source, metadata


def exclusion_reason(field_path: str, text: str) -> str | None:
    field_name = field_path.split("[", 1)[0].split(".", 1)[0]
    return INTERNAL_FIELDS.get(field_name)


def _read_aligned_string(data: bytes, offset: int) -> tuple[str, int]:
    if offset + 4 > len(data):
        raise ValueError("string length is outside object data")
    length = struct.unpack_from("<i", data, offset)[0]
    if length < 0 or offset + 4 + length > len(data):
        raise ValueError("string data is outside object data")
    start = offset + 4
    end = start + length
    value = data[start:end].decode("utf-8")
    return value, (end + 3) & ~3


def _aligned_string_bytes(value: str) -> bytes:
    encoded = value.encode("utf-8")
    result = struct.pack("<i", len(encoded)) + encoded
    return result + b"\0" * ((-len(result)) % 4)


def recover_string_array(data: bytes, first_value: str, expected_count: int) -> list[str]:
    """Recover a Unity string[] whose generated type tree incorrectly says string."""
    marker = struct.pack("<i", expected_count) + _aligned_string_bytes(first_value)
    start = data.find(marker)
    if start < 0:
        raise ValueError(f"string array marker not found: {first_value!r}")
    offset = start + 4
    values: list[str] = []
    for _ in range(expected_count):
        value, offset = _read_aligned_string(data, offset)
        values.append(value)
    return values


def recover_string_sequence(data: bytes, first_value: str, count: int) -> list[str]:
    """Recover consecutive aligned Unity string fields from a known first value."""
    marker = _aligned_string_bytes(first_value)
    offset = data.find(marker)
    if offset < 0:
        raise ValueError(f"string sequence marker not found: {first_value!r}")
    values: list[str] = []
    for _ in range(count):
        value, offset = _read_aligned_string(data, offset)
        values.append(value)
    return values
