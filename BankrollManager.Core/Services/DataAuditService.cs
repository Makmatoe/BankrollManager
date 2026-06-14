using BankrollManager.Core.Models;
using BankrollManager.Core.Validation;

namespace BankrollManager.Core.Services;

public static class DataAuditService
{
    private const int ActiveSessionTooOldDays = 1;

    public static DataAuditReport GetReport(BankrollData data, DateOnly today)
    {
        data.EnsureDefaults();
        var breakdown = BuildBreakdown(data);
        var platforms = BankrollCalculator.GetPlatformSummaries(data)
            .Select(summary => new DataAuditPlatformSummary(
                summary.Name,
                summary.WalletCashBalance,
                summary.ActiveTableCash,
                summary.TotalPlatformExposure,
                summary.TicketBalance,
                summary.ActualCashBalance,
                summary.AcceptedCashDifference,
                summary.Difference,
                summary.LastUpdatedDate,
                PlatformAuditStatus(summary)))
            .ToList();
        var issues = GetIssues(data, today, platforms);

        return new DataAuditReport(
            breakdown,
            BuildBreakdownRows(breakdown),
            platforms,
            issues);
    }

    private static DataAuditBreakdown BuildBreakdown(BankrollData data)
    {
        var startingBankroll = data.Settings.StartingBankroll;
        var deposits = BankrollCalculator.TotalDeposits(data);
        var withdrawals = BankrollCalculator.TotalWithdrawals(data);
        var ledgerNet = BankrollCalculator.LedgerTotal(data);
        var tournamentCashProfitLoss = BankrollCalculator.TournamentProfitLoss(data);
        var cashSessionProfitLoss = BankrollCalculator.CashProfitLoss(data);
        var expectedCashBankroll = startingBankroll
            + ledgerNet
            + tournamentCashProfitLoss
            + cashSessionProfitLoss;
        var currentCashBankroll = BankrollCalculator.CurrentBankroll(data);
        var ticketBalance = BankrollCalculator.TicketBalance(data);

        return new DataAuditBreakdown(
            startingBankroll,
            deposits,
            withdrawals,
            ledgerNet,
            tournamentCashProfitLoss,
            cashSessionProfitLoss,
            currentCashBankroll,
            ticketBalance,
            currentCashBankroll + ticketBalance,
            expectedCashBankroll,
            currentCashBankroll - expectedCashBankroll);
    }

    private static List<DataAuditMetric> BuildBreakdownRows(DataAuditBreakdown breakdown)
    {
        return
        [
            new("Starting bankroll", breakdown.StartingBankroll, "Configured bankroll before imported activity."),
            new("Deposits", breakdown.Deposits, "External cash added through ledger deposits."),
            new("Withdrawals", -breakdown.Withdrawals, "External cash removed through ledger withdrawals."),
            new("Ledger net", breakdown.LedgerNet, "Signed deposits, withdrawals, transfers, bonuses, rakeback, and corrections."),
            new("Tournament cash P/L", breakdown.TournamentCashProfitLoss, "Cash-only tournament profit/loss."),
            new("Cash session P/L", breakdown.CashSessionProfitLoss, "Closed cash-session profit/loss."),
            new("Expected cash bankroll", breakdown.ExpectedCashBankroll, "Starting bankroll + ledger net + poker cash P/L."),
            new("Current cash bankroll", breakdown.CurrentCashBankroll, "Current app cash bankroll."),
            new("Reconciliation difference", breakdown.CashReconciliationDifference, breakdown.Reconciles ? "Cash bankroll reconciles." : "Cash bankroll does not reconcile."),
            new("Ticket balance", breakdown.TicketBalance, "Unrealized ticket value."),
            new("Bankroll value", breakdown.BankrollValue, "Current cash bankroll + ticket balance.")
        ];
    }

    private static List<DataAuditIssue> GetIssues(
        BankrollData data,
        DateOnly today,
        IReadOnlyList<DataAuditPlatformSummary> platforms)
    {
        var issues = new List<DataAuditIssue>();
        var breakdown = BuildBreakdown(data);
        if (!breakdown.Reconciles)
        {
            issues.Add(new DataAuditIssue(
                AttentionSeverity.High,
                "Bankroll",
                "Cash bankroll does not reconcile.",
                $"Difference: {breakdown.CashReconciliationDifference:0.00}",
                AuditIssueTargetType.None,
                null,
                string.Empty,
                "Review"));
        }

        AddTournamentIssues(issues, data);
        AddCashIssues(issues, data, today);
        AddLedgerIssues(issues, data);
        AddWalletIssues(issues, platforms);

        return issues
            .OrderBy(issue => issue.Severity)
            .ThenBy(issue => issue.Area)
            .ThenBy(issue => issue.Summary)
            .ToList();
    }

    private static void AddTournamentIssues(List<DataAuditIssue> issues, BankrollData data)
    {
        foreach (var entry in data.TournamentEntries)
        {
            var label = TournamentLabel(entry);
            foreach (var error in EntryValidator.Validate(entry))
            {
                issues.Add(new DataAuditIssue(
                    AttentionSeverity.High,
                    "MTT",
                    $"{label}: {error}",
                    TournamentEvidence(entry),
                    AuditIssueTargetType.Tournament,
                    entry.Id,
                    string.Empty,
                    "Open"));
            }

            if (entry.Status == TournamentStatus.Finished
                && (entry.FinishedDate is null || entry.FinishedTime is null))
            {
                issues.Add(new DataAuditIssue(
                    AttentionSeverity.Check,
                    "MTT",
                    $"{label}: missing finished date/time.",
                    TournamentEvidence(entry),
                    AuditIssueTargetType.Tournament,
                    entry.Id,
                    string.Empty,
                    "Open"));
            }

            if (entry.TotalCost < 0m || entry.CashCost < 0m)
            {
                issues.Add(new DataAuditIssue(
                    AttentionSeverity.High,
                    "MTT",
                    $"{label}: negative tournament cost.",
                    $"Total cost {entry.TotalCost:0.00}; cash cost {entry.CashCost:0.00}.",
                    AuditIssueTargetType.Tournament,
                    entry.Id,
                    string.Empty,
                    "Open"));
            }

            if ((entry.TicketWon || entry.Qualified || entry.TicketConvertedRealized)
                && entry.EffectiveTicketValueWon <= 0m)
            {
                issues.Add(new DataAuditIssue(
                    AttentionSeverity.High,
                    "Ticket",
                    $"{label}: ticket result has no ticket value.",
                    TournamentEvidence(entry),
                    AuditIssueTargetType.Tournament,
                    entry.Id,
                    string.Empty,
                    "Open"));
            }
        }
    }

    private static void AddCashIssues(List<DataAuditIssue> issues, BankrollData data, DateOnly today)
    {
        foreach (var entry in data.CashSessions)
        {
            var label = CashLabel(entry);
            foreach (var error in EntryValidator.Validate(entry))
            {
                issues.Add(new DataAuditIssue(
                    AttentionSeverity.High,
                    "Cash",
                    $"{label}: {error}",
                    CashEvidence(entry),
                    AuditIssueTargetType.Cash,
                    entry.Id,
                    string.Empty,
                    "Open"));
            }

            if (entry.IsActive)
            {
                var ageDays = today.DayNumber - entry.Date.DayNumber;
                if (ageDays >= ActiveSessionTooOldDays)
                {
                    issues.Add(new DataAuditIssue(
                        AttentionSeverity.Check,
                        "Cash",
                        $"{label}: active session is {ageDays} day(s) old.",
                        CashEvidence(entry),
                        AuditIssueTargetType.Cash,
                        entry.Id,
                        string.Empty,
                        "Open"));
                }
            }

            if (entry.SessionCost < 0m)
            {
                issues.Add(new DataAuditIssue(
                    AttentionSeverity.High,
                    "Cash",
                    $"{label}: negative session cost.",
                    CashEvidence(entry),
                    AuditIssueTargetType.Cash,
                    entry.Id,
                    string.Empty,
                    "Open"));
            }
        }
    }

    private static void AddLedgerIssues(List<DataAuditIssue> issues, BankrollData data)
    {
        foreach (var entry in data.LedgerEntries)
        {
            foreach (var error in EntryValidator.Validate(entry))
            {
                issues.Add(new DataAuditIssue(
                    AttentionSeverity.High,
                    "Ledger",
                    $"{entry.Type}: {error}",
                    LedgerEvidence(entry),
                    AuditIssueTargetType.Ledger,
                    entry.Id,
                    string.Empty,
                    "Open"));
            }
        }
    }

    private static void AddWalletIssues(
        List<DataAuditIssue> issues,
        IReadOnlyList<DataAuditPlatformSummary> platforms)
    {
        foreach (var platform in platforms)
        {
            if (platform.Difference is { } difference && difference != 0m)
            {
                issues.Add(new DataAuditIssue(
                    Math.Abs(difference) >= 1m ? AttentionSeverity.High : AttentionSeverity.Check,
                    "Wallet",
                    $"{platform.Platform}: actual wallet does not match expected cash.",
                    $"Expected {platform.ExpectedCashBalance:0.00}; actual {platform.ActualCashBalance:0.00}; difference {difference:0.00}.",
                    AuditIssueTargetType.Wallet,
                    null,
                    platform.Platform,
                    "Open"));
            }
            else if (platform.ActualCashBalance is null && platform.ExpectedCashBalance != 0m)
            {
                issues.Add(new DataAuditIssue(
                    AttentionSeverity.Info,
                    "Wallet",
                    $"{platform.Platform}: wallet has not been reconciled yet.",
                    $"Expected cash balance {platform.ExpectedCashBalance:0.00}.",
                    AuditIssueTargetType.Wallet,
                    null,
                    platform.Platform,
                    "Open"));
            }
        }
    }

    private static string PlatformAuditStatus(PlatformSummary summary)
    {
        if (summary.ActualCashBalance is null)
        {
            return summary.WalletCashBalance == 0m ? "No activity" : "Not reconciled";
        }

        return summary.Difference is { } difference && difference != 0m
            ? "Mismatch"
            : "Reconciled";
    }

    private static string TournamentLabel(TournamentEntry entry)
    {
        return string.IsNullOrWhiteSpace(entry.EventName)
            ? entry.Format.ToString()
            : entry.EventName;
    }

    private static string CashLabel(CashSession entry)
    {
        var label = $"{entry.Game} {entry.Stakes}".Trim();
        return string.IsNullOrWhiteSpace(label) ? entry.Format.ToString() : label;
    }

    private static string TournamentEvidence(TournamentEntry entry)
    {
        return $"{entry.Date:yyyy-MM-dd}; {entry.Platform}; {entry.Category}; {entry.Format}; status {entry.Status}.";
    }

    private static string CashEvidence(CashSession entry)
    {
        return $"{entry.Date:yyyy-MM-dd}; {entry.Platform}; {entry.Format}; status {entry.Status}.";
    }

    private static string LedgerEvidence(LedgerEntry entry)
    {
        return $"{entry.Date:yyyy-MM-dd}; {entry.Platform}; {entry.Category}; amount {entry.Amount:0.00}.";
    }
}

public sealed record DataAuditReport(
    DataAuditBreakdown Breakdown,
    IReadOnlyList<DataAuditMetric> BreakdownRows,
    IReadOnlyList<DataAuditPlatformSummary> Platforms,
    IReadOnlyList<DataAuditIssue> Issues)
{
    public bool Reconciles => Breakdown.Reconciles;
}

public sealed record DataAuditBreakdown(
    decimal StartingBankroll,
    decimal Deposits,
    decimal Withdrawals,
    decimal LedgerNet,
    decimal TournamentCashProfitLoss,
    decimal CashSessionProfitLoss,
    decimal CurrentCashBankroll,
    decimal TicketBalance,
    decimal BankrollValue,
    decimal ExpectedCashBankroll,
    decimal CashReconciliationDifference)
{
    public bool Reconciles => Math.Abs(CashReconciliationDifference) < 0.01m;
}

public sealed record DataAuditMetric(
    string Metric,
    decimal Amount,
    string Notes);

public sealed record DataAuditPlatformSummary(
    string Platform,
    decimal ExpectedCashBalance,
    decimal ActiveTableCash,
    decimal TotalExposure,
    decimal TicketBalance,
    decimal? ActualCashBalance,
    decimal? AcceptedCashDifference,
    decimal? Difference,
    DateOnly? LastUpdatedDate,
    string Status);

public sealed record DataAuditIssue(
    AttentionSeverity Severity,
    string Area,
    string Summary,
    string Evidence,
    AuditIssueTargetType TargetType,
    Guid? TargetId,
    string TargetKey,
    string Action);

public enum AuditIssueTargetType
{
    None,
    Tournament,
    Cash,
    Ledger,
    Wallet
}
