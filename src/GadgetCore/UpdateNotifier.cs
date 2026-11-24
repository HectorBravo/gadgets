using System.Windows.Threading;

namespace HectorBravo.Gadgets.Core;

/// <summary>
/// Wires an UpdateService to a GadgetWindow&lt;T&gt;: periodic GitHub checks + the
/// flashing light-blue "New version available (stamp) — click to update" line at the
/// BOTTOM of the gadget body (the window grows by that line's height only while it is
/// visible). Clicking applies the update: download → swap helper → relaunch.
/// </summary>
public sealed class UpdateNotifier<T> where T : BaseSettings, new()
{
    private readonly GadgetWindow<T> _host;
    private readonly UpdateService _svc;
    private readonly DispatcherTimer _timer;
    private UpdateService.UpdateInfo? _info;

    public UpdateNotifier(GadgetWindow<T> host, UpdateService svc)
    {
        _host = host;
        _svc = svc;

        host.UpdateLineProvider = () => _info == null
            ? null
            : $"New version available ({_info.Tag.Split('/')[1]}) — click to update";
        host.OnUpdateClick = async () =>
        {
            var info = _info;
            if (info == null) return;
            try { await _svc.DownloadAndApplyAsync(info); } // shuts the app down on success
            catch { /* stays running; line remains for a retry */ }
        };

        _timer = new DispatcherTimer { Interval = TimeSpan.FromHours(1) };
        _timer.Tick += async (_, _) => await CheckNow();
        _timer.Start();
    }

    public async Task CheckNow()
    {
        try
        {
            var info = await _svc.CheckAsync();
            if (info != null && !Equals(info, _info))
            {
                _info = info;
                _host.InvalidateUpdateLine();
            }
        }
        catch { /* silent */ }
    }
}

