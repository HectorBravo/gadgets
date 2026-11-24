using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using HectorBravo.Gadgets.Core;

namespace HectorBravo.Gadgets.DrivesMeter;

public record DriveSample(
    string Letter,          // "C:"
    string Label,           // volume label or ""
    ulong Total, ulong Free,
    double ReadBps, double WriteBps);

/// <summary>
/// Per-drive space (DriveInfo) + read/write rates. Rates use IOCTL_DISK_PERFORMANCE on the
/// volume handle (\\.\C:) — works for standard users; falls back to PhysicalDrive counters,
/// then to the "LogicalDisk" perf counters if both are unavailable. Each drive keeps its own
/// history ring (#D2/#D3), plus aggregate read/write rings.
/// </summary>
public sealed class Sampler
{
    [StructLayout(LayoutKind.Sequential)]
    private struct DISK_PERFORMANCE
    {
        public long QueueTime;
        public long StartTime;
        public ulong ReadBytes;
        public ulong WriteBytes;
        public ulong ReadTime;
        public ulong WriteTime;
        public ulong IdleTime;
        public uint ReadCount;
        public uint WriteCount;
        public uint FlushCount;
        public uint Pad1;
    }

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateFileW(
        string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes,
        uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool DeviceIoControl(
        IntPtr hDevice, uint dwIoControlCode, IntPtr lpInBuffer, uint nInBufferSize,
        IntPtr lpOutBuffer, uint nOutBufferSize, out uint lpBytesReturned, IntPtr lpOverlapped);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    private const uint IOCTL_DISK_PERFORMANCE = 0x00070020;
    private const uint FILE_SHARE_READ_WRITE_DELETE = 0x00000007;
    private const uint OPEN_EXISTING = 3;

    private readonly Dictionary<string, (ulong read, ulong write, DateTime at)> _prev = new();

    public const int GraphLen = 160;

    /// <summary>Per-drive history rings (read/write bytes/s), newest-last.</summary>
    private readonly Dictionary<string, (Ring read, Ring write)> _hist = new();

    /// <summary>Aggregate rings over all selected drives (#D3).</summary>
    public HectorBravo.Gadgets.Core.Ring AggrReadHist { get; } = new(GraphLen);
    public HectorBravo.Gadgets.Core.Ring AggrWriteHist { get; } = new(GraphLen);

    public (HectorBravo.Gadgets.Core.Ring read, HectorBravo.Gadgets.Core.Ring write) HistFor(string letter)
    {
        if (!_hist.TryGetValue(letter, out var h))
        {
            h = (new HectorBravo.Gadgets.Core.Ring(GraphLen), new HectorBravo.Gadgets.Core.Ring(GraphLen));
            _hist[letter] = h;
        }
        return h;
    }

    public List<DriveSample> Sample(IReadOnlyList<string> letters)
    {
        var result = new List<DriveSample>();
        var now = DateTime.Now;
        double totalRead = 0, totalWrite = 0;

        foreach (var letter in letters)
        {
            try
            {
                var root = letter.TrimEnd('\\') + "\\"; // "C:\"
                var di = new DriveInfo(root);
                if (!di.IsReady) continue;

                ulong total = (ulong)di.TotalSize;
                ulong free = (ulong)di.TotalFreeSpace;

                var (rBps, wBps) = Counters(di.Name.TrimEnd('\\'), now); // "C:"
                totalRead += rBps;
                totalWrite += wBps;

                string label = "";
                try { label = di.VolumeLabel ?? ""; } catch { }

                var h = HistFor(di.Name);
                h.read.Add(rBps);
                h.write.Add(wBps);

                result.Add(new DriveSample(di.Name, label, total, free, rBps, wBps));
            }
            catch { /* drive gone / not ready */ }
        }

        AggrReadHist.Add(totalRead);
        AggrWriteHist.Add(totalWrite);

        // drop histories of drives that are no longer selected (memory bounded — #18/#21)
        foreach (var key in _hist.Keys.Where(k => !letters.Any(l =>
                     string.Equals(k, l.TrimEnd('\\') + ":", StringComparison.OrdinalIgnoreCase))).ToList())
            _hist.Remove(key);

        return result;
    }

    /// <summary>Read/write bytes/s for a drive: volume IOCTL → physical IOCTL → perf counters.</summary>
    private (double read, double write) Counters(string driveColon, DateTime now)
    {
        var (ok, r, w) = VolumeCounters(driveColon);
        if (!ok) (ok, r, w) = PhysicalCounters(driveColon);

        if (ok)
        {
            if (_prev.TryGetValue(driveColon, out var p) && now > p.at)
            {
                double secs = (now - p.at).TotalSeconds;
                if (secs > 0.1)
                {
                    double rBps = Math.Max(0, (long)(r - p.read)) / secs;
                    double wBps = Math.Max(0, (long)(w - p.write)) / secs;
                    _prev[driveColon] = (r, w, now);
                    return (rBps, wBps);
                }
            }
            _prev[driveColon] = (r, w, now);
            return (0, 0);
        }

        // last resort: OS perf counters (slower; only when IOCTLs are blocked)
        return PerfCounterRate(driveColon, now);
    }

    /// <summary>IOCTL_DISK_PERFORMANCE on \\.\C: — the reliable per-volume counter.</summary>
    private static (bool ok, ulong read, ulong write) VolumeCounters(string driveColon)
    {
        IntPtr h = CreateFileW($"\\\\.\\{driveColon}", 0, FILE_SHARE_READ_WRITE_DELETE,
            IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
        if (h == IntPtr.Zero || h == new IntPtr(-1)) return (false, 0, 0);
        try
        {
            int size = Marshal.SizeOf<DISK_PERFORMANCE>();
            IntPtr buf = Marshal.AllocHGlobal(size);
            try
            {
                if (!DeviceIoControl(h, IOCTL_DISK_PERFORMANCE, IntPtr.Zero, 0, buf, (uint)size, out _, IntPtr.Zero))
                    return (false, 0, 0);
                var dp = Marshal.PtrToStructure<DISK_PERFORMANCE>(buf);
                return (true, dp.ReadBytes, dp.WriteBytes);
            }
            finally { Marshal.FreeHGlobal(buf); }
        }
        finally { CloseHandle(h); }
    }

    /// <summary>IOCTL_DISK_PERFORMANCE on \\.\PhysicalDriveN (needs elevation; kept as a path).</summary>
    private (bool ok, ulong read, ulong write) PhysicalCounters(string driveColon)
    {
        int disk = PhysicalDiskFor(driveColon[0]);
        if (disk < 0) return (false, 0, 0);

        IntPtr h = CreateFileW($"\\\\.\\PhysicalDrive{disk}", 0, FILE_SHARE_READ_WRITE_DELETE,
            IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
        if (h == IntPtr.Zero || h == new IntPtr(-1)) return (false, 0, 0);
        try
        {
            int size = Marshal.SizeOf<DISK_PERFORMANCE>();
            IntPtr buf = Marshal.AllocHGlobal(size);
            try
            {
                if (!DeviceIoControl(h, IOCTL_DISK_PERFORMANCE, IntPtr.Zero, 0, buf, (uint)size, out _, IntPtr.Zero))
                    return (false, 0, 0);
                var dp = Marshal.PtrToStructure<DISK_PERFORMANCE>(buf);
                return (true, dp.ReadBytes, dp.WriteBytes);
            }
            finally { Marshal.FreeHGlobal(buf); }
        }
        finally { CloseHandle(h); }
    }

    // ── fallback: LogicalDisk perf counters (cached instances; rarely needed) ──
    private readonly Dictionary<string, (PerformanceCounter read, PerformanceCounter write)> _perfCounters = new();

    private (double read, double write) PerfCounterRate(string driveColon, DateTime now)
    {
        try
        {
            if (!_perfCounters.TryGetValue(driveColon, out var pcs))
            {
                string inst = driveColon.Replace(":", "").ToUpperInvariant() + ":";
                var cat = new PerformanceCounterCategory("LogicalDisk");
                string[] names = cat.GetInstanceNames();
                string? match = names.FirstOrDefault(n => n.Equals(inst, StringComparison.OrdinalIgnoreCase))
                             ?? names.FirstOrDefault(n => n.TrimEnd('\\').Equals(driveColon, StringComparison.OrdinalIgnoreCase));
                if (match == null) return (0, 0);

                pcs = (
                    new PerformanceCounter("LogicalDisk", "Disk Read Bytes/sec", match, readOnly: false),
                    new PerformanceCounter("LogicalDisk", "Disk Write Bytes/sec", match, readOnly: false));
                _perfCounters[driveColon] = pcs;
                pcs.read.NextValue();          // first read primes the counter
                pcs.write.NextValue();
                return (0, 0);
            }

            double r = Math.Max(0, pcs.read.NextValue());
            double w = Math.Max(0, pcs.write.NextValue());
            return (r, w);
        }
        catch { return (0, 0); }
    }

    // ── volume → physical disk mapping via storage enums ──────────────────────
    [StructLayout(LayoutKind.Sequential)]
    private struct STORAGE_DEVICE_NUMBER { public uint DeviceType; public uint DeviceNumber; public uint PartitionNumber; }

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr FindFirstVolumeW(StringBuilder lpszVolumeName, int cchBufferLength);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool FindNextVolumeW(IntPtr hFindVolume, StringBuilder lpszVolumeName, int cchBufferLength);

    [DllImport("kernel32.dll")]
    private static extern bool FindVolumeClose(IntPtr hFindVolume);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool GetVolumePathNamesForVolumeNameW(
        string lpszVolumeName, char[] lpszVolumePathNames, uint cchBufferLength, out uint lpcchReturnLength);

    /// <summary>Cache so the (slow) volume walk runs once per letter unless it failed.</summary>
    private readonly Dictionary<char, int> _diskMap = new();

    private int PhysicalDiskFor(char letter)
    {
        if (_diskMap.TryGetValue(char.ToUpperInvariant(letter), out var cached) && cached >= 0) return cached;
        int disk = MapVolumeToDisk(letter);
        if (disk >= 0) _diskMap[char.ToUpperInvariant(letter)] = disk;
        return disk;
    }

    private static int MapVolumeToDisk(char letter)
    {
        var sb = new StringBuilder(64);
        IntPtr hFind = FindFirstVolumeW(sb, sb.Capacity);
        if (hFind == IntPtr.Zero || hFind == new IntPtr(-1)) return -1;

        try
        {
            do
            {
                string vol = sb.ToString();
                var paths = new char[260];
                if (!GetVolumePathNamesForVolumeNameW(vol, paths, (uint)paths.Length, out uint len)) continue;
                string path = new string(paths, 0, (int)Math.Max(0, len - 1)); // e.g. "C:\..."
                if (path.Length == 0 || char.ToUpperInvariant(path[0]) != char.ToUpperInvariant(letter)) continue;

                IntPtr hv = CreateFileW(vol, 0, FILE_SHARE_READ_WRITE_DELETE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
                if (hv == IntPtr.Zero || hv == new IntPtr(-1)) continue;
                try
                {
                    const uint IOCTL_STORAGE_GET_DEVICE_NUMBER = 0x002D1080;
                    int size = Marshal.SizeOf<STORAGE_DEVICE_NUMBER>();
                    IntPtr buf = Marshal.AllocHGlobal(size);
                    try
                    {
                        if (DeviceIoControl(hv, IOCTL_STORAGE_GET_DEVICE_NUMBER, IntPtr.Zero, 0, buf, (uint)size, out _, IntPtr.Zero))
                        {
                            var sdn = Marshal.PtrToStructure<STORAGE_DEVICE_NUMBER>(buf);
                            if (sdn.DeviceType == 7) return (int)sdn.DeviceNumber; // FILE_DEVICE_DISK
                        }
                    }
                    finally { Marshal.FreeHGlobal(buf); }
                }
                finally { CloseHandle(hv); }
            } while (FindNextVolumeW(hFind, sb, sb.Capacity));
        }
        finally { FindVolumeClose(hFind); }

        return -1;
    }
}

