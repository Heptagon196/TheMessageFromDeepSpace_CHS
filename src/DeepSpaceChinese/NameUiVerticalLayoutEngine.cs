using System;

namespace DeepSpaceChinese;

internal readonly struct NameUiVerticalLayout
{
    public float TakenCenter { get; }
    public float PromptCenter { get; }
    public float RowCenter { get; }
    public float GoodCenter { get; }

    public NameUiVerticalLayout(float takenCenter, float promptCenter, float rowCenter,
        float goodCenter)
    {
        TakenCenter = takenCenter;
        PromptCenter = promptCenter;
        RowCenter = rowCenter;
        GoodCenter = goodCenter;
    }
}

internal static class NameUiVerticalLayoutEngine
{
    public static NameUiVerticalLayout Calculate(float promptCenter, float promptHeight,
        float rowHeight, float goodHeight, float takenHeight, float gap)
    {
        promptHeight = Math.Max(1f, promptHeight);
        rowHeight = Math.Max(1f, rowHeight);
        goodHeight = Math.Max(1f, goodHeight);
        takenHeight = Math.Max(1f, takenHeight);
        gap = Math.Max(0f, gap);
        float takenCenter = promptCenter + promptHeight * 0.5f + gap + takenHeight * 0.5f;
        float rowCenter = promptCenter - promptHeight * 0.5f - gap - rowHeight * 0.5f;
        float goodCenter = rowCenter - rowHeight * 0.5f - gap - goodHeight * 0.5f;
        return new NameUiVerticalLayout(takenCenter, promptCenter, rowCenter, goodCenter);
    }
}
