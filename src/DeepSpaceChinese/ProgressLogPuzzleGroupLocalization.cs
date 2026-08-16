using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;

namespace DeepSpaceChinese;

internal static class ProgressLogPuzzleGroupLocalization
{
    internal static string Localize(UiLocalizer localizer, string original,
        IReadOnlyList<string> groupNames)
    {
        if (localizer == null || original == null || groupNames == null)
            return original;

        int headingEnd = original.IndexOf('\n');
        if (headingEnd < 0)
            return original;

        var result = new StringBuilder(original.Length + 16);
        result.Append(original, 0, headingEnd + 1);
        for (int i = 0; i < groupNames.Count; i++)
        {
            result.Append(i + 1);
            result.Append('.');
            string groupName = groupNames[i] ?? string.Empty;
            result.Append(localizer.TranslateDisplayValueLiteral(groupName));
            if (i != groupNames.Count - 1)
                result.Append(", ");
        }
        return result.ToString();
    }

    internal static string LocalizeForTests(UiLocalizer localizer, string original,
        string[] groupNames) => Localize(localizer, original, groupNames);
}

[HarmonyPatch(typeof(ProgressLog), "BuildTransmissionGroupString")]
internal static class ProgressLogBuildTransmissionGroupStringPatch
{
    [HarmonyPostfix]
    private static void Postfix(ProgressLogData __0, ref string __result)
    {
        try
        {
            PuzzleList[] lists = __0?.listsCompleted;
            if (lists == null)
                return;

            var names = new string[lists.Length];
            for (int i = 0; i < lists.Length; i++)
                names[i] = lists[i]?.PuzzleGroupName ?? string.Empty;
            DeepSpaceChinesePlugin plugin = DeepSpaceChinesePlugin.Instance;
            if (plugin != null)
                __result = plugin.LocalizeCompletedPuzzleGroups(__result, names);
        }
        catch (Exception ex)
        {
            DeepSpaceChinesePlugin.Instance?.PluginLog.LogError(
                $"翻译每周结算谜题组标题失败，将保留原始标题：\n{ex}");
        }
    }
}
