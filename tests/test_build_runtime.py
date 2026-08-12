from __future__ import annotations

import importlib.util
import json
import re
import sys
from copy import deepcopy
from pathlib import Path


PROJECT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(PROJECT / "tools"))
spec = importlib.util.spec_from_file_location(
    "build_runtime", PROJECT / "tools" / "build_runtime.py"
)
assert spec and spec.loader
build_runtime = importlib.util.module_from_spec(spec)
spec.loader.exec_module(build_runtime)

cache = json.loads((PROJECT / "work" / "cache.json").read_text(encoding="utf-8"))
dialogue = next(
    item
    for item in build_runtime.iter_items(cache)
    if item.get("extra", {}).get("game", {}).get("kind") == "dialogue_frame"
    and item.get("translation_status") in (1, 2)
    and not build_runtime.validate_item(item)
)

valid = deepcopy(dialogue)
assert build_runtime.validate_item(valid) == [], "合法译文不应被拒绝"
assert build_runtime.category_for("dialogue_frame") == "dialogue"

assert build_runtime.should_translate_display_values(
    {"kind": "ui_template", "template_id": "puzzle-group"}
)
assert not build_runtime.should_translate_display_values(
    {"kind": "ui_template", "template_id": "save-path"}
)
assert build_runtime.should_translate_display_values(
    {
        "kind": "ui_text",
        "object_path": "Progress Log (Canvas) (start inactive)/FULL PROGRESS LOG (start inactive)[1]/TRANSMISSION GROUP LOG[6]",
    }
)
assert not build_runtime.should_translate_display_values(
    {"kind": "ui_text", "object_path": "Menu Window/Save Path"}
)

invalid = deepcopy(valid)
invalid["translated_text"] = invalid["translated_text"].replace("{PART_000}", "", 1)
errors = build_runtime.validate_item(invalid)
assert any("part" in error.lower() for error in errors), "缺少 PART 标记必须被拒绝"

signal_item = deepcopy(valid)
signal_item["source_text"] = "{SPEAKER_AKERS}{PART_000}Test {SIG_N160}"
signal_item["extra"]["game"]["source_sha256"] = build_runtime.sha256_text(signal_item["source_text"])
signal_item["extra"]["game"]["part_count"] = 1
signal_item["translated_text"] = "{SPEAKER_AKERS}{PART_000}测试 {SIG_N160}"
assert build_runtime.validate_item(signal_item) == [], "负数信号占位符应通过校验"
signal_item["translated_text"] = signal_item["translated_text"].replace("{SIG_N160}", "含义")
errors = build_runtime.validate_item(signal_item)
assert any("signal" in error.lower() for error in errors), "翻译外星信号必须被拒绝"

template_item = deepcopy(valid)
template_item["source_text"] = "Transmission: {DYN_0}, Time: {DYN_1}"
template_item["extra"]["game"]["source_sha256"] = build_runtime.sha256_text(
    template_item["source_text"]
)
template_item["extra"]["game"]["kind"] = "ui_template"
template_item["translated_text"] = "传输：{DYN_0}，时间：{DYN_1}"
assert build_runtime.validate_item(template_item) == [], "动态 UI 参数应通过校验"
template_item["translated_text"] = "传输完成"
errors = build_runtime.validate_item(template_item)
assert any("dynamic" in error.lower() for error in errors), "丢失 DYN 参数必须被拒绝"

nested_frame = deepcopy(valid)
nested_frame["extra"]["game"]["kind"] = "component_dialogue_frame"
assert build_runtime.validate_item(nested_frame) == [], "嵌套对白帧应使用对白结构校验"

ascii_dialogue_ellipsis = deepcopy(valid)
ascii_dialogue_ellipsis["translated_text"] += "..."
errors = build_runtime.validate_item(ascii_dialogue_ellipsis)
assert any("省略号" in error for error in errors), "对白中的 ASCII 三点必须被拒绝"

single_dialogue_ellipsis = deepcopy(valid)
single_dialogue_ellipsis["translated_text"] += "…"
errors = build_runtime.validate_item(single_dialogue_ellipsis)
assert any("成对" in error for error in errors), "对白中的单个 U+2026 必须被拒绝"

paired_dialogue_ellipsis = deepcopy(valid)
paired_dialogue_ellipsis["translated_text"] += "……"
assert build_runtime.validate_item(paired_dialogue_ellipsis) == [], (
    "规范的中文双省略号应通过对白校验"
)

duplicate_punctuation = deepcopy(valid)
duplicate_punctuation["translated_text"] += "。。"
errors = build_runtime.validate_item(duplicate_punctuation)
assert any("重复中文标点" in error for error in errors), (
    "所有运行时译文都必须经过重复标点校验"
)

journal_items = [
    item
    for item in build_runtime.iter_items(cache)
    if str(item.get("extra", {}).get("game", {}).get("chunk_name", "")).startswith(
        "Journal Entries #"
    )
    and item.get("translation_status") in (1, 2)
]
assert len(journal_items) == 265, "应完整审计全部 265 条已翻译角色手记"
assert not [
    (
        item.get("extra", {}).get("game", {}).get("stable_key"),
        build_runtime.validate_journal_layout(item),
    )
    for item in journal_items
    if build_runtime.validate_journal_layout(item)
], "所有超容量角色手记都必须按角色字号显式缩放"

akers_journal = next(
    item
    for item in journal_items
    if item.get("extra", {}).get("game", {}).get("stable_key")
    == "dialogue:57/frame:0"
)
unscaled_akers_journal = deepcopy(akers_journal)
unscaled_akers_journal["translated_text"] = re.sub(
    r"</?size=[^>]+>|</size>", "", unscaled_akers_journal["translated_text"]
)
errors = build_runtime.validate_journal_layout(unscaled_akers_journal)
assert any("角色手记超过版面容量" in error for error in errors), (
    "埃克斯的 84 px 手记必须使用独立容量阈值，不能按其他角色的 72 px 漏放"
)

legacy_system_ellipsis = [
    item
    for item in build_runtime.iter_items(cache)
    if item.get("translation_status") in (1, 2)
    and (
        item.get("extra", {}).get("game", {}).get("kind") == "component_string"
        or (
            item.get("extra", {}).get("game", {}).get("kind")
            == "component_dialogue_frame"
            and item.get("extra", {}).get("game", {}).get("field_path")
            == "autoLogStartFrame"
        )
    )
    and "…" in (item.get("translated_text") or "")
]
assert not legacy_system_ellipsis, (
    "旧系统字体文本中的 U+2026 会在游戏内乱码成 à；请改用 ASCII 三点省略号 ...："
    + repr([item.get("text_index") for item in legacy_system_ellipsis])
)

dialogue_ellipsis = [
    item
    for item in build_runtime.iter_items(cache)
    if item.get("translation_status") in (1, 2)
    and item.get("extra", {}).get("game", {}).get("kind")
    == "component_dialogue_frame"
    and item.get("extra", {}).get("game", {}).get("field_path")
    != "autoLogStartFrame"
    and "…" in (item.get("translated_text") or "")
]
assert dialogue_ellipsis, "非启动日志的内嵌对白应继续保留能正常显示的中文省略号"

def is_ellipsis_only_dialogue(item: dict) -> bool:
    visible = item.get("source_text") or ""
    for token_name in ("speaker", "part", "signal", "player", "dynamic", "animation", "tmp_tag"):
        visible = build_runtime.TOKEN_PATTERNS[token_name].sub("", visible)
    return re.fullmatch(r"\.{3,}", visible.strip()) is not None


unlocalized_ellipsis_only_dialogue = [
    item
    for item in build_runtime.iter_items(cache)
    if item.get("extra", {}).get("game", {}).get("kind") == "dialogue_frame"
    and item.get("translation_status") not in (1, 2)
    and is_ellipsis_only_dialogue(item)
]
assert not unlocalized_ellipsis_only_dialogue, (
    "纯省略号对白也必须进入汉化，不能因不含英文字母而漏掉："
    + repr([
        item.get("extra", {}).get("game", {}).get("stable_key")
        for item in unlocalized_ellipsis_only_dialogue
    ])
)

console_expected = {
    "system:ControlRoom:Console Message:component:3:field:correctInput":
        "检测到新信号！！",
    "system:ControlRoom:Console Message:component:3:field:loadingSignalMsg":
        "正在读取信号",
    "system:ControlRoom:Console Message:component:3:field:recompilingMsg":
        "正在重新编译",
    "system:ControlRoom:Console Message:component:3:field:sendingSignalMsg":
        "正在发送信号",
    "system:ControlRoom:Console Message:component:3:field:tokenizingSignalMsg":
        "正在分词",
    "system:ControlRoom:Console Message:component:3:field:updatingMsg":
        "正在更新系统",
    "system:ControlRoom:Console Message:component:3:field:wrongInput":
        "信号无变化",
    "system:ControlRoom:Puzzle Log Window/Viewport[0]/Puzzles Display[4]:component:3:field:failedToRetrieveResponse":
        "@未能获取翻译员的回应@",
    "system:ControlRoom:Puzzle Log Window/Viewport[0]/Puzzles Display[4]:component:3:field:loadingTxt":
        "正在编译...",
    "system:ControlRoom:Puzzle Log Window/Viewport[0]/Puzzles Display[4]:component:3:field:winResponseLine":
        "\n------------------------\n@成功响应：\n------------------------\n",
}
items_by_key = {
    item.get("extra", {}).get("game", {}).get("stable_key"): item
    for item in build_runtime.iter_items(cache)
}

fahrenheit_item = items_by_key["dialogue:16/frame:33"]
assert build_runtime.validate_item(fahrenheit_item) == [], "一千度语境应明确译为华氏度"
wrong_fahrenheit = deepcopy(fahrenheit_item)
wrong_fahrenheit["translated_text"] = wrong_fahrenheit["translated_text"].replace(
    "一千华氏度", "一千度"
)
errors = build_runtime.validate_item(wrong_fahrenheit)
assert any("华氏度" in error for error in errors), "省略 Fahrenheit 温标必须被拒绝"

celsius_item = items_by_key["dialogue:16/frame:32"]
assert build_runtime.validate_item(celsius_item) == [], "500 Celsius 应明确译为摄氏度"
wrong_celsius = deepcopy(celsius_item)
wrong_celsius["translated_text"] = wrong_celsius["translated_text"].replace(
    "500 摄氏度", "500 度"
)
errors = build_runtime.validate_item(wrong_celsius)
assert any("摄氏度" in error for error in errors), "省略 Celsius 温标必须被拒绝"

academic_degree = items_by_key["dialogue:1126/frame:5"]
assert build_runtime.validate_item(academic_degree) == [], "学位语境不得被温度校验误伤"

unclassified_degrees = deepcopy(fahrenheit_item)
unclassified_degrees["source_text"] = "{SPEAKER_AKERS}{PART_000}Turn it 90 degrees."
unclassified_degrees["translated_text"] = "{SPEAKER_AKERS}{PART_000}把它转 90 度。"
unclassified_degrees["extra"]["game"]["source_sha256"] = build_runtime.sha256_text(
    unclassified_degrees["source_text"]
)
unclassified_degrees["extra"]["game"]["stable_key"] = "test:unclassified-degrees"
errors = build_runtime.validate_item(unclassified_degrees)
assert any("尚未分类" in error for error in errors), "新的 degree/degrees 语境必须先人工分类"

for stable_key, expected in console_expected.items():
    item = items_by_key.get(stable_key)
    assert item is not None, f"控制台提示未被提取：{stable_key}"
    assert item.get("translation_status") in (1, 2), f"控制台提示尚未翻译：{stable_key}"
    assert item.get("translated_text") == expected, f"控制台提示译文不符合定稿：{stable_key}"

expected_categories = {
    "dialogue_frame": "dialogue",
    "dialogue_title": "titles",
    "ui_text": "ui",
    "ui_template": "ui",
    "achievement_name": "ui",
    "achievement_description": "ui",
    "display_value": "ui",
    "ui_fragment": "ui",
    "component_string": "system",
    "component_dialogue_frame": "system",
}
for kind, category in expected_categories.items():
    assert build_runtime.category_for(kind) == category, f"{kind} 运行时分类错误"

print(
    "Build-runtime self-test passed: category, tokens, ellipsis and temperature-unit validation."
)
