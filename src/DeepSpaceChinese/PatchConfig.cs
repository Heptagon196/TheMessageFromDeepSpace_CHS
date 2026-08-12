using System;
using System.Collections.Generic;
using System.IO;
using BepInEx.Configuration;
using BepInEx.Logging;

namespace DeepSpaceChinese;

internal enum DisplayMode
{
    TranslationOnly,
    OriginalOnly
}

internal sealed class PatchConfig
{
    public bool Enabled { get; private set; } = true;
    public DisplayMode DisplayMode { get; set; } = DisplayMode.TranslationOnly;
    public bool FallbackToOriginal { get; private set; } = true;
    public bool SpeakerColorsEnabled { get; private set; } = true;
    public string AkersColor { get; private set; } = "#FFD166";
    public string BautistaColor { get; private set; } = "#7FDBFF";
    public string CollinsColor { get; private set; } = "#FF9BD2";
    public string DopplerColor { get; private set; } = "#A7E87B";
    public string AutoLogColor { get; private set; } = "#E6E6E6";
    public string PilotColor { get; private set; } = "#C7B8FF";
    public string CoPilotColor { get; private set; } = "#FFB07C";
    public bool TranslateDialogue { get; private set; } = true;
    public bool TranslateLogs { get; private set; } = true;
    public bool TranslateUI { get; private set; } = true;
    public bool TranslateSystem { get; private set; } = true;
    public bool CompilerCaseInsensitive { get; private set; } = true;
    public bool CompilerPunctuationInsensitive { get; private set; } = true;
    public bool MoveNewWordPromptToLowerRight { get; private set; } = true;
    public bool PuzzleFixesEnabled { get; private set; } = true;
    public KeyboardShortcut ToggleModeHotkey { get; private set; } = KeyboardShortcut.Deserialize("F8");
    public KeyboardShortcut ReloadTranslationsHotkey { get; private set; } = KeyboardShortcut.Deserialize("F5");
    public string FontSource { get; private set; } = "Auto";
    public string BundledFont { get; private set; } = @"Fonts\fusion-pixel-12px-proportional-zh_hans.otf";
    public string FontFile { get; private set; } = string.Empty;
    public string[] SystemFontCandidates { get; private set; } =
        { "Microsoft YaHei", "Noto Sans CJK SC", "SimHei" };

    public static PatchConfig Load(string path, ManualLogSource log)
    {
        var values = ReadIni(path, log);
        var result = new PatchConfig
        {
            Enabled = GetBool(values, "Localization.Enabled", true),
            FallbackToOriginal = GetBool(values, "Localization.FallbackToOriginal", true),
            SpeakerColorsEnabled = GetBool(values, "DialogueColors.Enabled", true),
            AkersColor = NormalizeColor(Get(values, "DialogueColors.Akers", "#FFD166"), "#FFD166"),
            BautistaColor = NormalizeColor(Get(values, "DialogueColors.Bautista", "#7FDBFF"), "#7FDBFF"),
            CollinsColor = NormalizeColor(Get(values, "DialogueColors.Collins", "#FF9BD2"), "#FF9BD2"),
            DopplerColor = NormalizeColor(Get(values, "DialogueColors.Doppler", "#A7E87B"), "#A7E87B"),
            AutoLogColor = NormalizeColor(Get(values, "DialogueColors.AutoLog", "#E6E6E6"), "#E6E6E6"),
            PilotColor = NormalizeColor(Get(values, "DialogueColors.Pilot", "#C7B8FF"), "#C7B8FF"),
            CoPilotColor = NormalizeColor(Get(values, "DialogueColors.CoPilot", "#FFB07C"), "#FFB07C"),
            TranslateDialogue = GetBool(values, "Localization.TranslateDialogue", true),
            TranslateLogs = GetBool(values, "Localization.TranslateLogs", true),
            TranslateUI = GetBool(values, "Localization.TranslateUI", true),
            TranslateSystem = GetBool(values, "Localization.TranslateSystem", true),
            CompilerCaseInsensitive = GetBool(values,
                "Compatibility.CompilerCaseInsensitive", true),
            CompilerPunctuationInsensitive = GetBool(values,
                "Compatibility.CompilerPunctuationInsensitive", true),
            MoveNewWordPromptToLowerRight = GetBool(values,
                "Layout.NewWordPromptLowerRight", true),
            PuzzleFixesEnabled = GetBool(values, "PuzzleFixes.Enabled", true),
            FontSource = Get(values, "Font.FontSource", "Auto"),
            BundledFont = Get(values, "Font.BundledFont", @"Fonts\fusion-pixel-12px-proportional-zh_hans.otf"),
            FontFile = Get(values, "Font.FontFile", string.Empty),
            SystemFontCandidates = Get(values, "Font.SystemFontCandidates", "Microsoft YaHei;Noto Sans CJK SC;SimHei")
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries),
        };
        string hotkeyText = Get(values, "Localization.ToggleModeHotkey", "F8");
        result.ToggleModeHotkey = KeyboardShortcut.Deserialize(hotkeyText);
        if (result.ToggleModeHotkey.Equals(KeyboardShortcut.Empty) &&
            !string.Equals(hotkeyText, "None", StringComparison.OrdinalIgnoreCase))
        {
            log.LogWarning($"ToggleModeHotkey '{hotkeyText}' 无效，运行时切换已禁用。");
        }
        string reloadHotkeyText = Get(values, "Localization.ReloadTranslationsHotkey", "F5");
        result.ReloadTranslationsHotkey = KeyboardShortcut.Deserialize(reloadHotkeyText);
        if (result.ReloadTranslationsHotkey.Equals(KeyboardShortcut.Empty) &&
            !string.Equals(reloadHotkeyText, "None", StringComparison.OrdinalIgnoreCase))
        {
            log.LogWarning($"ReloadTranslationsHotkey '{reloadHotkeyText}' 无效，运行时重载已禁用。");
        }
        if (!result.ToggleModeHotkey.Equals(KeyboardShortcut.Empty) &&
            result.ToggleModeHotkey.Equals(result.ReloadTranslationsHotkey))
        {
            log.LogWarning("ToggleModeHotkey 与 ReloadTranslationsHotkey 相同，一次按键会同时执行两项操作。");
        }
        return result;
    }

    public void ReloadFontSettings(string path, ManualLogSource log)
    {
        var values = ReadIni(path, log);
        FontSource = Get(values, "Font.FontSource", "Auto");
        BundledFont = Get(values, "Font.BundledFont", @"Fonts\fusion-pixel-12px-proportional-zh_hans.otf");
        FontFile = Get(values, "Font.FontFile", string.Empty);
        SystemFontCandidates = Get(values, "Font.SystemFontCandidates",
                "Microsoft YaHei;Noto Sans CJK SC;SimHei")
            .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
    }

    public void ReloadDialogueColorSettings(string path, ManualLogSource log)
    {
        var values = ReadIni(path, log);
        SpeakerColorsEnabled = GetBool(values, "DialogueColors.Enabled", true);
        AkersColor = NormalizeColor(Get(values, "DialogueColors.Akers", "#FFD166"), "#FFD166");
        BautistaColor = NormalizeColor(Get(values, "DialogueColors.Bautista", "#7FDBFF"), "#7FDBFF");
        CollinsColor = NormalizeColor(Get(values, "DialogueColors.Collins", "#FF9BD2"), "#FF9BD2");
        DopplerColor = NormalizeColor(Get(values, "DialogueColors.Doppler", "#A7E87B"), "#A7E87B");
        AutoLogColor = NormalizeColor(Get(values, "DialogueColors.AutoLog", "#E6E6E6"), "#E6E6E6");
        PilotColor = NormalizeColor(Get(values, "DialogueColors.Pilot", "#C7B8FF"), "#C7B8FF");
        CoPilotColor = NormalizeColor(Get(values, "DialogueColors.CoPilot", "#FFB07C"), "#FFB07C");
    }

    public void ReloadCompatibilitySettings(string path, ManualLogSource log)
    {
        var values = ReadIni(path, log);
        CompilerCaseInsensitive = GetBool(values,
            "Compatibility.CompilerCaseInsensitive", true);
        CompilerPunctuationInsensitive = GetBool(values,
            "Compatibility.CompilerPunctuationInsensitive", true);
        PuzzleFixesEnabled = GetBool(values, "PuzzleFixes.Enabled", true);
    }

    public void ReloadLayoutSettings(string path, ManualLogSource log)
    {
        var values = ReadIni(path, log);
        MoveNewWordPromptToLowerRight = GetBool(values,
            "Layout.NewWordPromptLowerRight", true);
    }

    public string SpeakerColor(Speaker speaker) => speaker switch
    {
        Speaker.Alan => AkersColor,
        Speaker.BScientist => BautistaColor,
        Speaker.Carrie => CollinsColor,
        Speaker.Doppler => DopplerColor,
        Speaker.AutoLog => AutoLogColor,
        Speaker.Pilot => PilotColor,
        Speaker.Qopilot => CoPilotColor,
        _ => AutoLogColor,
    };

    private static Dictionary<string, string> ReadIni(string path, ManualLogSource log)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!File.Exists(path))
        {
            log.LogWarning($"找不到配置文件：{path}，使用内置默认值。");
            return result;
        }
        string section = string.Empty;
        int lineNumber = 0;
        foreach (string rawLine in File.ReadAllLines(path))
        {
            lineNumber++;
            string line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("#") || line.StartsWith(";"))
                continue;
            if (line.StartsWith("[") && line.EndsWith("]"))
            {
                section = line.Substring(1, line.Length - 2).Trim();
                continue;
            }
            int equals = line.IndexOf('=');
            if (equals <= 0)
            {
                log.LogWarning($"忽略无法解析的配置行 {lineNumber}: {rawLine}");
                continue;
            }
            string key = line.Substring(0, equals).Trim();
            string value = line.Substring(equals + 1).Trim();
            result[$"{section}.{key}"] = value;
        }
        return result;
    }

    private static string Get(Dictionary<string, string> values, string key, string fallback) =>
        values.TryGetValue(key, out string value) ? value : fallback;

    private static bool GetBool(Dictionary<string, string> values, string key, bool fallback) =>
        bool.TryParse(Get(values, key, fallback.ToString()), out bool value) ? value : fallback;

    private static string NormalizeColor(string value, string fallback) =>
        RegexColor(value) ? value.ToUpperInvariant() : fallback;

    private static bool RegexColor(string value)
    {
        if (value == null || value.Length != 7 || value[0] != '#')
            return false;
        for (int i = 1; i < value.Length; i++)
            if (!Uri.IsHexDigit(value[i]))
                return false;
        return true;
    }
}
