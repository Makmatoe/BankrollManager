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
        var grossEv = totalPrizeValue / currentEntries;
        var netEv = grossEv - buyIn;
        var roi = buyIn > 0m
            ? netEv / buyIn
            : 0m;
        var thresholds = CalculateThresholds(totalPrizeValue, buyIn);

        return new TournamentEvResult(
            totalPrizeValue,
            grossEv,
            netEv,
            roi,
            thresholds.ExactBreakEvenEntries,
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
            _ => Math.Max(0, request.NumberOfTickets)
                * Math.Max(0m, request.TicketValue)
                * Math.Clamp(request.TicketValueDiscountPercent, 0m, 100m)
                / 100m
        };
    }

    private static (decimal ExactBreakEvenEntries, long NegativeEvStartsAt, long MaxPositiveEntries) CalculateThresholds(
        decimal totalPrizeValue,
        decimal buyIn)
    {
        if (buyIn <= 0m)
        {
            return totalPrizeValue > 0m
                ? (0m, long.MaxValue, long.MaxValue)
                : (0m, 0L, 0L);
        }

        var exactBreakEvenEntries = totalPrizeValue / buyIn;
        var negativeEvStartsAt = Math.Max(1L, (long)Math.Floor(exactBreakEvenEntries) + 1L);
        var maxPositiveEntries = Math.Max(0L, (long)Math.Ceiling(exactBreakEvenEntries) - 1L);
        return (exactBreakEvenEntries, negativeEvStartsAt, maxPositiveEntries);
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
