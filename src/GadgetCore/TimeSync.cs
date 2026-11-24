using System.Runtime.InteropServices;

namespace HectorBravo.Gadgets.Core;

/// <summary>
/// Applies a clock offset via SetSystemTime (requires SE_SYSTEMTIME_NAME — i.e. an
/// elevated process). Returns false when the privilege is missing instead of throwing.
/// </summary>
public static class TimeSync
{
    [StructLayout(LayoutKind.Sequential)]
    private struct SYSTEMTIME
    {
        public ushort wYear, wMonth, wDayOfWeek, wDay;
        public ushort wHour, wMinute, wSecond, wMilliseconds;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetSystemTime(ref SYSTEMTIME st);

    /// <summary>Step the system clock by offset (server − local). True on success.</summary>
    public static bool ApplyOffset(TimeSpan offset)
    {
        try
        {
            var t = DateTime.UtcNow + offset;
            var st = new SYSTEMTIME
            {
                wYear = (ushort)t.Year,
                wMonth = (ushort)t.Month,
                wDayOfWeek = (ushort)t.DayOfWeek,
                wDay = (ushort)t.Day,
                wHour = (ushort)t.Hour,
                wMinute = (ushort)t.Minute,
                wSecond = (ushort)t.Second,
                wMilliseconds = (ushort)t.Millisecond,
            };
            return SetSystemTime(ref st);
        }
        catch
        {
            return false;
        }
    }
}
