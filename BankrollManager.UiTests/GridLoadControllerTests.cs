using System.ComponentModel;
using BankrollManager.App;
using BankrollManager.Core.Models;

namespace BankrollManager.UiTests;

[TestClass]
public sealed class GridLoadControllerTests
{
    [TestMethod]
    public void LoadUsesDefaultVisibleWindow()
    {
        var source = new BindingSource();
        var controller = new GridLoadController<TournamentEntry>(source, defaultVisibleLimit: 2, loadIncrement: 2);
        var rows = BuildTournamentRows(5);

        controller.SetRows(rows, loadNow: true);

        Assert.AreEqual(5, controller.TotalCount);
        Assert.AreEqual(2, controller.VisibleCount);
        Assert.AreEqual(2, source.Count);
        CollectionAssert.AreEqual(
            new[] { "Event 0", "Event 1" },
            source.Cast<TournamentEntry>().Select(entry => entry.EventName).ToArray());
    }

    [TestMethod]
    public void ShowMoreAddsOneWindow()
    {
        var source = new BindingSource();
        var controller = new GridLoadController<TournamentEntry>(source, defaultVisibleLimit: 2, loadIncrement: 2);
        controller.SetRows(BuildTournamentRows(5), loadNow: true);

        controller.ShowMore();

        Assert.AreEqual(4, controller.VisibleCount);
        Assert.AreEqual(4, source.Count);
    }

    [TestMethod]
    public void ShowAllBindsEveryRow()
    {
        var source = new BindingSource();
        var controller = new GridLoadController<TournamentEntry>(source, defaultVisibleLimit: 2, loadIncrement: 2);
        controller.SetRows(BuildTournamentRows(5), loadNow: true);

        controller.ShowAll();

        Assert.AreEqual(5, controller.VisibleCount);
        Assert.AreEqual(5, source.Count);
        Assert.IsFalse(controller.CanShowMore);
    }

    [TestMethod]
    public void SortAppliesToFullBackingRowsBeforeWindowing()
    {
        var source = new BindingSource();
        var controller = new GridLoadController<TournamentEntry>(source, defaultVisibleLimit: 2, loadIncrement: 2);
        var rows = new[]
        {
            new TournamentEntry { EventName = "Zulu" },
            new TournamentEntry { EventName = "Yankee" },
            new TournamentEntry { EventName = "Alpha" }
        };
        controller.SetRows(rows, loadNow: true);

        Assert.IsTrue(controller.TryApplySort(nameof(TournamentEntry.EventName), ListSortDirection.Ascending));

        CollectionAssert.AreEqual(
            new[] { "Alpha", "Yankee" },
            source.Cast<TournamentEntry>().Select(entry => entry.EventName).ToArray());
    }

    [TestMethod]
    public void SortDateUsesTimeTieBreaker()
    {
        var source = new BindingSource();
        var controller = new GridLoadController<TournamentEntry>(source, defaultVisibleLimit: 3, loadIncrement: 3);
        var date = new DateOnly(2026, 6, 14);
        var rows = new[]
        {
            new TournamentEntry { Date = date, RegistrationTime = new TimeOnly(9, 0), EventName = "Early" },
            new TournamentEntry { Date = date.AddDays(-1), RegistrationTime = new TimeOnly(23, 0), EventName = "Previous" },
            new TournamentEntry { Date = date, RegistrationTime = new TimeOnly(21, 0), EventName = "Late" }
        };
        controller.SetRows(rows, loadNow: true);

        Assert.IsTrue(controller.TryApplySort(nameof(TournamentEntry.Date), ListSortDirection.Descending));

        CollectionAssert.AreEqual(
            new[] { "Late", "Early", "Previous" },
            source.Cast<TournamentEntry>().Select(entry => entry.EventName).ToArray());
    }

    [TestMethod]
    public void EnsureVisibleExpandsWindowToMatchingRow()
    {
        var source = new BindingSource();
        var controller = new GridLoadController<TournamentEntry>(source, defaultVisibleLimit: 2, loadIncrement: 2);
        controller.SetRows(BuildTournamentRows(5), loadNow: true);

        Assert.IsTrue(controller.EnsureVisible(entry => entry.EventName == "Event 4"));

        Assert.AreEqual(5, controller.VisibleCount);
        Assert.AreEqual(5, source.Count);
        Assert.IsTrue(source.Cast<TournamentEntry>().Any(entry => entry.EventName == "Event 4"));
    }

    [TestMethod]
    public void SetRowsResetsVisibleWindowAfterShowAll()
    {
        var source = new BindingSource();
        var controller = new GridLoadController<TournamentEntry>(source, defaultVisibleLimit: 2, loadIncrement: 2);
        controller.SetRows(BuildTournamentRows(5), loadNow: true);
        controller.ShowAll();

        controller.SetRows(BuildTournamentRows(4), loadNow: true);

        Assert.AreEqual(4, controller.TotalCount);
        Assert.AreEqual(2, controller.VisibleCount);
        Assert.AreEqual(2, source.Count);
        Assert.IsTrue(controller.CanShowMore);
    }

    [TestMethod]
    public void ReleaseScaleRowsKeepInitialBindingSmallAndRevealDeepRows()
    {
        var source = new BindingSource();
        var controller = new GridLoadController<TournamentEntry>(source);

        controller.SetRows(BuildTournamentRows(10_000), loadNow: true);

        Assert.AreEqual(10_000, controller.TotalCount);
        Assert.AreEqual(GridLoadController<TournamentEntry>.DefaultVisibleLimit, controller.VisibleCount);
        Assert.AreEqual(GridLoadController<TournamentEntry>.DefaultVisibleLimit, source.Count);

        Assert.IsTrue(controller.EnsureVisible(entry => entry.EventName == "Event 8750"));

        Assert.AreEqual(9_000, controller.VisibleCount);
        Assert.AreEqual(9_000, source.Count);
        Assert.IsTrue(source.Cast<TournamentEntry>().Any(entry => entry.EventName == "Event 8750"));
    }

    private static IReadOnlyList<TournamentEntry> BuildTournamentRows(int count)
    {
        return Enumerable.Range(0, count)
            .Select(index => new TournamentEntry
            {
                Date = new DateOnly(2026, 6, 14).AddDays(-index),
                EventName = $"Event {index}"
            })
            .ToArray();
    }
}
