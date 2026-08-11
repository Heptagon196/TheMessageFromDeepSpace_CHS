from __future__ import annotations

import json
import os
import sys
from pathlib import Path


PROJECT_DIR = Path(__file__).resolve().parents[1]
GAME_ROOT = PROJECT_DIR.parent
dependency_dir = os.environ.get("TMFDS_PYTHON_PACKAGES")
if not dependency_dir:
    bundled = PROJECT_DIR / "build" / "puzzle-inspector-python"
    dependency_dir = str(bundled if bundled.exists() else PROJECT_DIR / "tools" / "python-packages")
sys.path.insert(0, dependency_dir)

import UnityPy  # noqa: E402
from UnityPy.helpers.TypeTreeGenerator import TypeTreeGenerator  # noqa: E402


def main() -> int:
    asset_name = sys.argv[1]
    path_ids = {int(value) for value in sys.argv[2:]}
    path = GAME_ROOT / "The Message From Deep Space_Data" / asset_name
    generator = TypeTreeGenerator("6000.0.73f1")
    generator.load_local_game(str(GAME_ROOT))
    env = UnityPy.load(str(path))
    env.typetree_generator = generator
    out = []
    for obj in env.objects:
        if obj.path_id not in path_ids:
            continue
        out.append(
            {
                "path_id": obj.path_id,
                "type": obj.type.name,
                "tree": obj.parse_as_dict(),
            }
        )
    print(json.dumps(out, ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
