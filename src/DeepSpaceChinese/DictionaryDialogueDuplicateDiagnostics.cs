using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace DeepSpaceChinese;

/// <summary>
/// Diagnostics only: never suppresses, reorders, or resolves dialogue triggers.
/// Build-time validation is responsible for making the alias table unambiguous.
/// </summary>
internal static class DictionaryDialogueDuplicateDiagnostics
{
    private static readonly List<DialogueChunk> Triggered = new();
    private static bool _tracking;
    private static int _termId;
    private static string _from;
    private static string _to;

    internal static void Begin(int termId, string from, string to)
    {
        _tracking = true;
        _termId = termId;
        _from = from ?? string.Empty;
        _to = to ?? string.Empty;
        Triggered.Clear();
    }

    internal static void Record(DialogueChunk chunk)
    {
        if (!_tracking || chunk == null || Triggered.Any(item => item == chunk))
            return;
        Triggered.Add(chunk);
    }

    internal static void End()
    {
        try
        {
            if (Triggered.Count <= 1)
                return;
            string chunks = string.Join(", ", Triggered.Select(chunk =>
                $"{chunk.UniqueID}:{chunk.name}"));
            DeepSpaceChinesePlugin.Instance?.PluginLog.LogError(
                "[词典对白触发冲突] 同一次词典命名实际请求了多段不同对白；" +
                $"term_id={_termId}，原名=\"{_from}\"，新名=\"{_to}\"，对白={chunks}。" +
                "运行时未拦截；请修复构建配置。");
        }
        finally
        {
            _tracking = false;
            Triggered.Clear();
        }
    }
}

[HarmonyPatch(typeof(AdvancedObserver), "OnDictionaryWordRenamed",
    new[] { typeof(int), typeof(string), typeof(string) })]
internal static class DictionaryRenameDiagnosticScopePatch
{
    [HarmonyPrefix]
    private static void Prefix(int id, string fromWord, string toWord) =>
        DictionaryDialogueDuplicateDiagnostics.Begin(id, fromWord, toWord);

    [HarmonyPostfix]
    private static void Postfix() => DictionaryDialogueDuplicateDiagnostics.End();

    [HarmonyFinalizer]
    private static void Finalizer() => DictionaryDialogueDuplicateDiagnostics.End();
}

[HarmonyPatch(typeof(DialogueManager), nameof(DialogueManager.PlayDialogueChunk))]
internal static class DictionaryDialogueTriggerDiagnosticPatch
{
    [HarmonyPrefix]
    private static void Prefix(DialogueChunk dc) =>
        DictionaryDialogueDuplicateDiagnostics.Record(dc);
}
