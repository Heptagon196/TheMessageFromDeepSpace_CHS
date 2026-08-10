using System;
using System.Globalization;
using HarmonyLib;

namespace DeepSpaceChinese;

internal static class CalculatorBaseConversionCompatibility
{
    public static string RepairResult(double sourceNumber, int fromBase, int toBase,
        string originalResult)
    {
        if (string.IsNullOrEmpty(originalResult) || fromBase == 10 || toBase != 10 ||
            sourceNumber == 0d || Math.Abs(sourceNumber) >= 1d ||
            !ContainsOnlySignedAsciiDigits(originalResult) ||
            !TryConvertSourceFraction(sourceNumber, fromBase, out double converted))
            return originalResult;

        return converted.ToString("0.###############################",
            CultureInfo.CurrentCulture);
    }

    private static bool TryConvertSourceFraction(double sourceNumber, int fromBase,
        out double converted)
    {
        converted = 0d;
        if (fromBase < 2 || fromBase > 10)
            return false;

        string source = Math.Abs(sourceNumber).ToString(
            "0.###############################", CultureInfo.InvariantCulture);
        int separator = source.IndexOf('.');
        if (separator < 0 || separator == source.Length - 1)
            return false;

        double place = 1d / fromBase;
        for (int index = separator + 1; index < source.Length; index++)
        {
            char character = source[index];
            if (character < '0' || character > '9')
                return false;
            int digit = character - '0';
            if (digit >= fromBase)
                return false;
            converted += digit * place;
            place /= fromBase;
        }

        if (sourceNumber < 0d)
            converted = -converted;
        return true;
    }

    private static bool ContainsOnlySignedAsciiDigits(string value)
    {
        int index = value[0] == '-' ? 1 : 0;
        if (index == value.Length)
            return false;
        for (; index < value.Length; index++)
            if (value[index] < '0' || value[index] > '9')
                return false;
        return true;
    }
}

[HarmonyPatch(typeof(CalculatorWindow), "EuclideanBaseChange")]
internal static class CalculatorBaseConversionPatch
{
    private static void Postfix(double __0, int __1, int __2, ref string __result)
    {
        __result = CalculatorBaseConversionCompatibility.RepairResult(
            __0, __1, __2, __result);
    }
}
