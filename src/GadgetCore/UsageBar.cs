using System.Windows;
using System.Windows.Media;

namespace HectorBravo.Gadgets.Core;

/// <summary>Simple horizontal usage bar drawn in OnRender (dark track + colored fill).</summary>
public class UsageBar : FrameworkElement
{
    public double Fraction { get; set; }
    public Color Color { get; set; } = Theme.Hex("#158000");

    protected override void OnRender(DrawingContext dc)
    {
        double w = ActualWidth, h = ActualHeight;
        if (w <= 1 || h <= 1) return;
        dc.DrawRectangle(new SolidColorBrush(Theme.Hex("#20242e")), null, new Rect(0, 0, w, h));
        double fw = Math.Clamp(Fraction, 0, 1) * w;
        if (fw > 0.5) dc.DrawRectangle(new SolidColorBrush(Color), null, new Rect(0, 0, fw, h));
    }
}
