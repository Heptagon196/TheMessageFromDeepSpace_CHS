using System;
using System.Text.RegularExpressions;

namespace DeepSpaceChinese;

internal static class CompilerErrorRuntime
{
    private const string Header = "- Compilation Failed -";
    private const string TranslatedHeader = "- 编译失败 -";
    private static readonly Regex ErrorLabel = new(
        @"(?m)^(?<prefix>\d+ - )(?<label>Entry not found|Not a Number|Miscellaneous Error): ?(?<cr>\r?)$",
        RegexOptions.Compiled);

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
}
