using BankrollManager.Core.Models;
using BankrollManager.Core.Services;
using BankrollManager.Core.Validation;

namespace BankrollManager.App.Forms;

internal sealed class CashSessionDialog : Form
{
    private readonly BankrollSettings _settings;
    private readonly DateTimePicker _date;
    private readonly DateTimePicker _sessionTime;
    private readonly ComboBox _status;
    private readonly DateTimePicker _closedDate;
    private readonly DateTimePicker _closedTime;
    private readonly ComboBox _platform;
    private readonly ComboBox _format;
    private readonly TextBox _game;
    private readonly TextBox _stakes;
    private readonly NumericUpDown _smallBlind;
    private readonly NumericUpDown _bigBlind;
    private readonly NumericUpDown _startStackBuyIn;
    private readonly NumericUpDown _reloads;
    private readonly NumericUpDown _reloadCap;
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

    public CashSessionDialog(CashSession entry, BankrollSettings settings)
    {
        _settings = settings;
        Entry = CashSessionDialogSupport.Clone(entry);
        _minutesEditedByUser = Entry.Minutes.HasValue;
        Text = Entry.Id == Guid.Empty ? "Add Cash Session" : "Cash Session";
        Size = new Size(650, 760);
        MinimumSize = new Size(600, 620);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Theme.Back;
        ForeColor = Theme.Text;
        Font = Theme.BodyFont;

        var layout = DialogLayout.Create(this, Save);
        _date = Theme.DatePicker(Entry.Date);
        _sessionTime = Theme.TimePicker(Entry.SessionTime ?? TimeOnly.FromDateTime(DateTime.Now));
        _status = Theme.EnumBox(Entry.Status);
        _closedDate = Theme.DatePicker(Entry.ClosedDate ?? Entry.Date);
        _closedTime = Theme.TimePicker(Entry.ClosedTime ?? Entry.SessionTime ?? TimeOnly.FromDateTime(DateTime.Now));
        _platform = Theme.EnumBox(Entry.Platform);
        _format = Theme.EnumBox(Entry.Format);
        _game = Theme.TextBox();
        _game.Text = Entry.Game;
        _stakes = Theme.TextBox();
        _stakes.Text = Entry.Stakes;
        _smallBlind = Theme.MoneyBox(Entry.SmallBlindAmount);
        _bigBlind = Theme.MoneyBox(Entry.BigBlindAmount);
        _startStackBuyIn = Theme.MoneyBox(Entry.StartStackBuyIn);
        _reloads = Theme.MoneyBox(Entry.Reloads);
        _reloadCap = Theme.MoneyBox(Entry.ReloadCap);
        _cashout = Theme.MoneyBox(Entry.Cashout);
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
        _thresholdWarning.MaximumSize = new Size(460, 0);
        _formatWarning = Theme.Label(string.Empty, Theme.SubHeaderFont, Theme.Warning);
        _formatWarning.MaximumSize = new Size(460, 0);
        _notes = Theme.TextBox(multiline: true);
        _notes.Text = Entry.Notes;

        DialogLayout.AddRow(layout, "Date", _date);
        DialogLayout.AddRow(layout, "Session time", _sessionTime);
        DialogLayout.AddRow(layout, "Status", _status);
        DialogLayout.AddRow(layout, "Closed date", _closedDate);
        DialogLayout.AddRow(layout, "Closed time", _closedTime);
        DialogLayout.AddRow(layout, "Platform", _platform);
        DialogLayout.AddRow(layout, "Cash format", _format);
        DialogLayout.AddRow(layout, "Game", _game);
        DialogLayout.AddRow(layout, "Stakes", _stakes);
        DialogLayout.AddRow(layout, "Small blind", _smallBlind);
        DialogLayout.AddRow(layout, "Big blind", _bigBlind);
        DialogLayout.AddRow(layout, "Start stack/buy-in", _startStackBuyIn);
        DialogLayout.AddRow(layout, "Reloads", _reloads);
        DialogLayout.AddRow(layout, "Reload cap", _reloadCap);
        DialogLayout.AddRow(layout, "Cashout", _cashout);
        _cashDropRow = DialogLayout.AddRow(layout, "Cash drop won", _cashDropWon);
        _jackpotRow = DialogLayout.AddRow(layout, "Jackpot/Fortune won", _jackpotFortunePrizeWon);
        DialogLayout.AddRow(layout, "Minutes", _minutes);
        DialogLayout.AddRow(layout, "Hands (0 unknown)", _hands);
        _formatWarningRow = DialogLayout.AddRow(layout, "Format warning", _formatWarning);
        DialogLayout.AddRow(layout, "Profit lock", _thresholdWarning);
        DialogLayout.AddRow(layout, "Notes", _notes);
        CashSessionDialogSupport.EnforceNonNegative(_smallBlind, _bigBlind, _startStackBuyIn, _reloads, _reloadCap, _cashout, _cashDropWon, _jackpotFortunePrizeWon);
        _date.ValueChanged += (_, _) => AutoFillMinutes();
        _sessionTime.ValueChanged += (_, _) => AutoFillMinutes();
        _status.SelectedIndexChanged += (_, _) => UpdateStatusControls();
        _closedDate.ValueChanged += (_, _) => AutoFillMinutes();
        _closedTime.ValueChanged += (_, _) => AutoFillMinutes();
        _format.SelectedIndexChanged += (_, _) => UpdateGgCashControls();
        UpdateStatusControls();
        AutoFillMinutes();
        UpdateThresholdWarning();
    }

    public CashSession Entry { get; private set; }

    private void Save()
    {
        Entry.Date = DateOnly.FromDateTime(_date.Value);
        Entry.SessionTime = TimeOnly.FromDateTime(_sessionTime.Value);
        Entry.Status = (CashSessionStatus)_status.SelectedItem!;
        Entry.Platform = (Platform)_platform.SelectedItem!;
        Entry.Format = (CashFormat)_format.SelectedItem!;
        Entry.Game = _game.Text.Trim();
        Entry.Stakes = _stakes.Text.Trim();
        Entry.SmallBlindAmount = _smallBlind.Value;
        Entry.BigBlindAmount = _bigBlind.Value;
        Entry.StartStackBuyIn = _startStackBuyIn.Value;
        Entry.Reloads = _reloads.Value;
        Entry.ReloadCap = _reloadCap.Value;
        Entry.CashDropWon = _cashDropWon.Value;
        Entry.JackpotFortunePrizeWon = _jackpotFortunePrizeWon.Value;
        Entry.Notes = _notes.Text.Trim();
        if (Entry.Status == CashSessionStatus.Finished)
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
        }
        else
        {
            CashSessionWorkflowService.MarkActive(Entry);
        }

        var errors = EntryValidator.Validate(Entry);
        if (DialogLayout.ShowErrors(errors))
        {
            return;
        }

        if (_settings.WithdrawalProfitLockThreshold > 0m
            && Entry.Status == CashSessionStatus.Finished
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
        if (_status.SelectedItem is CashSessionStatus.Active)
        {
            _thresholdWarning.Text = "Close the session to evaluate profit lock.";
            _thresholdWarning.ForeColor = Theme.Muted;
        }
        else if (_settings.WithdrawalProfitLockThreshold > 0m && _cashout.Value >= _settings.WithdrawalProfitLockThreshold)
        {
            _thresholdWarning.Text = "Threshold reached. Leave table or record reason for continuing.";
            _thresholdWarning.ForeColor = Theme.Warning;
        }
        else
        {
            _thresholdWarning.Text = _settings.WithdrawalProfitLockThreshold > 0m
                ? $"Warn at cashout {_settings.CurrencySymbol}{_settings.WithdrawalProfitLockThreshold:0.00}"
                : "Profit lock warning is disabled.";
            _thresholdWarning.ForeColor = Theme.Muted;
        }
    }

    private void UpdateStatusControls()
    {
        var isFinished = _status.SelectedItem is CashSessionStatus.Finished;
        _closedDate.Enabled = isFinished;
        _closedTime.Enabled = isFinished;
        _cashout.Enabled = isFinished;
        _minutes.Enabled = isFinished;
        _hands.Enabled = isFinished;
        _reloadCap.Enabled = !isFinished;
        _cashDropWon.Enabled = isFinished;
        _jackpotFortunePrizeWon.Enabled = isFinished;
        AutoFillMinutes();
        UpdateGgCashControls();
        UpdateThresholdWarning();
    }

    private void UpdateGgCashControls()
    {
        if (_format.SelectedItem is not CashFormat format)
        {
            return;
        }

        var isFinished = _status.SelectedItem is CashSessionStatus.Finished;
        var isRush = CashSessionDialogSupport.IsRushAndCash(format);
        var isAof = CashSessionDialogSupport.IsAllInOrFold(format);
        _cashDropRow.SetVisible(isFinished && (isRush || _cashDropWon.Value > 0m));
        _jackpotRow.SetVisible(isFinished && (isAof || _jackpotFortunePrizeWon.Value > 0m));
        _formatWarning.Text = CashSessionDialogSupport.BuildFormatWarning(format);
        _formatWarningRow.SetVisible(!string.IsNullOrWhiteSpace(_formatWarning.Text));
    }

    private void AutoFillMinutes()
    {
        if (_minutesEditedByUser || _status.SelectedItem is not CashSessionStatus.Finished)
        {
            return;
        }

        var trackedMinutes = CashSessionWorkflowService.CalculateTrackedMinutes(
            DateOnly.FromDateTime(_date.Value),
            TimeOnly.FromDateTime(_sessionTime.Value),
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
