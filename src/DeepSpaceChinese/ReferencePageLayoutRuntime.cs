using System;
using System.Collections.Generic;
using System.Text;
using BepInEx.Logging;
using TMPro;
using UnityEngine;

namespace DeepSpaceChinese;

internal static class ReferenceCopyButtonLayout
{
    internal static float PlaceAfterText(float textLeftX, float renderedTextWidth, float gap) =>
        textLeftX + renderedTextWidth + gap;

    internal static int MatchScore(string buttonName, string line, string copyValue)
    {
        string normalizedValue = Normalize(copyValue);
        string normalizedLine = Normalize(line);
        if (!string.IsNullOrEmpty(normalizedValue) && normalizedLine.Contains(normalizedValue))
            return 1000 + normalizedValue.Length;

        return buttonName switch
        {
            "Copy Mass" when StartsWithEither(line, "Mass:", "质量：") => 900,
            "Copy Volume" when StartsWithEither(line, "Volume:", "体积：") => 900,
            "Copy Density" when StartsWithEither(line, "Avg Density:", "平均密度：") => 900,
            "Copy Length" when StartsWithEither(line, "Length:", "长度：") => 900,
            "Copy Width" when StartsWithEither(line, "Width:", "宽度：") => 900,
            "Copy Height" when StartsWithEither(line, "Height:", "高度：") => 900,
            _ => 0,
        };
    }

    private static bool StartsWithEither(string value, string first, string second) =>
        value.TrimStart().StartsWith(first, StringComparison.OrdinalIgnoreCase) ||
        value.TrimStart().StartsWith(second, StringComparison.Ordinal);

    private static string Normalize(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;
        var result = new StringBuilder(value.Length);
        foreach (char character in value)
        {
            if (char.IsWhiteSpace(character) || character is ',' or '，')
                continue;
            result.Append(char.ToUpperInvariant(character));
        }
        return result.ToString();
    }
}

internal sealed class ReferencePageLayoutRuntime
{
    private readonly PatchConfig _config;
    private readonly ManualLogSource _log;
    private readonly Dictionary<int, SavedButtonPosition> _originalButtons = new();

    internal ReferencePageLayoutRuntime(PatchConfig config, ManualLogSource log)
    {
        _config = config;
        _log = log;
    }

    internal void ApplyAll()
    {
        if (_config?.Enabled != true || _config.DisplayMode != DisplayMode.TranslationOnly)
        {
            RestoreAll();
            return;
        }

        int adjusted = 0;
        foreach (ClipboardCopyButton button in Resources.FindObjectsOfTypeAll<ClipboardCopyButton>())
        {
            if (button == null || !button.gameObject.scene.IsValid() ||
                !IsUnderReferenceWindow(button.transform))
                continue;
            Transform parent = button.transform.parent;
            if (parent == null || !TryFindLineEnd(button, parent, out float lineEndX))
                continue;

            int id = button.GetInstanceID();
            if (!_originalButtons.ContainsKey(id))
                _originalButtons[id] = new SavedButtonPosition(button, button.transform.localPosition);
            Vector3 position = button.transform.localPosition;
            float halfIconWidth = Math.Abs(button.transform.localScale.x) * 0.5f;
            position.x = ReferenceCopyButtonLayout.PlaceAfterText(
                lineEndX, 0f, halfIconWidth);
            button.transform.localPosition = position;
            adjusted++;
        }

        RemoveDeadButtons();
        if (adjusted > 0)
            _log?.LogInfo($"参考页复制按钮已按当前文本重排：{adjusted} 个。");
    }

    internal void RestoreAll()
    {
        foreach (SavedButtonPosition saved in _originalButtons.Values)
        {
            if (saved.Button != null)
                saved.Button.transform.localPosition = saved.LocalPosition;
        }
        RemoveDeadButtons();
    }

    private static bool TryFindLineEnd(ClipboardCopyButton button, Transform parent,
        out float lineEndX)
    {
        lineEndX = 0f;
        TMP_Text bestText = null;
        int bestLine = -1;
        int bestScore = 0;

        foreach (TMP_Text text in parent.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text == null)
                continue;
            text.ForceMeshUpdate(ignoreActiveState: true, forceTextReparsing: true);
            TMP_TextInfo info = text.textInfo;
            for (int lineIndex = 0; lineIndex < info.lineCount; lineIndex++)
            {
                string line = GetLineText(info, lineIndex);
                int score = ReferenceCopyButtonLayout.MatchScore(
                    button.name, line, button.stringToCopy);
                if (score <= bestScore)
                    continue;
                bestScore = score;
                bestText = text;
                bestLine = lineIndex;
            }
        }

        if (bestText == null || bestLine < 0)
            return false;
        TMP_LineInfo matched = bestText.textInfo.lineInfo[bestLine];
        Vector3 worldEnd = bestText.transform.TransformPoint(
            new Vector3(matched.lineExtents.max.x, matched.baseline, 0f));
        lineEndX = parent.InverseTransformPoint(worldEnd).x;
        return true;
    }

    private static string GetLineText(TMP_TextInfo info, int lineIndex)
    {
        TMP_LineInfo line = info.lineInfo[lineIndex];
        var result = new StringBuilder(Math.Max(0, line.characterCount));
        int end = Math.Min(info.characterCount,
            line.firstCharacterIndex + Math.Max(0, line.characterCount));
        for (int index = line.firstCharacterIndex; index < end; index++)
            result.Append(info.characterInfo[index].character);
        return result.ToString();
    }

    private static bool IsUnderReferenceWindow(Transform transform)
    {
        for (Transform current = transform; current != null; current = current.parent)
        {
            if (current.name == "Reference Window")
                return true;
        }
        return false;
    }

    private void RemoveDeadButtons()
    {
        var dead = new List<int>();
        foreach (KeyValuePair<int, SavedButtonPosition> pair in _originalButtons)
        {
            if (pair.Value.Button == null)
                dead.Add(pair.Key);
        }
        foreach (int id in dead)
            _originalButtons.Remove(id);
    }

    private sealed class SavedButtonPosition
    {
        internal SavedButtonPosition(ClipboardCopyButton button, Vector3 localPosition)
        {
            Button = button;
            LocalPosition = localPosition;
        }

        internal ClipboardCopyButton Button { get; }
        internal Vector3 LocalPosition { get; }
    }
}
