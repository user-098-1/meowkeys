using System;
using System.IO;
using System.Xml.Linq;

internal sealed class Preferences
{
    public double X = double.NaN, Y = double.NaN, Scale = 1.0;
    public bool Labels = true, ClickThrough = false, OnTop = true, CursorEnabled = false;
    public CatStyle Cat = CatStyle.Black;
    public KeyboardStyle Keyboard = KeyboardStyle.Classic;
    public MouseStyle Mouse = MouseStyle.Classic;
    public CursorStyle Cursor = CursorStyle.Bird;
    private static string FileName
    {
        get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                  "MeowkeysDesktop", "preferences.xml"); }
    }
    public static Preferences Load()
    {
        Preferences p = new Preferences();
        try
        {
            if (File.Exists(FileName))
            {
                XElement root = XElement.Load(FileName);
                p.X = (double?)root.Attribute("x") ?? double.NaN;
                p.Y = (double?)root.Attribute("y") ?? double.NaN;
                p.Scale = (double?)root.Attribute("scale") ?? 1.0;
                if (!Finite(p.Scale) || p.Scale < .50 || p.Scale > 1.50) p.Scale = 1.0;
                p.Labels = (bool?)root.Attribute("labels") ?? true;
                p.ClickThrough = (bool?)root.Attribute("clickThrough") ?? false;
                p.OnTop = (bool?)root.Attribute("onTop") ?? true;
                p.CursorEnabled = (bool?)root.Attribute("cursorEnabled") ?? false;
                p.Cursor = ReadStyle(root, "cursorStyle", p.Cursor);
                p.Cat = ReadStyle(root, "catStyle", p.Cat);
                p.Keyboard = ReadStyle(root, "keyboard", p.Keyboard);
                p.Mouse = ReadStyle(root, "mouse", p.Mouse);
            }
        }
        catch (Exception) { return new Preferences(); }
        return p;
    }
    public static bool Finite(double number) { return !double.IsInfinity(number) && !double.IsNaN(number); }
    private static T ReadStyle<T>(XElement settings, string attribute, T fallback) where T : struct
    {
        T value;
        return Enum.TryParse((string)settings.Attribute(attribute), out value) && Enum.IsDefined(typeof(T), value)
            ? value : fallback;
    }
    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FileName));
            var settings = new XElement("cat", new XAttribute("x", X), new XAttribute("y", Y),
                new XAttribute("scale", Scale), new XAttribute("labels", Labels),
                new XAttribute("clickThrough", ClickThrough), new XAttribute("onTop", OnTop),
                new XAttribute("cursorEnabled", CursorEnabled), new XAttribute("cursorStyle", Cursor),
                new XAttribute("catStyle", Cat),
                new XAttribute("keyboard", Keyboard), new XAttribute("mouse", Mouse));
            string tmp = FileName + ".tmp";
            settings.Save(tmp);
            if (File.Exists(FileName)) File.Replace(tmp, FileName, null);
            else File.Move(tmp, FileName);
        }
        catch (UnauthorizedAccessException) { }
        catch (IOException) { }
    }
}
