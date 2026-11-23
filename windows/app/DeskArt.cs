using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

// The keyboard faces the cat. Column zero is the typist's left, on the viewer's right.
public static class DeskArt
{
    private sealed class Key
    {
        public int Code;
        public string Legend;
        public Point Center;
        public double U, V, Units;
        public bool Accent;
    }

    private sealed class KeySurface
    {
        public DrawingGroup Board;
        public readonly Dictionary<int, DrawingGroup> Lights = new Dictionary<int, DrawingGroup>();
    }

    private sealed class Finish
    {
        public readonly Brush Face, Accent, Edge, Text, Light, LitText;
        public Finish(uint face, uint accent, uint edge, uint text, uint light, uint litText)
        {
            Face = Enamel(face);
            Accent = Enamel(accent);
            Edge = Paint(edge);
            Text = Paint(text);
            Light = Paint(light);
            LitText = Paint(litText);
        }
    }

    private static readonly List<Key> Keys = new List<Key>();
    private static readonly Dictionary<int, Key> KeyByCode = new Dictionary<int, Key>();
    private static readonly Dictionary<KeyboardStyle, KeySurface> Surfaces =
        new Dictionary<KeyboardStyle, KeySurface>();
    private static readonly Dictionary<string, FormattedText> CapText = new Dictionary<string, FormattedText>();
    private static readonly Typeface Face = new Typeface("Segoe UI");
    private static readonly Brush Ink = Paint(0x1A191B);
    private static readonly Brush CapFill = Paint(0xFAF9F3);
    private static readonly Brush CapShade = Paint(0xBBB7B0);
    private static readonly Pen CapOutline = Line(0x4D4945, .8);
    private const double KeyTop = 1.15;

    static DeskArt()
    {
        // Row zero sits nearest the typist.
        Row(0,
            new int[] { 0xA2, 0x5B, 0xA4, 0x20, 0xA5, 0x5C, 0x5D, 0xA3 },
            new string[] { "Ctrl", "◇", "Alt", "", "Alt", "◇", "≡", "Ctrl" },
            new double[] { 1.25, 1.25, 1.25, 6.25, 1.25, 1.25, 1.25, 1.25 });
        Row(1,
            new int[] { 0xA0, 90, 88, 67, 86, 66, 78, 77, 0xBC, 0xBE, 0xBF, 0xA1 },
            new string[] { "⇧", "Z", "X", "C", "V", "B", "N", "M", ",", ".", "/", "⇧" },
            new double[] { 2.25, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2.75 });
        Row(2,
            new int[] { 0x14, 65, 83, 68, 70, 71, 72, 74, 75, 76, 0xBA, 0xDE, 0x0D },
            new string[] { "⇪", "A", "S", "D", "F", "G", "H", "J", "K", "L", ";", "'", "↵" },
            new double[] { 1.75, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2.25 });
        Row(3,
            new int[] { 0x09, 81, 87, 69, 82, 84, 89, 85, 73, 79, 80, 0xDB, 0xDD, 0xDC },
            new string[] { "⇥", "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P", "[", "]", "\\" },
            new double[] { 1.5, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1.5 });
        Row(4,
            new int[] { 0x1B, 49, 50, 51, 52, 53, 54, 55, 56, 57, 48, 0xBD, 0xBB, 0x08 },
            new string[] { "Esc", "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "-", "=", "⌫" },
            new double[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2 });

        Surfaces[KeyboardStyle.Classic] = Build(KeyboardStyle.Classic,
            new Finish(0xF6F5E8, 0xFFB71B, 0xBFBCAD, 0x6E6B60, 0xDAF575, 0x34382D));
        Surfaces[KeyboardStyle.Round] = Build(KeyboardStyle.Round,
            new Finish(0xF8A38E, 0xFAE9CC, 0x855A52, 0x613C38, 0xFFF0A9, 0x633F32));
        Surfaces[KeyboardStyle.Neon] = Build(KeyboardStyle.Neon,
            new Finish(0x38424E, 0x284D5B, 0x121920, 0xAEF0FF, 0x75EAFF, 0x132A37));
        Surfaces[KeyboardStyle.Wood] = Build(KeyboardStyle.Wood,
            new Finish(0xF2E0BD, 0xCBA377, 0x9D7856, 0x6C4E36, 0xFFD486, 0x493627));
    }

    private static Brush Paint(uint rgb)
    {
        SolidColorBrush brush = new SolidColorBrush(Color.FromRgb((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb));
        brush.Freeze();
        return brush;
    }

    private static Pen Line(uint rgb, double thickness)
    {
        Pen pen = new Pen(Paint(rgb), thickness);
        pen.Freeze();
        return pen;
    }

    private static Color Rgb(uint value)
    {
        return Color.FromRgb((byte)(value >> 16), (byte)(value >> 8), (byte)value);
    }

    private static Brush Enamel(uint value)
    {
        Color mid = Rgb(value);
        Color bright = Color.FromRgb((byte)(mid.R + (255 - mid.R) * .24),
            (byte)(mid.G + (255 - mid.G) * .24), (byte)(mid.B + (255 - mid.B) * .24));
        LinearGradientBrush brush = new LinearGradientBrush(bright, mid, 85);
        brush.Freeze();
        return brush;
    }

    // Deck coordinates are measured in key widths and rows, from the cat's far-right
    // corner as seen by the viewer. Width and row pitch increase together in perspective.
    private static Point Project(double u, double v, double lift = 0)
    {
        double depth = (v + .25) / 5.5;
        double scale = 1 / (1 - .15 * depth);
        return new Point(241 + (7.5 - u) * 12.2 * scale, 234 + 38.25 * depth * scale - lift);
    }

    // All four edges and their corner controls are projected from the same plane.
    private static Geometry Panel(double left, double rear, double right, double front,
        double rx, double ry, double lift, bool clipped)
    {
        const double arc = .552285;
        rx = Math.Min(rx, (right - left) / 2);
        ry = Math.Min(ry, (front - rear) / 2);
        StreamGeometry path = new StreamGeometry();
        using (StreamGeometryContext g = path.Open())
        {
            g.BeginFigure(Project(left + rx, rear, lift), true, true);
            g.LineTo(Project(right - rx, rear, lift), true, false);
            if (clipped) g.LineTo(Project(right, rear + ry, lift), true, false);
            else g.BezierTo(Project(right - rx * (1-arc), rear, lift),
                Project(right, rear + ry * (1-arc), lift), Project(right, rear + ry, lift), true, false);
            g.LineTo(Project(right, front - ry, lift), true, false);
            if (clipped) g.LineTo(Project(right - rx, front, lift), true, false);
            else g.BezierTo(Project(right, front - ry * (1-arc), lift),
                Project(right - rx * (1-arc), front, lift), Project(right - rx, front, lift), true, false);
            g.LineTo(Project(left + rx, front, lift), true, false);
            if (clipped) g.LineTo(Project(left, front - ry, lift), true, false);
            else g.BezierTo(Project(left + rx * (1-arc), front, lift),
                Project(left, front - ry * (1-arc), lift), Project(left, front - ry, lift), true, false);
            g.LineTo(Project(left, rear + ry, lift), true, false);
            if (clipped) g.LineTo(Project(left + rx, rear, lift), true, false);
            else g.BezierTo(Project(left, rear + ry * (1-arc), lift),
                Project(left + rx * (1-arc), rear, lift), Project(left + rx, rear, lift), true, false);
        }
        path.Freeze();
        return path;
    }

    private static void DrawCase(DrawingContext dc, KeyboardStyle style)
    {
        bool gaming = style == KeyboardStyle.Neon;
        bool retro = style == KeyboardStyle.Round;
        uint baseColor, topColor, wellColor, bevelColor;
        switch (style)
        {
            case KeyboardStyle.Round:
                baseColor = 0x50877A; topColor = 0xA0D9C4; wellColor = 0xC4E5D2; bevelColor = 0x7CB29F;
                break;
            case KeyboardStyle.Neon:
                baseColor = 0x17232E; topColor = 0x344758; wellColor = 0x15202C; bevelColor = 0x4E6474;
                break;
            case KeyboardStyle.Wood:
                baseColor = 0x65412C; topColor = 0xA5754F; wellColor = 0x4D3D30; bevelColor = 0xBC9066;
                break;
            default:
                baseColor = 0x262629; topColor = 0x454541; wellColor = 0x77756B; bevelColor = 0x66655E;
                break;
        }
        double rx = retro ? .62 : (gaming ? .56 : .24);
        double ry = retro ? .73 : (gaming ? .5 : .34);
        Geometry shape = Panel(-.46, -.25, 15.46, 5.25, rx, ry, 0, gaming);
        // Stack the side wall continuously through the rounded and clipped corners.
        Brush side = Paint(baseColor);
        for (double drop = 6.2; drop >= .2; drop -= 1)
        {
            dc.PushTransform(new TranslateTransform(0, drop));
            dc.DrawGeometry(side, null, shape);
            dc.Pop();
        }
        dc.PushTransform(new TranslateTransform(0, 1.18));
        dc.DrawGeometry(Paint(bevelColor), null, shape);
        dc.Pop();
        dc.DrawGeometry(Enamel(topColor), null, shape);
        dc.DrawGeometry(Paint(wellColor), null, Panel(-.09, -.01, 15.09, 5.01, rx * .65, ry * .65, 0, gaming));

        if (gaming)
        {
            dc.DrawGeometry(null, Line(0x76EFF9, 1.05), Geometry.Parse(
                "M 131,279.5 L 135,281 H 184 M 213,282 H 269 M 297,281 H 347 L 351,279.5"));
            dc.DrawGeometry(null, Line(0x65C4D6, .65), Geometry.Parse("M 146,237 L 149,235.5 H 163 M 319,235.5 H 333 L 336,237"));
        }
        else if (style == KeyboardStyle.Wood)
        {
            dc.DrawGeometry(null, Line(0xAC7D55, .68), Geometry.Parse(
                "M 132,281 Q 164,279.8 179,281.1 T 221,281 T 255,281.1 " +
                "M 260,281.4 Q 288,280 303,281.2 T 351,281 " +
                "M 146,283.1 Q 175,282 196,283.1 T 225,283 M 240,283 Q 269,282.1 282,283.3 T 337,283"));
            dc.DrawGeometry(null, Line(0xD0AA83, .55), Geometry.Parse(
                "M 145,235.3 Q 166,234.6 190,235.3 T 236,235.1 M 258,235.4 Q 280,234.7 297,235.2 T 335,235.1"));
        }
        else if (retro)
        {
            dc.DrawGeometry(null, Line(0xA4D0BB, .72), Geometry.Parse("M 135,280.6 H 347"));
            dc.DrawEllipse(Paint(0x567E6C), null, Project(15.24, 2.57), .95, .65);
            dc.DrawEllipse(Paint(0x567E6C), null, Project(-.24, 2.57), .95, .65);
        }
    }

    private static Geometry KeyFace(Key key, KeyboardStyle style, bool skirt)
    {
        bool round = style == KeyboardStyle.Round;
        double inset = round ? .115 : .062;
        if (skirt) inset -= .035;
        double u0 = key.U + inset, u1 = key.U + key.Units - inset;
        double v0 = key.V + (skirt ? .065 : .12);
        double v1 = key.V + (skirt ? .96 : .85);
        double rx = round ? (key.Units > 1 ? .38 : .5) : .1;
        double ry = round ? .5 : .14;
        return Panel(u0, v0, u1, v1, rx, ry, skirt ? -.34 : KeyTop, style == KeyboardStyle.Neon);
    }

    private static KeySurface Build(KeyboardStyle style, Finish finish)
    {
        KeySurface surface = new KeySurface();
        surface.Board = new DrawingGroup();
        using (DrawingContext dc = surface.Board.Open())
        {
            DrawCase(dc, style);
            Pen glint = Line(style == KeyboardStyle.Neon ? 0x70828Bu : 0xFBF2DAu, .45);
            foreach (Key key in Keys)
            {
                Geometry cap = KeyFace(key, style, false);
                dc.DrawGeometry(finish.Edge, null, KeyFace(key, style, true));
                dc.DrawGeometry(key.Accent ? finish.Accent : finish.Face, null, cap);
                double glossInset = style == KeyboardStyle.Round ? .3 : .18;
                dc.DrawLine(glint, Project(key.U + glossInset, key.V + .18, KeyTop),
                    Project(key.U + key.Units - glossInset, key.V + .18, KeyTop));
                DrawLegend(dc, key, finish.Text);

                DrawingGroup lit = new DrawingGroup();
                using (DrawingContext light = lit.Open())
                {
                    light.DrawGeometry(finish.Light, null, cap);
                    DrawLegend(light, key, finish.LitText);
                }
                lit.Freeze();
                surface.Lights[key.Code] = lit;
            }
        }
        surface.Board.Freeze();
        return surface;
    }

    private static void Row(int row, int[] codes, string[] legends, double[] units)
    {
        double u = 0;
        for (int i = 0; i < codes.Length; ++i)
        {
            Key key = new Key();
            key.Code = codes[i];
            key.Legend = legends[i];
            key.U = u;
            key.V = row;
            key.Center = Project(u + units[i] / 2, row + .485, KeyTop);
            key.Units = units[i];
            key.Accent = codes[i] == 0x0D || codes[i] == 0x1B;
            Keys.Add(key);
            KeyByCode.Add(codes[i], key);
            u += units[i];
        }
    }

    private static void DrawLegend(DrawingContext dc, Key key, Brush color)
    {
        if (key.Legend.Length == 0) return;
        FormattedText glyph = new FormattedText(key.Legend, CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight, Face, key.Legend.Length > 1 ? 2.7 : 3.5, color, 1.0);
        dc.PushTransform(new RotateTransform(180, key.Center.X, key.Center.Y));
        dc.DrawText(glyph, new Point(key.Center.X - glyph.Width / 2, key.Center.Y - glyph.Height / 2));
        dc.Pop();
    }

    private static Key Lookup(int vk)
    {
        if (vk >= 0x60 && vk <= 0x69) vk = 48 + vk - 0x60;
        if (vk == 0x10) vk = 0xA0;
        if (vk == 0x11) vk = 0xA2;
        if (vk == 0x12) vk = 0xA4;
        Key key;
        return KeyByCode.TryGetValue(vk, out key) ? key : null;
    }

    public static Point KeyPoint(int vk)
    {
        Key key = Lookup(vk);
        Point target;
        if (key != null) target = key.Center;
        else if (vk >= 0x70 && vk <= 0x7B) target = Project(1.5 + vk - 0x70, 4.5);
        else target = Project(13.5, .8);
        return target + new Vector(0, DeskLayout.Forward);
    }

    public static void DrawKeyboard(DrawingContext dc, KeyboardStyle keyboard, IDictionary<int, double> highlights)
    {
        KeySurface surface;
        if (!Surfaces.TryGetValue(keyboard, out surface)) surface = Surfaces[KeyboardStyle.Classic];
        dc.PushTransform(new TranslateTransform(0, DeskLayout.Forward));
        dc.DrawDrawing(surface.Board);
        if (highlights != null)
        {
            foreach (KeyValuePair<int, double> press in highlights)
            {
                Key key = Lookup(press.Key);
                if (key == null || press.Value <= 0) continue;
                dc.PushOpacity(Math.Min(1, press.Value));
                dc.DrawDrawing(surface.Lights[key.Code]);
                dc.Pop();
            }
        }
        dc.Pop();
    }

    public static void DrawDesk(DrawingContext dc, IDictionary<int, double> highlights, double leftGlow, double rightGlow,
        KeyboardStyle keyboard, MouseStyle mouse, Vector mouseOffset = default(Vector))
    {
        DrawKeyboard(dc, keyboard, highlights);
        dc.PushTransform(new TranslateTransform(mouseOffset.X, mouseOffset.Y));
        MouseArt.Draw(dc, mouse, leftGlow, rightGlow);
        dc.Pop();
    }

    public static void DrawCap(DrawingContext dc, string label, Point center, double opacity, double angle)
    {
        if (String.IsNullOrEmpty(label) || opacity <= .001) return;
        FormattedText glyph;
        if (!CapText.TryGetValue(label, out glyph))
        {
            glyph = new FormattedText(label, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal),
                label.Length > 3 ? 10.5 : 13.5, Ink, 1.0);
            if (CapText.Count < 256) CapText.Add(label, glyph);
        }
        double width = Math.Max(25, glyph.Width + 9);
        const double height = 19;
        dc.PushOpacity(Math.Min(1, opacity));
        dc.PushTransform(new TranslateTransform(center.X, center.Y));
        dc.PushTransform(new RotateTransform(angle));
        dc.DrawRoundedRectangle(CapShade, CapOutline, new Rect(-width / 2 + .3, -height / 2 + 1.3, width, height), 4, 4);
        dc.DrawRoundedRectangle(CapFill, CapOutline, new Rect(-width / 2, -height / 2, width, height - .5), 4, 4);
        dc.DrawText(glyph, new Point(-glyph.Width / 2, -glyph.Height / 2 - .8));
        dc.Pop();
        dc.Pop();
        dc.Pop();
    }
}
