using System;

namespace DeepSpaceChinese;

internal static class TmpUnderlineMeshCleanup
{
    internal static bool RequiresImmediateEmptyMesh(string previousText, string nextText)
    {
        return !string.IsNullOrEmpty(previousText) &&
               previousText.IndexOf("<u", StringComparison.OrdinalIgnoreCase) >= 0 &&
               !string.Equals(previousText, nextText, StringComparison.Ordinal);
    }
}
