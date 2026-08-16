using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using BepInEx.Logging;
using TMPro;
using UnityEngine;

namespace DeepSpaceChinese;

internal sealed class UiLocalizer
{
    private const string UndefinedSuffixStableKey = "ui-fragment:undefined-suffix";
    private static readonly Regex UnknownSignalPlaceholderRegex = new(
        @"(?<![A-Za-z0-9_])SIGNAL_(-?\d+)(?![A-Za-z0-9_])",
        RegexOptions.CultureInvariant);
    private static readonly Regex LocalizedUnknownSignalPlaceholderRegex = new(
        @"信号(-?\d+)(?![A-Za-z0-9_])",
        RegexOptions.CultureInvariant);
    private static readonly FieldInfo DictionaryWordLabelOverrideField =
        typeof(DictionaryWordLabel).GetField("overrideWithText",
            BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly MethodInfo DictionaryWordLabelUpdateMethod =
        typeof(DictionaryWordLabel).GetMethod("UpdateTermText",
            BindingFlags.Instance | BindingFlags.NonPublic);
    [ThreadStatic] private static bool _applying;

    private TranslationStore _store;
    private readonly PatchConfig _config;
    private readonly DialogueLocalizer _dialogue;
    private readonly DialogueFrameCatalog _frameCatalog;
    private readonly ManualLogSource _log;
    private readonly Dictionary<int, string> _originalTexts = new();
    private readonly Dictionary<int, string> _lastLocalizedTexts = new();
    private readonly Dictionary<string, string> _originalFields = new(StringComparer.Ordinal);
    private Dictionary<string, List<RuntimeTranslationEntry>> _systemByPrefix;
    private readonly HashSet<string> _reportedMismatches = new(StringComparer.Ordinal);

    public UiLocalizer(TranslationStore store, PatchConfig config, DialogueLocalizer dialogue,
        DialogueFrameCatalog frameCatalog, ManualLogSource log)
    {
        _store = store;
        _config = config;
        _dialogue = dialogue;
        _frameCatalog = frameCatalog;
        _log = log;
        _systemByPrefix = BuildSystemIndex(store);
    }

    public void ReplaceStore(TranslationStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _systemByPrefix = BuildSystemIndex(store);
        _reportedMismatches.Clear();
    }

    internal string OriginalTextForLayout(TMP_Text component)
    {
        if (component == null)
            return string.Empty;
        int id = component.GetInstanceID();
        _originalTexts.TryGetValue(id, out string saved);
        _lastLocalizedTexts.TryGetValue(id, out string lastLocalized);
        if (TryGetStableStaticReferenceOriginal(component, out string stableOriginal))
            return SelectStableReferenceSourceForTests(stableOriginal, saved,
                component.text, lastLocalized);
        if (saved != null)
            return saved;
        string key = BuildUiStableKey(component);
        if (_store.TryGet(key, out RuntimeTranslationEntry entry) && entry.Kind == "ui_text")
            return entry.GameString("original_text", entry.SourceText) ?? string.Empty;
        return component.text ?? string.Empty;
    }

    internal bool HasStableOriginalForLayout(TMP_Text component)
    {
        if (component == null)
            return false;
        return _store.TryGet(BuildUiStableKey(component), out RuntimeTranslationEntry entry) &&
               entry.Kind == "ui_text";
    }

    public bool TryResolveSystemLiteralPair(string current, out string original,
        out string translated)
    {
        original = null;
        translated = null;
        foreach (RuntimeTranslationEntry entry in _store.Entries.Where(value =>
                     value.Kind == "component_string"))
        {
            string candidateOriginal = TokenCodec.RestoreForEntry(
                entry.GameString("original_text", entry.SourceText), entry,
                _dialogue.PlayerFullName(DisplayMode.OriginalOnly));
            string candidateTranslated = TokenCodec.RestoreForEntry(entry.TranslatedText, entry,
                _dialogue.PlayerFullName(DisplayMode.TranslationOnly), true);
            if (current != candidateOriginal && current != candidateTranslated)
                continue;
            if (original == null)
            {
                original = candidateOriginal;
                translated = candidateTranslated;
                continue;
            }
            if (original != candidateOriginal || translated != candidateTranslated)
            {
                original = null;
                translated = null;
                return false;
            }
        }
        return original != null;
    }

    internal string LocalizeDialogueSpeakerName(string current, DisplayMode target)
    {
        if (string.IsNullOrEmpty(current))
            return current;
        string lookup = string.Equals(current, "Co-Pilot",
            StringComparison.OrdinalIgnoreCase) ? "Copilot" : current;
        if (!TryResolveSystemLiteralPair(lookup, out string original,
                out string translated))
            return current;
        if (target == DisplayMode.TranslationOnly)
            return translated;
        return string.Equals(original, "Copilot", StringComparison.OrdinalIgnoreCase)
            ? "Co-Pilot"
            : original;
    }

    private static Dictionary<string, List<RuntimeTranslationEntry>> BuildSystemIndex(
        TranslationStore store) =>
        store.Entries.Where(entry =>
                entry.Kind is "component_string" or "component_dialogue_frame")
            .GroupBy(entry => entry.StableKey.Substring(0, entry.StableKey.LastIndexOf(":field:", StringComparison.Ordinal) + 7))
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.Ordinal);

    public string TranslateIncoming(TMP_Text component, string proposed)
    {
        if (_applying || !_config.Enabled || !_config.TranslateUI || component == null || proposed == null)
            return proposed;
        if (PeriodicTableElementCompatibility.TryResolveSymbolText(
                component, proposed, out string periodicTableSymbol))
            return RememberDisplay(component, proposed, periodicTableSymbol);
        string localized;
        if (CompilerErrorRuntime.TryResolvePreparedSource(proposed,
                out string preparedOriginal, out string preparedTranslation))
            return RememberDisplay(component, preparedOriginal, preparedTranslation);
        if (CompilerErrorRuntime.IsCompilerError(proposed))
        {
            localized = CompilerErrorRuntime.Format(proposed, _config.DisplayMode);
            return RememberDisplay(component, proposed, localized);
        }
        if (ShouldPreservePlayerText(component))
            return proposed;
        RuntimeTranslationEntry entry = FindUiEntry(component, proposed);
        if (entry == null)
        {
            localized = TranslateDynamic(component, proposed);
            return RememberDisplay(component, proposed, localized);
        }
        string original = entry.GameString("original_text");
        string source = TokenCodec.ProtectForEntry(original, entry);
        if (TokenCodec.Sha256(source) != entry.SourceSha256)
        {
            ReportMismatch(entry.StableKey);
            return RememberDisplay(component, proposed, proposed);
        }
        localized = TokenCodec.FormatDisplayForEntry(entry.TranslatedText, original, entry,
            _config, _dialogue.PlayerFullName());
        return RememberDisplay(component, original, localized);
    }

    public void ReapplyAll()
    {
        if (!_config.Enabled)
            return;
        ReapplyTexts(Resources.FindObjectsOfTypeAll<TMP_Text>(), applySystemStrings: true);
    }

    internal void ReapplyUnder(Transform root)
    {
        if (!_config.Enabled || root == null)
            return;
        ReapplyTexts(root.GetComponentsInChildren<TMP_Text>(true), applySystemStrings: false);
    }

    private void ReapplyTexts(IEnumerable<TMP_Text> texts, bool applySystemStrings)
    {
        TMP_Text[] textList = texts.Where(text => text != null).ToArray();
        bool wasApplying = _applying;
        _applying = true;
        try
        {
            int uiCount = 0;
            foreach (TMP_Text text in textList)
            {
                if (text == null)
                    continue;
                int id = text.GetInstanceID();
                _originalTexts.TryGetValue(id, out string saved);
                _lastLocalizedTexts.TryGetValue(id, out string lastLocalized);
                string original;
                if (TryGetStableStaticReferenceOriginal(text, out string stableOriginal))
                    original = SelectStableReferenceSourceForTests(stableOriginal, saved,
                        text.text, lastLocalized);
                else
                    original = SelectRefreshSourceForTests(saved, text.text, lastLocalized);
                string localized = TranslateIncomingWithoutGuard(text, original);
                _originalTexts[id] = original;
                _lastLocalizedTexts[id] = localized;
                if (localized != text.text)
                {
                    text.text = localized;
                    uiCount++;
                }
            }
            int systemCount = applySystemStrings ? ApplySystemStrings() : 0;
            int dynamicLabelCount = RefreshDictionaryWordLabels(textList);
            if (uiCount + systemCount + dynamicLabelCount > 0)
                _log.LogInfo($"UI 本地化已应用：{uiCount} 个 TMP 文本，" +
                             $"{systemCount} 个运行时字符串模板，" +
                             $"{dynamicLabelCount} 个动态词典标签。");
        }
        finally
        {
            _applying = wasApplying;
        }
    }

    private string TranslateIncomingWithoutGuard(TMP_Text component, string original)
    {
        if (!_config.TranslateUI)
            return original;
        if (PeriodicTableElementCompatibility.TryResolveSymbolText(
                component, original, out string periodicTableSymbol))
            return periodicTableSymbol;
        if (CompilerErrorRuntime.IsCompilerError(original))
            return CompilerErrorRuntime.Format(original, _config.DisplayMode);
        if (ShouldPreservePlayerText(component))
            return original;
        RuntimeTranslationEntry entry = FindUiEntry(component, original);
        if (entry == null)
            return TranslateDynamic(component, original);
        string entryOriginal = entry.GameString("original_text");
        if (TokenCodec.Sha256(TokenCodec.ProtectForEntry(entryOriginal, entry)) != entry.SourceSha256)
        {
            ReportMismatch(entry.StableKey);
            return original;
        }
        return TokenCodec.FormatDisplayForEntry(entry.TranslatedText, entryOriginal, entry, _config,
            _dialogue.PlayerFullName());
    }

    private RuntimeTranslationEntry FindUiEntry(TMP_Text component, string proposed)
    {
        string key = BuildUiStableKey(component);
        if (_store.TryGet(key, out RuntimeTranslationEntry exact) &&
            exact.Kind == "ui_text" && exact.GameString("original_text") == proposed)
            return exact;
        RuntimeTranslationEntry fallback = _store.FindUnambiguousUiFallback(proposed);
        return fallback;
    }

    private string TranslateDynamic(TMP_Text component, string original)
    {
        string shortcut = ShortcutDisplayFormatter.Translate(original, _config.DisplayMode);
        if (shortcut != original)
            return shortcut;

        foreach (RuntimeTranslationEntry template in _store.UiTemplates)
        {
            if (TokenCodec.Sha256(template.SourceText ?? string.Empty) != template.SourceSha256)
            {
                ReportMismatch(template.StableKey);
                continue;
            }
            if (!UiTemplateRenderer.TryRender(template, original, out string rendered))
                continue;
            rendered = ApplyTemplateDisplayValues(template, rendered);
            return TokenCodec.FormatDisplayLiteral(rendered, original, _config);
        }

        RuntimeTranslationEntry achievement = _store.FindUnambiguousAchievement(original);
        if (achievement != null)
        {
            string localized = TranslateExactEntry(achievement, original);
            if (localized != null)
                return localized;
        }

        string displayValueLookup =
            PeriodicTableElementCompatibility.ResolvePreviewNameLookup(component, original);
        RuntimeTranslationEntry displayValue =
            _store.FindUnambiguousDisplayValue(displayValueLookup);
        if (displayValue != null)
        {
            string localized = TranslateExactEntry(displayValue, displayValueLookup);
            if (localized != null)
                return localized;
        }

        string composite = TranslateCompositeValues(original,
            ShouldTranslateDisplayValues(component));
        if (composite == original)
            return original;
        return TokenCodec.FormatDisplayLiteral(composite, original, _config);
    }

    internal string TranslateDynamicLiteral(string original) =>
        TranslateDynamic(null, original);

    internal string TranslateDisplayValueLiteral(string original)
    {
        if (original == null || !_config.Enabled || !_config.TranslateUI)
            return original;
        RuntimeTranslationEntry entry = _store.FindUnambiguousDisplayValue(original);
        if (entry != null)
            return TranslateExactEntry(entry, original) ?? original;

        // ProgressLog passes PuzzleList.name (for example "HELLO WORLD") while
        // the serialized field contains "Hello World!".  Use the same guarded,
        // longest-match alias path as composite UI text instead of falling through
        // to the shorter "Hello" title.
        return ApplyDisplayValues(original);
    }

    internal void ApplyKnownDynamicText(TMP_Text component, string original)
    {
        if (component == null)
            return;
        string localized = TranslateDynamicLiteral(original);
        int id = component.GetInstanceID();
        _originalTexts[id] = original;
        _lastLocalizedTexts[id] = localized;
        bool wasApplying = _applying;
        _applying = true;
        try
        {
            component.text = localized;
        }
        finally
        {
            _applying = wasApplying;
        }
    }

    internal string TranslateCompositeValues(string original,
        bool translateDisplayValues = false)
    {
        string composite = original;
        bool fragmentChanged = false;
        foreach (RuntimeTranslationEntry fragment in _store.UiFragments
                     .OrderByDescending(value => value.SourceText?.Length ?? 0))
        {
            string source = fragment.SourceText ?? string.Empty;
            if (string.IsNullOrEmpty(source) || !composite.Contains(source))
                continue;
            if (TokenCodec.Sha256(source) != fragment.SourceSha256)
            {
                ReportMismatch(fragment.StableKey);
                continue;
            }
            composite = composite.Replace(source, fragment.TranslatedText);
            fragmentChanged = true;
        }
        if (translateDisplayValues)
            composite = ApplyDisplayValues(composite);
        return fragmentChanged || composite != original ? composite : original;
    }

    internal string TranslateAnimatedSource(string original)
    {
        if (original == null || !_config.Enabled || !_config.TranslateUI)
            return original;
        string translated = TranslateCompositeValues(original, translateDisplayValues: true);
        return TokenCodec.FormatDisplayLiteral(translated, original, _config);
    }

    internal string ApplyTemplateDisplayValues(RuntimeTranslationEntry template,
        string rendered) =>
        template != null && template.GameBool("translate_display_values")
            ? ApplyDisplayValues(rendered)
            : rendered;

    private bool ShouldTranslateDisplayValues(TMP_Text component)
    {
        if (component == null)
            return false;
        if (PeriodicTableElementCompatibility.ShouldTranslateDisplayValues(component))
            return true;
        return _store.TryGet(BuildUiStableKey(component), out RuntimeTranslationEntry entry) &&
               entry.Kind == "ui_text" && entry.GameBool("translate_display_values");
    }

    internal string TranslateRuntimeSentinels(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;
        text = TranslateUnknownSignalPlaceholders(text);
        if (
            !_store.TryGet(UndefinedSuffixStableKey, out RuntimeTranslationEntry entry) ||
            entry.Kind != "ui_fragment")
            return text;
        string source = entry.SourceText ?? string.Empty;
        if (string.IsNullOrEmpty(source) || TokenCodec.Sha256(source) != entry.SourceSha256)
        {
            ReportMismatch(UndefinedSuffixStableKey);
            return text;
        }
        string translated = entry.TranslatedText ?? string.Empty;
        string from = _config.DisplayMode == DisplayMode.TranslationOnly ? source : translated;
        string to = _config.DisplayMode == DisplayMode.TranslationOnly ? translated : source;
        if (string.IsNullOrEmpty(from) || from == to)
            return text;
        return ReplaceUndefinedSignalSuffix(text, from, to);
    }

    private string TranslateUnknownSignalPlaceholders(string text)
    {
        if (_config.DisplayMode == DisplayMode.TranslationOnly)
            return UnknownSignalPlaceholderRegex.Replace(text, "信号$1");
        return LocalizedUnknownSignalPlaceholderRegex.Replace(text, "SIGNAL_$1");
    }

    private static string ReplaceUndefinedSignalSuffix(string text, string from, string to)
    {
        int searchStart = 0;
        StringBuilder result = null;
        while (searchStart < text.Length)
        {
            int match = text.IndexOf(from, searchStart, StringComparison.Ordinal);
            if (match < 0)
                break;
            int prefix = match - 1;
            while (prefix >= 0 && char.IsDigit(text[prefix]))
                prefix--;
            if (prefix >= 0 && text[prefix] == '-')
                prefix--;
            bool validPrefix = prefix >= 0 && text[prefix] == '@' && prefix < match - 1;
            int after = match + from.Length;
            bool validSuffix = after >= text.Length ||
                               (!char.IsLetterOrDigit(text[after]) && text[after] != '_');
            if (!validPrefix || !validSuffix)
            {
                searchStart = match + from.Length;
                continue;
            }
            result ??= new StringBuilder(text.Length + Math.Max(0, to.Length - from.Length));
            result.Append(text, searchStart, match - searchStart);
            result.Append(to);
            searchStart = after;
        }
        if (result == null)
            return text;
        result.Append(text, searchStart, text.Length - searchStart);
        return result.ToString();
    }

    private string TranslateExactEntry(RuntimeTranslationEntry entry, string original)
    {
        string expected = entry.GameString("original_text", entry.SourceText);
        if (TokenCodec.Sha256(TokenCodec.ProtectForEntry(expected, entry)) != entry.SourceSha256)
        {
            ReportMismatch(entry.StableKey);
            return null;
        }
        return TokenCodec.FormatDisplayForEntry(entry.TranslatedText, original, entry, _config,
            _dialogue.PlayerFullName());
    }

    internal string ApplyDisplayValues(string text)
    {
        text = ChineseYearQuantityFormatter.Translate(text);
        List<KeyValuePair<string, RuntimeTranslationEntry>> values = _store.DisplayValues
            .OrderByDescending(value => value.Key.Length)
            .ToList();
        var validValues = new List<(string Source, RuntimeTranslationEntry Entry)>(values.Count);
        foreach (KeyValuePair<string, RuntimeTranslationEntry> pair in values)
        {
            if (string.IsNullOrEmpty(pair.Key))
                continue;
            RuntimeTranslationEntry entry = pair.Value;
            if (TokenCodec.Sha256(TokenCodec.ProtectForEntry(pair.Key, entry)) !=
                entry.SourceSha256)
            {
                ReportMismatch(entry.StableKey);
                continue;
            }
            validValues.Add((pair.Key, entry));

            // Some summary screens use the Unity object's name instead of the
            // serialized display field.  In the shipped data that turns
            // "Hello World!" into "HELLO WORLD".  Accept the object name only
            // when it is exactly the same value with terminal punctuation
            // removed; this avoids turning arbitrary object names into aliases.
            string objectName = entry.GameString("object_name", string.Empty);
            if (IsTerminalPunctuationAlias(pair.Key, objectName))
                validValues.Add((objectName, entry));
        }

        validValues = validValues
            .OrderByDescending(value => value.Source.Length)
            .ToList();

        // Replace against the original rendered value in one pass. Re-running shorter
        // display keys over an already translated longer key caused names such as
        // "Hello World!" to become the mixed-language "你好 World!" because "Hello"
        // was processed again after the complete group name had matched.
        var matches = new List<(int Start, int Length, string Replacement)>();
        foreach ((string source, RuntimeTranslationEntry entry) in validValues)
        {
            int search = 0;
            int match = text.IndexOf(source, search, StringComparison.OrdinalIgnoreCase);
            while (match >= 0)
            {
                int after = match + source.Length;
                bool validStart = !IsAsciiIdentifier(source[0]) || match == 0 ||
                                  !IsAsciiIdentifier(text[match - 1]);
                bool validEnd = !IsAsciiIdentifier(source[source.Length - 1]) ||
                                after == text.Length || !IsAsciiIdentifier(text[after]);
                bool overlapsLongerMatch = matches.Any(existing =>
                    match < existing.Start + existing.Length && after > existing.Start);
                if (validStart && validEnd && !overlapsLongerMatch)
                    matches.Add((match, source.Length, entry.TranslatedText));
                search = match + Math.Max(1, source.Length);
                match = text.IndexOf(source, search, StringComparison.OrdinalIgnoreCase);
            }
        }

        if (matches.Count == 0)
            return text;
        matches.Sort((left, right) => left.Start.CompareTo(right.Start));
        var result = new StringBuilder(text.Length);
        int offset = 0;
        foreach ((int start, int length, string replacement) in matches)
        {
            result.Append(text, offset, start - offset);
            result.Append(replacement);
            offset = start + length;
        }
        result.Append(text, offset, text.Length - offset);
        return result.ToString();
    }

    private static string ReplaceOrdinalIgnoreCase(string text, string source,
        string replacement)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(source))
            return text;
        int scan = 0;
        int copyStart = 0;
        int match = text.IndexOf(source, scan, StringComparison.OrdinalIgnoreCase);
        if (match < 0)
            return text;
        var result = new StringBuilder(text.Length);
        bool replaced = false;
        while (match >= 0)
        {
            int after = match + source.Length;
            bool validStart = !IsAsciiIdentifier(source[0]) || match == 0 ||
                              !IsAsciiIdentifier(text[match - 1]);
            bool validEnd = !IsAsciiIdentifier(source[source.Length - 1]) ||
                            after == text.Length || !IsAsciiIdentifier(text[after]);
            if (!validStart || !validEnd)
            {
                scan = match + 1;
                match = text.IndexOf(source, scan, StringComparison.OrdinalIgnoreCase);
                continue;
            }
            result.Append(text, copyStart, match - copyStart);
            result.Append(replacement);
            copyStart = after;
            scan = after;
            replaced = true;
            match = text.IndexOf(source, scan, StringComparison.OrdinalIgnoreCase);
        }
        if (!replaced)
            return text;
        result.Append(text, copyStart, text.Length - copyStart);
        return result.ToString();
    }

    private static bool IsAsciiIdentifier(char value) =>
        value is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or '_';

    private static bool IsTerminalPunctuationAlias(string source, string alias)
    {
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(alias))
            return false;
        string trimmed = source.TrimEnd('!', '?', '.', '。', '！', '？');
        return trimmed.Length < source.Length &&
               string.Equals(trimmed, alias, StringComparison.OrdinalIgnoreCase);
    }

    internal static string SelectRefreshSourceForTests(string savedOriginal,
        string currentDisplay, string lastLocalizedDisplay)
    {
        if (savedOriginal == null)
            return currentDisplay;
        return lastLocalizedDisplay != null && currentDisplay == lastLocalizedDisplay
            ? savedOriginal
            : currentDisplay;
    }

    internal static string SelectStableReferenceSourceForTests(string stableOriginal,
        string savedOriginal, string currentDisplay, string lastLocalizedDisplay)
    {
        if (stableOriginal == null)
            return SelectRefreshSourceForTests(savedOriginal, currentDisplay,
                lastLocalizedDisplay);
        string stablePrefix = stableOriginal.TrimEnd();
        bool isGeneratedPrefix = stablePrefix.EndsWith("=", StringComparison.Ordinal);
        if (!isGeneratedPrefix)
            return stableOriginal;

        if (!string.IsNullOrEmpty(savedOriginal) &&
            savedOriginal.StartsWith(stableOriginal, StringComparison.OrdinalIgnoreCase) &&
            savedOriginal.Length > stableOriginal.Length)
            return lastLocalizedDisplay != null && currentDisplay == lastLocalizedDisplay
                ? savedOriginal
                : currentDisplay;
        if (!string.IsNullOrEmpty(currentDisplay) &&
            currentDisplay.StartsWith(stableOriginal, StringComparison.OrdinalIgnoreCase) &&
            currentDisplay.Length > stableOriginal.Length)
            return currentDisplay;
        return stableOriginal;
    }

    private bool TryGetStableStaticReferenceOriginal(TMP_Text component,
        out string original)
    {
        original = null;
        if (component == null)
            return false;
        string path = BuildObjectPath(component.transform);
        if (!IsStaticReferencePathForTests(path) ||
            !_store.TryGet(BuildUiStableKey(component), out RuntimeTranslationEntry entry) ||
            entry.Kind != "ui_text")
            return false;
        original = entry.GameString("original_text", entry.SourceText);
        return original != null;
    }

    internal static bool IsStaticReferencePathForTests(string path) =>
        !string.IsNullOrEmpty(path) &&
        path.StartsWith("Reference Window/", StringComparison.Ordinal) &&
        path.IndexOf("/ELEMENT DISPLAY", StringComparison.OrdinalIgnoreCase) < 0 &&
        path.IndexOf("/PERIODIC TABLE PAGE", StringComparison.OrdinalIgnoreCase) < 0;

    private string RememberDisplay(TMP_Text component, string original, string localized)
    {
        if (component == null)
            return localized;
        int id = component.GetInstanceID();
        _originalTexts[id] = original;
        _lastLocalizedTexts[id] = localized;
        return localized;
    }

    private static bool ShouldPreservePlayerText(TMP_Text component)
    {
        foreach (TMP_InputField input in component.GetComponentsInParent<TMP_InputField>(true))
            if (input != null && input.textComponent == component)
                return true;
        return PlayerAuthoredUiText.ShouldPreserve(BuildObjectPath(component.transform));
    }

    private int ApplySystemStrings()
    {
        if (!_config.TranslateSystem || _systemByPrefix.Count == 0)
            return 0;
        int count = 0;
        foreach (MonoBehaviour component in Resources.FindObjectsOfTypeAll<MonoBehaviour>())
        {
            if (component == null || !component.gameObject.scene.IsValid())
                continue;
            string prefix = BuildSystemPrefix(component);
            if (!_systemByPrefix.TryGetValue(prefix, out List<RuntimeTranslationEntry> entries))
                continue;
            foreach (RuntimeTranslationEntry entry in entries)
                if (ApplySystemEntry(component, entry))
                    count++;
        }
        return count;
    }

    private static int RefreshDictionaryWordLabels(IEnumerable<TMP_Text> texts)
    {
        if (DictionaryWordLabelUpdateMethod == null)
            return 0;
        var visited = new HashSet<int>();
        int count = 0;
        foreach (TMP_Text text in texts)
        {
            DictionaryWordLabel wordLabel = text?.GetComponent<DictionaryWordLabel>();
            if (wordLabel == null || !wordLabel.gameObject.scene.IsValid() ||
                !visited.Add(wordLabel.GetInstanceID()))
                continue;
            if (DictionaryWordLabelOverrideField?.GetValue(wordLabel) is true)
                continue;
            try
            {
                // The game composes these labels from prefix + dictionary word + suffix.
                // Re-run that composition only after component-string localization has
                // updated prefix/suffix; otherwise F5/F8 can leave the prefab prefix
                // (for example "1 Helisec = ") and discard its generated value.
                DictionaryWordLabelUpdateMethod.Invoke(wordLabel, null);
                count++;
            }
            catch
            {
                // Some inactive startup objects run before UserDictionary exists. Their
                // owning page is refreshed again when it is actually opened.
            }
        }
        return count;
    }

    private bool ApplySystemEntry(MonoBehaviour component, RuntimeTranslationEntry entry)
    {
        if (entry.Kind == "component_dialogue_frame")
            return ApplyComponentDialogueFrame(component, entry);
        string fieldPath = entry.GameString("field_path");
        if (!ReflectionPath.TryGetValue(component, fieldPath, out object current) ||
            current is not string currentString)
            return false;
        string trackingKey = component.GetInstanceID() + ":" + fieldPath;
        string original = _originalFields.TryGetValue(trackingKey, out string saved)
            ? saved
            : currentString;
        _originalFields[trackingKey] = original;
        return ReflectionPath.TrySetValue(component, fieldPath,
            LocalizeSystemValue(entry, original));
    }

    private string LocalizeSystemValue(RuntimeTranslationEntry entry, string original)
    {
        string source = TokenCodec.ProtectForEntry(original ?? string.Empty, entry);
        if (TokenCodec.Sha256(source) != entry.SourceSha256)
        {
            ReportMismatch(entry.StableKey);
            return original;
        }
        return TokenCodec.FormatDisplayForEntry(entry.TranslatedText, original, entry, _config,
            _dialogue.PlayerFullName());
    }

    private bool ApplyComponentDialogueFrame(MonoBehaviour component,
        RuntimeTranslationEntry entry)
    {
        string fieldPath = entry.GameString("field_path");
        if (!ReflectionPath.TryGetValue(component, fieldPath, out object value) ||
            value is not DialogueFrame current)
            return false;

        DialoguePart[] currentParts = current.dialogueParts ?? Array.Empty<DialoguePart>();
        var originalParts = new DialoguePart[currentParts.Length];
        Array.Copy(currentParts, originalParts, currentParts.Length);
        for (int index = 0; index < originalParts.Length; index++)
        {
            string trackingKey = component.GetInstanceID() + ":" + fieldPath + ":part:" + index;
            string original = _originalFields.TryGetValue(trackingKey, out string saved)
                ? saved
                : originalParts[index].txt ?? string.Empty;
            _originalFields[trackingKey] = original;
            originalParts[index].txt = original;
        }
        var sourceFrame = new DialogueFrame
        {
            speaker = current.speaker,
            dialogueParts = originalParts,
            msgDelay = current.msgDelay,
        };
        if (TokenCodec.Sha256(TokenCodec.BuildFrameSource(sourceFrame)) != entry.SourceSha256)
        {
            ReportMismatch(entry.StableKey);
            return false;
        }
        if (!TokenCodec.TrySplitFrameTranslation(entry.TranslatedText, originalParts.Length,
                _dialogue.PlayerFullName(DisplayMode.TranslationOnly),
                out string[] translatedParts))
            return false;

        var originalDisplayParts = new DialoguePart[originalParts.Length];
        Array.Copy(originalParts, originalDisplayParts, originalParts.Length);
        var localizedParts = new DialoguePart[originalParts.Length];
        Array.Copy(originalParts, localizedParts, originalParts.Length);
        for (int index = 0; index < localizedParts.Length; index++)
        {
            string displayOriginal = TokenCodec.RestoreForEntry(
                TokenCodec.ProtectForEntry(originalParts[index].txt, entry), entry,
                _dialogue.PlayerFullName(DisplayMode.OriginalOnly));
            originalDisplayParts[index].txt = TokenCodec.ApplyOriginalWhitespace(
                originalParts[index].txt, displayOriginal);
            localizedParts[index].txt = TokenCodec.ApplyOriginalWhitespace(
                originalParts[index].txt, translatedParts[index]);
        }
        var originalDisplayFrame = new DialogueFrame
        {
            speaker = current.speaker,
            dialogueParts = originalDisplayParts,
            msgDelay = current.msgDelay,
        };
        var translatedFrame = new DialogueFrame
        {
            speaker = current.speaker,
            dialogueParts = localizedParts,
            msgDelay = current.msgDelay,
        };
        _frameCatalog.Register(originalDisplayFrame, translatedFrame);
        current.dialogueParts = _config.DisplayMode == DisplayMode.TranslationOnly
            ? localizedParts
            : originalDisplayParts;
        return ReflectionPath.TrySetValue(component, fieldPath, current);
    }

    private void ReportMismatch(string key)
    {
        if (_reportedMismatches.Add(key))
            _log.LogError($"跳过 UI 译文 {key}：源文哈希不匹配。");
    }

    internal static string BuildUiStableKey(TMP_Text text)
    {
        string scope = text.gameObject.scene.IsValid() ? text.gameObject.scene.name : "<asset>";
        Component[] components = text.gameObject.GetComponents<Component>();
        int componentIndex = Array.IndexOf(components, text);
        return $"ui:{scope}:{BuildObjectPath(text.transform)}:component:{componentIndex}";
    }

    private static string BuildSystemPrefix(MonoBehaviour component)
    {
        Component[] components = component.gameObject.GetComponents<Component>();
        int componentIndex = Array.IndexOf(components, component);
        string scope = component.gameObject.scene.name;
        return $"system:{scope}:{BuildObjectPath(component.transform)}:component:{componentIndex}:field:";
    }

    internal static string BuildObjectPath(Transform transform)
    {
        var segments = new Stack<string>();
        Transform current = transform;
        while (current != null)
        {
            string segment = current.name;
            if (current.parent != null)
                segment += $"[{current.GetSiblingIndex()}]";
            segments.Push(segment);
            current = current.parent;
        }
        return string.Join("/", segments);
    }
}
