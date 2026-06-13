using System.ComponentModel;
using System.Globalization;
using System.Drawing.Drawing2D;
using BankrollManager.App.Controls;
using BankrollManager.App.Forms;
using BankrollManager.Core.Formatting;
using BankrollManager.Core.Models;
using BankrollManager.Core.Persistence;
using BankrollManager.Core.Services;
using Microsoft.Win32;

namespace BankrollManager.App;

public sealed partial class MainForm
{

    private const int GridFitSampleRows = 80;
    private const int GridFitPadding = 24;

    private DataGridView CreateGrid(BindingSource source, bool readOnly = true, IGridLoadController? loadController = null)
    {
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false,
            ReadOnly = readOnly,
            DataSource = source,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None,
            ShowCellToolTips = true,
            Tag = loadController
        };
        Theme.ApplyGrid(grid);
        grid.CellFormatting += GridCellFormatting;
        grid.CellParsing += GridCellParsing;
        grid.ColumnHeaderMouseClick += GridColumnHeaderMouseClick;
        grid.DataBindingComplete += (_, _) => FitGridColumns(grid);
        grid.SizeChanged += (_, _) => FitGridColumns(grid);
        return grid;
    }

    private DataGridView CreateComparisonGrid(BindingSource source, bool yearly = false)
    {
        var grid = CreateGrid(source);
        if (yearly)
        {
            AddTextColumn(grid, "Year", "Year", 80);
            AddTextColumn(grid, "Deposits", "Deposits", 100);
            AddTextColumn(grid, "Withdrawals", "Withdrawals", 110);
            AddTextColumn(grid, "TournamentProfitLoss", "MTT Cash P/L", 130);
            AddTextColumn(grid, "CashProfitLoss", "Cash Session P/L", 135);
            AddTextColumn(grid, "TicketProfitLoss", "Ticket P/L", 110);
            AddTextColumn(grid, "TotalValueProfitLoss", "Value P/L", 110);
            AddTextColumn(grid, "TotalPokerProfitLoss", "Total Cash P/L", 125);
            AddTextColumn(grid, "NumberOfTournaments", "Tournaments", 105);
            AddTextColumn(grid, "NumberOfCashSessions", "Cash Sessions", 115);
            AddTextColumn(grid, "HoursPlayed", "Hours", 82);
            AddTextColumn(grid, "CashPerHour", "Cash / Hr", 105);
            AddTextColumn(grid, "ValuePerHour", "Value / Hr", 105);
            AddTextColumn(grid, "AverageTournamentBuyIn", "Avg Buy-in", 110);
            AddTextColumn(grid, "BiggestWin", "Biggest Win", 110);
            AddTextColumn(grid, "BiggestLoss", "Biggest Loss", 110);
            AddTextColumn(grid, "StopLossBreaches", "Stop-loss Breaches", 150);
        }
        else
        {
            AddTextColumn(grid, "Name", "Name", 160);
            AddTextColumn(grid, "TournamentProfitLoss", "Tournament P/L", 130);
            AddTextColumn(grid, "CashProfitLoss", "Cash P/L", 110);
            AddTextColumn(grid, "TotalPokerProfitLoss", "Total Cash P/L", 125);
            AddTextColumn(grid, "TotalCost", "Cash Cost", 110);
            AddTextColumn(grid, "Count", "Count", 80);
        }

        return grid;
    }

    private DataGridView CreatePlatformGrid(BindingSource source)
    {
        var grid = CreateGrid(source);
        AddTextColumn(grid, "Name", "Platform", 160);
        AddTextColumn(grid, "WalletCashBalance", "Wallet Cash", 115);
        AddTextColumn(grid, "ActiveTableCash", "On Tables", 105);
        AddTextColumn(grid, "TotalPlatformExposure", "Total Exposure", 125);
        AddTextColumn(grid, "TotalPlatformValue", "Total Value", 115);
        AddTextColumn(grid, "Deposits", "Deposits", 100);
        AddTextColumn(grid, "Withdrawals", "Withdrawals", 110);
        AddTextColumn(grid, "LedgerNet", "Ledger Net", 105);
        AddTextColumn(grid, "TournamentProfitLoss", "Tournament P/L", 130);
        AddTextColumn(grid, "CashSessionProfitLoss", "Cash Session P/L", 135);
        AddTextColumn(grid, "TotalPokerProfitLoss", "Total Cash P/L", 125);
        AddTextColumn(grid, "TicketBalance", "Tickets", 90);
        AddTextColumn(grid, "CashCost", "Cash Cost", 110);
        AddTextColumn(grid, "Count", "Entries", 80);
        return grid;
    }

    private DataGridView CreateWalletGrid(BindingSource source)
    {
        var grid = CreateGrid(source);
        AddTextColumn(grid, "Name", "Platform", 160);
        AddTextColumn(grid, "WalletCashBalance", "Expected Wallet", 125);
        AddTextColumn(grid, "ActiveTableCash", "On Tables", 105);
        AddTextColumn(grid, "TotalPlatformExposure", "Total Exposure", 125);
        AddTextColumn(grid, "TotalPlatformValue", "Total Value", 115);
        AddTextColumn(grid, "ActualCashBalance", "Actual Cash", 115);
        AddTextColumn(grid, "Difference", "Difference", 105);
        AddTextColumn(grid, "AcceptedCashDifference", "Accepted Diff", 115);
        AddTextColumn(grid, "TicketBalance", "Tickets", 90);
        AddTextColumn(grid, "Deposits", "Deposits", 100);
        AddTextColumn(grid, "Withdrawals", "Withdrawals", 110);
        AddTextColumn(grid, "TournamentProfitLoss", "MTT P/L", 105);
        AddTextColumn(grid, "CashSessionProfitLoss", "Cash P/L", 105);
        AddTextColumn(grid, "LastUpdatedDate", "Updated", 95);
        AddTextColumn(grid, "Notes", "Notes", 260);
        return grid;
    }

    private void GridCellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (sender is not DataGridView grid || e.Value is null || e.ColumnIndex < 0)
        {
            return;
        }

        var property = grid.Columns[e.ColumnIndex].DataPropertyName;
        if ((property == "RuleCheckResult" || property == "Rule") && e.Value is string ruleText && e.CellStyle is not null)
        {
            e.CellStyle.ForeColor = RuleColor(ruleText);
            e.CellStyle.BackColor = RuleBackColor(ruleText);
            e.CellStyle.SelectionForeColor = RuleColor(ruleText);
        }
        else if (property == "Severity" && e.Value is AttentionSeverity severity && e.CellStyle is not null)
        {
            e.CellStyle.ForeColor = severity switch
            {
                AttentionSeverity.High => Theme.Negative,
                AttentionSeverity.Check => Theme.Warning,
                AttentionSeverity.Clear => Theme.Positive,
                _ => Theme.Muted
            };
            e.CellStyle.SelectionForeColor = e.CellStyle.ForeColor;
        }

        if (e.Value is decimal decimalValue)
        {
            if (property.Contains("RiskPercentage", StringComparison.OrdinalIgnoreCase) || property == "MaxRiskPercent" || property == "MonthlyBudgetPercent")
            {
                e.Value = $"{decimalValue:0.0}%";
                e.FormattingApplied = true;
                if (property.Contains("RiskPercentage", StringComparison.OrdinalIgnoreCase) && e.CellStyle is not null)
                {
                    e.CellStyle.ForeColor = decimalValue >= 10m
                        ? Theme.Negative
                        : decimalValue >= 6m
                            ? Theme.Warning
                            : Theme.Muted;
                }
            }
            else if (property.EndsWith("ROI", StringComparison.OrdinalIgnoreCase))
            {
                e.Value = $"{decimalValue:P1}";
                e.FormattingApplied = true;
            }
            else if (property == "HoursPlayed")
            {
                e.Value = $"{decimalValue:0.##}";
                e.FormattingApplied = true;
            }
            else if (property.Contains("BB", StringComparison.OrdinalIgnoreCase) && property != "BigBlindAmount")
            {
                e.Value = $"{decimalValue:0.##}";
                e.FormattingApplied = true;
            }
            else
            {
                e.Value = Money(decimalValue);
                e.FormattingApplied = true;
            }

            if (property.Contains("Profit", StringComparison.OrdinalIgnoreCase)
                || property.Contains("Loss", StringComparison.OrdinalIgnoreCase)
                || property.Contains("Net", StringComparison.OrdinalIgnoreCase)
                || property.Contains("Change", StringComparison.OrdinalIgnoreCase)
                || property.Contains("Biggest", StringComparison.OrdinalIgnoreCase)
                || property.EndsWith("PerHour", StringComparison.OrdinalIgnoreCase)
                || property.Contains("ROI", StringComparison.OrdinalIgnoreCase)
                || property == "Amount"
                || property == "WalletCashBalance"
                || property == "ActiveTableCash"
                || property == "TotalPlatformExposure"
                || property == "TotalPlatformValue"
                || property == "WalletCashImpact"
                || property == "TicketBalance"
                || property == "Difference"
                || property == "AcceptedCashDifference")
            {
                if (e.CellStyle is not null)
                {
                    e.CellStyle.ForeColor = decimalValue >= 0m ? Theme.Positive : Theme.Negative;
                }
            }
        }
        else if (e.Value is TimeOnly time)
        {
            e.Value = BankrollDateFormatter.FormatTime(time, CultureInfo.CurrentCulture);
            e.FormattingApplied = true;
        }
        else if (e.Value is DateOnly date)
        {
            e.Value = BankrollDateFormatter.FormatDate(date, CultureInfo.CurrentCulture);
            e.FormattingApplied = true;
        }
    }

    private void GridCellParsing(object? sender, DataGridViewCellParsingEventArgs e)
    {
        if (sender is not DataGridView grid || e.Value is not string text || e.ColumnIndex < 0)
        {
            return;
        }

        var property = grid.Columns[e.ColumnIndex].DataPropertyName;
        if (property is "MaxRiskPercent" or "MonthlyBudgetPercent" or "DefaultBuyInCap" or "MinBankroll")
        {
            if (TryParseDecimal(CleanNumber(text), out var decimalValue))
            {
                e.Value = decimalValue;
                e.ParsingApplied = true;
            }
        }

        if (property is "BulletCap" or "DailyEntryCap" or "CooldownDays"
            && int.TryParse(CleanNumber(text), out var intValue))
        {
            e.Value = intValue;
            e.ParsingApplied = true;
        }
    }

    private void GridColumnHeaderMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
    {
        if (sender is not DataGridView grid || e.ColumnIndex < 0)
        {
            return;
        }

        var column = grid.Columns[e.ColumnIndex];
        if (string.IsNullOrWhiteSpace(column.DataPropertyName))
        {
            return;
        }

        grid.EndEdit();
        if (grid.DataSource is BindingSource source)
        {
            source.EndEdit();
        }

        var sortOrder = column.HeaderCell.SortGlyphDirection == SortOrder.Descending
            ? SortOrder.Ascending
            : SortOrder.Descending;
        var direction = sortOrder == SortOrder.Ascending
            ? ListSortDirection.Ascending
            : ListSortDirection.Descending;

        if (grid.Tag is IGridLoadController loadController
            && loadController.TryApplySort(column.DataPropertyName, direction))
        {
            ApplySortGlyph(grid, column, sortOrder);
            FitGridColumns(grid);
            return;
        }

        grid.Sort(column, direction);
        ApplySortGlyph(grid, column, sortOrder);
        FitGridColumns(grid);
    }

    private static void ApplySortGlyph(DataGridView grid, DataGridViewColumn sortedColumn, SortOrder sortOrder)
    {
        foreach (DataGridViewColumn gridColumn in grid.Columns)
        {
            gridColumn.HeaderCell.SortGlyphDirection = SortOrder.None;
        }

        sortedColumn.HeaderCell.SortGlyphDirection = sortOrder;
    }

    private void FitGridColumns(DataGridView grid)
    {
        if (grid.IsDisposed || grid.Columns.Count == 0 || grid.ClientSize.Width <= 0)
        {
            return;
        }

        var columns = grid.Columns
            .Cast<DataGridViewColumn>()
            .Where(column => column.Visible)
            .ToList();
        if (columns.Count == 0)
        {
            return;
        }

        var rows = grid.Rows
            .Cast<DataGridViewRow>()
            .Where(row => !row.IsNewRow)
            .Take(GridFitSampleRows)
            .ToList();
        var widths = new Dictionary<DataGridViewColumn, int>();

        grid.SuspendLayout();
        try
        {
            foreach (var column in columns)
            {
                var layout = GetColumnLayout(column);
                var width = MeasureGridText(column.HeaderText, grid.ColumnHeadersDefaultCellStyle.Font ?? grid.Font);
                foreach (var row in rows)
                {
                    var text = TrimGridMeasureText(Convert.ToString(row.Cells[column.Index].FormattedValue, CultureInfo.CurrentCulture));
                    width = Math.Max(width, MeasureGridText(text, grid.Font));
                }

                widths[column] = Math.Clamp(width, layout.MinimumWidth, layout.MaximumWidth);
            }

            var availableWidth = grid.ClientSize.Width
                - grid.RowHeadersWidth
                - SystemInformation.VerticalScrollBarWidth
                - 8;
            if (availableWidth > 0)
            {
                FitWidthsToAvailableSpace(columns, widths, availableWidth);
            }

            foreach (var column in columns)
            {
                column.Width = Math.Max(column.MinimumWidth, widths[column]);
            }
        }
        finally
        {
            grid.ResumeLayout();
        }
    }

    private static void FitWidthsToAvailableSpace(
        IReadOnlyList<DataGridViewColumn> columns,
        Dictionary<DataGridViewColumn, int> widths,
        int availableWidth)
    {
        var totalWidth = widths.Values.Sum();
        if (totalWidth > availableWidth)
        {
            var overflow = totalWidth - availableWidth;
            var shrinkColumns = columns
                .OrderByDescending(column => GetColumnLayout(column).Flexible)
                .ThenByDescending(column => widths[column])
                .ToList();

            foreach (var column in shrinkColumns)
            {
                var layout = GetColumnLayout(column);
                var shrink = Math.Min(overflow, widths[column] - layout.MinimumWidth);
                if (shrink <= 0)
                {
                    continue;
                }

                widths[column] -= shrink;
                overflow -= shrink;
                if (overflow <= 0)
                {
                    break;
                }
            }

            return;
        }

        var remaining = availableWidth - totalWidth;
        while (remaining > 0)
        {
            var flexibleColumns = columns
                .Where(column => GetColumnLayout(column).Flexible && widths[column] < GetColumnLayout(column).MaximumWidth)
                .ToList();
            if (flexibleColumns.Count == 0)
            {
                break;
            }

            var share = Math.Max(1, remaining / flexibleColumns.Count);
            foreach (var column in flexibleColumns)
            {
                var layout = GetColumnLayout(column);
                var grow = Math.Min(share, layout.MaximumWidth - widths[column]);
                widths[column] += grow;
                remaining -= grow;
                if (remaining <= 0)
                {
                    break;
                }
            }
        }
    }

    private static int MeasureGridText(string? text, Font font)
    {
        if (string.IsNullOrEmpty(text))
        {
            return GridFitPadding;
        }

        return TextRenderer.MeasureText(text, font).Width + GridFitPadding;
    }

    private static string TrimGridMeasureText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        return text.Length > 90 ? text[..90] : text;
    }

    private static GridColumnLayout GetColumnLayout(DataGridViewColumn column)
    {
        return column.Tag as GridColumnLayout
            ?? new GridColumnLayout(column.MinimumWidth, column.MinimumWidth, Math.Max(column.Width, column.MinimumWidth), Flexible: false);
    }

    private static void AddTextColumn(DataGridView grid, string property, string header, int width, bool readOnly = true)
    {
        var layout = BuildColumnLayout(property, width);
        var column = new DataGridViewTextBoxColumn
        {
            DataPropertyName = property,
            HeaderText = header,
            MinimumWidth = layout.MinimumWidth,
            Width = width,
            ReadOnly = readOnly,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
            SortMode = DataGridViewColumnSortMode.Programmatic,
            Tag = layout
        };

        if (IsRightAlignedColumn(property))
        {
            column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            column.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
        }

        grid.Columns.Add(column);
    }

    private static void AddCheckColumn(DataGridView grid, string property, string header, int width)
    {
        var layout = new GridColumnLayout(width, Math.Min(width, 48), Math.Max(width, 64), Flexible: false);
        grid.Columns.Add(new DataGridViewCheckBoxColumn
        {
            DataPropertyName = property,
            HeaderText = header,
            MinimumWidth = layout.MinimumWidth,
            Width = width,
            ReadOnly = true,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
            SortMode = DataGridViewColumnSortMode.Programmatic,
            Tag = layout
        });
    }

    private static GridColumnLayout BuildColumnLayout(string property, int preferredWidth)
    {
        var flexible = IsFlexibleColumn(property);
        var minimumWidth = property switch
        {
            "Notes" or "Description" or "MistakeLesson" or "PreGameFocus" => 120,
            "EventName" or "Name" or "Summary" => 130,
            "Action" => 96,
            "Rule" or "RuleCheckResult" => 82,
            "Date" or "FinishedDate" or "ClosedDate" or "LastUpdatedDate" or "Month" => 82,
            "Time" or "RegistrationTime" or "FinishedTime" or "ClosedTime" or "SessionTime" => 62,
            "Status" or "Type" or "Platform" or "Category" or "Format" => 78,
            _ when IsRightAlignedColumn(property) => Math.Min(preferredWidth, 68),
            _ => Math.Min(preferredWidth, 76)
        };
        var maximumWidth = property switch
        {
            "Notes" => 360,
            "Description" or "MistakeLesson" or "PreGameFocus" => 320,
            "EventName" or "Name" or "Summary" => 360,
            "Action" => 240,
            "Tags" => 220,
            "Rule" or "RuleCheckResult" => 180,
            _ when flexible => Math.Max(preferredWidth + 120, 220),
            _ when IsRightAlignedColumn(property) => Math.Max(preferredWidth, 140),
            _ => Math.Max(preferredWidth, 180)
        };

        return new GridColumnLayout(preferredWidth, minimumWidth, Math.Max(maximumWidth, minimumWidth), flexible);
    }

    private static bool IsFlexibleColumn(string property)
    {
        return property is "Notes"
            or "Description"
            or "MistakeLesson"
            or "PreGameFocus"
            or "Tags"
            or "EventName"
            or "Name"
            or "Summary"
            or "Action"
            or "Rule"
            or "RuleCheckResult";
    }

    private sealed record GridColumnLayout(int PreferredWidth, int MinimumWidth, int MaximumWidth, bool Flexible);

    private static bool IsRightAlignedColumn(string property)
    {
        return property.Contains("Profit", StringComparison.OrdinalIgnoreCase)
            || property.Contains("Loss", StringComparison.OrdinalIgnoreCase)
            || property.Contains("Amount", StringComparison.OrdinalIgnoreCase)
            || property == "Result"
            || property.Contains("BuyIn", StringComparison.OrdinalIgnoreCase)
            || property.Contains("Buy-in", StringComparison.OrdinalIgnoreCase)
            || property.Contains("Cashout", StringComparison.OrdinalIgnoreCase)
            || property.Contains("Cash", StringComparison.OrdinalIgnoreCase)
            || property.Contains("Cost", StringComparison.OrdinalIgnoreCase)
            || property.Contains("ROI", StringComparison.OrdinalIgnoreCase)
            || property.Contains("Risk", StringComparison.OrdinalIgnoreCase)
            || property.Contains("Bankroll", StringComparison.OrdinalIgnoreCase)
            || property.Contains("Value", StringComparison.OrdinalIgnoreCase)
            || property.Contains("Deposits", StringComparison.OrdinalIgnoreCase)
            || property.Contains("Withdrawals", StringComparison.OrdinalIgnoreCase)
            || property.Contains("Ledger", StringComparison.OrdinalIgnoreCase)
            || property.Contains("Ticket", StringComparison.OrdinalIgnoreCase)
            || property == "Difference"
            || property.Contains("Exposure", StringComparison.OrdinalIgnoreCase)
            || property.Contains("BigBlind", StringComparison.OrdinalIgnoreCase)
            || property.Contains("BB", StringComparison.OrdinalIgnoreCase)
            || property.Contains("Reloads", StringComparison.OrdinalIgnoreCase)
            || property.Contains("Prize", StringComparison.OrdinalIgnoreCase)
            || property.Contains("Bounty", StringComparison.OrdinalIgnoreCase)
            || property.Contains("AddOns", StringComparison.OrdinalIgnoreCase)
            || property.Contains("Average", StringComparison.OrdinalIgnoreCase)
            || property.Contains("Biggest", StringComparison.OrdinalIgnoreCase)
            || property is "Hands" or "Minutes" or "HoursPlayed" or "Count" or "Year" or "Placement" or "FieldSize" or "DailyEntryCap" or "CooldownDays"
            || property.Contains("Bullets", StringComparison.OrdinalIgnoreCase);
    }
}
