using System;

namespace DeepSpaceChinese;

internal static class DialogueTextRectLayout
{
    public static float RequiredRightShrink(float textRight, float iconLeft, float gap)
    {
        if (float.IsNaN(textRight) || float.IsNaN(iconLeft) ||
            float.IsInfinity(textRight) || float.IsInfinity(iconLeft))
            return 0f;
        return Math.Max(0f, textRight - iconLeft + Math.Max(0f, gap));
    }
}
