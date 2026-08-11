using System;
using System.Reflection;
using HarmonyLib;

namespace DeepSpaceChinese;

internal static class ConsoleOutputScrollPadding
{
    internal const int ExtraLines = 3;

    internal static float AddToWorldHeight(float worldHeight, float lineHeight, bool enabled) =>
        enabled && lineHeight > 0f
            ? worldHeight + lineHeight * ExtraLines
            : worldHeight;

    internal static float AddToRelativeMenuHeight(float relativeMenuHeight, float lineHeight,
        float totalDisplayHeight, bool enabled) =>
        enabled && lineHeight > 0f && totalDisplayHeight > 0f
            ? relativeMenuHeight + lineHeight * ExtraLines / totalDisplayHeight
            : relativeMenuHeight;
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

    internal static void AdjustScrollbar(ScrollBar3D scrollbar, ref float relativeMenuHeight)
    {
        if (!TryGetContext(out ScrollBar3D outputScroll, out _, out float lineHeight,
                out float totalDisplayHeight) || !ReferenceEquals(scrollbar, outputScroll))
            return;
        relativeMenuHeight = ConsoleOutputScrollPadding.AddToRelativeMenuHeight(
            relativeMenuHeight, lineHeight, totalDisplayHeight, enabled: true);
    }

    internal static void AdjustScrollArea(ScrollArea scrollArea, ref float worldHeight)
    {
        if (!TryGetContext(out _, out ScrollArea outputScrollArea, out float lineHeight,
                out _) || !ReferenceEquals(scrollArea, outputScrollArea))
            return;
        worldHeight = ConsoleOutputScrollPadding.AddToWorldHeight(
            worldHeight, lineHeight, enabled: true);
    }

    private static bool TryGetContext(out ScrollBar3D outputScroll,
        out ScrollArea outputScrollArea, out float lineHeight, out float totalDisplayHeight)
    {
        outputScroll = null;
        outputScrollArea = null;
        lineHeight = 0f;
        totalDisplayHeight = 0f;
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
