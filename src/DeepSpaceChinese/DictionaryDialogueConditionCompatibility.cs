using System;
using HarmonyLib;
using UnityEngine;

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

        // Hypothesis component fields may already contain Chinese after UI localization.
        // Use only their term ID/index here and resolve the original/translation pair from
        // the translation store, so aliases remain exact and F5-reloadable.
        foreach (DictionaryHypotheses hypotheses in
                 Resources.FindObjectsOfTypeAll<DictionaryHypotheses>())
        {
            if (hypotheses?.hypos == null)
                continue;
            for (int index = 0; index < hypotheses.hypos.Length; index++)
            {
                if (hypotheses.hypos[index].termID != termId)
                    continue;
                if (MatchesTranslatedGuess(ui, index, "aGuess", condition.strValue,
                        candidate, contains) ||
                    MatchesTranslatedGuess(ui, index, "bGuess", condition.strValue,
                        candidate, contains) ||
                    MatchesTranslatedGuess(ui, index, "cGuess", condition.strValue,
                        candidate, contains))
                    return true;
            }
        }
        return false;
    }

    private static bool MatchesTranslatedGuess(UiLocalizer ui, int index,
        string guessField, string expectedEnglish, string candidate, bool contains) =>
        ui.TryResolveHypothesisTranslation(index, guessField, expectedEnglish,
            out string translated) &&
        DictionaryDialogueConditionMatcher.Matches(translated, candidate, contains);

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
