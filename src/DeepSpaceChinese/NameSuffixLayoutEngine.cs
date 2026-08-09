using System;

namespace DeepSpaceChinese;

internal readonly struct NameSuffixLayout
{
    public NameSuffixLayout(float inputLeft, float inputRight, float suffixLeft,
        float suffixRight)
    {
        InputLeft = inputLeft;
        InputRight = inputRight;
        SuffixLeft = suffixLeft;
        SuffixRight = suffixRight;
    }

    public float InputLeft { get; }
    public float InputRight { get; }
    public float SuffixLeft { get; }
    public float SuffixRight { get; }
}

internal static class NameSuffixLayoutEngine
{
    public static NameSuffixLayout Calculate(float containerLeft, float containerRight,
        float inputLeft, float inputRight, float suffixPreferredWidth, float gap)
    {
        if (containerRight < containerLeft)
            (containerLeft, containerRight) = (containerRight, containerLeft);
        float totalWidth = Math.Max(1f, containerRight - containerLeft);
        gap = Math.Max(0f, Math.Min(gap, totalWidth - 1f));
        float suffixWidth = Math.Max(1f, suffixPreferredWidth);
        suffixWidth = Math.Min(suffixWidth, Math.Max(1f, totalWidth - gap - 1f));
        float suffixRight = containerRight;
        float suffixLeft = suffixRight - suffixWidth;
        float calculatedInputRight = suffixLeft - gap;
        float calculatedInputLeft = containerLeft;
        if (calculatedInputRight <= calculatedInputLeft)
            calculatedInputRight = calculatedInputLeft + 1f;
        return new NameSuffixLayout(calculatedInputLeft, calculatedInputRight,
            suffixLeft, suffixRight);
    }
}

internal static class NameLayoutApplicationPolicy
{
    public static bool ShouldApplyDuringScan(bool activeInHierarchy) => activeInHierarchy;

    public static bool ShouldRelayoutOnFocus => false;
}
