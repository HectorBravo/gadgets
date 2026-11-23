using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using HectorBravo.Gadgets.Core;

namespace HectorBravo.Gadgets.CpuMeter;

/// <summary>CPU Usage gadget (matches the original layout).</summary>
public class MainWindow : GadgetWindow
{
    private readonly ConfigStore<CpuSettings> _cfg;
    private readonly Sampler _sampler = new();
    private readonly DispatcherTimer _timer;

    // history for the mini graph at the bottom
    private readonly List<double> _totalHist = new();
    private const int HistLen = 120;

    // dynamic UI parts
    private readonly TextBlock _totalPct = new();
    private readonly TextBlock _clock = new();
    private readonly StackPanel _memRows = new();
    private readonly ItemsControl _coresHost = new();
    private readonly LineGraph _graph = new();

    public MainWindow() : base("CpuMeter", new ConfigStore<BaseSettings>("CpuMeter"))
    {
        _cfg = OpenCfg();
        Title = "CPU Usage";

        var root = new StackPanel { Margin = new Thickness(8, 6, 8, 4) };
        ContentHost.Children.Add(root);

        // ── header: [icon] CPU Usage   15% ──
        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.ColumnDefinitions.Add(new ColumnDefinition());
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var icon = new TextBlock { Text = "🖥", FontSize = 16, VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(icon, 0); header.Children.Add(icon);

        var title = new TextBlock
        {
            Text = "CPU Usage",
            Foreground = Theme.TitleFg,
            FontSize = 15,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(6, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(title, 1); header.Children.Add(title);

        _totalPct.Foreground = Theme.Green;
        _totalPct.FontSize = 15;
        _totalPct.FontWeight = FontWeights.Bold;
        _totalPct.HorizontalAlignment = HorizontalAlignment.Right;
        Grid.SetColumn(_totalPct, 2); header.Children.Add(_totalPct);

        root.Children.Add(header);

        // ── Clock: xxxxMHz ──
        _clock.Foreground = Theme.Yellow;
        _clock.FontSize = 12;
        _clock.Margin = new Thickness(0, 1, 0, 4);
        root.Children.Add(_clock);

        // ── RAM/swap rows ──
        _memRows.Margin = new Thickness(0, 0, 0, 4);
        root.Children.Add(_memRows);

        // ── per-core rows ──
        _coresHost.Margin = new Thickness(0, 0, 0, 2);
        root.Children.Add(_coresHost);

        // ── total history graph ──
        _graph.Height = 46;
        _graph.LegendFormat = v => $"{v:0}%";
        root.Children.Add(_graph);

        OpenOptions = () => { var w = new OptionsWindow(_cfg) { Owner = this }; w.Show(); };

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => Refresh();
        Loaded += (_, _) => Refresh();
        _timer.Start();

        var updater = new UpdateNotifier(this, new UpdateService("cpu-meter", "CpuMeter.exe"));
    }

    private static ConfigStore<CpuSettings> OpenCfg() => new("CpuMeter");

    private void Refresh()
    {
        var s = _sampler.Sample();
        _totalPct.Text = $"{s.TotalPct:0}%";
        _totalPct.Foreground = Theme.Usage(s.TotalPct / 100);
        _clock.Text = $"Clock: {s.ClockMhz}MHz";

        // RAM + swap rows (Used / Total + % + bar)
        _memRows.Children.Clear();
        AddMemRow("RAM", s.RamUsed, s.RamFree, s.RamTotal);
        AddMemRow("Swap", s.SwapUsed, s.SwapFree, s.SwapTotal);

        // per-core rows: [n] bar  Core n   pct%
        var panel = new Grid();
        int i = 0;
        foreach (var pct in s.CorePct)
        {
            var row = new Grid();
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(34) });   // [n]
            row.ColumnDefinitions.Add(new ColumnDefinition());                                 // bar
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(58) });    // name
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(38) });    // pct

            var idx = new TextBlock { Text = $"[{i}]", Foreground = Theme.Muted, FontSize = 10.5, FontFamily = Theme.Mono };
            Grid.SetColumn(idx, 0); row.Children.Add(idx);

            var bar = new UsageBar { Height = 12, Margin = new Thickness(2, 2, 4, 2), Fraction = pct / 100.0, Color = Theme.CoreColors[i % Theme.CoreColors.Length] };
            Grid.SetColumn(bar, 1); row.Children.Add(bar);

            string name = i < 10 ? $"{i + 1} Core" : $"Core {i - 9}"; // matches original's odd numbering
            var lbl = new TextBlock { Text = name, Foreground = Theme.Muted, FontSize = 10.5 };
            Grid.SetColumn(lbl, 2); row.Children.Add(lbl);

            var p = new TextBlock
            {
                Text = $"{pct:0}%",
                Foreground = Theme.Usage(pct / 100),
                FontSize = 10.5,
                HorizontalAlignment = HorizontalAlignment.Right,
            };
            Grid.SetColumn(p, 3); row.Children.Add(p);

            panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            row.Margin = new Thickness(0, 0.5, 0, 0.5);
            Grid.SetRow(row, i);
            panel.Children.Add(row);
            i++;
        }
        _coresHost.Items.Clear();
        _coresHost.Items.Add(panel);

        // graph history
        _totalHist.Add(s.TotalPct);
        if (_totalHist.Count > HistLen) _totalHist.RemoveAt(0);
        _graph.SetSeries(new List<Series> { new("total", _totalHist, Theme.Hex("#bfffb2")) });
    }

    private void AddMemRow(string label, ulong used, ulong free, ulong total)
    {
        double frac = total > 0 ? (double)used / total : 0;

        var g = new Grid();
        g.ColumnDefinitions.Add(new ColumnDefinition());
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(64) });

        var left = new StackPanel();
        var line1 = new TextBlock { FontSize = 11 };
        line1.Inlines.Add(new Run(label + ": ") { Foreground = Theme.Muted });
        line1.Inlines.Add(new Run($"{Fmt.Bytes(used)}") { Foreground = Theme.Green });
        line1.Inlines.Add(new Run(" / ") { Foreground = Theme.Muted });
        line1.Inlines.Add(new Run($"{Fmt.Bytes(total)}") { Foreground = Theme.Green });
        left.Children.Add(line1);

        var bar = new UsageBar { Height = 10, Margin = new Thickness(0, 2, 6, 0), Fraction = frac, Color = Theme.Hex("#158000") };
        left.Children.Add(bar);
        Grid.SetColumn(left, 0); g.Children.Add(left);

        var pct = new TextBlock
        {
            Text = $"{frac * 100:0}%",
            Foreground = Theme.Usage(frac),
            FontSize = 11,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
        };
        Grid.SetColumn(pct, 1); g.Children.Add(pct);

        g.Margin = new Thickness(0, 1, 0, 3);
        _memRows.Children.Add(g);
    }
}
