using System.Net;
using System.Net.Sockets;
using System.Text;

namespace HectorBravo.Gadgets.Core;

/// <summary>
/// Minimal SNTP (RFC 4330) client: queries a server and reports the offset between
/// the local clock and the server time. Used by the Digital Clock "Sync time" action.
/// </summary>
public static class SntpClient
{
    /// <summary>Query result: round-trip time and (local → server) offset.</summary>
    public sealed record Result(TimeSpan RoundTrip, TimeSpan Offset);

    /// <summary>Query the NTP server (UDP/123). Throws on timeout / bad reply.</summary>
    public static async Task<Result> QueryAsync(string server, CancellationToken ct = default)
    {
        // resolve first (pool.ntp.org returns several A records; take any)
        var ips = await Dns.GetHostAddressesAsync(server, AddressFamily.InterNetwork, ct).ConfigureAwait(false);
        if (ips.Length == 0) throw new InvalidOperationException("No IPv4 address for " + server);

        var buf = new byte[48];
        buf[0] = 0x1B;                                   // LI=0, VN=3, Mode=3 (client)

        // translate the client transmit timestamp into the packet (NTP epoch 1900)
        var t1 = DateTime.UtcNow;
        ulong ntp = ToNtp(t1);
        for (int i = 0; i < 8; i++) buf[40 + i] = (byte)(ntp >> (56 - 8 * i));

        using var udp = new UdpClient();
        udp.Client.SendTimeout = 5000;
        udp.Client.ReceiveTimeout = 5000;
        await udp.SendAsync(buf, buf.Length, new IPEndPoint(ips[0], 123)).ConfigureAwait(false);

        var recvTask = udp.ReceiveAsync();
        var done = await Task.WhenAny(recvTask, Task.Delay(TimeSpan.FromSeconds(6), ct)).ConfigureAwait(false);
        if (done != recvTask) throw new TimeoutException("NTP server did not answer");
        var res = await recvTask.ConfigureAwait(false);
        var t4 = DateTime.UtcNow;

        var r = res.Buffer;
        if (r.Length < 48) throw new InvalidOperationException("Short NTP reply");
        byte mode = (byte)(r[0] & 7);
        if (mode != 4 && mode != 1) throw new InvalidOperationException("Not an NTP reply");
        byte stratum = r[1];
        if (stratum == 0 || stratum > 15) throw new InvalidOperationException("Invalid NTP stratum");

        ulong t2 = FromNtp(r, 32);   // server receive
        ulong t3 = FromNtp(r, 40);   // server transmit

        var spanT1 = TimeSpan.FromTicks(TicksFromNtp(ToNtp(t1)));
        var spanT2 = TimeSpan.FromTicks(TicksFromNtp(t2));
        var spanT3 = TimeSpan.FromTicks(TicksFromNtp(t3));
        var spanT4 = t4 - DateTime.UnixEpoch;

        var roundTrip = (spanT4 - spanT1) - (spanT3 - spanT2);
        var offset = ((spanT2 - spanT1) + (spanT3 - spanT4)) / 2;   // ≈ server − local
        return new Result(roundTrip, offset);
    }

    private const long NtpTicksEpoch = 621355968000000000L;   // DateTime(1900,1,1).Ticks

    private static ulong ToNtp(DateTime utc)
    {
        // seconds since 1900 in 32.32 fixed point
        long ticks = utc.Ticks - NtpTicksEpoch;
        ulong secs = (ulong)(ticks / TimeSpan.TicksPerSecond);
        ulong frac = (ulong)((ticks % TimeSpan.TicksPerSecond) * 4294967296L / TimeSpan.TicksPerSecond);
        return (secs << 32) | frac;
    }

    private static ulong FromNtp(byte[] b, int off)
    {
        ulong v = 0;
        for (int i = 0; i < 8; i++) v = (v << 8) | b[off + i];
        return v;
    }

    private static long TicksFromNtp(ulong ntp)
    {
        uint secs = (uint)(ntp >> 32);
        uint frac = (uint)ntp;
        return (long)secs * TimeSpan.TicksPerSecond + (long)((double)frac / 4294967296.0 * TimeSpan.TicksPerSecond);
    }
}
