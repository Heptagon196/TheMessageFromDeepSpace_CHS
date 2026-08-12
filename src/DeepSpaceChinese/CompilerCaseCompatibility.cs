using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace DeepSpaceChinese;

internal static class CompilerCaseCompatibility
{
    public static bool TryResolve(string input, IEnumerable<KeyValuePair<string, int>> entries,
        out int signal) => TryResolve(input, entries, true, false, out signal);

    public static bool TryResolve(string input, IEnumerable<KeyValuePair<string, int>> entries,
        bool ignoreCase, bool ignorePunctuation, out int signal)
    {
        signal = default;
        if (input == null || entries == null)
            return false;

        KeyValuePair<string, int>[] candidates = entries
            .Where(pair => Equivalent(pair.Key, input, ignoreCase, ignorePunctuation))
            .ToArray();
        foreach (KeyValuePair<string, int> candidate in candidates)
        {
            if (!string.Equals(candidate.Key, input, StringComparison.Ordinal))
                continue;
            signal = candidate.Value;
            return true;
        }
        if (candidates.Length != 1)
            return false;
        signal = candidates[0].Value;
        return true;
    }

    public static string NormalizeForReformatter(string input, IEnumerable<string> dictionaryKeys)
        => NormalizeForReformatter(input, dictionaryKeys, true, false);

    public static string NormalizeForReformatter(string input, IEnumerable<string> dictionaryKeys,
        bool ignoreCase, bool ignorePunctuation)
    {
        if (string.IsNullOrEmpty(input) || dictionaryKeys == null)
            return input;

        IGrouping<int, string>[] groups = dictionaryKeys
            .Where(key => !string.IsNullOrEmpty(key))
            .Distinct(StringComparer.Ordinal)
            .GroupBy(key => key.Length)
            .OrderByDescending(group => group.Key)
            .ToArray();
        if (groups.Length == 0)
            return input;

        char[] result = input.ToCharArray();
        var claimed = new bool[input.Length];
        foreach (IGrouping<int, string> group in groups)
        {
            int length = group.Key;
            for (int index = 0; index <= input.Length - length; index++)
            {
                if (IsClaimed(claimed, index, length))
                    continue;
                string[] candidates = group
                    .Where(key => RegionEquals(input, index, key, ignoreCase,
                        ignorePunctuation))
                    .ToArray();
                if (candidates.Length == 0)
                    continue;
                string resolved = candidates.FirstOrDefault(key =>
                    RegionEquals(input, index, key, false, false));
                if (resolved == null && candidates.Length == 1)
                    resolved = candidates[0];
                if (resolved == null)
                    continue;
                for (int offset = 0; offset < length; offset++)
                {
                    result[index + offset] = resolved[offset];
                    claimed[index + offset] = true;
                }
                index += length - 1;
            }
        }
        return new string(result);
    }

    public static bool Equivalent(string left, string right, bool ignoreCase,
        bool ignorePunctuation)
    {
        if (left == null || right == null || left.Length != right.Length)
            return false;
        for (int index = 0; index < left.Length; index++)
        {
            char leftChar = Canonicalize(left[index], ignorePunctuation);
            char rightChar = Canonicalize(right[index], ignorePunctuation);
            if (ignoreCase)
            {
                leftChar = char.ToUpperInvariant(leftChar);
                rightChar = char.ToUpperInvariant(rightChar);
            }
            if (leftChar != rightChar)
                return false;
        }
        return true;
    }

    private static bool IsClaimed(bool[] claimed, int index, int length)
    {
        for (int offset = 0; offset < length; offset++)
            if (claimed[index + offset])
                return true;
        return false;
    }

    private static bool RegionEquals(string input, int index, string key, bool ignoreCase,
        bool ignorePunctuation)
    {
        for (int offset = 0; offset < key.Length; offset++)
        {
            char inputChar = Canonicalize(input[index + offset], ignorePunctuation);
            char keyChar = Canonicalize(key[offset], ignorePunctuation);
            if (ignoreCase)
            {
                inputChar = char.ToUpperInvariant(inputChar);
                keyChar = char.ToUpperInvariant(keyChar);
            }
            if (inputChar != keyChar)
                return false;
        }
        return true;
    }

    private static char Canonicalize(char value, bool enabled)
    {
        if (!enabled)
            return value;
        return value switch
        {
            '，' => ',',
            '。' => '.',
            '；' => ';',
            '（' => '(',
            '）' => ')',
            '【' => '[',
            '】' => ']',
            _ => value,
        };
    }
}

[HarmonyPatch(typeof(C_Reformatter), nameof(C_Reformatter.CompileStringToSignal))]
internal static class ReformatterCaseCompatibilityPatch
{
    private static void Prefix(ref string input)
    {
        DeepSpaceChinesePlugin plugin = DeepSpaceChinesePlugin.Instance;
        bool ignoreCase = plugin?.CompilerCaseInsensitiveEnabled == true;
        bool ignorePunctuation = plugin?.CompilerPunctuationInsensitiveEnabled == true;
        if ((!ignoreCase && !ignorePunctuation) || UserDictionary.Instance?.keys == null)
            return;
        input = CompilerCaseCompatibility.NormalizeForReformatter(input,
            UserDictionary.Instance.keys.Keys, ignoreCase, ignorePunctuation);
    }
}

[HarmonyPatch(typeof(C_WordCatcher), "TryGetSignalFromWord")]
internal static class WordCatcherCaseCompatibilityPatch
{
    private static bool Prefix(string word, ref int signal, ref bool __result)
    {
        DeepSpaceChinesePlugin plugin = DeepSpaceChinesePlugin.Instance;
        bool ignoreCase = plugin?.CompilerCaseInsensitiveEnabled == true;
        bool ignorePunctuation = plugin?.CompilerPunctuationInsensitiveEnabled == true;
        if ((!ignoreCase && !ignorePunctuation) || UserDictionary.Instance?.keys == null ||
            UserDictionary.Instance.keys.ContainsKey(word))
            return true;

        if (!CompilerCaseCompatibility.TryResolve(word, UserDictionary.Instance.keys,
                ignoreCase, ignorePunctuation, out int resolvedSignal))
            return true;
        signal = resolvedSignal;
        __result = true;
        return false;
    }
}

internal static class DictionaryNameConflictCompatibility
{
    public static bool HasConflict(string candidate, IEnumerable<KeyValuePair<string, int>> entries,
        int? currentSignal, bool ignoreCase, bool ignorePunctuation)
    {
        if (candidate == null || entries == null)
            return false;
        return entries.Any(pair =>
            (!currentSignal.HasValue || pair.Value != currentSignal.Value) &&
            CompilerCaseCompatibility.Equivalent(pair.Key, candidate, ignoreCase,
                ignorePunctuation));
    }

    public static bool Enabled(out bool ignoreCase, out bool ignorePunctuation)
    {
        DeepSpaceChinesePlugin plugin = DeepSpaceChinesePlugin.Instance;
        ignoreCase = plugin?.CompilerCaseInsensitiveEnabled == true;
        ignorePunctuation = plugin?.CompilerPunctuationInsensitiveEnabled == true;
        return ignoreCase || ignorePunctuation;
    }
}

[HarmonyPatch(typeof(UserDictionary), nameof(UserDictionary.AnotherEntryAlreadyHasName))]
internal static class DictionaryNameConflictCheckPatch
{
    private static void Postfix(UserDictionary __instance, string __0, int __1, ref bool __result)
    {
        if (__result || __instance?.keys == null ||
            !DictionaryNameConflictCompatibility.Enabled(out bool ignoreCase,
                out bool ignorePunctuation))
            return;
        __result = DictionaryNameConflictCompatibility.HasConflict(__0,
            __instance.keys, __1, ignoreCase, ignorePunctuation);
    }
}

[HarmonyPatch(typeof(UserDictionary), nameof(UserDictionary.AddEntry))]
internal static class DictionaryAddNameConflictPatch
{
    private static bool Prefix(UserDictionary __instance, string __0, ref bool __result)
    {
        if (__instance?.keys == null ||
            !DictionaryNameConflictCompatibility.Enabled(out bool ignoreCase,
                out bool ignorePunctuation) ||
            !DictionaryNameConflictCompatibility.HasConflict(__0,
                __instance.keys, null, ignoreCase, ignorePunctuation))
            return true;
        __result = false;
        return false;
    }
}

[HarmonyPatch(typeof(UserDictionary), nameof(UserDictionary.EditEntry))]
internal static class DictionaryEditNameConflictPatch
{
    private static bool Prefix(UserDictionary __instance, string __0, int __1, ref bool __result)
    {
        if (__instance?.keys == null ||
            !DictionaryNameConflictCompatibility.Enabled(out bool ignoreCase,
                out bool ignorePunctuation) ||
            !DictionaryNameConflictCompatibility.HasConflict(__0,
                __instance.keys, __1, ignoreCase, ignorePunctuation))
            return true;
        __result = false;
        return false;
    }
}
