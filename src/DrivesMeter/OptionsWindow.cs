using System.Windows;
using System.Windows.Controls;
using HectorBravo.Gadgets.Core;

namespace HectorBravo.Gadgets.DrivesMeter;

/// <summary>Options window for the Drives gadget: drive slots + graph toggle.</summary>
public class OptionsWindow : Window
{
    private readonly ConfigStore<DrivesSettings> _cfg;

    public OptionsWindow(ConfigStore<DrivesSettings> cfg)
    {
        _cfg = cfg;
        Title = "Drives Meter — Options";
        Width = 360; Height = 300;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new System.Windows.Media.SolidColorBrush(Theme.Hex("#161a23"));

        var tabs = new TabControl();

        // ── Drives tab: five editable slots (letter or "off") ──
        var drivesTab = new TabItem { Header = "Drives" };
        var panel = new StackPanel { Margin = new Thickness(12) };
        panel.Children.Add(new TextBlock
        {
            Text = "Drive 1..5 — type a letter (e.g. C:) or \"off\" to stop the list.",
            Foreground = Theme.Muted,
            FontSize = 11,
            Margin = new Thickness(0, 0, 0, 8),
        });

        for (int i = 0; i < _cfg.Data.Slots.Count; i++)
        {
            int idx = i;
            var row = new Grid { Margin = new Thickness(0, 2, 0, 2) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
            row.ColumnDefinitions.Add(new ColumnDefinition());

            var lbl = new TextBlock { Text = $"Drive {i + 1}:", Foreground = Theme.Fg, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(lbl, 0); row.Children.Add(lbl);

            var box = new TextBox
            {
                Text = _cfg.Data.Slots[i],
                Width = 80,
                HorizontalAlignment = HorizontalAlignment.Left,
            };
            box.LostFocus += (_, _) => CommitSlot(idx, box.Text);
            Grid.SetColumn(box, 1); row.Children.Add(box);

            panel.Children.Add(row);
        }

        drivesTab.Content = panel;
        tabs.Items.Add(drivesTab);

        // ── General tab: graph toggle ──
        var general = new TabItem { Header = "General" };
        var gp = new StackPanel { Margin = new Thickness(12) };
        var showGraph = new CheckBox
        {
            Content = "Show aggregated activity graph",
            IsChecked = cfg.Data.ShowGraph,
            Foreground = Theme.Fg,
            Margin = new Thickness(0, 4, 0, 4),
        };
        showGraph.Checked += (_, _) => { cfg.Data.ShowGraph = true; cfg.Save(); };
        showGraph.Unchecked += (_, _) => { cfg.Data.ShowGraph = false; cfg.Save(); };
        gp.Children.Add(showGraph);
        general.Content = gp;
        tabs.Items.Add(general);

        Content = tabs;
    }

    /// <summary>Normalize a slot value to an uppercase letter + ':' or "off", then persist.</summary>
    private void CommitSlot(int idx, string raw)
    {
        var s = (raw ?? "").Trim();
        string norm;
        if (s.Length == 0) norm = "";
        else if (string.Equals(s, "off", StringComparison.OrdinalIgnoreCase)) norm = "off";
        else
        {
            char c = char.ToUpperInvariant(s[0]);
            norm = c >= 'A' && c <= 'Z' ? c + ":" : "";
        }

        while (_cfg.Data.Slots.Count <= idx) _cfg.Data.Slots.Add("");
        if (!string.Equals(_cfg.Data.Slots[idx], norm, StringComparison.Ordinal))
        {
            _cfg.Data.Slots[idx] = norm;
            _cfg.Save();
        }
    }
}
