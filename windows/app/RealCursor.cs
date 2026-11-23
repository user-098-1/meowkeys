using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

// Installs the embedded cursor sprites as the real system cursor through
// HKCU\Control Panel\Cursors (Arrow + IBeam). The displayed size is governed by
// the separate CursorBaseSize factor, which only refreshes through the
// undocumented SPI 0x2029 — a plain SPI_SETCURSORS reload leaves it untouched,
// so the 256px sprites would otherwise be squashed to the default 32px.
// The user's own values are backed up locally and restored when the pet quits
// or the effect is switched off; restoring at startup also heals residue from
// a crashed run.
internal static class RealCursor
{
    private const int Canvas = 256;  // the .cur format tops out at 256 (entry bytes encode 256 as 0)
    private const int DisplaySize = 128;  // shown size via the base-size factor; the sprite scales down from Canvas
    private const int SpiSetCursors = 0x57;
    private const int SpiSetCursorSize = 0x2029;  // undocumented: pvParam is the pixel size; the documented reload never refreshes it
    private const int SpifUpdateIni = 0x1;
    private const int SpifSendChange = 0x2;
    // Pointing pixel of each sprite in 256-point canvas space, matching cursor.json.
    private static readonly Point[] Anchors = { new Point(57, 118), new Point(60, 79) };

    [DllImport("user32.dll")]
    private static extern bool SystemParametersInfo(int action, int param, IntPtr value, int update);

    private static string Store
    {
        get
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                "MeowkeysDesktop", "cursors");
        }
    }
    private static string BackupFile
    {
        get { return Path.Combine(Path.GetDirectoryName(Store), "cursor-backup.txt"); }
    }

    private static readonly object SwapLock = new object();

    // Swaps the sprite in; passing null puts the user's own cursors back. The
    // registry writes plus the two system-wide reload calls each stall the UI
    // for a visible beat, so they run on a worker; quit-time Reset stays
    // synchronous so the restore cannot be cut off by process exit.
    internal static void Apply(CursorStyle? style)
    {
        if (style.HasValue)
        {
            string file = WriteCursor(style.Value);
            if (file == null) return;
            RunCommit(delegate { SwapIn(file); });
        }
        else RunCommit(SwapBack);
    }

    // Restores the user's cursors when present; safe to call unconditionally.
    internal static void Reset() { try { SwapBack(); } catch (Exception) { } }

    private static void RunCommit(Action swap)
    {
        ThreadPool.QueueUserWorkItem(delegate
        {
            lock (SwapLock)
            {
                try { swap(); }
                catch (Exception) { }
            }
        });
    }

    private static void SwapIn(string file)
    {
        RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Cursors", true);
        if (key == null) return;
        if (!File.Exists(BackupFile))
            File.WriteAllText(BackupFile, key.GetValue("Arrow", "") + "\n" + key.GetValue("IBeam", "") + "\n" +
                              Convert.ToString(key.GetValue("CursorBaseSize", "")));
        object previous = key.GetValue("CursorBaseSize");
        key.SetValue("Arrow", file, RegistryValueKind.ExpandString);
        key.SetValue("IBeam", file, RegistryValueKind.ExpandString);
        key.SetValue("CursorBaseSize", DisplaySize, RegistryValueKind.DWord);
        Reload(key, DisplaySize, !(previous is int) || (int)previous != DisplaySize);
    }

    private static void SwapBack()
    {
        if (!File.Exists(BackupFile)) return;
        RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Cursors", true);
        int baseSize = 32;
        bool sizeChanged = true;
        if (key != null)
        {
            object previous = key.GetValue("CursorBaseSize");
            string[] saved = File.ReadAllLines(BackupFile);
            key.SetValue("Arrow", saved.Length > 0 ? saved[0] : "", RegistryValueKind.ExpandString);
            key.SetValue("IBeam", saved.Length > 1 ? saved[1] : "", RegistryValueKind.ExpandString);
            if (saved.Length > 2 && saved[2] != "" && int.TryParse(saved[2], out baseSize))
                key.SetValue("CursorBaseSize", baseSize, RegistryValueKind.DWord);
            else
                key.DeleteValue("CursorBaseSize", false);
            sizeChanged = !(previous is int) || (int)previous != baseSize;
            key.Close();
        }
        File.Delete(BackupFile);
        Reload(key, baseSize, sizeChanged);
    }

    private static void Reload(RegistryKey key, int baseSize, bool sizeChanged)
    {
        if (key != null) key.Close();
        SystemParametersInfo(SpiSetCursors, 0, IntPtr.Zero, SpifUpdateIni | SpifSendChange);
        if (sizeChanged)
            SystemParametersInfo(SpiSetCursorSize, 0, (IntPtr)baseSize, SpifUpdateIni | SpifSendChange);
    }

    private static string WriteCursor(CursorStyle style)
    {
        try
        {
            Directory.CreateDirectory(Store);
            byte[] file = CursorFile(style);
            if (file == null) return null;
            string path = Path.Combine(Store, "cursor-" + (int)style + ".cur");
            File.WriteAllBytes(path, file);
            return path;
        }
        catch (IOException) { return null; }
    }

    // A .cur is an icon file whose entry stores the hotspot in the plane/bitcount
    // fields and whose bitmap is a bottom-up BGRA image plus an AND mask.
    internal static byte[] CursorFile(CursorStyle style)
    {
        BitmapSource sprite = Sprite(style);
        if (sprite == null) return null;
        DrawingVisual visual = new DrawingVisual();
        using (DrawingContext dc = visual.RenderOpen())
            dc.DrawImage(sprite, new Rect(0, 0, Canvas, Canvas));
        RenderTargetBitmap bitmap = new RenderTargetBitmap(Canvas, Canvas, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        Point anchor = Anchors[(int)style];
        byte[] pixels = new byte[Canvas * Canvas * 4];
        bitmap.CopyPixels(pixels, Canvas * 4, 0);

        using (MemoryStream stream = new MemoryStream())
        {
            BinaryWriter file = new BinaryWriter(stream);
            file.Write((short)0); file.Write((short)2); file.Write((short)1);
            // Entry width/height are bytes where 0 means 256 — the format's maximum.
            file.Write((byte)0); file.Write((byte)0); file.Write((byte)0); file.Write((byte)0);
            file.Write((short)anchor.X); file.Write((short)anchor.Y);
            file.Write(40 + Canvas * Canvas * 4 + Canvas * Canvas / 8);
            file.Write(22);
            file.Write(40); file.Write(Canvas); file.Write(Canvas * 2);
            file.Write((short)1); file.Write((short)32);
            file.Write(0); file.Write(Canvas * Canvas * 4); file.Write(0); file.Write(0); file.Write(0); file.Write(0);
            for (int row = Canvas - 1; row >= 0; row--)
            {
                for (int col = 0; col < Canvas; col++)
                {
                    int at = (row * Canvas + col) * 4;
                    int alpha = pixels[at + 3];
                    if (alpha > 0 && alpha < 255)
                    {
                        // The render is premultiplied; the .cur format expects straight alpha.
                        file.Write((byte)(pixels[at + 2] * 255 / alpha));
                        file.Write((byte)(pixels[at + 1] * 255 / alpha));
                        file.Write((byte)(pixels[at] * 255 / alpha));
                    }
                    else
                    {
                        file.Write(pixels[at + 2]); file.Write(pixels[at + 1]); file.Write(pixels[at]);
                    }
                    file.Write((byte)alpha);
                }
            }
            for (int i = 0; i < Canvas * Canvas / 8; i++) file.Write((byte)0);
            return stream.ToArray();
        }
    }

    private static BitmapSource Sprite(CursorStyle style)
    {
        string name = style == CursorStyle.Teaser ? "cursor-teaser.png" : "cursor-bird.png";
        using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
        {
            if (stream == null) return null;
            BitmapFrame frame = BitmapFrame.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            frame.Freeze();
            return frame;
        }
    }
}
