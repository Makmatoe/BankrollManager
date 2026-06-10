using BankrollManager.Core.Models;
using BankrollManager.Core.Services;
using BankrollManager.Core.Validation;

namespace BankrollManager.App.Forms;

internal static class CashSessionDialogSupport
{
    public static CashSession Clone(CashSession entry)
    {
        return new CashSession
        {
            Id = entry.Id == Guid.Empty ? Guid.NewGuid() : entry.Id,
            Date = entry.Date,
            SessionTime = entry.SessionTime,
            Status = entry.Status,
            ClosedDate = entry.ClosedDate,
            ClosedTime = entry.ClosedTime,
            Platform = entry.Platform,
            Format = entry.Format,
            Game = string.IsNullOrWhiteSpace(entry.Game) ? "Cash" : entry.Game,
            Stakes = entry.Stakes,
            SmallBlindAmount = entry.SmallBlindAmount,
            BigBlindAmount = entry.BigBlindAmount,
            StartStackBuyIn = entry.StartStackBuyIn,
            Reloads = entry.Reloads,
            ReloadCap = entry.ReloadCap,
            Cashout = entry.Cashout,
            CashDropWon = entry.CashDropWon,
            JackpotFortunePrizeWon = entry.JackpotFortunePrizeWon,
            Minutes = entry.Minutes,
            Hands = entry.Hands,
            RiskPercentageOfBankrollAtSessionStart = entry.RiskPercentageOfBankrollAtSessionStart,
            RuleCheckResult = entry.RuleCheckResult,
            BankrollBefore = entry.BankrollBefore,
            BankrollAfter = entry.BankrollAfter,
            Notes = entry.Notes
        };
    }

    public static int? NullableInt(NumericUpDown box)
    {
        return box.Value <= 0m ? null : (int)box.Value;
    }

    public static void EnforceNonNegative(params NumericUpDown[] boxes)
    {
        foreach (var box in boxes)
        {
            if (box.Value < 0m)
            {
                box.Value = 0m;
            }

            box.Minimum = 0m;
        }
    }

    public static string SessionSummary(CashSession entry, BankrollSettings settings)
    {
        var time = entry.SessionTime?.ToString("HH:mm") ?? "--:--";
        var stakes = string.IsNullOrWhiteSpace(entry.Stakes) ? "No stakes" : entry.Stakes;
        return $"{entry.Platform} | {entry.Format} | {entry.Game} {stakes} | Started {entry.Date:yyyy-MM-dd} {time} | Buy-in {settings.CurrencySymbol}{entry.StartStackBuyIn:0.00}";
    }

    public static bool IsRushAndCash(CashFormat format)
    {
        return format is CashFormat.RushAndCashHoldem or CashFormat.RushAndCashOmaha;
    }

    public static bool IsAllInOrFold(CashFormat format)
    {
        return format is CashFormat.AllInOrFoldHoldem or CashFormat.AllInOrFoldOmaha;
    }

    public static string BuildFormatWarning(CashFormat format)
    {
        if (IsAllInOrFold(format))
        {
            return "All-In or Fold is not normal cash. Use stricter stop-loss rules.";
        }

        if (IsRushAndCash(format))
        {
            return "Rush & Cash can hit stop-loss faster because of higher hand volume.";
        }

        return string.Empty;
    }
}
