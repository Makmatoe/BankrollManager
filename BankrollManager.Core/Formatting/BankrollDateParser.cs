using System.Globalization;

namespace BankrollManager.Core.Formatting;

public static class BankrollDateParser
{
    private static readonly string[] DateFormats =
    [
        "yyyy-MM-dd",
        "yyyy-M-d",
        "dd-MM-yyyy",
        "d-M-yyyy",
        "dd/MM/yyyy",
        "d/M/yyyy",
        "yyyy/MM/dd",
        "yyyy/M/d"
    ];

    private static readonly string[] TimeFormats =
    [
        "HH:mm",
        "H:mm",
        "HH:mm:ss",
        "H:mm:ss"
    ];

    public static DateOnly ParseDateOrDefault(string value, DateOnly fallback, CultureInfo? culture = null)
    {
        return TryParseDate(value, out var result, culture) ? result : fallback;
    }

    public static DateOnly? ParseNullableDate(string value, CultureInfo? culture = null)
    {
        return TryParseDate(value, out var result, culture) ? result : null;
    }

    public static bool TryParseDate(string value, out DateOnly result, CultureInfo? culture = null)
    {
        value = value.Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            result = default;
            return false;
        }

        return DateOnly.TryParseExact(value, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out result)
            || DateOnly.TryParse(value, culture ?? CultureInfo.CurrentCulture, DateTimeStyles.None, out result)
            || DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
    }

    public static TimeOnly? ParseNullableTime(string value, CultureInfo? culture = null)
    {
        return TryParseTime(value, out var result, culture) ? result : null;
    }

    public static bool TryParseTime(string value, out TimeOnly result, CultureInfo? culture = null)
    {
        value = value.Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            result = default;
            return false;
        }

        return TimeOnly.TryParseExact(value, TimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out result)
            || TimeOnly.TryParse(value, culture ?? CultureInfo.CurrentCulture, DateTimeStyles.None, out result)
            || TimeOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
    }
}
