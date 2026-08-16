using System;
using System.Reflection;
using HarmonyLib;
using TMPro;

namespace DeepSpaceChinese;

internal static class ConsoleOutputScrollPadding
{
    internal const int ExtraLines = 3;

    internal static float AddToWorldHeight(float worldHeight, float lineHeight, bool enabled) =>
        AddToWorldHeight(worldHeight, lineHeight, 0f, 0, enabled);

    internal static float AddToWorldHeight(float worldHeight, float lineHeight,
        float renderedContentHeight, int renderedLineCount, bool enabled) =>
        enabled
            ? worldHeight + CalculatePadding(lineHeight, renderedContentHeight,
                renderedLineCount)
            : worldHeight;

    internal static float AddToRelativeMenuHeight(float relativeMenuHeight, float lineHeight,
        float totalDisplayHeight, bool enabled) =>
        AddToRelativeMenuHeight(relativeMenuHeight, lineHeight, 0f, 0,
            totalDisplayHeight, enabled);

    internal static float AddToRelativeMenuHeight(float relativeMenuHeight, float lineHeight,
        float renderedContentHeight, int renderedLineCount, float totalDisplayHeight,
        bool enabled) =>
        enabled && totalDisplayHeight > 0f
            ? relativeMenuHeight + CalculatePadding(lineHeight, renderedContentHeight,
                renderedLineCount) / totalDisplayHeight
            : relativeMenuHeight;

    private static float CalculatePadding(float nominalLineHeight,
        float renderedContentHeight, int renderedLineCount)
    {
        if (nominalLineHeight <= 0f)
            return 0f;
        if (renderedContentHeight <= 0f || renderedLineCount <= 0)
            return nominalLineHeight * ExtraLines;

        float renderedLineHeight = renderedContentHeight / renderedLineCount;
        float effectiveLineHeight = Math.Max(nominalLineHeight, renderedLineHeight);
        float accumulatedShortfall = Math.Max(0f,
            renderedContentHeight - nominalLineHeight * renderedLineCount);
        return accumulatedShortfall + effectiveLineHeight * ExtraLines;
    }
}

internal static class ConsoleOutputScrollPaddingRuntime
{
    private static readonly FieldInfo OutputScrollField =
        AccessTools.Field(typeof(ConsoleDisplay), "outputScroll");
    private static readonly FieldInfo OutputScrollAreaField =
        AccessTools.Field(typeof(ConsoleDisplay), "outputScrollArea");
    private static readonly FieldInfo LineHeightField =
        AccessTools.Field(typeof(ConsoleDisplay), "lineHeight");
    private static readonly FieldInfo TotalDisplayHeightField =
        AccessTools.Field(typeof(ConsoleDisplay), "totalDisplayHeight");
    private static readonly FieldInfo DisplayField =
        AccessTools.Field(typeof(ConsoleDisplay), "display");

    internal static void AdjustScrollbar(ScrollBar3D scrollbar, ref float relativeMenuHeight)
    {
        if (!TryGetContext(out ScrollBar3D outputScroll, out _, out float lineHeight,
                out float totalDisplayHeight, out float renderedContentHeight,
                out int renderedLineCount) || !ReferenceEquals(scrollbar, outputScroll))
            return;
        relativeMenuHeight = ConsoleOutputScrollPadding.AddToRelativeMenuHeight(
            relativeMenuHeight, lineHeight, renderedContentHeight, renderedLineCount,
            totalDisplayHeight, enabled: true);
    }

    internal static void AdjustScrollArea(ScrollArea scrollArea, ref float worldHeight)
    {
        if (!TryGetContext(out _, out ScrollArea outputScrollArea, out float lineHeight,
                out _, out float renderedContentHeight, out int renderedLineCount) ||
            !ReferenceEquals(scrollArea, outputScrollArea))
            return;
        worldHeight = ConsoleOutputScrollPadding.AddToWorldHeight(
            worldHeight, lineHeight, renderedContentHeight, renderedLineCount,
            enabled: true);
    }

    private static bool TryGetContext(out ScrollBar3D outputScroll,
        out ScrollArea outputScrollArea, out float lineHeight, out float totalDisplayHeight,
        out float renderedContentHeight, out int renderedLineCount)
    {
        outputScroll = null;
        outputScrollArea = null;
        lineHeight = 0f;
        totalDisplayHeight = 0f;
        renderedContentHeight = 0f;
        renderedLineCount = 0;
        if (DeepSpaceChinesePlugin.Instance?.MoveNewWordPromptToLowerRightEnabled != true)
            return false;
        ConsoleDisplay console = ConsoleDisplay.Instance;
        if (console == null || OutputScrollField == null || OutputScrollAreaField == null ||
            LineHeightField == null || TotalDisplayHeightField == null)
            return false;
        outputScroll = OutputScrollField.GetValue(console) as ScrollBar3D;
        outputScrollArea = OutputScrollAreaField.GetValue(console) as ScrollArea;
        lineHeight = (float)LineHeightField.GetValue(console);
        totalDisplayHeight = (float)TotalDisplayHeightField.GetValue(console);
        TMP_Text display = DisplayField?.GetValue(console) as TMP_Text;
        if (display != null)
        {
            renderedContentHeight = display.preferredHeight;
            renderedLineCount = display.textInfo?.lineCount ?? 0;
            if (float.IsNaN(renderedContentHeight) ||
                float.IsInfinity(renderedContentHeight))
                renderedContentHeight = 0f;
        }
        return outputScroll != null && outputScrollArea != null;
    }
}

[HarmonyPatch(typeof(ScrollBar3D), nameof(ScrollBar3D.ConfigureHeight),
    new[] { typeof(float), typeof(bool) })]
internal static class ConsoleOutputScrollbarHeightPatch
{
    [HarmonyPrefix]
    private static void Prefix(ScrollBar3D __instance, ref float relativeMenuHeight) =>
        ConsoleOutputScrollPaddingRuntime.AdjustScrollbar(__instance, ref relativeMenuHeight);
}

[HarmonyPatch(typeof(ScrollArea), nameof(ScrollArea.Configure),
    new[] { typeof(ScrollBar3D), typeof(float), typeof(float) })]
internal static class ConsoleOutputScrollAreaHeightPatch
{
    [HarmonyPrefix]
    private static void Prefix(ScrollArea __instance, ref float worldHeight) =>
        ConsoleOutputScrollPaddingRuntime.AdjustScrollArea(__instance, ref worldHeight);
}
