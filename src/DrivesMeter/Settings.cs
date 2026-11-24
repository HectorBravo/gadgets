using HectorBravo.Gadgets.Core;

namespace HectorBravo.Gadgets.DrivesMeter;

/// <summary>Drives gadget settings — drive slots like the original (Drive 1..N, "off" ends the list).</summary>
public class DrivesSettings : BaseSettings
{
    /// <summary>"C:", "D:", ... or "off". Index 0 = Drive 1.</summary>
    public List<string> Slots { get; set; } = new() { "C:", "D:", "E:", "F:", "G:" };

    /// <summary>Aggregated reads+writes graph at the bottom (#D3).</summary>
    public bool ShowGraph { get; set; } = true;

    /// <summary>Per-drive read/write graph under each drive block (#D2, default on).</summary>
    public bool ShowPerDriveGraph { get; set; } = true;

    /// <summary>Unit for both reads and writes (#D5).</summary>
    public RateUnit ReadWriteUnit { get; set; } = RateUnit.Auto;
}

