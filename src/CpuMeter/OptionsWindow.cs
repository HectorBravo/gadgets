using System.Windows;
using System.Windows.Controls;
using HectorBravo.Gadgets.Core;

namespace HectorBravo.Gadgets.CpuMeter;

/// <summary>Options window for the CPU gadget (light theme, no scrollbars, saves immediately).</summary>
public class OptionsWindow : OptionsWindowBase
{
    private readonly ConfigStore<CpuSettings> _cfg;

    public OptionsWindow(ConfigStore<CpuSettings> cfg) : base("CPU Usage — Options")
    {
        _cfg = cfg;

        var general = new StackPanel { Margin = new Thickness(12) };

        general.Children.Add(Check("Show usage graph", cfg.Data.ShowGraph, v =>
        {
            cfg.Data.ShowGraph = v;
            cfg.Save();
            // toggle the graph live on the gadget via a light refresh signal
            (Application.Current?.MainWindow as MainWindow)?.ApplyGraphVisibility();
        }));

        general.Children.Add(Check("Show swap row", cfg.Data.ShowSwap, v =>
        {
            cfg.Data.ShowSwap = v;
            cfg.Save();
            (Application.Current?.MainWindow as MainWindow)?.InvalidateMemRows();
        }));

        // graph height slider (#10) + history length (#7 treatment from LabeledSlider)
        general.Children.Add(new LabeledSlider("Graph height (px):", 30, 240, 5, cfg.Data.GraphHeight, v =>
        {
            cfg.Data.GraphHeight = Math.Round(v);
            cfg.Save();
            (Application.Current?.MainWindow as MainWindow)?.ApplyGraphHeight(cfg.Data.GraphHeight);
        }, " px"));

        general.Children.Add(new LabeledSlider("History (s):", 30, 600, 10, cfg.Data.HistorySeconds, v =>
        {
            cfg.Data.HistorySeconds = Math.Round(v);
            cfg.Save();
        }, " s"));

        AddTab("General", general);
        FinalizeSize();
    }
}

