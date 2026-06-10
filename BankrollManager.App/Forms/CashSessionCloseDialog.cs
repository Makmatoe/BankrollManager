using BankrollManager.Core.Models;
using BankrollManager.Core.Services;
using BankrollManager.Core.Validation;

namespace BankrollManager.App.Forms;

internal sealed class CashSessionCloseDialog : Form
{
    private readonly BankrollSettings _settings;
    private readonly DateTimePicker _closedDate;
    private readonly DateTimePicker _closedTime;
    private readonly NumericUpDown _reloads;
    private readonly NumericUpDown _cashout;
    private readonly NumericUpDown _cashDropWon;
    private readonly NumericUpDown _jackpotFortunePrizeWon;
    private readonly NumericUpDown _minutes;
    private readonly NumericUpDown _hands;
    private readonly Label _thresholdWarning;
    private readonly Label _formatWarning;
    private readonly DialogLayout.Row _cashDropRow;
    private readonly DialogLayout.Row _jackpotRow;
    private readonly DialogLayout.Row _formatWarningRow;
    private readonly TextBox _notes;
    private bool _syncingMinutes;
    private bool _minutesEditedByUser;

    public CashSessionCloseDialog(CashSession entry, BankrollSettings settings)
    {
        _settings = settings;
        Entry = CashSessionDialogSupport.Clone(entry);
        _minutesEditedByUser = Entry.Minutes.HasValue;
        Text = "Close Cash Session";
        Size = new Size(620, 620);
        MinimumSize = new Size(560, 500);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Theme.Back;
        ForeColor = Theme.Text;
        Font = Theme.BodyFont;

        var layout = DialogLayout.Create(this, Save);
        var summary = Theme.Label(CashSessionDialogSupport.SessionSummary(Entry, settings), Theme.SmallFont, Theme.Muted);
        summary.MaximumSize = new Size(390, 0);
        _closedDate = Theme.DatePicker(Entry.ClosedDate ?? DateOnly.FromDateTime(DateTime.Today));
        _closedTime = Theme.TimePicker(Entry.ClosedTime ?? TimeOnly.FromDateTime(DateTime.Now));
        _reloads = Theme.MoneyBox(Entry.Reloads);
        _cashout = Theme.MoneyBox(Entry.Cashout > 0m ? Entry.Cashout : Entry.ActiveTableCash);
        _cashout.ValueChanged += (_, _) => UpdateThresholdWarning();
        _cashDropWon = Theme.MoneyBox(Entry.CashDropWon);
        _jackpotFortunePrizeWon = Theme.MoneyBox(Entry.JackpotFortunePrizeWon);
        _minutes = Theme.IntBox(Entry.Minutes ?? 0);
        _minutes.ValueChanged += (_, _) =>
        {
            if (!_syncingMinutes)
            {
                _minutesEditedByUser = true;
            }
        };
        _hands = Theme.IntBox(Entry.Hands ?? 0);
        _thresholdWarning = Theme.Label(string.Empty, Theme.SubHeaderFont, Theme.Warning);
        _thresholdWarning.MaximumSize = new Size(390, 0);
        _formatWarning = Theme.Label(string.Empty, Theme.SubHeaderFont, Theme.Warning);
        _formatWarning.MaximumSize = new Size(390, 0);
        _notes = Theme.TextBox(multiline: true);
        _notes.Text = Entry.Notes;
        CashSessionDialogSupport.EnforceNonNegative(_reloads, _cashout, _cashDropWon, _jackpotFortunePrizeWon);

        DialogLayout.AddRow(layout, "Session", summary);
        DialogLayout.AddRow(layout, "Close date", _closedDate);
        DialogLayout.AddRow(layout, "Close time", _closedTime);
        DialogLayout.AddRow(layout, "Reloads used", _reloads);
        DialogLayout.AddRow(layout, "Cashout", _cashout);
        _cashDropRow = DialogLayout.AddRow(layout, "Cash drop won", _cashDropWon);
        _jackpotRow = DialogLayout.AddRow(layout, "Jackpot/Fortune won", _jackpotFortunePrizeWon);
        DialogLayout.AddRow(layout, "Minutes", _minutes);
        DialogLayout.AddRow(layout, "Hands (0 unknown)", _hands);
        _formatWarningRow = DialogLayout.AddRow(layout, "Format warning", _formatWarning);
        DialogLayout.AddRow(layout, "Profit lock", _thresholdWarning);
        DialogLayout.AddRow(layout, "Notes", _notes);
        _closedDate.ValueChanged += (_, _) => AutoFillMinutes();
        _closedTime.ValueChanged += (_, _) => AutoFillMinutes();
        AutoFillMinutes();
        UpdateFormatRows();
        UpdateThresholdWarning();
    }

    public CashSession Entry { get; private set; }

    private void Save()
    {
        CashSessionWorkflowService.MarkClosed(
            Entry,
            new CashSessionCloseDetails(
                DateOnly.FromDateTime(_closedDate.Value),
                TimeOnly.FromDateTime(_closedTime.Value),
                _reloads.Value,
                _cashout.Value,
                CashSessionDialogSupport.NullableInt(_minutes),
                CashSessionDialogSupport.NullableInt(_hands),
                _notes.Text.Trim(),
                _cashDropWon.Value,
                _jackpotFortunePrizeWon.Value));

        var errors = EntryValidator.Validate(Entry);
        if (DialogLayout.ShowErrors(errors))
        {
            return;
        }

        if (_settings.WithdrawalProfitLockThreshold > 0m
            && Entry.Cashout >= _settings.WithdrawalProfitLockThreshold
            && string.IsNullOrWhiteSpace(Entry.Notes))
        {
            MessageBox.Show(
                "You reached your withdrawal/profit lock threshold. Leave table or record reason for continuing.",
                "Profit lock",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        DialogResult = DialogResult.OK;
        Close();
    }

    private void UpdateThresholdWarning()
    {
        if (_settings.WithdrawalProfitLockThreshold <= 0m)
        {
            _thresholdWarning.Text = "Profit lock warning is disabled.";
            _thresholdWarning.ForeColor = Theme.Muted;
            return;
        }

        if (_cashout.Value >= _settings.WithdrawalProfitLockThreshold)
        {
            _thresholdWarning.Text = "Threshold reached. Leave table or record reason for continuing.";
            _thresholdWarning.ForeColor = Theme.Warning;
            return;
        }

        _thresholdWarning.Text = $"Warn at cashout {_settings.CurrencySymbol}{_settings.WithdrawalProfitLockThreshold:0.00}";
        _thresholdWarning.ForeColor = Theme.Muted;
    }

    private void UpdateFormatRows()
    {
        var isRush = CashSessionDialogSupport.IsRushAndCash(Entry.Format);
        var isAof = CashSessionDialogSupport.IsAllInOrFold(Entry.Format);
        _cashDropRow.SetVisible(isRush || _cashDropWon.Value > 0m);
        _jackpotRow.SetVisible(isAof || _jackpotFortunePrizeWon.Value > 0m);
        _formatWarning.Text = CashSessionDialogSupport.BuildFormatWarning(Entry.Format);
        _formatWarningRow.SetVisible(!string.IsNullOrWhiteSpace(_formatWarning.Text));
    }

    private void AutoFillMinutes()
    {
        if (_minutesEditedByUser)
        {
            return;
        }

        var trackedMinutes = CashSessionWorkflowService.CalculateTrackedMinutes(
            Entry.Date,
            Entry.SessionTime,
            DateOnly.FromDateTime(_closedDate.Value),
            TimeOnly.FromDateTime(_closedTime.Value));
        if (trackedMinutes is null)
        {
            return;
        }

        _syncingMinutes = true;
        _minutes.Value = Math.Min(trackedMinutes.Value, (int)_minutes.Maximum);
        _syncingMinutes = false;
    }
}
