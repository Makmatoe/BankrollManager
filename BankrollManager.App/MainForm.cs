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

public sealed partial class MainForm : Form
{
    private readonly JsonBankrollRepository _repository;
    private readonly BindingSource _tournamentSource = new();
    private readonly BindingSource _cashSource = new();
    private readonly BindingSource _ledgerSource = new();
    private readonly BindingSource _timelineSource = new();
    private readonly BindingSource _dailySource = new();
    private readonly BindingSource _selectedDayTimelineSource = new();
    private readonly BindingSource _monthlySource = new();
    private readonly BindingSource _yearlySource = new();
    private readonly BindingSource _platformSource = new();
    private readonly BindingSource _walletSource = new();
    private readonly BindingSource _formatSource = new();
    private readonly BindingSource _categorySource = new();
    private readonly BindingSource _categoryRulesSource = new();
    private readonly BindingSource _overviewAttentionSource = new();
    private readonly BindingSource _overviewOpenTournamentSource = new();
    private readonly BindingSource _overviewRecentActivitySource = new();
    private readonly BindingSource _auditBreakdownSource = new();
    private readonly BindingSource _auditPlatformSource = new();
    private readonly BindingSource _auditIssueSource = new();
    private readonly BindingSource _monthlyReviewMetricSource = new();
    private readonly BindingSource _monthlyReviewFormatSource = new();
    private readonly BindingSource _monthlyReviewCategorySource = new();
    private readonly BindingSource _monthlyReviewPlatformSource = new();
    private readonly BindingSource _monthlyReviewSpecialtySource = new();
    private readonly BindingSource _monthlyReviewWinSource = new();
    private readonly BindingSource _monthlyReviewLossSource = new();
    private readonly BindingSource _monthlyReviewStopLossSource = new();
    private readonly BindingSource _monthlyReviewRiskSource = new();
    private readonly BindingSource _monthlyReviewNoteSource = new();
    private readonly Dictionary<string, KpiCard> _kpiValues = [];
    private GridLoadController<TournamentEntry> _tournamentLoader = null!;
    private GridLoadController<CashSession> _cashLoader = null!;
    private GridLoadController<LedgerEntry> _ledgerLoader = null!;
    private GridLoadController<AuditTimelineEntry> _timelineLoader = null!;
    private DetailTableFilterControls _tournamentFilterControls = null!;
    private DetailTableFilterControls _cashFilterControls = null!;
    private DetailTableFilterControls _ledgerFilterControls = null!;
    private DetailTableFilterControls _timelineFilterControls = null!;
    private BankrollViewData? _currentViewData;
    private static readonly HashSet<string> CompactTournamentColumns = new(StringComparer.Ordinal)
    {
        "Date",
        "RegistrationTime",
        "Status",
        "Platform",
        "Format",
        "EventName",
        "EventTag",
        "BuyIn",
        "TicketValueWon",
        "TotalValueProfitLoss",
        "ValueROI",
        "Placement",
        "RuleCheckResult"
    };
    private static readonly HashSet<string> CompactCashColumns = new(StringComparer.Ordinal)
    {
        "Date",
        "SessionTime",
        "Status",
        "Platform",
        "Format",
        "Stakes",
        "StartStackBuyIn",
        "ActiveTableCash",
        "Cashout",
        "NetProfit",
        "BBPer100",
        "RuleCheckResult"
    };

    private BankrollData _data = new();
    private Label _statusLabel = null!;
    private Label _stopLossBanner = null!;
    private Label _tournamentEmptyState = null!;
    private Label _cashEmptyState = null!;
    private Label _ledgerEmptyState = null!;
    private MiniChart _dailyChart = null!;
    private MiniChart _dailyReviewChart = null!;
    private MiniChart _runningChart = null!;
    private MiniChart _comparisonChart = null!;
    private MiniChart _monthlyChart = null!;
    private DataGridView _overviewAttentionGrid = null!;
    private DataGridView _overviewOpenGrid = null!;
    private DataGridView _overviewActivityGrid = null!;
    private DataGridView _auditBreakdownGrid = null!;
    private DataGridView _auditPlatformGrid = null!;
    private DataGridView _auditIssueGrid = null!;
    private Label _auditStatusLabel = null!;
    private DateTimePicker _monthlyReviewMonth = null!;
    private Label _monthlyReviewStatusLabel = null!;
    private DataGridView _monthlyReviewMetricGrid = null!;
    private DataGridView _monthlyReviewFormatGrid = null!;
    private DataGridView _monthlyReviewCategoryGrid = null!;
    private DataGridView _monthlyReviewPlatformGrid = null!;
    private DataGridView _monthlyReviewSpecialtyGrid = null!;
    private DataGridView _monthlyReviewWinGrid = null!;
    private DataGridView _monthlyReviewLossGrid = null!;
    private DataGridView _monthlyReviewStopLossGrid = null!;
    private DataGridView _monthlyReviewRiskGrid = null!;
    private DataGridView _monthlyReviewNoteGrid = null!;
    private DataGridView _tournamentGrid = null!;
    private Button _tournamentDetailsButton = null!;
    private Label _tournamentInspectorTitle = null!;
    private Label _tournamentInspectorResult = null!;
    private Label _tournamentInspectorMeta = null!;
    private Label _tournamentInspectorNotes = null!;
    private bool _tournamentDetailColumnsVisible;
    private DataGridView _cashGrid = null!;
    private Button _cashDetailsButton = null!;
    private Label _cashInspectorTitle = null!;
    private Label _cashInspectorResult = null!;
    private Label _cashInspectorMeta = null!;
    private Label _cashInspectorNotes = null!;
    private bool _cashDetailColumnsVisible;
    private DataGridView _ledgerGrid = null!;
    private DataGridView _timelineGrid = null!;
    private DataGridView _dailyGrid = null!;
    private MiniChart _selectedDayChart = null!;
    private DataGridView _selectedDayTimelineGrid = null!;
    private Label _selectedDayTitle = null!;
    private Label _selectedDayMeta = null!;
    private Label _selectedDayEmptyState = null!;
    private DateOnly? _selectedDayDate;
    private bool _syncingDailySelection;
    private DataGridView _monthlyGrid = null!;
    private DataGridView _yearlyGrid = null!;
    private DataGridView _platformGrid = null!;
    private DataGridView _walletGrid = null!;
    private DataGridView _formatGrid = null!;
    private DataGridView _categoryGrid = null!;
    private DataGridView _categoryRulesGrid = null!;
    private int _selectedNavigationIndex;
    private Panel _contentHost = null!;
    private IReadOnlyList<(string Title, Control Content)> _navigationPages = [];
    private IReadOnlyList<Label> _navigationButtons = [];
    private Panel _tutorialPanel = null!;
    private Label _tutorialProgressLabel = null!;
    private ProgressBar _tutorialProgressBar = null!;
    private ListBox _tutorialStepList = null!;
    private Label _tutorialTitle = null!;
    private Label _tutorialBody = null!;
    private FlowLayoutPanel _tutorialChecklist = null!;
    private Button _tutorialTryButton = null!;
    private Button _tutorialPreviousButton = null!;
    private Button _tutorialNextButton = null!;
    private IReadOnlyList<TutorialStep> _tutorialSteps = [];
    private int _tutorialStepIndex;
    private bool _syncingTutorialList;
    private bool _tutorialAutoResumeChecked;
    private bool _firstRunSetupChecked;
    private bool _updateCheckInProgress;

    private ComboBox _appearanceMode = null!;
    private TextBox _currency = null!;
    private CheckedListBox _enabledPlatforms = null!;
    private ComboBox _defaultPlatform = null!;
    private DateTimePicker _activeMonthStart = null!;
    private NumericUpDown _startingBankroll = null!;
    private NumericUpDown _defaultMaxBullets = null!;
    private NumericUpDown _activeReviewYear = null!;
    private NumericUpDown _normalMttRisk = null!;
    private NumericUpDown _sngRisk = null!;
    private NumericUpDown _flipRisk = null!;
    private NumericUpDown _shotRisk = null!;
    private NumericUpDown _cashRisk = null!;
    private NumericUpDown _reviewRiskCapUsage = null!;
    private NumericUpDown _budgetWarning = null!;
    private NumericUpDown _dailyRiskCap = null!;
    private NumericUpDown _activeExposureCap = null!;
    private NumericUpDown _stopLossWarning = null!;
    private NumericUpDown _cashReloadWarning = null!;
    private NumericUpDown _dailyStopLoss = null!;
    private NumericUpDown _monthlyStopLoss = null!;
    private NumericUpDown _reserveTarget = null!;
    private NumericUpDown _protectBelow = null!;
    private NumericUpDown _moveUpBankroll = null!;
    private NumericUpDown _greenLightBankroll = null!;
    private NumericUpDown _profitLockThreshold = null!;
    private CheckBox _sessionLocked = null!;
    private CheckBox _cooldownEnabled = null!;
    private DateTimePicker _cooldownUntil = null!;

    private CheckBox _decisionIsCash = null!;
    private ComboBox _decisionPlatform = null!;
    private ComboBox _decisionCategory = null!;
    private ComboBox _decisionFormat = null!;
    private ComboBox _decisionCashFormat = null!;
    private TextBox _decisionEventName = null!;
    private NumericUpDown _decisionBuyIn = null!;
    private NumericUpDown _decisionBullets = null!;
    private NumericUpDown _decisionAddOns = null!;
    private NumericUpDown _decisionTicketBuyIn = null!;
    private NumericUpDown _decisionCashBuyIn = null!;
    private NumericUpDown _decisionCashReloads = null!;
    private TextBox _decisionNotes = null!;
    private TournamentPreset? _decisionAppliedPreset;
    private bool _decisionApplyingPreset;
    private Button _decisionRegisterButton = null!;
    private Button _decisionStartCashButton = null!;
    private Label _decisionLabel = null!;
    private Label _decisionRisk = null!;
    private Label _decisionBudget = null!;
    private TextBox _decisionExplanation = null!;
    private TextBox _decisionAlternative = null!;
    private TextBox _decisionThresholds = null!;
    private TextBox _decisionWarnings = null!;
    private TextBox _tournamentEvName = null!;
    private NumericUpDown _tournamentEvBuyIn = null!;
    private ComboBox _tournamentEvPrizeType = null!;
    private ComboBox _tournamentEvTournamentType = null!;
    private NumericUpDown _tournamentEvNumberOfTickets = null!;
    private NumericUpDown _tournamentEvTicketValue = null!;
    private NumericUpDown _tournamentEvManualPrizeValue = null!;
    private NumericUpDown _tournamentEvCurrentEntries = null!;
    private NumericUpDown _tournamentEvTotalEntries = null!;
    private NumericUpDown _tournamentEvPaidPlaces = null!;
    private NumericUpDown _tournamentEvTicketDiscount = null!;
    private NumericUpDown _tournamentEvSampleSize = null!;
    private NumericUpDown _tournamentEvBankrollSize = null!;
    private TextBox _tournamentEvPayoutStructure = null!;
    private Button _tournamentEvCheckButton = null!;
    private Label _tournamentEvStatusLabel = null!;
    private Label _tournamentEvVarianceRatingLabel = null!;
    private Label _tournamentEvPrizeValueLabel = null!;
    private Label _tournamentEvMaxPrizeLabel = null!;
    private Label _tournamentEvUncappedGrossLabel = null!;
    private Label _tournamentEvGrossLabel = null!;
    private Label _tournamentEvNetLabel = null!;
    private Label _tournamentEvRoiLabel = null!;
    private Label _tournamentEvBreakevenLabel = null!;
    private Label _tournamentEvPositiveUntilLabel = null!;
    private Label _tournamentEvNegativeFromLabel = null!;
    private Label _tournamentEvVarianceEvLabel = null!;
    private Label _tournamentEvVarianceRoiLabel = null!;
    private Label _tournamentEvCashProbabilityLabel = null!;
    private Label _tournamentEvStdDevLabel = null!;
    private Label _tournamentEvStdDevBuyInsLabel = null!;
    private Label _tournamentEvExpectedAfterSampleLabel = null!;
    private Label _tournamentEvLikelyRangeLabel = null!;
    private Label _tournamentEvChanceNotAheadLabel = null!;
    private Label _tournamentEvBankrollSwingLabel = null!;

    public MainForm()
        : this(new JsonBankrollRepository())
    {
    }

    public MainForm(JsonBankrollRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);

        _repository = repository;
        _data = _repository.LoadOrCreate();
        Theme.Configure(_data.Settings.AppearanceMode);
        InitializeComponent();
        RefreshAll();
        SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;
        base.OnFormClosed(e);
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        if (!_firstRunSetupChecked)
        {
            _firstRunSetupChecked = true;
            ShowFirstRunSetupIfNeeded();
        }

        if (_tutorialAutoResumeChecked)
        {
            return;
        }

        _tutorialAutoResumeChecked = true;
        if (!_data.Settings.TutorialCompleted && _data.Settings.TutorialStepIndex > 0)
        {
            StartTutorial();
        }
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        UpdateTutorialPanelWidth();
    }

    private void SystemEvents_UserPreferenceChanged(object? sender, UserPreferenceChangedEventArgs e)
    {
        if (_data.Settings.AppearanceMode != AppearanceMode.System
            || e.Category is not (UserPreferenceCategory.Color or UserPreferenceCategory.General or UserPreferenceCategory.VisualStyle)
            || IsDisposed
            || !IsHandleCreated)
        {
            return;
        }

        BeginInvoke(new Action(() =>
        {
            if (_data.Settings.AppearanceMode != AppearanceMode.System
                || !Theme.Configure(AppearanceMode.System))
            {
                return;
            }

            RebuildInterface();
            _statusLabel.Text = "System appearance changed.";
        }));
    }

    private void InitializeComponent()
    {
        SuspendLayout();
        Text = "Bankroll Manager";
        Size = new Size(1420, 900);
        MinimumSize = new Size(1120, 720);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Theme.Back;
        ForeColor = Theme.Text;
        Font = Theme.BodyFont;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            BackColor = Theme.Back
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(root);

        root.Controls.Add(BuildToolbar(), 0, 0);

        var contentHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Back,
            Padding = new Padding(0)
        };
        _contentHost = contentHost;

        var pages = new List<(string Title, Control Content)>
        {
            ("Overview", BuildDashboardTab()),
            ("Wallets", BuildWalletsTab()),
            ("Audit", BuildDataAuditTab()),
            ("Timeline", BuildTimelineTab()),
            ("MTTs", BuildTournamentTab()),
            ("Cash", BuildCashTab()),
            ("Ledger", BuildLedgerTab()),
            ("Day", BuildDailyTab()),
            ("Month", BuildMonthlyTab()),
            ("Monthly Review", BuildMonthlyReviewTab()),
            ("Year", BuildYearlyTab()),
            ("Decide", BuildDecisionTab()),
            ("EV Check", BuildTournamentEvTab()),
            ("Settings", BuildSettingsTab())
        };
        _navigationPages = pages;
        root.Controls.Add(BuildNavigation(contentHost, pages), 0, 1);
        root.Controls.Add(contentHost, 0, 2);
        _tutorialSteps = BuildTutorialSteps();
        _tutorialPanel = BuildTutorialPanel();
        Controls.Add(_tutorialPanel);
        _tutorialPanel.BringToFront();

        ResumeLayout();
    }

    private Control BuildToolbar()
    {
        var shell = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Panel,
            Padding = new Padding(14, 8, 14, 8)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Theme.Panel
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 320));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        shell.Controls.Add(layout);

        var identity = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            BackColor = Theme.Panel,
            Margin = new Padding(0)
        };
        identity.RowStyles.Add(new RowStyle(SizeType.Absolute, 25));
        identity.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(identity, 0, 0);

        var title = Theme.Label("Bankroll Manager", Theme.SubHeaderFont, Theme.Text);
        title.AutoSize = false;
        title.Dock = DockStyle.Fill;
        title.Margin = new Padding(0);
        title.TextAlign = ContentAlignment.MiddleLeft;
        identity.Controls.Add(title, 0, 0);

        _statusLabel = Theme.Label("Ready", Theme.SmallFont, Theme.Muted);
        _statusLabel.AutoSize = false;
        _statusLabel.AutoEllipsis = true;
        _statusLabel.Dock = DockStyle.Fill;
        _statusLabel.Margin = new Padding(0);
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        identity.Controls.Add(_statusLabel, 0, 1);

        var commandHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Panel,
            Padding = new Padding(14, 5, 0, 5),
            Margin = new Padding(0)
        };
        var commandStrip = BuildCommandStrip();
        commandHost.Controls.Add(commandStrip);
        layout.Controls.Add(commandHost, 1, 0);

        return shell;
    }

    private ToolStrip BuildCommandStrip()
    {
        var strip = new ToolStrip
        {
            Dock = DockStyle.Top,
            AutoSize = false,
            Height = 42,
            GripStyle = ToolStripGripStyle.Hidden,
            BackColor = Theme.Panel,
            CanOverflow = true,
            LayoutStyle = ToolStripLayoutStyle.HorizontalStackWithOverflow,
            Padding = new Padding(0),
            Renderer = new HeaderToolStripRenderer()
        };

        strip.Items.Add(BuildCommandButton("Save", () => SaveData("Saved."), CommandTone.Primary));
        strip.Items.Add(BuildCommandButton("Setup", ShowQuickSetup));
        strip.Items.Add(BuildCommandButton("Updates", CheckForUpdates));
        strip.Items.Add(BuildCommandButton("About", ShowAbout));
        strip.Items.Add(BuildCommandButton("Backup", BackupData));
        strip.Items.Add(BuildCommandButton("Restore", RestoreBackupData));
        strip.Items.Add(BuildCommandButton("Tutorial", () => StartTutorial()));
        strip.Items.Add(BuildCommandButton("ChatGPT", ExportChatGpt));
        strip.Items.Add(new ToolStripSeparator());
        strip.Items.Add(BuildCommandDropDown(
            "Export",
            ("ChatGPT", ExportChatGpt),
            ("JSON", ExportJson),
            ("CSV", ExportCsv)));
        strip.Items.Add(BuildCommandDropDown(
            "Import",
            ("JSON", ImportJson),
            ("CSV", ImportCsv)));
        strip.Items.Add(new ToolStripSeparator());
        strip.Items.Add(BuildCommandButton("Lock Today", LockToday, CommandTone.Danger));
        strip.Items.Add(BuildCommandButton("Cooldown", CooldownTomorrow, CommandTone.Danger));
        strip.Items.Add(BuildCommandButton("Clear", ClearLock));

        return strip;
    }

    private Control BuildNavigation(Panel contentHost, IReadOnlyList<(string Title, Control Content)> pages)
    {
        var shell = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Back,
            Padding = new Padding(14, 6, 14, 5)
        };
        shell.Paint += (_, e) =>
        {
            using var border = new Pen(Theme.Border);
            e.Graphics.DrawLine(border, 0, shell.Height - 1, shell.Width, shell.Height - 1);
        };

        var navigation = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Back,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };
        shell.Controls.Add(navigation);

        var buttons = new List<Label>();
        for (var index = 0; index < pages.Count; index++)
        {
            var pageIndex = index;
            var button = BuildNavigationButton(pages[index].Title);
            button.Click += (_, _) => SelectNavigationPage(contentHost, pages, buttons, pageIndex);
            navigation.Controls.Add(button);
            buttons.Add(button);
        }

        SelectNavigationPage(contentHost, pages, buttons, Math.Clamp(_selectedNavigationIndex, 0, pages.Count - 1));
        _navigationButtons = buttons;
        return shell;
    }

    private static Label BuildNavigationButton(string title)
    {
        var button = new Label
        {
            Text = title,
            AutoSize = false,
            AutoEllipsis = true,
            Width = NavigationItemWidth(title),
            Height = 36,
            BackColor = Theme.Back,
            Cursor = Cursors.Hand,
            Font = Theme.BodyFont,
            ForeColor = Theme.Muted,
            Margin = new Padding(0, 0, 6, 0),
            Padding = new Padding(8, 0, 8, 1),
            TextAlign = ContentAlignment.MiddleCenter,
            UseMnemonic = false
        };

        button.Paint += (_, e) =>
        {
            if (button.Tag is not true)
            {
                return;
            }

            using var accent = new SolidBrush(Theme.Accent);
            e.Graphics.FillRectangle(accent, 10, button.Height - 3, Math.Max(8, button.Width - 20), 3);
        };
        button.MouseEnter += (_, _) =>
        {
            if (button.Tag is not true)
            {
                button.BackColor = Theme.Panel;
                button.ForeColor = Theme.Text;
            }
        };
        button.MouseLeave += (_, _) =>
        {
            if (button.Tag is not true)
            {
                button.BackColor = Theme.Back;
                button.ForeColor = Theme.Muted;
            }
        };

        return button;
    }

    private void SelectNavigationPage(
        Panel contentHost,
        IReadOnlyList<(string Title, Control Content)> pages,
        IReadOnlyList<Label> buttons,
        int selectedIndex)
    {
        _selectedNavigationIndex = selectedIndex;
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
            buttons[index].BackColor = selected ? Theme.Panel : Theme.Back;
            buttons[index].ForeColor = selected ? Theme.Text : Theme.Muted;
            buttons[index].Font = Theme.BodyFont;
            buttons[index].Invalidate();
        }

        LoadNavigationPageData(pages[selectedIndex].Title);
    }

}
