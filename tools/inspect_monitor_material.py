from __future__ import annotations

import json
import sys
from pathlib import Path


TOOLS_DIR = Path(__file__).resolve().parent
sys.path.insert(0, str(TOOLS_DIR))
from project_config import DATA_DIR, GAME_ROOT, PROJECT_DIR
from python_runtime import load_unitypy  # noqa: E402

UnityPy, TypeTreeGenerator = load_unitypy()


def pptr_info(ptr) -> dict[str, object] | None:
    if ptr is None:
        return None
    info: dict[str, object] = {
        "file_id": getattr(ptr, "file_id", getattr(ptr, "m_FileID", None)),
        "path_id": getattr(ptr, "path_id", getattr(ptr, "m_PathID", None)),
    }
    try:
        obj = ptr.deref()
        if obj is not None:
            info.update(
                {
                    "asset": obj.assets_file.name,
                    "resolved_path_id": obj.path_id,
                    "type": obj.type.name,
                }
            )
            value = obj.read()
            info["name"] = getattr(value, "m_Name", "")
    except Exception as exc:
        info["error"] = repr(exc)
    return info


def main() -> int:
    paths = [
        DATA_DIR / name
        for name in (
            "globalgamemanagers.assets",
            "resources.assets",
            "sharedassets0.assets",
            "sharedassets1.assets",
            "sharedassets2.assets",
            "sharedassets3.assets",
            "sharedassets4.assets",
            "level0",
            "level1",
            "level2",
            "level3",
            "level4",
        )
    ]
    generator = TypeTreeGenerator("6000.0.73f1")
    generator.load_local_game(str(GAME_ROOT))
    env = UnityPy.load(*map(str, paths))
    env.typetree_generator = generator

    target_materials: list[dict[str, object]] = []
    renderers: list[dict[str, object]] = []
    for obj in env.objects:
        if obj.type.name == "Material":
            try:
                material = obj.read()
                name = getattr(material, "m_Name", "")
            except Exception:
                continue
            if name in {"M_LeftScreen", "M_RightScreen"}:
                def key_name(value) -> str:
                    return value if isinstance(value, str) else str(getattr(value, "name", value))

                saved = getattr(material, "m_SavedProperties", None)
                texture_envs: list[dict[str, object]] = []
                for key, value in getattr(saved, "m_TexEnvs", []) if saved else []:
                    texture = getattr(value, "m_Texture", None)
                    texture_envs.append(
                        {
                            "key": key_name(key),
                            "texture": pptr_info(texture) if texture else None,
                            "scale": [float(value.m_Scale.x), float(value.m_Scale.y)],
                            "offset": [float(value.m_Offset.x), float(value.m_Offset.y)],
                        }
                    )
                target_materials.append(
                    {
                        "asset": obj.assets_file.name,
                        "path_id": obj.path_id,
                        "name": name,
                        "shader": pptr_info(getattr(material, "m_Shader", None)),
                        "texture_envs": texture_envs,
                        "floats": [
                            {"key": key_name(key), "value": float(value)}
                            for key, value in (getattr(saved, "m_Floats", []) if saved else [])
                        ],
                        "ints": [
                            {"key": key_name(key), "value": int(value)}
                            for key, value in (getattr(saved, "m_Ints", []) if saved else [])
                        ],
                        "colors": [
                            {
                                "key": key_name(key),
                                "value": [
                                    float(getattr(value, channel))
                                    for channel in ("r", "g", "b", "a")
                                ],
                            }
                            for key, value in (getattr(saved, "m_Colors", []) if saved else [])
                        ],
                    }
                )

    for obj in env.objects:
        if obj.type.name not in {"MeshRenderer", "SkinnedMeshRenderer", "SpriteRenderer"}:
            continue
        try:
            renderer = obj.read()
            material_names: list[str] = []
            material_refs: list[dict[str, object]] = []
            for ptr in getattr(renderer, "m_Materials", []):
                try:
                    material = ptr.read()
                    name = getattr(material, "m_Name", "")
                    material_names.append(name)
                    material_refs.append(
                        {
                            "asset": ptr.assets_file.name,
                            "path_id": ptr.path_id,
                            "name": name,
                        }
                    )
                except Exception as exc:
                    material_refs.append({"error": repr(exc)})
            if not any("RightScreen" in name or "LeftScreen" in name for name in material_names):
                continue
            go_name = ""
            try:
                game_object = renderer.m_GameObject.read()
                go_name = getattr(game_object, "m_Name", "")
            except Exception:
                pass
            renderers.append(
                {
                    "asset": obj.assets_file.name,
                    "path_id": obj.path_id,
                    "type": obj.type.name,
                    "game_object": go_name,
                    "materials": material_refs,
                }
            )
        except Exception:
            continue

    output = {"materials": target_materials, "renderers": renderers}
    output_path = PROJECT_DIR / "work" / "monitor-material-inspection.json"
    output_path.write_text(json.dumps(output, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps(output, ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
