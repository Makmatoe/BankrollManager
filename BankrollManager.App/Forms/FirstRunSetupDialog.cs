using BankrollManager.Core.Models;
using BankrollManager.Core.Services;

namespace BankrollManager.App.Forms;

internal sealed class FirstRunSetupDialog : Form
{
    private readonly TextBox _currency;
    private readonly CheckedListBox _enabledPlatforms;
    private readonly ComboBox _defaultPlatform;
    private readonly DateTimePicker _setupDate;
    private readonly ComboBox _fundingMode;
    private readonly NumericUpDown _fundingAmount;
    private readonly ComboBox _depositPlatform;
    private readonly Dictionary<Platform, CheckBox> _walletEnabled = [];
    private readonly Dictionary<Platform, NumericUpDown> _walletBalances = [];

    public FirstRunSetupDialog(BankrollData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        data.EnsureDefaults();

        Text = "Quick Setup";
        Size = new Size(680, 720);
        MinimumSize = new Size(620, 620);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Theme.Back;
        ForeColor = Theme.Text;
        Font = Theme.BodyFont;

        Options = new FirstRunSetupOptions();

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            BackColor = Theme.Back
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 108));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        Controls.Add(root);

        root.Controls.Add(BuildHeader(), 0, 0);

        var scroll = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Theme.Back,
            Padding = new Padding(14, 4, 14, 12)
        };
        root.Controls.Add(scroll, 0, 1);

        var form = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            BackColor = Theme.Back
        };
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 210));
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        scroll.Controls.Add(form);

        _currency = Theme.TextBox();
        _currency.Text = string.IsNullOrWhiteSpace(data.Settings.CurrencySymbol)
            ? "\u20ac"
            : data.Settings.CurrencySymbol;
        _enabledPlatforms = BuildEnabledPlatformsList(data.Settings.GetEnabledPlatforms());
        _enabledPlatforms.ItemCheck += (_, _) => BeginInvoke(new Action(UpdatePlatformChoices));
        _defaultPlatform = Theme.EnumBox(data.Settings.DefaultPlatform, PlatformCatalog.EnabledPlatforms(data.Settings, data.Settings.DefaultPlatform));
        _setupDate = Theme.DatePicker(DateOnly.FromDateTime(DateTime.Today));
        _fundingMode = BuildFundingModeBox();
        _fundingAmount = PositiveMoneyBox(Math.Max(0m, data.Settings.StartingBankroll));
        _depositPlatform = Theme.EnumBox(data.Settings.DefaultPlatform, PlatformCatalog.EnabledPlatforms(data.Settings, data.Settings.DefaultPlatform));
        _fundingMode.SelectedIndexChanged += (_, _) => UpdateFundingMode();

        AddSection(form, "Basics");
        AddRow(form, "Currency", _currency);
        AddTallRow(form, "Enabled platforms", _enabledPlatforms, 116);
        AddRow(form, "Default platform", _defaultPlatform);
        AddRow(form, "Setup date", _setupDate);

        AddSection(form, "Opening Cash Bankroll");
        AddRow(form, "Funding type", _fundingMode);
        AddRow(form, "Amount", _fundingAmount);
        AddRow(form, "Deposit platform", _depositPlatform);

        AddSection(form, "Platform Balances");
        AddTallRow(form, "Starting balances", BuildWalletBalancePanel(data), 190);

        root.Controls.Add(BuildFooter(), 0, 2);
        UpdatePlatformChoices();
        UpdateFundingMode();
    }

    public FirstRunSetupOptions Options { get; private set; }
    public bool SetupSkipped { get; private set; }

    private static Control BuildHeader()
    {
        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Panel,
            Padding = new Padding(18, 14, 18, 10),
            RowCount = 2
        };
        header.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        header.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var title = Theme.Label("Quick Setup", Theme.HeaderFont, Theme.Text);
        title.AutoSize = false;
        title.Dock = DockStyle.Fill;
        title.TextAlign = ContentAlignment.MiddleLeft;
        title.Margin = new Padding(0);
        header.Controls.Add(title, 0, 0);

        var body = Theme.Label(
            "Set the essentials for this bankroll before you start logging play.",
            Theme.BodyFont,
            Theme.Muted);
        body.AutoSize = false;
        body.Dock = DockStyle.Fill;
        body.TextAlign = ContentAlignment.MiddleLeft;
        body.Margin = new Padding(0);
        header.Controls.Add(body, 0, 1);

        return header;
    }

    private Control BuildFooter()
    {
        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(8),
            BackColor = Theme.Panel
        };

        var finish = Theme.Button("Finish Setup");
        finish.Click += (_, _) => Save();
        var skip = Theme.Button("Skip Setup");
        skip.Click += (_, _) =>
        {
            SetupSkipped = true;
            DialogResult = DialogResult.Ignore;
            Close();
        };
        var cancel = Theme.Button("Cancel");
        cancel.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        footer.Controls.Add(finish);
        footer.Controls.Add(skip);
        footer.Controls.Add(cancel);
        AcceptButton = finish;
        CancelButton = cancel;
        return footer;
    }

    private Control BuildWalletBalancePanel(BankrollData data)
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            BackColor = Theme.Back
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));

        foreach (var platform in Enum.GetValues<Platform>().OrderBy(platform => platform.ToString(), NaturalSortComparer.Instance))
        {
            var row = panel.RowCount++;
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));

            var wallet = data.PlatformWallets.FirstOrDefault(wallet => wallet.Platform == platform);
            var check = new CheckBox
            {
                Text = platform.ToString(),
                Checked = wallet?.ActualCashBalance.HasValue == true,
                AutoSize = true,
                ForeColor = Theme.Text,
                BackColor = Theme.Back,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 8, 8, 0)
            };
            var balance = PositiveMoneyBox(wallet?.ActualCashBalance ?? 0m);
            balance.Dock = DockStyle.Left;
            balance.Enabled = check.Checked;
            check.CheckedChanged += (_, _) => balance.Enabled = check.Checked && IsPlatformChecked(platform);

            _walletEnabled[platform] = check;
            _walletBalances[platform] = balance;
            panel.Controls.Add(check, 0, row);
            panel.Controls.Add(balance, 1, row);
        }

        return panel;
    }

    private static CheckedListBox BuildEnabledPlatformsList(IReadOnlyList<Platform> enabledPlatforms)
    {
        var list = new CheckedListBox
        {
            CheckOnClick = true,
            BackColor = Theme.PanelAlt,
            ForeColor = Theme.Text,
            BorderStyle = BorderStyle.FixedSingle,
            Font = Theme.BodyFont,
            Height = 104,
            Width = 320
        };

        foreach (var platform in Enum.GetValues<Platform>().OrderBy(platform => platform.ToString(), NaturalSortComparer.Instance))
        {
            list.Items.Add(platform, enabledPlatforms.Contains(platform));
        }

        return list;
    }

    private static ComboBox BuildFundingModeBox()
    {
        var box = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Theme.PanelAlt,
            ForeColor = Theme.Text,
            FlatStyle = FlatStyle.Flat,
            Font = Theme.BodyFont,
            Width = 300,
            Height = Theme.ControlHeight
        };
        box.Items.Add(new FundingModeItem(FirstRunFundingMode.StartingBankroll, "Starting cash bankroll setting"));
        box.Items.Add(new FundingModeItem(FirstRunFundingMode.DepositEntry, "First deposit ledger entry"));
        box.SelectedIndex = 0;
        return box;
    }

    private static NumericUpDown PositiveMoneyBox(decimal value)
    {
        var box = Theme.MoneyBox(Math.Max(0m, value));
        box.Minimum = 0m;
        return box;
    }

    private static void AddSection(TableLayoutPanel layout, string text)
    {
        var row = layout.RowCount++;
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        var label = Theme.Label(text, Theme.SubHeaderFont, Theme.Accent);
        label.AutoSize = false;
        label.Dock = DockStyle.Fill;
        label.Margin = new Padding(0, 16, 4, 4);
        label.TextAlign = ContentAlignment.BottomLeft;
        layout.Controls.Add(label, 0, row);
        layout.SetColumnSpan(label, 2);
    }

    private static void AddRow(TableLayoutPanel layout, string label, Control control)
    {
        var row = layout.RowCount++;
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));

        var labelControl = Theme.Label(label, Theme.BodyFont, Theme.Muted);
        labelControl.AutoSize = false;
        labelControl.Dock = DockStyle.Fill;
        labelControl.Margin = new Padding(0);
        labelControl.Padding = new Padding(0, 0, 14, 0);
        labelControl.TextAlign = ContentAlignment.MiddleLeft;

        control.AutoSize = false;
        control.Dock = DockStyle.Left;
        control.Width = 320;
        control.Height = Math.Max(32, control.Height);
        control.Margin = new Padding(0, 6, 0, 6);

        layout.Controls.Add(labelControl, 0, row);
        layout.Controls.Add(control, 1, row);
    }

    private static void AddTallRow(TableLayoutPanel layout, string label, Control control, int height)
    {
        var row = layout.RowCount++;
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, height));

        var labelControl = Theme.Label(label, Theme.BodyFont, Theme.Muted);
        labelControl.AutoSize = false;
        labelControl.Dock = DockStyle.Fill;
        labelControl.Margin = new Padding(0);
        labelControl.Padding = new Padding(0, 8, 14, 0);
        labelControl.TextAlign = ContentAlignment.TopLeft;

        control.AutoSize = false;
        control.Dock = DockStyle.Left;
        control.Width = 340;
        control.Height = Math.Max(32, height - 16);
        control.Margin = new Padding(0, 6, 0, 6);

        layout.Controls.Add(labelControl, 0, row);
        layout.Controls.Add(control, 1, row);
    }

    private void UpdatePlatformChoices()
    {
        var enabledPlatforms = CheckedEnabledPlatforms();
        if (enabledPlatforms.Count == 0)
        {
            enabledPlatforms = Enum.GetValues<Platform>().ToList();
        }

        var selectedDefault = _defaultPlatform.SelectedItem is Platform defaultPlatform
            ? defaultPlatform
            : enabledPlatforms[0];
        Theme.SetEnumBoxItems(
            _defaultPlatform,
            enabledPlatforms,
            enabledPlatforms.Contains(selectedDefault) ? selectedDefault : enabledPlatforms[0]);

        var selectedDeposit = _depositPlatform.SelectedItem is Platform depositPlatform
            ? depositPlatform
            : (Platform)_defaultPlatform.SelectedItem!;
        Theme.SetEnumBoxItems(
            _depositPlatform,
            enabledPlatforms,
            enabledPlatforms.Contains(selectedDeposit) ? selectedDeposit : (Platform)_defaultPlatform.SelectedItem!);

        foreach (var (platform, check) in _walletEnabled)
        {
            var platformEnabled = IsPlatformChecked(platform);
            check.Enabled = platformEnabled;
            if (!platformEnabled)
            {
                check.Checked = false;
            }

            _walletBalances[platform].Enabled = platformEnabled && check.Checked;
        }
    }

    private void UpdateFundingMode()
    {
        _depositPlatform.Enabled = _fundingMode.SelectedItem is FundingModeItem
        {
            Mode: FirstRunFundingMode.DepositEntry
        };
    }

    private List<Platform> CheckedEnabledPlatforms()
    {
        return _enabledPlatforms.CheckedItems
            .Cast<object>()
            .OfType<Platform>()
            .ToList();
    }

    private bool IsPlatformChecked(Platform platform)
    {
        return _enabledPlatforms.CheckedItems
            .Cast<object>()
            .OfType<Platform>()
            .Contains(platform);
    }

    private void Save()
    {
        var enabledPlatforms = CheckedEnabledPlatforms();
        if (enabledPlatforms.Count == 0)
        {
            MessageBox.Show(
                "Keep at least one platform enabled.",
                "Quick Setup",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        var platformBalances = new Dictionary<Platform, decimal?>();
        foreach (var (platform, check) in _walletEnabled)
        {
            if (check.Checked && check.Enabled)
            {
                platformBalances[platform] = _walletBalances[platform].Value;
            }
        }

        Options = new FirstRunSetupOptions
        {
            CurrencySymbol = string.IsNullOrWhiteSpace(_currency.Text) ? "\u20ac" : _currency.Text.Trim(),
            EnabledPlatforms = enabledPlatforms,
            DefaultPlatform = (Platform)_defaultPlatform.SelectedItem!,
            FundingMode = ((FundingModeItem)_fundingMode.SelectedItem!).Mode,
            FundingAmount = _fundingAmount.Value,
            DepositPlatform = (Platform)_depositPlatform.SelectedItem!,
            SetupDate = DateOnly.FromDateTime(_setupDate.Value),
            PlatformBalances = platformBalances
        };

        DialogResult = DialogResult.OK;
        Close();
    }

    private sealed record FundingModeItem(FirstRunFundingMode Mode, string Text)
    {
        public override string ToString()
        {
            return Text;
        }
    }
}
