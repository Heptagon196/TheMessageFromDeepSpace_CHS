from __future__ import annotations

import argparse
import hashlib
import json
import re
from collections import Counter
from pathlib import Path
from typing import Any, Iterable

from dictionary_trigger_conflicts import validate_no_conflicts
from translation_text_checks import (
    validate_chinese_quotes,
    validate_dialogue_ellipsis,
    validate_duplicate_punctuation,
)
from extraction_rules import DISPLAY_VALUE_UI_PATHS, UI_TEMPLATES


PROJECT_DIR = Path(__file__).resolve().parents[1]
DEFAULT_MANUAL_OVERRIDES = PROJECT_DIR / "work" / "manual_translation_overrides.json"
FORMAT_VERSION = 1
GAME_VERSION = "0.10"
TOKEN_PATTERNS = {
    "speaker": re.compile(r"\{SPEAKER_[A-Z0-9_]+\}"),
    "part": re.compile(r"\{PART_\d{3}\}"),
    "signal": re.compile(r"\{SIG_(?:N)?\d{3}\}"),
    "player": re.compile(r"\{PLAYER_NAME\}"),
    "dynamic": re.compile(r"\{DYN_\d+\}"),
    # The game uses current tags such as $animD19 and $animM6, bare model
    # tags such as $animM, plus a few legacy numeric-only tags ($anim06).
    "animation": re.compile(r"\$anim(?:[A-Za-z]\d{0,2}|\d{1,2})"),
    "format": re.compile(r"\{\d+\}|%(?:\d+\$)?[sdif]"),
    "tmp_tag": re.compile(r"</?[A-Za-z][^>]*>"),
}
TMP_SIZE_OPEN = re.compile(r"<size=(?:\d+(?:\.\d+)?%?)>", re.IGNORECASE)
TMP_SIZE_WRAPPER = re.compile(
    r"^<size=(\d+(?:\.\d+)?)%>(.*)</size>$", re.IGNORECASE | re.DOTALL
)
JOURNAL_STRUCTURE_TOKEN = re.compile(r"\{(?:SPEAKER_[^}]+|PART_\d{3})\}")
JOURNAL_SIGNAL_TOKEN = re.compile(r"\{SIG_(?:N)?\d{3}\}")
SOURCE_CELSIUS = re.compile(r"\bcelsius\b", re.IGNORECASE)
SOURCE_FAHRENHEIT = re.compile(r"\bfahrenheit\b", re.IGNORECASE)
SOURCE_DEGREES = re.compile(r"\bdegrees?\b", re.IGNORECASE)
TRANSLATED_CELSIUS = re.compile(r"(?:摄氏度|℃|°\s*[Cc])")
TRANSLATED_FAHRENHEIT = re.compile(r"(?:华氏度|℉|°\s*[Ff])")
TRANSLATED_ANGLE = re.compile(r"(?:度|°)")
TRANSLATED_ACADEMIC_DEGREE = re.compile(r"学位")
TRANSLATED_ABSTRACT_DEGREE = re.compile(r"程度")

# "degree" is polysemous, and English sometimes omits a temperature scale.
# Classify every occurrence by stable key instead of guessing from one isolated
# sentence. Frame 32 immediately says "500 Celsius", so Akers's "a thousand
# degrees" in frame 33 is the approximate Fahrenheit conversion.
DEGREE_CONTEXTS = {
    "dialogue:1126/frame:5": "academic",
    "dialogue:16/frame:33": "fahrenheit",
    "dialogue:637/frame:1": "academic",
    "dialogue:878/frame:0": "abstract",
}

DISPLAY_VALUE_TEMPLATE_IDS = {
    str(template["template_id"])
    for template in UI_TEMPLATES
    if template.get("translate_display_values", False)
}


def should_translate_display_values(game: dict[str, Any]) -> bool:
    """Allow semantic display-value substitution only in explicitly known UI."""
    kind = str(game.get("kind", ""))
    if kind == "ui_template":
        return str(game.get("template_id", "")) in DISPLAY_VALUE_TEMPLATE_IDS
    if kind == "ui_text":
        return str(game.get("object_path", "")) in DISPLAY_VALUE_UI_PATHS
    return False


def sha256_bytes(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def sha256_text(text: str) -> str:
    return sha256_bytes(text.encode("utf-8"))


def write_json(path: Path, value: Any) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def iter_items(cache: dict[str, Any]) -> Iterable[dict[str, Any]]:
    for file_data in cache.get("files", {}).values():
        yield from file_data.get("items", [])


def apply_manual_overrides(cache: dict[str, Any], path: Path) -> int:
    """Overlay small, reviewable manual corrections onto an extracted cache."""
    if not path.is_file():
        return 0
    payload = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(payload, dict):
        raise ValueError(f"人工译文修订文件格式无效：{path}")
    entries = payload.get("entries")
    if payload.get("format_version") != 1 or not isinstance(entries, list):
        raise ValueError(f"人工译文修订文件格式无效：{path}")

    cache_by_index: dict[int, dict[str, Any]] = {}
    for item in iter_items(cache):
        text_index = item.get("text_index")
        if text_index in cache_by_index:
            raise ValueError(f"缓存包含重复 text_index：{text_index}")
        cache_by_index[text_index] = item

    seen: set[int] = set()
    for position, override in enumerate(entries, start=1):
        if not isinstance(override, dict):
            raise ValueError(f"人工译文修订第 {position} 条不是对象。")
        text_index = override.get("text_index")
        if not isinstance(text_index, int) or text_index in seen:
            raise ValueError(f"人工译文修订第 {position} 条 text_index 无效或重复。")
        seen.add(text_index)
        item = cache_by_index.get(text_index)
        if item is None:
            raise ValueError(f"人工译文修订引用了不存在的 text_index：{text_index}")
        game = item.get("extra", {}).get("game", {})
        expected_hash = game.get("source_sha256", "")
        if override.get("source_sha256") != expected_hash:
            raise ValueError(
                f"人工译文修订 {text_index} 的原文哈希已变化；请重新审阅后更新。"
            )
        translated = override.get("translated_text")
        if not isinstance(translated, str):
            raise ValueError(f"人工译文修订 {text_index} 缺少 translated_text。")
        item["translated_text"] = translated
        item["translation_status"] = 1
    return len(entries)


def validate_temperature_units(item: dict[str, Any]) -> list[str]:
    errors: list[str] = []
    source = item.get("source_text") or ""
    translated = item.get("translated_text") or ""
    stable_key = item.get("extra", {}).get("game", {}).get("stable_key", "")
    has_celsius = SOURCE_CELSIUS.search(source) is not None
    has_fahrenheit = SOURCE_FAHRENHEIT.search(source) is not None

    if has_celsius and not TRANSLATED_CELSIUS.search(translated):
        errors.append("原文 Celsius 必须明确译为摄氏度（或 ℃/°C）")
    if has_fahrenheit and not TRANSLATED_FAHRENHEIT.search(translated):
        errors.append("原文 Fahrenheit 必须明确译为华氏度（或 ℉/°F）")

    # Explicit Celsius/Fahrenheit already classifies the scale. Every other
    # degree/degrees is ambiguous and must first be reviewed and registered.
    if SOURCE_DEGREES.search(source) and not (has_celsius or has_fahrenheit):
        expected = DEGREE_CONTEXTS.get(stable_key)
        if expected is None:
            errors.append(
                "degree/degrees 语境尚未分类；请按 stable_key 标记为 celsius、"
                "fahrenheit、angle、academic 或 abstract"
            )
        elif expected == "celsius" and not TRANSLATED_CELSIUS.search(translated):
            errors.append("该语境中的 degree/degrees 指摄氏度，译文必须明确温标")
        elif expected == "fahrenheit" and not TRANSLATED_FAHRENHEIT.search(translated):
            errors.append("该语境中的 degree/degrees 指华氏度，译文必须明确温标")
        elif expected == "angle" and not TRANSLATED_ANGLE.search(translated):
            errors.append("该语境中的 degree/degrees 指角度，译文必须保留角度单位")
        elif expected == "academic" and not TRANSLATED_ACADEMIC_DEGREE.search(translated):
            errors.append("该语境中的 degree 指学位，译文必须保留学位含义")
        elif expected == "abstract" and not TRANSLATED_ABSTRACT_DEGREE.search(translated):
            errors.append("该语境中的 degree 指程度，译文必须保留程度含义")
        elif expected not in {"celsius", "fahrenheit", "angle", "academic", "abstract"}:
            errors.append(f"未知 degree/degrees 语境分类: {expected!r}")
    return errors


def journal_visible_weight(text: str) -> float:
    """Estimate CJK journal occupancy while ignoring structural/TMP markup."""
    text = TOKEN_PATTERNS["tmp_tag"].sub("", text)
    text = JOURNAL_STRUCTURE_TOKEN.sub("", text)
    # Runtime values are longer than their structural tokens; budget a
    # conservative four Han-character cells for both signals and player name.
    text = JOURNAL_SIGNAL_TOKEN.sub("词语词语", text)
    text = TOKEN_PATTERNS["player"].sub("玩家名字", text)
    return sum(
        1.0 if ord(char) > 127 else (0.35 if char.isspace() else 0.58)
        for char in text
    )


def recommended_journal_scale(item: dict[str, Any]) -> int:
    """Return the largest safe size tier for a ProgressLog journal body."""
    game = item.get("extra", {}).get("game", {})
    if not str(game.get("chunk_name", "")).startswith("Journal Entries #"):
        return 100
    weight = journal_visible_weight(item.get("translated_text") or "")
    speaker = str(game.get("speaker", ""))
    if speaker == "AKERS":  # the source component is 84 px
        tiers = ((120, 75), (100, 80), (84, 85), (75, 90))
    elif speaker == "COLLINS":  # 72 px, but Collins has the longest prose
        tiers = ((160, 75), (135, 80), (105, 85), (90, 90))
    else:  # Bautista and Doppler are 72 px
        tiers = ((150, 75), (130, 80), (120, 85), (90, 90))
    return next((scale for limit, scale in tiers if weight > limit), 100)


def applied_journal_scale(translated: str) -> int:
    """Return 100 unless every non-empty PART is fully protected by a size tag."""
    parts = TOKEN_PATTERNS["part"].split(
        TOKEN_PATTERNS["speaker"].sub("", translated, count=1)
    )
    scales: list[float] = []
    for part in parts:
        if not part.strip():
            continue
        match = TMP_SIZE_WRAPPER.fullmatch(part)
        if match is None:
            return 100
        scales.append(float(match.group(1)))
    return int(max(scales)) if scales else 100


def validate_journal_layout(item: dict[str, Any]) -> list[str]:
    recommended = recommended_journal_scale(item)
    if recommended == 100:
        return []
    applied = applied_journal_scale(item.get("translated_text") or "")
    if applied <= recommended:
        return []
    game = item.get("extra", {}).get("game", {})
    return [
        "角色手记超过版面容量："
        f"{game.get('speaker', 'UNKNOWN')} 需缩放至 {recommended}% 或更小，"
        f"当前为 {applied}%"
    ]


def validate_item(item: dict[str, Any]) -> list[str]:
    errors: list[str] = []
    source = item.get("source_text") or ""
    translated = item.get("translated_text") or ""
    game = item.get("extra", {}).get("game", {})
    if not translated.strip():
        errors.append("translated_text 为空")
    if sha256_text(source) != game.get("source_sha256"):
        errors.append("source_sha256 与 source_text 不一致")
    errors.extend(validate_temperature_units(item))
    errors.extend(validate_chinese_quotes(translated))
    errors.extend(validate_duplicate_punctuation(translated))
    errors.extend(validate_journal_layout(item))
    for name, pattern in TOKEN_PATTERNS.items():
        source_tokens = pattern.findall(source)
        translated_tokens = pattern.findall(translated)
        if name == "tmp_tag" and source_tokens != translated_tokens and not any(
            token.lower().startswith("<size") or token.lower() == "</size>"
            for token in source_tokens
        ):
            size_depth = 0
            filtered_tokens: list[str] = []
            valid_added_size_tags = True
            for token in translated_tokens:
                if TMP_SIZE_OPEN.fullmatch(token):
                    size_depth += 1
                elif token.lower() == "</size>":
                    if size_depth == 0:
                        valid_added_size_tags = False
                    else:
                        size_depth -= 1
                else:
                    filtered_tokens.append(token)
            if valid_added_size_tags and size_depth == 0 and filtered_tokens == source_tokens:
                continue
        if source_tokens != translated_tokens:
            errors.append(f"{name} 标记序列不一致: {source_tokens!r} != {translated_tokens!r}")
    kind = game.get("kind")
    # The game's ordinary system labels and the startup auto-log frame use a
    # legacy TMP font asset whose U+2026 mapping renders as "à". Other dialogue
    # frames use different font assets and display Chinese ellipses correctly.
    legacy_ellipsis_path = kind == "component_string" or (
        kind == "component_dialogue_frame"
        and game.get("field_path") == "autoLogStartFrame"
    )
    if legacy_ellipsis_path and "…" in translated:
        errors.append("旧系统字体文本不能使用 U+2026；请改用 ASCII 三点省略号 ...")
    if kind in {"dialogue_frame", "component_dialogue_frame"} and not legacy_ellipsis_path:
        errors.extend(validate_dialogue_ellipsis(translated))
    if kind in {"dialogue_frame", "component_dialogue_frame"}:
        expected_parts = int(game.get("part_count", -1))
        actual_parts = len(TOKEN_PATTERNS["part"].findall(translated))
        if actual_parts != expected_parts:
            errors.append(f"对白 part 数量错误: {actual_parts} != {expected_parts}")
        if len(TOKEN_PATTERNS["speaker"].findall(translated)) != 1:
            errors.append("对白必须且只能包含一个 SPEAKER 标记")
    return errors


def category_for(kind: str) -> str:
    return {
        "dialogue_frame": "dialogue",
        "dialogue_title": "titles",
        "ui_text": "ui",
        "ui_template": "ui",
        "achievement_name": "ui",
        "achievement_description": "ui",
        "display_value": "ui",
        "ui_fragment": "ui",
        "component_string": "system",
        "component_dialogue_frame": "system",
    }.get(kind, "system")


def build_file(category: str, entries: list[dict[str, Any]]) -> dict[str, Any]:
    return {
        "format_version": FORMAT_VERSION,
        "game_version": GAME_VERSION,
        "language": "zh-CN",
        "category": category,
        "entries": sorted(entries, key=lambda entry: entry["stable_key"]),
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="Build validated runtime translation JSON files")
    parser.add_argument("--cache", type=Path, default=PROJECT_DIR / "work" / "cache.json")
    parser.add_argument(
        "--output",
        type=Path,
        default=PROJECT_DIR / "build" / "package" / "DeepSpaceChinese" / "Translations",
    )
    parser.add_argument(
        "--report", type=Path, default=PROJECT_DIR / "build" / "validation-report.json"
    )
    parser.add_argument(
        "--overrides",
        type=Path,
        default=DEFAULT_MANUAL_OVERRIDES,
        help="人工译文修订源；构建前按 text_index 和原文哈希覆盖缓存。",
    )
    parser.add_argument("--strict", action="store_true", help="有无效译文时返回非零退出码")
    args = parser.parse_args()

    cache = json.loads(args.cache.read_text(encoding="utf-8"))
    applied_overrides = apply_manual_overrides(cache, args.overrides)
    output: dict[str, list[dict[str, Any]]] = {
        "dialogue": [],
        "titles": [],
        "ui": [],
        "system": [],
    }
    issues: list[dict[str, Any]] = []
    seen_keys: set[str] = set()
    translated_status = Counter()

    for item in iter_items(cache):
        status = int(item.get("translation_status", 0))
        translated_status[status] += 1
        if status not in (1, 2):
            continue
        game = item.get("extra", {}).get("game", {})
        stable_key = game.get("stable_key", "")
        errors = validate_item(item)
        if not stable_key:
            errors.append("缺少 stable_key")
        elif stable_key in seen_keys:
            errors.append("stable_key 重复")
        if errors:
            issues.append(
                {
                    "text_index": item.get("text_index"),
                    "stable_key": stable_key,
                    "errors": errors,
                }
            )
            continue
        seen_keys.add(stable_key)
        kind = str(game.get("kind", ""))
        category = category_for(kind)
        runtime_game = {
            key: value
            for key, value in game.items()
            if key
            in {
                "kind",
                "chunk_id",
                "frame_index",
                "part_count",
                "scope",
                "object_path",
                "component_index",
                "field_path",
                "original_text",
                "protect_player_name",
                "player_token_literal",
                "runtime_tokens",
                "template_id",
                "achievement_index",
                "display_field",
                "fragment_id",
                "object_name",
                "translate_display_values",
            }
        }
        # Derive this security-sensitive policy from extraction rules instead of
        # trusting stale cache metadata. Absence means false in the runtime.
        if should_translate_display_values(game):
            runtime_game["translate_display_values"] = True
        else:
            runtime_game.pop("translate_display_values", None)
        output[category].append(
            {
                "stable_key": stable_key,
                "kind": kind,
                "source_sha256": game["source_sha256"],
                "source_text": item["source_text"],
                "translated_text": item["translated_text"],
                "game": runtime_game,
            }
        )

    file_names = {
        "dialogue": "dialogue.json",
        "titles": "titles.json",
        "ui": "ui.json",
        "system": "system.json",
    }
    manifest_files: list[dict[str, Any]] = []
    for category, file_name in file_names.items():
        path = args.output / file_name
        write_json(path, build_file(category, output[category]))
        data = path.read_bytes()
        manifest_files.append(
            {
                "path": file_name,
                "category": category,
                "entries": len(output[category]),
                "sha256": sha256_bytes(data),
            }
        )

    alias_source = PROJECT_DIR / "patch" / "Translations" / "dictionary_trigger_aliases.json"
    alias_payload = json.loads(alias_source.read_text(encoding="utf-8"))
    alias_entries = alias_payload.get("entries")
    if alias_payload.get("format_version") != 1 or not isinstance(alias_entries, list):
        raise ValueError(f"词典中文触发规则格式无效：{alias_source}")
    allowed_rule_types = {"exact", "contains", "contains_all"}
    for index, entry in enumerate(alias_entries):
        if not isinstance(entry, dict) or not entry.get("channel") or not entry.get("english"):
            raise ValueError(f"词典中文触发规则第 {index + 1} 条缺少定位键。")
        for rule in entry.get("rules", []):
            if rule.get("type") not in allowed_rule_types or not isinstance(rule.get("values"), list):
                raise ValueError(f"词典中文触发规则第 {index + 1} 条包含无效匹配器。")
            if "exclude_any" in rule and not isinstance(rule.get("exclude_any"), list):
                raise ValueError(f"词典中文触发规则第 {index + 1} 条包含无效排除项。")
    validate_no_conflicts(alias_entries)
    alias_path = args.output / "dictionary_trigger_aliases.json"
    alias_path.write_bytes(alias_source.read_bytes())
    alias_data = alias_path.read_bytes()
    manifest_files.append(
        {
            "path": alias_path.name,
            "category": "dictionary_trigger_aliases",
            "entries": len(alias_entries),
            "sha256": sha256_bytes(alias_data),
        }
    )

    # Four generated text JSON files plus one maintained trigger-rule JSON are
    # exposed. Older builds wrote an unused manifest.json beside them; remove
    # that known legacy artifact so repeated builds stay deterministic.
    legacy_manifest = args.output / "manifest.json"
    if legacy_manifest.is_file():
        legacy_manifest.unlink()
    total_entries = sum(len(entries) for entries in output.values())
    report = {
        "format_version": FORMAT_VERSION,
        "cache": str(args.cache),
        "output": str(args.output),
        "status_counts": {str(key): value for key, value in sorted(translated_status.items())},
        "valid_runtime_entries": total_entries,
        "manual_overrides": applied_overrides,
        "invalid_entries": len(issues),
        "issues": issues,
        "files": manifest_files,
    }
    write_json(args.report, report)
    print(json.dumps(report, ensure_ascii=False, indent=2))
    return 1 if args.strict and issues else 0


if __name__ == "__main__":
    raise SystemExit(main())
