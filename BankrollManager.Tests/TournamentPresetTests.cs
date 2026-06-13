using System.Globalization;
using BankrollManager.Core.Models;
using BankrollManager.Core.Persistence;
using BankrollManager.Core.Services;
using BankrollManager.Core.Validation;
using static BankrollManager.Tests.TestAssertions;

namespace BankrollManager.Tests;

[TestClass]
public sealed class TournamentPresetTests
{
    [TestMethod]
    public void TournamentPresetDisplayNameUsesBuyInNameAndTicketValue()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            var preset = new TournamentPreset
            {
                Name = "  Supermoon   Flip ",
                BuyIn = 0.04m,
                TicketValueWon = 0.40m
            };

            var displayName = TournamentPresetService.DisplayName(
                preset,
                new BankrollSettings { CurrencySymbol = "\u20ac" });

            Assert.AreEqual("\u20ac0.04 Supermoon Flip \u20ac0.40 Ticket", displayName);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [TestMethod]
    public void TournamentPresetCreatesFreshRegisteredEntryWithCurrentTime()
    {
        var source = new TournamentEntry
        {
            Date = new DateOnly(2026, 6, 1),
            RegistrationTime = new TimeOnly(10, 0),
            Status = TournamentStatus.Finished,
            FinishedDate = new DateOnly(2026, 6, 1),
            FinishedTime = new TimeOnly(10, 2),
            Platform = Platform.Unibet,
            Category = TournamentCategory.FlipSatellite,
            Format = TournamentFormat.Flip,
            EventName = "Old row name",
            BuyIn = 0.04m,
            PlannedBullets = 1,
            ActualBullets = 1,
            TicketValueWon = 0.40m,
            CashPrize = 1.20m,
            Placement = 1,
            FieldSize = 10,
            ITM = true,
            FinalTable = true,
            Tags = "Flip"
        };
        var preset = TournamentPresetService.CreateFromEntry(
            source,
            "Supermoon Flip",
            new DateTime(2026, 6, 9, 12, 0, 0, DateTimeKind.Utc));

        var entry = TournamentPresetService.CreateEntry(preset, new DateTime(2026, 6, 9, 14, 45, 0));

        Assert.AreEqual(new DateOnly(2026, 6, 9), entry.Date);
        Assert.AreEqual(new TimeOnly(14, 45), entry.RegistrationTime);
        Assert.AreEqual(TournamentStatus.Registered, entry.Status);
        Assert.IsNull(entry.FinishedDate);
        Assert.IsNull(entry.FinishedTime);
        Assert.AreEqual("Supermoon Flip", entry.EventName);
        Assert.AreEqual(Platform.Unibet, entry.Platform);
        Assert.AreEqual(TournamentCategory.FlipSatellite, entry.Category);
        Assert.AreEqual(TournamentFormat.Flip, entry.Format);
        AssertMoney(0.04m, entry.BuyIn);
        AssertMoney(0.40m, entry.TicketValueWon);
        AssertMoney(1.20m, entry.CashPrize);
        Assert.IsNull(entry.Placement);
        Assert.AreEqual(10, entry.FieldSize);
        Assert.IsFalse(entry.ITM);
        Assert.IsFalse(entry.FinalTable);
        Assert.AreEqual("Flip, Preset", entry.Tags);
    }

    [TestMethod]
    public void TournamentPresetUpsertUpdatesMatchingPreset()
    {
        var presets = new List<TournamentPreset>();
        var source = new TournamentEntry
        {
            Platform = Platform.Unibet,
            Category = TournamentCategory.FlipSatellite,
            Format = TournamentFormat.Flip,
            EventName = "Supermoon Flip",
            BuyIn = 0.04m,
            ActualBullets = 1,
            TicketValueWon = 0.40m
        };

        var first = TournamentPresetService.UpsertFromEntry(
            presets,
            source,
            "Supermoon Flip",
            new DateTime(2026, 6, 9, 12, 0, 0, DateTimeKind.Utc));
        source.TicketValueWon = 0.50m;
        var second = TournamentPresetService.UpsertFromEntry(
            presets,
            source,
            " supermoon flip ",
            new DateTime(2026, 6, 9, 12, 5, 0, DateTimeKind.Utc));

        Assert.HasCount(1, presets);
        Assert.AreEqual(first.Id, second.Id);
        AssertMoney(0.50m, presets[0].TicketValueWon);
    }

    [TestMethod]
    public void TournamentPresetQuickEntryClearsPresetResultsForRegisteredEntries()
    {
        var source = new TournamentEntry
        {
            Platform = Platform.Unibet,
            Category = TournamentCategory.MainGrind,
            Format = TournamentFormat.MTT,
            EventName = "Daily",
            BuyIn = 1.10m,
            ActualBullets = 1,
            CashPrize = 12.50m,
            TicketValueWon = 5m,
            ITM = true,
            Placement = 1
        };
        var preset = TournamentPresetService.CreateFromEntry(
            source,
            "Daily",
            new DateTime(2026, 6, 9, 12, 0, 0, DateTimeKind.Utc));

        var entry = TournamentPresetService.CreateQuickEntry(
            preset,
            new DateOnly(2026, 6, 10),
            new TimeOnly(19, 30),
            finished: false,
            winAmount: 99m);

        Assert.AreEqual(TournamentStatus.Registered, entry.Status);
        Assert.AreEqual(new DateOnly(2026, 6, 10), entry.Date);
        Assert.AreEqual(new TimeOnly(19, 30), entry.RegistrationTime);
        Assert.IsNull(entry.FinishedDate);
        Assert.IsNull(entry.FinishedTime);
        AssertMoney(0m, entry.CashPrize);
        AssertMoney(0m, entry.TicketValueWon);
        Assert.IsFalse(entry.ITM);
        Assert.IsNull(entry.Placement);
        CollectionAssert.AreEqual(Array.Empty<string>(), EntryValidator.Validate(entry));
    }

    [TestMethod]
    public void TournamentPresetQuickEntryAppliesWinAmountToFinishedSatellite()
    {
        var preset = new TournamentPreset
        {
            Name = "Target satellite",
            Platform = Platform.Unibet,
            Category = TournamentCategory.FlipSatellite,
            Format = TournamentFormat.Satellite,
            BuyIn = 1m,
            ActualBullets = 1
        };

        var entry = TournamentPresetService.CreateQuickEntry(
            preset,
            new DateOnly(2026, 6, 10),
            new TimeOnly(20, 15),
            finished: true,
            winAmount: 11m);

        Assert.AreEqual(TournamentStatus.Finished, entry.Status);
        Assert.AreEqual(new DateOnly(2026, 6, 10), entry.FinishedDate);
        Assert.AreEqual(new TimeOnly(20, 15), entry.FinishedTime);
        AssertMoney(11m, entry.TicketValueWon);
        Assert.IsTrue(entry.TicketWon);
        AssertMoney(11m, entry.TargetEventBuyIn);
        Assert.IsTrue(entry.ITM);
        CollectionAssert.AreEqual(Array.Empty<string>(), EntryValidator.Validate(entry));
    }
}
