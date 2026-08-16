from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path
from typing import Any


TOOLS_DIR = Path(__file__).resolve().parent
sys.path.insert(0, str(TOOLS_DIR))
from project_config import DATA_DIR, GAME_ROOT, PROJECT_DIR
from python_runtime import load_unitypy
SAVE_DIR = (
    Path.home()
    / "AppData"
    / "LocalLow"
    / "Applesinmypants"
    / "The Message From Deep Space"
)


def find_dictionary(explicit_path: str | None) -> Path | None:
    if explicit_path:
        path = Path(explicit_path).expanduser().resolve()
        if not path.is_file():
            raise FileNotFoundError(f"找不到词典存档：{path}")
        return path

    candidates = [
        path
        for path in SAVE_DIR.glob("DICTIONARY-*.save")
        if re.fullmatch(r"DICTIONARY-\d+\.save", path.name, re.IGNORECASE)
    ]
    return max(candidates, key=lambda path: path.stat().st_mtime) if candidates else None


def load_dictionary(path: Path | None) -> dict[int, str]:
    if path is None:
        return {}
    data = json.loads(path.read_text(encoding="utf-8-sig"))
    word_dict = data.get("wordDict", {})
    keys = word_dict.get("keys", [])
    values = word_dict.get("values", [])
    result: dict[int, str] = {}
    for key, value in zip(keys, values):
        if isinstance(value, dict):
            name = value.get("name", "")
        else:
            name = str(value)
        if name:
            result[int(key)] = name
    return result


def decode_tokens(signals: list[int], dictionary: dict[int, str]) -> list[str]:
    return [dictionary.get(value, str(value)) if value < 0 else str(value) for value in signals]


def render_tokens(tokens: list[str]) -> str:
    """Render a readable approximation without claiming to reproduce game layout."""
    compact_before = {".", ",", ";", ")", "?"}
    compact_after = {"("}
    output = ""
    previous = ""
    for token in tokens:
        if not output:
            output = token
        elif token in compact_before or previous in compact_after:
            output += token
        elif token.isdigit() and (previous.isdigit() or previous == "."):
            output += token
        else:
            output += " " + token
        previous = token
    return output


def decoded_signal(signals: list[int], dictionary: dict[int, str]) -> dict[str, Any]:
    tokens = decode_tokens(signals, dictionary)
    return {
        "raw": signals,
        "tokens": tokens,
        "text": render_tokens(tokens),
    }


def extract(display_id: int, dictionary: dict[int, str]) -> dict[str, Any]:
    if display_id < 1:
        raise ValueError("显示题号必须从 1 开始。")

    UnityPy, TypeTreeGenerator = load_unitypy()
    generator = TypeTreeGenerator("6000.0.73f1")
    generator.load_local_game(str(GAME_ROOT))
    env = UnityPy.load(str(DATA_DIR / "sharedassets0.assets"), str(DATA_DIR / "level0"))
    env.typetree_generator = generator

    puzzles: dict[int, dict[str, Any]] = {}
    puzzle_lists: dict[int, dict[str, Any]] = {}
    managers: list[dict[str, Any]] = []
    parse_failures = 0
    for obj in env.objects:
        if obj.type.name != "MonoBehaviour":
            continue
        try:
            tree = obj.parse_as_dict()
        except Exception:
            parse_failures += 1
            continue
        if "winningResponse" in tree and "rockOutput" in tree:
            puzzles[obj.path_id] = tree
        if "puzzleGroupName" in tree and "puzzles" in tree:
            puzzle_lists[obj.path_id] = tree
        if "puzzleLists" in tree and "puzzleListsToReset" in tree:
            managers.append(tree)

    if not managers:
        raise RuntimeError("资源中没有找到 PuzzleManager。")
    manager = max(managers, key=lambda item: len(item.get("puzzleLists", [])))

    ordered: list[tuple[int, str]] = []
    for pointer in manager.get("puzzleLists", []):
        puzzle_list = puzzle_lists.get(pointer.get("m_PathID", 0))
        if not puzzle_list:
            continue
        group = puzzle_list.get("puzzleGroupName", "")
        ordered.extend(
            (entry.get("m_PathID", 0), group)
            for entry in puzzle_list.get("puzzles", [])
        )

    zero_based = display_id - 1
    if zero_based >= len(ordered):
        raise IndexError(f"显示题号 {display_id} 超出范围；资源中共有 {len(ordered)} 道题。")
    path_id, group = ordered[zero_based]
    puzzle = puzzles.get(path_id)
    if puzzle is None:
        raise RuntimeError(f"题目 {display_id} 对应资源 {path_id} 无法解析。")

    rock_output = puzzle.get("rockOutput", {}).get("signals", [])
    winning_response = puzzle.get("winningResponse", {}).get("signals", [])
    alt_responses = [
        entry.get("signals", []) for entry in puzzle.get("altResponses", [])
    ]
    return {
        "display_id": display_id,
        "zero_based_index": zero_based,
        "path_id": path_id,
        "developer_name": puzzle.get("m_Name", ""),
        "developer_title": puzzle.get("title", ""),
        "developer_unique_id": puzzle.get("uniqueID"),
        "group": group,
        "premise": decoded_signal(rock_output, dictionary),
        "winning_response": decoded_signal(winning_response, dictionary),
        "allow_alt_responses": puzzle.get("allowAltResponses", False),
        "alt_responses": [decoded_signal(item, dictionary) for item in alt_responses],
        "diagnostics": {
            "puzzle_count": len(ordered),
            "parse_failures": parse_failures,
        },
    }


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="按游戏界面显示题号提取题面，并用当前玩家词典解码。"
    )
    parser.add_argument("display_id", type=int, help="游戏界面显示的题号，例如 100")
    parser.add_argument(
        "--dictionary",
        help="词典存档路径；省略时自动选择最近修改的 DICTIONARY-*.save",
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    dictionary_path = find_dictionary(args.dictionary)
    dictionary = load_dictionary(dictionary_path)
    result = extract(args.display_id, dictionary)
    result["dictionary_path"] = str(dictionary_path) if dictionary_path else None
    result["dictionary_entries"] = len(dictionary)
    print(json.dumps(result, ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except Exception as exc:
        print(f"题目提取失败：{exc}", file=sys.stderr)
        raise SystemExit(1)
