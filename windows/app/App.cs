using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Drawing = System.Drawing;
using Forms = System.Windows.Forms;

[assembly: AssemblyTitle("键盘喵")]
[assembly: AssemblyDescription("会跟着键盘和鼠标一起动的小猫桌宠")]
[assembly: AssemblyVersion("1.5.0.0")]
internal static class Program
{
    [STAThread]
    public static void Main()
    {
        bool first;
        using (var mutex = new Mutex(true, @"Local\MeowkeysDesktop.B48D8B", out first))
        using (var show = new EventWaitHandle(false, EventResetMode.AutoReset, @"Local\MeowkeysDesktop.B48D8B.Show"))
        {
            if (!first) { show.Set(); return; }
            var app = new Application();
            app.ShutdownMode = ShutdownMode.OnMainWindowClose;
            var pet = new PetWindow();
            app.MainWindow = pet;
            app.DispatcherUnhandledException += delegate(object sender, DispatcherUnhandledExceptionEventArgs e)
            {
                e.Handled = true;
                pet.ReportError(e.Exception);
            };
            RegisteredWaitHandle wake = ThreadPool.RegisterWaitForSingleObject(show,
                delegate { pet.Dispatcher.BeginInvoke(new Action(pet.Reveal)); }, null, Timeout.Infinite, false);
            try { app.Run(pet); }
            finally { wake.Unregister(null); mutex.ReleaseMutex(); }
        }
    }
}

internal sealed class PetWindow : Window
{
    private readonly Preferences settings = Preferences.Load();
    private readonly CatScene cat = new CatScene();
    private readonly Canvas stage = new Canvas();
    private readonly DispatcherTimer timer = new DispatcherTimer(DispatcherPriority.Render);
    private readonly Stopwatch clock = Stopwatch.StartNew();
    private readonly Forms.NotifyIcon tray = new Forms.NotifyIcon();
    private readonly Drawing.Icon icon;
    private IntPtr hwnd, inputBuffer;
    private Drawing.Rectangle cursorMonitor;
    private bool listening;
    private SettingsPanel panel;
    private Point? dragStart;
    private Point dragOrigin;
    private bool dragging;
    private HwndSource source;

    public PetWindow()
    {
        Title = "键盘喵";
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        Topmost = settings.OnTop;
        ShowActivated = false;
        ShowInTaskbar = false;
        Focusable = false;
        Content = stage;
        stage.ClipToBounds = true;
        stage.Children.Add(cat);
        cat.LabelsEnabled = settings.Labels;
        cat.Cat = settings.Cat;
        cat.Keyboard = settings.Keyboard;
        cat.Mouse = settings.Mouse;
        ApplySize();
        icon = Drawing.Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
        tray.Icon = icon;
        tray.Text = "键盘喵 · 双击显示 · 右键设置";
        tray.DoubleClick += delegate { Reveal(); };
        tray.MouseUp += delegate(object sender, Forms.MouseEventArgs e)
        {
            if (e.Button == Forms.MouseButtons.Right) Dispatcher.BeginInvoke(new Action(TogglePanel));
        };
        MouseLeftButtonDown += BeginDrag;
        MouseMove += ContinueDrag;
        MouseLeftButtonUp += delegate { EndDrag(); };
        LostMouseCapture += delegate { if (dragStart.HasValue) EndDrag(); };
        MouseRightButtonUp += delegate(object sender, MouseButtonEventArgs e)
        {
            TogglePanel(); e.Handled = true;
        };
        Cursor = Cursors.Hand;
        timer.Interval = TimeSpan.FromMilliseconds(1000.0 / 40);
        timer.Tick += delegate { Tick(); };
        Closed += delegate { CleanUp(); };
    }

    protected override void OnSourceInitialized(EventArgs args)
    {
        base.OnSourceInitialized(args);
        hwnd = new WindowInteropHelper(this).Handle;
        source = HwndSource.FromHwnd(hwnd);
        source.AddHook(Messages);
        ApplyStyle();
        inputBuffer = Marshal.AllocHGlobal(128);
        tray.Visible = true;
        try { WindowsInput.Listen(hwnd, true); listening = true; }
        catch (Win32Exception e)
        {
            tray.ShowBalloonTip(4500, Title, "键鼠输入监听未能启动。请退出后重开。\n" + e.Message, Forms.ToolTipIcon.Warning);
        }
        RealCursor.Reset();
        if (settings.CursorEnabled) RealCursor.Apply(settings.Cursor);
        if (Preferences.Finite(settings.X) && Preferences.Finite(settings.Y))
        {
            Left = settings.X; Top = settings.Y;
            if (!InView()) DockToCorner();
        }
        else DockToCorner();
        timer.Start();
    }

    private IntPtr Messages(IntPtr window, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == 0xFF && listening && IsVisible && panel == null && inputBuffer != IntPtr.Zero)
        {
            WindowsInput.Process(lParam, inputBuffer, cat, clock.Elapsed.TotalSeconds);
        }
        if (msg == 0x21) { handled = true; return new IntPtr(3); } // MA_NOACTIVATE
        if (msg == 0x7E) Dispatcher.BeginInvoke(new Action(delegate
        {
            cursorMonitor = Drawing.Rectangle.Empty;
            if (!InView()) DockToCorner();
        }));
        return IntPtr.Zero;
    }

    private void ApplyStyle()
    {
        int style = WindowsInput.GetWindowLong(hwnd, -20) | 0x08000000 | 0x80;
        style = settings.ClickThrough ? style | 0x20 : style & ~0x20;
        WindowsInput.SetWindowLong(hwnd, -20, style);
    }
    private void ApplySize()
    {
        double s = settings.Scale;
        Width = 306 * s; Height = (284 + DeskLayout.Forward) * s;
        cat.Width = 480 * s; cat.Height = 320 * s;
        Canvas.SetLeft(cat, -75 * s); Canvas.SetTop(cat, -9 * s);
    }
    private void Tick()
    {
        WindowsInput.ScreenPoint screen;
        double now = clock.Elapsed.TotalSeconds;
        if (WindowsInput.GetCursorPos(out screen))
        {
            Point p = cat.PointFromScreen(new Point(screen.X, screen.Y));
            if (!cursorMonitor.Contains(screen.X, screen.Y))
                cursorMonitor = Forms.Screen.FromPoint(new Drawing.Point(screen.X, screen.Y)).Bounds;
            Vector onMonitor = new Vector(
                2.0 * (screen.X - cursorMonitor.Left) / Math.Max(1, cursorMonitor.Width - 1) - 1,
                2.0 * (screen.Y - cursorMonitor.Top) / Math.Max(1, cursorMonitor.Height - 1) - 1);
            bool leftDown = WindowsInput.GetAsyncKeyState(1) < 0;
            bool rightDown = WindowsInput.GetAsyncKeyState(2) < 0;
            cat.Pointer(new Point(p.X / settings.Scale, p.Y / settings.Scale), leftDown, rightDown, now, onMonitor);
        }
        cat.Advance(now);
    }

    private Point CursorInDips()
    {
        var p = Forms.Cursor.Position;
        return source.CompositionTarget.TransformFromDevice.Transform(new Point(p.X, p.Y));
    }
    private void BeginDrag(object sender, MouseButtonEventArgs e)
    {
        ClosePanel();
        if (settings.ClickThrough) return;
        dragStart = CursorInDips();
        dragOrigin = new Point(Left, Top);
        CaptureMouse(); e.Handled = true;
    }
    private void ContinueDrag(object sender, MouseEventArgs e)
    {
        if (!dragStart.HasValue || e.LeftButton != MouseButtonState.Pressed) return;
        Vector delta = CursorInDips() - dragStart.Value;
        if (!dragging && delta.Length < 4) return;
        dragging = true;
        Left = dragOrigin.X + delta.X; Top = dragOrigin.Y + delta.Y;
    }
    private void EndDrag()
    {
        dragStart = null;
        if (IsMouseCaptured) ReleaseMouseCapture();
        if (dragging) { dragging = false; if (!InView()) DockToCorner(); Save(); }
    }
    private Rect LogicalArea(Drawing.Rectangle r)
    {
        var transform = source.CompositionTarget.TransformFromDevice;
        return new Rect(transform.Transform(new Point(r.Left, r.Top)), transform.Transform(new Point(r.Right, r.Bottom)));
    }
    private bool InView()
    {
        if (!Preferences.Finite(Left) || !Preferences.Finite(Top)) return false;
        var center = new Point(Left + Width * .45, Top + Height * .48);
        foreach (Forms.Screen screen in Forms.Screen.AllScreens)
            if (LogicalArea(screen.WorkingArea).Contains(center)) return true;
        return false;
    }
    private void DockToCorner()
    {
        Rect area = LogicalArea(Forms.Screen.PrimaryScreen.WorkingArea);
        Left = area.Right - Width - 16; Top = area.Bottom - Height - 12;
    }
    public void Reveal()
    {
        if (!IsVisible) Show();
        if (!InView()) DockToCorner();
        timer.Start();
    }
    private void Save()
    {
        settings.X = Left; settings.Y = Top; settings.Save();
    }
    private void TogglePanel() { if (panel != null) ClosePanel(); else OpenPanel(); }
    private void OpenPanel()
    {
        if (panel != null) return;
        SettingsPanel open = new SettingsPanel(this, settings);
        open.Closed += delegate { if (panel == open) panel = null; };
        panel = open;
        open.Show();
    }
    private void ClosePanel() { if (panel != null) panel.Close(); }
    internal void ReportError(Exception error)
    {
        try
        {
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MeowkeysDesktop");
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "last-error.txt"), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\n" + error);
        }
        catch (Exception) { }
        try { tray.ShowBalloonTip(4000, Title, "小猫遇到一点问题但已自动恢复。\n" + error.Message, Forms.ToolTipIcon.Warning); }
        catch (Exception) { }
    }
    internal void SetCatStyle(CatStyle style) { settings.Cat = cat.Cat = style; cat.InvalidateVisual(); Save(); }
    internal void SetKeyboardStyle(KeyboardStyle style) { settings.Keyboard = cat.Keyboard = style; cat.InvalidateVisual(); Save(); }
    internal void SetMouseStyle(MouseStyle style) { settings.Mouse = cat.Mouse = style; cat.InvalidateVisual(); Save(); }
    internal void SetCursorEnabled(bool enabled)
    {
        settings.CursorEnabled = enabled;
        RealCursor.Apply(enabled ? settings.Cursor : (CursorStyle?)null);
        Save();
    }
    internal void SetCursorStyle(CursorStyle style)
    {
        settings.Cursor = style;
        if (settings.CursorEnabled) RealCursor.Apply(style);
        Save();
    }
    internal void ApplyScale(double scale)
    {
        double right = Left + Width, bottom = Top + Height;
        settings.Scale = scale; ApplySize();
        Left = right - Width; Top = bottom - Height;
        if (!InView()) DockToCorner();
    }
    internal void ToggleLabels() { settings.Labels = cat.LabelsEnabled = !settings.Labels; cat.ClearLabels(); Save(); }
    internal void ToggleTopmost() { Topmost = settings.OnTop = !settings.OnTop; Save(); }
    internal void ToggleClickThrough() { settings.ClickThrough = !settings.ClickThrough; ApplyStyle(); Save(); }
    internal void DockToCornerAndSave() { DockToCorner(); Reveal(); Save(); }
    internal void ToggleVisible()
    {
        if (IsVisible) { timer.Stop(); Hide(); cat.ClearLabels(); }
        else Reveal();
    }
    private void CleanUp()
    {
        timer.Stop(); Save();
        RealCursor.Reset();
        if (listening) { listening = false; try { WindowsInput.Listen(hwnd, false); } catch (Win32Exception) { } }
        if (source != null) source.RemoveHook(Messages);
        if (inputBuffer != IntPtr.Zero) { Marshal.FreeHGlobal(inputBuffer); inputBuffer = IntPtr.Zero; }
        tray.Visible = false; tray.Dispose(); if (icon != null) icon.Dispose();
    }
}
