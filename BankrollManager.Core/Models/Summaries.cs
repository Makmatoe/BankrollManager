namespace BankrollManager.Core.Models;

public sealed record DashboardSummary(
    decimal CurrentBankroll,
    decimal ActiveTableCash,
    decimal TotalDeposits,
    decimal TotalWithdrawals,
    decimal TicketBalance,
    decimal TotalPokerProfitLoss,
    decimal TournamentProfitLoss,
    decimal CashProfitLoss,
    decimal TodayProfitLoss,
    decimal ThisMonthProfitLoss,
    DailySummary? BestDay,
    DailySummary? WorstDay,
    StopLossStatus StopLossStatus,
    string BankrollTier);

public sealed record DailySummary(
    DateOnly Date,
    decimal TournamentProfitLoss,
    decimal CashProfitLoss,
    decimal TotalProfitLoss,
    int NumberOfSessions,
    decimal RunningMonthProfitLoss,
    decimal RunningLifetimeBankroll);

public sealed record MonthlySummary(
    DateOnly Month,
    decimal Deposits,
    decimal Withdrawals,
    decimal TournamentProfitLoss,
    decimal CashProfitLoss,
    decimal TotalPokerProfitLoss,
    int NumberOfTournaments,
    int NumberOfCashSessions,
    decimal AverageTournamentBuyIn,
    decimal BiggestWin,
    decimal BiggestLoss,
    int StopLossBreaches,
    string Notes);

public sealed record YearlySummary(
    int Year,
    decimal Deposits,
    decimal Withdrawals,
    decimal TournamentProfitLoss,
    decimal CashProfitLoss,
    decimal TotalPokerProfitLoss,
    int NumberOfTournaments,
    int NumberOfCashSessions,
    decimal AverageTournamentBuyIn,
    decimal BiggestWin,
    decimal BiggestLoss,
    int StopLossBreaches,
    string Notes);

public sealed record ComparisonSummary(
    string Name,
    decimal TournamentProfitLoss,
    decimal CashProfitLoss,
    decimal TotalPokerProfitLoss,
    decimal TotalCost,
    int Count);

public sealed record PlatformSummary(
    string Name,
    decimal WalletCashBalance,
    decimal ActiveTableCash,
    decimal TotalPlatformExposure,
    decimal Deposits,
    decimal Withdrawals,
    decimal LedgerNet,
    decimal TournamentProfitLoss,
    decimal CashSessionProfitLoss,
    decimal TotalPokerProfitLoss,
    decimal TicketBalance,
    decimal CashCost,
    int Count,
    decimal? ActualCashBalance,
    decimal? Difference,
    DateOnly? LastUpdatedDate,
    string Notes)
{
    public decimal ActiveCashBalance => WalletCashBalance;
}

public sealed record AuditTimelineEntry(
    DateOnly Date,
    TimeOnly? Time,
    string Type,
    string Name,
    decimal CostRisk,
    decimal Result,
    decimal BankrollBefore,
    decimal BankrollAfter,
    string Rule);

public sealed record RunningBankrollPoint(
    DateOnly Date,
    string Source,
    string Label,
    decimal Amount,
    decimal Bankroll);

public sealed record StopLossStatus(
    bool DailyStopLossHit,
    bool MonthlyStopLossHit,
    bool ProtectModeActive,
    bool SessionLocked,
    bool BreakRequired,
    decimal TodayProfitLoss,
    decimal ThisMonthProfitLoss,
    decimal DailyStopLossLimit,
    decimal MonthlyStopLossLimit,
    string StatusText,
    string Explanation);
