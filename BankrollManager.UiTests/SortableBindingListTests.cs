using System.ComponentModel;
using BankrollManager.App;
using BankrollManager.Core.Models;

namespace BankrollManager.UiTests;

[TestClass]
public sealed class SortableBindingListTests
{
    [TestMethod]
    public void SortingDateFallsBackToRegistrationTimeWhenAvailable()
    {
        var date = new DateOnly(2026, 6, 9);
        var early = new TournamentEntry
        {
            Date = date,
            RegistrationTime = new TimeOnly(9, 30),
            EventName = "Early"
        };
        var late = new TournamentEntry
        {
            Date = date,
            RegistrationTime = new TimeOnly(20, 15),
            EventName = "Late"
        };
        var previousDay = new TournamentEntry
        {
            Date = date.AddDays(-1),
            RegistrationTime = new TimeOnly(23, 0),
            EventName = "Previous"
        };
        var list = new SortableBindingList<TournamentEntry>([early, previousDay, late]);

        ApplySort(list, "Date", ListSortDirection.Descending);

        CollectionAssert.AreEqual(
            new[] { "Late", "Early", "Previous" },
            list.Select(entry => entry.EventName).ToArray());

        ApplySort(list, "Date", ListSortDirection.Ascending);

        CollectionAssert.AreEqual(
            new[] { "Previous", "Early", "Late" },
            list.Select(entry => entry.EventName).ToArray());
    }

    [TestMethod]
    public void SortingFinishedDateFallsBackToFinishedTimeWhenAvailable()
    {
        var finishedDate = new DateOnly(2026, 6, 9);
        var earlyFinish = new TournamentEntry
        {
            FinishedDate = finishedDate,
            FinishedTime = new TimeOnly(10, 0),
            EventName = "Early finish"
        };
        var lateFinish = new TournamentEntry
        {
            FinishedDate = finishedDate,
            FinishedTime = new TimeOnly(23, 30),
            EventName = "Late finish"
        };
        var nextDay = new TournamentEntry
        {
            FinishedDate = finishedDate.AddDays(1),
            FinishedTime = new TimeOnly(0, 15),
            EventName = "Next day"
        };
        var list = new SortableBindingList<TournamentEntry>([earlyFinish, nextDay, lateFinish]);

        ApplySort(list, "FinishedDate", ListSortDirection.Ascending);

        CollectionAssert.AreEqual(
            new[] { "Early finish", "Late finish", "Next day" },
            list.Select(entry => entry.EventName).ToArray());
    }

    private static void ApplySort<T>(
        SortableBindingList<T> list,
        string propertyName,
        ListSortDirection direction)
    {
        var property = TypeDescriptor.GetProperties(typeof(T))[propertyName];
        Assert.IsNotNull(property, $"{typeof(T).Name}.{propertyName} was not found.");
        ((IBindingList)list).ApplySort(property, direction);
    }
}
