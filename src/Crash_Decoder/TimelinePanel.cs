// MIT License. Copyright (c) 2026 MSFS-Input-Blackbox contributors.
using System.Drawing.Drawing2D;

namespace MsfsInputBlackbox.Decoder;

internal sealed class TimelinePanel : Panel
{
    private IReadOnlyList<TelemetrySample> _frames = Array.Empty<TelemetrySample>();

    public TimelinePanel()
    {
        DoubleBuffered = true;
        BackColor = Color.FromArgb(22, 27, 34);
        Dock = DockStyle.Fill;
    }

    public void SetFrames(IReadOnlyList<TelemetrySample> frames)
    {
        _frames = frames;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (_frames.Count < 2) return;
        const int padding = 28;
        var plot = Rectangle.FromLTRB(padding, padding, ClientSize.Width - 10, ClientSize.Height - padding);
        if (plot.Width < 2 || plot.Height < 2) return;
        using var grid = new Pen(Color.FromArgb(65, 148, 163, 184));
        using var zero = new Pen(Color.FromArgb(110, 148, 163, 184));
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        for (var i = 0; i <= 4; i++)
        {
            var y = plot.Top + plot.Height * i / 4;
            e.Graphics.DrawLine(i == 2 ? zero : grid, plot.Left, y, plot.Right, y);
        }
        DrawSeries(e.Graphics, plot, frame => frame.YokeX, Color.FromArgb(88, 166, 255));
        DrawSeries(e.Graphics, plot, frame => frame.YokeY, Color.FromArgb(63, 185, 80));
        DrawSeries(e.Graphics, plot, frame => frame.Rudder, Color.FromArgb(210, 153, 34));
        DrawSeries(e.Graphics, plot, frame => frame.Throttle * 2f - 1f, Color.FromArgb(248, 81, 73));
        TextRenderer.DrawText(e.Graphics, "−1", Font, new Point(3, plot.Bottom - 8), Color.LightGray);
        TextRenderer.DrawText(e.Graphics, "0", Font, new Point(9, plot.Top + plot.Height / 2 - 8), Color.LightGray);
        TextRenderer.DrawText(e.Graphics, "+1", Font, new Point(3, plot.Top - 8), Color.LightGray);
    }

    private void DrawSeries(Graphics graphics, Rectangle plot, Func<TelemetrySample, float> select, Color color)
    {
        using var pen = new Pen(color, 1.7f);
        var points = new PointF[_frames.Count];
        for (var i = 0; i < _frames.Count; i++)
        {
            var x = plot.Left + plot.Width * i / (float)(_frames.Count - 1);
            var normalized = Math.Clamp(select(_frames[i]), -1f, 1f);
            var y = plot.Top + plot.Height * (1f - (normalized + 1f) / 2f);
            points[i] = new PointF(x, y);
        }
        graphics.DrawLines(pen, points);
    }
}

