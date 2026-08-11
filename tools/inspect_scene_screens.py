from __future__ import annotations

import json
import sys
from pathlib import Path


TOOLS_DIR = Path(__file__).resolve().parent
sys.path.insert(0, str(TOOLS_DIR))
from project_config import DATA_DIR, GAME_ROOT, PROJECT_DIR
sys.path.insert(0, str(PROJECT_DIR / "tools" / "python-packages"))

import UnityPy  # noqa: E402
from UnityPy.helpers.TypeTreeGenerator import TypeTreeGenerator  # noqa: E402
from UnityPy.helpers.MeshHelper import MeshHandler  # noqa: E402


def pptr_info(ptr) -> dict[str, object]:
    info: dict[str, object] = {
        "file_id": getattr(ptr, "file_id", getattr(ptr, "m_FileID", None)),
        "path_id": getattr(ptr, "path_id", getattr(ptr, "m_PathID", None)),
    }
    try:
        obj = ptr.deref()
        if obj is not None:
            info["asset"] = obj.assets_file.name
            info["type"] = obj.type.name
            info["resolved_path_id"] = obj.path_id
    except Exception as exc:
        info["deref_error"] = repr(exc)
    return info


def object_name(obj) -> str:
    try:
        value = obj.read()
        return str(getattr(value, "m_Name", ""))
    except Exception:
        return ""


def game_object_name(component) -> str:
    try:
        return str(getattr(component.m_GameObject.read(), "m_Name", ""))
    except Exception:
        return ""


def transform_path(transform) -> str:
    names: list[str] = []
    current = transform
    visited: set[tuple[int, int]] = set()
    while current is not None:
        try:
            obj = current.object_reader
            key = (id(obj.assets_file), obj.path_id)
            if key in visited:
                names.append("<cycle>")
                break
            visited.add(key)
        except Exception:
            pass
        try:
            names.append(game_object_name(current) or "<unnamed>")
            parent_ptr = getattr(current, "m_Father", None)
            if parent_ptr is None or getattr(parent_ptr, "path_id", 0) == 0:
                break
            current = parent_ptr.read()
        except Exception:
            break
    return "/".join(reversed(names))


def vector(value) -> list[float] | None:
    if value is None:
        return None
    fields = [name for name in ("x", "y", "z", "w") if hasattr(value, name)]
    return [float(getattr(value, name)) for name in fields]


def describe_component(ptr) -> dict[str, object]:
    result = pptr_info(ptr)
    try:
        component = ptr.read()
    except Exception as exc:
        result["read_error"] = repr(exc)
        return result

    type_name = result.get("type", type(component).__name__)
    if type_name in {"Transform", "RectTransform"}:
        result.update(
            {
                "path": transform_path(component),
                "local_position": vector(getattr(component, "m_LocalPosition", None)),
                "local_rotation": vector(getattr(component, "m_LocalRotation", None)),
                "local_scale": vector(getattr(component, "m_LocalScale", None)),
                "father": pptr_info(getattr(component, "m_Father", None)),
                "children": [pptr_info(child) for child in getattr(component, "m_Children", [])],
            }
        )
    elif type_name == "MeshRenderer":
        result.update(
            {
                "enabled": bool(getattr(component, "m_Enabled", 0)),
                "materials": [
                    {**pptr_info(material), "name": object_name(material.deref()) if material.deref() else ""}
                    for material in getattr(component, "m_Materials", [])
                ],
            }
        )
    elif type_name == "MeshFilter":
        mesh_ptr = getattr(component, "m_Mesh", None)
        result["mesh"] = pptr_info(mesh_ptr)
        try:
            mesh = mesh_ptr.read()
            handler = MeshHandler(mesh)
            handler.process()
            result["mesh"].update(
                {
                    "name": getattr(mesh, "m_Name", ""),
                    "vertex_count": handler.m_VertexCount,
                    "vertices": handler.m_Vertices,
                    "uv0": handler.m_UV0,
                    "triangles": handler.get_triangles(),
                }
            )
        except Exception as exc:
            result["mesh"]["read_error"] = repr(exc)
    return result


def main() -> int:
    paths = [DATA_DIR / "level0", DATA_DIR / "sharedassets0.assets"]
    generator = TypeTreeGenerator("6000.0.73f1")
    generator.load_local_game(str(GAME_ROOT))
    env = UnityPy.load(*map(str, paths))
    env.typetree_generator = generator

    screens: list[dict[str, object]] = []
    for obj in env.objects:
        if obj.type.name != "GameObject":
            continue
        game_object = obj.read()
        if getattr(game_object, "m_Name", "") != "Screen":
            continue
        components = [describe_component(entry.component) for entry in game_object.m_Component]
        transform = next(
            (item for item in components if item.get("type") in {"Transform", "RectTransform"}),
            None,
        )
        screens.append(
            {
                "asset": obj.assets_file.name,
                "path_id": obj.path_id,
                "name": game_object.m_Name,
                "layer": game_object.m_Layer,
                "active": bool(game_object.m_IsActive),
                "path": transform.get("path") if transform else None,
                "components": components,
            }
        )

    output = {"screens": screens}
    output_path = PROJECT_DIR / "work" / "scene-screen-inspection.json"
    output_path.write_text(json.dumps(output, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps(output, ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
