using System.Collections.Generic;
using System;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DeepSpaceChinese;

internal static class SharedInputReturnCompatibility
{
    private static readonly HashSet<int> TrackedInputIds = new();
    private static readonly HashSet<int> LoggedActivations = new();
    private static readonly HashSet<int> LoggedDeactivations = new();

    internal static void Track(TMP_InputField input)
    {
        if (input != null)
            TrackedInputIds.Add(input.GetInstanceID());
    }

    internal static bool IsTracked(TMP_InputField input) =>
        input != null && TrackedInputIds.Contains(input.GetInstanceID());

    internal static bool MarkActivationForLog(TMP_InputField input) =>
        input != null && LoggedActivations.Add(input.GetInstanceID());

    internal static bool MarkDeactivationForLog(TMP_InputField input) =>
        input != null && LoggedDeactivations.Add(input.GetInstanceID());

    internal static bool ShouldSuppressEarlySubmit(bool isTrackedSharedInput,
        bool isFocused, bool isMultiLineNewline, bool returnPressed,
        bool isInputSystemNavigationEvent)
    {
        // InputSystemUIInputModule may dispatch Submit immediately after focus is
        // acquired, before the legacy Input API reports Return. For multiline fields
        // Submit must never deactivate TMP; TMP's normal keyboard path inserts the
        // newline later in the same frame.
        return isTrackedSharedInput && isFocused && isMultiLineNewline &&
               isInputSystemNavigationEvent;
    }

    internal static bool IsInputSystemNavigationEvent(BaseEventData eventData)
    {
        Type eventType = eventData?.GetType();
        return eventType != null &&
               eventType.Name == "ExtendedSubmitCancelEventData" &&
               (eventType.Namespace?.StartsWith("UnityEngine.InputSystem.UI",
                    StringComparison.Ordinal) ?? false);
    }
}

// InputSystemUIInputModule sends Return as an ISubmitHandler event before TMP's
// OnUpdateSelected gets to interpret it as a newline. Suppress only that premature
// submit; TMP then processes the same key through its normal multiline path.
[HarmonyPatch(typeof(TMP_InputField), nameof(TMP_InputField.OnSubmit))]
internal static class SharedMultilineEarlySubmitPatch
{
    [HarmonyPrefix]
    private static bool Prefix(TMP_InputField __instance, BaseEventData eventData)
    {
        bool returnPressed = Input.GetKeyDown(KeyCode.Return) ||
                             Input.GetKeyDown(KeyCode.KeypadEnter);
        bool isInputSystemNavigationEvent =
            SharedInputReturnCompatibility.IsInputSystemNavigationEvent(eventData);
        bool suppress = SharedInputReturnCompatibility.ShouldSuppressEarlySubmit(
            SharedInputReturnCompatibility.IsTracked(__instance), __instance.isFocused,
            __instance.lineType == TMP_InputField.LineType.MultiLineNewline, returnPressed,
            isInputSystemNavigationEvent);
        if (SharedInputReturnCompatibility.IsTracked(__instance))
            DeepSpaceChinesePlugin.Instance?.PluginLog.LogInfo(
                $"[DEBUG-enter34] OnSubmit frame={Time.frameCount} id={__instance.GetInstanceID()} " +
                $"returnPressed={returnPressed} focused={__instance.isFocused} " +
                $"lineType={__instance.lineType} lineLimit={__instance.lineLimit} " +
                $"navigation={isInputSystemNavigationEvent} suppress={suppress} " +
                $"event={eventData?.GetType().FullName ?? "<null>"}");
        return !suppress;
    }
}

[HarmonyPatch(typeof(TMP_InputField), nameof(TMP_InputField.DeactivateInputField))]
internal static class SharedInputDeactivateTracePatch
{
    [HarmonyPrefix]
    private static void Prefix(TMP_InputField __instance, bool clearSelection)
    {
        if (!SharedInputReturnCompatibility.IsTracked(__instance) ||
            !SharedInputReturnCompatibility.MarkDeactivationForLog(__instance))
            return;
        DeepSpaceChinesePlugin.Instance?.PluginLog.LogInfo(
            $"[DEBUG-focus34] first DeactivateInputField frame={Time.frameCount} " +
            $"id={__instance.GetInstanceID()} " +
            $"focused={__instance.isFocused} clear={clearSelection} " +
            $"lineType={__instance.lineType} lineLimit={__instance.lineLimit}\n" +
            Environment.StackTrace);
    }
}

[HarmonyPatch(typeof(TMP_InputField), nameof(TMP_InputField.OnDeselect))]
internal static class SharedInputDeselectTracePatch
{
    [HarmonyPrefix]
    private static void Prefix(TMP_InputField __instance, BaseEventData eventData)
    {
        if (!SharedInputReturnCompatibility.IsTracked(__instance))
            return;
        DeepSpaceChinesePlugin.Instance?.PluginLog.LogInfo(
            $"[DEBUG-focus34] OnDeselect frame={Time.frameCount} id={__instance.GetInstanceID()} " +
            $"focused={__instance.isFocused} event={eventData?.GetType().Name ?? "<null>"}\n" +
            Environment.StackTrace);
    }
}

[HarmonyPatch(typeof(TMP_InputField), "ActivateInputFieldInternal")]
internal static class SharedInputActivateTracePatch
{
    [HarmonyPostfix]
    private static void Postfix(TMP_InputField __instance)
    {
        if (!SharedInputReturnCompatibility.IsTracked(__instance) ||
            !SharedInputReturnCompatibility.MarkActivationForLog(__instance))
            return;
        DeepSpaceChinesePlugin.Instance?.PluginLog.LogInfo(
            $"[DEBUG-focus34] first ActivateInputFieldInternal frame={Time.frameCount} " +
            $"id={__instance.GetInstanceID()} focused={__instance.isFocused} " +
            $"lineType={__instance.lineType} lineLimit={__instance.lineLimit}");
    }
}
