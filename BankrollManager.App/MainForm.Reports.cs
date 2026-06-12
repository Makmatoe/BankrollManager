using System.ComponentModel;
using System.Globalization;
using System.Drawing.Drawing2D;
using BankrollManager.App.Controls;
using BankrollManager.App.Forms;
using BankrollManager.Core.Models;
using BankrollManager.Core.Persistence;
using BankrollManager.Core.Services;
using Microsoft.Win32;

namespace BankrollManager.App;

public sealed partial class MainForm
{

    private Control BuildTimelineTab()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 1,
            BackColor = Theme.Back,
            Padding = new Padding(8)
        };

        _timelineGrid = CreateGrid(_timelineSource);
        AddTextColumn(_timelineGrid, "Date", "Date", 100);
        AddTextColumn(_timelineGrid, "Time", "Time", 70);
        AddTextColumn(_timelineGrid, "Type", "Type", 105);
        AddTextColumn(_timelineGrid, "Name", "Name", 240);
        AddTextColumn(_timelineGrid, "CostRisk", "Cost/Risk", 105);
        AddTextColumn(_timelineGrid, "Result", "Result", 105);
        AddTextColumn(_timelineGrid, "BankrollBefore", "Cash BR Before", 125);
        AddTextColumn(_timelineGrid, "BankrollAfter", "Cash BR After", 125);
        AddTextColumn(_timelineGrid, "Rule", "Rule", 110);
        root.Controls.Add(_timelineGrid, 0, 0);
        return root;
    }

    private Control BuildLedgerTab()
    {
        var root = BuildGridShell(out var buttons);
        AddGridButton(buttons, "Add", AddLedger);
        AddGridButton(buttons, "Edit", EditLedger);
        AddGridButton(buttons, "Delete", DeleteLedger);

        _ledgerGrid = CreateGrid(_ledgerSource);
        _ledgerGrid.CellDoubleClick += (_, _) => EditLedger();
        AddTextColumn(_ledgerGrid, "Date", "Date", 92);
        AddTextColumn(_ledgerGrid, "Type", "Type", 110);
        AddTextColumn(_ledgerGrid, "Platform", "Platform", 120);
        AddTextColumn(_ledgerGrid, "Description", "Description", 260);
        AddTextColumn(_ledgerGrid, "Amount", "Amount", 90);
        AddTextColumn(_ledgerGrid, "Category", "Category", 120);
        AddTextColumn(_ledgerGrid, "BankrollBefore", "Cash BR Before", 125);
        AddTextColumn(_ledgerGrid, "BankrollAfter", "Cash BR After", 120);
        AddTextColumn(_ledgerGrid, "Notes", "Notes", 320);
        root.Controls.Add(BuildGridWithEmptyState(
            _ledgerGrid,
            out _ledgerEmptyState,
            "No ledger entries yet. Add a deposit, withdrawal, bonus, rakeback, or correction here."), 0, 1);
        return root;
    }

    private Control BuildDailyTab()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Theme.Back,
            Padding = new Padding(8)
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 64));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36));

        var review = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            BackColor = Theme.Back,
            Margin = new Padding(0)
        };
        review.RowStyles.Add(new RowStyle(SizeType.Absolute, 220));
        review.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.Controls.Add(review, 0, 0);

        _dailyReviewChart = new MiniChart { Dock = DockStyle.Fill, Margin = new Padding(6) };
        _dailyReviewChart.PointActivated += (_, e) => OpenDailyChartPoint(e.Point);
        review.Controls.Add(_dailyReviewChart, 0, 0);

        _dailyGrid = CreateGrid(_dailySource);
        _dailyGrid.SelectionChanged += (_, _) => UpdateSelectedDayFromGrid();
        AddTextColumn(_dailyGrid, "Date", "Date", 100);
        AddTextColumn(_dailyGrid, "TournamentProfitLoss", "MTT Cash P/L", 130);
        AddTextColumn(_dailyGrid, "CashProfitLoss", "Cash Session P/L", 135);
        AddTextColumn(_dailyGrid, "TicketProfitLoss", "Ticket P/L", 110);
        AddTextColumn(_dailyGrid, "TotalValueProfitLoss", "Value P/L", 110);
        AddTextColumn(_dailyGrid, "TotalProfitLoss", "Total Cash P/L", 125);
        AddTextColumn(_dailyGrid, "NumberOfSessions", "Sessions", 88);
        AddTextColumn(_dailyGrid, "HoursPlayed", "Hours", 82);
        AddTextColumn(_dailyGrid, "CashPerHour", "Cash / Hr", 105);
        AddTextColumn(_dailyGrid, "ValuePerHour", "Value / Hr", 105);
        AddTextColumn(_dailyGrid, "RunningMonthProfitLoss", "Running Month P/L", 150);
        AddTextColumn(_dailyGrid, "RunningLifetimeBankrollValue", "Running Value", 140);
        AddTextColumn(_dailyGrid, "RunningLifetimeBankroll", "Running Cash", 130);
        review.Controls.Add(_dailyGrid, 0, 1);

        root.Controls.Add(BuildSelectedDayPanel(), 1, 0);
        return root;
    }

    private Control BuildSelectedDayPanel()
    {
        var shell = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            BackColor = Theme.Panel,
            Padding = new Padding(10),
            Margin = new Padding(6)
        };
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 190));
        shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            BackColor = Theme.Panel,
            Margin = new Padding(0, 0, 0, 8)
        };
        header.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        header.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _selectedDayTitle = Theme.Label("Selected day", Theme.SubHeaderFont, Theme.Text);
        _selectedDayTitle.AutoSize = false;
        _selectedDayTitle.AutoEllipsis = true;
        _selectedDayTitle.Dock = DockStyle.Fill;
        _selectedDayTitle.Margin = new Padding(0);
        _selectedDayTitle.TextAlign = ContentAlignment.MiddleLeft;
        header.Controls.Add(_selectedDayTitle, 0, 0);

        _selectedDayMeta = Theme.Label("No day selected", Theme.SmallFont, Theme.Muted);
        _selectedDayMeta.AutoSize = false;
        _selectedDayMeta.AutoEllipsis = true;
        _selectedDayMeta.Dock = DockStyle.Fill;
        _selectedDayMeta.Margin = new Padding(0);
        _selectedDayMeta.TextAlign = ContentAlignment.TopLeft;
        header.Controls.Add(_selectedDayMeta, 0, 1);
        shell.Controls.Add(header, 0, 0);

        _selectedDayChart = new MiniChart
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 8),
            MinimumSize = new Size(180, 150)
        };
        _selectedDayChart.PointActivated += (_, e) => OpenSelectedDayChartPoint(e.Point);
        shell.Controls.Add(_selectedDayChart, 0, 1);

        _selectedDayTimelineGrid = CreateGrid(_selectedDayTimelineSource);
        _selectedDayTimelineGrid.SelectionChanged += (_, _) => UpdateSelectedDayChartSelectionFromTimeline();
        AddTextColumn(_selectedDayTimelineGrid, "Time", "Time", 68);
        AddTextColumn(_selectedDayTimelineGrid, "Type", "Type", 112);
        AddTextColumn(_selectedDayTimelineGrid, "Name", "Event", 190);
        AddTextColumn(_selectedDayTimelineGrid, "CostRisk", "Risk", 86);
        AddTextColumn(_selectedDayTimelineGrid, "CashChange", "Cash", 86);
        AddTextColumn(_selectedDayTimelineGrid, "TicketChange", "Tickets", 86);
        AddTextColumn(_selectedDayTimelineGrid, "ValueChange", "Value", 86);
        AddTextColumn(_selectedDayTimelineGrid, "BankrollValueAfter", "Value After", 108);
        AddTextColumn(_selectedDayTimelineGrid, "Rule", "Rule", 100);
        shell.Controls.Add(BuildGridWithEmptyState(
            _selectedDayTimelineGrid,
            out _selectedDayEmptyState,
            "No events for this day."), 0, 2);

        return shell;
    }

    private void UpdateSelectedDayFromGrid()
    {
        if (_syncingDailySelection || _selectedDayChart is null)
        {
            return;
        }

        if (Selected<DailySummary>(_dailySource) is not { } summary)
        {
            UpdateSelectedDayDetail(null);
            return;
        }

        UpdateSelectedDayDetail(summary.Date);
        _statusLabel.Text = $"Showing day detail for {summary.Date:yyyy-MM-dd}.";
    }

    private void RefreshSelectedDaySelection()
    {
        if (_selectedDayChart is null)
        {
            return;
        }

        DailySummary? selectedSummary = null;
        DailySummary? firstSummary = null;
        for (var index = 0; index < _dailySource.Count; index++)
        {
            if (_dailySource[index] is not DailySummary summary)
            {
                continue;
            }

            firstSummary ??= summary;
            if (_selectedDayDate == summary.Date)
            {
                selectedSummary = summary;
                break;
            }
        }

        selectedSummary ??= firstSummary;
        if (selectedSummary is null)
        {
            UpdateSelectedDayDetail(null);
            return;
        }

        _syncingDailySelection = true;
        try
        {
            SelectGridRow<DailySummary>(_dailySource, _dailyGrid, summary => summary.Date == selectedSummary.Date);
        }
        finally
        {
            _syncingDailySelection = false;
        }

        UpdateSelectedDayDetail(selectedSummary.Date);
    }

    private void UpdateSelectedDayDetail(DateOnly? date)
    {
        if (_selectedDayChart is null || _selectedDayTimelineGrid is null)
        {
            return;
        }

        _selectedDayDate = date;
        if (date is null)
        {
            _selectedDayTitle.Text = "Selected day";
            _selectedDayMeta.Text = "No day selected";
            _selectedDayTimelineSource.DataSource = new SortableBindingList<DayTimelineEntry>([]);
            _selectedDayChart.SetData("Intraday Value P&L", [], MiniChartKind.Line);
            _selectedDayEmptyState.Visible = true;
            return;
        }

        var selectedDate = date.Value;
        var rows = BankrollCalculator.GetDayTimeline(_data, selectedDate);
        _selectedDayTimelineSource.DataSource = new SortableBindingList<DayTimelineEntry>(rows);
        _selectedDayEmptyState.Visible = rows.Count == 0;

        var summary = FindDailySummary(selectedDate);
        _selectedDayTitle.Text = selectedDate.ToString("dddd, dd MMM yyyy", CultureInfo.CurrentCulture);
        _selectedDayMeta.Text = summary is null
            ? $"{rows.Count} event(s)"
            : $"Value {Money(summary.TotalValueProfitLoss)}   Cash {Money(summary.TotalProfitLoss)}   Tickets {Money(summary.TicketProfitLoss)}   Sessions {summary.NumberOfSessions}";

        UpdateSelectedDayChart(selectedDate, rows);
    }

    private DailySummary? FindDailySummary(DateOnly date)
    {
        for (var index = 0; index < _dailySource.Count; index++)
        {
            if (_dailySource[index] is DailySummary summary && summary.Date == date)
            {
                return summary;
            }
        }

        return null;
    }

    private void UpdateSelectedDayChart(DateOnly date, IReadOnlyList<DayTimelineEntry> rows)
    {
        var points = new List<MiniChartPoint>();
        if (rows.Count > 0)
        {
            points.Add(new MiniChartPoint(
                "Start",
                0m,
                null,
                $"Intraday Value P&L{Environment.NewLine}{date:yyyy-MM-dd}{Environment.NewLine}Start: {Money(0m)}"));
        }

        var runningValue = 0m;
        foreach (var row in rows)
        {
            runningValue += row.ValueChange;
            var label = row.Time is { } time
                ? time.ToString("HH:mm", CultureInfo.CurrentCulture)
                : row.Type;
            points.Add(new MiniChartPoint(
                label,
                runningValue,
                row,
                $"Intraday Value P&L{Environment.NewLine}{date:yyyy-MM-dd} {label}{Environment.NewLine}{row.Name}{Environment.NewLine}Value change: {Money(row.ValueChange)}{Environment.NewLine}Cash change: {Money(row.CashChange)}{Environment.NewLine}Ticket change: {Money(row.TicketChange)}{Environment.NewLine}Day total: {Money(runningValue)}{Environment.NewLine}Overall value after: {Money(row.BankrollValueAfter)}"));
        }

        _selectedDayChart.SetData("Intraday Value P&L", points, MiniChartKind.Line);
        UpdateSelectedDayChartSelectionFromTimeline();
    }

    private void OpenSelectedDayChartPoint(MiniChartPoint point)
    {
        if (point.Tag is not DayTimelineEntry timelineEntry)
        {
            _selectedDayTimelineGrid.ClearSelection();
            return;
        }

        if (!SelectGridRow<DayTimelineEntry>(
            _selectedDayTimelineSource,
            _selectedDayTimelineGrid,
            entry => ReferenceEquals(entry, timelineEntry)))
        {
            SelectGridRow<DayTimelineEntry>(
                _selectedDayTimelineSource,
                _selectedDayTimelineGrid,
                entry => entry == timelineEntry);
        }

        var timeText = timelineEntry.Time is { } time
            ? time.ToString("HH:mm", CultureInfo.CurrentCulture)
            : "day start";
        _statusLabel.Text = $"Showing {timelineEntry.Type} at {timeText}.";
    }

    private void UpdateSelectedDayChartSelectionFromTimeline()
    {
        if (_selectedDayChart is null)
        {
            return;
        }

        if (Selected<DayTimelineEntry>(_selectedDayTimelineSource) is not { } timelineEntry)
        {
            _selectedDayChart.ClearSelection();
            return;
        }

        _selectedDayChart.SelectPoint(point =>
            ReferenceEquals(point.Tag, timelineEntry)
            || point.Tag is DayTimelineEntry pointEntry && pointEntry == timelineEntry);
    }

    private Control BuildMonthlyTab()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 1, BackColor = Theme.Back, Padding = new Padding(8) };
        _monthlyGrid = CreateGrid(_monthlySource);
        AddTextColumn(_monthlyGrid, "Month", "Month", 100);
        AddTextColumn(_monthlyGrid, "Deposits", "Deposits", 100);
        AddTextColumn(_monthlyGrid, "Withdrawals", "Withdrawals", 110);
        AddTextColumn(_monthlyGrid, "TournamentProfitLoss", "MTT Cash P/L", 130);
        AddTextColumn(_monthlyGrid, "CashProfitLoss", "Cash Session P/L", 135);
        AddTextColumn(_monthlyGrid, "TicketProfitLoss", "Ticket P/L", 110);
        AddTextColumn(_monthlyGrid, "TotalValueProfitLoss", "Value P/L", 110);
        AddTextColumn(_monthlyGrid, "TotalPokerProfitLoss", "Total Cash P/L", 125);
        AddTextColumn(_monthlyGrid, "NumberOfTournaments", "Tournaments", 105);
        AddTextColumn(_monthlyGrid, "NumberOfCashSessions", "Cash Sessions", 115);
        AddTextColumn(_monthlyGrid, "HoursPlayed", "Hours", 82);
        AddTextColumn(_monthlyGrid, "CashPerHour", "Cash / Hr", 105);
        AddTextColumn(_monthlyGrid, "ValuePerHour", "Value / Hr", 105);
        AddTextColumn(_monthlyGrid, "AverageTournamentBuyIn", "Avg Buy-in", 110);
        AddTextColumn(_monthlyGrid, "BiggestWin", "Biggest Win", 110);
        AddTextColumn(_monthlyGrid, "BiggestLoss", "Biggest Loss", 110);
        AddTextColumn(_monthlyGrid, "StopLossBreaches", "Stop-loss Breaches", 150);
        AddTextColumn(_monthlyGrid, "Notes", "Notes", 240);
        root.Controls.Add(_monthlyGrid, 0, 0);
        return root;
    }

    private Control BuildYearlyTab()
    {
        _yearlyGrid = CreateComparisonGrid(_yearlySource, yearly: true);
        _platformGrid = CreatePlatformGrid(_platformSource);
        _formatGrid = CreateComparisonGrid(_formatSource);
        _categoryGrid = CreateComparisonGrid(_categorySource);

        return BuildSegmentedContent(
        [
            ("Yearly", _yearlyGrid),
            ("Platforms", _platformGrid),
            ("Formats", _formatGrid),
            ("Categories", _categoryGrid)
        ]);
    }

    private static Control BuildSegmentedContent(IReadOnlyList<(string Title, Control Content)> pages)
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            BackColor = Theme.Back,
            Padding = new Padding(12)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var navigation = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Theme.Back,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(0, 0, 0, 10),
            Margin = new Padding(0)
        };
        root.Controls.Add(navigation, 0, 0);

        var contentHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Back,
            Padding = new Padding(0)
        };
        root.Controls.Add(contentHost, 0, 1);

        var buttons = new List<Label>();
        for (var index = 0; index < pages.Count; index++)
        {
            var pageIndex = index;
            var button = BuildSegmentButton(pages[index].Title);
            button.Click += (_, _) => SelectSegmentPage(contentHost, pages, buttons, pageIndex);
            navigation.Controls.Add(button);
            buttons.Add(button);
        }

        SelectSegmentPage(contentHost, pages, buttons, 0);
        return root;
    }

    private static Label BuildSegmentButton(string title)
    {
        var measuredWidth = TextRenderer.MeasureText(title, Theme.BodyFont).Width + 30;
        var button = new Label
        {
            Text = title,
            AutoSize = false,
            AutoEllipsis = true,
            Width = Math.Max(96, measuredWidth),
            Height = 38,
            BackColor = Theme.Panel,
            Cursor = Cursors.Hand,
            Font = Theme.BodyFont,
            ForeColor = Theme.Muted,
            Margin = new Padding(0, 0, 8, 0),
            Padding = new Padding(10, 0, 10, 1),
            TextAlign = ContentAlignment.MiddleCenter,
            UseMnemonic = false
        };

        button.Paint += (_, e) =>
        {
            using var border = new Pen(Theme.Border);
            e.Graphics.DrawRectangle(border, 0, 0, button.Width - 1, button.Height - 1);

            if (button.Tag is not true)
            {
                return;
            }

            using var accent = new SolidBrush(Theme.Accent);
            e.Graphics.FillRectangle(accent, 8, button.Height - 4, Math.Max(8, button.Width - 16), 3);
        };
        button.MouseEnter += (_, _) =>
        {
            if (button.Tag is not true)
            {
                button.BackColor = Theme.PanelAlt;
                button.ForeColor = Theme.Text;
            }
        };
        button.MouseLeave += (_, _) =>
        {
            if (button.Tag is not true)
            {
                button.BackColor = Theme.Panel;
                button.ForeColor = Theme.Muted;
            }
        };

        return button;
    }

    private static void SelectSegmentPage(
        Panel contentHost,
        IReadOnlyList<(string Title, Control Content)> pages,
        IReadOnlyList<Label> buttons,
        int selectedIndex)
    {
        contentHost.SuspendLayout();
        contentHost.Controls.Clear();
        var content = pages[selectedIndex].Content;
        content.Dock = DockStyle.Fill;
        contentHost.Controls.Add(content);
        contentHost.ResumeLayout();

        for (var index = 0; index < buttons.Count; index++)
        {
            var selected = index == selectedIndex;
            buttons[index].Tag = selected;
            buttons[index].BackColor = selected ? Theme.PanelAlt : Theme.Panel;
            buttons[index].ForeColor = selected ? Theme.Text : Theme.Muted;
            buttons[index].Invalidate();
        }
    }


    private void AddLedger()
    {
        var entry = new LedgerEntry
        {
            Date = DateOnly.FromDateTime(DateTime.Today),
            Platform = _data.Settings.DefaultPlatform
        };

        using var dialog = new LedgerEntryDialog(entry, _data.Settings);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _data.LedgerEntries.Add(dialog.Entry);
        SaveData("Ledger entry added.");
    }

    private void EditLedger()
    {
        if (Selected<LedgerEntry>(_ledgerSource) is not { } selected)
        {
            return;
        }

        using var dialog = new LedgerEntryDialog(selected, _data.Settings);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        CopyLedger(dialog.Entry, _data.LedgerEntries.First(entry => entry.Id == selected.Id));
        SaveData("Ledger entry updated.");
    }

    private void DeleteLedger()
    {
        if (Selected<LedgerEntry>(_ledgerSource) is not { } selected || !ConfirmDelete("ledger entry"))
        {
            return;
        }

        _data.LedgerEntries.RemoveAll(entry => entry.Id == selected.Id);
        SaveData("Ledger entry deleted.");
    }

    private static void CopyLedger(LedgerEntry source, LedgerEntry target)
    {
        target.Date = source.Date;
        target.Type = source.Type;
        target.Platform = source.Platform;
        target.Description = source.Description;
        target.Amount = source.Amount;
        target.Category = source.Category;
        target.BankrollBefore = source.BankrollBefore;
        target.BankrollAfter = source.BankrollAfter;
        target.Notes = source.Notes;
    }
}
