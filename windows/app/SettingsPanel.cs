using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using Drawing = System.Drawing;
using Forms = System.Windows.Forms;

// Dark card panel styled after the black cat: charcoal surface, lime accents.
// Closes on Esc, on losing activation (click into any other app) and when the
// pet itself is clicked. Rebuilt on open so every control reflects saved state.
internal sealed class SettingsPanel : Window
{
    private readonly PetWindow pet;
    private readonly Preferences settings;
    private readonly DispatcherTimer saveDelay = new DispatcherTimer();
    private readonly ControlTemplate pillIdle, pillActive, ghost, sliderTemplate, toggleTemplate, comboTemplate;
    private readonly Style comboItemStyle;
    private readonly DataTemplate comboItemTemplate;
    private readonly Border card;
    private bool closing;
    private Slider scaleSlider;
    private TextBlock scaleValue;
    private Button hideButton;

    public SettingsPanel(PetWindow owner, Preferences prefs)
    {
        pet = owner; settings = prefs;
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        ShowInTaskbar = false;
        ShowActivated = true;
        SizeToContent = SizeToContent.Height;
        Topmost = true;
        Width = 306;
        FontFamily = new FontFamily("Microsoft YaHei UI");
        FontSize = 12;

        pillIdle = PillTemplate("#2C2933", "#373340", "#423E4A");
        pillActive = PillTemplate("#CDF159", "#D9F76B", "#B8DC49");
        ghost = PillTemplate("Transparent", "#2C2933", "#373340");
        sliderTemplate = BuildSliderTemplate();
        toggleTemplate = BuildToggleTemplate();
        comboTemplate = BuildComboTemplate();
        comboItemStyle = BuildComboItemStyle();
        comboItemTemplate = BuildComboItemTemplate();

        saveDelay.Interval = TimeSpan.FromMilliseconds(300);
        saveDelay.Tick += delegate { saveDelay.Stop(); settings.Save(); };
        PreviewKeyDown += delegate(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) Close();
        };
        // Closing makes the panel lose activation; without this guard the WM_ACTIVATE
        // that arrives mid-close calls Close() again and WPF throws.
        Deactivated += delegate { if (!closing) Close(); };

        card = new Border
        {
            Background = Paint("#221F26"),
            BorderBrush = Paint("#3A3641"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(16, 14, 16, 14),
            Effect = new DropShadowEffect { BlurRadius = 24, ShadowDepth = 0, Opacity = .55, Color = Colors.Black }
        };
        card.Child = BuildContent();
        Content = card;
        card.Measure(new Size(272, double.PositiveInfinity));
        Place();
    }

    // Height as measured during construction, used to position the panel before it shows.
    public double OpenHeight { get { return card.DesiredSize.Height; } }

    private void Place()
    {
        // Runs before Show, so the pet's own source carries the DPI transform and
        // the size comes from the pre-measured card: the window lands next to the
        // pet on first paint instead of flashing at the default corner first.
        PresentationSource view = PresentationSource.FromVisual(pet);
        Matrix fromDevice = view != null && view.CompositionTarget != null
            ? view.CompositionTarget.TransformFromDevice : Matrix.Identity;
        Rect petRect = new Rect(pet.Left, pet.Top, pet.Width, pet.Height);
        Point center = new Point(petRect.Left + petRect.Width * .5, petRect.Top + petRect.Height * .5);
        Rect area = Rect.Empty;
        foreach (Forms.Screen screen in Forms.Screen.AllScreens)
        {
            Drawing.Rectangle work = screen.WorkingArea;
            Rect logical = new Rect(fromDevice.Transform(new Point(work.Left, work.Top)),
                                    fromDevice.Transform(new Point(work.Right, work.Bottom)));
            if (area.IsEmpty) area = logical;
            if (logical.Contains(center)) { area = logical; break; }
        }
        if (area.IsEmpty) area = new Rect(0, 0, 1200, 800);
        double width = Width, height = card.DesiredSize.Height;
        double x = petRect.Left - width - 10;
        if (x < area.Left + 4) x = petRect.Right + 10;
        if (x + width > area.Right - 4) x = area.Right - width - 4;
        double y = petRect.Top + petRect.Height * .5 - height * .5;
        y = Math.Max(area.Top + 4, Math.Min(y, area.Bottom - height - 4));
        Left = x; Top = y;
    }

    protected override void OnClosed(EventArgs args)
    {
        base.OnClosed(args);
        saveDelay.Stop();
        settings.Save();
    }

    protected override void OnClosing(CancelEventArgs args)
    {
        closing = true;
        base.OnClosing(args);
    }

    private void Refresh() { card.Child = BuildContent(); }

    private FrameworkElement BuildContent()
    {
        StackPanel root = new StackPanel();

        Grid head = new Grid();
        head.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        head.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        TextBlock title = new TextBlock { Text = "键盘喵设置", FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = Paint("#F2EFF7") };
        Grid.SetColumn(title, 0);
        Button close = Ghost("✕", delegate { Close(); });
        Grid.SetColumn(close, 1);
        head.Children.Add(title);
        head.Children.Add(close);
        root.Children.Add(head);

        root.Children.Add(Section("小猫样式"));
        root.Children.Add(CatCombo());

        scaleValue = new TextBlock { FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = Paint("#CDF159") };
        root.Children.Add(Section("小猫大小", scaleValue));
        scaleSlider = new Slider
        {
            Minimum = .5, Maximum = 1.5,
            SmallChange = .05, LargeChange = .1,
            IsMoveToPointEnabled = true,
            Focusable = false,
            Template = sliderTemplate,
            Cursor = Cursors.Hand,
            Margin = new Thickness(0, 2, 0, 0)
        };
        scaleSlider.Value = settings.Scale;
        scaleValue.Text = Math.Round(settings.Scale * 100).ToString("0") + "%";
        scaleSlider.ValueChanged += SliderChanged;
        root.Children.Add(scaleSlider);
        Grid hints = new Grid();
        hints.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        hints.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        TextBlock min = new TextBlock { Text = "50%", FontSize = 10, Foreground = Paint("#6E6979") };
        TextBlock max = new TextBlock { Text = "150%", FontSize = 10, Foreground = Paint("#6E6979"), TextAlignment = TextAlignment.Right };
        Grid.SetColumn(min, 0);
        Grid.SetColumn(max, 1);
        hints.Children.Add(min);
        hints.Children.Add(max);
        root.Children.Add(hints);

        root.Children.Add(Section("键盘样式"));
        StackPanel keyboards = Row();
        keyboards.Children.Add(TextPill("经典", settings.Keyboard == KeyboardStyle.Classic, delegate { pet.SetKeyboardStyle(KeyboardStyle.Classic); Refresh(); }));
        keyboards.Children.Add(TextPill("圆润", settings.Keyboard == KeyboardStyle.Round, delegate { pet.SetKeyboardStyle(KeyboardStyle.Round); Refresh(); }));
        keyboards.Children.Add(TextPill("霓虹", settings.Keyboard == KeyboardStyle.Neon, delegate { pet.SetKeyboardStyle(KeyboardStyle.Neon); Refresh(); }));
        keyboards.Children.Add(TextPill("木质", settings.Keyboard == KeyboardStyle.Wood, delegate { pet.SetKeyboardStyle(KeyboardStyle.Wood); Refresh(); }));
        root.Children.Add(keyboards);

        root.Children.Add(Section("鼠标样式"));
        StackPanel mice = Row();
        mice.Children.Add(TextPill("经典", settings.Mouse == MouseStyle.Classic, delegate { pet.SetMouseStyle(MouseStyle.Classic); Refresh(); }));
        mice.Children.Add(TextPill("圆润", settings.Mouse == MouseStyle.Pebble, delegate { pet.SetMouseStyle(MouseStyle.Pebble); Refresh(); }));
        mice.Children.Add(TextPill("电竞", settings.Mouse == MouseStyle.Gaming, delegate { pet.SetMouseStyle(MouseStyle.Gaming); Refresh(); }));
        mice.Children.Add(TextPill("轨迹球", settings.Mouse == MouseStyle.Trackball, delegate { pet.SetMouseStyle(MouseStyle.Trackball); Refresh(); }));
        root.Children.Add(mice);

        root.Children.Add(Section("光标样式", MiniToggle(settings.CursorEnabled,
            delegate { pet.SetCursorEnabled(!settings.CursorEnabled); Refresh(); })));
        StackPanel cursors = Row();
        cursors.Children.Add(GatedPill("小鸟", settings.Cursor == CursorStyle.Bird, settings.CursorEnabled, delegate { pet.SetCursorStyle(CursorStyle.Bird); Refresh(); }));
        cursors.Children.Add(GatedPill("逗猫棒", settings.Cursor == CursorStyle.Teaser, settings.CursorEnabled, delegate { pet.SetCursorStyle(CursorStyle.Teaser); Refresh(); }));
        root.Children.Add(cursors);
        root.Children.Add(new TextBlock { Text = "开启后系统鼠标指针替换为对应造型", FontSize = 10, Foreground = Paint("#6E6979"), Margin = new Thickness(0, 4, 0, 0) });

        root.Children.Add(Section("开关"));
        root.Children.Add(ToggleRow("显示浮动键帽", settings.Labels, delegate { pet.ToggleLabels(); }));
        root.Children.Add(ToggleRow("保持置顶", settings.OnTop, delegate { pet.ToggleTopmost(); }));
        root.Children.Add(ToggleRow("鼠标穿透", settings.ClickThrough, delegate { pet.ToggleClickThrough(); }));

        Border divider = new Border { Height = 1, Background = Paint("#35313D"), Margin = new Thickness(0, 14, 0, 10) };
        root.Children.Add(divider);

        UniformGrid actions = new UniformGrid { Columns = 3 };
        Button dock = Ghost("回到右下角", delegate { pet.DockToCornerAndSave(); });
        hideButton = Ghost(pet.IsVisible ? "隐藏小猫" : "显示小猫", delegate
        {
            pet.ToggleVisible();
            hideButton.Content = pet.IsVisible ? "隐藏小猫" : "显示小猫";
        });
        Button exit = Ghost("退出", delegate { pet.Close(); }, "#FF8A7E");
        dock.Margin = hideButton.Margin = new Thickness(0, 0, 8, 0);
        actions.Children.Add(dock);
        actions.Children.Add(hideButton);
        actions.Children.Add(exit);
        root.Children.Add(actions);
        return root;
    }

    private void SliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        scaleValue.Text = Math.Round(e.NewValue * 100).ToString("0") + "%";
        pet.ApplyScale(e.NewValue);
        saveDelay.Stop();
        saveDelay.Start();
    }

    private sealed class CatChoice
    {
        public CatStyle Style { get; set; }
        public string Name { get; set; }
        public ImageSource Portrait { get; set; }
    }

    private ComboBox CatCombo()
    {
        CatChoice[] choices = {
            new CatChoice { Style = CatStyle.Black, Name = "黑猫", Portrait = PortraitImage(CatStyle.Black) },
            new CatChoice { Style = CatStyle.Tuxedo, Name = "奶牛猫", Portrait = PortraitImage(CatStyle.Tuxedo) },
            new CatChoice { Style = CatStyle.Siamese, Name = "暹罗猫", Portrait = PortraitImage(CatStyle.Siamese) },
            new CatChoice { Style = CatStyle.Golden, Name = "金渐层", Portrait = PortraitImage(CatStyle.Golden) }
        };
        ComboBox combo = new ComboBox
        {
            Template = comboTemplate,
            ItemContainerStyle = comboItemStyle,
            ItemTemplate = comboItemTemplate,
            ItemsSource = choices,
            Cursor = Cursors.Hand,
            Focusable = false,
            Margin = new Thickness(0, 2, 0, 0)
        };
        combo.SelectedItem = choices[(int)settings.Cat];
        combo.SelectionChanged += delegate(object sender, SelectionChangedEventArgs e)
        {
            CatChoice choice = ((ComboBox)sender).SelectedItem as CatChoice;
            if (choice == null || choice.Style == settings.Cat) return;
            pet.SetCatStyle(choice.Style);
            Refresh();
        };
        return combo;
    }

    private Button TextPill(string label, bool selected, Action click)
    {
        Button pill = new Button
        {
            Content = label,
            FontSize = 12,
            Template = selected ? pillActive : pillIdle,
            Cursor = Cursors.Hand,
            Focusable = false,
            Foreground = Paint(selected ? "#23211A" : "#C9C4D4")
        };
        pill.Click += delegate { click(); Refresh(); };
        pill.Margin = new Thickness(0, 0, 8, 0);
        return pill;
    }

    private Button Ghost(string label, Action click) { return Ghost(label, click, "#D9D5E2"); }
    private Button Ghost(string label, Action click, string color)
    {
        Button button = new Button { Content = label, FontSize = 12, Template = ghost, Cursor = Cursors.Hand, Focusable = false, Foreground = Paint(color) };
        button.Click += delegate { click(); };
        return button;
    }

    private Button GatedPill(string label, bool selected, bool enabled, Action click)
    {
        Button pill = TextPill(label, selected && enabled, click);
        pill.IsEnabled = enabled;
        if (!enabled) pill.Foreground = Paint("#5C5766");
        return pill;
    }

    private CheckBox MiniToggle(bool on, Action change)
    {
        CheckBox box = new CheckBox { IsChecked = on, Template = toggleTemplate, Cursor = Cursors.Hand, VerticalAlignment = VerticalAlignment.Center, Focusable = false };
        box.Checked += delegate { change(); };
        box.Unchecked += delegate { change(); };
        return box;
    }

    private FrameworkElement ToggleRow(string label, bool on, Action change)
    {
        CheckBox box = MiniToggle(on, change);
        Grid row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        TextBlock text = new TextBlock { Text = label, FontSize = 12, Foreground = Paint("#DDD9E6"), VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(text, 0);
        Grid.SetColumn(box, 1);
        row.Children.Add(text);
        row.Children.Add(box);
        row.Margin = new Thickness(0, 5, 0, 5);
        return row;
    }

    private FrameworkElement Section(string label) { return Section(label, null); }
    private FrameworkElement Section(string label, FrameworkElement right)
    {
        Grid g = new Grid();
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        TextBlock text = new TextBlock { Text = label, FontSize = 11, Foreground = Paint("#8F8A9C") };
        Grid.SetColumn(text, 0);
        g.Children.Add(text);
        if (right != null) { Grid.SetColumn(right, 1); g.Children.Add(right); }
        g.Margin = new Thickness(0, 14, 0, 6);
        return g;
    }

    private static StackPanel Row() { return new StackPanel { Orientation = Orientation.Horizontal }; }

    private static ImageSource PortraitImage(CatStyle style)
    {
        DrawingGroup drawing = new DrawingGroup();
        using (DrawingContext dc = drawing.Open()) CatArt.DrawPortrait(dc, style);
        drawing.Freeze();
        DrawingImage image = new DrawingImage(drawing);
        image.Freeze();
        return image;
    }

    private static SolidColorBrush Paint(string hex)
    {
        SolidColorBrush brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return brush;
    }

    private static ControlTemplate PillTemplate(string idle, string hover, string pressed)
    {
        return (ControlTemplate)XamlReader.Parse(
            "<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' " +
            "xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='Button'>" +
            "<Border x:Name='bd' Background='" + idle + "' CornerRadius='8' Padding='10,5'>" +
            "<ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center'/></Border>" +
            "<ControlTemplate.Triggers>" +
            "<Trigger Property='IsMouseOver' Value='True'><Setter TargetName='bd' Property='Background' Value='" + hover + "'/></Trigger>" +
            "<Trigger Property='IsPressed' Value='True'><Setter TargetName='bd' Property='Background' Value='" + pressed + "'/></Trigger>" +
            "</ControlTemplate.Triggers></ControlTemplate>");
    }

    private static ControlTemplate BuildSliderTemplate()
    {
        return (ControlTemplate)XamlReader.Parse(
            "<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' " +
            "xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='Slider'>" +
            "<Grid Background='Transparent'>" +
            "<Border Height='4' CornerRadius='2' Background='#3A3641' VerticalAlignment='Center'/>" +
            "<Track x:Name='PART_Track' VerticalAlignment='Center'>" +
            "<Track.DecreaseRepeatButton><RepeatButton Command='Slider.DecreaseLarge' Focusable='False' Cursor='Hand'>" +
            "<RepeatButton.Template><ControlTemplate TargetType='RepeatButton'>" +
            "<Border Height='4' CornerRadius='2' Background='#CDF159' VerticalAlignment='Center'/></ControlTemplate>" +
            "</RepeatButton.Template></RepeatButton></Track.DecreaseRepeatButton>" +
            "<Track.IncreaseRepeatButton><RepeatButton Command='Slider.IncreaseLarge' Focusable='False' Cursor='Hand'>" +
            "<RepeatButton.Template><ControlTemplate TargetType='RepeatButton'>" +
            "<Border Height='4' CornerRadius='2' Background='#3A3641' VerticalAlignment='Center'/></ControlTemplate>" +
            "</RepeatButton.Template></RepeatButton></Track.IncreaseRepeatButton>" +
            "<Track.Thumb><Thumb Focusable='False' Width='14' Height='14' Cursor='Hand'>" +
            "<Thumb.Template><ControlTemplate TargetType='Thumb'>" +
            "<Ellipse Fill='#F4F1FA' Stroke='#CDF159' StrokeThickness='2'/></ControlTemplate>" +
            "</Thumb.Template></Thumb></Track.Thumb>" +
            "</Track></Grid></ControlTemplate>");
    }

    private static ControlTemplate BuildToggleTemplate()
    {
        return (ControlTemplate)XamlReader.Parse(
            "<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' " +
            "xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='CheckBox'>" +
            "<Grid Background='Transparent'>" +
            "<Border x:Name='track' Width='34' Height='18' CornerRadius='9' Background='#3A3641'/>" +
            "<Ellipse x:Name='dot' Width='12' Height='12' Fill='#B9B3C4' HorizontalAlignment='Left' Margin='3,0,0,0' VerticalAlignment='Center'/>" +
            "</Grid>" +
            "<ControlTemplate.Triggers>" +
            "<Trigger Property='IsChecked' Value='True'>" +
            "<Setter TargetName='track' Property='Background' Value='#CDF159'/>" +
            "<Setter TargetName='dot' Property='Fill' Value='#23211A'/>" +
            "<Setter TargetName='dot' Property='HorizontalAlignment' Value='Right'/>" +
            "<Setter TargetName='dot' Property='Margin' Value='0,0,3,0'/>" +
            "</Trigger>" +
            "<Trigger Property='IsMouseOver' Value='True'><Setter TargetName='dot' Property='Fill' Value='#E8E4F2'/></Trigger>" +
            "</ControlTemplate.Triggers></ControlTemplate>");
    }

    private DataTemplate BuildComboItemTemplate()
    {
        return (DataTemplate)XamlReader.Parse(
            "<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' " +
            "xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>" +
            "<StackPanel Orientation='Horizontal'>" +
            "<Image Source='{Binding Portrait}' Width='22' Height='22' Stretch='Uniform' VerticalAlignment='Center'/>" +
            "<TextBlock Text='{Binding Name}' FontSize='12' Foreground='#C9C4D4' " +
            "VerticalAlignment='Center' Margin='9,0,0,0'/>" +
            "</StackPanel></DataTemplate>");
    }

    private static ControlTemplate BuildComboTemplate()
    {
        return (ControlTemplate)XamlReader.Parse(
            "<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' " +
            "xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='ComboBox'>" +
            "<Grid Background='Transparent'>" +
            "<ToggleButton Focusable='False' ClickMode='Press' " +
            "IsChecked='{Binding IsDropDownOpen, RelativeSource={RelativeSource TemplatedParent}}'>" +
            "<ToggleButton.Template><ControlTemplate TargetType='ToggleButton'>" +
            "<Border x:Name='bd' Background='#2C2933' CornerRadius='8' Padding='10,6'>" +
            "<TextBlock Text='\u25BE' FontSize='11' Foreground='#8F8A9C' HorizontalAlignment='Right' VerticalAlignment='Center'/>" +
            "</Border>" +
            "<ControlTemplate.Triggers>" +
            "<Trigger Property='IsMouseOver' Value='True'><Setter TargetName='bd' Property='Background' Value='#373340'/></Trigger>" +
            "<Trigger Property='IsChecked' Value='True'><Setter TargetName='bd' Property='Background' Value='#423E4A'/></Trigger>" +
            "</ControlTemplate.Triggers></ControlTemplate></ToggleButton.Template>" +
            "</ToggleButton>" +
            "<ContentPresenter IsHitTestVisible='False' Margin='10,6' HorizontalAlignment='Left' VerticalAlignment='Center' " +
            "Content='{TemplateBinding SelectionBoxItem}' " +
            "ContentTemplate='{TemplateBinding SelectionBoxItemTemplate}'/>" +
            "<Popup IsOpen='{Binding IsDropDownOpen, RelativeSource={RelativeSource TemplatedParent}}' Placement='Bottom' AllowsTransparency='True' " +
            "PopupAnimation='Fade' Focusable='False'>" +
            "<Border Background='#2A2730' BorderBrush='#3A3641' BorderThickness='1' CornerRadius='8' " +
            "Margin='0,6,0,0' MinWidth='{Binding ActualWidth, RelativeSource={RelativeSource TemplatedParent}}'>" +
            "<ScrollViewer MaxHeight='248' VerticalScrollBarVisibility='Auto'>" +
            "<ItemsPresenter Margin='4'/></ScrollViewer></Border></Popup>" +
            "</Grid></ControlTemplate>");
    }

    private static Style BuildComboItemStyle()
    {
        return (Style)XamlReader.Parse(
            "<Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' " +
            "xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='ComboBoxItem'>" +
            "<Setter Property='Cursor' Value='Hand'/>" +
            "<Setter Property='Template'>" +
            "<Setter.Value><ControlTemplate TargetType='ComboBoxItem'>" +
            "<Border x:Name='bd' Background='Transparent' CornerRadius='6' Margin='0,1' Padding='8,6'>" +
            "<ContentPresenter/></Border>" +
            "<ControlTemplate.Triggers>" +
            "<Trigger Property='IsHighlighted' Value='True'><Setter TargetName='bd' Property='Background' Value='#373340'/></Trigger>" +
            "<Trigger Property='IsSelected' Value='True'><Setter TargetName='bd' Property='Background' Value='#3A3641'/></Trigger>" +
            "</ControlTemplate.Triggers></ControlTemplate></Setter.Value>" +
            "</Setter></Style>");
    }
}
