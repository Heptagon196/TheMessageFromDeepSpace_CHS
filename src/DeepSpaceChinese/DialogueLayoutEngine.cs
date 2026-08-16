using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeepSpaceChinese;

internal readonly struct DialogueLayoutPart
{
    public readonly string Text;
    public readonly float CharacterDelay;
    public readonly bool ClearPrevious;
    public readonly float MessageDelay;

    public DialogueLayoutPart(string text, float characterDelay, bool clearPrevious, float messageDelay)
    {
        Text = text ?? string.Empty;
        CharacterDelay = characterDelay;
        ClearPrevious = clearPrevious;
        MessageDelay = messageDelay;
    }
}

internal sealed class DialogueLayoutResult
{
    public IReadOnlyList<DialogueLayoutPart> Parts { get; }
    public int AddedPages { get; }

    public bool WasPaginated => AddedPages > 0;

    public DialogueLayoutResult(IReadOnlyList<DialogueLayoutPart> parts, int addedPages)
    {
        Parts = parts;
        AddedPages = addedPages;
    }
}

internal static class DialogueWidthBudget
{
    internal static float AvailableWidth(float rectWidth, float leftMargin,
        float rightMargin) =>
        Math.Max(0f, rectWidth - leftMargin - rightMargin);
}

/// <summary>
/// Pure dialogue pagination logic. Font measurement is supplied by the runtime adapter so this
/// class can be regression-tested without starting Unity.
/// </summary>
internal static class DialogueLayoutEngine
{
    private const int MaxDisplayLines = 2;

    private enum TokenKind
    {
        Text,
        RichTag,
        Control,
        PartBoundary,
    }

    private sealed class Token
    {
        public string Raw;
        public int PartIndex;
        public TokenKind Kind;
        public bool PreferredBreakAfter;

        public bool IsVisible => Kind == TokenKind.Text;
    }

    private sealed class OpenTag
    {
        public string Name;
        public string Raw;

    }

    private readonly struct PageRange
    {
        public readonly int Start;
        public readonly int End;

        public PageRange(int start, int end)
        {
            Start = start;
            End = end;
        }
    }

    public static DialogueLayoutResult Fit(
        IReadOnlyList<DialogueLayoutPart> input,
        float availableWidth,
        float shrinkThreshold,
        Func<string, float> measure,
        string repeatedPrefix = "",
        Func<string, bool> fitsDisplayArea = null)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));
        if (measure == null)
            throw new ArgumentNullException(nameof(measure));
        if (input.Count == 0 || availableWidth <= 0f || shrinkThreshold <= 1f)
            return new DialogueLayoutResult(input ?? Array.Empty<DialogueLayoutPart>(), 0);

        var output = new List<DialogueLayoutPart>(input.Count);
        int addedPages = 0;
        int groupStart = 0;
        while (groupStart < input.Count)
        {
            int groupEnd = groupStart + 1;
            while (groupEnd < input.Count && !input[groupEnd].ClearPrevious)
                groupEnd++;

            string groupText = string.Concat(input.Skip(groupStart).Take(groupEnd - groupStart)
                .Select(part => part.Text));
            float fullWidth = SafeMeasure(measure, repeatedPrefix + groupText);
            float pageCapacity = availableWidth * MaxDisplayLines;
            bool fitsArea = fitsDisplayArea == null ||
                            fitsDisplayArea(repeatedPrefix + groupText);
            if (fullWidth <= pageCapacity && fitsArea)
            {
                for (int index = groupStart; index < groupEnd; index++)
                    output.Add(input[index]);
            }
            else
            {
                IReadOnlyList<DialogueLayoutPart> group = input.Skip(groupStart)
                    .Take(groupEnd - groupStart).ToArray();
                List<DialogueLayoutPart> paginated = PaginateGroup(group, pageCapacity,
                    measure, repeatedPrefix, fitsDisplayArea, out int pageCount);
                output.AddRange(paginated);
                addedPages += Math.Max(0, pageCount - 1);
            }
            groupStart = groupEnd;
        }
        return new DialogueLayoutResult(output, addedPages);
    }

    private static List<DialogueLayoutPart> PaginateGroup(
        IReadOnlyList<DialogueLayoutPart> parts,
        float availableWidth,
        Func<string, float> measure,
        string prefix,
        Func<string, bool> fitsDisplayArea,
        out int pageCount)
    {
        List<Token> tokens = Tokenize(parts);
        if (!tokens.Any(token => token.IsVisible))
        {
            pageCount = 1;
            return parts.ToList();
        }

        var pages = new List<PageRange>();
        int pageStart = 0;
        while (pageStart < tokens.Count)
        {
            int lastFitEnd = -1;
            int lastPreferredEnd = -1;
            int scan = pageStart;
            bool overflowed = false;
            while (scan < tokens.Count)
            {
                scan++;
                if (!tokens[scan - 1].IsVisible)
                    continue;
                string candidate = BuildBalanced(tokens, pageStart, scan);
                float width = SafeMeasure(measure, prefix + candidate);
                bool fitsArea = fitsDisplayArea == null ||
                                fitsDisplayArea(prefix + candidate);
                if (width <= availableWidth && fitsArea)
                {
                    lastFitEnd = scan;
                    if (tokens[scan - 1].PreferredBreakAfter)
                        lastPreferredEnd = scan;
                    continue;
                }
                if (lastFitEnd < 0)
                    lastFitEnd = scan;
                overflowed = true;
                break;
            }

            if (!overflowed && scan >= tokens.Count)
            {
                pages.Add(new PageRange(pageStart, tokens.Count));
                break;
            }

            int pageEnd = lastFitEnd;
            if (lastPreferredEnd > pageStart)
            {
                string preferred = BuildBalanced(tokens, pageStart, lastPreferredEnd);
                float preferredWidth = SafeMeasure(measure, prefix + preferred);
                bool preferredFitsArea = fitsDisplayArea == null ||
                                         fitsDisplayArea(prefix + preferred);
                if (preferredWidth >= availableWidth * 0.45f && preferredFitsArea)
                    pageEnd = lastPreferredEnd;
            }
            if (pageEnd <= pageStart)
                pageEnd = Math.Min(tokens.Count, pageStart + 1);

            // Chinese closing punctuation must never start a new page. A signal marker is an
            // atomic token and a preferred break point, so without this kinsoku pass a phrase
            // such as "|-90。" can otherwise leave the full stop alone on the following page.
            pageEnd = IncludeLeadingClosingPunctuation(tokens, pageEnd);

            pages.Add(new PageRange(pageStart, pageEnd));
            pageStart = pageEnd;
        }

        pageCount = pages.Count;
        var result = new List<DialogueLayoutPart>(parts.Count + Math.Max(0, pageCount - 1));
        for (int pageIndex = 0; pageIndex < pages.Count; pageIndex++)
        {
            PageRange page = pages[pageIndex];
            int fragmentStart = page.Start;
            bool firstFragment = true;
            while (fragmentStart < page.End)
            {
                int sourceIndex = tokens[fragmentStart].PartIndex;
                int fragmentEnd = fragmentStart + 1;
                while (fragmentEnd < page.End && tokens[fragmentEnd].PartIndex == sourceIndex)
                    fragmentEnd++;

                DialogueLayoutPart source = parts[sourceIndex];
                bool endsSourcePart = fragmentEnd >= tokens.Count ||
                                      tokens[fragmentEnd].PartIndex != sourceIndex;
                bool clearPrevious = firstFragment
                    ? pageIndex == 0 ? source.ClearPrevious : true
                    : false;
                result.Add(new DialogueLayoutPart(
                    BuildBalanced(tokens, fragmentStart, fragmentEnd),
                    source.CharacterDelay,
                    clearPrevious,
                    endsSourcePart ? source.MessageDelay : 0f));
                firstFragment = false;
                fragmentStart = fragmentEnd;
            }
        }
        return result;
    }

    private static List<Token> Tokenize(IReadOnlyList<DialogueLayoutPart> parts)
    {
        var result = new List<Token>();
        for (int partIndex = 0; partIndex < parts.Count; partIndex++)
        {
            result.Add(new Token
            {
                Raw = string.Empty,
                PartIndex = partIndex,
                Kind = TokenKind.PartBoundary,
            });
            string text = parts[partIndex].Text ?? string.Empty;
            int index = 0;
            while (index < text.Length)
            {
                if (text[index] == '<')
                {
                    int close = text.IndexOf('>', index + 1);
                    if (close >= 0)
                    {
                        result.Add(new Token
                        {
                            Raw = text.Substring(index, close - index + 1),
                            PartIndex = partIndex,
                            Kind = TokenKind.RichTag,
                        });
                        index = close + 1;
                        continue;
                    }
                }
                if (StartsAnimation(text, index, out int animationLength))
                {
                    result.Add(new Token
                    {
                        Raw = text.Substring(index, animationLength),
                        PartIndex = partIndex,
                        Kind = TokenKind.Control,
                    });
                    index += animationLength;
                    continue;
                }
                if (text[index] == '|' && TrySignalLength(text, index, out int signalLength))
                {
                    string rawSignal = text.Substring(index, signalLength);
                    result.Add(new Token
                    {
                        Raw = rawSignal,
                        PartIndex = partIndex,
                        Kind = TokenKind.Text,
                        PreferredBreakAfter = true,
                    });
                    index += signalLength;
                    continue;
                }

                int length = char.IsHighSurrogate(text[index]) && index + 1 < text.Length &&
                             char.IsLowSurrogate(text[index + 1]) ? 2 : 1;
                string raw = text.Substring(index, length);
                result.Add(new Token
                {
                    Raw = raw,
                    PartIndex = partIndex,
                    Kind = TokenKind.Text,
                    PreferredBreakAfter = IsPreferredBreak(raw),
                });
                index += length;
            }
        }
        return result;
    }

    private static string BuildBalanced(IReadOnlyList<Token> tokens, int start, int end)
    {
        var active = new List<OpenTag>();
        for (int index = 0; index < start; index++)
            ApplyTag(tokens[index], active);

        var output = new StringBuilder();
        foreach (OpenTag tag in active)
            output.Append(tag.Raw);
        for (int index = start; index < end; index++)
        {
            output.Append(tokens[index].Raw);
            ApplyTag(tokens[index], active);
        }
        for (int index = active.Count - 1; index >= 0; index--)
            output.Append("</").Append(active[index].Name).Append('>');
        return output.ToString();
    }

    private static void ApplyTag(Token token, List<OpenTag> active)
    {
        if (token.Kind != TokenKind.RichTag || !TryParseTag(token.Raw,
                out string name, out bool closing, out bool selfClosing))
            return;
        if (selfClosing)
            return;
        if (!closing)
        {
            active.Add(new OpenTag { Name = name, Raw = token.Raw });
            return;
        }
        for (int index = active.Count - 1; index >= 0; index--)
        {
            if (!string.Equals(active[index].Name, name, StringComparison.OrdinalIgnoreCase))
                continue;
            active.RemoveRange(index, active.Count - index);
            return;
        }
    }

    private static bool TryParseTag(string raw, out string name, out bool closing, out bool selfClosing)
    {
        name = string.Empty;
        closing = false;
        selfClosing = false;
        if (string.IsNullOrEmpty(raw) || raw.Length < 3 || raw[0] != '<' || raw[raw.Length - 1] != '>')
            return false;
        int index = 1;
        if (raw[index] == '/')
        {
            closing = true;
            index++;
        }
        while (index < raw.Length - 1 && char.IsWhiteSpace(raw[index]))
            index++;
        int start = index;
        while (index < raw.Length - 1 && (char.IsLetterOrDigit(raw[index]) || raw[index] == '-'))
            index++;
        if (index == start)
            return false;
        name = raw.Substring(start, index - start);
        selfClosing = raw.EndsWith("/>", StringComparison.Ordinal) ||
                      string.Equals(name, "br", StringComparison.OrdinalIgnoreCase) ||
                      string.Equals(name, "sprite", StringComparison.OrdinalIgnoreCase);
        return true;
    }

    private static bool StartsAnimation(string text, int index, out int length)
    {
        length = 0;
        if (index + 6 > text.Length ||
            !string.Equals(text.Substring(index, 5), "$anim", StringComparison.Ordinal))
            return false;
        length = 6;
        while (length < 8 && index + length < text.Length && char.IsDigit(text[index + length]))
            length++;
        return true;
    }

    private static bool TrySignalLength(string text, int index, out int length)
    {
        length = 0;
        int cursor = index + 1;
        if (cursor < text.Length && text[cursor] == '-')
            cursor++;
        int digitStart = cursor;
        while (cursor < text.Length && char.IsDigit(text[cursor]))
            cursor++;
        if (cursor == digitStart)
            return false;
        length = cursor - index;
        return true;
    }

    private static bool IsPreferredBreak(string raw)
    {
        if (string.IsNullOrEmpty(raw))
            return false;
        char value = raw[raw.Length - 1];
        return char.IsWhiteSpace(value) || "，。！？；：、,.!?;:)]}）】》”’—-".IndexOf(value) >= 0;
    }

    private static int IncludeLeadingClosingPunctuation(IReadOnlyList<Token> tokens, int pageEnd)
    {
        int scan = pageEnd;
        int punctuationEnd = -1;
        while (scan < tokens.Count)
        {
            Token token = tokens[scan];
            if (token.Kind == TokenKind.RichTag)
            {
                scan++;
                continue;
            }
            if (token.Kind != TokenKind.Text || !IsClosingPunctuation(token.Raw))
                break;
            punctuationEnd = ++scan;
        }
        if (punctuationEnd < 0)
            return pageEnd;
        while (punctuationEnd < tokens.Count &&
               tokens[punctuationEnd].Kind == TokenKind.RichTag)
            punctuationEnd++;
        return punctuationEnd;
    }

    private static bool IsClosingPunctuation(string raw)
    {
        if (string.IsNullOrEmpty(raw))
            return false;
        char value = raw[raw.Length - 1];
        return "，。！？；：、,.!?;:)]}）】》”’…—～".IndexOf(value) >= 0;
    }

    private static float SafeMeasure(Func<string, float> measure, string value)
    {
        float measured = measure(value ?? string.Empty);
        return float.IsNaN(measured) || float.IsInfinity(measured) ? float.MaxValue : measured;
    }
}
