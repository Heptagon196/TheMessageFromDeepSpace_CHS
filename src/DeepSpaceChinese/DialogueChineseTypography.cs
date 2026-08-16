using System;
using System.Text;
using System.Text.RegularExpressions;

namespace DeepSpaceChinese;

internal static class DialogueChineseTypography
{
    private static readonly Regex UnderlinedTerm = new(
        @"(?<left>[ \t]*)(?<term><u(?:\s[^>]*)?>.*?</u>)(?<right>[ \t]*)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

    internal static bool ShouldNormalize(DisplayMode mode, bool isTrackedDialogue,
        bool isLogReplayBody) =>
        mode == DisplayMode.TranslationOnly && (isTrackedDialogue || isLogReplayBody);

    public static string Normalize(string text)
    {
        string value = NormalizeDictionaryTermSpacing(text);
        var output = new StringBuilder(value);
        for (int index = 0; index < output.Length; index++)
        {
            char replacement = ChinesePunctuation(output[index]);
            if (replacement == '\0' || IsDotRun(output, index))
                continue;
            char previous = PreviousVisible(output, index);
            char following = NextVisible(output, index);
            if (IsChineseContext(previous) || IsChineseContext(following))
                output[index] = replacement;
        }
        return output.ToString();
    }

    internal static string NormalizeDictionaryTermSpacing(string text)
    {
        string value = text ?? string.Empty;
        // ReplaceSignalEmbeds follows English word-boundary rules and may leave
        // padding around its underlined dictionary term. A Chinese term removes
        // padding only at a Chinese/punctuation boundary; an adjacent English word
        // or number still needs exactly one separating space. Inspect visible
        // neighbours outside TMP tags rather than the tag characters themselves.
        return UnderlinedTerm.Replace(value, match =>
        {
            string term = match.Groups["term"].Value;
            if (!ContainsChinese(term))
                return match.Value;
            var source = new StringBuilder(value);
            char previous = PreviousVisible(source, match.Index);
            char following = NextVisible(source, match.Index + match.Length - 1);
            string left = IsLatinWordContext(previous) ? " " : string.Empty;
            string right = IsLatinWordContext(following) ? " " : string.Empty;
            return left + term + right;
        });
    }

    private static char ChinesePunctuation(char value) => value switch
    {
        '.' => '。',
        ',' => '，',
        ';' => '；',
        ':' => '：',
        '!' => '！',
        '?' => '？',
        _ => '\0',
    };

    private static bool IsDotRun(StringBuilder value, int index) =>
        value[index] == '.' &&
        (index > 0 && value[index - 1] == '.' ||
         index + 1 < value.Length && value[index + 1] == '.');

    private static char PreviousVisible(StringBuilder value, int index)
    {
        int cursor = index - 1;
        while (cursor >= 0)
        {
            if (char.IsWhiteSpace(value[cursor]))
            {
                cursor--;
                continue;
            }
            if (value[cursor] == '>')
            {
                while (cursor >= 0 && value[cursor] != '<')
                    cursor--;
                cursor--;
                continue;
            }
            return value[cursor];
        }
        return '\0';
    }

    private static char NextVisible(StringBuilder value, int index)
    {
        int cursor = index + 1;
        while (cursor < value.Length)
        {
            if (char.IsWhiteSpace(value[cursor]))
            {
                cursor++;
                continue;
            }
            if (value[cursor] == '<')
            {
                while (cursor < value.Length && value[cursor] != '>')
                    cursor++;
                cursor++;
                continue;
            }
            return value[cursor];
        }
        return '\0';
    }

    private static bool IsChineseContext(char value) =>
        value is >= '\u3400' and <= '\u4DBF' or >= '\u4E00' and <= '\u9FFF' or
            >= '\uF900' and <= '\uFAFF' ||
        "，。！？；：、…—～（）【】《》“”‘’".IndexOf(value) >= 0;

    private static bool IsLatinWordContext(char value) =>
        value is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or '_';

    private static bool ContainsChinese(string value)
    {
        foreach (char character in value)
        {
            if (character is >= '\u3400' and <= '\u4DBF' or
                >= '\u4E00' and <= '\u9FFF' or
                >= '\uF900' and <= '\uFAFF')
                return true;
        }
        return false;
    }
}
