from __future__ import annotations

import importlib.util
import unittest
from pathlib import Path
from unittest.mock import patch


SCRIPT = Path(__file__).resolve().parents[1] / "tools" / "inspect_puzzles.py"
SPEC = importlib.util.spec_from_file_location("inspect_puzzles", SCRIPT)
MODULE = importlib.util.module_from_spec(SPEC)
assert SPEC.loader is not None
SPEC.loader.exec_module(MODULE)


class InspectPuzzlesTests(unittest.TestCase):
    def test_decodes_with_player_dictionary(self) -> None:
        tokens = MODULE.decode_tokens([1, 2, -10, 3, -22, -4, -12], {
            -10: ".",
            -22: "面积",
            -4: "=",
            -12: "?",
        })
        self.assertEqual(tokens, ["1", "2", ".", "3", "面积", "=", "?"])
        self.assertEqual(MODULE.render_tokens(tokens), "12.3 面积 =?")

    def test_loads_dictionary_save(self) -> None:
        content = (
            '{"wordDict":{"keys":[-11,-22],"values":'
            '[{"name":"var"},{"name":"面积"}]}}'
        )
        with patch.object(Path, "read_text", return_value=content):
            result = MODULE.load_dictionary(Path("DICTIONARY-1.save"))
        self.assertEqual(result, {-11: "var", -22: "面积"})


if __name__ == "__main__":
    unittest.main()
