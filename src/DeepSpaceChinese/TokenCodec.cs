using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace DeepSpaceChinese;

internal static class TokenCodec
{
    private static readonly Regex SignalEmbed = new(@"\|(-?\d{1,3})", RegexOptions.Compiled);
    private static readonly Regex SignalPlaceholder = new(@"\{SIG_(N)?(\d{3})\}", RegexOptions.Compiled);
    private static readonly Regex Player = new(@"(?<![A-Za-z])(?:[Tt]he\s+)?Translator\b",
        RegexOptions.Compiled);
    private static readonly Regex SpeakerMarker = new(@"\{SPEAKER_[A-Z0-9_]+\}", RegexOptions.Compiled);
    private static readonly Regex PartMarker = new(@"\{PART_(\d{3})\}", RegexOptions.Compiled);
    private static readonly Regex Animation = new(@"\$anim(?:[A-Za-z]\d{0,2}|\d{1,2})", RegexOptions.Compiled);
    private static readonly Regex RichTextTag = new(@"<[^>]+>", RegexOptions.Compiled);
    private const string PlayerPlaceholder = "{PLAYER_NAME}";

    public static string ProtectRuntimeTokens(string text)
    {
        if (text == null)
            return string.Empty;
        text = SignalEmbed.Replace(text, match =>
        {
            int value = int.Parse(match.Groups[1].Value);
            return value < 0 ? $"{{SIG_N{Math.Abs(value):000}}}" : $"{{SIG_{value:000}}}";
        });
        return Player.Replace(text, "{PLAYER_NAME}");
    }

    public static string RestoreRuntimeTokens(string text, string playerFullName)
    {
        return RestoreRuntimeTokens(text, playerFullName, false);
    }

    private static string RestoreRuntimeTokens(string text, string playerFullName,
        bool translatedTypography)
    {
        if (text == null)
            return string.Empty;
        text = SignalPlaceholder.Replace(text, match =>
        {
            int value = int.Parse(match.Groups[2].Value);
            if (match.Groups[1].Success)
                value = -value;
            return "|" + value;
        });
        return ReplacePlayerPlaceholder(text, playerFullName ?? "Translator", translatedTypography);
    }

    public static string ProtectForEntry(string text, RuntimeTranslationEntry entry)
    {
        if (text == null)
            return string.Empty;
        text = SignalEmbed.Replace(text, match =>
        {
            int value = int.Parse(match.Groups[1].Value);
            return value < 0 ? $"{{SIG_N{Math.Abs(value):000}}}" : $"{{SIG_{value:000}}}";
        });
        foreach (KeyValuePair<string, string> token in entry.RuntimeTokens()
                     .OrderByDescending(value => value.Key.Length))
            text = text.Replace(token.Key, token.Value);
        string playerLiteral = entry.GameString("player_token_literal");
        if (!string.IsNullOrEmpty(playerLiteral))
            text = text.Replace(playerLiteral, "{PLAYER_NAME}");
        return entry.GameBool("protect_player_name")
            ? Player.Replace(text, "{PLAYER_NAME}")
            : text;
    }

    public static string RestoreForEntry(string text, RuntimeTranslationEntry entry,
        string playerFullName, bool translatedTypography = false)
    {
        if (text == null)
            return string.Empty;
        text = SignalPlaceholder.Replace(text, match =>
        {
            int value = int.Parse(match.Groups[2].Value);
            if (match.Groups[1].Success)
                value = -value;
            return "|" + value;
        });
        foreach (KeyValuePair<string, string> token in entry.RuntimeTokens()
                     .OrderByDescending(value => value.Value.Length))
            text = text.Replace(token.Value, token.Key);
        return ReplacePlayerPlaceholder(text, playerFullName ?? "Translator", translatedTypography);
    }

    public static string FormatDisplayForEntry(string translated, string original,
        RuntimeTranslationEntry entry, PatchConfig config, string playerFullName)
    {
        translated = RestoreForEntry(translated ?? string.Empty, entry, playerFullName, true);
        original = RestoreForEntry(ProtectForEntry(original ?? string.Empty, entry), entry,
            playerFullName);
        return FormatDisplayLiteral(translated, original, config);
    }

    public static string FormatDisplayLiteral(string translated, string original, PatchConfig config)
    {
        if (config.DisplayMode == DisplayMode.TranslationOnly)
            return translated ?? string.Empty;
        return original ?? string.Empty;
    }

    public static string BuildFrameSource(DialogueFrame frame)
    {
        var builder = new StringBuilder();
        builder.Append("{SPEAKER_").Append(SpeakerName(frame.speaker)).Append('}');
        DialoguePart[] parts = frame.dialogueParts ?? Array.Empty<DialoguePart>();
        for (int index = 0; index < parts.Length; index++)
        {
            string protectedText = ProtectRuntimeTokens(parts[index].txt ?? string.Empty);
            builder.Append("{PART_").Append(index.ToString("000")).Append('}')
                .Append(TrimEdges(protectedText));
        }
        return builder.ToString();
    }

    public static bool TrySplitFrameTranslation(string translatedText, int expectedParts,
        string playerFullName, out string[] translatedParts)
    {
        translatedParts = null;
        if (string.IsNullOrEmpty(translatedText) || SpeakerMarker.Matches(translatedText).Count != 1)
            return false;
        string body = SpeakerMarker.Replace(translatedText, string.Empty, 1);
        MatchCollection matches = PartMarker.Matches(body);
        if (matches.Count != expectedParts)
            return false;
        var result = new string[expectedParts];
        for (int index = 0; index < matches.Count; index++)
        {
            if (int.Parse(matches[index].Groups[1].Value) != index)
                return false;
            int start = matches[index].Index + matches[index].Length;
            int end = index + 1 < matches.Count ? matches[index + 1].Index : body.Length;
            result[index] = RestoreRuntimeTokens(body.Substring(start, end - start), playerFullName,
                true);
        }
        translatedParts = result;
        return true;
    }

    public static string FormatDisplay(string translated, string original, PatchConfig config,
        string playerFullName)
    {
        translated = RestoreRuntimeTokens(translated ?? string.Empty, playerFullName, true);
        original = RestoreRuntimeTokens(ProtectRuntimeTokens(original ?? string.Empty), playerFullName);
        if (config.DisplayMode == DisplayMode.TranslationOnly)
            return translated;
        return original;
    }

    public static string ApplyOriginalWhitespace(string original, string localized)
    {
        original ??= string.Empty;
        localized ??= string.Empty;
        int leading = 0;
        while (leading < original.Length && char.IsWhiteSpace(original[leading]))
            leading++;
        int trailing = original.Length;
        while (trailing > leading && char.IsWhiteSpace(original[trailing - 1]))
            trailing--;
        return original.Substring(0, leading) + localized + original.Substring(trailing);
    }

    public static string[] ApplyTranslatedWhitespace(IReadOnlyList<string> originals,
        IReadOnlyList<string> localizedParts)
    {
        if (originals == null)
            throw new ArgumentNullException(nameof(originals));
        if (localizedParts == null)
            throw new ArgumentNullException(nameof(localizedParts));
        if (originals.Count != localizedParts.Count)
            throw new ArgumentException("原文与译文 PART 数量必须一致。", nameof(localizedParts));

        var result = new string[localizedParts.Count];
        for (int index = 0; index < result.Length; index++)
            result[index] = ApplyOriginalWhitespace(originals[index], localizedParts[index]);

        for (int index = 0; index + 1 < result.Length; index++)
        {
            int leftEnd = HorizontalWhitespaceStart(result[index]);
            int rightStart = HorizontalWhitespaceEnd(result[index + 1]);
            if (leftEnd == result[index].Length && rightStart == 0)
                continue;

            string leftVisible = VisibleText(result[index].Substring(0, leftEnd));
            string rightVisible = VisibleText(result[index + 1].Substring(rightStart));
            if (leftVisible.Length == 0 || rightVisible.Length == 0 ||
                !IsChineseTypographyCharacter(leftVisible[leftVisible.Length - 1]) ||
                !IsChineseTypographyCharacter(rightVisible[0]))
                continue;

            result[index] = result[index].Substring(0, leftEnd);
            result[index + 1] = result[index + 1].Substring(rightStart);
        }
        return result;
    }

    public static string RemoveAnimations(string value) => Animation.Replace(value ?? string.Empty, string.Empty);

    private static int HorizontalWhitespaceStart(string value)
    {
        int end = value?.Length ?? 0;
        while (end > 0 && value[end - 1] is ' ' or '\t')
            end--;
        return end;
    }

    private static int HorizontalWhitespaceEnd(string value)
    {
        int start = 0;
        while (start < (value?.Length ?? 0) && value[start] is ' ' or '\t')
            start++;
        return start;
    }

    private static string VisibleText(string value) =>
        RichTextTag.Replace(RemoveAnimations(value), string.Empty);

    private static string ReplacePlayerPlaceholder(string text, string playerFullName,
        bool translatedTypography)
    {
        if (!translatedTypography || string.IsNullOrEmpty(text) ||
            text.IndexOf(PlayerPlaceholder, StringComparison.Ordinal) < 0)
            return text?.Replace(PlayerPlaceholder, playerFullName) ?? string.Empty;

        string[] segments = text.Split(new[] { PlayerPlaceholder }, StringSplitOptions.None);
        var result = new StringBuilder(text.Length + playerFullName.Length * (segments.Length - 1));
        result.Append(segments[0]);
        bool compact = PlayerNameContainsCjk(playerFullName);

        for (int index = 1; index < segments.Length; index++)
        {
            string right = segments[index];
            if (compact)
            {
                int leftEnd = HorizontalWhitespaceStart(result.ToString());
                string compactLeftVisible = VisibleText(result.ToString(0, leftEnd));
                bool followsEnglishLetter = compactLeftVisible.Length > 0 &&
                    IsEnglishLetter(compactLeftVisible[compactLeftVisible.Length - 1]);
                result.Length = leftEnd;
                if (followsEnglishLetter)
                    result.Append(' ');
                int rightStart = HorizontalWhitespaceEnd(right);
                result.Append(playerFullName);
                result.Append(right, rightStart, right.Length - rightStart);
                continue;
            }

            string leftVisible = VisibleText(result.ToString());
            if (result.Length > 0 && result[result.Length - 1] is not (' ' or '\t' or '\r' or '\n') &&
                leftVisible.Length > 0 && NeedsPlayerBoundarySpace(leftVisible[leftVisible.Length - 1]))
                result.Append(' ');
            result.Append(playerFullName);
            string rightVisible = VisibleText(right);
            if (right.Length > 0 && right[0] is not (' ' or '\t' or '\r' or '\n') &&
                rightVisible.Length > 0 && NeedsPlayerBoundarySpace(rightVisible[0]))
                result.Append(' ');
            result.Append(right);
        }
        return result.ToString();
    }

    private static bool PlayerNameContainsCjk(string fullName)
    {
        string personalName = (fullName ?? string.Empty).Trim();
        if (personalName.EndsWith("博士", StringComparison.Ordinal))
            personalName = personalName.Substring(0, personalName.Length - 2).TrimEnd();
        return personalName.Any(character =>
            character is >= '\u3400' and <= '\u4DBF' or >= '\u4E00' and <= '\u9FFF' or
                >= '\uF900' and <= '\uFAFF');
    }

    private static bool NeedsPlayerBoundarySpace(char value) =>
        !char.IsWhiteSpace(value) && !char.IsPunctuation(value);

    private static bool IsEnglishLetter(char value) =>
        value is >= 'A' and <= 'Z' or >= 'a' and <= 'z';

    private static bool IsChineseTypographyCharacter(char value) =>
        value is >= '\u3400' and <= '\u4DBF' or >= '\u4E00' and <= '\u9FFF' or
            >= '\uF900' and <= '\uFAFF' ||
        "，。！？；：、…—～（）【】《》“”‘’".IndexOf(value) >= 0;

    public static string Sha256(string value)
    {
        using var sha = SHA256.Create();
        byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(value ?? string.Empty));
        return string.Concat(hash.Select(valueByte => valueByte.ToString("x2")));
    }

    public static string SpeakerName(Speaker speaker) => speaker switch
    {
        Speaker.Alan => "AKERS",
        Speaker.BScientist => "BAUTISTA",
        Speaker.Carrie => "COLLINS",
        Speaker.Doppler => "DOPPLER",
        Speaker.AutoLog => "AUTO_LOG",
        Speaker.Pilot => "PILOT",
        Speaker.Qopilot => "CO_PILOT",
        _ => "UNKNOWN_" + (int)speaker,
    };

    private static string TrimEdges(string value)
    {
        int start = 0;
        int end = value.Length;
        while (start < end && char.IsWhiteSpace(value[start]))
            start++;
        while (end > start && char.IsWhiteSpace(value[end - 1]))
            end--;
        return value.Substring(start, end - start);
    }
}
