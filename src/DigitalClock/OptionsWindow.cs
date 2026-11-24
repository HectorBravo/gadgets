using System.Windows;
using System.Windows.Controls;
using HectorBravo.Gadgets.Core;

namespace HectorBravo.Gadgets.DigitalClock;

/// <summary>Options window for the Digital Clock: Format / Lines / NTP tabs (light, no scrollbars).</summary>
public class OptionsWindow : OptionsWindowBase
{
    private readonly ConfigStore<ClockSettings> _cfg;

    public OptionsWindow(ConfigStore<ClockSettings> cfg) : base("Digital Clock — Options")
    {
        _cfg = cfg;

        // ── Format tab ──
        var fp = new StackPanel { Margin = new Thickness(12) };
        fp.Children.Add(Check("12-hour clock (AM/PM)", cfg.Data.Use12h, v => { cfg.Data.Use12h = v; cfg.Save(); }));
        fp.Children.Add(Check("Show seconds", cfg.Data.ShowSeconds, v => { cfg.Data.ShowSeconds = v; cfg.Save(); }));
        fp.Children.Add(Check("Show date", cfg.Data.ShowDate, v => { cfg.Data.ShowDate = v; cfg.Save(); }));

        fp.Children.Add(new TextBlock { Text = "Weekday name:", Foreground = MenuStyle.Fg, Margin = new Thickness(0, 8, 0, 2) });
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
                Foreground = MenuStyle.Fg,
                IsChecked = cfg.Data.Weekday == pos,
                Margin = new Thickness(12, 2, 0, 2),
            };
            rb.Checked += (_, _) => { cfg.Data.Weekday = captured; cfg.Save(); };
            fp.Children.Add(rb);
        }
        AddTab("Format", fp);

        // ── Lines tab ──
        var lp = new StackPanel { Margin = new Thickness(12) };
        lp.Children.Add(Check("Day of year", cfg.Data.ShowDayOfYear, v => { cfg.Data.ShowDayOfYear = v; cfg.Save(); }));
        lp.Children.Add(Check("Week number (ISO)", cfg.Data.ShowWeek, v => { cfg.Data.ShowWeek = v; cfg.Save(); }));
        lp.Children.Add(Check("Quarter", cfg.Data.ShowQuarter, v => { cfg.Data.ShowQuarter = v; cfg.Save(); }));
        lp.Children.Add(Check("System uptime (since boot)", cfg.Data.ShowUptime, v => { cfg.Data.ShowUptime = v; cfg.Save(); }));
        AddTab("Lines", lp);

        // ── NTP tab (#C1) ──
        var np = new StackPanel { Margin = new Thickness(12) };
        np.Children.Add(Hint("Server used by the \"Sync time\" button on the gadget. Clicking Sync queries it and steps the system clock (applying requires running the gadget as administrator)."));
        np.Children.Add(Row("NTP server:", Field(cfg.Data.NtpServer, s =>
        {
            var v = (s ?? "").Trim();
            if (v.Length == 0) v = "pool.ntp.org";
            if (!string.Equals(_cfg.Data.NtpServer, v, System.StringComparison.Ordinal))
            {
                _cfg.Data.NtpServer = v;
                _cfg.Save();                       // saved immediately
            }
        })));
        AddTab("NTP", np);

        FinalizeSize();
    }
}

