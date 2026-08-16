using System;
using System.Collections;
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
    private static readonly FieldInfo DialogueTextDumpField =
        AccessTools.Field(typeof(LogWindow), "dialogueTextDump");

    private readonly DialogueLocalizer _dialogue;
    private readonly FontFallback _font;
    private readonly PatchConfig _config;
    private readonly ManualLogSource _log;
    private readonly Dictionary<LogWindow, DialogueChunk> _openDetails = new();
    private readonly Dictionary<int, TMP_Text> _openReplayBodies = new();

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
        var body = DialogueTextDumpField?.GetValue(window) as TMP_Text;
        if (body != null)
        {
            _openReplayBodies[body.GetInstanceID()] = body;
            string normalized = NormalizeReplayBodyForDisplay(body.text, _config.DisplayMode);
            if (!string.Equals(normalized, body.text, StringComparison.Ordinal))
                body.text = normalized;
        }
        ApplyOpenBodyLayout(window);
    }

    public bool IsReplayBody(TMP_Text component)
    {
        if (component == null ||
            !_openReplayBodies.TryGetValue(component.GetInstanceID(), out TMP_Text tracked))
            return false;
        if (tracked != null && ReferenceEquals(tracked, component))
            return true;
        _openReplayBodies.Remove(component.GetInstanceID());
        return false;
    }

    internal static string NormalizeReplayBodyForDisplay(string text, DisplayMode mode) =>
        DialogueChineseTypography.ShouldNormalize(mode, false, true)
            ? DialogueChineseTypography.Normalize(text)
            : text;

    private static void ApplyOpenBodyLayout(LogWindow window)
    {
        var body = DialogueTextDumpField?.GetValue(window) as TMP_Text;
        if (body == null)
            return;
        body.richText = true;
        body.textWrappingMode = TextWrappingModes.Normal;
        body.overflowMode = TextOverflowModes.Overflow;
        body.maxVisibleLines = int.MaxValue;
        body.maxVisibleCharacters = int.MaxValue;
        body.maxVisibleWords = int.MaxValue;
    }

    public void ForgetOpen(LogWindow window)
    {
        if (window == null)
            return;
        var body = DialogueTextDumpField?.GetValue(window) as TMP_Text;
        if (body != null)
            _openReplayBodies.Remove(body.GetInstanceID());
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
                if (window != null)
                    ForgetOpen(window);
                _openDetails.Remove(window);
                continue;
            }
            var dialogueView = DialogueViewField?.GetValue(window) as GameObject;
            if (dialogueView == null || !dialogueView.activeSelf)
            {
                ForgetOpen(window);
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

internal static class LogWindowBodyLayoutRuntime
{
    private static readonly FieldInfo DialogueTextDumpField =
        AccessTools.Field(typeof(LogWindow), "dialogueTextDump");
    private static readonly FieldInfo DisplayScrollField =
        AccessTools.Field(typeof(LogWindow), "displayScroll");
    private static readonly FieldInfo DisplayAreaField =
        AccessTools.Field(typeof(LogWindow), "displayArea");
    private static readonly FieldInfo LinesPerDisplayPageField =
        AccessTools.Field(typeof(LogWindow), "linesPerDisplayPage");
    private static readonly FieldInfo BonusLinesPaddingField =
        AccessTools.Field(typeof(LogWindow), "bonusLinesPadding");
    private static readonly FieldInfo SingleLineHeightField =
        AccessTools.Field(typeof(LogWindow), "singleLineHeight");

    internal static IEnumerator ReconfigureAfterTextLayout(LogWindow window, IEnumerator original)
    {
        while (original != null && original.MoveNext())
            yield return original.Current;

        // OpenDialogue's original coroutine measures one frame after assigning text.
        // Font fallback and localized wrapping may settle on that same frame, so wait
        // once more and rebuild the mesh before deriving the scrollable height.
        yield return null;
        if (window == null)
            yield break;

        var body = DialogueTextDumpField?.GetValue(window) as TMP_Text;
        var scroll = DisplayScrollField?.GetValue(window) as ScrollBar3D;
        var area = DisplayAreaField?.GetValue(window) as ScrollArea;
        if (body == null || scroll == null || area == null)
            yield break;

        body.richText = true;
        body.textWrappingMode = TextWrappingModes.Normal;
        body.overflowMode = TextOverflowModes.Overflow;
        body.maxVisibleLines = int.MaxValue;
        body.maxVisibleCharacters = int.MaxValue;
        body.maxVisibleWords = int.MaxValue;
        body.ForceMeshUpdate(ignoreActiveState: true, forceTextReparsing: true);

        int lineCount = Math.Max(1, body.textInfo?.lineCount ?? 1) + 2;
        int padding = Math.Max(0, (int)(BonusLinesPaddingField?.GetValue(window) ?? 5));
        int pageLines = Math.Max(1, (int)(LinesPerDisplayPageField?.GetValue(window) ?? 1));
        float lineHeight = (float)(SingleLineHeightField?.GetValue(window) ?? 0f);
        if (lineHeight <= 0f)
        {
            var rect = body.GetComponent<RectTransform>();
            lineHeight = body.transform.lossyScale.y * (rect?.sizeDelta.y ?? 0f) + 0.0013f;
        }
        if (lineHeight <= 0f)
            yield break;

        float preferredBodyWorldHeight =
            Math.Max(0f, body.preferredHeight) * Math.Abs(body.transform.lossyScale.y) +
            2f * lineHeight;
        LogWindowScrollMetrics metrics = CalculateForTests(
            lineCount, padding, pageLines, lineHeight, preferredBodyWorldHeight);
        area.Configure(scroll, metrics.WorldHeight, metrics.ScreenHeight);
        scroll.ConfigureHeight(metrics.RelativeHeight);
    }

    internal static LogWindowScrollMetrics CalculateForTests(int lineCount, int padding,
        int pageLines, float lineHeight, float preferredBodyWorldHeight = 0f)
    {
        int bodyLines = Math.Max(1, lineCount);
        int paddingLines = Math.Max(0, padding);
        int visibleLines = Math.Max(1, pageLines);
        float safeLineHeight = Math.Max(0f, lineHeight);
        float paddingWorldHeight = paddingLines * safeLineHeight;
        float estimatedBodyWorldHeight = bodyLines * safeLineHeight;
        float worldHeight = Math.Max(
            estimatedBodyWorldHeight,
            Math.Max(0f, preferredBodyWorldHeight)) + paddingWorldHeight;
        float screenHeight = visibleLines * safeLineHeight;
        float relativeHeight = screenHeight > 0f
            ? worldHeight / screenHeight
            : (bodyLines + paddingLines) / (float)visibleLines;
        return new LogWindowScrollMetrics(worldHeight, screenHeight, relativeHeight);
    }
}

internal readonly struct LogWindowScrollMetrics
{
    internal LogWindowScrollMetrics(float worldHeight, float screenHeight, float relativeHeight)
    {
        WorldHeight = worldHeight;
        ScreenHeight = screenHeight;
        RelativeHeight = relativeHeight;
    }

    internal float WorldHeight { get; }
    internal float ScreenHeight { get; }
    internal float RelativeHeight { get; }
}

[HarmonyPatch(typeof(LogWindow), "DisplayScrollConfigureRoutine")]
internal static class LogWindowDisplayScrollConfigureRoutinePatch
{
    [HarmonyPostfix]
    private static void Postfix(LogWindow __instance, ref IEnumerator __result)
    {
        __result = LogWindowBodyLayoutRuntime.ReconfigureAfterTextLayout(__instance, __result);
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
