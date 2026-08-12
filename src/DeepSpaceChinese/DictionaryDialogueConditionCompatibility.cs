using System;
using HarmonyLib;

namespace DeepSpaceChinese;

internal static class DictionaryDialogueConditionMatcher
{
    public static bool Matches(string expected, string candidate, bool contains)
    {
        if (expected == null || candidate == null)
            return false;
        return contains
            ? candidate.IndexOf(expected, StringComparison.OrdinalIgnoreCase) >= 0
            : string.Equals(candidate, expected, StringComparison.OrdinalIgnoreCase);
    }
}

internal static class DictionaryDialogueConditionCompatibility
{
    public static bool TryMatch(ListenerCondition condition, UiLocalizer ui,
        DictionaryTriggerAliasStore aliases)
    {
        if (condition == null || ui == null ||
            !TryGetCandidate(condition, out int termId, out string candidate,
                out bool contains))
            return false;

        // The stock game compares these values case-sensitively. Preserve its result first,
        // then add the intended case-insensitive English compatibility here.
        if (DictionaryDialogueConditionMatcher.Matches(condition.strValue, candidate, contains))
            return true;

        // Chinese aliases are an additional fallback only. The stock English
        // condition and its case-insensitive compatibility check above always
        // remain active, even if this optional data file is absent or invalid.
        if (aliases != null && aliases.Matches(termId, condition.listenChannel,
                condition.strValue, candidate))
            return true;

        return false;
    }

    private static bool TryGetCandidate(ListenerCondition condition, out int termId,
        out string candidate, out bool contains)
    {
        termId = 0;
        candidate = null;
        contains = false;
        AdvancedObserver observer = AdvancedObserver.Instance;
        switch (condition.listenChannel)
        {
            case ListenChannel.EditEntryFromName:
                if (observer == null)
                    return false;
                termId = observer.EditEntryID;
                candidate = observer.EditEntryFromName;
                return true;
            case ListenChannel.EditEntryToName:
                if (observer == null)
                    return false;
                termId = observer.EditEntryID;
                candidate = observer.EditEntryToName;
                return true;
            case ListenChannel.EditEntryIDToName:
                if (observer == null || observer.EditEntryID != (int)condition.value)
                    return false;
                termId = (int)condition.value;
                candidate = observer.EditEntryToName;
                return true;
            case ListenChannel.EditEntryIDFromName:
                if (observer == null || observer.EditEntryID != (int)condition.value)
                    return false;
                termId = (int)condition.value;
                candidate = observer.EditEntryFromName;
                return true;
            case ListenChannel.DictEntryIs:
            case ListenChannel.EditEntryIDContains:
                termId = (int)condition.value;
                if (UserDictionary.Instance?.terms == null ||
                    !UserDictionary.Instance.terms.TryGetValue(termId, out candidate))
                    return false;
                contains = condition.listenChannel == ListenChannel.EditEntryIDContains;
                return true;
            default:
                return false;
        }
    }
}

[HarmonyPatch(typeof(ListenerCondition), nameof(ListenerCondition.MetCon))]
internal static class ListenerConditionDictionaryCompatibilityPatch
{
    [HarmonyPostfix]
    private static void Postfix(ListenerCondition __instance, ref bool __result)
    {
        if (__result)
            return;
        DeepSpaceChinesePlugin plugin = DeepSpaceChinesePlugin.Instance;
        if (plugin != null)
            __result = plugin.TryMatchDictionaryDialogueCondition(__instance);
    }
}
