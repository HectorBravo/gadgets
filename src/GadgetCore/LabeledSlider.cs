using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace HectorBravo.Gadgets.Core;

/// <summary>
/// Slider with the full tick/label treatment (#7): a small mark for every selectable
/// value on the track, the currently selected value right under the thumb, and min /
/// max + evenly spaced intermediate numbers under the bar. Changes commit immediately.
/// </summary>
public class LabeledSlider : StackPanel
{
    private readonly Slider _slider;
    private readonly TextBlock _valueText = new();
    private readonly Canvas _ticks = new() { Height = 5 };
    private readonly Canvas _labels = new() { Height = 14 };
    private readonly int _min, _max, _step;
    private readonly string _suffix;
    private bool _ready;

    /// <param name="label">Text above the slider.</param>
    /// <param name="min">Minimum (selectable) value.</param>
    /// <param name="max">Maximum (selectable) value.</param>
    /// <param name="step">Selectable step between values (tick marks are drawn per step).</param>
    /// <param name="value">Initial value.</param>
    /// <param name="onChange">Called with the new value on every change (saved immediately).</param>
    /// <param name="suffix">Appended to the displayed numbers (e.g. " px", " s").</param>
    public LabeledSlider(string label, int min, int max, int step, double value, Action<double> onChange, string suffix = "")
    {
        _min = min; _max = max; _step = Math.Max(1, step); _suffix = suffix;

        Margin = new Thickness(0, 4, 0, 6);

        Children.Add(new TextBlock { Text = label, Foreground = MenuStyle.Fg, FontSize = 12 });

        // current value right under the selector
        _valueText.Foreground = MenuStyle.Fg;
        _valueText.FontSize = 11;
        _valueText.HorizontalAlignment = HorizontalAlignment.Center;
        _valueText.Margin = new Thickness(0, 1, 0, 1);

        _slider = new Slider
        {
            Minimum = min,
            Maximum = max,
            Value = Math.Clamp(value, min, max),
            IsSnapToTickEnabled = true,
            TickFrequency = _step,
            TickPlacement = System.Windows.Controls.Primitives.TickPlacement.BottomRight,
            IsMoveToPointEnabled = true,
        };
        _slider.ValueChanged += (_, e) =>
        {
            UpdateValueText();
            if (_ready) onChange(e.NewValue);
        };

        Children.Add(_slider);
        Children.Add(_valueText);
        Children.Add(_ticks);     // small mark per selectable value (no numbers)
        Children.Add(_labels);    // min / intermediate / max numbers

        Loaded += (_, _) => { _ready = true; BuildTicks(); UpdateValueText(); };
        SizeChanged += (_, _) => { if (_ready) BuildTicks(); };
    }

    /// <summary>Set the slider position without firing onChange (programmatic sync).</summary>
    public void SetValueSilently(double v)
    {
        _ready = false;
        _slider.Value = Math.Clamp(v, _min, _max);
        UpdateValueText();
        _ready = true;
    }

    private int Steps => Math.Max(1, (_max - _min) / _step);

    private void UpdateValueText() =>
        _valueText.Text = $"{(int)Math.Round(_slider.Value)}{_suffix}";

    /// <summary>Draw one small tick per selectable value + the min/max/intermediate numbers.</summary>
    private void BuildTicks()
    {
        double w = _slider.ActualWidth;
        if (w <= 4) return;

        _ticks.Children.Clear();
        _labels.Children.Clear();

        int steps = Steps;
        // guard: don't draw thousands of marks when step is tiny relative to range
        int markEvery = Math.Max(1, steps / 60);

        for (int i = 0; i <= steps; i++)
        {
            double x = w * i / steps;
            var line = new System.Windows.Shapes.Rectangle
            {
                Width = 1,
                Height = i % markEvery == 0 ? 5 : 3,
                Fill = new SolidColorBrush(Color.FromRgb(0x60, 0x6a, 0x78)),
            };
            Canvas.SetLeft(line, Math.Round(x));
            _ticks.Children.Add(line);
        }

        // min, max + evenly spaced intermediate numbers (max 7 labels)
        int labelEvery = Math.Max(1, steps / 6);
        for (int i = 0; i <= steps; i += labelEvery)
        {
            double x = w * i / steps;
            int v = _min + i * _step;
            var tb = new TextBlock
            {
                Text = v + _suffix,
                FontSize = 9.5,
                Foreground = MenuStyle.Fg,
            };
            tb.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double tx = x - tb.DesiredSize.Width / 2;
            tx = Math.Clamp(tx, 0, Math.Max(0, w - tb.DesiredSize.Width));
            Canvas.SetLeft(tb, tx);
            _labels.Children.Add(tb);
        }

        // always include the max label when the loop skipped it
        int lastLabeled = steps - steps % labelEvery;
        if (lastLabeled != steps)
        {
            var tb = new TextBlock { Text = _max + _suffix, FontSize = 9.5, Foreground = MenuStyle.Fg };
            tb.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(tb, Math.Max(0, w - tb.DesiredSize.Width));
            _labels.Children.Add(tb);
        }
    }
}
