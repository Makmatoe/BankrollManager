namespace BankrollManager.App.Controls;

using System.Drawing.Drawing2D;

public enum MiniChartKind
{
    Bars,
    Line
}

public sealed record MiniChartPoint(string Label, decimal Value, object? Tag = null, string? Tooltip = null);

public sealed class MiniChartPointActivatedEventArgs(MiniChartPoint point, int index) : EventArgs
{
    public MiniChartPoint Point { get; } = point;
    public int Index { get; } = index;
}

public sealed class MiniChart : Control
{
    private const int ChartLeftGutter = 18;
    private const int ChartTopGutter = 56;
    private const int ChartRightGutter = 66;
    private const int ChartBottomGutter = 50;
    private List<MiniChartPoint> _points = [];
    private readonly ToolTip _toolTip = new();
    private int _hoveredIndex = -1;

    public MiniChart()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.ResizeRedraw, true);
        BackColor = Theme.Panel;
        ForeColor = Theme.Text;
        Font = Theme.SmallFont;
        Padding = new Padding(14);
        MinimumSize = new Size(180, 150);
        Margin = new Padding(8);
    }

    public event EventHandler<MiniChartPointActivatedEventArgs>? PointActivated;

    public string Title { get; private set; } = string.Empty;
    public MiniChartKind Kind { get; private set; } = MiniChartKind.Bars;

    public void SetData(string title, IEnumerable<MiniChartPoint> points, MiniChartKind kind)
    {
        Title = title;
        _points = points.ToList();
        Kind = kind;
        _hoveredIndex = -1;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.Clear(BackColor);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var bounds = ClientRectangle;
        bounds.Width -= 1;
        bounds.Height -= 1;
        using var cardPath = RoundedRect(bounds, 8);
        using var cardBrush = new SolidBrush(Theme.Panel);
        using var cardBorder = new Pen(Theme.Border);
        e.Graphics.FillPath(cardBrush, cardPath);
        e.Graphics.DrawPath(cardBorder, cardPath);

        using var titleBrush = new SolidBrush(Theme.Text);
        using var mutedBrush = new SolidBrush(Theme.Muted);
        e.Graphics.DrawString(Title, Theme.SubHeaderFont, titleBrush, Padding.Left, Padding.Top + 2);

        var area = ChartArea();

        if (_points.Count == 0 || area.Width <= 0 || area.Height <= 0)
        {
            e.Graphics.DrawString("No data", Font, mutedBrush, area.Left + 8, area.Top + 8);
            return;
        }

        var min = Math.Min(0m, _points.Min(point => point.Value));
        var max = Math.Max(0m, _points.Max(point => point.Value));
        if (min == max)
        {
            min -= 1m;
            max += 1m;
        }

        DrawGrid(e.Graphics, area, min, max);
        DrawZeroLine(e.Graphics, area, min, max);

        if (Kind == MiniChartKind.Line)
        {
            DrawLine(e.Graphics, area, min, max);
        }
        else
        {
            DrawBars(e.Graphics, area, min, max);
        }

        DrawChartLabels(e.Graphics, area, min, max);
    }

    private void DrawZeroLine(Graphics graphics, Rectangle area, decimal min, decimal max)
    {
        var y = ScaleY(0m, area, min, max);
        using var pen = new Pen(Theme.ChartZeroLine);
        graphics.DrawLine(pen, area.Left, y, area.Right, y);
    }

    private void DrawBars(Graphics graphics, Rectangle area, decimal min, decimal max)
    {
        var barGap = _points.Count > 36 ? 1 : 5;
        var barWidth = Math.Max(2f, (area.Width - barGap * (_points.Count + 1)) / Math.Max(1f, _points.Count));
        var zeroY = ScaleY(0m, area, min, max);

        for (var index = 0; index < _points.Count; index++)
        {
            var point = _points[index];
            var x = area.Left + barGap + index * (barWidth + barGap);
            var y = ScaleY(point.Value, area, min, max);
            var top = Math.Min(y, zeroY);
            var height = Math.Max(2, Math.Abs(y - zeroY));
            using var brush = new SolidBrush(point.Value >= 0m ? Theme.Positive : Theme.Negative);
            graphics.FillRectangle(brush, x, top, barWidth, height);
            if (index == _hoveredIndex)
            {
                using var border = new Pen(Theme.Text, 1.5f);
                graphics.DrawRectangle(border, x, top, barWidth, height);
            }
        }
    }

    private void DrawLine(Graphics graphics, Rectangle area, decimal min, decimal max)
    {
        if (_points.Count == 1)
        {
            var y = ScaleY(_points[0].Value, area, min, max);
            using var singlePointBrush = new SolidBrush(Theme.Accent);
            var radius = _hoveredIndex == 0 ? 6 : 4;
            graphics.FillEllipse(singlePointBrush, area.Left + area.Width / 2 - radius, y - radius, radius * 2, radius * 2);
            return;
        }

        var points = _points.Select((point, index) =>
        {
            var x = area.Left + index * area.Width / Math.Max(1, _points.Count - 1);
            var y = ScaleY(point.Value, area, min, max);
            return new Point(x, y);
        }).ToArray();

        using var pen = new Pen(Theme.Accent, 2f);
        graphics.DrawLines(pen, points);
        using var pointBrush = new SolidBrush(Theme.Accent);
        for (var index = 0; index < points.Length; index++)
        {
            var point = points[index];
            var radius = index == _hoveredIndex ? 5 : 3;
            graphics.FillEllipse(pointBrush, point.X - radius, point.Y - radius, radius * 2, radius * 2);
            if (index == _hoveredIndex)
            {
                using var outline = new Pen(Theme.Text, 1.5f);
                graphics.DrawEllipse(outline, point.X - radius, point.Y - radius, radius * 2, radius * 2);
            }
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var hoveredIndex = HitTestPoint(e.Location);
        if (hoveredIndex == _hoveredIndex)
        {
            return;
        }

        _hoveredIndex = hoveredIndex;
        Cursor = hoveredIndex >= 0 ? Cursors.Hand : Cursors.Default;
        _toolTip.SetToolTip(this, hoveredIndex >= 0 ? TooltipText(_points[hoveredIndex]) : string.Empty);
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hoveredIndex = -1;
        Cursor = Cursors.Default;
        _toolTip.SetToolTip(this, string.Empty);
        Invalidate();
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        var pointIndex = HitTestPoint(e.Location);
        if (pointIndex < 0)
        {
            return;
        }

        PointActivated?.Invoke(this, new MiniChartPointActivatedEventArgs(_points[pointIndex], pointIndex));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _toolTip.Dispose();
        }

        base.Dispose(disposing);
    }

    private static void DrawGrid(Graphics graphics, Rectangle area, decimal min, decimal max)
    {
        using var pen = new Pen(Theme.ChartGrid);
        for (var index = 1; index <= 3; index++)
        {
            var y = area.Top + area.Height * index / 4;
            graphics.DrawLine(pen, area.Left, y, area.Right, y);
        }
    }

    private void DrawChartLabels(Graphics graphics, Rectangle area, decimal min, decimal max)
    {
        using var mutedBrush = new SolidBrush(Theme.Muted);
        using var titleBrush = new SolidBrush(Theme.Text);
        using var leftFormat = new StringFormat
        {
            Trimming = StringTrimming.EllipsisCharacter,
            FormatFlags = StringFormatFlags.NoWrap,
            Alignment = StringAlignment.Near,
            LineAlignment = StringAlignment.Center
        };
        using var rightFormat = new StringFormat
        {
            Trimming = StringTrimming.EllipsisCharacter,
            FormatFlags = StringFormatFlags.NoWrap,
            Alignment = StringAlignment.Far,
            LineAlignment = StringAlignment.Center
        };

        var last = _points.Last();
        var lastText = $"{last.Label}: {last.Value:0.00}";
        var lastRect = new RectangleF(
            area.Left + area.Width * 0.55f,
            Padding.Top + 4,
            Math.Max(10, area.Right - area.Left - area.Width * 0.55f),
            20);
        graphics.DrawString(lastText, Theme.SmallFont, titleBrush, lastRect, rightFormat);

        var axisLabelWidth = Math.Max(30, Width - area.Right - 18);
        var maxRect = new RectangleF(area.Right + 8, area.Top - 9, axisLabelWidth, 18);
        var minRect = new RectangleF(area.Right + 8, area.Bottom - 9, axisLabelWidth, 18);
        graphics.DrawString(max.ToString("0.00"), Theme.SmallFont, mutedBrush, maxRect, rightFormat);
        graphics.DrawString(min.ToString("0.00"), Theme.SmallFont, mutedBrush, minRect, rightFormat);

        if (_points.Count > 1)
        {
            var labelArea = new RectangleF(area.Left, area.Bottom + 17, area.Width, 18);
            graphics.DrawString($"{_points.First().Label}  ->  {_points.Last().Label}", Theme.SmallFont, mutedBrush, labelArea, leftFormat);
        }
    }

    private static int ScaleY(decimal value, Rectangle area, decimal min, decimal max)
    {
        var range = max - min;
        if (range <= 0m)
        {
            return area.Bottom;
        }

        var normalized = (double)((value - min) / range);
        return area.Bottom - (int)Math.Round(normalized * area.Height);
    }

    private Rectangle ChartArea()
    {
        return new Rectangle(
            ChartLeftGutter,
            ChartTopGutter,
            Math.Max(0, Width - ChartLeftGutter - ChartRightGutter),
            Math.Max(0, Height - ChartTopGutter - ChartBottomGutter));
    }

    private bool TryGetScale(out Rectangle area, out decimal min, out decimal max)
    {
        area = ChartArea();
        min = 0m;
        max = 0m;
        if (_points.Count == 0 || area.Width <= 0 || area.Height <= 0)
        {
            return false;
        }

        min = Math.Min(0m, _points.Min(point => point.Value));
        max = Math.Max(0m, _points.Max(point => point.Value));
        if (min == max)
        {
            min -= 1m;
            max += 1m;
        }

        return true;
    }

    private int HitTestPoint(Point location)
    {
        if (!TryGetScale(out var area, out var min, out var max))
        {
            return -1;
        }

        return Kind == MiniChartKind.Line
            ? HitTestLinePoint(location, area, min, max)
            : HitTestBarPoint(location, area);
    }

    private int HitTestBarPoint(Point location, Rectangle area)
    {
        var barGap = _points.Count > 36 ? 1 : 5;
        var barWidth = Math.Max(2f, (area.Width - barGap * (_points.Count + 1)) / Math.Max(1f, _points.Count));

        for (var index = 0; index < _points.Count; index++)
        {
            var x = area.Left + barGap + index * (barWidth + barGap);
            var hitBox = new RectangleF(
                x - Math.Max(2, barGap / 2f),
                area.Top,
                barWidth + Math.Max(4, barGap),
                area.Height);
            if (hitBox.Contains(location))
            {
                return index;
            }
        }

        return -1;
    }

    private int HitTestLinePoint(Point location, Rectangle area, decimal min, decimal max)
    {
        for (var index = 0; index < _points.Count; index++)
        {
            var x = _points.Count == 1
                ? area.Left + area.Width / 2
                : area.Left + index * area.Width / Math.Max(1, _points.Count - 1);
            var y = ScaleY(_points[index].Value, area, min, max);
            var hitBox = new Rectangle(x - 10, y - 10, 20, 20);
            if (hitBox.Contains(location))
            {
                return index;
            }
        }

        return -1;
    }

    private string TooltipText(MiniChartPoint point)
    {
        return string.IsNullOrWhiteSpace(point.Tooltip)
            ? $"{Title}{Environment.NewLine}{point.Label}: {point.Value:0.00}"
            : point.Tooltip;
    }

    private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
    {
        var diameter = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
