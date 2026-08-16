using System;
using System.Text.RegularExpressions;

namespace DeepSpaceChinese;

internal static class ShortcutDisplayFormatter
{
    private static readonly Regex ShortcutPattern = new(
        @"(?<![A-Za-z0-9])(?<modifier>LeftControl|RightControl|LeftShift|RightShift|LeftAlt|RightAlt)\s*\+\s*(?<key>[A-Za-z0-9]+)(?![A-Za-z0-9])",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    public static string Translate(string text, DisplayMode mode)
    {
        if (mode != DisplayMode.TranslationOnly || string.IsNullOrEmpty(text))
            return text;
        return ShortcutPattern.Replace(text, match =>
        {
            string modifier = TranslateModifier(match.Groups["modifier"].Value);
            return modifier + " + " + match.Groups["key"].Value.ToUpperInvariant();
        });
    }

    private static string TranslateModifier(string modifier)
    {
        switch (modifier.ToUpperInvariant())
        {
            case "LEFTCONTROL": return "左Ctrl";
            case "RIGHTCONTROL": return "右Ctrl";
            case "LEFTSHIFT": return "左Shift";
            case "RIGHTSHIFT": return "右Shift";
            case "LEFTALT": return "左Alt";
            case "RIGHTALT": return "右Alt";
            default: return modifier;
        }
    }
}
