using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using HectorBravo.Gadgets.Core;

namespace HectorBravo.Gadgets.DigitalClock;

/// <summary>Digital Clock gadget: big HH:MM[:SS], date line, optional info lines + uptime.</summary>
public class MainWindow : GadgetWindow
{
    private static readonly string[] Months =
    {
        "January", "February", "March", "April", "May", "June",
        "July", "August", "September", "October", "November", "December"
    };
    private static readonly string[] Weekdays =
    {
        "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"
    };

    private readonly ConfigStore<ClockSettings> _cfg;
    private readonly DispatcherTimer _timer;

    private readonly TextBlock _time = new();
    private readonly TextBlock _suffix = new();
    private readonly TextBlock _date = new();
    private readonly TextBlock _info = new();
    private readonly TextBlock _uptime = new();

    public MainWindow() : base("DigitalClock", new ConfigStore<BaseSettings>("DigitalClock"))
    {
        _cfg = new ConfigStore<ClockSettings>("DigitalClock");
        Title = "Digital Clock";

        var root = new StackPanel { Margin = new Thickness(12, 8, 12, 8) };
        ContentHost.Children.Add(root);

        // big time row: HH:MM + smaller :SS (or AM/PM in 12h mode)
        var timeRow = new StackPanel { Orientation = Orientation.Horizontal };
        _time.Foreground = Theme.Blue;
        _time.FontSize = 40;
        _time.FontWeight = FontWeights.Bold;
        timeRow.Children.Add(_time);

        _suffix.Foreground = Theme.Fg;
        _suffix.FontSize = 20;
        _suffix.VerticalAlignment = VerticalAlignment.Bottom;
        _suffix.Margin = new Thickness(2, 0, 0, 6);
        timeRow.Children.Add(_suffix);

        root.Children.Add(timeRow);

        _date.Foreground = Theme.Yellow;
        _date.FontSize = 13;
        _date.Margin = new Thickness(0, 2, 0, 0);
        root.Children.Add(_date);

        _info.Foreground = Theme.Green;
        _info.FontSize = 12;
        _info.Margin = new Thickness(0, 4, 0, 0);
        root.Children.Add(_info);

        _uptime.Foreground = Theme.Fg;
        _uptime.FontSize = 12;
        _uptime.Margin = new Thickness(0, 3, 0, 0);
        root.Children.Add(_uptime);

        OpenOptions = () => { var w = new OptionsWindow(_cfg) { Owner = this }; w.Show(); };

        // refresh a hair after each wall-clock second boundary
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _timer.Tick += (_, _) => Refresh();
        Loaded += (_, _) => Refresh();
        _timer.Start();

        var updater = new UpdateNotifier(this, new UpdateService("digital-clock", "DigitalClock.exe"));
    }

    private void Refresh()
    {
        var s = Sampler.Sample();
        var opt = _cfg.Data;
        var now = s.Now;

        int h24 = now.Hour;
        if (opt.Use12h)
        {
            int h12 = h24 % 12 == 0 ? 12 : h24 % 12;
            _time.Text = $"{h12:00}:{now.Minute:00}";
            _suffix.Text = opt.ShowSeconds ? $":{now.Second:00}" : (h24 < 12 ? "AM" : "PM");
        }
        else
        {
            _time.Text = $"{h24:00}:{now.Minute:00}";
            _suffix.Text = opt.ShowSeconds ? $":{now.Second:00}" : "";
        }

        if (opt.ShowDate)
        {
            string wd = Weekdays[(int)now.DayOfWeek];
            string date = $"{now.Day} {Months[now.Month - 1]}, {now.Year}";
            _date.Text = opt.Weekday switch
            {
                WeekdayPos.Before => $"{wd} {date}",
                WeekdayPos.After => $"{date} ({wd})",
                _ => date,
            };
            _date.Visibility = Visibility.Visible;
        }
        else _date.Visibility = Visibility.Collapsed;

        var parts = new List<string>();
        if (opt.ShowDayOfYear) parts.Add($"Day: {s.DayOfYear}");
        if (opt.ShowWeek) parts.Add($"Week: {s.IsoWeek}");
        if (opt.ShowQuarter) parts.Add($"Quarter: {s.Quarter}");
        _info.Text = string.Join("  ", parts);
        _info.Visibility = parts.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

        if (opt.ShowUptime)
        {
            _uptime.Text = "UT Current: " + Sampler.FmtUptime(s.UptimeSecs);
            _uptime.Visibility = Visibility.Visible;
        }
        else _uptime.Visibility = Visibility.Collapsed;
    }
}
