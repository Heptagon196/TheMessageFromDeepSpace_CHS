from __future__ import annotations

import argparse
import importlib
import os
import re
import subprocess
import sys
from pathlib import Path
from typing import Any


UNITYPY_VERSION = "1.25.3"
TYPE_TREE_GENERATOR_VERSION = "0.0.10"
TOOLS_DIR = Path(__file__).resolve().parent
PROJECT_DIR = TOOLS_DIR.parent
PYTHON_TAG = f"cp{sys.version_info.major}{sys.version_info.minor}"
RUNTIME_DIR = (
    PROJECT_DIR
    / "build"
    / "python-runtime"
    / PYTHON_TAG
    / f"unitypy-{UNITYPY_VERSION}-typetree-{TYPE_TREE_GENERATOR_VERSION}"
)


def sanitized_windows_environment() -> dict[str, str]:
    environment = dict(os.environ)
    if os.name != "nt":
        return environment

    system_root = environment.get("SystemRoot") or environment.get("WINDIR") or r"C:\Windows"
    drive_match = re.match(r"^([A-Za-z]:)", system_root)
    system_drive = drive_match.group(1) if drive_match else "C:"
    environment["SystemDrive"] = system_drive
    environment["SystemRoot"] = system_root
    environment["WINDIR"] = system_root

    defaults = {
        "ProgramData": rf"{system_drive}\ProgramData",
        "ALLUSERSPROFILE": rf"{system_drive}\ProgramData",
        "PUBLIC": rf"{system_drive}\Users\Public",
    }
    for name, default in defaults.items():
        value = environment.get(name, "")
        if not value or "%SystemDrive%" in value:
            environment[name] = default
    return environment


def _marker_is_readable() -> bool:
    marker = RUNTIME_DIR / "UnityPy" / "__init__.py"
    try:
        with marker.open("rb") as stream:
            return bool(stream.read(1))
    except (FileNotFoundError, PermissionError, OSError):
        return False


def _restore_windows_acl() -> None:
    if os.name != "nt" or not RUNTIME_DIR.exists():
        return
    subprocess.run(
        ["icacls", str(RUNTIME_DIR), "/inheritance:e", "/T", "/C"],
        check=False,
        stdout=subprocess.DEVNULL,
        stderr=subprocess.DEVNULL,
    )


def _install() -> None:
    RUNTIME_DIR.mkdir(parents=True, exist_ok=True)
    print(
        "首次使用：正在下载固定版本的 Unity 资产分析依赖……",
        file=sys.stderr,
    )
    command = [
        sys.executable,
        "-m",
        "pip",
        "install",
        "--disable-pip-version-check",
        "--upgrade",
        "--target",
        str(RUNTIME_DIR),
        f"UnityPy=={UNITYPY_VERSION}",
        f"TypeTreeGeneratorAPI=={TYPE_TREE_GENERATOR_VERSION}",
    ]
    try:
        subprocess.run(command, check=True, env=sanitized_windows_environment())
    except subprocess.CalledProcessError as exc:
        raise RuntimeError(
            "UnityPy 自动安装失败。若网络受限，请设置 HTTP_PROXY 和 HTTPS_PROXY 后重试。"
        ) from exc
    _restore_windows_acl()


def _clear_stale_modules() -> None:
    for module_name in tuple(sys.modules):
        if module_name == "UnityPy" or module_name.startswith("UnityPy."):
            del sys.modules[module_name]


def _import_and_validate() -> tuple[Any, Any]:
    runtime_text = str(RUNTIME_DIR)
    if runtime_text not in sys.path:
        sys.path.insert(0, runtime_text)
    importlib.invalidate_caches()
    _clear_stale_modules()

    UnityPy = importlib.import_module("UnityPy")
    type_tree_module = importlib.import_module("UnityPy.helpers.TypeTreeGenerator")
    TypeTreeGenerator = type_tree_module.TypeTreeGenerator
    if getattr(UnityPy, "__version__", None) != UNITYPY_VERSION:
        raise RuntimeError(
            f"UnityPy 版本错误：需要 {UNITYPY_VERSION}，实际为 "
            f"{getattr(UnityPy, '__version__', '未知')}。"
        )
    generator = TypeTreeGenerator("6000.0.73f1")
    if generator is None:
        raise RuntimeError("TypeTreeGenerator 初始化失败。")
    return UnityPy, TypeTreeGenerator


def load_unitypy() -> tuple[Any, Any]:
    if not _marker_is_readable():
        _restore_windows_acl()
    if not _marker_is_readable():
        _install()
    try:
        return _import_and_validate()
    except (ImportError, ModuleNotFoundError, OSError, RuntimeError):
        _restore_windows_acl()
        try:
            return _import_and_validate()
        except (ImportError, ModuleNotFoundError, OSError, RuntimeError):
            _install()
            return _import_and_validate()


def main() -> int:
    parser = argparse.ArgumentParser(description="准备并校验 Unity 资产分析依赖。")
    parser.add_argument(
        "--print-path",
        action="store_true",
        help="校验通过后输出隔离运行时目录。",
    )
    args = parser.parse_args()
    UnityPy, TypeTreeGenerator = load_unitypy()
    if args.print_path:
        print(RUNTIME_DIR)
    else:
        print(
            f"UnityPy {UnityPy.__version__} / "
            f"{TypeTreeGenerator.__module__} 已就绪（{PYTHON_TAG}）。"
        )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
