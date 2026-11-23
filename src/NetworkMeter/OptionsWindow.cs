using System.Windows;
using System.Windows.Controls;
using HectorBravo.Gadgets.Core;

namespace HectorBravo.Gadgets.NetworkMeter;

/// <summary>Options window for the Network gadget: interface selection + graph/refresh settings.</summary>
public class OptionsWindow : Window
{
    private readonly ConfigStore<NetworkSettings> _cfg;

    public OptionsWindow(ConfigStore<NetworkSettings> cfg)
    {
        _cfg = cfg;
        Title = "Network Meter — Options";
        Width = 400; Height = 360;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new System.Windows.Media.SolidColorBrush(Theme.Hex("#161a23"));

        var tabs = new TabControl();

        // ── Interfaces tab ──
        var ifaceTab = new TabItem { Header = "Interfaces" };
        var panel = new StackPanel { Margin = new Thickness(12) };

        bool auto = cfg.Data.InterfaceNames.Count == 0;
        var autoRadio = new RadioButton { Content = "Auto (all connected adapters)", IsChecked = auto, Foreground = Theme.Fg, Margin = new Thickness(0, 0, 0, 6) };
        autoRadio.Checked += (_, _) => { cfg.Data.InterfaceNames.Clear(); cfg.Save(); };
        panel.Children.Add(autoRadio);

        var listHost = new ScrollViewer
        {
            MaxHeight = 170,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = BuildAdapterChecks(cfg, auto),
        };
        panel.Children.Add(listHost);

        ifaceTab.Content = panel;
        tabs.Items.Add(ifaceTab);

        // ── General tab ──
        var general = new TabItem { Header = "General" };
        var gp = new StackPanel { Margin = new Thickness(12) };

        gp.Children.Add(SliderRow("Graph height (px):", 30, 200, cfg.Data.GraphHeight, v => { cfg.Data.GraphHeight = Math.Round(v); cfg.Save(); }));
        gp.Children.Add(SliderRow("History (s):", 30, 600, cfg.Data.HistorySeconds, v => { cfg.Data.HistorySeconds = (int)Math.Round(v); cfg.Save(); }));
        gp.Children.Add(SliderRow("External IP refresh (min):", 1, 60, cfg.Data.ExternalRefreshMinutes, v => { cfg.Data.ExternalRefreshMinutes = (int)Math.Round(v); cfg.Save(); }));

        general.Content = gp;
        tabs.Items.Add(general);

        Content = tabs;
    }

    private static UIElement BuildAdapterChecks(ConfigStore<NetworkSettings> cfg, bool auto)
    {
        var sp = new StackPanel();
        foreach (var a in new Sampler().Adapters())
        {
            string status = a.IsLoopback ? "loopback" : a.IsUp ? "connected" : "disconnected";
            bool on = !auto && cfg.Data.InterfaceNames.Contains(a.Name);
            var cb = new CheckBox
            {
                Content = $"{Truncate(a.Description.Length > 0 ? a.Description : a.Name, 46)}  ({status})",
                IsChecked = on,
                Foreground = Theme.Fg,
                Margin = new Thickness(0, 2, 0, 2),
            };
            cb.Checked += (_, _) =>
            {
                if (!cfg.Data.InterfaceNames.Contains(a.Name)) cfg.Data.InterfaceNames.Add(a.Name);
                cfg.Save();
            };
            cb.Unchecked += (_, _) =>
            {
                cfg.Data.InterfaceNames.Remove(a.Name);
                cfg.Save();
            };
            sp.Children.Add(cb);
        }
        return sp;
    }

    private static UIElement SliderRow(string label, double min, double max, double val, Action<double> onChange)
    {
        var panel = new StackPanel { Margin = new Thickness(0, 4, 0, 4) };
        var lbl = new TextBlock { Foreground = Theme.Fg, FontSize = 12 };
        lbl.Text = label;
        panel.Children.Add(lbl);

        var slider = new Slider { Minimum = min, Maximum = max, Value = val, IsSnapToTickEnabled = true, TickFrequency = 1 };
        slider.ValueChanged += (_, e) => onChange(e.NewValue);
        panel.Children.Add(slider);
        return panel;
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s.Substring(0, max) + "\u2026";
}
