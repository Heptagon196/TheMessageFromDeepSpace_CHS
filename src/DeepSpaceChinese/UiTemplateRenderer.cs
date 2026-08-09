using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace DeepSpaceChinese;

internal static class UiTemplateRenderer
{
    private static readonly Regex Placeholder = new(@"\{DYN_(\d+)\}", RegexOptions.Compiled);

    public static bool TryRender(RuntimeTranslationEntry entry, string original,
        out string translated)
    {
        translated = null;
        string source = entry?.SourceText ?? string.Empty;
        MatchCollection tokens = Placeholder.Matches(source);
        if (tokens.Count == 0)
        {
            if (source != original)
                return false;
            translated = entry.TranslatedText;
            return true;
        }

        var pattern = new StringBuilder("^");
        var tokenNames = new List<string>();
        int cursor = 0;
        foreach (Match token in tokens)
        {
            pattern.Append(Regex.Escape(source.Substring(cursor, token.Index - cursor)));
            pattern.Append("(.*?)");
            tokenNames.Add(token.Value);
            cursor = token.Index + token.Length;
        }
        pattern.Append(Regex.Escape(source.Substring(cursor))).Append('$');
        Match valueMatch = Regex.Match(original ?? string.Empty, pattern.ToString(),
            RegexOptions.Singleline | RegexOptions.CultureInvariant);
        if (!valueMatch.Success)
            return false;

        var values = new Dictionary<string, string>();
        for (int index = 0; index < tokenNames.Count; index++)
        {
            string tokenName = tokenNames[index];
            string value = valueMatch.Groups[index + 1].Value;
            if (values.TryGetValue(tokenName, out string existing) && existing != value)
                return false;
            values[tokenName] = value;
        }
        translated = Placeholder.Replace(entry.TranslatedText ?? string.Empty,
            match => values.TryGetValue(match.Value, out string value) ? value : match.Value);
        return true;
    }
}
