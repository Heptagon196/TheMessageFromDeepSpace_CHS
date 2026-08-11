using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace DeepSpaceChinese;

internal sealed class LogTitleRuntime
{
    private static readonly FieldInfo DialogueNameLabelField =
        AccessTools.Field(typeof(DialogueLogEntry), "dialogueNameLabel");
    private static readonly FieldInfo ChunkField = AccessTools.Field(typeof(DialogueLogEntry), "chunk");
    private static readonly FieldInfo LogWindowField = AccessTools.Field(typeof(DialogueLogEntry), "lw");
    private static readonly FieldInfo LogNameLimitField = AccessTools.Field(typeof(LogWindow), "logNameLimit");
    private static readonly FieldInfo LogOverflowTextField =
        AccessTools.Field(typeof(LogWindow), "logOverflowText");
    private static readonly FieldInfo OpenTitleLabelField =
        AccessTools.Field(typeof(LogWindow), "logTitleLabel");
    private static readonly FieldInfo DialogueViewField =
        AccessTools.Field(typeof(LogWindow), "dialogueView");

    private readonly DialogueLocalizer _dialogue;
    private readonly FontFallback _font;
    private readonly PatchConfig _config;
    private readonly ManualLogSource _log;
    private readonly Dictionary<LogWindow, DialogueChunk> _openDetails = new();

    public LogTitleRuntime(DialogueLocalizer dialogue, FontFallback font, PatchConfig config,
        ManualLogSource log)
    {
        _dialogue = dialogue;
        _font = font;
        _config = config;
        _log = log;
    }

    public void ApplyEntry(DialogueLogEntry entry, DialogueChunk chunk, LogWindow window)
    {
        if (entry == null || chunk == null)
            return;
        TMP_Text label = (TMP_Text)DialogueNameLabelField?.GetValue(entry);
        if (label == null)
            return;
        string title = _dialogue.ResolveLogTitle(chunk);
        if (window != null)
        {
            int limit = (int)(LogNameLimitField?.GetValue(window) ?? int.MaxValue);
            string overflow = (string)LogOverflowTextField?.GetValue(window) ?? "...";
            title = TruncateForTests(title, limit, overflow);
        }
        _font.ApplyDirect(label,
            InterfaceFontPolicy.ShouldUseDirectLogTitleFont(title, _config.DisplayMode));
        label.text = title;
    }

    public void ApplyOpenTitle(LogWindow window, DialogueChunk chunk)
    {
        if (window == null || chunk == null)
            return;
        _openDetails[window] = chunk;
        TMP_Text label = (TMP_Text)OpenTitleLabelField?.GetValue(window);
        if (label != null)
        {
            string title = _dialogue.ResolveLogTitle(chunk);
            _font.ApplyDirect(label,
                InterfaceFontPolicy.ShouldUseDirectLogTitleFont(title, _config.DisplayMode));
            label.text = title;
        }
    }

    public void ForgetOpen(LogWindow window)
    {
        if (window != null)
            _openDetails.Remove(window);
    }

    public void RefreshAll()
    {
        int count = 0;
        foreach (DialogueLogEntry entry in Resources.FindObjectsOfTypeAll<DialogueLogEntry>())
        {
            if (entry == null)
                continue;
            var chunk = (DialogueChunk)ChunkField?.GetValue(entry);
            if (chunk == null)
                continue;
            var window = (LogWindow)LogWindowField?.GetValue(entry);
            ApplyEntry(entry, chunk, window);
            count++;
        }
        int openCount = 0;
        foreach (KeyValuePair<LogWindow, DialogueChunk> pair in
                 new List<KeyValuePair<LogWindow, DialogueChunk>>(_openDetails))
        {
            LogWindow window = pair.Key;
            DialogueChunk chunk = pair.Value;
            if (window == null || chunk == null)
            {
                _openDetails.Remove(window);
                continue;
            }
            var dialogueView = DialogueViewField?.GetValue(window) as GameObject;
            if (dialogueView == null || !dialogueView.activeSelf)
            {
                _openDetails.Remove(window);
                continue;
            }
            window.OpenDialogue(chunk);
            openCount++;
        }
        if (count > 0)
            _log.LogInfo($"日志列表标题已刷新：{count} 项。");
        if (openCount > 0)
            _log.LogInfo($"当前日志详情已按语言模式重新生成：{openCount} 项。");
    }

    internal static string TruncateForTests(string title, int limit, string overflow)
    {
        title ??= string.Empty;
        overflow ??= string.Empty;
        if (limit < 0 || title.Length < limit)
            return title;
        return title.Substring(0, limit) + overflow;
    }
}

[HarmonyPatch(typeof(DialogueLogEntry), "Configure",
    new[] { typeof(DialogueChunk), typeof(LogWindow) })]
internal static class DialogueLogEntryConfigureSimplePatch
{
    [HarmonyPostfix]
    private static void Postfix(DialogueLogEntry __instance, DialogueChunk __0, LogWindow __1)
    {
        try
        {
            DeepSpaceChinesePlugin.Instance?.ApplyLogEntryTitle(__instance, __0, __1);
        }
        catch (Exception ex)
        {
            DeepSpaceChinesePlugin.Instance?.PluginLog.LogError($"应用日志列表标题翻译失败：\n{ex}");
        }
    }
}

[HarmonyPatch(typeof(DialogueLogEntry), "Configure",
    new[] { typeof(DialogueChunk), typeof(LogWindow), typeof(string), typeof(bool) })]
internal static class DialogueLogEntryConfigureListPatch
{
    [HarmonyPostfix]
    private static void Postfix(DialogueLogEntry __instance, DialogueChunk __0, LogWindow __1)
    {
        try
        {
            DeepSpaceChinesePlugin.Instance?.ApplyLogEntryTitle(__instance, __0, __1);
        }
        catch (Exception ex)
        {
            DeepSpaceChinesePlugin.Instance?.PluginLog.LogError($"应用日志列表标题翻译失败：\n{ex}");
        }
    }
}

[HarmonyPatch(typeof(LogWindow), "OpenDialogue")]
internal static class LogWindowOpenDialogueTitlePatch
{
    [HarmonyPostfix]
    private static void Postfix(LogWindow __instance, DialogueChunk dc)
    {
        try
        {
            DeepSpaceChinesePlugin.Instance?.ApplyOpenLogTitle(__instance, dc);
        }
        catch (Exception ex)
        {
            DeepSpaceChinesePlugin.Instance?.PluginLog.LogError($"应用日志详情标题翻译失败：\n{ex}");
        }
    }
}

[HarmonyPatch(typeof(LogWindow), "CloseDialogue")]
internal static class LogWindowCloseDialogueTitlePatch
{
    [HarmonyPostfix]
    private static void Postfix(LogWindow __instance)
    {
        DeepSpaceChinesePlugin.Instance?.ForgetOpenLog(__instance);
    }
}
