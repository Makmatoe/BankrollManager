namespace BankrollManager.Core.Models;

public sealed record MonthlyReviewReport(
    DateOnly Month,
    MonthlySummary Summary,
    IReadOnlyList<MonthlyReviewMetric> Metrics,
    IReadOnlyList<MonthlyReviewGroupResult> FormatResults,
    IReadOnlyList<MonthlyReviewGroupResult> CategoryResults,
    IReadOnlyList<MonthlyReviewGroupResult> PlatformResults,
    IReadOnlyList<MonthlyReviewGroupResult> SpecialtyResults,
    IReadOnlyList<MonthlyReviewEntry> BiggestWins,
    IReadOnlyList<MonthlyReviewEntry> BiggestLosses,
    IReadOnlyList<MonthlyReviewEntry> StopLossBreaches,
    IReadOnlyList<MonthlyReviewEntry> RiskBreaches,
    IReadOnlyList<MonthlyReviewNote> Notes);

public sealed record MonthlyReviewMetric(string Metric, string Value, string Notes);

public sealed record MonthlyReviewGroupResult(
    string Name,
    decimal TournamentProfitLoss,
    decimal CashProfitLoss,
    decimal TicketProfitLoss,
    decimal TotalCashProfitLoss,
    decimal TotalValueProfitLoss,
    decimal TotalCost,
    int Count,
    decimal HoursPlayed)
{
    public decimal CashPerHour => HoursPlayed > 0m ? TotalCashProfitLoss / HoursPlayed : 0m;
    public decimal ValuePerHour => HoursPlayed > 0m ? TotalValueProfitLoss / HoursPlayed : 0m;
}

public sealed record MonthlyReviewEntry(
    DateOnly Date,
    TimeOnly? Time,
    string Kind,
    string Name,
    string Platform,
    string Format,
    string Category,
    decimal CashProfitLoss,
    decimal TicketProfitLoss,
    decimal ValueProfitLoss,
    decimal RiskPercentage,
    string Rule,
    string Notes);

public sealed record MonthlyReviewNote(
    DateOnly Date,
    string Kind,
    string Name,
    string Area,
    string Text);
