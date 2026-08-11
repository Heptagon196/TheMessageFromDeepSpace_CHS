# 《The Message from Deep Space》汉化规则

本文档记录本项目的文本提取、AiNiee/Codex 翻译、校验和运行时注入规则。实现与翻译均应以本文档为准。

## 1. 已确认的游戏结构

- 游戏版本：`0.10`。
- Unity 版本：`6000.0.73f1`。
- 脚本后端：Mono；主要逻辑位于 `The Message From Deep Space_Data/Managed/Assembly-CSharp.dll`。
- 主要对白资源位于 `The Message From Deep Space_Data/sharedassets0.assets`。
- 推荐使用 BepInEx + Harmony 运行时注入，不直接覆盖 Unity `.assets` 文件。

### 1.1 对白结构

人物对白的运行时结构为：

```text
DialogueBank.allDialogues
└─ DialogueChunk
   ├─ uniqueID
   ├─ logName
   ├─ raw
   ├─ processedRaw
   └─ frames: DialogueFrame[]
      ├─ speaker
      ├─ msgDelay
      └─ dialogueParts: DialoguePart[]
         ├─ txt
         ├─ charDelay
         ├─ clearPrev
         └─ msgDelay
```

实际逐字显示使用 `DialoguePart.txt`。`charDelay`、`clearPrev`、`msgDelay` 控制打字、清屏和停顿，不能因翻译而改变。

`DialogueChunk.raw` 是带编辑器解析标记的源稿；`processedRaw` 主要供日志窗口使用。提取器不得重新解析或覆盖 `raw`。

### 1.2 外星语言结构

外星传输不是普通英文字符串，而是：

```text
SignalMessage.signals: int[]
```

谜题的原始传输、正确响应和替代响应分别存于：

```text
Puzzle.rockOutput
Puzzle.winningResponse
Puzzle.altResponses
```

信号显示时由 `SignalCompiler` 和玩家的 `UserDictionary` 动态转换。未知信号的内部占位内容类似 `SIGNAL_17`，汉化显示为“信号17”（负数如 `SIGNAL_-43` 显示为“信号-43”）；已识别信号显示为玩家自己命名的词。

这些信号数组、谜题答案和玩家词典属于玩法数据，不属于本地化文本。

## 2. 文本分类

### 2.1 必须翻译

- `DialogueFrame.dialogueParts[].txt` 中的人物对白。
- 日志标题 `DialogueChunk.logName`。
- 菜单、按钮、说明、提示、教程和固定标签等静态 UI 文本。
- 游戏内显示给玩家看的终端、启动日志、控制台输出和代码风格提示。外观看起来像代码不等于内部文本。
- 非自动日志对白和过场字幕中的普通英语。
- 成就名称与说明、谜题标题与组名、元素名、歌曲名、日志章节名和科学家在词典界面显示的猜测。
- 设置说明、固定错误提示以及由代码拼接的动态 UI 模板；若其中包含动态值，必须保护占位符。

### 2.2 绝对禁止翻译或修改

- `SignalMessage.signals` 中的任何整数及其顺序。
- `Puzzle.rockOutput`、`Puzzle.winningResponse`、`Puzzle.altResponses`。
- `ContactTransmissions` 中的传输配对。
- 未知信号的 ID，例如 `SIGNAL_17` 中的 `17`。
- 玩家词典 `UserDictionary.terms` 的键、值和存档数据。
- 玩家自己输入的译者名称。
- 存档路径、资源名、Unity 对象名、枚举值和代码标识符。
- 存档后缀、成就内部 ID、谜题条件和答案、元素符号、用户词典键值等不会作为自然语言直接显示的玩法数据。
- 动画、富文本、动态替换及汉化桥接占位符。

### 2.3 需要上下文判断

- `SIGNALS`、`TRANSMISSIONS`、`TRANSLATOR` 等静态界面标签是 UI，应翻译；其中 `TRANSLATOR` 固定译为“翻译员”。
- `SIGNAL_17` 是游戏内未知词：数字 ID 必须原样保留，不得把它翻成谜底；仅在最终画面把 `SIGNAL_` 前缀本地化为“信号”。F8 切回原文时必须恢复完整的 `SIGNAL_17`。
- 人物对白中谈论“signal”“transmission”等普通单词时可以翻译。
- 人名、地名、机构名和科学术语必须先进入锁定词汇表，再统一决定保留、音译或意译。
- 玩家已识别的外星词由 `UserDictionary` 动态提供，不做静态翻译，也不得提前写入译文。

## 3. 必须保护的标记

### 3.1 游戏原生标记

- 动画命令：`$animC5`、`$animA2` 等，必须逐字符保留。
- TextMeshPro 富文本：`<u>`、`</u>`、`<color=...>`、`<size=...>` 等。
- 格式化占位符：`{0}`、`{1}`、`%s` 等。
- 换行和转义序列：实际换行、`\n`、`\t`。
- 动态玩家名：原游戏会把对白中的 `Translator`、`The Translator`、`the Translator` 替换为玩家姓名。提取器必须将这些称呼规范化为 `{PLAYER_NAME}`，包括紧跟在 `$anim...` 后、前面没有空格的情况。

`DialogueChunk.raw` 中还可能含 `@`、`#1`、`#3` 等编辑器解析标记。运行时提取应直接读取已经解析好的 `frames/dialogueParts`，不把 `raw` 送去翻译，因此翻译文件中通常不应出现这些标记。

### 3.2 汉化桥接占位符

提取器把动态内容规范化为以下占位符：

```text
{PLAYER_NAME}
{SIG_017}
{SIG_N160}
{PART_000}
{PART_001}
{SPEAKER_AKERS}
{DYN_0}
{DYN_1}
```

规则：

- 占位符仅使用 ASCII 大写字母、数字、下划线和花括号。
- 信号编号使用固定宽度，例如信号 17 写作 `{SIG_017}`；负数信号 -160 写作 `{SIG_N160}`。
- 译文必须包含与原文完全相同的占位符集合、数量和顺序。
- 翻译模型不得翻译、删除、复制或改写占位符。
- 注入器负责把 `{PLAYER_NAME}` 替换为玩家名称，把 `{SIG_017}` 交回游戏词典解析。
- `{PART_nnn}` 是对白分段边界。它可以随中文语序移动到更自然的位置，但相对顺序不得改变。
- TMP 富文本样式不得跨越 `{PART_nnn}` 边界；每个 part 会被游戏独立逐字写入，因此 `<size>`、`<font>` 等标签必须在同一个 part 内成对闭合。
- `{DYN_n}` 是运行时动态值或代码拼接片段。译文可围绕它调整中文语序，但编号、数量和相对顺序不得改变；同一编号在原文中重复出现时，译文也必须保留相同次数。

### 3.3 空白与边界

`DialoguePart.txt` 可能含有有意义的前导或尾随空格。为避免翻译模型丢失空白：

- 提取时从可翻译正文中剥离前导和尾随空白。
- 将原空白分别记录到 `CacheItem.extra.game.leading_whitespace` 和 `trailing_whitespace`。
- 回注时由桥接工具恢复，不要求翻译模型维护边界空格。
- 译文回注后，如果相邻 `{PART_nnn}` 边界两侧都是汉字或中文标点，必须删除从英文原文继承的横向空格；例如“我这边{PART_001}对陨石本体”显示为“我这边对陨石本体”。中英文、英文与数字之间的必要空格仍保留，原文模式不做此规范化。
- 文本内部空格和换行仍属于正文；校验器必须检查换行数量。
- 中文一级引用统一使用“ ”，‘ ’只允许作为“ ”内部的二级引用；半角直引号不得用于中文引用。构建与正式批次审计必须同时检查引号层级、开闭顺序和成对情况。

## 4. 与 `ainiee-translate` 的数据接口

### 4.1 中间格式

翻译中间态必须使用该 skill 的 `CacheProject/cache.json` 格式，不把普通 `{key: translation}` JSON 直接交给翻译循环。

核心字段：

```text
CacheProject
└─ files: dict[str, CacheFile]
   └─ items: CacheItem[]
      ├─ text_index
      ├─ translation_status
      ├─ source_text
      ├─ translated_text
      └─ extra
```

翻译状态沿用 skill 定义：

```text
0 = UNTRANSLATED
1 = TRANSLATED
2 = POLISHED
7 = EXCLUDED
```

这样可以直接使用：

```powershell
python -m ainiee_translate.batch read work/cache.json --size 100
python -m ainiee_translate.batch write work/cache.json work/translations_001.json
python -m ainiee_translate.polish write work/cache.json work/polished_001.json
```

### 4.2 项目目录

当前项目目录：

```text
TranslationProject/
├─ work/
│  ├─ cache.json
│  ├─ glossary.locked.json
│  └─ user_prompt.md
├─ build/
│  ├─ extraction-report.json
│  ├─ validation-report.json
│  └─ package/DeepSpaceChinese/Translations/
│     ├─ dialogue.json
│     ├─ titles.json
│     ├─ ui.json
│     └─ system.json
└─ tools/
```

标准 `ainiee_translate.export` 面向 EPUB、TXT、JSON 等原始文档，不负责写回 Unity 游戏资源。本项目完成翻译后，由 `tools/build_runtime.py` 读取 `cache.json` 并生成上列 4 个固定 JSON。全部对白合并在 `dialogue.json`；插件仍会递归合并 `Translations` 下所有 JSON，因此以后需要人工拆分时无需改插件。

### 4.3 CacheFile 分组

Cache 按以下类别分组，便于翻译和复查（UI 可按场景细分 CacheFile，但运行时统一合并到 `ui.json`）：

```text
dialogue.frames
dialogue.titles
ui.<scene>
system.messages
*.excluded
```

外星传输和玩家词典不得进入待翻译 CacheFile。若为审计而导出，应写入单独报告，并将状态标为 `EXCLUDED`。

### 4.4 CacheItem.extra 元数据

每个可翻译项目必须保留稳定的游戏定位信息。例如：

```json
{
  "text_index": 1201,
  "translation_status": 0,
  "source_text": "{SPEAKER_AKERS}{PART_000}Hey Carrie,{PART_001}$animA2 I've got a theory.",
  "translated_text": "",
  "extra": {
    "game": {
      "kind": "dialogue_frame",
      "stable_key": "dialogue:143/frame:0",
      "chunk_id": 143,
      "chunk_name": "Example Dialogue",
      "frame_index": 0,
      "speaker": "Akers",
      "part_count": 2,
      "source_sha256": "...",
      "parts": [
        {
          "part_index": 0,
          "leading_whitespace": "",
          "trailing_whitespace": " "
        },
        {
          "part_index": 1,
          "leading_whitespace": "",
          "trailing_whitespace": ""
        }
      ]
    }
  }
}
```

要求：

- `text_index` 在整个项目内唯一，并在重新提取同一游戏版本时保持确定性。
- `stable_key` 是回注主键；不得用英文原句作主键。
- `source_sha256` 用于检测游戏更新或源文变化。
- 对白以一个 `DialogueFrame` 为翻译单元，并用 `{PART_nnn}` 保留游戏内部分段。
- 一个 frame 不得和相邻 frame 合并，以免改变说话人、推进输入和停顿逻辑。
- UI 项目的 `stable_key` 应包含场景名、对象层级路径和组件序号；同时记录原文哈希。
- `component_string` 和 `component_dialogue_frame` 用于序列化组件内、玩家实际可见的系统文本；字段可包含 `progressLogData.actName`、`hypos[i].aGuess` 等嵌套路径。
- `achievement_name`、`achievement_description` 只导出显示名称与说明，不导出 Steam/存档所用的成就 ID。
- `display_value` 用于谜题标题与组名、元素名、同位素单位、丰度等级和歌曲名；这些值可被动态 UI 模板引用。
- `ui_template` 用 `{DYN_n}` 表示代码运行时插入的变量，`ui_fragment` 用于周期表等由多个固定片段拼成的界面文本。

### 4.5 为什么不把每个 DialoguePart 单独翻译

单独翻译每个 `DialoguePart` 虽然回注简单，但会丢失句子上下文，尤其一个句子被动画命令、停顿或清屏拆成多段时容易产生语义错误。

因此采用“一帧一个 CacheItem”的规则：

1. 提取同一 frame 的全部 part。
2. 在各 part 前插入 `{PART_nnn}`。
3. 加入 `{SPEAKER_...}` 供翻译模型识别说话人。
4. 翻译后按 `{PART_nnn}` 拆回原 part 数组。
5. 保留每个 part 原有的 `charDelay`、`clearPrev` 和 `msgDelay`。

如果译文缺少、重复或打乱任何 `{PART_nnn}`，该条翻译必须拒绝回注并列入复查报告。

## 5. AiNiee 翻译规则

翻译过程必须遵循 `ainiee-translate` skill 的标准规则：

- `batch read` 与 `batch write` 必须按 `text_index` 一一对应。
- 不合并、不拆分、不漏译任何 CacheItem。
- 标签、占位符、转义序列和代码标记原样保留。
- 人名和术语以 `glossary.locked.json` 为唯一真相源。
- 锁定表外的新专名默认保留原文并进入待审列表。
- 翻译中断后只继续处理 `translation_status = 0` 的条目。
- 每批写回前保留时间戳备份。

### 5.1 项目提示词必须补充的游戏规则

`work/user_prompt.md` 至少应包含：

```text
这是解读外星语言的游戏。不得根据上下文猜测并翻译任何 {SIG_nnn}，
不得解释或泄露其含义。所有 {SIG_nnn} 必须原样保留。

保留所有 {PART_nnn}、{SPEAKER_*}、{PLAYER_NAME}、{DYN_n}、$anim...、TMP 富文本标签。
不得增删或重新编号。对话应自然简洁，适合逐字显示和有限 UI 空间。
```

### 5.2 锁定词汇表

`glossary.locked.json` 的 `non_translate` 至少包含：

```json
{
  "non_translate": [
    {"marker": "{SIG_", "category": "alien_signal_placeholder"},
    {"marker": "{PART_", "category": "dialogue_partition"},
    {"marker": "{SPEAKER_", "category": "speaker_context"},
    {"marker": "{PLAYER_NAME}", "category": "runtime_player_name"},
    {"marker": "{DYN_", "category": "runtime_dynamic_value"},
    {"marker": "$anim", "category": "animation_command"}
  ]
}
```

人物名及最终显示形式须在正式翻译前人工锁定。不要仅依靠自动扫描生成的人名表。

## 6. 翻译完成后的校验

每个 `TRANSLATED` 或 `POLISHED` 项目至少检查：

1. `translated_text` 非空。
2. `text_index` 存在且唯一。
3. 原文和译文的 `{SIG_nnn}` 列表完全一致。
4. 原文和译文的 `{PART_nnn}` 列表完全一致且顺序不变。
5. `{PLAYER_NAME}` 数量一致。
6. `{DYN_n}` 列表、重复次数和顺序完全一致。
7. `$anim...` 命令列表完全一致。
8. TMP 富文本标签成对且结构合法。
9. 格式化占位符数量和类型一致。
10. 不包含新的未知控制标记。
11. `source_sha256` 与当前游戏提取结果一致。
12. 译文长度不超过相应 UI 的安全阈值；超长条目进入人工复查。
13. `scan --mode all` 的结果经人工复核，尤其关注人名错译和模型凭空新增的英文词。
14. 温度表达必须保留温标：原文 `Celsius` / `Fahrenheit` 分别明确译为“摄氏度”/“华氏度”（或 `℃`/`℉`、`°C`/`°F`）。
15. 所有含 `degree/degrees` 的原文都必须结合上下文按 `stable_key` 登记为摄氏度、华氏度、角度、学位或抽象“程度”；未登记的新条目令严格构建失败，各类译文还必须包含对应含义。

任何硬性校验失败时：

- 不得把该条译文写入最终运行时翻译包。
- 若 `FallbackToOriginal = true`，游戏继续显示原文。
- 在 `validation-report.json` 中记录 `stable_key`、错误类型和原译文。

## 7. 运行时注入规则

### 7.1 对白

- 在 `DialogueBank.SetDataFromLoad()` 完成后通过 Harmony 后置钩子枚举 `allDialogues`。
- 以 `DialogueChunk.UniqueID + frame_index` 查找译文。
- 按 `{PART_nnn}` 拆分后写入对应 `DialoguePart.txt`。
- 注入前保存全部原文，切换显示模式时从原文重新生成，禁止在已注入字符串上重复拼接。
- `WriteFrameRoutine` 会按值持有当前 `DialogueFrame`，仅重写 `DialogueBank` 无法刷新正在播放的这一帧。运行时必须同时跟踪当前主对白/场景对白的中英文页状态，在 `TMP_Text.set_text` 入口即时映射当前帧，并按两种文本的长度同比换算 `maxVisibleCharacters`；不得通过跳过、重播对白或重复触发动画来实现切换。
- `ProgressLog` 的个人日志正文通过 `CharacterDialogueTypeRoutine` 按值持有 `ProgressLogData.aDF/bDF/cDF/dDF`，标题则通过两个 `GenericTypeRoutine` 重载独立逐字显示。F8 必须在这些通用入口登记由 `dialogue.json` / `system.json` 提供的中英文文本对，并将当前可见进度从“协程生产文本长度”先还原、再换算到目标语言；禁止把某个角色标题硬编码进补丁，也禁止直接沿用中文标题的 `maxVisibleCharacters`，否则 `Alan's Journal:` 等较长原文会被截成 `Alan's`。
- `ProgressLog` 的五个个人日志标题使用独立的手写 TMP 字体和材质；仅把 Fusion Pixel 注册为全局 fallback 不能保证中文字形实际可见。译文模式必须给 `aLogTitle/bLogTitle/cLogTitle/dLogTitle/tLogTitle` 直接绑定中文字体及其材质，并把溢出模式设为 `Overflow`，避免原标题框裁掉中文；切回原文时恢复每个组件原有的字体、材质和溢出模式。标题内容仍只能从翻译 JSON 解析，不得在补丁代码中写死。
- 不修改 `SignalMessage`、`Puzzle` 或 `UserDictionary`。

### 7.2 日志

不能只修改 `DialoguePart.txt` 而忽略日志窗口，因为原游戏日志会读取 `processedRaw`。

优先方案：运行时由已翻译的 frame/part 重建本地化日志文本，并在日志显示入口使用重建结果。这样避免重复翻译 `processedRaw`，也能保证实时信号词和玩家名称正确替换。

纯译文模式使用重建后的 frame 文本；纯原文模式使用保存的原始 frame 文本重新生成日志。动态信号必须在显示当刻解析，不能在翻译包中固化为某个词义。

### 7.3 UI

- 场景加载后扫描 `TMP_Text`，按稳定对象路径或已知静态原文查找 UI 译文。
- 枚举已知系统组件并按嵌套字段路径回写 `component_string` 和 `component_dialogue_frame`；模式切换时仍从保存的原始字段重新生成。
- 对代码拼接的可见文本使用锚定的 `ui_template` 匹配，只替换模板固定部分并把捕获值放回 `{DYN_n}`；不要把动态数字、路径或玩家命名当成译文固化。
- 由 `DialogueManager.GenericTypeRoutine()` 写入固定宽度标签的运行时 `component_string`，不能依赖英文单词空格触发自动换行；若对应静态 UI 文本已有手动换行，运行时字段译文必须采用相同的分行结构，并以该完整显示文本做回归测试。
- `TermLogger.Configure()` 会把不带尾随空格的 `NAME SIGNAL` 与负数信号编号直接拼接成 `NAME SIGNAL-42`。必须用无空格模板 `NAME SIGNAL{DYN_0}` 翻译为“为信号 {DYN_0} 命名”，并用负数编号做回归测试。由于该方法会在运行时重新覆盖标签，补丁必须在 `Configure(signal)` 完成后用实际 `signal` 重建、翻译并登记该文本，保证 F8 也能恢复原文；不能只测试模板渲染器或依赖预制体字段的静态译文。
- “新单词命名”浮窗可能同时生成多条。`[Layout] NewWordPromptLowerRight = true` 时必须把同一父节点下的整组 `TermLogger` 作为列表移动到右下角：按原纵坐标保持顺序，保留原行距，并从右下角向上增长；禁止把每条都写入同一坐标。弹窗实际由右侧显示器的 RenderTexture 摄像机渲染，移动时必须在该源摄像机的 viewport 内换算，禁止使用 `Camera.main`，否则对象会被移出显示器而彻底消失。找不到源摄像机时保留原位置。该开关默认开启并支持 F5 热重载，关闭后恢复已记录的原始位置。
- 上述浮窗移到右下角后，右侧输出的 `ScrollBar3D` 相对内容高度和对应 `ScrollArea` 世界高度都必须额外增加 `3 × lineHeight`；只移动文本或只扩大其中一个范围会造成末尾内容仍被遮挡或滚动条比例失真。该余量跟随 `NewWordPromptLowerRight` 开关启停。
- 参考页的 `ClipboardCopyButton` 与正文 TMP 是同一 `Area` 下的独立兄弟对象，原坐标按英文行宽写死。译文模式必须用 `TMP_TextInfo.lineInfo` 的实际排版右边界，把匹配复制值（科学记数法无法直接匹配时按字段名兜底）的按钮换算到共同父坐标系并紧贴行末；F5、F8 和场景加载后都重新计算，原文模式恢复预制体坐标。参考页正文中连续空行还承担给独立图片和注释预留空间的排版作用；若中文段落比英文少占视觉行，必须补偿相同数量的显式空行，并用完整稳定键回归测试，禁止仅按文本字数猜测位置。
- 左侧主菜单的按钮碰撞/点击宽度统一沿用“传输”按钮，但每个图标必须分别紧贴在自身中文文字右侧，不得排成统一的末尾列。ControlRoom 启动动画会在 `sceneLoaded` 后继续改写按钮 Transform，因此首次应用后要做有限次延后重排；不得依赖玩家先用 F8 往返一次来纠正布局。
- 左上角歌曲标题由 `AnalogTextBanner` 保存完整源串，再逐帧删除首字符形成滚动效果。必须在 `SoundtrackTitleView.DisplayTitle()` 把完整字符串交给滚动器之前翻译歌名；禁止仅在 `TMP_Text.set_text` 的逐帧结果上匹配，否则完整中文滚过后，失去开头的英文残片（如 `ING SOMEWHERE`）会重新显示。
- 取名界面由运行时补丁把前置 `Dr.` 标签移到输入框右侧并显示为“博士”，形成“{输入框}博士”；输入框本身只保存玩家输入的姓名。中文布局复用原版“前缀 + 输入框”的总宽度：输入框占用左侧释放出的空间，右侧按当前字体的实际首选宽度为“博士”预留位置；标签高度与输入框对齐，并强制单行、溢出可见。F5、F8、场景重载前都先恢复原始 RectTransform，禁止重复应用导致布局漂移。纯原文模式完整恢复 `Dr. + 姓名` 及原始 TMP 设置。
- 所有输入框必须关闭 TMP 的自定义字符验证并允许完整 Unicode。游戏使用 Unity 6000.0.73f1；`TMP_InputField.ActivateInputFieldInternal()` 会正常设置 `Input.imeCompositionMode = On` 并读取 `Input.compositionString`，但游戏自己的 `InputManager.LateUpdate()` 只有 `ldc.i4.2`、调用 `Input.set_imeCompositionMode`、`ret` 三条 IL 指令，即每帧把 IME 强制设为 `Off`。Windows 11 运行时探针也确认 TMP 聚焦后状态从 `On` 立即变为 `Off`，随后 IMM32 上下文被解除。补丁必须只跳过这个强制关闭方法，让 TMP 按焦点管理输入法；不得把 IME 永久强制为 `On`，否则普通游戏键盘输入会受影响。
- 场景内输入框聚焦时，候选窗位置必须跟随真实 TMP 文本光标：先从 `TMP_TextInfo.characterInfo` 取得插入点在文本 RectTransform 中的局部位置，再转换为世界坐标，最后把 Unity 左下原点转换为 Windows 左上原点后写入 `Input.compositionCursorPos`。`TextMeshProUGUI` 继续使用 TMP 自带逻辑；`InputTextDummy` 临时绑定的 3D `TextMeshPro` 由内部摄像机渲染到 RenderTexture，必须按完整显示链路换算：源摄像机世界坐标 → RenderTexture viewport/UV → 承载该纹理的物理监视器网格 → 主摄像机屏幕坐标。`WorldSpaceClicker.CursorXBounds/CursorYBounds` 只能作为找不到显示网格时的回退，不能当作最终游戏窗口边界。空输入框以文本视口左下方作为光标世界位置；不得使用固定坐标或鼠标点击点近似。
- 场景共用输入框使用 `MultiLineNewline`。Input System 会在 TMP 的键盘更新前发送导航 Submit，因此普通 Enter 的早期 `TMP_InputField.OnSubmit` 必须对已聚焦的共用多行输入框予以拦截，随后仍由 TMP 正常插入换行。游戏另把 Ctrl+Enter 绑定为提交答案；该组合键必须在 `OnUpdateSelected` 前跳过本帧 TMP 文本更新，避免先在光标处插入 LF 再提交。这个跳过条件必须同时要求“已跟踪、已聚焦、多行、Enter 本帧按下、Ctrl 按住”，不得影响纯 Enter、粘贴换行或其他输入框。清除自定义输入验证器时必须先清 `inputValidator`，再把 `characterValidation` 设为 `None`，避免 TMP setter 把验证模式反向改回 `CustomValidator`。
- 当前游戏程序集只有三个底层 `TMP_InputField`：`NameTranslator.nameEntryInput`（起名）、`ProgressLog.translatorInput`（翻译员想法）和复用的 `InputTextDummy.inputField`（词典命名、单行/多行文本及谜题输入）。构建前运行 `tools/audit_unicode_inputs.ps1`；以后游戏版本新增或移除输入框时必须显式更新覆盖逻辑，禁止静默漏过。
- 谜题输入转回信号时不按空格分词：`C_Reformatter.CompileStringToSignal()` 先删除普通空格，再把 `UserDictionary.keys` 按字符串长度降序扫描；较长词条命中后先占用对应区段并在两侧插入 LF，最后由 `SignalCompiler` 以 LF（`TOKEN_DELIMITER = 10`）分隔并逐项精确查词典。这套逻辑本身已是语言无关的“最长词条优先”，中文输入不得另加逐字切分或重复分词。原生多行输入必须把 Windows CRLF 规范成单个 LF 并保留，不能像姓名输入一样删除换行。
- `NameEntry()` 会先激活起名对象，再清空文本并启动输入协程。补丁必须在该方法完成后通过 Postfix 应用一次布局；场景扫描时跳过未激活的起名对象。禁止在输入框首次聚焦时再次强制刷新 Canvas、LayoutRebuilder 或整套 RectTransform，否则提示、输入行和确认文字可能被二次重排到同一位置。
- 中文起名界面必须把“姓名已占用”提示、两行姓名提示、输入框与“博士”、确认提示作为一个整体布局。以姓名提示原中心为基准，按各文本的 TMP 首选高度和至少 16 像素间距从上到下排列；三块提示文本统一扩展到原“Dr. + 输入框”的总宽度。所有边界只按各根 RectTransform 的四角计算，禁止把文本视口、Placeholder、Caret 等子节点纳入根节点中心计算。
- 禁止创建额外的 Win32 输入窗口、覆盖 TMP 输入框、转移键盘焦点或自行切换系统键盘布局。所有输入必须继续使用游戏原生 `TMP_InputField`，仅修复游戏强制关闭 IME 的错误并放宽字符验证；游戏原有的提交、取消、非空、保留名检查和姓名 14 字符上限继续生效。
- 普通 `component_string` 以及 `DialogueManager.autoLogStartFrame` 使用的旧系统 TMP 字体会把 U+2026 `…` 错绘为 `à`；这些路径必须使用 ASCII 三点 `...`。普通角色对话和其他内嵌对白使用不同字体，只能使用成对的中文省略号 `……`，不得使用 `...`、更长的连续半角句点或单个 `…`。纯省略号对白即使不含英文字母也必须进入提取与翻译。构建期必须按 `kind + field_path` 校验，禁止把限制错误扩大到旧系统字体文本。
- `ConsoleLoaderMessage` 的 `loadingSignalMsg/tokenizingSignalMsg/correctInput/wrongInput/sendingSignalMsg/recompilingMsg/updatingMsg`，以及谜题日志的 `winResponseLine/loadingTxt/failedToRetrieveResponse` 都是运行时可见文本。它们的字段名不符合通用提示词规则，提取器必须通过显式白名单纳入；字段白名单及十条定稿译文均须有回归测试。
- 对话日志的说话者前缀必须跟随显示模式：译文模式使用角色中文名开头的 `埃/巴/科/多`，自动日志使用完整的 `日志`，驾驶员和副驾驶员使用 `驾/副`；原文模式仍使用 `A/B/C/D/L/P/Q`。F8 切换模式时重建日志文本，禁止让科林斯继续显示为 `C`。
- 日志列表会在 `DialogueBank.SetDataFromLoad()` 内、对白库本地化 Postfix 执行前调用 `LogWindow.SetInitialLog()`，提前把当时的 `DialogueChunk.LogName` 复制进 `DialogueLogEntry`。因此不能只改 `DialogueChunk.logName`；必须在两个 `DialogueLogEntry.Configure()` 重载完成后按 `UniqueID` 解析当前显示模式的标题，并在 F5、F8 和对白库注册后刷新已存在的列表项。标题长度限制在翻译完成后再应用，日志详情页标题也使用同一解析入口。
- 成就名称与说明、谜题/元素/歌曲等显示数据，以及周期表拼接片段均属于 UI，应在显示时翻译。
- 精确映射不到的文本保持原文。
- 不对所有 `TMP_Text.text` 做无条件机器翻译。
- 传输控制台、显示器启动日志及其他代码风格界面中的人类可读英文必须翻译；不能因为它看起来像日志或代码就当成内部文本。
- 普通系统字段 `component_string` 所用的旧 TMP 字体资产会把 `U+2026` 显示为 `à`，因此这类字段中的省略号固定写成 ASCII `...`；使用另一套字体且已确认显示正常的 `dialogue_frame` / `component_dialogue_frame` 继续使用中文省略号 `……`。同一条系统提示若另有静态 `ui_text` 副本，两份译文必须统一使用 `...`。
- 构建期必须对全部运行时译文检查意外重复的中文句号、逗号、顿号、分号和冒号。检查前须剥离 `{SPEAKER_*}`、`{PART_nnn}`、`$anim*` 与 TMP 富文本标签，因为这些控制标记在画面上不可见；例如 `没错。$animD19。` 实际会显示为 `没错。。`。规范的 `……`、`——` 以及角色有意使用的连续问号、叹号不属于此类错误。
- 外星信号本体、玩家输入、玩家给词典条目起的名字和运行时信号解析结果保持动态，不做静态替换。

### 7.4 字体

- 汉化包必须提供包含简体中文字形的 TextMeshPro 字体资源或 fallback。
- 场景加载后给相关 `TMP_FontAsset` 添加中文 fallback。
- 输入框也必须使用中文 fallback，以允许玩家输入中文词名。
- 字体替换不得影响外星信号使用的专用字体和材质。
- 不得假定所有玩家环境都已安装“Microsoft YaHei / 微软雅黑”；标准桌面版 Windows 10/11 通常包含它，但精简系统、Server/WinPE、Wine/Proton 等环境不可靠。
- 不得随补丁分发 Windows 自带的 `msyh.ttc`、`msyhbd.ttc`、`msyhl.ttc`，也不得把这些文件转换成 TextMeshPro 字体资源后分发。它们只能在玩家已获许可的 Windows 设备上按系统字体名称调用。
- 正式补丁默认随包携带 Fusion Pixel Font 12px 非等宽简体中文 OTF：`fusion-pixel-12px-proportional-zh_hans.otf`（SIL Open Font License 1.1），取自官方 GitHub Release 2026.07.20，并同时携带官方 `OFL.txt`。游戏原拉丁字体仍作为主字体，Fusion Pixel Font 仅为缺失中文字形提供 fallback。
- 本游戏的 TextMeshPro 提供从字体文件路径动态创建 `TMP_FontAsset` 的公开接口，因此优先直接加载随包的 OTF，再注册到 `TMP_Settings.fallbackFontAssets`；无需依赖系统安装，也无需预制 AssetBundle。
- 默认字体解析顺序：随包开放字体 -> 玩家配置的字体文件 -> 已安装的系统字体候选 -> 保持游戏原字体并记录错误。系统候选可包括 `Microsoft YaHei`、`Noto Sans CJK SC`、`SimHei`，但仅作兜底。
- 为保证翻译正文及玩家给外星词条输入的中文都能显示，正式版使用官方 `CN` 区域子集（它不是按本项目译文字符裁剪的自制子集）；不要再按当前译文字符二次裁剪，否则玩家输入未收录汉字时会出现方框字。
- 建议配置项：`FontSource = Auto|Bundled|System|File`、`BundledFont`、`FontFile`、`SystemFontCandidates`。正式分发默认 `Auto`，且 `Auto` 优先使用随包开放字体，以保证不同机器排版一致。

## 8. 显示模式

配置至少支持：

```ini
[Localization]
Enabled = true
ToggleModeHotkey = F8
ReloadTranslationsHotkey = F5
FallbackToOriginal = true

TranslateDialogue = true
TranslateLogs = true
TranslateUI = true

[DialogueColors]
Enabled = true
Akers = #FFD166
Bautista = #7FDBFF
Collins = #FF9BD2
Doppler = #A7E87B
AutoLog = #E6E6E6
Pilot = #C7B8FF
CoPilot = #FFB07C
```

外星信号保护是不可配置的内部硬规则，不提供 `TranslateSignals` 选项，避免让用户误以为可以开启信号翻译。

`ToggleModeHotkey` 使用 BepInEx `KeyboardShortcut` 语法。单键可写 `F8`；组合键把最后按下的主键写在前面，例如 `F8 + LeftControl`；写 `None` 可禁用运行时切换。按键后插件从保存的原文重新生成对白、日志、UI 和系统模板。

启动显示模式固定为仅译文，不再暴露 `DisplayMode` 配置项。F8 切换只影响当前运行会话；重启游戏后恢复仅译文。

显示模式只有“仅译文”和“仅原文”两种，任何时刻只显示其中一种。

`ReloadTranslationsHotkey` 使用相同语法，默认为 `F5`。按下后重新读取 `DeepSpaceChinese\Translations` 下的四个 JSON、根目录 INI 的 `[Font]`、`[DialogueColors]` 和中文字体文件；只有四个 JSON 全部成功解析且至少读到一条译文时才替换当前内存中的译文。字体根据配置和文件 SHA-256 指纹判断是否变化，未变化时不重建；变化时先创建新 `TMP_FontAsset`，成功后才替换全局 fallback，失败则保留旧字体。译文与字体重载互相独立，随后统一重新应用对白、日志、UI 和系统模板，并用新字体重新测量后续对白宽度。

F5 还必须重建当前正在逐字显示的对白、个人日志正文和标题映射。重建后要保留“旧译文状态 → 新译文状态”的临时别名，直到原逐字协程结束；否则协程下一帧仍会提交旧字符串，造成热加载不生效或文字重叠。

开发测试可按 `F6` 输入 `dialogue:<chunk id>/frame:<frame id>` 稳定键，在真实 `ProgressLog` 组件中直接预览指定个人日志；例如 `dialogue:55/frame:3`。再次按 `F6` 关闭预览。该入口只用于排版与热加载验证，不修改日志存档或章节进度。

### 8.1 纯译文模式

```text
我们终于收到了回复。
```

缺少或未通过校验的译文在 `FallbackToOriginal = true` 时显示原文。

### 8.2 纯原文模式

```text
We finally received a response.
```

规则：

- F8 在纯译文和纯原文之间互斥切换，任何时刻只显示其中一种。
- `translated_text` 永远只保存译文；原文从运行时保存的游戏原始值恢复。
- 纯原文模式必须完整保留原文的 `$anim...` 指令、实时信号和玩家姓名替换。
- 两种模式都使用显示当刻的实时信号词典结果，不能在翻译文件中固化某个解读状态。

### 8.3 说话者颜色

- `DialogueColors.Enabled = true` 时，主对白字幕、非日志字幕及非日志说话者标题按角色着色；关闭后恢复游戏组件的原始颜色。
- 颜色直接设置到 TMP 组件，不向正文插入 `<color>` 标签，避免影响逐字计数、分页和动画命令。
- 默认色以黑色和深蓝黑背景为基准，最低对比度高于 9:1；用户颜色格式固定为 `#RRGGBB`，无效值回退为该角色默认色。
- 默认色：埃克斯 `#FFD166`、巴蒂斯塔 `#7FDBFF`、柯林斯 `#FF9BD2`、多普勒 `#A7E87B`、自动日志 `#E6E6E6`、飞行员 `#C7B8FF`、副驾驶 `#FFB07C`。

### 8.4 单行对白自动适配

`{PART_nnn}` 只保留原游戏的逐字速度、动画和停顿边界，并不天然代表显示换行。游戏会持续拼接后续 `clearPrev = false` 的 part，直到遇到 `clearPrev = true` 才清空单行字幕框。因此不能仅靠约束 JSON 中每个 PART 的字数来解决截断。

运行时在 `DialogueManager.WriteFrameRoutine` 和 `NonLogDialogueManager.WriteFrameRoutine` 入口对仅用于本次显示的 `DialogueFrame` 副本排版，不修改 DialogueBank、日志、存档或翻译 JSON。排版时使用当前 TMP 字体、文本框实际可用宽度，并先展开玩家姓名和实时信号词：

- 总宽度不超过文本框上限：保持原字号、原分段。
- 超出上限但不超过上限的 1.5 倍：保持一页，启用 TMP 自动字号，最小字号为原字号的 66.7%。逐字显示过程中不临时拆字。
- 超过上限的 1.5 倍：按标点或空格优先分页；找不到自然断点时才按字符边界拆分。续页前注入 `clearPrev = true`。
- 自动分页必须保留 `$anim...`、TMP 富文本、实时信号、每个原 part 的 `charDelay` 和最终 `msgDelay`；富文本跨页时重新闭合并打开样式标签。
- 构建期长度扫描只是发现异常长译文的回归护栏，不能替代运行时测量；字体、玩家姓名、信号释义和当前显示模式都会改变最终宽度。

## 9. 游戏更新与可恢复性

- 每个条目同时保存 `stable_key` 和 `source_sha256`。
- 游戏更新后重新提取，并按 `stable_key` 对齐。
- 键相同且哈希相同：保留已有译文和状态。
- 键相同但哈希变化：标记为 `UNTRANSLATED` 或 `needs_review`，不得静默复用旧译文。
- 新键：新增为 `UNTRANSLATED`。
- 消失的键：保留在迁移报告，不写入新翻译包。
- 注入插件、翻译缓存和最终翻译包都要记录目标游戏版本。

## 10. 实施顺序

1. 编写只读运行时提取器，导出所有 `DialogueChunk`、静态 UI 和定位元数据。
2. 生成兼容 `ainiee-translate` 的 `work/cache.json`。
3. 生成人工可审查的分类报告，确认外星信号全部被排除。
4. 建立并锁定 `glossary.locked.json` 和 `user_prompt.md`。
5. 先抽样翻译一小批对白，验证标记保持、语气、UI 长度和分段。
6. 完成翻译、润色、`verify` 和 `scan --mode all`。
7. 运行专用桥接器，生成 `dialogue.json`、`titles.json`、`ui.json`、`system.json`。
8. 实现 BepInEx 运行时注入、日志重建、UI 映射和字体 fallback。
9. 测试纯译文、纯原文、未解读信号、已解读信号、玩家改名、存档载入和游戏更新回退。

## 11. 当前实现与分发约定

截至游戏 v0.10 的正式翻译与运行时构建结果：

- 有效译文：11,127 条；排除的不可翻译/玩法数据：1,372 条。
- `dialogue.json`：6,984 条全部对白。
- `titles.json`：1,192 条日志标题。
- `ui.json`：2,030 条界面文本。
- `system.json`：931 条系统提示与运行时模板。
- 外星信号数组、谜题答案和玩家词典没有进入待翻译集合。

根目录分发结构固定为：

```text
游戏根目录/
├─ DeepSpaceChinese.ini              # 唯一直接暴露给用户的配置文件
├─ DeepSpaceChinese.ConfigEditor.exe # 26 KiB、无第三方依赖的 WinForms 配置编辑器
├─ winhttp.dll
├─ doorstop_config.ini
├─ BepInEx/
│  ├─ core/
│  └─ plugins/DeepSpaceChinese.dll
└─ DeepSpaceChinese/
   ├─ README_简体中文.txt
   ├─ Fonts/fusion-pixel-12px-proportional-zh_hans.otf
   ├─ Licenses/
   └─ Translations/
      ├─ dialogue.json
      ├─ titles.json
      ├─ ui.json
      └─ system.json
```

配置编辑器固定使用 .NET Framework 4.7.2 WinForms，依赖 Windows 已安装的 .NET Framework，不打包 WebView、Chromium 或独立运行时。编辑器直接读写同目录的 `DeepSpaceChinese.ini`，提供常规开关、界面排布、快捷键、七种角色颜色和字体来源设置；保存时校验配置，并保留未知行及原注释。

编译兼容规则：`[Compatibility] CompilerCaseInsensitive = true` 默认开启，使玩家输入的 `VAR` 能匹配词典中的 `var`。运行时仍保持原版“最长词条优先”的拆词规则；完整的精确大小写匹配优先，若词典中存在仅大小写不同的多个候选且输入无法唯一判断，则继续按未找到词条处理。该设置可按 F5 热重载。

字体固定使用 Fusion Pixel Font 官方 Release 2026.07.20 的 12px 非等宽简体中文 OTF，随包附带 SIL OFL 1.1 原始许可证。BepInEx 使用 5.4.23.5，并附带 LGPL 许可证。最终 ZIP 必须以游戏根目录为压缩包根，用户直接解压即可安装。
