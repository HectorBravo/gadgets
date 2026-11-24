using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using HectorBravo.Gadgets.Core;

namespace HectorBravo.Gadgets.DigitalClock;

/// <summary>Digital Clock gadget: big HH:MM[:SS], date line, optional info lines + uptime.</summary>
public class MainWindow : GadgetWindow<ClockSettings>
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

    private readonly DispatcherTimer _timer;

    private readonly TextBlock _time = new();
    private readonly TextBlock _suffix = new();
    private readonly TextBlock _date = new();
    private readonly TextBlock _info = new();
    private readonly TextBlock _uptime = new();
    private readonly TextBlock _syncStatus = new();

    private bool _syncing;

    public MainWindow() : base("DigitalClock", new ConfigStore<ClockSettings>("DigitalClock"))
    {
        Title = "Digital Clock";

        var root = new StackPanel { Margin = new Thickness(12, 8, 12, 8) };
        ContentHost.Children.Add(root);

        // big time row: HH:MM + smaller :SS (or AM/PM in 12h mode) — all light blue
        var timeRow = new StackPanel { Orientation = Orientation.Horizontal };
        _time.Foreground = Theme.Blue;
        _time.FontSize = 40;
        _time.FontWeight = FontWeights.Bold;
        timeRow.Children.Add(_time);

        _suffix.Foreground = Theme.Blue;      // seconds share the HH:MM light blue (#C2)
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

        // "Sync time" row: icon + text (clickable) + result of the last sync (#C1)
        var syncRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 6, 0, 0),
            Cursor = System.Windows.Input.Cursors.Hand,
            ToolTip = "Synchronize the system clock with the NTP server (server set in options). Applying needs administrator rights.",
        };
        syncRow.Children.Add(new TextBlock { Text = "\uD83D\uDD59", FontSize = 12 });   // 🕙
        syncRow.Children.Add(new TextBlock
        {
            Text = " Sync time",
            Foreground = Theme.Blue,
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center,
        });
        _syncStatus.Foreground = Theme.Muted;
        _syncStatus.FontSize = 11;
        _syncStatus.VerticalAlignment = VerticalAlignment.Center;
        _syncStatus.Margin = new Thickness(6, 0, 0, 0);
        syncRow.Children.Add(_syncStatus);
        syncRow.MouseLeftButtonUp += (_, e) => { e.Handled = true; _ = SyncTimeAsync(); };
        root.Children.Add(syncRow);

        OpenOptions = () => { var w = new OptionsWindow(Cfg) { Owner = this }; w.Show(); };

        // refresh a hair after each wall-clock second boundary
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _timer.Tick += (_, _) => Refresh();
        Loaded += (_, _) => Refresh();
        _timer.Start();

        var updater = new UpdateNotifier<ClockSettings>(this, new UpdateService("digital-clock", "DigitalClock.exe"));
    }

    /// <summary>Query the NTP server and (if allowed) step the system clock.</summary>
    private async Task SyncTimeAsync()
    {
        if (_syncing) return;
        _syncing = true;
        _syncStatus.Text = "querying…";
        try
        {
            var r = await SntpClient.QueryAsync(Cfg.Data.NtpServer);
            var ok = TimeSync.ApplyOffset(r.Offset);
            _syncStatus.Text = ok
                ? $"ok ({r.Offset.TotalSeconds:+0.00;-0.00}s)"
                : $"{r.Offset.TotalSeconds:+0.00;-0.00}s — needs admin to apply";
        }
        catch (Exception ex)
        {
            _syncStatus.Text = "failed: " + ex.Message;
        }
        finally
        {
            _syncing = false;
        }
    }

    private void Refresh()
    {
        var s = Sampler.Sample();
        var opt = Cfg.Data;
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

