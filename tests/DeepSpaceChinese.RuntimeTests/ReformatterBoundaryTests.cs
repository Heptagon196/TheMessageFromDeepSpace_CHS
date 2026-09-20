using System;
using System.Linq;
using DeepSpaceChinese;

internal static class ReformatterBoundaryTests
{
    internal static void Run()
    {
        string[] keys = { "STAR", "TO", "START", "AT", "ATOM", "MAKE", "DO" };
        Check(ReplayShippedReformatter("STAR TO", keys), "START|O");
        foreach (bool ignoreCase in new[] { false, true })
        {
            Check(Compile("STAR TO", keys, ignoreCase), "STAR|TO");
            Check(Compile("TO STAR", keys, ignoreCase), "TO|STAR");
            Check(Compile("STAR", keys, ignoreCase), "STAR");
            Check(Compile("TO", keys, ignoreCase), "TO");
            Check(Compile("STAR AT", keys, ignoreCase), "STAR|AT");
            Check(Compile("STAR TO ATOM MAKE DO", keys, ignoreCase), "STAR|TO|ATOM|MAKE|DO");
            Check(Compile("STAR  TO\nATOM", keys, ignoreCase), "STAR|TO|ATOM");
            Check(Compile("STARTO", keys, ignoreCase), "START|O");
            Check(Compile("12 34", keys, ignoreCase), "12|34");
        }
        Check(Compile("star to", keys, true), "STAR|TO");
        Check(Compile("恒星 向", new[] { "恒星", "向", "恒星向" }, true), "恒星|向");
        Check(Compile("恒星向", new[] { "恒星", "向", "恒星向" }, true), "恒星向");
    }

    private static string[] Compile(string input, string[] keys, bool ignoreCase) =>
        ReplayShippedReformatter(CompilerCaseCompatibility.PrepareForReformatter(
            input, keys, ignoreCase, false), keys);

    private static void Check(string[] actual, string expected)
    {
        string result = string.Join("|", actual);
        if (result != expected)
            throw new InvalidOperationException($"Reformatter boundary: expected {expected}, got {result}");
    }

    // Replay the shipped C_Reformatter's pure string stage, including its space
    // removal, longest-key-first search, masking and inserted LF delimiters.
    // Unity singleton access and the subsequent per-token lookup are omitted.
    private static string[] ReplayShippedReformatter(string input, string[] keys)
    {
        input = input.Replace(" ", string.Empty).Replace("\0", string.Empty);
        string[] sorted = keys.ToArray();
        Array.Sort(sorted, (x, y) => y.Length.CompareTo(x.Length));
        string masked = input;
        foreach (string key in sorted)
        {
            int offset = 0;
            int iterations = 0;
            while (offset < input.Length && iterations++ < 100)
            {
                int found = key.StartsWith("@") ? -2 : masked.IndexOf(key, offset);
                if (found < 0)
                    break;
                offset = found + 1;
                if (found > 0)
                {
                    offset++;
                    input = input.Insert(found, "\n").Insert(found + key.Length + 1, "\n");
                    masked = masked.Remove(found, key.Length).Insert(found, new string('@', key.Length))
                        .Insert(found, "\n").Insert(found + key.Length + 1, "\n");
                }
                else
                {
                    input = input.Insert(key.Length, "\n");
                    masked = masked.Remove(0, key.Length).Insert(0, new string('@', key.Length))
                        .Insert(key.Length, "\n");
                }
            }
        }
        return input.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
    }
}
