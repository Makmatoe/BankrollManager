using System.Globalization;
using BankrollManager.Core.Models;
using BankrollManager.Core.Services;
using BankrollManager.Core.Validation;

namespace BankrollManager.App.Forms;

internal sealed class TournamentBulkFinishDialog : Form
{
    private readonly IReadOnlyList<TournamentEntry> _candidates;
    private readonly CheckedListBox _entries;
    private readonly DateTimePicker _finishedDate;
    private readonly DateTimePicker _finishedTime;
    private readonly ComboBox _resultKind;
    private readonly NumericUpDown _resultAmount;

    public TournamentBulkFinishDialog(IReadOnlyList<TournamentEntry> candidates, BankrollSettings settings)
    {
        _candidates = candidates;
        settings.EnsureDefaults();
        FinishRequests = [];

        Text = "Bulk Finish Tournaments";
        Size = new Size(720, 560);
        MinimumSize = new Size(620, 460);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Theme.Back;
        ForeColor = Theme.Text;
        Font = Theme.BodyFont;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            BackColor = Theme.Back
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        Controls.Add(root);

        var fields = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            BackColor = Theme.Panel,
            Padding = new Padding(12)
        };
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.Controls.Add(fields, 0, 0);

        var defaultFinish = DateTime.Now;
        _finishedDate = Theme.DatePicker(DateOnly.FromDateTime(defaultFinish));
        _finishedTime = Theme.TimePicker(TimeOnly.FromDateTime(defaultFinish));
        _resultKind = Theme.EnumBox(DefaultResultKind(candidates));
        _resultAmount = Theme.MoneyBox(0m);
        _resultAmount.Minimum = 0m;

        DialogLayout.AddRow(fields, "Finished date", _finishedDate);
        DialogLayout.AddRow(fields, "Finished time", _finishedTime);
        DialogLayout.AddRow(fields, "Result kind", _resultKind);
        DialogLayout.AddRow(fields, "Result amount", _resultAmount);

        _entries = new CheckedListBox
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.PanelAlt,
            ForeColor = Theme.Text,
            BorderStyle = BorderStyle.FixedSingle,
            CheckOnClick = true,
            Font = Theme.BodyFont
        };
        foreach (var candidate in candidates)
        {
            _entries.Items.Add(new TournamentBulkFinishListItem(candidate, DisplayName(candidate, settings)), isChecked: true);
        }

        root.Controls.Add(_entries, 0, 1);

        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(8),
            BackColor = Theme.Panel
        };
        root.Controls.Add(footer, 0, 2);

        var save = Theme.Button("Finish selected");
        save.Click += (_, _) => Save();
        var selectAll = Theme.Button("Select all");
        selectAll.Click += (_, _) => SetAllChecked(true);
        var clear = Theme.Button("Clear");
        clear.Click += (_, _) => SetAllChecked(false);
        var cancel = Theme.Button("Cancel");
        cancel.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        footer.Controls.Add(save);
        footer.Controls.Add(selectAll);
        footer.Controls.Add(clear);
        footer.Controls.Add(cancel);
        AcceptButton = save;
        CancelButton = cancel;
    }

    public IReadOnlyList<(Guid EntryId, TournamentFinishRequest Request)> FinishRequests { get; private set; }

    private void SetAllChecked(bool isChecked)
    {
        for (var index = 0; index < _entries.Items.Count; index++)
        {
            _entries.SetItemChecked(index, isChecked);
        }
    }

    private void Save()
    {
        var requests = new List<(Guid EntryId, TournamentFinishRequest Request)>();
        var errors = new List<string>();
        foreach (var item in _entries.CheckedItems.OfType<TournamentBulkFinishListItem>())
        {
            var request = new TournamentFinishRequest
            {
                FinishedDate = DateOnly.FromDateTime(_finishedDate.Value),
                FinishedTime = TimeOnly.FromDateTime(_finishedTime.Value),
                ResultKind = (TournamentQuickResultKind)_resultKind.SelectedItem!,
                ResultAmount = _resultAmount.Value,
                ITM = _resultAmount.Value > 0m,
                FlipPhaseWon = _resultAmount.Value > 0m,
                GoPhaseReached = _resultAmount.Value > 0m
            };
            var draft = Clone(item.Entry);
            TournamentPresetService.ApplyFinish(draft, request);
            errors.AddRange(EntryValidator.Validate(draft)
                .Select(error => $"{item.Text}: {error}"));
            requests.Add((item.Entry.Id, request));
        }

        if (requests.Count == 0)
        {
            errors.Add("Select at least one tournament to finish.");
        }

        if (DialogLayout.ShowErrors(errors))
        {
            return;
        }

        FinishRequests = requests;
        DialogResult = DialogResult.OK;
        Close();
    }

    private static TournamentQuickResultKind DefaultResultKind(IReadOnlyList<TournamentEntry> candidates)
    {
        return candidates.Count > 0 && candidates.All(entry => entry.Format is TournamentFormat.Satellite
                or TournamentFormat.TurboSatellite
                or TournamentFormat.TargetStackSatellite
                or TournamentFormat.FlashSatellite
                or TournamentFormat.WSOPExpress)
            ? TournamentQuickResultKind.TicketWon
            : TournamentQuickResultKind.CashPrize;
    }

    private static string DisplayName(TournamentEntry entry, BankrollSettings settings)
    {
        var eventName = string.IsNullOrWhiteSpace(entry.EventName) ? entry.Format.ToString() : entry.EventName;
        var time = entry.RegistrationTime?.ToString("HH\\:mm", CultureInfo.CurrentCulture) ?? "--:--";
        return $"{entry.Date:yyyy-MM-dd} {time} | {Money(entry.TotalCost, settings)} | {entry.Platform} | {eventName}";
    }

    private static string Money(decimal value, BankrollSettings settings)
    {
        var sign = value < 0m ? "-" : string.Empty;
        var currency = string.IsNullOrWhiteSpace(settings.CurrencySymbol) ? "\u20ac" : settings.CurrencySymbol;
        return $"{sign}{currency}{Math.Abs(value):0.00}";
    }

    private static TournamentEntry Clone(TournamentEntry entry)
    {
        return new TournamentEntry
        {
            Id = entry.Id,
            Date = entry.Date,
            RegistrationTime = entry.RegistrationTime,
            Status = entry.Status,
            FinishedDate = entry.FinishedDate,
            FinishedTime = entry.FinishedTime,
            Platform = entry.Platform,
            Category = entry.Category,
            Format = entry.Format,
            EventName = entry.EventName,
            Currency = entry.Currency,
            EventTag = entry.EventTag,
            IsPromoFreebieTicketEvent = entry.IsPromoFreebieTicketEvent,
            BuyIn = entry.BuyIn,
            FeeRake = entry.FeeRake,
            PlannedBullets = entry.PlannedBullets,
            ActualBullets = entry.ActualBullets,
            AddOnsRebuys = entry.AddOnsRebuys,
            TicketBuyInValue = entry.TicketBuyInValue,
            TicketBuyInPlatform = entry.TicketBuyInPlatform,
            FlipBuyInPerStack = entry.FlipBuyInPerStack,
            FlipStacksBought = entry.FlipStacksBought,
            TargetEventName = entry.TargetEventName,
            TargetEventBuyIn = entry.TargetEventBuyIn,
            FieldSize = entry.FieldSize,
            PreGameFocus = entry.PreGameFocus,
            Tags = entry.Tags,
            Notes = entry.Notes
        };
    }

    private sealed record TournamentBulkFinishListItem(TournamentEntry Entry, string Text)
    {
        public override string ToString()
        {
            return Text;
        }
    }
}
