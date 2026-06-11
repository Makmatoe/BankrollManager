using BankrollManager.App.Controls;
using BankrollManager.Core.Models;

namespace BankrollManager.App;

public sealed partial class MainForm
{
    private void OpenDailyChartPoint(MiniChartPoint point)
    {
        if (point.Tag is not DateOnly date)
        {
            return;
        }

        SelectNavigationPage("Day");
        SelectGridRow<DailySummary>(_dailySource, _dailyGrid, summary => summary.Date == date);
        UpdateSelectedDayDetail(date);
        _statusLabel.Text = $"Showing day detail for {date:yyyy-MM-dd}.";
    }

    private void OpenMonthlyChartPoint(MiniChartPoint point)
    {
        if (point.Tag is not DateOnly month)
        {
            return;
        }

        SelectNavigationPage("Month");
        SelectGridRow<MonthlySummary>(_monthlySource, _monthlyGrid, summary => summary.Month == month);
        _statusLabel.Text = $"Showing month detail for {month:yyyy-MM}.";
    }

    private void OpenRunningChartPoint(MiniChartPoint point)
    {
        if (point.Tag is not RunningBankrollPoint runningPoint)
        {
            return;
        }

        SelectNavigationPage("Timeline");
        if (!SelectGridRow<AuditTimelineEntry>(
            _timelineSource,
            _timelineGrid,
            entry => entry.Date == runningPoint.Date
                && string.Equals(entry.Name, runningPoint.Label, StringComparison.OrdinalIgnoreCase)))
        {
            SelectGridRow<AuditTimelineEntry>(
                _timelineSource,
                _timelineGrid,
                entry => entry.Date == runningPoint.Date);
        }

        _statusLabel.Text = $"Showing timeline detail for {runningPoint.Date:yyyy-MM-dd}.";
    }

    private void OpenComparisonChartPoint(MiniChartPoint point)
    {
        if (string.Equals(point.Label, "Tournaments", StringComparison.OrdinalIgnoreCase))
        {
            SelectNavigationPage("MTTs");
            _statusLabel.Text = "Showing tournament detail.";
            return;
        }

        if (string.Equals(point.Label, "Cash", StringComparison.OrdinalIgnoreCase))
        {
            SelectNavigationPage("Cash");
            _statusLabel.Text = "Showing cash detail.";
        }
    }

    private void SelectNavigationPage(string title)
    {
        var index = _navigationPages
            .Select((page, pageIndex) => new { page.Title, Index = pageIndex })
            .FirstOrDefault(page => string.Equals(page.Title, title, StringComparison.OrdinalIgnoreCase))
            ?.Index;
        if (index is null || _navigationButtons.Count == 0)
        {
            return;
        }

        SelectNavigationPage(_contentHost, _navigationPages, _navigationButtons, index.Value);
    }

    private static bool SelectGridRow<T>(
        BindingSource source,
        DataGridView grid,
        Predicate<T> predicate)
        where T : class
    {
        for (var index = 0; index < source.Count; index++)
        {
            if (source[index] is not T item || !predicate(item))
            {
                continue;
            }

            source.Position = index;
            SelectGridRow(grid, index);
            return true;
        }

        return false;
    }

    private static void SelectGridRow(DataGridView grid, int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= grid.Rows.Count)
        {
            return;
        }

        grid.ClearSelection();
        var row = grid.Rows[rowIndex];
        row.Selected = true;

        if (FirstVisibleCell(row) is { } cell)
        {
            grid.CurrentCell = cell;
        }

        if (!row.Displayed)
        {
            grid.FirstDisplayedScrollingRowIndex = rowIndex;
        }
    }

    private static DataGridViewCell? FirstVisibleCell(DataGridViewRow row)
    {
        foreach (DataGridViewCell cell in row.Cells)
        {
            if (cell.Visible)
            {
                return cell;
            }
        }

        return null;
    }
}
