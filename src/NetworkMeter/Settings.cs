using HectorBravo.Gadgets.Core;

namespace HectorBravo.Gadgets.NetworkMeter;

/// <summary>Network gadget settings. Empty InterfaceNames = auto (all up, non-loopback).</summary>
public class NetworkSettings : BaseSettings
{
    /// <summary>Adapter names to show (stable across reboots). Empty = auto-select.</summary>
    public List<string> InterfaceNames { get; set; } = new();
    public double GraphHeight { get; set; } = 70;
    public int HistorySeconds { get; set; } = 120;
    public int ExternalRefreshMinutes { get; set; } = 5;
}
