using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace DeepSpaceChinese;

internal sealed class DialogueLayoutRuntime
{
    private const float ShrinkThreshold = 1.5f;

    private static readonly FieldInfo MainSubtitleField =
        AccessTools.Field(typeof(DialogueManager), "subtitle");
    private static readonly FieldInfo MainAdvanceIconField =
        AccessTools.Field(typeof(DialogueManager), "advanceIcon");
    private static readonly FieldInfo NonLogSubtitleField =
        AccessTools.Field(typeof(NonLogDialogueManager), "subtitle");
    private static readonly FieldInfo NonLogSpeakerTitleField =
        AccessTools.Field(typeof(NonLogDialogueManager), "speakerTitle");
    private static readonly FieldInfo NonLogDialogueManagerField =
        AccessTools.Field(typeof(NonLogDialogueManager), "dialogueManager");

    private readonly PatchConfig _config;
    private readonly ManualLogSource _log;
    private readonly Dictionary<int, float> _baseFontSizes = new();
    private readonly Dictionary<int, Vector4> _baseMargins = new();
    private readonly Dictionary<int, Color> _baseColors = new();
    private readonly Dictionary<TMP_Text, Speaker> _lastSpeakers = new();
    private readonly HashSet<int> _resizedSubtitleRects = new();

    public DialogueLayoutRuntime(PatchConfig config, ManualLogSource log)
    {
        _config = config;
        _log = log;
    }

    public DialogueFrame FitMain(DialogueManager manager, DialogueFrame frame)
    {
        if (_config.Enabled == false || manager == null)
            return frame;
        var subtitle = MainSubtitleField?.GetValue(manager) as TMP_Text;
        ReserveAdvanceIconSpace(manager, subtitle);
        ConfigureSpeakerColor(subtitle, frame.speaker);
        if (!_config.TranslateDialogue)
            return frame;
        TMP_FontAsset font = manager.CurrProf.font;
        return Fit(subtitle, font, frame, string.Empty, "主对话框");
    }

    public DialogueFrame FitNonLog(NonLogDialogueManager manager, DialogueFrame frame)
    {
        if (_config.Enabled == false || manager == null)
            return frame;
        var subtitle = NonLogSubtitleField?.GetValue(manager) as TMP_Text;
        var speakerTitle = NonLogSpeakerTitleField?.GetValue(manager) as TMP_Text;
        ConfigureSpeakerColor(subtitle, frame.speaker);
        ConfigureSpeakerColor(speakerTitle, frame.speaker);
        if (!_config.TranslateDialogue)
            return frame;
        var dialogueManager = NonLogDialogueManagerField?.GetValue(manager) as DialogueManager;
        if (dialogueManager != null)
            dialogueManager.PlayFrame(frame);
        TMP_FontAsset font = dialogueManager == null ? null : dialogueManager.CurrProf.font;
        string prefix = DialogueManager.GetSpeakerNameDr(frame.speaker) + ": ";
        return Fit(subtitle, font, frame, prefix, "场景对话框");
    }

    public void ReapplySpeakerColors()
    {
        foreach (KeyValuePair<TMP_Text, Speaker> entry in new List<KeyValuePair<TMP_Text, Speaker>>(_lastSpeakers))
        {
            if (entry.Key == null)
            {
                _lastSpeakers.Remove(entry.Key);
                continue;
            }
            ConfigureSpeakerColor(entry.Key, entry.Value);
        }
    }

    private DialogueFrame Fit(TMP_Text subtitle, TMP_FontAsset font, DialogueFrame frame,
        string repeatedPrefix, string context)
    {
        if (subtitle == null || frame.dialogueParts == null || frame.dialogueParts.Length == 0)
            return frame;
        if (font != null)
            subtitle.font = font;

        float baseFontSize = GetBaseFontSize(subtitle);
        subtitle.enableAutoSizing = false;
        subtitle.fontSize = baseFontSize;
        Vector4 baseMargin = GetBaseMargin(subtitle);
        subtitle.margin = baseMargin;
        float availableWidth = DialogueWidthBudget.AvailableWidth(
            subtitle.rectTransform.rect.width, baseMargin.x, baseMargin.z);
        if (availableWidth <= 1f)
        {
            ConfigureAutoSizing(subtitle, baseFontSize);
            return frame;
        }

        float Measure(string raw)
        {
            string resolved = ResolveRuntimeText(raw);
            return subtitle.GetPreferredValues(resolved, 32767f, 32767f).x;
        }

        bool FitsDisplayArea(string raw)
        {
            float savedFontSize = subtitle.fontSize;
            try
            {
                subtitle.fontSize = baseFontSize / ShrinkThreshold;
                string resolved = ResolveRuntimeText(raw);
                float availableHeight = Math.Max(0f,
                    subtitle.rectTransform.rect.height - baseMargin.y - baseMargin.w);
                Vector2 preferred = subtitle.GetPreferredValues(
                    resolved, availableWidth, 32767f);
                return preferred.y <= availableHeight + 0.001f;
            }
            catch (Exception ex)
            {
                _log.LogWarning($"测量对白实际显示高度失败，将沿用两行宽度判定：{ex.Message}");
                return true;
            }
            finally
            {
                subtitle.fontSize = savedFontSize;
            }
        }

        var input = new DialogueLayoutPart[frame.dialogueParts.Length];
        for (int index = 0; index < input.Length; index++)
        {
            DialoguePart part = frame.dialogueParts[index];
            input[index] = new DialogueLayoutPart(part.txt, part.charDelay, part.clearPrev, part.msgDelay);
        }
        DialogueLayoutResult layout = DialogueLayoutEngine.Fit(input, availableWidth,
            ShrinkThreshold, Measure, repeatedPrefix, FitsDisplayArea);
        ConfigureAutoSizing(subtitle, baseFontSize);
        if (!layout.WasPaginated)
            return frame;

        var output = new DialoguePart[layout.Parts.Count];
        for (int index = 0; index < output.Length; index++)
        {
            DialogueLayoutPart part = layout.Parts[index];
            output[index] = new DialoguePart
            {
                txt = part.Text,
                charDelay = part.CharacterDelay,
                clearPrev = part.ClearPrevious,
                msgDelay = part.MessageDelay,
            };
        }
        frame.dialogueParts = output;
        _log.LogDebug($"{context}自动分页：新增 {layout.AddedPages} 页。原段数 {input.Length}，显示段数 {output.Length}。");
        return frame;
    }

    private float GetBaseFontSize(TMP_Text subtitle)
    {
        int id = subtitle.GetInstanceID();
        if (_baseFontSizes.TryGetValue(id, out float size))
            return size;
        size = subtitle.enableAutoSizing && subtitle.fontSizeMax > 0f
            ? subtitle.fontSizeMax
            : subtitle.fontSize;
        if (size <= 0f)
            size = 36f;
        _baseFontSizes[id] = size;
        return size;
    }

    private void ReserveAdvanceIconSpace(DialogueManager manager, TMP_Text subtitle)
    {
        var iconComponent = MainAdvanceIconField?.GetValue(manager) as Component;
        Transform icon = iconComponent?.transform;
        RectTransform textRect = subtitle?.rectTransform;
        Transform parent = icon?.parent;
        if (icon == null || textRect == null || parent == null)
            return;

        int id = textRect.GetInstanceID();
        if (_resizedSubtitleRects.Contains(id))
            return;

        float textRight = parent.InverseTransformPoint(
            textRect.TransformPoint(new Vector3(textRect.rect.xMax, textRect.rect.center.y, 0f))).x;
        if (!TryGetMeshLeft(icon, parent, out float iconLeft))
        {
            _log.LogWarning("[DialogueAdvanceIcon] 无法读取继续图标网格边界，未调整位置。");
            return;
        }

        const float gap = 0.005f;
        float shrink = DialogueTextRectLayout.RequiredRightShrink(textRight, iconLeft, gap);
        Vector2 beforeSize = textRect.sizeDelta;
        Vector2 beforePosition = textRect.anchoredPosition;
        if (shrink > 0.0001f && shrink < textRect.rect.width * 0.25f)
        {
            textRect.sizeDelta = new Vector2(beforeSize.x - shrink, beforeSize.y);
            textRect.anchoredPosition = new Vector2(
                beforePosition.x - shrink * (1f - textRect.pivot.x), beforePosition.y);
        }
        _resizedSubtitleRects.Add(id);
        _log.LogInfo($"[DialogueTextRect] textRight={textRight:F4}, iconLeft={iconLeft:F4}, " +
                     $"shrink={shrink:F4}, width={beforeSize.x:F4}->{textRect.sizeDelta.x:F4}, " +
                     $"anchoredX={beforePosition.x:F4}->{textRect.anchoredPosition.x:F4}");
    }

    private static bool TryGetMeshLeft(Transform icon, Transform parent, out float left)
    {
        left = float.PositiveInfinity;
        var filter = icon.GetComponent<MeshFilter>();
        Mesh mesh = filter == null ? null : filter.sharedMesh;
        if (mesh == null)
            return false;
        Bounds bounds = mesh.bounds;
        Vector3 center = bounds.center;
        Vector3 extents = bounds.extents;
        for (int x = -1; x <= 1; x += 2)
        for (int y = -1; y <= 1; y += 2)
        for (int z = -1; z <= 1; z += 2)
        {
            Vector3 local = center + Vector3.Scale(extents, new Vector3(x, y, z));
            float value = parent.InverseTransformPoint(icon.TransformPoint(local)).x;
            if (value < left)
                left = value;
        }
        return !float.IsInfinity(left) && !float.IsNaN(left);
    }

    private Vector4 GetBaseMargin(TMP_Text subtitle)
    {
        int id = subtitle.GetInstanceID();
        if (_baseMargins.TryGetValue(id, out Vector4 margin))
            return margin;
        margin = subtitle.margin;
        _baseMargins[id] = margin;
        return margin;
    }

    private void ConfigureSpeakerColor(TMP_Text text, Speaker speaker)
    {
        if (text == null)
            return;
        _lastSpeakers[text] = speaker;
        int id = text.GetInstanceID();
        if (!_baseColors.TryGetValue(id, out Color original))
        {
            original = text.color;
            _baseColors[id] = original;
        }
        if (!_config.SpeakerColorsEnabled)
        {
            text.color = original;
            return;
        }
        text.color = ColorUtility.TryParseHtmlString(_config.SpeakerColor(speaker), out Color configured)
            ? configured
            : original;
    }

    private static void ConfigureAutoSizing(TMP_Text subtitle, float baseFontSize)
    {
        subtitle.fontSizeMax = baseFontSize;
        subtitle.fontSizeMin = baseFontSize / ShrinkThreshold;
        subtitle.enableAutoSizing = true;
    }

    private string ResolveRuntimeText(string raw)
    {
        try
        {
            string value = DialogueChunk.RemoveAnimCommands(raw ?? string.Empty);
            if (value.IndexOf('|') >= 0)
                value = DialogueManager.ReplaceSignalEmbeds(value);
            if (value.IndexOf("Translator", StringComparison.Ordinal) >= 0)
                value = DialogueManager.ReplaceTranslator(value);
            return value;
        }
        catch (Exception ex)
        {
            _log.LogWarning($"测量动态对白时无法展开运行时标记，将按原标记估算：{ex.Message}");
            return DialogueChunk.RemoveAnimCommands(raw ?? string.Empty);
        }
    }

}
