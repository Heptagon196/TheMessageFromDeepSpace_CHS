from __future__ import annotations

import json
import sys
from pathlib import Path


TOOLS_DIR = Path(__file__).resolve().parent
sys.path.insert(0, str(TOOLS_DIR))
from project_config import GAME_ROOT, PROJECT_DIR

from python_runtime import load_unitypy  # noqa: E402

UnityPy, TypeTreeGenerator = load_unitypy()


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
