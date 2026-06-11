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

    private Control BuildDecisionTab()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            BackColor = Theme.Back,
            Padding = new Padding(12)
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 430));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var formScroller = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Theme.Back,
            Margin = new Padding(0)
        };
        root.Controls.Add(formScroller, 0, 0);

        var form = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            BackColor = Theme.Back
        };
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        formScroller.Controls.Add(form);

        _decisionIsCash = new CheckBox { Text = "Cash session", AutoSize = true, ForeColor = Theme.Text, Font = Theme.BodyFont };
        _decisionPlatform = Theme.EnumBox(
            _data.Settings.DefaultPlatform,
            PlatformCatalog.EnabledPlatforms(_data.Settings, _data.Settings.DefaultPlatform));
        _decisionCategory = Theme.EnumBox(
            TournamentCategory.MainGrind,
            PlatformCatalog.TournamentCategoriesFor(_data.Settings.DefaultPlatform));
        _decisionFormat = Theme.EnumBox(
            TournamentFormat.MTT,
            PlatformCatalog.TournamentFormatsFor(_data.Settings.DefaultPlatform));
        _decisionCashFormat = Theme.EnumBox(
            CashFormat.HoldemCash,
            PlatformCatalog.CashFormatsFor(_data.Settings.DefaultPlatform));
        _decisionEventName = Theme.TextBox();
        _decisionBuyIn = Theme.MoneyBox(0m);
        _decisionBullets = Theme.IntBox(_data.Settings.DefaultMaxBullets);
        _decisionAddOns = Theme.MoneyBox(0m);
        _decisionTicketBuyIn = Theme.MoneyBox(0m);
        _decisionCashBuyIn = Theme.MoneyBox(0m);
        _decisionCashReloads = Theme.MoneyBox(0m);
        _decisionNotes = Theme.TextBox(multiline: true);

        AddDecisionRow(form, "Mode", _decisionIsCash);
        AddDecisionRow(form, "Platform", _decisionPlatform);
        AddDecisionRow(form, "Category", _decisionCategory);
        AddDecisionRow(form, "Format", _decisionFormat);
        AddDecisionRow(form, "Cash format", _decisionCashFormat);
        AddDecisionRow(form, "Tournament/Event", _decisionEventName);
        AddDecisionRow(form, "Buy-in", _decisionBuyIn);
        AddDecisionRow(form, "Planned bullets", _decisionBullets);
        AddDecisionRow(form, "Add-ons/Rebuys", _decisionAddOns);
        AddDecisionRow(form, "Ticket buy-in", _decisionTicketBuyIn);
        AddDecisionRow(form, "Cash buy-in", _decisionCashBuyIn);
        AddDecisionRow(form, "Cash reloads", _decisionCashReloads);
        AddDecisionRow(form, "Notes", _decisionNotes);

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = true,
            BackColor = Theme.Back,
            Margin = new Padding(0)
        };

        var usePreset = Theme.Button("Use Preset");
        usePreset.Click += (_, _) => ApplyTournamentPresetToDecision();
        var guide = Theme.Button("GGPoker Guide");
        guide.Click += (_, _) => ShowGgPokerGuide();
        var evaluate = Theme.Button("Evaluate");
        evaluate.Click += (_, _) => RefreshDecision();
        _decisionRegisterButton = Theme.Button("Register MTT");
        _decisionRegisterButton.Click += (_, _) => RegisterTournamentFromDecision();
        _decisionStartCashButton = Theme.Button("Start Cash");
        _decisionStartCashButton.Click += (_, _) => StartCashFromDecision();
        actions.Controls.Add(usePreset);
        actions.Controls.Add(guide);
        actions.Controls.Add(evaluate);
        actions.Controls.Add(_decisionRegisterButton);
        actions.Controls.Add(_decisionStartCashButton);
        AddDecisionRow(form, string.Empty, actions);

        foreach (Control control in form.Controls)
        {
            switch (control)
            {
                case NumericUpDown numeric:
                    numeric.ValueChanged += (_, _) => DecisionSetupChanged();
                    break;
                case ComboBox combo when ReferenceEquals(combo, _decisionPlatform):
                    combo.SelectedIndexChanged += (_, _) =>
                    {
                        RefreshDecisionChoices(includeCurrent: false);
                        DecisionSetupChanged();
                    };
                    break;
                case ComboBox combo:
                    combo.SelectedIndexChanged += (_, _) => DecisionSetupChanged();
                    break;
                case CheckBox checkBox:
                    checkBox.CheckedChanged += (_, _) => DecisionSetupChanged();
                    break;
                case TextBox textBox when ReferenceEquals(textBox, _decisionEventName):
                    textBox.TextChanged += (_, _) => DecisionSetupChanged();
                    break;
                case TextBox textBox:
                    textBox.TextChanged += (_, _) => RefreshDecision();
                    break;
            }
        }

        var result = Theme.Card();
        result.Dock = DockStyle.Fill;
        result.Margin = new Padding(12, 0, 0, 0);
        var resultLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 7, BackColor = Theme.Panel };
        resultLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        resultLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        resultLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        resultLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
        resultLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
        resultLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
        resultLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
        result.Controls.Add(resultLayout);
        root.Controls.Add(result, 1, 0);

        _decisionLabel = Theme.Label("", new Font("Segoe UI", 22f, FontStyle.Bold), Theme.Text);
        _decisionRisk = Theme.Label("", Theme.SubHeaderFont, Theme.Muted);
        _decisionBudget = Theme.Label("", Theme.SubHeaderFont, Theme.Muted);
        ConfigureResultLabel(_decisionLabel, 60);
        ConfigureResultLabel(_decisionRisk, 34);
        ConfigureResultLabel(_decisionBudget, 34);
        _decisionExplanation = BuildResultTextBox();
        _decisionAlternative = BuildResultTextBox();
        _decisionThresholds = BuildResultTextBox();
        _decisionWarnings = BuildResultTextBox();
        resultLayout.Controls.Add(_decisionLabel, 0, 0);
        resultLayout.Controls.Add(_decisionRisk, 0, 1);
        resultLayout.Controls.Add(_decisionBudget, 0, 2);
        resultLayout.Controls.Add(_decisionExplanation, 0, 3);
        resultLayout.Controls.Add(_decisionAlternative, 0, 4);
        resultLayout.Controls.Add(_decisionThresholds, 0, 5);
        resultLayout.Controls.Add(_decisionWarnings, 0, 6);

        return root;
    }

    private void RefreshDecisionChoices(bool includeCurrent)
    {
        if (_decisionPlatform.SelectedItem is not Platform platform)
        {
            return;
        }

        var selectedCategory = _decisionCategory.SelectedItem is TournamentCategory category
            ? category
            : TournamentCategory.MainGrind;
        var selectedFormat = _decisionFormat.SelectedItem is TournamentFormat format
            ? format
            : TournamentFormat.MTT;
        var selectedCashFormat = _decisionCashFormat.SelectedItem is CashFormat cashFormat
            ? cashFormat
            : CashFormat.HoldemCash;

        Theme.SetEnumBoxItems(
            _decisionCategory,
            PlatformCatalog.TournamentCategoriesFor(platform),
            selectedCategory,
            includeCurrent);
        Theme.SetEnumBoxItems(
            _decisionFormat,
            PlatformCatalog.TournamentFormatsFor(platform),
            selectedFormat,
            includeCurrent);
        Theme.SetEnumBoxItems(
            _decisionCashFormat,
            PlatformCatalog.CashFormatsFor(platform),
            selectedCashFormat,
            includeCurrent);
    }

    private void RefreshDecisionPlatformChoices(bool includeCurrent)
    {
        if (_decisionPlatform is null)
        {
            return;
        }

        var selectedPlatform = _decisionPlatform.SelectedItem is Platform platform
            ? platform
            : _data.Settings.DefaultPlatform;
        Theme.SetEnumBoxItems(
            _decisionPlatform,
            PlatformCatalog.EnabledPlatforms(_data.Settings, selectedPlatform),
            selectedPlatform,
            includeCurrent);
        RefreshDecisionChoices(includeCurrent);
    }

    private static void ConfigureResultLabel(Label label, int minimumHeight)
    {
        label.AutoSize = false;
        label.AutoEllipsis = true;
        label.Dock = DockStyle.Top;
        label.Height = minimumHeight;
        label.Margin = new Padding(0, 0, 0, 6);
        label.TextAlign = ContentAlignment.MiddleLeft;
    }


    private void RefreshDecision()
    {
        if (_decisionLabel is null)
        {
            return;
        }

        var request = BuildDecisionRequest();

        var isCash = request.IsCashSession;
        _decisionCategory.Enabled = !isCash;
        _decisionFormat.Enabled = !isCash;
        _decisionCashFormat.Enabled = isCash;
        _decisionEventName.Enabled = !isCash;
        _decisionBuyIn.Enabled = !isCash;
        _decisionBullets.Enabled = !isCash;
        _decisionAddOns.Enabled = !isCash;
        _decisionTicketBuyIn.Enabled = !isCash;
        _decisionCashBuyIn.Enabled = isCash;
        _decisionCashReloads.Enabled = isCash;

        var result = RuleEngine.Evaluate(_data, request, DateOnly.FromDateTime(DateTime.Today));
        _decisionLabel.Text = result.DisplayLabel;
        _decisionLabel.ForeColor = LabelColor(result.Label);
        _decisionRisk.Text = $"Risk: {Money(result.TotalPlannedRisk)} ({result.RiskPercent:0.0}% of cash bankroll {Money(result.CurrentBankroll)})";
        _decisionBudget.Text = $"Category budget remaining: {Money(result.CategoryBudgetRemaining)}";
        _decisionExplanation.Text = $"Explanation:{Environment.NewLine}{result.Explanation}";
        _decisionAlternative.Text = $"Safer alternative:{Environment.NewLine}{result.SuggestedSaferAlternative}";
        _decisionThresholds.Text = result.Thresholds.Count == 0
            ? "Thresholds:\r\nNone"
            : $"Thresholds:{Environment.NewLine}{string.Join(Environment.NewLine, result.Thresholds.Select(threshold => "- " + threshold))}";
        _decisionWarnings.Text = result.Warnings.Count == 0
            ? "Warnings:\r\nNone"
            : $"Warnings:{Environment.NewLine}{string.Join(Environment.NewLine, result.Warnings.Select(warning => "- " + warning))}";
        var canLogDecision = IsActionableDecision(result.Label);
        _decisionRegisterButton.Enabled = !isCash && canLogDecision;
        _decisionStartCashButton.Enabled = isCash && canLogDecision && request.CashBuyIn > 0m;
    }

    private void DecisionSetupChanged()
    {
        if (!_decisionApplyingPreset)
        {
            _decisionAppliedPreset = null;
        }

        RefreshDecision();
    }

    private void ApplyTournamentPresetToDecision()
    {
        if (_data.TournamentPresets.Count == 0)
        {
            MessageBox.Show(
                "Save a tournament as a preset first.",
                "No presets",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        if (PromptTournamentPreset() is not { } preset)
        {
            return;
        }

        var displayName = TournamentPresetService.DisplayName(preset, _data.Settings);
        _decisionApplyingPreset = true;
        try
        {
            _decisionAppliedPreset = preset;
            _decisionIsCash.Checked = false;
            Theme.SelectEnumBoxItem(_decisionPlatform, preset.Platform);
            RefreshDecisionChoices(includeCurrent: true);
            Theme.SelectEnumBoxItem(_decisionCategory, preset.Category);
            Theme.SelectEnumBoxItem(_decisionFormat, preset.Format);
            Theme.SelectEnumBoxItem(_decisionCashFormat, CashFormat.HoldemCash);
            _decisionEventName.Text = string.IsNullOrWhiteSpace(preset.EventName) ? preset.Name : preset.EventName;
            _decisionBuyIn.Value = ClampToBox(_decisionBuyIn, preset.BuyIn);
            _decisionBullets.Value = ClampToBox(_decisionBullets, Math.Max(1, preset.PlannedBullets));
            _decisionAddOns.Value = ClampToBox(_decisionAddOns, preset.AddOnsRebuys);
            _decisionTicketBuyIn.Value = ClampToBox(_decisionTicketBuyIn, preset.TicketBuyInValue);
            _decisionCashBuyIn.Value = ClampToBox(_decisionCashBuyIn, 0m);
            _decisionCashReloads.Value = ClampToBox(_decisionCashReloads, 0m);

            if (string.IsNullOrWhiteSpace(_decisionNotes.Text) && !string.IsNullOrWhiteSpace(preset.Notes))
            {
                _decisionNotes.Text = preset.Notes;
            }
        }
        finally
        {
            _decisionApplyingPreset = false;
        }

        RefreshDecision();
        _statusLabel.Text = $"Decision preset loaded: {displayName}.";
    }

    private DecisionRequest BuildDecisionRequest()
    {
        return new DecisionRequest
        {
            IsCashSession = _decisionIsCash.Checked,
            Platform = (Platform)_decisionPlatform.SelectedItem!,
            Category = (TournamentCategory)_decisionCategory.SelectedItem!,
            Format = (TournamentFormat)_decisionFormat.SelectedItem!,
            CashFormat = (CashFormat)_decisionCashFormat.SelectedItem!,
            BuyIn = _decisionBuyIn.Value,
            PlannedBullets = (int)_decisionBullets.Value,
            AddOnsRebuys = _decisionAddOns.Value,
            TicketBuyInValue = _decisionTicketBuyIn.Value,
            CashBuyIn = _decisionCashBuyIn.Value,
            CashReloads = _decisionCashReloads.Value,
            Notes = _decisionNotes.Text.Trim()
        };
    }

    private void RegisterTournamentFromDecision()
    {
        var request = BuildDecisionRequest();
        var result = RuleEngine.Evaluate(_data, request, DateOnly.FromDateTime(DateTime.Today));
        if (request.IsCashSession || !ConfirmDecisionAction(result, "register this tournament"))
        {
            return;
        }

        var now = DateTime.Now;
        var entry = _decisionAppliedPreset is { } preset
            ? TournamentPresetService.CreateEntry(preset, now)
            : new TournamentEntry();
        entry.Date = DateOnly.FromDateTime(now);
        entry.RegistrationTime = TimeOnly.FromDateTime(now);
        entry.Status = TournamentStatus.Registered;
        entry.Platform = request.Platform;
        entry.Category = request.Category;
        entry.Format = request.Format;
        entry.EventName = _decisionEventName.Text.Trim();
        entry.BuyIn = request.BuyIn;
        entry.PlannedBullets = request.PlannedBullets;
        entry.ActualBullets = request.PlannedBullets;
        entry.AddOnsRebuys = request.AddOnsRebuys;
        entry.TicketBuyInValue = request.TicketBuyInValue;
        entry.PreGameFocus = result.DisplayLabel;
        entry.Tags = AppendTag(entry.Tags, "Decision");
        entry.Notes = BuildDecisionAuditNotes(request, result);

        using var dialog = new TournamentEntryDialog(entry, _data.Settings);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _data.TournamentEntries.Add(dialog.Entry);
        SaveData("Tournament registered from decision.");
    }

    private void StartCashFromDecision()
    {
        var request = BuildDecisionRequest();
        var result = RuleEngine.Evaluate(_data, request, DateOnly.FromDateTime(DateTime.Today));
        if (!request.IsCashSession || !ConfirmDecisionAction(result, "start this cash session"))
        {
            return;
        }

        if (request.CashBuyIn <= 0m)
        {
            MessageBox.Show(
                "Enter a cash buy-in before starting a session from Decide.",
                "Cash buy-in required",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        var entry = CashSessionWorkflowService.CreateActiveDraft(DateTime.Now, request.Platform);
        entry.Format = request.CashFormat;
        entry.StartStackBuyIn = request.CashBuyIn;
        entry.ReloadCap = request.CashReloads;
        entry.Notes = BuildDecisionAuditNotes(request, result);

        using var dialog = new CashSessionStartDialog(entry, _data.Settings);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _data.CashSessions.Add(dialog.Entry);
        SaveData("Cash session started from decision.");
    }

    private void ShowGgPokerGuide()
    {
        MessageBox.Show(
            string.Join(Environment.NewLine,
            [
                "Spin & Gold: log multiplier, insurance, prize, and placement.",
                "Flip & Go: log buy-in per stack and number of stacks.",
                "Mystery Bounty: log cash prize and mystery bounty prize separately.",
                "PKO: log bounty winnings separately from normal prize.",
                "Mystery Battle Royale: log rush stage, final table, bounties, and placement.",
                "Rush & Cash: log as fast-fold cash and respect faster stop-loss pressure.",
                "All-In or Fold: log as high-variance cash or SNG.",
                "Satellites/WSOP Express: log ticket value separately from cash."
            ]),
            "GGPoker quick guide",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private bool ConfirmDecisionAction(DecisionResult result, string action)
    {
        if (!IsActionableDecision(result.Label))
        {
            MessageBox.Show(
                $"{result.DisplayLabel}: {result.Explanation}",
                "Decision not actionable",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return false;
        }

        if (result.Label == DecisionLabel.PlayOk)
        {
            return true;
        }

        return MessageBox.Show(
            $"{result.DisplayLabel}: {result.Explanation}{Environment.NewLine}{Environment.NewLine}Continue to {action}?",
            "Confirm decision",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning) == DialogResult.Yes;
    }

    private static bool IsActionableDecision(DecisionLabel label)
    {
        return label is DecisionLabel.PlayOk or DecisionLabel.Review or DecisionLabel.ShotOk or DecisionLabel.ShotOnly;
    }

    private string BuildDecisionAuditNotes(DecisionRequest request, DecisionResult result)
    {
        var lines = new List<string>
        {
            $"Decision: {result.DisplayLabel}",
            $"Risk: {Money(result.TotalPlannedRisk)} ({result.RiskPercent:0.0}% of cash bankroll {Money(result.CurrentBankroll)})",
            request.IsCashSession
                ? $"Cash format: {request.CashFormat}"
                : $"Tournament format: {request.Format}",
            $"Category budget remaining: {Money(result.CategoryBudgetRemaining)}",
            $"Explanation: {result.Explanation}"
        };

        if (result.Warnings.Count > 0)
        {
            lines.Add($"Warnings: {string.Join(" | ", result.Warnings)}");
        }

        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            lines.Add($"Notes: {request.Notes.Trim()}");
        }

        return string.Join(Environment.NewLine, lines);
    }
}
