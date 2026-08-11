using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace DeepSpaceChinese;

internal static class TermLoggerLayoutEngine
{
    internal const float BottomViewportY = 0.12f;
    internal const float FallbackSpacing = 0.045f;

    internal static Vector3 TargetViewportPoint(Vector3 original, int index,
        int count, float spacing)
    {
        int safeCount = Math.Max(1, count);
        int safeIndex = Math.Max(0, Math.Min(index, safeCount - 1));
        float safeSpacing = spacing > 0f ? spacing : FallbackSpacing;
        return new Vector3(original.x,
            BottomViewportY + (safeCount - 1 - safeIndex) * safeSpacing,
            original.z);
    }

    internal static float MeasureSpacing(IReadOnlyList<Vector3> orderedViewportPoints)
    {
        if (orderedViewportPoints == null || orderedViewportPoints.Count < 2)
            return FallbackSpacing;
        var gaps = new List<float>(orderedViewportPoints.Count - 1);
        for (int index = 1; index < orderedViewportPoints.Count; index++)
        {
            float gap = orderedViewportPoints[index - 1].y - orderedViewportPoints[index].y;
            if (gap > 0.001f)
                gaps.Add(gap);
        }
        if (gaps.Count == 0)
            return FallbackSpacing;
        gaps.Sort();
        return gaps[gaps.Count / 2];
    }
}

internal static class TermLoggerLayoutRuntime
{
    private static readonly Dictionary<int, OriginalEntry> OriginalPositions = new();

    internal static void ApplyCurrentLists()
    {
        List<TermLogger> activeLoggers = Resources.FindObjectsOfTypeAll<TermLogger>()
            .Where(logger => logger != null && logger.gameObject.scene.IsValid() &&
                             logger.gameObject.activeInHierarchy && logger.transform.parent != null)
            .ToList();
        var activeIds = new HashSet<int>(activeLoggers.Select(logger => logger.GetInstanceID()));
        foreach (int staleId in OriginalPositions.Keys.Where(id => !activeIds.Contains(id)).ToList())
            OriginalPositions.Remove(staleId);

        IEnumerable<IGrouping<int, TermLogger>> groups = activeLoggers
            .GroupBy(logger => logger.transform.parent.GetInstanceID());
        foreach (IGrouping<int, TermLogger> group in groups)
            ApplyList(group);
    }

    private static void ApplyList(IEnumerable<TermLogger> loggers)
    {
        List<TermLogger> loggerList = loggers.ToList();
        if (loggerList.Count == 0)
            return;
        Camera camera = FindRenderingCamera(loggerList[0]);
        if (camera == null)
        {
            DeepSpaceChinesePlugin.Instance?.PluginLog.LogWarning(
                "[NewWordPrompt] 找不到显示新词弹窗的 RenderTexture 摄像机，已保留原位置。");
            return;
        }

        var entries = loggerList
            .Select(logger => CreateEntry(camera, logger))
            .Where(entry => entry.Viewport.z > 0f)
            .OrderByDescending(entry => entry.Viewport.y)
            .ToList();
        if (entries.Count == 0)
            return;

        float spacing = TermLoggerLayoutEngine.MeasureSpacing(
            entries.Select(entry => entry.Viewport).ToList());
        for (int index = 0; index < entries.Count; index++)
        {
            Entry entry = entries[index];
            Vector3 target = TermLoggerLayoutEngine.TargetViewportPoint(
                entry.Viewport, index, entries.Count, spacing);
            entry.Logger.transform.position = camera.ViewportToWorldPoint(target);
        }
        DeepSpaceChinesePlugin.Instance?.PluginLog.LogInfo(
            $"[NewWordPrompt] 已在 {camera.name}/{camera.targetTexture?.name ?? "<screen>"} " +
            $"内移动 {entries.Count} 项到右下角，底部 Y={TermLoggerLayoutEngine.BottomViewportY:F3}。");
    }

    private static Camera FindRenderingCamera(TermLogger logger)
    {
        if (logger == null)
            return null;
        int layerMask = 1 << logger.gameObject.layer;
        Vector3 original = OriginalPosition(logger);
        return Resources.FindObjectsOfTypeAll<Camera>()
            .Where(camera => camera != null && camera.gameObject.scene.IsValid() &&
                             camera.gameObject.activeInHierarchy && camera.enabled &&
                             camera.targetTexture != null &&
                             (camera.cullingMask & layerMask) != 0)
            .Select(camera => new
            {
                Camera = camera,
                Viewport = camera.WorldToViewportPoint(original),
            })
            .Where(candidate => candidate.Viewport.z > 0f &&
                                candidate.Viewport.x >= 0f && candidate.Viewport.x <= 1f &&
                                candidate.Viewport.y >= 0f && candidate.Viewport.y <= 1f)
            .OrderByDescending(candidate => string.Equals(
                candidate.Camera.targetTexture.name, "RT IO Monitor",
                StringComparison.OrdinalIgnoreCase))
            .Select(candidate => candidate.Camera)
            .FirstOrDefault();
    }

    private static Vector3 OriginalPosition(TermLogger logger)
    {
        int instanceId = logger.GetInstanceID();
        if (OriginalPositions.TryGetValue(instanceId, out OriginalEntry original) &&
            ReferenceEquals(original.Logger, logger))
            return original.Position;
        return logger.transform.position;
    }

    private static Entry CreateEntry(Camera camera, TermLogger logger)
    {
        int instanceId = logger.GetInstanceID();
        if (!OriginalPositions.TryGetValue(instanceId, out OriginalEntry original) ||
            !ReferenceEquals(original.Logger, logger))
        {
            original = new OriginalEntry(logger, logger.transform.position);
            OriginalPositions[instanceId] = original;
        }
        return new Entry(logger, camera.WorldToViewportPoint(original.Position));
    }

    internal static void RestoreCurrentLists()
    {
        foreach (TermLogger logger in Resources.FindObjectsOfTypeAll<TermLogger>())
        {
            if (logger == null || !logger.gameObject.scene.IsValid())
                continue;
            if (OriginalPositions.TryGetValue(logger.GetInstanceID(), out OriginalEntry original) &&
                ReferenceEquals(original.Logger, logger))
                logger.transform.position = original.Position;
        }
        OriginalPositions.Clear();
    }

    private readonly struct Entry
    {
        public readonly TermLogger Logger;
        public readonly Vector3 Viewport;

        public Entry(TermLogger logger, Vector3 viewport)
        {
            Logger = logger;
            Viewport = viewport;
        }
    }

    private readonly struct OriginalEntry
    {
        public readonly TermLogger Logger;
        public readonly Vector3 Position;

        public OriginalEntry(TermLogger logger, Vector3 position)
        {
            Logger = logger;
            Position = position;
        }
    }
}

[HarmonyPatch(typeof(ConsoleDisplay), "HandleNewWordCaught")]
internal static class ConsoleDisplayNewWordLayoutPatch
{
    private static void Postfix()
    {
        if (DeepSpaceChinesePlugin.Instance?.MoveNewWordPromptToLowerRightEnabled == true)
            TermLoggerLayoutRuntime.ApplyCurrentLists();
    }
}

[HarmonyPatch(typeof(TermLogger), nameof(TermLogger.Configure))]
internal static class TermLoggerConfigureLocalizationPatch
{
    private static readonly FieldInfo LabelField =
        AccessTools.Field(typeof(WriteableButton), "label");

    [HarmonyPostfix]
    private static void Postfix(TermLogger __instance, int signal)
    {
        try
        {
            TMP_Text label = LabelField?.GetValue(__instance) as TMP_Text;
            DeepSpaceChinesePlugin.Instance?.ApplyTermLoggerPrompt(label, signal);
        }
        catch (Exception ex)
        {
            DeepSpaceChinesePlugin.Instance?.PluginLog.LogError(
                $"翻译新单词命名浮窗失败：\n{ex}");
        }
    }
}
