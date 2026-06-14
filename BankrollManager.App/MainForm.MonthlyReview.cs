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
    private Control BuildMonthlyReviewTab()
    {
        var root = BuildGridShell(out var controls);

        var monthLabel = Theme.Label("Month", Theme.BodyFont, Theme.Muted);
        monthLabel.Height = Theme.ControlHeight;
        monthLabel.TextAlign = ContentAlignment.MiddleLeft;
        monthLabel.Margin = new Padding(0, 8, 4, 4);
        controls.Controls.Add(monthLabel);

        _monthlyReviewMonth = BuildMonthPicker(DefaultMonthlyReviewMonth());
        _monthlyReviewMonth.ValueChanged += (_, _) => RefreshMonthlyReviewSources();
        controls.Controls.Add(_monthlyReviewMonth);

        AddGridButton(controls, "Refresh", RefreshMonthlyReviewSources);
        AddGridButton(controls, "Export Markdown", ExportMonthlyReviewMarkdown);

        _monthlyReviewStatusLabel = Theme.Label(string.Empty, Theme.BodyFont, Theme.Muted);
        _monthlyReviewStatusLabel.AutoSize = false;
        _monthlyReviewStatusLabel.Width = 560;
        _monthlyReviewStatusLabel.Height = Theme.ControlHeight;
        _monthlyReviewStatusLabel.TextAlign = ContentAlignment.MiddleLeft;
        _monthlyReviewStatusLabel.Margin = new Padding(10, 8, 4, 4);
        controls.Controls.Add(_monthlyReviewStatusLabel);

        _monthlyReviewMetricGrid = CreateMonthlyReviewMetricGrid();
        _monthlyReviewFormatGrid = CreateMonthlyReviewGroupGrid(_monthlyReviewFormatSource);
        _monthlyReviewCategoryGrid = CreateMonthlyReviewGroupGrid(_monthlyReviewCategorySource);
        _monthlyReviewPlatformGrid = CreateMonthlyReviewGroupGrid(_monthlyReviewPlatformSource);
        _monthlyReviewSpecialtyGrid = CreateMonthlyReviewGroupGrid(_monthlyReviewSpecialtySource);
        _monthlyReviewWinGrid = CreateMonthlyReviewEntryGrid(_monthlyReviewWinSource);
        _monthlyReviewLossGrid = CreateMonthlyReviewEntryGrid(_monthlyReviewLossSource);
        _monthlyReviewStopLossGrid = CreateMonthlyReviewEntryGrid(_monthlyReviewStopLossSource);
        _monthlyReviewRiskGrid = CreateMonthlyReviewEntryGrid(_monthlyReviewRiskSource);
        _monthlyReviewNoteGrid = CreateMonthlyReviewNoteGrid();

        root.Controls.Add(
            BuildSegmentedContent(
            [
                ("Summary", _monthlyReviewMetricGrid),
                ("Formats", _monthlyReviewFormatGrid),
                ("Categories", _monthlyReviewCategoryGrid),
                ("Platforms", _monthlyReviewPlatformGrid),
                ("Specialty", _monthlyReviewSpecialtyGrid),
                ("Wins", _monthlyReviewWinGrid),
                ("Losses", _monthlyReviewLossGrid),
                ("Stop-loss", _monthlyReviewStopLossGrid),
                ("Risk", _monthlyReviewRiskGrid),
                ("Notes", _monthlyReviewNoteGrid)
            ]),
            0,
            1);
        return root;
    }

    private DateOnly DefaultMonthlyReviewMonth()
    {
        return MonthlyReviewService.GetAvailableMonths(_data).FirstOrDefault() is { } month && month != default
            ? month
            : new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1);
    }

    private static DateTimePicker BuildMonthPicker(DateOnly month)
    {
        return new DateTimePicker
        {
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "yyyy-MM",
            ShowUpDown = true,
            Value = month.ToDateTime(TimeOnly.MinValue),
            CalendarMonthBackground = Theme.PanelAlt,
            CalendarForeColor = Theme.Text,
            Font = Theme.BodyFont,
            Width = 118,
            Height = Theme.ControlHeight,
            Margin = new Padding(0, 4, 8, 4)
        };
    }

    private DataGridView CreateMonthlyReviewMetricGrid()
    {
        var grid = CreateGrid(_monthlyReviewMetricSource);
        AddTextColumn(grid, "Metric", "Metric", 170);
        AddTextColumn(grid, "Value", "Value", 120);
        AddTextColumn(grid, "Notes", "Notes", 420);
        return grid;
    }

    private DataGridView CreateMonthlyReviewGroupGrid(BindingSource source)
    {
        var grid = CreateGrid(source);
        AddTextColumn(grid, "Name", "Name", 170);
        AddTextColumn(grid, "TournamentProfitLoss", "MTT Cash P/L", 130);
        AddTextColumn(grid, "CashProfitLoss", "Cash P/L", 105);
        AddTextColumn(grid, "TicketProfitLoss", "Ticket P/L", 105);
        AddTextColumn(grid, "TotalCashProfitLoss", "Total Cash P/L", 125);
        AddTextColumn(grid, "TotalValueProfitLoss", "Value P/L", 110);
        AddTextColumn(grid, "TotalCost", "Cost", 95);
        AddTextColumn(grid, "Count", "Count", 76);
        AddTextColumn(grid, "HoursPlayed", "Hours", 82);
        AddTextColumn(grid, "CashPerHour", "Cash / Hr", 105);
        AddTextColumn(grid, "ValuePerHour", "Value / Hr", 105);
        return grid;
    }

    private DataGridView CreateMonthlyReviewEntryGrid(BindingSource source)
    {
        var grid = CreateGrid(source);
        AddTextColumn(grid, "Date", "Date", 92);
        AddTextColumn(grid, "Time", "Time", 70);
        AddTextColumn(grid, "Kind", "Kind", 120);
        AddTextColumn(grid, "Name", "Name", 190);
        AddTextColumn(grid, "Platform", "Platform", 115);
        AddTextColumn(grid, "Format", "Format", 112);
        AddTextColumn(grid, "Category", "Category", 118);
        AddTextColumn(grid, "CashProfitLoss", "Cash P/L", 100);
        AddTextColumn(grid, "TicketProfitLoss", "Ticket P/L", 100);
        AddTextColumn(grid, "ValueProfitLoss", "Value P/L", 100);
        AddTextColumn(grid, "RiskPercentage", "Risk %", 82);
        AddTextColumn(grid, "Rule", "Rule", 100);
        AddTextColumn(grid, "Notes", "Notes", 260);
        return grid;
    }

    private DataGridView CreateMonthlyReviewNoteGrid()
    {
        var grid = CreateGrid(_monthlyReviewNoteSource);
        AddTextColumn(grid, "Date", "Date", 92);
        AddTextColumn(grid, "Kind", "Kind", 110);
        AddTextColumn(grid, "Name", "Name", 190);
        AddTextColumn(grid, "Area", "Area", 110);
        AddTextColumn(grid, "Text", "Text", 520);
        return grid;
    }

    private void RefreshMonthlyReviewSources()
    {
        if (_monthlyReviewMonth is null)
        {
            return;
        }

        var month = new DateOnly(_monthlyReviewMonth.Value.Year, _monthlyReviewMonth.Value.Month, 1);
        var report = MonthlyReviewService.GetReport(_data, month);
        ReplaceSource(_monthlyReviewMetricSource, report.Metrics);
        ReplaceSource(_monthlyReviewFormatSource, report.FormatResults);
        ReplaceSource(_monthlyReviewCategorySource, report.CategoryResults);
        ReplaceSource(_monthlyReviewPlatformSource, report.PlatformResults);
        ReplaceSource(_monthlyReviewSpecialtySource, report.SpecialtyResults);
        ReplaceSource(_monthlyReviewWinSource, report.BiggestWins);
        ReplaceSource(_monthlyReviewLossSource, report.BiggestLosses);
        ReplaceSource(_monthlyReviewStopLossSource, report.StopLossBreaches);
        ReplaceSource(_monthlyReviewRiskSource, report.RiskBreaches);
        ReplaceSource(_monthlyReviewNoteSource, report.Notes);

        _monthlyReviewStatusLabel.Text =
            $"{report.Month:yyyy-MM}  Cash {Money(report.Summary.TotalPokerProfitLoss)}  Value {Money(report.Summary.TotalValueProfitLoss)}  Hours {report.Summary.HoursPlayed:0.##}  Risk {report.RiskBreaches.Count}";
    }

    private void ExportMonthlyReviewMarkdown()
    {
        if (_monthlyReviewMonth is null)
        {
            return;
        }

        var month = new DateOnly(_monthlyReviewMonth.Value.Year, _monthlyReviewMonth.Value.Month, 1);
        using var dialog = new SaveFileDialog
        {
            Filter = "Markdown files (*.md)|*.md|Text files (*.txt)|*.txt|All files (*.*)|*.*",
            FileName = $"bankroll-monthly-review-{month:yyyy-MM}.md"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        ChatGptBankrollExporter.ExportMonthlyReviewToFile(_data, month, dialog.FileName, DateTime.Now);
        _statusLabel.Text = $"Monthly review exported: {dialog.FileName}";
    }
}
