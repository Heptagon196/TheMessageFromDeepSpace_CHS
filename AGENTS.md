# 项目代理工作约定

本文件是自动化代理进入 `TranslationProject` 后的首要工作入口。完整工具索引见
[`tools/README.md`](tools/README.md)，翻译格式与运行时规则见
[`LOCALIZATION_RULES.md`](LOCALIZATION_RULES.md)。

## 修改单条译文

必须优先使用一键工具，不要直接编辑或局部拼接运行时 JSON：

```powershell
python tools/update_translation.py <text_index> "<新译文>"
```

例如：

```powershell
python tools/update_translation.py 1552153359 "[含混的嘟囔]"
```

该工具会把修订写入 `work/manual_translation_overrides.json`，校验控制标记，完整重建
运行时文件，并仅在全部成功后原子覆盖 `patch/Translations` 与游戏安装目录。单段对白
可以只写可见文本；多段对白必须传入完整的 `{SPEAKER_*}{PART_*}` 结构。

- 禁止直接编辑 `patch/Translations/*.json` 或游戏目录中的翻译 JSON；它们是生成物。
- 禁止为单条修订手工串联 `validate_translation_batch.py`、
  `apply_translation_batch.py` 和 `build_runtime.py`。
- 批量正式翻译仍维护 `work/formal_batches`，使用对应批次工具；小规模人工修订进入
  `work/manual_translation_overrides.json`。
- 只更新项目、不安装到游戏时，加 `--no-install`。

## 验证与构建

- 修改 Python 工具后至少运行对应的单项测试。
- 完成一组功能修改后运行：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/test_project.ps1
```

- 构建分发包使用 `tools/build_patch.ps1`；第三方依赖和构建二进制不得提交 Git。
- 提交前检查 `git diff --check`、`git status --short`，并确认没有 DLL、EXE、字体或压缩包。

## 文件职责

- `work/cache.json`：游戏文本提取缓存，不是单条人工修订的首选编辑点。
- `work/formal_batches/`：正式翻译批次及可重建脚本。
- `work/manual_translation_overrides.json`：经原文哈希保护的小规模人工修订源。
- `patch/`：最终补丁的源码与数据模板；其中运行时翻译 JSON 由工具生成。
- `src/`：BepInEx 插件和配置编辑器源码。
- `tests/`：构建期、翻译数据和运行时回归测试。
