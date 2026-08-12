using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using Newtonsoft.Json;
using UnityEngine;

namespace DeepSpaceChinese;

internal sealed class DictionaryDialogueFixRule
{
    [JsonProperty("dialogue_chunk_id", Required = Required.Always)]
    public int DialogueChunkId { get; set; }

    [JsonProperty("channel", Required = Required.Always)]
    public string Channel { get; set; }

    [JsonProperty("english", Required = Required.Always)]
    public string English { get; set; }

    [JsonProperty("original_term_id", Required = Required.Always)]
    public int OriginalTermId { get; set; }

    [JsonProperty("replacement_term_id", Required = Required.Always)]
    public int ReplacementTermId { get; set; }

    [JsonProperty("note")]
    public string Note { get; set; }

    [JsonIgnore]
    public ListenChannel ParsedChannel { get; private set; }

    internal static bool TryParse(string json, string fileName,
        out DictionaryDialogueFixRule rule, out string error)
    {
        rule = null;
        error = null;
        try
        {
            rule = JsonConvert.DeserializeObject<DictionaryDialogueFixRule>(json,
                new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Error,
                });
        }
        catch (Exception ex)
        {
            error = "JSON 解析失败：" + ex.Message;
            return false;
        }
        if (rule == null)
        {
            error = "文件内容为空";
            return false;
        }
        if (rule.DialogueChunkId <= 0)
        {
            error = "dialogue_chunk_id 必须是正整数";
            return false;
        }
        if (!int.TryParse(Path.GetFileNameWithoutExtension(fileName), out int fileId) ||
            fileId != rule.DialogueChunkId)
        {
            error = "文件名必须与 dialogue_chunk_id 一致";
            return false;
        }
        if (!Enum.TryParse(rule.Channel, true, out ListenChannel parsedChannel))
        {
            error = $"未知监听频道：{rule.Channel}";
            return false;
        }
        if (string.IsNullOrWhiteSpace(rule.English))
        {
            error = "english 不能为空";
            return false;
        }
        if (rule.OriginalTermId == rule.ReplacementTermId)
        {
            error = "original_term_id 与 replacement_term_id 不能相同";
            return false;
        }
        rule.Channel = parsedChannel.ToString();
        rule.English = rule.English.Trim();
        rule.ParsedChannel = parsedChannel;
        return true;
    }
}

internal sealed class DictionaryDialogueFixRuntime
{
    private static readonly System.Reflection.FieldInfo DialogueChunksField =
        AccessTools.Field(typeof(AdvancedListener), "dc");

    private readonly string _directory;
    private readonly ManualLogSource _log;
    private readonly List<DictionaryDialogueFixRule> _rules = new();
    private readonly Dictionary<ListenerCondition, float> _applied = new();
    private readonly HashSet<string> _reportedMismatches = new(StringComparer.Ordinal);

    internal DictionaryDialogueFixRuntime(string directory, ManualLogSource log)
    {
        _directory = directory;
        _log = log;
    }

    internal void ReloadRules()
    {
        RestoreAll();
        _rules.Clear();
        _reportedMismatches.Clear();
        if (!Directory.Exists(_directory))
        {
            _log.LogInfo($"词典对白修正目录不存在，未加载规则：{_directory}");
            return;
        }
        foreach (string path in Directory.GetFiles(_directory, "*.json",
                     SearchOption.TopDirectoryOnly).OrderBy(item => item,
                     StringComparer.OrdinalIgnoreCase))
        {
            string fileName = Path.GetFileName(path);
            try
            {
                if (!DictionaryDialogueFixRule.TryParse(File.ReadAllText(path), fileName,
                        out DictionaryDialogueFixRule rule, out string error))
                {
                    _log.LogWarning($"忽略词典对白修正文件 {fileName}：{error}");
                    continue;
                }
                if (_rules.Any(item => item.DialogueChunkId == rule.DialogueChunkId &&
                    item.ParsedChannel == rule.ParsedChannel &&
                    string.Equals(item.English, rule.English,
                        StringComparison.OrdinalIgnoreCase)))
                {
                    _log.LogWarning($"忽略重复的词典对白修正规则：{fileName}");
                    continue;
                }
                _rules.Add(rule);
            }
            catch (Exception ex)
            {
                _log.LogWarning($"读取词典对白修正文件 {fileName} 失败：{ex.Message}");
            }
        }
        _log.LogInfo($"已读取 {_rules.Count} 条词典对白条件修正规则。");
    }

    internal void ApplyAll()
    {
        foreach (AdvancedListener listener in Resources.FindObjectsOfTypeAll<AdvancedListener>())
            Apply(listener);
    }

    internal void Apply(AdvancedListener listener)
    {
        if (listener == null || DialogueChunksField == null || _rules.Count == 0)
            return;
        var chunks = DialogueChunksField.GetValue(listener) as DialogueChunk[] ??
                     Array.Empty<DialogueChunk>();
        foreach (DictionaryDialogueFixRule rule in _rules)
        {
            if (!chunks.Any(chunk => chunk != null && chunk.UniqueID == rule.DialogueChunkId))
                continue;
            ListenerCondition[] candidates = (listener.conditions ??
                Array.Empty<ListenerCondition>()).Where(condition =>
                    condition != null && condition.listenChannel == rule.ParsedChannel &&
                    string.Equals(condition.strValue, rule.English,
                        StringComparison.OrdinalIgnoreCase)).ToArray();
            ListenerCondition original = candidates.Length == 1 &&
                (int)candidates[0].value == rule.OriginalTermId
                    ? candidates[0]
                    : null;
            if (original != null)
            {
                if (!_applied.ContainsKey(original))
                    _applied.Add(original, original.value);
                original.value = rule.ReplacementTermId;
                _log.LogInfo($"已修正词典对白 {rule.DialogueChunkId} 的条件：" +
                             $"{rule.Channel} {rule.English}，词条 " +
                             $"{rule.OriginalTermId} -> {rule.ReplacementTermId}。");
                continue;
            }
            if (candidates.Length == 1 &&
                (int)candidates[0].value == rule.ReplacementTermId)
                continue;
            string key = listener.GetInstanceID() + ":" + rule.DialogueChunkId;
            if (_reportedMismatches.Add(key))
            {
                string actual = string.Join(", ", candidates.Select(condition =>
                    $"{condition.listenChannel}/{condition.strValue}/{(int)condition.value}"));
                _log.LogError($"词典对白 {rule.DialogueChunkId} 未应用条件修正：" +
                              "原始条件与修正文件不一致；" +
                              $"期望 {rule.Channel}/{rule.English}/{rule.OriginalTermId}，" +
                              $"实际 [{actual}]。已保留游戏原数据。");
            }
        }
    }

    internal void RestoreAll()
    {
        foreach (KeyValuePair<ListenerCondition, float> pair in _applied)
            if (pair.Key != null)
                pair.Key.value = pair.Value;
        _applied.Clear();
    }
}

[HarmonyPatch(typeof(AdvancedListener), nameof(AdvancedListener.Activate))]
internal static class DictionaryDialogueListenerFixPatch
{
    [HarmonyPrefix]
    private static void Prefix(AdvancedListener __instance, bool on)
    {
        if (on)
            DeepSpaceChinesePlugin.Instance?.ApplyDictionaryDialogueFix(__instance);
    }
}
