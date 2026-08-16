from __future__ import annotations

import importlib.util
import json
import sys
import unittest
from copy import deepcopy
from pathlib import Path


PROJECT_DIR = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(PROJECT_DIR / "tools"))

spec = importlib.util.spec_from_file_location(
    "build_dictionary_trigger_aliases",
    PROJECT_DIR / "tools" / "build_dictionary_trigger_aliases.py",
)
assert spec and spec.loader
builder = importlib.util.module_from_spec(spec)
spec.loader.exec_module(builder)


class DictionaryDialogueVariantTests(unittest.TestCase):
    def setUp(self) -> None:
        self.entries = [{
            "term_id": -107,
            "channel": "EditEntryIDToName",
            "english": "VERY",
            "rules": [{"type": "exact", "values": ["非常"]}],
            "dialogue_ids": [905],
            "note": "base",
        }]
        self.source = {
            "entries": [],
            "covered_entries": [{
                "term_id": -107,
                "channel_name": "EditEntryIDToName",
                "english_trigger": "VERY",
                "dialogue_chunk_ids": [905],
                "dialogues": [{
                    "chunk_id": 905,
                    "frames": [{
                        "frame_index": 0,
                        "source": (
                            "{SPEAKER_COLLINS}{PART_000}$animC4Very good,"
                            "{PART_001}{PLAYER_NAME}!"
                        ),
                    }, {
                        "frame_index": 1,
                        "source": (
                            "{SPEAKER_AKERS}{PART_000}Oh I get it!"
                            "{PART_001}Very very good!"
                        ),
                    }],
                }],
            }],
        }
        self.variants = [{
            "term_id": -107,
            "channel": "EditEntryIDToName",
            "english": "VERY",
            "dialogue_id": 905,
            "synthetic_dialogue_id": 1905001,
            "rules": [{"type": "exact", "values": ["很"]}],
            "translated_title": "很",
            "frames": [{
                "frame_index": 0,
                "translated_text": (
                    "{SPEAKER_COLLINS}{PART_000}$animC4很好，"
                    "{PART_001}{PLAYER_NAME}！"
                ),
            }, {
                "frame_index": 1,
                "translated_text": (
                    "{SPEAKER_AKERS}{PART_000}哦，我明白了！"
                    "{PART_001}很好，很好！"
                ),
            }],
        }]

    def test_variant_keeps_its_trigger_separate_from_the_base_condition(self) -> None:
        runtime_variants = builder.apply_dialogue_variants(
            self.entries, self.source, self.variants
        )
        values = {
            value
            for rule in self.entries[0]["rules"]
            for value in rule["values"]
        }
        self.assertEqual({"非常"}, values)
        self.assertEqual("很", runtime_variants[0]["translated_title"])
        self.assertEqual(1905001, runtime_variants[0]["synthetic_dialogue_id"])
        self.assertEqual(2, len(runtime_variants[0]["frames"]))

    def test_variant_rejects_a_translation_that_loses_control_tokens(self) -> None:
        self.variants[0]["frames"][0]["translated_text"] = (
            "{SPEAKER_COLLINS}{PART_000}很好！"
        )
        with self.assertRaisesRegex(ValueError, "标记|part|PART"):
            builder.apply_dialogue_variants(self.entries, self.source, self.variants)

    def test_variant_rejects_an_unknown_dialogue_condition(self) -> None:
        self.variants[0]["dialogue_id"] = 999
        with self.assertRaisesRegex(ValueError, "999"):
            builder.apply_dialogue_variants(self.entries, self.source, self.variants)

    def test_multiple_disjoint_variants_can_reuse_one_source_dialogue(self) -> None:
        second = deepcopy(self.variants[0])
        second["synthetic_dialogue_id"] = 1905002
        second["rules"][0]["values"] = ["挺"]
        second["translated_title"] = "挺"
        runtime = builder.apply_dialogue_variants(
            self.entries, self.source, [self.variants[0], second]
        )
        self.assertEqual([1905001, 1905002], [
            item["synthetic_dialogue_id"] for item in runtime
        ])

    def test_project_variant_file_defines_the_very_dialogue(self) -> None:
        payload = json.loads(
            (PROJECT_DIR / "work" / "dictionary_trigger_aliases" /
             "dialogue_variants.json").read_text(encoding="utf-8")
        )
        variants = payload["variants"]
        variant = next(item for item in variants if item["term_id"] == -107)
        self.assertEqual(905, variant["dialogue_id"])
        self.assertEqual(1905001, variant["synthetic_dialogue_id"])
        self.assertEqual(["很"], variant["rules"][0]["values"])
        self.assertNotIn("非常非常好", json.dumps(variant, ensure_ascii=False))

    def test_rostral_organ_uses_only_exact_chinese_dialogue_variants(self) -> None:
        payload = json.loads(
            (PROJECT_DIR / "work" / "dictionary_trigger_aliases" /
             "dialogue_variants.json").read_text(encoding="utf-8")
        )
        variants = [
            item for item in payload["variants"]
            if item["term_id"] == -192 and item["english"] == "ROST"
        ]
        self.assertEqual([], builder.ALIASES["ROST"])
        self.assertNotIn((-192, "ROST"), builder.CONTAINS_ALL_ALIASES)
        self.assertEqual(
            [["吻部器官"], ["吻端器官"]],
            [item["rules"][0]["values"] for item in variants],
        )
        self.assertTrue(all(item["rules"][0]["type"] == "exact" for item in variants))
        self.assertNotIn("当今科学尚未知晓", json.dumps(variants, ensure_ascii=False))

    def test_infinity_has_all_common_chinese_aliases(self) -> None:
        self.assertEqual(
            ["无限", "无限大", "无穷大"],
            builder.ALIASES["INFINITY"],
        )

    def test_extrema_include_value_form_aliases(self) -> None:
        self.assertIn("最小值", builder.ALIASES["SMALLEST"])
        self.assertIn("最大值", builder.ALIASES["BIGGEST"])

    def test_perception_name_triggers_the_feel_dialogue(self) -> None:
        self.assertEqual(["感觉", "感受", "感知"], builder.ALIASES["FEEL"])

    def test_temperature_accepts_common_heat_names(self) -> None:
        self.assertEqual(
            ["温度", "热量", "热度"],
            builder.ALIASES["TEMPERATURE"],
        )

    def test_early_dictionary_aliases_match_intended_concepts(self) -> None:
        self.assertEqual(["小数点"], builder.ALIASES["DECIMAL"])
        self.assertEqual(["延续", "等等"], builder.ALIASES["CONTINUED"])
        self.assertEqual(["球体", "球"], builder.ALIASES["SPHERE"])

    def test_brain_uses_contains_matching(self) -> None:
        self.assertEqual(["脑"], builder.ALIASES["BRAIN"])
        self.assertNotIn((-147, "BRAIN"), builder.EXACT_ENTRY_KEYS)

    def test_limb_uses_short_contains_alias(self) -> None:
        self.assertEqual(["肢"], builder.ALIASES["LIMB"])

    def test_arm_uses_broad_contains_aliases(self) -> None:
        self.assertEqual(["手", "臂", "胳膊"], builder.ALIASES["ARM"])


if __name__ == "__main__":
    unittest.main()
