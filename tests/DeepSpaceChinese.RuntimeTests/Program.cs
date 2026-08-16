using System;
using System.Collections.Generic;
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
            return Run();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.ToString());
            if (ex.InnerException != null)
            {
                Console.Error.WriteLine("Inner: " + ex.InnerException.GetType().FullName +
                                        ": " + ex.InnerException.Message);
            }
            return 1;
        }
    }

    private static int Run()
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
            Assert(config.CompilerPunctuationInsensitive,
                "编译及词典冲突检查时忽略中英文标点差异必须默认开启");
            Assert(config.PuzzleFixesEnabled,
                "题面修正功能必须默认开启");
            Assert(config.MoveNewWordPromptToLowerRight,
                "新单词命名浮窗移动到右下角必须默认开启");
            Assert(config.KonamiAnswerAutofillEnabled,
                "科乐美序列填入正确答案功能必须默认开启");
            RunKonamiCodeDetectorTests();
            Assert(ShortcutDisplayFormatter.Translate("LeftControl + R",
                       DisplayMode.TranslationOnly) == "左Ctrl + R" &&
                   ShortcutDisplayFormatter.Translate("LEFTCONTROL + R",
                       DisplayMode.TranslationOnly) == "左Ctrl + R" &&
                   ShortcutDisplayFormatter.Translate("按LeftControl+T跳过",
                       DisplayMode.TranslationOnly) == "按左Ctrl + T跳过" &&
                   ShortcutDisplayFormatter.Translate("LeftControl + R",
                       DisplayMode.OriginalOnly) == "LeftControl + R" &&
                   ShortcutDisplayFormatter.Translate("RightControl + E",
                       DisplayMode.TranslationOnly) == "右Ctrl + E" &&
                   ShortcutDisplayFormatter.Translate("LeftShift + Alpha1",
                       DisplayMode.TranslationOnly) == "左Shift + ALPHA1" &&
                   ShortcutDisplayFormatter.Translate("LEFTCONTROL room",
                       DisplayMode.TranslationOnly) == "LEFTCONTROL room",
                "运行时生成的快捷键必须在译文模式显示为“左Ctrl + R”，原文模式及普通文本不得误改");
            RunTmpUnderlineMeshCleanupTests();
            Type manualJournalSyncType = typeof(DeepSpaceChinesePlugin).Assembly.GetType(
                "DeepSpaceChinese.ManualJournalLogWindowSync");
            MethodInfo shouldAppendManualJournal = manualJournalSyncType?.GetMethod(
                "ShouldAppendForTests", BindingFlags.Static | BindingFlags.NonPublic);
            Assert(shouldAppendManualJournal != null &&
                   (bool)shouldAppendManualJournal.Invoke(null,
                       new object[] { false, true, (int)DialogueType.journalEntries, true }) &&
                   !(bool)shouldAppendManualJournal.Invoke(null,
                       new object[] { true, true, (int)DialogueType.journalEntries, true }) &&
                   !(bool)shouldAppendManualJournal.Invoke(null,
                       new object[] { false, true, (int)DialogueType.advancement, true }) &&
                   !(bool)shouldAppendManualJournal.Invoke(null,
                       new object[] { false, true, (int)DialogueType.journalEntries, false }),
                "手记由 ManualLogDialogueEntry 写入后必须立即追加到当前日志窗口，且不得重复追加已有条目");
            Assert(PeriodicTableElementCompatibility.ResolveSymbol(
                       "Radium", "Ra", "Radium") == "Ra",
                "镭的资源字段写反时，周期表必须显示化学符号 Ra，而不是把 Radium 拆成 RA/DI/UM");
            Assert(PeriodicTableElementCompatibility.ResolveSymbol(
                       "Radon", "Radon", "Rn") == "Rn",
                "周期表兼容修正不得影响字段正常的其他元素");
            var contactPairs = new[]
            {
                new TransmissionPair
                {
                    playerTransmission = new SignalMessage { signals = new[] { -153 } },
                    responseTransmission = new SignalMessage
                    {
                        signals = new[] { -153, -100, -186 },
                    },
                },
            };
            SignalMessage specialContactResponse =
                ContactTransmissionCompatibility.ResolveResponseForTests(
                    contactPairs,
                    new SignalMessage { signals = new[] { -45, -86, -46, -244, -85 } },
                    new SignalMessage { signals = new[] { -153 } });
            SignalMessage genericContactResponse =
                ContactTransmissionCompatibility.ResolveResponseForTests(
                    contactPairs,
                    new SignalMessage { signals = new[] { -45, -86, -46, -244, -85 } },
                    new SignalMessage { signals = new[] { -999 } });
            Assert(specialContactResponse.signals.SequenceEqual(
                       new[] { -153, -100, -186 }) &&
                   genericContactResponse.signals.SequenceEqual(
                       new[] { -45, -86, -46, -244, -85 }),
                "结局最终联络必须按信号数组内容命中特殊回复；不能因 SignalMessage 的数组哈希错误而总是返回通用回复");
            Assert(PeriodicTableElementCompatibility.ShouldTranslateDisplayValues(
                       isPeriodicTable: true, isSymbol: false) &&
                   !PeriodicTableElementCompatibility.ShouldTranslateDisplayValues(
                       isPeriodicTable: true, isSymbol: true) &&
                   !PeriodicTableElementCompatibility.ShouldTranslateDisplayValues(
                       isPeriodicTable: false, isSymbol: false),
                "整个元素周期表的组合文本都必须翻译显示值，但化学符号必须保持拉丁字母");
            Assert(PeriodicTableElementCompatibility.ShouldTranslateDisplayValues(
                       isPeriodicTable: false, isSymbol: false, isRegisteredPreview: true),
                "周期表层级之外的底部悬浮提示也必须翻译动态元素名");
            Assert(PeriodicTableElementCompatibility.ResolvePreviewNameLookup(
                       "Radium", isRegisteredPreview: true) == "Ra" &&
                   PeriodicTableElementCompatibility.ResolvePreviewNameLookup(
                       "Radium", isRegisteredPreview: false) == "Radium",
                "镭的底部预览必须用修正后的元素名 Ra 查询译文，但不得改写其他位置的 Radium");
            Assert(ChineseYearQuantityFormatter.Translate("4.400000 billion years") ==
                       "44亿年" &&
                   ChineseYearQuantityFormatter.Translate("700.000000 million years") ==
                       "7亿年" &&
                   ChineseYearQuantityFormatter.Translate("1.250000 million years") ==
                       "125万年" &&
                   ChineseYearQuantityFormatter.Translate("12.200000 years") ==
                       "12.2年" &&
                   ChineseYearQuantityFormatter.Translate("12 thousand years") ==
                       "1.2万年",
                "周期表年代必须换算为中文常用的亿年、万年或年，不能直译成小数十亿年、百万年");
            Assert(DictionaryDialogueConditionMatcher.Matches("VAR", "var", false) &&
                   DictionaryDialogueConditionMatcher.Matches("frequency", "FREQUENCY", false) &&
                   DictionaryDialogueConditionMatcher.Matches("频率", "信号频率", true) &&
                   !DictionaryDialogueConditionMatcher.Matches("频率", "信号频率", false) &&
                   !DictionaryDialogueConditionMatcher.Matches("VAR", "variable", false),
                "词典命名对白必须忽略英文大小写，同时保留完整相等与包含条件的语义差异");
            Assert(DictionaryTriggerAliasStore.RuleMatches(
                       new DictionaryTriggerAliasStore.Rule
                       {
                           Type = "contains",
                           Values = new System.Collections.Generic.List<string>
                               { "md不知道", "tm不知道" }
                       }, "我tm不知道啊") &&
                   DictionaryTriggerAliasStore.RuleMatches(
                       new DictionaryTriggerAliasStore.Rule
                       {
                           Type = "contains_all",
                           Values = new System.Collections.Generic.List<string>
                               { "妈", "不知道" }
                       }, "我他妈真不知道") &&
                   !DictionaryTriggerAliasStore.RuleMatches(
                       new DictionaryTriggerAliasStore.Rule
                       {
                           Type = "contains_all",
                           Values = new System.Collections.Generic.List<string>
                               { "妈", "不知道" }
                       }, "我不知道"),
                "中文附加触发必须支持任一包含和全部包含，且不能把 IDFK 放宽成普通 IDK");
            string triggerAliasPath = Path.Combine(projectRoot, "patch", "Translations",
                "dictionary_trigger_aliases.json");
            Assert(DictionaryTriggerAliasStore.TryLoad(triggerAliasPath, log,
                       out DictionaryTriggerAliasStore triggerAliases), "触发别名表必须载入");
            Assert(triggerAliases.Count == 277,
                "触发别名表必须覆盖全部人工规则和固化假说别名");
            bool hasVeryVariant = triggerAliases.TryGetDialogueVariant(
                -107, "旧名", "很", 905,
                out DictionaryTriggerAliasStore.DialogueVariant veryVariant);
            Assert(triggerAliases.VariantCount == 3 && hasVeryVariant &&
                   veryVariant.SyntheticDialogueId == 1905001 &&
                   veryVariant.TranslatedTitle == "很" &&
                   veryVariant.Frames.Count == 2 &&
                   !triggerAliases.TryGetDialogueVariant(-107, "旧名", "非常", 905,
                       out _) &&
                   !triggerAliases.Matches(-107, "EditEntryIDToName", "VERY", "很") &&
                   triggerAliases.Matches(-107, "EditEntryIDToName", "VERY", "非常"),
                "词典对白变体必须只让“很”选择独立对白，“非常”继续使用原对白");
            Assert(DictionaryAliasDialogueRuntime.TrySelectIndependentVariant(
                       triggerAliases, renameActive: true, termId: -107,
                       fromName: "很\u200b", toName: "很",
                       out DictionaryTriggerAliasStore.DialogueVariant selectedVeryVariant) &&
                   selectedVeryVariant.SyntheticDialogueId == 1905001 &&
                   !DictionaryAliasDialogueRuntime.TrySelectIndependentVariant(
                       triggerAliases, renameActive: true, termId: -107,
                       fromName: "旧名", toName: "非常", out _) &&
                   !DictionaryAliasDialogueRuntime.TrySelectIndependentVariant(
                       triggerAliases, renameActive: false, termId: -107,
                       fromName: "旧名", toName: "很", out _),
                "独立对白必须由本地化监听自主选择，不得依赖原版源对白是否仍在监听");
            DialogueFrame[] verySourceFrames =
            {
                new DialogueFrame
                {
                    speaker = Speaker.Carrie,
                    dialogueParts = new[]
                    {
                        new DialoguePart { txt = "$animC4Very good," },
                        new DialoguePart { txt = "Translator!", clearPrev = true },
                    },
                },
                new DialogueFrame
                {
                    speaker = Speaker.Alan,
                    dialogueParts = new[]
                    {
                        new DialoguePart { txt = "Oh I get it!" },
                        new DialoguePart { txt = "Very very good!", clearPrev = true },
                    },
                },
            };
            Assert(DictionaryAliasDialogueRuntime.TryBuildTranslatedFrames(
                       verySourceFrames, veryVariant, "翻译员",
                       out DialogueFrame[] veryTranslatedFrames, out string variantError) &&
                   veryTranslatedFrames[0].dialogueParts[0].txt == "$animC4很好，" &&
                   veryTranslatedFrames[0].dialogueParts[1].txt == "翻译员！" &&
                   veryTranslatedFrames[1].dialogueParts[1].txt == "很好，很好！",
                "独立对白必须继承原角色、分段和动画，只替换本地化文本：" + variantError);
            Assert(triggerAliases.Matches(-40, "EditEntryIDToName",
                       "TO", "到") &&
                   triggerAliases.Matches(-41, "EditEntryIDToName",
                       "TO", "到"),
                "对白 158 修正后，FROM 词条和 TO 词条各自的“到”命名都必须触发对应对白");
            const string validDialogueFix =
                "{\"dialogue_chunk_id\":158,\"channel\":\"EditEntryIDToName\"," +
                "\"english\":\"TO\",\"original_term_id\":-41," +
                "\"replacement_term_id\":-40}";
            Assert(DictionaryDialogueFixRule.TryParse(validDialogueFix, "158.json",
                       out DictionaryDialogueFixRule dialogueFix, out string dialogueFixError) &&
                   dialogueFix.DialogueChunkId == 158 &&
                   dialogueFix.ParsedChannel == ListenChannel.EditEntryIDToName &&
                   dialogueFix.OriginalTermId == -41 && dialogueFix.ReplacementTermId == -40,
                "词典对白 158 的条件修正规则必须可严格解析：" + dialogueFixError);
            Assert(!DictionaryDialogueFixRule.TryParse(validDialogueFix, "165.json",
                       out _, out _),
                "词典对白修正文件名必须与 DialogueChunk ID 一致");
            Assert(triggerAliases.Matches(0, "EditEntryToName",
                       "IDK", "我也不知道"), "IDK 中文触发");
            Assert(triggerAliases.Matches(0, "EditEntryToName",
                       "IDFK", "我他妈真的不知道"), "IDFK 中文触发");
            Assert(!triggerAliases.Matches(0, "EditEntryToName",
                       "IDK", "我他妈真的不知道"), "IDK 不得抢占 IDFK");
            Assert(!triggerAliases.Matches(0, "EditEntryToName",
                       "IDFK", "不知道"), "IDFK 不得放宽成 IDK");
            Assert(
                   triggerAliases.Matches(0, "EditEntryToName",
                       "IDK", "我也不知道") &&
                   triggerAliases.Matches(0, "EditEntryToName",
                       "IDFK", "我他妈真的不知道") &&
                   !triggerAliases.Matches(0, "EditEntryToName",
                       "IDK", "我他妈真的不知道") &&
                   !triggerAliases.Matches(0, "EditEntryToName",
                       "IDFK", "不知道") &&
                   triggerAliases.Matches(-69, "EditEntryIDToName",
                       "HELISEC", "氦秒") &&
                   !triggerAliases.Matches(-69, "EditEntryIDContains",
                       "HELIUM", "氦秒") &&
                   !triggerAliases.Matches(-46, "EditEntryIDToName",
                       "HUMANITY", "人类") &&
                   triggerAliases.Matches(-46, "EditEntryIDToName",
                       "HUMANS", "人类") &&
                   triggerAliases.Matches(-101, "EditEntryIDToName",
                       "LIFE", "生命") &&
                   triggerAliases.Matches(-101, "EditEntryIDToName",
                       "LIFE", "生物") &&
                   triggerAliases.Matches(-110, "EditEntryIDToName",
                       "ALL", "全部") &&
                   triggerAliases.Matches(-110, "EditEntryIDToName",
                       "ALL", "所有") &&
                   !triggerAliases.Matches(-110, "EditEntryIDToName",
                       "TOTAL", "所有") &&
                   triggerAliases.Matches(-111, "EditEntryIDToName",
                       "NONE", "无") &&
                   !triggerAliases.Matches(-111, "EditEntryIDToName",
                       "NONE", "空") &&
                   !triggerAliases.Matches(-111, "EditEntryIDToName",
                       "ZERO", "空") &&
                   triggerAliases.Matches(-111, "EditEntryIDToName",
                       "NOTHING", "什么都没有") &&
                   triggerAliases.Matches(-111, "EditEntryIDToName",
                       "NOTHING", "空") &&
                   triggerAliases.Matches(-126, "EditEntryIDToName",
                       "INFINITY", "无穷") &&
                   triggerAliases.Matches(-126, "EditEntryIDToName",
                       "INFINITY", "无限") &&
                   triggerAliases.Matches(-126, "EditEntryIDToName",
                       "INFINITY", "无限大") &&
                   triggerAliases.Matches(-126, "EditEntryIDToName",
                       "INFINITY", "无穷大") &&
                   !triggerAliases.Matches(-126, "EditEntryIDToName",
                       "INFINITY", "无限制") &&
                   triggerAliases.Matches(-195, "EditEntryIDToName",
                       "LANGUAGE", "语言") &&
                   triggerAliases.Matches(-195, "EditEntryIDToName",
                       "LANGUAGE", "文字") &&
                   !triggerAliases.Matches(-195, "EditEntryIDToName",
                       "LANGUAGE", "文字语言") &&
                   !triggerAliases.Matches(-188, "EditEntryIDToName",
                       "POSITIVE", "正") &&
                   triggerAliases.Matches(-140, "EditEntryIDToName",
                       "SHEEN", "光泽") &&
                   !triggerAliases.Matches(-140, "EditEntryIDToName",
                       "SHEEN", "希恩") &&
                   !triggerAliases.Matches(-36, "EditEntryIDToName",
                       "THEN", "则") &&
                   !triggerAliases.Matches(-36, "EditEntryIDToName",
                       "THEN", "所以") &&
                   !triggerAliases.Matches(-36, "EditEntryIDToName",
                       "THEREFORE", "所以") &&
                   triggerAliases.Matches(-36, "EditEntryIDToName",
                       "SO", "所以") &&
                   triggerAliases.Matches(-36, "EditEntryIDToName",
                       "THEREFORE", "则") &&
                   !triggerAliases.Matches(-31, "EditEntryIDToName",
                       "|", "或") &&
                   !triggerAliases.Matches(-31, "EditEntryIDToName",
                       "|", "｜") &&
                   triggerAliases.Matches(-2, "EditEntryIDToName",
                       "PLUSONE", "递增") &&
                   triggerAliases.Matches(-2, "EditEntryIDToName",
                       "PLUSONE", "自增") &&
                   !triggerAliases.Matches(-122, "EditEntryIDToName",
                       "THEN", "则") &&
                   triggerAliases.Matches(-61, "EditEntryIDToName",
                       "NUETRON", "种子") &&
                   !triggerAliases.Matches(-60, "EditEntryIDToName",
                       "NUETRON", "种子"),
                "维护表必须完整载入，且构建消歧结果必须落入对应源条件");
            Assert(
                   triggerAliases.Matches(-16, "DictEntryIs",
                       "POINT", "点") &&
                   triggerAliases.Matches(-17, "DictEntryIs",
                       "LINE", "线") &&
                   triggerAliases.Matches(-18, "EditEntryIDToName",
                       "SHAPE", "多边形") &&
                   triggerAliases.Matches(-62, "DictEntryIs",
                       "ELECTRON", "电子") &&
                   triggerAliases.Matches(-61, "DictEntryIs",
                       "NEUTRON", "中子") &&
                   triggerAliases.Matches(-60, "EditEntryIDToName",
                       "PROTON", "质子"),
                "几何与亚原子粒子的组合对白必须使用固化中文别名，不能依赖场景中的假说组件");
            string longestAliasPath = Path.Combine(testRoot, "longest-trigger-aliases.json");
            File.WriteAllText(longestAliasPath,
                @"{""entries"":[" +
                @"{""term_id"":-1,""channel"":""EditEntryIDToName"",""english"":""LONG"",""rules"":[{""type"":""exact"",""values"":[""氦秒""]}]}," +
                @"{""term_id"":-1,""channel"":""EditEntryIDContains"",""english"":""SHORT"",""rules"":[{""type"":""contains"",""values"":[""氦""]}]}," +
                @"{""term_id"":-2,""channel"":""EditEntryIDToName"",""english"":""TIE_A"",""rules"":[{""type"":""exact"",""values"":[""人类""]}]}," +
                @"{""term_id"":-2,""channel"":""EditEntryIDToName"",""english"":""TIE_B"",""rules"":[{""type"":""exact"",""values"":[""人类""]}]}]}");
            Assert(DictionaryTriggerAliasStore.TryLoad(longestAliasPath, log,
                       out DictionaryTriggerAliasStore longestAliases) &&
                   longestAliases.Matches(-1, "EditEntryIDToName", "LONG", "氦秒") &&
                   longestAliases.Matches(-1, "EditEntryIDContains", "SHORT", "氦秒") &&
                   longestAliases.Matches(-2, "EditEntryIDToName", "TIE_A", "人类") &&
                   longestAliases.Matches(-2, "EditEntryIDToName", "TIE_B", "人类"),
                "运行时不得以最长命中或并列检查掩盖构建配置冲突");
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
            MethodInfo renderedWorldPadding = typeof(ConsoleOutputScrollPadding).GetMethod(
                "AddToWorldHeight", BindingFlags.Static | BindingFlags.NonPublic, null,
                new[] { typeof(float), typeof(float), typeof(float), typeof(int), typeof(bool) },
                null);
            MethodInfo renderedRelativePadding = typeof(ConsoleOutputScrollPadding).GetMethod(
                "AddToRelativeMenuHeight", BindingFlags.Static | BindingFlags.NonPublic, null,
                new[]
                {
                    typeof(float), typeof(float), typeof(float), typeof(int), typeof(float),
                    typeof(bool),
                }, null);
            Assert(renderedWorldPadding != null && renderedRelativePadding != null &&
                   Math.Abs((float)renderedWorldPadding.Invoke(null,
                       new object[] { 0.8f, 0.04f, 0.42f, 10, true }) - 0.946f) < 0.0001f &&
                   Math.Abs((float)renderedRelativePadding.Invoke(null,
                       new object[] { 1.2f, 0.04f, 0.42f, 10, 0.5f, true }) - 1.492f) < 0.0001f,
                "题面输出滚动范围必须补偿中文 TMP 实际高度超过原版固定行高的累计误差");
            Assert(Math.Abs(ReferenceCopyButtonLayout.PlaceAfterText(
                       textLeftX: -0.30f, renderedTextWidth: 0.22f, gap: 0.01f) - (-0.07f)) < 0.0001f &&
                   Math.Abs(ReferenceCopyButtonLayout.PlaceAfterText(
                       textLeftX: -0.30f, renderedTextWidth: 0.47f, gap: 0.01f) - 0.18f) < 0.0001f,
                "参考页复制按钮必须跟随每行文本的实际渲染末端，而不是沿用英文固定坐标");
            Assert(UiLocalizer.IsStaticReferencePathForTests(
                       "Reference Window/2D SHAPES/Area/KEY") &&
                   !UiLocalizer.IsStaticReferencePathForTests(
                       "Reference Window/ELEMENT DISPLAY/Area/Name") &&
                   !UiLocalizer.IsStaticReferencePathForTests(
                       "Reference Window/PERIODIC TABLE PAGE/Periodic Table/Element/Symbol"),
                "静态参考页必须始终从翻译条目恢复原文；动态元素页不得被静态原文覆盖");
            Vector2 alignedCopyButton = ReferencePageLayoutEngine.PlaceAtLineEnd(
                new Vector2(-0.08f, -0.42f), new Vector2(0.01f, 0.015f));
            Vector2 alignedOverlay = ReferencePageLayoutEngine.MapToTranslatedBaseline(
                new Vector2(-0.21f, -3.87f), originalBaselineY: -3.84f,
                translatedBaselineY: -4.11f);
            Assert(Math.Abs(alignedCopyButton.x - (-0.07f)) < 0.0001f &&
                   Math.Abs(alignedCopyButton.y - (-0.405f)) < 0.0001f &&
                   Math.Abs(alignedOverlay.x - (-0.21f)) < 0.0001f &&
                   Math.Abs(alignedOverlay.y - (-4.14f)) < 0.0001f &&
                   ReferencePageLayoutEngine.FindNearestBaseline(
                       new[] { 0.2f, -0.1f, -0.4f, -0.7f }, -0.36f) == 2 &&
                   ReferencePageLayoutEngine.FindNearestPosition(
                       new[] { new Vector2(-3f, -1f), new Vector2(2f, -1f) },
                       new Vector2(1.8f, -0.9f)) == 1 &&
                   Math.Abs(ReferencePageLayoutEngine.ExtendContentHeight(
                       4.5f, originalBottomY: -4.2f, translatedBottomY: -4.8f) - 5.1f) < 0.0001f,
                "参考页公式、图像和复制按钮必须同时跟随译文的实际行基线，不能只修横坐标");
            Assert(ReferenceCopyButtonLayout.MatchScore(
                       "Copy Volume", "体积：4053 m^3", "4053") > 1000 &&
                   ReferenceCopyButtonLayout.MatchScore(
                       "Copy Mass", "质量：1.253 x 10^7 kg", "12530000") == 900 &&
                   ReferenceCopyButtonLayout.MatchScore(
                       "Copy Avogadro's Number",
                       "6.02214076*10^23 道尔顿 = 1 克。",
                       "602214076000000000000000") == 1900 &&
                   ReferenceCopyButtonLayout.MatchScore(
                       "Copy Sphere Volume Ratio", "V = (4/3) * (PI) *", "4.18879020") == 1850 &&
                   ReferenceCopyButtonLayout.MatchScore(
                       "Copy Sphere SA Ratio", "A = 4 * (PI) *", "12.566370") == 1850 &&
                   ReferenceCopyButtonLayout.MatchScore(
                       "Copy Height", "Height: 3.8 meters", "3.8") > 1000,
                "参考页复制按钮必须按复制值匹配对应行，并为科学记数法数值保留语义匹配");
            Type referenceFormatterType = typeof(DeepSpaceChinesePlugin).Assembly.GetType(
                "DeepSpaceChinese.ReferencePageTextFormatter");
            MethodInfo formatTruthTable = referenceFormatterType?.GetMethod(
                "FormatTruthTable", BindingFlags.Static | BindingFlags.NonPublic);
            string formattedTruthTable = formatTruthTable?.Invoke(null, new object[]
            {
                "P1      P2     运算       结果\n\n真      不适用   非         假\n",
            }) as string;
            Assert(formattedTruthTable != null &&
                   Regex.Matches(formattedTruthTable, "<pos=").Count == 8 &&
                   formattedTruthTable.Contains("<pos=7%>P1") &&
                   formattedTruthTable.Contains("<pos=27%>P2") &&
                   formattedTruthTable.Contains("<pos=49%>运算") &&
                   formattedTruthTable.Contains("<pos=74%>结果"),
                "真值表必须按固定网格列坐标排版，不能依赖中英文字宽不同的空格对齐");
            MethodInfo formatDistanceFormula = referenceFormatterType?.GetMethod(
                "FormatDistanceFormula", BindingFlags.Static | BindingFlags.NonPublic);
            Assert((string)formatDistanceFormula?.Invoke(null,
                       new object[] { "ABOVE STUFF (1)", "其中 c 为斜边：\n\n\n           a + b = c" }) ==
                   "其中 c 为斜边：\n\n\n           a<sup>2</sup> + b<sup>2</sup> = c<sup>2</sup>" &&
                   (string)formatDistanceFormula?.Invoke(null,
                       new object[] { "ABOVE STUFF (2)", "           3 + 4 = 5\n            = 9 + 16 = 25" }) ==
                   "           3<sup>2</sup> + 4<sup>2</sup> = 5<sup>2</sup>\n            = 9 + 16 = 25",
                "勾股公式的平方必须内联绑定到基字符，不能继续依赖独立绝对坐标小文本");
            Type fontPolicyType = typeof(DeepSpaceChinesePlugin).Assembly.GetType(
                "DeepSpaceChinese.ReferencePageFontPolicy");
            MethodInfo useDirectChineseFont = fontPolicyType?.GetMethod(
                "UseDirectChineseFont", BindingFlags.Static | BindingFlags.NonPublic);
            Assert(useDirectChineseFont != null &&
                   !(bool)useDirectChineseFont.Invoke(null, new object[] { "2" }) &&
                   !(bool)useDirectChineseFont.Invoke(null, new object[] { "a + b = c" }) &&
                   (bool)useDirectChineseFont.Invoke(null, new object[] { "勾股定理" }),
                "纯数字、公式和化学符号必须保留原字体字宽，只有中文文本才可直接切中文字体");
            MethodInfo preserveOriginalMetrics = fontPolicyType?.GetMethod(
                "PreserveOriginalMetrics", BindingFlags.Static | BindingFlags.NonPublic);
            Assert(preserveOriginalMetrics != null &&
                   (bool)preserveOriginalMetrics.Invoke(null, new object[] { "LOGIC PAGE" }) &&
                   (bool)preserveOriginalMetrics.Invoke(null, new object[] { "DISTANCE PAGE" }) &&
                   (bool)preserveOriginalMetrics.Invoke(null, new object[] { "STAR PAGE" }) &&
                   !(bool)preserveOriginalMetrics.Invoke(null, new object[] { "" }),
                "所有参考页必须保留原字体度量并由 fallback 补齐中文字形，避免换字体后文本、图像和按钮整体漂移");
            MethodInfo shouldFitSingleLineNote = referenceFormatterType?.GetMethod(
                "ShouldFitSingleLineNote", BindingFlags.Static | BindingFlags.NonPublic);
            Assert(shouldFitSingleLineNote != null &&
                   (bool)shouldFitSingleLineNote.Invoke(null, new object[] { 0.6f, 1.8f, 1 }) &&
                   !(bool)shouldFitSingleLineNote.Invoke(null, new object[] { 0.8f, 1.8f, 1 }) &&
                   !(bool)shouldFitSingleLineNote.Invoke(null, new object[] { 0.6f, 0.8f, 1 }) &&
                   !(bool)shouldFitSingleLineNote.Invoke(null, new object[] { 0.6f, 1.8f, 3 }),
                "参考页原本只有一行的宽幅小字应自动缩到单行，避免翻译换行后压住正文");
            MethodInfo shouldFitOriginalLineBudget = referenceFormatterType?.GetMethod(
                "ShouldFitOriginalLineBudget", BindingFlags.Static | BindingFlags.NonPublic);
            Assert(shouldFitOriginalLineBudget != null &&
                   (bool)shouldFitOriginalLineBudget.Invoke(null, new object[] { 31, 34 }) &&
                   (bool)shouldFitOriginalLineBudget.Invoke(null, new object[] { 1, 2 }) &&
                   !(bool)shouldFitOriginalLineBudget.Invoke(null, new object[] { 31, 31 }) &&
                   !(bool)shouldFitOriginalLineBudget.Invoke(null, new object[] { 0, 34 }),
                "长参考页译文超过原始渲染行数时也必须缩放，不能只处理单行小注释");
            MethodInfo isFormulaAnnotation = referenceFormatterType?.GetMethod(
                "IsFormulaAnnotation", BindingFlags.Static | BindingFlags.NonPublic);
            Assert(isFormulaAnnotation != null &&
                   (bool)isFormulaAnnotation.Invoke(null, new object[] { "T/H" }) &&
                   (bool)isFormulaAnnotation.Invoke(null, new object[] { "5.3/5.3" }) &&
                   !(bool)isFormulaAnnotation.Invoke(null, new object[] { "（天文学注：这是一段说明）" }),
                "T/H、5.3/5.3 等短指数必须作为公式结构标注跟随其锚点，普通说明文字不能误判");
            MethodInfo extendForRenderedBottom = typeof(ReferencePageLayoutEngine).GetMethod(
                "ExtendContentHeightForRenderedBottom",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert(extendForRenderedBottom != null &&
                   Math.Abs((float)extendForRenderedBottom.Invoke(null,
                       new object[] { 4.5f, -4.2f, -4.8f, 0.18f }) - 5.28f) < 0.0001f,
                "参考页滚动高度必须包含末行实际下沿和安全余量，不能在拉到底后仍裁掉最后一行");
            MethodInfo heightForTranslatedBounds = typeof(ReferencePageLayoutEngine).GetMethod(
                "HeightForTranslatedBounds", BindingFlags.Static | BindingFlags.NonPublic);
            Assert(heightForTranslatedBounds != null &&
                   Math.Abs((float)heightForTranslatedBounds.Invoke(null,
                       new object[] { 1.6f, 4.5f, -4.2f, -4.8f }) - 5.1f) < 0.0001f &&
                   Math.Abs((float)heightForTranslatedBounds.Invoke(null,
                       new object[] { 1.6f, 4.5f, -4.2f, -3.7f }) - 4.0f) < 0.0001f &&
                   Math.Abs((float)heightForTranslatedBounds.Invoke(null,
                       new object[] { 1.6f, 4.5f, -4.2f, 0.2f }) - 1.6f) < 0.0001f,
                "参考页必须以原始滚动高度为标定，按中文底边相对原文底边的位移增减高度，且不得小于窗口高度");
            MethodInfo shouldTrackOverlay = typeof(ReferencePageLayoutEngine).GetMethod(
                "ShouldTrackAsOverlay", BindingFlags.Static | BindingFlags.NonPublic);
            Assert(shouldTrackOverlay != null &&
                   !(bool)shouldTrackOverlay.Invoke(null, new object[] { true, false }) &&
                   !(bool)shouldTrackOverlay.Invoke(null, new object[] { false, true }) &&
                   (bool)shouldTrackOverlay.Invoke(null, new object[] { false, false }),
                "独立文字和复制按钮不得再被通用最近行算法二次移动，只有图像等附属对象才跟随正文");
            MethodInfo lineVisualCenter = typeof(ReferencePageLayoutEngine).GetMethod(
                "LineVisualCenter", BindingFlags.Static | BindingFlags.NonPublic);
            Assert(lineVisualCenter != null && Math.Abs((float)lineVisualCenter.Invoke(null,
                       new object[] { 0.18f, -0.06f }) - 0.06f) < 0.0001f,
                "复制按钮应按文字行的视觉中线对齐，不能落在基线上");
            MethodInfo chooseBestLine = typeof(ReferencePageLayoutEngine).GetMethod(
                "ChooseBestLine", BindingFlags.Static | BindingFlags.NonPublic);
            Assert(chooseBestLine != null &&
                   (int)chooseBestLine.Invoke(null, new object[]
                   {
                       new[] { 1004, 1004, 0 }, new[] { -0.2f, -1.3f, -2.1f }, -1.2f,
                   }) == 1,
                "复制值在同页重复时，必须在同分候选中选择最接近按钮原始纵坐标的行");
            MethodInfo assignUniqueLines = typeof(ReferencePageLayoutEngine).GetMethod(
                "AssignUniqueLines", BindingFlags.Static | BindingFlags.NonPublic);
            int[] uniqueLines = (int[])assignUniqueLines?.Invoke(null, new object[]
            {
                new[,] { { 1004, 1004 }, { 1004, 1004 } },
                new[,] { { 0.05f, 1.2f }, { 1.1f, 0.04f } },
            });
            Assert(uniqueLines != null && uniqueLines.SequenceEqual(new[] { 0, 1 }),
                "同页复制值相近的多个按钮必须一对一绑定各自最近原始行，不能全部挤到同一行");
            MethodInfo arrangeVerticalBlocks = typeof(ReferencePageLayoutEngine).GetMethod(
                "ArrangeVerticalBlocks", BindingFlags.Static | BindingFlags.NonPublic);
            var originalBlocks = new[]
            {
                new ReferencePageLayoutEngine.VerticalBounds(4.0f, 3.5f),
                new ReferencePageLayoutEngine.VerticalBounds(3.2f, 2.4f),
                new ReferencePageLayoutEngine.VerticalBounds(2.1f, 1.6f),
            };
            var expandedBlocks = new[]
            {
                new ReferencePageLayoutEngine.VerticalBounds(4.0f, 2.9f),
                new ReferencePageLayoutEngine.VerticalBounds(3.2f, 2.4f),
                new ReferencePageLayoutEngine.VerticalBounds(2.1f, 0.9f),
            };
            float[] firstArrangement = (float[])arrangeVerticalBlocks?.Invoke(null,
                new object[] { originalBlocks, expandedBlocks, 0.035f });
            var reappliedBlocks = expandedBlocks.Select((block, index) =>
                new ReferencePageLayoutEngine.VerticalBounds(
                    block.Top + firstArrangement[index],
                    block.Bottom + firstArrangement[index])).ToArray();
            float[] secondArrangement = (float[])arrangeVerticalBlocks?.Invoke(null,
                new object[] { originalBlocks, expandedBlocks, 0.035f });
            Assert(firstArrangement != null && secondArrangement != null &&
                   firstArrangement.SequenceEqual(secondArrangement) &&
                   firstArrangement[0] == 0f && firstArrangement[1] < 0f &&
                   firstArrangement[2] <= firstArrangement[1] &&
                   reappliedBlocks[0].Bottom - reappliedBlocks[1].Top >= 0.035f &&
                   reappliedBlocks[1].Bottom - reappliedBlocks[2].Top >= 0.035f,
                "参考页正文、图片和后续说明必须按实际中文边界顺序下推，且重复计算保持幂等");
            var twoColumnOriginal = new[]
            {
                new ReferencePageLayoutEngine.LayoutBounds(-3.2f, 0.3f, 5.0f, 3.2f),
                new ReferencePageLayoutEngine.LayoutBounds(1.0f, 3.2f, 4.9f, 3.3f),
                new ReferencePageLayoutEngine.LayoutBounds(-2.2f, 2.2f, 2.8f, 2.2f),
            };
            var twoColumnRendered = new[]
            {
                new ReferencePageLayoutEngine.LayoutBounds(-3.2f, 0.3f, 5.0f, 2.8f),
                new ReferencePageLayoutEngine.LayoutBounds(1.0f, 3.2f, 4.9f, 3.1f),
                new ReferencePageLayoutEngine.LayoutBounds(-2.2f, 2.2f, 2.8f, 2.2f),
            };
            float[] rowArrangement = ReferencePageLayoutEngine.ArrangeVerticalRows(
                twoColumnOriginal, twoColumnRendered, 0.035f);
            Assert(rowArrangement.Length == 3 && rowArrangement[0] == 0f &&
                   rowArrangement[1] == 0f && rowArrangement[2] < 0f,
                "2D/3D 参考页同一行的介绍和 KEY 栏必须作为一个横向行组移动，不能把右栏排到左栏下面");
            MethodInfo shiftTextOutsideGraphics = typeof(ReferencePageLayoutEngine).GetMethod(
                "ShiftTextOutsideGraphics", BindingFlags.Static | BindingFlags.NonPublic);
            float starCaptionShift = (float)shiftTextOutsideGraphics?.Invoke(null,
                new object[]
                {
                    new ReferencePageLayoutEngine.LayoutBounds(-0.9f, 0.9f, 0.8f, 0.5f),
                    new ReferencePageLayoutEngine.LayoutBounds(-0.9f, 0.9f, 0.8f, 0.1f),
                    new[] { new ReferencePageLayoutEngine.LayoutBounds(-0.3f, 0.3f, 0.42f, -0.1f) },
                    0.025f,
                });
            Assert(starCaptionShift > 0.3f,
                "星体参考页的长中文首段侵入图片时必须向上避让，图片本身保持原位");
            MethodInfo mayShrinkStaticText = fontPolicyType?.GetMethod(
                "MayShrinkStaticText", BindingFlags.Static | BindingFlags.NonPublic);
            Assert(mayShrinkStaticText != null &&
                   !(bool)mayShrinkStaticText.Invoke(null,
                       new object[] { "PERIODIC TABLE PAGE", "Hydrogen", "氢" }) &&
                   !(bool)mayShrinkStaticText.Invoke(null,
                       new object[] { "ELEMENT DISPLAY", "Hydrogen", "氢" }) &&
                   !(bool)mayShrinkStaticText.Invoke(null,
                       new object[] { "STAR PAGE", "动态值", "动态值" }) &&
                   (bool)mayShrinkStaticText.Invoke(null,
                       new object[] { "STAR PAGE", "Original note", "中文说明" }),
                "周期表动态详情和未发生翻译的占位文本不得参与字号缩放，避免反复打开越缩越小");
            MethodInfo matchObjectScore = typeof(ReferenceCopyButtonLayout).GetMethod(
                "MatchObjectScore", BindingFlags.Static | BindingFlags.NonPublic);
            Assert(matchObjectScore != null &&
                   (int)matchObjectScore.Invoke(null,
                       new object[] { "Copy Kilograms per Filogram", "Filogram" }) > 0 &&
                   (int)matchObjectScore.Invoke(null,
                       new object[] { "Copy Filograms per MFG", "Filo -> MFG" }) > 0 &&
                   (int)matchObjectScore.Invoke(null,
                       new object[] { "Copy SOL", "ABOVE STUFF" }) > 0 &&
                   (int)matchObjectScore.Invoke(null,
                       new object[] { "Copy Filograms per MFG", "Filogram" }) == 0,
                "复制值不直接出现在公式文字中时，按钮必须按用途绑定唯一组件，不能留在旧行或挤到另一按钮行");
            MethodInfo readableLineSpacing = fontPolicyType?.GetMethod(
                "ReadableLineSpacing", BindingFlags.Static | BindingFlags.NonPublic);
            Assert(readableLineSpacing != null &&
                   (float)readableLineSpacing.Invoke(null,
                       new object[] { 0f, 0.6f, "Line one\nLine two", "第一行\n第二行", "STAR PAGE" }) > 0.1f &&
                   (float)readableLineSpacing.Invoke(null,
                       new object[] { 0f, 0.6f, "Value", "值", "PERIODIC TABLE PAGE" }) == 0f &&
                   (float)readableLineSpacing.Invoke(null,
                       new object[] { 0f, 0.6f, "TRUE\nFALSE", "真\n假", "LOGIC PAGE" }) == 0f,
                "中文多行说明必须增加可读行距，同时动态元素页和固定网格页保持原始字号与行距");
            Assert(typeof(DeepSpaceChinesePlugin).Assembly.GetType(
                       "DeepSpaceChinese.ReferenceSubWindowOpenPatch") != null,
                "参考子页首次打开后必须再次重排，不能要求玩家按 F5 才生效");
            string referenceLayoutRuntimeSource = File.ReadAllText(Path.Combine(projectRoot,
                "src", "DeepSpaceChinese", "ReferencePageLayoutRuntime.cs"));
            Assert(referenceLayoutRuntimeSource.Contains("FitAllTextToOriginalLineBudgets") &&
                   referenceLayoutRuntimeSource.Contains("ApplyFixedStructureTextMetrics") &&
                   referenceLayoutRuntimeSource.Contains("CaptureButtonAnchor") &&
                   referenceLayoutRuntimeSource.Contains("TryResolveButtonLineEnd") &&
                   referenceLayoutRuntimeSource.Contains("AnchoredText") &&
                   referenceLayoutRuntimeSource.Contains("Bounds bounds = text.textBounds") &&
                   referenceLayoutRuntimeSource.Contains("TransformPoint(bounds.min)") &&
                   referenceLayoutRuntimeSource.Contains("saved.SubWindow.FullInfoHeight") &&
                   referenceLayoutRuntimeSource.Contains("ScrollAreaField?.GetValue(candidate)") &&
                   referenceLayoutRuntimeSource.Contains("OriginalFullInfoHeightFor"),
                "参考页运行时必须统一按原文行数压缩译文、以原文本锚定复制按钮，并通过 scrollArea 引用找到分离式页面的真正窗口后按中文边界更新滚动高度");
            string referenceCaptureRuntimeSource = File.ReadAllText(Path.Combine(projectRoot,
                "src", "DeepSpaceChinese", "ReferenceCaptureRuntime.cs"));
            Assert(referenceCaptureRuntimeSource.Contains(
                       "Math.Max(fullHeight, originalFullHeight)") &&
                   referenceCaptureRuntimeSource.Contains(
                       "CaptureLayout(page, languageCode, normalized") &&
                   referenceCaptureRuntimeSource.Contains("pngPath, captureCamera") &&
                   referenceCaptureRuntimeSource.Contains(
                       "Resources.FindObjectsOfTypeAll<PeriodicTableDisplay>()") &&
                   referenceCaptureRuntimeSource.Contains("RT Info Monitor") &&
                   referenceCaptureRuntimeSource.Contains(
                       ".OrderByDescending(candidate => candidate.Visible)") &&
                   referenceCaptureRuntimeSource.Contains(
                       "FindRenderingCamera(infoDisplay?.transform)") &&
                   referenceCaptureRuntimeSource.Contains(
                       "FindCaptureCamera(contentRoot)") &&
                   referenceCaptureRuntimeSource.Contains("LockedCaptureCamera") &&
                   referenceCaptureRuntimeSource.Contains("_lockedCaptureCamera") &&
                   referenceCaptureRuntimeSource.Contains("CaptureTexts(contentRoot)") &&
                   referenceCaptureRuntimeSource.Contains(
                       "RenderedTextHitScore(_lockedCaptureCamera, texts) > 0") &&
                   referenceCaptureRuntimeSource.Contains(
                       "probe.BrightPixelCount >= 1000 && probe.Score >= 2") &&
                   referenceCaptureRuntimeSource.Contains(
                       "float cameraDeadline = Time.realtimeSinceStartup + 6f") &&
                   referenceCaptureRuntimeSource.Contains("scrollBar?.ForceScrollTo(1f)") &&
                   referenceCaptureRuntimeSource.Contains("infoDisplay.OpenReference();") &&
                   referenceCaptureRuntimeSource.Contains("reopenCount") &&
                   referenceCaptureRuntimeSource.Contains("RenderedTextHitScore") &&
                   !referenceCaptureRuntimeSource.Contains("camera.Render();") &&
                   referenceCaptureRuntimeSource.Contains("Graphics.Blit") &&
                   referenceCaptureRuntimeSource.Contains(".Take(128)") &&
                   referenceCaptureRuntimeSource.Contains("rightViewport <= 0f") &&
                   referenceCaptureRuntimeSource.Contains(
                       "Time.realtimeSinceStartup - readySince >= 10f"),
                "中英文成对截图必须等待主界面稳定十秒、按两种语言的较大高度采样，每次切换语言先回到顶部再用当前页的实际文字像素验证或重新锁定参考摄像机，并有界地重走参考窗口打开流程，且排除屏外字形造成的启动图标误命中");
            string referenceContactSheetSource = File.ReadAllText(Path.Combine(projectRoot,
                "tools", "make_reference_contact_sheet.py"));
            Assert(referenceContactSheetSource.Contains("PILLOW_RUNTIME") &&
                   referenceContactSheetSource.Contains(
                       "name.casefold() not in selected_pages") &&
                   referenceContactSheetSource.Contains("\"--all-frames\"") &&
                   referenceContactSheetSource.Contains("if all_frames:") &&
                   !referenceContactSheetSource.Contains("TOOLS_DIR / \"python-packages\""),
                "参考页联系表必须使用隔离的固定 Pillow、精确匹配页面名并支持审查全部滚动帧");
            Assert(!referenceLayoutRuntimeSource.Contains("FitFlowToOriginalLineBudget") &&
                   referenceLayoutRuntimeSource.IndexOf("RestoreLayoutBaseline();", StringComparison.Ordinal) <
                   referenceLayoutRuntimeSource.IndexOf("CaptureButtonAnchors();", StringComparison.Ordinal),
                "参考页每次重排必须先恢复初始排版，且不得再对动态文本流递归缩小字号");
            Assert(referenceLayoutRuntimeSource.Contains(
                       "if (IsFixedStructurePage(saved.Area.transform))") &&
                   referenceLayoutRuntimeSource.Contains(
                       "pageName is \"LOGIC PAGE\" or \"DISTANCE PAGE\""),
                "真值表和距离公式必须保持原版固定结构，不能参与普通文本块重排");
            string periodicTableCompatibilitySource = File.ReadAllText(Path.Combine(projectRoot,
                "src", "DeepSpaceChinese", "PeriodicTableElementCompatibility.cs"));
            string pluginRuntimeSource = File.ReadAllText(Path.Combine(projectRoot,
                "src", "DeepSpaceChinese", "DeepSpaceChinesePlugin.cs"));
            Assert(periodicTableCompatibilitySource.Contains(
                       "nameof(PeriodicTableDisplay.DisplayElementData)") &&
                   periodicTableCompatibilitySource.Contains("PeriodicTableElementDisplayed") &&
                   pluginRuntimeSource.Contains("ReapplyPeriodicTableAfterDisplay") &&
                   pluginRuntimeSource.Contains("_ui.ReapplyUnder(pageRoot ?? display.transform)") &&
                   pluginRuntimeSource.Contains("_referencePageLayout.ApplyContaining(display.transform)") &&
                   pluginRuntimeSource.Contains("_referencePageLayout.ApplyFor(subWindow)") &&
                   referenceLayoutRuntimeSource.Contains("internal void ApplyFor(ReferenceSubWindow subWindow)") &&
                   referenceLayoutRuntimeSource.Contains("private int ApplyCopyButtons(Transform root)") &&
                   !referenceCaptureRuntimeSource.Contains("[DEBUG-refcam]"),
                "周期表动态详情与首次打开的参考页必须在原版写值结束后仅重应用当前页面，避免残留英文、全局扫描卡顿和临时调试日志");
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
            int[] puzzle460Original =
            {
                -14, -100, -117, -15, -77, -14, -100, -29, -117, -15, -2, -2,
                -18, 0, -22, -4, 3, -2, -2,
                -18, 1, -22, -4, 1, 2, 9, 8, -2, -2,
                -18, 2, -22, -4, 3, -10, 1, 2, 5, -2, -2,
                -18, 0, -16, -23, -4, 5, -2, -2,
                -18, 1, -16, -23, -4, 6, -2, -2,
                -18, 2, -16, -23, -4, 9, 4, 2, -2, -2,
                -18, 0, -100, -117, -12
            };
            int[] puzzle460Replacement =
            {
                -18, 0, -22, -4, 3, -2, -2,
                -18, 1, -22, -4, 1, 2, 9, 8, -2, -2,
                -18, 2, -22, -4, 3, -10, 1, 2, 5, -2, -2,
                -18, 0, -16, -23, -4, 5, -2, -2,
                -18, 1, -16, -23, -4, 6, -2, -2,
                -18, 2, -16, -23, -4, 9, 4, 2, -2, -2,
                -18, 0, -100, -117, -12
            };
            string puzzle460FixPath = Path.Combine(projectRoot, "patch", "Fix", "460.json");
            string puzzle460FixJson = File.ReadAllText(puzzle460FixPath);
            Assert(PuzzleFixRule.TryParse(puzzle460FixJson, "460.json",
                       out PuzzleFixRule puzzle460Rule, out string puzzle460ParseError) &&
                   puzzle460ParseError == null &&
                   puzzle460Rule.Matches(puzzle460Original) &&
                   puzzle460Rule.TryCreatePlan(puzzle460Original,
                       new[] { new[] { -18, 2 } },
                       out PuzzleFixPlan puzzle460Plan, out string puzzle460PlanError) &&
                   puzzle460PlanError == null &&
                   PuzzleFixRule.SignalsEqual(puzzle460Plan.ReplacementSignals,
                       puzzle460Replacement) &&
                   puzzle460Plan.ReplacementAnswers == null,
                "第 460 题必须只删除误复制的关系二选一前缀，并保持原答案集不变");
            int[] puzzle490Original =
            {
                -126, -2, -2,
                -126, -99, -79, -2, -2,
                -19, 0, -14, 0, -3, 1, -3, 2, -3, 3, -3, 4, -3, -25, -15, -2, -2,
                -19, 0, -23, -4, -126, -2, -2,
                -19, 0, -110, -5, -4, -126,
                -19, 1, -14, 0, -3, 2, -3, 4, -3, 6, -3, 8, -3, -25, -15, -2, -2,
                -19, 1, -23, -4, -126, -2, -2,
                -19, 1, -110, -5, -4, -12
            };
            int[] puzzle490Replacement =
            {
                -126, -2, -2,
                -126, -99, -79, -2, -2,
                -19, 0, -14, 0, -3, 1, -3, 2, -3, 3, -3, 4, -3, -25, -15, -2, -2,
                -19, 0, -23, -4, -126, -2, -2,
                -19, 0, -110, -5, -4, -126, -2, -2,
                -19, 1, -14, 0, -3, 2, -3, 4, -3, 6, -3, 8, -3, -25, -15, -2, -2,
                -19, 1, -23, -4, -126, -2, -2,
                -19, 1, -110, -5, -4, -12
            };
            string puzzle490FixPath = Path.Combine(projectRoot, "patch", "Fix", "490.json");
            string puzzle490FixJson = File.ReadAllText(puzzle490FixPath);
            Assert(PuzzleFixRule.TryParse(puzzle490FixJson, "490.json",
                       out PuzzleFixRule puzzle490Rule, out string puzzle490ParseError) &&
                   puzzle490ParseError == null &&
                   puzzle490Rule.Matches(puzzle490Original) &&
                   puzzle490Rule.TryCreatePlan(puzzle490Original,
                       new[] { new[] { -126 } },
                       out PuzzleFixPlan puzzle490Plan, out string puzzle490PlanError) &&
                   puzzle490PlanError == null &&
                   PuzzleFixRule.SignalsEqual(puzzle490Plan.ReplacementSignals,
                       puzzle490Replacement) &&
                   puzzle490Plan.ReplacementAnswers == null,
                "第 490 题必须补回结构体 0 与结构体 1 之间缺失的双分号，并保持答案不变");
            int[] puzzle598Original =
            {
                -137, -2, -2,
                -137, -99, -79, -2, -2,
                -137, -99, -129, -128, -2, -2,
                -136, -100, -129, -30, -137, -100, -129, -2, -2,
                -136, -131, -30, -137, -131, -4, -12
            };
            int[] puzzle598Replacement =
            {
                -137, -2, -2,
                -137, -99, -79, -2, -2,
                -137, -99, -129, -2, -2,
                -136, -100, -129, -30, -137, -100, -129, -2, -2,
                -136, -131, -30, -137, -131, -4, -12
            };
            string puzzle598FixPath = Path.Combine(projectRoot, "patch", "Fix", "598.json");
            string puzzle598FixJson = File.ReadAllText(puzzle598FixPath);
            Assert(PuzzleFixRule.TryParse(puzzle598FixJson, "598.json",
                       out PuzzleFixRule puzzle598Rule, out string puzzle598ParseError) &&
                   puzzle598ParseError == null &&
                   puzzle598Rule.Matches(puzzle598Original) &&
                   puzzle598Rule.TryCreatePlan(puzzle598Original,
                       new[] { new[] { -45 } },
                       out PuzzleFixPlan puzzle598Plan, out string puzzle598PlanError) &&
                   puzzle598PlanError == null &&
                   PuzzleFixRule.SignalsEqual(puzzle598Plan.ReplacementSignals,
                       puzzle598Replacement) &&
                   puzzle598Plan.ReplacementAnswers == null,
                "第 598 题必须删除 AUTHORMATE 分类句中多余的 INDIVIDUAL，并保持答案不变");
            string puzzle483FixPath = Path.Combine(projectRoot, "patch", "Fix", "483.json");
            string puzzle483FixJson = File.ReadAllText(puzzle483FixPath);
            Assert(PuzzleFixRule.TryParse(puzzle483FixJson, "483.json",
                       out PuzzleFixRule puzzle483Rule, out string puzzle483ParseError) &&
                   puzzle483ParseError == null &&
                   !puzzle483Rule.HasQuestionReplacement &&
                   puzzle483Rule.TryCreatePlan(new[] { 999 },
                       new[] { new[] { -11, 1, -100, -123 } },
                       out PuzzleFixPlan puzzle483Plan, out string puzzle483PlanError) &&
                   puzzle483PlanError == null &&
                   puzzle483Plan.ReplacementSignals == null &&
                   PuzzleFixRule.AnswerSetsEqual(puzzle483Plan.ReplacementAnswers,
                       new[]
                       {
                           new[] { -11, 1, -100, -123 },
                           new[] { -11, 1, -100, -123, -30, -11, 0, -4, 1 },
                       }),
                "第 483 题必须保留原主答案，并接受‘变量1是已知且变量0等于1’作为备用答案");
            string puzzle649FixPath = Path.Combine(projectRoot, "patch", "Fix", "649.json");
            string puzzle649FixJson = File.ReadAllText(puzzle649FixPath);
            Assert(PuzzleFixRule.TryParse(puzzle649FixJson, "649.json",
                       out PuzzleFixRule puzzle649Rule, out string puzzle649ParseError) &&
                   puzzle649ParseError == null &&
                   !puzzle649Rule.HasQuestionReplacement &&
                   puzzle649Rule.TryCreatePlan(new[] { 999 },
                       new[]
                       {
                           new[] { -57, 0 },
                           new[] { -57, 0, -14, 2, -56, 7, -15 },
                           new[] { 2, -56, 7 },
                       },
                       out PuzzleFixPlan puzzle649Plan, out string puzzle649PlanError) &&
                   puzzle649PlanError == null &&
                   puzzle649Plan.ReplacementSignals == null &&
                   PuzzleFixRule.AnswerSetsEqual(puzzle649Plan.ReplacementAnswers,
                       new[]
                       {
                           new[] { -57, 1 },
                           new[] { -57, 1, -14, 2, -56, 7, -15 },
                           new[] { 2, -56, 7 },
                           new[] { -57, -14, 2, -56, 7, -15 },
                       }),
                "第 649 题必须把错误的化合物 0 改为化合物 1，并接受完整组成写法");
            int[] puzzle650Original =
            {
                -168, -172, -100, -115, -57, 3, -30,
                -119, -172, -100, -115, -57, 0, -36, -172, -12, -85,
            };
            int[] puzzle650Replacement =
            {
                -168, -172, -100, -115, -57, 3, -30,
                -119, -172, -100, -115, -57, 1, -36, -172, -12, -85,
            };
            string puzzle650FixPath = Path.Combine(projectRoot, "patch", "Fix", "650.json");
            string puzzle650FixJson = File.ReadAllText(puzzle650FixPath);
            Assert(PuzzleFixRule.TryParse(puzzle650FixJson, "650.json",
                       out PuzzleFixRule puzzle650Rule, out string puzzle650ParseError) &&
                   puzzle650ParseError == null &&
                   puzzle650Rule.HasQuestionReplacement &&
                   !puzzle650Rule.HasAnswerReplacement &&
                   puzzle650Rule.Matches(puzzle650Original) &&
                   puzzle650Rule.TryCreatePlan(puzzle650Original,
                       new[] { new[] { -174 } },
                       out PuzzleFixPlan puzzle650Plan, out string puzzle650PlanError) &&
                   puzzle650PlanError == null &&
                   PuzzleFixRule.SignalsEqual(puzzle650Plan.ReplacementSignals,
                       puzzle650Replacement) &&
                   puzzle650Plan.ReplacementAnswers == null,
                "第 650 题必须把现在大气层的化合物 0 改为化合物 1，并保持答案不变");
            int[] puzzle681Original =
            {
                -109, -174, -100, -109, -174, -2, -2,
                -109, -174, -2, -109, -174, -5, -108, -174, -2, -2,
                -109, -174, -2, -109, -174, -2, -109, -174, -2,
                -109, -174, -2, -25, -5, -100, -12,
            };
            int[] puzzle681Replacement =
            {
                -109, -174, -100, -109, -174, -2, -2,
                -109, -174, -2, -109, -174, -5, -100, -108, -174, -2, -2,
                -109, -174, -2, -109, -174, -2, -109, -174, -2,
                -109, -174, -2, -25, -5, -100, -12,
            };
            string puzzle681FixPath = Path.Combine(projectRoot, "patch", "Fix", "681.json");
            string puzzle681FixJson = File.ReadAllText(puzzle681FixPath);
            Assert(PuzzleFixRule.TryParse(puzzle681FixJson, "681.json",
                       out PuzzleFixRule puzzle681Rule, out string puzzle681ParseError) &&
                   puzzle681ParseError == null &&
                   puzzle681Rule.HasQuestionReplacement &&
                   !puzzle681Rule.HasAnswerReplacement &&
                   puzzle681Rule.Matches(puzzle681Original) &&
                   puzzle681Rule.TryCreatePlan(puzzle681Original,
                       new[] { new[] { -107, -108, -174 }, new[] { -108, -174 } },
                       out PuzzleFixPlan puzzle681Plan, out string puzzle681PlanError) &&
                   puzzle681PlanError == null &&
                   PuzzleFixRule.SignalsEqual(puzzle681Plan.ReplacementSignals,
                       puzzle681Replacement) &&
                   puzzle681Plan.ReplacementAnswers == null,
                "第 681 题必须在第二个类加法表达式的加号后补入‘是’，并保持答案不变");
            int[] puzzle700Original =
            {
                -189, -3, -190, -2, -2,
                -189, -3, -190, -99, -80, -2, -2,
                -189, -100, -186, -39, -2, -2,
                -190, -100, -189, -87, -2, -2,
                -16, 0, -14, 0, -3, 0, -15, -16, 1, -14, 8, -3, 0, -15, -2, -2,
                -16, 0, -30, -16, 1, -189, -36, -14, 4, -3, 0, -15, -41,
                -16, 0, -39, -85, -30, -12, -41, -16, 1, -39, -85,
            };
            int[] puzzle700Replacement =
            {
                -189, -3, -190, -2, -2,
                -189, -3, -190, -99, -80, -2, -2,
                -189, -100, -186, -39, -2, -2,
                -190, -100, -189, -87, -2, -2,
                -16, 0, -14, 0, -3, 0, -15, -16, 1, -14, 8, -3, 0, -15, -2, -2,
                -16, 0, -30, -16, 1, -189, -85, -36, -16, -14, 4, -3, 0, -15, -41,
                -16, 0, -39, -85, -30, -12, -41, -16, 1, -39, -85,
            };
            string puzzle700FixPath = Path.Combine(projectRoot, "patch", "Fix", "700.json");
            string puzzle700FixJson = File.ReadAllText(puzzle700FixPath);
            Assert(PuzzleFixRule.TryParse(puzzle700FixJson, "700.json",
                       out PuzzleFixRule puzzle700Rule, out string puzzle700ParseError) &&
                   puzzle700ParseError == null &&
                   puzzle700Rule.HasQuestionReplacement &&
                   puzzle700Rule.HasAnswerReplacement &&
                   puzzle700Rule.TryCreatePlan(puzzle700Original,
                       new[] { new[] { -14, 4, -3, 0, -15 } },
                       out PuzzleFixPlan puzzle700Plan, out string puzzle700PlanError) &&
                   puzzle700PlanError == null &&
                   PuzzleFixRule.SignalsEqual(puzzle700Plan.ReplacementSignals,
                       puzzle700Replacement) &&
                   PuzzleFixRule.AnswerSetsEqual(puzzle700Plan.ReplacementAnswers,
                       new[]
                       {
                           new[] { -14, 4, -3, 0, -15 },
                           new[] { -16, -14, 4, -3, 0, -15 },
                       }),
                "第 700 题必须补全‘做’和目的地点，并额外接受‘点 (4, 0)’");
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
            var punctuationCompilerEntries = new[]
            {
                new System.Collections.Generic.KeyValuePair<string, int>("左【右】；完。", -201),
                new System.Collections.Generic.KeyValuePair<string, int>("甲，乙", -202),
            };
            Assert(CompilerCaseCompatibility.TryResolve("左[右];完.", punctuationCompilerEntries,
                       false, true, out int punctuationSignal) && punctuationSignal == -201 &&
                   CompilerCaseCompatibility.TryResolve("甲,乙", punctuationCompilerEntries,
                       false, true, out int commaSignal) && commaSignal == -202,
                "编译兼容必须把中英文逗号、句号、分号、圆括号和方括号视为等价标点");
            Assert(CompilerCaseCompatibility.NormalizeForReformatter(
                       "左[右];完.", punctuationCompilerEntries.Select(pair => pair.Key),
                       false, true) == "左【右】；完。" &&
                   !CompilerCaseCompatibility.TryResolve("甲,a", new[]
                       {
                           new System.Collections.Generic.KeyValuePair<string, int>("甲，A", -202),
                           new System.Collections.Generic.KeyValuePair<string, int>("甲,A", -203),
                       }, true, true, out _),
                "重格式化器必须恢复词典实际标点；标点规范化后有多个候选时不得误选");
            var dictionaryNames = new[]
            {
                new System.Collections.Generic.KeyValuePair<string, int>("甲（乙）", -1),
                new System.Collections.Generic.KeyValuePair<string, int>("VAR", -2),
            };
            Assert(DictionaryNameConflictCompatibility.HasConflict("甲(乙)", dictionaryNames,
                       null, true, true) &&
                   !DictionaryNameConflictCompatibility.HasConflict("甲(乙)", dictionaryNames,
                       -1, true, true) &&
                   DictionaryNameConflictCompatibility.HasConflict("var", dictionaryNames,
                       null, true, true) &&
                   !DictionaryNameConflictCompatibility.HasConflict("甲(乙)", dictionaryNames,
                       null, true, false),
                "词典新增和编辑的冲突校验必须同时遵守大小写及标点兼容开关");
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
                "[Compatibility]\nCompilerCaseInsensitive=false\nCompilerPunctuationInsensitive=false\n" +
                "[Layout]\nNewWordPromptLowerRight=false\n" +
                "[PuzzleFixes]\nEnabled=false\n" +
                "[Font]\nFontSource=File\nFontFile=CustomChinese.otf\n" +
                "SystemFontCandidates=Test Sans;Test Hei\n");
            config.ReloadCompatibilitySettings(iniPath, log);
            Assert(!config.CompilerCaseInsensitive && !config.CompilerPunctuationInsensitive &&
                   !config.PuzzleFixesEnabled,
                "兼容项和题面修正开关必须能从 INI 热重载");
            config.ReloadLayoutSettings(iniPath, log);
            Assert(!config.MoveNewWordPromptToLowerRight,
                "新单词浮窗位置开关必须能从 INI 热重载");
            File.WriteAllText(iniPath,
                "[Cheats]\nKonamiAnswerAutofill=false\n" +
                "[Font]\nFontSource=File\nFontFile=CustomChinese.otf\n" +
                "SystemFontCandidates=Test Sans;Test Hei\n");
            config.ReloadCheatSettings(iniPath, log);
            Assert(!config.KonamiAnswerAutofillEnabled,
                "科乐美序列填入正确答案开关必须能从 INI 热重载");
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
            RunSignalEmbedBoundaryTests();
            RunLiveDialogueSwitchTests(projectRoot);

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
            string preparedCompilerError = CompilerErrorRuntime.PrepareForTyping(
                compilerError, DisplayMode.TranslationOnly);
            Assert(preparedCompilerError.StartsWith("- 编译失败 -", StringComparison.Ordinal) &&
                   !preparedCompilerError.Contains("Compilation Failed") &&
                   CompilerErrorRuntime.TryResolvePreparedSource(preparedCompilerError,
                       out string preparedCompilerOriginal,
                       out string preparedCompilerTranslation) &&
                   preparedCompilerOriginal == compilerError &&
                   preparedCompilerTranslation == preparedCompilerError,
                "编译错误必须在逐字协程开始前整体翻译，并保留原文映射供 F8 恢复");
            Type compilerErrorTypingPatch = typeof(DeepSpaceChinesePlugin).Assembly.GetType(
                "DeepSpaceChinese.CompilerResultErrorMessagePatch");
            Assert(compilerErrorTypingPatch?.GetMethod("Postfix",
                       BindingFlags.Static | BindingFlags.NonPublic) != null,
                "CompilerResult.ErrorMsg 必须在逐字动画取得完整源文本时预先翻译，不能等英文逐字显示完再整体替换");

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
            Assert(PlayerNameRuntime.FormatFullName("Eric", "Dr. Eric", DisplayMode.TranslationOnly) == "Eric 博士" &&
                   PlayerNameRuntime.FormatFullName("Eric博士", "Dr. Eric博士", DisplayMode.TranslationOnly) == "Eric 博士" &&
                   PlayerNameRuntime.FormatFullName("Dr.Eric", "Dr. Dr.Eric", DisplayMode.TranslationOnly) == "Eric 博士" &&
                   PlayerNameRuntime.FormatFullName("Dr. Eric", "Dr. Dr. Eric", DisplayMode.TranslationOnly) == "Eric 博士" &&
                   PlayerNameRuntime.FormatFullName("Dr.Eric", "Dr. Eric", DisplayMode.OriginalOnly) == "Dr. Eric",
                "拉丁字母玩家名与博士之间必须留空格，且译文显示层必须兼容带 Dr. 前缀的旧存档值");
            string latinPlayerSpacing = TokenCodec.FormatDisplay(
                "还有{PLAYER_NAME}，那{PLAYER_NAME}呢？解得漂亮，{PLAYER_NAME}！",
                "There is also the Translator. Nice work, Translator!", config, "Eric 博士");
            Assert(latinPlayerSpacing == "还有 Eric 博士，那 Eric 博士 呢？解得漂亮，Eric 博士！",
                "拉丁字母玩家名在中文正文中必须按可见文字与标点边界补空格");
            string chinesePlayerSpacing = TokenCodec.FormatDisplay(
                "还有 {PLAYER_NAME} ，那 {PLAYER_NAME} 呢？",
                "There is also the Translator.", config, "林博士");
            Assert(chinesePlayerSpacing == "还有林博士，那林博士呢？",
                "中文玩家名在中文正文中不得保留模板为英文姓名预留的空格");
            string startupPlayerSpacing = TokenCodec.RestoreForEntry(
                "METEOR_OS{PLAYER_NAME}.MOS", componentEntry, "林博士", true);
            Assert(startupPlayerSpacing == "METEOR_OS 林博士.MOS",
                "启动界面中玩家名前一个可见字符为英文字母时必须补空格，即使玩家名是中文");
            Assert(TokenCodec.TrySplitFrameTranslation(
                       "{SPEAKER_AKERS}{PART_000}还有{PLAYER_NAME}。{PART_001}{PLAYER_NAME}正在记录。",
                       2, "Eric 博士", out string[] latinPlayerParts) &&
                   latinPlayerParts[0] == "还有 Eric 博士。" &&
                   latinPlayerParts[1] == "Eric 博士 正在记录。",
                "对话 PART 路径也必须应用玩家名的中英文空格规则");
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
            Assert(DialogueChineseTypography.Normalize("<u>外星克</u>. \r") ==
                       "<u>外星克</u>。 \r",
                "译文模式下动态词语后的英文句号必须规范为中文句号");
            Assert(DialogueChineseTypography.Normalize(
                       "他们描述了自己的 <u>恒星</u> 和 <u>行星</u>，") ==
                       "他们描述了自己的<u>恒星</u>和<u>行星</u>，",
                "动态词典词语前后不得保留英文分词产生的额外空格");
            Assert(DialogueChineseTypography.Normalize("NAME <u>VAR</u> NOW") ==
                       "NAME <u>VAR</u> NOW",
                "英文动态词语必须保留英文分词空格");
            Assert(DialogueChineseTypography.Normalize("词语 <u>VAR</u> 仍为英文") ==
                       "词语 <u>VAR</u> 仍为英文",
                "英文动态词语即使位于中文句子中也必须保留分词空格");
            Assert(DialogueChineseTypography.Normalize("版本 1.2.3。RUN CORE.MOS") ==
                       "版本 1.2.3。RUN CORE.MOS",
                "小数、版本号和程序文件名中的半角句点不得被误改");
            Assert(DialogueChineseTypography.ShouldNormalize(
                       DisplayMode.TranslationOnly, false, true),
                "汉化模式下的日志回放正文必须执行中文动态词语和标点规范化");
            Assert(!DialogueChineseTypography.ShouldNormalize(
                       DisplayMode.OriginalOnly, false, true),
                "原文模式下的日志回放正文不得执行中文排版规范化");
            Assert(LogTitleRuntime.NormalizeReplayBodyForDisplay(
                       "科：还有 <u>外星纳米</u>！\n多：<u>外星克</u>.",
                       DisplayMode.TranslationOnly) ==
                   "科：还有<u>外星纳米</u>！\n多：<u>外星克</u>。",
                "日志回放必须同时清理中文动态词语空格并修正英文句号");
            Assert(LogTitleRuntime.NormalizeReplayBodyForDisplay(
                       "C: More <u>EXOPLANET</u>.\nD: <u>EXO</u>.",
                       DisplayMode.OriginalOnly) ==
                   "C: More <u>EXOPLANET</u>.\nD: <u>EXO</u>.",
                "日志回放切到原文后必须完整保留英文标点和空格");
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
                @"public void ApplySharedInput\(InputTextDummy dummy, TMP_Text targetText\)(?<body>[\s\S]*?)\n    public void UpdateImeCursorPosition");
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
                   playerNameRuntimeSource.Contains(
                       "Postfix(InputTextDummy __instance, TMP_Text __0)") &&
                   playerNameRuntimeSource.Contains(
                       "ApplySharedInput(__instance, __0)") &&
                   sharedInputMethod.Groups["body"].Value.Contains(
                       "BuildObjectPath(targetText)") &&
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
            const string sixteenChineseCharacters = "一二三四五六七八九十甲乙丙丁戊己";
            Assert(DictionaryTermNameInputPolicy.IsTermNameInput(
                       "Input Text Dummy - Term Names", null) &&
                   DictionaryTermNameInputPolicy.IsTermNameInput(
                       null, "TermNameInputValidator") &&
                   !DictionaryTermNameInputPolicy.IsTermNameInput(
                       "Input Text Dummy - Puzzle Input", null) &&
                   DictionaryTermNameInputPolicy.IsLegal(sixteenChineseCharacters) &&
                   !DictionaryTermNameInputPolicy.IsLegal(sixteenChineseCharacters + "庚") &&
                   !DictionaryTermNameInputPolicy.IsLegal("变量1") &&
                   !DictionaryTermNameInputPolicy.IsLegal("变量１") &&
                   !DictionaryTermNameInputPolicy.IsLegal("变量 名") &&
                   !DictionaryTermNameInputPolicy.IsLegal("变量　名") &&
                   DictionaryTermNameInputPolicy.ValidateCharacter("变量", 2, '中') == '中' &&
                   DictionaryTermNameInputPolicy.ValidateCharacter("变量", 2, 'a') == 'A' &&
                   DictionaryTermNameInputPolicy.ValidateCharacter("变量", 2, '1') == '\0' &&
                   DictionaryTermNameInputPolicy.ValidateCharacter("变量", 2, ' ') == '\0' &&
                   DictionaryTermNameInputPolicy.NormalizeForSubmit("中文name") == "中文NAME" &&
                   DictionaryTermNameInputPolicy.NormalizeForSubmit("中文 name") == string.Empty,
                "词典条目和 NAME SIGNAL 必须共用 16 字限制，禁止数字/空白并自动大写英文字母");
            Assert(!DictionaryTermNameInputPolicy.IsTermNameInput(
                       "Input Text Dummy - Term Names", "TermNameInputValidator",
                       "ControlRoom/Dictionary Window/Viewport/DictionaryNotes/Notes Content") &&
                   DictionaryTermNameInputPolicy.ResolveCharacterLimit(
                       isTranslatorNotes: true, currentLimit: 16) == 0 &&
                   DictionaryTermNameInputPolicy.ResolveCharacterLimit(
                       isTranslatorNotes: false, currentLimit: 48) == 48,
                "译者注即使复用 Term Names 输入框也必须允许数字、空格和无限长度");
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
                "    { \"stable_key\": \"display:years\", \"kind\": \"display_value\", " +
                "\"source_sha256\": \"" + TokenCodec.Sha256("years") + "\", " +
                "\"source_text\": \"years\", \"translated_text\": \"年\", " +
                "\"game\": { \"original_text\": \"years\" } },\n" +
                "    { \"stable_key\": \"display:shapes\", \"kind\": \"display_value\", " +
                "\"source_sha256\": \"" + TokenCodec.Sha256("Shapes") + "\", " +
                "\"source_text\": \"Shapes\", \"translated_text\": \"形状\", " +
                "\"game\": { \"original_text\": \"Shapes\" } },\n" +
                "    { \"stable_key\": \"display:artificial\", \"kind\": \"display_value\", " +
                "\"source_sha256\": \"" + TokenCodec.Sha256("Artificial") + "\", " +
                "\"source_text\": \"Artificial\", \"translated_text\": \"人工合成\", " +
                "\"game\": { \"original_text\": \"Artificial\" } },\n" +
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
            Assert(store.Count == 9 && store.TryGet("ui:test", out RuntimeTranslationEntry entry) &&
                   entry.TranslatedText == "开始", "运行时 JSON 加载失败");
            Assert(store.UiTemplates.Count() == 1 &&
                   store.FindUnambiguousAchievement("Hello World!")?.TranslatedText == "你好，世界！",
                "动态模板或成就显示译文索引失败");
            Assert(store.FindUnambiguousDisplayValue("Hydrogen")?.TranslatedText == "氢" &&
                   store.UiFragments.Count() == 2, "动态显示值或富文本片段索引失败");
            Assert(store.FindUnambiguousDisplayValue("SHAPES")?.TranslatedText == "形状",
                "总结界面的全大写谜题组标题必须命中显示值译文");
            Assert(store.FindUnambiguousDisplayValue("Artificial")?.TranslatedText == "人工合成",
                "周期表的人造元素丰度必须命中动态显示值译文");
            var displayLocalizer = new UiLocalizer(store, config, null, null, log);
            Assert(displayLocalizer.TranslateCompositeValues(
                       "已完成的谜题组：\n1. SHAPES", translateDisplayValues: true) ==
                   "已完成的谜题组：\n1. 形状",
                "总结界面的复合文本必须翻译大小写不同的谜题组标题");
            Assert(displayLocalizer.TranslateCompositeValues(
                       "3 HYDROGEN-3 / 12.200000 YEARS",
                       translateDisplayValues: true) ==
                   "3 氢-3 / 12.2年",
                "周期表必须翻译元素同位素名称和半衰期单位，不能残留 HYDROGEN-3 或 YEARS");
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
            Assert(displayLocalizer.TranslateRuntimeSentinels("每个 SIGNAL_-43 都包含信号") ==
                   "每个 信号-43 都包含信号",
                "对话中的未命名 SIGNAL_数字占位词必须翻译为信号数字");
            Assert(displayLocalizer.TranslateRuntimeSentinels("SIGNAL_17 / MY_SIGNAL_18") ==
                   "信号17 / MY_SIGNAL_18",
                "正数未命名信号也必须翻译，但不得替换较长标识符内部的 SIGNAL");
            Assert(displayLocalizer.TranslateRuntimeSentinels("UNDEFINED _UNDEF") ==
                   "UNDEFINED _UNDEF",
                "不得全局替换说明文字或玩家输入的普通 UNDEF 字样");
            config.DisplayMode = DisplayMode.OriginalOnly;
            Assert(displayLocalizer.TranslateRuntimeSentinels("@-2_未定义") == "@-2_UNDEF",
                "切回原文时必须恢复游戏原始的未定义后缀");
            Assert(displayLocalizer.TranslateRuntimeSentinels("每个 信号-43 都包含信号") ==
                   "每个 SIGNAL_-43 都包含信号",
                "切回原文时必须恢复未命名信号的游戏占位词");

            TranslationStore fullStore = TranslationStore.Load(
                Path.Combine(projectRoot, "build", "package", "DeepSpaceChinese", "Translations"),
                log);
            config.DisplayMode = DisplayMode.TranslationOnly;
            var fullFrameCatalog = new DialogueFrameCatalog();
            var fullDialogueLocalizer = new DialogueLocalizer(fullStore, config,
                fullFrameCatalog, log);
            var fullUiLocalizer = new UiLocalizer(fullStore, config, fullDialogueLocalizer,
                fullFrameCatalog, log);
            Assert(fullUiLocalizer.LocalizeDialogueSpeakerName("Pilot",
                       DisplayMode.TranslationOnly) == "驾驶员" &&
                   fullUiLocalizer.LocalizeDialogueSpeakerName("Co-Pilot",
                       DisplayMode.TranslationOnly) == "副驾驶员" &&
                   fullUiLocalizer.LocalizeDialogueSpeakerName("副驾驶员",
                       DisplayMode.OriginalOnly) == "Co-Pilot",
                "结局场景的 Pilot/Co-Pilot 必须复用翻译表中的角色资料译名，并能随 F8 恢复原文");
            Assert(fullStore.TryGet("ui-template:universal-abundance",
                       out RuntimeTranslationEntry abundanceTemplate) &&
                   abundanceTemplate.GameBool("translate_display_values") &&
                   fullUiLocalizer.ApplyTemplateDisplayValues(abundanceTemplate,
                       "宇宙丰度：Artificial") == "宇宙丰度：人工合成",
                "周期表宇宙丰度模板必须继续翻译动态值 Artificial，不能只翻译标签");
            Type progressLogGroupLocalizationType =
                typeof(DeepSpaceChinesePlugin).Assembly.GetType(
                    "DeepSpaceChinese.ProgressLogPuzzleGroupLocalization");
            MethodInfo localizeCompletedGroups = progressLogGroupLocalizationType?.GetMethod(
                "LocalizeForTests", BindingFlags.Static | BindingFlags.NonPublic);
            string localizedCompletedGroups = localizeCompletedGroups == null
                ? null
                : (string)localizeCompletedGroups.Invoke(null, new object[]
                {
                    fullUiLocalizer,
                    "已完成的谜题组：\n1.陨石信息, 2.ALTERNATIVE",
                    new[] { "陨石信息", "ALTERNATIVE" }
                });
            Assert(localizedCompletedGroups ==
                       "已完成的谜题组：\n1.陨石信息, 2.可选项",
                "每周结算拼接谜题组名称时必须逐项查询显示值译文，不能残留 ALTERNATIVE；实际=" +
                (localizedCompletedGroups ?? "<null>"));
        Assert(fullUiLocalizer.TranslateCompositeValues(
                   "已完成的谜题组：\n1. HELLO WORLD!",
                   translateDisplayValues: true) ==
               "已完成的谜题组：\n1. Hello World",
               "较长的完整谜题组名命中后不得再被较短显示值二次翻译成“你好 World”");
        Assert(fullUiLocalizer.TranslateCompositeValues(
                   "已完成的谜题组：\n1. HELLO WORLD",
                   translateDisplayValues: true) ==
               "已完成的谜题组：\n1. Hello World",
               "结算页使用不带末尾标点的对象名时也必须完整匹配 Hello World");
        Assert(fullUiLocalizer.TranslateDisplayValueLiteral("HELLO WORLD") ==
               "Hello World",
               "周结算页的单个谜题组名本字路径也必须应用无标点别名");
            foreach (KeyValuePair<string, RuntimeTranslationEntry> displayValue in
                     fullStore.DisplayValues)
            {
                Assert(fullUiLocalizer.ApplyDisplayValues(displayValue.Key) ==
                       displayValue.Value.TranslatedText,
                    "动态显示值不得被其他短显示值二次翻译：" + displayValue.Key);
            }
            config.DisplayMode = DisplayMode.OriginalOnly;
            Assert((string)localizeCompletedGroups.Invoke(null, new object[]
                   {
                       fullUiLocalizer,
                       "Groups Completed:\n1.Meteor Info, 2.ALTERNATIVE",
                       new[] { "Meteor Info", "ALTERNATIVE" }
                   }) == "Groups Completed:\n1.Meteor Info, 2.ALTERNATIVE",
                "切换纯原文模式后，每周结算的谜题组标题必须恢复英文");
            config.DisplayMode = DisplayMode.TranslationOnly;
            Type progressLogGroupPatchType = typeof(DeepSpaceChinesePlugin).Assembly.GetType(
                "DeepSpaceChinese.ProgressLogBuildTransmissionGroupStringPatch");
            Assert(progressLogGroupPatchType?.GetMethod("Postfix",
                       BindingFlags.Static | BindingFlags.NonPublic) != null,
                "ProgressLog.BuildTransmissionGroupString 必须接入谜题组标题本地化补丁");
            var scaledJournals = new[]
            {
                ("dialogue:46/frame:2", 9, "85%"),
                ("dialogue:55/frame:3", 12, "85%"),
                ("dialogue:58/frame:2", 9, "80%"),
                ("dialogue:63/frame:3", 9, "90%"),
                ("dialogue:69/frame:2", 12, "75%"),
                ("dialogue:70/frame:2", 9, "85%")
            };
            foreach ((string key, int partCount, string scale) in scaledJournals)
            {
                Assert(fullStore.TryGet(key, out RuntimeTranslationEntry journal) &&
                       TokenCodec.TrySplitFrameTranslation(journal.TranslatedText, partCount,
                           string.Empty, out string[] journalParts) &&
                       journalParts.All(part =>
                           part.StartsWith($"<size={scale}>", StringComparison.Ordinal) &&
                           part.EndsWith("</size>", StringComparison.Ordinal)),
                    $"超长个人日志 {key} 的每个 PART 都必须独立闭合 {scale} 字号标签");
            }
            Assert(fullStore.TryGet(
                   "system:ControlRoom:Hypotheses Log:component:1:field:viewInDict_s",
                   out RuntimeTranslationEntry hypothesesInstruction) &&
                   hypothesesInstruction.TranslatedText ==
                   "<size=80%>可在词典各条目中查看假说。\n（词典 → 条目注释 → 假说）</size>" &&
                   fullStore.TryGet(
                       "ui:ControlRoom:Progress Log (Canvas) (start inactive)/Hypotheses Log (start inactive)[4]/View in Dict[1]:component:2",
                       out RuntimeTranslationEntry hypothesesInstructionUi) &&
                   hypothesesInstructionUi.TranslatedText ==
                   "<size=80%>可在词典各条目中查看假说。\n（词典 -> 条目注释 -> 假说）</size>",
                "章节总结的词典假说提示必须显式分为两行并缩小字号，不能依赖英文空格自动换行");
            Assert(fullStore.TryGet(
                       "ui:ControlRoom:Reference Window/WHITEDWARF STAR PAGE[38]/Area[0]/STUFF[1]:component:2",
                       out RuntimeTranslationEntry whiteDwarfReference) &&
                   whiteDwarfReference.TranslatedText.Contains(
                       "恒星残骸。\n\n\n\n\n\n\n\n绝大多数恒星"),
                "白矮星参考页必须补偿中文首段少占的一行，避免图片遮挡后续正文");
            Assert(fullStore.TryGet(
                       "ui:ControlRoom:Reference Window/BLACKHOLE PAGE[40]/Area[0]/STUFF[3]:component:2",
                       out RuntimeTranslationEntry blackholeReference) &&
                   blackholeReference.TranslatedText.Contains(
                       "首次探测到黑洞。\n\n\n\n\n只有质量最大的恒星"),
                "黑洞参考页必须补偿中文第三段少占的一行，避免正文与天文学家注释重叠");
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
                "C:/Users/TestUser/AppData/LocalLow/Applesinmypants/The Message From Deep Space";
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
            Assert(UiLocalizer.SelectStableReferenceSourceForTests(
                       "1 Helisec = ", null, "1 Helisec = 0.8066 seconds", null) ==
                   "1 Helisec = 0.8066 seconds" &&
                   UiLocalizer.SelectStableReferenceSourceForTests(
                       "1 Helisec = ", "1 Helisec = 0.8066 seconds",
                       "1 Helisec = 0.8066 秒", "1 Helisec = 0.8066 秒") ==
                   "1 Helisec = 0.8066 seconds" &&
                   UiLocalizer.SelectStableReferenceSourceForTests(
                       "Static title", null, "Static title plus runtime", null) ==
                   "Static title",
                "单位换算页以等号结尾的静态前缀不得覆盖运行时追加的数值，普通静态参考文本仍必须稳定还原");

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
            Assert(MainMenuButtonLayoutEngine.BelongsToReferenceColumn(
                       -0.72552f, transmissionX) &&
                   MainMenuButtonLayoutEngine.BelongsToReferenceColumn(
                       -0.61553f, transmissionX) &&
                   !MainMenuButtonLayoutEngine.BelongsToReferenceColumn(
                       0.325f, transmissionX),
                "中文主菜单只能统一左侧主列；结局后激活的右侧 Flight Tab 不得移到 Menu/设置按钮上方");
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
            Assert(MainMenuButtonLayoutEngine.ActiveTabSetChanged(
                       new[] { 101, 102, 103 }, new[] { 101, 102, 103, 104 }) &&
                   !MainMenuButtonLayoutEngine.ActiveTabSetChanged(
                       new[] { 101, 102, 103 }, new[] { 103, 102, 101 }) &&
                   MainMenuButtonLayoutEngine.ActiveTabSetChanged(
                       new[] { 101, 102, 103 }, new[] { 101, 103, 104 }),
                "运行中解锁或替换菜单项时必须检测到激活集合变化并触发一次中文图标重排");

            RunConfigEditorLayoutTests(projectRoot);

            Console.WriteLine("Runtime self-test passed: INI, hotkey, JSON, tokens, original/translation display, dialogue layout.");
            return 0;
        }
        catch (Exception ex)
        {
            try
            {
                Console.Error.WriteLine(ex.GetType().FullName + ": " + ex.Message);
                if (ex.InnerException != null)
                {
                    Console.Error.WriteLine("Inner: " + ex.InnerException.GetType().FullName +
                                            ": " + ex.InnerException.Message);
                }
            }
            catch
            {
                Console.Error.WriteLine("Runtime self-test failed with an unprintable exception.");
            }
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
            "汉化补丁配置.exe");
        Assembly editorAssembly = Assembly.LoadFrom(editorAssemblyPath);
        Type formType = editorAssembly.GetType(
            "DeepSpaceChinese.ConfigEditor.ConfigEditorForm", true);
        using var form = (Form)Activator.CreateInstance(formType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null, new object[] { Path.Combine(projectRoot, "patch", "DeepSpaceChinese.ini") },
            null);
        Control[] controls = Descendants(form).ToArray();
        ComboBox fontSource = controls.OfType<ComboBox>().Single(control =>
            control.Items.Count == 4 &&
            control.Items.Cast<object>().Any(item => item?.ToString() == "自动选择（推荐）"));
        string[] expectedFontLabels =
        {
            "自动选择（推荐）", "补丁内置字体", "自定义字体文件", "系统字体",
        };
        Assert(fontSource.Items.Cast<object>().Select(item => item?.ToString()).
                   SequenceEqual(expectedFontLabels),
            "字体选择下拉框必须显示中文含义，不得暴露 Auto/Bundled/File/System 内部键名");
        Assert(!controls.Any(control =>
                control.Text.Contains("忽略英文字母大小写") ||
                control.Text.Contains("VAR 可匹配词典中的 var")),
            "大小写兼容项只能保留在 INI 中，不得出现在配置编辑器里");
        CheckBox punctuationOption = controls.OfType<CheckBox>().Single(control =>
            control.Text.Contains("忽略中英文标点差异"));
        CheckBox puzzleOption = controls.OfType<CheckBox>().Single(control =>
            control.Text.Contains("题目及答案的修正规则"));
        CheckBox layoutOption = controls.OfType<CheckBox>().Single(control =>
            control.Text.Contains("新单词命名") && control.Text.Contains("右下角"));
        CheckBox cheatOption = controls.OfType<CheckBox>().Single(control =>
            control.Text.Contains("↑ ↑ ↓ ↓ ← → ← → B A"));
        Label punctuationHint = controls.OfType<Label>().Single(control =>
            control.Text.Contains("标点兼容范围"));
        Label layoutHint = controls.OfType<Label>().Single(control =>
            control.Text.Contains("保持列表原有顺序和行距"));
        Label puzzleHint = controls.OfType<Label>().Single(control =>
            control.Text.Contains("题面和答案集可单独修正"));
        Label reloadHint = controls.OfType<Label>().Single(control =>
            control.Text.Contains("以上兼容项、界面排布和题目及答案修正规则保存后"));
        Label cheatHint = controls.OfType<Label>().Single(control =>
            control.Text.Contains("只填入答案") && control.Text.Contains("不会自动提交"));

        Assert(!ReferenceEquals(layoutHint, puzzleHint) &&
               !ReferenceEquals(puzzleHint, reloadHint) &&
               !ReferenceEquals(punctuationHint, reloadHint) &&
               !ReferenceEquals(cheatHint, reloadHint),
            "常规页的标点兼容、浮窗排布、题面修正和 F5 说明必须使用独立标签");
        Assert(punctuationHint.Parent == punctuationOption.Parent &&
               punctuationHint.Top >= punctuationOption.Bottom &&
               punctuationHint.Top - punctuationOption.Bottom <= 12 &&
               punctuationHint.Bottom <= layoutOption.Top,
            "标点兼容说明必须紧跟在对应复选框下方");
        Assert(layoutHint.Parent == layoutOption.Parent &&
               layoutHint.Top >= layoutOption.Bottom &&
               layoutHint.Top - layoutOption.Bottom <= 12 &&
               layoutHint.Bottom <= puzzleOption.Top,
            "新单词浮窗列表的排布说明必须紧跟在对应复选框下方");
        Assert(puzzleHint.Parent == puzzleOption.Parent &&
               puzzleHint.Top >= puzzleOption.Bottom &&
               puzzleHint.Top - puzzleOption.Bottom <= 12 &&
               cheatOption.Top >= puzzleHint.Bottom &&
               cheatHint.Top >= cheatOption.Bottom &&
               reloadHint.Top >= cheatHint.Bottom,
            "题面修正和作弊码说明必须紧跟对应复选框，F5 公共说明应位于其后");

        string hiddenCompatibilityIni = Path.Combine(projectRoot, "build", "runtime-selftest",
            "config-editor-hidden-case.ini");
        Directory.CreateDirectory(Path.GetDirectoryName(hiddenCompatibilityIni));
        MethodInfo saveMethod = formType.GetMethod("Save",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert(saveMethod != null, "配置编辑器必须保留可测试的保存入口");

        File.WriteAllText(hiddenCompatibilityIni,
            "[Compatibility]\nCompilerPunctuationInsensitive=true\n");
        using (var defaultForm = (Form)Activator.CreateInstance(formType,
                   BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                   null, new object[] { hiddenCompatibilityIni }, null))
        {
            saveMethod.Invoke(defaultForm, new object[] { false });
        }
        Assert(!File.ReadAllText(hiddenCompatibilityIni).Contains("CompilerCaseInsensitive"),
            "配置编辑器不得向新 INI 自动写入隐藏的大小写兼容项");

        File.WriteAllText(hiddenCompatibilityIni,
            "[Compatibility]\nCompilerCaseInsensitive=false\nCompilerPunctuationInsensitive=true\n");
        using (var legacyForm = (Form)Activator.CreateInstance(formType,
                   BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                   null, new object[] { hiddenCompatibilityIni }, null))
        {
            saveMethod.Invoke(legacyForm, new object[] { false });
        }
        Assert(File.ReadAllText(hiddenCompatibilityIni).Contains("CompilerCaseInsensitive=false"),
            "旧 INI 中显式写入的大小写兼容项必须被原样保留");
    }

    private static void RunKonamiCodeDetectorTests()
    {
        Assert(KonamiAnswerCheatRuntime.IsPuzzleReplyRecipientType(
                   typeof(InputDisplaySelector)) &&
               !KonamiAnswerCheatRuntime.IsPuzzleReplyRecipientType(typeof(SimpleWriter)) &&
               !KonamiAnswerCheatRuntime.IsPuzzleReplyRecipientType(typeof(DictionaryEntry)) &&
               !KonamiAnswerCheatRuntime.IsPuzzleReplyRecipientType(null),
            "作弊码只能绑定实际回复框类型，不能依赖 ConsoleDisplay 的场景层级或误接管其它输入框");

        var detector = new KonamiCodeDetector();
        KeyCode[] sequence =
        {
            KeyCode.UpArrow, KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.DownArrow,
            KeyCode.LeftArrow, KeyCode.RightArrow, KeyCode.LeftArrow, KeyCode.RightArrow,
            KeyCode.B, KeyCode.A,
        };
        for (int i = 0; i < sequence.Length - 1; i++)
            Assert(!detector.Push(sequence[i]), "科乐美序列未输完时不得触发填入答案");
        Assert(detector.Push(sequence[sequence.Length - 1]),
            "完整输入 ↑↑↓↓←→←→BA 后必须触发填入答案");
        Assert(detector.Progress == 0, "成功触发后必须清空序列状态");

        detector.Push(KeyCode.UpArrow);
        detector.Push(KeyCode.DownArrow);
        Assert(detector.Progress == 0, "错误按键必须清空不匹配的序列状态");
        detector.Push(KeyCode.UpArrow);
        detector.Push(KeyCode.UpArrow);
        detector.Push(KeyCode.UpArrow);
        Assert(detector.Progress == 1,
            "输错时若当前键可作为新序列起点，必须立即从该键重新计数");
        detector.Reset();
        Assert(detector.Progress == 0, "显式重置必须清空序列状态");
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

    private static void RunSignalEmbedBoundaryTests()
    {
        DialogueLayoutResult splitAtSignal = DialogueLayoutEngine.Fit(
            new[] { new DialogueLayoutPart(
                new string('甲', 10) + "体积更小的 |-90。", 0f, false, 0f) },
            5f, 1.5f, ApproximateWidth);
        Assert(splitAtSignal.WasPaginated &&
               splitAtSignal.Parts.Any(part => part.Text.Contains("|-90。")) &&
               splitAtSignal.Parts.All(part =>
                   !part.Text.TrimStart().StartsWith("。", StringComparison.Ordinal)),
            "信号后的中文句号必须与信号留在同一页，不能独占下一页");
        Assert(SignalEmbedRuntime.RequiresSafePath("体积更小的 |-90"),
            "分页后恰好位于字符串末尾的两位负信号编号必须走边界安全路径");
        Assert(SignalEmbedRuntime.RequiresSafePath("|-4") &&
               SignalEmbedRuntime.RequiresSafePath("|17") &&
               SignalEmbedRuntime.RequiresSafePath("|-123"),
            "末尾信号的正负号及一至三位编号都必须受保护");
        Assert(SignalEmbedRuntime.RequiresSafePath("|-90。") &&
               SignalEmbedRuntime.RequiresSafePath("|-90 后续") &&
               SignalEmbedRuntime.RequiresSafePath("|-27 和 |-28，|-30 和 |-31。"),
            "本地化或分页后的所有信号标记都必须走安全解析，不能再把密集信号送给原游戏的脆弱解析器");

        string replaced = SignalEmbedRuntime.Replace("更小的 |-90", key =>
            key == -90 ? "恒星" : null);
        Assert(replaced == "更小的 <u>恒星</u>",
            "边界安全替换必须保留原游戏的词典下划线格式");
        Assert(SignalEmbedRuntime.Replace("|-4", _ => null) ==
               "<u>SIGNAL_-4</u>",
            "未命名的末尾信号必须保留原游戏 SIGNAL_编号占位形式");
        Assert(SignalEmbedRuntime.Replace("|-4 和 |17", key => key == -4 ? "甲" : "乙") ==
               "<u>甲</u> 和 <u>乙</u>",
            "安全路径必须能处理同一段中的多个信号，而不只修最后一个");
        Assert(SignalEmbedRuntime.NormalizeOutput("更小的 <u>恒星</u>。") ==
                   "更小的<u>恒星</u>。" &&
               SignalEmbedRuntime.NormalizeOutput("a <u>star</u>.") ==
                   "a <u>star</u>.",
            "所有信号替换路径都必须去掉中文词条两侧空格，同时保留英文词条空格");
        Assert(SignalEmbedRuntime.NormalizeOutput("the <u>恒星</u>很亮") ==
                   "the <u>恒星</u>很亮" &&
               SignalEmbedRuntime.NormalizeOutput("这颗<u>恒星</u> is bright") ==
                   "这颗<u>恒星</u> is bright" &&
               SignalEmbedRuntime.NormalizeOutput(
                   "the<font=\"DeepSpaceChinese\"> <u>恒星</u> </font>is bright") ==
                   "the<font=\"DeepSpaceChinese\"> <u>恒星</u> </font>is bright",
            "中文词条与英文或数字相邻时必须保留或补上边界空格，且语境判断应跳过富文本标签");

        DialogueLayoutPart[] reportOriginal =
        {
            new("|-45 |-199,", 0f, false, 0f),
            new("huh?", 0f, false, 0f),
            new("Man,", 0f, false, 0f),
            new("Translator's good at what they do.", 0f, false, 0f),
        };
        DialogueLayoutPart[] reportTranslated =
        {
            new("|-45 |-199，", 0f, false, 0f),
            new("是吗？", 0f, false, 0f),
            new("天啊，", 0f, false, 0f),
            new("译者真是干这行的好手。", 0f, false, 0f),
        };
        string ResolveReportSignals(string raw) => SignalEmbedRuntime.Replace(raw, key =>
            key == -45 ? "恒星" : key == -199 ? "太阳" : null);
        DialogueTextMap reportMap = DialogueTextMap.Create(reportOriginal,
            reportTranslated, string.Empty, ResolveReportSignals);
        const string rawReportAfterClick = "|-45 |-199，是吗？天啊，译者真是干这行的好手。";
        Assert(reportMap.TryMap(rawReportAfterClick, DisplayMode.TranslationOnly,
                   out string restoredReport, out _, out _) &&
               restoredReport == ResolveReportSignals(rawReportAfterClick),
            "报告页再次点击后即使游戏写回原始 |编号，也必须重新展开词语，不能回退为裸标记");

        const int replayInstanceId = 1145005;
        DialogueReplayRuntime.Register(replayInstanceId);
        Assert(DialogueReplayRuntime.IsReplay(replayInstanceId),
            "F6 临时对白必须在播放期间登记为不写日志的重播对象");
        DialogueReplayRuntime.Unregister(replayInstanceId);
        Assert(!DialogueReplayRuntime.IsReplay(replayInstanceId),
            "F6 临时对白结束后必须清除重播登记，不能影响正常对白");
    }

    private static void RunTmpUnderlineMeshCleanupTests()
    {
        Assert(TmpUnderlineMeshCleanup.RequiresImmediateEmptyMesh(
                   "<font=\"DeepSpaceChinese\"><u>恒星</u></font>", string.Empty),
            "带下划线的词典词语被清空时必须立即重建空网格，不能把下划线残留到周结算页");
        Assert(TmpUnderlineMeshCleanup.RequiresImmediateEmptyMesh(
                   "<u>恒星</u>", "周数：四十五") &&
               !TmpUnderlineMeshCleanup.RequiresImmediateEmptyMesh(
                   "普通对白", string.Empty) &&
               !TmpUnderlineMeshCleanup.RequiresImmediateEmptyMesh(
                   "<u>恒星</u>", "<u>恒星</u>"),
            "离开带下划线的词典文本时必须立即重建网格；普通文本或相同文本不得额外刷新");
        Type weeklyCleanupType = typeof(DeepSpaceChinesePlugin).Assembly.GetType(
            "DeepSpaceChinese.WeeklyReportSubtitleCleanup");
        Type weeklyPatchType = typeof(DeepSpaceChinesePlugin).Assembly.GetType(
            "DeepSpaceChinese.ProgressLogStartSubtitleCleanupPatch");
        Assert(weeklyCleanupType?.GetMethod("Clear",
                   BindingFlags.Static | BindingFlags.NonPublic) != null &&
               weeklyPatchType?.GetMethod("Prefix",
                   BindingFlags.Static | BindingFlags.NonPublic) != null,
            "周会结束进入周结算时必须显式清除对白字幕网格，不能等待下一次下划线文本覆盖残影");
    }

    private static void RunLiveDialogueSwitchTests(string projectRoot)
    {
        string journalPreviewSource = File.ReadAllText(Path.Combine(projectRoot,
            "src", "DeepSpaceChinese", "JournalPreviewRuntime.cs"));
        Assert(!journalPreviewSource.Contains("GUI.Button("),
            "F6 日志预览输入面板不得依赖会被游戏拦截的鼠标按钮，只能由 Enter/Esc 操作");
        Assert(journalPreviewSource.Contains("continueButton.SetActive(true)") &&
               !journalPreviewSource.Contains("continueButton.SetActive(false)"),
            "F6 日志预览必须显示实际日志页底部的 Continue 提示，才能检查正文是否与其重叠");
        Assert(journalPreviewSource.Contains("haveUnlockedLabel.richText = true") &&
               journalPreviewSource.Contains("viewInDictLabel.richText = true") &&
               journalPreviewSource.Contains("viewInDictLabel.overflowMode = TextOverflowModes.Overflow"),
            "F6 假说页预览必须先开启两个说明标签的 TMP 富文本，不能把 size/font 标签显示出来");
        int previewRichTextIndex = journalPreviewSource.IndexOf(
            "hypotheses.viewInDictLabel.richText = true", StringComparison.Ordinal);
        int previewTextIndex = journalPreviewSource.IndexOf(
            "hypotheses.viewInDictLabel.text =", StringComparison.Ordinal);
        Assert(previewRichTextIndex >= 0 && previewTextIndex >= 0 &&
               previewRichTextIndex < previewTextIndex &&
               journalPreviewSource.Contains(
                   "hypotheses.viewInDictLabel.maxVisibleLines = int.MaxValue"),
            "F6 假说页必须在写入带标签的译文前开启富文本，并解除可见行数限制，不能显示 size 标签或吞掉第二行");
        string pluginSource = File.ReadAllText(Path.Combine(projectRoot,
            "src", "DeepSpaceChinese", "DeepSpaceChinesePlugin.cs"));
        string logTitleRuntimeSource = File.ReadAllText(Path.Combine(projectRoot,
            "src", "DeepSpaceChinese", "LogTitleRuntime.cs"));
        Assert(pluginSource.Contains(
                   "ApplyProgressLogSpeakerColor(textBox, frame.speaker)") &&
               journalPreviewSource.Contains(
                   "_plugin.ApplyProgressLogSpeakerColor(title, pair.Original.speaker)"),
            "角色手记正文开始逐字显示及 F6 预览时，最上方标题必须同步使用该角色颜色，不能一直保留预制体白色");
        Assert(pluginSource.Contains("ApplyHypothesesTextLayout(component)") &&
               pluginSource.Contains("component.overflowMode = TextOverflowModes.Overflow") &&
               pluginSource.Contains("component.maxVisibleLines = int.MaxValue") &&
               pluginSource.Contains("DictionaryHypothesesLogRoutinePatch") &&
               pluginSource.Contains("ApplyHypothesesTextLayout(__instance)"),
            "正常假说页与 F6 预览都必须允许底部说明换行溢出，不能把第二行截成省略号");
        Assert(logTitleRuntimeSource.Contains(
                   "AccessTools.Field(typeof(LogWindow), \"dialogueTextDump\")") &&
               logTitleRuntimeSource.Contains("body.overflowMode = TextOverflowModes.Overflow") &&
               logTitleRuntimeSource.Contains("body.maxVisibleLines = int.MaxValue") &&
               logTitleRuntimeSource.Contains("body.maxVisibleCharacters = int.MaxValue") &&
               logTitleRuntimeSource.Contains("body.maxVisibleWords = int.MaxValue") &&
               logTitleRuntimeSource.Contains("ApplyOpenBodyLayout(window)"),
            "日志详情正文必须解除 TMP 的可见字符、词语和行数上限，长篇中文日志不能在尾部截断");
        LogWindowScrollMetrics longLogMetrics =
            LogWindowBodyLayoutRuntime.CalculateForTests(80, 5, 12, 0.02f);
        Assert(Math.Abs(longLogMetrics.WorldHeight - 1.7f) < 0.0001f &&
               Math.Abs(longLogMetrics.ScreenHeight - 0.24f) < 0.0001f &&
               Math.Abs(longLogMetrics.RelativeHeight - 85f / 12f) < 0.0001f &&
               logTitleRuntimeSource.Contains("DisplayScrollConfigureRoutine") &&
               logTitleRuntimeSource.Contains("body.ForceMeshUpdate") &&
               logTitleRuntimeSource.Contains("body.preferredHeight") &&
               logTitleRuntimeSource.Contains("area.Configure(scroll, metrics.WorldHeight"),
            "日志详情必须等中文字体与换行稳定后，按实际行数重算滚动区域；长日志末尾必须可以滚到");
        LogWindowScrollMetrics tallerRenderedLogMetrics =
            LogWindowBodyLayoutRuntime.CalculateForTests(40, 5, 12, 0.02f, 1.1f);
        Assert(Math.Abs(tallerRenderedLogMetrics.WorldHeight - 1.2f) < 0.0001f &&
               Math.Abs(tallerRenderedLogMetrics.RelativeHeight - 5f) < 0.0001f,
            "日志中文字体的实际排版高度大于行数估算时，滚动范围必须采用实际高度并保留底部余量");
        Assert(JournalPreviewPromptInput.Resolve(true, KeyCode.Return) ==
                   JournalPreviewPromptAction.Submit &&
               JournalPreviewPromptInput.Resolve(true, KeyCode.KeypadEnter) ==
                   JournalPreviewPromptAction.Submit &&
               JournalPreviewPromptInput.Resolve(true, KeyCode.Escape) ==
                   JournalPreviewPromptAction.Cancel &&
               JournalPreviewPromptInput.Resolve(false, KeyCode.Return) ==
                   JournalPreviewPromptAction.None,
            "F6 日志预览必须只在 KeyDown 时由 Enter 提交，并由 Esc 取消");
        Assert(JournalPreviewId.TryNormalize(" dialogue:55/frame:3 ",
                   out string previewId) && previewId == "dialogue:55/frame:3" &&
               JournalPreviewId.TryNormalize(" PLAY:1145/FRAME:5 ",
                   out string replayFrameId) && replayFrameId == "play:1145/frame:5" &&
               JournalPreviewId.TryNormalize(" play:1145 ",
                   out string replayChunkId) && replayChunkId == "play:1145" &&
               JournalPreviewId.TryNormalize(" HYPOTHESES ",
                   out string hypothesesPreviewId) && hypothesesPreviewId == "hypotheses" &&
               JournalPreviewId.TryNormalize(" hypotheses:3 ",
                   out string historicalHypothesesId) &&
               historicalHypothesesId == "hypotheses:3" &&
               JournalPreviewId.TryNormalize(" CREDITS ",
                   out string creditsPreviewId) && creditsPreviewId == "credits" &&
               JournalPreviewId.TryNormalize(" CONTACT ",
                   out string contactPreviewId) && contactPreviewId == "contact" &&
               JournalPreviewId.TryNormalize(" REPORT:1 ",
                   out string reportPreviewId) && reportPreviewId == "report:1" &&
               !JournalPreviewId.TryNormalize("55/3", out _),
            "F6 测试入口必须接受日志预览、真实对白帧/整段重播、周报、假说页、最终联络输入及结局滚动字幕命令");
        Assert(journalPreviewSource.Contains("ShowProgressReport(stableKey)") &&
               journalPreviewSource.Contains("BuildTransmissionGroupString") &&
               journalPreviewSource.Contains("LocalizeCompletedPuzzleGroups"),
            "F6 report 命令必须复用原生周报数据，并通过本地化管线处理谜题组标题");
        Assert(journalPreviewSource.Contains("ShowContact()") &&
               journalPreviewSource.Contains("ContactRoutine") &&
               journalPreviewSource.Contains("StopAllCoroutines()"),
            "F6 contact 命令必须停止结局场景原协程，并从原生 ContactRoutine 打开最终联络输入界面");
        Assert(journalPreviewSource.Contains("ShowCredits()") &&
               journalPreviewSource.Contains("credits.PlayCredits()"),
            "F6 credits 命令必须调用游戏原生 CreditsSequence.PlayCredits，不能只生成静态假预览");
        Assert(Math.Abs(CreditsScrollRuntime.CalculateAdditionalDistanceForTests(
                           100f, 132f) - 32f) < 0.0001f &&
               Math.Abs(CreditsScrollRuntime.CalculateAdditionalDistanceForTests(
                           100f, 80f)) < 0.0001f &&
               journalPreviewSource.Contains("_plugin.LocalizeCredits(credits)") &&
               journalPreviewSource.Contains("CreditsScrollRuntime.Prepare(credits)"),
            "结局译文变高时必须把增加的实际高度补进滚动终点，且 F6 与正常结局共用同一处理");
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
        DialogueTextMap refreshedMap = DialogueTextMap.Create(original,
            new[]
            {
                new DialogueLayoutPart("您好，", 0f, false, 0f),
                new DialogueLayoutPart("新世界。", 0f, false, 0f),
                new DialogueLayoutPart("新的一页。", 0f, true, 0f),
            }, string.Empty, value => value);
        Assert(DialogueTextMap.TryRetarget(map, refreshedMap, "你好，世界。",
                   DisplayMode.TranslationOnly, out string refreshed) &&
               refreshed == "您好，新世界。",
            "F5 重载后必须先把屏幕上的旧译文映射回原文，再映射为新译文");
        refreshedMap.ImportRetargetAliases(map);
        Assert(refreshedMap.TryMap("你好，世界。", DisplayMode.TranslationOnly,
                   out string continuedAfterReload, out _, out _) &&
               continuedAfterReload == "您好，新世界。",
            "F5 后仍在运行的逐字协程会继续提交旧译文，刷新映射必须保留旧状态别名");
        Assert(DialogueTextMap.ScaleVisibleCharacters(chineseLength, chineseLength,
                   englishLength) == englishLength &&
               DialogueTextMap.ScaleVisibleCharacters(3, 6, 12) == 6,
            "切换语言时必须同比换算逐字显示进度，不能截断更长的英文");
        Assert(DialogueTextMap.VisibleLength("<size=85%>你好，世界。</size>") == 6,
            "逐字进度换算必须忽略 TMP 富文本标签，否则 F5/F8 会把字号标签算成可见字符");
        Assert(DialogueTypingLengthCompatibility.VisibleLengthForTyping(
                   "驾驶员：<u>恒星</u>。") == 7,
            "场景对白的完成条件必须按可见字数计算，不能把字体/下划线标签当成尚未打完的字符");
        Assert(DialogueTypingLengthCompatibility.PatchedCoroutineCountForTests == 3 &&
               DialogueTypingLengthCompatibility.ResolvedTargetCountForTests() == 3,
            "主对白、场景对白和个人日志三条逐字协程都必须统一按可见字数判断完成");
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
