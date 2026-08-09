using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using BepInEx.Logging;
using DeepSpaceChinese;
using Newtonsoft.Json.Linq;

internal static class Program
{
    private static int Main()
    {
        try
        {
            string projectRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "..", "..", "..", "..", ".."));
            string testRoot = Path.Combine(projectRoot, "build", "runtime-selftest");
            Directory.CreateDirectory(testRoot);

            string iniPath = Path.Combine(testRoot, "DeepSpaceChinese.ini");
            File.WriteAllText(iniPath,
                "[Localization]\nToggleModeHotkey=F8\n" +
                "[Font]\nFontSource=Bundled\n");
            var log = new ManualLogSource("DeepSpaceChinese.RuntimeTests");
            PatchConfig config = PatchConfig.Load(iniPath, log);
            Assert(config.DisplayMode == DisplayMode.TranslationOnly, "启动显示模式必须固定为仅译文");
            Assert(config.CompilerCaseInsensitive,
                "编译时忽略英文字母大小写必须默认开启");
            var compilerEntries = new[]
            {
                new System.Collections.Generic.KeyValuePair<string, int>("var", -11),
                new System.Collections.Generic.KeyValuePair<string, int>("=", -2),
            };
            Assert(CompilerCaseCompatibility.NormalizeForReformatter(
                       "VAR 1 = 6", compilerEntries.Select(pair => pair.Key)) == "var 1 = 6" &&
                   CompilerCaseCompatibility.TryResolve("VAR", compilerEntries, out int varSignal) &&
                   varSignal == -11,
                "编译兼容必须把唯一的大小写无关词条恢复为词典中的实际拼写");
            Assert(CompilerCaseCompatibility.NormalizeForReformatter(
                       "varIABLE", new[] { "var", "variable" }) == "variable",
                "重格式化器兼容必须保持原版的最长词条优先规则");
            var ambiguousCompilerEntries = new[]
            {
                new System.Collections.Generic.KeyValuePair<string, int>("var", -11),
                new System.Collections.Generic.KeyValuePair<string, int>("VAR", -12),
            };
            Assert(CompilerCaseCompatibility.NormalizeForReformatter(
                       "VAR", ambiguousCompilerEntries.Select(pair => pair.Key)) == "VAR" &&
                   CompilerCaseCompatibility.TryResolve("VAR", ambiguousCompilerEntries,
                       out int exactSignal) && exactSignal == -12 &&
                   !CompilerCaseCompatibility.TryResolve("VaR", ambiguousCompilerEntries,
                       out _),
                "精确大小写必须优先；仅大小写不同的多个词条不得被模糊匹配误选");
            Assert(!GameInputManagerImePatch.AllowOriginalLateUpdate(),
                "必须阻止游戏 InputManager 每帧把 IME 强制设为 Off");
            UnityEngine.Vector2 imePosition = ImeCursorPositionEngine.ToWindowsScreen(
                new UnityEngine.Vector2(320f, 180f), 1920, 1080);
            UnityEngine.Vector2 clampedImePosition = ImeCursorPositionEngine.ToWindowsScreen(
                new UnityEngine.Vector2(-20f, 1200f), 1920, 1080);
            Assert(imePosition == new UnityEngine.Vector2(320f, 900f) &&
                   clampedImePosition == new UnityEngine.Vector2(0f, 0f),
                "IME 候选窗坐标必须从 Unity 左下原点转换为 Windows 左上原点并限制在屏幕内");
            UnityEngine.Vector2 monitorMapped = ImeCursorPositionEngine.MapMonitorWorldToUnityScreen(
                new UnityEngine.Vector2(-27.85f, 5.59f),
                new UnityEngine.Vector2(-31.05f, -26.8f),
                new UnityEngine.Vector2(4.27f, 5.85f), 2560, 1494);
            Assert(Math.Abs(monitorMapped.x - 1927.53f) < 0.1f &&
                   Math.Abs(monitorMapped.y - 1248.15f) < 0.1f,
                "场景输入光标的旧边界映射回退必须保持稳定");
            Assert(ImeCursorPositionEngine.TryBarycentric(
                       new UnityEngine.Vector2(0.25f, 0.25f),
                       new UnityEngine.Vector2(0f, 0f),
                       new UnityEngine.Vector2(1f, 0f),
                       new UnityEngine.Vector2(0f, 1f), out UnityEngine.Vector3 weights) &&
                   Math.Abs(weights.x - 0.5f) < 0.001f &&
                   Math.Abs(weights.y - 0.25f) < 0.001f &&
                   Math.Abs(weights.z - 0.25f) < 0.001f,
                "RenderTexture 光标坐标必须能通过显示网格 UV 反算到物理监视器位置");
            var discoveryState = new ImeBindingDiscoveryState();
            Assert(discoveryState.TryBegin(7) &&
                   !discoveryState.TryBegin(7) &&
                   !discoveryState.TryBegin(7) &&
                   discoveryState.TryBegin(8),
                "同一场景中的屏幕绑定只能发现一次，切换场景后才允许重新发现");
            var gridUv = new[]
            {
                new UnityEngine.Vector2(0f, 0f), new UnityEngine.Vector2(1f, 0f),
                new UnityEngine.Vector2(0f, 1f), new UnityEngine.Vector2(1f, 1f),
            };
            var gridVertices = new[]
            {
                new UnityEngine.Vector3(0f, 0f, 0f), new UnityEngine.Vector3(10f, 0f, 0f),
                new UnityEngine.Vector3(0f, 20f, 0f), new UnityEngine.Vector3(10f, 20f, 0f),
            };
            Assert(ImeCursorPositionEngine.TryBilinearUvGrid(
                       new UnityEngine.Vector2(0.25f, 0.75f), gridUv, gridVertices,
                       out UnityEngine.Vector3 gridPoint) &&
                   Math.Abs(gridPoint.x - 2.5f) < 0.001f &&
                   Math.Abs(gridPoint.y - 15f) < 0.001f,
                "规则监视器 UV 网格必须能在不依赖运行时三角索引的情况下反算局部坐标");
            Assert(ImeCursorPositionEngine.TryKnownMonitorSurface(
                       new UnityEngine.Vector2(0.1790f, 0.8113f),
                       out UnityEngine.Vector3 knownSurfacePoint) &&
                   Math.Abs(knownSurfacePoint.x - 0.8025f) < 0.0001f &&
                   Math.Abs(knownSurfacePoint.y - -0.00103f) < 0.0001f &&
                   Math.Abs(knownSurfacePoint.z - -0.77825f) < 0.0001f,
                "运行时 Mesh 不可读时必须使用从 level0 提取的固定监视器曲面映射");
            UnityEngine.Vector2 firstLineUv = ImeCursorPositionEngine.ApplyMonitorImeUv(
                new UnityEngine.Vector2(0.1790f, 0.8113f),
                0.8113f,
                new UnityEngine.Vector2(1.15f, 1.15f),
                new UnityEngine.Vector2(-0.075f, -0.075f));
            UnityEngine.Vector2 laterLineUv = ImeCursorPositionEngine.ApplyMonitorImeUv(
                new UnityEngine.Vector2(0.25f, 0.7175f),
                0.7862f,
                new UnityEngine.Vector2(1.15f, 1.15f),
                new UnityEngine.Vector2(-0.075f, -0.075f));
            Assert(Math.Abs(firstLineUv.x - 0.13085f) < 0.0001f &&
                   Math.Abs(firstLineUv.y - 0.8113f) < 0.0001f &&
                   Math.Abs(laterLineUv.y - 0.707195f) < 0.0001f &&
                   ImeCursorPositionEngine.ApplyMonitorImeUv(
                       new UnityEngine.Vector2(0.5f, 0.5f), 0.5f,
                       new UnityEngine.Vector2(1.15f, 1.15f),
                       new UnityEngine.Vector2(-0.075f, -0.075f)) ==
                   new UnityEngine.Vector2(0.5f, 0.5f),
                "候选框横向必须采用材质 UV 变换，纵向必须保持第一行基线并按 1.15 倍修正后续行距");
            Assert(SharedInputReturnCompatibility.ShouldSuppressEarlySubmit(true, true,
                        true, false, true) &&
                   !SharedInputReturnCompatibility.ShouldSuppressEarlySubmit(false, true,
                        true, false, true) &&
                   !SharedInputReturnCompatibility.ShouldSuppressEarlySubmit(true, true,
                        false, false, true) &&
                   !SharedInputReturnCompatibility.ShouldSuppressEarlySubmit(true, true,
                        true, true, false),
                "已聚焦的场景多行输入框必须拦截 Input System 导航 Submit，且不依赖旧输入 API 的回车状态");
            config.DisplayMode = DisplayMode.OriginalOnly;
            Assert(config.ToggleModeHotkey.MainKey.ToString() == "F8", "INI 快捷键解析失败");
            Assert(config.SpeakerColorsEnabled && config.AkersColor == "#FFD166",
                "说话者颜色默认配置解析失败");
            var defaultColors = new PatchConfig();
            string[] readableColors =
            {
                defaultColors.AkersColor, defaultColors.BautistaColor, defaultColors.CollinsColor,
                defaultColors.DopplerColor, defaultColors.AutoLogColor, defaultColors.PilotColor,
                defaultColors.CoPilotColor,
            };
            Assert(readableColors.All(color => ContrastRatio(color, "#101820") >= 9.0),
                "默认说话者颜色在深蓝黑背景上的对比度不足 9:1");

            File.WriteAllText(iniPath,
                "[DialogueColors]\nEnabled=false\nAkers=#123456\nCollins=invalid\n");
            config.ReloadDialogueColorSettings(iniPath, log);
            Assert(!config.SpeakerColorsEnabled && config.AkersColor == "#123456" &&
                   config.CollinsColor == "#FF9BD2",
                "说话者颜色热重载、关闭开关或无效颜色回退失败");

            File.WriteAllText(iniPath,
                "[Localization]\nToggleModeHotkey=F8\n" +
                "[Compatibility]\nCompilerCaseInsensitive=false\n" +
                "[Font]\nFontSource=File\nFontFile=CustomChinese.otf\n" +
                "SystemFontCandidates=Test Sans;Test Hei\n");
            config.ReloadCompatibilitySettings(iniPath, log);
            Assert(!config.CompilerCaseInsensitive,
                "编译大小写兼容开关必须能从 INI 热重载");
            config.ReloadFontSettings(iniPath, log);
            Assert(config.FontSource == "File" && config.FontFile == "CustomChinese.otf" &&
                   config.SystemFontCandidates.SequenceEqual(new[] { "Test Sans", "Test Hei" }),
                "F5 所用的字体配置热重载失败");

            string fingerprintFont = Path.Combine(testRoot, "fingerprint-font.otf");
            File.WriteAllText(fingerprintFont, "font-version-a");
            File.WriteAllText(iniPath,
                "[Font]\nFontSource=Bundled\nBundledFont=fingerprint-font.otf\n");
            config.ReloadFontSettings(iniPath, log);
            var fontFallback = new FontFallback(config, testRoot, log);
            string fingerprintA1 = fontFallback.CurrentFingerprintForTests();
            string fingerprintA2 = fontFallback.CurrentFingerprintForTests();
            File.WriteAllText(fingerprintFont, "font-version-b");
            string fingerprintB = fontFallback.CurrentFingerprintForTests();
            Assert(fingerprintA1 == fingerprintA2 && fingerprintA1 != fingerprintB,
                "字体内容指纹必须在文件未变时稳定、文件变化后更新");

            RunDialogueLayoutTests();
            RunLiveDialogueSwitchTests();

            string protectedText = TokenCodec.ProtectRuntimeTokens("Hello |-160, the Translator.");
            Assert(protectedText == "Hello {SIG_N160}, {PLAYER_NAME}.", "运行时标记保护失败");
            string restored = TokenCodec.RestoreRuntimeTokens(protectedText, "Lin");
            Assert(restored == "Hello |-160, Lin.", "运行时标记还原失败");

            var componentEntry = new RuntimeTranslationEntry
            {
                SourceText = "Translator / v{DYN_0} / {PLAYER_NAME}",
                Game = new JObject
                {
                    ["protect_player_name"] = false,
                    ["player_token_literal"] = "TRANSLATION",
                    ["runtime_tokens"] = new JObject { ["XXX"] = "{DYN_0}" },
                },
            };
            string componentSource = TokenCodec.ProtectForEntry(
                "Translator / vXXX / TRANSLATION", componentEntry);
            Assert(componentSource == componentEntry.SourceText,
                "静态 Translator 或组件运行时标记保护失败");
            string componentRestored = TokenCodec.RestoreForEntry(
                "翻译员 / v{DYN_0} / {PLAYER_NAME}", componentEntry, "Lin");
            Assert(componentRestored == "翻译员 / vXXX / Lin", "组件运行时标记还原失败");

            var templateEntry = new RuntimeTranslationEntry
            {
                SourceText = "Transmission: {DYN_0}, Time: {DYN_1}",
                TranslatedText = "传输：{DYN_0}，时间：{DYN_1}",
            };
            Assert(UiTemplateRenderer.TryRender(templateEntry, "Transmission: 7, Time: 01:23",
                    out string renderedTemplate) && renderedTemplate == "传输：7，时间：01:23",
                "动态 UI 模板匹配或参数恢复失败");
            const string compilerError =
                "- Compilation Failed -\n1 - Entry not found: \nEntry not found\n";
            Assert(CompilerErrorRuntime.IsCompilerError(compilerError) &&
                   CompilerErrorRuntime.Format(compilerError, DisplayMode.TranslationOnly) ==
                   "- 编译失败 -\n1 - 未找到词条：\nEntry not found\n" &&
                   CompilerErrorRuntime.Format(compilerError, DisplayMode.OriginalOnly) ==
                   compilerError,
                "编译错误标题和类型必须翻译，但导致错误的玩家输入必须保持原文");

            var nested = new NestedHolder();
            Assert(ReflectionPath.TrySetValue(nested, "frames[0].parts[0].txt", "你好") &&
                   ReflectionPath.TryGetValue(nested, "frames[0].parts[0].txt", out object nestedValue) &&
                   (string)nestedValue == "你好", "嵌套序列化字段回注失败");
            string originalOnly = TokenCodec.FormatDisplay("你好，{PLAYER_NAME}。", "$animA2Hello, the Translator.",
                config, "Lin");
            Assert(originalOnly == "$animA2Hello, Lin." && !originalOnly.Contains("你好"),
                "仅原文模式必须只显示原文，并完整保留原文动画命令");
            config.DisplayMode = DisplayMode.TranslationOnly;
            string translationOnly = TokenCodec.FormatDisplay("你好，{PLAYER_NAME}。", "$animA2Hello, the Translator.",
                config, "Lin");
            Assert(translationOnly == "你好，Lin。" && !translationOnly.Contains("Hello"),
                "仅译文模式必须只显示译文");
            Assert(PlayerNameRuntime.FormatFullName("林", "Dr. 林", DisplayMode.TranslationOnly) == "林博士" &&
                   PlayerNameRuntime.FormatFullName("林博士", "Dr. 林博士", DisplayMode.TranslationOnly) == "林博士" &&
                   PlayerNameRuntime.FormatFullName("林", "Dr. 林", DisplayMode.OriginalOnly) == "Dr. 林",
                "玩家姓名必须在译文模式使用中文后置称谓，并在原文模式恢复原语序");
            Assert(DialogueLocalizer.LogSpeakerPrefix(0, DisplayMode.TranslationOnly) == "埃" &&
                   DialogueLocalizer.LogSpeakerPrefix(1, DisplayMode.TranslationOnly) == "巴" &&
                   DialogueLocalizer.LogSpeakerPrefix(2, DisplayMode.TranslationOnly) == "科" &&
                   DialogueLocalizer.LogSpeakerPrefix(3, DisplayMode.TranslationOnly) == "多" &&
                   DialogueLocalizer.LogSpeakerPrefix(4, DisplayMode.TranslationOnly) == "日志" &&
                   DialogueLocalizer.LogSpeakerPrefix(2, DisplayMode.OriginalOnly) == "C",
                "译文日志必须使用中文角色名首字，原文日志仍使用拉丁首字母");
            var logTitleEntry = new RuntimeTranslationEntry
            {
                StableKey = "dialogue:42/title",
                Kind = "dialogue_title",
                SourceText = "THE MISSION",
                SourceSha256 = TokenCodec.Sha256("THE MISSION"),
                TranslatedText = "任务",
            };
            Assert(DialogueLocalizer.ResolveLogTitleForTests(logTitleEntry, "THE MISSION", config,
                       "林博士") == "任务" &&
                   LogTitleRuntime.TruncateForTests("这是一个超过十二个汉字的日志标题", 12, "...") ==
                   "这是一个超过十二个汉字的...",
                "日志列表项必须在生成时解析当前模式的标题，并在翻译后按列表上限截断");
            NameSuffixLayout suffixLayout = NameSuffixLayoutEngine.Calculate(
                containerLeft: 0f, containerRight: 360f,
                inputLeft: 24f, inputRight: 340f,
                suffixPreferredWidth: 48f, gap: 8f);
            Assert(suffixLayout.InputRight <= suffixLayout.SuffixLeft - 8f &&
                   suffixLayout.SuffixRight <= 360f &&
                   suffixLayout.SuffixRight - suffixLayout.SuffixLeft >= 48f,
                "中文取名布局必须为“博士”预留同一行宽度，不能换行或被父级遮罩裁切");
            Assert(!NameLayoutApplicationPolicy.ShouldApplyDuringScan(activeInHierarchy: false) &&
                   NameLayoutApplicationPolicy.ShouldApplyDuringScan(activeInHierarchy: true) &&
                   !NameLayoutApplicationPolicy.ShouldRelayoutOnFocus,
                "起名布局只能在激活后应用一次，禁止焦点到来时再次强制重排整套 UI");
            string playerNameRuntimeSource = File.ReadAllText(Path.Combine(projectRoot,
                "src", "DeepSpaceChinese", "PlayerNameRuntime.cs"));
            Match sharedInputMethod = Regex.Match(playerNameRuntimeSource,
                @"public void ApplySharedInput\(InputTextDummy dummy\)(?<body>[\s\S]*?)\n    public void UpdateImeCursorPosition");
            Match unicodeInputMethod = Regex.Match(playerNameRuntimeSource,
                @"private static void ConfigureUnicodeInput\(TMP_InputField input\)(?<body>[\s\S]*?)\n    private void ApplyLabelLayout");
            string imeCursorSource = File.ReadAllText(Path.Combine(projectRoot,
                "src", "DeepSpaceChinese", "ImeCursorPositionEngine.cs"));
            Assert(sharedInputMethod.Success &&
                   unicodeInputMethod.Success &&
                   !Regex.IsMatch(sharedInputMethod.Groups["body"].Value,
                       @"\.lineType\s*=") &&
                   !Regex.IsMatch(sharedInputMethod.Groups["body"].Value,
                       @"\.lineLimit\s*=") &&
                   !imeCursorSource.Contains("ForceMeshUpdate") &&
                   !playerNameRuntimeSource.Contains("GenerateCaret") &&
                   unicodeInputMethod.Groups["body"].Value.IndexOf("input.inputValidator = null",
                       StringComparison.Ordinal) <
                   unicodeInputMethod.Groups["body"].Value.IndexOf(
                       "input.characterValidation = TMP_InputField.CharacterValidation.None",
                       StringComparison.Ordinal),
                "场景输入兼容不得改写原版行类型/行数上限，也不得强制刷新外部文本 Mesh");
            NameUiVerticalLayout verticalLayout = NameUiVerticalLayoutEngine.Calculate(
                promptCenter: 180f, promptHeight: 136f, rowHeight: 120f,
                goodHeight: 136f, takenHeight: 136f, gap: 16f);
            Assert(verticalLayout.TakenCenter - 68f >= verticalLayout.PromptCenter + 68f + 16f &&
                   verticalLayout.PromptCenter - 68f >= verticalLayout.RowCenter + 60f + 16f &&
                   verticalLayout.RowCenter - 60f >= verticalLayout.GoodCenter + 68f + 16f,
                "起名界面的错误提示、姓名提示、输入行和确认提示必须按实际高度整体重排且互不遮挡");
            NameInputEdit insertedName = NameInputTextEngine.Insert("Li", 0, 2, "林", 14);
            NameInputEdit limitedName = NameInputTextEngine.Insert("一二三四五六七八九十甲乙丙", 13, 13,
                "𠀀", 14);
            Assert(insertedName.Text == "林" && insertedName.Caret == 1 &&
                   limitedName.Text == "一二三四五六七八九十甲乙丙" && limitedName.Caret == 13,
                "中文输入必须按选区写入，且字符上限不能截断代理项对");
            Assert(UnicodeInputCoverage.IsSupported("NameTranslator", "nameEntryInput") &&
                   UnicodeInputCoverage.IsSupported("ProgressLog", "translatorInput") &&
                   UnicodeInputCoverage.IsSupported("InputTextDummy", "inputField") &&
                   !UnicodeInputCoverage.IsSupported("UnknownInput", "inputField"),
                "三类游戏输入框必须全部纳入中文输入兼容覆盖范围");
            Assert(PlayerAuthoredUiText.ShouldPreserve(
                       "ControlRoom/Dictionary Window/Viewport/EntryViewport/" +
                       "Dict E @ -11: VAR/Visuals/Word") &&
                   PlayerAuthoredUiText.ShouldPreserve(
                       "ControlRoom/Input Output Objects/Response Group/Input Display") &&
                   !PlayerAuthoredUiText.ShouldPreserve(
                       "ControlRoom/Calculator Window/Viewport/Edit Var Menu/Name Input/Label"),
                "词典词名和响应输入必须保留玩家原文，静态 VAR 标签仍应正常翻译");
            string translations = Path.Combine(testRoot, "Translations");
            Directory.CreateDirectory(translations);
            string json = "{\n" +
                "  \"format_version\": 1,\n" +
                "  \"game_version\": \"0.10\",\n" +
                "  \"language\": \"zh-CN\",\n" +
                "  \"category\": \"ui\",\n" +
                "  \"entries\": [{\n" +
                "    \"stable_key\": \"ui:test\",\n" +
                "    \"kind\": \"ui_text\",\n" +
                "    \"source_sha256\": \"x\",\n" +
                "    \"source_text\": \"Start\",\n" +
                "    \"translated_text\": \"开始\",\n" +
                "    \"game\": { \"original_text\": \"Start\" }\n" +
                "  }]\n" +
                "}";
            File.WriteAllText(Path.Combine(translations, "ui.json"), json);
            string dynamicJson = "{\n" +
                "  \"format_version\": 1, \"game_version\": \"0.10\", \"language\": \"zh-CN\",\n" +
                "  \"category\": \"ui\", \"entries\": [\n" +
                "    { \"stable_key\": \"ui-template:test\", \"kind\": \"ui_template\", " +
                "\"source_sha256\": \"" + TokenCodec.Sha256("Value: {DYN_0}") + "\", " +
                "\"source_text\": \"Value: {DYN_0}\", \"translated_text\": \"数值：{DYN_0}\", " +
                "\"game\": { \"template_id\": \"test\" } },\n" +
                "    { \"stable_key\": \"achievement:test:name\", \"kind\": \"achievement_name\", " +
                "\"source_sha256\": \"" + TokenCodec.Sha256("Hello World!") + "\", " +
                "\"source_text\": \"Hello World!\", \"translated_text\": \"你好，世界！\", " +
                "\"game\": { \"original_text\": \"Hello World!\" } }\n" +
                "  ]\n}";
            File.WriteAllText(Path.Combine(translations, "dynamic.json"), dynamicJson);
            string displayJson = "{\n" +
                "  \"format_version\": 1, \"game_version\": \"0.10\", \"language\": \"zh-CN\",\n" +
                "  \"category\": \"ui\", \"entries\": [\n" +
                "    { \"stable_key\": \"display:test\", \"kind\": \"display_value\", " +
                "\"source_sha256\": \"" + TokenCodec.Sha256("Hydrogen") + "\", " +
                "\"source_text\": \"Hydrogen\", \"translated_text\": \"氢\", " +
                "\"game\": { \"original_text\": \"Hydrogen\" } },\n" +
                "    { \"stable_key\": \"ui-fragment:test\", \"kind\": \"ui_fragment\", " +
                "\"source_sha256\": \"" + TokenCodec.Sha256("Stable: yes") + "\", " +
                "\"source_text\": \"Stable: yes\", \"translated_text\": \"稳定：是\", " +
                "\"game\": { \"original_text\": \"Stable: yes\" } }\n" +
                "  ]\n}";
            File.WriteAllText(Path.Combine(translations, "display.json"), displayJson);
            TranslationStore store = TranslationStore.Load(translations, log);
            Assert(store.Count == 5 && store.TryGet("ui:test", out RuntimeTranslationEntry entry) &&
                   entry.TranslatedText == "开始", "运行时 JSON 加载失败");
            Assert(store.UiTemplates.Count() == 1 &&
                   store.FindUnambiguousAchievement("Hello World!")?.TranslatedText == "你好，世界！",
                "动态模板或成就显示译文索引失败");
            Assert(store.FindUnambiguousDisplayValue("Hydrogen")?.TranslatedText == "氢" &&
                   store.UiFragments.Count() == 1, "动态显示值或富文本片段索引失败");

            Console.WriteLine("Runtime self-test passed: INI, hotkey, JSON, tokens, original/translation display, dialogue layout.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static void RunDialogueLayoutTests()
    {
        const string reportedLine = "我们开始调查人类历史上第一次与外星生命的接触。";
        DialogueLayoutResult shrunk = DialogueLayoutEngine.Fit(
            new[] { new DialogueLayoutPart(reportedLine, 0.02f, false, 0.4f) },
            19f, 1.5f, ApproximateWidth);
        Assert(!shrunk.WasPaginated && shrunk.Parts.Count == 1 &&
               shrunk.Parts[0].Text == reportedLine,
            "不超过 1.5 倍上限的对白不应分页，应交给 TMP 自动缩小字号");

        string longLine = new string('测', 30);
        DialogueLayoutResult paginated = DialogueLayoutEngine.Fit(
            new[] { new DialogueLayoutPart("$animD00" + longLine, 0.02f, false, 0.4f) },
            19f, 1.5f, ApproximateWidth);
        Assert(paginated.WasPaginated && paginated.Parts.Count == 2,
            "超过 1.5 倍上限的对白应分页");
        Assert(!paginated.Parts[0].ClearPrevious && paginated.Parts[1].ClearPrevious,
            "自动分页必须只在续页前插入清屏标记");
        Assert(paginated.Parts.Sum(part => CountVisible(part.Text)) == 30 &&
               paginated.Parts.Count(part => part.Text.Contains("$animD00")) == 1,
            "分页不得丢字、复制作画指令或改变正文");
        Assert(paginated.Parts.All(part => ApproximateWidth(part.Text) <= 19f),
            "分页后的每一页都必须落在原字号宽度上限内");
        Assert(Math.Abs(paginated.Parts[0].MessageDelay) < 0.001f &&
               Math.Abs(paginated.Parts[1].MessageDelay - 0.4f) < 0.001f,
            "拆分同一 PART 时只能由最后一段继承消息延迟");

        string rich = "<size=75%><u>" + new string('原', 35) + "</u></size>";
        DialogueLayoutResult richPages = DialogueLayoutEngine.Fit(
            new[] { new DialogueLayoutPart(rich, 0f, true, 0f) },
            19f, 1.5f, ApproximateWidth);
        Assert(richPages.WasPaginated && richPages.Parts.All(IsBalancedRichText),
            "富文本跨页时每一页都必须拥有完整、平衡的标签");

        DialogueLayoutResult grouped = DialogueLayoutEngine.Fit(
            new[]
            {
                new DialogueLayoutPart(new string('甲', 15), 0.01f, false, 0.2f),
                new DialogueLayoutPart(new string('乙', 15), 0.03f, false, 0.5f),
            },
            19f, 1.5f, ApproximateWidth);
        Assert(grouped.WasPaginated && grouped.Parts.Last().CharacterDelay == 0.03f &&
               Math.Abs(grouped.Parts.Last().MessageDelay - 0.5f) < 0.001f,
            "跨 PART 分页必须保留各段逐字速度和最终延迟");
    }

    private static void RunLiveDialogueSwitchTests()
    {
        DialogueLayoutPart[] original =
        {
            new DialogueLayoutPart("Hello, ", 0f, false, 0f),
            new DialogueLayoutPart("world.", 0f, false, 0f),
            new DialogueLayoutPart("Next page.", 0f, true, 0f),
        };
        DialogueLayoutPart[] translated =
        {
            new DialogueLayoutPart("你好，", 0f, false, 0f),
            new DialogueLayoutPart("世界。", 0f, false, 0f),
            new DialogueLayoutPart("下一页。", 0f, true, 0f),
        };
        DialogueTextMap map = DialogueTextMap.Create(original, translated, string.Empty,
            value => value);
        Assert(map.TryMap("你好，世界。", DisplayMode.OriginalOnly, out string english,
                   out int chineseLength, out int englishLength) && english == "Hello, world.",
            "当前中文对白必须能在不重启协程的情况下即时映射回英文");
        Assert(map.TryMap("Next page.", DisplayMode.TranslationOnly, out string chinese,
                   out _, out _) && chinese == "下一页。",
            "当前英文对白必须能即时映射为中文，并正确处理清屏后的新页");
        Assert(DialogueTextMap.ScaleVisibleCharacters(chineseLength, chineseLength,
                   englishLength) == englishLength &&
               DialogueTextMap.ScaleVisibleCharacters(3, 6, 12) == 6,
            "切换语言时必须同比换算逐字显示进度，不能截断更长的英文");
        DialogueTextMap titleMap = DialogueTextMap.Create(
            new[] { new DialogueLayoutPart("Alan's Journal: ", 0f, false, 0f) },
            new[] { new DialogueLayoutPart("艾伦的手记：", 0f, false, 0f) },
            string.Empty, value => value);
        Assert(titleMap.TryMap("艾伦的手记：", DisplayMode.OriginalOnly,
                   out string originalTitle, out int producerTitleLength,
                   out int originalTitleLength) && originalTitle == "Alan's Journal: " &&
               DialogueTextMap.RemapVisibleCharacters(
                   visible: producerTitleLength,
                   producerLength: producerTitleLength,
                   oldTargetLength: producerTitleLength,
                   newTargetLength: originalTitleLength) == originalTitleLength,
            "个人日志切到原文时必须显示完整 Alan's Journal:，不能沿用中文标题的可见字符数而只剩 Alan's");
    }

    private static readonly Regex RichTag = new Regex("<[^>]+>", RegexOptions.Compiled);
    private static readonly Regex Animation = new Regex(@"\$anim(?:[A-Za-z]\d{0,2}|\d{1,2})",
        RegexOptions.Compiled);

    private static float ApproximateWidth(string text) => CountVisible(text);

    private static int CountVisible(string text)
    {
        string visible = RichTag.Replace(Animation.Replace(text ?? string.Empty, string.Empty), string.Empty);
        return visible.Length;
    }

    private static bool IsBalancedRichText(DialogueLayoutPart part)
    {
        int sizes = Regex.Matches(part.Text, "<size(?:=[^>]*)?>", RegexOptions.IgnoreCase).Count;
        int sizeEnds = Regex.Matches(part.Text, "</size>", RegexOptions.IgnoreCase).Count;
        int underlines = Regex.Matches(part.Text, "<u>", RegexOptions.IgnoreCase).Count;
        int underlineEnds = Regex.Matches(part.Text, "</u>", RegexOptions.IgnoreCase).Count;
        return sizes == sizeEnds && underlines == underlineEnds;
    }

    private static double ContrastRatio(string foreground, string background)
    {
        double first = RelativeLuminance(foreground);
        double second = RelativeLuminance(background);
        return (Math.Max(first, second) + 0.05) / (Math.Min(first, second) + 0.05);
    }

    private static double RelativeLuminance(string color)
    {
        double Channel(int start)
        {
            double value = Convert.ToInt32(color.Substring(start, 2), 16) / 255.0;
            return value <= 0.04045 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
        }
        return 0.2126 * Channel(1) + 0.7152 * Channel(3) + 0.0722 * Channel(5);
    }
}

internal sealed class NestedHolder
{
    private NestedFrame[] frames =
    {
        new NestedFrame { parts = new[] { new NestedPart { txt = "Hello" } } },
    };
}

internal struct NestedFrame
{
    public NestedPart[] parts;
}

internal struct NestedPart
{
    public string txt;
}
