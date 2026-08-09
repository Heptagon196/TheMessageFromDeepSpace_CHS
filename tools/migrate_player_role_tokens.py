from __future__ import annotations

import argparse
import hashlib
import json
import re
import shutil
from datetime import datetime
from pathlib import Path
from typing import Any


PLAYER_RE = re.compile(r"(?<![A-Za-z])(?:[Tt]he\s+)?Translator\b")
SIGNAL_RE = re.compile(r"\|(-?\d{1,3})")
SPEAKER_RE = re.compile(r"^\{SPEAKER_[A-Z0-9_]+\}")

TRANSLATIONS = {
    1310185773: "{SPEAKER_BAUTISTA}{PART_000}就是浮点数。{PART_001}这位翻译员让我刮目相看。",
    1251455204: "{SPEAKER_COLLINS}{PART_000}他是巴蒂斯塔博士，{PART_001}我是柯林斯博士，{PART_002}这位翻译员是{PLAYER_NAME}。",
    236651567: "{SPEAKER_AKERS}{PART_000}悠着点，{PART_001}翻译员，{PART_002}可别一上来就挑起星际冲突，{PART_003}好吗？",
    2036594555: "{SPEAKER_DOPPLER}{PART_000}我是道格拉斯·多普勒博士，美国航天局材料科学部门负责人。现在是 1973 年 5 月 13 日凌晨 5 点 04 分。与我一同在场的有备受尊敬的程序员兼图形工程师布莱恩·巴蒂斯塔博士、波士顿学院著名语言学教授凯莉·柯林斯博士，以及天文学家艾伦·埃克斯博士。此外，还有一位翻译员——为什么需要翻译员，希望各位很快就会明白。",
    1487549189: "{SPEAKER_DOPPLER}{PART_000}$animD23我是道格拉斯·多普勒博士，{PART_001}美国航天局材料科学部门负责人。{PART_002}$animD19与我一同在场的有天文学家埃克斯博士、{PART_003}$animD20程序员巴蒂斯塔博士、{PART_004}$animD21语言学家柯林斯博士，{PART_005}$animD22以及翻译员 {PLAYER_NAME}。",
    1783091105: "{SPEAKER_COLLINS}{PART_000}他们的核心……{PART_001}{PLAYER_NAME}，{PART_002}我们也许还没完全弄懂，{PART_003}但绝对相信你的判断。{PART_004}你就是我们的翻译员。",
    691491379: "{SPEAKER_AKERS}{PART_000}$animA5随你吧。{PART_001}我不管了。{PART_002}正就是负，{PART_003}负就是正。{PART_004}行。{PART_005}好。{PART_006}随便。{PART_007}你可是翻译员，{PART_008}{PLAYER_NAME}。{PART_009}现实由你说了算。",
}

TARGETS = set(TRANSLATIONS) | {1186374130}


def signal_placeholder(match: re.Match[str]) -> str:
    value = int(match.group(1))
    return f"{{SIG_N{abs(value):03d}}}" if value < 0 else f"{{SIG_{value:03d}}}"


def protect(text: str) -> str:
    return PLAYER_RE.sub("{PLAYER_NAME}", SIGNAL_RE.sub(signal_placeholder, text))


def trim_edges(text: str) -> str:
    return text.strip()


def rebuild_source(item: dict[str, Any]) -> str:
    game = item["extra"]["game"]
    speaker = SPEAKER_RE.match(item["source_text"])
    if speaker is None:
        raise ValueError(f"缺少 SPEAKER 标记: {item['text_index']}")
    parts = game.get("parts") or []
    if len(parts) != int(game.get("part_count", -1)):
        raise ValueError(f"parts 元数据不完整: {item['text_index']}")
    body = [speaker.group(0)]
    for index, part in enumerate(parts):
        original = str(part.get("original_text", ""))
        body.append(f"{{PART_{index:03d}}}{trim_edges(protect(original))}")
    return "".join(body)


def main() -> int:
    parser = argparse.ArgumentParser(description="区分小写 translator 角色称谓与玩家姓名 Translator")
    parser.add_argument("cache", type=Path)
    args = parser.parse_args()
    cache_path = args.cache.resolve()
    project = json.loads(cache_path.read_text(encoding="utf-8"))
    found: set[int] = set()
    changed: list[dict[str, Any]] = []
    for file_data in project.get("files", {}).values():
        for item in file_data.get("items", []):
            index = int(item.get("text_index", -1))
            if index not in TARGETS:
                continue
            found.add(index)
            old_source = item["source_text"]
            new_source = rebuild_source(item)
            if old_source == new_source:
                raise ValueError(f"目标源文未发生变化: {index}")
            item["source_text"] = new_source
            item["text_to_detect"] = new_source
            item["extra"]["game"]["source_sha256"] = hashlib.sha256(
                new_source.encode("utf-8")
            ).hexdigest()
            if index in TRANSLATIONS:
                item["translated_text"] = TRANSLATIONS[index]
            changed.append(
                {
                    "text_index": index,
                    "old_source": old_source,
                    "new_source": new_source,
                    "translated_text": item.get("translated_text", ""),
                }
            )
    missing = TARGETS - found
    if missing:
        raise ValueError(f"cache 缺少目标条目: {sorted(missing)}")
    stamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    backup = cache_path.with_name(f"{cache_path.name}.bak.{stamp}.player-role")
    if backup.exists():
        raise FileExistsError(backup)
    shutil.copy2(cache_path, backup)
    cache_path.write_text(
        json.dumps(project, ensure_ascii=False, separators=(",", ":")) + "\n",
        encoding="utf-8",
    )
    report = cache_path.with_name("player_role_migration_report.json")
    report.write_text(json.dumps(changed, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"changed": len(changed), "backup": str(backup), "report": str(report)}, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
