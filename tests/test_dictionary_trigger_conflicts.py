from __future__ import annotations

import sys
import unittest
from pathlib import Path


PROJECT_DIR = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(PROJECT_DIR / "tools"))

from dictionary_dialogue_fixes import (
    DictionaryDialogueFix,
    apply_to_alias_entries,
    validate_against_source,
)
from dictionary_trigger_conflicts import find_conflicts, validate_no_conflicts
from build_dictionary_trigger_aliases import ALIASES, make_rules


def entry(term_id: int | None, english: str, value: str,
          channel: str = "EditEntryIDToName", **rule_extra):
    return {
        "term_id": term_id,
        "channel": channel,
        "english": english,
        "rules": [{"type": "exact", "values": [value], **rule_extra}],
    }


class DictionaryTriggerConflictTests(unittest.TestCase):
    def test_planet_side_display_names_trigger_the_directional_dialogues(self):
        self.assertIn("阳面", ALIASES["DAYSIDE"])
        self.assertNotIn("阳面", ALIASES["HOTSIDE"])
        self.assertIn("阴面", ALIASES["NIGHTSIDE"])
        self.assertNotIn("阴面", ALIASES["COLDSIDE"])

    def test_evolution_accepts_common_chinese_names(self):
        self.assertEqual(["演化", "进化"], ALIASES["EVOLUTION"])

    def test_rostral_organ_has_no_broad_base_aliases(self):
        rules = make_rules(-192, "EditEntryIDContains", "ROST", "contains")
        self.assertEqual([], rules)

    def test_dialogue_158_fix_moves_only_that_dialogue_to_term_minus_40(self):
        fix = DictionaryDialogueFix(
            158, "EditEntryIDToName", "TO", -41, -40, "test"
        )
        source = {"entries": [{
            "term_id": -41,
            "channel_name": "EditEntryIDToName",
            "english_trigger": "TO",
            "dialogue_chunk_ids": [158, 165],
        }, {
            "term_id": -40,
            "channel_name": "EditEntryIDToName",
            "english_trigger": "FROM",
            "dialogue_chunk_ids": [157],
        }]}
        validate_against_source([fix], source)
        aliases = [{
            "term_id": -41,
            "channel": "EditEntryIDToName",
            "english": "TO",
            "rules": [{"type": "exact", "values": ["到"]}],
            "dialogue_ids": [158, 165],
            "note": "base",
        }]
        apply_to_alias_entries(aliases, [fix])
        by_term = {item["term_id"]: item for item in aliases}
        self.assertEqual([165], by_term[-41]["dialogue_ids"])
        self.assertEqual([158], by_term[-40]["dialogue_ids"])

    def test_same_alias_under_different_term_ids_is_legal(self):
        entries = [entry(-86, "TO", "到"), entry(-41, "TO", "到")]
        self.assertEqual([], find_conflicts(entries))
        validate_no_conflicts(entries)

    def test_same_event_matching_two_conditions_is_rejected(self):
        entries = [entry(-1, "FIRST", "同义词"), entry(-1, "SECOND", "同义词")]
        conflicts = find_conflicts(entries)
        self.assertEqual(1, len(conflicts))
        self.assertEqual((-1, 1, "同义词"), conflicts[0].resolution_key)
        with self.assertRaisesRegex(ValueError, "同时命中多个条件"):
            validate_no_conflicts(entries)

    def test_explicit_exclusion_makes_contains_rules_disjoint(self):
        entries = [
            {
                "term_id": None,
                "channel": "EditEntryToName",
                "english": "IDK",
                "rules": [{
                    "type": "contains",
                    "values": ["不知道"],
                    "exclude_any": ["妈"],
                }],
            },
            {
                "term_id": None,
                "channel": "EditEntryToName",
                "english": "IDFK",
                "rules": [{
                    "type": "contains_all",
                    "values": ["妈", "不知道"],
                }],
            },
        ]
        self.assertEqual([], find_conflicts(entries))


if __name__ == "__main__":
    unittest.main()
