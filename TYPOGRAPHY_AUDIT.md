# 游戏原始字体与字号统计

数据来自游戏 v0.10 的全部场景及 `.assets` 资源。已识别 1,779 个 `TMP_Text` 组件，共 29 个序列化字号、13 个原始 TMP 字体。原始明细位于 `work/tmp-typography-audit.json`，可用 `tools/audit_tmp_typography.py` 重新生成。

注意：世界空间 UI 主要使用 `0.2–4.0`，屏幕 Canvas 主要使用 `36–84`，两套数值受不同 Canvas 缩放影响，不能直接用数值比较视觉大小。

## 对白运行时字体

主自动日志和非日志过场都会根据 `DialogueFrame.speaker` 把字幕字体换成角色配置中的字体，因此不能以字幕组件序列化的默认字体为准。

| 说话者 | 游戏字段 | 原文字体 | 是否等宽 | 备注 |
|---|---|---|---:|---|
| 埃克斯 | `aProf` | `pointfree SDF` | 是 | `post.isFixedPitch` 非零 |
| 巴蒂斯塔 | `bProf` | `NaturalMono-Regular SDF` | 是 | `post.isFixedPitch = 1` |
| 柯林斯 | `cProf` | `Jupiteroid-Regular SDF` | 否 | 比例字体 |
| 多普勒 | `dProf` | `Bowman SDF` | 是 | `post.isFixedPitch = 1` |
| 自动日志 | `logProf` | `Perfect DOS VGA 437 SDF` | 是 | `post.isFixedPitch = 1` |
| 飞行员 | `pilotProf` | `Jupiteroid-Regular SDF` | 否 | 与柯林斯相同 |
| 副驾驶 | `qopilotProf` | `Bowman SDF` | 是 | 与多普勒相同 |

主对白字幕组件的基础字号为 `1.2`；非日志字幕为 `42`，说话者标题为 `50`。汉化补丁的自动适配只让字幕在超宽时缩至基础字号的 66.7%，标题不缩放。

## 原始 TMP 字体使用量

| 原字体 | 组件数 |
|---|---:|
| Fake Receipt SDF | 1,671 |
| Jupiteroid-Regular SDF | 42 |
| Trapper John SDF | 18 |
| VCR_OSD_MONO_1.001 SDF | 15 |
| LiberationSans SDF | 12 |
| negotiate rg 1 SDF | 6 |
| Bowman SDF | 4 |
| Perfect DOS VGA 437 SDF | 3 |
| Deluxe Ducks SDF | 2 |
| pointfree SDF | 2 |
| NaturalMono-Regular SDF | 2 |
| rainyhearts SDF | 1 |
| negotiate rg SDF | 1 |

## 字号与原字体对应表

括号内为使用该组合的 TMP 组件数，包含未激活的调试和占位界面。

| 字号 | 组件数 | 原字体（组件数） |
|---:|---:|---|
| 0.2 | 1 | negotiate rg 1 SDF (1) |
| 0.26 | 1 | negotiate rg 1 SDF (1) |
| 0.32 | 1 | negotiate rg 1 SDF (1) |
| 0.4 | 1 | negotiate rg 1 SDF (1) |
| 0.45 | 3 | negotiate rg 1 SDF (2), Fake Receipt SDF (1) |
| 0.5 | 3 | Fake Receipt SDF (3) |
| 0.6 | 67 | Fake Receipt SDF (67) |
| 0.64 | 3 | Fake Receipt SDF (3) |
| 0.7 | 28 | Fake Receipt SDF (28) |
| 0.75 | 1 | Fake Receipt SDF (1) |
| 0.8 | 406 | Fake Receipt SDF (402), LiberationSans SDF (4) |
| 0.9 | 121 | Fake Receipt SDF (119), Bowman SDF (2) |
| 1.0 | 1,002 | Fake Receipt SDF (1,001), LiberationSans SDF (1) |
| 1.1 | 13 | Fake Receipt SDF (13) |
| 1.2 | 39 | Trapper John SDF (17), Fake Receipt SDF (17), VCR_OSD_MONO_1.001 SDF (4), Perfect DOS VGA 437 SDF (1) |
| 1.25 | 1 | negotiate rg SDF (1) |
| 1.5 | 1 | Fake Receipt SDF (1) |
| 1.6 | 1 | Fake Receipt SDF (1) |
| 2.2 | 40 | Jupiteroid-Regular SDF (40) |
| 4 | 1 | Trapper John SDF (1) |
| 36 | 7 | LiberationSans SDF (6), Fake Receipt SDF (1) |
| 42 | 1 | rainyhearts SDF (1) |
| 45 | 1 | Fake Receipt SDF (1) |
| 48 | 7 | Fake Receipt SDF (7) |
| 50 | 1 | LiberationSans SDF (1) |
| 56 | 3 | VCR_OSD_MONO_1.001 SDF (3) |
| 64 | 8 | VCR_OSD_MONO_1.001 SDF (6), Fake Receipt SDF (2) |
| 72 | 13 | Fake Receipt SDF (3), Deluxe Ducks SDF (2), Jupiteroid-Regular SDF (2), Bowman SDF (2), VCR_OSD_MONO_1.001 SDF (2), NaturalMono-Regular SDF (2) |
| 84 | 4 | pointfree SDF (2), Perfect DOS VGA 437 SDF (2) |

## 汉化字体选择结论

原文不是统一等宽字体。补丁按项目需求使用 Fusion Pixel Font 12px 非等宽简体中文版作为全局中文 fallback；游戏原字体仍优先渲染其已有的拉丁字形，只有缺失的中文字形和中文标点落到 Fusion Pixel Font。由于中文 fallback 无法保留七套角色字体的风格差异，补丁另用可配置的高对比度说话者颜色区分角色。
