using System;
using System.Text.RegularExpressions;

namespace DeepSpaceChinese;

internal static class PlayerAuthoredUiText
{
    private static readonly Regex SiblingIndex = new(@"\[\d+\]", RegexOptions.Compiled);

    internal static bool ShouldPreserve(string objectPath)
    {
        string path = SiblingIndex.Replace(objectPath ?? string.Empty, string.Empty);
        return path.IndexOf("Dictionary Window/Viewport/EntryViewport/",
                   StringComparison.Ordinal) >= 0 &&
               path.EndsWith("/Visuals/Word", StringComparison.Ordinal) ||
               path.IndexOf("Dictionary Window/Viewport/DictionaryNotes/Notes Content",
                   StringComparison.Ordinal) >= 0 ||
               path.IndexOf("Input Output Objects/Response Group/Input Display",
                   StringComparison.Ordinal) >= 0 ||
               path.IndexOf("Input Output Objects/Read Message Group/Term Logger(Clone)/Text",
                   StringComparison.Ordinal) >= 0;
    }
}
