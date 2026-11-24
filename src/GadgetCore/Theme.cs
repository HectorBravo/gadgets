using System.Windows;
using System.Windows.Media;

namespace HectorBravo.Gadgets.Core;

/// <summary>
/// Shared "gadget" palette. Dark opaque panel, white bold titles, white labels,
/// light-blue values (Theme.Blue), green/yellow/red ramp for percent-of-load
/// values. Menus + options windows use the separate light palette (MenuStyle).
/// </summary>
public static class Theme
{
    public static Color Hex(string s) => (Color)ColorConverter.ConvertFromString(s);

    // Panel + borders
    public static readonly Brush BgBrush  = Frozen(Hex("#0d1117"));           // panel fill
    public static readonly Brush PanelBg   = Frozen(Hex("#0d1117"));          // OPAQUE — window opacity alone controls translucency
    public static readonly Brush Border    = Frozen(Hex("#46506e")); // thin blue-grey border
    public static readonly Brush TitleBar  = Frozen(Color.FromArgb(200, 20, 26, 38));

    // Text
    public static readonly Brush White     = Frozen(Hex("#ffffff"));   // titles (bold) + labels
    public static readonly Brush Fg        = Frozen(Hex("#e6e9ef"));
    public static readonly Brush Muted     = Frozen(Hex("#8a93a6"));   // de-emphasised text only
    public static readonly Brush TitleFg   = Frozen(Hex("#ffffff"));   // gadget title (white, bold)

    // Values / states
    public static readonly Brush Green     = Frozen(Hex("#bfffb2"));   // good values / reads / upload
    public static readonly Brush DeepGreen = Frozen(Hex("#158000"));   // green bars
    public static readonly Brush Yellow    = Frozen(Hex("#ffe14d"));   // writes / download / clock date line
    public static readonly Brush Amber     = Frozen(Hex("#ffb2b2"));   // (kept for compat)
    public static readonly Brush Red       = Frozen(Hex("#ff5a5a"));   // high usage % / down
    public static readonly Brush Orange    = Frozen(Hex("#e0a030"));
    public static readonly Brush Blue      = Frozen(Hex("#87cefa"));   // light blue — values (same as clock HH:MM)

    // Graph
    public static readonly Brush GraphBg   = Frozen(Color.FromArgb(255, 6, 8, 12));
    public static readonly Pen GridPen     = FrozenPen(Hex("#1c2130"));
    public static readonly Pen BorderPen   = FrozenPen(Hex("#46506e"));

    /// <summary>Green → yellow → red ramp for a 0..1 usage fraction (percent-of-load values keep this).</summary>
    public static Brush Usage(double frac) =>
        frac < 0.6 ? Green : frac < 0.85 ? Yellow : Red;

    /// <summary>Cycled per-core bar colors (like the original CPU gadget rows).</summary>
    public static readonly Color[] CoreColors =
    {
        Hex("#46a0ff"), Hex("#158000"), Hex("#f0c846"), Hex("#e6783c"),
        Hex("#dc505a"), Hex("#b45adc"), Hex("#50c8c8"), Hex("#a0aab4"),
    };

    public static readonly FontFamily Ui = new("Segoe UI");
    public static readonly FontFamily Mono = new("Consolas, Courier New, monospace");

    private static Brush Frozen(Color c) { var b = new SolidColorBrush(c); b.Freeze(); return b; }
    private static Pen FrozenPen(Color c) { var p = new Pen(new SolidColorBrush(c), 1); p.Freeze(); return p; }
}

