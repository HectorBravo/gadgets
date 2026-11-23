using HectorBravo.Gadgets.Core;

namespace HectorBravo.Gadgets.DrivesMeter;

/// <summary>Drives gadget settings — drive slots like the original (Drive 1..N, "off" ends the list).</summary>
public class DrivesSettings : BaseSettings
{
    /// <summary>"C:", "D:", ... or "off". Index 0 = Drive 1.</summary>
    public List<string> Slots { get; set; } = new() { "C:", "D:", "E:", "F:", "G:" };
    public bool ShowGraph { get; set; } = true;
}
