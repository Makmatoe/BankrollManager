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

    private void RefreshAll()
    {
        _data.EnsureDefaults();
        if (_data.Settings.NormalizePlayLocks(DateOnly.FromDateTime(DateTime.Today)))
        {
            _repository.Save(_data);
        }

        BankrollCalculator.RecalculateTrackingFields(_data);
        RefreshDataSources();
        RefreshDashboard();
        RefreshEmptyStates();
        LoadSettingsControls();
        RefreshDecisionPlatformChoices(includeCurrent: true);
        RefreshDecision();
    }

    private void RefreshDataSources()
    {
        var auditTimeline = BankrollCalculator.GetAuditTimeline(_data);
        var platformSummaries = BankrollCalculator.GetPlatformSummaries(_data).OrderBy(summary => summary.Name).ToList();
        _overviewAttentionSource.DataSource = new SortableBindingList<AttentionItem>(
            NeedsAttentionService.GetItems(_data, DateOnly.FromDateTime(DateTime.Today), platformSummaries));
        _tournamentSource.DataSource = new SortableBindingList<TournamentEntry>(_data.TournamentEntries
            .OrderByDescending(entry => entry.Date)
            .ThenByDescending(entry => entry.RegistrationTime ?? TimeOnly.MinValue)
            .ToList());
        _overviewOpenTournamentSource.DataSource = new SortableBindingList<TournamentEntry>(_data.TournamentEntries
            .Where(entry => entry.Status != TournamentStatus.Finished)
            .OrderBy(entry => entry.Date)
            .ThenBy(entry => entry.RegistrationTime ?? TimeOnly.MinValue)
            .Take(12)
            .ToList());
        _overviewRecentActivitySource.DataSource = new SortableBindingList<AuditTimelineEntry>(auditTimeline
            .OrderByDescending(entry => entry.Date)
            .ThenByDescending(entry => entry.Time ?? TimeOnly.MinValue)
            .Take(12)
            .ToList());
        _cashSource.DataSource = new SortableBindingList<CashSession>(_data.CashSessions
            .OrderByDescending(entry => entry.Date)
            .ThenByDescending(entry => entry.SessionTime ?? TimeOnly.MinValue)
            .ToList());
        _ledgerSource.DataSource = new SortableBindingList<LedgerEntry>(_data.LedgerEntries.OrderByDescending(entry => entry.Date).ToList());
        _timelineSource.DataSource = new SortableBindingList<AuditTimelineEntry>(auditTimeline);
        _dailySource.DataSource = new SortableBindingList<DailySummary>(BankrollCalculator.GetDailySummaries(_data).OrderByDescending(summary => summary.Date).ToList());
        _monthlySource.DataSource = new SortableBindingList<MonthlySummary>(BankrollCalculator.GetMonthlySummaries(_data).OrderByDescending(summary => summary.Month).ToList());
        _yearlySource.DataSource = new SortableBindingList<YearlySummary>(BankrollCalculator.GetYearlySummaries(_data).OrderByDescending(summary => summary.Year).ToList());
        _platformSource.DataSource = new SortableBindingList<PlatformSummary>(platformSummaries);
        _walletSource.DataSource = new SortableBindingList<PlatformSummary>(platformSummaries);
        _formatSource.DataSource = new SortableBindingList<ComparisonSummary>(BankrollCalculator.GetFormatComparison(_data).OrderBy(summary => summary.Name).ToList());
        _categorySource.DataSource = new SortableBindingList<ComparisonSummary>(BankrollCalculator.GetCategoryComparison(_data).OrderBy(summary => summary.Name).ToList());
        _categoryRulesSource.DataSource = new SortableBindingList<CategoryRuleSettings>(_data.Settings.CategoryRules
            .OrderBy(rule => rule.Category.ToString(), NaturalSortComparer.Instance)
            .ToList());
        UpdateTournamentInspector();
        UpdateCashInspector();
    }

    private void OpenSelectedAttentionItem()
    {
        if (Selected<AttentionItem>(_overviewAttentionSource) is not { } item)
        {
            return;
        }

        switch (item.TargetType)
        {
            case AttentionTargetType.Cash:
                if (item.TargetId is { } cashId
                    && _data.CashSessions.FirstOrDefault(entry => entry.Id == cashId) is { } cashSession)
                {
                    if (cashSession.IsActive)
                    {
                        CloseCashSession(cashSession);
                    }
                    else
                    {
                        EditCashSession(cashSession);
                    }
                }
                break;
            case AttentionTargetType.Tournament:
                if (item.TargetId is { } tournamentId
                    && _data.TournamentEntries.FirstOrDefault(entry => entry.Id == tournamentId) is { } tournament)
                {
                    EditTournamentEntry(tournament);
                }
                break;
            case AttentionTargetType.Wallet:
                if (Enum.TryParse<Platform>(item.TargetKey, out var platform))
                {
                    ReconcileWallet(platform);
                }
                break;
            case AttentionTargetType.Settings:
                MessageBox.Show(
                    item.Summary,
                    "Needs attention",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                break;
            default:
                _statusLabel.Text = item.Summary;
                break;
        }
    }


    private void SaveData(string message)
    {
        _repository.Save(_data);
        RefreshAll();
        _statusLabel.Text = $"{message}  File: {_repository.FilePath}";
    }

    private void RefreshEmptyStates()
    {
        if (_tournamentEmptyState is not null)
        {
            _tournamentEmptyState.Visible = _data.TournamentEntries.Count == 0;
        }

        if (_cashEmptyState is not null)
        {
            _cashEmptyState.Visible = _data.CashSessions.Count == 0;
        }

        if (_ledgerEmptyState is not null)
        {
            _ledgerEmptyState.Visible = _data.LedgerEntries.Count == 0;
        }
    }

    private void BackupData()
    {
        var path = _repository.CreateTimestampedBackup(_data);
        _statusLabel.Text = $"Backup created: {path}";
        MessageBox.Show($"Backup created:{Environment.NewLine}{path}", "Backup", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void ExportJson()
    {
        using var dialog = new SaveFileDialog
        {
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            FileName = $"bankroll-export-{DateTime.Now:yyyyMMdd-HHmmss}.json"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _repository.ExportJson(_data, dialog.FileName);
        _statusLabel.Text = $"JSON exported: {dialog.FileName}";
    }

    private void ImportJson()
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _data = _repository.ImportJson(dialog.FileName);
        SaveData("JSON imported.");
    }

    private void ExportCsv()
    {
        using var dialog = new FolderBrowserDialog { Description = "Choose a folder for CSV export." };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        CsvBankrollSerializer.ExportToFolder(_data, dialog.SelectedPath);
        _statusLabel.Text = $"CSV exported: {dialog.SelectedPath}";
    }

    private void ImportCsv()
    {
        using var dialog = new FolderBrowserDialog { Description = "Choose a folder containing ledger.csv, tournaments.csv, and cash.csv." };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _data = CsvBankrollSerializer.ImportFromFolder(dialog.SelectedPath, _data.Settings);
        SaveData("CSV imported.");
    }
}
