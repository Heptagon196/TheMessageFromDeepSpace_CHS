using System;

namespace DeepSpaceChinese;

internal static class InterfaceFontPolicy
{
    internal static bool ShouldUseDirectFont(string objectPath, string displayText,
        DisplayMode mode, bool hasSpecialMaterialOwner = false)
    {
        if (mode != DisplayMode.TranslationOnly || !ContainsHan(displayText))
            return false;
        return IsDirectFontTarget(objectPath, hasSpecialMaterialOwner);
    }

    internal static bool IsDirectFontTarget(string objectPath,
        bool hasSpecialMaterialOwner = false)
    {
        if (hasSpecialMaterialOwner)
            return true;
        objectPath ??= string.Empty;
        return IsPath(objectPath, "GlobalPopups/Clipboard Popup") ||
               IsPath(objectPath, "Idea Log Entry") ||
               IsPath(objectPath, "Ideas Window/Idea Popup") ||
               IsPath(objectPath, "Ideas Window/Idea Reminder Popup") ||
               IsPath(objectPath, "Ideas Window/Idea Sent Popup") ||
               IsPath(objectPath, "Ideas Window/Ideas Marked Read Popup");
    }

    internal static bool ShouldUseDirectLogTitleFont(string displayText, DisplayMode mode) =>
        mode == DisplayMode.TranslationOnly && ContainsHan(displayText);

    private static bool IsPath(string objectPath, string segment) =>
        objectPath.Equals(segment, StringComparison.OrdinalIgnoreCase) ||
        objectPath.StartsWith(segment + "/", StringComparison.OrdinalIgnoreCase) ||
        objectPath.StartsWith(segment + "[", StringComparison.OrdinalIgnoreCase) ||
        objectPath.StartsWith(segment + "(", StringComparison.OrdinalIgnoreCase) ||
        objectPath.IndexOf("/" + segment + "/", StringComparison.OrdinalIgnoreCase) >= 0 ||
        objectPath.IndexOf("/" + segment + "[", StringComparison.OrdinalIgnoreCase) >= 0 ||
        objectPath.IndexOf("/" + segment + "(", StringComparison.OrdinalIgnoreCase) >= 0;

    private static bool ContainsHan(string text)
    {
        if (string.IsNullOrEmpty(text))
            return false;
        foreach (char value in text)
        {
            if (value is >= '\u3400' and <= '\u9fff' or >= '\uf900' and <= '\ufaff')
                return true;
        }
        return false;
    }
}
