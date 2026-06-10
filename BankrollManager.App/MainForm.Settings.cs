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

    private Control BuildSettingsTab()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            BackColor = Theme.Back,
            Padding = new Padding(12)
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 640));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var formScroll = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Theme.Back,
            Padding = new Padding(8, 0, 14, 0)
        };
        var form = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, BackColor = Theme.Back };
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 270));
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 320));
        formScroll.Controls.Add(form);
        root.Controls.Add(formScroll, 0, 0);

        _appearanceMode = Theme.EnumBox(AppearanceMode.Dark);
        _currency = Theme.TextBox();
        _defaultPlatform = Theme.EnumBox(Platform.Unibet);
        _activeMonthStart = Theme.DatePicker(new DateOnly(2026, 6, 1));
        _startingBankroll = Theme.MoneyBox(0m);
        _defaultMaxBullets = Theme.IntBox(1, 99);
        _activeReviewYear = Theme.IntBox(2026, 9999);
        _normalMttRisk = PercentBox();
        _sngRisk = PercentBox();
        _flipRisk = PercentBox();
        _shotRisk = PercentBox();
        _cashRisk = PercentBox();
        _reviewRiskCapUsage = PercentBox();
        _budgetWarning = PercentBox();
        _dailyRiskCap = PercentBox();
        _activeExposureCap = PercentBox();
        _stopLossWarning = PercentBox();
        _cashReloadWarning = PercentBox();
        _dailyStopLoss = Theme.MoneyBox(0m);
        _monthlyStopLoss = PercentBox();
        _reserveTarget = PercentBox();
        _protectBelow = Theme.MoneyBox(0m);
        _moveUpBankroll = Theme.MoneyBox(0m);
        _greenLightBankroll = Theme.MoneyBox(0m);
        _profitLockThreshold = Theme.MoneyBox(0m);
        _sessionLocked = new CheckBox { ForeColor = Theme.Text, AutoSize = true };
        _cooldownEnabled = new CheckBox { ForeColor = Theme.Text, AutoSize = true };
        _cooldownUntil = Theme.DatePicker(DateOnly.FromDateTime(DateTime.Today.AddDays(1)));

        AddSettingsSection(form, "App Defaults");
        AddSettingsRow(form, "Appearance", _appearanceMode);
        AddSettingsRow(form, "Currency", _currency);
        AddSettingsRow(form, "Default platform", _defaultPlatform);
        AddSettingsRow(form, "Active month start", _activeMonthStart);
        AddSettingsRow(form, "Starting bankroll", _startingBankroll);
        AddSettingsRow(form, "Default max bullets", _defaultMaxBullets);
        AddSettingsRow(form, "Active review year", _activeReviewYear);

        AddSettingsSection(form, "Risk Caps");
        AddSettingsRow(form, "Normal MTT max risk %", _normalMttRisk);
        AddSettingsRow(form, "SNG/HexaPro max risk %", _sngRisk);
        AddSettingsRow(form, "Flip max risk %", _flipRisk);
        AddSettingsRow(form, "Shot/Tower max risk %", _shotRisk);
        AddSettingsRow(form, "Cash session max risk %", _cashRisk);

        AddSettingsSection(form, "Guide Thresholds");
        AddSettingsRow(form, "Review at cap usage %", _reviewRiskCapUsage);
        AddSettingsRow(form, "Low budget warning %", _budgetWarning);
        AddSettingsRow(form, "Daily risk cap %", _dailyRiskCap);
        AddSettingsRow(form, "Active exposure cap %", _activeExposureCap);
        AddSettingsRow(form, "Stop-loss warning %", _stopLossWarning);
        AddSettingsRow(form, "Cash reload warning %", _cashReloadWarning);

        AddSettingsSection(form, "Bankroll Protections");
        AddSettingsRow(form, "Daily stop-loss amount", _dailyStopLoss);
        AddSettingsRow(form, "Monthly stop-loss %", _monthlyStopLoss);
        AddSettingsRow(form, "Reserve target %", _reserveTarget);
        AddSettingsRow(form, "Protect below bankroll", _protectBelow);
        AddSettingsRow(form, "Move-up review bankroll", _moveUpBankroll);
        AddSettingsRow(form, "Green-light shot bankroll", _greenLightBankroll);
        AddSettingsRow(form, "Withdrawal/profit lock", _profitLockThreshold);
        AddSettingsRow(form, "Session locked today", _sessionLocked);
        AddSettingsRow(form, "Cooldown enabled", _cooldownEnabled);
        AddSettingsRow(form, "Cooldown until", _cooldownUntil);

        var save = Theme.Button("Save Settings");
        save.Click += (_, _) => SaveSettings();
        AddSettingsRow(form, string.Empty, save);

        var reset = Theme.Button("Reset Category Defaults");
        reset.Click += (_, _) =>
        {
            _data.Settings.CategoryRules = CategoryRuleSettings.CreateDefaults();
            SaveData("Category defaults restored.");
        };
        AddSettingsRow(form, string.Empty, reset);

        var right = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, BackColor = Theme.Back };
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var categoryDefaultsTitle = Theme.Label("Category Defaults", Theme.HeaderFont, Theme.Text);
        categoryDefaultsTitle.AutoSize = false;
        categoryDefaultsTitle.Dock = DockStyle.Fill;
        categoryDefaultsTitle.TextAlign = ContentAlignment.MiddleLeft;
        categoryDefaultsTitle.Margin = new Padding(0, 0, 0, 8);
        right.Controls.Add(categoryDefaultsTitle, 0, 0);
        _categoryRulesGrid = CreateGrid(_categoryRulesSource, readOnly: false);
        _categoryRulesGrid.AutoGenerateColumns = false;
        AddTextColumn(_categoryRulesGrid, "Category", "Category", 120, readOnly: true);
        AddTextColumn(_categoryRulesGrid, "MaxRiskPercent", "Max Risk %", 100, readOnly: false);
        AddTextColumn(_categoryRulesGrid, "MonthlyBudgetPercent", "Budget %", 90, readOnly: false);
        AddTextColumn(_categoryRulesGrid, "DefaultBuyInCap", "Buy-in Cap", 90, readOnly: false);
        AddTextColumn(_categoryRulesGrid, "MinBankroll", "Min Bankroll", 110, readOnly: false);
        AddTextColumn(_categoryRulesGrid, "BulletCap", "Bullets", 70, readOnly: false);
        AddTextColumn(_categoryRulesGrid, "DailyEntryCap", "Day Cap", 80, readOnly: false);
        AddTextColumn(_categoryRulesGrid, "CooldownDays", "Cooldown", 88, readOnly: false);
        AddTextColumn(_categoryRulesGrid, "UsageNote", "Usage Note", 360, readOnly: false);
        _categoryRulesGrid.CellEndEdit += (_, _) => SaveSettings();
        right.Controls.Add(_categoryRulesGrid, 0, 1);
        root.Controls.Add(right, 1, 0);

        return root;
    }


    private void LoadSettingsControls()
    {
        var settings = _data.Settings;
        var today = DateOnly.FromDateTime(DateTime.Today);
        _appearanceMode.SelectedItem = settings.AppearanceMode;
        _currency.Text = settings.CurrencySymbol;
        _defaultPlatform.SelectedItem = settings.DefaultPlatform;
        _activeMonthStart.Value = settings.ActiveMonthStart.ToDateTime(TimeOnly.MinValue);
        _startingBankroll.Value = ClampToBox(_startingBankroll, settings.StartingBankroll);
        _defaultMaxBullets.Value = ClampToBox(_defaultMaxBullets, settings.DefaultMaxBullets);
        _activeReviewYear.Value = ClampToBox(_activeReviewYear, settings.ActiveReviewYear);
        _normalMttRisk.Value = ClampToBox(_normalMttRisk, settings.NormalMttMaxRiskPercent);
        _sngRisk.Value = ClampToBox(_sngRisk, settings.SngHexaProMaxRiskPercent);
        _flipRisk.Value = ClampToBox(_flipRisk, settings.FlipMaxRiskPercent);
        _shotRisk.Value = ClampToBox(_shotRisk, settings.ShotTowerMaxRiskPercent);
        _cashRisk.Value = ClampToBox(_cashRisk, settings.CashSessionMaxRiskPercent);
        _reviewRiskCapUsage.Value = ClampToBox(_reviewRiskCapUsage, settings.ReviewRiskCapUsagePercent);
        _budgetWarning.Value = ClampToBox(_budgetWarning, settings.BudgetWarningPercent);
        _dailyRiskCap.Value = ClampToBox(_dailyRiskCap, settings.DailyRiskCapPercent);
        _activeExposureCap.Value = ClampToBox(_activeExposureCap, settings.ActiveExposureCapPercent);
        _stopLossWarning.Value = ClampToBox(_stopLossWarning, settings.StopLossWarningPercent);
        _cashReloadWarning.Value = ClampToBox(_cashReloadWarning, settings.CashReloadWarningPercent);
        _dailyStopLoss.Value = ClampToBox(_dailyStopLoss, settings.DailyStopLossAmount);
        _monthlyStopLoss.Value = ClampToBox(_monthlyStopLoss, settings.MonthlyPokerStopLossPercent);
        _reserveTarget.Value = ClampToBox(_reserveTarget, settings.ReserveTargetPercent);
        _protectBelow.Value = ClampToBox(_protectBelow, settings.ProtectModeBelowBankroll);
        _moveUpBankroll.Value = ClampToBox(_moveUpBankroll, settings.MoveUpReviewBankroll);
        _greenLightBankroll.Value = ClampToBox(_greenLightBankroll, settings.GreenLightShotBankroll);
        _profitLockThreshold.Value = ClampToBox(_profitLockThreshold, settings.WithdrawalProfitLockThreshold);
        _sessionLocked.Checked = settings.IsSessionLocked(today);
        _cooldownEnabled.Checked = settings.IsCooldownActive(today);
        _cooldownUntil.Value = (settings.CooldownUntilDate ?? today.AddDays(1)).ToDateTime(TimeOnly.MinValue);
    }

    private void SaveSettings()
    {
        _data.Settings.AppearanceMode = (AppearanceMode)_appearanceMode.SelectedItem!;
        _data.Settings.CurrencySymbol = string.IsNullOrWhiteSpace(_currency.Text) ? "\u20ac" : _currency.Text.Trim();
        _data.Settings.DefaultPlatform = (Platform)_defaultPlatform.SelectedItem!;
        _data.Settings.ActiveMonthStart = DateOnly.FromDateTime(_activeMonthStart.Value);
        _data.Settings.StartingBankroll = _startingBankroll.Value;
        _data.Settings.DefaultMaxBullets = (int)_defaultMaxBullets.Value;
        _data.Settings.ActiveReviewYear = (int)_activeReviewYear.Value;
        _data.Settings.NormalMttMaxRiskPercent = _normalMttRisk.Value;
        _data.Settings.SngHexaProMaxRiskPercent = _sngRisk.Value;
        _data.Settings.FlipMaxRiskPercent = _flipRisk.Value;
        _data.Settings.ShotTowerMaxRiskPercent = _shotRisk.Value;
        _data.Settings.CashSessionMaxRiskPercent = _cashRisk.Value;
        _data.Settings.ReviewRiskCapUsagePercent = _reviewRiskCapUsage.Value;
        _data.Settings.BudgetWarningPercent = _budgetWarning.Value;
        _data.Settings.DailyRiskCapPercent = _dailyRiskCap.Value;
        _data.Settings.ActiveExposureCapPercent = _activeExposureCap.Value;
        _data.Settings.StopLossWarningPercent = _stopLossWarning.Value;
        _data.Settings.CashReloadWarningPercent = _cashReloadWarning.Value;
        _data.Settings.DailyStopLossAmount = _dailyStopLoss.Value;
        _data.Settings.MonthlyPokerStopLossPercent = _monthlyStopLoss.Value;
        _data.Settings.ReserveTargetPercent = _reserveTarget.Value;
        _data.Settings.ProtectModeBelowBankroll = _protectBelow.Value;
        _data.Settings.MoveUpReviewBankroll = _moveUpBankroll.Value;
        _data.Settings.GreenLightShotBankroll = _greenLightBankroll.Value;
        _data.Settings.WithdrawalProfitLockThreshold = _profitLockThreshold.Value;
        var today = DateOnly.FromDateTime(DateTime.Today);
        if (_sessionLocked.Checked)
        {
            _data.Settings.LockSessionFor(today);
        }
        else
        {
            _data.Settings.ClearSessionLock();
        }

        if (_cooldownEnabled.Checked)
        {
            _data.Settings.SetCooldownUntil(DateOnly.FromDateTime(_cooldownUntil.Value));
        }
        else
        {
            _data.Settings.ClearCooldown();
        }
        _data.Settings.NormalizePlayLocks(today);
        _data.Settings.CategoryRules = ((BindingList<CategoryRuleSettings>)_categoryRulesSource.DataSource).ToList();
        var paletteChanged = Theme.Configure(_data.Settings.AppearanceMode);
        _repository.Save(_data);
        if (paletteChanged)
        {
            RebuildInterface();
        }
        else
        {
            RefreshAll();
        }

        _statusLabel.Text = $"Settings saved.  File: {_repository.FilePath}";
    }

    private void RebuildInterface()
    {
        SuspendLayout();
        var oldControls = Controls.Cast<Control>().ToList();
        Controls.Clear();
        foreach (var control in oldControls)
        {
            control.Dispose();
        }

        _kpiValues.Clear();
        InitializeComponent();
        RefreshAll();
        ResumeLayout();
    }


    private void LockToday()
    {
        _data.Settings.LockSessionFor(DateOnly.FromDateTime(DateTime.Today));
        SaveData("Session locked for today.");
    }

    private void CooldownTomorrow()
    {
        _data.Settings.ClearSessionLock();
        _data.Settings.SetCooldownUntil(DateOnly.FromDateTime(DateTime.Today.AddDays(1)));
        SaveData("Cooldown set until tomorrow.");
    }

    private void ClearLock()
    {
        _data.Settings.ClearPlayLocks();
        SaveData("Lock cleared.");
    }
}
