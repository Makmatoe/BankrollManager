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

    private DataGridView CreateGrid(BindingSource source, bool readOnly = true)
    {
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false,
            ReadOnly = readOnly,
            DataSource = source,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells
        };
        Theme.ApplyGrid(grid);
        grid.CellFormatting += GridCellFormatting;
        grid.CellParsing += GridCellParsing;
        grid.ColumnHeaderMouseClick += GridColumnHeaderMouseClick;
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
                || property == "Difference")
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

    private static void GridColumnHeaderMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
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

        grid.Sort(column, direction);
        foreach (DataGridViewColumn gridColumn in grid.Columns)
        {
            gridColumn.HeaderCell.SortGlyphDirection = SortOrder.None;
        }

        column.HeaderCell.SortGlyphDirection = sortOrder;
    }

    private static void AddTextColumn(DataGridView grid, string property, string header, int width, bool readOnly = true)
    {
        var column = new DataGridViewTextBoxColumn
        {
            DataPropertyName = property,
            HeaderText = header,
            MinimumWidth = width,
            ReadOnly = readOnly,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
            SortMode = DataGridViewColumnSortMode.Programmatic
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
        grid.Columns.Add(new DataGridViewCheckBoxColumn
        {
            DataPropertyName = property,
            HeaderText = header,
            MinimumWidth = width,
            ReadOnly = true,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
            SortMode = DataGridViewColumnSortMode.Programmatic
        });
    }

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
