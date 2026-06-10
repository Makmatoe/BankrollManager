namespace BankrollManager.Core.Models;

public sealed class LedgerEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public LedgerType Type { get; set; } = LedgerType.Deposit;
    public Platform Platform { get; set; } = Platform.Unibet;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public TournamentCategory Category { get; set; } = TournamentCategory.MainGrind;
    public decimal BankrollBefore { get; set; }
    public decimal BankrollAfter { get; set; }
    public string Notes { get; set; } = string.Empty;
}
