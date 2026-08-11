from __future__ import annotations

import argparse
import hashlib
import json
import re
import sys
from collections import Counter, defaultdict
from datetime import datetime, timezone
from pathlib import Path
from typing import Any, Iterable


PROJECT_DIR = Path(__file__).resolve().parents[1]
GAME_ROOT = PROJECT_DIR.parent
DATA_DIR = GAME_ROOT / "The Message From Deep Space_Data"
LOCAL_PACKAGES = PROJECT_DIR / "tools" / "python-packages"
sys.path.insert(0, str(LOCAL_PACKAGES))

import UnityPy  # noqa: E402
from UnityPy.helpers.TypeTreeGenerator import TypeTreeGenerator  # noqa: E402
from extraction_rules import (  # noqa: E402
    EXPLICIT_COMPONENT_STRING_FIELDS,
    UI_FRAGMENTS,
    UI_TEMPLATES,
    exclusion_reason,
    protect_component_source,
    recover_string_array,
    recover_string_sequence,
)


UNITY_VERSION = "6000.0.73f1"
GAME_VERSION = "0.10"
FORMAT_VERSION = 1
SIGNAL_RE = re.compile(r"\|(-?\d{1,3})")
PLAYER_RE = re.compile(r"(?<![A-Za-z])(?:[Tt]he\s+)?Translator\b")
ENGLISH_WORD_RE = re.compile(r"[A-Za-z]{2,}")
CONTROL_ONLY_RE = re.compile(
    r"\{(?:SPEAKER_[A-Z0-9_]+|PART_\d{3}|SIG_(?:N)?\d{3}|PLAYER_NAME|DYN_\d+)\}|"
    r"\$anim[A-Za-z]\d{1,2}|<[^>]+>"
)
SPEAKERS = {
    0: "AKERS",
    1: "BAUTISTA",
    2: "COLLINS",
    3: "DOPPLER",
    4: "AUTO_LOG",
    5: "PILOT",
    6: "CO_PILOT",
}
ENGINE_STRING_FIELDS = {
    "m_Name",
    "raw",
    "processedRaw",
    "txt",
    "m_text",
}
SYSTEM_FIELD_HINTS = (
    "text",
    "title",
    "message",
    "prompt",
    "description",
    "tooltip",
    "label",
    "format",
    "prefix",
    "suffix",
    "missing",
    "overflow",
    "error",
    "warning",
    "empty",
    "complete",
    "begins",
    "strings",
    "_s",
)
DEVELOPMENT_DIALOGUE_TITLES = {
    "Demo Complete",
    "Journal Entries #25 - Demo Complete",
    "Unicode Parse Test",
    "Debug Test",
    "Test Convo",
    "Test Convo 2",
    "Arm Animations Test",
    "Demo Computer Broke!!",
    "Model Viewer Test",
    "{PLAYER_NAME} Name Replace",
}
PLACEHOLDER_TEXT_EXACT = {
    "new text",
    "wordname",
    "dr. speaker",
    "blah",
    "wibble",
    "message text",
    "panel title",
}
DEVELOPMENT_OBJECT_PATH_PARTS = (
    "Debug/Debug Canvas",
    "DEBUG - Scroll Test",
    "Demo End Canvas",
)
PROGRESS_STRING_FIELDS = (
    "aLogTitle_s",
    "bLogTitle_s",
    "cLogTitle_s",
    "dLogTitle_s",
    "tLogTitle_s",
    "tLogNotEmpty_s",
    "week_s",
    "transmissions_s",
    "signalsEncountered_s",
    "totalTransmissions_s",
    "totalWords_s",
    "wordsNamed_s",
    "transmissionGroups_s",
    "noWordsAdded_s",
)
DISPLAY_VALUE_FIELDS = {
    "title",
    "puzzleGroupName",
    "elementName",
    "universalAbundanceRank",
    "songTitle",
}


def sha256_text(text: str) -> str:
    return hashlib.sha256(text.encode("utf-8")).hexdigest()


def signal_placeholder(value: int) -> str:
    if value < 0:
        return f"{{SIG_N{abs(value):03d}}}"
    return f"{{SIG_{value:03d}}}"


def protect_runtime_tokens(text: str, *, protect_player_name: bool = True) -> str:
    text = SIGNAL_RE.sub(lambda match: signal_placeholder(int(match.group(1))), text)
    return PLAYER_RE.sub("{PLAYER_NAME}", text) if protect_player_name else text


def build_component_dialogue_source(frame: dict[str, Any]) -> tuple[str, list[dict[str, Any]]]:
    speaker_value = int(frame.get("speaker", -1))
    speaker_name = SPEAKERS.get(speaker_value, f"UNKNOWN_{speaker_value}")
    source_parts = [f"{{SPEAKER_{speaker_name}}}"]
    parts_meta: list[dict[str, Any]] = []
    for part_index, part in enumerate(frame.get("dialogueParts", [])):
        original = str(part.get("txt", ""))
        protected = protect_runtime_tokens(original)
        body, leading, trailing = trim_edges(protected)
        source_parts.append(f"{{PART_{part_index:03d}}}{body}")
        parts_meta.append(
            {
                "part_index": part_index,
                "leading_whitespace": leading,
                "trailing_whitespace": trailing,
                "original_text": original,
            }
        )
    return "".join(source_parts), parts_meta


def trim_edges(text: str) -> tuple[str, str, str]:
    leading_len = len(text) - len(text.lstrip())
    trailing_len = len(text) - len(text.rstrip())
    leading = text[:leading_len]
    trailing = text[len(text) - trailing_len :] if trailing_len else ""
    end = len(text) - trailing_len if trailing_len else len(text)
    return text[leading_len:end], leading, trailing


def is_translatable(text: str) -> bool:
    visible = CONTROL_ONLY_RE.sub("", text)
    return bool(ENGLISH_WORD_RE.search(visible))


def is_localizable_dialogue(text: str) -> bool:
    """Include punctuation-only dialogue whose ellipsis changes in Chinese."""

    visible = CONTROL_ONLY_RE.sub("", text).strip()
    return is_translatable(text) or bool(re.fullmatch(r"\.{3,}", visible))


def is_development_or_placeholder(text: str, *, object_path: str, scope: str) -> bool:
    """Reject serialized editor/debug placeholders without hiding real game terminology."""
    visible = CONTROL_ONLY_RE.sub("", text).strip()
    folded = visible.casefold()
    if scope == "resources.assets":
        return True
    if any(part.casefold() in object_path.casefold() for part in DEVELOPMENT_OBJECT_PATH_PARTS):
        return True
    if folded in PLACEHOLDER_TEXT_EXACT:
        return True
    if folded.startswith("lorem ipsum") or folded.startswith("loremipsum"):
        return True
    if folded.startswith("sample textsample text"):
        return True
    if re.match(r"^(?:asef|asdf)(?:\s|$)", folded):
        return True
    if re.match(r"^(?:title\s*){2,}$", folded):
        return True
    return False


def deterministic_index(stable_key: str, used: dict[int, str]) -> int:
    index = int(hashlib.sha256(stable_key.encode("utf-8")).hexdigest()[:8], 16) & 0x7FFFFFFF
    if index == 0:
        index = 1
    while index in used and used[index] != stable_key:
        index = 1 if index == 0x7FFFFFFF else index + 1
    used[index] = stable_key
    return index


def cache_item(
    *,
    stable_key: str,
    source_text: str,
    kind: str,
    game_extra: dict[str, Any],
    used_indexes: dict[int, str],
    excluded: bool = False,
) -> dict[str, Any]:
    source_hash = sha256_text(source_text)
    game = {
        "kind": kind,
        "stable_key": stable_key,
        "source_sha256": source_hash,
        **game_extra,
    }
    return {
        "text_index": deterministic_index(stable_key, used_indexes),
        "translation_status": 7 if excluded else 0,
        "model": "",
        "source_text": source_text,
        "translated_text": "",
        "text_to_detect": source_text,
        "lang_code": None,
        "extra": {"game": game},
    }


def cache_file(storage_path: str, items: list[dict[str, Any]]) -> dict[str, Any]:
    return {
        "storage_path": storage_path,
        "encoding": "utf-8",
        "file_project_type": "Mtool",
        "line_ending": "\n",
        "items": items,
        "language_stats": [],
        "lc_language_stats": [],
        "extra": {"game_category": storage_path},
    }


def iter_cache_items(project: dict[str, Any]) -> Iterable[dict[str, Any]]:
    for file_data in project.get("files", {}).values():
        yield from file_data.get("items", [])


def migrate_previous(project: dict[str, Any], previous_path: Path) -> dict[str, int]:
    result = {"preserved": 0, "changed": 0, "new": 0, "removed": 0}
    if not previous_path.exists():
        result["new"] = sum(1 for _ in iter_cache_items(project))
        return result
    previous = json.loads(previous_path.read_text(encoding="utf-8"))
    previous_by_key = {
        item.get("extra", {}).get("game", {}).get("stable_key"): item
        for item in iter_cache_items(previous)
        if item.get("extra", {}).get("game", {}).get("stable_key")
    }
    seen: set[str] = set()
    for item in iter_cache_items(project):
        game = item["extra"]["game"]
        key = game["stable_key"]
        seen.add(key)
        old = previous_by_key.get(key)
        if old is None:
            result["new"] += 1
            continue
        old_game = old.get("extra", {}).get("game", {})
        if old_game.get("source_sha256") != game["source_sha256"]:
            result["changed"] += 1
            continue
        if old.get("translation_status") in (1, 2) and old.get("translated_text"):
            item["translation_status"] = old["translation_status"]
            item["translated_text"] = old["translated_text"]
            item["model"] = old.get("model", "")
        result["preserved"] += 1
    result["removed"] = len(set(previous_by_key) - seen)
    return result


def make_generator() -> TypeTreeGenerator:
    generator = TypeTreeGenerator(UNITY_VERSION)
    generator.load_local_game(str(GAME_ROOT))
    return generator


def extract_dialogue(
    generator: TypeTreeGenerator, used_indexes: dict[int, str]
) -> tuple[dict[str, list[dict[str, Any]]], dict[str, Any]]:
    asset_path = DATA_DIR / "sharedassets0.assets"
    env = UnityPy.load(str(asset_path))
    env.typetree_generator = generator
    chunks: list[tuple[int, int, dict[str, Any]]] = []
    parse_failures: list[dict[str, Any]] = []
    for obj in env.objects:
        if obj.type.name != "MonoBehaviour":
            continue
        try:
            tree = obj.parse_as_dict()
        except Exception as exc:
            parse_failures.append({"path_id": obj.path_id, "error": repr(exc)})
            continue
        if (
            isinstance(tree.get("uniqueID"), int)
            and isinstance(tree.get("frames"), list)
            and "processedRaw" in tree
            and "logName" in tree
        ):
            chunks.append((int(tree["uniqueID"]), obj.path_id, tree))

    groups: dict[str, list[dict[str, Any]]] = defaultdict(list)
    signal_counter: Counter[str] = Counter()
    speaker_counter: Counter[str] = Counter()
    total_parts = 0
    for chunk_id, path_id, tree in sorted(chunks):
        chunk_name = str(tree.get("m_Name", ""))
        title_original = str(tree.get("logName", ""))
        title_source = protect_runtime_tokens(title_original)
        development_chunk = title_original.strip() in DEVELOPMENT_DIALOGUE_TITLES
        title_key = f"dialogue:{chunk_id}/title"
        if title_source.strip():
            groups["dialogue.titles"].append(
                cache_item(
                    stable_key=title_key,
                    source_text=title_source,
                    kind="dialogue_title",
                    game_extra={
                        "chunk_id": chunk_id,
                        "chunk_name": chunk_name,
                        "asset": asset_path.name,
                        "asset_path_id": path_id,
                        "original_text": title_original,
                    },
                    used_indexes=used_indexes,
                    excluded=development_chunk or not is_translatable(title_source),
                )
            )

        frames = tree.get("frames", [])
        for frame_index, frame in enumerate(frames):
            speaker_value = int(frame.get("speaker", -1))
            speaker_name = SPEAKERS.get(speaker_value, f"UNKNOWN_{speaker_value}")
            speaker_counter[speaker_name] += 1
            source_parts: list[str] = [f"{{SPEAKER_{speaker_name}}}"]
            parts_meta: list[dict[str, Any]] = []
            for part_index, part in enumerate(frame.get("dialogueParts", [])):
                total_parts += 1
                original = str(part.get("txt", ""))
                for match in SIGNAL_RE.finditer(original):
                    signal_counter[match.group(1)] += 1
                protected = protect_runtime_tokens(original)
                body, leading, trailing = trim_edges(protected)
                source_parts.append(f"{{PART_{part_index:03d}}}{body}")
                parts_meta.append(
                    {
                        "part_index": part_index,
                        "leading_whitespace": leading,
                        "trailing_whitespace": trailing,
                        "original_text": original,
                    }
                )
            source_text = "".join(source_parts)
            stable_key = f"dialogue:{chunk_id}/frame:{frame_index}"
            # Development prose stays excluded, but punctuation-only frames are
            # harmless shared dialogue assets and still need Chinese ellipses.
            excluded = not is_localizable_dialogue(source_text) or (
                development_chunk and is_translatable(source_text)
            )
            group = "dialogue.excluded" if excluded else "dialogue.frames"
            groups[group].append(
                cache_item(
                    stable_key=stable_key,
                    source_text=source_text,
                    kind="dialogue_frame",
                    game_extra={
                        "chunk_id": chunk_id,
                        "chunk_name": chunk_name,
                        "frame_index": frame_index,
                        "speaker": speaker_name,
                        "speaker_value": speaker_value,
                        "part_count": len(parts_meta),
                        "parts": parts_meta,
                        "asset": asset_path.name,
                        "asset_path_id": path_id,
                    },
                    used_indexes=used_indexes,
                    excluded=excluded,
                )
            )

    report = {
        "asset": str(asset_path.relative_to(GAME_ROOT)),
        "chunks": len(chunks),
        "unique_chunk_ids": len({chunk_id for chunk_id, _, _ in chunks}),
        "frames": sum(len(tree.get("frames", [])) for _, _, tree in chunks),
        "parts": total_parts,
        "translatable_frames": len(groups["dialogue.frames"]),
        "excluded_frames": len(groups["dialogue.excluded"]),
        "titles": len(groups["dialogue.titles"]),
        "speaker_counts": dict(sorted(speaker_counter.items())),
        "signal_embed_count": sum(signal_counter.values()),
        "signal_ids": dict(sorted(signal_counter.items(), key=lambda pair: int(pair[0]))),
        "parse_failures": parse_failures,
    }
    return groups, report


def ptr_path_id(value: Any) -> int:
    if isinstance(value, dict):
        if "m_PathID" in value:
            return int(value.get("m_PathID", 0))
        if "component" in value:
            return ptr_path_id(value["component"])
    return 0


def scene_names() -> list[str]:
    data = (DATA_DIR / "globalgamemanagers").read_bytes()
    paths = re.findall(rb"Assets/[^\x00]{1,200}\.unity", data)
    return [Path(path.decode("utf-8")).stem for path in paths]


def extract_ui(
    generator: TypeTreeGenerator, used_indexes: dict[int, str]
) -> tuple[dict[str, list[dict[str, Any]]], dict[str, Any]]:
    scenes = scene_names()
    asset_paths = sorted(DATA_DIR.glob("level*")) + sorted(DATA_DIR.glob("*.assets"))
    groups: dict[str, list[dict[str, Any]]] = defaultdict(list)
    parse_failures: list[dict[str, Any]] = []
    parse_recoveries: list[dict[str, Any]] = []
    localization_recoveries: list[dict[str, Any]] = []
    candidates = 0
    excluded = 0
    scene_counts: Counter[str] = Counter()

    for asset_path in asset_paths:
        env = UnityPy.load(str(asset_path))
        env.typetree_generator = generator
        parsed: dict[int, tuple[str, dict[str, Any]]] = {}
        raw_objects: dict[int, bytes] = {}
        for obj in env.objects:
            if obj.type.name not in {"GameObject", "Transform", "RectTransform", "MonoBehaviour"}:
                continue
            if obj.type.name == "MonoBehaviour":
                raw_objects[obj.path_id] = obj.get_raw_data()
            try:
                parsed[obj.path_id] = (obj.type.name, obj.parse_as_dict())
            except Exception as exc:
                if obj.type.name == "MonoBehaviour":
                    try:
                        parsed[obj.path_id] = (
                            obj.type.name,
                            obj.parse_as_dict(check_read=False),
                        )
                        parse_recoveries.append(
                            {
                                "asset": asset_path.name,
                                "path_id": obj.path_id,
                                "strict_error": repr(exc),
                                "method": "typetree_check_read_false",
                            }
                        )
                        continue
                    except Exception as loose_exc:
                        parse_failures.append(
                            {
                                "asset": asset_path.name,
                                "path_id": obj.path_id,
                                "error": repr(loose_exc),
                                "strict_error": repr(exc),
                            }
                        )
                else:
                    parse_failures.append(
                        {"asset": asset_path.name, "path_id": obj.path_id, "error": repr(exc)}
                    )

        game_objects = {
            path_id: tree
            for path_id, (type_name, tree) in parsed.items()
            if type_name == "GameObject"
        }
        transforms = {
            path_id: tree
            for path_id, (type_name, tree) in parsed.items()
            if type_name in {"Transform", "RectTransform"}
        }
        transform_to_go = {
            transform_id: ptr_path_id(tree.get("m_GameObject"))
            for transform_id, tree in transforms.items()
        }
        go_to_transform: dict[int, int] = {}
        component_indexes: dict[int, int] = {}
        component_game_objects: dict[int, int] = {}
        for go_id, go in game_objects.items():
            for component_index, component in enumerate(go.get("m_Component", [])):
                component_id = ptr_path_id(component)
                component_indexes[component_id] = component_index
                component_game_objects[component_id] = go_id
                if component_id in transforms:
                    go_to_transform[go_id] = component_id

        sibling_indexes: dict[int, int] = {}
        for transform in transforms.values():
            for child_index, child in enumerate(transform.get("m_Children", [])):
                sibling_indexes[ptr_path_id(child)] = child_index

        path_cache: dict[int, str] = {}

        def hierarchy_path(go_id: int, active: set[int] | None = None) -> str:
            if go_id in path_cache:
                return path_cache[go_id]
            if active is None:
                active = set()
            if go_id in active:
                return f"<cycle:{go_id}>"
            active.add(go_id)
            go = game_objects.get(go_id, {})
            name = str(go.get("m_Name", f"<GameObject:{go_id}>"))
            transform_id = go_to_transform.get(go_id, 0)
            transform = transforms.get(transform_id, {})
            father_id = ptr_path_id(transform.get("m_Father"))
            segment = name
            if father_id:
                segment += f"[{sibling_indexes.get(transform_id, 0)}]"
                parent_go = transform_to_go.get(father_id, 0)
                result = f"{hierarchy_path(parent_go, active)}/{segment}"
            else:
                result = segment
            active.remove(go_id)
            path_cache[go_id] = result
            return result

        if asset_path.name.startswith("level") and asset_path.name[5:].isdigit():
            build_index = int(asset_path.name[5:])
            scope = scenes[build_index] if build_index < len(scenes) else asset_path.name
            category = f"ui.{scope}"
        else:
            scope = asset_path.name
            category = f"ui.assets.{asset_path.stem}"

        for path_id, (type_name, tree) in parsed.items():
            if type_name != "MonoBehaviour" or not isinstance(tree.get("m_text"), str):
                continue
            candidates += 1
            original = tree["m_text"]
            source = protect_runtime_tokens(original, protect_player_name=False)
            go_id = ptr_path_id(tree.get("m_GameObject"))
            object_path = hierarchy_path(go_id) if go_id else "<no-game-object>"
            component_index = component_indexes.get(path_id, -1)
            stable_key = f"ui:{scope}:{object_path}:component:{component_index}"
            should_exclude = not is_translatable(source) or is_development_or_placeholder(
                source, object_path=object_path, scope=scope
            )
            if should_exclude:
                excluded += 1
            else:
                scene_counts[scope] += 1
            group = "ui.excluded" if should_exclude else category
            groups[group].append(
                cache_item(
                    stable_key=stable_key,
                    source_text=source,
                    kind="ui_text",
                    game_extra={
                        "asset": asset_path.name,
                        "scope": scope,
                        "object_path": object_path,
                        "component_index": component_index,
                        "asset_path_id": path_id,
                        "game_object_path_id": go_id,
                        "original_text": original,
                    },
                    used_indexes=used_indexes,
                    excluded=should_exclude,
                )
            )

        def add_component_string(
            path_id: int,
            field_path: str,
            original: str,
            *,
            recovery: str | None = None,
        ) -> None:
            go_id = component_game_objects.get(path_id, 0)
            if not go_id:
                return
            object_path = hierarchy_path(go_id)
            component_index = component_indexes.get(path_id, -1)
            protected, token_metadata = protect_component_source(original, field_path)
            source = protect_runtime_tokens(protected, protect_player_name=False)
            reason = exclusion_reason(field_path, original)
            if not is_translatable(source) and reason is None:
                return
            should_exclude = reason is not None or is_development_or_placeholder(
                source, object_path=object_path, scope=scope
            )
            stable_key = (
                f"system:{scope}:{object_path}:component:{component_index}:field:{field_path}"
            )
            game_extra = {
                "asset": asset_path.name,
                "scope": scope,
                "object_path": object_path,
                "component_index": component_index,
                "asset_path_id": path_id,
                "game_object_path_id": go_id,
                "field_path": field_path,
                "original_text": original,
                **token_metadata,
            }
            if reason:
                game_extra["exclude_reason"] = reason
            if recovery:
                game_extra["recovery"] = recovery
            group = "system.excluded" if should_exclude else "system.messages"
            groups[group].append(
                cache_item(
                    stable_key=stable_key,
                    source_text=source,
                    kind="component_string",
                    game_extra=game_extra,
                    used_indexes=used_indexes,
                    excluded=should_exclude,
                )
            )

        # Extract flat serialized user-facing templates such as "MISSION TIME: ".
        for path_id, (type_name, tree) in parsed.items():
            if type_name != "MonoBehaviour":
                continue
            for field_name, value in tree.items():
                lower_name = field_name.lower()
                if field_name in ENGINE_STRING_FIELDS or (
                    field_name not in EXPLICIT_COMPONENT_STRING_FIELDS
                    and not any(hint in lower_name for hint in SYSTEM_FIELD_HINTS)
                ):
                    continue
                if isinstance(value, str):
                    add_component_string(path_id, field_name, value)
                elif isinstance(value, list) and all(isinstance(element, str) for element in value):
                    for index, element in enumerate(value):
                        add_component_string(path_id, f"{field_name}[{index}]", element)

        def add_component_dialogue_frame(
            path_id: int, field_path: str, frame: dict[str, Any]
        ) -> None:
            go_id = component_game_objects.get(path_id, 0)
            if not go_id:
                return
            object_path = hierarchy_path(go_id)
            component_index = component_indexes.get(path_id, -1)
            source, parts = build_component_dialogue_source(frame)
            stable_key = (
                f"system:{scope}:{object_path}:component:{component_index}:field:{field_path}"
            )
            should_exclude = not is_translatable(source)
            group = "system.excluded" if should_exclude else "system.messages"
            groups[group].append(
                cache_item(
                    stable_key=stable_key,
                    source_text=source,
                    kind="component_dialogue_frame",
                    game_extra={
                        "asset": asset_path.name,
                        "scope": scope,
                        "object_path": object_path,
                        "component_index": component_index,
                        "asset_path_id": path_id,
                        "game_object_path_id": go_id,
                        "field_path": field_path,
                        "speaker": SPEAKERS.get(
                            int(frame.get("speaker", -1)),
                            f"UNKNOWN_{int(frame.get('speaker', -1))}",
                        ),
                        "speaker_value": int(frame.get("speaker", -1)),
                        "part_count": len(parts),
                        "parts": parts,
                        "protect_player_name": True,
                    },
                    used_indexes=used_indexes,
                    excluded=should_exclude,
                )
            )

        # NameTranslator stores its reserved-name responses as nested DialogueFrame values.
        for path_id, (type_name, tree) in parsed.items():
            frames = tree.get("nameIsTakenPrompts") if type_name == "MonoBehaviour" else None
            if not isinstance(frames, list):
                continue
            for frame_index, frame in enumerate(frames):
                if not isinstance(frame, dict):
                    continue
                add_component_dialogue_frame(
                    path_id, f"nameIsTakenPrompts[{frame_index}]", frame
                )

        # Other nested, player-facing structures used by live UI.
        for path_id, (type_name, tree) in parsed.items():
            if type_name != "MonoBehaviour":
                continue
            progress_data = tree.get("progressLogData")
            if isinstance(progress_data, dict):
                for field_name in ("actSection", "nextActName", "actName"):
                    value = progress_data.get(field_name)
                    if isinstance(value, str):
                        add_component_string(
                            path_id, f"progressLogData.{field_name}", value
                        )
            hypotheses = tree.get("hypos")
            if isinstance(hypotheses, list):
                for index, hypothesis in enumerate(hypotheses):
                    if not isinstance(hypothesis, dict):
                        continue
                    for field_name in ("aGuess", "bGuess", "cGuess"):
                        value = hypothesis.get(field_name)
                        if isinstance(value, str):
                            add_component_string(
                                path_id, f"hypos[{index}].{field_name}", value
                            )
            for profile_name in ("pilotProf", "qopilotProf"):
                profile = tree.get(profile_name)
                if isinstance(profile, dict) and isinstance(profile.get("speakerName"), str):
                    add_component_string(
                        path_id, f"{profile_name}.speakerName", profile["speakerName"]
                    )
            for field_name in ("autoLogStartFrame", "autoLogEndFrame", "inactivityExitFrame"):
                frame = tree.get(field_name)
                if isinstance(frame, dict) and isinstance(frame.get("dialogueParts"), list):
                    add_component_dialogue_frame(path_id, field_name, frame)

        # AchievementData names/descriptions are display data. AchievementManager's
        # achievementNames array is deliberately not exported because it is also used
        # as the Steam achievement ID and save-file key.
        for path_id, (type_name, tree) in parsed.items():
            achievements = tree.get("achievements") if type_name == "MonoBehaviour" else None
            if not isinstance(achievements, list):
                continue
            for index, achievement in enumerate(achievements):
                if not isinstance(achievement, dict):
                    continue
                for display_field, kind in (
                    ("name", "achievement_name"),
                    ("description", "achievement_description"),
                ):
                    original = str(achievement.get(display_field, ""))
                    source = protect_runtime_tokens(original, protect_player_name=False)
                    if not is_translatable(source):
                        continue
                    stable_key = f"achievement:{index}:{display_field}"
                    groups["ui.achievements"].append(
                        cache_item(
                            stable_key=stable_key,
                            source_text=source,
                            kind=kind,
                            game_extra={
                                "asset": asset_path.name,
                                "asset_path_id": path_id,
                                "achievement_index": index,
                                "display_field": display_field,
                                "original_text": original,
                                "protect_player_name": False,
                            },
                            used_indexes=used_indexes,
                        )
                    )

        # ScriptableObject display values are assigned to TMP labels at runtime and
        # have no GameObject hierarchy. Export only fields proven to be presentation
        # data; puzzle conditions, answer keys and element symbols remain untouched.
        for path_id, (type_name, tree) in parsed.items():
            if type_name != "MonoBehaviour" or component_game_objects.get(path_id, 0):
                continue

            def add_display_value(field_path: str, original: str) -> None:
                source = protect_runtime_tokens(original, protect_player_name=False)
                if not is_translatable(source):
                    return
                groups["ui.display-data"].append(
                    cache_item(
                        stable_key=f"display:{asset_path.name}:{path_id}:field:{field_path}",
                        source_text=source,
                        kind="display_value",
                        game_extra={
                            "asset": asset_path.name,
                            "asset_path_id": path_id,
                            "field_path": field_path,
                            "object_name": str(tree.get("m_Name", "")),
                            "original_text": original,
                            "protect_player_name": False,
                        },
                        used_indexes=used_indexes,
                    )
                )

            for field_name in DISPLAY_VALUE_FIELDS:
                value = tree.get(field_name)
                if isinstance(value, str):
                    add_display_value(field_name, value)
            isotopes = tree.get("isotopes")
            if isinstance(isotopes, list):
                for index, isotope in enumerate(isotopes):
                    if isinstance(isotope, dict) and isinstance(isotope.get("unit"), str):
                        add_display_value(f"isotopes[{index}].unit", isotope["unit"])

        # Unity's generated type trees are wrong for two string arrays in v0.10.
        if asset_path.name == "level0" and 23192 in raw_objects:
            for index, original in enumerate(
                recover_string_array(raw_objects[23192], "Act 0", expected_count=13)
            ):
                add_component_string(
                    23192, f"actSections[{index}]", original, recovery="raw_string_array"
                )
            localization_recoveries.append(
                {"asset": asset_path.name, "path_id": 23192, "fields": "actSections[13]"}
            )

        # ProgressLog cannot be parsed by the generated type tree, but its localized
        # fields form stable aligned-string sequences in the serialized object.
        if asset_path.name == "level0" and 23218 in raw_objects:
            raw = raw_objects[23218]
            direct_values = recover_string_sequence(raw, "Alan's Journal: ", 14)
            for field_name, original in zip(PROGRESS_STRING_FIELDS, direct_values, strict=True):
                add_component_string(23218, field_name, original, recovery="raw_string_sequence")
            for index, original in enumerate(
                recover_string_array(raw, "Zero", expected_count=61)
            ):
                add_component_string(
                    23218, f"numberStrings[{index}]", original, recovery="raw_string_array"
                )
            add_component_string(
                23218,
                "actComplete_s",
                recover_string_sequence(raw, " - Complete!!", 1)[0],
                recovery="raw_aligned_string",
            )
            add_component_string(
                23218,
                "nextActStart_s",
                recover_string_sequence(raw, " Begins...", 1)[0],
                recovery="raw_aligned_string",
            )
            localization_recoveries.append(
                {
                    "asset": asset_path.name,
                    "path_id": 23218,
                    "fields": "ProgressLog strings and numberStrings[61]",
                }
            )

    # Text composed directly in game code cannot be discovered from Unity objects.
    for template in UI_TEMPLATES:
        source = str(template["source_text"])
        template_id = str(template["template_id"])
        template_extra = {
            "template_id": template_id,
            "original_text": source,
            "protect_player_name": False,
        }
        if template.get("translate_display_values", False):
            template_extra["translate_display_values"] = True
        groups["ui.templates"].append(
            cache_item(
                stable_key=f"ui-template:{template_id}",
                source_text=source,
                kind="ui_template",
                game_extra=template_extra,
                used_indexes=used_indexes,
            )
        )

    for fragment in UI_FRAGMENTS:
        source = str(fragment["source_text"])
        fragment_id = str(fragment["fragment_id"])
        groups["ui.fragments"].append(
            cache_item(
                stable_key=f"ui-fragment:{fragment_id}",
                source_text=source,
                kind="ui_fragment",
                game_extra={
                    "fragment_id": fragment_id,
                    "original_text": source,
                    "protect_player_name": False,
                },
                used_indexes=used_indexes,
            )
        )

    recovered_keys = {
        (str(item["asset"]), int(item["path_id"])) for item in localization_recoveries
    }
    ignored_parse_failures = []
    unresolved_parse_failures = []
    for failure in parse_failures:
        key = (str(failure["asset"]), int(failure["path_id"]))
        if key in recovered_keys:
            continue
        if key == ("globalgamemanagers.assets", 2742):
            ignored_parse_failures.append(
                {
                    **failure,
                    "reason": "UniversalRenderPipelineGlobalSettings contains engine settings, not game UI",
                }
            )
            continue
        unresolved_parse_failures.append(failure)

    report = {
        "build_scenes": scenes,
        "assets_scanned": [path.name for path in asset_paths],
        "tmp_text_candidates": candidates,
        "tmp_text_translatable": candidates - excluded,
        "tmp_text_excluded": excluded,
        "system_messages": len(groups["system.messages"]),
        "system_excluded": len(groups["system.excluded"]),
        "scene_counts": dict(sorted(scene_counts.items())),
        "parse_failures": unresolved_parse_failures,
        "ignored_parse_failures": ignored_parse_failures,
        "parse_recoveries": parse_recoveries,
        "localization_recoveries": localization_recoveries,
        "ui_templates": len(groups["ui.templates"]),
        "ui_fragments": len(groups["ui.fragments"]),
        "achievement_display_strings": len(groups["ui.achievements"]),
        "display_values": len(groups["ui.display-data"]),
    }
    return groups, report


def write_json(path: Path, value: Any) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser(description="Extract game localization text into an AiNiee cache")
    parser.add_argument("--cache", type=Path, default=PROJECT_DIR / "work" / "cache.json")
    parser.add_argument(
        "--report", type=Path, default=PROJECT_DIR / "build" / "extraction-report.json"
    )
    args = parser.parse_args()

    generator = make_generator()
    used_indexes: dict[int, str] = {}
    dialogue_groups, dialogue_report = extract_dialogue(generator, used_indexes)
    ui_groups, ui_report = extract_ui(generator, used_indexes)
    groups: dict[str, list[dict[str, Any]]] = defaultdict(list)
    for source_groups in (dialogue_groups, ui_groups):
        for group_name, items in source_groups.items():
            groups[group_name].extend(items)

    files = {
        group_name: cache_file(group_name, sorted(items, key=lambda item: item["extra"]["game"]["stable_key"]))
        for group_name, items in sorted(groups.items())
        if items
    }
    project = {
        "project_id": "the-message-from-deep-space-zh-cn",
        "project_type": "Mtool",
        "project_name": "The Message from Deep Space 简体中文",
        "project_create_time": datetime.now(timezone.utc).isoformat(),
        "input_path": DATA_DIR.relative_to(PROJECT_DIR.parent).as_posix(),
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
        "files": files,
        "detected_encoding": "utf-8",
        "detected_line_ending": "\n",
        "extra": {
            "format_version": FORMAT_VERSION,
            "game_version": GAME_VERSION,
            "unity_version": UNITY_VERSION,
            "source_asset_sha256": hashlib.sha256(
                (DATA_DIR / "sharedassets0.assets").read_bytes()
            ).hexdigest(),
        },
    }
    migration = migrate_previous(project, args.cache)
    write_json(args.cache, project)

    status_counts = Counter(item["translation_status"] for item in iter_cache_items(project))
    report = {
        "format_version": FORMAT_VERSION,
        "game_version": GAME_VERSION,
        "unity_version": UNITY_VERSION,
        "generated_utc": datetime.now(timezone.utc).isoformat(),
        "dialogue": dialogue_report,
        "ui": ui_report,
        "cache_files": {name: len(data["items"]) for name, data in files.items()},
        "total_items": sum(len(data["items"]) for data in files.values()),
        "status_counts": {str(key): value for key, value in sorted(status_counts.items())},
        "migration": migration,
        "invariants": {
            "signal_arrays_modified": False,
            "puzzle_answers_exported": False,
            "user_dictionary_exported": False,
        },
    }
    write_json(args.report, report)
    print(f"Wrote {args.cache}")
    print(f"Wrote {args.report}")
    print(json.dumps({"cache_files": report["cache_files"], "migration": migration}, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
