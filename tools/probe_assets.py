from __future__ import annotations

import json
import sys
from collections import Counter
from pathlib import Path


TOOLS_DIR = Path(__file__).resolve().parent
sys.path.insert(0, str(TOOLS_DIR))
from project_config import GAME_ROOT, PROJECT_DIR
from python_runtime import load_unitypy  # noqa: E402

UnityPy, TypeTreeGenerator = load_unitypy()


def main() -> int:
    asset_path = GAME_ROOT / "The Message From Deep Space_Data" / "sharedassets0.assets"
    generator = TypeTreeGenerator("6000.0.73f1")
    generator.load_local_game(str(GAME_ROOT))

    env = UnityPy.load(str(asset_path))
    env.typetree_generator = generator

    counts: Counter[str] = Counter()
    candidates: list[dict[str, object]] = []
    failures: list[dict[str, object]] = []
    for obj in env.objects:
        counts[obj.type.name] += 1
        if obj.type.name != "MonoBehaviour":
            continue
        try:
            tree = obj.parse_as_dict()
        except Exception as exc:  # diagnostic utility
            failures.append({"path_id": obj.path_id, "error": repr(exc)})
            continue

        keys = sorted(tree.keys())
        if any(
            marker in tree
            for marker in (
                "allDialogues",
                "frames",
                "raw",
                "processedRaw",
                "dialogueParts",
            )
        ):
            candidates.append(
                {
                    "path_id": obj.path_id,
                    "name": tree.get("m_Name", ""),
                    "keys": keys,
                    "tree": tree,
                }
            )

    output = {
        "unitypy_version": UnityPy.__version__,
        "asset": str(asset_path),
        "object_counts": dict(sorted(counts.items())),
        "candidate_count": len(candidates),
        "parse_failure_count": len(failures),
        "parse_failures": failures[:20],
        "candidates": candidates,
    }
    out_path = PROJECT_DIR / "work" / "asset-probe.json"
    out_path.parent.mkdir(parents=True, exist_ok=True)
    out_path.write_text(json.dumps(output, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"Wrote {out_path}")
    print(json.dumps({k: output[k] for k in ("object_counts", "candidate_count", "parse_failure_count")}, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
