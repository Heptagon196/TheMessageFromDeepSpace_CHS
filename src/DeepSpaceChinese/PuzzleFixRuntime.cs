using System;
using System.Collections.Generic;
using System.IO;
using BepInEx.Logging;
using HarmonyLib;
using Newtonsoft.Json;

namespace DeepSpaceChinese;

internal sealed class PuzzleFixRule
{
    [JsonProperty("display_id", Required = Required.Always)]
    public int DisplayId { get; set; }

    [JsonProperty("original_signals", Required = Required.Always)]
    public int[] OriginalSignals { get; set; }

    [JsonProperty("replacement_signals", Required = Required.Always)]
    public int[] ReplacementSignals { get; set; }

    [JsonProperty("note")]
    public string Note { get; set; }

    internal bool Matches(int[] signals) => SignalsEqual(OriginalSignals, signals);

    internal static bool TryParse(string json, string fileName, out PuzzleFixRule rule,
        out string error)
    {
        rule = null;
        error = null;
        try
        {
            rule = JsonConvert.DeserializeObject<PuzzleFixRule>(json,
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
        if (rule.DisplayId <= 0)
        {
            error = "display_id 必须是从 1 开始的游戏显示编号";
            return false;
        }
        if (rule.OriginalSignals == null || rule.OriginalSignals.Length == 0)
        {
            error = "original_signals 不能为空";
            return false;
        }
        if (rule.ReplacementSignals == null || rule.ReplacementSignals.Length == 0)
        {
            error = "replacement_signals 不能为空";
            return false;
        }
        if (rule.OriginalSignals.Length > 100000 || rule.ReplacementSignals.Length > 100000)
        {
            error = "信号数量超过安全上限";
            return false;
        }

        string stem = Path.GetFileNameWithoutExtension(fileName ?? string.Empty);
        if (!int.TryParse(stem, out int fileDisplayId) || fileDisplayId != rule.DisplayId)
        {
            error = $"文件名必须是显示编号；当前文件名 '{fileName}' 与 display_id " +
                    $"{rule.DisplayId} 不一致";
            return false;
        }
        return true;
    }

    internal static bool SignalsEqual(int[] left, int[] right)
    {
        if (ReferenceEquals(left, right))
            return true;
        if (left == null || right == null || left.Length != right.Length)
            return false;
        for (int index = 0; index < left.Length; index++)
            if (left[index] != right[index])
                return false;
        return true;
    }
}

internal sealed class PuzzleFixRuntime
{
    private sealed class AppliedFix
    {
        public int[] Original;
        public int[] Replacement;
    }

    private static readonly System.Reflection.FieldInfo RockOutputField =
        AccessTools.Field(typeof(Puzzle), "rockOutput");

    private readonly PatchConfig _config;
    private readonly string _directory;
    private readonly ManualLogSource _log;
    private readonly Dictionary<int, PuzzleFixRule> _rules = new();
    private readonly Dictionary<Puzzle, AppliedFix> _applied = new();

    public PuzzleFixRuntime(PatchConfig config, string directory, ManualLogSource log)
    {
        _config = config;
        _directory = directory;
        _log = log;
    }

    public void ReloadRules()
    {
        RestoreAll();
        _rules.Clear();
        if (!Directory.Exists(_directory))
        {
            _log.LogInfo($"题面修正目录不存在，未加载规则：{_directory}");
            return;
        }

        string[] paths;
        try
        {
            paths = Directory.GetFiles(_directory, "*.json", SearchOption.TopDirectoryOnly);
        }
        catch (Exception ex)
        {
            _log.LogWarning($"无法枚举题面修正目录，未加载规则：{ex.Message}");
            return;
        }
        Array.Sort(paths, StringComparer.OrdinalIgnoreCase);

        foreach (string path in paths)
        {
            string fileName = Path.GetFileName(path);
            try
            {
                if (!PuzzleFixRule.TryParse(File.ReadAllText(path), fileName,
                        out PuzzleFixRule rule, out string error))
                {
                    _log.LogWarning($"忽略题面修正文件 {fileName}：{error}");
                    continue;
                }
                if (_rules.ContainsKey(rule.DisplayId))
                {
                    _log.LogWarning($"忽略重复的题面修正文件 {fileName}：显示编号 " +
                                    $"{rule.DisplayId} 已有规则。");
                    continue;
                }
                _rules.Add(rule.DisplayId, rule);
            }
            catch (Exception ex)
            {
                _log.LogWarning($"读取题面修正文件 {fileName} 失败：{ex.Message}");
            }
        }
        _log.LogInfo($"已读取 {_rules.Count} 条题面修正规则。");
    }

    public bool ApplyAll(PuzzleManager manager)
    {
        RestoreAll();
        if (_config?.Enabled != true || !_config.PuzzleFixesEnabled || manager == null ||
            RockOutputField == null)
            return false;

        bool currentPuzzleChanged = false;
        int displayId = 1;
        PuzzleList[] lists = manager.PuzzleLists ?? Array.Empty<PuzzleList>();
        foreach (PuzzleList list in lists)
        {
            Puzzle[] puzzles = list?.Puzzles ?? Array.Empty<Puzzle>();
            foreach (Puzzle puzzle in puzzles)
            {
                if (puzzle != null && _rules.TryGetValue(displayId, out PuzzleFixRule rule))
                {
                    bool applied = TryApply(puzzle, displayId, rule);
                    if (applied && ReferenceEquals(puzzle, manager.CurrPuzzle))
                        currentPuzzleChanged = true;
                }
                displayId++;
            }
        }
        return currentPuzzleChanged;
    }

    public void RestoreAll()
    {
        if (RockOutputField == null)
        {
            _applied.Clear();
            return;
        }
        foreach (KeyValuePair<Puzzle, AppliedFix> pair in _applied)
        {
            Puzzle puzzle = pair.Key;
            if (puzzle == null)
                continue;
            int[] current = puzzle.RockOutput.signals;
            if (PuzzleFixRule.SignalsEqual(current, pair.Value.Replacement))
                SetRockOutput(puzzle, pair.Value.Original);
        }
        _applied.Clear();
    }

    private bool TryApply(Puzzle puzzle, int displayId, PuzzleFixRule rule)
    {
        int[] current = puzzle.RockOutput.signals;
        if (!rule.Matches(current))
        {
            _log.LogWarning($"第 {displayId} 题未应用修正：修正文件中的原题面与游戏数据不一致。" +
                            $"\n期望：{Format(rule.OriginalSignals)}" +
                            $"\n实际：{Format(current)}");
            return false;
        }

        int[] original = Clone(current);
        int[] replacement = Clone(rule.ReplacementSignals);
        SetRockOutput(puzzle, replacement);
        _applied[puzzle] = new AppliedFix
        {
            Original = original,
            Replacement = replacement,
        };
        _log.LogInfo($"已应用第 {displayId} 题题面修正" +
                     (string.IsNullOrWhiteSpace(rule.Note) ? "。" : $"：{rule.Note}"));
        return true;
    }

    private static void SetRockOutput(Puzzle puzzle, int[] signals)
    {
        RockOutputField.SetValue(puzzle, new SignalMessage { signals = Clone(signals) });
    }

    private static int[] Clone(int[] signals) =>
        signals == null ? Array.Empty<int>() : (int[])signals.Clone();

    private static string Format(int[] signals) =>
        signals == null ? "<null>" : string.Join(" ", signals);
}

[HarmonyPatch(typeof(PuzzleManager), "Start")]
internal static class PuzzleManagerStartFixPatch
{
    [HarmonyPostfix]
    private static void Postfix(PuzzleManager __instance) =>
        DeepSpaceChinesePlugin.Instance?.ApplyPuzzleFixes(__instance);
}
