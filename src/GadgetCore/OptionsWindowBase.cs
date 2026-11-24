using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace HectorBravo.Gadgets.Core;

/// <summary>
/// Base for every gadget's Options window: light theme (white background, black
/// text), NO scrollbars — the window is sized to the largest tab so all content of
/// every tab is visible at once (#9). Every change saves immediately (#8).
/// </summary>
public class OptionsWindowBase : Window
{
    /// <summary>Fixed width for options windows (DIP).</summary>
    public const double OptionsWidth = 400;

    private readonly TabControl _tabs = new();
    private double _contentHeight;

    protected OptionsWindowBase(string title)
    {
        Title = title;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = MenuStyle.Bg;                       // white-ish
        Foreground = MenuStyle.Fg;                       // black
        SizeToContent = SizeToContent.Manual;
        ResizeMode = ResizeMode.CanResize;               // user may enlarge, never loses content
        Width = OptionsWidth;

        Content = _tabs;
    }

    /// <summary>Add a tab. All tabs are then forced to the same (largest) size.</summary>
    protected void AddTab(string header, Panel content)
    {
        var item = new TabItem { Header = header, Content = content };
        _tabs.Items.Add(item);
    }

    /// <summary>Call after building all tabs: measures each tab's desired size and sizes the window so nothing scrolls.</summary>
    protected void FinalizeSize()
    {
        // measure every tab's content at the fixed width, take the largest height
        double maxH = 0;
        var availW = OptionsWidth - 24;   // borders + padding allowance
        foreach (var obj in _tabs.Items)
        {
            if (obj is not TabItem ti || ti.Content is not FrameworkElement fe) continue;
            fe.Measure(new Size(availW, double.PositiveInfinity));
            var d = fe.DesiredSize.Height;
            if (d > maxH) maxH = d;
        }

        // tab headers + padding + title bar/borders allowance
        Height = Math.Round(maxH + 96);
        MinHeight = Height;
        _contentHeight = maxH;
    }

    // ── shared light controls (all save immediately via the callback) ──────────
    protected static CheckBox Check(string text, bool initial, Action<bool> onChange)
    {
        var cb = new CheckBox
        {
            Content = text,
            IsChecked = initial,
            Foreground = MenuStyle.Fg,
            Margin = new Thickness(0, 4, 0, 4),
        };
        cb.Checked += (_, _) => onChange(true);
        cb.Unchecked += (_, _) => onChange(false);
        return cb;
    }

    protected static TextBlock Heading(string text) => new()
    {
        Text = text,
        Foreground = MenuStyle.Fg,
        FontSize = 13,
        FontWeight = FontWeights.Bold,
        Margin = new Thickness(0, 8, 0, 4),
    };

    protected static TextBlock Hint(string text) => new()
    {
        Text = text,
        Foreground = MenuStyle.Disabled,
        FontSize = 11,
        Margin = new Thickness(0, 0, 0, 6),
        TextWrapping = TextWrapping.Wrap,
    };

    /// <summary>Combo for enum-like settings; saves on selection.</summary>
    protected static ComboBox EnumChoice<TEnum>(TEnum current, Action<TEnum> onChange) where TEnum : struct, Enum
    {
        var combo = new ComboBox { Width = 160, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 2, 0, 4) };
        foreach (var v in Enum.GetValues<TEnum>()) combo.Items.Add(v.ToString());
        combo.SelectedItem = current.ToString();
        combo.SelectionChanged += (_, _) =>
        {
            if (combo.SelectedItem is string s && Enum.TryParse<TEnum>(s, out var val)) onChange(val);
        };
        return combo;
    }

    /// <summary>Text field that commits + saves on every keystroke-safe change (LostFocus + Enter).</summary>
    protected static TextBox Field(string initial, Action<string> onCommit)
    {
        var box = new TextBox { Text = initial, Width = 220, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 2, 0, 4) };
        box.LostFocus += (_, _) => onCommit(box.Text);
        box.KeyUp += (_, e) =>
        {
            if (e.Key == System.Windows.Input.Key.Enter) onCommit(box.Text);
        };
        return box;
    }

    /// <summary>Label + control row.</summary>
    protected static FrameworkElement Row(string label, FrameworkElement control)
    {
        var g = new Grid { Margin = new Thickness(0, 3, 0, 3) };
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
        g.ColumnDefinitions.Add(new ColumnDefinition());
        var lbl = new TextBlock { Text = label, Foreground = MenuStyle.Fg, VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(lbl, 0); g.Children.Add(lbl);
        Grid.SetColumn(control, 1); g.Children.Add(control);
        return g;
    }
}
