import json
import pathlib
import unittest


PROJECT_ROOT = pathlib.Path(__file__).resolve().parents[1]
CREDITS_KEY = "ui:SpaceshipRoom:Credits Canvas/BG[0]/Main Text[0]:component:2"


class EndingCreditsTests(unittest.TestCase):
    def test_final_title_is_separated_from_previous_credit_by_six_blank_lines(self) -> None:
        runtime = json.loads(
            (PROJECT_ROOT / "patch" / "Translations" / "ui.json").read_text(
                encoding="utf-8"
            )
        )
        credits = next(
            entry["translated_text"]
            for entry in runtime["entries"]
            if entry.get("stable_key") == CREDITS_KEY
        )
        self.assertIn(
            "Maid thing\n\n\n\n\n\n\n这就是……",
            credits,
            "最终标题前须比原版再多留两行，滚到“这就是”时不能同时露出上一段名单",
        )
        self.assertIn(
            "巴蒂斯塔拍立得 - Miguel San Juan",
            credits,
            "较长的署名须保持单行，避免无意义地增加字幕总高度",
        )
        self.assertNotIn("巴蒂斯塔拍立得照片 - Miguel San Juan", credits)


if __name__ == "__main__":
    unittest.main()
