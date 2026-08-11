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

    internal static void Track(TMP_InputField input)
    {
        if (input != null)
            TrackedInputIds.Add(input.GetInstanceID());
    }

    internal static bool IsTracked(TMP_InputField input) =>
        input != null && TrackedInputIds.Contains(input.GetInstanceID());

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

    internal static bool ShouldSkipTextUpdateForSubmitShortcut(
        bool isTrackedSharedInput, bool isFocused, bool isMultiLineNewline,
        bool returnPressed, bool controlPressed)
    {
        // The game binds Ctrl+Enter to submission independently of TMP. If TMP
        // processes the same key first, it inserts an LF at the caret and the
        // compiler receives a modified answer. Skip only that one text-update
        // frame; plain Enter continues through TMP's multiline newline path.
        return isTrackedSharedInput && isFocused && isMultiLineNewline &&
               returnPressed && controlPressed;
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

[HarmonyPatch(typeof(TMP_InputField), nameof(TMP_InputField.OnUpdateSelected))]
internal static class SharedMultilineSubmitShortcutPatch
{
    [HarmonyPrefix]
    private static bool Prefix(TMP_InputField __instance, BaseEventData eventData)
    {
        bool returnPressed = Input.GetKeyDown(KeyCode.Return) ||
                             Input.GetKeyDown(KeyCode.KeypadEnter);
        bool controlPressed = Input.GetKey(KeyCode.LeftControl) ||
                              Input.GetKey(KeyCode.RightControl);
        bool skip = SharedInputReturnCompatibility.ShouldSkipTextUpdateForSubmitShortcut(
            SharedInputReturnCompatibility.IsTracked(__instance), __instance.isFocused,
            __instance.lineType == TMP_InputField.LineType.MultiLineNewline,
            returnPressed, controlPressed);
        return !skip;
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
        return !suppress;
    }
}
