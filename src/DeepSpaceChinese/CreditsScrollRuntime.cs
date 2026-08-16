using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace DeepSpaceChinese;

internal static class CreditsScrollRuntime
{
    private sealed class EndpointState
    {
        public CreditsSequence Credits;
        public Transform End;
        public Vector3 BasePosition;
    }

    private static readonly Dictionary<int, EndpointState> Endpoints = new();

    public static void Prepare(CreditsSequence credits)
    {
        if (credits == null || credits.endPos == null || credits.startPos == null)
            return;

        Transform end = credits.endPos.transform;
        int id = credits.GetInstanceID();
        if (!Endpoints.TryGetValue(id, out EndpointState state) ||
            !ReferenceEquals(state.Credits, credits) || !ReferenceEquals(state.End, end))
        {
            state = new EndpointState
            {
                Credits = credits,
                End = end,
                BasePosition = end.position
            };
            Endpoints[id] = state;
        }

        // Always start from the authored endpoint so F6 replays cannot accumulate offsets.
        end.position = state.BasePosition;
        TMP_Text text = FindMainCreditsText(credits);
        if (text == null)
            return;

        string original = DeepSpaceChinesePlugin.Instance?.OriginalTextForLayout(text);
        string localized = text.text ?? string.Empty;
        if (string.IsNullOrEmpty(original) || localized == original)
            return;

        float width = Math.Max(1f, text.rectTransform.rect.width);
        float originalHeight = text.GetPreferredValues(original, width, 0f).y;
        float localizedHeight = text.GetPreferredValues(localized, width, 0f).y;
        float extraLocal = CalculateAdditionalDistanceForTests(originalHeight,
            localizedHeight);
        if (extraLocal <= 0f)
            return;

        float extraWorld = Math.Abs(text.rectTransform.TransformVector(
            new Vector3(0f, extraLocal, 0f)).y);
        float direction = Math.Sign(state.BasePosition.y - credits.startPos.transform.position.y);
        if (direction == 0f)
            direction = 1f;
        Vector3 adjusted = state.BasePosition;
        adjusted.y += direction * extraWorld;
        end.position = adjusted;
    }

    private static TMP_Text FindMainCreditsText(CreditsSequence credits)
    {
        IEnumerable<TMP_Text> candidates = credits.credits1Text != null
            ? credits.credits1Text.GetComponentsInChildren<TMP_Text>(true)
            : credits.creditsObject != null
                ? credits.creditsObject.GetComponentsInChildren<TMP_Text>(true)
                : Array.Empty<TMP_Text>();
        return candidates
            .Where(item => item != null && !ReferenceEquals(item, credits.finalMessageLabel))
            .OrderByDescending(item =>
                (DeepSpaceChinesePlugin.Instance?.OriginalTextForLayout(item) ?? item.text ??
                 string.Empty).Length)
            .FirstOrDefault();
    }

    internal static float CalculateAdditionalDistanceForTests(float originalHeight,
        float localizedHeight) => Math.Max(0f, localizedHeight - originalHeight);
}
