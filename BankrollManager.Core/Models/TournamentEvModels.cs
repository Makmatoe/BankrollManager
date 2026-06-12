namespace BankrollManager.Core.Models;

public enum TournamentEvPrizeType
{
    Tickets,
    CashPrizePool
}

public enum TournamentEvStatus
{
    Positive,
    Breakeven,
    Negative
}

public sealed class TournamentEvRequest
{
    public string TournamentName { get; set; } = string.Empty;
    public decimal BuyIn { get; set; }
    public TournamentEvPrizeType PrizeType { get; set; } = TournamentEvPrizeType.Tickets;
    public int NumberOfTickets { get; set; }
    public decimal TicketValue { get; set; }
    public decimal ManualPrizeValue { get; set; }
    public int CurrentEntries { get; set; }
    public decimal TicketValueDiscountPercent { get; set; } = 100m;
}

public sealed record TournamentEvResult(
    decimal TotalPrizeValue,
    decimal GrossEv,
    decimal NetEv,
    decimal Roi,
    decimal ExactBreakEvenEntries,
    long NegativeEvStartsAt,
    long MaxPositiveEntries,
    TournamentEvStatus Status);
