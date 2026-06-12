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
    decimal TodayValueProfitLoss,
    decimal ThisMonthProfitLoss,
    decimal ThisMonthValueProfitLoss,
    DailySummary? BestDay,
    DailySummary? WorstDay,
    StopLossStatus StopLossStatus,
    string BankrollTier)
{
    public decimal CurrentBankrollValue => CurrentBankroll + TicketBalance;
    public decimal TournamentValueProfitLoss => TournamentProfitLoss + TicketBalance;
    public decimal TotalValueProfitLoss => TotalPokerProfitLoss + TicketBalance;
}

public sealed record DailySummary(
    DateOnly Date,
    decimal TournamentProfitLoss,
    decimal CashProfitLoss,
    decimal TicketProfitLoss,
    decimal TotalProfitLoss,
    int NumberOfSessions,
    decimal HoursPlayed,
    decimal RunningMonthProfitLoss,
    decimal RunningLifetimeBankroll,
    decimal RunningLifetimeBankrollValue)
{
    public decimal TotalValueProfitLoss => TotalProfitLoss + TicketProfitLoss;
    public decimal CashPerHour => HoursPlayed > 0m ? TotalProfitLoss / HoursPlayed : 0m;
    public decimal ValuePerHour => HoursPlayed > 0m ? TotalValueProfitLoss / HoursPlayed : 0m;
}

public sealed record MonthlySummary(
    DateOnly Month,
    decimal Deposits,
    decimal Withdrawals,
    decimal TournamentProfitLoss,
    decimal CashProfitLoss,
    decimal TicketProfitLoss,
    decimal TotalPokerProfitLoss,
    int NumberOfTournaments,
    int NumberOfCashSessions,
    decimal HoursPlayed,
    decimal AverageTournamentBuyIn,
    decimal BiggestWin,
    decimal BiggestLoss,
    int StopLossBreaches,
    string Notes)
{
    public decimal TotalValueProfitLoss => TotalPokerProfitLoss + TicketProfitLoss;
    public decimal CashPerHour => HoursPlayed > 0m ? TotalPokerProfitLoss / HoursPlayed : 0m;
    public decimal ValuePerHour => HoursPlayed > 0m ? TotalValueProfitLoss / HoursPlayed : 0m;
}

public sealed record YearlySummary(
    int Year,
    decimal Deposits,
    decimal Withdrawals,
    decimal TournamentProfitLoss,
    decimal CashProfitLoss,
    decimal TicketProfitLoss,
    decimal TotalPokerProfitLoss,
    int NumberOfTournaments,
    int NumberOfCashSessions,
    decimal HoursPlayed,
    decimal AverageTournamentBuyIn,
    decimal BiggestWin,
    decimal BiggestLoss,
    int StopLossBreaches,
    string Notes)
{
    public decimal TotalValueProfitLoss => TotalPokerProfitLoss + TicketProfitLoss;
    public decimal CashPerHour => HoursPlayed > 0m ? TotalPokerProfitLoss / HoursPlayed : 0m;
    public decimal ValuePerHour => HoursPlayed > 0m ? TotalValueProfitLoss / HoursPlayed : 0m;
}

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
    public decimal TotalPlatformValue => TotalPlatformExposure + TicketBalance;
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

public sealed record DayTimelineEntry(
    DateOnly Date,
    TimeOnly? Time,
    string Type,
    string Name,
    decimal CostRisk,
    decimal CashChange,
    decimal TicketChange,
    decimal BankrollBefore,
    decimal BankrollAfter,
    decimal TicketBalanceBefore,
    decimal TicketBalanceAfter,
    string Rule)
{
    public decimal ValueChange => CashChange + TicketChange;
    public decimal BankrollValueBefore => BankrollBefore + TicketBalanceBefore;
    public decimal BankrollValueAfter => BankrollAfter + TicketBalanceAfter;
}

public sealed record RunningBankrollPoint(
    DateOnly Date,
    string Source,
    string Label,
    decimal Amount,
    decimal Bankroll,
    decimal TicketAmount,
    decimal TicketBalance)
{
    public decimal ValueAmount => Amount + TicketAmount;
    public decimal BankrollValue => Bankroll + TicketBalance;
}

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
