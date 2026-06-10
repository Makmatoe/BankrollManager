using BankrollManager.Core.Models;
using BankrollManager.Core.Services;
using BankrollManager.Core.Validation;

namespace BankrollManager.App.Forms;

internal static class DialogLayout
{
    public sealed record Row(Control Label, Control Control)
    {
        public void SetVisible(bool visible)
        {
            Label.Visible = visible;
            Control.Visible = visible;
        }
    }

    public static TableLayoutPanel Create(Form form, Action save)
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            BackColor = Theme.Back
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        form.Controls.Add(root);

        var scroll = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(12),
            BackColor = Theme.Back
        };
        root.Controls.Add(scroll, 0, 0);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            BackColor = Theme.Back
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 185));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        scroll.Controls.Add(layout);

        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(8),
            BackColor = Theme.Panel
        };
        root.Controls.Add(footer, 0, 1);

        var ok = Theme.Button("Save");
        ok.Click += (_, _) => save();
        var cancel = Theme.Button("Cancel");
        cancel.Click += (_, _) =>
        {
            form.DialogResult = DialogResult.Cancel;
            form.Close();
        };
        footer.Controls.Add(ok);
        footer.Controls.Add(cancel);
        form.AcceptButton = ok;
        form.CancelButton = cancel;

        return layout;
    }

    public static Row AddRow(TableLayoutPanel layout, string label, Control control)
    {
        var row = layout.RowCount++;
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var labelControl = Theme.Label(label, Theme.BodyFont, Theme.Muted);
        labelControl.Dock = DockStyle.Fill;
        labelControl.TextAlign = ContentAlignment.MiddleLeft;
        labelControl.Padding = new Padding(0, 6, 8, 6);

        control.Dock = DockStyle.Fill;
        control.Margin = new Padding(4, 5, 4, 5);

        layout.Controls.Add(labelControl, 0, row);
        layout.Controls.Add(control, 1, row);
        return new Row(labelControl, control);
    }

    public static bool ShowErrors(IReadOnlyCollection<string> errors)
    {
        if (errors.Count == 0)
        {
            return false;
        }

        MessageBox.Show(
            string.Join(Environment.NewLine, errors),
            "Please fix these fields",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);
        return true;
    }
}
