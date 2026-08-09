from __future__ import annotations

import argparse
import json
import re
from collections import Counter
from pathlib import Path


PROJECT = Path(__file__).resolve().parents[1]


def items(cache: dict):
    for cache_file in cache.get("files", {}).values():
        yield from cache_file.get("items", [])


def main() -> int:
    parser = argparse.ArgumentParser(description="Inspect likely glossary terms and their contexts")
    parser.add_argument("terms", nargs="*")
    parser.add_argument("--limit", type=int, default=300)
    parser.add_argument("--group", help="Print source texts from CacheFile names beginning with this value")
    parser.add_argument("--key-prefix", help="Print items whose stable key begins with this value")
    parser.add_argument("--ngrams", type=int, choices=(2, 3), help="Print frequent word n-grams")
    args = parser.parse_args()
    cache = json.loads((PROJECT / "work" / "cache.json").read_text(encoding="utf-8"))
    if args.group:
        shown = 0
        for file_name, cache_file in cache.get("files", {}).items():
            if not file_name.startswith(args.group):
                continue
            for item in cache_file.get("items", []):
                if int(item.get("translation_status", 0)) == 7:
                    continue
                print(f"{file_name}\t{item.get('text_index')}\t{item.get('source_text', '')}")
                shown += 1
                if shown >= args.limit:
                    return 0
        return 0
    active = [item for item in items(cache) if int(item.get("translation_status", 0)) != 7]
    if args.key_prefix:
        matched = [
            item for item in active
            if str(item.get("extra", {}).get("game", {}).get("stable_key", "")).startswith(args.key_prefix)
        ]
        for item in matched[: args.limit]:
            game = item.get("extra", {}).get("game", {})
            print(f"{item.get('text_index')}\t{game.get('stable_key', '')}\t{item.get('source_text', '')}")
        return 0
    if args.terms:
        needles = [term.casefold() for term in args.terms]
        matched = [
            item for item in active
            if any(needle in str(item.get("source_text", "")).casefold() for needle in needles)
        ]
        for item in matched[: args.limit]:
            game = item.get("extra", {}).get("game", {})
            print(
                f"{item.get('text_index')}\t{game.get('stable_key', '')}\t"
                f"{item.get('source_text', '')}"
            )
        return 0

    if args.ngrams:
        stop = {
            "a", "an", "and", "are", "as", "at", "be", "been", "but", "by", "do", "for",
            "from", "had", "has", "have", "he", "her", "him", "his", "i", "if", "in", "is",
            "it", "its", "me", "my", "not", "of", "on", "or", "our", "she", "so", "that",
            "the", "their", "them", "then", "they", "this", "to", "too", "up", "was", "we",
            "were", "what", "when", "where", "which", "who", "why", "will", "with", "would",
            "you", "your",
        }
        counts: Counter[tuple[str, ...]] = Counter()
        for item in active:
            text = re.sub(r"\{[^}]+\}|\$anim[A-Za-z]\d{1,2}|<[^>]+>", " ", item.get("source_text", ""))
            words = re.findall(r"[A-Za-z]+(?:[-'][A-Za-z]+)*|\d+(?:\.\d+)?", text.lower())
            for index in range(len(words) - args.ngrams + 1):
                phrase = tuple(words[index : index + args.ngrams])
                if all(word in stop or word.isdigit() for word in phrase):
                    continue
                if phrase[0] in stop and phrase[-1] in stop:
                    continue
                counts[phrase] += 1
        for phrase, count in counts.most_common(args.limit):
            print(f"{count:4} {' '.join(phrase)}")
        return 0

    pattern = re.compile(r"\b(?:Dr\.\s+)?[A-Z][a-z]{2,}\b")
    counts = Counter(match for item in active for match in pattern.findall(item.get("source_text", "")))
    for token, count in counts.most_common(args.limit):
        print(f"{count:4} {token}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
