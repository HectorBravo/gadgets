using System.Runtime.InteropServices;
using HectorBravo.Gadgets.Core;

namespace HectorBravo.Gadgets.CpuMeter;

/// <summary>What a logical processor is: normal/P core, efficient core, or a thread (SMT sibling).</summary>
public enum CoreKind { NormalCore, EfficientCore, Thread }

public record CoreStat(double Pct, uint ClockMhz, CoreKind Kind);

public record Snapshot(
    double TotalPct,
    uint ClockMhz,
    ulong RamUsed, ulong RamFree, ulong RamTotal,
    ulong SwapUsed, ulong SwapFree, ulong SwapTotal,
    IReadOnlyList<CoreStat> Cores);

/// <summary>
/// CPU / clock / RAM / swap sampler — pure Win32 (one NtQuerySystemInformation call per
/// tick for usage; GetLogicalProcessorInformationEx once for topology; registry + PDH-style
/// CurrentClockSpeed fallback for clocks). Kept as cheap as possible (#21).
/// </summary>
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

    // topology (built once): per logical index → kind + base clock
    private CoreKind[] _kinds = Array.Empty<CoreKind>();
    private uint[] _baseClock = Array.Empty<uint>();
    private uint[] _clockNow = Array.Empty<uint>();
    private DateTime _clockAt = DateTime.MinValue;

    public Snapshot Sample()
    {
        // ── CPU per core + total ──
        var pcts = new double[_nCpu];
        double totalPct = 0;

        var cur = QueryProcessorTimes();
        var now = DateTime.Now;
        if (cur.Length > 0 && _prev.Length == cur.Length && now > _prevAt)
        {
            double sumPct = 0;
            for (int i = 0; i < cur.Length && i < _nCpu; i++)
            {
                long idleDelta = cur[i].IdleTime - _prev[i].IdleTime;
                long busyDelta = (cur[i].KernelTime + cur[i].UserTime) - (_prev[i].KernelTime + _prev[i].UserTime);
                if (busyDelta > 0)
                {
                    double pct = Math.Clamp(100.0 * (busyDelta - idleDelta) / busyDelta, 0, 100);
                    pcts[i] = pct;
                    sumPct += pct;
                }
                else pcts[i] = _prevCores[i];
            }
            totalPct = cur.Length > 0 ? sumPct / cur.Length : 0;
            _prevCores = pcts;
        }

        _prev = cur;
        _prevAt = now;

        EnsureTopology();
        RefreshClocks(now);

        var cores = new CoreStat[_nCpu];
        for (int i = 0; i < _nCpu; i++)
            cores[i] = new CoreStat(pcts[i], _clockNow.Length > i ? _clockNow[i] : 0, _kinds.Length > i ? _kinds[i] : CoreKind.NormalCore);

        // ── memory + swap ──
        var m = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
        GlobalMemoryStatusEx(ref m);

        if (_clockMhz == 0) _clockMhz = ReadBaseClockMhz();

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

    // ── topology: real cores vs threads vs efficiency classes (#P3) ───────────
    private void EnsureTopology()
    {
        if (_kinds.Length == _nCpu) return;

        _kinds = new CoreKind[_nCpu];
        _baseClock = new uint[_nCpu];
        for (int i = 0; i < _nCpu; i++) { _kinds[i] = CoreKind.NormalCore; _baseClock[i] = 0; }

        try
        {
            int size = 0;
            GetLogicalProcessorInformationEx(RelationProcessorCore, IntPtr.Zero, ref size);
            if (size <= 0) return;
            IntPtr buf = Marshal.AllocHGlobal(size);
            try
            {
                if (!GetLogicalProcessorInformationEx(RelationProcessorCore, buf, ref size)) return;

                int pos = 0;
                while (pos + 8 <= size)
                {
                    IntPtr entry = buf + pos;
                    int relationship = Marshal.ReadInt32(entry);
                    uint entryLen = (uint)Marshal.ReadInt32(entry, 4);
                    if (entryLen < 8 || pos + entryLen > size) break;

                    if (relationship == RelationProcessorCore)
                    {
                        // LPI_GROUP_ARRAY_DESCRIPTOR<LPI_PROCESSOR> at offset 8: group(count) + processors[]
                        uint count = (uint)Marshal.ReadInt32(entry, 8);
                        IntPtr procArr = entry + 16;
                        int effClass = 0;
                        bool hasEff = false;
                        // effective class follows the processor array on Windows 10+
                        int effOff = 16 + (int)count * 4;
                        if (effOff + 4 <= entryLen)
                        {
                            effClass = Marshal.ReadInt32(entry, effOff);
                            hasEff = true;
                        }

                        // first logical processor of a core = the core itself, rest are SMT threads.
                        // Efficiency class < 167 → E-core (Intel hybrid topology).
                        for (uint p = 0; p < count; p++)
                        {
                            int idx = Marshal.ReadInt32(procArr, (int)p * 4);
                            if (idx < 0 || idx >= _nCpu) continue;
                            if (p == 0)
                                _kinds[idx] = hasEff && effClass < 167 ? CoreKind.EfficientCore : CoreKind.NormalCore;
                            else
                                _kinds[idx] = CoreKind.Thread;
                        }
                    }

                    pos += (int)entryLen;
                }
            }
            finally { Marshal.FreeHGlobal(buf); }
        }
        catch { /* keep defaults */ }

        // per-logical base clock from the registry (~MHz of each logical processor)
        try
        {
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                @"HARDWARE\DESCRIPTION\System\CentralProcessor");
            if (key != null)
            {
                foreach (var name in key.GetSubKeyNames())
                {
                    if (!int.TryParse(name, out int idx)) continue;
                    using var sub = key.OpenSubKey(name);
                    if (sub?.GetValue("~MHz") is int mhz && idx < _nCpu)
                        _baseClock[idx] = (uint)mhz;
                }
            }
        }
        catch { /* optional */ }

        _clockNow = new uint[_nCpu];
        for (int i = 0; i < _nCpu; i++) _clockNow[i] = _baseClock[i];
    }

    private const int RelationProcessorCore = 0;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetLogicalProcessorInformationEx(int relationship, IntPtr buffer, ref int returnLength);

    /// <summary>Kind of logical processor i (topology must be built first).</summary>
    public CoreKind KindFor(int i) => _kinds.Length > i ? _kinds[i] : CoreKind.NormalCore;

    /// <summary>Per-logical effective clock: registry base scaled by the load (cheap heuristic).</summary>
    private void RefreshClocks(DateTime now)
    {
        if (_clockNow.Length == 0) return;
        // refresh at most twice per second — cheap and enough for display
        if (now - _clockAt < TimeSpan.FromMilliseconds(500)) return;
        _clockAt = now;

        uint sys = ReadClockMhzKey("CurrentClockSpeed");   // live-ish base reported by OS/BIOS
        for (int i = 0; i < _nCpu; i++)
        {
            uint b = _baseClock[i] > 0 ? _baseClock[i] : sys;
            if (b == 0) { _clockNow[i] = 0; continue; }

            double load = _prevCores.Length > i ? _prevCores[i] / 100.0 : 0;
            // idle logical processors downclock toward ~40% base; busy ones sit at/above base
            double eff = b * (0.4 + 0.6 * load) * (load > 0.8 ? 1.15 : 1.0);
            _clockNow[i] = (uint)Math.Round(eff);
        }
    }

    /// <summary>HKLM\...\HardwareDescription\Processor ~MHz (base clock, always available).</summary>
    private static uint ReadBaseClockMhz() => ReadClockMhzKey("~MHz");

    private static uint ReadClockMhzKey(string valueName)
    {
        try
        {
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                @"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
            if (key?.GetValue(valueName) is int mhz && mhz > 0) return (uint)mhz;
        }
        catch { /* fall through */ }
        return 0;
    }
}

