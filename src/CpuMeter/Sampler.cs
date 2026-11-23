using System.Runtime.InteropServices;

namespace HectorBravo.Gadgets.CpuMeter;

public record CoreStat(double Pct);

public record Snapshot(
    double TotalPct,
    uint ClockMhz,
    ulong RamUsed, ulong RamFree, ulong RamTotal,
    ulong SwapUsed, ulong SwapFree, ulong SwapTotal,
    IReadOnlyList<double> CorePct);

/// <summary>CPU / clock / RAM / swap sampler (pure Win32 P/Invoke).</summary>
public sealed class Sampler
{
    // ── per-processor times via NtQuerySystemInformation ─────────────────────
    [StructLayout(LayoutKind.Sequential)]
    private struct SYSTEM_PROCESSOR_PERFORMANCE_INFORMATION
    {
        public long IdleTime;
        public long KernelTime;
        public long UserTime;
        public long DpcTime;
        public long InterruptTime;
        public uint InterruptCount;
    }

    [DllImport("ntdll.dll")]
    private static extern int NtQuerySystemInformation(
        int systemInformationClass, IntPtr systemInformation, int systemInformationLength, ref int returnLength);

    private const int SystemProcessorPerformanceInformation = 8;

    // ── clock speed via WMI (Win32_PerfFormattedData_PerfProc_Process is noisy; use registry CurrentClockSpeed fallback) ──
    [StructLayout(LayoutKind.Sequential)]
    private struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    private SYSTEM_PROCESSOR_PERFORMANCE_INFORMATION[] _prev = Array.Empty<SYSTEM_PROCESSOR_PERFORMANCE_INFORMATION>();
    private DateTime _prevAt = DateTime.MinValue;
    private uint _clockMhz;
    private readonly int _nCpu = Environment.ProcessorCount;

    public Snapshot Sample()
    {
        // ── CPU per core + total ──
        var cores = new double[_nCpu];
        double totalPct = 0;

        var cur = QueryProcessorTimes();
        var now = DateTime.Now;
        if (cur.Length > 0 && _prev.Length == cur.Length && now > _prevAt)
        {
            double wallMs = (now - _prevAt).TotalMilliseconds;
            double sumPct = 0;
            for (int i = 0; i < cur.Length && i < _nCpu; i++)
            {
                long idleDelta = cur[i].IdleTime - _prev[i].IdleTime;
                long busyDelta = (cur[i].KernelTime + cur[i].UserTime) - (_prev[i].KernelTime + _prev[i].UserTime);
                if (busyDelta > 0)
                {
                    double pct = Math.Clamp(100.0 * (busyDelta - idleDelta) / busyDelta, 0, 100);
                    cores[i] = pct;
                    sumPct += pct;
                }
                else cores[i] = _prevCores[i];
            }
            totalPct = cur.Length > 0 ? sumPct / cur.Length : 0;
            _prevCores = cores;
        }

        _prev = cur;
        _prevAt = now;

        // ── memory + swap ──
        var m = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
        GlobalMemoryStatusEx(ref m);

        if (_clockMhz == 0) _clockMhz = ReadClockMhz();

        return new Snapshot(
            totalPct, _clockMhz,
            m.ullTotalPhys - m.ullAvailPhys, m.ullAvailPhys, m.ullTotalPhys,
            m.ullTotalPageFile - m.ullAvailPageFile, m.ullAvailPageFile, m.ullTotalPageFile,
            cores);
    }

    private double[] _prevCores = Array.Empty<double>();

    private SYSTEM_PROCESSOR_PERFORMANCE_INFORMATION[] QueryProcessorTimes()
    {
        int len = 0;
        // first call with null buffer to get the required size (STATUS_INFO_LENGTH_MISMATCH = 0xC0000004)
        NtQuerySystemInformation(SystemProcessorPerformanceInformation, IntPtr.Zero, 0, ref len);
        if (len <= 0) return Array.Empty<SYSTEM_PROCESSOR_PERFORMANCE_INFORMATION>();

        IntPtr buf = Marshal.AllocHGlobal(len);
        try
        {
            int status = NtQuerySystemInformation(SystemProcessorPerformanceInformation, buf, len, ref len);
            if (status != 0) return Array.Empty<SYSTEM_PROCESSOR_PERFORMANCE_INFORMATION>();

            int stride = Marshal.SizeOf<SYSTEM_PROCESSOR_PERFORMANCE_INFORMATION>();
            int n = len / stride;
            var arr = new SYSTEM_PROCESSOR_PERFORMANCE_INFORMATION[n];
            for (int i = 0; i < n; i++)
                arr[i] = Marshal.PtrToStructure<SYSTEM_PROCESSOR_PERFORMANCE_INFORMATION>(buf + i * stride);
            return arr;
        }
        finally { Marshal.FreeHGlobal(buf); }
    }

    /// <summary>HKLM\...\HardwareDescription\Processor ~MHz (base clock, always available).</summary>
    private static uint ReadClockMhz()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                @"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
            var v = key?.GetValue("~MHz") as int?;
            if (v is int mhz && mhz > 0) return (uint)mhz;
        }
        catch { /* fall through */ }
        return 0;
    }
}
