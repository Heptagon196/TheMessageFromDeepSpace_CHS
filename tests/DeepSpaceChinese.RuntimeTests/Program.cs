using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using BepInEx.Logging;
using DeepSpaceChinese;
using Newtonsoft.Json.Linq;
using UnityEngine;

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
            Assert(config.PuzzleFixesEnabled,
                "题面修正功能必须默认开启");
            Assert(config.MoveNewWordPromptToLowerRight,
                "新单词命名浮窗移动到右下角必须默认开启");
            Vector3 promptLast = TermLoggerLayoutEngine.TargetViewportPoint(
                new Vector3(0.82f, 0.71f, 12f), 2, 3, 0.045f);
            Vector3 promptFirst = TermLoggerLayoutEngine.TargetViewportPoint(
                new Vector3(0.82f, 0.62f, 12f), 0, 3, 0.045f);
            Assert(Math.Abs(promptLast.x - 0.82f) < 0.0001f &&
                   Math.Abs(promptLast.y - 0.12f) < 0.0001f &&
                   Math.Abs(promptLast.z - 12f) < 0.0001f &&
                   Math.Abs(promptFirst.y - 0.21f) < 0.0001f,
                "新单词浮窗列表必须保持右侧横坐标、深度和原顺序，并从右下角向上排列");
            Assert(Math.Abs(ConsoleOutputScrollPadding.AddToWorldHeight(
                       0.8f, 0.04f, enabled: true) - 0.92f) < 0.0001f &&
                   Math.Abs(ConsoleOutputScrollPadding.AddToRelativeMenuHeight(
                       1.2f, 0.04f, 0.5f, enabled: true) - 1.44f) < 0.0001f &&
                   Math.Abs(ConsoleOutputScrollPadding.AddToWorldHeight(
                       0.8f, 0.04f, enabled: false) - 0.8f) < 0.0001f,
                "右侧输出滚动范围必须在新单词浮窗下方增加整整三行余量");
            const string validPuzzleFix =
                "{\"display_id\":80,\"original_signals\":[-11,1,-2,6]," +
                "\"replacement_signals\":[-11,1,-2,7],\"note\":\"test\"}";
            Assert(PuzzleFixRule.TryParse(validPuzzleFix, "80.json",
                       out PuzzleFixRule puzzleFixRule, out string puzzleFixError) &&
                   puzzleFixError == null &&
                   puzzleFixRule.Matches(new[] { -11, 1, -2, 6 }) &&
                   !puzzleFixRule.Matches(new[] { -11, 1, -2, 7 }),
                "题面修正规则必须读取显示编号，并严格匹配原始数字信号");
            Assert(!PuzzleFixRule.TryParse(validPuzzleFix, "81.json",
                       out _, out string wrongFixIdError) &&
                   wrongFixIdError.Contains("display_id"),
                "题面修正文件名必须与游戏显示编号一致");
            const string puzzleFixWithAnswers =
                "{\"display_id\":80,\"original_signals\":[-11,1,-2,6]," +
                "\"replacement_signals\":[-11,1,-2,7]," +
                "\"original_answers\":[[-11,2],[-11,3]]," +
                "\"replacement_answers\":[[-11,4],[-11,5]]}";
            Assert(PuzzleFixRule.TryParse(puzzleFixWithAnswers, "80.json",
                       out PuzzleFixRule answerFixRule, out string answerFixError) &&
                   answerFixError == null && answerFixRule.HasAnswerReplacement &&
                   answerFixRule.OriginalAnswers.Length == 2 &&
                   answerFixRule.ReplacementAnswers.Length == 2,
                "题面修正规则必须支持可选的原始答案集和替换答案集");
            const string incompleteAnswerFix =
                "{\"display_id\":80,\"original_signals\":[1]," +
                "\"replacement_signals\":[2],\"replacement_answers\":[[3]]}";
            Assert(!PuzzleFixRule.TryParse(incompleteAnswerFix, "80.json", out _,
                       out string incompleteAnswerError) &&
                   incompleteAnswerError.Contains("original_answers") &&
                   incompleteAnswerError.Contains("replacement_answers"),
                "答案修正必须同时提供原始答案集和替换答案集");
            const string emptyAnswerFix =
                "{\"display_id\":80,\"original_signals\":[1]," +
                "\"replacement_signals\":[2],\"original_answers\":[]," +
                "\"replacement_answers\":[[]]}";
            Assert(!PuzzleFixRule.TryParse(emptyAnswerFix, "80.json", out _,
                       out string emptyAnswerError) &&
                   emptyAnswerError.Contains("答案集"),
                "答案修正不得接受空答案集或空答案");
            Assert(answerFixRule.TryCreatePlan(
                       new[] { -11, 1, -2, 6 },
                       new[] { new[] { -11, 2 }, new[] { -11, 3 } },
                       out PuzzleFixPlan answerPlan, out string answerPlanError) &&
                   answerPlanError == null &&
                   PuzzleFixRule.SignalsEqual(answerPlan.ReplacementSignals,
                       new[] { -11, 1, -2, 7 }) &&
                   PuzzleFixRule.AnswerSetsEqual(answerPlan.ReplacementAnswers,
                       new[] { new[] { -11, 4 }, new[] { -11, 5 } }),
                "题面和原始答案集匹配时，规则必须生成同时替换题面与答案集的计划");
            Assert(puzzleFixRule.TryCreatePlan(
                       new[] { -11, 1, -2, 6 },
                       new[] { new[] { 999 } },
                       out PuzzleFixPlan legacyPlan, out string legacyPlanError) &&
                   legacyPlanError == null && legacyPlan.ReplacementAnswers == null,
                "未填写答案字段的旧规则必须只替换题面，不检查或改写答案集");
            Assert(!answerFixRule.TryCreatePlan(
                       new[] { -11, 1, -2, 6 },
                       new[] { new[] { -11, 2 }, new[] { -11, 99 } },
                       out _, out string mismatchedAnswersError) &&
                   mismatchedAnswersError.Contains("原始答案集"),
                "原始答案集不匹配时必须原子地拒绝整条修正规则");
            Assert(!answerFixRule.TryCreatePlan(
                       new[] { -11, 1, -2, 99 },
                       new[] { new[] { -11, 2 }, new[] { -11, 3 } },
                       out _, out string mismatchedQuestionError) &&
                   mismatchedQuestionError.Contains("原题面"),
                "原题面不匹配时不得替换答案集");
            const string answerOnlyFix =
                "{\"display_id\":81," +
                "\"original_answers\":[[-11,2],[-11,3]]," +
                "\"replacement_answers\":[[-11,4]]}";
            Assert(PuzzleFixRule.TryParse(answerOnlyFix, "81.json",
                       out PuzzleFixRule answerOnlyRule, out string answerOnlyParseError) &&
                   answerOnlyParseError == null && !answerOnlyRule.HasQuestionReplacement &&
                   answerOnlyRule.HasAnswerReplacement &&
                   answerOnlyRule.TryCreatePlan(new[] { 999 },
                       new[] { new[] { -11, 2 }, new[] { -11, 3 } },
                       out PuzzleFixPlan answerOnlyPlan, out string answerOnlyPlanError) &&
                   answerOnlyPlanError == null &&
                   answerOnlyPlan.ReplacementSignals == null &&
                   answerOnlyPlan.ReplacementAnswers.Length == 1,
                "只提供答案区时必须跳过题面校验并只生成答案集替换");
            const string incompleteQuestionFix =
                "{\"display_id\":82,\"original_signals\":[1]}";
            Assert(!PuzzleFixRule.TryParse(incompleteQuestionFix, "82.json", out _,
                       out string incompleteQuestionError) &&
                   incompleteQuestionError.Contains("original_signals") &&
                   incompleteQuestionError.Contains("replacement_signals"),
                "题面修正必须同时提供非空的原始题面和替换题面");
            const string noActiveFix =
                "{\"display_id\":83,\"original_signals\":[]," +
                "\"replacement_signals\":[],\"original_answers\":[]," +
                "\"replacement_answers\":[]}";
            Assert(!PuzzleFixRule.TryParse(noActiveFix, "83.json", out _,
                       out string noActiveFixError) &&
                   noActiveFixError.Contains("至少"),
                "题面和答案集都为空时不得加载无效规则");
            const string answerOnlyWithEmptyQuestionArrays =
                "{\"display_id\":84,\"original_signals\":[]," +
                "\"replacement_signals\":[],\"original_answers\":[[1]]," +
                "\"replacement_answers\":[[2]]}";
            Assert(PuzzleFixRule.TryParse(answerOnlyWithEmptyQuestionArrays, "84.json",
                       out PuzzleFixRule emptyQuestionRule,
                       out string emptyQuestionError) &&
                   emptyQuestionError == null &&
                   !emptyQuestionRule.HasQuestionReplacement &&
                   emptyQuestionRule.HasAnswerReplacement,
                "题面数组同时为空时应视为未提供题面区，不得阻止答案集修正");
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
            Assert(SharedInputReturnCompatibility.ShouldSkipTextUpdateForSubmitShortcut(
                       true, true, true, true, true) &&
                   !SharedInputReturnCompatibility.ShouldSkipTextUpdateForSubmitShortcut(
                       true, true, true, true, false) &&
                   !SharedInputReturnCompatibility.ShouldSkipTextUpdateForSubmitShortcut(
                       true, true, true, false, true) &&
                   !SharedInputReturnCompatibility.ShouldSkipTextUpdateForSubmitShortcut(
                       false, true, true, true, true),
                "Ctrl+Enter 提交快捷键必须跳过 TMP 的换行处理，纯 Enter 仍须正常换行");
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
                "[Layout]\nNewWordPromptLowerRight=false\n" +
                "[PuzzleFixes]\nEnabled=false\n" +
                "[Font]\nFontSource=File\nFontFile=CustomChinese.otf\n" +
                "SystemFontCandidates=Test Sans;Test Hei\n");
            config.ReloadCompatibilitySettings(iniPath, log);
            Assert(!config.CompilerCaseInsensitive && !config.PuzzleFixesEnabled,
                "兼容项和题面修正开关必须能从 INI 热重载");
            config.ReloadLayoutSettings(iniPath, log);
            Assert(!config.MoveNewWordPromptToLowerRight,
                "新单词浮窗位置开关必须能从 INI 热重载");
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
            string[] compactChineseParts = TokenCodec.ApplyTranslatedWhitespace(
                new[] { "On my end, ", "the investigation ", "is progressing." },
                new[] { "我这边", "对陨石本体的调查", "已经有进展。" });
            Assert(string.Concat(compactChineseParts) ==
                   "我这边对陨石本体的调查已经有进展。",
                "原文 PART 边界的英文空格不得插入相邻中文片段之间");
            string[] mixedLanguageParts = TokenCodec.ApplyTranslatedWhitespace(
                new[] { "Use ", "VAR ", "1" },
                new[] { "使用", "VAR", "1" });
            Assert(string.Concat(mixedLanguageParts) == "使用 VAR 1",
                "统一清理 PART 空格时必须保留中英文、英文与数字之间的必要间隔");

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
            Assert(InterfaceFontPolicy.ShouldUseDirectFont(
                       "Ideas Window/Idea Popup[1]/Text[0]", "*** 有新想法 ***",
                       DisplayMode.TranslationOnly) &&
                   InterfaceFontPolicy.ShouldUseDirectFont(
                       "GlobalPopups/Clipboard Popup[0]/Text[0]", "*** 有新想法 ***",
                       DisplayMode.TranslationOnly) &&
                   InterfaceFontPolicy.ShouldUseDirectFont(
                       "Idea Log Entry(Clone)[0]/Name[0]", "传输：69，时间：01:30:23",
                       DisplayMode.TranslationOnly) &&
                   InterfaceFontPolicy.ShouldUseDirectFont(
                       "Calculator Window/Select to Edit Var/Text", "选择要编辑的变量",
                       DisplayMode.TranslationOnly, hasSpecialMaterialOwner: true) &&
                   InterfaceFontPolicy.ShouldUseDirectFont(
                       "Input Output Objects/Term Logger/Text", "为信号 -256 命名",
                       DisplayMode.TranslationOnly, hasSpecialMaterialOwner: true) &&
                   !InterfaceFontPolicy.ShouldUseDirectFont(
                       "Ideas Window/Idea Popup[1]/Text[0]", "*** IDEA AVAILABLE ***",
                       DisplayMode.OriginalOnly) &&
                   !InterfaceFontPolicy.ShouldUseDirectFont(
                       "Ideas Window/Viewport[0]/Top Tab[2]/Text[0]", "想法",
                       DisplayMode.TranslationOnly),
                "特殊界面材质上的日志与想法中文必须改用直绑字体，原文和普通 UI 不得受影响");
            Assert(DialoguePunctuationFontMarkup.Apply(
                       "你好，等等……OK!《记录》", "DeepSpaceChinese Fallback",
                       "12AB34FF") ==
                   "你好<font=\"DeepSpaceChinese Fallback\"><color=#12AB34FF>，" +
                   "</color></font>等等" +
                   "<font=\"DeepSpaceChinese Fallback\"><color=#12AB34FF>……" +
                   "</color></font>OK!" +
                   "<font=\"DeepSpaceChinese Fallback\"><color=#12AB34FF>《" +
                   "</color></font>记录" +
                   "<font=\"DeepSpaceChinese Fallback\"><color=#12AB34FF>》" +
                   "</color></font>",
                "中文译文中的中文标点必须显式使用中文字体并继承角色颜色，ASCII 标点不得改变");
            Assert(DialoguePunctuationPolicy.ShouldDecorate(
                       isTrackedDialogue: false, usesCharacterFont: true,
                       supportsRichText: true, DisplayMode.TranslationOnly, "角色日志，正文。") &&
                   DialoguePunctuationPolicy.ShouldDecorate(
                       isTrackedDialogue: true, usesCharacterFont: false,
                       supportsRichText: true, DisplayMode.TranslationOnly, "场景对白……") &&
                   !DialoguePunctuationPolicy.ShouldDecorate(
                       isTrackedDialogue: false, usesCharacterFont: false,
                       supportsRichText: true, DisplayMode.TranslationOnly, "普通界面，文本。") &&
                   !DialoguePunctuationPolicy.ShouldDecorate(
                       isTrackedDialogue: false, usesCharacterFont: true,
                       supportsRichText: true, DisplayMode.OriginalOnly, "Character log, text.") &&
                   !DialoguePunctuationPolicy.ShouldDecorate(
                       isTrackedDialogue: false, usesCharacterFont: true,
                       supportsRichText: false, DisplayMode.TranslationOnly,
                       "可在词典各条目中查看假说。（词典 → 条目注释 → 假说）"),
                "角色字体组件与实时对话仅可在支持富文本时注入标点标签，普通 UI、原文及 richText=false 组件不得受影响");
            Color compensatedColor = DialoguePunctuationColor.Compensate(
                new Color(0.5f, 0.25f, 1f, 1f),
                new Color(0.5f, 1f, 0.25f, 1f),
                new Color(0.25f, 0.5f, 0.5f, 1f));
            Assert(Math.Abs(compensatedColor.r - 1f) < 0.001f &&
                   Math.Abs(compensatedColor.g - 0.5f) < 0.001f &&
                   Math.Abs(compensatedColor.b - 0.5f) < 0.001f &&
                   Math.Abs(compensatedColor.a - 1f) < 0.001f,
                "中文标点颜色必须补偿原角色字体和中文字体材质的 Face Color 差异");
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
                "    { \"stable_key\": \"display:shapes\", \"kind\": \"display_value\", " +
                "\"source_sha256\": \"" + TokenCodec.Sha256("Shapes") + "\", " +
                "\"source_text\": \"Shapes\", \"translated_text\": \"形状\", " +
                "\"game\": { \"original_text\": \"Shapes\" } },\n" +
                "    { \"stable_key\": \"ui-fragment:test\", \"kind\": \"ui_fragment\", " +
                "\"source_sha256\": \"" + TokenCodec.Sha256("Stable: yes") + "\", " +
                "\"source_text\": \"Stable: yes\", \"translated_text\": \"稳定：是\", " +
                "\"game\": { \"original_text\": \"Stable: yes\" } },\n" +
                "    { \"stable_key\": \"ui-fragment:undefined-suffix\", \"kind\": \"ui_fragment\", " +
                "\"source_sha256\": \"" + TokenCodec.Sha256("_UNDEF") + "\", " +
                "\"source_text\": \"_UNDEF\", \"translated_text\": \"_未定义\", " +
                "\"game\": { \"original_text\": \"_UNDEF\" } }\n" +
                "  ]\n}";
            File.WriteAllText(Path.Combine(translations, "display.json"), displayJson);
            TranslationStore store = TranslationStore.Load(translations, log);
            Assert(store.Count == 7 && store.TryGet("ui:test", out RuntimeTranslationEntry entry) &&
                   entry.TranslatedText == "开始", "运行时 JSON 加载失败");
            Assert(store.UiTemplates.Count() == 1 &&
                   store.FindUnambiguousAchievement("Hello World!")?.TranslatedText == "你好，世界！",
                "动态模板或成就显示译文索引失败");
            Assert(store.FindUnambiguousDisplayValue("Hydrogen")?.TranslatedText == "氢" &&
                   store.UiFragments.Count() == 2, "动态显示值或富文本片段索引失败");
            Assert(store.FindUnambiguousDisplayValue("SHAPES")?.TranslatedText == "形状",
                "总结界面的全大写谜题组标题必须命中显示值译文");
            var displayLocalizer = new UiLocalizer(store, config, null, null, log);
            Assert(displayLocalizer.TranslateCompositeValues(
                       "已完成的谜题组：\n1. SHAPES", translateDisplayValues: true) ==
                   "已完成的谜题组：\n1. 形状",
                "总结界面的复合文本必须翻译大小写不同的谜题组标题");
            var safeTemplate = new RuntimeTranslationEntry
            {
                StableKey = "ui-template:save-path",
                Game = new JObject { ["translate_display_values"] = false },
            };
            var displayValueTemplate = new RuntimeTranslationEntry
            {
                StableKey = "ui-template:puzzle-group",
                Game = new JObject { ["translate_display_values"] = true },
            };
            Assert(displayLocalizer.ApplyTemplateDisplayValues(safeTemplate,
                       "路径：C:/AppData/The Message From Deep Space") ==
                   "路径：C:/AppData/The Message From Deep Space" &&
                   displayLocalizer.ApplyTemplateDisplayValues(displayValueTemplate,
                       "第 15 组 - Shapes") == "第 15 组 - 形状",
                "只有显式声明的动态模板才能翻译显示值，路径模板必须原样保留动态参数");
            Assert(displayLocalizer.TranslateCompositeValues("@-2_UNDEF") == "@-2_未定义",
                "未定义词典条目的动态数字必须保留，只翻译 _UNDEF 后缀");
            config.DisplayMode = DisplayMode.TranslationOnly;
            Assert(displayLocalizer.TranslateRuntimeSentinels("@-2_UNDEF") == "@-2_未定义",
                "绕过普通 UI 翻译的对话与词典词条仍须翻译未定义后缀");
            Assert(displayLocalizer.TranslateRuntimeSentinels("<u>@-2_UNDEF</u>") ==
                   "<u>@-2_未定义</u>",
                "对话中的富文本下划线不得阻止未定义后缀翻译");
            Assert(displayLocalizer.TranslateRuntimeSentinels("UNDEFINED _UNDEF") ==
                   "UNDEFINED _UNDEF",
                "不得全局替换说明文字或玩家输入的普通 UNDEF 字样");
            config.DisplayMode = DisplayMode.OriginalOnly;
            Assert(displayLocalizer.TranslateRuntimeSentinels("@-2_未定义") == "@-2_UNDEF",
                "切回原文时必须恢复游戏原始的未定义后缀");

            TranslationStore fullStore = TranslationStore.Load(
                Path.Combine(projectRoot, "build", "package", "DeepSpaceChinese", "Translations"),
                log);
            config.DisplayMode = DisplayMode.TranslationOnly;
            var fullUiLocalizer = new UiLocalizer(fullStore, config, null, null, log);
            Assert(fullStore.TryGet(
                       "system:ControlRoom:Hypotheses Log:component:1:field:viewInDict_s",
                       out RuntimeTranslationEntry hypothesesInstruction) &&
                   hypothesesInstruction.TranslatedText ==
                   "可在词典各条目中查看假说。\n（词典 → 条目注释 → 假说）",
                "章节总结的词典假说提示必须显式分为两行，不能依赖英文空格自动换行");
            Type bannerRuntimeType = typeof(DeepSpaceChinesePlugin).Assembly.GetType(
                "DeepSpaceChinese.AnalogTextBannerLocalizationRuntime");
            Assert(bannerRuntimeType != null,
                "歌曲标题必须在 AnalogTextBanner 开始逐字滚动前完成整段翻译，不能逐帧翻译英文残片");
            Type bannerPatchType = typeof(DeepSpaceChinesePlugin).Assembly.GetType(
                "DeepSpaceChinese.SoundtrackTitleViewDisplayPatch");
            Assert(bannerPatchType?.GetMethod("Prefix",
                       BindingFlags.Static | BindingFlags.NonPublic) != null,
                "SoundtrackTitleView.DisplayTitle 必须接入滚动前整段翻译补丁");
            MethodInfo prepareBannerSource = bannerRuntimeType.GetMethod(
                "PrepareSource", BindingFlags.Static | BindingFlags.NonPublic);
            Assert(prepareBannerSource != null,
                "歌曲标题滚动源文本必须提供可测试的预翻译入口");
            string localizedBannerSource = (string)prepareBannerSource.Invoke(null,
                new object[] { fullUiLocalizer, "正在播放：Falling Somewhere" });
            Assert(localizedBannerSource == "正在播放：坠向某处" &&
                   !localizedBannerSource.Contains("ing Somewhere"),
                "歌曲标题必须先整体翻译为“坠向某处”再滚动，滚动结束不得露出 ING SOMEWHERE");
            config.DisplayMode = DisplayMode.OriginalOnly;
            string originalBannerSource = (string)prepareBannerSource.Invoke(null,
                new object[] { fullUiLocalizer, "Song Playing: Falling Somewhere" });
            Assert(originalBannerSource == "Song Playing: Falling Somewhere",
                "纯原文模式的歌曲标题滚动源文本必须保持完整英文");
            config.DisplayMode = DisplayMode.TranslationOnly;
            string nameSignalLocalized = fullUiLocalizer.TranslateDynamicLiteral(
                "NAME SIGNAL-25");
            Assert(nameSignalLocalized == "为信号 -25 命名",
                "右侧新单词浮窗必须通过运行时动态文本入口翻译 NAME SIGNAL-数字");
            Type termLoggerPatchType = typeof(DeepSpaceChinesePlugin).Assembly.GetType(
                "DeepSpaceChinese.TermLoggerConfigureLocalizationPatch");
            Assert(termLoggerPatchType?.GetMethod("Postfix",
                       BindingFlags.Static | BindingFlags.NonPublic) != null,
                "TermLogger.Configure 必须接入运行时浮窗翻译补丁，不能只验证模板函数");
            Assert(fullUiLocalizer.TranslateCompositeValues("0.33203125") == "0.33203125",
                "动态 UI 本地化不得删掉八进制转换结果 0.33203125 的前导 0 和小数点");
            Assert(fullUiLocalizer.TranslateCompositeValues(
                       "COMPILATION FAILED", translateDisplayValues: true) ==
                   "COMPILATION FAILED",
                "逐字加载编译错误时不得把 COMPILATION 内部的 PI 替换为圆周率");
            Assert(fullUiLocalizer.TranslateCompositeValues(
                       "-RUN BAU_CORE_5.MOS", translateDisplayValues: true) ==
                   "-RUN BAU_CORE_5.MOS",
                "启动日志不得把程序名 CORE 内部的 OR 替换为“或”");
            const string savePath =
                "C:/Users/hepta/AppData/LocalLow/Applesinmypants/The Message From Deep Space";
            Assert(fullUiLocalizer.TranslateCompositeValues(savePath) == savePath,
                "保存路径中的 AppData、The Message From 等文件夹名必须保持原样");
            Assert(UiLocalizer.SelectRefreshSourceForTests(
                       "TITLE TITLE TITLE", "THE MISSION", "标题 标题 标题") ==
                   "THE MISSION" &&
                   UiLocalizer.SelectRefreshSourceForTests(
                       "LOREMIPSUMLOREMIPSUM", "Character journal text.",
                       "角色日记正文。") == "Character journal text." &&
                   UiLocalizer.SelectRefreshSourceForTests(
                       "MISSION TIME", "任务时间", "任务时间") == "MISSION TIME" &&
                   UiLocalizer.SelectRefreshSourceForTests(
                       null, "Runtime generated text", null) == "Runtime generated text",
                "F8 刷新动态日志时不得用 prefab 标题或 LOREMIPSUM 占位文本覆盖游戏已生成的内容");

            Assert(CalculatorBaseConversionCompatibility.RepairResult(
                       0.252d, 8, 10, "33203125") == "0.33203125",
                "八进制小数转十进制时必须恢复原版算法漏掉的前导 0 和小数点");
            Assert(CalculatorBaseConversionCompatibility.RepairResult(
                       0.001d, 8, 10, "1953125") == "0.001953125",
                "修复不得只在小数点后补零，较小的八进制小数也必须保持数值");
            Assert(CalculatorBaseConversionCompatibility.RepairResult(
                       0.252d, 8, 10, "0.33203125") == "0.33203125" &&
                   CalculatorBaseConversionCompatibility.RepairResult(
                       1.252d, 8, 10, "1.33203125") == "1.33203125" &&
                   CalculatorBaseConversionCompatibility.RepairResult(
                       0.9d, 8, 10, "9") == "9",
                "计算器兼容修复不得改写正常结果、整数结果或含无效进制数字的结果");

            float transmissionX = -0.47925f;
            float transmissionWidth = 0.88151f;
            foreach (float originalWidth in new[] { 0.38896f, 0.60893f, 0.70944f })
            {
                MainMenuButtonLayout layout = MainMenuButtonLayoutEngine.Calculate(
                    originalParentScaleX: originalWidth,
                    originalChildScaleX: 1f / originalWidth,
                    referencePositionX: transmissionX,
                    referenceScaleX: transmissionWidth,
                    labelRightX: 0.25f,
                    visualGapX: 0.04f,
                    iconHalfWidthX: 0.03f);
                Assert(Math.Abs(layout.RootPositionX - transmissionX) < 0.00001f &&
                       Math.Abs(layout.RootScaleX - transmissionWidth) < 0.00001f,
                    "中文主菜单的所有按钮必须保持与“传输”按钮相同的完整点击宽度");
                Assert(Math.Abs(layout.ChildScaleX * transmissionWidth - 1f) < 0.0001f,
                    "统一按钮宽度时不得横向拉伸文字或图标");
                Assert(Math.Abs(layout.IconCenterX - 0.32f) < 0.00001f,
                    "图标必须按中文文字右边界和固定视觉间距定位");
            }

            RunConfigEditorLayoutTests(projectRoot);

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

    private static void RunConfigEditorLayoutTests(string projectRoot)
    {
        string editorAssemblyPath = Path.Combine(projectRoot, "src",
            "DeepSpaceChinese.ConfigEditor", "bin", "Release", "net472",
            "DeepSpaceChinese.ConfigEditor.exe");
        Assembly editorAssembly = Assembly.LoadFrom(editorAssemblyPath);
        Type formType = editorAssembly.GetType(
            "DeepSpaceChinese.ConfigEditor.ConfigEditorForm", true);
        using var form = (Form)Activator.CreateInstance(formType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null, new object[] { Path.Combine(projectRoot, "patch", "DeepSpaceChinese.ini") },
            null);
        Control[] controls = Descendants(form).ToArray();
        CheckBox compilerOption = controls.OfType<CheckBox>().Single(control =>
            control.Text.Contains("忽略英文字母大小写"));
        CheckBox puzzleOption = controls.OfType<CheckBox>().Single(control =>
            control.Text.Contains("题目及答案的修正规则"));
        CheckBox layoutOption = controls.OfType<CheckBox>().Single(control =>
            control.Text.Contains("新单词命名") && control.Text.Contains("右下角"));
        Label compilerHint = controls.OfType<Label>().Single(control =>
            control.Text.Contains("VAR 可匹配词典中的 var"));
        Label layoutHint = controls.OfType<Label>().Single(control =>
            control.Text.Contains("保持列表原有顺序和行距"));
        Label puzzleHint = controls.OfType<Label>().Single(control =>
            control.Text.Contains("题面和答案集可单独修正"));
        Label reloadHint = controls.OfType<Label>().Single(control =>
            control.Text.Contains("以上兼容项、界面排布和题目及答案修正规则保存后"));

        Assert(!ReferenceEquals(compilerHint, layoutHint) &&
               !ReferenceEquals(layoutHint, puzzleHint) &&
               !ReferenceEquals(puzzleHint, reloadHint) &&
               !ReferenceEquals(compilerHint, reloadHint),
            "常规页的大小写、浮窗排布、题面修正和 F5 说明必须使用独立标签");
        Assert(compilerHint.Parent == compilerOption.Parent &&
               compilerHint.Top >= compilerOption.Bottom &&
               compilerHint.Top - compilerOption.Bottom <= 12 &&
               compilerHint.Bottom <= layoutOption.Top,
            "大小写兼容说明必须紧跟在对应复选框下方");
        Assert(layoutHint.Parent == layoutOption.Parent &&
               layoutHint.Top >= layoutOption.Bottom &&
               layoutHint.Top - layoutOption.Bottom <= 12 &&
               layoutHint.Bottom <= puzzleOption.Top,
            "新单词浮窗列表的排布说明必须紧跟在对应复选框下方");
        Assert(puzzleHint.Parent == puzzleOption.Parent &&
               puzzleHint.Top >= puzzleOption.Bottom &&
               puzzleHint.Top - puzzleOption.Bottom <= 12 &&
               reloadHint.Top >= puzzleHint.Bottom,
            "题面修正说明必须紧跟在对应复选框下方，F5 公共说明应位于其后");
    }

    private static System.Collections.Generic.IEnumerable<Control> Descendants(Control parent)
    {
        foreach (Control child in parent.Controls)
        {
            yield return child;
            foreach (Control descendant in Descendants(child))
                yield return descendant;
        }
    }

    private static void RunDialogueLayoutTests()
    {
        float rectShrink = DialogueTextRectLayout.RequiredRightShrink(
            textRight: 2.7797f, iconLeft: 2.5900f, gap: 0.01f);
        Assert(Math.Abs(rectShrink - 0.1997f) < 0.0001f,
            "主对话框应按继续图标的实际左边缘收窄右边界");
        Assert(Math.Abs(DialogueWidthBudget.AvailableWidth(
                   rectWidth: 100f, leftMargin: 5f, rightMargin: 7f) - 88f) < 0.001f,
            "分页宽度必须使用文本框收窄后的实际宽度并扣除左右边距");
        float originalWidthAtBoundary = DialogueWidthBudget.AvailableWidth(
            rectWidth: 21f, leftMargin: 1f, rightMargin: 1f);
        DialogueLayoutResult nearShrinkLimit = DialogueLayoutEngine.Fit(
            new[] { new DialogueLayoutPart(new string('测', 28), 0.02f, false, 0.4f) },
            originalWidthAtBoundary, 1.5f, ApproximateWidth);
        Assert(!nearShrinkLimit.WasPaginated &&
               nearShrinkLimit.Parts.Count == 1 &&
               nearShrinkLimit.Parts[0].Text == new string('测', 28),
            "原宽度 1.5 倍以内的对白不能提前分页或插入猜测性的手动换行");
        DialogueLayoutResult shortLine = DialogueLayoutEngine.Fit(
            new[] { new DialogueLayoutPart("短文本", 0.02f, false, 0.4f) },
            19f, 1.5f, ApproximateWidth);
        Assert(!shortLine.WasPaginated && shortLine.Parts[0].Text == "短文本",
            "本来能以正常字号单行显示的短对白不能被强制换行");
        DialogueLayoutResult reportedTwoLineDialogue = DialogueLayoutEngine.Fit(
            new[]
            {
                new DialogueLayoutPart("所以每经历一步，", 0f, true, 0.1f),
                new DialogueLayoutPart("更年轻的恒星就能制造更重的原子，", 0f, false, 0.1f),
                new DialogueLayoutPart("再形成更重的岩石。", 0f, false, 1.93f),
            },
            19f, 1.5f, ApproximateWidth);
        Assert(!reportedTwoLineDialogue.WasPaginated &&
               reportedTwoLineDialogue.Parts.Count == 3,
            "两行缩小后能够完整显示的连续 PART 不得被拆成两个页面");
        DialogueLayoutResult forcedOverflowFallback = DialogueLayoutEngine.Fit(
            new[]
            {
                new DialogueLayoutPart("所以每经历一步，", 0f, true, 0.1f),
                new DialogueLayoutPart("更年轻的恒星就能制造更重的原子，", 0f, false, 0.1f),
                new DialogueLayoutPart("再形成更重的岩石。", 0f, false, 1.93f),
            },
            19f, 1.5f, ApproximateWidth, string.Empty,
            text => CountVisible(text) <= 30);
        Assert(forcedOverflowFallback.WasPaginated &&
               forcedOverflowFallback.Parts.Count(part => part.ClearPrevious) >= 2,
            "即使宽度估算未超限，TMP 实际显示区域溢出也必须强制分页");
        const string reportedLine = "我们开始调查人类历史上第一次与外星生命的接触。";
        DialogueLayoutResult shrunk = DialogueLayoutEngine.Fit(
            new[] { new DialogueLayoutPart(reportedLine, 0.02f, false, 0.4f) },
            19f, 1.5f, ApproximateWidth);
        Assert(!shrunk.WasPaginated && shrunk.Parts.Count == 1 &&
               shrunk.Parts[0].Text == reportedLine,
            "不超过 1.5 倍上限的对白不应分页，应交给 TMP 自动缩小字号");

        string longLine = new string('测', 60);
        DialogueLayoutResult paginated = DialogueLayoutEngine.Fit(
            new[] { new DialogueLayoutPart("$animD00" + longLine, 0.02f, false, 0.4f) },
            19f, 1.5f, ApproximateWidth);
        Assert(paginated.WasPaginated && paginated.Parts.Count == 2,
            "超过 1.5 倍上限的对白应分页");
        Assert(!paginated.Parts[0].ClearPrevious && paginated.Parts[1].ClearPrevious,
            "自动分页必须只在续页前插入清屏标记");
        Assert(paginated.Parts.Sum(part => CountVisible(part.Text)) == 60 &&
               paginated.Parts.Count(part => part.Text.Contains("$animD00")) == 1,
            "分页不得丢字、复制作画指令或改变正文");
        Assert(paginated.Parts.All(part => ApproximateWidth(part.Text) <= 19f * 2f),
            "分页后的每一页都必须落在文本框的两行容量内");
        Assert(Math.Abs(paginated.Parts[0].MessageDelay) < 0.001f &&
               Math.Abs(paginated.Parts[1].MessageDelay - 0.4f) < 0.001f,
            "拆分同一 PART 时只能由最后一段继承消息延迟");

        string rich = "<size=75%><u>" + new string('原', 65) + "</u></size>";
        DialogueLayoutResult richPages = DialogueLayoutEngine.Fit(
            new[] { new DialogueLayoutPart(rich, 0f, true, 0f) },
            19f, 1.5f, ApproximateWidth);
        Assert(richPages.WasPaginated && richPages.Parts.All(IsBalancedRichText),
            "富文本跨页时每一页都必须拥有完整、平衡的标签");
        string punctuationMarkup = string.Concat(Enumerable.Repeat(
            "正文<font=\"DeepSpaceChinese Fallback\"><color=#12AB34FF>，</color></font>", 20));
        DialogueLayoutResult punctuationPages = DialogueLayoutEngine.Fit(
            new[] { new DialogueLayoutPart(punctuationMarkup, 0f, true, 0f) },
            19f, 1.5f, ApproximateWidth);
        Assert(punctuationPages.WasPaginated &&
               punctuationPages.Parts.All(IsBalancedRichText) &&
               punctuationPages.Parts.Sum(part => CountVisible(part.Text)) == 60,
            "分页不得切断中文标点的 font/color 标签，也不得丢失标签内字符");

        DialogueLayoutResult grouped = DialogueLayoutEngine.Fit(
            new[]
            {
                new DialogueLayoutPart(new string('甲', 35), 0.01f, false, 0.2f),
                new DialogueLayoutPart(new string('乙', 35), 0.03f, false, 0.5f),
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
        int fonts = Regex.Matches(part.Text, "<font(?:=[^>]*)?>", RegexOptions.IgnoreCase).Count;
        int fontEnds = Regex.Matches(part.Text, "</font>", RegexOptions.IgnoreCase).Count;
        int colors = Regex.Matches(part.Text, "<color(?:=[^>]*)?>", RegexOptions.IgnoreCase).Count;
        int colorEnds = Regex.Matches(part.Text, "</color>", RegexOptions.IgnoreCase).Count;
        return sizes == sizeEnds && underlines == underlineEnds &&
               fonts == fontEnds && colors == colorEnds;
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
