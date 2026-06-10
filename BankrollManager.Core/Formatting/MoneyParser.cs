using System.Globalization;

namespace BankrollManager.Core.Formatting;

public static class MoneyParser
{
    public static string Clean(string value, string? currencySymbol = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var cleaned = value.Trim();
        if (!string.IsNullOrWhiteSpace(currencySymbol))
        {
            cleaned = cleaned.Replace(currencySymbol, string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        cleaned = cleaned
            .Replace("\u20ac", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("EUR", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("%", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("\u00a0", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(" ", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("'", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("\u2212", "-", StringComparison.OrdinalIgnoreCase)
            .Trim();

        return cleaned;
    }

    public static decimal ParseOrDefault(
        string value,
        decimal fallback = 0m,
        string? currencySymbol = null,
        CultureInfo? culture = null)
    {
        return TryParse(value, out var result, currencySymbol, culture) ? result : fallback;
    }

    public static bool TryParse(
        string value,
        out decimal result,
        string? currencySymbol = null,
        CultureInfo? culture = null)
    {
        var cleaned = Clean(value, currencySymbol);
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            result = 0m;
            return false;
        }

        var normalized = NormalizeDecimalSeparators(cleaned);
        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out result)
            || decimal.TryParse(cleaned, NumberStyles.Number, culture ?? CultureInfo.CurrentCulture, out result)
            || decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out result);
    }

    private static string NormalizeDecimalSeparators(string value)
    {
        var lastDot = value.LastIndexOf('.');
        var lastComma = value.LastIndexOf(',');
        if (lastDot < 0 && lastComma < 0)
        {
            return value;
        }

        if (lastDot >= 0 && lastComma >= 0)
        {
            return lastDot > lastComma
                ? value.Replace(",", string.Empty, StringComparison.Ordinal)
                : value.Replace(".", string.Empty, StringComparison.Ordinal).Replace(',', '.');
        }

        if (lastComma >= 0)
        {
            return LooksLikeGroupedInteger(value, ',')
                ? value.Replace(",", string.Empty, StringComparison.Ordinal)
                : value.Replace(',', '.');
        }

        return value;
    }

    private static bool LooksLikeGroupedInteger(string value, char separator)
    {
        var unsigned = value.TrimStart('+', '-');
        var parts = unsigned.Split(separator);
        return parts.Length > 1
            && parts[0].Length is >= 1 and <= 3
            && parts.Skip(1).All(part => part.Length == 3 && part.All(char.IsDigit));
    }
}
