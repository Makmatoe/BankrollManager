using System.Diagnostics;
using BankrollManager.Core.Models;
using BankrollManager.Core.Persistence;
using BankrollManager.Core.Services;

namespace BankrollManager.Tests;

[TestClass]
public sealed class ReleaseHardeningTests
{
    [TestMethod]
    [TestCategory("Performance")]
    public void ReleaseScaleDatasetExercisesCoreCalculationsWithinBudget()
    {
        var data = ReleaseScaleDataFactory.Create();

        var watch = Stopwatch.StartNew();
        BankrollCalculator.RecalculateTrackingFields(data);
        var timeline = BankrollCalculator.GetAuditTimeline(data);
        var dailySummaries = BankrollCalculator.GetDailySummaries(data);
        var monthlySummaries = BankrollCalculator.GetMonthlySummaries(data, dailySummaries);
        var monthlyReview = MonthlyReviewService.GetReport(data, new DateOnly(2025, 6, 1));
        var audit = DataAuditService.GetReport(data, new DateOnly(2026, 12, 31));
        watch.Stop();

        Assert.IsGreaterThanOrEqualTo(5_000, data.TournamentEntries.Count);
        Assert.IsGreaterThanOrEqualTo(2_000, data.CashSessions.Count);
        Assert.IsGreaterThanOrEqualTo(2_000, data.LedgerEntries.Count);
        Assert.IsGreaterThanOrEqualTo(9_000, timeline.Count);
        Assert.IsGreaterThanOrEqualTo(600, dailySummaries.Count);
        Assert.IsGreaterThanOrEqualTo(20, monthlySummaries.Count);
        Assert.IsNotEmpty(monthlyReview.FormatResults);
        Assert.IsTrue(audit.Reconciles);
        Assert.IsFalse(
            audit.Issues.Any(issue => issue.Severity == AttentionSeverity.High),
            string.Join(Environment.NewLine, audit.Issues.Where(issue => issue.Severity == AttentionSeverity.High).Select(issue => issue.Summary).Take(5)));
        Assert.IsTrue(
            watch.Elapsed < TimeSpan.FromSeconds(30),
            $"Release-scale calculations took {watch.Elapsed}.");
    }

    [TestMethod]
    public void ReleaseScaleDatasetKeepsTicketWalletAndAuditTotalsAligned()
    {
        var data = ReleaseScaleDataFactory.Create();

        var totalTicketBalance = BankrollCalculator.TicketBalance(data);
        var platformTicketBalance = Enum.GetValues<Platform>()
            .Sum(platform => BankrollCalculator.TicketBalance(data, platform));
        var audit = DataAuditService.GetReport(data, new DateOnly(2026, 12, 31));

        AssertWithinCent(totalTicketBalance, platformTicketBalance);
        AssertWithinCent(BankrollCalculator.CurrentBankroll(data), audit.Breakdown.CurrentCashBankroll);
        AssertWithinCent(BankrollCalculator.CurrentBankrollValue(data), audit.Breakdown.BankrollValue);
        AssertWithinCent(0m, audit.Breakdown.CashReconciliationDifference);
        Assert.IsTrue(audit.Platforms.All(platform => platform.Status == "Reconciled"));
    }

    [TestMethod]
    public void RepositoryBackupRestoreRoundTripsReleaseScaleData()
    {
        var folder = CreateTempFolder();
        try
        {
            var repository = new JsonBankrollRepository(folder);
            var original = ReleaseScaleDataFactory.Create(240, 120, 120);
            repository.Save(original);
            var backupPath = repository.CreateTimestampedBackup(original);
            var changed = ReleaseScaleDataFactory.Create(12, 8, 8);
            changed.Settings.StartingBankroll = 123m;
            repository.Save(changed);

            var restored = repository.ImportJson(backupPath);
            repository.Save(restored);
            var loaded = repository.LoadOrCreate();

            Assert.IsTrue(File.Exists(backupPath));
            Assert.HasCount(original.TournamentEntries.Count, loaded.TournamentEntries);
            Assert.HasCount(original.CashSessions.Count, loaded.CashSessions);
            Assert.HasCount(original.LedgerEntries.Count, loaded.LedgerEntries);
            Assert.AreEqual(BankrollData.CurrentDataSchemaVersion, loaded.DataSchemaVersion);
            AssertWithinCent(BankrollCalculator.CurrentBankroll(original), BankrollCalculator.CurrentBankroll(loaded));
            AssertWithinCent(BankrollCalculator.TicketBalance(original), BankrollCalculator.TicketBalance(loaded));
        }
        finally
        {
            DeleteTempFolder(folder);
        }
    }

    private static string CreateTempFolder()
    {
        var folder = Path.Combine(Path.GetTempPath(), "BankrollManagerTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        return folder;
    }

    private static void AssertWithinCent(decimal expected, decimal actual)
    {
        Assert.AreEqual(
            (double)expected,
            (double)actual,
            0.01d,
            $"Expected {expected:0.####}, actual {actual:0.####}.");
    }

    private static void DeleteTempFolder(string folder)
    {
        if (Directory.Exists(folder))
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}
