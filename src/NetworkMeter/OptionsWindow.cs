using System.Windows;
using System.Windows.Controls;
using HectorBravo.Gadgets.Core;

namespace HectorBravo.Gadgets.NetworkMeter;

/// <summary>Options window for the Network gadget (light theme, no scrollbars, saves immediately).</summary>
public class OptionsWindow : OptionsWindowBase
{
    private readonly ConfigStore<NetSettings> _cfg;

    // interface list UI state: index → checkbox, in display order (#N4)
    private readonly List<(string id, string name, CheckBox cb)> _rows = new();
    private StackPanel _listHost = new();

    public OptionsWindow(ConfigStore<NetSettings> cfg) : base("Network Meter — Options")
    {
        _cfg = cfg;

        // ── Interfaces tab (#N4): All / None buttons + individual checkboxes + sort ──
        var ip = new StackPanel { Margin = new Thickness(12) };

        var btnRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 6) };
        btnRow.Children.Add(MakeButton("Select all", () => { foreach (var r in _rows) r.cb.IsChecked = true; SaveSelection(); }));
        btnRow.Children.Add(MakeButton("Select none", () => { foreach (var r in _rows) r.cb.IsChecked = false; SaveSelection(); }));
        ip.Children.Add(btnRow);

        // sort controls
        var sortRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 6) };
        sortRow.Children.Add(new TextBlock { Text = "Sort:", Foreground = MenuStyle.Fg, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 6, 0) });
        foreach (var (key, label) in new[] { ("name", "Name"), ("speed", "Link speed") })
        {
            string captured = key;
            sortRow.Children.Add(MakeButton(label, () => SortRows(captured)));
        }
        ip.Children.Add(sortRow);

        _listHost = new StackPanel();
        ip.Children.Add(_listHost);

        foreach (var ni in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.Loopback) continue;

            bool selected = cfg.Data.Interfaces.Count == 0 || cfg.Data.Interfaces.Contains(ni.Id);
            string id = ni.Id;
            var cb = new CheckBox
            {
                Content = $"{ni.Name}  ({ni.OperationalStatus})",
                Foreground = MenuStyle.Fg,
                IsChecked = selected,
                Margin = new Thickness(0, 2, 0, 2),
            };
            cb.Checked += (_, _) => SaveSelection();
            cb.Unchecked += (_, _) => SaveSelection();
            _rows.Add((id, ni.Name, cb));
            _listHost.Children.Add(cb);
        }

        AddTab("Interfaces", ip);

        // ── Graphs tab ──
        var gp = new StackPanel { Margin = new Thickness(12) };
        gp.Children.Add(new LabeledSlider("Graph height (px):", 30, 240, 5, cfg.Data.GraphHeight, v =>
        {
            cfg.Data.GraphHeight = Math.Round(v);
            cfg.Save();
        }, " px"));
        gp.Children.Add(new LabeledSlider("History (s):", 30, 600, 10, cfg.Data.HistorySeconds, v =>
        {
            cfg.Data.HistorySeconds = Math.Round(v);
            cfg.Save();
        }, " s"));
        AddTab("Graphs", gp);

        // ── Units tab (#N2) ──
        var up = new StackPanel { Margin = new Thickness(12) };
        up.Children.Add(Hint("Unit for upload and download (both use the same setting). Auto always shows 4 digits with one dot, e.g. 4.345 MB/s."));
        foreach (var (u, label) in new[]
        {
            (RateUnit.Auto, "Auto"),
            (RateUnit.GBps, "GB/s"),
            (RateUnit.MBps, "MB/s"),
            (RateUnit.KBps, "KB/s"),
        })
        {
            RateUnit captured = u;
            var rb = new RadioButton
            {
                Content = label,
                Foreground = MenuStyle.Fg,
                IsChecked = cfg.Data.SpeedUnit == u,
                Margin = new Thickness(0, 3, 0, 3),
            };
            rb.Checked += (_, _) => { _cfg.Data.SpeedUnit = captured; _cfg.Save(); };
            up.Children.Add(rb);
        }
        AddTab("Units", up);

        // ── Misc tab ──
        var mp = new StackPanel { Margin = new Thickness(12) };
        mp.Children.Add(Check("Show external IP line", cfg.Data.ShowExtIP, v => { cfg.Data.ShowExtIP = v; cfg.Save(); }));
        AddTab("Misc", mp);

        FinalizeSize();
    }

    private Button MakeButton(string text, Action onClick)
    {
        var b = new Button
        {
            Content = text,
            Foreground = MenuStyle.Fg,
            Background = MenuStyle.BgAlt,
            BorderBrush = MenuStyle.BorderBrush,
            Padding = new Thickness(10, 4, 10, 4),
            Margin = new Thickness(0, 0, 8, 0),
        };
        b.Click += (_, _) => onClick();
        return b;
    }

    /// <summary>Reorder the checkbox rows (display order) — persisted as the saved order (#N4).</summary>
    private void SortRows(string by)
    {
        var sorted = by == "speed"
            ? _rows.OrderBy(r => LinkSpeedOf(r.id)).ThenBy(r => r.name, StringComparer.OrdinalIgnoreCase).ToList()
            : _rows.OrderBy(r => r.name, StringComparer.OrdinalIgnoreCase).ToList();

        _rows.Clear();
        _rows.AddRange(sorted);

        // re-add in the new order (children are shared instances — Add moves existing children)
        foreach (var r in _rows) _listHost.Children.Add(r.cb);

        SaveSelection();
    }

    private static long LinkSpeedOf(string id)
    {
        try
        {
            var ni = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
                        .FirstOrDefault(n => n.Id == id);
            return ni?.Speed ?? 0;
        }
        catch { return 0; }
    }

    /// <summary>Persist the checked interfaces in display order (indices + names for re-mapping).</summary>
    private void SaveSelection()
    {
        _cfg.Data.Interfaces = _rows.Where(r => r.cb.IsChecked == true).Select(r => r.id).ToList();
        _cfg.Data.InterfaceNames = _rows.Where(r => r.cb.IsChecked == true).Select(r => r.name).ToList();
        _cfg.Save();                       // saved immediately (#8)
    }
}





