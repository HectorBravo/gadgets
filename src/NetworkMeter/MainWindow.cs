using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using HectorBravo.Gadgets.Core;

namespace HectorBravo.Gadgets.NetworkMeter;

/// <summary>Network Meter gadget: per-adapter TX/RX rates, internal + external IP, two-line graph.</summary>
public class MainWindow : GadgetWindow
{
    private readonly ConfigStore<NetworkSettings> _cfg;
    private readonly Sampler _sampler = new();
    private readonly DispatcherTimer _timer;
    private readonly ExternalIp _ext = new();
    private DateTime _lastExtRefresh = DateTime.MinValue;

    // per-adapter history (name -> tx/rx samples), newest-last
    private readonly Dictionary<string, (List<double> tx, List<double> rx)> _hists = new();

    private readonly StackPanel _ifaceRows = new();
    private readonly TextBlock _extIpText = new();

    public MainWindow() : base("NetworkMeter", new ConfigStore<BaseSettings>("NetworkMeter"))
    {
        _cfg = new ConfigStore<NetworkSettings>("NetworkMeter");
        Title = "Network Meter";

        var root = new StackPanel { Margin = new Thickness(8, 6, 8, 4) };
        ContentHost.Children.Add(root);

        // ── header: [icon] Network Meter ──
        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.ColumnDefinitions.Add(new ColumnDefinition());

        var icon = new TextBlock { Text = "🌐", FontSize = 16, VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(icon, 0); header.Children.Add(icon);

        var title = new TextBlock
        {
            Text = "Network Meter",
            Foreground = Theme.TitleFg,
            FontSize = 15,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(6, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(title, 1); header.Children.Add(title);

        root.Children.Add(header);

        // ── per-interface blocks ──
        _ifaceRows.Margin = new Thickness(0, 4, 0, 2);
        root.Children.Add(_ifaceRows);

        // ── external IP row with refresh button ──
        var extRow = new Grid { Margin = new Thickness(0, 4, 0, 0) };
        extRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        extRow.ColumnDefinitions.Add(new ColumnDefinition());
        extRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var extLbl = new TextBlock { Text = "Ext. IP: ", Foreground = Theme.Muted, FontSize = 11 };
        Grid.SetColumn(extLbl, 0); extRow.Children.Add(extLbl);

        _extIpText.Foreground = Theme.Green;
        _extIpText.FontSize = 11;
        Grid.SetColumn(_extIpText, 1); extRow.Children.Add(_extIpText);

        var refreshBtn = new Button { Content = "⟳", Padding = new Thickness(4, 0, 4, 0), FontSize = 12 };
        refreshBtn.Click += (_, _) => { _ext.Refresh(); _lastExtRefresh = DateTime.Now; UpdateExtIp(); };
        Grid.SetColumn(refreshBtn, 2); extRow.Children.Add(refreshBtn);

        root.Children.Add(extRow);

        OpenOptions = () => { var w = new OptionsWindow(_cfg) { Owner = this }; w.Show(); };

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => Refresh();
        Loaded += (_, _) => { RebuildHistories(); Refresh(); };
        _ext.Refresh();
        _lastExtRefresh = DateTime.Now;
        _timer.Start();

        var updater = new UpdateNotifier(this, new UpdateService("network-meter", "NetworkMeter.exe"));
    }

    /// <summary>Names of the interfaces to display (empty config = auto: all up, non-loopback).</summary>
    private List<string> Wanted()
    {
        if (_cfg.Data.InterfaceNames.Count > 0)
            return _cfg.Data.InterfaceNames.ToList();
        return _sampler.Adapters()
                       .Where(a => a.IsUp && !a.IsLoopback)
                       .Select(a => a.Name)
                       .ToList();
    }

    private int Cap => Math.Clamp(Math.Max(10, _cfg.Data.HistorySeconds), 30, 600);

    /// <summary>Ensure a history buffer exists for each wanted interface.</summary>
    private void RebuildHistories()
    {
        var wanted = Wanted();
        foreach (var k in _hists.Keys.Where(k => !wanted.Contains(k)).ToList())
            _hists.Remove(k);
        foreach (var w in wanted)
            if (!_hists.ContainsKey(w))
                _hists[w] = (new List<double>(), new List<double>());
    }

    private void Refresh()
    {
        RebuildHistories();
        var snaps = _sampler.Sample(Wanted());

        // push samples into history
        foreach (var s in snaps)
        {
            if (_hists.TryGetValue(s.Name, out var h))
            {
                h.tx.Add(s.TxRate);
                h.rx.Add(s.RxRate);
                int cap = Cap;
                while (h.tx.Count > cap) h.tx.RemoveAt(0);
                while (h.rx.Count > cap) h.rx.RemoveAt(0);
            }
        }

        // auto external-IP refresh
        var every = TimeSpan.FromMinutes(Math.Max(1, _cfg.Data.ExternalRefreshMinutes));
        if (!_ext.Fetching && DateTime.Now - _lastExtRefresh >= every)
        {
            _lastExtRefresh = DateTime.Now;
            _ext.Refresh();
        }
        UpdateExtIp();

        // rebuild the visible blocks
        _ifaceRows.Children.Clear();
        if (snaps.Count == 0)
        {
            _ifaceRows.Children.Add(new TextBlock
            {
                Text = "No interfaces selected (right-click → Options…)",
                Foreground = Theme.Muted,
                FontSize = 11,
                Margin = new Thickness(0, 2, 0, 2),
            });
        }
        foreach (var s in snaps)
            AddIfaceBlock(s);
    }

    private void UpdateExtIp() => _extIpText.Text = _ext.Display;

    private void AddIfaceBlock(IfaceSample s)
    {
        var block = new StackPanel { Margin = new Thickness(0, 2, 0, 4) };

        // header: name (+ link speed)
        string title = LinkLabel(s.LinkSpeedBps) is { Length: > 0 } ll
            ? Truncate(s.Description.Length > 0 ? s.Description : s.Name, 32) + " (" + ll + ")"
            : Truncate(s.Description.Length > 0 ? s.Description : s.Name, 40);
        block.Children.Add(new TextBlock { Text = title, Foreground = Theme.TitleFg, FontSize = 12, FontWeight = FontWeights.Bold });

        // internal IP (+ down)
        var ipRow = new StackPanel { Orientation = Orientation.Horizontal };
        ipRow.Children.Add(new TextBlock { Text = "Int. IP: ", Foreground = Theme.Muted, FontSize = 11 });
        ipRow.Children.Add(new TextBlock { Text = s.Ipv4.Length > 0 ? s.Ipv4 : "n/a", Foreground = Theme.Green, FontSize = 11 });
        if (!s.IsUp)
            ipRow.Children.Add(new TextBlock { Text = " (down)", Foreground = Theme.Red, FontSize = 11 });
        block.Children.Add(ipRow);

        // rates: ▲ Mbit/s   ▼ MB/s
        var rateRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 1, 0, 0) };
        rateRow.Children.Add(new TextBlock { Text = "▲ ", Foreground = Theme.Yellow, FontSize = 11 });
        rateRow.Children.Add(new TextBlock { Text = Fmt.BitRate(s.TxRate), Foreground = Theme.Green, FontSize = 11 });
        rateRow.Children.Add(new TextBlock { Text = "   ▼ ", Foreground = Theme.Yellow, FontSize = 11 });
        rateRow.Children.Add(new TextBlock { Text = Fmt.Rate(s.RxRate), Foreground = Theme.Green, FontSize = 11 });
        block.Children.Add(rateRow);

        // graph (TX yellow / RX green)
        var graph = new LineGraph { Height = Math.Clamp(_cfg.Data.GraphHeight, 30, 200), Margin = new Thickness(0, 3, 0, 0) };
        if (_hists.TryGetValue(s.Name, out var h))
            graph.SetSeries(new List<Series>
            {
                new("tx", h.tx, Theme.Hex("#ffe14d")),
                new("rx", h.rx, Theme.Hex("#bfffb2")),
            });
        block.Children.Add(graph);

        _ifaceRows.Children.Add(block);
    }

    private static string LinkLabel(ulong bitsPerSec)
    {
        if (bitsPerSec == 0 || bitsPerSec >= ulong.MaxValue / 2) return "";
        double g = bitsPerSec / 1e9;
        return g >= 0.95 ? $"{g:0}Gbps link" : $"{bitsPerSec / 1e6:0}Mbps link";
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s.Substring(0, max) + "\u2026";
}

