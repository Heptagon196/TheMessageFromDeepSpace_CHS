using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace DeepSpaceChinese;

internal static class CompilerCaseCompatibility
{
    public static bool TryResolve(string input, IEnumerable<KeyValuePair<string, int>> entries,
        out int signal)
    {
        signal = default;
        if (input == null || entries == null)
            return false;

        KeyValuePair<string, int>[] candidates = entries
            .Where(pair => string.Equals(pair.Key, input, StringComparison.OrdinalIgnoreCase))
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
                    .Where(key => RegionEquals(input, index, key,
                        StringComparison.OrdinalIgnoreCase))
                    .ToArray();
                if (candidates.Length == 0)
                    continue;
                string resolved = candidates.FirstOrDefault(key =>
                    RegionEquals(input, index, key, StringComparison.Ordinal));
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

    private static bool IsClaimed(bool[] claimed, int index, int length)
    {
        for (int offset = 0; offset < length; offset++)
            if (claimed[index + offset])
                return true;
        return false;
    }

    private static bool RegionEquals(string input, int index, string key,
        StringComparison comparison) =>
        string.Compare(input, index, key, 0, key.Length, comparison) == 0;
}

[HarmonyPatch(typeof(C_Reformatter), nameof(C_Reformatter.CompileStringToSignal))]
internal static class ReformatterCaseCompatibilityPatch
{
    private static void Prefix(ref string input)
    {
        if (DeepSpaceChinesePlugin.Instance?.CompilerCaseInsensitiveEnabled != true ||
            UserDictionary.Instance?.keys == null)
            return;
        input = CompilerCaseCompatibility.NormalizeForReformatter(input,
            UserDictionary.Instance.keys.Keys);
    }
}

[HarmonyPatch(typeof(C_WordCatcher), "TryGetSignalFromWord")]
internal static class WordCatcherCaseCompatibilityPatch
{
    private static bool Prefix(string word, ref int signal, ref bool __result)
    {
        if (DeepSpaceChinesePlugin.Instance?.CompilerCaseInsensitiveEnabled != true ||
            UserDictionary.Instance?.keys == null ||
            UserDictionary.Instance.keys.ContainsKey(word))
            return true;

        if (!CompilerCaseCompatibility.TryResolve(word, UserDictionary.Instance.keys,
                out int resolvedSignal))
            return true;
        signal = resolvedSignal;
        __result = true;
        return false;
    }
}
