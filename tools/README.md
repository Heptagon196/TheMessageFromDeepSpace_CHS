# 工具索引

以下命令均从 `TranslationProject` 根目录运行。面向日常工作的脚本列在前面；标为
“内部模块”的文件通常由其他工具导入，不需要直接执行。

## 日常入口

| 工具 | 作用 | 常用命令 |
|---|---|---|
| `update_translation.py` | 一条命令维护单条译文源、全量校验、生成运行时 JSON 并安装。全部成功前不会覆盖目标文件。 | `python tools/update_translation.py 1552153359 "[含混的嘟囔]"` |
| `update_translation_batch.py` | 批量维护多条人工译文源，只做一次全量校验与构建。 | `python tools/update_translation_batch.py build/updates.json --no-install` |
| `test_project.ps1` | 构建补丁并运行全部 Python、.NET 和数据校验。 | `powershell -NoProfile -ExecutionPolicy Bypass -File tools/test_project.ps1` |
| `build_patch.ps1` | 准备依赖、构建插件和配置编辑器、生成可分发补丁目录。 | `powershell -NoProfile -ExecutionPolicy Bypass -File tools/build_patch.ps1` |
| `dotnet.ps1` | 在受控终端中补全 Windows 标准环境变量后调用 .NET CLI，避免生成字面量 `%SystemDrive%` 目录。 | `& tools/dotnet.ps1 -DotNetArguments @('build', '项目.csproj')` |
| `run_python.ps1` | 使用固定的 Python 3.12、规范化 Windows 环境后运行任意项目 Python 工具。UnityPy 等资产依赖会由统一引导器自动准备。 | `& tools/run_python.ps1 tools/inspect_reference_layouts.py --output work/reference-layouts.json` |
| `python_runtime.py` | Unity 资产工具的唯一依赖入口：锁定 UnityPy/TypeTreeGeneratorAPI 版本，隔离缓存，修复 pip 目录权限并验证 Unity 6 类型树 API。通常无需直接调用。 | `& tools/run_python.ps1 tools/python_runtime.py` |
| `capture_reference_pages.ps1` | 用真实游戏渲染参考页。默认只对 `Blackhole` 做中文/英文各顶部和底部的成对截图，并验证两种语言都真实滚动；仅显式传 `-Batch` 才扫描批量页面。批量时只截打开后确认含图片、复制按钮、混合字号或小字的页面，并按相同滚动位置生成中英对照。可用 `-BatchPages` 先跑跨类别短名单，保留根目录 INI。 | `& tools/capture_reference_pages.ps1`；短名单：`& tools/capture_reference_pages.ps1 -Batch -BatchPages 'Blackhole','Meteorite Linguistics','2D SHAPES'`；烟雾测试通过后再用 `-Batch` 全量运行 |
| `analyze_reference_capture.py` | 分析真实截图清单中的文本/图形重叠与慢页面，通常由截图入口自动调用。 | `& tools/run_python.ps1 tools/analyze_reference_capture.py work/reference-captures/.../manifest.json` |
| `make_reference_contact_sheet.py` | 把自动截图整理为每页首帧、中间帧和末帧的联系表，便于人工检查图片、复制按钮、混合字号和滚动末端；可重复传 `--page` 限定页面。 | `& tools/run_python.ps1 tools/make_reference_contact_sheet.py work/reference-captures/... --language zh` |
| `inspect_puzzle.ps1` | 按游戏显示题号查看题面、答案及相关词典解释。 | `powershell -NoProfile -ExecutionPolicy Bypass -File tools/inspect_puzzle.ps1 -DisplayId 100` |
| `inspect_dictionary_trigger.ps1` | 按词条 ID 查询普通、已覆盖及组合型词典命名对白触发，避免遗漏分区。 | `powershell -NoProfile -ExecutionPolicy Bypass -File tools/inspect_dictionary_trigger.ps1 -TermId -102` |
| `restore_pre_ending_save.ps1` | 恢复界面题号 976、结局尚未触发的测试存档；不改词典，覆盖前自动备份当前剧情存档。 | `powershell -NoProfile -ExecutionPolicy Bypass -File tools/restore_pre_ending_save.ps1` |

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
| `build_dialogue_part_metadata.py` | 从游戏资产生成紧凑的 `clearPrev` 元数据，供对白分页边界校验使用。 |
| `audit_dialogue_part_boundaries.py` | 仅检查真正会清屏换页的 PART，禁止在中文词句中间分页。 |
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
| `inspect_reference_layouts.py` | 导出全部参考页的层级、RectTransform、文本、图片和复制按钮组件，用于排版诊断与回归夹具。 |
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
| `inspect_dictionary_trigger.py` | 合并原始触发数据的全部分区，并用最终中文附加触发表补充可用命名。 |

### 为中文别名添加独立对白

当一个英文触发词的多个中文译法无法共用原对白措辞时，在
`work/dictionary_trigger_aliases/dialogue_variants.json` 添加变体。不要把新词直接写进
生成的 `patch/Translations/dictionary_trigger_aliases.json`。

```json
{
  "term_id": -107,
  "channel": "EditEntryIDToName",
  "english": "VERY",
  "dialogue_id": 905,
  "synthetic_dialogue_id": 1905001,
  "rules": [{"type": "exact", "values": ["很"]}],
  "translated_title": "很",
  "frames": [
    {
      "frame_index": 0,
      "translated_text": "{SPEAKER_COLLINS}{PART_000}$animC4很好，{PART_001}{PLAYER_NAME}！"
    }
  ]
}
```

- `dialogue_id` 指向要继承角色、动画、分段、时序和对白类型的原版对白。
- `synthetic_dialogue_id` 必须是正整数，并且不能与原版或其他变体的 ID 重复；它使变体能独立记录和保存，不会被源对白的“已播放”状态拦截。
- `rules` 是一条独立的本地化监听，不会合并进原条件；因此“很”和“非常”可以按任意顺序各触发一次。构建器会拒绝它与普通触发或其他变体重叠。
- `frames` 必须完整覆盖源对白的全部 frame，并保留相同的 `SPEAKER`、`PART`、动画、信号、玩家名及 TMP 标记。
- 同一个源条件和源对白可以添加多个触发互不重叠的变体；可能被同一次输入同时命中的配置会令构建失败。

修改后运行 `python tools/build_dictionary_trigger_aliases.py`；完整交付前仍运行
`powershell -NoProfile -ExecutionPolicy Bypass -File tools/test_project.ps1`。

## 环境、依赖与辅助配置

| 工具 | 作用 |
|---|---|
| `ensure_dependencies.ps1` | 下载并校验 BepInEx、字体和许可证等构建依赖。 |
| `resolve_game_root.ps1` | 为 PowerShell 工具解析游戏目录（内部模块）。 |
| `project_config.py` | 为 Python 工具解析 `TMFDS_GAME_ROOT`、`local.config.json` 或项目配置（内部模块）。 |
| `audit_unicode_inputs.ps1` | 检查所有游戏输入框是否具备中文输入兼容处理。 |

工具新增、删除或职责发生变化时，必须同步更新本索引和根目录 `AGENTS.md` 中的推荐入口。
