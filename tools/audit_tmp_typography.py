from __future__ import annotations

import json
import re
import sys
from collections import defaultdict
from pathlib import Path
from typing import Any


TOOLS_DIR = Path(__file__).resolve().parent
sys.path.insert(0, str(TOOLS_DIR))
from project_config import DATA_DIR, GAME_ROOT, PROJECT_DIR
sys.path.insert(0, str(PROJECT_DIR / "tools" / "python-packages"))

import UnityPy  # noqa: E402
from UnityPy.helpers.TypeTreeGenerator import TypeTreeGenerator  # noqa: E402


def ptr_path_id(value: Any) -> int:
    if isinstance(value, dict):
        if "m_PathID" in value:
            return int(value.get("m_PathID", 0))
        if "component" in value:
            return ptr_path_id(value["component"])
    return 0


def scene_names() -> list[str]:
    data = (DATA_DIR / "globalgamemanagers").read_bytes()
    paths = re.findall(rb"Assets/[^\x00]{1,200}\.unity", data)
    return [Path(path.decode("utf-8")).stem for path in paths]


def clean_number(value: Any) -> float:
    return round(float(value or 0.0), 4)


def font_name(obj: Any) -> str:
    try:
        parsed = obj.parse_as_object()
        pointer = getattr(parsed, "m_fontAsset", None)
        if pointer is None or not pointer:
            return "<none>"
        target = pointer.read()
        return str(getattr(target, "m_Name", None) or f"<{target.__class__.__name__}>")
    except Exception as exc:
        return f"<unresolved:{exc.__class__.__name__}>"


def main() -> int:
    generator = TypeTreeGenerator("6000.0.73f1")
    generator.load_local_game(str(GAME_ROOT))
    scenes = scene_names()
    asset_paths = sorted(DATA_DIR.glob("level*")) + sorted(DATA_DIR.glob("*.assets"))
    rows: list[dict[str, Any]] = []
    failures: list[dict[str, Any]] = []
    recoveries: list[dict[str, Any]] = []

    for asset_path in asset_paths:
        env = UnityPy.load(str(asset_path))
        env.typetree_generator = generator
        parsed: dict[int, tuple[str, dict[str, Any], Any]] = {}
        for obj in env.objects:
            if obj.type.name not in {"GameObject", "Transform", "RectTransform", "MonoBehaviour"}:
                continue
            try:
                tree = obj.parse_as_dict()
                parsed[obj.path_id] = (obj.type.name, tree, obj)
            except Exception as exc:
                if obj.type.name == "MonoBehaviour":
                    try:
                        tree = obj.parse_as_dict(check_read=False)
                        parsed[obj.path_id] = (obj.type.name, tree, obj)
                        recoveries.append(
                            {"asset": asset_path.name, "path_id": obj.path_id, "strict_error": repr(exc)}
                        )
                        continue
                    except Exception as loose_exc:
                        failures.append(
                            {
                                "asset": asset_path.name,
                                "path_id": obj.path_id,
                                "error": repr(loose_exc),
                                "strict_error": repr(exc),
                            }
                        )
                else:
                    failures.append({"asset": asset_path.name, "path_id": obj.path_id, "error": repr(exc)})

        game_objects = {
            path_id: tree
            for path_id, (type_name, tree, _) in parsed.items()
            if type_name == "GameObject"
        }
        transforms = {
            path_id: tree
            for path_id, (type_name, tree, _) in parsed.items()
            if type_name in {"Transform", "RectTransform"}
        }
        transform_to_go = {
            transform_id: ptr_path_id(tree.get("m_GameObject"))
            for transform_id, tree in transforms.items()
        }
        go_to_transform: dict[int, int] = {}
        component_indexes: dict[int, int] = {}
        for go_id, go in game_objects.items():
            for component_index, component in enumerate(go.get("m_Component", [])):
                component_id = ptr_path_id(component)
                component_indexes[component_id] = component_index
                if component_id in transforms:
                    go_to_transform[go_id] = component_id

        sibling_indexes: dict[int, int] = {}
        for transform in transforms.values():
            for child_index, child in enumerate(transform.get("m_Children", [])):
                sibling_indexes[ptr_path_id(child)] = child_index

        path_cache: dict[int, str] = {}

        def hierarchy_path(go_id: int, active: set[int] | None = None) -> str:
            if go_id in path_cache:
                return path_cache[go_id]
            if active is None:
                active = set()
            if go_id in active:
                return f"<cycle:{go_id}>"
            active.add(go_id)
            go = game_objects.get(go_id, {})
            name = str(go.get("m_Name", f"<GameObject:{go_id}>"))
            transform_id = go_to_transform.get(go_id, 0)
            transform = transforms.get(transform_id, {})
            father_id = ptr_path_id(transform.get("m_Father"))
            segment = name
            if father_id:
                segment += f"[{sibling_indexes.get(transform_id, 0)}]"
                parent_go = transform_to_go.get(father_id, 0)
                result = f"{hierarchy_path(parent_go, active)}/{segment}"
            else:
                result = segment
            active.remove(go_id)
            path_cache[go_id] = result
            return result

        if asset_path.name.startswith("level") and asset_path.name[5:].isdigit():
            build_index = int(asset_path.name[5:])
            scope = scenes[build_index] if build_index < len(scenes) else asset_path.name
        else:
            scope = asset_path.name

        for path_id, (type_name, tree, obj) in parsed.items():
            if type_name != "MonoBehaviour" or not isinstance(tree.get("m_text"), str):
                continue
            go_id = ptr_path_id(tree.get("m_GameObject"))
            rows.append(
                {
                    "asset": asset_path.name,
                    "scope": scope,
                    "path_id": path_id,
                    "object_path": hierarchy_path(go_id) if go_id else "<no-game-object>",
                    "component_index": component_indexes.get(path_id, -1),
                    "text": tree.get("m_text", ""),
                    "font": font_name(obj),
                    "font_size": clean_number(tree.get("m_fontSize")),
                    "font_size_base": clean_number(tree.get("m_fontSizeBase")),
                    "auto_size": bool(tree.get("m_enableAutoSizing", 0)),
                    "font_size_min": clean_number(tree.get("m_fontSizeMin")),
                    "font_size_max": clean_number(tree.get("m_fontSizeMax")),
                }
            )

    grouped: dict[tuple[float, str, bool, float, float], list[dict[str, Any]]] = defaultdict(list)
    for row in rows:
        key = (
            row["font_size"],
            row["font"],
            row["auto_size"],
            row["font_size_min"],
            row["font_size_max"],
        )
        grouped[key].append(row)

    summary = []
    for key, items in sorted(grouped.items(), key=lambda pair: (pair[0][0], pair[0][1])):
        font_size, font, auto_size, font_min, font_max = key
        examples = []
        seen = set()
        for item in items:
            label = f"{item['scope']}:{item['object_path']}"
            if label in seen:
                continue
            seen.add(label)
            examples.append({"location": label, "text": item["text"][:100]})
            if len(examples) == 6:
                break
        summary.append(
            {
                "font_size": font_size,
                "font": font,
                "auto_size": auto_size,
                "font_size_min": font_min,
                "font_size_max": font_max,
                "count": len(items),
                "scopes": sorted({item["scope"] for item in items}),
                "examples": examples,
            }
        )

    output = {
        "game_version": "0.10",
        "tmp_text_components": len(rows),
        "distinct_font_sizes": sorted({row["font_size"] for row in rows}),
        "distinct_fonts": sorted({row["font"] for row in rows}),
        "parse_recoveries": recoveries,
        "parse_failures": failures,
        "summary": summary,
        "rows": rows,
    }
    output_path = PROJECT_DIR / "work" / "tmp-typography-audit.json"
    output_path.write_text(json.dumps(output, ensure_ascii=False, indent=2), encoding="utf-8")
    print(
        json.dumps(
            {
                "output": str(output_path),
                "tmp_text_components": len(rows),
                "distinct_font_sizes": output["distinct_font_sizes"],
                "distinct_fonts": output["distinct_fonts"],
                "summary_groups": len(summary),
                "parse_recoveries": len(recoveries),
                "parse_failures": len(failures),
            },
            ensure_ascii=False,
            indent=2,
        )
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
