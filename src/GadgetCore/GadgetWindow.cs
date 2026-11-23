using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace HectorBravo.Gadgets.Core;

/// <summary>
/// Base window for every gadget: borderless, rounded dark panel with blue
/// border, drag-to-move anywhere (snapped to monitor edges and other gadgets),
/// the common right-click menu (Always on top / Scale ▶ / Opacity ▶ / Options…
/// / Exit + update line), scale + opacity applied live.
/// </summary>
public class GadgetWindow : Window
{
    private readonly ConfigStore<BaseSettings> _cfg;
    private readonly string _snapName;
    private readonly ScaleTransform _scale = new(1, 1);
    private ContextMenu? _menu;

    /// <summary>Set by the gadget to open its Options window from the menu.</summary>
    public Action? OpenOptions { get; set; }

    /// <summary>Optional update line shown at the top of the context menu.</summary>
    public Func<string?> UpdateLineProvider { get; set; } = () => null;

    /// <summary>Invoked when the user clicks the update line.</summary>
    public Action? OnUpdateClick { get; set; }

    public GadgetWindow(string snapName, ConfigStore<BaseSettings> cfg)
    {
        _snapName = snapName;
        _cfg = cfg;
        var s = cfg.Data;

        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.CanResizeWithGrip; // thin grip bottom-right only
        AllowsTransparency = true;                // rounded corners + per-window opacity
        ShowInTaskbar = false;
        Background = Brushes.Transparent;
        Topmost = s.AlwaysOnTop;
        Opacity = Math.Clamp(s.Opacity, 0.2, 1.0);
        Width = s.Width; Height = s.Height;

        var panel = new Border
        {
            Background = Theme.PanelBg,
            BorderBrush = Theme.Border,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            SnapsToDevicePixels = true,
        };
        var layout = new Grid { RenderTransform = _scale };
        panel.Child = layout;
        Content = panel;

        if (s.X is double x && s.Y is double y)
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = x; Top = y;
            Loaded += (_, _) => EnsureOnScreen();
        }
        else
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        _scale.ScaleX = _scale.ScaleY = s.Scale;

        MouseLeftButtonDown += OnMouseDown;
        MouseRightButtonUp += OnRightClick;
        LocationChanged += (_, _) => Snap.Publish(_snapName, new WindowInteropHelper(this).Handle);
        Closing += (_, _) => { SaveGeometry(); Application.Current.Shutdown(); };
    }

    /// <summary>Content host inside the scaled panel (gadgets add their root here).</summary>
    public Grid ContentHost => ((Border)Content!).Child! as Grid;

    // ── drag anywhere with snapping ───────────────────────────────────────────
    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState != MouseButtonState.Pressed) return;
        try { DragMove(); } catch { /* released before loop */ }

        // snap after the drag settles (DragMove returned with final pos)
        var px = PhysicalLeftTop(out _, out _);
        var (sx, sy) = Snap.Apply(new WindowInteropHelper(this).Handle, px.x, px.y, _snapName);
        if (sx != px.x || sy != px.y)
        {
            var (dx, dy) = DeviceToDip(sx - px.x, sy - px.y);
            Left += dx; Top += dy;
        }
        SaveGeometry();
        Snap.Publish(_snapName, new WindowInteropHelper(this).Handle);
    }

    // ── right-click common menu ───────────────────────────────────────────────
    private void OnRightClick(object sender, MouseButtonEventArgs e)
    {
        _menu = BuildMenu();
        _menu.IsOpen = true;
        e.Handled = true;
    }

    private ContextMenu BuildMenu()
    {
        var s = _cfg.Data;
        var menu = new ContextMenu { Background = new SolidColorBrush(Theme.Hex("#161a23")), Foreground = Theme.Fg };

        var upd = UpdateLineProvider();
        if (!string.IsNullOrEmpty(upd))
        {
            var up = Item(upd!, Theme.Blue);
            up.FontWeight = FontWeights.Bold;
            up.Click += (_, _) => { menu.IsOpen = false; OnUpdateClick?.Invoke(); };
            menu.Items.Add(up);
            menu.Items.Add(new Separator());
        }

        var top = CheckItem("Always on top", s.AlwaysOnTop);
        top.Click += (_, _) =>
        {
            s.AlwaysOnTop = !s.AlwaysOnTop;
            Topmost = s.AlwaysOnTop;
            _cfg.Save();
        };
        menu.Items.Add(top);

        var scale = SubMenu("Scale");
        foreach (var sc in new[] { 1.0, 1.25, 1.5, 2.0 })
        {
            double captured = sc;
            var mi = CheckItem($"{(int)(sc * 100)}%", Math.Abs(s.Scale - sc) < 0.01);
            mi.Click += (_, _) =>
            {
                s.Scale = captured;
                _scale.ScaleX = _scale.ScaleY = captured;
                _cfg.Save();
            };
            scale.Items.Add(mi);
        }
        menu.Items.Add(scale);

        var opac = SubMenu("Opacity");
        foreach (var op in new[] { 0.2, 0.4, 0.6, 0.8, 1.0 })
        {
            double captured = op;
            var mi = CheckItem($"{(int)(op * 100)}%", Math.Abs(s.Opacity - op) < 0.01);
            mi.Click += (_, _) =>
            {
                s.Opacity = captured;
                Opacity = captured;
                _cfg.Save();
            };
            opac.Items.Add(mi);
        }
        menu.Items.Add(opac);

        menu.Items.Add(new Separator());

        var opts = Item("Options…");
        opts.Click += (_, _) => { menu.IsOpen = false; OpenOptions?.Invoke(); };
        menu.Items.Add(opts);

        var exit = Item("Exit");
        exit.Click += (_, _) => { SaveGeometry(); Close(); };
        menu.Items.Add(exit);

        return menu;
    }

    private static MenuItem Item(string text, Brush? fg = null) => new()
    {
        Header = text,
        Foreground = fg ?? Theme.Fg,
    };

    private static MenuItem CheckItem(string text, bool isChecked) => new()
    {
        Header = text,
        IsCheckable = true,
        IsChecked = isChecked,
        Foreground = Theme.Fg,
    };

    private static MenuItem SubMenu(string text) => new()
    {
        Header = text,
        Foreground = Theme.Fg,
    };

    // ── geometry helpers (device px vs DIP) ───────────────────────────────────
    private (int x, int y, int w, int h) PhysicalLeftTop(out int wPx, out int hPx)
    {
        var src = PresentationSource.FromVisual(this);
        double sx = 1, sy = 1;
        if (src?.CompositionTarget != null)
        {
            sx = src.CompositionTarget.TransformToDevice.M11;
            sy = src.CompositionTarget.TransformToDevice.M22;
        }
        wPx = (int)(ActualWidth * sx); hPx = (int)(ActualHeight * sy);
        return ((int)(Left * sx), (int)(Top * sy), wPx, hPx);
    }

    private (double dx, double dy) DeviceToDip(int dxPx, int dyPx)
    {
        var src = PresentationSource.FromVisual(this);
        if (src?.CompositionTarget == null) return (dxPx, dyPx);
        var m = src.CompositionTarget.TransformFromDevice;
        return (dxPx * m.M11, dyPx * m.M22);
    }

    private void EnsureOnScreen()
    {
        var area = SystemParameters.WorkArea;
        if (Left < area.Left - Width / 2 || Left > area.Right - Width / 2 ||
            Top < area.Top - Height / 2 || Top > area.Bottom - Height / 2)
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
    }

    public void SaveGeometry()
    {
        var s = _cfg.Data;
        if (WindowState == WindowState.Normal && ActualWidth > 0)
        {
            s.X = Left; s.Y = Top;
            s.Width = ActualWidth; s.Height = ActualHeight;
        }
        _cfg.Save();
    }

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        Snap.Publish(_snapName, new WindowInteropHelper(this).Handle);
    }
}
