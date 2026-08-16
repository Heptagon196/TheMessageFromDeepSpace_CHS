from __future__ import annotations

import json
import sys
from pathlib import Path


TOOLS_DIR = Path(__file__).resolve().parent
sys.path.insert(0, str(TOOLS_DIR))
from project_config import DATA_DIR, GAME_ROOT, PROJECT_DIR
from python_runtime import load_unitypy  # noqa: E402

UnityPy, TypeTreeGenerator = load_unitypy()
from UnityPy.export.ShaderConverter import export_shader  # noqa: E402


def main() -> int:
    generator = TypeTreeGenerator("6000.0.73f1")
    generator.load_local_game(str(GAME_ROOT))
    env = UnityPy.load(str(DATA_DIR / "sharedassets0.assets"))
    env.typetree_generator = generator
    shader_obj = next(
        obj for obj in env.objects if obj.type.name == "Shader" and obj.path_id == 445
    )
    tree = shader_obj.parse_as_dict()
    output_path = PROJECT_DIR / "work" / "monitor-shader.json"
    output_path.write_text(json.dumps(tree, ensure_ascii=False, indent=2), encoding="utf-8")
    shader = shader_obj.read()
    shader_path = PROJECT_DIR / "work" / "monitor-shader.shader"
    shader_path.write_text(export_shader(shader), encoding="utf-8")
    print(f"Wrote {output_path} ({output_path.stat().st_size} bytes)")
    print(f"Wrote {shader_path} ({shader_path.stat().st_size} bytes)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
