using System;
using System.Collections;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepSpaceChinese;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class DeepSpaceChinesePlugin : BaseUnityPlugin
{
    public const string PluginGuid = "hepta.deepspace.chinese";
    public const string PluginName = "The Message from Deep Space Chinese Patch";
    public const string PluginVersion = "0.1.59";

    internal static DeepSpaceChinesePlugin Instance { get; private set; }
    internal ManualLogSource PluginLog => Logger;
    internal bool CompilerCaseInsensitiveEnabled =>
        _patchConfig?.Enabled == true && _patchConfig.CompilerCaseInsensitive;

    private PatchConfig _patchConfig;
    private DialogueFrameCatalog _frameCatalog;
    private DialogueLocalizer _dialogue;
    private LogTitleRuntime _logTitles;
    private UiLocalizer _ui;
    private DialogueLiveTextRuntime _liveDialogueText;
    private FontFallback _font;
    private CharacterFontRegistry _characterFonts;
    private DialogueLayoutRuntime _dialogueLayout;
    private PlayerNameRuntime _playerName;
    private PuzzleFixRuntime _puzzleFixes;
    private Harmony _harmony;
    private string _translationDirectory;
    private string _configPath;

    private void Awake()
    {
        Instance = this;
        string gameRoot = Paths.GameRootPath;
        string contentRoot = System.IO.Path.Combine(gameRoot, "DeepSpaceChinese");
        _configPath = System.IO.Path.Combine(gameRoot, "DeepSpaceChinese.ini");
        _patchConfig = PatchConfig.Load(_configPath, Logger);
        _translationDirectory = System.IO.Path.Combine(contentRoot, "Translations");
        string fixDirectory = System.IO.Path.Combine(contentRoot, "Fix");
        TranslationStore store = TranslationStore.Load(_translationDirectory, Logger);
        _frameCatalog = new DialogueFrameCatalog();
        _dialogue = new DialogueLocalizer(store, _patchConfig, _frameCatalog, Logger);
        _font = new FontFallback(_patchConfig, contentRoot, Logger);
        _characterFonts = new CharacterFontRegistry();
        _logTitles = new LogTitleRuntime(_dialogue, _font, _patchConfig, Logger);
        _ui = new UiLocalizer(store, _patchConfig, _dialogue, _frameCatalog, Logger);
        _liveDialogueText = new DialogueLiveTextRuntime(_patchConfig, Logger);
        _dialogueLayout = new DialogueLayoutRuntime(_patchConfig, Logger);
        _playerName = new PlayerNameRuntime(_patchConfig, Logger);
        _puzzleFixes = new PuzzleFixRuntime(_patchConfig, fixDirectory, Logger);
        _puzzleFixes.ReloadRules();

        _harmony = new Harmony(PluginGuid);
        _harmony.PatchAll(typeof(DeepSpaceChinesePlugin).Assembly);
        SceneManager.sceneLoaded += OnSceneLoaded;
        RegisterCharacterFonts();

        if (_patchConfig.Enabled)
        {
            _font.EnsureLoaded();
            StartCoroutine(ReapplyNextFrame());
        }
        Logger.LogInfo($"汉化补丁 {PluginVersion} 已加载；显示模式 {_patchConfig.DisplayMode}；" +
                       $"切换键 {_patchConfig.ToggleModeHotkey}；重载键 {_patchConfig.ReloadTranslationsHotkey}。");
    }

    private void Update()
    {
        if (!_patchConfig.Enabled)
            return;
        try
        {
            if (!_patchConfig.ToggleModeHotkey.Equals(BepInEx.Configuration.KeyboardShortcut.Empty) &&
                _patchConfig.ToggleModeHotkey.IsDown())
                ToggleDisplayMode();
            if (!_patchConfig.ReloadTranslationsHotkey.Equals(BepInEx.Configuration.KeyboardShortcut.Empty) &&
                _patchConfig.ReloadTranslationsHotkey.IsDown())
                ReloadTranslations();
        }
        catch (Exception ex)
        {
            Logger.LogError($"处理汉化快捷键时发生错误：\n{ex}");
        }
    }

    private void LateUpdate()
    {
        if (_patchConfig?.Enabled != true)
            return;
        try
        {
            _playerName?.UpdateImeCursorPosition();
        }
        catch (Exception ex)
        {
            Logger.LogError($"更新输入法候选窗位置失败：\n{ex}");
        }
    }

    private void ToggleDisplayMode()
    {
        _patchConfig.DisplayMode = _patchConfig.DisplayMode == DisplayMode.TranslationOnly
            ? DisplayMode.OriginalOnly
            : DisplayMode.TranslationOnly;
        _font.EnsureLoaded();
        RegisterCharacterFonts();
        ApplyProgressLogTitleFonts();
        _dialogue.ReapplyAll();
        _liveDialogueText.RefreshAll();
        _ui.ReapplyAll();
        _logTitles.RefreshAll();
        _playerName.ApplyAll();
        string displayName = _patchConfig.DisplayMode == DisplayMode.TranslationOnly ? "仅译文" : "仅原文";
        Logger.LogMessage($"汉化显示模式已切换为：{displayName}");
    }

    private void ReloadTranslations()
    {
        RegisterCharacterFonts();
        _patchConfig.ReloadFontSettings(_configPath, Logger);
        _patchConfig.ReloadDialogueColorSettings(_configPath, Logger);
        _patchConfig.ReloadCompatibilitySettings(_configPath, Logger);
        _puzzleFixes.ReloadRules();
        _dialogueLayout.ReapplySpeakerColors();
        bool fontReady = _font.ReloadIfChanged(out bool fontReloaded);
        ApplyProgressLogTitleFonts();
        TranslationStore replacement = TranslationStore.Load(_translationDirectory, Logger);
        bool translationsReloaded = replacement.LoadErrors == 0 &&
                                    replacement.FilesLoaded == 4 && replacement.Count > 0;
        if (!translationsReloaded)
        {
            Logger.LogError($"译文重载失败，继续使用当前译文：成功文件 {replacement.FilesLoaded}/4，" +
                            $"错误 {replacement.LoadErrors}，有效条目 {replacement.Count}。");
        }
        else
        {
            _dialogue.ReplaceStore(replacement);
            _ui.ReplaceStore(replacement);
        }
        _dialogue.ReapplyAll();
        _liveDialogueText.RefreshAll();
        _ui.ReapplyAll();
        _logTitles.RefreshAll();
        _playerName.ApplyAll();
        ApplyPuzzleFixes(PuzzleManager.Instance, refreshCurrentDisplay: true);
        if (translationsReloaded)
            Logger.LogMessage($"译文已重新载入并应用：{replacement.Count} 条；" +
                              $"中文字体{(fontReloaded ? "已刷新" : fontReady ? "未变化" : "保持原字体")}。");
        else if (fontReloaded)
            Logger.LogMessage("译文文件未替换，但中文字体已刷新并重新应用。");
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!_patchConfig.Enabled)
            return;
        _font.EnsureLoaded();
        StartCoroutine(ReapplyNextFrame());
    }

    private IEnumerator ReapplyNextFrame()
    {
        yield return null;
        yield return null;
        _font.EnsureLoaded();
        RegisterCharacterFonts();
        ApplyProgressLogTitleFonts();
        _dialogue.ReapplyAll();
        _liveDialogueText.RefreshAll();
        _ui.ReapplyAll();
        _logTitles.RefreshAll();
        _playerName.ApplyAll();
        ApplyPuzzleFixes(PuzzleManager.Instance);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        _puzzleFixes?.RestoreAll();
        _harmony?.UnpatchSelf();
        if (ReferenceEquals(Instance, this))
            Instance = null;
    }

    internal void ApplyPuzzleFixes(PuzzleManager manager, bool refreshCurrentDisplay = false)
    {
        try
        {
            bool currentPuzzleChanged = _puzzleFixes?.ApplyAll(manager) == true;
            if (!refreshCurrentDisplay || !currentPuzzleChanged)
                return;

            Puzzle currentPuzzle = manager?.CurrPuzzle;
            ConsoleDisplay consoleDisplay = ConsoleDisplay.Instance;
            if (currentPuzzle == null || consoleDisplay == null)
            {
                Logger.LogWarning("当前题面已修正，但找不到正在显示题目的控制台，无法立即刷新显示。");
                return;
            }

            consoleDisplay.LoadNewSignal(currentPuzzle.RockOutput);
            Logger.LogInfo($"题面显示已刷新：第 {manager.TotalPuzzleID + 1} 题。");
        }
        catch (Exception ex)
        {
            Logger.LogError($"应用题面修正规则失败，游戏将继续使用当前题面：\n{ex}");
        }
    }

    internal void ApplyDialogueBank(DialogueBank bank)
    {
        if (_patchConfig?.Enabled == true)
        {
            _dialogue.RegisterAndApply(bank);
            _logTitles.RefreshAll();
        }
    }

    internal void ApplyLogEntryTitle(DialogueLogEntry entry, DialogueChunk chunk, LogWindow window)
    {
        if (_patchConfig?.Enabled == true)
            _logTitles?.ApplyEntry(entry, chunk, window);
    }

    internal void ApplyOpenLogTitle(LogWindow window, DialogueChunk chunk)
    {
        if (_patchConfig?.Enabled == true)
            _logTitles?.ApplyOpenTitle(window, chunk);
    }

    internal string TranslateTmpText(TMP_Text component, string proposed)
    {
        string translated;
        bool isTrackedDialogue = false;
        if (_liveDialogueText != null &&
            _liveDialogueText.TryTranslate(component, proposed, out translated))
        {
            isTrackedDialogue = true;
            translated = _ui?.TranslateRuntimeSentinels(translated) ?? translated;
            bool usesCharacterFont = _characterFonts?.Contains(component?.font) == true;
            ApplyInterfaceFont(component, translated);
            return ApplyCharacterPunctuation(component, translated, isTrackedDialogue,
                usesCharacterFont);
        }
        translated = _ui == null ? proposed : _ui.TranslateIncoming(component, proposed);
        translated = _ui?.TranslateRuntimeSentinels(translated) ?? translated;
        bool usesRoleFont = _characterFonts?.Contains(component?.font) == true;
        ApplyInterfaceFont(component, translated);
        return ApplyCharacterPunctuation(component, translated, isTrackedDialogue,
            usesRoleFont);
    }

    private string ApplyCharacterPunctuation(TMP_Text component, string displayText,
        bool isTrackedDialogue, bool usesCharacterFont)
    {
        if (component == null || _font == null || _patchConfig == null ||
            !DialoguePunctuationPolicy.ShouldDecorate(isTrackedDialogue,
                usesCharacterFont, component.richText,
                _patchConfig.DisplayMode, displayText))
            return displayText;
        return DialoguePunctuationFontMarkup.Apply(displayText, _font.RichTextFontName,
            _font.RichTextColorFor(component));
    }

    private void ApplyInterfaceFont(TMP_Text component, string displayText)
    {
        if (component == null || _font == null || _patchConfig == null)
            return;
        string path = UiLocalizer.BuildObjectPath(component.transform);
        bool hasSpecialMaterialOwner =
            component.GetComponentInParent<PopupBox>(true) != null ||
            component.GetComponentInParent<TermLogger>(true) != null ||
            component.GetComponentInParent<IdeaEntry>(true) != null;
        if (InterfaceFontPolicy.ShouldUseDirectFont(path, displayText,
                _patchConfig.DisplayMode, hasSpecialMaterialOwner))
            _font.ApplyDirect(component, true);
        else if (InterfaceFontPolicy.IsDirectFontTarget(path, hasSpecialMaterialOwner))
            _font.ApplyDirect(component, false);
    }

    internal void AdjustDialogueVisibleCharacters(TMP_Text component, ref int value)
    {
        _liveDialogueText?.AdjustMaxVisibleCharacters(component, ref value);
    }

    internal void ApplyPlayerNameUi(NameTranslator namer)
    {
        if (_patchConfig?.Enabled == true)
            _playerName?.Apply(namer);
    }

    internal void ApplyProgressLogInput(ProgressLog progressLog)
    {
        if (_patchConfig?.Enabled == true)
            _playerName?.ApplyProgressLogInput(progressLog);
    }

    internal void ApplySharedInput(InputTextDummy dummy)
    {
        if (_patchConfig?.Enabled == true)
            _playerName?.ApplySharedInput(dummy);
    }

    internal void UpdateImeCursorPosition(TMP_InputField input)
    {
        if (_patchConfig?.Enabled != true)
            return;
        try
        {
            _playerName?.UpdateImeCursorPosition(input);
        }
        catch (Exception ex)
        {
            Logger.LogError($"在 TMP 绘制后更新输入法候选窗位置失败：\n{ex}");
        }
    }

    internal DialogueFrame FitMainDialogue(DialogueManager manager, DialogueFrame frame)
    {
        _characterFonts?.Register(manager);
        if (_dialogueLayout == null)
            return frame;
        if (_frameCatalog != null && _frameCatalog.TryGet(frame, out DialogueFramePair pair))
        {
            DialogueFrame original = _dialogueLayout.FitMain(manager, pair.Original);
            DialogueFrame translated = _dialogueLayout.FitMain(manager, pair.Translated);
            _liveDialogueText?.TrackMain(manager, original, translated);
            return _patchConfig.DisplayMode == DisplayMode.TranslationOnly
                ? translated
                : original;
        }
        return _dialogueLayout.FitMain(manager, frame);
    }

    internal DialogueFrame FitNonLogDialogue(NonLogDialogueManager manager, DialogueFrame frame)
    {
        _characterFonts?.Register(manager);
        if (_dialogueLayout == null)
            return frame;
        if (_frameCatalog != null && _frameCatalog.TryGet(frame, out DialogueFramePair pair))
        {
            DialogueFrame original = _dialogueLayout.FitNonLog(manager, pair.Original);
            DialogueFrame translated = _dialogueLayout.FitNonLog(manager, pair.Translated);
            _liveDialogueText?.TrackNonLog(manager, original, translated);
            return _patchConfig.DisplayMode == DisplayMode.TranslationOnly
                ? translated
                : original;
        }
        return _dialogueLayout.FitNonLog(manager, frame);
    }

    internal DialogueFrame PrepareCharacterTypedDialogue(TMP_Text textBox,
        DialogueFrame frame)
    {
        _characterFonts?.Register(textBox?.font);
        if (_frameCatalog != null && _frameCatalog.TryGet(frame, out DialogueFramePair pair))
        {
            _liveDialogueText?.TrackCharacter(textBox, pair.Original, pair.Translated);
            return _patchConfig.DisplayMode == DisplayMode.TranslationOnly
                ? pair.Translated
                : pair.Original;
        }
        return frame;
    }

    internal string PrepareGenericTypedText(TMP_Text label, string proposed)
    {
        ApplyProgressLogTitleFont(label);
        if (_ui != null && _ui.TryResolveSystemLiteralPair(proposed,
                out string original, out string translated))
        {
            _liveDialogueText?.TrackLiteral(label, original, translated);
            return _patchConfig.DisplayMode == DisplayMode.TranslationOnly
                ? translated
                : original;
        }
        return proposed;
    }

    private void ApplyProgressLogTitleFonts()
    {
        int count = 0;
        foreach (ProgressLog progressLog in Resources.FindObjectsOfTypeAll<ProgressLog>())
        {
            if (progressLog == null)
                continue;
            count += ApplyProgressLogTitleFont(progressLog.aLogTitle) ? 1 : 0;
            count += ApplyProgressLogTitleFont(progressLog.bLogTitle) ? 1 : 0;
            count += ApplyProgressLogTitleFont(progressLog.cLogTitle) ? 1 : 0;
            count += ApplyProgressLogTitleFont(progressLog.dLogTitle) ? 1 : 0;
            count += ApplyProgressLogTitleFont(progressLog.tLogTitle) ? 1 : 0;
        }
        if (count > 0)
            Logger.LogInfo($"个人日志标题字体已应用：{count} 项，模式 {_patchConfig.DisplayMode}。");
    }

    private void RegisterCharacterFonts()
    {
        if (_characterFonts == null)
            return;
        foreach (DialogueManager manager in Resources.FindObjectsOfTypeAll<DialogueManager>())
            _characterFonts.Register(manager);
    }

    private bool ApplyProgressLogTitleFont(TMP_Text label)
    {
        if (label == null || !IsProgressLogTitle(label))
            return false;
        return _font.ApplyDirect(label,
            _patchConfig.DisplayMode == DisplayMode.TranslationOnly);
    }

    private static bool IsProgressLogTitle(TMP_Text label)
    {
        foreach (ProgressLog progressLog in Resources.FindObjectsOfTypeAll<ProgressLog>())
        {
            if (progressLog != null &&
                (ReferenceEquals(label, progressLog.aLogTitle) ||
                 ReferenceEquals(label, progressLog.bLogTitle) ||
                 ReferenceEquals(label, progressLog.cLogTitle) ||
                 ReferenceEquals(label, progressLog.dLogTitle) ||
                 ReferenceEquals(label, progressLog.tLogTitle)))
                return true;
        }
        return false;
    }
}

[HarmonyPatch(typeof(DialogueBank), "SetDataFromLoad")]
internal static class DialogueBankSetDataPatch
{
    [HarmonyPostfix]
    private static void Postfix(DialogueBank __instance)
    {
        try
        {
            DeepSpaceChinesePlugin.Instance?.ApplyDialogueBank(__instance);
        }
        catch (Exception ex)
        {
            DeepSpaceChinesePlugin.Instance?.PluginLog.LogError($"应用对白翻译失败：\n{ex}");
        }
    }
}

[HarmonyPatch(typeof(TMP_Text), "set_text")]
internal static class TmpTextSetterPatch
{
    [HarmonyPrefix]
    private static void Prefix(TMP_Text __instance, ref string value)
    {
        try
        {
            DeepSpaceChinesePlugin plugin = DeepSpaceChinesePlugin.Instance;
            if (plugin != null)
                value = plugin.TranslateTmpText(__instance, value);
        }
        catch (Exception ex)
        {
            DeepSpaceChinesePlugin.Instance?.PluginLog.LogError($"应用 UI 翻译失败：\n{ex}");
        }
    }
}

[HarmonyPatch(typeof(TMP_Text), "set_maxVisibleCharacters")]
internal static class TmpMaxVisibleCharactersPatch
{
    [HarmonyPrefix]
    private static void Prefix(TMP_Text __instance, ref int value)
    {
        try
        {
            DeepSpaceChinesePlugin.Instance?.AdjustDialogueVisibleCharacters(__instance,
                ref value);
        }
        catch (Exception ex)
        {
            DeepSpaceChinesePlugin.Instance?.PluginLog.LogError(
                $"换算对白逐字显示进度失败：\n{ex}");
        }
    }
}

[HarmonyPatch(typeof(DialogueManager), "WriteFrameRoutine")]
internal static class DialogueManagerWriteFramePatch
{
    [HarmonyPrefix]
    private static void Prefix(DialogueManager __instance, ref DialogueFrame df)
    {
        try
        {
            DeepSpaceChinesePlugin plugin = DeepSpaceChinesePlugin.Instance;
            if (plugin != null)
                df = plugin.FitMainDialogue(__instance, df);
        }
        catch (Exception ex)
        {
            DeepSpaceChinesePlugin.Instance?.PluginLog.LogError($"调整主对白显示失败，将使用原始排版：\n{ex}");
        }
    }
}

[HarmonyPatch(typeof(NonLogDialogueManager), "WriteFrameRoutine")]
internal static class NonLogDialogueManagerWriteFramePatch
{
    [HarmonyPrefix]
    private static void Prefix(NonLogDialogueManager __instance, ref DialogueFrame df)
    {
        try
        {
            DeepSpaceChinesePlugin plugin = DeepSpaceChinesePlugin.Instance;
            if (plugin != null)
                df = plugin.FitNonLogDialogue(__instance, df);
        }
        catch (Exception ex)
        {
            DeepSpaceChinesePlugin.Instance?.PluginLog.LogError($"调整场景对白显示失败，将使用原始排版：\n{ex}");
        }
    }
}

[HarmonyPatch(typeof(DialogueManager), "CharacterDialogueTypeRoutine",
    new[] { typeof(Speaker), typeof(TMP_Text), typeof(DialogueFrame), typeof(bool) })]
internal static class DialogueManagerCharacterTypePatch
{
    [HarmonyPrefix]
    private static void Prefix(TMP_Text __1, ref DialogueFrame __2)
    {
        try
        {
            DeepSpaceChinesePlugin plugin = DeepSpaceChinesePlugin.Instance;
            if (plugin != null)
                __2 = plugin.PrepareCharacterTypedDialogue(__1, __2);
        }
        catch (Exception ex)
        {
            DeepSpaceChinesePlugin.Instance?.PluginLog.LogError(
                $"跟踪个人日志正文语言切换失败：\n{ex}");
        }
    }
}

[HarmonyPatch(typeof(DialogueManager), "GenericTypeRoutine",
    new[] { typeof(string), typeof(TMP_Text), typeof(bool) })]
internal static class DialogueManagerGenericTypePatch
{
    [HarmonyPrefix]
    private static void Prefix(ref string __0, TMP_Text __1)
    {
        try
        {
            DeepSpaceChinesePlugin plugin = DeepSpaceChinesePlugin.Instance;
            if (plugin != null)
                __0 = plugin.PrepareGenericTypedText(__1, __0);
        }
        catch (Exception ex)
        {
            DeepSpaceChinesePlugin.Instance?.PluginLog.LogError(
                $"跟踪逐字标题语言切换失败：\n{ex}");
        }
    }
}

[HarmonyPatch(typeof(DialogueManager), "GenericTypeRoutine",
    new[] { typeof(string), typeof(TMP_Text), typeof(bool), typeof(float) })]
internal static class DialogueManagerGenericTimedTypePatch
{
    [HarmonyPrefix]
    private static void Prefix(ref string __0, TMP_Text __1)
    {
        try
        {
            DeepSpaceChinesePlugin plugin = DeepSpaceChinesePlugin.Instance;
            if (plugin != null)
                __0 = plugin.PrepareGenericTypedText(__1, __0);
        }
        catch (Exception ex)
        {
            DeepSpaceChinesePlugin.Instance?.PluginLog.LogError(
                $"跟踪定时逐字标题语言切换失败：\n{ex}");
        }
    }
}
