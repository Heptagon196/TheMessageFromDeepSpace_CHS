using System.Globalization;
using System.Text;
using UnityEngine;

namespace DeepSpaceChinese;

internal static class DialoguePunctuationFontMarkup
{
    internal static bool ContainsChinesePunctuation(string text)
    {
        if (string.IsNullOrEmpty(text))
            return false;
        foreach (char value in text)
        {
            if (IsChinesePunctuation(value))
                return true;
        }
        return false;
    }

    internal static string Apply(string text, string fontName, string colorRgba)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrWhiteSpace(fontName) ||
            string.IsNullOrWhiteSpace(colorRgba))
            return text ?? string.Empty;

        var result = new StringBuilder(text.Length + 32);
        for (int index = 0; index < text.Length;)
        {
            if (text[index] == '<')
            {
                int tagEnd = text.IndexOf('>', index + 1);
                if (tagEnd >= 0)
                {
                    result.Append(text, index, tagEnd - index + 1);
                    index = tagEnd + 1;
                    continue;
                }
            }

            if (!IsChinesePunctuation(text[index]))
            {
                result.Append(text[index++]);
                continue;
            }

            int runStart = index++;
            while (index < text.Length && IsChinesePunctuation(text[index]))
                index++;
            result.Append("<font=\"").Append(fontName).Append("\"><color=#")
                .Append(colorRgba).Append('>');
            result.Append(text, runStart, index - runStart);
            result.Append("</color></font>");
        }
        return result.ToString();
    }

    private static bool IsChinesePunctuation(char value)
    {
        if (value <= '\u007f')
            return false;
        bool cjkForm = value is >= '\u3000' and <= '\u303f' or
            >= '\ufe10' and <= '\ufe1f' or
            >= '\ufe30' and <= '\ufe4f' or
            >= '\uff01' and <= '\uff65';
        bool commonChineseForm = value is '\u00b7' or >= '\u2010' and <= '\u2027';
        if (!cjkForm && !commonChineseForm)
            return false;
        UnicodeCategory category = char.GetUnicodeCategory(value);
        return category is UnicodeCategory.ConnectorPunctuation or
            UnicodeCategory.DashPunctuation or
            UnicodeCategory.OpenPunctuation or
            UnicodeCategory.ClosePunctuation or
            UnicodeCategory.InitialQuotePunctuation or
            UnicodeCategory.FinalQuotePunctuation or
            UnicodeCategory.OtherPunctuation;
    }
}

internal static class DialoguePunctuationPolicy
{
    internal static bool ShouldDecorate(bool isTrackedDialogue, bool usesCharacterFont,
        bool supportsRichText, DisplayMode mode, string text) =>
        supportsRichText &&
        mode == DisplayMode.TranslationOnly &&
        (isTrackedDialogue || usesCharacterFont) &&
        DialoguePunctuationFontMarkup.ContainsChinesePunctuation(text) &&
        (text?.IndexOf("<font=\"DeepSpaceChinese Fallback ",
            System.StringComparison.Ordinal) ?? -1) < 0;
}

internal static class DialoguePunctuationColor
{
    internal static Color Compensate(Color textColor, Color sourceFaceColor,
        Color targetFaceColor) => new(
        Divide(textColor.r * sourceFaceColor.r, targetFaceColor.r),
        Divide(textColor.g * sourceFaceColor.g, targetFaceColor.g),
        Divide(textColor.b * sourceFaceColor.b, targetFaceColor.b),
        Divide(textColor.a * sourceFaceColor.a, targetFaceColor.a));

    private static float Divide(float value, float divisor) =>
        Mathf.Clamp01(divisor <= 0.0001f ? value : value / divisor);
}
