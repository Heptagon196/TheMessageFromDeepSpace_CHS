from __future__ import annotations

import json
import sys
from pathlib import Path


PROJECT_DIR = Path(__file__).resolve().parents[1]
GAME_ROOT = PROJECT_DIR.parent
DATA_DIR = GAME_ROOT / "The Message From Deep Space_Data"
sys.path.insert(0, str(PROJECT_DIR / "tools" / "python-packages"))

import UnityPy  # noqa: E402
from UnityPy.helpers.TypeTreeGenerator import TypeTreeGenerator  # noqa: E402


def main() -> int:
    target = int(sys.argv[1]) if len(sys.argv) > 1 else 62
    generator = TypeTreeGenerator("6000.0.73f1")
    generator.load_local_game(str(GAME_ROOT))
    env = UnityPy.load(str(DATA_DIR / "sharedassets0.assets"), str(DATA_DIR / "level0"))
    env.typetree_generator = generator

    puzzles: dict[int, dict] = {}
    lists: list[dict] = []
    managers: list[dict] = []
    failures = 0
    for obj in env.objects:
        if obj.type.name != "MonoBehaviour":
            continue
        try:
            tree = obj.parse_as_dict()
        except Exception:
            failures += 1
            continue
        if "winningResponse" in tree and "rockOutput" in tree:
            puzzles[obj.path_id] = tree
        if "puzzleGroupName" in tree and "puzzles" in tree:
            lists.append({"path_id": obj.path_id, "tree": tree})
        if "puzzleLists" in tree and "puzzleListsToReset" in tree:
            managers.append({"path_id": obj.path_id, "tree": tree})

    result = {
        "target_zero_based": target,
        "parse_failures": failures,
        "puzzle_count": len(puzzles),
        "list_count": len(lists),
        "manager_count": len(managers),
        "lists": [
            {
                "path_id": item["path_id"],
                "name": item["tree"].get("m_Name", ""),
                "group": item["tree"].get("puzzleGroupName", ""),
                "puzzle_path_ids": [pointer.get("m_PathID", 0)
                                    for pointer in item["tree"].get("puzzles", [])],
            }
            for item in lists
        ],
        "managers": [
            {
                "path_id": item["path_id"],
                "puzzle_list_path_ids": [pointer.get("m_PathID", 0)
                                         for pointer in item["tree"].get("puzzleLists", [])],
            }
            for item in managers
        ],
    }

    ordered_ids: list[int] = []
    if managers:
        list_by_id = {item["path_id"]: item["tree"] for item in lists}
        for pointer in managers[0]["tree"].get("puzzleLists", []):
            puzzle_list = list_by_id.get(pointer.get("m_PathID", 0))
            if puzzle_list:
                ordered_ids.extend(entry.get("m_PathID", 0)
                                   for entry in puzzle_list.get("puzzles", []))
    if 0 <= target < len(ordered_ids):
        puzzle = puzzles.get(ordered_ids[target])
        if puzzle:
            result["target"] = {
                "path_id": ordered_ids[target],
                "name": puzzle.get("m_Name", ""),
                "title": puzzle.get("title", ""),
                "unique_id": puzzle.get("uniqueID"),
                "rock_output": puzzle.get("rockOutput", {}).get("signals", []),
                "winning_response": puzzle.get("winningResponse", {}).get("signals", []),
                "allow_alt_responses": puzzle.get("allowAltResponses", False),
                "alt_responses": [entry.get("signals", [])
                                  for entry in puzzle.get("altResponses", [])],
            }
    print(json.dumps(result, ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
