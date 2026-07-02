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
        var viewData = BuildViewData();
        _currentViewData = viewData;
        RefreshDataSources(viewData);
        RefreshDashboard(viewData);
        RefreshEmptyStates();
        LoadSettingsControls();
        RefreshDecisionPlatformChoices(includeCurrent: true);
        RefreshDecision();
        RefreshTournamentEv();
    }

    private BankrollViewData BuildViewData()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var auditTimeline = BankrollCalculator.GetAuditTimeline(_data);
        var platformSummaries = BankrollCalculator.GetPlatformSummaries(_data).OrderBy(summary => summary.Name).ToList();
        var dailySummaries = BankrollCalculator.GetDailySummaries(_data);
        return new BankrollViewData(
            auditTimeline,
            platformSummaries,
            dailySummaries,
            BankrollCalculator.GetMonthlySummaries(_data, dailySummaries),
            BankrollCalculator.GetYearlySummaries(_data, dailySummaries),
            BankrollCalculator.GetRunningBankroll(_data),
            BankrollCalculator.GetFormatComparison(_data),
            BankrollCalculator.GetCategoryComparison(_data),
            NeedsAttentionService.GetItems(_data, today, platformSummaries));
    }

    private void RefreshDataSources(BankrollViewData viewData)
    {
        var grids = RefreshableDataGrids().ToArray();
        foreach (var grid in grids)
        {
            grid.SuspendLayout();
        }

        try
        {
            ReplaceSource(_overviewAttentionSource, viewData.AttentionItems);
            _tournamentLoader.SetRows(FilteredTournamentRows(), IsNavigationPageSelected("MTTs"));
            ReplaceSource(
                _overviewOpenTournamentSource,
                _data.TournamentEntries
                    .Where(entry => entry.Status != TournamentStatus.Finished)
                    .OrderBy(entry => entry.Date)
                    .ThenBy(entry => entry.RegistrationTime ?? TimeOnly.MinValue)
                    .Take(12));
            ReplaceSource(
                _overviewRecentActivitySource,
                viewData.AuditTimeline
                    .OrderByDescending(entry => entry.Date)
                    .ThenByDescending(entry => entry.Time ?? TimeOnly.MinValue)
                    .Take(12));
            _cashLoader.SetRows(FilteredCashRows(), IsNavigationPageSelected("Cash"));
            _ledgerLoader.SetRows(FilteredLedgerRows(), IsNavigationPageSelected("Ledger"));
            _timelineLoader.SetRows(FilteredTimelineRows(), IsNavigationPageSelected("Timeline"));
            ReplaceSource(_dailySource, viewData.DailySummaries.OrderByDescending(summary => summary.Date));
            ReplaceSource(_monthlySource, viewData.MonthlySummaries.OrderByDescending(summary => summary.Month));
            ReplaceSource(_yearlySource, viewData.YearlySummaries.OrderByDescending(summary => summary.Year));
            ReplaceSource(_platformSource, viewData.PlatformSummaries);
            ReplaceSource(_walletSource, viewData.PlatformSummaries);
            ReplaceSource(_formatSource, viewData.FormatComparison.OrderBy(summary => summary.Name));
            ReplaceSource(_categorySource, viewData.CategoryComparison.OrderBy(summary => summary.Name));
            RefreshAuditSources();
            RefreshMonthlyReviewSources();
            ReplaceSource(
                _categoryRulesSource,
                _data.Settings.CategoryRules.OrderBy(rule => rule.Category.ToString(), NaturalSortComparer.Instance));
        }
        finally
        {
            foreach (var grid in grids)
            {
                grid.ResumeLayout();
            }
        }

        UpdateTournamentInspector();
        UpdateCashInspector();
        RefreshSelectedDaySelection();
    }

    private bool IsNavigationPageSelected(string title)
    {
        return _navigationPages.Count > _selectedNavigationIndex
            && string.Equals(_navigationPages[_selectedNavigationIndex].Title, title, StringComparison.OrdinalIgnoreCase);
    }

    private void LoadNavigationPageData(string title)
    {
        if (string.Equals(title, "MTTs", StringComparison.OrdinalIgnoreCase))
        {
            _tournamentLoader.SetRows(FilteredTournamentRows(), loadNow: true);
            UpdateTournamentInspector();
            FitGridColumns(_tournamentGrid);
            return;
        }

        if (string.Equals(title, "Cash", StringComparison.OrdinalIgnoreCase))
        {
            _cashLoader.SetRows(FilteredCashRows(), loadNow: true);
            UpdateCashInspector();
            FitGridColumns(_cashGrid);
            return;
        }

        if (string.Equals(title, "Ledger", StringComparison.OrdinalIgnoreCase))
        {
            _ledgerLoader.SetRows(FilteredLedgerRows(), loadNow: true);
            FitGridColumns(_ledgerGrid);
            return;
        }

        if (string.Equals(title, "Timeline", StringComparison.OrdinalIgnoreCase))
        {
            _timelineLoader.SetRows(FilteredTimelineRows(), loadNow: true);
            FitGridColumns(_timelineGrid);
        }
    }

    private IEnumerable<DataGridView> RefreshableDataGrids()
    {
        DataGridView?[] grids =
        [
            _overviewAttentionGrid,
            _overviewOpenGrid,
            _overviewActivityGrid,
            _auditBreakdownGrid,
            _auditPlatformGrid,
            _auditIssueGrid,
            _monthlyReviewMetricGrid,
            _monthlyReviewFormatGrid,
            _monthlyReviewCategoryGrid,
            _monthlyReviewPlatformGrid,
            _monthlyReviewSpecialtyGrid,
            _monthlyReviewWinGrid,
            _monthlyReviewLossGrid,
            _monthlyReviewStopLossGrid,
            _monthlyReviewRiskGrid,
            _monthlyReviewNoteGrid,
            _tournamentGrid,
            _cashGrid,
            _ledgerGrid,
            _timelineGrid,
            _dailyGrid,
            _selectedDayTimelineGrid,
            _monthlyGrid,
            _yearlyGrid,
            _platformGrid,
            _walletGrid,
            _formatGrid,
            _categoryGrid,
            _categoryRulesGrid
        ];

        return grids.OfType<DataGridView>().Where(grid => !grid.IsDisposed);
    }

    private static void ReplaceSource<T>(BindingSource source, IEnumerable<T> items)
    {
        source.RaiseListChangedEvents = false;
        try
        {
            source.DataSource = new SortableBindingList<T>(items);
        }
        finally
        {
            source.RaiseListChangedEvents = true;
        }

        source.ResetBindings(metadataChanged: false);
    }

    private sealed record BankrollViewData(
        List<AuditTimelineEntry> AuditTimeline,
        List<PlatformSummary> PlatformSummaries,
        List<DailySummary> DailySummaries,
        List<MonthlySummary> MonthlySummaries,
        List<YearlySummary> YearlySummaries,
        List<RunningBankrollPoint> RunningBankroll,
        List<ComparisonSummary> FormatComparison,
        List<ComparisonSummary> CategoryComparison,
        IReadOnlyList<AttentionItem> AttentionItems);

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

    private void RestoreBackupData()
    {
        var backupDirectory = Path.Combine(_repository.DataDirectory, "Backups");
        using var dialog = new OpenFileDialog
        {
            Filter = "JSON backup files (*.json)|*.json|All files (*.*)|*.*",
            Title = "Restore Bankroll Backup",
            InitialDirectory = Directory.Exists(backupDirectory) ? backupDirectory : _repository.DataDirectory
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        if (MessageBox.Show(
            "Restore this backup over the current bankroll data? A safety backup of the current data will be created first.",
            "Confirm restore",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning) != DialogResult.Yes)
        {
            return;
        }

        var restoredData = _repository.ImportJson(dialog.FileName);
        var safetyBackupPath = _repository.CreateTimestampedBackup(_data);
        _data = restoredData;
        SaveData($"Backup restored. Previous data safety backup: {safetyBackupPath}");
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

    private void ExportChatGpt()
    {
        using var dialog = new SaveFileDialog
        {
            Filter = "Markdown files (*.md)|*.md|Text files (*.txt)|*.txt|All files (*.*)|*.*",
            FileName = $"bankroll-chatgpt-export-{DateTime.Now:yyyyMMdd-HHmmss}.md"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        ChatGptBankrollExporter.ExportToFile(_data, dialog.FileName, DateTime.Now);
        _statusLabel.Text = $"ChatGPT export created: {dialog.FileName}";
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
