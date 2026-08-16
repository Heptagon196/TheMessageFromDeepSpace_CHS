using System;
using System.Text;

namespace DeepSpaceChinese;

/// <summary>
/// Replaces signal markers without relying on the game's fixed-position parser. Localization,
/// rich-text decoration and automatic pagination can all change the characters surrounding a
/// marker, so every valid marker must use this parser rather than only end-of-string markers.
/// </summary>
internal static class SignalEmbedRuntime
{
    internal static bool RequiresSafePath(string input)
    {
        if (string.IsNullOrEmpty(input))
            return false;
        for (int index = input.IndexOf('|'); index >= 0;
             index = input.IndexOf('|', index + 1))
        {
            if (TryReadSignal(input, index, out _, out _))
                return true;
        }
        return false;
    }

    internal static string Replace(string input, Func<int, string> resolveTerm)
    {
        if (string.IsNullOrEmpty(input))
            return input ?? string.Empty;
        var output = new StringBuilder(input.Length + 16);
        int copiedThrough = 0;
        for (int index = 0; index < input.Length; index++)
        {
            if (input[index] != '|' ||
                !TryReadSignal(input, index, out int signal, out int end))
                continue;
            output.Append(input, copiedThrough, index - copiedThrough);
            string term = resolveTerm?.Invoke(signal);
            output.Append("<u>")
                .Append(term ?? "SIGNAL_" + signal)
                .Append("</u>");
            copiedThrough = end;
            index = end - 1;
        }
        if (copiedThrough == 0)
            return input;
        output.Append(input, copiedThrough, input.Length - copiedThrough);
        return output.ToString();
    }

    internal static string NormalizeOutput(string text) =>
        DialogueChineseTypography.NormalizeDictionaryTermSpacing(text);

    private static bool TryReadSignal(string input, int start, out int signal, out int end)
    {
        signal = 0;
        end = start;
        if (string.IsNullOrEmpty(input) || start < 0 || start >= input.Length ||
            input[start] != '|')
            return false;
        int cursor = start + 1;
        bool negative = cursor < input.Length && input[cursor] == '-';
        if (negative)
            cursor++;
        int digitStart = cursor;
        int digits = 0;
        while (cursor < input.Length && digits < 3 && char.IsDigit(input[cursor]))
        {
            signal = checked(signal * 10 + (input[cursor] - '0'));
            cursor++;
            digits++;
        }
        if (cursor == digitStart || cursor < input.Length && char.IsDigit(input[cursor]))
            return false;
        if (negative)
            signal = -signal;
        end = cursor;
        return true;
    }
}
