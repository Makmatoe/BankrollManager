using System.Globalization;

namespace BankrollManager.Core.Formatting;

public static class MoneyFormatter
{
    public static string Format(
        decimal value,
        string? currencySymbol = null,
        CultureInfo? culture = null,
        string format = "0.00")
    {
        var number = Math.Abs(value).ToString(format, culture ?? CultureInfo.CurrentCulture);
        var sign = value < 0m ? "-" : string.Empty;
        return string.IsNullOrWhiteSpace(currencySymbol)
            ? $"{sign}{number}"
            : $"{sign}{currencySymbol}{number}";
    }

    public static string FormatNumber(
        decimal value,
        CultureInfo? culture = null,
        string format = "0.00")
    {
        return value.ToString(format, culture ?? CultureInfo.CurrentCulture);
    }
}
