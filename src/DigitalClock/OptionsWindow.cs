using System.Windows;
using System.Windows.Controls;
using HectorBravo.Gadgets.Core;

namespace HectorBravo.Gadgets.DigitalClock;

/// <summary>Options window for the Digital Clock: Format / Lines / General tabs.</summary>
public class OptionsWindow : Window
{
    private readonly ConfigStore<ClockSettings> _cfg;

    public OptionsWindow(ConfigStore<ClockSettings> cfg)
    {
        _cfg = cfg;
        Title = "Digital Clock — Options";
        Width = 360; Height = 320;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new System.Windows.Media.SolidColorBrush(Theme.Hex("#161a23"));

        var tabs = new TabControl();

        // ── Format tab ──
        var format = new TabItem { Header = "Format" };
        var fp = new StackPanel { Margin = new Thickness(12) };
        fp.Children.Add(Check("12-hour clock (AM/PM)", cfg.Data.Use12h, v => cfg.Data.Use12h = v));
        fp.Children.Add(Check("Show seconds", cfg.Data.ShowSeconds, v => cfg.Data.ShowSeconds = v));
        fp.Children.Add(Check("Show date", cfg.Data.ShowDate, v => cfg.Data.ShowDate = v));

        fp.Children.Add(new TextBlock { Text = "Weekday name:", Foreground = Theme.Fg, Margin = new Thickness(0, 8, 0, 2) });
        foreach (var (pos, label) in new[]
        {
            (WeekdayPos.None, "Hidden"),
            (WeekdayPos.Before, "Before date"),
            (WeekdayPos.After, "After date"),
        })
        {
            WeekdayPos captured = pos;
            var rb = new RadioButton
            {
                Content = label,
                Foreground = Theme.Fg,
                IsChecked = cfg.Data.Weekday == pos,
                Margin = new Thickness(12, 2, 0, 2),
            };
            rb.Checked += (_, _) => { cfg.Data.Weekday = captured; cfg.Save(); };
            fp.Children.Add(rb);
        }
        format.Content = fp;
        tabs.Items.Add(format);

        // ── Lines tab ──
        var lines = new TabItem { Header = "Lines" };
        var lp = new StackPanel { Margin = new Thickness(12) };
        lp.Children.Add(Check("Day of year", cfg.Data.ShowDayOfYear, v => cfg.Data.ShowDayOfYear = v));
        lp.Children.Add(Check("Week number (ISO)", cfg.Data.ShowWeek, v => cfg.Data.ShowWeek = v));
        lp.Children.Add(Check("Quarter", cfg.Data.ShowQuarter, v => cfg.Data.ShowQuarter = v));
        lp.Children.Add(Check("System uptime (since boot)", cfg.Data.ShowUptime, v => cfg.Data.ShowUptime = v));
        lines.Content = lp;
        tabs.Items.Add(lines);

        // ── General tab ──
        var general = new TabItem { Header = "General" };
        var gp = new StackPanel { Margin = new Thickness(12) };
        gp.Children.Add(new TextBlock { Text = "Digital Clock", Foreground = Theme.Fg, FontSize = 13, FontWeight = FontWeights.Bold });
        gp.Children.Add(new TextBlock { Text = "Drag the gadget to move it; it snaps to screen edges and other gadgets.", Foreground = Theme.Muted, FontSize = 11, Margin = new Thickness(0, 6, 0, 0) });
        gp.Children.Add(new TextBlock { Text = "Scale and opacity are in the right-click menu.", Foreground = Theme.Muted, FontSize = 11, Margin = new Thickness(0, 4, 0, 0) });
        general.Content = gp;
        tabs.Items.Add(general);

        Content = tabs;
    }

    private CheckBox Check(string text, bool initial, Action<bool> onChange)
    {
        var cb = new CheckBox
        {
            Content = text,
            IsChecked = initial,
            Foreground = Theme.Fg,
            Margin = new Thickness(0, 4, 0, 4),
        };
        cb.Checked += (_, _) => { onChange(true); _cfg.Save(); };
        cb.Unchecked += (_, _) => { onChange(false); _cfg.Save(); };
        return cb;
    }
}
