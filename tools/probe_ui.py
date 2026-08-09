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
    generator = TypeTreeGenerator("6000.0.73f1")
    generator.load_local_game(str(GAME_ROOT))
    paths = sorted(DATA_DIR.glob("level*")) + sorted(DATA_DIR.glob("*.assets"))

    candidates: list[dict[str, object]] = []
    failures: list[dict[str, object]] = []
    for path in paths:
        print(f"Scanning {path.name}...")
        env = UnityPy.load(str(path))
        env.typetree_generator = generator
        for obj in env.objects:
            if obj.type.name != "MonoBehaviour":
                continue
            try:
                tree = obj.parse_as_dict()
            except Exception as exc:
                failures.append({"asset": path.name, "path_id": obj.path_id, "error": repr(exc)})
                continue
            text = tree.get("m_text")
            if not isinstance(text, str):
                continue
            candidates.append(
                {
                    "asset": path.name,
                    "path_id": obj.path_id,
                    "name": tree.get("m_Name", ""),
                    "game_object": tree.get("m_GameObject"),
                    "text": text,
                    "keys": sorted(tree.keys()),
                }
            )

    out = {
        "asset_count": len(paths),
        "candidate_count": len(candidates),
        "failure_count": len(failures),
        "failures": failures,
        "candidates": candidates,
    }
    out_path = PROJECT_DIR / "work" / "ui-probe.json"
    out_path.write_text(json.dumps(out, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"Wrote {out_path} ({len(candidates)} TMP text candidates, {len(failures)} failures)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
