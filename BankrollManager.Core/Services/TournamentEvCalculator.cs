using BankrollManager.Core.Models;

namespace BankrollManager.Core.Services;

public static class TournamentEvCalculator
{
    public static TournamentEvResult Evaluate(TournamentEvRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var buyIn = Math.Max(0m, request.BuyIn);
        var currentEntries = Math.Max(1, request.CurrentEntries);
        var totalPrizeValue = TotalPrizeValue(request);
        var maxSinglePrizeValue = MaxSinglePrizeValue(request, totalPrizeValue);
        var uncappedGrossEv = totalPrizeValue / currentEntries;
        var grossEv = Math.Min(uncappedGrossEv, maxSinglePrizeValue);
        var netEv = grossEv - buyIn;
        var roi = buyIn > 0m
            ? netEv / buyIn
            : 0m;
        var thresholds = CalculateThresholds(totalPrizeValue, maxSinglePrizeValue, buyIn);

        return new TournamentEvResult(
            totalPrizeValue,
            maxSinglePrizeValue,
            uncappedGrossEv,
            grossEv,
            netEv,
            roi,
            thresholds.ExactBreakEvenEntries,
            thresholds.CanBreakEven,
            thresholds.NegativeEvStartsAt,
            thresholds.MaxPositiveEntries,
            StatusFor(netEv));
    }

    public static decimal TotalPrizeValue(TournamentEvRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return request.PrizeType switch
        {
            TournamentEvPrizeType.CashPrizePool => Math.Max(0m, request.ManualPrizeValue),
            _ => Math.Max(0, request.NumberOfTickets) * DiscountedTicketValue(request)
        };
    }

    public static decimal MaxSinglePrizeValue(TournamentEvRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return request.PrizeType switch
        {
            TournamentEvPrizeType.CashPrizePool => Math.Max(0m, request.ManualPrizeValue),
            _ => request.NumberOfTickets > 0 ? DiscountedTicketValue(request) : 0m
        };
    }

    private static decimal MaxSinglePrizeValue(TournamentEvRequest request, decimal totalPrizeValue)
    {
        return request.PrizeType switch
        {
            TournamentEvPrizeType.CashPrizePool => totalPrizeValue,
            _ => MaxSinglePrizeValue(request)
        };
    }

    private static decimal DiscountedTicketValue(TournamentEvRequest request)
    {
        return Math.Max(0m, request.TicketValue)
            * Math.Clamp(request.TicketValueDiscountPercent, 0m, 100m)
            / 100m;
    }

    private static (decimal ExactBreakEvenEntries, bool CanBreakEven, long NegativeEvStartsAt, long MaxPositiveEntries) CalculateThresholds(
        decimal totalPrizeValue,
        decimal maxSinglePrizeValue,
        decimal buyIn)
    {
        if (buyIn <= 0m)
        {
            return totalPrizeValue > 0m
                ? (0m, true, long.MaxValue, long.MaxValue)
                : (0m, true, 0L, 0L);
        }

        if (totalPrizeValue <= 0m || maxSinglePrizeValue < buyIn)
        {
            return (0m, false, 1L, 0L);
        }

        var exactBreakEvenEntries = totalPrizeValue / buyIn;
        var negativeEvStartsAt = Math.Max(1L, (long)Math.Floor(exactBreakEvenEntries) + 1L);
        var maxPositiveEntries = maxSinglePrizeValue > buyIn
            ? Math.Max(0L, (long)Math.Ceiling(exactBreakEvenEntries) - 1L)
            : 0L;
        return (exactBreakEvenEntries, true, negativeEvStartsAt, maxPositiveEntries);
    }

    private static TournamentEvStatus StatusFor(decimal netEv)
    {
        return netEv switch
        {
            > 0m => TournamentEvStatus.Positive,
            < 0m => TournamentEvStatus.Negative,
            _ => TournamentEvStatus.Breakeven
        };
    }
}
