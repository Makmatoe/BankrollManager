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

public enum TournamentEvTournamentType
{
    FlatTicketSatellite,
    NormalMtt,
    TopHeavyMtt,
    WinnerTakeAll,
    CustomPayouts
}

public enum TournamentEvVarianceRating
{
    Low,
    Medium,
    High,
    Extreme
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
    public TournamentEvTournamentType TournamentType { get; set; } = TournamentEvTournamentType.FlatTicketSatellite;
    public int TotalEntries { get; set; }
    public int PaidPlaces { get; set; }
    public IReadOnlyList<decimal> Payouts { get; set; } = [];
    public string PayoutStructure { get; set; } = string.Empty;
    public int SampleSize { get; set; } = 100;
    public decimal BankrollSize { get; set; }
}

public sealed record TournamentEvResult(
    decimal TotalPrizeValue,
    decimal MaxSinglePrizeValue,
    decimal UncappedGrossEv,
    decimal GrossEv,
    decimal NetEv,
    decimal Roi,
    decimal ExactBreakEvenEntries,
    bool CanBreakEven,
    long NegativeEvStartsAt,
    long MaxPositiveEntries,
    TournamentEvStatus Status,
    TournamentEvVarianceResult Variance);

public sealed record TournamentEvVarianceResult(
    decimal EvPerTournament,
    decimal Roi,
    decimal WinOrCashProbability,
    decimal StandardDeviation,
    decimal StandardDeviationInBuyIns,
    int SampleSize,
    decimal ExpectedProfitAfterSample,
    decimal StandardDeviationAfterSample,
    decimal LikelyResultLowAfterSample,
    decimal LikelyResultHighAfterSample,
    decimal ChanceNotAheadAfterSample,
    bool ChanceNotAheadIsExact,
    decimal BankrollSwingPercentAfterSample,
    TournamentEvVarianceRating Rating);
