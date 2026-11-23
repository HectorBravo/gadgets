using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using HectorBravo.Gadgets.Core;

namespace HectorBravo.Gadgets.DrivesMeter;

/// <summary>Drives Meter gadget: per-drive space + read/write rates + aggregated activity graph.</summary>
public class MainWindow : GadgetWindow
{
    private readonly ConfigStore<DrivesSettings> _cfg;
    private readonly Sampler _sampler = new();
    private readonly DispatcherTimer _timer;

    private readonly StackPanel _driveRows = new();
    private readonly LineGraph _graph = new();

    public MainWindow() : base("DrivesMeter", new ConfigStore<BaseSettings>("DrivesMeter"))
    {
        _cfg = new ConfigStore<DrivesSettings>("DrivesMeter");
        Title = "Drives";

        var root = new StackPanel { Margin = new Thickness(8, 6, 8, 4) };
        ContentHost.Children.Add(root);

        // ── header: [icon] Drives Meter ──
        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.ColumnDefinitions.Add(new ColumnDefinition());

        var icon = new TextBlock { Text = "🗄", FontSize = 16, VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(icon, 0); header.Children.Add(icon);

        var title = new TextBlock
        {
            Text = "Drives Meter",
            Foreground = Theme.TitleFg,
            FontSize = 15,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(6, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(title, 1); header.Children.Add(title);

        root.Children.Add(header);

        // ── per-drive blocks ──
        _driveRows.Margin = new Thickness(0, 4, 0, 2);
        root.Children.Add(_driveRows);

        // ── aggregated activity graph ──
        _graph.Height = 60;
        _graph.LegendFormat = v => Fmt.Rate(v);
        root.Children.Add(_graph);

        OpenOptions = () => { var w = new OptionsWindow(_cfg) { Owner = this }; w.Show(); };

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => Refresh();
        Loaded += (_, _) => Refresh();
        _timer.Start();

        var updater = new UpdateNotifier(this, new UpdateService("drives-meter", "DrivesMeter.exe"));
    }

    /// <summary>Active drive letters: slots up to the first "off", skipping blanks.</summary>
    private List<string> ActiveSlots()
    {
        var list = new List<string>();
        foreach (var raw in _cfg.Data.Slots)
        {
            var s = (raw ?? "").Trim();
            if (s.Length == 0) continue;
            if (string.Equals(s, "off", StringComparison.OrdinalIgnoreCase)) break;
            list.Add(s);
        }
        return list;
    }

    private void Refresh()
    {
        var samples = _sampler.Sample(ActiveSlots());

        _driveRows.Children.Clear();
        foreach (var d in samples)
            AddDriveRow(d);

        if (_cfg.Data.ShowGraph)
        {
            _graph.Visibility = Visibility.Visible;
            _graph.SetSeries(new List<Series> { new("activity", _sampler.ActivityHist, Theme.Hex("#87cefa")) });
        }
        else
        {
            _graph.Visibility = Visibility.Collapsed;
        }
    }

    private void AddDriveRow(DriveSample d)
    {
        double total = d.Total > 0 ? d.Total : 1;
        double used = (double)d.Total - d.Free;
        double frac = Math.Clamp(used / total, 0, 1);

        var block = new StackPanel { Margin = new Thickness(0, 2, 0, 4) };

        // drive letter + % badge on the right
        var head = new Grid();
        head.ColumnDefinitions.Add(new ColumnDefinition());
        head.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var name = new TextBlock { Foreground = Theme.Fg, FontSize = 13, FontWeight = FontWeights.Bold };
        name.Inlines.Add(new Run(d.Letter) { Foreground = Theme.TitleFg });
        if (!string.IsNullOrEmpty(d.Label))
            name.Inlines.Add(new Run("  " + d.Label) { Foreground = Theme.Muted, FontWeight = FontWeights.Normal, FontSize = 11 });
        Grid.SetColumn(name, 0); head.Children.Add(name);

        var pct = new TextBlock
        {
            Text = $"{frac * 100:0}%",
            Foreground = Theme.Usage(frac),
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        Grid.SetColumn(pct, 1); head.Children.Add(pct);

        block.Children.Add(head);

        // Used / Free
        var line2 = new TextBlock { FontSize = 11, Margin = new Thickness(0, 1, 0, 0) };
        line2.Inlines.Add(new Run("Used: ") { Foreground = Theme.Muted });
        line2.Inlines.Add(new Run(Fmt.Bytes(used)) { Foreground = Theme.Green });
        line2.Inlines.Add(new Run("   Free: ") { Foreground = Theme.Muted });
        line2.Inlines.Add(new Run(Fmt.Bytes(d.Free)) { Foreground = Theme.Green });
        block.Children.Add(line2);

        // Total + read/write rates
        var line3 = new TextBlock { FontSize = 11, Margin = new Thickness(0, 1, 0, 0) };
        line3.Inlines.Add(new Run("Total: ") { Foreground = Theme.Muted });
        line3.Inlines.Add(new Run(Fmt.Bytes(d.Total)) { Foreground = Theme.Green });
        line3.Inlines.Add(new Run("   ▲") { Foreground = Theme.Yellow });
        line3.Inlines.Add(new Run(Fmt.Rate(d.ReadBps)) { Foreground = Theme.Yellow });
        line3.Inlines.Add(new Run(" ▼") { Foreground = Theme.Yellow });
        line3.Inlines.Add(new Run(Fmt.Rate(d.WriteBps)) { Foreground = Theme.Yellow });
        block.Children.Add(line3);

        // usage bar
        var bar = new UsageBar { Height = 8, Margin = new Thickness(0, 3, 0, 0), Fraction = frac, Color = Theme.Hex("#158000") };
        block.Children.Add(bar);

        _driveRows.Children.Add(block);
    }
}

