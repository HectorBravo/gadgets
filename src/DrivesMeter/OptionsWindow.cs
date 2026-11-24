using System.Windows;
using System.Windows.Controls;
using HectorBravo.Gadgets.Core;

namespace HectorBravo.Gadgets.DrivesMeter;

/// <summary>Options window for the Drives gadget (light theme, no scrollbars, saves immediately).</summary>
public class OptionsWindow : OptionsWindowBase
{
    /// <summary>Separators accepted after a drive letter ("C", "C:", "C;", "C:\").</summary>
    private static readonly char[] SplitSep = { ':', ';', (char)92 };

    private readonly ConfigStore<DrivesSettings> _cfg;

    public OptionsWindow(ConfigStore<DrivesSettings> cfg) : base("Drives Meter — Options")
    {
        _cfg = cfg;

        // ── Drives tab: Drive 1..N text boxes (#D1) ──
        var dp = new StackPanel { Margin = new Thickness(12) };
        dp.Children.Add(Hint("Type a drive letter (C, D, V… — no colon or semicolon needed). \"off\" ends the list."));

        int slotCount = Math.Max(5, _cfg.Data.Slots.Count);
        for (int i = 0; i < slotCount; i++)
        {
            string cur = i < _cfg.Data.Slots.Count ? _cfg.Data.Slots[i] : "off";
            int idx = i;
            dp.Children.Add(Row($"Drive {i + 1}:", Field(cur, s =>
            {
                var v = (s ?? "").Trim();
                if (v.Length == 0) v = "off";

                // normalize: accept "c", "C", "c:", "C:\path" → canonical form; keep "off" as-is (#D1)
                if (!string.Equals(v, "off", StringComparison.OrdinalIgnoreCase))
                {
                    var letters = v.Split(SplitSep, StringSplitOptions.RemoveEmptyEntries);
                    if (letters.Length > 0)
                    {
                        string first = letters[0];
                        v = char.ToUpper(first[0]) + ":";
                    }
                    else v = "off";
                }

                while (_cfg.Data.Slots.Count <= idx) _cfg.Data.Slots.Add("off");
                if (!string.Equals(_cfg.Data.Slots[idx], v, StringComparison.OrdinalIgnoreCase))
                {
                    _cfg.Data.Slots[idx] = v;
                    _cfg.Save();                   // saved immediately (#8)
                    InvalidateBlocks();
                }
            })));
        }
        AddTab("Drives", dp);

        // ── Graphs tab ──
        var gp = new StackPanel { Margin = new Thickness(12) };
        gp.Children.Add(Check("Show aggregate graph (all drives)", cfg.Data.ShowGraph, v => { cfg.Data.ShowGraph = v; cfg.Save(); }));
        gp.Children.Add(Check("Show per-drive graphs", cfg.Data.ShowPerDriveGraph, v => { cfg.Data.ShowPerDriveGraph = v; cfg.Save(); InvalidateBlocks(); }));
        gp.Children.Add(new LabeledSlider("Graph height (px):", 30, 240, 5, cfg.Data.GraphHeight, v =>
        {
            cfg.Data.GraphHeight = Math.Round(v);
            cfg.Save();
        }, " px"));
        AddTab("Graphs", gp);

        // ── Units tab (#D5) ──
        var up = new StackPanel { Margin = new Thickness(12) };
        up.Children.Add(Hint("Unit for reads and writes (both use the same setting). Auto always shows 4 digits with one dot, e.g. 4.345 GB/s."));
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
                IsChecked = cfg.Data.ReadWriteUnit == u,
                Margin = new Thickness(0, 3, 0, 3),
            };
            rb.Checked += (_, _) => { _cfg.Data.ReadWriteUnit = captured; _cfg.Save(); };
            up.Children.Add(rb);
        }
        AddTab("Units", up);

        FinalizeSize();
    }

    /// <summary>Force the gadget to rebuild its drive blocks on the next tick.</summary>
    private static void InvalidateBlocks() => (Application.Current?.MainWindow as MainWindow)?.InvalidateBlocks();
}




