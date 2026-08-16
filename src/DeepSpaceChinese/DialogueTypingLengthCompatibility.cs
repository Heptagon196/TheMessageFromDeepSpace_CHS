using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace DeepSpaceChinese;

/// <summary>
/// The game compares TMP maxVisibleCharacters with string.Length while typing.
/// Localized dialogue contains TMP font and underline tags, so the raw length can
/// remain larger after every visible glyph is already on screen. Compare against
/// visible glyph count instead, preserving the original click-to-complete behavior.
/// </summary>
[HarmonyPatch]
internal static class DialogueTypingLengthCompatibility
{
    internal const int PatchedCoroutineCountForTests = 3;

    internal static int ResolvedTargetCountForTests()
    {
        int count = 0;
        foreach (MethodBase target in TargetMethods())
        {
            if (target != null)
                count++;
        }
        return count;
    }

    internal static int VisibleLengthForTyping(string value) =>
        DialogueTextMap.VisibleLength(value);

    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.EnumeratorMoveNext(
            AccessTools.Method(typeof(DialogueManager), "WriteFrameRoutine",
                new[] { typeof(DialogueFrame), typeof(bool) }));
        yield return AccessTools.EnumeratorMoveNext(
            AccessTools.Method(typeof(DialogueManager), "CharacterDialogueTypeRoutine",
                new[] { typeof(Speaker), typeof(TMPro.TMP_Text),
                    typeof(DialogueFrame), typeof(bool) }));
        yield return AccessTools.EnumeratorMoveNext(
            AccessTools.Method(typeof(NonLogDialogueManager), "WriteFrameRoutine",
                new[] { typeof(DialogueFrame) }));
    }

    private static IEnumerable<CodeInstruction> Transpiler(
        IEnumerable<CodeInstruction> instructions)
    {
        MethodInfo stringLength = AccessTools.PropertyGetter(typeof(string),
            nameof(string.Length));
        MethodInfo visibleLength = AccessTools.Method(
            typeof(DialogueTypingLengthCompatibility), nameof(VisibleLengthForTyping));
        foreach (CodeInstruction instruction in instructions)
        {
            if (!instruction.Calls(stringLength))
            {
                yield return instruction;
                continue;
            }

            var replacement = new CodeInstruction(OpCodes.Call, visibleLength);
            replacement.labels.AddRange(instruction.labels);
            replacement.blocks.AddRange(instruction.blocks);
            yield return replacement;
        }
    }
}
