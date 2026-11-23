using System.Diagnostics;

namespace HectorBravo.Gadgets.NetworkMeter;

/// <summary>Fetches the machine-wide external IPv4 via a hidden PowerShell (api.ipify.org).</summary>
public sealed class ExternalIp
{
    private readonly object _lock = new();
    private string? _value;          // null = unknown, "n/a" = failed
    private bool _fetching;

    public string Display
    {
        get { lock (_lock) return _value ?? "…"; }
    }

    public bool Fetching { get { lock (_lock) return _fetching; } }

    /// <summary>Start a background fetch if none is in flight.</summary>
    public void Refresh()
    {
        lock (_lock)
        {
            if (_fetching) return;
            _fetching = true;
        }

        Task.Run(() =>
        {
            string result = FetchBlocking();
            lock (_lock) { _value = result; _fetching = false; }
        });
    }

    private static string FetchBlocking()
    {
        try
        {
            var psi = new ProcessStartInfo("powershell.exe",
                "-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command " +
                "\"try{(Invoke-RestMethod -UseBasicParsing -Uri https://api.ipify.org -TimeoutSec 15)}catch{exit 1}\"")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using var p = Process.Start(psi);
            if (p == null) return "n/a";
            string stdout = p.StandardOutput.ReadToEnd().Trim();
            if (!p.WaitForExit(20000)) { try { p.Kill(); } catch { } return "n/a"; }
            return IsValidIpv4(stdout) ? stdout : "n/a";
        }
        catch { return "n/a"; }
    }

    private static bool IsValidIpv4(string s)
    {
        var parts = s.Split('.');
        if (parts.Length != 4) return false;
        foreach (var part in parts)
            if (part.Length == 0 || part.Length > 3 || !byte.TryParse(part, out _)) return false;
        return true;
    }
}
