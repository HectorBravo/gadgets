namespace HectorBravo.Gadgets.Core;

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
