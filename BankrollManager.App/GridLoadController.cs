using System.ComponentModel;

namespace BankrollManager.App;

internal interface IGridLoadController
{
    event EventHandler? ViewChanged;

    int TotalCount { get; }

    int VisibleCount { get; }

    bool CanShowMore { get; }

    void Load();

    void ShowMore();

    void ShowAll();

    bool TryApplySort(string propertyName, ListSortDirection direction);
}

internal sealed class GridLoadController<T> : IGridLoadController where T : class
{
    public const int DefaultVisibleLimit = 500;
    public const int DefaultLoadIncrement = 500;

    private readonly BindingSource _source;
    private readonly int _defaultVisibleLimit;
    private readonly int _loadIncrement;
    private List<T> _allRows = [];
    private bool _isDirty = true;
    private int _visibleLimit;
    private PropertyDescriptor? _sortProperty;
    private ListSortDirection _sortDirection = ListSortDirection.Descending;

    public GridLoadController(
        BindingSource source,
        int defaultVisibleLimit = DefaultVisibleLimit,
        int loadIncrement = DefaultLoadIncrement)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (defaultVisibleLimit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(defaultVisibleLimit), "The default visible limit must be positive.");
        }

        if (loadIncrement <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(loadIncrement), "The load increment must be positive.");
        }

        _source = source;
        _defaultVisibleLimit = defaultVisibleLimit;
        _loadIncrement = loadIncrement;
        _visibleLimit = defaultVisibleLimit;
    }

    public event EventHandler? ViewChanged;

    public int TotalCount => _allRows.Count;

    public int VisibleCount => Math.Min(VisibleLimitForCount(), TotalCount);

    public bool CanShowMore => VisibleCount < TotalCount;

    public void SetRows(IEnumerable<T> rows, bool loadNow)
    {
        ArgumentNullException.ThrowIfNull(rows);

        _allRows = rows.ToList();
        _visibleLimit = _defaultVisibleLimit;
        if (_sortProperty is not null)
        {
            SortAllRows();
        }

        _isDirty = true;
        if (loadNow)
        {
            Load();
            return;
        }

        OnViewChanged();
    }

    public void Load()
    {
        if (_isDirty)
        {
            ApplyVisibleRows();
        }
        else
        {
            OnViewChanged();
        }
    }

    public void ShowMore()
    {
        if (!CanShowMore)
        {
            return;
        }

        _visibleLimit = Math.Min(TotalCount, VisibleLimitForCount() + _loadIncrement);
        _isDirty = true;
        Load();
    }

    public void ShowAll()
    {
        if (!CanShowMore)
        {
            return;
        }

        _visibleLimit = int.MaxValue;
        _isDirty = true;
        Load();
    }

    public bool TryApplySort(string propertyName, ListSortDirection direction)
    {
        var property = TypeDescriptor.GetProperties(typeof(T))[propertyName];
        if (property is null)
        {
            return false;
        }

        _sortProperty = property;
        _sortDirection = direction;
        SortAllRows();
        _isDirty = true;
        Load();
        return true;
    }

    public bool EnsureVisible(Predicate<T> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        var index = _allRows.FindIndex(predicate);
        if (index < 0)
        {
            return false;
        }

        var requiredCount = index + 1;
        if (_visibleLimit != int.MaxValue && requiredCount > _visibleLimit)
        {
            _visibleLimit = RoundUpToIncrement(requiredCount);
            _isDirty = true;
        }

        Load();
        return true;
    }

    private void SortAllRows()
    {
        if (_sortProperty is null)
        {
            return;
        }

        _allRows.Sort((left, right) => SortableBindingList<T>.CompareItems(left, right, _sortProperty, _sortDirection));
    }

    private int VisibleLimitForCount()
    {
        return _visibleLimit == int.MaxValue
            ? int.MaxValue
            : Math.Max(_defaultVisibleLimit, _visibleLimit);
    }

    private int RoundUpToIncrement(int requiredCount)
    {
        var rounded = ((requiredCount + _loadIncrement - 1) / _loadIncrement) * _loadIncrement;
        return Math.Min(TotalCount, Math.Max(_defaultVisibleLimit, rounded));
    }

    private void ApplyVisibleRows()
    {
        _source.RaiseListChangedEvents = false;
        try
        {
            _source.DataSource = new SortableBindingList<T>(_allRows.Take(VisibleCount));
        }
        finally
        {
            _source.RaiseListChangedEvents = true;
        }

        _source.ResetBindings(metadataChanged: false);
        _isDirty = false;
        OnViewChanged();
    }

    private void OnViewChanged()
    {
        ViewChanged?.Invoke(this, EventArgs.Empty);
    }
}
