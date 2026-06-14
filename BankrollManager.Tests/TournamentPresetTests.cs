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
        AssertMoney(0m, entry.TicketValueWon);
        AssertMoney(0m, entry.CashPrize);
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
        Assert.AreEqual(new TimeOnly(20, 16), entry.FinishedTime);
        AssertMoney(11m, entry.TicketValueWon);
        Assert.IsTrue(entry.TicketWon);
        AssertMoney(11m, entry.TargetEventBuyIn);
        Assert.IsTrue(entry.ITM);
        CollectionAssert.AreEqual(Array.Empty<string>(), EntryValidator.Validate(entry));
    }

    [TestMethod]
    public void TournamentPresetQuickEntryClearsPresetTicketBuyInSoRiskUsesCashCost()
    {
        var preset = new TournamentPreset
        {
            Name = "Ticketed Daily",
            Platform = Platform.Unibet,
            Category = TournamentCategory.MainGrind,
            Format = TournamentFormat.MTT,
            BuyIn = 2m,
            ActualBullets = 1,
            TicketBuyInValue = 2m,
            TicketBuyInPlatform = Platform.Unibet
        };

        var entry = TournamentPresetService.CreateQuickEntry(
            preset,
            new DateOnly(2026, 6, 10),
            new TimeOnly(20, 15),
            finished: false,
            winAmount: 0m);

        AssertMoney(0m, entry.TicketBuyInValue);
        Assert.IsNull(entry.TicketBuyInPlatform);
        AssertMoney(2m, entry.CashCost);

        entry.Status = TournamentStatus.Finished;
        entry.FinishedDate = entry.Date;
        entry.FinishedTime = new TimeOnly(22, 0);
        entry.CashPrize = 5m;

        var data = new BankrollData
        {
            Settings = new BankrollSettings { StartingBankroll = 40m },
            TournamentEntries = [entry]
        };

        BankrollCalculator.RecalculateTrackingFields(data);
        var timeline = BankrollCalculator.GetAuditTimeline(data);

        AssertMoney(5m, entry.RiskPercentageOfBankrollAtRegistration);
        AssertMoney(2m, timeline[0].CostRisk);
        Assert.AreEqual("Tournament Buy-in", timeline[0].Type);
    }

    [TestMethod]
    public void TournamentPresetQuickEntryCanApplyExplicitTicketUseAndRealizedSatelliteResult()
    {
        var preset = new TournamentPreset
        {
            Name = "Target satellite",
            Platform = Platform.Unibet,
            Category = TournamentCategory.FlipSatellite,
            Format = TournamentFormat.Satellite,
            BuyIn = 5m,
            ActualBullets = 1,
            TargetEventName = "Sunday target"
        };

        var entry = TournamentPresetService.CreateQuickEntry(
            preset,
            new TournamentQuickEntryRequest
            {
                RegistrationDate = new DateOnly(2026, 6, 10),
                RegistrationTime = new TimeOnly(20, 15),
                TicketBuyInValue = 2m,
                TicketBuyInPlatform = Platform.GGPoker,
                Finished = true,
                FinishedDate = new DateOnly(2026, 6, 11),
                FinishedTime = new TimeOnly(0, 5),
                ResultKind = TournamentQuickResultKind.RealizedTicket,
                ResultAmount = 11m
            });

        Assert.AreEqual(TournamentStatus.Finished, entry.Status);
        Assert.AreEqual(new DateOnly(2026, 6, 11), entry.FinishedDate);
        Assert.AreEqual(new TimeOnly(0, 5), entry.FinishedTime);
        AssertMoney(2m, entry.TicketBuyInValue);
        Assert.AreEqual(Platform.GGPoker, entry.TicketBuyInPlatform);
        AssertMoney(3m, entry.CashCost);
        AssertMoney(11m, entry.TicketValueWon);
        Assert.IsTrue(entry.TicketWon);
        Assert.IsTrue(entry.TicketConvertedRealized);
        AssertMoney(0m, entry.TicketReturnAmount);
        AssertMoney(-2m, entry.TicketBalanceImpact);
        CollectionAssert.AreEqual(Array.Empty<string>(), EntryValidator.Validate(entry));
    }

    [TestMethod]
    public void QuickAddedEntryKeepsRiskAndTrackingFieldsAfterBulkFinish()
    {
        var preset = new TournamentPreset
        {
            Name = "Ticket target",
            Platform = Platform.Unibet,
            Category = TournamentCategory.MainGrind,
            Format = TournamentFormat.MTT,
            BuyIn = 10m,
            ActualBullets = 1
        };
        var entry = TournamentPresetService.CreateQuickEntry(
            preset,
            new TournamentQuickEntryRequest
            {
                RegistrationDate = new DateOnly(2026, 6, 10),
                RegistrationTime = new TimeOnly(19, 0),
                TicketBuyInValue = 4m,
                TicketBuyInPlatform = Platform.GGPoker
            });

        TournamentPresetService.ApplyFinish(
            entry,
            new TournamentFinishRequest
            {
                FinishedDate = new DateOnly(2026, 6, 10),
                FinishedTime = new TimeOnly(21, 30),
                ResultKind = TournamentQuickResultKind.CashPrize,
                ResultAmount = 20m
            });
        var data = new BankrollData
        {
            Settings = new BankrollSettings { StartingBankroll = 100m },
            TournamentEntries = [entry]
        };

        BankrollCalculator.RecalculateTrackingFields(data);

        AssertMoney(4m, entry.TicketBuyInValue);
        Assert.AreEqual(Platform.GGPoker, entry.TicketBuyInPlatform);
        AssertMoney(6m, entry.CashCost);
        AssertMoney(6m, entry.RiskPercentageOfBankrollAtRegistration);
        AssertMoney(100m, entry.BankrollBefore);
        AssertMoney(114m, entry.BankrollAfter);
        AssertMoney(20m, entry.CashPrize);
        CollectionAssert.AreEqual(Array.Empty<string>(), EntryValidator.Validate(entry));
    }

    [TestMethod]
    public void BulkFinishCandidateUsesTicketFieldsForSatelliteResults()
    {
        var entry = new TournamentEntry
        {
            Date = new DateOnly(2026, 6, 10),
            RegistrationTime = new TimeOnly(20, 0),
            Status = TournamentStatus.Registered,
            EventName = "Target stack",
            Platform = Platform.GGPoker,
            Category = TournamentCategory.FlipSatellite,
            Format = TournamentFormat.TargetStackSatellite,
            BuyIn = 1m,
            ActualBullets = 1
        };

        Assert.IsTrue(TournamentPresetService.IsBulkFinishCandidate(entry));

        TournamentPresetService.ApplyFinish(
            entry,
            new TournamentFinishRequest
            {
                FinishedDate = new DateOnly(2026, 6, 10),
                FinishedTime = new TimeOnly(20, 1),
                ResultKind = TournamentQuickResultKind.Auto,
                ResultAmount = 12m
            });

        Assert.AreEqual(TournamentStatus.Finished, entry.Status);
        AssertMoney(12m, entry.TicketValueWon);
        Assert.IsTrue(entry.TicketWon);
        AssertMoney(12m, entry.TargetEventBuyIn);
        AssertMoney(12m, entry.TicketBalanceImpact);
        CollectionAssert.AreEqual(Array.Empty<string>(), EntryValidator.Validate(entry));
    }

    [TestMethod]
    public void PresetUpdatePreservesManagerMetadataAndOrdering()
    {
        var created = new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        var used = new DateTime(2026, 6, 9, 12, 0, 0, DateTimeKind.Utc);
        var preset = new TournamentPreset
        {
            Id = Guid.NewGuid(),
            Name = "Daily",
            Platform = Platform.Unibet,
            Category = TournamentCategory.MainGrind,
            Format = TournamentFormat.MTT,
            BuyIn = 1m,
            IsFavorite = true,
            SortOrder = 20,
            CreatedUtc = created,
            LastUsedUtc = used
        };
        var entry = new TournamentEntry
        {
            EventName = "Updated Daily",
            Platform = Platform.GGPoker,
            Category = TournamentCategory.TowerShot,
            Format = TournamentFormat.FlipAndGo,
            BuyIn = 2m,
            ActualBullets = 1,
            FlipBuyInPerStack = 2m,
            FlipStacksBought = 1
        };

        TournamentPresetService.UpdateFromEntry(
            preset,
            entry,
            "Updated Daily",
            new DateTime(2026, 6, 10, 12, 0, 0, DateTimeKind.Utc));

        Assert.AreEqual("Updated Daily", preset.Name);
        Assert.AreEqual(Platform.GGPoker, preset.Platform);
        Assert.AreEqual(TournamentFormat.FlipAndGo, preset.Format);
        Assert.IsTrue(preset.IsFavorite);
        Assert.AreEqual(20, preset.SortOrder);
        Assert.AreEqual(created, preset.CreatedUtc);
        Assert.AreEqual(used, preset.LastUsedUtc);

        var ordered = TournamentPresetService.OrderedPresets(
            [
                new TournamentPreset { Name = "B", BuyIn = 1m, SortOrder = 30 },
                preset,
                new TournamentPreset { Name = "A", BuyIn = 1m }
            ]);

        Assert.AreEqual(preset.Id, ordered[0].Id);
        Assert.AreEqual("B", ordered[1].Name);
        Assert.AreEqual("A", ordered[2].Name);
    }

    [TestMethod]
    public void FavoriteQuickAddFlipAndGoBulkFinishKeepsTicketRiskAndPresetOrdering()
    {
        var favorite = new TournamentPreset
        {
            Name = "Favorite Flip & Go",
            Platform = Platform.GGPoker,
            Category = TournamentCategory.FlipSatellite,
            Format = TournamentFormat.FlipAndGo,
            EventTag = EventTag.FlipAndGo,
            BuyIn = 0m,
            FlipBuyInPerStack = 1m,
            FlipStacksBought = 2,
            IsFavorite = true,
            SortOrder = 20,
            TicketValueWon = 99m
        };
        var presets = new[]
        {
            new TournamentPreset
            {
                Name = "Regular MTT",
                Platform = Platform.Unibet,
                Category = TournamentCategory.MainGrind,
                Format = TournamentFormat.MTT,
                BuyIn = 2m,
                SortOrder = 10
            },
            favorite
        };

        var ordered = TournamentPresetService.OrderedPresets(presets);
        var entry = TournamentPresetService.CreateQuickEntry(
            ordered[0],
            new TournamentQuickEntryRequest
            {
                RegistrationDate = new DateOnly(2026, 6, 14),
                RegistrationTime = new TimeOnly(20, 30),
                TicketBuyInValue = 0.50m,
                TicketBuyInPlatform = Platform.Unibet
            });

        Assert.AreEqual(favorite.Id, ordered[0].Id);
        Assert.AreEqual(TournamentStatus.Registered, entry.Status);
        AssertMoney(0m, entry.TicketValueWon);
        Assert.IsTrue(TournamentPresetService.IsBulkFinishCandidate(entry));

        TournamentPresetService.ApplyFinish(
            entry,
            new TournamentFinishRequest
            {
                FinishedDate = new DateOnly(2026, 6, 14),
                FinishedTime = new TimeOnly(20, 31),
                ResultKind = TournamentQuickResultKind.Auto,
                ResultAmount = 8m,
                FlipPhaseWon = true,
                GoPhaseReached = true
            });
        var data = new BankrollData
        {
            Settings = new BankrollSettings { StartingBankroll = 50m },
            TournamentEntries = [entry]
        };

        BankrollCalculator.RecalculateTrackingFields(data);

        Assert.AreEqual(TournamentStatus.Finished, entry.Status);
        Assert.AreEqual(new DateOnly(2026, 6, 14), entry.FinishedDate);
        Assert.AreEqual(new TimeOnly(20, 31), entry.FinishedTime);
        AssertMoney(2m, entry.TotalCost);
        AssertMoney(0.50m, entry.TicketBuyInValue);
        Assert.AreEqual(Platform.Unibet, entry.TicketBuyInPlatform);
        AssertMoney(1.50m, entry.CashCost);
        AssertMoney(3m, entry.RiskPercentageOfBankrollAtRegistration);
        AssertMoney(8m, entry.PrizeWon);
        AssertMoney(-0.50m, entry.TicketBalanceImpact);
        CollectionAssert.AreEqual(Array.Empty<string>(), EntryValidator.Validate(entry));
    }
}
