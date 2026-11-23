using System.Windows;
using System.Windows.Controls;
using HectorBravo.Gadgets.Core;

namespace HectorBravo.Gadgets.CpuMeter;

/// <summary>Options window for the CPU gadget (separate window, opened from the menu).</summary>
public class OptionsWindow : Window
{
    private readonly ConfigStore<CpuSettings> _cfg;

    public OptionsWindow(ConfigStore<CpuSettings> cfg)
    {
        _cfg = cfg;
        Title = "CPU Usage — Options";
        Width = 340; Height = 260;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new System.Windows.Media.SolidColorBrush(Theme.Hex("#161a23"));

        var tabs = new TabControl();
        var general = new TabItem { Header = "General" };
        var panel = new StackPanel { Margin = new Thickness(12) };

        panel.Children.Add(new CheckBox
        {
            Content = "Show usage graph",
            IsChecked = cfg.Data.ShowGraph,
            Foreground = Theme.Fg,
            Margin = new Thickness(0, 4, 0, 4),
        });
        ((CheckBox)panel.Children[^1]).Checked += (_, _) => { cfg.Data.ShowGraph = true; cfg.Save(); };
        ((CheckBox)panel.Children[^1]).Unchecked += (_, _) => { cfg.Data.ShowGraph = false; cfg.Save(); };

        panel.Children.Add(new CheckBox
        {
            Content = "Show swap row",
            IsChecked = cfg.Data.ShowSwap,
            Foreground = Theme.Fg,
            Margin = new Thickness(0, 4, 0, 4),
        });
        ((CheckBox)panel.Children[^1]).Checked += (_, _) => { cfg.Data.ShowSwap = true; cfg.Save(); };
        ((CheckBox)panel.Children[^1]).Unchecked += (_, _) => { cfg.Data.ShowSwap = false; cfg.Save(); };

        general.Content = panel;
        tabs.Items.Add(general);
        Content = tabs;
    }
}
