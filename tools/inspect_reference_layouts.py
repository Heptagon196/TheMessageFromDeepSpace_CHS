from __future__ import annotations

import argparse
import json
import sys
from collections import Counter
from pathlib import Path
from typing import Any


TOOLS_DIR = Path(__file__).resolve().parent
sys.path.insert(0, str(TOOLS_DIR))
from project_config import DATA_DIR, GAME_ROOT, PROJECT_DIR
from python_runtime import load_unitypy

UnityPy, TypeTreeGenerator = load_unitypy()


def ptr_path_id(value: Any) -> int:
    if isinstance(value, dict):
        for key in ("m_PathID", "path_id"):
            if key in value:
                return int(value[key])
        if "component" in value:
            return ptr_path_id(value["component"])
    return 0


def rect_summary(tree: dict[str, Any]) -> dict[str, Any]:
    return {
        key: tree.get(key)
        for key in (
            "m_AnchorMin",
            "m_AnchorMax",
            "m_AnchoredPosition",
            "m_SizeDelta",
            "m_Pivot",
            "m_LocalPosition",
            "m_LocalScale",
        )
        if key in tree
    }


def mono_summary(tree: dict[str, Any]) -> dict[str, Any]:
    summary: dict[str, Any] = {}
    for key in (
        "m_text",
        "m_fontSize",
        "m_fontSizeMin",
        "m_fontSizeMax",
        "m_enableAutoSizing",
        "m_lineSpacing",
        "m_paragraphSpacing",
        "m_margin",
        "m_textAlignment",
        "copyValue",
        "copyText",
        "textToCopy",
        "stringToCopy",
        "m_Sprite",
        "m_Color",
    ):
        if key in tree:
            summary[key] = tree[key]
    summary["keys"] = sorted(tree.keys())
    return summary


def main() -> int:
    parser = argparse.ArgumentParser(description="导出全部参考页的层级、RectTransform 和组件布局数据。")
    parser.add_argument("--page", help="只保留名称包含此文本的参考页。")
    parser.add_argument(
        "--output",
        type=Path,
        default=PROJECT_DIR / "work" / "reference-layouts.json",
    )
    args = parser.parse_args()

    asset_path = DATA_DIR / "level0"
    generator = TypeTreeGenerator("6000.0.73f1")
    generator.load_local_game(str(GAME_ROOT))
    env = UnityPy.load(str(asset_path))
    env.typetree_generator = generator

    parsed: dict[int, tuple[str, dict[str, Any]]] = {}
    failures: list[dict[str, Any]] = []
    for obj in env.objects:
        if obj.type.name not in {
            "GameObject",
            "Transform",
            "RectTransform",
            "MonoBehaviour",
            "SpriteRenderer",
        }:
            continue
        try:
            parsed[obj.path_id] = (obj.type.name, obj.parse_as_dict())
        except Exception as exc:
            failures.append({"path_id": obj.path_id, "type": obj.type.name, "error": repr(exc)})

    game_objects = {
        path_id: tree for path_id, (type_name, tree) in parsed.items() if type_name == "GameObject"
    }
    transforms = {
        path_id: tree
        for path_id, (type_name, tree) in parsed.items()
        if type_name in {"Transform", "RectTransform"}
    }
    transform_to_go = {
        transform_id: ptr_path_id(tree.get("m_GameObject"))
        for transform_id, tree in transforms.items()
    }
    go_to_transform: dict[int, int] = {}
    go_components: dict[int, list[int]] = {}
    component_go: dict[int, int] = {}
    for go_id, game_object in game_objects.items():
        component_ids = [ptr_path_id(value) for value in game_object.get("m_Component", [])]
        go_components[go_id] = component_ids
        for component_id in component_ids:
            component_go[component_id] = go_id
            if component_id in transforms:
                go_to_transform[go_id] = component_id

    sibling_indexes: dict[int, int] = {}
    for transform in transforms.values():
        for index, child in enumerate(transform.get("m_Children", [])):
            sibling_indexes[ptr_path_id(child)] = index

    path_cache: dict[int, str] = {}

    def hierarchy_path(go_id: int) -> str:
        if go_id in path_cache:
            return path_cache[go_id]
        game_object = game_objects.get(go_id, {})
        name = str(game_object.get("m_Name", f"<GameObject:{go_id}>"))
        transform_id = go_to_transform.get(go_id, 0)
        transform = transforms.get(transform_id, {})
        father_id = ptr_path_id(transform.get("m_Father"))
        if father_id:
            parent_go = transform_to_go.get(father_id, 0)
            result = f"{hierarchy_path(parent_go)}/{name}[{sibling_indexes.get(transform_id, 0)}]"
        else:
            result = name
        path_cache[go_id] = result
        return result

    reference_go = next(
        (
            go_id
            for go_id, game_object in game_objects.items()
            if game_object.get("m_Name") == "Reference Window"
        ),
        0,
    )
    if not reference_go:
        raise RuntimeError("Reference Window not found in level0")
    reference_transform = go_to_transform[reference_go]
    reference_prefix = hierarchy_path(reference_go) + "/"

    pages: dict[str, list[dict[str, Any]]] = {}
    component_types: Counter[str] = Counter()
    for go_id, game_object in game_objects.items():
        path = hierarchy_path(go_id)
        if not path.startswith(reference_prefix):
            continue
        relative = path[len(reference_prefix) :]
        page_segment = relative.split("/", 1)[0]
        page_name = page_segment.rsplit("[", 1)[0]
        if args.page and args.page.casefold() not in page_name.casefold():
            continue
        transform_id = go_to_transform.get(go_id, 0)
        components: list[dict[str, Any]] = []
        for component_index, component_id in enumerate(go_components.get(go_id, [])):
            if component_id == transform_id:
                continue
            type_name, tree = parsed.get(component_id, ("Unknown", {}))
            component_types[type_name] += 1
            component = {
                "component_index": component_index,
                "path_id": component_id,
                "type": type_name,
            }
            if type_name == "MonoBehaviour":
                component.update(mono_summary(tree))
            elif type_name == "SpriteRenderer":
                component.update(
                    {
                        key: tree.get(key)
                        for key in ("m_Sprite", "m_Color", "m_Size", "m_DrawMode")
                        if key in tree
                    }
                )
            components.append(component)
        pages.setdefault(page_name, []).append(
            {
                "path": relative,
                "game_object_path_id": go_id,
                "active": game_object.get("m_IsActive"),
                "transform_path_id": transform_id,
                "transform_type": parsed.get(transform_id, ("", {}))[0],
                "rect": rect_summary(transforms.get(transform_id, {})),
                "components": components,
            }
        )

    summaries: dict[str, Any] = {}
    for page_name, elements in sorted(pages.items()):
        area_prefixes = sorted({
            element["path"].rsplit("/", 1)[0]
            for element in elements
            if element["path"].split("/")[-1].startswith("Area[")
        })
        direct_area_children = []
        for element in elements:
            relative = element["path"]
            if "/Area[" not in relative or relative.count("/") != 2:
                continue
            texts = [
                component["m_text"]
                for component in element["components"]
                if "m_text" in component
            ]
            direct_area_children.append({
                "path": relative,
                "texts": texts,
                "max_logical_lines": max(
                    (value.count("\n") + 1 for value in texts), default=0
                ),
                "has_copy_button": any(
                    "stringToCopy" in component for component in element["components"]
                ),
            })
        summaries[page_name] = {
            "area_count": len(area_prefixes),
            "direct_area_child_count": len(direct_area_children),
            "direct_area_children": direct_area_children,
        }

    output = {
        "asset": str(asset_path),
        "reference_transform_path_id": reference_transform,
        "page_count": len(pages),
        "component_types": dict(component_types),
        "parse_failures": failures,
        "summaries": summaries,
        "pages": {name: elements for name, elements in sorted(pages.items())},
    }
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(output, ensure_ascii=False, indent=2), encoding="utf-8")
    print(
        json.dumps(
            {
                "output": str(args.output),
                "page_count": len(pages),
                "element_count": sum(len(elements) for elements in pages.values()),
                "component_types": dict(component_types),
                "parse_failure_count": len(failures),
            },
            ensure_ascii=False,
            indent=2,
        )
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
