using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeepSpaceChinese;

internal sealed class PlayerNameRuntime
{
    private sealed class LabelLayout
    {
        public Vector2 AnchorMin;
        public Vector2 AnchorMax;
        public Vector2 Pivot;
        public Vector2 AnchoredPosition;
        public Vector2 SizeDelta;
        public TextAlignmentOptions Alignment;
        public TextWrappingModes Wrapping;
        public TextOverflowModes Overflow;
        public Vector2 InputAnchorMin;
        public Vector2 InputAnchorMax;
        public Vector2 InputPivot;
        public Vector2 InputAnchoredPosition;
        public Vector2 InputSizeDelta;
    }

    private sealed class RectLayout
    {
        public Vector2 AnchorMin;
        public Vector2 AnchorMax;
        public Vector2 Pivot;
        public Vector2 AnchoredPosition;
        public Vector2 SizeDelta;
    }

    private static readonly FieldInfo InputField = AccessTools.Field(typeof(NameTranslator), "nameEntryInput");
    private static readonly FieldInfo PrefixLabel = AccessTools.Field(typeof(NameTranslator), "drPrefix");
    private static readonly FieldInfo PrefixSource = AccessTools.Field(typeof(NameTranslator), "drPrefix_s");
    private readonly PatchConfig _config;
    private readonly ManualLogSource _log;
    private readonly Dictionary<int, LabelLayout> _originalLayouts = new();
    private readonly Dictionary<int, RectLayout> _originalAuxiliaryLayouts = new();
    private readonly HashSet<TMP_InputField> _knownInputs = new();

    public PlayerNameRuntime(PatchConfig config, ManualLogSource log)
    {
        _config = config;
        _log = log;
    }

    public void ApplyAll()
    {
        foreach (NameTranslator namer in Resources.FindObjectsOfTypeAll<NameTranslator>())
        {
            if (namer != null && namer.gameObject.scene.IsValid() &&
                NameLayoutApplicationPolicy.ShouldApplyDuringScan(namer.gameObject.activeInHierarchy))
                Apply(namer);
        }
    }

    public void Apply(NameTranslator namer)
    {
        if (namer == null)
            return;
        try
        {
            TMP_InputField input = InputField?.GetValue(namer) as TMP_InputField;
            TMP_Text label = PrefixLabel?.GetValue(namer) as TMP_Text;
            bool translated = _config.DisplayMode == DisplayMode.TranslationOnly;

            PrefixSource?.SetValue(namer, translated ? "博士" : "Dr. ");
            ConfigureNameInput(input);
            TrackInput(input);
            if (label != null)
                ApplyLabelLayout(namer, label, input, translated);
        }
        catch (Exception ex)
        {
            _log.LogError($"调整起名界面失败，将保留游戏原布局：\n{ex}");
        }
    }

    public void ApplyProgressLogInput(ProgressLog progressLog)
    {
        if (progressLog?.translatorInput == null)
            return;
        ConfigureUnicodeInput(progressLog.translatorInput);
        TrackInput(progressLog.translatorInput);
    }

    public void ApplySharedInput(InputTextDummy dummy)
    {
        TMP_InputField input = dummy?.InputField;
        if (input == null)
            return;
        _log.LogInfo($"[DEBUG-enter31] ApplySharedInput before path={ObjectPath(input.transform)} " +
                     $"lineType={input.lineType} lineLimit={input.lineLimit} " +
                     $"contentType={input.contentType} validation={input.characterValidation} " +
                     $"textComponent={ObjectPath(input.textComponent?.transform)}");
        ConfigureUnicodeInput(input);
        SharedInputReturnCompatibility.Track(input);
        TrackInput(input);
        _log.LogInfo($"[DEBUG-enter31] ApplySharedInput after path={ObjectPath(input.transform)} " +
                     $"lineType={input.lineType} lineLimit={input.lineLimit} " +
                     $"contentType={input.contentType} validation={input.characterValidation} " +
                     $"tracked={SharedInputReturnCompatibility.IsTracked(input)}");
    }

    public void UpdateImeCursorPosition(TMP_InputField preferred = null)
    {
        TrackInput(preferred);
        _knownInputs.RemoveWhere(input => input == null);
        TMP_InputField focused = preferred != null && preferred.isActiveAndEnabled &&
                                 preferred.isFocused
            ? preferred
            : null;
        foreach (TMP_InputField input in _knownInputs)
        {
            if (focused != null)
                break;
            if (input != null && input.isActiveAndEnabled && input.isFocused)
            {
                focused = input;
                break;
            }
        }

        if (focused == null)
            return;

        if (!ImeCursorPositionEngine.TryCalculate(focused, out Vector2 unityPosition,
                out Vector2 windowsPosition))
            return;

        Input.compositionCursorPos = windowsPosition;
    }

    private static string ObjectPath(Transform transform)
    {
        if (transform == null)
            return "<null>";
        string path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }
        return path;
    }

    internal static string FormatFullName(string rawName, string originalFullName, DisplayMode mode)
    {
        if (mode == DisplayMode.OriginalOnly)
            return string.IsNullOrWhiteSpace(originalFullName) ? "Translator" : originalFullName;
        if (string.IsNullOrWhiteSpace(rawName))
            return "翻译员";
        return rawName.EndsWith("博士", StringComparison.Ordinal) ? rawName : rawName + "博士";
    }

    private static void ConfigureNameInput(TMP_InputField input)
    {
        if (input == null)
            return;
        ConfigureUnicodeInput(input);
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.characterLimit = 14;
    }

    private void TrackInput(TMP_InputField input)
    {
        if (input != null)
            _knownInputs.Add(input);
    }

    private static void ConfigureUnicodeInput(TMP_InputField input)
    {
        if (input == null)
            return;
        input.contentType = TMP_InputField.ContentType.Custom;
        // TMP_InputField.inputValidator's setter changes validation back to
        // CustomValidator. Clear it first, then make None the final state.
        input.inputValidator = null;
        input.characterValidation = TMP_InputField.CharacterValidation.None;
        input.onValidateInput = null;
        input.readOnly = false;
    }

    private void ApplyLabelLayout(NameTranslator namer, TMP_Text label, TMP_InputField input,
        bool translated)
    {
        RectTransform labelRect = label.rectTransform;
        int id = label.GetInstanceID();
        if (!_originalLayouts.TryGetValue(id, out LabelLayout original))
        {
            original = new LabelLayout
            {
                AnchorMin = labelRect.anchorMin,
                AnchorMax = labelRect.anchorMax,
                Pivot = labelRect.pivot,
                AnchoredPosition = labelRect.anchoredPosition,
                SizeDelta = labelRect.sizeDelta,
                Alignment = label.alignment,
                Wrapping = label.textWrappingMode,
                Overflow = label.overflowMode,
            };
            if (input != null && input.transform is RectTransform originalInputRect)
            {
                original.InputAnchorMin = originalInputRect.anchorMin;
                original.InputAnchorMax = originalInputRect.anchorMax;
                original.InputPivot = originalInputRect.pivot;
                original.InputAnchoredPosition = originalInputRect.anchoredPosition;
                original.InputSizeDelta = originalInputRect.sizeDelta;
            }
            _originalLayouts[id] = original;
        }

        RestoreOriginalLayout(labelRect, label, input, original);
        RestoreAuxiliaryLayout(namer.namePrompt);
        RestoreAuxiliaryLayout(namer.isThisGood);
        RestoreAuxiliaryLayout(namer.nameIsTaken);
        if (!translated || input == null)
        {
            label.text = "Dr.";
            return;
        }

        label.text = "博士";
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Overflow;
        Vector2 preferred = label.GetPreferredValues("博士");

        RectTransform inputRect = input.transform as RectTransform;
        if (inputRect == null)
            return;
        RectTransform commonParent = inputRect.parent as RectTransform;
        if (commonParent == null || labelRect.parent != commonParent)
            return;

        Bounds originalLabelBounds = SelfBounds(commonParent, labelRect);
        Bounds originalInputBounds = SelfBounds(commonParent, inputRect);
        float containerLeft = Math.Min(originalLabelBounds.min.x, originalInputBounds.min.x);
        float containerRight = Math.Max(originalLabelBounds.max.x, originalInputBounds.max.x);
        float containerCenter = (containerLeft + containerRight) * 0.5f;
        float containerWidth = containerRight - containerLeft;
        float suffixWidth = Math.Max(1f, preferred.x + 8f);
        NameSuffixLayout layout = NameSuffixLayoutEngine.Calculate(containerLeft, containerRight,
            originalInputBounds.min.x, originalInputBounds.max.x, suffixWidth, 8f);

        RectTransform promptRect = namer.namePrompt?.rectTransform;
        RectTransform goodRect = namer.isThisGood?.rectTransform;
        RectTransform takenRect = namer.nameIsTaken?.rectTransform;
        float promptCenter = promptRect != null
            ? SelfBounds(commonParent, promptRect).center.y
            : originalInputBounds.center.y + 130f;
        float promptHeight = PreferredHeight(namer.namePrompt, namer.nameTranslator_s, 50f);
        float goodHeight = PreferredHeight(namer.isThisGood, namer.isThisGood_s, 50f);
        float takenHeight = Math.Max(50f, (namer.nameIsTaken?.fontSize ?? 64f) * 2f + 8f);
        NameUiVerticalLayout vertical = NameUiVerticalLayoutEngine.Calculate(promptCenter,
            promptHeight, originalInputBounds.size.y, goodHeight, takenHeight, 16f);

        SetRect(inputRect, commonParent,
            (layout.InputLeft + layout.InputRight) * 0.5f, vertical.RowCenter,
            layout.InputRight - layout.InputLeft, originalInputBounds.size.y);
        SetRect(labelRect, commonParent,
            (layout.SuffixLeft + layout.SuffixRight) * 0.5f, vertical.RowCenter,
            layout.SuffixRight - layout.SuffixLeft, originalInputBounds.size.y);
        SetRect(promptRect, commonParent, containerCenter, vertical.PromptCenter,
            containerWidth, promptHeight);
        SetRect(goodRect, commonParent, containerCenter, vertical.GoodCenter,
            containerWidth, goodHeight);
        SetRect(takenRect, commonParent, containerCenter, vertical.TakenCenter,
            containerWidth, takenHeight);
    }

    private void RestoreAuxiliaryLayout(TMP_Text text)
    {
        if (text == null)
            return;
        RectTransform rect = text.rectTransform;
        int id = text.GetInstanceID();
        if (!_originalAuxiliaryLayouts.TryGetValue(id, out RectLayout original))
        {
            original = new RectLayout
            {
                AnchorMin = rect.anchorMin,
                AnchorMax = rect.anchorMax,
                Pivot = rect.pivot,
                AnchoredPosition = rect.anchoredPosition,
                SizeDelta = rect.sizeDelta,
            };
            _originalAuxiliaryLayouts[id] = original;
        }
        rect.anchorMin = original.AnchorMin;
        rect.anchorMax = original.AnchorMax;
        rect.pivot = original.Pivot;
        rect.anchoredPosition = original.AnchoredPosition;
        rect.sizeDelta = original.SizeDelta;
    }

    private static float PreferredHeight(TMP_Text text, string value, float minimum)
    {
        if (text == null)
            return minimum;
        Vector2 preferred = text.GetPreferredValues(value ?? text.text ?? string.Empty);
        return Math.Max(minimum, preferred.y + 8f);
    }

    private static Bounds SelfBounds(RectTransform parent, RectTransform rect)
    {
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        Vector3 first = parent.InverseTransformPoint(corners[0]);
        Bounds bounds = new Bounds(first, Vector3.zero);
        for (int i = 1; i < corners.Length; i++)
            bounds.Encapsulate(parent.InverseTransformPoint(corners[i]));
        return bounds;
    }

    private static void SetRect(RectTransform rect, RectTransform parent, float centerX,
        float centerY, float width, float height)
    {
        if (rect == null || rect.parent != parent)
            return;
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Math.Max(1f, width));
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Math.Max(1f, height));
        Bounds current = SelfBounds(parent, rect);
        Vector3 delta = parent.TransformVector(new Vector3(
            centerX - current.center.x, centerY - current.center.y, 0f));
        rect.position += delta;
    }

    private static void RestoreOriginalLayout(RectTransform labelRect, TMP_Text label,
        TMP_InputField input, LabelLayout original)
    {
        labelRect.anchorMin = original.AnchorMin;
        labelRect.anchorMax = original.AnchorMax;
        labelRect.pivot = original.Pivot;
        labelRect.anchoredPosition = original.AnchoredPosition;
        labelRect.sizeDelta = original.SizeDelta;
        label.alignment = original.Alignment;
        label.textWrappingMode = original.Wrapping;
        label.overflowMode = original.Overflow;
        if (input != null && input.transform is RectTransform inputRect)
        {
            inputRect.anchorMin = original.InputAnchorMin;
            inputRect.anchorMax = original.InputAnchorMax;
            inputRect.pivot = original.InputPivot;
            inputRect.anchoredPosition = original.InputAnchoredPosition;
            inputRect.sizeDelta = original.InputSizeDelta;
        }
    }

}

[HarmonyPatch(typeof(NameTranslator), "NameEntry")]
internal static class NameTranslatorNameEntryPatch
{
    [HarmonyPostfix]
    private static void Postfix(NameTranslator __instance)
    {
        DeepSpaceChinesePlugin.Instance?.ApplyPlayerNameUi(__instance);
    }
}

[HarmonyPatch(typeof(ProgressLog), "WaitForTextComplete")]
internal static class ProgressLogTextInputPatch
{
    [HarmonyPrefix]
    private static void Prefix(ProgressLog __instance)
    {
        DeepSpaceChinesePlugin.Instance?.ApplyProgressLogInput(__instance);
    }
}

[HarmonyPatch(typeof(InputTextDummy), "BecomeDummy")]
internal static class SharedTextInputPatch
{
    [HarmonyPostfix]
    private static void Postfix(InputTextDummy __instance)
    {
        DeepSpaceChinesePlugin.Instance?.ApplySharedInput(__instance);
    }
}
