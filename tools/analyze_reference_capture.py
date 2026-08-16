from __future__ import annotations

import argparse
import json
import re
from pathlib import Path
from typing import Any


def overlap_area(left: dict[str, float], right: dict[str, float]) -> float:
    width = max(0.0, min(left["right"], right["right"]) - max(left["left"], right["left"]))
    height = max(0.0, min(left["top"], right["top"]) - max(left["bottom"], right["bottom"]))
    return width * height


def overlap_height(left: dict[str, float], right: dict[str, float]) -> float:
    return max(0.0, min(left["top"], right["top"]) - max(left["bottom"], right["bottom"]))


def point_to_line_end(point: dict[str, float], line: dict[str, Any]) -> float:
    rect = line["screen_rect"]
    center_y = (rect["top"] + rect["bottom"]) * 0.5
    return ((point["x"] - rect["right"]) ** 2 + (point["y"] - center_y) ** 2) ** 0.5


def rect_center_y(rect: dict[str, float]) -> float:
    return (rect["top"] + rect["bottom"]) * 0.5


def horizontal_gap(left: dict[str, float], right: dict[str, float]) -> float:
    if left["right"] <= right["left"]:
        return right["left"] - left["right"]
    if right["right"] <= left["left"]:
        return left["left"] - right["right"]
    return -min(left["right"], right["right"]) + max(left["left"], right["left"])


def analyze(manifest: dict[str, Any]) -> dict[str, Any]:
    findings: list[dict[str, Any]] = []
    paired_capture_count = 0
    unpaired_captures: list[dict[str, Any]] = []
    checked_dynamic_values: set[tuple[str, str]] = set()
    for page in manifest.get("pages", []):
        captures = page.get("captures", [])
        indexed = {
            (capture.get("language", "zh"), round(float(capture.get("requested_scroll", 0)), 5)): capture
            for capture in captures
        }
        for scroll in sorted({key[1] for key in indexed}):
            translated = indexed.get(("zh", scroll))
            original = indexed.get(("en", scroll))
            if not translated or not original:
                unpaired_captures.append({"page": page.get("name"), "scroll": scroll})
                continue
            paired_capture_count += 1
            translated_by_id = {
                element.get("instance_id"): element
                for element in translated.get("elements", [])
                if element.get("instance_id") is not None
            }
            original_by_id = {
                element.get("instance_id"): element
                for element in original.get("elements", [])
                if element.get("instance_id") is not None
            }
            translated_texts = [
                element for element in translated_by_id.values()
                if element.get("kind") == "text" and element.get("screen_rect")
            ]
            translated_graphics = [
                element for element in translated_by_id.values()
                if element.get("kind") == "graphic" and element.get("screen_rect")
            ]
            translated_text_paths = {
                element.get("path") for element in translated_texts if element.get("path")
            }
            translated_button_paths = {
                element.get("path") for element in translated_by_id.values()
                if element.get("kind") == "copy-button" and element.get("path")
            }
            # Alien Units contains labels assembled at runtime. A stale reference-page
            # refresh used to replace the generated value with the prefab prefix in both
            # languages, leaving "1 HELISEC =" blank. Treat that as data loss rather than
            # a visual-layout issue so the real-game capture gate cannot miss it again.
            if str(page.get("name", "")).casefold() == "alien units":
                for language, capture, elements in (
                    ("zh", translated, translated_by_id),
                    ("en", original, original_by_id),
                ):
                    check_key = (str(page.get("name")), language)
                    if check_key in checked_dynamic_values:
                        continue
                    checked_dynamic_values.add(check_key)
                    helisec = next(
                        (
                            element for element in elements.values()
                            if element.get("kind") == "text"
                            and str(element.get("path", "")).casefold().endswith("/helisec")
                        ),
                        None,
                    )
                    rendered = re.sub(r"<[^>]+>", "", str((helisec or {}).get("text", "")))
                    if "0.8066" not in rendered:
                        findings.append({
                            "kind": "dynamic_value_missing",
                            "page": page.get("name"),
                            "language": language,
                            "screenshot": capture.get("screenshot"),
                            "text_path": (helisec or {}).get("path"),
                            "text": rendered,
                            "expected_fragment": "0.8066",
                        })
            # If two independent text blocks were authored side by side on the same row,
            # translating one block must not turn the pair into a vertical sequence. This
            # catches the 3D-shapes regression where KEY was pushed below the introduction
            # and consequently moved every following graphic down by an entire screen.
            for text_index, left in enumerate(translated_texts):
                original_left = original_by_id.get(left.get("instance_id"))
                if not original_left or not original_left.get("screen_rect"):
                    continue
                for right in translated_texts[text_index + 1:]:
                    original_right = original_by_id.get(right.get("instance_id"))
                    if not original_right or not original_right.get("screen_rect"):
                        continue
                    original_left_rect = original_left["screen_rect"]
                    original_right_rect = original_right["screen_rect"]
                    translated_left_rect = left["screen_rect"]
                    translated_right_rect = right["screen_rect"]
                    # Authored reference pages commonly place a narrow KEY column beside a
                    # wider introduction. Their glyph rectangles can touch (or overlap by a
                    # few pixels) even though the objects are distinct columns, so a positive
                    # rectangle gap is not a reliable discriminator. Treat a large difference
                    # in left anchors as a side-by-side layout as well.
                    separated_columns = (
                        horizontal_gap(original_left_rect, original_right_rect) >= 8.0
                        or abs(original_left_rect["left"] - original_right_rect["left"]) >= 180.0
                    )
                    if not separated_columns:
                        continue
                    if overlap_height(original_left_rect, original_right_rect) < 20.0:
                        continue
                    left_shift = rect_center_y(translated_left_rect) - rect_center_y(original_left_rect)
                    right_shift = rect_center_y(translated_right_rect) - rect_center_y(original_right_rect)
                    if abs(left_shift - right_shift) < 60.0:
                        continue
                    findings.append({
                        "kind": "side_by_side_block_vertical_drift",
                        "page": page.get("name"),
                        "zh_screenshot": translated.get("screenshot"),
                        "en_screenshot": original.get("screenshot"),
                        "left_path": left.get("path"),
                        "right_path": right.get("path"),
                        "left_shift_y": round(left_shift, 2),
                        "right_shift_y": round(right_shift, 2),
                    })
            # Detect line collisions within a single TMP object. These are invisible to the
            # object-level rectangle test and caused several small-print regressions.
            for text in translated_texts:
                original_text = original_by_id.get(text.get("instance_id"))
                if not original_text:
                    continue
                zh_lines = text.get("lines") or []
                en_lines = original_text.get("lines") or []
                for line_index in range(min(len(zh_lines), len(en_lines)) - 1):
                    zh_overlap = overlap_area(
                        zh_lines[line_index]["screen_rect"],
                        zh_lines[line_index + 1]["screen_rect"],
                    )
                    en_overlap = overlap_area(
                        en_lines[line_index]["screen_rect"],
                        en_lines[line_index + 1]["screen_rect"],
                    )
                    zh_height = overlap_height(
                        zh_lines[line_index]["screen_rect"],
                        zh_lines[line_index + 1]["screen_rect"],
                    )
                    en_height = overlap_height(
                        en_lines[line_index]["screen_rect"],
                        en_lines[line_index + 1]["screen_rect"],
                    )
                    # TMP glyph bounds commonly overlap by one or two anti-aliased pixels
                    # even when the lines are visually separate. Only flag a materially new
                    # vertical collision; area alone produces hundreds of false positives.
                    if zh_height - en_height < 4.0:
                        continue
                    findings.append({
                        "kind": "new_intra_text_line_overlap",
                        "page": page.get("name"),
                        "zh_screenshot": translated.get("screenshot"),
                        "en_screenshot": original.get("screenshot"),
                        "text": text.get("text", "")[:120],
                        "text_path": text.get("path"),
                        "line_index": line_index,
                        "zh_overlap_area": round(zh_overlap, 2),
                        "en_overlap_area": round(en_overlap, 2),
                        "zh_overlap_height": round(zh_height, 2),
                        "en_overlap_height": round(en_height, 2),
                    })
            for text in translated_texts:
                original_text = original_by_id.get(text.get("instance_id"))
                if not original_text or not original_text.get("screen_rect"):
                    continue
                for graphic in translated_graphics:
                    text_path = text.get("path") or ""
                    graphic_path = graphic.get("path") or ""
                    # TMP creates child renderer objects for underlines/decorations. They are
                    # part of the text itself, not an independent image that can obscure it.
                    if text_path and graphic_path.startswith(text_path + "/"):
                        continue
                    parent_path = graphic_path.rsplit("/", 1)[0] if "/" in graphic_path else ""
                    if parent_path in translated_text_paths:
                        continue
                    if any(
                        graphic_path == button_path or graphic_path.startswith(button_path + "/")
                        for button_path in translated_button_paths
                    ):
                        continue
                    original_graphic = original_by_id.get(graphic.get("instance_id"))
                    if not original_graphic or not original_graphic.get("screen_rect"):
                        continue
                    translated_overlap = overlap_area(text["screen_rect"], graphic["screen_rect"])
                    original_overlap = overlap_area(
                        original_text["screen_rect"], original_graphic["screen_rect"]
                    )
                    minimum_delta = max(64.0, original_overlap * 0.08)
                    if translated_overlap - original_overlap < minimum_delta:
                        continue
                    findings.append(
                        {
                            "kind": "new_text_graphic_overlap",
                            "page": page.get("name"),
                            "zh_screenshot": translated.get("screenshot"),
                            "en_screenshot": original.get("screenshot"),
                            "text": text.get("text", "")[:120],
                            "text_path": text.get("path"),
                            "graphic_path": graphic.get("path"),
                            "zh_overlap_area": round(translated_overlap, 2),
                            "en_overlap_area": round(original_overlap, 2),
                        }
                    )
            for button_id, translated_button in translated_by_id.items():
                if translated_button.get("kind") != "copy-button" or not translated_button.get("screen_point"):
                    continue
                original_button = original_by_id.get(button_id)
                text_id = translated_button.get("anchor_text_instance_id")
                if text_id is None:
                    findings.append({
                        "kind": "copy_button_missing_anchor",
                        "page": page.get("name"),
                        "zh_screenshot": translated.get("screenshot"),
                        "en_screenshot": original.get("screenshot"),
                        "button_path": translated_button.get("path"),
                        "button_name": translated_button.get("name"),
                        "copy_value": translated_button.get("copy_value"),
                    })
                    continue
                if not original_button or not original_button.get("screen_point"):
                    continue
                original_text = original_by_id.get(text_id)
                translated_text = translated_by_id.get(text_id)
                if not original_text or not translated_text:
                    continue
                logical_line = original_button.get("anchor_logical_line")
                wrap_index = original_button.get("anchor_wrap_index")
                original_matching = [
                    line for line in (original_text.get("lines") or [])
                    if line.get("logical_line") == logical_line
                ]
                matching = [
                    line for line in (translated_text.get("lines") or [])
                    if line.get("logical_line") == logical_line
                ]
                if not original_matching or not matching:
                    continue
                original_line = original_matching[min(max(int(wrap_index or 0), 0), len(original_matching) - 1)]
                translated_line = matching[min(max(int(wrap_index or 0), 0), len(matching) - 1)]
                en_distance = point_to_line_end(original_button["screen_point"], original_line)
                zh_distance = point_to_line_end(translated_button["screen_point"], translated_line)
                if zh_distance - en_distance < 28.0:
                    continue
                findings.append({
                    "kind": "copy_button_line_misalignment",
                    "page": page.get("name"),
                    "zh_screenshot": translated.get("screenshot"),
                    "en_screenshot": original.get("screenshot"),
                    "button_path": translated_button.get("path"),
                    "text_path": translated_text.get("path"),
                    "zh_distance": round(zh_distance, 2),
                    "en_distance": round(en_distance, 2),
                })
            for text_index, left in enumerate(translated_texts):
                original_left = original_by_id.get(left.get("instance_id"))
                if not original_left or not original_left.get("screen_rect"):
                    continue
                for right in translated_texts[text_index + 1:]:
                    original_right = original_by_id.get(right.get("instance_id"))
                    if not original_right or not original_right.get("screen_rect"):
                        continue
                    translated_overlap = overlap_area(left["screen_rect"], right["screen_rect"])
                    original_overlap = overlap_area(
                        original_left["screen_rect"], original_right["screen_rect"]
                    )
                    translated_height = overlap_height(left["screen_rect"], right["screen_rect"])
                    original_height = overlap_height(
                        original_left["screen_rect"], original_right["screen_rect"]
                    )
                    # Vertical projection overlap alone is expected for independent
                    # side-by-side columns. Only report an actual 2D collision that is
                    # materially larger than the corresponding English overlap.
                    if translated_overlap - original_overlap < 64.0:
                        continue
                    if translated_height - original_height < 4.0:
                        continue
                    findings.append(
                        {
                            "kind": "new_text_text_overlap",
                            "page": page.get("name"),
                            "zh_screenshot": translated.get("screenshot"),
                            "en_screenshot": original.get("screenshot"),
                            "left_text": left.get("text", "")[:80],
                            "right_text": right.get("text", "")[:80],
                            "zh_overlap_area": round(translated_overlap, 2),
                            "en_overlap_area": round(original_overlap, 2),
                            "zh_overlap_height": round(translated_height, 2),
                            "en_overlap_height": round(original_height, 2),
                        }
                    )
    slow_pages = [
        {
            "page": page.get("name"),
            "milliseconds": round(page.get("open_and_stabilize_ms", 0.0), 2),
        }
        for page in manifest.get("pages", [])
        if page.get("open_and_stabilize_ms", 0.0) >= 150.0
    ]
    return {
        "page_count": len(manifest.get("pages", [])),
        "capture_count": sum(len(page.get("captures", [])) for page in manifest.get("pages", [])),
        "paired_capture_count": paired_capture_count,
        "unpaired_capture_count": len(unpaired_captures),
        "unpaired_captures": unpaired_captures,
        "finding_count": len(findings),
        "findings": findings,
        "slow_page_count": len(slow_pages),
        "slow_pages": slow_pages,
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="分析真实游戏参考页截图的布局矩形。")
    parser.add_argument("manifest", type=Path)
    parser.add_argument("--output", type=Path)
    parser.add_argument("--fail-on-findings", action="store_true")
    args = parser.parse_args()
    manifest = json.loads(args.manifest.read_text(encoding="utf-8-sig"))
    report = analyze(manifest)
    output = args.output or args.manifest.with_name("report.json")
    output.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps({**report, "findings": report["findings"][:10]}, ensure_ascii=False, indent=2))
    return 1 if args.fail_on_findings and report["finding_count"] else 0


if __name__ == "__main__":
    raise SystemExit(main())
