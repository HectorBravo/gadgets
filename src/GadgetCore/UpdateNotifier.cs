using System.Windows;
using System.Windows.Threading;

namespace HectorBravo.Gadgets.Core;

/// <summary>
/// Wires an UpdateService to a GadgetWindow: periodic GitHub checks + the
/// flashing "New version available" line in the common right-click menu.
/// </summary>
public sealed class UpdateNotifier
{
    private readonly GadgetWindow _window;
    private readonly UpdateService _svc;
    private readonly DispatcherTimer _timer;
    private UpdateService.UpdateInfo? _info;

    public UpdateNotifier(GadgetWindow window, UpdateService svc)
    {
        _window = window;
        _svc = svc;

        window.UpdateLineProvider = () =>
            _info == null ? null : $"New version available ({_info.Tag.Split('/')[1]}) — click to update";
        window.OnUpdateClick = async () =>
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
                FlashOnce();
            }
        }
        catch { /* silent */ }
    }

    /// <summary>One-shot attention pulse on the window border when an update lands.</summary>
    private void FlashOnce()
    {
        try
        {
            var panel = (System.Windows.Controls.Border)_window.Content;
            var anim = new System.Windows.Media.Animation.ColorAnimationUsingKeyFrames();
            foreach (var (sec, c) in new (double, Color)[]
            {
                (0.0, Theme.Hex("#46506e")), (0.5, Theme.Hex("#87cefa")),
                (1.0, Theme.Hex("#46506e")), (1.5, Theme.Hex("#87cefa")), (2.0, Theme.Hex("#46506e")),
            })
                anim.KeyFrames.Add(new System.Windows.Media.Animation.LinearColorKeyFrame(c, TimeSpan.FromSeconds(sec)));
            anim.Duration = TimeSpan.FromSeconds(2);
            var brush = panel.BorderBrush as System.Windows.Media.SolidColorBrush;
            brush?.BeginAnimation(System.Windows.Media.SolidColorBrush.ColorProperty, anim);
        }
        catch { /* cosmetic */ }
    }
}
