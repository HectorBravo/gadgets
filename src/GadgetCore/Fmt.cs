namespace HectorBravo.Gadgets.Core;

/// <summary>How a rate (bytes/s) is rendered.</summary>
public enum RateUnit { Auto = 0, GBps = 1, MBps = 2, KBps = 3 }

/// <summary>Formatting helpers matching the original gadgets' style.</summary>
public static class Fmt
{
    /// <summary>"930.6GB", "14.10TB", "509.9MB" (binary units, like the originals).</summary>
    public static string Bytes(double bytes)
    {
        string[] units = { "B", "KB", "MB", "GB", "TB", "PB" };
        double v = Math.Max(0, bytes);
        int u = 0;
        while (v >= 1024 && u < units.Length - 1) { v /= 1024; u++; }
        return u <= 1 ? $"{v:0}{units[u]}" : $"{v:0.00}{units[u]}";
    }

    /// <summary>"1.425MB/s", "0.000MB/s" (original drives-meter style).</summary>
    public static string Rate(double bytesPerSec)
    {
        string[] units = { "B/s", "KB/s", "MB/s", "GB/s" };
        double v = Math.Max(0, bytesPerSec);
        int u = 0;
        while (v >= 1024 && u < units.Length - 1) { v /= 1024; u++; }
        return u == 0 ? $"{v:0}{units[u]}" : $"{v:0.000}{units[u]}";
    }

    /// <summary>
    /// Rate with a fixed unit choice: always 4 significant digits with exactly one dot
    /// ("4.345GB/s", "43.45MB/s", "434.5KB/s"). Auto picks the unit that keeps the
    /// value in [1, 1000).
    /// </summary>
    public static string RateU(double bytesPerSec, RateUnit unit)
    {
        double v = Math.Max(0, bytesPerSec);
        // auto: pick largest unit ≤ value (KB floor so small traffic still shows decimals)
        if (unit == RateUnit.Auto)
        {
            if (v >= 1024UL * 1024 * 1024) unit = RateUnit.GBps;
            else if (v >= 1024 * 1024) unit = RateUnit.MBps;
            else unit = RateUnit.KBps;
        }

        double div = unit switch
        {
            RateUnit.GBps => 1024.0 * 1024 * 1024,
            RateUnit.MBps => 1024.0 * 1024,
            _ => 1024.0,
        };
        string suffix = unit switch
        {
            RateUnit.GBps => "GB/s",
            RateUnit.MBps => "MB/s",
            _ => "KB/s",
        };

        double n = v / div;
        // exactly 4 significant digits with one decimal dot: d.ddd / dd.dd / ddd.d
        string text = n switch
        {
            < 10 => n.ToString("0.000"),
            < 100 => n.ToString("00.00"),
            < 1000 => n.ToString("000.0"),
            _ => n.ToString("0000"),
        };
        return text + suffix;
    }

    /// <summary>"367.79Mbit/s", "0.059MB/s" (network meter mixed style).</summary>
    public static string BitRate(double bytesPerSec)
    {
        double bits = Math.Max(0, bytesPerSec) * 8;
        string[] units = { "bit/s", "Kbit/s", "Mbit/s", "Gbit/s" };
        double v = bits;
        int u = 0;
        while (v >= 1000 && u < units.Length - 1) { v /= 1000; u++; }
        return $"{v:0.00}{units[u]}";
    }
}

