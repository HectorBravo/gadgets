using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using HectorBravo.Gadgets.Core;

namespace HectorBravo.Gadgets.NetworkMeter;

/// <summary>Network Meter gadget: ext IP line (top), per-interface blocks, traffic graph.</summary>
public class MainWindow : GadgetWindow<NetSettings>
{
    private readonly Sampler _sampler = new();
    private readonly DispatcherTimer _timer;

    private readonly TextBlock _title = new();
    private readonly TextBlock _extIpLine = new();
    private readonly StackPanel _ifRows = new();
    private readonly LineGraph _graph = new();

    // cached per-interface visuals (#21)
    private readonly Dictionary<string, IfBlock> _blocks = new();
    private string _blockKey = "";

    // watchdog: if the sampler stops ticking (e.g. enumeration stall), force a refresh (#19)
    private long _lastTick;
    private int _stalledTicks;

    // external IP fetcher (background, cached — never blocks the UI thread)
    private readonly ExternalIp _extIp = new();

    public MainWindow() : base("NetworkMeter", new ConfigStore<NetSettings>("NetworkMeter"))
    {
        Title = "Network";

        var root = new StackPanel { Margin = new Thickness(8, 6, 8, 4) };
        ContentHost.Children.Add(root);

        // ── header: [icon] Network Meter ──
        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.ColumnDefinitions.Add(new ColumnDefinition());

        var icon = new TextBlock { Text = "🌐", FontSize = 16, VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(icon, 0); header.Children.Add(icon);

        _title.Text = "Network Meter";
        _title.Foreground = Theme.TitleFg;       // white + bold (#13)
        _title.FontSize = 15;
        _title.FontWeight = FontWeights.Bold;
        _title.Margin = new Thickness(6, 0, 0, 0);
        _title.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(_title, 1); header.Children.Add(_title);

        root.Children.Add(header);

        // ── external IP line — printed just before the first interface (#N5) ──
        _extIpLine.FontSize = 11;
        _extIpLine.Margin = new Thickness(0, 2, 0, 1);
        root.Children.Add(_extIpLine);

        // ── per-interface blocks ──
        _ifRows.Margin = new Thickness(0, 2, 0, 2);
        root.Children.Add(_ifRows);

        // ── traffic graph (upload green / download yellow — #N3) ──
        _graph.Height = Math.Clamp(Cfg.Data.GraphHeight, 30, 240);
        _graph.LegendFormat = v => Fmt.Rate(v);
        root.Children.Add(_graph);

        OpenOptions = () => { var w = new OptionsWindow(Cfg) { Owner = this }; w.Show(); };

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => Refresh();
        Loaded += (_, _) => Refresh();
        _timer.Start();

        var updater = new UpdateNotifier<NetSettings>(this, new UpdateService("network-meter", "NetworkMeter.exe"));
    }

    /// <summary>Interfaces to show: configured selection re-mapped by name, or all "up" adapters when empty.</summary>
    private List<string> ResolveSelected()
    {
        var all = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();

        if (Cfg.Data.Interfaces.Count == 0)
            return all.Where(n => n.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up &&
                                   n.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback)
                      .Select(n => n.Id).ToList();

        // re-map saved indices by name (indices change across boots/adapters)
        var resolved = new List<string>();
        for (int i = 0; i < Cfg.Data.Interfaces.Count && i < Cfg.Data.InterfaceNames.Count; i++)
        {
            string idx = Cfg.Data.Interfaces[i];
            string name = Cfg.Data.InterfaceNames[i];
            var ni = all.FirstOrDefault(n => n.Id == idx)
                  ?? all.FirstOrDefault(n => string.Equals(n.Name, name, StringComparison.OrdinalIgnoreCase));
            if (ni != null && !resolved.Contains(ni.Id)) resolved.Add(ni.Id);
        }
        return resolved;
    }

    private void Refresh()
    {
        var selected = ResolveSelected();
        var samples = _sampler.Sample(selected);

        // watchdog (#19): if the sampler tick did not advance, re-baseline and keep going
        if (_sampler.Tick == _lastTick)
        {
            if (++_stalledTicks >= 3) { _stalledTicks = 0; /* next Sample() call will retry enumeration */ }
        }
        else _stalledTicks = 0;
        _lastTick = _sampler.Tick;

        // external IP line before the interfaces (#N5): label white, value light blue (#20)
        if (Cfg.Data.ShowExtIP)
        {
            _extIp.Refresh();                       // fire-and-forget; Display shows "…" until ready
            _extIpLine.Visibility = Visibility.Visible;
            _extIpLine.Inlines.Clear();
            _extIpLine.Inlines.Add(new Run("Ext IP: ") { Foreground = Theme.White });
            _extIpLine.Inlines.Add(new Run(_extIp.Display) { Foreground = Theme.Blue });
        }
        else _extIpLine.Visibility = Visibility.Collapsed;

        // rebuild interface blocks only when the set changed (#21)
        string key = string.Join(",", samples.Select(s => s.Id));
        if (key != _blockKey)
        {
            _blockKey = key;
            _ifRows.Children.Clear();
            _blocks.Clear();
            foreach (var s in samples)
            {
                var b = new IfBlock();
                _blocks[s.Id] = b;
                _ifRows.Children.Add(b.Root);
            }
        }

        foreach (var s in samples)
            if (_blocks.TryGetValue(s.Id, out var b))
                b.Update(s, Cfg.Data.SpeedUnit);

        // graph: upload green / download yellow (#N3), trimmed to the configured history (#18)
        int cap = Math.Clamp((int)Cfg.Data.HistorySeconds, 30, _sampler.RxHist.Capacity);
        _graph.Height = Math.Clamp(Cfg.Data.GraphHeight, 30, 240);
        _graph.SetSeries(new List<Series>
        {
            new("up", Trim(_sampler.TxHist.Snapshot(), cap), Theme.Hex("#bfffb2")),
            new("down", Trim(_sampler.RxHist.Snapshot(), cap), Theme.Hex("#ffe14d")),
        });
    }

    private static double[] Trim(double[] a, int cap)
    {
        if (a.Length <= cap) return a;
        var r = new double[cap];
        Array.Copy(a, a.Length - cap, r, 0, cap);
        return r;
    }

    /// <summary>One interface: name line, link speed on its own line (#N1), up/down rates.</summary>
    private sealed class IfBlock
    {
        public StackPanel Root { get; } = new() { Margin = new Thickness(0, 2, 0, 3) };

        private readonly TextBlock _nameLine = new();
        private readonly TextBlock _speedLine = new();
        private readonly TextBlock _ratesLine = new();

        public void Update(IfSample s, RateUnit unit)
        {
            // interface name (label white — #20)
            _nameLine.Inlines.Clear();
            _nameLine.Inlines.Add(new Run(s.Name) { Foreground = Theme.White, FontWeight = FontWeights.Bold });

            // link speed on the NEXT line, light blue (#N1/#N5 clarification)
            _speedLine.Inlines.Clear();
            string speed = s.SpeedBps <= 0 ? "—" : Fmt.LinkSpeed(s.SpeedBps);
            _speedLine.Inlines.Add(new Run("Link: ") { Foreground = Theme.White });
            _speedLine.Inlines.Add(new Run(speed) { Foreground = Theme.Blue });

            // up (green) / down (yellow) — both text and icon (#N3), unit from settings (#N2)
            _ratesLine.Inlines.Clear();
            _ratesLine.Inlines.Add(new Run("\u25B2 ") { Foreground = Theme.Green });
            _ratesLine.Inlines.Add(new Run(Fmt.RateU(s.TxRate, unit)) { Foreground = Theme.Green });
            _ratesLine.Inlines.Add(new Run("   \u25BC ") { Foreground = Theme.Yellow });
            _ratesLine.Inlines.Add(new Run(Fmt.RateU(s.RxRate, unit)) { Foreground = Theme.Yellow });
        }

        public IfBlock()
        {
            _nameLine.FontSize = 12;
            Root.Children.Add(_nameLine);

            _speedLine.FontSize = 11;
            _speedLine.Margin = new Thickness(0, 1, 0, 0);
            Root.Children.Add(_speedLine);

            _ratesLine.FontSize = 11;
            _ratesLine.Margin = new Thickness(0, 1, 0, 0);
            Root.Children.Add(_ratesLine);
        }
    }
}




