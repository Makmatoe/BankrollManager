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

    private static T? Selected<T>(BindingSource source) where T : class
    {
        return source.Current as T;
    }

    private static bool ConfirmDelete(string name)
    {
        return MessageBox.Show(
            $"Delete selected {name}?",
            "Confirm delete",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning) == DialogResult.Yes;
    }

    private decimal? PromptMoney(string title, string label, decimal initialValue, decimal minimum, decimal maximum)
    {
        maximum = Math.Max(minimum, maximum);
        var result = (decimal?)null;
        using var form = new Form
        {
            Text = title,
            Size = new Size(420, 180),
            MinimumSize = new Size(380, 170),
            StartPosition = FormStartPosition.CenterParent,
            BackColor = Theme.Back,
            ForeColor = Theme.Text,
            Font = Theme.BodyFont
        };

        var input = Theme.MoneyBox(0m);
        input.Minimum = minimum;
        input.Maximum = maximum;
        input.Value = ClampToBox(input, initialValue);

        var layout = DialogLayout.Create(form, () =>
        {
            result = input.Value;
            form.DialogResult = DialogResult.OK;
            form.Close();
        });
        DialogLayout.AddRow(layout, label, input);

        return form.ShowDialog(this) == DialogResult.OK ? result : null;
    }

    private string? PromptText(string title, string label, string initialValue)
    {
        var result = (string?)null;
        using var form = new Form
        {
            Text = title,
            Size = new Size(500, 190),
            MinimumSize = new Size(440, 180),
            StartPosition = FormStartPosition.CenterParent,
            BackColor = Theme.Back,
            ForeColor = Theme.Text,
            Font = Theme.BodyFont
        };

        var input = Theme.TextBox();
        input.Text = initialValue;
        input.SelectAll();

        var layout = DialogLayout.Create(form, () =>
        {
            var value = input.Text.Trim();
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(value))
            {
                errors.Add("Preset name is required.");
            }

            if (DialogLayout.ShowErrors(errors))
            {
                return;
            }

            result = value;
            form.DialogResult = DialogResult.OK;
            form.Close();
        });
        DialogLayout.AddRow(layout, label, input);

        return form.ShowDialog(this) == DialogResult.OK ? result : null;
    }

    private TournamentPreset? PromptTournamentPreset()
    {
        var items = BuildTournamentPresetItems();
        if (items.Length == 0)
        {
            return null;
        }

        TournamentPreset? result = null;
        using var form = new Form
        {
            Text = "Use Tournament Preset",
            Size = new Size(620, 210),
            MinimumSize = new Size(540, 190),
            StartPosition = FormStartPosition.CenterParent,
            BackColor = Theme.Back,
            ForeColor = Theme.Text,
            Font = Theme.BodyFont
        };

        var presets = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Theme.PanelAlt,
            ForeColor = Theme.Text,
            FlatStyle = FlatStyle.Flat,
            Font = Theme.BodyFont
        };
        presets.Items.AddRange(items);
        presets.SelectedIndex = 0;

        var layout = DialogLayout.Create(form, () =>
        {
            if (presets.SelectedItem is not TournamentPresetListItem selected)
            {
                return;
            }

            result = selected.Preset;
            form.DialogResult = DialogResult.OK;
            form.Close();
        });
        DialogLayout.AddRow(layout, "Preset", presets);

        return form.ShowDialog(this) == DialogResult.OK ? result : null;
    }

    private TournamentQuickAddSetup? PromptTournamentQuickAddSetup()
    {
        var items = BuildTournamentPresetItems();
        if (items.Length == 0)
        {
            return null;
        }

        TournamentQuickAddSetup? result = null;
        using var form = new Form
        {
            Text = "Quick Add Tournaments",
            Size = new Size(640, 260),
            MinimumSize = new Size(560, 230),
            StartPosition = FormStartPosition.CenterParent,
            BackColor = Theme.Back,
            ForeColor = Theme.Text,
            Font = Theme.BodyFont
        };

        var presets = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Theme.PanelAlt,
            ForeColor = Theme.Text,
            FlatStyle = FlatStyle.Flat,
            Font = Theme.BodyFont
        };
        presets.Items.AddRange(items);
        presets.SelectedIndex = 0;

        var count = Theme.IntBox(1, 200);
        count.Minimum = 1m;
        count.Maximum = 200m;
        count.Value = 1m;

        var layout = DialogLayout.Create(form, () =>
        {
            if (presets.SelectedItem is not TournamentPresetListItem selected)
            {
                return;
            }

            result = new TournamentQuickAddSetup(selected.Preset, (int)count.Value);
            form.DialogResult = DialogResult.OK;
            form.Close();
        }, "Next");
        DialogLayout.AddRow(layout, "Preset", presets);
        DialogLayout.AddRow(layout, "How many", count);

        return form.ShowDialog(this) == DialogResult.OK ? result : null;
    }

    private TournamentPresetListItem[] BuildTournamentPresetItems()
    {
        return _data.TournamentPresets
            .Where(preset => _data.Settings.IsPlatformEnabled(preset.Platform))
            .Select(preset => new TournamentPresetListItem(
                preset,
                TournamentPresetService.DisplayName(preset, _data.Settings)))
            .OrderBy(item => item.Text, NaturalSortComparer.Instance)
            .ToArray();
    }

    private TicketBuyInPromptResult? PromptTicketBuyIn(
        IReadOnlyList<TicketPlatformPromptItem> platformItems,
        Platform initialPlatform,
        decimal initialValue,
        decimal tournamentCost)
    {
        if (platformItems.Count == 0)
        {
            return null;
        }

        var result = (TicketBuyInPromptResult?)null;
        using var form = new Form
        {
            Text = "Use Ticket",
            Size = new Size(540, 245),
            MinimumSize = new Size(500, 220),
            StartPosition = FormStartPosition.CenterParent,
            BackColor = Theme.Back,
            ForeColor = Theme.Text,
            Font = Theme.BodyFont
        };

        var platforms = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Theme.PanelAlt,
            ForeColor = Theme.Text,
            FlatStyle = FlatStyle.Flat,
            Font = Theme.BodyFont
        };
        platforms.Items.AddRange(platformItems.Cast<object>().ToArray());
        platforms.SelectedItem = platformItems.FirstOrDefault(item => item.Platform == initialPlatform) ?? platformItems[0];

        var amount = Theme.MoneyBox(0m);
        amount.Minimum = 0m;
        var balanceLabel = Theme.Label(string.Empty, Theme.SmallFont, Theme.Muted);
        balanceLabel.AutoSize = false;
        balanceLabel.Height = 40;
        balanceLabel.TextAlign = ContentAlignment.MiddleLeft;

        void UpdateAmountLimit(bool useInitialValue)
        {
            if (platforms.SelectedItem is not TicketPlatformPromptItem selected)
            {
                return;
            }

            var maximum = Math.Max(0m, Math.Min(tournamentCost, selected.AvailableTicketValue));
            if (amount.Maximum < maximum)
            {
                amount.Maximum = maximum;
            }

            var nextValue = useInitialValue && selected.Platform == initialPlatform && initialValue > 0m
                ? initialValue
                : amount.Value > 0m
                    ? amount.Value
                    : maximum;
            amount.Value = ClampToBox(amount, nextValue);
            if (amount.Value > maximum)
            {
                amount.Value = maximum;
            }

            amount.Maximum = maximum;
            balanceLabel.Text = $"Available: {Money(selected.AvailableTicketValue)}   Max for this entry: {Money(maximum)}";
        }

        platforms.SelectedIndexChanged += (_, _) => UpdateAmountLimit(useInitialValue: false);
        UpdateAmountLimit(useInitialValue: true);

        var layout = DialogLayout.Create(form, () =>
        {
            if (platforms.SelectedItem is not TicketPlatformPromptItem selected)
            {
                return;
            }

            result = new TicketBuyInPromptResult(selected.Platform, amount.Value);
            form.DialogResult = DialogResult.OK;
            form.Close();
        });
        DialogLayout.AddRow(layout, "Ticket platform", platforms);
        DialogLayout.AddRow(layout, "Ticket buy-in value", amount);
        DialogLayout.AddRow(layout, "Balance", balanceLabel);

        return form.ShowDialog(this) == DialogResult.OK ? result : null;
    }

    private static decimal ClampToBox(NumericUpDown box, decimal value)
    {
        return Math.Min(Math.Max(value, box.Minimum), box.Maximum);
    }

    private static string AppendTag(string tags, string tag)
    {
        if (string.IsNullOrWhiteSpace(tags))
        {
            return tag;
        }

        return tags.Contains(tag, StringComparison.OrdinalIgnoreCase) ? tags.Trim() : $"{tags.Trim()}, {tag}";
    }

    private sealed record TicketBuyInPromptResult(Platform Platform, decimal Amount);

    private sealed record TournamentQuickAddSetup(TournamentPreset Preset, int Count);

    private sealed record TicketPlatformPromptItem(Platform Platform, decimal AvailableTicketValue, string Text)
    {
        public override string ToString()
        {
            return Text;
        }
    }
}
