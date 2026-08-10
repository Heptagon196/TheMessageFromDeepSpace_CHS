using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TMPro;

namespace DeepSpaceChinese;

internal sealed class CharacterFontRegistry
{
    private static readonly FieldInfo[] ProfileFields =
    {
        AccessTools.Field(typeof(DialogueManager), "aProf"),
        AccessTools.Field(typeof(DialogueManager), "bProf"),
        AccessTools.Field(typeof(DialogueManager), "cProf"),
        AccessTools.Field(typeof(DialogueManager), "dProf"),
        AccessTools.Field(typeof(DialogueManager), "logProf"),
        AccessTools.Field(typeof(DialogueManager), "pilotProf"),
        AccessTools.Field(typeof(DialogueManager), "qopilotProf"),
    };
    private static readonly FieldInfo NonLogDialogueManagerField =
        AccessTools.Field(typeof(NonLogDialogueManager), "dialogueManager");

    private readonly HashSet<int> _fontIds = new();
    private readonly HashSet<string> _fontNames = new(StringComparer.Ordinal);

    public void Register(DialogueManager manager)
    {
        if (manager == null)
            return;
        foreach (FieldInfo field in ProfileFields)
        {
            if (field?.GetValue(manager) is CharacterDialogueProfile profile)
                Register(profile.font);
        }
        Register(manager.CurrProf.font);
    }

    public void Register(NonLogDialogueManager manager)
    {
        if (manager != null)
            Register(NonLogDialogueManagerField?.GetValue(manager) as DialogueManager);
    }

    public void Register(TMP_FontAsset font)
    {
        if (font == null)
            return;
        _fontIds.Add(font.GetInstanceID());
        if (!string.IsNullOrEmpty(font.name))
            _fontNames.Add(font.name);
    }

    public bool Contains(TMP_FontAsset font) =>
        font != null && (_fontIds.Contains(font.GetInstanceID()) ||
                         (!string.IsNullOrEmpty(font.name) && _fontNames.Contains(font.name)));
}
