using System.Globalization;

namespace BankrollManager.Core.Formatting;

public static class BankrollDateFormatter
{
    public const string DateFormat = "yyyy-MM-dd";
    public const string TimeFormat = "HH:mm";
    public const string MonthFormat = "yyyy-MM";

    public static string FormatDate(DateOnly? value, CultureInfo? culture = null)
    {
        return value?.ToString(DateFormat, culture ?? CultureInfo.InvariantCulture) ?? string.Empty;
    }

    public static string FormatTime(TimeOnly? value, CultureInfo? culture = null)
    {
        return value?.ToString(TimeFormat, culture ?? CultureInfo.InvariantCulture) ?? string.Empty;
    }

    public static string FormatMonth(DateOnly value, CultureInfo? culture = null)
    {
        return value.ToString(MonthFormat, culture ?? CultureInfo.InvariantCulture);
    }
}
