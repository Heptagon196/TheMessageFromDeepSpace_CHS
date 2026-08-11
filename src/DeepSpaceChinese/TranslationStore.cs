using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DeepSpaceChinese;

internal sealed class RuntimeTranslationFile
{
    [JsonProperty("format_version")] public int FormatVersion { get; set; }
    [JsonProperty("game_version")] public string GameVersion { get; set; }
    [JsonProperty("language")] public string Language { get; set; }
    [JsonProperty("category")] public string Category { get; set; }
    [JsonProperty("entries")] public List<RuntimeTranslationEntry> Entries { get; set; } = new();
}

internal sealed class RuntimeTranslationEntry
{
    [JsonProperty("stable_key")] public string StableKey { get; set; }
    [JsonProperty("kind")] public string Kind { get; set; }
    [JsonProperty("source_sha256")] public string SourceSha256 { get; set; }
    [JsonProperty("source_text")] public string SourceText { get; set; }
    [JsonProperty("translated_text")] public string TranslatedText { get; set; }
    [JsonProperty("game")] public JObject Game { get; set; } = new();

    public string GameString(string name, string fallback = "") =>
        Game.TryGetValue(name, StringComparison.OrdinalIgnoreCase, out JToken value)
            ? value.Type == JTokenType.Null ? fallback : value.ToString()
            : fallback;

    public int GameInt(string name, int fallback = -1) =>
        int.TryParse(GameString(name), out int value) ? value : fallback;

    public bool GameBool(string name, bool fallback = false) =>
        bool.TryParse(GameString(name), out bool value) ? value : fallback;

    public IEnumerable<KeyValuePair<string, string>> RuntimeTokens()
    {
        if (!Game.TryGetValue("runtime_tokens", StringComparison.OrdinalIgnoreCase,
                out JToken value) || value is not JObject tokens)
            yield break;
        foreach (JProperty property in tokens.Properties())
            yield return new KeyValuePair<string, string>(property.Name, property.Value.ToString());
    }
}

internal sealed class TranslationStore
{
    private readonly Dictionary<string, RuntimeTranslationEntry> _entries =
        new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<RuntimeTranslationEntry>> _uiByOriginal =
        new(StringComparer.Ordinal);
    private readonly List<RuntimeTranslationEntry> _uiTemplates = new();
    private readonly Dictionary<string, List<RuntimeTranslationEntry>> _achievementByOriginal =
        new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<RuntimeTranslationEntry>> _displayByOriginal =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly List<RuntimeTranslationEntry> _uiFragments = new();
    private readonly Dictionary<string, List<RuntimeTranslationEntry>> _hypothesesByFieldPath =
        new(StringComparer.Ordinal);

    public int Count => _entries.Count;
    public int FilesLoaded { get; private set; }
    public int LoadErrors { get; private set; }
    public IEnumerable<RuntimeTranslationEntry> Entries => _entries.Values;

    public static TranslationStore Load(string directory, ManualLogSource log)
    {
        var result = new TranslationStore();
        if (!Directory.Exists(directory))
        {
            log.LogWarning($"找不到译文目录：{directory}");
            return result;
        }
        int files = 0;
        foreach (string path in Directory.GetFiles(directory, "*.json", SearchOption.AllDirectories)
                     .OrderBy(value => value, StringComparer.OrdinalIgnoreCase))
        {
            if (string.Equals(Path.GetFileName(path), "manifest.json", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(Path.GetFileName(path), "dictionary_trigger_aliases.json",
                    StringComparison.OrdinalIgnoreCase))
                continue;
            try
            {
                RuntimeTranslationFile file = JsonConvert.DeserializeObject<RuntimeTranslationFile>(
                    File.ReadAllText(path));
                if (file == null || file.FormatVersion != 1)
                {
                    log.LogError($"跳过格式版本不支持的译文文件：{path}");
                    result.LoadErrors++;
                    continue;
                }
                files++;
                foreach (RuntimeTranslationEntry entry in file.Entries ?? Enumerable.Empty<RuntimeTranslationEntry>())
                {
                    if (string.IsNullOrWhiteSpace(entry.StableKey) || string.IsNullOrWhiteSpace(entry.TranslatedText))
                        continue;
                    if (result._entries.ContainsKey(entry.StableKey))
                    {
                        log.LogError($"重复译文键，已跳过后出现的条目：{entry.StableKey} ({path})");
                        result.LoadErrors++;
                        continue;
                    }
                    result._entries.Add(entry.StableKey, entry);
                    if (entry.Kind == "ui_text")
                    {
                        string original = entry.GameString("original_text");
                        if (!string.IsNullOrEmpty(original))
                        {
                            if (!result._uiByOriginal.TryGetValue(original, out List<RuntimeTranslationEntry> list))
                                result._uiByOriginal[original] = list = new List<RuntimeTranslationEntry>();
                            list.Add(entry);
                        }
                    }
                    else if (entry.Kind == "ui_template")
                    {
                        result._uiTemplates.Add(entry);
                    }
                    else if (entry.Kind is "achievement_name" or "achievement_description")
                    {
                        string original = entry.GameString("original_text", entry.SourceText);
                        if (!string.IsNullOrEmpty(original))
                        {
                            if (!result._achievementByOriginal.TryGetValue(original,
                                    out List<RuntimeTranslationEntry> list))
                                result._achievementByOriginal[original] = list =
                                    new List<RuntimeTranslationEntry>();
                            list.Add(entry);
                        }
                    }
                    else if (entry.Kind == "display_value")
                    {
                        AddOriginalIndex(result._displayByOriginal, entry);
                    }
                    else if (entry.Kind == "ui_fragment")
                    {
                        result._uiFragments.Add(entry);
                    }
                    else if (entry.Kind == "component_string")
                    {
                        string fieldPath = entry.GameString("field_path");
                        if (fieldPath.StartsWith("hypos[", StringComparison.Ordinal) &&
                            (fieldPath.EndsWith(".aGuess", StringComparison.Ordinal) ||
                             fieldPath.EndsWith(".bGuess", StringComparison.Ordinal) ||
                             fieldPath.EndsWith(".cGuess", StringComparison.Ordinal)))
                        {
                            if (!result._hypothesesByFieldPath.TryGetValue(fieldPath,
                                    out List<RuntimeTranslationEntry> hypotheses))
                                result._hypothesesByFieldPath[fieldPath] = hypotheses =
                                    new List<RuntimeTranslationEntry>();
                            hypotheses.Add(entry);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result.LoadErrors++;
                log.LogError($"读取译文文件失败：{path}\n{ex}");
            }
        }
        result.FilesLoaded = files;
        log.LogInfo($"已读取 {files} 个译文文件，合并 {result.Count} 条有效译文。");
        return result;
    }

    public bool TryGet(string stableKey, out RuntimeTranslationEntry entry) =>
        _entries.TryGetValue(stableKey, out entry);

    public RuntimeTranslationEntry FindUnambiguousUiFallback(string original)
    {
        if (!_uiByOriginal.TryGetValue(original, out List<RuntimeTranslationEntry> entries) || entries.Count == 0)
            return null;
        string first = entries[0].TranslatedText;
        return entries.All(entry => entry.TranslatedText == first) ? entries[0] : null;
    }

    public IEnumerable<RuntimeTranslationEntry> UiTemplates => _uiTemplates;

    public RuntimeTranslationEntry FindUnambiguousAchievement(string original)
    {
        if (!_achievementByOriginal.TryGetValue(original,
                out List<RuntimeTranslationEntry> entries) || entries.Count == 0)
            return null;
        string first = entries[0].TranslatedText;
        return entries.All(entry => entry.TranslatedText == first) ? entries[0] : null;
    }

    public RuntimeTranslationEntry FindUnambiguousDisplayValue(string original) =>
        FindUnambiguous(_displayByOriginal, original);

    public IEnumerable<KeyValuePair<string, RuntimeTranslationEntry>> DisplayValues =>
        _displayByOriginal.Select(pair =>
            new KeyValuePair<string, RuntimeTranslationEntry>(pair.Key,
                FindUnambiguous(_displayByOriginal, pair.Key)))
            .Where(pair => pair.Value != null);

    public IEnumerable<RuntimeTranslationEntry> UiFragments => _uiFragments;

    public RuntimeTranslationEntry FindUnambiguousHypothesis(string fieldPath,
        string original)
    {
        if (string.IsNullOrEmpty(fieldPath) || string.IsNullOrEmpty(original) ||
            !_hypothesesByFieldPath.TryGetValue(fieldPath,
                out List<RuntimeTranslationEntry> entries))
            return null;
        RuntimeTranslationEntry[] matches = entries.Where(entry =>
            string.Equals(entry.GameString("original_text", entry.SourceText), original,
                StringComparison.OrdinalIgnoreCase)).ToArray();
        if (matches.Length == 0)
            return null;
        string first = matches[0].TranslatedText;
        return matches.All(entry => entry.TranslatedText == first) ? matches[0] : null;
    }

    private static void AddOriginalIndex(
        IDictionary<string, List<RuntimeTranslationEntry>> index,
        RuntimeTranslationEntry entry)
    {
        string original = entry.GameString("original_text", entry.SourceText);
        if (string.IsNullOrEmpty(original))
            return;
        if (!index.TryGetValue(original, out List<RuntimeTranslationEntry> entries))
            index[original] = entries = new List<RuntimeTranslationEntry>();
        entries.Add(entry);
    }

    private static RuntimeTranslationEntry FindUnambiguous(
        IReadOnlyDictionary<string, List<RuntimeTranslationEntry>> index, string original)
    {
        if (!index.TryGetValue(original, out List<RuntimeTranslationEntry> entries) ||
            entries.Count == 0)
            return null;
        string first = entries[0].TranslatedText;
        return entries.All(entry => entry.TranslatedText == first) ? entries[0] : null;
    }
}
