namespace BankrollManager.Core.Models;

public enum TournamentQuickResultKind
{
    Auto,
    None,
    CashPrize,
    TicketWon,
    RealizedTicket
}

public sealed class TournamentQuickEntryRequest
{
    public DateOnly RegistrationDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public TimeOnly RegistrationTime { get; set; } = TimeOnly.FromDateTime(DateTime.Now);
    public decimal TicketBuyInValue { get; set; }
    public Platform? TicketBuyInPlatform { get; set; }
    public bool Finished { get; set; }
    public DateOnly? FinishedDate { get; set; }
    public TimeOnly? FinishedTime { get; set; }
    public TournamentQuickResultKind ResultKind { get; set; } = TournamentQuickResultKind.Auto;
    public decimal ResultAmount { get; set; }
    public int? Placement { get; set; }
    public int? FieldSize { get; set; }
    public bool ITM { get; set; }
    public bool FinalTable { get; set; }
    public bool FlipPhaseWon { get; set; }
    public bool GoPhaseReached { get; set; }
}

public sealed class TournamentFinishRequest
{
    public DateOnly FinishedDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public TimeOnly FinishedTime { get; set; } = TimeOnly.FromDateTime(DateTime.Now);
    public TournamentQuickResultKind ResultKind { get; set; } = TournamentQuickResultKind.Auto;
    public decimal ResultAmount { get; set; }
    public int? Placement { get; set; }
    public int? FieldSize { get; set; }
    public bool ITM { get; set; }
    public bool FinalTable { get; set; }
    public bool FlipPhaseWon { get; set; }
    public bool GoPhaseReached { get; set; }
}
