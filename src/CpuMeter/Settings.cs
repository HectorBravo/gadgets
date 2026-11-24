using HectorBravo.Gadgets.Core;

namespace HectorBravo.Gadgets.CpuMeter;

/// <summary>CPU gadget settings (settings only — no runtime data).</summary>
public class CpuSettings : BaseSettings
{
    public bool ShowGraph { get; set; } = true;
    public bool ShowSwap { get; set; } = true;
    /// <summary>History length in seconds for the usage graph.</summary>
    public double HistorySeconds { get; set; } = 120;
}

