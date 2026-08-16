using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using BepInEx.Logging;
using TMPro;
using UnityEngine;

namespace DeepSpaceChinese;

internal static class ReferenceCopyButtonLayout
{
    internal static float PlaceAfterText(float textLeftX, float renderedTextWidth, float gap) =>
        textLeftX + renderedTextWidth + gap;

    internal static int MatchScore(string buttonName, string line, string copyValue,
        string textObjectName = "")
    {
        string normalizedValue = Normalize(copyValue);
        string normalizedLine = Normalize(line);
        if (!string.IsNullOrEmpty(normalizedValue) && normalizedLine.Contains(normalizedValue))
            return 2000 + normalizedValue.Length;

        int objectScore = MatchObjectScore(buttonName, textObjectName);
        if (objectScore > 0)
            return objectScore;

        return buttonName switch
        {
            "Copy Avogadro's Number" when
                normalizedLine.Contains("6.02214076*10^23") ||
                normalizedLine.Contains("6.02214076X10^23") => 1900,
            "Copy Sphere Volume Ratio" when StartsWithFormulaVariable(line, "V") => 1850,
            "Copy Sphere SA Ratio" when StartsWithFormulaVariable(line, "A") => 1850,
            "Copy Mass" when StartsWithEither(line, "Mass:", "质量：") => 900,
            "Copy Volume" when StartsWithEither(line, "Volume:", "体积：") => 900,
            "Copy Density" when StartsWithEither(line, "Avg Density:", "平均密度：") => 900,
            "Copy Length" when StartsWithEither(line, "Length:", "长度：") => 900,
            "Copy Width" when StartsWithEither(line, "Width:", "宽度：") => 900,
            "Copy Height" when StartsWithEither(line, "Height:", "高度：") => 900,
            _ => 0,
        };
    }

    internal static int MatchObjectScore(string buttonName, string textObjectName)
    {
        string expected = buttonName switch
        {
            "Copy Second Conversion" => "Helisec",
            "Copy Meters per Heter" => "Heter",
            "Copy Meters per Miniheter" => "Miniheter",
            "Copy 8^9" => "8 ^ 9",
            "Copy Kilograms per Filogram" => "Filogram",
            "Copy Filograms per MFG" => "Filo -> MFG",
            "Copy SOL" => "ABOVE STUFF",
            _ => string.Empty,
        };
        return !string.IsNullOrEmpty(expected) &&
               string.Equals(NormalizeIdentifier(textObjectName),
                   NormalizeIdentifier(expected), StringComparison.Ordinal)
            ? 1500
            : 0;
    }

    private static bool StartsWithEither(string value, string first, string second) =>
        value.TrimStart().StartsWith(first, StringComparison.OrdinalIgnoreCase) ||
        value.TrimStart().StartsWith(second, StringComparison.Ordinal);

    private static bool StartsWithFormulaVariable(string value, string variable)
    {
        string trimmed = value?.TrimStart() ?? string.Empty;
        return trimmed.StartsWith(variable + " =", StringComparison.OrdinalIgnoreCase) ||
               trimmed.StartsWith(variable + "=", StringComparison.OrdinalIgnoreCase);
    }

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

    private static string NormalizeIdentifier(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;
        var result = new StringBuilder(value.Length);
        foreach (char character in value)
            if (char.IsLetterOrDigit(character))
                result.Append(char.ToUpperInvariant(character));
        return result.ToString();
    }
}

internal static class ReferencePageFontPolicy
{
    internal static bool UseDirectChineseFont(string text)
    {
        if (string.IsNullOrEmpty(text))
            return false;
        foreach (char character in text)
        {
            if (character is >= '\u3400' and <= '\u9fff' or >= '\uf900' and <= '\ufaff')
                return true;
        }
        return false;
    }

    internal static bool PreserveOriginalMetrics(string pageName) =>
        !string.IsNullOrEmpty(pageName);

    internal static bool MayShrinkStaticText(string pageName, string originalText,
        string translatedText) =>
        !IsDynamicElementPage(pageName) &&
        !string.IsNullOrEmpty(originalText) &&
        !string.Equals(originalText, translatedText, StringComparison.Ordinal);

    internal static float ReadableLineSpacing(float originalLineSpacing, float fontSize,
        string originalText, string translatedText, string pageName)
    {
        // Fixed grids and formula diagrams rely on authored row baselines. Increasing the
        // generic Chinese paragraph spacing makes the last truth-table row fall below its
        // bottom rule even though every row still has the correct column anchor.
        if (IsDynamicElementPage(pageName) || IsFixedStructurePage(pageName) ||
            string.IsNullOrEmpty(originalText) ||
            !UseDirectChineseFont(translatedText) ||
            originalText.IndexOf('\n') < 0)
            return originalLineSpacing;
        return Math.Max(originalLineSpacing, fontSize * 0.18f);
    }

    private static bool IsDynamicElementPage(string pageName) =>
        string.Equals(pageName, "ELEMENT DISPLAY", StringComparison.OrdinalIgnoreCase) ||
        pageName.IndexOf("PERIODIC", StringComparison.OrdinalIgnoreCase) >= 0;

    private static bool IsFixedStructurePage(string pageName) =>
        string.Equals(pageName, "LOGIC PAGE", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(pageName, "DISTANCE PAGE", StringComparison.OrdinalIgnoreCase);
}

internal static class ReferencePageTextFormatter
{
    private static readonly string[] TruthColumnPositions = { "7%", "27%", "49%", "74%" };

    internal static string FormatTruthTable(string text)
    {
        if (string.IsNullOrEmpty(text) || text.Contains("<pos="))
            return text;
        string[] lines = text.Replace("\r\n", "\n").Split('\n');
        for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            if (string.IsNullOrWhiteSpace(lines[lineIndex]))
                continue;
            string[] cells = System.Text.RegularExpressions.Regex.Split(
                lines[lineIndex].Trim(), @"\s{2,}");
            if (cells.Length != TruthColumnPositions.Length)
                continue;
            var formatted = new StringBuilder();
            for (int cellIndex = 0; cellIndex < cells.Length; cellIndex++)
                formatted.Append("<pos=").Append(TruthColumnPositions[cellIndex])
                    .Append('>').Append(cells[cellIndex]);
            lines[lineIndex] = formatted.ToString();
        }
        return string.Join("\n", lines);
    }

    internal static string FormatDistanceFormula(string objectName, string text)
    {
        if (string.IsNullOrEmpty(text) || text.Contains("<sup>"))
            return text;
        return objectName switch
        {
            "ABOVE STUFF" => text
                .Replace("(x1 - x2) + (y1 - y2)",
                    "(x1 - x2)<sup>2</sup> + (y1 - y2)<sup>2</sup>"),
            "ABOVE STUFF (1)" => text.Replace("a + b = c",
                "a<sup>2</sup> + b<sup>2</sup> = c<sup>2</sup>"),
            "ABOVE STUFF (2)" => text.Replace("3 + 4 = 5",
                "3<sup>2</sup> + 4<sup>2</sup> = 5<sup>2</sup>"),
            _ => text,
        };
    }

    internal static bool ShouldFitSingleLineNote(float fontSize, float rectWidth,
        int originalLogicalLines) =>
        originalLogicalLines == 1 && fontSize <= 0.65f && rectWidth >= 1.4f;

    internal static bool ShouldFitOriginalLineBudget(int originalRenderedLines,
        int translatedRenderedLines) =>
        originalRenderedLines > 0 && translatedRenderedLines > originalRenderedLines;

    internal static bool IsFormulaAnnotation(string text)
    {
        string value = (text ?? string.Empty).Trim();
        return value.Length is > 2 and <= 18 && value.Contains('/') &&
               !value.Contains('\n');
    }
}

internal static class ReferencePageLayoutEngine
{
    internal readonly struct VerticalBounds
    {
        internal VerticalBounds(float top, float bottom)
        {
            Top = top;
            Bottom = bottom;
        }

        internal float Top { get; }
        internal float Bottom { get; }
    }

    internal readonly struct LayoutBounds
    {
        internal LayoutBounds(float left, float right, float top, float bottom)
        {
            Left = left;
            Right = right;
            Top = top;
            Bottom = bottom;
        }

        internal float Left { get; }
        internal float Right { get; }
        internal float Top { get; }
        internal float Bottom { get; }
    }

    internal static Vector2 PlaceAtLineEnd(Vector2 lineEnd, Vector2 gap) =>
        lineEnd + gap;

    internal static Vector2 MapToTranslatedBaseline(Vector2 originalPosition,
        float originalBaselineY, float translatedBaselineY) =>
        new(originalPosition.x,
            translatedBaselineY + originalPosition.y - originalBaselineY);

    internal static int FindNearestBaseline(IReadOnlyList<float> baselines, float y)
    {
        if (baselines == null || baselines.Count == 0)
            return -1;
        int best = 0;
        float bestDistance = Math.Abs(baselines[0] - y);
        for (int index = 1; index < baselines.Count; index++)
        {
            float distance = Math.Abs(baselines[index] - y);
            if (distance >= bestDistance)
                continue;
            best = index;
            bestDistance = distance;
        }
        return best;
    }

    internal static int FindNearestPosition(IReadOnlyList<Vector2> positions, Vector2 point)
    {
        if (positions == null || positions.Count == 0)
            return -1;
        int best = 0;
        float bestDistance = (positions[0] - point).sqrMagnitude;
        for (int index = 1; index < positions.Count; index++)
        {
            float distance = (positions[index] - point).sqrMagnitude;
            if (distance >= bestDistance)
                continue;
            best = index;
            bestDistance = distance;
        }
        return best;
    }

    internal static float ExtendContentHeight(float originalHeight,
        float originalBottomY, float translatedBottomY) =>
        originalHeight + Math.Max(0f, originalBottomY - translatedBottomY);

    internal static float ExtendContentHeightForRenderedBottom(float originalHeight,
        float originalBottomY, float translatedBottomY, float bottomPadding) =>
        originalHeight + Math.Max(0f, originalBottomY - translatedBottomY) +
        Math.Max(0f, bottomPadding);

    internal static float HeightForTranslatedBounds(float minimumHeight,
        float originalHeight, float originalBottomY, float translatedBottomY) =>
        Math.Max(minimumHeight,
            originalHeight + originalBottomY - translatedBottomY);

    internal static bool ShouldTrackAsOverlay(bool hasText, bool hasCopyButton) =>
        !hasText && !hasCopyButton;

    internal static float LineVisualCenter(float ascender, float descender) =>
        (ascender + descender) * 0.5f;

    internal static int ChooseBestLine(IReadOnlyList<int> scores,
        IReadOnlyList<float> lineCenters, float originalY)
    {
        if (scores == null || lineCenters == null || scores.Count == 0 ||
            scores.Count != lineCenters.Count)
            return -1;
        int best = -1;
        int bestScore = 0;
        float bestDistance = float.MaxValue;
        for (int index = 0; index < scores.Count; index++)
        {
            float distance = Math.Abs(lineCenters[index] - originalY);
            if (scores[index] < bestScore ||
                (scores[index] == bestScore && distance >= bestDistance))
                continue;
            best = index;
            bestScore = scores[index];
            bestDistance = distance;
        }
        return bestScore > 0 ? best : -1;
    }

    internal static int[] AssignUniqueLines(int[,] scores, float[,] distances)
    {
        int buttonCount = scores?.GetLength(0) ?? 0;
        int lineCount = scores?.GetLength(1) ?? 0;
        if (buttonCount == 0 || lineCount == 0 || distances == null ||
            distances.GetLength(0) != buttonCount || distances.GetLength(1) != lineCount)
            return Array.Empty<int>();
        var assignments = Enumerable.Repeat(-1, buttonCount).ToArray();
        var usedLines = new bool[lineCount];
        while (true)
        {
            int bestButton = -1;
            int bestLine = -1;
            int bestScore = 0;
            float bestDistance = float.MaxValue;
            for (int button = 0; button < buttonCount; button++)
            {
                if (assignments[button] >= 0)
                    continue;
                for (int line = 0; line < lineCount; line++)
                {
                    int score = scores[button, line];
                    float distance = distances[button, line];
                    if (usedLines[line] || score < bestScore ||
                        (score == bestScore && distance >= bestDistance))
                        continue;
                    bestButton = button;
                    bestLine = line;
                    bestScore = score;
                    bestDistance = distance;
                }
            }
            if (bestButton < 0 || bestScore <= 0)
                break;
            assignments[bestButton] = bestLine;
            usedLines[bestLine] = true;
        }
        return assignments;
    }

    internal static float[] ArrangeVerticalBlocks(
        IReadOnlyList<VerticalBounds> originalBounds,
        IReadOnlyList<VerticalBounds> renderedBounds, float minimumGap)
    {
        if (originalBounds == null || renderedBounds == null ||
            originalBounds.Count != renderedBounds.Count)
            return Array.Empty<float>();
        var shifts = new float[originalBounds.Count];
        if (shifts.Length < 2)
            return shifts;
        float previousBottom = renderedBounds[0].Bottom;
        for (int index = 1; index < shifts.Length; index++)
        {
            float originalGap = Math.Max(minimumGap,
                originalBounds[index - 1].Bottom - originalBounds[index].Top);
            float targetTop = previousBottom - originalGap;
            float shift = Math.Min(0f, targetTop - renderedBounds[index].Top);
            shifts[index] = shift;
            previousBottom = renderedBounds[index].Bottom + shift;
        }
        return shifts;
    }

    internal static float[] ArrangeVerticalRows(
        IReadOnlyList<LayoutBounds> originalBounds,
        IReadOnlyList<LayoutBounds> renderedBounds, float minimumGap)
    {
        if (originalBounds == null || renderedBounds == null ||
            originalBounds.Count != renderedBounds.Count)
            return Array.Empty<float>();
        int count = originalBounds.Count;
        var shifts = new float[count];
        if (count < 2)
            return shifts;

        // A number of authored pages use two independent text objects as the left
        // introduction and the right KEY column. They occupy the same original row and
        // must move as one unit; treating them as consecutive paragraphs pushes KEY and
        // every following diagram down by an entire block.
        var parent = Enumerable.Range(0, count).ToArray();
        int Find(int value)
        {
            while (parent[value] != value)
            {
                parent[value] = parent[parent[value]];
                value = parent[value];
            }
            return value;
        }
        void Union(int left, int right)
        {
            left = Find(left);
            right = Find(right);
            if (left != right)
                parent[right] = left;
        }
        for (int left = 0; left < count; left++)
        {
            LayoutBounds a = originalBounds[left];
            float aWidth = Math.Max(0.001f, a.Right - a.Left);
            for (int right = left + 1; right < count; right++)
            {
                LayoutBounds b = originalBounds[right];
                float verticalOverlap = Math.Min(a.Top, b.Top) -
                                        Math.Max(a.Bottom, b.Bottom);
                if (verticalOverlap <= 0.01f)
                    continue;
                float bWidth = Math.Max(0.001f, b.Right - b.Left);
                float requiredSeparation = Math.Max(0.1f,
                    Math.Min(aWidth, bWidth) * 0.45f);
                if (Math.Abs(a.Left - b.Left) < requiredSeparation)
                    continue;
                Union(left, right);
            }
        }

        var rows = Enumerable.Range(0, count)
            .GroupBy(Find)
            .Select(group => group.ToArray())
            .OrderByDescending(group => group.Max(index => originalBounds[index].Top))
            .ToArray();
        var originalRows = new List<VerticalBounds>(rows.Length);
        var renderedRows = new List<VerticalBounds>(rows.Length);
        foreach (int[] row in rows)
        {
            originalRows.Add(new VerticalBounds(
                row.Max(index => originalBounds[index].Top),
                row.Min(index => originalBounds[index].Bottom)));
            renderedRows.Add(new VerticalBounds(
                row.Max(index => renderedBounds[index].Top),
                row.Min(index => renderedBounds[index].Bottom)));
        }
        float[] rowShifts = ArrangeVerticalBlocks(originalRows, renderedRows, minimumGap);
        for (int rowIndex = 0; rowIndex < rows.Length; rowIndex++)
            foreach (int blockIndex in rows[rowIndex])
                shifts[blockIndex] = rowShifts[rowIndex];
        return shifts;
    }

    internal static float ShiftTextOutsideGraphics(LayoutBounds originalText,
        LayoutBounds renderedText, IReadOnlyList<LayoutBounds> graphics, float gap)
    {
        if (graphics == null || graphics.Count == 0)
            return 0f;
        float shift = 0f;
        foreach (LayoutBounds graphic in graphics)
        {
            if (originalText.Right <= graphic.Left || originalText.Left >= graphic.Right)
                continue;
            if (originalText.Bottom >= graphic.Top)
                shift = Math.Max(shift, graphic.Top + gap - renderedText.Bottom);
            else if (originalText.Top <= graphic.Bottom)
                shift = Math.Min(shift, graphic.Bottom - gap - renderedText.Top);
        }
        return shift;
    }
}

internal sealed class ReferencePageLayoutRuntime
{
    private static readonly FieldInfo FullInfoHeightField =
        typeof(ReferenceSubWindow).GetField("fullInfoHeight",
            BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo WindowHeightField =
        typeof(ReferenceSubWindow).GetField("windowHeight",
            BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo ScrollAreaField =
        typeof(ReferenceSubWindow).GetField("scrollArea",
            BindingFlags.Instance | BindingFlags.NonPublic);
    private readonly PatchConfig _config;
    private readonly ManualLogSource _log;
    private readonly FontFallback _font;
    private readonly UiLocalizer _ui;
    private readonly Dictionary<int, SavedButtonPosition> _originalButtons = new();
    private readonly Dictionary<int, SavedActiveState> _formulaParts = new();
    private readonly Dictionary<int, SavedTextPresentation> _textPresentations = new();
    private readonly Dictionary<int, SavedAreaLayout> _areas = new();

    internal ReferencePageLayoutRuntime(PatchConfig config, ManualLogSource log,
        FontFallback font, UiLocalizer ui)
    {
        _config = config;
        _log = log;
        _font = font;
        _ui = ui;
    }

    internal void CaptureAll()
    {
        if (_config?.Enabled != true)
            return;
        int capturedAreas = 0;
        int capturedFlows = 0;
        int capturedOverlays = 0;
        foreach (ScrollArea area in Resources.FindObjectsOfTypeAll<ScrollArea>())
        {
            if (area == null || !area.gameObject.scene.IsValid() ||
                !IsUnderReferenceWindow(area.transform))
                continue;
            int id = area.GetInstanceID();
            if (_areas.TryGetValue(id, out SavedAreaLayout existing) &&
                existing.Area != null && ReferenceEquals(existing.Area, area))
                continue;
            SavedAreaLayout captured = CaptureArea(area);
            if (captured != null)
            {
                _areas[id] = captured;
                capturedAreas++;
                capturedFlows += captured.Flows.Count;
                capturedOverlays += captured.Overlays.Count;
            }
        }
        RemoveDeadObjects();
        if (capturedAreas > 0)
            _log?.LogInfo($"参考页布局已采样：区域 {capturedAreas}，文本流 {capturedFlows}，" +
                          $"附属对象 {capturedOverlays}。");
    }

    internal void ApplyAll()
    {
        if (_config?.Enabled != true || _config.DisplayMode != DisplayMode.TranslationOnly)
        {
            RestoreAll();
            return;
        }

        RestoreLayoutBaseline();
        CaptureAll();
        CaptureButtonAnchors();
        int specialTextCount = ApplySpecialTextLayouts();
        int fontCount = ApplyReferenceFonts(useChineseFont: true);
        int fittedTextCount = FitAllTextToOriginalLineBudgets();
        int spacedTextCount = ApplyReadableLineSpacing();
        spacedTextCount += ApplyFixedStructureTextMetrics();
        int blockCount = 0;
        foreach (SavedAreaLayout area in _areas.Values)
            blockCount += ArrangeContentBlocks(area);
        int overlayCount = 0;
        foreach (SavedAreaLayout area in _areas.Values)
            overlayCount += ApplyArea(area);
        // Both content reflow and overlay baseline mapping can move objects away from their
        // original English coordinates. Resolve text/graphic collisions only after those
        // transformations, using the graphics' current bounds.
        foreach (SavedAreaLayout area in _areas.Values)
            blockCount += AvoidFixedGraphics(area);

        int buttonCount = 0;
        foreach (ClipboardCopyButton button in Resources.FindObjectsOfTypeAll<ClipboardCopyButton>())
        {
            if (button == null || !button.gameObject.scene.IsValid() ||
                !IsUnderReferenceWindow(button.transform))
                continue;
            int id = button.GetInstanceID();
            Transform parent = button.transform.parent;
            if (parent == null)
                continue;
            if (!_originalButtons.TryGetValue(id, out SavedButtonPosition savedButton) ||
                savedButton.Button == null || !ReferenceEquals(savedButton.Button, button))
            {
                savedButton = CaptureButtonAnchor(button, parent);
                _originalButtons[id] = savedButton;
            }
            if (!TryResolveButtonLineEnd(savedButton, parent, out Vector2 lineEnd))
                continue;
            Vector3 position = button.transform.localPosition;
            float halfIconWidth = Math.Abs(button.transform.localScale.x) * 0.5f;
            Vector2 target = ReferencePageLayoutEngine.PlaceAtLineEnd(lineEnd,
                new Vector2(halfIconWidth, 0f));
            position.x = target.x;
            position.y = target.y;
            button.transform.localPosition = position;
            buttonCount++;
        }

        RemoveDeadObjects();
        foreach (SavedAreaLayout area in _areas.Values)
            UpdateScrollHeight(area);

        if (specialTextCount + fontCount + fittedTextCount + spacedTextCount +
            blockCount + overlayCount + buttonCount > 0)
            _log?.LogInfo($"参考页已统一重排：结构文本 {specialTextCount}，字体 {fontCount}，" +
                          $"行数适配 {fittedTextCount}，行距 {spacedTextCount}，" +
                          $"内容块 {blockCount}，" +
                          $"叠加对象 {overlayCount}，" +
                          $"复制按钮 {buttonCount}。");
    }

    internal Transform PageRootFor(Transform child)
    {
        if (child == null)
            return null;
        ScrollArea parentArea = child.GetComponentInParent<ScrollArea>(true);
        if (parentArea != null && IsUnderReferenceWindow(parentArea.transform))
            return parentArea.transform;
        SavedAreaLayout saved = _areas.Values.FirstOrDefault(value =>
            value?.Area != null && child.IsChildOf(value.Area.transform));
        return saved?.Area?.transform;
    }

    internal void ApplyFor(ReferenceSubWindow subWindow)
    {
        ScrollArea area = subWindow == null ? null : ScrollAreaField?.GetValue(subWindow) as ScrollArea;
        if (area == null)
            area = subWindow?.GetComponentInChildren<ScrollArea>(true);
        ApplyFor(area);
    }

    internal void ApplyContaining(Transform child)
    {
        Transform root = PageRootFor(child);
        ApplyFor(root == null ? null : root.GetComponent<ScrollArea>());
    }

    private void ApplyFor(ScrollArea area)
    {
        if (area == null || !area.gameObject.scene.IsValid() ||
            !IsUnderReferenceWindow(area.transform))
            return;
        SavedAreaLayout saved = EnsureArea(area);
        if (saved == null)
            return;
        if (_config?.Enabled != true || _config.DisplayMode != DisplayMode.TranslationOnly)
        {
            RestoreLayoutBaseline(saved);
            return;
        }

        Transform root = area.transform;
        RestoreLayoutBaseline(saved);
        CaptureButtonAnchors(root);
        int specialTextCount = ApplySpecialTextLayouts(root);
        int fontCount = ApplyReferenceFonts(true, root);
        int fittedTextCount = FitAllTextToOriginalLineBudgets(root);
        int spacedTextCount = ApplyReadableLineSpacing(root);
        spacedTextCount += ApplyFixedStructureTextMetrics(root);
        int blockCount = ArrangeContentBlocks(saved);
        int overlayCount = ApplyArea(saved);
        blockCount += AvoidFixedGraphics(saved);
        int buttonCount = ApplyCopyButtons(root);
        UpdateScrollHeight(saved);
        RemoveDeadObjects();

        if (specialTextCount + fontCount + fittedTextCount + spacedTextCount +
            blockCount + overlayCount + buttonCount > 0)
            _log?.LogInfo($"当前参考页已重排：结构文本 {specialTextCount}，字体 {fontCount}，" +
                          $"行数适配 {fittedTextCount}，行距 {spacedTextCount}，" +
                          $"内容块 {blockCount}，叠加对象 {overlayCount}，复制按钮 {buttonCount}。");
    }

    private SavedAreaLayout EnsureArea(ScrollArea area)
    {
        int id = area.GetInstanceID();
        if (_areas.TryGetValue(id, out SavedAreaLayout existing) &&
            existing?.Area != null && ReferenceEquals(existing.Area, area))
            return existing;
        SavedAreaLayout captured = CaptureArea(area);
        if (captured != null)
            _areas[id] = captured;
        return captured;
    }

    private int ApplyCopyButtons(Transform root)
    {
        int buttonCount = 0;
        foreach (ClipboardCopyButton button in root.GetComponentsInChildren<ClipboardCopyButton>(true))
        {
            if (button == null || button.transform.parent == null)
                continue;
            int id = button.GetInstanceID();
            Transform parent = button.transform.parent;
            if (!_originalButtons.TryGetValue(id, out SavedButtonPosition savedButton) ||
                savedButton.Button == null || !ReferenceEquals(savedButton.Button, button))
            {
                savedButton = CaptureButtonAnchor(button, parent);
                _originalButtons[id] = savedButton;
            }
            if (!TryResolveButtonLineEnd(savedButton, parent, out Vector2 lineEnd))
                continue;
            Vector3 position = button.transform.localPosition;
            float halfIconWidth = Math.Abs(button.transform.localScale.x) * 0.5f;
            Vector2 target = ReferencePageLayoutEngine.PlaceAtLineEnd(lineEnd,
                new Vector2(halfIconWidth, 0f));
            position.x = target.x;
            position.y = target.y;
            button.transform.localPosition = position;
            buttonCount++;
        }
        return buttonCount;
    }

    private void CaptureButtonAnchors(Transform root = null)
    {
        var groups = Resources.FindObjectsOfTypeAll<ClipboardCopyButton>()
            .Where(button => button != null && button.gameObject.scene.IsValid() &&
                             IsUnderReferenceWindow(button.transform) &&
                             (root == null || button.transform.IsChildOf(root)) &&
                             button.transform.parent != null)
            .GroupBy(button => button.transform.parent);
        foreach (IGrouping<Transform, ClipboardCopyButton> group in groups)
        {
            ClipboardCopyButton[] buttons = group.ToArray();
            CaptureButtonGroup(buttons, group.Key);
        }
    }

    private void CaptureButtonGroup(IReadOnlyList<ClipboardCopyButton> buttons,
        Transform parent)
    {
        List<ButtonLineCandidate> candidates = BuildButtonLineCandidates(parent);
        var scores = new int[buttons.Count, candidates.Count];
        var distances = new float[buttons.Count, candidates.Count];
        for (int buttonIndex = 0; buttonIndex < buttons.Count; buttonIndex++)
        {
            ClipboardCopyButton button = buttons[buttonIndex];
            for (int lineIndex = 0; lineIndex < candidates.Count; lineIndex++)
            {
                ButtonLineCandidate candidate = candidates[lineIndex];
                scores[buttonIndex, lineIndex] = ReferenceCopyButtonLayout.MatchScore(
                    button.name, candidate.TextValue, button.stringToCopy,
                    candidate.Text.name);
                distances[buttonIndex, lineIndex] = Math.Abs(
                    candidate.CenterY - button.transform.localPosition.y);
            }
        }
        int[] assignments = ReferencePageLayoutEngine.AssignUniqueLines(scores, distances);
        for (int buttonIndex = 0; buttonIndex < buttons.Count; buttonIndex++)
        {
            ClipboardCopyButton button = buttons[buttonIndex];
            int lineIndex = assignments.Length > buttonIndex ? assignments[buttonIndex] : -1;
            if (lineIndex < 0)
            {
                _originalButtons[button.GetInstanceID()] = new SavedButtonPosition(
                    button, button.transform.localPosition, null, -1, -1);
                continue;
            }
            ButtonLineCandidate candidate = candidates[lineIndex];
            _originalButtons[button.GetInstanceID()] = new SavedButtonPosition(
                button, button.transform.localPosition, candidate.Text,
                candidate.LogicalLine, candidate.WrapIndex);
        }
    }

    private List<ButtonLineCandidate> BuildButtonLineCandidates(Transform parent)
    {
        var candidates = new List<ButtonLineCandidate>();
        foreach (TMP_Text text in parent.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text == null)
                continue;
            string original = _ui?.OriginalTextForLayout(text) ?? string.Empty;
            if (string.IsNullOrEmpty(original))
                continue;
            try
            {
                MeasureTextInfo(text, original, info =>
                {
                    var wraps = new Dictionary<int, int>();
                    for (int lineIndex = 0; lineIndex < info.lineCount; lineIndex++)
                    {
                        int logicalLine = LogicalLineFor(info, lineIndex);
                        wraps.TryGetValue(logicalLine, out int wrapIndex);
                        wraps[logicalLine] = wrapIndex + 1;
                        TMP_LineInfo line = info.lineInfo[lineIndex];
                        float center = ReferencePageLayoutEngine.LineVisualCenter(
                            line.ascender, line.descender);
                        Vector3 worldCenter = text.transform.TransformPoint(
                            new Vector3(0f, center, 0f));
                        float parentCenterY = parent.InverseTransformPoint(worldCenter).y;
                        candidates.Add(new ButtonLineCandidate(text, logicalLine, wrapIndex,
                            GetLineText(info, lineIndex), parentCenterY));
                    }
                    return true;
                });
            }
            catch
            {
                continue;
            }
        }
        return candidates;
    }

    private void RestoreLayoutBaseline()
    {
        foreach (SavedAreaLayout saved in _areas.Values)
        {
            foreach (SavedLayoutBlock block in saved.Blocks)
                if (block.Transform != null)
                    block.Transform.localPosition = block.OriginalLocalPosition;
            foreach (SavedOverlay overlay in saved.Overlays)
                if (overlay.Transform != null)
                    overlay.Transform.localPosition = overlay.OriginalLocalPosition;
            if (saved.SubWindow != null && saved.OriginalFullInfoHeight > 0f)
                saved.SubWindow.FullInfoHeight = saved.OriginalFullInfoHeight;
        }
        foreach (SavedButtonPosition saved in _originalButtons.Values)
            if (saved.Button != null)
                saved.Button.transform.localPosition = saved.LocalPosition;
        foreach (SavedTextPresentation saved in _textPresentations.Values)
            saved.Restore();
    }

    private void RestoreLayoutBaseline(SavedAreaLayout saved)
    {
        if (saved?.Area == null)
            return;
        Transform root = saved.Area.transform;
        foreach (SavedLayoutBlock block in saved.Blocks)
            if (block.Transform != null)
                block.Transform.localPosition = block.OriginalLocalPosition;
        foreach (SavedOverlay overlay in saved.Overlays)
            if (overlay.Transform != null)
                overlay.Transform.localPosition = overlay.OriginalLocalPosition;
        if (saved.SubWindow != null && saved.OriginalFullInfoHeight > 0f)
            saved.SubWindow.FullInfoHeight = saved.OriginalFullInfoHeight;
        foreach (SavedButtonPosition button in _originalButtons.Values)
            if (button.Button != null && button.Button.transform.IsChildOf(root))
                button.Button.transform.localPosition = button.LocalPosition;
        foreach (SavedTextPresentation presentation in _textPresentations.Values)
            if (presentation.Text != null && presentation.Text.transform.IsChildOf(root))
                presentation.Restore();
    }

    internal void RestoreAll()
    {
        RestoreLayoutBaseline();
        foreach (SavedActiveState saved in _formulaParts.Values)
        {
            if (saved.GameObject != null)
                saved.GameObject.SetActive(saved.ActiveSelf);
        }
        ApplyReferenceFonts(useChineseFont: false);
        RemoveDeadObjects();
    }

    internal bool TryGetCopyButtonAnchor(ClipboardCopyButton button,
        out TMP_Text text, out int logicalLine, out int wrapIndex)
    {
        text = null;
        logicalLine = -1;
        wrapIndex = -1;
        if (button == null ||
            !_originalButtons.TryGetValue(button.GetInstanceID(), out SavedButtonPosition saved) ||
            saved?.AnchoredText == null)
            return false;
        text = saved.AnchoredText;
        logicalLine = saved.LogicalLine;
        wrapIndex = saved.WrapIndex;
        return true;
    }

    internal float OriginalFullInfoHeightFor(ReferenceSubWindow subWindow)
    {
        if (subWindow == null)
            return 0f;
        SavedAreaLayout saved = _areas.Values.FirstOrDefault(value =>
            value?.SubWindow != null && ReferenceEquals(value.SubWindow, subWindow));
        return saved?.OriginalFullInfoHeight > 0f
            ? saved.OriginalFullInfoHeight
            : ReadFullInfoHeight(subWindow);
    }

    private SavedAreaLayout CaptureArea(ScrollArea area)
    {
        List<TMP_Text> flowTexts = FindFlowTexts(area.transform);
        var flows = new List<SavedTextFlow>();
        foreach (TMP_Text flowText in flowTexts)
        {
            string original = _ui?.OriginalTextForLayout(flowText) ?? flowText.text ?? string.Empty;
            if (string.IsNullOrEmpty(original))
                continue;
            try
            {
                List<LineAnchor> originalLines = MeasureTextInfo(flowText, original,
                    originalInfo => BuildLineAnchors(flowText, area.transform, originalInfo));
                if (originalLines.Count >= 2)
                    flows.Add(new SavedTextFlow(flowText, originalLines));
            }
            catch (Exception ex)
            {
                _log?.LogWarning($"无法读取参考页原文排版 {BuildPath(flowText.transform)}：" +
                                 ex.GetBaseException().Message);
            }
        }
        var anchorPositions = new List<Vector2>();
        var anchorOwners = new List<(int FlowIndex, LineAnchor Anchor)>();
        for (int flowIndex = 0; flowIndex < flows.Count; flowIndex++)
        {
            foreach (LineAnchor anchor in flows[flowIndex].OriginalLines)
            {
                anchorPositions.Add(new Vector2(anchor.LocalX, anchor.LocalY));
                anchorOwners.Add((flowIndex, anchor));
            }
        }
        var overlays = new List<SavedOverlay>();
        foreach (Transform child in DirectChildren(area.transform))
        {
            if (flows.Any(flow => child == flow.Text.transform))
                continue;
            bool hasCopyButton = child.GetComponent<ClipboardCopyButton>() != null;
            TMP_Text childText = child.GetComponent<TMP_Text>();
            string childOriginal = childText == null ? string.Empty :
                (_ui?.OriginalTextForLayout(childText) ?? childText.text ?? string.Empty);
            bool formulaAnnotation = childText != null &&
                                     ReferencePageTextFormatter.IsFormulaAnnotation(childOriginal);
            bool hasGraphic = child.GetComponentsInChildren<Renderer>(true)
                .Any(renderer => renderer != null && renderer.GetComponent<TMP_Text>() == null);
            // Long reference texts deliberately contain blank lines that reserve space for
            // diagrams, captions and notes. Anchor every direct visual sibling to the nearest
            // original text line so it follows the translated line metrics instead of staying
            // at the English absolute Y coordinate. Fixed table/formula pages use their own
            // structural rules below.
            if ((!formulaAnnotation && childText == null && !hasGraphic) || hasCopyButton ||
                IsFixedStructurePage(area.transform))
                continue;
            Vector3 originalPosition = child.localPosition;
            int nearest = ReferencePageLayoutEngine.FindNearestPosition(anchorPositions,
                new Vector2(originalPosition.x, originalPosition.y));
            if (nearest < 0)
                continue;
            (int flowIndex, LineAnchor anchor) = anchorOwners[nearest];
            overlays.Add(new SavedOverlay(child, originalPosition,
                flowIndex, anchor.LogicalLine, anchor.WrapIndex, anchor.LocalY));
        }
        // Many detail pages keep their viewport and ScrollArea as a separate sibling tree;
        // the owning ReferenceSubWindow component lives on the category-list button and
        // points at that area through its serialized field. GetComponentInParent therefore
        // returns null for exactly the pages whose translated scroll range matters most.
        ReferenceSubWindow subWindow = Resources.FindObjectsOfTypeAll<ReferenceSubWindow>()
            .FirstOrDefault(candidate => candidate != null &&
                candidate.gameObject.scene.IsValid() &&
                ReferenceEquals(ScrollAreaField?.GetValue(candidate), area));
        subWindow ??= area.GetComponentInParent<ReferenceSubWindow>(true);
        float originalFullInfoHeight = ReadFullInfoHeight(subWindow);
        List<SavedLayoutBlock> blocks = CaptureLayoutBlocks(area.transform,
            new HashSet<Transform>(overlays.Select(value => value.Transform)));
        List<ReferencePageLayoutEngine.LayoutBounds> graphics =
            CaptureFixedGraphics(area.transform);
        if (flows.Count == 0 && blocks.Count == 0)
            return null;
        return new SavedAreaLayout(area, flows, overlays, blocks,
            graphics, subWindow, originalFullInfoHeight);
    }

    private List<SavedLayoutBlock> CaptureLayoutBlocks(Transform area,
        ISet<Transform> overlayTransforms)
    {
        var blocks = new List<SavedLayoutBlock>();
        foreach (Transform child in DirectChildren(area))
        {
            if (overlayTransforms?.Contains(child) == true)
                continue;
            TMP_Text directText = child.GetComponent<TMP_Text>();
            if (directText == null || child.GetComponent<ClipboardCopyButton>() != null)
                continue;
            string original = directText == null ? string.Empty :
                (_ui?.OriginalTextForLayout(directText) ?? string.Empty);
            if (directText != null && ReferencePageTextFormatter.IsFormulaAnnotation(original))
                continue;
            if (!TryGetBlockBounds(child, area, useOriginalText: true, out var bounds))
                continue;
            blocks.Add(new SavedLayoutBlock(child, child.localPosition, bounds));
        }
        return blocks;
    }

    private int ArrangeContentBlocks(SavedAreaLayout saved)
    {
        if (saved?.Area == null || saved.Blocks.Count < 2)
            return 0;
        // LOGIC and DISTANCE are authored as fixed diagrams/tables: their direct text
        // objects deliberately reserve blank lines for lines, triangles and annotations.
        // Treating every label as a flowing content block cascades tiny metric differences
        // through the whole page and separates formula labels from their graphics.
        if (IsFixedStructurePage(saved.Area.transform))
            return 0;
        SavedLayoutBlock[] ordered = saved.Blocks
            .Where(block => block.Transform != null)
            .OrderByDescending(block => block.OriginalBounds.Top)
            .ToArray();
        if (ordered.Length < 2)
            return 0;
        var original = new List<ReferencePageLayoutEngine.LayoutBounds>(ordered.Length);
        var rendered = new List<ReferencePageLayoutEngine.LayoutBounds>(ordered.Length);
        foreach (SavedLayoutBlock block in ordered)
        {
            if (!TryGetBlockBounds(block.Transform, saved.Area.transform,
                    useOriginalText: false, out var bounds))
                bounds = block.OriginalBounds;
            original.Add(block.OriginalBounds);
            rendered.Add(bounds);
        }
        float[] shifts = ReferencePageLayoutEngine.ArrangeVerticalRows(
            original, rendered, 0.035f);
        int moved = 0;
        for (int index = 0; index < ordered.Length; index++)
        {
            if (Math.Abs(shifts[index]) < 0.0001f)
                continue;
            Vector3 position = ordered[index].Transform.localPosition;
            position.y += shifts[index];
            ordered[index].Transform.localPosition = position;
            moved++;
        }
        return moved;
    }

    private List<ReferencePageLayoutEngine.LayoutBounds> CaptureFixedGraphics(
        Transform area)
    {
        var graphics = new List<ReferencePageLayoutEngine.LayoutBounds>();
        foreach (Transform child in DirectChildren(area))
        {
            if (child.GetComponent<TMP_Text>() != null ||
                child.GetComponent<ClipboardCopyButton>() != null ||
                IsHorizontalSeparator(child))
                continue;
            if (TryGetRendererBounds(child, area, out var bounds))
                graphics.Add(bounds);
        }
        return graphics;
    }

    private int AvoidFixedGraphics(SavedAreaLayout saved)
    {
        if (saved?.Area == null || saved.FixedGraphics.Count == 0)
            return 0;
        if (IsFixedStructurePage(saved.Area.transform))
            return 0;
        List<ReferencePageLayoutEngine.LayoutBounds> currentGraphics =
            CaptureFixedGraphics(saved.Area.transform);
        if (currentGraphics.Count == 0)
            return 0;
        int moved = 0;
        foreach (SavedLayoutBlock block in saved.Blocks)
        {
            if (block.Transform == null ||
                !TryGetBlockBounds(block.Transform, saved.Area.transform,
                    useOriginalText: false, out var rendered))
                continue;
            float shift = ReferencePageLayoutEngine.ShiftTextOutsideGraphics(
                block.OriginalBounds, rendered, currentGraphics, 0.025f);
            if (Math.Abs(shift) < 0.0001f)
                continue;
            Vector3 position = block.OriginalLocalPosition;
            position.y += shift;
            block.Transform.localPosition = position;
            moved++;
        }
        return moved;
    }

    private bool TryGetBlockBounds(Transform block, Transform area,
        bool useOriginalText, out ReferencePageLayoutEngine.LayoutBounds bounds)
    {
        bounds = default;
        if (block == null || area == null)
            return false;
        float top = float.NegativeInfinity;
        float bottom = float.PositiveInfinity;
        float left = float.PositiveInfinity;
        float right = float.NegativeInfinity;
        foreach (TMP_Text text in block.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text == null)
                continue;
            if (useOriginalText)
            {
                string original = _ui?.OriginalTextForLayout(text) ?? string.Empty;
                if (string.IsNullOrEmpty(original))
                    continue;
                try
                {
                    MeasureTextInfo(text, original, info =>
                    {
                        for (int lineIndex = 0; lineIndex < info.lineCount; lineIndex++)
                        {
                            TMP_LineInfo line = info.lineInfo[lineIndex];
                            AddLocalPoint(text.transform, area, line.lineExtents.min.x,
                                line.descender, ref left, ref right, ref top, ref bottom);
                            AddLocalPoint(text.transform, area, line.lineExtents.max.x,
                                line.ascender, ref left, ref right, ref top, ref bottom);
                        }
                        return true;
                    });
                }
                catch
                {
                    continue;
                }
            }
            else
            {
                text.ForceMeshUpdate(ignoreActiveState: true, forceTextReparsing: true);
                Bounds textBounds = text.textBounds;
                AddLocalPoint(text.transform, area, textBounds.min.x, textBounds.min.y,
                    ref left, ref right, ref top, ref bottom);
                AddLocalPoint(text.transform, area, textBounds.max.x, textBounds.max.y,
                    ref left, ref right, ref top, ref bottom);
            }
        }
        if (float.IsNegativeInfinity(top) || float.IsPositiveInfinity(bottom))
            return false;
        bounds = new ReferencePageLayoutEngine.LayoutBounds(left, right, top, bottom);
        return true;
    }

    private static bool TryGetRendererBounds(Transform block, Transform area,
        out ReferencePageLayoutEngine.LayoutBounds bounds)
    {
        float left = float.PositiveInfinity;
        float right = float.NegativeInfinity;
        float top = float.NegativeInfinity;
        float bottom = float.PositiveInfinity;
        foreach (Renderer renderer in block.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null || renderer.GetComponent<TMP_Text>() != null)
                continue;
            Vector3 min = area.InverseTransformPoint(renderer.bounds.min);
            Vector3 max = area.InverseTransformPoint(renderer.bounds.max);
            left = Math.Min(left, Math.Min(min.x, max.x));
            right = Math.Max(right, Math.Max(min.x, max.x));
            top = Math.Max(top, Math.Max(min.y, max.y));
            bottom = Math.Min(bottom, Math.Min(min.y, max.y));
        }
        if (float.IsPositiveInfinity(left) || float.IsNegativeInfinity(top))
        {
            bounds = default;
            return false;
        }
        bounds = new ReferencePageLayoutEngine.LayoutBounds(left, right, top, bottom);
        return true;
    }

    private static void AddLocalPoint(Transform source, Transform area,
        float sourceX, float sourceY, ref float left, ref float right,
        ref float top, ref float bottom)
    {
        Vector3 local = area.InverseTransformPoint(
            source.TransformPoint(new Vector3(sourceX, sourceY, 0f)));
        left = Math.Min(left, local.x);
        right = Math.Max(right, local.x);
        top = Math.Max(top, local.y);
        bottom = Math.Min(bottom, local.y);
    }

    private int ApplyArea(SavedAreaLayout saved)
    {
        if (saved?.Area == null || saved.Flows.Count == 0)
            return 0;
        var translatedFlows = new List<List<LineAnchor>>(saved.Flows.Count);
        foreach (SavedTextFlow flow in saved.Flows)
        {
            flow.Text.ForceMeshUpdate(ignoreActiveState: true, forceTextReparsing: true);
            translatedFlows.Add(BuildLineAnchors(flow.Text,
                saved.Area.transform, flow.Text.textInfo));
        }

        int adjusted = 0;
        foreach (SavedOverlay overlay in saved.Overlays)
        {
            if (overlay.Transform == null || overlay.FlowIndex < 0 ||
                overlay.FlowIndex >= translatedFlows.Count)
                continue;
            if (IsHorizontalSeparator(overlay.Transform) &&
                TryFollowNearestLayoutBlock(saved, overlay, out Vector3 separatorPosition))
            {
                overlay.Transform.localPosition = separatorPosition;
                adjusted++;
                continue;
            }
            List<LineAnchor> translatedLines = translatedFlows[overlay.FlowIndex];
            if (translatedLines.Count == 0)
                continue;
            LineAnchor target = FindMatchingLine(translatedLines,
                overlay.LogicalLine, overlay.WrapIndex);
            Vector2 mapped = ReferencePageLayoutEngine.MapToTranslatedBaseline(
                new Vector2(overlay.OriginalLocalPosition.x, overlay.OriginalLocalPosition.y),
                overlay.OriginalBaselineY, target.LocalY);
            Vector3 position = overlay.OriginalLocalPosition;
            position.x = mapped.x;
            position.y = mapped.y;
            overlay.Transform.localPosition = position;
            adjusted++;
        }
        return adjusted;
    }

    private static bool IsHorizontalSeparator(Transform transform) =>
        transform != null &&
        transform.name.IndexOf("Separator", StringComparison.OrdinalIgnoreCase) >= 0;

    private bool TryFollowNearestLayoutBlock(SavedAreaLayout saved, SavedOverlay overlay,
        out Vector3 position)
    {
        position = overlay.OriginalLocalPosition;
        SavedLayoutBlock nearest = null;
        bool useBottomEdge = true;
        float nearestDistance = float.PositiveInfinity;
        float originalY = overlay.OriginalLocalPosition.y;
        foreach (SavedLayoutBlock block in saved.Blocks)
        {
            if (block.Transform == null)
                continue;
            if (block.OriginalBounds.Bottom >= originalY)
            {
                float distance = block.OriginalBounds.Bottom - originalY;
                if (distance < nearestDistance)
                {
                    nearest = block;
                    useBottomEdge = true;
                    nearestDistance = distance;
                }
            }
            else if (block.OriginalBounds.Top <= originalY)
            {
                float distance = originalY - block.OriginalBounds.Top;
                if (distance < nearestDistance)
                {
                    nearest = block;
                    useBottomEdge = false;
                    nearestDistance = distance;
                }
            }
        }
        if (nearest == null || !TryGetBlockBounds(nearest.Transform,
                saved.Area.transform, useOriginalText: false, out var rendered))
            return false;
        float originalEdge = useBottomEdge
            ? nearest.OriginalBounds.Bottom
            : nearest.OriginalBounds.Top;
        float renderedEdge = useBottomEdge ? rendered.Bottom : rendered.Top;
        position.y += renderedEdge - originalEdge;
        return true;
    }

    private int ApplyReferenceFonts(bool useChineseFont, Transform root = null)
    {
        if (_font == null)
            return 0;
        int count = 0;
        foreach (TMP_Text text in Resources.FindObjectsOfTypeAll<TMP_Text>())
        {
            if (text == null || !text.gameObject.scene.IsValid() ||
                !IsUnderReferenceWindow(text.transform) ||
                (root != null && !text.transform.IsChildOf(root)))
                continue;
            bool directChinese = useChineseFont &&
                                 !ReferencePageFontPolicy.PreserveOriginalMetrics(
                                     FindReferencePageName(text.transform)) &&
                                 ReferencePageFontPolicy.UseDirectChineseFont(text.text);
            if (_font.ApplyDirect(text, directChinese))
                count++;
        }
        return count;
    }

    private int ApplySpecialTextLayouts(Transform root = null)
    {
        int count = 0;
        foreach (TMP_Text text in Resources.FindObjectsOfTypeAll<TMP_Text>())
        {
            if (text == null || !text.gameObject.scene.IsValid() ||
                !IsUnderReferenceWindow(text.transform) ||
                (root != null && !text.transform.IsChildOf(root)))
                continue;
            string pageName = FindReferencePageName(text.transform);
            string formatted = text.text;
            if (pageName == "LOGIC PAGE" && text.name == "TRUTH VALUES")
                formatted = ReferencePageTextFormatter.FormatTruthTable(formatted);
            else if (pageName == "DISTANCE PAGE")
                formatted = ReferencePageTextFormatter.FormatDistanceFormula(text.name, formatted);
            if (!string.Equals(formatted, text.text, StringComparison.Ordinal))
            {
                text.richText = true;
                text.text = formatted;
                count++;
            }
            RememberTextPresentation(text);
        }

        foreach (Transform transform in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (transform == null || !transform.gameObject.scene.IsValid() ||
                (root != null && !transform.IsChildOf(root)) ||
                FindReferencePageName(transform) != "DISTANCE PAGE" ||
                !transform.name.StartsWith("Dist Form Expo", StringComparison.Ordinal))
                continue;
            int id = transform.GetInstanceID();
            if (!_formulaParts.ContainsKey(id))
                _formulaParts[id] = new SavedActiveState(transform.gameObject,
                    transform.gameObject.activeSelf);
            transform.gameObject.SetActive(false);
        }
        return count;
    }

    private SavedTextPresentation RememberTextPresentation(TMP_Text text)
    {
        int id = text.GetInstanceID();
        if (!_textPresentations.TryGetValue(id, out SavedTextPresentation saved) ||
            saved.Text == null || !ReferenceEquals(saved.Text, text))
        {
            string original = _ui?.OriginalTextForLayout(text) ?? text.text ?? string.Empty;
            int originalRenderedLines = 0;
            try
            {
                originalRenderedLines = string.IsNullOrEmpty(original)
                    ? 0
                    : MeasureTextInfo(text, original, info => info.lineCount);
            }
            catch
            {
                originalRenderedLines = CountLogicalLines(original);
            }
            saved = new SavedTextPresentation(text, originalRenderedLines);
            _textPresentations[id] = saved;
        }
        return saved;
    }

    private int FitAllTextToOriginalLineBudgets(Transform root = null)
    {
        int count = 0;
        foreach (TMP_Text text in Resources.FindObjectsOfTypeAll<TMP_Text>())
        {
            if (text == null || !text.gameObject.scene.IsValid() ||
                !IsUnderReferenceWindow(text.transform) ||
                (root != null && !text.transform.IsChildOf(root)))
                continue;
            SavedTextPresentation saved = RememberTextPresentation(text);
            string original = _ui?.OriginalTextForLayout(text) ?? string.Empty;
            if (_ui?.HasStableOriginalForLayout(text) != true ||
                !ReferencePageFontPolicy.MayShrinkStaticText(
                    FindReferencePageName(text.transform), original, text.text))
                continue;
            if (FitTextToOriginalLineBudget(saved))
                count++;
        }
        return count;
    }

    private static bool FitTextToOriginalLineBudget(SavedTextPresentation saved)
    {
        TMP_Text text = saved?.Text;
        if (text == null || saved.OriginalRenderedLines <= 0 ||
            string.IsNullOrEmpty(text.text))
            return false;
        text.ForceMeshUpdate(ignoreActiveState: true, forceTextReparsing: true);
        if (!ReferencePageTextFormatter.ShouldFitOriginalLineBudget(
                saved.OriginalRenderedLines, text.textInfo.lineCount))
            return false;

        text.enableAutoSizing = false;
        text.overflowMode = TextOverflowModes.Overflow;
        float minimumScale = saved.OriginalRenderedLines == 1 ? 0.42f : 0.68f;
        float low = saved.FontSize * minimumScale;
        float high = saved.FontSize;
        float best = low;
        for (int iteration = 0; iteration < 12; iteration++)
        {
            float candidate = (low + high) * 0.5f;
            text.fontSize = candidate;
            text.ForceMeshUpdate(ignoreActiveState: true, forceTextReparsing: true);
            if (text.textInfo.lineCount <= saved.OriginalRenderedLines)
            {
                best = candidate;
                low = candidate;
            }
            else
            {
                high = candidate;
            }
        }
        text.fontSize = best;
        text.ForceMeshUpdate(ignoreActiveState: true, forceTextReparsing: true);
        if (text.textInfo.lineCount > saved.OriginalRenderedLines &&
            saved.OriginalRenderedLines == 1)
        {
            text.enableAutoSizing = true;
            text.fontSizeMin = saved.FontSize * 0.42f;
            text.fontSizeMax = saved.FontSize;
            text.textWrappingMode = TextWrappingModes.NoWrap;
        }
        text.ForceMeshUpdate(ignoreActiveState: true, forceTextReparsing: true);
        return true;
    }

    private int ApplyReadableLineSpacing(Transform root = null)
    {
        int count = 0;
        foreach (SavedTextPresentation saved in _textPresentations.Values)
        {
            TMP_Text text = saved.Text;
            if (text == null || (root != null && !text.transform.IsChildOf(root)) ||
                _ui?.HasStableOriginalForLayout(text) != true)
                continue;
            string original = _ui.OriginalTextForLayout(text) ?? string.Empty;
            float spacing = ReferencePageFontPolicy.ReadableLineSpacing(
                saved.LineSpacing, saved.FontSize, original, text.text,
                FindReferencePageName(text.transform));
            if (Math.Abs(text.lineSpacing - spacing) < 0.0001f)
                continue;
            text.lineSpacing = spacing;
            text.ForceMeshUpdate(ignoreActiveState: true, forceTextReparsing: true);
            count++;
        }
        return count;
    }

    private int ApplyFixedStructureTextMetrics(Transform root = null)
    {
        int count = 0;
        foreach (SavedTextPresentation saved in _textPresentations.Values)
        {
            TMP_Text text = saved.Text;
            if (text == null || (root != null && !text.transform.IsChildOf(root)) ||
                !string.Equals(FindReferencePageName(text.transform), "LOGIC PAGE",
                    StringComparison.OrdinalIgnoreCase) ||
                text.name is not ("ABOVE STUFF" or "TRUTH VALUES"))
                continue;

            // The Chinese pixel font has a slightly taller em box than the authored logic
            // font. Keep the table and all lines fixed; reducing only these two text meshes
            // puts the header and final XOR row back inside the original grid.
            text.enableAutoSizing = false;
            text.fontSize = saved.FontSize * 0.92f;
            text.ForceMeshUpdate(ignoreActiveState: true, forceTextReparsing: true);
            count++;
        }
        return count;
    }

    private static float FindTranslatedContentBottom(SavedAreaLayout saved)
    {
        float bottom = float.PositiveInfinity;
        Transform areaTransform = saved.Area.transform;
        foreach (TMP_Text text in areaTransform.GetComponentsInChildren<TMP_Text>(false))
        {
            if (text == null || !text.gameObject.activeInHierarchy || string.IsNullOrEmpty(text.text))
                continue;
            text.ForceMeshUpdate(ignoreActiveState: false, forceTextReparsing: true);
            Bounds bounds = text.textBounds;
            Vector3 local = areaTransform.InverseTransformPoint(
                text.transform.TransformPoint(bounds.min));
            bottom = Math.Min(bottom, local.y);
        }
        foreach (Renderer renderer in areaTransform.GetComponentsInChildren<Renderer>(false))
        {
            if (renderer == null || !renderer.gameObject.activeInHierarchy ||
                renderer.transform == areaTransform || renderer.GetComponent<TMP_Text>() != null)
                continue;
            Vector3 local = areaTransform.InverseTransformPoint(renderer.bounds.min);
            bottom = Math.Min(bottom, local.y);
        }
        return float.IsPositiveInfinity(bottom) ? 0f : bottom;
    }

    private static void UpdateScrollHeight(SavedAreaLayout saved)
    {
        if (saved?.SubWindow == null || saved.Area == null ||
            saved.OriginalFullInfoHeight <= 0f ||
            (saved.Flows.Count == 0 && saved.Blocks.Count == 0))
            return;
        var originalBottoms = new List<float>();
        originalBottoms.AddRange(saved.Blocks.Select(value => value.OriginalBounds.Bottom));
        originalBottoms.AddRange(saved.Flows.SelectMany(flow => flow.OriginalLines)
            .Select(value => value.LocalY));
        originalBottoms.AddRange(saved.FixedGraphics.Select(value => value.Bottom));
        if (originalBottoms.Count == 0)
            return;
        float originalBottom = originalBottoms.Min();
        float translatedBottom = FindTranslatedContentBottom(saved);
        float windowHeight = ReadWindowHeight(saved.SubWindow);
        const float bottomSafetyPadding = 0.15f;
        saved.SubWindow.FullInfoHeight = ReferencePageLayoutEngine.HeightForTranslatedBounds(
            windowHeight, saved.OriginalFullInfoHeight, originalBottom,
            translatedBottom - bottomSafetyPadding);
    }

    private List<TMP_Text> FindFlowTexts(Transform area)
    {
        var candidates = new List<TMP_Text>();
        foreach (TMP_Text text in area.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text == null || text.transform.parent != area)
                continue;
            string original = _ui?.OriginalTextForLayout(text) ?? text.text ?? string.Empty;
            int logicalLines = CountLogicalLines(original);
            if (logicalLines < 4)
                continue;
            candidates.Add(text);
        }
        return candidates;
    }

    private static int CountLogicalLines(string value)
    {
        if (string.IsNullOrEmpty(value))
            return 0;
        int count = 1;
        foreach (char character in value)
        {
            if (character == '\n')
                count++;
        }
        return count;
    }

    private static IEnumerable<Transform> DirectChildren(Transform parent)
    {
        for (int index = 0; index < parent.childCount; index++)
            yield return parent.GetChild(index);
    }

    private static List<LineAnchor> BuildLineAnchors(TMP_Text text, Transform area,
        TMP_TextInfo info)
    {
        var anchors = new List<LineAnchor>();
        var wraps = new Dictionary<int, int>();
        for (int lineIndex = 0; lineIndex < info.lineCount; lineIndex++)
        {
            TMP_LineInfo line = info.lineInfo[lineIndex];
            int logicalLine = 0;
            int limit = Math.Min(info.characterCount, Math.Max(0, line.firstCharacterIndex));
            for (int characterIndex = 0; characterIndex < limit; characterIndex++)
            {
                if (info.characterInfo[characterIndex].character == '\n')
                    logicalLine++;
            }
            wraps.TryGetValue(logicalLine, out int wrapIndex);
            wraps[logicalLine] = wrapIndex + 1;
            Vector3 world = text.transform.TransformPoint(
                new Vector3(line.lineExtents.min.x, line.baseline, 0f));
            Vector3 local = area.InverseTransformPoint(world);
            anchors.Add(new LineAnchor(logicalLine, wrapIndex,
                local.x, local.y));
        }
        return anchors;
    }

    private static LineAnchor FindMatchingLine(IReadOnlyList<LineAnchor> lines,
        int logicalLine, int wrapIndex)
    {
        LineAnchor sameLogical = null;
        foreach (LineAnchor line in lines)
        {
            if (line.LogicalLine == logicalLine && line.WrapIndex == wrapIndex)
                return line;
            if (line.LogicalLine == logicalLine)
                sameLogical = line;
        }
        if (sameLogical != null)
            return sameLogical;
        LineAnchor best = lines[0];
        int bestDistance = Math.Abs(best.LogicalLine - logicalLine);
        foreach (LineAnchor line in lines)
        {
            int distance = Math.Abs(line.LogicalLine - logicalLine);
            if (distance >= bestDistance)
                continue;
            best = line;
            bestDistance = distance;
        }
        return best;
    }

    private SavedButtonPosition CaptureButtonAnchor(ClipboardCopyButton button,
        Transform parent)
    {
        Vector3 originalPosition = button.transform.localPosition;
        var candidates = new List<(TMP_Text Text, int LogicalLine, int WrapIndex,
            int Score, float CenterY)>();

        foreach (TMP_Text text in parent.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text == null)
                continue;
            string original = _ui?.OriginalTextForLayout(text) ?? text.text ?? string.Empty;
            if (string.IsNullOrEmpty(original))
                continue;
            try
            {
                MeasureTextInfo(text, original, info =>
                {
                    var wraps = new Dictionary<int, int>();
                    for (int lineIndex = 0; lineIndex < info.lineCount; lineIndex++)
                    {
                        string line = GetLineText(info, lineIndex);
                        int score = ReferenceCopyButtonLayout.MatchScore(
                            button.name, line, button.stringToCopy);
                        if (score <= 0)
                            continue;
                        int logicalLine = LogicalLineFor(info, lineIndex);
                        wraps.TryGetValue(logicalLine, out int wrapIndex);
                        wraps[logicalLine] = wrapIndex + 1;
                        TMP_LineInfo infoLine = info.lineInfo[lineIndex];
                        float center = ReferencePageLayoutEngine.LineVisualCenter(
                            infoLine.ascender, infoLine.descender);
                        Vector3 worldCenter = text.transform.TransformPoint(
                            new Vector3(0f, center, 0f));
                        float parentCenterY = parent.InverseTransformPoint(worldCenter).y;
                        candidates.Add((text, logicalLine, wrapIndex, score, parentCenterY));
                    }
                    return true;
                });
            }
            catch
            {
                continue;
            }
        }

        int chosen = ReferencePageLayoutEngine.ChooseBestLine(
            candidates.Select(value => value.Score).ToArray(),
            candidates.Select(value => value.CenterY).ToArray(), originalPosition.y);
        if (chosen < 0)
            return new SavedButtonPosition(button, originalPosition, null, -1, -1);
        return new SavedButtonPosition(button, originalPosition,
            candidates[chosen].Text, candidates[chosen].LogicalLine,
            candidates[chosen].WrapIndex);
    }

    private static bool TryResolveButtonLineEnd(SavedButtonPosition saved,
        Transform parent, out Vector2 lineEnd)
    {
        lineEnd = Vector2.zero;
        TMP_Text text = saved?.AnchoredText;
        if (text == null)
            return false;
        text.ForceMeshUpdate(ignoreActiveState: true, forceTextReparsing: true);
        TMP_TextInfo info = text.textInfo;
        int bestLine = -1;
        int sameLogical = -1;
        var wraps = new Dictionary<int, int>();
        for (int lineIndex = 0; lineIndex < info.lineCount; lineIndex++)
        {
            int logicalLine = LogicalLineFor(info, lineIndex);
            wraps.TryGetValue(logicalLine, out int wrapIndex);
            wraps[logicalLine] = wrapIndex + 1;
            if (logicalLine != saved.LogicalLine)
                continue;
            sameLogical = lineIndex;
            if (wrapIndex == saved.WrapIndex)
            {
                bestLine = lineIndex;
                break;
            }
        }
        if (bestLine < 0)
            bestLine = sameLogical;
        if (bestLine < 0)
            return false;
        TMP_LineInfo matched = info.lineInfo[bestLine];
        float centerY = ReferencePageLayoutEngine.LineVisualCenter(
            matched.ascender, matched.descender);
        Vector3 worldEnd = text.transform.TransformPoint(
            new Vector3(matched.lineExtents.max.x, centerY, 0f));
        Vector3 localEnd = parent.InverseTransformPoint(worldEnd);
        lineEnd = new Vector2(localEnd.x, localEnd.y);
        return true;
    }

    private static int LogicalLineFor(TMP_TextInfo info, int lineIndex)
    {
        TMP_LineInfo line = info.lineInfo[lineIndex];
        int logicalLine = 0;
        int limit = Math.Min(info.characterCount, Math.Max(0, line.firstCharacterIndex));
        for (int characterIndex = 0; characterIndex < limit; characterIndex++)
            if (info.characterInfo[characterIndex].character == '\n')
                logicalLine++;
        return logicalLine;
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

    private static TResult MeasureTextInfo<TResult>(TMP_Text text, string sample,
        Func<TMP_TextInfo, TResult> measure)
    {
        if (text == null)
            throw new ArgumentNullException(nameof(text));
        if (measure == null)
            throw new ArgumentNullException(nameof(measure));
        string displayed = text.text;
        try
        {
            return measure(text.GetTextInfo(sample ?? string.Empty));
        }
        finally
        {
            // TMP_Text.GetTextInfo(string) uses the component itself for measurement and
            // leaves that sample assigned. Reference layout frequently measures the English
            // source while Chinese is visible, so always restore the actual display value.
            if (text != null && text.text != displayed)
            {
                text.text = displayed;
                text.ForceMeshUpdate(ignoreActiveState: true, forceTextReparsing: true);
            }
        }
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

    private static string FindReferencePageName(Transform transform)
    {
        Transform previous = null;
        for (Transform current = transform; current != null; current = current.parent)
        {
            if (current.name == "Reference Window")
                return previous?.name ?? string.Empty;
            previous = current;
        }
        return string.Empty;
    }

    private static bool IsFixedStructurePage(Transform transform)
    {
        string pageName = FindReferencePageName(transform);
        return pageName is "LOGIC PAGE" or "DISTANCE PAGE";
    }

    private static string BuildPath(Transform transform)
    {
        var names = new List<string>();
        for (Transform current = transform; current != null; current = current.parent)
            names.Add(current.name);
        names.Reverse();
        return string.Join("/", names);
    }

    private static float ReadFullInfoHeight(ReferenceSubWindow subWindow)
    {
        if (subWindow == null || FullInfoHeightField == null)
            return 0f;
        object value = FullInfoHeightField.GetValue(subWindow);
        return value is float height ? height : 0f;
    }

    private static float ReadWindowHeight(ReferenceSubWindow subWindow)
    {
        if (subWindow == null || WindowHeightField == null)
            return 0f;
        object value = WindowHeightField.GetValue(subWindow);
        return value is float height ? height : 0f;
    }

    private void RemoveDeadObjects()
    {
        foreach (int id in _originalButtons.Where(pair => pair.Value.Button == null)
                     .Select(pair => pair.Key).ToArray())
            _originalButtons.Remove(id);
        foreach (int id in _areas.Where(pair => pair.Value.Area == null)
                     .Select(pair => pair.Key).ToArray())
            _areas.Remove(id);
    }

    private sealed class LineAnchor
    {
        internal LineAnchor(int logicalLine, int wrapIndex, float localX, float localY)
        {
            LogicalLine = logicalLine;
            WrapIndex = wrapIndex;
            LocalX = localX;
            LocalY = localY;
        }

        internal int LogicalLine { get; }
        internal int WrapIndex { get; }
        internal float LocalX { get; }
        internal float LocalY { get; }
    }

    private sealed class SavedTextFlow
    {
        internal SavedTextFlow(TMP_Text text, List<LineAnchor> originalLines)
        {
            Text = text;
            OriginalLines = originalLines;
        }

        internal TMP_Text Text { get; }
        internal List<LineAnchor> OriginalLines { get; }
    }

    private sealed class SavedOverlay
    {
        internal SavedOverlay(Transform transform, Vector3 originalLocalPosition,
            int flowIndex, int logicalLine, int wrapIndex, float originalBaselineY)
        {
            Transform = transform;
            OriginalLocalPosition = originalLocalPosition;
            FlowIndex = flowIndex;
            LogicalLine = logicalLine;
            WrapIndex = wrapIndex;
            OriginalBaselineY = originalBaselineY;
        }

        internal Transform Transform { get; }
        internal Vector3 OriginalLocalPosition { get; }
        internal int FlowIndex { get; }
        internal int LogicalLine { get; }
        internal int WrapIndex { get; }
        internal float OriginalBaselineY { get; }
    }

    private sealed class SavedAreaLayout
    {
        internal SavedAreaLayout(ScrollArea area, List<SavedTextFlow> flows,
            List<SavedOverlay> overlays, List<SavedLayoutBlock> blocks,
            List<ReferencePageLayoutEngine.LayoutBounds> fixedGraphics,
            ReferenceSubWindow subWindow, float originalFullInfoHeight)
        {
            Area = area;
            Flows = flows;
            Overlays = overlays;
            Blocks = blocks;
            FixedGraphics = fixedGraphics;
            SubWindow = subWindow;
            OriginalFullInfoHeight = originalFullInfoHeight;
        }

        internal ScrollArea Area { get; }
        internal List<SavedTextFlow> Flows { get; }
        internal List<SavedOverlay> Overlays { get; }
        internal List<SavedLayoutBlock> Blocks { get; }
        internal List<ReferencePageLayoutEngine.LayoutBounds> FixedGraphics { get; }
        internal ReferenceSubWindow SubWindow { get; }
        internal float OriginalFullInfoHeight { get; }
    }

    private sealed class SavedLayoutBlock
    {
        internal SavedLayoutBlock(Transform transform, Vector3 originalLocalPosition,
            ReferencePageLayoutEngine.LayoutBounds originalBounds)
        {
            Transform = transform;
            OriginalLocalPosition = originalLocalPosition;
            OriginalBounds = originalBounds;
        }

        internal Transform Transform { get; }
        internal Vector3 OriginalLocalPosition { get; }
        internal ReferencePageLayoutEngine.LayoutBounds OriginalBounds { get; }
    }

    private sealed class SavedButtonPosition
    {
        internal SavedButtonPosition(ClipboardCopyButton button, Vector3 localPosition,
            TMP_Text anchoredText, int logicalLine, int wrapIndex)
        {
            Button = button;
            LocalPosition = localPosition;
            AnchoredText = anchoredText;
            LogicalLine = logicalLine;
            WrapIndex = wrapIndex;
        }

        internal ClipboardCopyButton Button { get; }
        internal Vector3 LocalPosition { get; }
        internal TMP_Text AnchoredText { get; }
        internal int LogicalLine { get; }
        internal int WrapIndex { get; }
    }

    private sealed class ButtonLineCandidate
    {
        internal ButtonLineCandidate(TMP_Text text, int logicalLine, int wrapIndex,
            string textValue, float centerY)
        {
            Text = text;
            LogicalLine = logicalLine;
            WrapIndex = wrapIndex;
            TextValue = textValue;
            CenterY = centerY;
        }

        internal TMP_Text Text { get; }
        internal int LogicalLine { get; }
        internal int WrapIndex { get; }
        internal string TextValue { get; }
        internal float CenterY { get; }
    }


    private sealed class SavedActiveState
    {
        internal SavedActiveState(GameObject gameObject, bool activeSelf)
        {
            GameObject = gameObject;
            ActiveSelf = activeSelf;
        }

        internal GameObject GameObject { get; }
        internal bool ActiveSelf { get; }
    }

    private sealed class SavedTextPresentation
    {
        internal SavedTextPresentation(TMP_Text text, int originalRenderedLines)
        {
            Text = text;
            OriginalRenderedLines = originalRenderedLines;
            FontSize = text.fontSize;
            FontSizeMin = text.fontSizeMin;
            FontSizeMax = text.fontSizeMax;
            LineSpacing = text.lineSpacing;
            EnableAutoSizing = text.enableAutoSizing;
            WrappingMode = text.textWrappingMode;
            OverflowMode = text.overflowMode;
        }

        internal TMP_Text Text { get; }
        internal int OriginalRenderedLines { get; }
        internal float FontSize { get; }
        internal float LineSpacing { get; }
        private float FontSizeMin { get; }
        private float FontSizeMax { get; }
        private bool EnableAutoSizing { get; }
        private TextWrappingModes WrappingMode { get; }
        private TextOverflowModes OverflowMode { get; }

        internal void Restore()
        {
            if (Text == null)
                return;
            Text.fontSize = FontSize;
            Text.fontSizeMin = FontSizeMin;
            Text.fontSizeMax = FontSizeMax;
            Text.lineSpacing = LineSpacing;
            Text.enableAutoSizing = EnableAutoSizing;
            Text.textWrappingMode = WrappingMode;
            Text.overflowMode = OverflowMode;
        }
    }
}
