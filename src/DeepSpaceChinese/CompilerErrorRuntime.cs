using System;
using System.Text.RegularExpressions;
using HarmonyLib;

namespace DeepSpaceChinese;

internal static class CompilerErrorRuntime
{
    private const string Header = "- Compilation Failed -";
    private const string TranslatedHeader = "- 编译失败 -";
    private static readonly Regex ErrorLabel = new(
        @"(?m)^(?<prefix>\d+ - )(?<label>Entry not found|Not a Number|Miscellaneous Error): ?(?<cr>\r?)$",
        RegexOptions.Compiled);
    private static readonly object PreparedSourceLock = new();
    private static string _preparedOriginal;
    private static string _preparedTranslation;

    public static bool IsCompilerError(string text) =>
        (text ?? string.Empty).StartsWith(Header, StringComparison.Ordinal);

    public static string Format(string text, DisplayMode mode)
    {
        if (mode != DisplayMode.TranslationOnly || !IsCompilerError(text))
            return text;

        string body = text.Substring(Header.Length);
        if (body.StartsWith("\r\nNull Input", StringComparison.Ordinal))
            body = "\r\n输入为空" + body.Substring("\r\nNull Input".Length);
        else if (body.StartsWith("\nNull Input", StringComparison.Ordinal))
            body = "\n输入为空" + body.Substring("\nNull Input".Length);

        body = ErrorLabel.Replace(body, match =>
        {
            string translated = match.Groups["label"].Value switch
            {
                "Entry not found" => "未找到词条",
                "Not a Number" => "不是数字",
                _ => "其他错误",
            };
            return match.Groups["prefix"].Value + translated + "：" +
                   match.Groups["cr"].Value;
        });
        return TranslatedHeader + body;
    }

    public static string PrepareForTyping(string text, DisplayMode mode)
    {
        string prepared = Format(text, mode);
        lock (PreparedSourceLock)
        {
            _preparedOriginal = text;
            _preparedTranslation = prepared;
        }
        return prepared;
    }

    public static bool TryResolvePreparedSource(string text, out string original,
        out string translated)
    {
        lock (PreparedSourceLock)
        {
            if (!string.Equals(text, _preparedTranslation, StringComparison.Ordinal))
            {
                original = null;
                translated = null;
                return false;
            }
            original = _preparedOriginal;
            translated = _preparedTranslation;
            return original != null && translated != null;
        }
    }
}

[HarmonyPatch(typeof(CompilerResult), "get_ErrorMsg")]
internal static class CompilerResultErrorMessagePatch
{
    [HarmonyPostfix]
    private static void Postfix(ref string __result)
    {
        try
        {
            DeepSpaceChinesePlugin plugin = DeepSpaceChinesePlugin.Instance;
            if (plugin != null)
                __result = plugin.PrepareCompilerErrorForTyping(__result);
        }
        catch (Exception ex)
        {
            DeepSpaceChinesePlugin.Instance?.PluginLog.LogError(
                $"预翻译编译错误逐字文本失败：\n{ex}");
        }
    }
}
