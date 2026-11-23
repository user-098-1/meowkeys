using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;

internal static class WindowsInput
{
    [StructLayout(LayoutKind.Sequential)]
    private struct Device
    {
        public ushort Page, Usage;
        public uint Flags;
        public IntPtr Target;
    }
    [StructLayout(LayoutKind.Sequential)]
    internal struct ScreenPoint { public int X, Y; }
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterRawInputDevices([In] Device[] devices, uint count, uint size);
    [DllImport("user32.dll")]
    private static extern uint GetRawInputData(IntPtr message, uint command, IntPtr buffer, ref uint size, uint headerSize);
    [DllImport("user32.dll")] internal static extern bool GetCursorPos(out ScreenPoint pt);
    [DllImport("user32.dll")] internal static extern short GetAsyncKeyState(int vk);
    [DllImport("user32.dll")] internal static extern int GetWindowLong(IntPtr hwnd, int index);
    [DllImport("user32.dll")] internal static extern int SetWindowLong(IntPtr hwnd, int index, int value);

    public static void Listen(IntPtr hwnd, bool enable)
    {
        Device keyboard = new Device { Page = 1, Usage = 6,
            Flags = enable ? 0x100u : 1u, Target = enable ? hwnd : IntPtr.Zero };
        Device mouse = keyboard;
        mouse.Usage = 2;
        if (!RegisterRawInputDevices(new Device[] { keyboard, mouse }, 2, (uint)Marshal.SizeOf(typeof(Device))))
            throw new Win32Exception(Marshal.GetLastWin32Error());
    }

    public static void Process(IntPtr message, IntPtr buffer, CatScene scene, double now)
    {
        uint size = 128;
        int header = 8 + 2 * IntPtr.Size;
        uint read = GetRawInputData(message, 0x10000003, buffer, ref size, (uint)header);
        ProcessPacket(buffer, read, scene, now);
    }

    internal static void ProcessPacket(IntPtr buffer, uint read, CatScene scene, double now)
    {
        int header = 8 + 2 * IntPtr.Size;
        if (read == uint.MaxValue || read < header) return;
        int type = Marshal.ReadInt32(buffer);
        if (type == 0 && read >= header + 24)
        {
            // RAWMOUSE's aligned button union begins four bytes into its payload.
            int buttons = (ushort)Marshal.ReadInt16(buffer, header + 4);
            if ((buttons & 1) != 0) scene.MouseButton(1, true, now);
            if ((buttons & 2) != 0) scene.MouseButton(1, false, now);
            if ((buttons & 4) != 0) scene.MouseButton(2, true, now);
            if ((buttons & 8) != 0) scene.MouseButton(2, false, now);
            return;
        }
        if (type != 1 || read < header + 16) return;
        int scan = (ushort)Marshal.ReadInt16(buffer, header);
        int flags = (ushort)Marshal.ReadInt16(buffer, header + 2);
        int key = (ushort)Marshal.ReadInt16(buffer, header + 6);
        if ((flags & 1) != 0 || scan == 255 || key < 1 || key > 254) return;
        if (key == 16) key = scan == 0x36 ? 161 : 160;
        else if (key == 17) key = (flags & 2) != 0 ? 163 : 162;
        else if (key == 18) key = (flags & 2) != 0 ? 165 : 164;
        scene.Strike(key, now);
    }
}
