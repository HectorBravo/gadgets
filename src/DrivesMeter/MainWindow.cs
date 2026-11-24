using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using HectorBravo.Gadgets.Core;

namespace HectorBravo.Gadgets.DrivesMeter;

/// <summary>Drives Meter gadget: per-drive space + read/write rates (+ per-drive graphs) and an aggregate graph.</summary>
public class MainWindow : GadgetWindow<DrivesSettings>
{
    private readonly Sampler _sampler = new();
    private readonly DispatcherTimer _timer;

    private readonly StackPanel _driveRows = new();
    private readonly Grid _aggrRow = new();
    private readonly TextBlock _aggrReadVal = new();
    private readonly TextBlock _aggrWriteVal = new();
    private readonly LineGraph _graph = new();

    // per-drive blocks cached by letter — updated in place, rebuilt only when the set changes (#21)
    private readonly Dictionary<string, DriveBlock> _blocks = new();
    private string _blockKey = "";

    public MainWindow() : base("DrivesMeter", new ConfigStore<DrivesSettings>("DrivesMeter"))
    {
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
            Foreground = Theme.TitleFg,          // white + bold (#13)
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

        // ── aggregate reads/writes values row (#D3) ──
        _aggrRow.ColumnDefinitions.Add(new ColumnDefinition());
        _aggrRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var aggrLeft = new TextBlock { FontSize = 11 };
        aggrLeft.Inlines.Add(new Run("\u25B2 Aggr Reads: ") { Foreground = Theme.Green });   // read → green (#D7)
        Grid.SetColumn(aggrLeft, 0); _aggrRow.Children.Add(aggrLeft);
        var aggrRight = new TextBlock { FontSize = 11, HorizontalAlignment = HorizontalAlignment.Right };
        aggrRight.Inlines.Add(new Run("\u25BC Aggr Writes: ") { Foreground = Theme.Yellow }); // write → yellow (#D7)
        Grid.SetColumn(aggrRight, 1); _aggrRow.Children.Add(aggrRight);
        _aggrReadVal.Foreground = Theme.Green;
        _aggrWriteVal.Foreground = Theme.Yellow;
        aggrLeft.Inlines.Add(_aggrReadVal);
        aggrRight.Inlines.Add(_aggrWriteVal);
        _aggrRow.Margin = new Thickness(0, 2, 0, 1);
        root.Children.Add(_aggrRow);

        // ── aggregated activity graph (reads + writes) ──
        _graph.Height = Math.Clamp(Cfg.Data.GraphHeight, 30, 240);
        _graph.Capacity = Sampler.GraphLen;
        _graph.LegendFormat = v => Fmt.Rate(v);
        root.Children.Add(_graph);

        OpenOptions = () => { var w = new OptionsWindow(Cfg) { Owner = this }; w.Show(); };

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => Refresh();
        Loaded += (_, _) => Refresh();
        _timer.Start();

        var updater = new UpdateNotifier<DrivesSettings>(this, new UpdateService("drives-meter", "DrivesMeter.exe"));
    }

    /// <summary>Active drive letters: slots up to the first "off", skipping blanks.</summary>
    private List<string> ActiveSlots()
    {
        var list = new List<string>();
        foreach (var raw in Cfg.Data.Slots)
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

        // rebuild the drive blocks only when the selected set (or graph mode) changed
        string key = string.Join(",", samples.Select(s => s.Letter)) + "|" + Cfg.Data.ShowPerDriveGraph;
        if (key != _blockKey)
        {
            _blockKey = key;
            _driveRows.Children.Clear();
            _blocks.Clear();
            foreach (var d in samples)
            {
                var b = new DriveBlock(d.Letter);
                _blocks[d.Letter] = b;
                _driveRows.Children.Add(b.Root);
            }
        }

        // update the cached blocks in place (#21)
        foreach (var d in samples)
        {
            if (_blocks.TryGetValue(d.Letter, out var b))
                b.Update(d, _sampler.HistFor(d.Letter), Cfg.Data);
        }

        // aggregate values row
        double ar = Math.Max(0, _sampler.AggrReadHist.Count > 0 ? _sampler.AggrReadHist[_sampler.AggrReadHist.Count - 1] : 0);
        double aw = Math.Max(0, _sampler.AggrWriteHist.Count > 0 ? _sampler.AggrWriteHist[_sampler.AggrWriteHist.Count - 1] : 0);
        _aggrReadVal.Text = Fmt.RateU(ar, Cfg.Data.ReadWriteUnit);
        _aggrWriteVal.Text = Fmt.RateU(aw, Cfg.Data.ReadWriteUnit);

        // aggregate graph: reads (green) + writes (yellow) — both plotted now (#D3/#D7)
        if (Cfg.Data.ShowGraph)
        {
            _graph.Visibility = Visibility.Visible;
            _graph.Height = Math.Clamp(Cfg.Data.GraphHeight, 30, 240);
            _graph.SetSeries(new List<Series>
            {
                new("reads", _sampler.AggrReadHist.Snapshot(), Theme.Hex("#bfffb2")),
                new("writes", _sampler.AggrWriteHist.Snapshot(), Theme.Hex("#ffe14d")),
            });
        }
        else
        {
            _graph.Visibility = Visibility.Collapsed;
        }
    }

    /// <summary>One drive block: header, Free line + aligned bar, read/write line, optional graph.</summary>
    private sealed class DriveBlock
    {
        public StackPanel Root { get; } = new() { Margin = new Thickness(0, 2, 0, 4) };

        private readonly TextBlock _name = new();
        private readonly TextBlock _pct = new();
        private readonly TextBlock _usedLine = new();
        private readonly TextBlock _totalLine = new();
        private readonly TextBlock _freeLine = new();       // the bar aligns under this line (#D6)
        private readonly UsageBar _bar = new() { Height = 8 };
        private readonly TextBlock _rwLine = new();         // ▲ reads ▼ writes on its own line (#D7)
        private readonly LineGraph? _graph;

        public DriveBlock(string letter)
        {
            // drive letter + % badge on the right
            var head = new Grid();
            head.ColumnDefinitions.Add(new ColumnDefinition());
            head.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _name.Foreground = Theme.White;
            _name.FontSize = 13;
            _name.FontWeight = FontWeights.Bold;
            Grid.SetColumn(_name, 0); head.Children.Add(_name);

            _pct.FontSize = 13;
            _pct.FontWeight = FontWeights.Bold;
            _pct.HorizontalAlignment = HorizontalAlignment.Right;
            Grid.SetColumn(_pct, 1); head.Children.Add(_pct);

            Root.Children.Add(head);

            // Used / Total lines (labels white, values light blue — #20)
            _usedLine.FontSize = 11;
            _usedLine.Margin = new Thickness(0, 1, 0, 0);
            Root.Children.Add(_usedLine);

            _totalLine.FontSize = 11;
            _totalLine.Margin = new Thickness(0, 1, 0, 0);
            Root.Children.Add(_totalLine);

            // Free line with the usage bar right under it (#D6) — the host spans exactly
            // from where "Free:" starts to where the % badge ends (same columns as head).
            var freeRow = new Grid();
            freeRow.ColumnDefinitions.Add(new ColumnDefinition());                    // aligns with the name column
            freeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // aligns with the % badge

            var freeHost = new StackPanel { Margin = new Thickness(0, 1, 0, 0) };
            _freeLine.FontSize = 11;
            freeHost.Children.Add(_freeLine);
            _bar.Margin = new Thickness(0, 3, 0, 0);
            freeHost.Children.Add(_bar);
            Grid.SetColumn(freeHost, 0); freeRow.Children.Add(freeHost);

            Root.Children.Add(freeRow);

            // read/write line (green ▲ reads / yellow ▼ writes) (#D7)
            _rwLine.FontSize = 11;
            _rwLine.Margin = new Thickness(0, 1, 0, 0);
            Root.Children.Add(_rwLine);

            // per-drive graph (#D2)
            _graph = new LineGraph
            {
                Height = 46,
                Margin = new Thickness(0, 3, 0, 0),
                Capacity = Sampler.GraphLen,
                LegendFormat = v => Fmt.Rate(v),
            };
            Root.Children.Add(_graph);
        }

        public void Update(DriveSample d, (HectorBravo.Gadgets.Core.Ring read, HectorBravo.Gadgets.Core.Ring write) hist, DrivesSettings cfg)
        {
            double total = d.Total > 0 ? d.Total : 1;
            double used = (double)d.Total - d.Free;
            double frac = Math.Clamp(used / total, 0, 1);

            // letter + volume label at the SAME font size as the letter (#D9)
            _name.Inlines.Clear();
            _name.Inlines.Add(new Run(d.Letter));
            if (!string.IsNullOrEmpty(d.Label))
                _name.Inlines.Add(new Run("  " + d.Label) { FontWeight = FontWeights.Normal });

            // percent keeps the usage ramp (user clarification)
            _pct.Text = $"{frac * 100:0}%";
            _pct.Foreground = Theme.Usage(frac);

            SetValuedLine(_usedLine, "Used: ", Fmt.Bytes(used));
            SetValuedLine(_totalLine, "Total: ", Fmt.Bytes(d.Total));
            SetValuedLine(_freeLine, "Free: ", Fmt.Bytes(d.Free));

            _bar.Fraction = frac;
            _bar.Color = Theme.Hex("#158000");

            // read/write line (#D4 correct values, #D5 unit setting, #D7 colors)
            _rwLine.Inlines.Clear();
            _rwLine.Inlines.Add(new Run("\u25B2 ") { Foreground = Theme.Green });
            _rwLine.Inlines.Add(new Run(Fmt.RateU(d.ReadBps, cfg.ReadWriteUnit)) { Foreground = Theme.Green });
            _rwLine.Inlines.Add(new Run("   \u25BC ") { Foreground = Theme.Yellow });
            _rwLine.Inlines.Add(new Run(Fmt.RateU(d.WriteBps, cfg.ReadWriteUnit)) { Foreground = Theme.Yellow });

            // per-drive graph (reads green / writes yellow — same colors as the text) (#D7)
            if (_graph != null)
            {
                _graph.Visibility = cfg.ShowPerDriveGraph ? Visibility.Visible : Visibility.Collapsed;
                if (cfg.ShowPerDriveGraph)
                    _graph.SetSeries(new List<Series>
                    {
                        new("reads", hist.read.Snapshot(), Theme.Hex("#bfffb2")),
                        new("writes", hist.write.Snapshot(), Theme.Hex("#ffe14d")),
                    });
            }
        }

        private static void SetValuedLine(TextBlock line, string label, string value)
        {
            line.Inlines.Clear();
            line.Inlines.Add(new Run(label) { Foreground = Theme.White });   // label white (#20)
            line.Inlines.Add(new Run(value) { Foreground = Theme.Blue });    // value light blue (#20)
        }
    }

    /// <summary>Force the gadget to rebuild its drive blocks on the next tick.</summary>
    public void InvalidateBlocks() => _blockKey = "";
}

