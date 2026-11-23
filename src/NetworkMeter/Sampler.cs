using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace HectorBravo.Gadgets.NetworkMeter;

public record IfaceSample(
    string Name,          // adapter name (stable key)
    string Description,   // friendly description
    string Ipv4,          // first internal IPv4 or ""
    ulong LinkSpeedBps,   // negotiated speed, 0 = unknown
    double TxRate,        // bytes/sec
    double RxRate,        // bytes/sec
    bool IsUp);

public record AdapterInfo(string Name, string Description, bool IsLoopback, bool IsUp);

/// <summary>Per-interface rates via NetworkInterface IPv4 statistics deltas.</summary>
public sealed class Sampler
{
    private readonly Dictionary<string, (ulong rx, ulong tx, DateTime at)> _prev = new();
    private List<IfaceSample> _latest = new();

    public IReadOnlyList<IfaceSample> Latest => _latest;

    /// <summary>All adapters (for the Options checkbox list).</summary>
    public List<AdapterInfo> Adapters()
    {
        var list = new List<AdapterInfo>();
        try
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                list.Add(new AdapterInfo(ni.Name, ni.Description,
                    ni.NetworkInterfaceType is NetworkInterfaceType.Loopback,
                    ni.OperationalStatus == OperationalStatus.Up));
        }
        catch { /* transient */ }
        return list;
    }

    /// <summary>Sample the interfaces whose name is in `wanted`.</summary>
    public List<IfaceSample> Sample(IReadOnlyList<string> wanted)
    {
        var now = DateTime.Now;
        var outList = new List<IfaceSample>();

        try
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (!wanted.Contains(ni.Name)) continue;
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;

                ulong rx = 0, tx = 0;
                try
                {
                    var st = ni.GetIPv4Statistics();
                    rx = (ulong)Math.Max(0, st.BytesReceived);
                    tx = (ulong)Math.Max(0, st.BytesSent);
                }
                catch { /* adapter gone */ }

                double rxRate = 0, txRate = 0;
                if (_prev.TryGetValue(ni.Name, out var p) && now > p.at)
                {
                    double secs = (now - p.at).TotalSeconds;
                    if (secs > 0.1)
                    {
                        rxRate = Math.Max(0, rx - p.rx) / secs;
                        txRate = Math.Max(0, tx - p.tx) / secs;
                    }
                }
                _prev[ni.Name] = (rx, tx, now);

                outList.Add(new IfaceSample(
                    ni.Name, ni.Description, FirstIpv4(ni), (ulong)Math.Max(0, ni.Speed),
                    txRate, rxRate, ni.OperationalStatus == OperationalStatus.Up));
            }
        }
        catch { /* enumeration race */ }

        // drop counters of adapters that disappeared
        _prev.Keys.Where(k => outList.All(s => s.Name != k)).ToList()
                  .ForEach(k => _prev.Remove(k));

        _latest = outList;
        return outList;
    }

    private static string FirstIpv4(NetworkInterface ni)
    {
        try
        {
            foreach (var ua in ni.GetIPProperties().UnicastAddresses)
                if (ua.Address.AddressFamily == AddressFamily.InterNetwork)
                    return ua.Address.ToString();
        }
        catch { /* ignore */ }
        return "";
    }
}
