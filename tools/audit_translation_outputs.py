#!/usr/bin/env python3
"""Audit all formal translation batches before they are written to cache.json."""

from __future__ import annotations

import argparse
import json
import re
from collections import Counter, defaultdict
from pathlib import Path

from translation_text_checks import validate_chinese_quotes


CONTROL_RE = re.compile(
    r"\{(?:SPEAKER_[A-Z0-9_]+|PART_\d{3}|SIG_(?:N)?\d{3}|PLAYER_NAME|DYN_\d+)\}|"
    r"\$anim(?:[A-Za-z]\d{0,2}|\d{1,2})|<[^>]+>"
)
LATIN_RE = re.compile(r"[A-Za-z][A-Za-z0-9_'-]*(?:[ .&/+:-]+[A-Za-z0-9][A-Za-z0-9_'-]*)*")
NEUTRAL_MANDARIN_FORBIDDEN = ("哪儿", "这儿", "那儿")


def load(path: Path):
    return json.loads(path.read_text(encoding="utf-8"))


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("manifest", type=Path)
    parser.add_argument("glossary", type=Path)
    parser.add_argument("--report", type=Path)
    args = parser.parse_args()

    manifest = load(args.manifest)
    glossary = load(args.glossary)
    batch_dir = args.manifest.parent
    errors: list[dict] = []
    warnings: list[dict] = []
    combined: list[tuple[dict, dict, int]] = []
    all_ids: list[int] = []

    locked_pairs = glossary_pairs(glossary)
    allowed_latin = keep_source_terms(glossary)

    for group in manifest["groups"]:
        number = group["group"]
        source_path = batch_dir / group["source"]
        translation_path = batch_dir / group["translation"]
        source = load(source_path)
        translations = load(translation_path)
        source_ids = [item.get("text_index") for item in source]
        translation_ids = [item.get("text_index") for item in translations]
        if source_ids != translation_ids:
            errors.append({"type": "index_order", "group": number})
            continue
        if len(translations) != group["count"]:
            errors.append(
                {
                    "type": "count",
                    "group": number,
                    "expected": group["count"],
                    "actual": len(translations),
                }
            )
        all_ids.extend(translation_ids)
        for original, translated in zip(source, translations, strict=True):
            combined.append((original, translated, number))

        newterms_path = batch_dir / group["newterms"]
        if newterms_path.exists():
            unresolved = [
                line.strip()
                for line in newterms_path.read_text(encoding="utf-8").splitlines()
                if line.strip() and not line.lstrip().startswith("#")
            ]
            if unresolved:
                errors.append(
                    {
                        "type": "unresolved_newterms",
                        "group": number,
                        "items": unresolved[:30],
                    }
                )

    duplicates = [value for value, count in Counter(all_ids).items() if count > 1]
    if duplicates:
        errors.append({"type": "duplicate_text_index", "items": duplicates[:100]})
    if len(all_ids) != manifest["untranslated"]:
        errors.append(
            {
                "type": "total_count",
                "expected": manifest["untranslated"],
                "actual": len(all_ids),
            }
        )

    repeated: dict[str, list[tuple[str, int, int, str]]] = defaultdict(list)
    for original, translated, group in combined:
        source_text = original.get("source_text", "")
        translated_text = translated.get("translated_text", "")
        text_index = translated.get("text_index")
        context = original.get("context", {})
        kind = context.get("kind", "")
        is_credits = "Credits" in context.get("object_path", "")
        if not translated_text:
            errors.append({"type": "empty_translation", "group": group, "text_index": text_index})
            continue
        quote_issues = validate_chinese_quotes(translated_text)
        if quote_issues:
            errors.append(
                {
                    "type": "chinese_quote_style",
                    "group": group,
                    "text_index": text_index,
                    "issues": quote_issues,
                }
            )
        for forbidden in NEUTRAL_MANDARIN_FORBIDDEN:
            if forbidden in translated_text:
                errors.append(
                    {
                        "type": "non_neutral_mandarin",
                        "group": group,
                        "text_index": text_index,
                        "token": forbidden,
                    }
                )
        for source_term, target_term in locked_pairs:
            if (
                not is_credits
                and contains_term(source_text, source_term)
                and contains_term(translated_text, source_term)
            ):
                errors.append(
                    {
                        "type": "locked_term_left_in_english",
                        "group": group,
                        "text_index": text_index,
                        "source_term": source_term,
                        "target_term": target_term,
                    }
                )
        if kind not in {"dialogue_frame", "component_dialogue_frame", "dialogue_title"}:
            repeated[source_text].append((translated_text, group, text_index, kind))
        visible = CONTROL_RE.sub("", translated_text)
        candidates = [] if is_credits else latin_candidates(visible, allowed_latin)
        if candidates:
            warnings.append(
                {
                    "type": "latin_candidate",
                    "group": group,
                    "text_index": text_index,
                    "items": candidates,
                }
            )

    for source_text, entries in repeated.items():
        variants = {entry[0] for entry in entries}
        if len(entries) > 1 and len(variants) > 1:
            errors.append(
                {
                    "type": "inconsistent_duplicate_source",
                    "source_text": source_text,
                    "entries": [
                        {"translation": text, "group": group, "text_index": index, "kind": kind}
                        for text, group, index, kind in entries[:20]
                    ],
                }
            )

    report = {
        "valid": not errors,
        "groups": len(manifest["groups"]),
        "items": len(all_ids),
        "errors": errors,
        "latin_candidates": warnings,
        "latin_candidate_count": len(warnings),
    }
    rendered = json.dumps(report, ensure_ascii=False, indent=2)
    if args.report:
        args.report.write_text(rendered + "\n", encoding="utf-8")
    print(
        json.dumps(
            {
                "valid": report["valid"],
                "items": report["items"],
                "errors": len(errors),
                "latin_candidates": len(warnings),
                "report": str(args.report) if args.report else None,
            },
            ensure_ascii=False,
        )
    )
    return 0 if not errors else 1


def glossary_pairs(glossary: dict) -> list[tuple[str, str]]:
    pairs: list[tuple[str, str]] = []
    for item in glossary.get("terms", []):
        source = item.get("src", "")
        target = item.get("dst", "")
        if source and target and source != target and re.search(r"[A-Za-z]", source):
            pairs.append((source, target))
    for item in glossary.get("characters", []):
        target = item.get("render", "")
        for source in [item.get("canonical", ""), *item.get("aliases", [])]:
            if source and target and source != target and len(source) > 1:
                pairs.append((source, target))
    return sorted(set(pairs), key=lambda pair: len(pair[0]), reverse=True)


def keep_source_terms(glossary: dict) -> set[str]:
    return {
        item["src"]
        for item in glossary.get("terms", [])
        if item.get("keep_source") and item.get("src")
    }


def contains_term(text: str, term: str) -> bool:
    if not term:
        return False
    if term[0].isalnum() and term[-1].isalnum():
        pattern = rf"(?<![A-Za-z0-9_]){re.escape(term)}(?![A-Za-z0-9_])"
        return re.search(pattern, text) is not None
    return term in text


def latin_candidates(text: str, allowed: set[str]) -> list[str]:
    candidates: list[str] = []
    for match in LATIN_RE.finditer(text):
        token = match.group(0).strip(" .,:;!?-/")
        if not token or token in allowed:
            continue
        if re.fullmatch(r"[A-Z0-9_]{1,5}", token):
            continue
        if re.fullmatch(r"(?:Hz|kHz|MHz|GHz|kg|km|cm|mm|ms|nm|eV|keV|MeV|GeV)", token):
            continue
        candidates.append(token)
    return candidates


if __name__ == "__main__":
    raise SystemExit(main())
