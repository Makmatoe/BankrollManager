using BankrollManager.Core.Models;
using BankrollManager.Core.Services;

namespace BankrollManager.App;

public sealed partial class MainForm
{
    private Control BuildDataAuditTab()
    {
        var root = BuildGridShell(out var buttons);
        AddGridButton(buttons, "Open Issue", OpenSelectedAuditIssue);
        AddGridButton(buttons, "Refresh Audit", RefreshAuditSources);

        _auditStatusLabel = Theme.Label("Audit not run yet", Theme.BodyFont, Theme.Muted);
        _auditStatusLabel.AutoSize = false;
        _auditStatusLabel.Width = 460;
        _auditStatusLabel.Height = Theme.ControlHeight;
        _auditStatusLabel.Margin = new Padding(10, 7, 4, 0);
        _auditStatusLabel.TextAlign = ContentAlignment.MiddleLeft;
        buttons.Controls.Add(_auditStatusLabel);

        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            BackColor = Theme.Back,
            Margin = new Padding(0)
        };
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 260));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var top = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 1,
            ColumnCount = 2,
            BackColor = Theme.Back,
            Margin = new Padding(0, 0, 0, 8)
        };
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));
        content.Controls.Add(top, 0, 0);

        _auditBreakdownGrid = CreateGrid(_auditBreakdownSource);
        AddTextColumn(_auditBreakdownGrid, "Metric", "Metric", 180);
        AddTextColumn(_auditBreakdownGrid, "Amount", "Amount", 110);
        AddTextColumn(_auditBreakdownGrid, "Notes", "Notes", 360);
        top.Controls.Add(BuildAuditSection("Bankroll Breakdown", _auditBreakdownGrid), 0, 0);

        _auditPlatformGrid = CreateGrid(_auditPlatformSource);
        AddTextColumn(_auditPlatformGrid, "Platform", "Platform", 120);
        AddTextColumn(_auditPlatformGrid, "ExpectedCashBalance", "Expected Cash", 125);
        AddTextColumn(_auditPlatformGrid, "ActiveTableCash", "On Tables", 105);
        AddTextColumn(_auditPlatformGrid, "TotalExposure", "Exposure", 105);
        AddTextColumn(_auditPlatformGrid, "TicketBalance", "Tickets", 90);
        AddTextColumn(_auditPlatformGrid, "ActualCashBalance", "Actual Cash", 115);
        AddTextColumn(_auditPlatformGrid, "AcceptedCashDifference", "Accepted Diff", 115);
        AddTextColumn(_auditPlatformGrid, "Difference", "Difference", 105);
        AddTextColumn(_auditPlatformGrid, "LastUpdatedDate", "Updated", 95);
        AddTextColumn(_auditPlatformGrid, "Status", "Status", 115);
        top.Controls.Add(BuildAuditSection("Wallet Reconciliation", _auditPlatformGrid), 1, 0);

        _auditIssueGrid = CreateGrid(_auditIssueSource);
        _auditIssueGrid.CellDoubleClick += (_, _) => OpenSelectedAuditIssue();
        AddTextColumn(_auditIssueGrid, "Severity", "Severity", 82);
        AddTextColumn(_auditIssueGrid, "Area", "Area", 90);
        AddTextColumn(_auditIssueGrid, "Summary", "Issue", 360);
        AddTextColumn(_auditIssueGrid, "Evidence", "Evidence", 360);
        AddTextColumn(_auditIssueGrid, "Action", "Action", 90);
        content.Controls.Add(BuildAuditSection("Audit Issues", _auditIssueGrid), 0, 1);

        root.Controls.Add(content, 0, 1);
        return root;
    }

    private static Control BuildAuditSection(string title, DataGridView grid)
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            BackColor = Theme.Back,
            Padding = new Padding(0),
            Margin = new Padding(4)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var label = Theme.Label(title, Theme.SubHeaderFont, Theme.Text);
        label.AutoSize = false;
        label.Dock = DockStyle.Fill;
        label.Margin = new Padding(0, 0, 0, 6);
        label.TextAlign = ContentAlignment.MiddleLeft;
        root.Controls.Add(label, 0, 0);
        root.Controls.Add(grid, 0, 1);
        return root;
    }

    private void RefreshAuditSources()
    {
        var report = DataAuditService.GetReport(_data, DateOnly.FromDateTime(DateTime.Today));
        ReplaceSource(_auditBreakdownSource, report.BreakdownRows);
        ReplaceSource(_auditPlatformSource, report.Platforms);
        ReplaceSource(_auditIssueSource, report.Issues);

        if (_auditStatusLabel is not null)
        {
            _auditStatusLabel.Text = report.Reconciles
                ? $"Cash bankroll reconciles. {report.Issues.Count} audit issue(s)."
                : $"Cash bankroll does not reconcile. {report.Issues.Count} audit issue(s).";
            _auditStatusLabel.ForeColor = report.Reconciles && report.Issues.Count == 0
                ? Theme.Positive
                : report.Reconciles
                    ? Theme.Warning
                    : Theme.Negative;
        }
    }

    private void OpenSelectedAuditIssue()
    {
        if (Selected<DataAuditIssue>(_auditIssueSource) is not { } issue)
        {
            return;
        }

        switch (issue.TargetType)
        {
            case AuditIssueTargetType.Tournament:
                OpenAuditTournamentIssue(issue);
                break;
            case AuditIssueTargetType.Cash:
                OpenAuditCashIssue(issue);
                break;
            case AuditIssueTargetType.Ledger:
                OpenAuditLedgerIssue(issue);
                break;
            case AuditIssueTargetType.Wallet:
                OpenAuditWalletIssue(issue);
                break;
            default:
                _statusLabel.Text = issue.Summary;
                break;
        }
    }

    private void OpenAuditTournamentIssue(DataAuditIssue issue)
    {
        if (issue.TargetId is not { } id)
        {
            return;
        }

        _tournamentFilterControls.ClearFilters();
        SelectNavigationPage("MTTs");
        if (SelectGridRow(_tournamentLoader, _tournamentSource, _tournamentGrid, entry => entry.Id == id))
        {
            _statusLabel.Text = issue.Summary;
        }
    }

    private void OpenAuditCashIssue(DataAuditIssue issue)
    {
        if (issue.TargetId is not { } id)
        {
            return;
        }

        _cashFilterControls.ClearFilters();
        SelectNavigationPage("Cash");
        if (SelectGridRow(_cashLoader, _cashSource, _cashGrid, entry => entry.Id == id))
        {
            _statusLabel.Text = issue.Summary;
        }
    }

    private void OpenAuditLedgerIssue(DataAuditIssue issue)
    {
        if (issue.TargetId is not { } id)
        {
            return;
        }

        _ledgerFilterControls.ClearFilters();
        SelectNavigationPage("Ledger");
        if (SelectGridRow(_ledgerLoader, _ledgerSource, _ledgerGrid, entry => entry.Id == id))
        {
            _statusLabel.Text = issue.Summary;
        }
    }

    private void OpenAuditWalletIssue(DataAuditIssue issue)
    {
        SelectNavigationPage("Wallets");
        if (SelectGridRow<PlatformSummary>(
            _walletSource,
            _walletGrid,
            summary => string.Equals(summary.Name, issue.TargetKey, StringComparison.OrdinalIgnoreCase)))
        {
            _statusLabel.Text = issue.Summary;
        }
    }
}
