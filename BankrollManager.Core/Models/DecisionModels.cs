namespace BankrollManager.Core.Models;

public sealed class DecisionRequest
{
    public Platform Platform { get; set; } = Platform.Unibet;
    public TournamentFormat Format { get; set; } = TournamentFormat.MTT;
    public CashFormat CashFormat { get; set; } = CashFormat.HoldemCash;
    public TournamentCategory Category { get; set; } = TournamentCategory.MainGrind;
    public decimal BuyIn { get; set; }
    public int PlannedBullets { get; set; } = 1;
    public decimal AddOnsRebuys { get; set; }
    public decimal TicketBuyInValue { get; set; }
    public bool IsCashSession { get; set; }
    public decimal CashBuyIn { get; set; }
    public decimal CashReloads { get; set; }
    public string Notes { get; set; } = string.Empty;

    public decimal TotalPlannedRisk => IsCashSession
        ? CashBuyIn + CashReloads
        : Math.Max(0m, BuyIn * PlannedBullets + AddOnsRebuys - TicketBuyInValue);
}

public sealed record DecisionResult(
    DecisionLabel Label,
    string DisplayLabel,
    decimal CurrentBankroll,
    decimal TotalPlannedRisk,
    decimal RiskPercent,
    decimal CategoryBudgetRemaining,
    string Explanation,
    string SuggestedSaferAlternative,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Thresholds);
