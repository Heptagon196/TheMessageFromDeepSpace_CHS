using System;

namespace DeepSpaceChinese;

internal readonly struct NameInputEdit
{
    public string Text { get; }
    public int Caret { get; }

    public NameInputEdit(string text, int caret)
    {
        Text = text;
        Caret = caret;
    }
}

internal static class NameInputTextEngine
{
    public static NameInputEdit Insert(string current, int position, int selection,
        string incoming, int characterLimit)
    {
        current ??= string.Empty;
        incoming ??= string.Empty;
        int start = Math.Max(0, Math.Min(Math.Min(position, selection), current.Length));
        int end = Math.Max(start, Math.Min(Math.Max(position, selection), current.Length));
        int available = characterLimit <= 0
            ? incoming.Length
            : Math.Max(0, characterLimit - (current.Length - (end - start)));
        int acceptedLength = Math.Min(incoming.Length, available);
        if (acceptedLength > 0 && acceptedLength < incoming.Length &&
            char.IsHighSurrogate(incoming[acceptedLength - 1]) &&
            char.IsLowSurrogate(incoming[acceptedLength]))
            acceptedLength--;
        string accepted = incoming.Substring(0, acceptedLength);
        string text = current.Substring(0, start) + accepted + current.Substring(end);
        return new NameInputEdit(text, start + accepted.Length);
    }
}
