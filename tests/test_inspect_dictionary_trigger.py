from __future__ import annotations

import importlib.util
import unittest
from pathlib import Path


SCRIPT = Path(__file__).resolve().parents[1] / "tools" / "inspect_dictionary_trigger.py"
SPEC = importlib.util.spec_from_file_location("inspect_dictionary_trigger", SCRIPT)
MODULE = importlib.util.module_from_spec(SPEC)
assert SPEC.loader is not None
SPEC.loader.exec_module(MODULE)


class InspectDictionaryTriggerTests(unittest.TestCase):
    def setUp(self) -> None:
        self.source = {
            "entries": [
                {
                    "term_id": -103,
                    "channel": 16,
                    "channel_name": "EditEntryIDToName",
                    "match_mode": "exact",
                    "english_trigger": "HYCEAN",
                    "dialogue_chunk_ids": [904],
                    "dialogues": [{"chunk_id": 904}],
                }
            ],
            "covered_entries": [
                {
                    "term_id": -102,
                    "channel": 16,
                    "channel_name": "EditEntryIDToName",
                    "match_mode": "exact",
                    "english_trigger": "ORGANIC",
                    "dialogue_chunk_ids": [259],
                    "dialogues": [{"chunk_id": 259}],
                }
            ],
            "combination_listeners": [
                {
                    "listener_path_id": 1,
                    "conditions": [
                        {
                            "term_id": -102,
                            "channel_name": "EditEntryIDToName",
                            "english_trigger": "ORGANIC",
                        },
                        {
                            "term_id": -101,
                            "channel_name": "EditEntryIDToName",
                            "english_trigger": "LIFE",
                        },
                    ],
                    "dialogue_chunk_ids": [999],
                }
            ],
        }
        self.aliases = {
            "entries": [
                {
                    "term_id": -103,
                    "channel": "EditEntryIDToName",
                    "english": "HYCEAN",
                    "rules": [{"type": "exact", "values": ["海氢", "海氢行星"]}],
                    "dialogue_ids": [904],
                },
                {
                    "term_id": -102,
                    "channel": "EditEntryIDToName",
                    "english": "ORGANIC",
                    "rules": [{"type": "exact", "values": ["有机"]}],
                    "dialogue_ids": [259],
                },
            ],
            "dialogue_variants": [{
                "term_id": -102,
                "channel": "EditEntryIDToName",
                "english": "ORGANIC",
                "dialogue_id": 259,
                "synthetic_dialogue_id": 1900259,
                "rules": [{"type": "exact", "values": ["有机物"]}],
                "translated_title": "有机物",
                "frames": [],
            }],
        }

    def test_finds_trigger_in_covered_entries(self) -> None:
        result = MODULE.inspect_term_triggers(self.source, self.aliases, -102)
        self.assertTrue(result["has_trigger"])
        self.assertEqual(result["single_triggers"][0]["source_partition"], "covered_entries")
        self.assertEqual(result["single_triggers"][0]["english"], "ORGANIC")
        self.assertEqual(
            result["single_triggers"][0]["localized_rules"][0]["values"], ["有机"]
        )
        self.assertEqual(result["single_triggers"][0]["dialogue_ids"], [259])
        self.assertEqual(result["combination_triggers"][0]["dialogue_ids"], [999])
        self.assertEqual(
            result["dialogue_variants"][0]["synthetic_dialogue_id"], 1900259
        )

    def test_finds_trigger_in_uncovered_entries(self) -> None:
        result = MODULE.inspect_term_triggers(self.source, self.aliases, -103)
        self.assertTrue(result["has_trigger"])
        self.assertEqual(result["single_triggers"][0]["source_partition"], "entries")
        self.assertEqual(result["single_triggers"][0]["english"], "HYCEAN")
        self.assertEqual(result["single_triggers"][0]["dialogue_ids"], [904])

    def test_reports_missing_term(self) -> None:
        result = MODULE.inspect_term_triggers(self.source, self.aliases, -999)
        self.assertFalse(result["has_trigger"])
        self.assertEqual(result["single_triggers"], [])
        self.assertEqual(result["combination_triggers"], [])
        self.assertEqual(result["dialogue_variants"], [])


if __name__ == "__main__":
    unittest.main()
