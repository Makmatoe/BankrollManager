using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("BankrollManager.UiTests")]

namespace BankrollManager.App;

internal sealed class SortableBindingList<T> : BindingList<T>
{
    private static readonly IReadOnlyDictionary<string, string[]> DateTimeTieBreakers =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["Date"] = ["Time", "RegistrationTime", "SessionTime"],
            ["FinishedDate"] = ["FinishedTime"],
            ["ClosedDate"] = ["ClosedTime"]
        };

    private bool _isSorted;
    private ListSortDirection _sortDirection = ListSortDirection.Descending;
    private PropertyDescriptor? _sortProperty;

    public SortableBindingList(IEnumerable<T> items)
        : base(items.ToList())
    {
    }

    protected override bool SupportsSortingCore => true;

    protected override bool IsSortedCore => _isSorted;

    protected override ListSortDirection SortDirectionCore => _sortDirection;

    protected override PropertyDescriptor? SortPropertyCore => _sortProperty;

    protected override void ApplySortCore(PropertyDescriptor property, ListSortDirection direction)
    {
        if (Items is not List<T> list)
        {
            return;
        }

        list.Sort((left, right) => CompareItems(left, right, property, direction));
        _sortProperty = property;
        _sortDirection = direction;
        _isSorted = true;
        OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
    }

    protected override void RemoveSortCore()
    {
        _isSorted = false;
        _sortProperty = null;
        OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
    }

    private static int CompareValues(object? left, object? right, ListSortDirection direction)
    {
        if (ReferenceEquals(left, right))
        {
            return 0;
        }

        if (left is null)
        {
            return 1;
        }

        if (right is null)
        {
            return -1;
        }

        var result = CompareValues(left, right);
        return direction == ListSortDirection.Ascending ? result : -result;
    }

    private static int CompareItems(T left, T right, PropertyDescriptor property, ListSortDirection direction)
    {
        var result = CompareValues(property.GetValue(left), property.GetValue(right), direction);
        if (result != 0 || !DateTimeTieBreakers.TryGetValue(property.Name, out var timePropertyNames))
        {
            return result;
        }

        return CompareTimes(left, right, timePropertyNames, direction);
    }

    private static int CompareTimes(T left, T right, IEnumerable<string> propertyNames, ListSortDirection direction)
    {
        foreach (var propertyName in propertyNames)
        {
            var property = typeof(T).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            if (property is null || property.PropertyType != typeof(TimeOnly?) && property.PropertyType != typeof(TimeOnly))
            {
                continue;
            }

            return CompareValues(property.GetValue(left), property.GetValue(right), direction);
        }

        return 0;
    }

    private static int CompareValues(object? left, object? right)
    {
        if (left is string leftText && right is string rightText)
        {
            return string.Compare(leftText, rightText, StringComparison.CurrentCultureIgnoreCase);
        }

        if (left is IComparable comparable)
        {
            return comparable.CompareTo(right);
        }

        var leftFallback = left?.ToString() ?? string.Empty;
        var rightFallback = right?.ToString() ?? string.Empty;
        return string.Compare(leftFallback, rightFallback, StringComparison.CurrentCultureIgnoreCase);
    }
}
