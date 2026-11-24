using System;
using System.Windows;
using System.Windows.Media;

public static partial class CatArt
{
    // Golden shaded coat: bright apricot-gold fur with a soft cream chest that
    // fades into the body, a cream muzzle, pink nose and a ringed tail tip.
    private static class Golden
    {
        private static readonly Brush Cream = Wash(new byte[] { 252,240,216 }, new byte[] { 240,224,190 }, new Point(.2,0), new Point(.8,1));
        private static readonly Brush Chest = SoftCream();
        private static readonly Brush Tip = Wash(new byte[] { 148,108,62 }, new byte[] { 118,82,46 }, new Point(0,0), new Point(0.85,1));
        private static readonly Brush NosePink = Wash(new byte[] { 236,150,138 }, new byte[] { 212,114,102 }, new Point(0,0), new Point(0.9,1));
        private static readonly Pen Mouth = Stroke(Ink(128,86,66), 1.1);
        private static readonly Pen Tabby = Stroke(Ink(186,140,80), 2.1);

        // Faint golden tabby pencil lines: forehead M and two cheek strokes.
        private static readonly Geometry ForeheadLines = Shape(
            "M -21,-37 C -16,-33 -12,-32 -7,-34 " +
            "M 0,-45 C -1,-42 -1,-40 0,-37 " +
            "M 21,-37 C 16,-33 12,-32 7,-34");
        private static readonly Geometry CheekLines = Shape(
            "M -44,-3 C -38,-1 -33,1 -29,4 " +
            "M -43,7 C -38,8 -34,9 -30,11 " +
            "M 44,-3 C 38,-1 33,1 29,4 " +
            "M 43,7 C 38,8 34,9 30,11");

        private static Brush SoftCream()
        {
            RadialGradientBrush brush = new RadialGradientBrush();
            brush.Center = new Point(.5,.42);
            brush.GradientOrigin = new Point(.5,.42);
            brush.RadiusX = .62;
            brush.RadiusY = .62;
            brush.GradientStops.Add(new GradientStop(Color.FromArgb(235,250,238,214), 0));
            brush.GradientStops.Add(new GradientStop(Color.FromArgb(190,248,232,200), .55));
            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0,248,232,200), 1));
            brush.Freeze();
            return brush;
        }

        // Rounder, chubbier muzzle than the tuxedo blaze face.
        private static readonly Geometry Muzzle = Shape(
            "M -23,8 C -22,1 -14,-3 -6,-2.5 C -2,-2.2 2,-2.2 6,-2.5 " +
            "C 14,-3 22,1 23,8 C 24,16 19,26 10,30 C 4,32.5 -4,32.5 -10,30 " +
            "C -19,26 -24,16 -23,8 Z");

        internal static void DrawBodyMarkings(DrawingContext dc, Geometry body)
        {
            dc.PushClip(body);
            dc.DrawEllipse(Chest, null, new Point(222,162), 31, 64);
            dc.Pop();
        }

        internal static void DrawFaceMarkings(DrawingContext dc, Geometry head, double gx)
        {
            dc.PushClip(head);
            dc.PushOpacity(.5);
            dc.DrawGeometry(null, Tabby, ForeheadLines);
            dc.DrawGeometry(null, Tabby, CheekLines);
            dc.Pop();
            dc.PushTransform(new TranslateTransform(gx * 2.9, 0));
            dc.DrawGeometry(Cream, null, Muzzle);
            dc.DrawGeometry(NosePink, null, NoseShape);
            dc.DrawGeometry(null, Mouth, SmileShape);
            dc.Pop();
            dc.Pop();
        }

        internal static void DrawTailTip(DrawingContext dc, Geometry tail, double tip)
        {
            dc.PushClip(tail);
            dc.DrawEllipse(Tip, null, new Point(298,184 + tip * 4), 6.5, 11);
            dc.DrawEllipse(Tip, null, new Point(310,175 + tip * 5), 6.5, 12);
            dc.DrawEllipse(Tip, null, new Point(323,173 + tip * 6), 12.5, 12.5);
            dc.Pop();
        }
    }
}
