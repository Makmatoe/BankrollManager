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

    private static ToolStripButton BuildCommandButton(string text, Action action, CommandTone tone = CommandTone.Neutral)
    {
        var button = new ToolStripButton(text)
        {
            AutoSize = false,
            DisplayStyle = ToolStripItemDisplayStyle.Text,
            Font = Theme.BodyFont,
            ForeColor = Theme.Text,
            Margin = new Padding(3),
            Padding = new Padding(0),
            Size = CommandItemSize(text),
            Tag = tone,
            TextAlign = ContentAlignment.MiddleCenter,
            ToolTipText = text
        };
        button.Click += (_, _) => action();
        return button;
    }

    private static ToolStripDropDownButton BuildCommandDropDown(string text, params (string Label, Action Action)[] menuItems)
    {
        var button = new ToolStripDropDownButton(text)
        {
            AutoSize = false,
            DisplayStyle = ToolStripItemDisplayStyle.Text,
            Font = Theme.BodyFont,
            ForeColor = Theme.Text,
            Margin = new Padding(3),
            Padding = new Padding(0),
            ShowDropDownArrow = true,
            Size = CommandItemSize(text, dropDown: true),
            Tag = CommandTone.Neutral,
            TextAlign = ContentAlignment.MiddleCenter,
            ToolTipText = text
        };

        button.DropDown.BackColor = Theme.Panel;
        button.DropDown.Padding = new Padding(4);
        button.DropDown.Renderer = new HeaderToolStripRenderer();

        foreach (var menuItem in menuItems.OrderBy(menuItem => menuItem.Label, NaturalSortComparer.Instance))
        {
            button.DropDownItems.Add(BuildCommandMenuItem(menuItem.Label, menuItem.Action));
        }

        return button;
    }

    private static ToolStripMenuItem BuildCommandMenuItem(string text, Action action)
    {
        var item = new ToolStripMenuItem(text)
        {
            DisplayStyle = ToolStripItemDisplayStyle.Text,
            Font = Theme.BodyFont,
            ForeColor = Theme.Text,
            BackColor = Theme.Panel,
            Margin = new Padding(0),
            Padding = new Padding(10, 5, 28, 5)
        };
        item.Click += (_, _) => action();
        return item;
    }

    private static Size CommandItemSize(string text, bool dropDown = false)
    {
        var textSize = TextRenderer.MeasureText(text, Theme.BodyFont);
        return new Size(textSize.Width + (dropDown ? 38 : 28), 34);
    }

    private static Button AddGridButton(FlowLayoutPanel panel, string text, Action action)
    {
        var button = Theme.Button(text);
        button.Click += (_, _) => action();
        panel.Controls.Add(button);
        return button;
    }

    private enum CommandTone
    {
        Neutral,
        Primary,
        Danger
    }

    private sealed class HeaderToolStripRenderer : ToolStripProfessionalRenderer
    {
        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            using var background = new SolidBrush(Theme.Panel);
            e.Graphics.FillRectangle(background, e.AffectedBounds);
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            if (e.ToolStrip is not ToolStripDropDown)
            {
                return;
            }

            using var border = new Pen(Theme.Border);
            var bounds = new Rectangle(Point.Empty, e.ToolStrip.Size);
            bounds.Width--;
            bounds.Height--;
            e.Graphics.DrawRectangle(border, bounds);
        }

        protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
        {
            PaintCommandItem(e);
        }

        protected override void OnRenderDropDownButtonBackground(ToolStripItemRenderEventArgs e)
        {
            PaintCommandItem(e);
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            var bounds = new Rectangle(Point.Empty, e.Item.Size);
            using var background = new SolidBrush(e.Item.Selected ? Theme.PanelAlt : Theme.Panel);
            e.Graphics.FillRectangle(background, bounds);

            if (!e.Item.Selected)
            {
                return;
            }

            using var accent = new SolidBrush(Theme.Accent);
            e.Graphics.FillRectangle(accent, 0, 4, 3, Math.Max(1, bounds.Height - 8));
        }

        protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
        {
            using var background = new SolidBrush(Theme.Panel);
            e.Graphics.FillRectangle(background, e.AffectedBounds);
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            using var line = new Pen(Theme.Border);
            if (e.Vertical)
            {
                var x = e.Item.Width / 2;
                e.Graphics.DrawLine(line, x, 7, x, Math.Max(7, e.Item.Height - 7));
                return;
            }

            var y = e.Item.Height / 2;
            e.Graphics.DrawLine(line, 8, y, Math.Max(8, e.Item.Width - 8), y);
        }

        private static void PaintCommandItem(ToolStripItemRenderEventArgs e)
        {
            var bounds = new Rectangle(Point.Empty, e.Item.Size);
            bounds.Inflate(-1, -1);
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                return;
            }

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var tone = e.Item.Tag is CommandTone commandTone ? commandTone : CommandTone.Neutral;
            var pressed = e.Item is ToolStripButton { Pressed: true }
                || e.Item is ToolStripDropDownButton { Pressed: true };
            using var background = new SolidBrush(CommandBackColor(tone, e.Item.Selected, pressed));
            using var border = new Pen(CommandBorderColor(tone, e.Item.Selected, pressed));
            using var path = RoundedRectangle(bounds, 6);
            e.Graphics.FillPath(background, path);
            e.Graphics.DrawPath(border, path);
        }

        private static Color CommandBackColor(CommandTone tone, bool selected, bool pressed)
        {
            return tone switch
            {
                CommandTone.Primary when pressed => Theme.CommandPrimaryDown,
                CommandTone.Primary when selected => Theme.CommandPrimaryHover,
                CommandTone.Primary => Theme.CommandPrimary,
                CommandTone.Danger when pressed => Theme.CommandDangerDown,
                CommandTone.Danger when selected => Theme.CommandDangerHover,
                CommandTone.Danger => Theme.CommandDanger,
                _ when pressed => Theme.CommandNeutralDown,
                _ when selected => Theme.CommandNeutralHover,
                _ => Theme.PanelRaised
            };
        }

        private static Color CommandBorderColor(CommandTone tone, bool selected, bool pressed)
        {
            if (tone == CommandTone.Primary)
            {
                return selected || pressed ? Theme.Accent : Theme.CommandPrimaryBorder;
            }

            if (tone == CommandTone.Danger)
            {
                return selected || pressed ? Theme.Negative : Theme.CommandDangerBorder;
            }

            return selected || pressed ? Theme.CommandNeutralBorder : Theme.Border;
        }

        private static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
        {
            var path = new GraphicsPath();
            var adjustedRadius = Math.Min(radius, Math.Min(bounds.Width, bounds.Height) / 2);
            var diameter = adjustedRadius * 2;

            if (diameter <= 0)
            {
                path.AddRectangle(bounds);
                return path;
            }

            path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
