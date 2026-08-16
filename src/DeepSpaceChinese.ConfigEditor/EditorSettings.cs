using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DeepSpaceChinese.ConfigEditor;

internal sealed class EditorSettings
{
    public bool Enabled = true;
    public string ToggleModeHotkey = "F8";
    public string ReloadTranslationsHotkey = "F5";
    public bool FallbackToOriginal = true;
    public bool TranslateDialogue = true;
    public bool TranslateLogs = true;
    public bool TranslateUI = true;
    public bool TranslateSystem = true;
    public bool CompilerCaseInsensitive = true;
    public bool CompilerPunctuationInsensitive = true;
    public bool MoveNewWordPromptToLowerRight = true;
    public bool PuzzleFixesEnabled = true;
    public bool KonamiAnswerAutofillEnabled = true;
    public bool DialogueColorsEnabled = true;
    public readonly Dictionary<string, string> Colors = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Akers"] = "#FFD166",
        ["Bautista"] = "#7FDBFF",
        ["Collins"] = "#FF9BD2",
        ["Doppler"] = "#A7E87B",
        ["AutoLog"] = "#E6E6E6",
        ["Pilot"] = "#C7B8FF",
        ["CoPilot"] = "#FFB07C",
    };
    public string FontSource = "Auto";
    public string BundledFont = @"Fonts\fusion-pixel-12px-proportional-zh_hans.otf";
    public string FontFile = string.Empty;
    public string SystemFontCandidates = "Microsoft YaHei;Noto Sans CJK SC;SimHei";

    public static EditorSettings FromIni(IniDocument ini)
    {
        var result = new EditorSettings
        {
            Enabled = Bool(ini.Get("Localization", "Enabled", "true"), true),
            ToggleModeHotkey = ini.Get("Localization", "ToggleModeHotkey", "F8"),
            ReloadTranslationsHotkey = ini.Get("Localization", "ReloadTranslationsHotkey", "F5"),
            FallbackToOriginal = Bool(ini.Get("Localization", "FallbackToOriginal", "true"), true),
            TranslateDialogue = Bool(ini.Get("Localization", "TranslateDialogue", "true"), true),
            TranslateLogs = Bool(ini.Get("Localization", "TranslateLogs", "true"), true),
            TranslateUI = Bool(ini.Get("Localization", "TranslateUI", "true"), true),
            TranslateSystem = Bool(ini.Get("Localization", "TranslateSystem", "true"), true),
            CompilerCaseInsensitive = Bool(ini.Get("Compatibility",
                "CompilerCaseInsensitive", "true"), true),
            CompilerPunctuationInsensitive = Bool(ini.Get("Compatibility",
                "CompilerPunctuationInsensitive", "true"), true),
            MoveNewWordPromptToLowerRight = Bool(ini.Get("Layout",
                "NewWordPromptLowerRight", "true"), true),
            PuzzleFixesEnabled = Bool(ini.Get("PuzzleFixes", "Enabled", "true"), true),
            KonamiAnswerAutofillEnabled = Bool(ini.Get("Cheats",
                "KonamiAnswerAutofill", "true"), true),
            DialogueColorsEnabled = Bool(ini.Get("DialogueColors", "Enabled", "true"), true),
            FontSource = ini.Get("Font", "FontSource", "Auto"),
            BundledFont = ini.Get("Font", "BundledFont", @"Fonts\fusion-pixel-12px-proportional-zh_hans.otf"),
            FontFile = ini.Get("Font", "FontFile", string.Empty),
            SystemFontCandidates = ini.Get("Font", "SystemFontCandidates",
                "Microsoft YaHei;Noto Sans CJK SC;SimHei"),
        };
        foreach (string key in new[] { "Akers", "Bautista", "Collins", "Doppler", "AutoLog", "Pilot", "CoPilot" })
            result.Colors[key] = ini.Get("DialogueColors", key, result.Colors[key]).ToUpperInvariant();
        return result;
    }

    public void ApplyTo(IniDocument ini)
    {
        ini.Set("Localization", "Enabled", Enabled.ToString().ToLowerInvariant());
        ini.Set("Localization", "ToggleModeHotkey", ToggleModeHotkey.Trim());
        ini.Set("Localization", "ReloadTranslationsHotkey", ReloadTranslationsHotkey.Trim());
        ini.Set("Localization", "FallbackToOriginal", FallbackToOriginal.ToString().ToLowerInvariant());
        ini.Set("Localization", "TranslateDialogue", TranslateDialogue.ToString().ToLowerInvariant());
        ini.Set("Localization", "TranslateLogs", TranslateLogs.ToString().ToLowerInvariant());
        ini.Set("Localization", "TranslateUI", TranslateUI.ToString().ToLowerInvariant());
        ini.Set("Localization", "TranslateSystem", TranslateSystem.ToString().ToLowerInvariant());
        // CompilerCaseInsensitive is a hidden backward-compatibility key.
        // Preserve it when an old INI already contains it, but never add it to
        // newly generated or editor-saved configuration files.
        ini.Set("Compatibility", "CompilerPunctuationInsensitive",
            CompilerPunctuationInsensitive.ToString().ToLowerInvariant());
        ini.Set("Layout", "NewWordPromptLowerRight",
            MoveNewWordPromptToLowerRight.ToString().ToLowerInvariant());
        ini.Set("PuzzleFixes", "Enabled", PuzzleFixesEnabled.ToString().ToLowerInvariant());
        ini.Set("Cheats", "KonamiAnswerAutofill",
            KonamiAnswerAutofillEnabled.ToString().ToLowerInvariant());
        ini.Set("DialogueColors", "Enabled", DialogueColorsEnabled.ToString().ToLowerInvariant());
        foreach (KeyValuePair<string, string> pair in Colors)
            ini.Set("DialogueColors", pair.Key, pair.Value.ToUpperInvariant());
        ini.Set("Font", "FontSource", FontSource);
        ini.Set("Font", "BundledFont", BundledFont.Trim());
        ini.Set("Font", "FontFile", FontFile.Trim());
        ini.Set("Font", "SystemFontCandidates", SystemFontCandidates.Trim());
    }

    public string Validate()
    {
        if (string.IsNullOrWhiteSpace(ToggleModeHotkey) || string.IsNullOrWhiteSpace(ReloadTranslationsHotkey))
            return "快捷键不能为空；如需禁用，请填写 None。";
        if (!string.Equals(ToggleModeHotkey.Trim(), "None", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(ToggleModeHotkey.Trim(), ReloadTranslationsHotkey.Trim(), StringComparison.OrdinalIgnoreCase))
            return "模式切换键和重载键不能相同。";
        foreach (KeyValuePair<string, string> pair in Colors)
            if (!Regex.IsMatch(pair.Value ?? string.Empty, "^#[0-9A-Fa-f]{6}$"))
                return pair.Key + " 的颜色无效，请使用 #RRGGBB。";
        if (!new[] { "Auto", "Bundled", "File", "System" }.ContainsIgnoreCase(FontSource))
            return "字体来源无效。";
        if (string.Equals(FontSource, "Bundled", StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrWhiteSpace(BundledFont))
            return "选择随包字体时，字体路径不能为空。";
        if (string.Equals(FontSource, "File", StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrWhiteSpace(FontFile))
            return "选择自定义文件时，请先指定字体文件。";
        if (string.Equals(FontSource, "System", StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrWhiteSpace(SystemFontCandidates))
            return "选择系统字体时，候选字体不能为空。";
        return null;
    }

    private static bool Bool(string text, bool fallback) =>
        bool.TryParse(text, out bool result) ? result : fallback;
}

internal static class StringArrayExtensions
{
    public static bool ContainsIgnoreCase(this IEnumerable<string> values, string target)
    {
        foreach (string value in values)
            if (string.Equals(value, target, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }
}
