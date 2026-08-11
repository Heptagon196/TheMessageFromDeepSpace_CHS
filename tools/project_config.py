from __future__ import annotations

import json
import os
from pathlib import Path


PROJECT_DIR = Path(__file__).resolve().parents[1]
LOCAL_CONFIG_PATH = PROJECT_DIR / "local.config.json"
PROJECT_CONFIG_PATH = PROJECT_DIR / "project.config.json"


def resolve_game_root() -> Path:
    environment_value = os.environ.get("TMFDS_GAME_ROOT", "").strip()
    if environment_value:
        candidate = Path(environment_value).expanduser()
        if not candidate.is_absolute():
            candidate = Path.cwd() / candidate
    else:
        config_path = next(
            (
                path
                for path in (LOCAL_CONFIG_PATH, PROJECT_CONFIG_PATH)
                if path.is_file()
            ),
            None,
        )
        if config_path is None:
            candidate = PROJECT_DIR.parent
        else:
            try:
                config = json.loads(config_path.read_text(encoding="utf-8"))
            except (OSError, json.JSONDecodeError) as exc:
                raise RuntimeError(f"无法读取配置文件 {config_path}：{exc}") from exc
            configured_value = str(config.get("GameRoot", "")).strip()
            if not configured_value:
                raise RuntimeError(f"配置文件缺少非空的 GameRoot：{config_path}")
            candidate = Path(configured_value).expanduser()
            if not candidate.is_absolute():
                candidate = config_path.parent / candidate

    game_root = candidate.resolve()
    if not game_root.is_dir():
        raise FileNotFoundError(f"游戏根目录不存在：{game_root}")
    return game_root


GAME_ROOT = resolve_game_root()
DATA_DIR = GAME_ROOT / "The Message From Deep Space_Data"
if not DATA_DIR.is_dir():
    raise FileNotFoundError(f"游戏数据目录不存在：{DATA_DIR}")
