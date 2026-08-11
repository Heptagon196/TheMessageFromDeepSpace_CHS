using System;
using System.Collections.Generic;
using System.IO;
using BepInEx.Logging;
using HarmonyLib;
using Newtonsoft.Json;

namespace DeepSpaceChinese;

internal sealed class PuzzleFixPlan
{
    public int[] ReplacementSignals { get; set; }
    public int[][] ReplacementAnswers { get; set; }
}

internal sealed class PuzzleFixRule
{
    [JsonProperty("display_id", Required = Required.Always)]
    public int DisplayId { get; set; }

    [JsonProperty("original_signals")]
    public int[] OriginalSignals { get; set; }

    [JsonProperty("replacement_signals")]
    public int[] ReplacementSignals { get; set; }

    [JsonProperty("original_answers")]
    public int[][] OriginalAnswers { get; set; }

    [JsonProperty("replacement_answers")]
    public int[][] ReplacementAnswers { get; set; }

    [JsonProperty("note")]
    public string Note { get; set; }

    [JsonIgnore]
    internal bool HasQuestionReplacement =>
        OriginalSignals?.Length > 0 && ReplacementSignals?.Length > 0;

    [JsonIgnore]
    internal bool HasAnswerReplacement =>
        OriginalAnswers?.Length > 0 && ReplacementAnswers?.Length > 0;

    internal bool Matches(int[] signals) => SignalsEqual(OriginalSignals, signals);

    internal bool TryCreatePlan(int[] currentSignals, int[][] currentAnswers,
        out PuzzleFixPlan plan, out string error)
    {
        plan = null;
        error = null;
        if (HasQuestionReplacement && !Matches(currentSignals))
        {
            error = "修正文件中的原题面与游戏数据不一致";
            return false;
        }
        if (HasAnswerReplacement && !AnswerSetsEqual(OriginalAnswers, currentAnswers))
        {
            error = "修正文件中的原始答案集与游戏数据不一致";
            return false;
        }
        plan = new PuzzleFixPlan
        {
            ReplacementSignals = HasQuestionReplacement
                ? CloneSignals(ReplacementSignals)
                : null,
            ReplacementAnswers = HasAnswerReplacement
                ? CloneAnswerSet(ReplacementAnswers)
                : null,
        };
        return true;
    }

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
        bool hasOriginalSignals = rule.OriginalSignals?.Length > 0;
        bool hasReplacementSignals = rule.ReplacementSignals?.Length > 0;
        if (hasOriginalSignals != hasReplacementSignals)
        {
            error = "original_signals 和 replacement_signals 必须同时非空或同时省略";
            return false;
        }
        bool hasOriginalAnswers = rule.OriginalAnswers?.Length > 0;
        bool hasReplacementAnswers = rule.ReplacementAnswers?.Length > 0;
        if (hasOriginalAnswers != hasReplacementAnswers)
        {
            error = "答案集字段 original_answers 和 replacement_answers " +
                    "必须同时非空或同时省略";
            return false;
        }
        if (!rule.HasQuestionReplacement && !rule.HasAnswerReplacement)
        {
            error = "题面修正和答案集修正至少需要提供一组非空数据";
            return false;
        }
        if (rule.HasQuestionReplacement &&
            (rule.OriginalSignals.Length > 100000 ||
             rule.ReplacementSignals.Length > 100000))
        {
            error = "信号数量超过安全上限";
            return false;
        }
        if (rule.HasAnswerReplacement &&
            (!IsValidAnswerSet(rule.OriginalAnswers) ||
             !IsValidAnswerSet(rule.ReplacementAnswers)))
        {
            error = "答案集不能为空，且其中每个答案都必须包含至少一个数字信号";
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

    private static bool IsValidAnswerSet(int[][] answers)
    {
        if (answers == null || answers.Length == 0)
            return false;
        foreach (int[] answer in answers)
            if (answer == null || answer.Length == 0)
                return false;
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

    internal static bool AnswerSetsEqual(int[][] left, int[][] right)
    {
        if (ReferenceEquals(left, right))
            return true;
        if (left == null || right == null || left.Length != right.Length)
            return false;
        for (int index = 0; index < left.Length; index++)
            if (!SignalsEqual(left[index], right[index]))
                return false;
        return true;
    }

    internal static int[] CloneSignals(int[] signals) =>
        signals == null ? Array.Empty<int>() : (int[])signals.Clone();

    internal static int[][] CloneAnswerSet(int[][] answers)
    {
        if (answers == null)
            return null;
        var clone = new int[answers.Length][];
        for (int index = 0; index < answers.Length; index++)
            clone[index] = CloneSignals(answers[index]);
        return clone;
    }
}

internal sealed class PuzzleFixRuntime
{
    private sealed class PuzzleState
    {
        public int[] Rock;
        public int[] Winning;
        public int[][] Alternatives;
        public bool AllowAlternatives;

        public int[][] AcceptedAnswers()
        {
            int alternativeCount = AllowAlternatives ? Alternatives.Length : 0;
            var answers = new int[1 + alternativeCount][];
            answers[0] = PuzzleFixRule.CloneSignals(Winning);
            for (int index = 0; index < alternativeCount; index++)
                answers[index + 1] = PuzzleFixRule.CloneSignals(Alternatives[index]);
            return answers;
        }
    }

    private sealed class AppliedFix
    {
        public PuzzleState Original;
        public PuzzleState Replacement;
    }

    private static readonly System.Reflection.FieldInfo AlternativeResponsesField =
        AccessTools.Field(typeof(Puzzle), "altResponses");
    private static readonly System.Reflection.FieldInfo AllowAlternativeResponsesField =
        AccessTools.Field(typeof(Puzzle), "allowAltResponses");

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
            _log.LogInfo($"题目修正目录不存在，未加载规则：{_directory}");
            return;
        }

        string[] paths;
        try
        {
            paths = Directory.GetFiles(_directory, "*.json", SearchOption.TopDirectoryOnly);
        }
        catch (Exception ex)
        {
            _log.LogWarning($"无法枚举题目修正目录，未加载规则：{ex.Message}");
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
                    _log.LogWarning($"忽略题目修正文件 {fileName}：{error}");
                    continue;
                }
                if (_rules.ContainsKey(rule.DisplayId))
                {
                    _log.LogWarning($"忽略重复的题目修正文件 {fileName}：显示编号 " +
                                    $"{rule.DisplayId} 已有规则。");
                    continue;
                }
                _rules.Add(rule.DisplayId, rule);
            }
            catch (Exception ex)
            {
                _log.LogWarning($"读取题目修正文件 {fileName} 失败：{ex.Message}");
            }
        }
        _log.LogInfo($"已读取 {_rules.Count} 条题目修正规则。");
    }

    public bool ApplyAll(PuzzleManager manager)
    {
        RestoreAll();
        if (_config?.Enabled != true || !_config.PuzzleFixesEnabled || manager == null ||
            AlternativeResponsesField == null || AllowAlternativeResponsesField == null)
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
                    bool applied = TryApply(puzzle, displayId, rule,
                        out bool questionChanged);
                    if (applied && questionChanged &&
                        ReferenceEquals(puzzle, manager.CurrPuzzle))
                        currentPuzzleChanged = true;
                }
                displayId++;
            }
        }
        return currentPuzzleChanged;
    }

    public void RestoreAll()
    {
        if (AlternativeResponsesField == null || AllowAlternativeResponsesField == null)
        {
            _applied.Clear();
            return;
        }
        foreach (KeyValuePair<Puzzle, AppliedFix> pair in _applied)
        {
            Puzzle puzzle = pair.Key;
            if (puzzle == null)
                continue;
            PuzzleState current = CaptureState(puzzle);
            if (StatesEqual(current, pair.Value.Replacement))
                ApplyState(puzzle, pair.Value.Original);
        }
        _applied.Clear();
    }

    private bool TryApply(Puzzle puzzle, int displayId, PuzzleFixRule rule,
        out bool questionChanged)
    {
        questionChanged = false;
        PuzzleState original = CaptureState(puzzle);
        int[][] currentAnswers = original.AcceptedAnswers();
        if (!rule.TryCreatePlan(original.Rock, currentAnswers,
                out PuzzleFixPlan plan, out string error))
        {
            _log.LogWarning($"第 {displayId} 题未应用修正：{error}。" +
                            (rule.HasQuestionReplacement
                                ? $"\n期望题面：{Format(rule.OriginalSignals)}" +
                                  $"\n实际题面：{Format(original.Rock)}"
                                : string.Empty) +
                            (rule.HasAnswerReplacement
                                ? $"\n期望答案集：{FormatAnswers(rule.OriginalAnswers)}" +
                                  $"\n实际答案集：{FormatAnswers(currentAnswers)}"
                                : string.Empty));
            return false;
        }

        PuzzleState replacement = BuildReplacementState(original, plan);
        ApplyState(puzzle, replacement);
        questionChanged = rule.HasQuestionReplacement;
        _applied[puzzle] = new AppliedFix
        {
            Original = original,
            Replacement = replacement,
        };
        string fixKind = rule.HasQuestionReplacement
            ? (rule.HasAnswerReplacement ? "题面与答案集修正" : "题面修正")
            : "答案集修正";
        _log.LogInfo($"已应用第 {displayId} 题{fixKind}" +
                     (string.IsNullOrWhiteSpace(rule.Note) ? "。" : $"：{rule.Note}"));
        return true;
    }

    private static PuzzleState CaptureState(Puzzle puzzle)
    {
        var alternatives = AlternativeResponsesField.GetValue(puzzle) as SignalMessage[] ??
                           Array.Empty<SignalMessage>();
        var alternativeSignals = new int[alternatives.Length][];
        for (int index = 0; index < alternatives.Length; index++)
            alternativeSignals[index] = PuzzleFixRule.CloneSignals(alternatives[index].signals);
        return new PuzzleState
        {
            Rock = PuzzleFixRule.CloneSignals(puzzle.RockOutput.signals),
            Winning = PuzzleFixRule.CloneSignals(puzzle.WinningResponse.signals),
            Alternatives = alternativeSignals,
            AllowAlternatives = (bool)AllowAlternativeResponsesField.GetValue(puzzle),
        };
    }

    private static PuzzleState BuildReplacementState(PuzzleState original, PuzzleFixPlan plan)
    {
        if (plan.ReplacementAnswers == null)
        {
            return new PuzzleState
            {
                Rock = plan.ReplacementSignals == null
                    ? PuzzleFixRule.CloneSignals(original.Rock)
                    : PuzzleFixRule.CloneSignals(plan.ReplacementSignals),
                Winning = PuzzleFixRule.CloneSignals(original.Winning),
                Alternatives = PuzzleFixRule.CloneAnswerSet(original.Alternatives),
                AllowAlternatives = original.AllowAlternatives,
            };
        }

        int alternativeCount = plan.ReplacementAnswers.Length - 1;
        var alternatives = new int[alternativeCount][];
        for (int index = 0; index < alternativeCount; index++)
            alternatives[index] = PuzzleFixRule.CloneSignals(
                plan.ReplacementAnswers[index + 1]);
        return new PuzzleState
        {
            Rock = plan.ReplacementSignals == null
                ? PuzzleFixRule.CloneSignals(original.Rock)
                : PuzzleFixRule.CloneSignals(plan.ReplacementSignals),
            Winning = PuzzleFixRule.CloneSignals(plan.ReplacementAnswers[0]),
            Alternatives = alternatives,
            AllowAlternatives = alternativeCount > 0,
        };
    }

    private static void ApplyState(Puzzle puzzle, PuzzleState state)
    {
        var alternatives = new SignalMessage[state.Alternatives.Length];
        for (int index = 0; index < alternatives.Length; index++)
            alternatives[index] = ToMessage(state.Alternatives[index]);
        puzzle.OverridePuzzle(ToMessage(state.Rock), ToMessage(state.Winning), alternatives,
            state.AllowAlternatives);
    }

    private static SignalMessage ToMessage(int[] signals) =>
        new() { signals = PuzzleFixRule.CloneSignals(signals) };

    private static bool StatesEqual(PuzzleState left, PuzzleState right) =>
        left != null && right != null &&
        left.AllowAlternatives == right.AllowAlternatives &&
        PuzzleFixRule.SignalsEqual(left.Rock, right.Rock) &&
        PuzzleFixRule.SignalsEqual(left.Winning, right.Winning) &&
        PuzzleFixRule.AnswerSetsEqual(left.Alternatives, right.Alternatives);

    private static string Format(int[] signals) =>
        signals == null ? "<null>" : string.Join(" ", signals);

    private static string FormatAnswers(int[][] answers)
    {
        if (answers == null)
            return "<null>";
        var formatted = new string[answers.Length];
        for (int index = 0; index < answers.Length; index++)
            formatted[index] = "[" + Format(answers[index]) + "]";
        return string.Join(" | ", formatted);
    }
}

[HarmonyPatch(typeof(PuzzleManager), "Start")]
internal static class PuzzleManagerStartFixPatch
{
    [HarmonyPostfix]
    private static void Postfix(PuzzleManager __instance) =>
        DeepSpaceChinesePlugin.Instance?.ApplyPuzzleFixes(__instance);
}
