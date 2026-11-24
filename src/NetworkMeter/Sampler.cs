using System.Net.NetworkInformation;
using HectorBravo.Gadgets.Core;

namespace HectorBravo.Gadgets.NetworkMeter;

public record IfSample(
    string Id, string Name, long SpeedBps,
    double RxRate, double TxRate);

/// <summary>
/// Network sampler: per-interface rx/tx bytes/s (from the OS interface statistics — no
/// packet capture), plus a self-heal counter so a stalled enumeration (#19) recovers.
/// </summary>
public sealed class Sampler
{
    private readonly Dictionary<string, (long rx, long tx, DateTime at)> _prev = new();

    /// <summary>Monotonically increasing; increments on every successful sample (UI watchdog #19).</summary>
    public long Tick { get; private set; }

    /// <summary>Rx/tx history rings (aggregate over selected interfaces), newest-last.</summary>
    public Ring RxHist { get; } = new(600);
    public Ring TxHist { get; } = new(600);

    public List<IfSample> Sample(IReadOnlyList<string> selectedIds)
    {
        var list = new List<IfSample>();
        var now = DateTime.Now;
        double sumRx = 0, sumTx = 0;

        try
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (!selectedIds.Contains(ni.Id)) continue;

                var st = ni.GetIPv4Statistics();
                long rx = st.BytesReceived, tx = st.BytesSent;

                double rxRate = 0, txRate = 0;
                if (_prev.TryGetValue(ni.Id, out var p) && now > p.at)
                {
                    double secs = (now - p.at).TotalSeconds;
                    if (secs > 0.1)
                    {
                        rxRate = Math.Max(0, rx - p.rx) / secs;
                        txRate = Math.Max(0, tx - p.tx) / secs;
                    }
                }
                _prev[ni.Id] = (rx, tx, now);

                sumRx += rxRate;
                sumTx += txRate;

                list.Add(new IfSample(ni.Id, ni.Name, ni.Speed, rxRate, txRate));
            }
        }
        catch
        {
            // enumeration hiccup — drop the stale prev entries so the next tick re-baselines
            _prev.Clear();
        }

        RxHist.Add(sumRx);
        TxHist.Add(sumTx);
        Tick++;
        return list;
    }
}






