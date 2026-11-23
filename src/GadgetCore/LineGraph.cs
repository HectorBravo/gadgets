using System.Windows;
using System.Windows.Media;

namespace HectorBravo.Gadgets.Core;

/// <summary>One plotted series for LineGraph.</summary>
public record Series(string Name, IReadOnlyList<double> Samples, Color Color);

/// <summary>
/// Lightweight multi-series line graph drawn in OnRender (no GPU acceleration —
/// pure WPF vector rendering, works everywhere). Auto-scales vertically to the
/// data and paints a min/max legend on the right, like the original gadgets.
/// </summary>
public class LineGraph : FrameworkElement
{
    private List<Series> _series = new();
    private double _minY, _maxY;
    private bool _hasData;

    /// <summary>Fixed axis (overrides auto-scale) when set.</summary>
    public double? FixedMax { get; set; }

    public LineGraph()
    {
        IsHitTestVisible = false; // let drags pass through to the window
        SnapsToDevicePixels = true;
    }

    public void SetSeries(List<Series> series)
    {
        _series = series;
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext dc)
    {
        double w = ActualWidth, h = ActualHeight;
        if (w <= 2 || h <= 2) return;

        // background + border
        dc.DrawRectangle(Theme.GraphBg, Theme.BorderPen, new Rect(0.5, 0.5, w - 1, h - 1));

        _hasData = false;
        double min = double.MaxValue, max = double.MinValue;
        foreach (var s in _series)
            foreach (var v in s.Samples)
            {
                if (v < min) min = v;
                if (v > max) max = v;
            }
        if (min == double.MaxValue) { min = 0; max = 1; }
        if (FixedMax is double fm) max = Math.Max(fm, max);
        if (max - min < 1e-9) max = min + 1;
        _minY = min; _maxY = max;
        _hasData = true;

        // horizontal gridlines at 25/50/75%
        var gridPen = Theme.GridPen;
        for (int g = 1; g <= 3; g++)
        {
            double y = h * g / 4.0;
            dc.DrawLine(gridPen, new Point(1, y), new Point(w - 1, y));
        }

        foreach (var s in _series)
        {
            var pts = s.Samples;
            if (pts.Count < 2) continue;
            var pen = new Pen(new SolidColorBrush(s.Color), 1.2);
            pen.Freeze();
            var geo = new StreamGeometry();
            using (var gc = geo.Open())
            {
                for (int i = 0; i < pts.Count; i++)
                {
                    double x = w * i / (pts.Count - 1);
                    double y = h - h * ((pts[i] - min) / (max - min));
                    var p = new Point(x, Math.Clamp(y, 0, h));
                    if (i == 0) gc.BeginFigure(p, false, false);
                    else gc.LineTo(p, true, true);
                }
            }
            geo.Freeze();
            dc.DrawGeometry(null, pen, geo);
        }

        // min/max legend on the right edge (like the originals' axis labels)
        var fmt = LegendFormat;
        if (fmt != null)
        {
            DrawLabel(dc, fmt(max), w - 4, 1, HorizontalAlignment.Right);
            DrawLabel(dc, fmt(min), w - 4, h - 13, HorizontalAlignment.Right);
        }
    }

    /// <summary>Optional legend formatter (e.g. v => Fmt.Rate(v)).</summary>
    public Func<double, string>? LegendFormat { get; set; }

    private void DrawLabel(DrawingContext dc, string text, double x, double y, HorizontalAlignment align)
    {
        var ft = new FormattedText(
            text,
            System.Globalization.CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface(Theme.Mono, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal),
            9, Theme.Muted, VisualTreeHelper.GetDpi(this).PixelsPerDip);
        double tx = align == HorizontalAlignment.Right ? x - ft.Width : x;
        dc.DrawText(ft, new Point(Math.Max(1, tx), y));
    }

    /// <summary>Value at the vertical position of a point (for tooltips), or null.</summary>
    public double? ValueAt(double localY)
    {
        if (!_hasData) return null;
        double frac = 1 - localY / ActualHeight;
        return _minY + frac * (_maxY - _minY);
    }
}
