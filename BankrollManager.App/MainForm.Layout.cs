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

    private static int NavigationItemWidth(string title)
    {
        var measuredWidth = TextRenderer.MeasureText(title, Theme.BodyFont).Width + 24;
        var minimumWidth = title switch
        {
            "Overview" => 92,
            "Wallets" => 78,
            "Audit" => 64,
            "Timeline" => 88,
            "MTTs" => 58,
            "Cash" => 60,
            "Ledger" => 70,
            "Day" => 56,
            "Month" => 68,
            "Monthly Review" => 128,
            "Year" => 58,
            "Decide" => 70,
            "EV Check" => 88,
            "Settings" => 78,
            _ => 88
        };
        return Math.Max(minimumWidth, measuredWidth);
    }

    private TableLayoutPanel BuildGridShell(out FlowLayoutPanel buttons)
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            BackColor = Theme.Back,
            Padding = new Padding(12)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        buttons = BuildActionBar();
        root.Controls.Add(buttons, 0, 0);
        return root;
    }

    private static FlowLayoutPanel BuildActionBar()
    {
        return new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Theme.Back,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(0, 0, 0, 10),
            Margin = new Padding(0)
        };
    }

    private static Control BuildGridWithEmptyState(DataGridView grid, out Label emptyState, string text)
    {
        var host = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Back
        };

        grid.Dock = DockStyle.Fill;
        emptyState = BuildEmptyStateLabel(text);
        emptyState.Dock = DockStyle.Top;
        host.Controls.Add(grid);
        host.Controls.Add(emptyState);
        return host;
    }

    private static Control BuildPagedGrid(DataGridView grid, IGridLoadController loadController)
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            BackColor = Theme.Back,
            Margin = new Padding(0)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.Controls.Add(grid, 0, 0);
        root.Controls.Add(BuildGridLoadBar(loadController), 0, 1);
        return root;
    }

    private static Control BuildPagedGridWithEmptyState(
        DataGridView grid,
        IGridLoadController loadController,
        out Label emptyState,
        string text)
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            BackColor = Theme.Back,
            Margin = new Padding(0)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.Controls.Add(BuildGridWithEmptyState(grid, out emptyState, text), 0, 0);
        root.Controls.Add(BuildGridLoadBar(loadController), 0, 1);
        return root;
    }

    private static Control BuildGridLoadBar(IGridLoadController loadController)
    {
        var bar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Theme.Back,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(0, 6, 0, 0),
            Margin = new Padding(0)
        };

        var status = Theme.Label(string.Empty, Theme.SmallFont, Theme.Muted);
        status.AutoSize = false;
        status.Width = 180;
        status.Height = Theme.ControlHeight;
        status.TextAlign = ContentAlignment.MiddleLeft;
        status.Margin = new Padding(0, 4, 8, 0);

        var loadMore = Theme.Button("Load more");
        loadMore.AutoSize = false;
        loadMore.Width = 110;
        loadMore.Click += (_, _) => loadController.ShowMore();

        var showAll = Theme.Button("Show all");
        showAll.AutoSize = false;
        showAll.Width = 96;
        showAll.Click += (_, _) => loadController.ShowAll();

        void UpdateState()
        {
            status.Text = loadController.TotalCount == 0
                ? "No rows"
                : loadController.CanShowMore
                    ? $"Showing {loadController.VisibleCount:N0} of {loadController.TotalCount:N0}"
                    : $"Showing all {loadController.TotalCount:N0}";
            loadMore.Enabled = loadController.CanShowMore;
            showAll.Enabled = loadController.CanShowMore;
        }

        loadController.ViewChanged += (_, _) =>
        {
            if (!bar.IsDisposed)
            {
                UpdateState();
            }
        };
        UpdateState();

        bar.Controls.Add(status);
        bar.Controls.Add(loadMore);
        bar.Controls.Add(showAll);
        return bar;
    }

    private static Label BuildEmptyStateLabel(string text)
    {
        return new Label
        {
            Text = text,
            AutoSize = false,
            AutoEllipsis = true,
            Height = 42,
            Dock = DockStyle.Top,
            BackColor = Theme.AccentSurface,
            ForeColor = Theme.Text,
            Font = Theme.BodyFont,
            Margin = new Padding(0, 0, 0, 8),
            Padding = new Padding(14, 0, 14, 1),
            TextAlign = ContentAlignment.MiddleLeft,
            UseMnemonic = false
        };
    }

    private static void AddDecisionRow(TableLayoutPanel layout, string label, Control control)
    {
        var row = layout.RowCount++;
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var labelControl = Theme.Label(label, Theme.BodyFont, Theme.Muted);
        labelControl.Padding = new Padding(0, 8, 8, 8);
        labelControl.Dock = DockStyle.Fill;
        control.Margin = new Padding(4, 5, 4, 5);
        control.Dock = DockStyle.Fill;
        layout.Controls.Add(labelControl, 0, row);
        layout.Controls.Add(control, 1, row);
    }

    private static void AddSettingsRow(TableLayoutPanel layout, string label, Control control)
    {
        var row = layout.RowCount++;
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));

        var labelControl = Theme.Label(label, Theme.BodyFont, Theme.Muted);
        labelControl.AutoSize = false;
        labelControl.AutoEllipsis = true;
        labelControl.Dock = DockStyle.Fill;
        labelControl.Margin = new Padding(0);
        labelControl.Padding = new Padding(0, 0, 14, 0);
        labelControl.TextAlign = ContentAlignment.MiddleLeft;
        labelControl.UseMnemonic = false;

        ConfigureSettingsControl(control);

        layout.Controls.Add(labelControl, 0, row);
        layout.Controls.Add(control, 1, row);
    }

    private static void AddTallSettingsRow(TableLayoutPanel layout, string label, Control control, int height)
    {
        var row = layout.RowCount++;
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, height));

        var labelControl = Theme.Label(label, Theme.BodyFont, Theme.Muted);
        labelControl.AutoSize = false;
        labelControl.AutoEllipsis = true;
        labelControl.Dock = DockStyle.Fill;
        labelControl.Margin = new Padding(0);
        labelControl.Padding = new Padding(0, 8, 14, 0);
        labelControl.TextAlign = ContentAlignment.TopLeft;
        labelControl.UseMnemonic = false;

        control.AutoSize = false;
        control.Dock = DockStyle.Left;
        control.Width = 300;
        control.Height = Math.Max(32, height - 16);
        control.Margin = new Padding(0, 6, 0, 6);

        layout.Controls.Add(labelControl, 0, row);
        layout.Controls.Add(control, 1, row);
    }

    private static void AddSettingsSection(TableLayoutPanel layout, string text)
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

    private static void ConfigureSettingsControl(Control control)
    {
        control.Margin = new Padding(0, 6, 0, 6);

        if (control is CheckBox checkBox)
        {
            checkBox.AutoSize = true;
            checkBox.Dock = DockStyle.None;
            checkBox.Anchor = AnchorStyles.Left;
            checkBox.Margin = new Padding(0, 14, 0, 0);
            return;
        }

        control.AutoSize = false;
        control.Dock = DockStyle.Left;
        control.Width = 300;
        control.Height = Math.Max(32, control.Height);

        if (control is Button button)
        {
            button.Height = 38;
            button.TextAlign = ContentAlignment.MiddleCenter;
        }
    }

    private static NumericUpDown PercentBox(decimal value = 0m)
    {
        return new NumericUpDown
        {
            DecimalPlaces = 2,
            Increment = 0.25m,
            Minimum = 0m,
            Maximum = 100m,
            Value = value,
            BackColor = Theme.PanelAlt,
            ForeColor = Theme.Text,
            Font = Theme.BodyFont,
            Width = 150,
            Height = Theme.ControlHeight
        };
    }

    private static TextBox BuildResultTextBox()
    {
        return new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Theme.PanelAlt,
            ForeColor = Theme.Text,
            Font = Theme.BodyFont,
            Margin = new Padding(0, 4, 0, 4),
            WordWrap = true
        };
    }
}
