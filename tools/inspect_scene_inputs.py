from __future__ import annotations

import json
import sys
from pathlib import Path


TOOLS_DIR = Path(__file__).resolve().parent
sys.path.insert(0, str(TOOLS_DIR))
from project_config import DATA_DIR, GAME_ROOT, PROJECT_DIR
from python_runtime import load_unitypy  # noqa: E402

UnityPy, TypeTreeGenerator = load_unitypy()


TARGET_CLASSES = {"TMP_InputField", "InputTextDummy", "NameTranslator", "ProgressLog"}


def read_ptr(ptr):
    try:
        return ptr.read()
    except Exception:
        return None


def component_class(component_ptr) -> str:
    try:
        obj = component_ptr.deref()
        if obj is None:
            return "<missing>"
        if obj.type.name != "MonoBehaviour":
            return obj.type.name
        component = obj.read()
        script = read_ptr(component.m_Script)
        return str(
            getattr(script, "m_ClassName", "")
            or getattr(script, "m_Name", "")
            or "MonoBehaviour"
        )
    except Exception as exc:
        return f"<error:{type(exc).__name__}>"


def transform_path(transform) -> str:
    names: list[str] = []
    visited: set[int] = set()
    current = transform
    while current is not None:
        try:
            reader = current.object_reader
            if reader.path_id in visited:
                names.append("<cycle>")
                break
            visited.add(reader.path_id)
            game_object = read_ptr(current.m_GameObject)
            names.append(str(getattr(game_object, "m_Name", "<unnamed>")))
            father = getattr(current, "m_Father", None)
            if father is None or getattr(father, "path_id", 0) == 0:
                break
            current = read_ptr(father)
        except Exception:
            break
    return "/".join(reversed(names))


def main() -> int:
    paths = [DATA_DIR / f"level{index}" for index in range(5)] + [
        DATA_DIR / f"sharedassets{index}.assets" for index in range(5)
    ]
    generator = TypeTreeGenerator("6000.0.73f1")
    generator.load_local_game(str(GAME_ROOT))
    env = UnityPy.load(*map(str, paths))
    env.typetree_generator = generator

    results: list[dict[str, object]] = []
    for obj in env.objects:
        if obj.type.name != "GameObject":
            continue
        game_object = obj.read()
        components: list[dict[str, object]] = []
        path = ""
        for entry in game_object.m_Component:
            ptr = entry.component
            class_name = component_class(ptr)
            component_obj = ptr.deref()
            components.append(
                {
                    "type": component_obj.type.name if component_obj else "<missing>",
                    "class": class_name,
                    "path_id": getattr(ptr, "path_id", 0),
                }
            )
            if class_name in {"Transform", "RectTransform"} and component_obj:
                path = transform_path(component_obj.read())

        target_classes = sorted(
            item["class"] for item in components if item["class"] in TARGET_CLASSES
        )
        lowered_name = str(game_object.m_Name).lower()
        interesting_name = any(
            marker in lowered_name
            for marker in ("input", "translator", "progress log", "text dummy")
        )
        if not target_classes and not interesting_name:
            continue
        results.append(
            {
                "asset": obj.assets_file.name,
                "game_object_path_id": obj.path_id,
                "path": path,
                "name": game_object.m_Name,
                "layer": game_object.m_Layer,
                "active": bool(game_object.m_IsActive),
                "target_classes": target_classes,
                "components": components,
            }
        )

    results.sort(key=lambda item: (item["asset"], item["path"]))
    output = {"input_objects": results}
    output_path = PROJECT_DIR / "work" / "scene-input-inspection.json"
    output_path.write_text(json.dumps(output, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps(output, ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
