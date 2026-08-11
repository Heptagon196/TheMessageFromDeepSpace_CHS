using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using TMPro;
using UnityEngine;

namespace DeepSpaceChinese;

internal readonly struct MainMenuButtonLayout
{
    public MainMenuButtonLayout(float rootPositionX, float rootScaleX,
        float childScaleX, float iconCenterX)
    {
        RootPositionX = rootPositionX;
        RootScaleX = rootScaleX;
        ChildScaleX = childScaleX;
        IconCenterX = iconCenterX;
    }

    public float RootPositionX { get; }
    public float RootScaleX { get; }
    public float ChildScaleX { get; }
    public float IconCenterX { get; }
}

internal static class MainMenuButtonLayoutEngine
{
    public static MainMenuButtonLayout Calculate(float originalParentScaleX,
        float originalChildScaleX, float referencePositionX, float referenceScaleX,
        float labelRightX, float visualGapX, float iconHalfWidthX)
    {
        float childScaleX = Math.Abs(referenceScaleX) < 0.00001f
            ? originalChildScaleX
            : originalChildScaleX * originalParentScaleX / referenceScaleX;
        return new MainMenuButtonLayout(referencePositionX, referenceScaleX, childScaleX,
            labelRightX + visualGapX + iconHalfWidthX);
    }
}

internal sealed class MainMenuLayoutRuntime
{
    private const string TabsListName = "Tabs List";
    private const string TabsWindowName = "Tabs Window";
    private const string ReferenceTabName = "Puzzle Log Tab";
    private const float ChineseGapEm = 0.5f;

    private readonly PatchConfig _config;
    private readonly ManualLogSource _log;
    private readonly Dictionary<int, SavedTabLayout> _saved = new();

    public MainMenuLayoutRuntime(PatchConfig config, ManualLogSource log)
    {
        _config = config;
        _log = log;
    }

    public void ApplyAll()
    {
        List<SavedTabLayout> tabs = FindTabs();
        if (tabs.Count == 0)
            return;

        Restore(tabs);
        if (!_config.TranslateUI || _config.DisplayMode != DisplayMode.TranslationOnly)
            return;

        SavedTabLayout reference = tabs.FirstOrDefault(tab =>
            tab.Root.name == ReferenceTabName);
        if (reference == null)
        {
            _log.LogWarning("未找到“传输”菜单按钮，跳过中文主菜单等宽布局。");
            return;
        }

        Transform list = reference.Root.parent;
        foreach (SavedTabLayout tab in tabs)
            NormalizeButton(tab, reference);

        float visualGap = MeasureChineseGlyphWidth(reference.Text, list) * ChineseGapEm;
        if (!(visualGap > 0f) || float.IsInfinity(visualGap) || float.IsNaN(visualGap))
            visualGap = 0.025f;
        foreach (SavedTabLayout tab in tabs)
            PlaceIconAfterText(tab, list, visualGap);

        _log.LogInfo($"中文主菜单布局已应用：{tabs.Count} 个按钮与“传输”等宽，图标间距 {visualGap:F4}。");
    }

    public void RestoreAll()
    {
        Restore(_saved.Values.Where(tab => tab.IsAlive).ToList());
    }

    private List<SavedTabLayout> FindTabs()
    {
        var result = new List<SavedTabLayout>();
        foreach (TMP_Text text in Resources.FindObjectsOfTypeAll<TMP_Text>())
        {
            if (text == null || text.name != "Text" || !text.gameObject.scene.IsValid() ||
                text.gameObject.scene.name != "ControlRoom")
                continue;
            Transform root = text.transform.parent;
            Transform list = root?.parent;
            if (root == null || list == null || list.name != TabsListName ||
                list.parent == null || list.parent.name != TabsWindowName ||
                !root.name.EndsWith(" Tab", StringComparison.Ordinal))
                continue;
            Transform icon = root.Find("Icon");
            if (icon == null || root.GetComponent<MeshFilter>() == null)
                continue;

            int id = root.GetInstanceID();
            if (!_saved.TryGetValue(id, out SavedTabLayout saved) || !saved.IsAlive)
            {
                saved = new SavedTabLayout(root, text, icon);
                _saved[id] = saved;
            }
            result.Add(saved);
        }
        return result;
    }

    private static void NormalizeButton(SavedTabLayout tab, SavedTabLayout reference)
    {
        Vector3 textWorldPosition = tab.Text.transform.position;
        MainMenuButtonLayout textLayout = MainMenuButtonLayoutEngine.Calculate(
            tab.RootScale.x, tab.TextScale.x,
            reference.RootPosition.x, reference.RootScale.x,
            0f, 0f, 0f);
        MainMenuButtonLayout iconLayout = MainMenuButtonLayoutEngine.Calculate(
            tab.RootScale.x, tab.IconScale.x,
            reference.RootPosition.x, reference.RootScale.x,
            0f, 0f, 0f);

        Vector3 rootPosition = tab.RootPosition;
        rootPosition.x = textLayout.RootPositionX;
        tab.Root.localPosition = rootPosition;
        Vector3 rootScale = tab.RootScale;
        rootScale.x = textLayout.RootScaleX;
        tab.Root.localScale = rootScale;

        Vector3 textScale = tab.TextScale;
        textScale.x = textLayout.ChildScaleX;
        tab.Text.transform.localScale = textScale;
        tab.Text.transform.position = textWorldPosition;

        Vector3 iconScale = tab.IconScale;
        iconScale.x = iconLayout.ChildScaleX;
        tab.Icon.localScale = iconScale;
        tab.Icon.localPosition = tab.IconPosition;
    }

    private static void PlaceIconAfterText(SavedTabLayout tab, Transform list,
        float visualGap)
    {
        tab.Text.ForceMeshUpdate(true, true);
        float textRight = BoundsMaxX(tab.Text.transform, tab.Text.textBounds, list);
        Bounds iconBounds = GetLocalBounds(tab.Icon);
        float iconHalfWidth = BoundsWidthX(tab.Icon, iconBounds, list) * 0.5f;
        MainMenuButtonLayout layout = MainMenuButtonLayoutEngine.Calculate(
            tab.Root.localScale.x, tab.Icon.localScale.x,
            tab.Root.localPosition.x, tab.Root.localScale.x,
            textRight, visualGap, iconHalfWidth);
        Vector3 center = list.InverseTransformPoint(tab.Icon.position);
        center.x = layout.IconCenterX;
        tab.Icon.position = list.TransformPoint(center);
    }

    private static float MeasureChineseGlyphWidth(TMP_Text text, Transform list)
    {
        float localWidth = text.GetPreferredValues("传").x;
        Vector3 start = list.InverseTransformPoint(text.transform.TransformPoint(Vector3.zero));
        Vector3 end = list.InverseTransformPoint(
            text.transform.TransformPoint(new Vector3(localWidth, 0f, 0f)));
        return Math.Abs(end.x - start.x);
    }

    private static float BoundsMaxX(Transform owner, Bounds bounds, Transform target)
    {
        float max = float.NegativeInfinity;
        foreach (Vector3 corner in BoundsCorners(bounds))
            max = Math.Max(max, target.InverseTransformPoint(owner.TransformPoint(corner)).x);
        return max;
    }

    private static float BoundsWidthX(Transform owner, Bounds bounds, Transform target)
    {
        float min = float.PositiveInfinity;
        float max = float.NegativeInfinity;
        foreach (Vector3 corner in BoundsCorners(bounds))
        {
            float x = target.InverseTransformPoint(owner.TransformPoint(corner)).x;
            min = Math.Min(min, x);
            max = Math.Max(max, x);
        }
        return max - min;
    }

    private static IEnumerable<Vector3> BoundsCorners(Bounds bounds)
    {
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;
        for (int x = 0; x < 2; x++)
        for (int y = 0; y < 2; y++)
        for (int z = 0; z < 2; z++)
            yield return new Vector3(x == 0 ? min.x : max.x,
                y == 0 ? min.y : max.y, z == 0 ? min.z : max.z);
    }

    private static Bounds GetLocalBounds(Transform transform)
    {
        MeshFilter mesh = transform.GetComponent<MeshFilter>();
        if (mesh != null && mesh.sharedMesh != null)
            return mesh.sharedMesh.bounds;
        Renderer renderer = transform.GetComponent<Renderer>();
        if (renderer != null)
            return new Bounds(Vector3.zero, Vector3.one);
        return new Bounds(Vector3.zero, Vector3.zero);
    }

    private static void Restore(IReadOnlyCollection<SavedTabLayout> tabs)
    {
        foreach (SavedTabLayout tab in tabs)
            tab.Restore();
    }

    private sealed class SavedTabLayout
    {
        public SavedTabLayout(Transform root, TMP_Text text, Transform icon)
        {
            Root = root;
            Text = text;
            Icon = icon;
            RootPosition = root.localPosition;
            RootScale = root.localScale;
            TextPosition = text.transform.localPosition;
            TextScale = text.transform.localScale;
            TextAnchoredPosition = text.rectTransform.anchoredPosition;
            IconPosition = icon.localPosition;
            IconScale = icon.localScale;
        }

        public Transform Root { get; }
        public TMP_Text Text { get; }
        public Transform Icon { get; }
        public Vector3 RootPosition { get; }
        public Vector3 RootScale { get; }
        public Vector3 TextPosition { get; }
        public Vector3 TextScale { get; }
        public Vector2 TextAnchoredPosition { get; }
        public Vector3 IconPosition { get; }
        public Vector3 IconScale { get; }
        public bool IsAlive => Root != null && Text != null && Icon != null;

        public void Restore()
        {
            if (!IsAlive)
                return;
            Root.localPosition = RootPosition;
            Root.localScale = RootScale;
            Text.transform.localScale = TextScale;
            Text.rectTransform.anchoredPosition = TextAnchoredPosition;
            Vector3 textPosition = Text.transform.localPosition;
            textPosition.z = TextPosition.z;
            Text.transform.localPosition = textPosition;
            Icon.localScale = IconScale;
            Icon.localPosition = IconPosition;
        }
    }
}
