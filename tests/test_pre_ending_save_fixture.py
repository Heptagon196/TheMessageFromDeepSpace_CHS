import json
import unittest
from pathlib import Path


PROJECT_ROOT = Path(__file__).resolve().parents[1]
FIXTURE_DIR = (
    PROJECT_ROOT / "tests" / "fixtures" / "saves" / "pre-ending-display-976"
)


class PreEndingSaveFixtureTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.save_path = FIXTURE_DIR / "TMFDS.save"
        cls.save = json.loads(cls.save_path.read_text(encoding="utf-8-sig"))

    def test_fixture_is_display_puzzle_976_before_completion(self) -> None:
        self.assertEqual(self.save["currPuzz"], 975)
        self.assertEqual(self.save["currPuzz"] + 1, 976)
        self.assertEqual(self.save["currPuzzListID"], 144)
        self.assertEqual(self.save["currPuzzLocalID"], 6)
        self.assertFalse(self.save["translationComplete"])

    def test_ending_state_has_not_been_recorded(self) -> None:
        winning = self.save["winningResponses"]
        self.assertEqual(len(winning["keys"]), len(winning["values"]))
        self.assertNotIn(921, winning["keys"])
        self.assertNotIn("The Message from Deep Space", self.save["achievements"])
        self.assertFalse(
            any(
                entry.get("dialogueBankID") == 1207
                for entry in self.save["dialogueEntryData"]
            )
        )

if __name__ == "__main__":
    unittest.main()
