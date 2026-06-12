using BankrollManager.Core.Models;

namespace BankrollManager.Core.Services;

public static class TournamentEvCalculator
{
    public static TournamentEvResult Evaluate(TournamentEvRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var totalPrizeValue = TotalPrizeValue(request);
        var grossEv = request.CurrentEntries > 0
            ? totalPrizeValue / request.CurrentEntries
            : 0m;
        var netEv = grossEv - request.BuyIn;
        var roi = request.BuyIn > 0m
            ? netEv / request.BuyIn
            : 0m;
        var exactBreakEvenEntries = request.BuyIn > 0m
            ? totalPrizeValue / request.BuyIn
            : 0m;
        var negativeEvStartsAt = (long)Math.Floor(exactBreakEvenEntries) + 1;
        var maxPositiveEntries = (long)Math.Ceiling(exactBreakEvenEntries) - 1;

        return new TournamentEvResult(
            totalPrizeValue,
            grossEv,
            netEv,
            roi,
            exactBreakEvenEntries,
            negativeEvStartsAt,
            maxPositiveEntries,
            StatusFor(netEv));
    }

    public static decimal TotalPrizeValue(TournamentEvRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return request.PrizeType switch
        {
            TournamentEvPrizeType.CashPrizePool => request.ManualPrizeValue,
            _ => request.NumberOfTickets * request.TicketValue * request.TicketValueDiscountPercent / 100m
        };
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
