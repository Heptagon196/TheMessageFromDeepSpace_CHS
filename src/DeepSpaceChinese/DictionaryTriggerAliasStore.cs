using System;
using System.Collections.Generic;
using System.IO;
using BepInEx.Logging;
using Newtonsoft.Json;

namespace DeepSpaceChinese;

internal sealed class DictionaryTriggerAliasStore
{
    private readonly List<Entry> _entries;
    private readonly List<DialogueVariant> _dialogueVariants;

    private DictionaryTriggerAliasStore(List<Entry> entries,
        List<DialogueVariant> dialogueVariants)
    {
        _entries = entries ?? new List<Entry>();
        _dialogueVariants = dialogueVariants ?? new List<DialogueVariant>();
    }

    public int Count => _entries.Count;
    public int VariantCount => _dialogueVariants.Count;
    internal IReadOnlyList<DialogueVariant> DialogueVariants => _dialogueVariants;

    public static DictionaryTriggerAliasStore Empty { get; } =
        new DictionaryTriggerAliasStore(new List<Entry>(), new List<DialogueVariant>());

    public static bool TryLoad(string path, ManualLogSource log,
        out DictionaryTriggerAliasStore store)
    {
        store = Empty;
        try
        {
            if (!File.Exists(path))
            {
                log?.LogWarning($"未找到词典中文触发规则：{path}");
                return false;
            }
            Root root = JsonConvert.DeserializeObject<Root>(File.ReadAllText(path));
            if (root?.Entries == null)
                throw new InvalidDataException("缺少 entries 数组。");
            var valid = new List<Entry>();
            foreach (Entry entry in root.Entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.Channel) ||
                    string.IsNullOrWhiteSpace(entry.English) || entry.Rules == null)
                    continue;
                valid.Add(entry);
            }
            var variants = new List<DialogueVariant>();
            var syntheticIds = new HashSet<int>();
            foreach (DialogueVariant variant in root.DialogueVariants ??
                     new List<DialogueVariant>())
            {
                ValidateVariant(variant);
                if (!syntheticIds.Add(variant.SyntheticDialogueId))
                    throw new InvalidDataException(
                        $"词典独立对白 ID {variant.SyntheticDialogueId} 重复。");
                variants.Add(variant);
            }
            store = new DictionaryTriggerAliasStore(valid, variants);
            log?.LogInfo($"已载入词典中文附加触发规则：{valid.Count} 条，" +
                         $"独立对白变体 {variants.Count} 条。");
            return true;
        }
        catch (Exception ex)
        {
            log?.LogError($"载入词典中文触发规则失败，继续使用原版英文条件：{ex.Message}");
            return false;
        }
    }

    public bool Matches(int termId, ListenChannel channel, string english,
        string candidate) => Matches(termId, channel.ToString(), english, candidate);

    internal bool Matches(int termId, string channelName, string english,
        string candidate)
    {
        if (string.IsNullOrEmpty(english) || candidate == null)
            return false;

        foreach (Entry entry in _entries)
        {
            if (!string.Equals(entry.Channel, channelName,
                    StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(entry.English, english,
                    StringComparison.OrdinalIgnoreCase) ||
                (entry.TermId.HasValue && entry.TermId.Value != termId))
                continue;
            foreach (Rule rule in entry.Rules)
                if (RuleMatches(rule, candidate))
                    return true;
        }
        return false;
    }

    internal bool TryGetDialogueVariant(int termId, string fromName, string toName,
        int dialogueId, out DialogueVariant variant)
    {
        return TryGetDialogueVariant(termId, fromName, toName,
            candidate => candidate.DialogueId == dialogueId, out variant);
    }

    internal bool TryGetDialogueVariant(int termId, string fromName, string toName,
        out DialogueVariant variant)
    {
        return TryGetDialogueVariant(termId, fromName, toName,
            _ => true, out variant);
    }

    private bool TryGetDialogueVariant(int termId, string fromName, string toName,
        Func<DialogueVariant, bool> filter, out DialogueVariant variant)
    {
        variant = null;
        foreach (DialogueVariant candidate in _dialogueVariants)
        {
            if (!filter(candidate) ||
                (candidate.TermId.HasValue && candidate.TermId.Value != termId))
                continue;
            string value = CandidateForChannel(candidate.Channel, fromName, toName);
            if (value == null)
                continue;
            foreach (Rule rule in candidate.Rules)
            {
                if (!RuleMatches(rule, value))
                    continue;
                variant = candidate;
                return true;
            }
        }
        return false;
    }

    private static string CandidateForChannel(string channel, string fromName,
        string toName)
    {
        if (string.Equals(channel, "EditEntryFromName",
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(channel, "EditEntryIDFromName",
                StringComparison.OrdinalIgnoreCase))
            return fromName;
        if (string.Equals(channel, "EditEntryToName",
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(channel, "EditEntryIDToName",
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(channel, "EditEntryIDContains",
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(channel, "DictEntryIs",
                StringComparison.OrdinalIgnoreCase))
            return toName;
        return null;
    }

    private static void ValidateVariant(DialogueVariant variant)
    {
        if (variant == null || variant.DialogueId <= 0 ||
            variant.SyntheticDialogueId <= 0 ||
            string.IsNullOrWhiteSpace(variant.Channel) ||
            string.IsNullOrWhiteSpace(variant.English) ||
            string.IsNullOrWhiteSpace(variant.TranslatedTitle) ||
            variant.Rules == null || variant.Rules.Count == 0 ||
            variant.Frames == null || variant.Frames.Count == 0)
            throw new InvalidDataException("词典独立对白变体字段不完整。");
        foreach (Rule rule in variant.Rules)
        {
            if (rule == null || rule.Values == null || rule.Values.Count == 0 ||
                string.IsNullOrWhiteSpace(rule.Type))
                throw new InvalidDataException(
                    $"词典独立对白 {variant.SyntheticDialogueId} 的触发规则无效。");
        }
        var frameIndices = new HashSet<int>();
        foreach (DialogueVariantFrame frame in variant.Frames)
        {
            if (frame == null || frame.FrameIndex < 0 ||
                string.IsNullOrWhiteSpace(frame.TranslatedText) ||
                !frameIndices.Add(frame.FrameIndex))
                throw new InvalidDataException(
                    $"词典独立对白 {variant.SyntheticDialogueId} 的 frame 无效或重复。");
        }
    }

    internal static bool RuleMatches(Rule rule, string candidate) =>
        RuleMatchLength(rule, candidate) > 0;

    internal static int RuleMatchLength(Rule rule, string candidate)
    {
        if (rule?.Values == null || rule.Values.Count == 0 || candidate == null)
            return 0;
        if (rule.ExcludeAny != null)
            foreach (string excluded in rule.ExcludeAny)
                if (!string.IsNullOrEmpty(excluded) &&
                    candidate.IndexOf(excluded,
                        StringComparison.OrdinalIgnoreCase) >= 0)
                    return 0;
        string type = rule.Type?.Trim() ?? string.Empty;
        if (string.Equals(type, "contains_all", StringComparison.OrdinalIgnoreCase))
        {
            int totalLength = 0;
            foreach (string value in rule.Values)
            {
                if (string.IsNullOrEmpty(value) ||
                    candidate.IndexOf(value, StringComparison.OrdinalIgnoreCase) < 0)
                    return 0;
                totalLength += value.Length;
            }
            return totalLength;
        }
        int longest = 0;
        foreach (string value in rule.Values)
        {
            if (string.IsNullOrEmpty(value))
                continue;
            if (string.Equals(type, "contains", StringComparison.OrdinalIgnoreCase))
            {
                if (candidate.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0)
                    longest = Math.Max(longest, value.Length);
            }
            else if (string.Equals(type, "exact", StringComparison.OrdinalIgnoreCase) &&
                     string.Equals(candidate.Trim(), value.Trim(),
                         StringComparison.OrdinalIgnoreCase))
            {
                longest = Math.Max(longest, value.Trim().Length);
            }
        }
        return longest;
    }

    internal sealed class Root
    {
        [JsonProperty("entries")]
        public List<Entry> Entries { get; set; }

        [JsonProperty("dialogue_variants")]
        public List<DialogueVariant> DialogueVariants { get; set; }
    }

    internal sealed class Entry
    {
        [JsonProperty("term_id")]
        public int? TermId { get; set; }

        [JsonProperty("channel")]
        public string Channel { get; set; }

        [JsonProperty("english")]
        public string English { get; set; }

        [JsonProperty("rules")]
        public List<Rule> Rules { get; set; }
    }

    internal sealed class Rule
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("values")]
        public List<string> Values { get; set; }

        [JsonProperty("exclude_any")]
        public List<string> ExcludeAny { get; set; }
    }

    internal sealed class DialogueVariant
    {
        [JsonProperty("term_id")]
        public int? TermId { get; set; }

        [JsonProperty("channel")]
        public string Channel { get; set; }

        [JsonProperty("english")]
        public string English { get; set; }

        [JsonProperty("dialogue_id")]
        public int DialogueId { get; set; }

        [JsonProperty("synthetic_dialogue_id")]
        public int SyntheticDialogueId { get; set; }

        [JsonProperty("rules")]
        public List<Rule> Rules { get; set; }

        [JsonProperty("translated_title")]
        public string TranslatedTitle { get; set; }

        [JsonProperty("frames")]
        public List<DialogueVariantFrame> Frames { get; set; }
    }

    internal sealed class DialogueVariantFrame
    {
        [JsonProperty("frame_index")]
        public int FrameIndex { get; set; }

        [JsonProperty("translated_text")]
        public string TranslatedText { get; set; }
    }
}
