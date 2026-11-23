using System;
using System.Windows;
using System.Windows.Media;

public static class MouseArt
{
    public static readonly Point Center = new Point(104, 260 + DeskLayout.Forward);
    // The back of the mouse turns toward the wrist. Physical left is the viewer's right.
    private const double Angle = 11;
    public static readonly Point PawTarget = Center + new RotateTransform(Angle).Value.Transform(new Vector(-.5,-10));

    private static readonly DrawingGroup[] Normal = new DrawingGroup[Enum.GetValues(typeof(MouseStyle)).Length];
    private static readonly DrawingGroup[] LeftDown = new DrawingGroup[Normal.Length];
    private static readonly DrawingGroup[] RightDown = new DrawingGroup[Normal.Length];

    static MouseArt()
    {
        foreach (MouseStyle style in Enum.GetValues(typeof(MouseStyle)))
        {
            Normal[(int)style] = MakeMouse(style, 0);
            LeftDown[(int)style] = MakeMouse(style, 1);
            RightDown[(int)style] = MakeMouse(style, 2);
        }
    }

    private static Color ColorOf(uint rgb)
    {
        return Color.FromRgb((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);
    }

    private static SolidColorBrush Paint(uint rgb) { return new SolidColorBrush(ColorOf(rgb)); }

    private static Brush Ramp(uint light, uint dark)
    {
        return new LinearGradientBrush(ColorOf(light), ColorOf(dark), new Point(.3, 0), new Point(.7, 1));
    }

    private static Brush Crown(uint light, uint mid, uint dark)
    {
        RadialGradientBrush surface = new RadialGradientBrush();
        surface.Center = new Point(.37, .27);
        surface.GradientOrigin = new Point(.36, .24);
        surface.RadiusX = .74;
        surface.RadiusY = .91;
        surface.GradientStops.Add(new GradientStop(ColorOf(light), 0));
        surface.GradientStops.Add(new GradientStop(ColorOf(mid), .64));
        surface.GradientStops.Add(new GradientStop(ColorOf(dark), 1));
        return surface;
    }

    private static Pen Line(uint color, double width)
    {
        Pen pen = new Pen(Paint(color), width);
        pen.StartLineCap = PenLineCap.Round;
        pen.EndLineCap = PenLineCap.Round;
        pen.LineJoin = PenLineJoin.Round;
        return pen;
    }

    private static void Detail(DrawingContext dc, string curve, uint color, double width)
    {
        dc.DrawGeometry(null, Line(color, width), Geometry.Parse(curve));
    }

    private static void Surface(DrawingContext dc, string contour, Brush material, Pen edge)
    {
        dc.DrawGeometry(material, edge, Geometry.Parse(contour));
    }

    // The near wall stays planted. Only the movable button plates sit lower when pressed.
    private static void Shell(DrawingContext dc, string footprint, string deck, Brush wall, Brush surface, uint edge)
    {
        Surface(dc, footprint, wall, Line(edge, .85));
        Surface(dc, deck, surface, Line(edge, .55));
    }

    // The wheel's long axis points away from the wrist resting on the back of the shell.
    private static void Wheel(DrawingContext dc, double x, double y, double width, double height, uint well, uint rubber, uint shine)
    {
        dc.DrawRoundedRectangle(Paint(well), null, new Rect(x-width*.5, y, width, height), width*.47, width*.47);
        dc.DrawRoundedRectangle(Ramp(shine, rubber), null, new Rect(x-width*.5+.63, y+.62, width-1.26, height-1.28), width*.33, width*.33);
        for(int i=0;i<3;i++)
        {
            double ridgeY = y + height*(.31 + i*.17);
            dc.DrawLine(Line(rubber,.48), new Point(x-width*.24,ridgeY), new Point(x+width*.24,ridgeY+.11));
        }
    }

    private static void Classic(DrawingContext dc)
    {
        Shell(dc,
            "M -13.9,-6.7 C -9,-14 7,-15 13.4,-7.2 C 14.7,-3.1 15.1,4.7 14,10 C 13,15 7.2,17.3 .3,17.5 C -7,17.7 -12.7,15.8 -14,11.9 C -15,8.5 -15.4,-.3 -13.9,-6.7 Z",
            "M -.7,-16.5 C 7.7,-16.9 13.2,-12.3 14,-5.7 C 14.8,.4 14.7,5.7 13.6,8.5 C 12.2,11.9 6.4,13.6 .1,13.6 C -6.1,13.6 -12.3,12.1 -13.7,8.7 C -14.8,5.4 -15.2,-.7 -14.2,-6.4 C -13.2,-12.6 -8.3,-16.2 -.7,-16.5 Z",
            Ramp(0xa8a393,0x625e55), Crown(0xfffef5,0xf3f1e4,0xc4c1b3),0x504c45);
        Wheel(dc,0,2.15,4.8,7.8,0x776440,0xcb8c28,0xffde77);
        Detail(dc,"M -11,-9.7 Q -8.1,-13.6 -3,-14.3",0xffffff,.95);
        Detail(dc,"M -8.7,13.1 Q -5.3,14.5 -2.5,14.6",0xc9c4b4,.58);
        dc.DrawRoundedRectangle(Paint(0x615c50), null, new Rect(-2.1,14.25,4.2,1.25),.6,.6);
    }

    private static void Pebble(DrawingContext dc)
    {
        Shell(dc,
            "M -14.4,-4.8 C -10.8,-13.7 7.7,-13.5 13.2,-7.1 C 16.1,-3 16.3,4.2 14.9,8.2 C 12.8,14.1 7.2,16.5 -.3,16.3 C -9,16.2 -14.2,12.7 -15.1,7.6 C -15.7,4.7 -15.8,-.7 -14.4,-4.8 Z",
            "M -.7,-14.2 C 8.7,-14.7 15.4,-8.7 15.2,-1.2 C 15.2,5.8 11.1,11.1 3.3,12.2 C -3.4,13.4 -11.2,10.8 -14.1,6.4 C -16.1,2.6 -15.9,-3.7 -13.7,-7.6 C -11.2,-12.1 -6.5,-14.2 -.7,-14.2 Z",
            Ramp(0xb19fc1,0x74667f),Crown(0xf4eaf8,0xdfcfe9,0xb19dbf),0x65576e);
        Detail(dc,"M -11.6,7.3 Q -2.7,14 8.4,9.3",0xe9dcee,.7);
        Detail(dc,"M -9.6,-8.1 Q -6.1,-11.8 -1.2,-12",0xf9f1fc,.94);
        Wheel(dc,0,1.95,5.2,7.4,0x9a83a9,0xb7a1c4,0xfff6ff);
        Detail(dc,"M -7.8,13 Q -1,16 5.9,13.9",0x9c88aa,.8);
    }

    private static void Gaming(DrawingContext dc)
    {
        Shell(dc,
            "M -13,-8 L -8.1,-12.9 L 8,-12.9 L 13,-8 L 14.6,-3.7 L 13.5,2.3 L 15,6.6 L 14.6,13.3 L 9.9,17 L 3,17.6 L .1,16 L -3,17.6 L -9.9,17 L -14.6,13.1 L -15,6.3 L -13.6,2.1 L -14.5,-3.7 Z",
            "M -13,-8.5 L -8.3,-14.2 Q -.5,-17.1 8.1,-14.2 L 12.9,-8.8 L 14,-4.1 L 12.9,1.1 L 14.8,6 L 13.8,10.3 L 10,13.3 L 3,13.8 L 0,11.5 L -3,13.8 L -10,13.3 L -13.9,10.2 L -14.7,5.8 L -12.9,.9 L -14.1,-4.2 Z",
            Ramp(0x414950,0x1b2228),Crown(0x69737d,0x444951,0x282e35),0x191d23);
        Surface(dc,"M -12.5,-7.8 L -7.7,-12.6 Q -1.3,-15 6.7,-13.2 L 9.9,-10.2 L 5.6,-5.3 L -6.8,-5.1 Z",Ramp(0x68727c,0x414a52),null);
        const uint accent = 0x42dcd2;
        Detail(dc,"M -12,-6.6 L -12.9,-3.7 L -11.8,-.9 M 12.1,-6.6 L 12.9,-3.8 L 11.9,-.8",accent,1.05);
        Wheel(dc,0,2.4,4.6,7.5,0x181e24,0x3d9897,accent);
        Detail(dc,"M -11.9,12.4 L -9.5,14.4 L -4.2,14.9 M 11.9,12.4 L 9.5,14.4 L 4.2,14.9",accent,.95);
    }

    private static void Trackball(DrawingContext dc)
    {
        Shell(dc,
            "M -14.1,-7.3 C -9.5,-13.7 2.1,-14.5 7.3,-10.8 Q 11.2,-8.5 11.5,-2.9 Q 15.4,-.8 15.6,4.3 L 15.3,9 Q 14.8,13.2 11.1,14.8 Q 2.3,19.5 -8.9,17 Q -14.2,16 -15.1,12 L -15,2.6 Z",
            "M -14.2,-6.6 Q -13.5,-14.3 -6.5,-15.5 Q 1.4,-17 6.1,-13.4 Q 10.1,-10.9 10.5,-4.1 Q 15,-2.8 15.4,1.9 Q 16.1,7.7 12.2,10.3 Q 6.5,14.2 -.4,14.1 L -9,13.7 Q -13.8,13.2 -14.6,9.9 Q -15.3,2.1 -14.2,-6.6 Z",
            Ramp(0x956c6c,0x5b424b),Crown(0xfff5d9,0xeadcbd,0xcab299),0x604a47);
        Detail(dc,"M -12.2,-7.3 Q -11.3,-12.2 -6.5,-13.4",0xfff5e1,1);
        Wheel(dc,0,3.8,3.6,6.3,0x795e5e,0xb58a6b,0xf7d49a);
        // The thumb ball is a separate fixed control behind the two front switches.
        Point ball = new Point(7.1,-4.95);
        dc.DrawEllipse(Ramp(0xad8d71,0xffeac6),Line(0xc5a484,.45),ball,6.05,5.8);
        dc.DrawEllipse(Paint(0x654149),null,ball,5.39,5.0);
        dc.DrawEllipse(Crown(0xff9d6b,0xd75e4a,0x902f41),null,new Point(7.1,-5.21),4.82,4.54);
        dc.DrawEllipse(Paint(0xffdbb3),null,new Point(5.52,-7.13),1.04,.68);
        Detail(dc,"M -10.2,15 Q -7.4,16 -4.9,15.9",0xbd9b8e,.58);
    }

    // The contour is authored for the viewer's right switch. Mirror this local plate only,
    // so the engraved wheel, trackball, base and front wall never move with either switch.
    private static void Button(DrawingContext dc, MouseStyle style, bool physicalLeft, bool down)
    {
        string contour, shoulder, rim;
        uint top, lower, activeTop, activeLower, socket, glow;
        switch (style)
        {
            case MouseStyle.Pebble:
                contour = "M 2.7,.6 Q 8.2,.2 13.8,-.2 Q 14,5.1 10.6,8.4 Q 7.8,10.8 2.3,11.4 L 2.6,9.1 Q 3.2,6.5 2.7,.6 Z";
                shoulder = "M 3.6,1.9 Q 8.1,1.6 12.3,1.4";
                rim = "M 4.1,11.3 Q 10.2,10.4 12.8,6.4";
                top=0xF0E4F4; lower=0xD5BDDE; activeTop=0xFFDAFA; activeLower=0xC87CEC; socket=0x877191; glow=0xFFDAFF;
                break;
            case MouseStyle.Gaming:
                contour = "M 11.9,-2.1 L 4.4,-2.9 L 2.75,1.1 L 2.75,11.6 L 9.4,11.3 L 12.2,9 L 13.1,5.8 L 11.7,1.5 Z";
                shoulder = "M 4.3,.15 L 5.25,-1.55 L 10.8,-1";
                rim = "M 3.9,11.3 L 9.3,11.2 L 12.2,8.6";
                top=0x58636D; lower=0x363D45; activeTop=0xA6FFF3; activeLower=0x30C9D1; socket=0x202831; glow=0xDAFFF8;
                break;
            case MouseStyle.Trackball:
                contour = "M 2.45,2.2 Q 6.4,2.7 12.75,1.8 Q 13.7,6.9 11.1,9.7 Q 8.6,12.3 2.25,12.55 L 2.55,10.2 Z";
                shoulder = "M 3.6,3.45 Q 8.2,3.75 11.6,3";
                rim = "M 3.5,12.05 Q 9.5,11.8 11.7,8.9";
                top=physicalLeft ? 0xB47A80u : 0xDFBDA0u;
                lower=physicalLeft ? 0x895C65u : 0xAD8D78u;
                activeTop=0xFFDDAB; activeLower=0xF49271; socket=0x73545A; glow=0xFFF0CE;
                break;
            default:
                contour = "M 2.65,1.05 Q 8.1,1.0 13.4,-.4 Q 14,5.6 12.8,8.35 Q 10.5,11.95 2.45,12.37 L 2.7,10.15 Z";
                shoulder = "M 3.6,2.25 Q 8.3,2.3 12.2,.95";
                rim = "M 3.9,12 Q 10.2,11.5 12.35,8.25";
                top=0xFBF9EF; lower=0xE2DECC; activeTop=0xFFED8F; activeLower=0xFFAE37; socket=0x989383; glow=0xFFF4AE;
                break;
        }
        Geometry plate = Geometry.Parse(contour);
        dc.PushTransform(new ScaleTransform(physicalLeft ? 1 : -1, 1));
        dc.DrawGeometry(Paint(socket), Line(socket, .65), plate);
        // With the button up, its cap overlaps the rear edge of the socket. A press
        // reveals that stationary edge and brings the cap down against the front lip.
        dc.PushTransform(new TranslateTransform(0, down ? 1.35 : -.4));
        dc.DrawGeometry(down ? Ramp(activeTop,activeLower) : Ramp(top,lower), null, plate);
        if (down)
        {
            Detail(dc, shoulder, socket, .64);
            Detail(dc, rim, glow, 1.06);
        }
        dc.Pop();
        dc.Pop();
    }

    private static DrawingGroup MakeMouse(MouseStyle style, int pressedSwitch)
    {
        DrawingGroup group = new DrawingGroup();
        using (DrawingContext dc = group.Open())
        {
            dc.PushTransform(new TranslateTransform(Center.X, Center.Y));
            dc.PushTransform(new RotateTransform(Angle));
            if (pressedSwitch == 0)
            {
                switch (style)
                {
                    case MouseStyle.Pebble: Pebble(dc); break;
                    case MouseStyle.Gaming: Gaming(dc); break;
                    case MouseStyle.Trackball: Trackball(dc); break;
                    default: Classic(dc); break;
                }
                Button(dc, style, true, false);
                Button(dc, style, false, false);
            }
            else Button(dc, style, pressedSwitch == 1, true);
            dc.Pop();
            dc.Pop();
        }
        group.Freeze();
        return group;
    }

    private static void DrawPress(DrawingContext dc, DrawingGroup plate, double glow)
    {
        if (glow <= .001) return;
        dc.PushOpacity(Math.Min(1, glow));
        dc.DrawDrawing(plate);
        dc.Pop();
    }

    public static void Draw(DrawingContext dc, MouseStyle style, double leftGlow, double rightGlow)
    {
        int index = (int)style;
        if (index < 0 || index >= Normal.Length) index = (int)MouseStyle.Classic;
        dc.DrawDrawing(Normal[index]);
        DrawPress(dc, LeftDown[index], leftGlow);
        DrawPress(dc, RightDown[index], rightGlow);
    }

    public static void Draw(DrawingContext dc, MouseStyle style, double glow)
    {
        Draw(dc, style, glow, 0);
    }
}
