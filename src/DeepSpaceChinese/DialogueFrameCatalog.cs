using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace DeepSpaceChinese;

internal readonly struct DialogueFramePair
{
    public DialogueFramePair(DialogueFrame original, DialogueFrame translated)
    {
        Original = original;
        Translated = translated;
    }

    public DialogueFrame Original { get; }
    public DialogueFrame Translated { get; }
}

internal sealed class DialogueFrameCatalog
{
    private readonly Dictionary<string, DialogueFramePair> _pairs = new(StringComparer.Ordinal);
    private readonly Dictionary<string, DialogueFramePair> _stablePairs = new(StringComparer.Ordinal);
    private readonly HashSet<string> _ambiguous = new(StringComparer.Ordinal);

    public void Clear()
    {
        _pairs.Clear();
        _stablePairs.Clear();
        _ambiguous.Clear();
    }

    public void Register(DialogueFrame original, DialogueFrame translated,
        string stableKey = null)
    {
        var pair = new DialogueFramePair(original, translated);
        RegisterKey(FrameKey(original), pair);
        RegisterKey(FrameKey(translated), pair);
        if (!string.IsNullOrWhiteSpace(stableKey))
            _stablePairs[stableKey] = pair;
    }

    public bool TryGet(DialogueFrame current, out DialogueFramePair pair) =>
        _pairs.TryGetValue(FrameKey(current), out pair);

    public bool TryGet(string stableKey, out DialogueFramePair pair) =>
        _stablePairs.TryGetValue(stableKey ?? string.Empty, out pair);

    private void RegisterKey(string key, DialogueFramePair pair)
    {
        if (_ambiguous.Contains(key))
            return;
        if (!_pairs.TryGetValue(key, out DialogueFramePair existing))
        {
            _pairs.Add(key, pair);
            return;
        }
        if (FrameKey(existing.Original) == FrameKey(pair.Original) &&
            FrameKey(existing.Translated) == FrameKey(pair.Translated))
            return;
        _pairs.Remove(key);
        _ambiguous.Add(key);
    }

    private static string FrameKey(DialogueFrame frame)
    {
        var output = new StringBuilder();
        output.Append((int)frame.speaker).Append('|');
        DialoguePart[] parts = frame.dialogueParts ?? Array.Empty<DialoguePart>();
        output.Append(parts.Length).Append('|');
        foreach (DialoguePart part in parts)
        {
            string text = part.txt ?? string.Empty;
            output.Append(text.Length).Append(':').Append(text)
                .Append('|').Append(part.clearPrev ? '1' : '0')
                .Append('|').Append(part.charDelay.ToString("R", CultureInfo.InvariantCulture))
                .Append('|').Append(part.msgDelay.ToString("R", CultureInfo.InvariantCulture))
                .Append(';');
        }
        return output.ToString();
    }
}
