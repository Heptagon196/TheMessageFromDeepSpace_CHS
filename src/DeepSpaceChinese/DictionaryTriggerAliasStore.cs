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
        foreach (Entry entry in _entries)
        {
            if (!string.Equals(entry.Channel, channelName,
                    StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(entry.English, english,
                    StringComparison.OrdinalIgnoreCase) ||
                (entry.TermId.HasValue && entry.TermId.Value != termId))
                continue;
            foreach (Rule rule in entry.Rules)
            {
                if (RuleMatches(rule, candidate))
                    return true;
            }
        }
        return false;
    }

    internal static bool RuleMatches(Rule rule, string candidate)
    {
        if (rule?.Values == null || rule.Values.Count == 0 || candidate == null)
            return false;
        string type = rule.Type?.Trim() ?? string.Empty;
        if (string.Equals(type, "contains_all", StringComparison.OrdinalIgnoreCase))
        {
            foreach (string value in rule.Values)
            {
                if (string.IsNullOrEmpty(value) ||
                    candidate.IndexOf(value, StringComparison.OrdinalIgnoreCase) < 0)
                    return false;
            }
            return true;
        }
        foreach (string value in rule.Values)
        {
            if (string.IsNullOrEmpty(value))
                continue;
            if (string.Equals(type, "contains", StringComparison.OrdinalIgnoreCase))
            {
                if (candidate.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            else if (string.Equals(type, "exact", StringComparison.OrdinalIgnoreCase) &&
                     string.Equals(candidate.Trim(), value.Trim(),
                         StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
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
