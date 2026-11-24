using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace HectorBravo.Gadgets.Core;

/// <summary>
/// Base window for every gadget: borderless dark panel (opaque; translucency only
/// through Window.Opacity), never user-resizable — it consumes exactly the width and
/// height its content needs (fixed gadget width, auto height). Scale is a
/// LayoutTransform so changing it resizes the window. Drag anywhere with live magnet
/// snapping; the right-click menu opens instantly and every menu/submenu is light
/// (white background, black text) via MenuStyle. A flashing "update available" line
/// sits at the BOTTOM of the gadget body and applies the update on click.
/// </summary>
public class GadgetWindow<T> : Window where T : BaseSettings, new()
{
    /// <summary>Gadget width at 100% scale (DIP) — the Drives Meter reference width.</summary>
    public const double GadgetWidth = 236;

    protected readonly ConfigStore<T> Cfg;
    private readonly string _snapName;
    private readonly ScaleTransform _scale = new(1, 1);
    private readonly Border _panel;
    private readonly Grid _layout;
    private readonly TextBlock _updateLine;
    private readonly UpdateFlasher _flasher;

    /// <summary>Set by the gadget to open its Options window from the menu.</summary>
    public Action? OpenOptions { get; set; }

    /// <summary>Optional update-line text (null/empty hides the bottom line).</summary>
    public Func<string?> UpdateLineProvider { get; set; } = () => null;

    /// <summary>Invoked when the user clicks the flashing update line.</summary>
    public Action? OnUpdateClick { get; set; }

    protected GadgetWindow(string snapName, ConfigStore<T> cfg)
    {
        Cfg = cfg;
        _snapName = snapName;
        var s = cfg.Data;

        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;                 // gadgets are never user-resizable
        AllowsTransparency = true;                        // rounded corners + window opacity
        ShowInTaskbar = false;
        Background = Brushes.Transparent;
        SizeToContent = SizeToContent.WidthAndHeight;     // exactly the space content needs
        Topmost = s.AlwaysOnTop;
        Opacity = Math.Clamp(s.Opacity, 0.2, 1.0);

        _panel = new Border
        {
            Background = Theme.PanelBg,                   // fully opaque (#0d1117)
            BorderBrush = Theme.Border,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            SnapsToDevicePixels = true,
        };

        _layout = new Grid { LayoutTransform = _scale, Width = GadgetWidth };
        _panel.Child = _layout;
        Content = _panel;

        // bottom-of-gadget update line (flashing light blue) — hidden until an update exists
        _updateLine = new TextBlock
        {
            Foreground = Theme.Blue,
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(6, 2, 6, 4),
            Visibility = Visibility.Collapsed,
            Cursor = Cursors.Hand,
        };
        _updateLine.MouseLeftButtonUp += (_, e) =>
        {
            e.Handled = true;
            if (_updateLine.Visibility == Visibility.Visible) OnUpdateClick?.Invoke();
        };
        _layout.Children.Add(_updateLine);

        _flasher = new UpdateFlasher(_updateLine);

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

        _scale.ScaleX = _scale.ScaleY = ScaleValue(s.Scale);

        MouseLeftButtonDown += OnMouseDown;
        ContextMenuOpening += OnContextMenuOpening;       // instant, light menu at the cursor
        LocationChanged += (_, _) => Snap.Publish(_snapName, new WindowInteropHelper(this).Handle);
        Closing += (_, _) => { SaveGeometry(); Application.Current.Shutdown(); };

        // keep the bottom update line in sync with UpdateLineProvider
        var vt = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        vt.Tick += (_, _) => RefreshUpdateLineVisibility();
        vt.Start();
    }

    /// <summary>Content host inside the scaled panel (gadgets add their root here).</summary>
    public Grid ContentHost => _layout;

    /// <summary>The bottom update-line TextBlock (flashing) — managed by UpdateNotifier.</summary>
    public TextBlock UpdateLineText => _updateLine;

    private static double ScaleValue(double s) => s switch
    {
        >= 1.875 => 2.0,
        >= 1.375 => 1.5,
        >= 1.125 => 1.25,
        _ => 1.0,
    };

    /// <summary>Call after UpdateLineProvider may have changed (UpdateNotifier does).</summary>
    public void InvalidateUpdateLine() => RefreshUpdateLineVisibility();

    private void RefreshUpdateLineVisibility()
    {
        string? txt = null;
        try { txt = UpdateLineProvider(); } catch { /* provider not ready */ }
        if (string.IsNullOrEmpty(txt))
        {
            _updateLine.Visibility = Visibility.Collapsed;
            _flasher.Stop();
        }
        else
        {
            if (_updateLine.Text != txt) _updateLine.Text = txt;
            _updateLine.Visibility = Visibility.Visible;
            _flasher.Start();
        }
    }

    // ── drag anywhere with LIVE magnet snapping (WPF mouse capture) ───────────
    private bool _dragActive;
    private int _grabX, _grabY;      // cursor offset inside the window (device px)

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState != MouseButtonState.Pressed) return;

        var hwnd = new WindowInteropHelper(this).Handle;
        Win32Drag.GetCursorPos(out var pt);
        Win32Drag.GetWindowRect(hwnd, out var r);
        _grabX = pt.X - r.Left;
        _grabY = pt.Y - r.Top;

        _dragActive = true;
        Mouse.Capture(this, CaptureMode.SubTree);
    }

    /// <summary>While captured, mouse-move reaches us even outside the window.</summary>
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!_dragActive) return;
        if (e.LeftButton != MouseButtonState.Pressed) { FinishDrag(); return; }

        Win32Drag.GetCursorPos(out var pt);
        MoveDevice(pt.X - _grabX, pt.Y - _grabY);
    }



    protected override void OnLostMouseCapture(MouseEventArgs e)
    {
        base.OnLostMouseCapture(e);
        if (_dragActive) FinishDrag();
    }

    private void FinishDrag()
    {
        if (!_dragActive) return;
        _dragActive = false;
        Mouse.Capture(null);

        var hwnd = new WindowInteropHelper(this).Handle;
        // settle: final snap + publish + persist position
        var px = PhysicalLeftTop(out _, out _);
        var (sx, sy) = Snap.Apply(hwnd, px.x, px.y, _snapName);
        if (sx != px.x || sy != px.y)
        {
            var (dx, dy) = DeviceToDip(sx - px.x, sy - px.y);
            Left += dx; Top += dy;
        }
        SaveGeometry();
        Snap.Publish(_snapName, hwnd);
    }

    /// <summary>Move the window so its top-left is at device-px (x,y), snapped live.</summary>
    private void MoveDevice(int xPx, int yPx)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        var (sx, sy) = Snap.Apply(hwnd, xPx, yPx, _snapName);
        Win32Drag.SetWindowPos(hwnd, IntPtr.Zero, sx, sy, 0, 0,
            Win32Drag.SWP_NOSIZE | Win32Drag.SWP_NOZORDER | Win32Drag.SWP_NOACTIVATE);
    }

    // ── right-click common menu (opens instantly, light styling) ──────────────
    private void OnContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        var menu = BuildMenu();
        menu.PlacementTarget = this;
        menu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
        menu.IsOpen = true;                           // shown on this very event — no delay
        e.Handled = true;                             // suppress the default (deferred) open
    }

    private ContextMenu BuildMenu()
    {
        var s = Cfg.Data;
        // light styling comes from the app-wide MenuStyle resources (white bg, black text)
        var menu = new ContextMenu();

        var top = CheckItem("Always on top", s.AlwaysOnTop);
        top.Click += (_, _) =>
        {
            s.AlwaysOnTop = !s.AlwaysOnTop;
            Topmost = s.AlwaysOnTop;
            Cfg.Save();                               // saved immediately
        };
        menu.Items.Add(top);

        var scale = SubMenu("Scale");
        foreach (var sc in new[] { 1.0, 1.25, 1.5, 2.0 })
        {
            double captured = sc;
            var mi = CheckItem($"{(int)(captured * 100)}%", Math.Abs(s.Scale - captured) < 0.01);
            mi.Click += (_, _) =>
            {
                s.Scale = captured;
                _scale.ScaleX = _scale.ScaleY = captured;   // LayoutTransform → window resizes
                Cfg.Save();
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    EnsureOnScreen();
                    var px = PhysicalLeftTop(out _, out _);
                    var (sx, sy) = Snap.Apply(new WindowInteropHelper(this).Handle, px.x, px.y, _snapName);
                    if (sx != px.x || sy != px.y)
                    {
                        var (dx, dy) = DeviceToDip(sx - px.x, sy - px.y);
                        Left += dx; Top += dy;
                    }
                    SaveGeometry();
                }), DispatcherPriority.Loaded);
            };
            scale.Items.Add(mi);
        }
        menu.Items.Add(scale);

        var opac = SubMenu("Opacity");
        foreach (var op in new[] { 0.2, 0.4, 0.6, 0.8, 1.0 })
        {
            double captured = op;
            var mi = CheckItem($"{(int)(captured * 100)}%", Math.Abs(s.Opacity - op) < 0.01);
            mi.Click += (_, _) =>
            {
                s.Opacity = captured;
                Opacity = captured;                   // panel is opaque → true 100% at 1.0
                Cfg.Save();
            };
            opac.Items.Add(mi);
        }
        menu.Items.Add(opac);

        menu.Items.Add(new Separator());

        var opts = Item("Options…");
        opts.Click += (_, _) => { OpenOptions?.Invoke(); };
        menu.Items.Add(opts);

        var exit = Item("Exit");
        exit.Click += (_, _) => { SaveGeometry(); Close(); };
        menu.Items.Add(exit);

        return menu;
    }

    private static MenuItem Item(string text) => new() { Header = text };

    private static MenuItem CheckItem(string text, bool isChecked) => new()
    {
        Header = text,
        IsCheckable = true,
        IsChecked = isChecked,
    };

    private static MenuItem SubMenu(string text) => new() { Header = text };

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
        if (Left < area.Left - ActualWidth / 2 || Left > area.Right - ActualWidth / 2 ||
            Top < area.Top - ActualHeight / 2 || Top > area.Bottom - ActualHeight / 2)
        {
            Left = Math.Clamp(Left, area.Left, Math.Max(area.Left, area.Right - ActualWidth));
            Top = Math.Clamp(Top, area.Top, Math.Max(area.Top, area.Bottom - ActualHeight));
        }
    }

    /// <summary>Persist the window position (size is derived from content — never stored).</summary>
    public void SaveGeometry()
    {
        var s = Cfg.Data;
        if (WindowState == WindowState.Normal && ActualWidth > 0)
        {
            s.X = Left; s.Y = Top;
        }
        Cfg.Save();
    }

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        Snap.Publish(_snapName, new WindowInteropHelper(this).Handle);
    }
}

