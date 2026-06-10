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

    private Control BuildWalletsTab()
    {
        var root = BuildGridShell(out var buttons);
        AddGridButton(buttons, "Reconcile", ReconcileSelectedWallet);
        AddGridButton(buttons, "Transfer", TransferBetweenWallets);
        AddGridButton(buttons, "Ledger", AddLedger);

        _walletGrid = CreateWalletGrid(_walletSource);
        root.Controls.Add(_walletGrid, 0, 1);
        return root;
    }


    private void ReconcileSelectedWallet()
    {
        if (Selected<PlatformSummary>(_walletSource) is not { } selected
            || !Enum.TryParse<Platform>(selected.Name, out var platform))
        {
            return;
        }

        ReconcileWallet(platform);
    }

    private void ReconcileWallet(Platform platform)
    {
        var selected = BankrollCalculator.GetPlatformSummaries(_data)
            .FirstOrDefault(summary => string.Equals(summary.Name, platform.ToString(), StringComparison.Ordinal));
        var displayName = selected?.Name ?? platform.ToString();
        var expectedCash = selected?.WalletCashBalance ?? 0m;
        var amount = PromptMoney(
            "Reconcile Wallet",
            $"Actual {displayName} cash",
            selected?.ActualCashBalance ?? Math.Max(0m, expectedCash),
            0m,
            1_000_000m);
        if (amount is null)
        {
            return;
        }

        var wallet = _data.PlatformWallets.FirstOrDefault(wallet => wallet.Platform == platform);
        if (wallet is null)
        {
            wallet = new PlatformWallet { Platform = platform };
            _data.PlatformWallets.Add(wallet);
        }

        wallet.ActualCashBalance = amount.Value;
        wallet.LastUpdatedDate = DateOnly.FromDateTime(DateTime.Today);
        SaveData($"{displayName} wallet reconciled.");
    }

    private void TransferBetweenWallets()
    {
        if (PromptWalletTransfer() is not { } transfer)
        {
            return;
        }

        var sharedId = Guid.NewGuid().ToString("N")[..8];
        _data.LedgerEntries.Add(new LedgerEntry
        {
            Date = transfer.Date,
            Type = LedgerType.TransferOut,
            Platform = transfer.From,
            Description = $"Transfer to {transfer.To} ({sharedId})",
            Amount = transfer.Amount,
            Category = TournamentCategory.Reserve,
            Notes = transfer.Notes
        });
        _data.LedgerEntries.Add(new LedgerEntry
        {
            Date = transfer.Date,
            Type = LedgerType.TransferIn,
            Platform = transfer.To,
            Description = $"Transfer from {transfer.From} ({sharedId})",
            Amount = transfer.Amount,
            Category = TournamentCategory.Reserve,
            Notes = transfer.Notes
        });

        SaveData($"Transferred {Money(transfer.Amount)} from {transfer.From} to {transfer.To}.");
    }

    private WalletTransferDraft? PromptWalletTransfer()
    {
        var selectedPlatform = Selected<PlatformSummary>(_walletSource) is { } selected
            && Enum.TryParse<Platform>(selected.Name, out var parsed)
                ? parsed
                : _data.Settings.DefaultPlatform;
        var transferPlatforms = PlatformCatalog.EnabledPlatforms(_data.Settings, selectedPlatform)
            .OrderBy(platform => platform.ToString(), NaturalSortComparer.Instance)
            .ToList();
        if (transferPlatforms.Count < 2)
        {
            transferPlatforms = Enum.GetValues<Platform>()
                .OrderBy(platform => platform.ToString(), NaturalSortComparer.Instance)
                .ToList();
        }

        var fallbackTo = transferPlatforms.First(platform => platform != selectedPlatform);
        WalletTransferDraft? result = null;

        using var form = new Form
        {
            Text = "Transfer Between Wallets",
            Size = new Size(540, 360),
            MinimumSize = new Size(500, 320),
            StartPosition = FormStartPosition.CenterParent,
            BackColor = Theme.Back,
            ForeColor = Theme.Text,
            Font = Theme.BodyFont
        };

        DateTimePicker transferDate = null!;
        ComboBox fromPlatform = null!;
        ComboBox toPlatform = null!;
        NumericUpDown transferAmount = null!;
        TextBox transferNotes = null!;
        var layout = DialogLayout.Create(form, () =>
        {
            var from = (Platform)fromPlatform.SelectedItem!;
            var to = (Platform)toPlatform.SelectedItem!;
            var amount = transferAmount.Value;
            var errors = new List<string>();
            if (from == to)
            {
                errors.Add("Transfer source and destination must be different.");
            }

            if (amount <= 0m)
            {
                errors.Add("Transfer amount must be greater than zero.");
            }

            if (DialogLayout.ShowErrors(errors))
            {
                return;
            }

            result = new WalletTransferDraft(
                from,
                to,
                DateOnly.FromDateTime(transferDate.Value),
                amount,
                transferNotes.Text.Trim());
            form.DialogResult = DialogResult.OK;
            form.Close();
        });

        transferDate = Theme.DatePicker(DateOnly.FromDateTime(DateTime.Today));
        fromPlatform = Theme.EnumBox(selectedPlatform, transferPlatforms);
        toPlatform = Theme.EnumBox(fallbackTo, transferPlatforms);
        transferAmount = Theme.MoneyBox(0m);
        transferAmount.Minimum = 0.01m;
        transferAmount.Value = 0.01m;
        transferNotes = Theme.TextBox(multiline: true);

        DialogLayout.AddRow(layout, "Date", transferDate);
        DialogLayout.AddRow(layout, "From", fromPlatform);
        DialogLayout.AddRow(layout, "To", toPlatform);
        DialogLayout.AddRow(layout, "Amount", transferAmount);
        DialogLayout.AddRow(layout, "Notes", transferNotes);

        return form.ShowDialog(this) == DialogResult.OK ? result : null;
    }

    private sealed record WalletTransferDraft(
        Platform From,
        Platform To,
        DateOnly Date,
        decimal Amount,
        string Notes);
}
