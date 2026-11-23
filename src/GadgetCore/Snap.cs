using System.Runtime.InteropServices;

namespace HectorBravo.Gadgets.Core;

/// <summary>
/// Snap helper: while dragging, snap the window to monitor edges and to other
/// gadgets' rects. Other gadgets publish their rect (device px) to
/// %TEMP%\RustGadgets\&lt;name&gt;.rect on every move.
/// </summary>
public static class Snap
{
    private const int Threshold = 14;

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left, Top, Right, Bottom; }

    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
    [DllImport("user32.dll")] private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);
    [DllImport("user32.dll")] private static extern bool GetMonitorInfo(IntPtr hMon, ref MONITORINFO lpmi);

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    private const uint MONITOR_DEFAULTTONEAREST = 2;

    public static string Dir => System.IO.Path.Combine(Path.GetTempPath(), "RustGadgets");

    /// <summary>Publish our rect (device px) so other gadgets snap to us.</summary>
    public static void Publish(string name, IntPtr hwnd)
    {
        try
        {
            Directory.CreateDirectory(Dir);
            if (GetWindowRect(hwnd, out var r))
                File.WriteAllText(System.IO.Path.Combine(Dir, name + ".rect"),
                    $"{r.Left},{r.Top},{r.Right - r.Left},{r.Bottom - r.Top}");
        }
        catch { /* cosmetic */ }
    }

    /// <summary>
    /// Adjust a proposed window position (device px) so it snaps to monitor edges
    /// and other gadget rects. Returns the adjusted (x, y).
    /// </summary>
    public static (int x, int y) Apply(IntPtr hwnd, int x, int y, string selfName)
    {
        if (!GetWindowRect(hwnd, out var r)) return (x, y);
        int w = r.Right - r.Left, h = r.Bottom - r.Top;

        var xs = new List<int>();
        var ys = new List<int>();

        // monitor work areas (taskbar aware) — one per monitor
        foreach (var mon in Monitors())
        {
            xs.Add(mon.rcWork.Left); xs.Add(mon.rcWork.Right);
            ys.Add(mon.rcWork.Top);  ys.Add(mon.rcWork.Bottom);
            xs.Add(mon.rcMonitor.Left); xs.Add(mon.rcMonitor.Right);
            ys.Add(mon.rcMonitor.Top);  ys.Add(mon.rcMonitor.Bottom);
        }

        // other gadgets' rects
        foreach (var (l, t, ow, oh) in OtherRects(selfName))
        {
            int oright = l + ow, obottom = t + oh;
            xs.AddRange(new[] { oright, l - w, l, oright - w });
            ys.AddRange(new[] { obottom, t - h, t, obottom - h });
        }

        return (Nearest(x, xs), Nearest(y, ys));
    }

    private static IEnumerable<MONITORINFO> Monitors()
    {
        // walk monitors via EnumDisplayMonitors callback
        var list = new List<MONITORINFO>();
        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMon, IntPtr hdc, ref RECT rc, IntPtr data) =>
        {
            var mi = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
            if (GetMonitorInfo(hMon, ref mi)) list.Add(mi);
            return true;
        }, IntPtr.Zero);
        return list;
    }

    private delegate bool MonitorEnumProc(IntPtr hMon, IntPtr hdc, ref RECT lprc, IntPtr data);
    [DllImport("user32.dll")] private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

    private static IEnumerable<(int l, int t, int w, int h)> OtherRects(string selfName)
    {
        var found = new List<(int, int, int, int)>();
        if (!Directory.Exists(Dir)) return found;
        foreach (var file in Directory.GetFiles(Dir, "*.rect"))
        {
            var stem = System.IO.Path.GetFileNameWithoutExtension(file);
            if (stem == selfName) continue;
            try
            {
                if (DateTime.Now - System.IO.File.GetLastWriteTime(file) > TimeSpan.FromSeconds(10)) continue; // stale
                var parts = System.IO.File.ReadAllText(file).Split(',');
                if (parts.Length == 4 &&
                    int.TryParse(parts[0], out var l) && int.TryParse(parts[1], out var t) &&
                    int.TryParse(parts[2], out var w) && int.TryParse(parts[3], out var h))
                    found.Add((l, t, w, h));
            }
            catch { /* transient file race */ }
        }
        return found;
    }

    private static int Nearest(int v, List<int> targets)
    {
        int best = v, bestD = Threshold + 1;
        foreach (var t in targets)
        {
            int d = Math.Abs(v - t);
            if (d < bestD) { bestD = d; best = t; }
        }
        return best;
    }
}
