using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace HectorBravo.Gadgets.Core;

/// <summary>
/// Light palette + styles for every popup (ContextMenu / MenuItem / submenus) and
/// for the Options windows: whitish background, black text everywhere. Applied
/// app-wide from GadgetApplication so nothing inherits a dark system theme.
/// </summary>
public static class MenuStyle
{
    public static readonly Brush Bg        = Frozen("#ffffff");   // menu / window background
    public static readonly Brush Fg        = Frozen("#000000");   // black text everywhere
    public static readonly Brush Hover     = Frozen("#e8f2fe");   // item highlight (light blue)
    public static readonly Brush Pressed   = Frozen("#d2e8fb");
    public static readonly Brush Disabled  = Frozen("#7a7a7a");
    public static readonly Brush Outline    = Frozen("#b9c3cf");
    public static readonly Brush InputBg   = Frozen("#ffffff");
    public static readonly Brush BgAlt     = Frozen("#eef2f6");   // button face
    public static readonly Brush BorderBrush = Frozen("#b9c3cf");  // control borders

    private static Color Hex(string s) => (Color)ColorConverter.ConvertFromString(s);
    private static Brush Frozen(string s) { var b = new SolidColorBrush(Hex(s)); b.Freeze(); return b; }

    /// <summary>Build the light-theme resource dictionary and merge it app-wide.</summary>
    public static void Apply(Application app)
    {
        var rd = new ResourceDictionary();
        rd[typeof(ContextMenu)] = ContextMenuStyle();
        rd[typeof(MenuItem)] = MenuItemStyle();
        rd[typeof(Separator)] = SeparatorStyle();
        rd[typeof(Window)] = Style(typeof(Window), (Window.BackgroundProperty, Frozen("#f6f8fa")));
        rd[typeof(TabControl)] = Style(typeof(TabControl), (TabControl.BackgroundProperty, Bg));
        rd[typeof(TabItem)] = Style(typeof(TabItem), (TabControl.ForegroundProperty, Fg));
        rd[typeof(CheckBox)] = Style(typeof(CheckBox), (CheckBox.ForegroundProperty, Fg));
        rd[typeof(RadioButton)] = Style(typeof(RadioButton), (RadioButton.ForegroundProperty, Fg));
        rd[typeof(GroupBox)] = Style(typeof(GroupBox), (GroupBox.ForegroundProperty, Fg));
        rd[typeof(Label)] = Style(typeof(Label), (Label.ForegroundProperty, Fg));
        rd[typeof(TextBlock)] = Style(typeof(TextBlock), (TextBlock.ForegroundProperty, Fg));
        rd[typeof(TextBox)] = TextBoxStyle();
        rd[typeof(Button)] = ButtonStyle();
        rd[typeof(Slider)] = SliderStyle();
        rd[typeof(ListBox)] = ListBoxStyle();
        rd[typeof(ListViewItem)] = ListViewItemStyle();
        app.Resources.MergedDictionaries.Add(rd);
    }

    private static Style Style(Type t, params (DependencyProperty, object)[] setters)
    {
        var s = new Style(t);
        foreach (var (p, v) in setters) s.Setters.Add(new Setter(p, v));
        return s;
    }

    private static Style ContextMenuStyle() =>
        Style(typeof(ContextMenu),
            (ContextMenu.BackgroundProperty, Bg),
            (TextBlock.ForegroundProperty, Fg),
            (ContextMenu.BorderBrushProperty, Outline),
            (ContextMenu.FontSizeProperty, 12.0));

    private static Style SeparatorStyle() =>
        Style(typeof(Separator),
            (Separator.BackgroundProperty, Frozen("#d7dde5")),
            (FrameworkElement.MarginProperty, new Thickness(2, 3, 2, 3)),
            (FrameworkElement.HeightProperty, 1.0));

    private static Style TextBoxStyle() =>
        Style(typeof(TextBox),
            (TextBox.BackgroundProperty, InputBg), (TextBox.ForegroundProperty, Fg),
            (TextBox.BorderBrushProperty, Outline), (Control.PaddingProperty, new Thickness(3, 2, 3, 2)));

    private static Style ButtonStyle() =>
        Style(typeof(Button),
            (Button.BackgroundProperty, Frozen("#eef2f6")), (Button.ForegroundProperty, Fg),
            (Button.BorderBrushProperty, Outline), (Control.PaddingProperty, new Thickness(8, 3, 8, 3)));

    private static Style SliderStyle() =>
        Style(typeof(Slider),
            (Slider.ForegroundProperty, Fg), (Slider.BackgroundProperty, Frozen("#d8dee6")));

    private static Style ListBoxStyle() =>
        Style(typeof(ListBox),
            (ListBox.BackgroundProperty, InputBg), (ListBox.ForegroundProperty, Fg),
            (ListBox.BorderBrushProperty, Outline));

    private static Style ListViewItemStyle() =>
        Style(typeof(ListViewItem), (ListViewItem.ForegroundProperty, Fg), (ListViewItem.BackgroundProperty, Bg));

    private static Style MenuItemStyle()
    {
        var s = new Style(typeof(MenuItem));
        s.Setters.Add(new Setter(MenuItem.ForegroundProperty, Fg));
        s.Setters.Add(new Setter(MenuItem.BackgroundProperty, Bg));
        s.Setters.Add(new Setter(MenuItem.FontFamilyProperty, Theme.Ui));
        s.Setters.Add(new Setter(Control.TemplateProperty, MenuItemTemplate()));
        return s;
    }

    private static System.Collections.ObjectModel.Collection<ColumnDefinition> Cols(params ColumnDefinition[] defs)
    {
        var c = new System.Collections.ObjectModel.Collection<ColumnDefinition>();
        foreach (var d in defs) c.Add(d);
        return c;
    }

    /// <summary>Fluent helper: configure a FrameworkElementFactory inline.</summary>
    private static FrameworkElementFactory With(this FrameworkElementFactory f, Action<FrameworkElementFactory> setup)
    {
        setup(f);
        return f;
    }

    /// <summary>
    /// Light MenuItem template valid for every Role (TopLevelHeader, SubmenuHeader,
    /// SubmenuItem): white panel, black text, black tick mark, submenu arrow.
    /// </summary>
    private static ControlTemplate MenuItemTemplate()
    {
        var t = new ControlTemplate(typeof(MenuItem));

        // highlight border around the item row
        var bdr = new FrameworkElementFactory(typeof(Border));
        bdr.Name = "bd";
        bdr.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
        bdr.SetValue(Border.BorderBrushProperty, Frozen("#cfe4fa"));
        bdr.SetValue(Border.BorderThicknessProperty, new Thickness(0));
        bdr.SetValue(Border.SnapsToDevicePixelsProperty, true);

        var g = new FrameworkElementFactory(typeof(Grid));
        // three columns: tick | header (star) | submenu arrow
        g.AppendChild(new FrameworkElementFactory(typeof(ColumnDefinition))
            .With(v => { v.SetValue(ColumnDefinition.WidthProperty, GridLength.Auto); v.SetValue(FrameworkElement.MinWidthProperty, 20.0); }));
        g.AppendChild(new FrameworkElementFactory(typeof(ColumnDefinition))
            .With(v => v.SetValue(ColumnDefinition.WidthProperty, new GridLength(1, GridUnitType.Star))));
        g.AppendChild(new FrameworkElementFactory(typeof(ColumnDefinition))
            .With(v => v.SetValue(ColumnDefinition.WidthProperty, GridLength.Auto)));

        var check = new FrameworkElementFactory(typeof(TextBlock));
        check.Name = "checkMark";
        check.SetValue(Grid.ColumnProperty, 0);
        check.SetValue(TextBlock.TextProperty, "\u2713");
        check.SetValue(TextBlock.ForegroundProperty, Fg);
        check.SetValue(TextBlock.FontWeightProperty, FontWeights.Bold);
        check.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        check.SetValue(FrameworkElement.MarginProperty, new Thickness(4, 0, 2, 0));
        check.SetValue(UIElement.VisibilityProperty, Visibility.Collapsed);

        var hdr = new FrameworkElementFactory(typeof(ContentPresenter));
        hdr.Name = "headerHost";
        hdr.SetValue(Grid.ColumnProperty, 1);
        hdr.SetValue(ContentPresenter.ContentSourceProperty, "Header");
        hdr.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        hdr.SetValue(TextBlock.ForegroundProperty, Fg);
        hdr.SetValue(FrameworkElement.MarginProperty, new Thickness(4, 3, 10, 3));

        var arrow = new FrameworkElementFactory(typeof(TextBlock));
        arrow.Name = "subArrow";
        arrow.SetValue(Grid.ColumnProperty, 2);
        arrow.SetValue(TextBlock.TextProperty, "\u25ba");
        arrow.SetValue(TextBlock.ForegroundProperty, Fg);
        arrow.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        arrow.SetValue(FrameworkElement.MarginProperty, new Thickness(6, 0, 4, 0));
        arrow.SetValue(UIElement.VisibilityProperty, Visibility.Collapsed);

        g.AppendChild(check);
        g.AppendChild(hdr);
        g.AppendChild(arrow);
        bdr.AppendChild(g);

        // submenu popup — same light styling as the parent menu
        var pop = new FrameworkElementFactory(typeof(System.Windows.Controls.Primitives.Popup));
        pop.Name = "PART_Popup";
        pop.SetValue(System.Windows.Controls.Primitives.Popup.AllowsTransparencyProperty, false);
        pop.SetValue(System.Windows.Controls.Primitives.Popup.PlacementProperty,
                     System.Windows.Controls.Primitives.PlacementMode.Right);
        pop.SetValue(System.Windows.Controls.Primitives.Popup.VerticalOffsetProperty, -3.0);
        pop.SetValue(System.Windows.Controls.Primitives.Popup.IsOpenProperty, false);

        var popBdr = new FrameworkElementFactory(typeof(Border));
        popBdr.SetValue(Border.BackgroundProperty, Bg);
        popBdr.SetValue(Border.BorderBrushProperty, Outline);
        popBdr.SetValue(Border.BorderThicknessProperty, new Thickness(1));
        popBdr.SetValue(TextBlock.ForegroundProperty, Fg);
        var itemsHost = new FrameworkElementFactory(typeof(ItemsPresenter));
        itemsHost.SetValue(FrameworkElement.MarginProperty, new Thickness(2));
        popBdr.AppendChild(itemsHost);
        pop.AppendChild(popBdr);

        var root = new FrameworkElementFactory(typeof(DockPanel));
        root.AppendChild(pop);
        root.AppendChild(bdr);
        t.VisualTree = root;

        // hover / press / disabled
        var hl = new Trigger { Property = MenuItem.IsHighlightedProperty, Value = true };
        hl.Setters.Add(new Setter(Border.BackgroundProperty, Hover, "bd"));
        t.Triggers.Add(hl);

        var pr = new Trigger { Property = MenuItem.IsPressedProperty, Value = true };
        pr.Setters.Add(new Setter(Border.BackgroundProperty, Pressed, "bd"));
        t.Triggers.Add(pr);

        var dis = new Trigger { Property = UIElement.IsEnabledProperty, Value = false };
        dis.Setters.Add(new Setter(TextBlock.ForegroundProperty, Disabled));
        t.Triggers.Add(dis);

        // checked → show the black tick
        var ck = new Trigger { Property = MenuItem.IsCheckedProperty, Value = true };
        ck.Setters.Add(new Setter(UIElement.VisibilityProperty, Visibility.Visible, "checkMark"));
        t.Triggers.Add(ck);

        // Role: submenu parent — arrow + open popup
        var roleSubHeader = new Trigger { Property = MenuItem.RoleProperty, Value = "SubmenuHeader" };
        roleSubHeader.Setters.Add(new Setter(UIElement.VisibilityProperty, Visibility.Visible, "subArrow"));
        roleSubHeader.Setters.Add(new Setter(System.Windows.Controls.Primitives.Popup.IsOpenProperty, true, "PART_Popup"));
        t.Triggers.Add(roleSubHeader);

        // Role: submenu leaf — its popup is hosted by the parent menu, not here
        var roleSubItem = new Trigger { Property = MenuItem.RoleProperty, Value = "SubmenuItem" };
        roleSubItem.Setters.Add(new Setter(UIElement.VisibilityProperty, Visibility.Collapsed, "PART_Popup"));
        t.Triggers.Add(roleSubItem);

        // Role: top-level header — no popup needed for our menus
        var roleTop = new Trigger { Property = MenuItem.RoleProperty, Value = "TopLevelHeader" };
        roleTop.Setters.Add(new Setter(UIElement.VisibilityProperty, Visibility.Collapsed, "PART_Popup"));
        t.Triggers.Add(roleTop);

        return t;
    }
}



