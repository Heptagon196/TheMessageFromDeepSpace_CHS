using System;

namespace DeepSpaceChinese;

internal static class UnicodeInputCoverage
{
    public static bool IsSupported(Type declaringType, string fieldName)
    {
        return IsSupported(declaringType?.FullName, fieldName);
    }

    public static bool IsSupported(string declaringType, string fieldName)
    {
        string id = declaringType + "." + fieldName;
        return string.Equals(id, "NameTranslator.nameEntryInput", StringComparison.Ordinal) ||
               string.Equals(id, "ProgressLog.translatorInput", StringComparison.Ordinal) ||
               string.Equals(id, "InputTextDummy.inputField", StringComparison.Ordinal);
    }
}
