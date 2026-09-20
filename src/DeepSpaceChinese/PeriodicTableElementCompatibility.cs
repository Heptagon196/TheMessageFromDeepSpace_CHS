using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace DeepSpaceChinese;

internal static class PeriodicTableElementCompatibility
{
    private static readonly FieldInfo ElementButtonSymbolField =
        AccessTools.Field(typeof(ElementButton), "symbol");
    private static readonly FieldInfo PeriodicTableSymbolField =
        AccessTools.Field(typeof(PeriodicTableDisplay), "symbol_label");
    private static readonly HashSet<int> RegisteredPreviewLabels = new();
    private static readonly FieldInfo WindowHeightField =
        AccessTools.Field(typeof(ReferenceSubWindow), "windowHeight");
    private static readonly FieldInfo ScrollbarField =
        AccessTools.Field(typeof(ReferenceSubWindow), "scrollbar");

    internal static float FullHeightForRenderedBounds(float top, float bottom,
        float windowHeight, float padding) =>
        // ReferenceSubWindow divides FullInfoHeight by windowHeight before passing
        // it to ScrollArea, which expects a world-space height.
        Math.Max(windowHeight, top - bottom + padding) * windowHeight;

    internal static bool UpdateScrollHeight(ReferenceSubWindow window)
    {
        if (window == null || !window.gameObject.scene.IsValid())
            return false;
        foreach (PeriodicTableDisplay display in Resources.FindObjectsOfTypeAll<PeriodicTableDisplay>())
        {
            if (display == null || display.elementDisplay != window ||
                display.isotopeData_label == null || display.name_label == null ||
                !display.isotopeData_label.gameObject.activeInHierarchy)
                continue;
            float windowHeight = WindowHeightField?.GetValue(window) is float height ? height : 0f;
            if (windowHeight <= 0f)
                return false;
            var corners = new Vector3[4];
            display.name_label.rectTransform.GetWorldCorners(corners);
            float top = Math.Max(corners[1].y, corners[2].y);
            TMP_Text text = display.isotopeData_label;
            text.ForceMeshUpdate(ignoreActiveState: true, forceTextReparsing: true);
            float bottom = float.PositiveInfinity;
            for (int index = 0; index < text.textInfo.characterCount; index++)
            {
                TMP_CharacterInfo character = text.textInfo.characterInfo[index];
                if (!character.isVisible)
                    continue;
                bottom = Math.Min(bottom, text.transform.TransformPoint(character.bottomLeft).y);
                bottom = Math.Min(bottom, text.transform.TransformPoint(character.bottomRight).y);
            }
            if (float.IsPositiveInfinity(bottom))
                return false;
            ScrollBar3D scrollbar = ScrollbarField?.GetValue(window) as ScrollBar3D;
            float scroll = scrollbar != null ? scrollbar.NormalizedScroll : 1f;
            window.FullInfoHeight = FullHeightForRenderedBounds(top, bottom, windowHeight, 0.10f);
            scrollbar?.ForceScrollTo(scroll);
            return true;
        }
        return false;
    }

    internal static string ResolveSymbol(string objectName, string elementName,
        string elementSymbol)
    {
        // The shipped Radium asset has its name/symbol values reversed: name="Ra",
        // symbol="Radium". Keep this narrowly scoped so valid element data is untouched.
        if (string.Equals(objectName, "Radium", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(elementName, "Ra", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(elementSymbol, "Radium", StringComparison.OrdinalIgnoreCase))
            return "Ra";
        return elementSymbol;
    }

    internal static bool TryResolveSymbolText(TMP_Text component, string proposed,
        out string resolved)
    {
        resolved = proposed;
        if (component == null || !IsPeriodicTableSymbol(component))
            return false;

        // Symbol labels must stay as chemical symbols. Radium is the sole malformed
        // built-in asset; correcting it here also covers the grid and detail pane.
        if (string.Equals(proposed, "Radium", StringComparison.OrdinalIgnoreCase))
            resolved = "Ra";
        return true;
    }

    internal static void RegisterPreviewLabel(PeriodicTableDisplay display)
    {
        TMP_Text label = display?.previewElement_label;
        if (label != null)
            RegisteredPreviewLabels.Add(label.GetInstanceID());
    }

    internal static bool ShouldTranslateDisplayValues(bool isPeriodicTable,
        bool isSymbol, bool isRegisteredPreview = false) =>
        (isPeriodicTable && !isSymbol) || isRegisteredPreview;

    internal static string ResolvePreviewNameLookup(string proposed,
        bool isRegisteredPreview)
    {
        // Radium's shipped fields are reversed, so its preview receives "Radium"
        // while the extracted element-name translation is keyed by "Ra". Only
        // normalize the registered preview label; the grid's symbol still needs
        // the separate Radium -> Ra correction above.
        return isRegisteredPreview &&
               string.Equals(proposed, "Radium", StringComparison.OrdinalIgnoreCase)
            ? "Ra"
            : proposed;
    }

    internal static string ResolvePreviewNameLookup(TMP_Text component, string proposed) =>
        ResolvePreviewNameLookup(proposed,
            component != null && RegisteredPreviewLabels.Contains(component.GetInstanceID()));

    internal static bool ShouldTranslateDisplayValues(TMP_Text component)
    {
        if (component == null)
            return false;
        bool isPeriodicTable =
            component.GetComponentInParent<PeriodicTableDisplay>(true) != null;
        return ShouldTranslateDisplayValues(isPeriodicTable,
            IsPeriodicTableSymbol(component),
            RegisteredPreviewLabels.Contains(component.GetInstanceID()));
    }

    private static bool IsPeriodicTableSymbol(TMP_Text component)
    {
        ElementButton button = component.GetComponentInParent<ElementButton>(true);
        if (button != null && ElementButtonSymbolField != null &&
            ReferenceEquals(ElementButtonSymbolField.GetValue(button), component))
            return true;

        PeriodicTableDisplay display =
            component.GetComponentInParent<PeriodicTableDisplay>(true);
        return display != null && PeriodicTableSymbolField != null &&
               ReferenceEquals(PeriodicTableSymbolField.GetValue(display), component);
    }
}

[HarmonyPatch(typeof(PeriodicTableDisplay), nameof(PeriodicTableDisplay.PreviewElement))]
internal static class PeriodicTablePreviewElementLocalizationPatch
{
    [HarmonyPrefix]
    private static void Prefix(PeriodicTableDisplay __instance) =>
        PeriodicTableElementCompatibility.RegisterPreviewLabel(__instance);
}

[HarmonyPatch(typeof(PeriodicTableDisplay), nameof(PeriodicTableDisplay.DisplayElementData))]
internal static class PeriodicTableDisplayElementLocalizationPatch
{
    [HarmonyPostfix]
    private static void Postfix(PeriodicTableDisplay __instance)
    {
        try
        {
            DeepSpaceChinesePlugin.Instance?.PeriodicTableElementDisplayed(__instance);
        }
        catch (Exception ex)
        {
            DeepSpaceChinesePlugin.Instance?.PluginLog.LogError(
                $"周期表详情刷新汉化失败：\n{ex}");
        }
    }
}
