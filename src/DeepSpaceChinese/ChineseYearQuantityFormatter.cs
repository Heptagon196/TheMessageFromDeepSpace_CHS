using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DeepSpaceChinese;

internal static class ChineseYearQuantityFormatter
{
    private static readonly Regex EnglishYearQuantity = new(
        @"(?<![A-Za-z0-9_.])(?<value>\d+(?:\.\d+)?)\s+(?<unit>billion years|million years|thousand years|years)(?![A-Za-z])",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    internal static string Translate(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;
        return EnglishYearQuantity.Replace(text, match =>
        {
            if (!decimal.TryParse(match.Groups["value"].Value,
                    NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture,
                    out decimal value))
                return match.Value;

            decimal years;
            switch (match.Groups["unit"].Value.ToLowerInvariant())
            {
                case "billion years":
                    years = value * 1_000_000_000m;
                    break;
                case "million years":
                    years = value * 1_000_000m;
                    break;
                case "thousand years":
                    years = value * 1_000m;
                    break;
                default:
                    years = value;
                    break;
            }

            if (years >= 100_000_000m)
                return Format(years / 100_000_000m) + "亿年";
            if (years >= 10_000m)
                return Format(years / 10_000m) + "万年";
            return Format(years) + "年";
        });
    }

    private static string Format(decimal value) =>
        value.ToString("0.######", CultureInfo.InvariantCulture);
}
