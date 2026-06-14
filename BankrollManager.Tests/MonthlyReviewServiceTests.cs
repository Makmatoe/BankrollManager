using BankrollManager.Core.Models;
using BankrollManager.Core.Persistence;
using BankrollManager.Core.Services;
using static BankrollManager.Tests.TestAssertions;

namespace BankrollManager.Tests;

[TestClass]
public sealed class MonthlyReviewServiceTests
{
    [TestMethod]
    public void MonthlyReviewCalculatesTotalsBreakdownsAndBreaches()
    {
        var data = BuildReviewData();

        var report = MonthlyReviewService.GetReport(data, new DateOnly(2026, 6, 1));

        AssertMoney(0m, report.Summary.TotalPokerProfitLoss);
        AssertMoney(20m, report.Summary.TicketProfitLoss);
        AssertMoney(20m, report.Summary.TotalValueProfitLoss);
        AssertMoney(3.02m, report.Summary.HoursPlayed);
        AssertMoney(0m, report.Summary.CashPerHour);
        AssertMoney(6.63m, report.Summary.ValuePerHour);

        var platform = report.PlatformResults.Single(result => result.Name == Platform.GGPoker.ToString());
        AssertMoney(15m, platform.TotalCashProfitLoss);
        AssertMoney(20m, platform.TicketProfitLoss);
        AssertMoney(35m, platform.TotalValueProfitLoss);

        var cashFormat = report.FormatResults.Single(result => result.Name == CashFormat.HoldemCash.ToString());
        AssertMoney(-15m, cashFormat.TotalValueProfitLoss);
        AssertMoney(1m, cashFormat.HoursPlayed);

        Assert.HasCount(1, report.StopLossBreaches);
        Assert.AreEqual(new DateOnly(2026, 6, 7), report.StopLossBreaches[0].Date);
        Assert.IsGreaterThanOrEqualTo(2, report.RiskBreaches.Count);
        Assert.IsTrue(report.RiskBreaches.Any(entry => entry.Name == "Big Win"));
        Assert.IsTrue(report.RiskBreaches.Any(entry => entry.Kind == "Cash Risk"));
        Assert.IsTrue(report.Notes.Any(note => note.Area == "Leak/Lesson" && note.Text.Contains("Called too wide", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(report.Notes.Any(note => note.Area == "Highlight" && note.Text.Contains("highlight", StringComparison.OrdinalIgnoreCase)));

        var ticketPerformance = report.SpecialtyResults.Single(result => result.Name == "Ticket-related");
        AssertMoney(15m, ticketPerformance.TotalValueProfitLoss);
    }

    [TestMethod]
    public void MonthlyReviewExportBuildsChatGptFriendlyMarkdown()
    {
        var markdown = ChatGptBankrollExporter.BuildMonthlyReviewMarkdown(
            BuildReviewData(),
            new DateOnly(2026, 6, 1),
            new DateTime(2026, 7, 1, 9, 0, 0));

        StringAssert.Contains(markdown, "# Bankroll Manager Monthly Review");
        StringAssert.Contains(markdown, "Review month: 2026-06");
        StringAssert.Contains(markdown, "## Summary Metrics");
        StringAssert.Contains(markdown, "## Results By Format");
        StringAssert.Contains(markdown, "## Results By Platform");
        StringAssert.Contains(markdown, "## Flip Satellite Ticket Performance");
        StringAssert.Contains(markdown, "## Risk Breaches");
        StringAssert.Contains(markdown, "Big Win");
        StringAssert.Contains(markdown, "Called too wide");
        StringAssert.Contains(markdown, "highlight");
    }

    private static BankrollData BuildReviewData()
    {
        return new BankrollData
        {
            Settings = new BankrollSettings
            {
                StartingBankroll = 100m,
                CurrencySymbol = "$",
                DailyStopLossAmount = 10m,
                MonthlyPokerStopLossPercent = 0m,
                NormalMttMaxRiskPercent = 5m,
                CashSessionMaxRiskPercent = 6m,
                FlipMaxRiskPercent = 1m
            },
            TournamentEntries =
            [
                new TournamentEntry
                {
                    Date = new DateOnly(2026, 6, 5),
                    RegistrationTime = new TimeOnly(10, 0),
                    FinishedDate = new DateOnly(2026, 6, 5),
                    FinishedTime = new TimeOnly(12, 0),
                    Status = TournamentStatus.Finished,
                    Platform = Platform.GGPoker,
                    Category = TournamentCategory.MainGrind,
                    Format = TournamentFormat.MTT,
                    EventName = "Big Win",
                    BuyIn = 10m,
                    ActualBullets = 1,
                    CashPrize = 30m,
                    MistakeLesson = "Called too wide once",
                    Notes = "highlight: kept focus"
                },
                new TournamentEntry
                {
                    Date = new DateOnly(2026, 6, 6),
                    Status = TournamentStatus.Finished,
                    Platform = Platform.GGPoker,
                    Category = TournamentCategory.FlipSatellite,
                    Format = TournamentFormat.Satellite,
                    EventName = "Ticket Path",
                    BuyIn = 5m,
                    ActualBullets = 1,
                    TicketValueWon = 20m,
                    TicketWon = true,
                    TargetEventBuyIn = 20m,
                    TargetEventName = "Sunday target",
                    Notes = "ticket path worked"
                }
            ],
            CashSessions =
            [
                new CashSession
                {
                    Date = new DateOnly(2026, 6, 7),
                    SessionTime = new TimeOnly(14, 0),
                    ClosedDate = new DateOnly(2026, 6, 7),
                    ClosedTime = new TimeOnly(15, 0),
                    Platform = Platform.Unibet,
                    Format = CashFormat.HoldemCash,
                    Game = "Cash",
                    Stakes = "0.01/0.02",
                    StartStackBuyIn = 20m,
                    Cashout = 5m,
                    Notes = "leak: chased river"
                }
            ]
        };
    }
}
