using System.Globalization;
using System.Runtime.InteropServices;

namespace HectorBravo.Gadgets.DigitalClock;

/// <summary>Everything the clock draws for one tick.</summary>
public record ClockSnapshot(DateTime Now, ulong UptimeSecs)
{
    public int DayOfYear => Now.DayOfYear;
    public int IsoWeek => ISOWeek.GetWeekOfYear(Now);
    public int Quarter => (Now.Month - 1) / 3 + 1;
}

/// <summary>Time samplers: local wall clock via DateTime.Now, uptime via GetTickCount64.</summary>
public static class Sampler
{
    [DllImport("kernel32.dll")]
    private static extern ulong GetTickCount64();

    public static ClockSnapshot Sample() => new(DateTime.Now, GetTickCount64() / 1000);

    /// <summary>"0d 5h 30m 45s" — like the original gadget.</summary>
    public static string FmtUptime(ulong secs)
    {
        ulong d = secs / 86_400;
        ulong h = (secs % 86_400) / 3600;
        ulong m = (secs % 3600) / 60;
        ulong s = secs % 60;
        return $"{d}d {h}h {m}m {s}s";
    }
}
