using System;
using System.Reflection;
using HarmonyLib;

namespace DeepSpaceChinese;

internal static class ManualJournalLogWindowSync
{
    private static readonly FieldInfo LogWindowField =
        AccessTools.Field(typeof(DialogueBank), "logWindow");

    internal static bool ShouldAppendForTests(
        bool alreadyLogged,
        bool hasEntry,
        int dialogueType,
        bool hasWindow)
    {
        return !alreadyLogged &&
               hasEntry &&
               dialogueType == (int)DialogueType.journalEntries &&
               hasWindow;
    }

    internal static void AppendIfNeeded(
        DialogueBank dialogueBank,
        DialoguePlayInfo playInfo,
        bool alreadyLogged)
    {
        DialogueChunk chunk = playInfo?.dc;
        if (dialogueBank == null || chunk == null)
            return;

        DialogueEntryData entry = dialogueBank.GetLogEntry(chunk);
        var logWindow = LogWindowField?.GetValue(dialogueBank) as LogWindow;
        if (!ShouldAppendForTests(
                alreadyLogged,
                !ReferenceEquals(entry, null),
                (int)chunk.DialogueType,
                logWindow != null))
        {
            return;
        }

        logWindow.AddLog(entry, chunk);
    }
}

[HarmonyPatch(typeof(DialogueBank), nameof(DialogueBank.ManualLogDialogueEntry))]
internal static class DialogueBankManualJournalLogSyncPatch
{
    [HarmonyPrefix]
    private static void Prefix(DialogueBank __instance, DialoguePlayInfo __0, out bool __state)
    {
        DialogueChunk chunk = __0?.dc;
        __state = __instance != null &&
                  chunk != null &&
                  __instance.HasPlayedDialogue(chunk);
    }

    [HarmonyPostfix]
    private static void Postfix(DialogueBank __instance, DialoguePlayInfo __0, bool __state)
    {
        try
        {
            ManualJournalLogWindowSync.AppendIfNeeded(__instance, __0, __state);
        }
        catch (Exception ex)
        {
            DeepSpaceChinesePlugin.Instance?.PluginLog.LogError(
                $"同步手记到当前日志窗口失败；手记仍已保存在存档中：\n{ex}");
        }
    }
}
