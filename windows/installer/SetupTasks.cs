using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

static class SetupTasks
{
    delegate bool EnumWindow(IntPtr hwnd, IntPtr data);
    [DllImport("user32.dll")] static extern bool EnumWindows(EnumWindow callback, IntPtr data);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint pid);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] static extern int GetWindowText(IntPtr h, StringBuilder text, int capacity);
    [DllImport("user32.dll", SetLastError=true)] static extern bool PostMessage(IntPtr hwnd, uint msg, IntPtr wp, IntPtr lp);
    static bool SamePath(string a, string b)
    {
        if(String.IsNullOrEmpty(a)||String.IsNullOrEmpty(b))return false;
        return String.Equals(Path.GetFullPath(a),Path.GetFullPath(b),StringComparison.OrdinalIgnoreCase);
    }
    static int CloseInstalled(string target)
    {
        foreach(var process in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(target)))
        using(process)
        {
            try
            {
                if(process.HasExited || !SamePath(process.MainModule.FileName,target))continue;
            }
            catch(System.ComponentModel.Win32Exception){continue;}
            catch(InvalidOperationException){continue;}
            // Include a hidden tray pet and select its own top-level WPF window explicitly.
            IntPtr pet=IntPtr.Zero;
            EnumWindows(delegate(IntPtr h,IntPtr p){
                uint id;GetWindowThreadProcessId(h,out id);
                if(id!=(uint)process.Id)return true;
                var title=new StringBuilder(128);GetWindowText(h,title,title.Capacity);
                if(title.ToString()!="键盘喵")return true;
                pet=h;return false;
            },IntPtr.Zero);
            if(process.HasExited)continue;
            if(pet==IntPtr.Zero || !PostMessage(pet,0x10,IntPtr.Zero,IntPtr.Zero))return 2;
            if(!process.WaitForExit(6000))return 2;
        }
        return 0;
    }
    static int Link(string operation,string target,string file)
    {
        bool exists=File.Exists(file);
        if(operation=="remove-link"&&!exists)return 0;
        dynamic shell=Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell",true));
        dynamic link=shell.CreateShortcut(file);
        try
        {
            // Existing shortcuts targeting a portable copy belong to that copy.
            if(exists&&!SamePath((string)link.TargetPath,target))return 0;
            if(operation=="remove-link") { File.Delete(file);return 0; }
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            link.TargetPath=target;
            link.WorkingDirectory=Path.GetDirectoryName(target);
            link.Description="启动键盘喵";
            link.IconLocation=target+",0";
            link.Save();
        }
        finally {Marshal.FinalReleaseComObject(link);Marshal.FinalReleaseComObject(shell);}
        return 0;
    }
    static int FirstRun(string preferences)
    {
        if(File.Exists(preferences))return 0;
        string dir=Path.GetDirectoryName(preferences);
        Directory.CreateDirectory(dir);
        string staging=Path.Combine(dir,Guid.NewGuid().ToString("N")+".tmp");
        try
        {
            File.WriteAllText(staging,"<cat keyboard=\"Round\" mouse=\"Trackball\" />",new UTF8Encoding(false));
            try { File.Move(staging,preferences); }
            catch(IOException){if(!File.Exists(preferences))throw;}
        }
        finally {if(File.Exists(staging))File.Delete(staging);}
        return 0;
    }
    [STAThread] static int Main(string[] args)
    {
        try
        {
            if(args.Length==2 && args[0]=="first-run")return FirstRun(args[1]);
            if(args.Length==2 && args[0]=="close")return CloseInstalled(args[1]);
            if(args.Length==3 && (args[0]=="create-link" || args[0]=="remove-link"))return Link(args[0],args[1],args[2]);
            return 3;
        }
        catch { return 2; }
    }
}
