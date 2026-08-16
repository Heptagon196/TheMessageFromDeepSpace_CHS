from __future__ import annotations

import sys
import unittest
from pathlib import Path


PROJECT_DIR = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(PROJECT_DIR / "tools"))

import python_runtime


class PythonRuntimeTests(unittest.TestCase):
    def test_subprocess_environment_expands_windows_system_drive(self) -> None:
        environment = python_runtime.sanitized_windows_environment()
        if sys.platform == "win32":
            self.assertRegex(environment["SystemDrive"], r"^[A-Za-z]:$")
            self.assertNotIn("%SystemDrive%", environment["ProgramData"])

    def test_runtime_path_is_versioned_by_python_abi_and_packages(self) -> None:
        path = str(python_runtime.RUNTIME_DIR)
        self.assertIn(python_runtime.PYTHON_TAG, path)
        self.assertIn(f"unitypy-{python_runtime.UNITYPY_VERSION}", path)
        self.assertIn(
            f"typetree-{python_runtime.TYPE_TREE_GENERATOR_VERSION}", path
        )

    def test_unitypy_and_unity6_typetree_api_are_usable(self) -> None:
        UnityPy, TypeTreeGenerator = python_runtime.load_unitypy()
        self.assertEqual(UnityPy.__version__, python_runtime.UNITYPY_VERSION)
        self.assertEqual(
            type(TypeTreeGenerator("6000.0.73f1")).__name__,
            "TypeTreeGenerator",
        )


if __name__ == "__main__":
    unittest.main()
