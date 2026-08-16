using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace DeepSpaceChinese;

internal static class WeeklyReportSubtitleCleanup
{
    private static readonly FieldInfo SubtitleField =
        AccessTools.Field(typeof(DialogueManager), "subtitle");
    private static readonly MethodInfo ClearMeshMethod =
        AccessTools.Method(typeof(TMP_Text), "ClearMesh", new[] { typeof(bool) });

    internal static void Clear(DialogueManager dialogueManager)
    {
        if (dialogueManager == null ||
            SubtitleField?.GetValue(dialogueManager) is not TMP_Text subtitle)
            return;

        // StartLog is the exact hand-off from the weekly meeting to its report. At this
        // point no dialogue subtitle should remain visible. Clearing only `text` is not
        // enough: TMP can retain the underline vertices from the final dictionary term
        // until another underlined term dirties the mesh.
        subtitle.maxVisibleCharacters = 0;
        subtitle.text = string.Empty;
        subtitle.ForceMeshUpdate(ignoreActiveState: true, forceTextReparsing: true);
        ClearMeshMethod?.Invoke(subtitle, new object[] { true });

        foreach (CanvasRenderer renderer in
                 subtitle.GetComponentsInChildren<CanvasRenderer>(includeInactive: true))
            renderer.Clear();
    }
}
