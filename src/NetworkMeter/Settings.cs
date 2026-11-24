using HectorBravo.Gadgets.Core;

namespace HectorBravo.Gadgets.NetworkMeter;

/// <summary>Network gadget settings (settings only — no runtime data).</summary>
public class NetSettings : BaseSettings
{
    /// <summary>Selected interface indices. Empty = select all adapters that are up.</summary>
    public List<string> Interfaces { get; set; } = new();

    /// <summary>Names of the interfaces the indices were resolved against (for re-mapping after reboots).</summary>
    public List<string> InterfaceNames { get; set; } = new();

    /// <summary>Show the external IP line (#N5 — printed before the first interface).</summary>
    public bool ShowExtIP { get; set; } = true;

    /// <summary>Unit for both upload and download (#N2).</summary>
    public RateUnit SpeedUnit { get; set; } = RateUnit.Auto;

    /// <summary>History length in seconds for the traffic graph.</summary>
    public double HistorySeconds { get; set; } = 120;
}


