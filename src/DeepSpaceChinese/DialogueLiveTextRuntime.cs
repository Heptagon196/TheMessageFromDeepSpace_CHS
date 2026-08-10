using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using TMPro;

namespace DeepSpaceChinese;

internal sealed class DialogueTextMap
{
    private readonly Dictionary<string, string> _toOriginal = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _toTranslation = new(StringComparer.Ordinal);
    private readonly HashSet<string> _ambiguousOriginal = new(StringComparer.Ordinal);
    private readonly HashSet<string> _ambiguousTranslation = new(StringComparer.Ordinal);

    public static DialogueTextMap Create(IReadOnlyList<DialogueLayoutPart> original,
        IReadOnlyList<DialogueLayoutPart> translated, string repeatedPrefix,
        Func<string, string> resolver)
    {
        var result = new DialogueTextMap();
        List<string> originalStates = BuildStates(original, resolver);
        List<string> translatedStates = BuildStates(translated, resolver);
        result.AddStatePairs(originalStates, translatedStates, repeatedPrefix ?? string.Empty);
        if (!string.IsNullOrEmpty(repeatedPrefix))
            result.AddStatePairs(originalStates, translatedStates, string.Empty);
        return result;
    }

    public bool TryMap(string proposed, DisplayMode target, out string mapped,
        out int sourceLength, out int targetLength)
    {
        proposed ??= string.Empty;
        Dictionary<string, string> index = target == DisplayMode.TranslationOnly
            ? _toTranslation
            : _toOriginal;
        if (!index.TryGetValue(proposed, out mapped))
        {
            sourceLength = proposed.Length;
            targetLength = proposed.Length;
            return false;
        }
        sourceLength = proposed.Length;
        targetLength = mapped.Length;
        return true;
    }

    public static int ScaleVisibleCharacters(int visible, int sourceLength, int targetLength)
    {
        if (visible <= 0 || targetLength <= 0)
            return 0;
        if (sourceLength <= 0 || visible >= sourceLength)
            return targetLength;
        return Math.Min(targetLength,
             Math.Max(1, (int)Math.Round((double)visible / sourceLength * targetLength)));
    }

    public static int RemapVisibleCharacters(int visible, int producerLength,
        int oldTargetLength, int newTargetLength)
    {
        int producerVisible = ScaleVisibleCharacters(visible,
            oldTargetLength <= 0 ? producerLength : oldTargetLength, producerLength);
        return ScaleVisibleCharacters(producerVisible, producerLength, newTargetLength);
    }

    private void AddStatePairs(IReadOnlyList<string> original,
        IReadOnlyList<string> translated, string prefix)
    {
        if (original.Count == 0 || translated.Count == 0)
            return;
        for (int index = 0; index < original.Count; index++)
        {
            int translatedIndex = AlignedIndex(index, original.Count, translated.Count);
            string source = prefix + original[index];
            AddUnambiguous(_toOriginal, _ambiguousOriginal, source, source);
            AddUnambiguous(_toTranslation, _ambiguousTranslation, source,
                prefix + translated[translatedIndex]);
        }
        for (int index = 0; index < translated.Count; index++)
        {
            int originalIndex = AlignedIndex(index, translated.Count, original.Count);
            string source = prefix + translated[index];
            AddUnambiguous(_toOriginal, _ambiguousOriginal, source,
                prefix + original[originalIndex]);
            AddUnambiguous(_toTranslation, _ambiguousTranslation, source, source);
        }
    }

    private static List<string> BuildStates(IReadOnlyList<DialogueLayoutPart> parts,
        Func<string, string> resolver)
    {
        var result = new List<string>();
        string accumulated = string.Empty;
        for (int index = 0; index < parts.Count; index++)
        {
            DialogueLayoutPart part = parts[index];
            if (index == 0 || part.ClearPrevious)
                accumulated = string.Empty;
            accumulated += resolver(part.Text ?? string.Empty) ?? string.Empty;
            result.Add(accumulated);
        }
        return result;
    }

    private static int AlignedIndex(int index, int sourceCount, int targetCount)
    {
        if (sourceCount <= 1 || targetCount <= 1)
            return 0;
        return Math.Min(targetCount - 1,
            (int)Math.Round((double)index * (targetCount - 1) / (sourceCount - 1)));
    }

    private static void AddUnambiguous(Dictionary<string, string> index,
        HashSet<string> ambiguous, string source, string target)
    {
        if (ambiguous.Contains(source))
            return;
        if (!index.TryGetValue(source, out string existing))
        {
            index.Add(source, target);
            return;
        }
        if (existing == target)
            return;
        index.Remove(source);
        ambiguous.Add(source);
    }
}

internal sealed class DialogueLiveTextRuntime
{
    private sealed class TrackedText
    {
        public TMP_Text Component;
        public DialogueTextMap Map;
        public bool ScaleActive;
        public string ProducerText;
        public int SourceLength;
        public int TargetLength;
    }

    private static readonly FieldInfo MainSubtitleField =
        AccessTools.Field(typeof(DialogueManager), "subtitle");
    private static readonly FieldInfo NonLogSubtitleField =
        AccessTools.Field(typeof(NonLogDialogueManager), "subtitle");

    private readonly PatchConfig _config;
    private readonly ManualLogSource _log;
    private readonly Dictionary<int, TrackedText> _tracked = new();
    private bool _refreshing;

    public DialogueLiveTextRuntime(PatchConfig config, ManualLogSource log)
    {
        _config = config;
        _log = log;
    }

    public void TrackMain(DialogueManager manager, DialogueFrame original,
        DialogueFrame translated)
    {
        Track(MainSubtitleField?.GetValue(manager) as TMP_Text, original, translated,
            string.Empty);
    }

    public void TrackNonLog(NonLogDialogueManager manager, DialogueFrame original,
        DialogueFrame translated)
    {
        string prefix = DialogueManager.GetSpeakerNameDr(original.speaker) + ": ";
        Track(NonLogSubtitleField?.GetValue(manager) as TMP_Text, original, translated, prefix);
    }

    public void TrackCharacter(TMP_Text component, DialogueFrame original,
        DialogueFrame translated)
    {
        Track(component, original, translated, string.Empty);
    }

    public void TrackLiteral(TMP_Text component, string original, string translated)
    {
        Track(component,
            new DialogueFrame
            {
                dialogueParts = new[] { new DialoguePart { txt = original ?? string.Empty } },
            },
            new DialogueFrame
            {
                dialogueParts = new[] { new DialoguePart { txt = translated ?? string.Empty } },
            }, string.Empty);
    }

    public bool TryTranslate(TMP_Text component, string proposed, out string translated)
    {
        translated = proposed;
        if (_refreshing || component == null ||
            !_tracked.TryGetValue(component.GetInstanceID(), out TrackedText state))
            return false;
        if (!state.Map.TryMap(proposed, _config.DisplayMode, out translated,
                out int sourceLength, out int targetLength))
        {
            state.ScaleActive = false;
            return false;
        }
        state.ProducerText = proposed ?? string.Empty;
        state.ScaleActive = translated != proposed;
        state.SourceLength = sourceLength;
        state.TargetLength = targetLength;
        return true;
    }

    public void AdjustMaxVisibleCharacters(TMP_Text component, ref int value)
    {
        if (_refreshing || component == null ||
            !_tracked.TryGetValue(component.GetInstanceID(), out TrackedText state) ||
            !state.ScaleActive)
            return;
        value = DialogueTextMap.ScaleVisibleCharacters(value, state.SourceLength,
            state.TargetLength);
    }

    public void RefreshAll()
    {
        foreach (KeyValuePair<int, TrackedText> item in
                 new List<KeyValuePair<int, TrackedText>>(_tracked))
        {
            TrackedText state = item.Value;
            if (state.Component == null)
            {
                _tracked.Remove(item.Key);
                continue;
            }
            string producer = state.ProducerText ?? state.Component.text;
            if (!state.Map.TryMap(producer, _config.DisplayMode, out string mapped,
                    out int sourceLength, out int targetLength))
                continue;
            if (mapped == state.Component.text && state.TargetLength == targetLength)
                continue;
            int visible = state.Component.maxVisibleCharacters;
            int mappedVisible = DialogueTextMap.RemapVisibleCharacters(visible,
                sourceLength, state.TargetLength <= 0 ? sourceLength : state.TargetLength,
                targetLength);
            _refreshing = true;
            try
            {
                state.Component.text = mapped;
                state.Component.maxVisibleCharacters = mappedVisible;
            }
            finally
            {
                _refreshing = false;
            }
            state.SourceLength = sourceLength;
            state.TargetLength = targetLength;
            state.ScaleActive = sourceLength != targetLength;
        }
    }

    private void Track(TMP_Text component, DialogueFrame original, DialogueFrame translated,
        string prefix)
    {
        if (component == null)
            return;
        try
        {
            _tracked[component.GetInstanceID()] = new TrackedText
            {
                Component = component,
                Map = DialogueTextMap.Create(ToLayoutParts(original), ToLayoutParts(translated),
                    prefix, ResolveRuntimeText),
            };
        }
        catch (Exception ex)
        {
            _log.LogWarning($"无法跟踪当前对白的语言切换：{ex.Message}");
        }
    }

    private static DialogueLayoutPart[] ToLayoutParts(DialogueFrame frame)
    {
        DialoguePart[] parts = frame.dialogueParts ?? Array.Empty<DialoguePart>();
        var result = new DialogueLayoutPart[parts.Length];
        for (int index = 0; index < parts.Length; index++)
        {
            DialoguePart part = parts[index];
            result[index] = new DialogueLayoutPart(part.txt, part.charDelay, part.clearPrev,
                part.msgDelay);
        }
        return result;
    }

    private static string ResolveRuntimeText(string raw)
    {
        string value = DialogueChunk.RemoveAnimCommands(raw ?? string.Empty);
        if (value.IndexOf('|') >= 0)
            value = DialogueManager.ReplaceSignalEmbeds(value);
        if (value.IndexOf("Translator", StringComparison.Ordinal) >= 0)
            value = DialogueManager.ReplaceTranslator(value);
        return value;
    }

}
