using BankrollManager.Core.Models;
using BankrollManager.Core.Services;
using static BankrollManager.Tests.TestAssertions;

namespace BankrollManager.Tests;

[TestClass]
public sealed class DataAuditServiceTests
{
    [TestMethod]
    public void ReportBuildsReconciliationBreakdown()
    {
        var data = new BankrollData
        {
            Settings = new BankrollSettings { StartingBankroll = 100m },
            LedgerEntries =
            [
                new LedgerEntry
                {
                    Date = new DateOnly(2026, 6, 1),
                    Type = LedgerType.Deposit,
                    Amount = 50m,
                    Description = "Deposit"
                },
                new LedgerEntry
                {
                    Date = new DateOnly(2026, 6, 2),
                    Type = LedgerType.Withdrawal,
                    Amount = 10m,
                    Description = "Withdraw"
                }
            ],
            TournamentEntries =
            [
                new TournamentEntry
                {
                    Date = new DateOnly(2026, 6, 3),
                    EventName = "Daily",
                    BuyIn = 5m,
                    ActualBullets = 1,
                    CashPrize = 12m,
                    TicketValueWon = 3m,
                    TicketWon = true
                }
            ],
            CashSessions =
            [
                new CashSession
                {
                    Date = new DateOnly(2026, 6, 4),
                    Game = "Cash",
                    StartStackBuyIn = 20m,
                    Cashout = 25m,
                    ClosedDate = new DateOnly(2026, 6, 4),
                    ClosedTime = new TimeOnly(13, 0)
                }
            ]
        };

        var report = DataAuditService.GetReport(data, new DateOnly(2026, 6, 14));

        Assert.IsTrue(report.Reconciles);
        AssertMoney(50m, report.Breakdown.Deposits);
        AssertMoney(10m, report.Breakdown.Withdrawals);
        AssertMoney(40m, report.Breakdown.LedgerNet);
        AssertMoney(7m, report.Breakdown.TournamentCashProfitLoss);
        AssertMoney(5m, report.Breakdown.CashSessionProfitLoss);
        AssertMoney(152m, report.Breakdown.CurrentCashBankroll);
        AssertMoney(3m, report.Breakdown.TicketBalance);
        AssertMoney(155m, report.Breakdown.BankrollValue);
        AssertMoney(0m, report.Breakdown.CashReconciliationDifference);
    }

    [TestMethod]
    public void ReportFindsSuspiciousEntries()
    {
        var oldActiveCash = new CashSession
        {
            Date = new DateOnly(2026, 6, 10),
            Status = CashSessionStatus.Active,
            Game = "Old active",
            StartStackBuyIn = 5m
        };
        var missingFinish = new TournamentEntry
        {
            Date = new DateOnly(2026, 6, 14),
            EventName = "Missing finish",
            Status = TournamentStatus.Finished,
            BuyIn = 1m,
            ActualBullets = 1,
            CashPrize = 2m
        };
        var impossibleTicket = new TournamentEntry
        {
            Date = new DateOnly(2026, 6, 14),
            EventName = "Ticket without value",
            Status = TournamentStatus.Finished,
            BuyIn = 1m,
            ActualBullets = 1,
            TicketWon = true
        };
        var invalidLedger = new LedgerEntry
        {
            Date = new DateOnly(2026, 6, 14),
            Type = LedgerType.Deposit,
            Amount = 0m,
            Description = string.Empty
        };
        var data = new BankrollData
        {
            Settings = new BankrollSettings { StartingBankroll = 100m },
            CashSessions = [oldActiveCash],
            TournamentEntries = [missingFinish, impossibleTicket],
            LedgerEntries = [invalidLedger]
        };

        var report = DataAuditService.GetReport(data, new DateOnly(2026, 6, 14));

        Assert.IsTrue(report.Issues.Any(issue => issue.TargetId == oldActiveCash.Id && issue.Summary.Contains("active session", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(report.Issues.Any(issue => issue.TargetId == missingFinish.Id && issue.Summary.Contains("missing finished date/time", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(report.Issues.Any(issue => issue.TargetId == impossibleTicket.Id && issue.Summary.Contains("ticket result has no ticket value", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(report.Issues.Any(issue => issue.TargetId == invalidLedger.Id && issue.Summary.Contains("Description is required", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(report.Issues.Any(issue => issue.TargetId == invalidLedger.Id && issue.Summary.Contains("Amount cannot be zero", StringComparison.OrdinalIgnoreCase)));
    }
}
