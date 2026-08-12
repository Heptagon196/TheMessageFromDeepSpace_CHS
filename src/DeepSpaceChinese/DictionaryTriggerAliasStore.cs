using System;
using System.Collections.Generic;
using System.IO;
using BepInEx.Logging;
using Newtonsoft.Json;

namespace DeepSpaceChinese;

internal sealed class DictionaryTriggerAliasStore
{
    private readonly List<Entry> _entries;

    private DictionaryTriggerAliasStore(List<Entry> entries)
    {
        _entries = entries ?? new List<Entry>();
    }

    public int Count => _entries.Count;

    public static DictionaryTriggerAliasStore Empty { get; } =
        new DictionaryTriggerAliasStore(new List<Entry>());

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
            store = new DictionaryTriggerAliasStore(valid);
            log?.LogInfo($"已载入词典中文附加触发规则：{valid.Count} 条。");
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

        int longest = 0;
        Entry winner = null;
        bool tied = false;
        foreach (Entry entry in _entries)
        {
            if (!ChannelsCompete(entry.Channel, channelName) ||
                (entry.TermId.HasValue && entry.TermId.Value != termId))
                continue;

            int matchedLength = 0;
            foreach (Rule rule in entry.Rules)
                matchedLength = Math.Max(matchedLength,
                    RuleMatchLength(rule, candidate));
            if (matchedLength <= 0)
                continue;

            if (matchedLength > longest)
            {
                longest = matchedLength;
                winner = entry;
                tied = false;
            }
            else if (matchedLength == longest &&
                     !SameCondition(winner, entry))
                tied = true;
        }

        return longest > 0 && !tied && winner != null &&
               string.Equals(winner.Channel, channelName,
                   StringComparison.OrdinalIgnoreCase) &&
               string.Equals(winner.English, english,
                   StringComparison.OrdinalIgnoreCase);
    }

    private static bool ChannelsCompete(string first, string second)
    {
        int firstGroup = ChannelGroup(first);
        return firstGroup >= 0 && firstGroup == ChannelGroup(second);
    }

    private static int ChannelGroup(string channelName)
    {
        if (string.Equals(channelName, "EditEntryFromName",
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(channelName, "EditEntryIDFromName",
                StringComparison.OrdinalIgnoreCase))
            return 0;
        if (string.Equals(channelName, "EditEntryToName",
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(channelName, "EditEntryIDToName",
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(channelName, "EditEntryIDContains",
                StringComparison.OrdinalIgnoreCase))
            return 1;
        if (string.Equals(channelName, "DictEntryIs",
                StringComparison.OrdinalIgnoreCase))
            return 2;
        return -1;
    }

    private static bool SameCondition(Entry first, Entry second) =>
        first != null && second != null && first.TermId == second.TermId &&
        string.Equals(first.Channel, second.Channel,
            StringComparison.OrdinalIgnoreCase) &&
        string.Equals(first.English, second.English,
            StringComparison.OrdinalIgnoreCase);

    internal static bool RuleMatches(Rule rule, string candidate) =>
        RuleMatchLength(rule, candidate) > 0;

    internal static int RuleMatchLength(Rule rule, string candidate)
    {
        if (rule?.Values == null || rule.Values.Count == 0 || candidate == null)
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
    }
}
