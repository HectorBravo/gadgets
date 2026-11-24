using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace HectorBravo.Gadgets.Core;

/// <summary>
/// Continuously flashes a TextBlock between dim and bright light blue (same hue as
/// the clock's HH:MM) — used for the "update available" line at the gadget bottom.
/// </summary>
public sealed class UpdateFlasher
{
    private readonly TextBlock _target;
    private readonly ColorAnimationUsingKeyFrames _anim;
    private readonly SolidColorBrush _brush;
    private bool _on;

    public UpdateFlasher(TextBlock target)
    {
        _target = target;
        _brush = new SolidColorBrush(Theme.Hex("#87cefa"));
        _target.Foreground = _brush;

        var dim = (Color)ColorConverter.ConvertFromString("#3d5c73");   // darkened #87cefa
        var bright = Theme.Hex("#87cefa");
        _anim = new ColorAnimationUsingKeyFrames
        {
            Duration = TimeSpan.FromSeconds(1.6),
            RepeatBehavior = RepeatBehavior.Forever,
        };
        _anim.KeyFrames.Add(new LinearColorKeyFrame(dim, KeyTime.FromPercent(0.0)));
        _anim.KeyFrames.Add(new LinearColorKeyFrame(bright, KeyTime.FromPercent(0.5)));
        _anim.KeyFrames.Add(new LinearColorKeyFrame(dim, KeyTime.FromPercent(1.0)));
    }

    public void Start()
    {
        if (_on) return;
        _on = true;
        _brush.BeginAnimation(SolidColorBrush.ColorProperty, _anim);
    }

    public void Stop()
    {
        if (!_on) return;
        _on = false;
        _brush.BeginAnimation(SolidColorBrush.ColorProperty, null);
        _target.Foreground = Theme.Blue;
    }
}
