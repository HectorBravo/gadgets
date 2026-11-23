using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace HectorBravo.Gadgets.DrivesMeter;

public record DriveSample(
    string Letter,          // "C:"
    string Label,           // volume label or ""
    ulong Total, ulong Free,
    double ReadBps, double WriteBps);

/// <summary>Per-drive space (DriveInfo) + read/write rates (IOCTL_DISK_PERFORMANCE deltas).</summary>
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

    [DllImport("kernel32.dll", SetLastError = true)]
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
    private const uint GENERIC_READ = 0x80000000;
    private const uint FILE_SHARE_READ_WRITE_DELETE = 0x00000007;
    private const uint OPEN_EXISTING = 3;

    private readonly Dictionary<string, (ulong read, ulong write, DateTime at)> _prev = new();

    public const int GraphLen = 160;

    /// <summary>Aggregate activity samples (bytes/s) newest-last.</summary>
    public List<double> ActivityHist { get; } = new();

    public List<DriveSample> Sample(IReadOnlyList<string> letters)
    {
        var result = new List<DriveSample>();
        var now = DateTime.Now;
        double totalActivity = 0;

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
                totalActivity += rBps + wBps;

                string label = "";
                try { label = di.VolumeLabel ?? ""; } catch { }

                result.Add(new DriveSample(di.Name, label, total, free, rBps, wBps));
            }
            catch { /* drive gone / not ready */ }
        }

        ActivityHist.Add(totalActivity);
        if (ActivityHist.Count > GraphLen) ActivityHist.RemoveAt(0);
        return result;
    }

    private (double read, double write) Counters(string driveColon, DateTime now)
    {
        int disk = PhysicalDiskFor(driveColon[0]);
        if (disk < 0) return (0, 0);

        IntPtr h = CreateFileW($"\\.\\PhysicalDrive{disk}", GENERIC_READ, FILE_SHARE_READ_WRITE_DELETE,
            IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
        if (h == IntPtr.Zero || h == new IntPtr(-1)) return (0, 0);

        try
        {
            int size = Marshal.SizeOf<DISK_PERFORMANCE>();
            IntPtr buf = Marshal.AllocHGlobal(size);
            try
            {
                if (!DeviceIoControl(h, IOCTL_DISK_PERFORMANCE, IntPtr.Zero, 0, buf, (uint)size, out _, IntPtr.Zero))
                    return (0, 0);

                var dp = Marshal.PtrToStructure<DISK_PERFORMANCE>(buf);

                double rBps = 0, wBps = 0;
                if (_prev.TryGetValue(driveColon, out var p) && now > p.at)
                {
                    double secs = (now - p.at).TotalSeconds;
                    if (secs > 0.1)
                    {
                        rBps = Math.Max(0, dp.ReadBytes - p.read) / secs;
                        wBps = Math.Max(0, dp.WriteBytes - p.write) / secs;
                    }
                }
                _prev[driveColon] = (dp.ReadBytes, dp.WriteBytes, now);
                return (rBps, wBps);
            }
            finally { Marshal.FreeHGlobal(buf); }
        }
        finally { CloseHandle(h); }
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
