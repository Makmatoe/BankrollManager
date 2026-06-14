using System.ComponentModel;
using BankrollManager.App;
using BankrollManager.Core.Models;

namespace BankrollManager.UiTests;

[TestClass]
public sealed class DetailTableFilterTests
{
    [TestMethod]
    public void TournamentFiltersApplyBeforeProgressiveWindowing()
    {
        var today = new DateOnly(2026, 6, 14);
        var criteria = DetailTableFilterCriteria.Default(today) with
        {
            DateRange = DetailTableDateRange.CurrentMonth,
            SearchText = "flip",
            FlipsOnly = true
        };
        var rows = new[]
        {
            new TournamentEntry
            {
                Date = today,
                Category = TournamentCategory.FlipSatellite,
                Format = TournamentFormat.Flip,
                EventName = "Morning Flip"
            },
            new TournamentEntry
            {
                Date = today.AddDays(-1),
                Format = TournamentFormat.FlipAndGo,
                EventName = "Evening Flip"
            },
            new TournamentEntry
            {
                Date = today.AddMonths(-1),
                Category = TournamentCategory.FlipSatellite,
                Format = TournamentFormat.Flip,
                EventName = "Old Flip"
            },
            new TournamentEntry
            {
                Date = today,
                Format = TournamentFormat.MTT,
                EventName = "Regular MTT"
            }
        };
        var source = new BindingSource();
        var controller = new GridLoadController<TournamentEntry>(source, defaultVisibleLimit: 1, loadIncrement: 1);

        controller.SetRows(DetailTableFilter.Apply(rows, criteria), loadNow: true);

        Assert.AreEqual(2, controller.TotalCount);
        Assert.AreEqual(1, controller.VisibleCount);
        Assert.AreEqual(1, source.Count);

        controller.ShowMore();

        Assert.AreEqual(2, controller.VisibleCount);
        CollectionAssert.AreEqual(
            new[] { "Morning Flip", "Evening Flip" },
            source.Cast<TournamentEntry>().Select(entry => entry.EventName).ToArray());
    }

    [TestMethod]
    public void CashFiltersSearchStatusRiskAndProfit()
    {
        var today = new DateOnly(2026, 6, 14);
        var criteria = DetailTableFilterCriteria.Default(today) with
        {
            DateRange = DetailTableDateRange.Last30Days,
            SearchText = "rush",
            FinishedOnly = true,
            HighRiskOnly = true,
            ProfitableOnly = true
        };
        var rows = new[]
        {
            new CashSession
            {
                Date = today,
                Status = CashSessionStatus.Finished,
                Format = CashFormat.RushAndCashHoldem,
                Game = "Rush",
                StartStackBuyIn = 5m,
                Cashout = 7m,
                RiskPercentageOfBankrollAtSessionStart = 7m
            },
            new CashSession
            {
                Date = today,
                Status = CashSessionStatus.Active,
                Format = CashFormat.RushAndCashHoldem,
                Game = "Rush",
                StartStackBuyIn = 5m,
                RiskPercentageOfBankrollAtSessionStart = 8m
            },
            new CashSession
            {
                Date = today,
                Status = CashSessionStatus.Finished,
                Format = CashFormat.HoldemCash,
                Game = "Regular",
                StartStackBuyIn = 5m,
                Cashout = 7m,
                RiskPercentageOfBankrollAtSessionStart = 7m
            }
        };

        var filtered = DetailTableFilter.Apply(rows, criteria).ToArray();

        Assert.HasCount(1, filtered);
        Assert.AreEqual(CashFormat.RushAndCashHoldem, filtered[0].Format);
    }

    [TestMethod]
    public void LedgerFiltersCustomDateAndTicketText()
    {
        var criteria = DetailTableFilterCriteria.Default(new DateOnly(2026, 6, 14)) with
        {
            DateRange = DetailTableDateRange.Custom,
            CustomFrom = new DateOnly(2026, 6, 1),
            CustomTo = new DateOnly(2026, 6, 30),
            TicketRelatedOnly = true,
            SearchText = "bonus"
        };
        var rows = new[]
        {
            new LedgerEntry
            {
                Date = new DateOnly(2026, 6, 10),
                Type = LedgerType.TicketCredit,
                Description = "Ticket bonus"
            },
            new LedgerEntry
            {
                Date = new DateOnly(2026, 7, 1),
                Type = LedgerType.TicketCredit,
                Description = "Ticket bonus"
            },
            new LedgerEntry
            {
                Date = new DateOnly(2026, 6, 10),
                Type = LedgerType.Deposit,
                Description = "Cash bonus"
            }
        };

        var filtered = DetailTableFilter.Apply(rows, criteria).ToArray();

        Assert.HasCount(1, filtered);
        Assert.AreEqual(LedgerType.TicketCredit, filtered[0].Type);
    }

    [TestMethod]
    public void TimelineFiltersTextAndResultDirection()
    {
        var criteria = DetailTableFilterCriteria.Default(new DateOnly(2026, 6, 14)) with
        {
            SearchText = "satellite",
            ProfitableOnly = true
        };
        var rows = new[]
        {
            new AuditTimelineEntry(
                new DateOnly(2026, 6, 14),
                null,
                "Tournament",
                "Late satellite",
                2m,
                8m,
                10m,
                18m,
                "OK"),
            new AuditTimelineEntry(
                new DateOnly(2026, 6, 14),
                null,
                "Tournament",
                "Losing satellite",
                2m,
                -2m,
                10m,
                8m,
                "OK")
        };

        var filtered = DetailTableFilter.Apply(rows, criteria).ToArray();

        Assert.HasCount(1, filtered);
        Assert.AreEqual("Late satellite", filtered[0].Name);
    }

    [TestMethod]
    public void LargeFilteredTournamentSetReportsFilteredTotalBeforeWindowing()
    {
        var today = new DateOnly(2026, 6, 14);
        var criteria = DetailTableFilterCriteria.Default(today) with
        {
            DateRange = DetailTableDateRange.Last30Days,
            SearchText = "release",
            FlipsOnly = true,
            TicketRelatedOnly = true
        };
        var rows = Enumerable.Range(0, 5_000)
            .Select(index =>
            {
                var target = index % 3 == 0;
                return new TournamentEntry
                {
                    Date = today.AddDays(-(index % 20)),
                    Category = target ? TournamentCategory.FlipSatellite : TournamentCategory.MainGrind,
                    Format = target ? TournamentFormat.Flip : TournamentFormat.MTT,
                    EventName = target ? $"Release flip ticket {index}" : $"Regular MTT {index}",
                    TicketValueWon = target ? 1m : 0m,
                    TicketWon = target,
                    TargetEventBuyIn = target ? 1m : 0m
                };
            })
            .ToArray();
        var source = new BindingSource();
        var controller = new GridLoadController<TournamentEntry>(source);
        var expectedFilteredCount = rows.Count(entry => entry.EventName.StartsWith("Release", StringComparison.Ordinal));

        controller.SetRows(DetailTableFilter.Apply(rows, criteria), loadNow: true);

        Assert.AreEqual(expectedFilteredCount, controller.TotalCount);
        Assert.AreEqual(GridLoadController<TournamentEntry>.DefaultVisibleLimit, controller.VisibleCount);
        Assert.AreEqual(GridLoadController<TournamentEntry>.DefaultVisibleLimit, source.Count);
        Assert.IsTrue(controller.CanShowMore);

        controller.ShowMore();

        Assert.AreEqual(GridLoadController<TournamentEntry>.DefaultVisibleLimit * 2, controller.VisibleCount);
        Assert.AreEqual(GridLoadController<TournamentEntry>.DefaultVisibleLimit * 2, source.Count);
    }
}
