using System.Runtime.InteropServices;

namespace HectorBravo.Gadgets.Core;

/// <summary>Win32 helpers shared by the (generic) GadgetWindow — DllImport is not allowed on generic types.</summary>
internal static class Win32Drag
{
    [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X; public int Y; }

    [DllImport("user32.dll")] public static extern bool GetCursorPos(out POINT p);
    [DllImport("user32.dll")] public static extern IntPtr SetCapture(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool ReleaseCapture();
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out Snap.RECT r);

    public const uint SWP_NOSIZE = 0x0001, SWP_NOZORDER = 0x0004, SWP_NOACTIVATE = 0x0010;

    [DllImport("user32.dll")]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr after, int x, int y, int cx, int cy, uint flags);
}

/// <summary>
/// Magnet snap: a dragged gadget is pulled (with a small separation Gap) to monitor
/// edges and to other gadgets' rects — both live during the drag and when it settles.
/// Other gadgets publish their rect (device px) to %TEMP%\RustGadgets\<name>.rect.
/// </summary>
public static class Snap
{
    /// <summary>Pull radius in device px: within this distance an edge "grabs" the window.</summary>
    private const int Threshold = 18;

    /// <small>Separation kept between two gadgets / between a gadget and the screen edge.</small>
    public const int Gap = 6;

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
    /// Adjust a proposed window position (device px) so it snaps — with Gap separation —
    /// to monitor edges and other gadget rects. Returns the adjusted (x, y).
    /// </summary>
    public static (int x, int y) Apply(IntPtr hwnd, int x, int y, string selfName)
    {
        if (!GetWindowRect(hwnd, out var r)) return (x, y);
        int w = r.Right - r.Left, h = r.Bottom - r.Top;

        var xs = new List<int>();
        var ys = new List<int>();

        // monitor work areas (taskbar aware) + full monitors — one per monitor
        foreach (var mon in Monitors())
        {
            AddPair(xs, mon.rcWork.Left, mon.rcWork.Right, w);
            AddPair(ys, mon.rcWork.Top, mon.rcWork.Bottom, h);
            AddPair(xs, mon.rcMonitor.Left, mon.rcMonitor.Right, w);
            AddPair(ys, mon.rcMonitor.Top, mon.rcMonitor.Bottom, h);
        }

        // other gadgets' rects (magnet on all four sides, with separation)
        foreach (var (l, t, ow, oh) in OtherRects(selfName))
        {
            int oright = l + ow, obottom = t + oh;
            AddPair(xs, l - w - Gap, oright + Gap, 0);      // our right ↔ their left / ours left ↔ their right
            AddPair(ys, t - h - Gap, obottom + Gap, 0);
            // edge alignment (left↔left, right↔right) without the gap on the same axis
            xs.Add(l); xs.Add(oright - w);
            ys.Add(t); ys.Add(obottom - h);
        }

        return (Nearest(x, xs), Nearest(y, ys));
    }

    /// <summary>Add both snap positions of an axis (min edge, max edge minus extent).</summary>
    private static void AddPair(List<int> list, int minEdge, int maxEdge, int extent)
    {
        if (extent > 0)
        {
            list.Add(minEdge - Gap);              // our near edge just inside the monitor
            list.Add(maxEdge + Gap - extent);     // our far edge just inside
        }
        else
        {
            list.Add(minEdge);
            list.Add(maxEdge);
        }
    }

    private static IEnumerable<MONITORINFO> Monitors()
    {
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
                if (DateTime.Now - File.GetLastWriteTime(file) > TimeSpan.FromSeconds(10)) continue; // stale
                var parts = File.ReadAllText(file).Split(',');
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

