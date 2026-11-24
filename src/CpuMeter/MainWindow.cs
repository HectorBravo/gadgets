using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using HectorBravo.Gadgets.Core;

namespace HectorBravo.Gadgets.CpuMeter;

/// <summary>CPU Usage gadget: total %, clock, RAM/swap, per-core rows (1-based), history graph.</summary>
public class MainWindow : GadgetWindow<CpuSettings>
{
    private readonly Sampler _sampler = new();
    private readonly DispatcherTimer _timer;

    // history for the mini graph at the bottom (ring buffer — never grows past capacity, #18)
    private readonly Ring _totalHist = new(600);
    private int _histCap = 120;

    // dynamic UI parts
    private readonly TextBlock _totalPct = new();
    private readonly TextBlock _clock = new();
    private readonly StackPanel _memRows = new();
    private readonly Grid _coresHost = new();
    private readonly LineGraph _graph = new();

    // reused per-core row visuals (rebuilt only when the core count changes)
    private List<(TextBlock idx, UsageBar bar, TextBlock mhz, TextBlock pct)> _rows = new();

    public MainWindow() : base("CpuMeter", new ConfigStore<CpuSettings>("CpuMeter"))
    {
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
            Foreground = Theme.TitleFg,          // white + bold (#13)
            FontSize = 15,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(6, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(title, 1); header.Children.Add(title);

        _totalPct.FontSize = 15;                 // percent keeps the usage ramp (#20 clarification)
        _totalPct.FontWeight = FontWeights.Bold;
        _totalPct.HorizontalAlignment = HorizontalAlignment.Right;
        Grid.SetColumn(_totalPct, 2); header.Children.Add(_totalPct);

        root.Children.Add(header);

        // ── Clock: xxxxMHz ──
        _clock.Foreground = Theme.Blue;          // value → light blue (#20)
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
        _graph.Height = Math.Clamp(Cfg.Data.GraphHeight, 30, 240);
        _graph.Capacity = _histCap;
        _graph.LegendFormat = v => $"{v:0}%";
        root.Children.Add(_graph);

        // keep a handle so options can toggle the graph live

        OpenOptions = () => { var w = new OptionsWindow(Cfg) { Owner = this }; w.Show(); };

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => Refresh();
        Loaded += (_, _) => Refresh();
        _timer.Start();

        var updater = new UpdateNotifier<CpuSettings>(this, new UpdateService("cpu-meter", "CpuMeter.exe"));
    }

    /// <summary>Options hooks: live graph visibility / height and RAM-Swap rebuild.</summary>
    public void ApplyGraphVisibility() => _graph.Visibility = Cfg.Data.ShowGraph ? Visibility.Visible : Visibility.Collapsed;

    public void ApplyGraphHeight(double h) => _graph.Height = Math.Clamp(h, 30, 240);

    public void InvalidateMemRows() => _memBuilt = false;

    private void Refresh()
    {
        var s = _sampler.Sample();

        // header: total % keeps the green/yellow/red ramp (user clarification)
        _totalPct.Text = $"{s.TotalPct:0}%";
        _totalPct.Foreground = Theme.Usage(s.TotalPct / 100);
        _clock.Text = $"{s.ClockMhz}MHz";

        // RAM/swap rows are rebuilt only when their visibility toggles (cheap otherwise)
        RebuildMemRows(s);

        // per-core rows: [n] bar  mhz   pct% — created once, updated in place (#21)
        if (_rows.Count != s.Cores.Count) RebuildCoreRows(s.Cores.Count);
        for (int i = 0; i < s.Cores.Count; i++)
        {
            var c = s.Cores[i];
            var row = _rows[i];
            row.idx.Text = $"{i + 1}";                                   // numbering starts at 1 (#P1)
            row.bar.Fraction = c.Pct / 100.0;
            row.mhz.Text = c.ClockMhz > 0 ? $"{c.ClockMhz}" : "—";       // effective clock, no "Core n" text (#P2)
            row.pct.Text = $"{c.Pct:0}%";
            row.pct.Foreground = Theme.Usage(c.Pct / 100);               // percent keeps the ramp
        }

        // graph history (ring buffer trimmed to the configured capacity — #18)
        int cap = Math.Clamp((int)Cfg.Data.HistorySeconds, 30, _totalHist.Capacity);
        if (cap != _histCap) { _histCap = cap; _graph.Capacity = cap; }
        _totalHist.Add(s.TotalPct);
        var snap = _totalHist.Snapshot();
        if (snap.Length > _histCap)
        {
            var trimmed = new double[_histCap];
            Array.Copy(snap, snap.Length - _histCap, trimmed, 0, _histCap);
            snap = trimmed;
        }
        _graph.SetSeries(new List<Series> { new("total", snap, Theme.Hex("#bfffb2")) });
    }

    private void RebuildCoreRows(int count)
    {
        _coresHost.Children.Clear();
        _rows.Clear();
        for (int i = 0; i < count; i++)
        {
            var rowGrid = new Grid { Margin = new Thickness(0, 0.5, 0, 0.5) };
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) });   // n
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition());                                 // bar
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(56) });    // MHz
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(38) });    // pct

            var idx = new TextBlock { Foreground = Theme.White, FontSize = 10.5, FontFamily = Theme.Mono };   // label → white (#20)
            Grid.SetColumn(idx, 0); rowGrid.Children.Add(idx);

            var bar = new UsageBar
            {
                Height = 12,
                Margin = new Thickness(2, 2, 4, 2),
                Color = Theme.CoreColors[i % Theme.CoreColors.Length],
            };
            Grid.SetColumn(bar, 1); rowGrid.Children.Add(bar);

            // kind color for the clock-speed label (#P3): cores white, E-cores green, threads light blue
            var kind = _sampler.KindFor(i);
            var mhz = new TextBlock
            {
                FontSize = 10.5,
                Foreground = kind switch
                {
                    CoreKind.EfficientCore => Theme.Green,
                    CoreKind.Thread => Theme.Blue,
                    _ => Theme.White,
                },
            };
            Grid.SetColumn(mhz, 2); rowGrid.Children.Add(mhz);

            var pct = new TextBlock { FontSize = 10.5, HorizontalAlignment = HorizontalAlignment.Right };
            Grid.SetColumn(pct, 3); rowGrid.Children.Add(pct);

            _coresHost.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            Grid.SetRow(rowGrid, i);
            _coresHost.Children.Add(rowGrid);
            _rows.Add((idx, bar, mhz, pct));
        }
    }

    private bool _memBuilt;
    private TextBlock? _ramLine, _swapLine;

    private void RebuildMemRows(Snapshot s)
    {
        if (!_memBuilt)
        {
            _memRows.Children.Clear();
            _ramLine = new TextBlock { FontSize = 11 };
            _memRows.Children.Add(_ramLine);
            if (Cfg.Data.ShowSwap)
            {
                _swapLine = new TextBlock { FontSize = 11, Margin = new Thickness(0, 2, 0, 0) };
                _memRows.Children.Add(_swapLine);
            }
            else _swapLine = null;
            _memBuilt = true;
        }

        // labels white (#20), values light blue — percent text keeps the ramp where shown
        SetMemLine(_ramLine!, "RAM", s.RamUsed, s.RamTotal);
        if (_swapLine != null && Cfg.Data.ShowSwap) SetMemLine(_swapLine, "Swap", s.SwapUsed, s.SwapTotal);
    }

    private static void SetMemLine(TextBlock line, string label, ulong used, ulong total)
    {
        double frac = total > 0 ? (double)used / total : 0;
        line.Inlines.Clear();
        line.Inlines.Add(new Run(label + ": ") { Foreground = Theme.White });
        line.Inlines.Add(new Run(Fmt.Bytes(used)) { Foreground = Theme.Blue });
        line.Inlines.Add(new Run(" / ") { Foreground = Theme.White });
        line.Inlines.Add(new Run(Fmt.Bytes(total)) { Foreground = Theme.Blue });
        line.Inlines.Add(new Run($"  ({frac * 100:0}%)") { Foreground = Theme.Usage(frac) });
    }
}

