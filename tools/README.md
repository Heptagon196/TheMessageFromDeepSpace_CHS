# 工具索引

以下命令均从 `TranslationProject` 根目录运行。面向日常工作的脚本列在前面；标为
“内部模块”的文件通常由其他工具导入，不需要直接执行。

## 日常入口

| 工具 | 作用 | 常用命令 |
|---|---|---|
| `update_translation.py` | 一条命令维护单条译文源、全量校验、生成运行时 JSON 并安装。全部成功前不会覆盖目标文件。 | `python tools/update_translation.py 1552153359 "[含混的嘟囔]"` |
| `test_project.ps1` | 构建补丁并运行全部 Python、.NET 和数据校验。 | `powershell -NoProfile -ExecutionPolicy Bypass -File tools/test_project.ps1` |
| `build_patch.ps1` | 准备依赖、构建插件和配置编辑器、生成可分发补丁目录。 | `powershell -NoProfile -ExecutionPolicy Bypass -File tools/build_patch.ps1` |
| `inspect_puzzle.ps1` | 按游戏显示题号查看题面、答案及相关词典解释。 | `powershell -NoProfile -ExecutionPolicy Bypass -File tools/inspect_puzzle.ps1 -DisplayId 100` |

### 一键修改译文

```powershell
python tools/update_translation.py <text_index> "<新译文>"
```

- 单段对白可只输入可见文本，工具自动沿用原文的说话者和 `PART_000`。
- 多段对白必须提供完整控制标记，防止错误合并分段。
- 修订保存在 `work/manual_translation_overrides.json`，同时记录原文 SHA-256；游戏更新
  改变原文后构建会失败并要求重新审阅，不会静默套用旧译文。
- `--no-install` 只更新项目；`--game-root` 可临时指定另一个游戏目录。
- `patch/Translations/*.json` 和游戏目录的同名文件均为生成物，禁止手工修改。

## 翻译批次与运行时数据

| 工具 | 作用 |
|---|---|
| `prepare_translation_batches.py` | 将未翻译缓存拆成带上下文、适合并行翻译的批次。 |
| `validate_translation_batch.py` | 对照权威缓存检查批次索引、控制标记、PART 数量和空段。 |
| `combine_translation_batches.py` | 合并已验证的批次产物，形成一次可审查的写入集合。 |
| `apply_translation_batch.py` | 按 `text_index` 将已验证批次写回 `work/cache.json`；仅供批量流程使用。 |
| `audit_translation_outputs.py` | 写回缓存前审计所有正式翻译批次和锁定术语。 |
| `build_runtime.py` | 从缓存和人工修订源生成 `dialogue/titles/ui/system` 等运行时 JSON。 |
| `check_dialogue_part_width.py` | 扫描对白 PART 的可见长度，发现潜在溢出。 |
| `audit_tmp_typography.py` | 检查 TMP 标签、字号和相关排版约束。 |
| `translation_text_checks.py` | 中文引号、省略号、重复标点等共享校验库（内部模块，也可供测试调用）。 |
| `normalize_chinese_quotes.py` | 批量规范中文引号。 |
| `normalize_dialogue_ellipses.py` | 批量规范对白省略号。 |
| `sync_sample_sources.py` | 给审校 JSON 补入权威原文，便于中英对照。 |
| `sync_ui_templates.py` | 将代码维护的动态 UI 模板同步到翻译缓存。 |
| `migrate_player_role_tokens.py` | 迁移玩家身份相关占位符。 |
| `analyze_glossary.py` | 分析译文中的候选术语和一致性。 |

## 提取与游戏数据检查

| 工具 | 作用 |
|---|---|
| `extract.py` | 从 Unity 资源提取对白、UI、系统文本及定位元数据，生成缓存。 |
| `extraction_rules.py` | 提取白名单、排除项、动态模板等规则（内部模块）。 |
| `probe_assets.py` | 枚举和检查 Unity 资源。 |
| `probe_object.py` | 按对象定位并输出组件数据。 |
| `probe_ui.py` | 检查 UI 对象及文本组件。 |
| `inspect_scene_inputs.py` | 列出场景输入框及其关键属性。 |
| `inspect_scene_screens.py` | 检查场景中的屏幕/显示对象。 |
| `inspect_puzzles.py` | 题面、答案和显示题号检查的 Python 实现。 |
| `inspect_monitor_material.py` | 检查显示器材质参数。 |
| `inspect_monitor_shader.py` | 检查显示器 Shader 数据。 |

## 词典触发与对白修正

| 工具 | 作用 |
|---|---|
| `extract_dictionary_trigger_aliases.py` | 从游戏数据提取原始词典对白触发条件。 |
| `build_dictionary_trigger_aliases.py` | 构建可分发的中文附加触发表。 |
| `dictionary_trigger_conflicts.py` | 检查精确、包含、组合条件之间是否可能双触发（内部模块）。 |
| `dictionary_dialogue_fixes.py` | 读取和校验按对白 ID 维护的修正规则（内部模块）。 |

## 环境、依赖与辅助配置

| 工具 | 作用 |
|---|---|
| `ensure_dependencies.ps1` | 下载并校验 BepInEx、字体和许可证等构建依赖。 |
| `resolve_game_root.ps1` | 为 PowerShell 工具解析游戏目录（内部模块）。 |
| `project_config.py` | 为 Python 工具解析 `TMFDS_GAME_ROOT`、`local.config.json` 或项目配置（内部模块）。 |
| `audit_unicode_inputs.ps1` | 检查所有游戏输入框是否具备中文输入兼容处理。 |

工具新增、删除或职责发生变化时，必须同步更新本索引和根目录 `AGENTS.md` 中的推荐入口。
