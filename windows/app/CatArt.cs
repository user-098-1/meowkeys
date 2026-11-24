using System;
using System.Windows;
using System.Windows.Media;

// The resting pose, keyboard and mouse share the same 480 x 320 drawing space.
public static partial class CatArt
{
    private static SolidColorBrush Ink(byte r, byte g, byte b)
    {
        SolidColorBrush brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }

    private static Brush Wash(byte[] start, byte[] end, Point from, Point to)
    {
        LinearGradientBrush brush = new LinearGradientBrush(
            Color.FromRgb(start[0], start[1], start[2]),
            Color.FromRgb(end[0], end[1], end[2]), from, to);
        brush.Freeze();
        return brush;
    }

    private static Pen Stroke(Brush brush, double width)
    {
        Pen pen = new Pen(brush, width);
        pen.StartLineCap = PenLineCap.Round;
        pen.EndLineCap = PenLineCap.Round;
        pen.LineJoin = PenLineJoin.Round;
        pen.Freeze();
        return pen;
    }

    private static Geometry Shape(string data)
    {
        Geometry g = Geometry.Parse(data);
        g.Freeze();
        return g;
    }

    // Coat colors are locked here, one entry per CatStyle; the marking shapes
    // for each coat live in their own style files.
    internal sealed class CatCoat
    {
        public Brush Fur, Face, Foreleg, Iris, Ear, Pupil;
        public Pen Lid, Whisker, Root, PawRim, EyeRim;
    }

    private static readonly Brush Pupil = Ink(22,24,16);

    private static CatCoat Define(Brush fur, Brush face, Brush foreleg, Brush iris, Brush ear,
                                  Pen lid, Pen whisker, Pen root, Pen pawRim, Pen eyeRim)
    {
        return new CatCoat { Fur=fur, Face=face, Foreleg=foreleg, Iris=iris, Ear=ear,
                             Pupil=Pupil, Lid=lid, Whisker=whisker, Root=root, PawRim=pawRim, EyeRim=eyeRim };
    }

    private static readonly CatCoat[] Coats = BuildCoats();

    private static CatCoat[] BuildCoats()
    {
        CatCoat black = Define(
            Wash(new byte[] { 31,29,32 }, new byte[] { 22,21,24 }, new Point(0,0), new Point(1,0.85)),
            Wash(new byte[] { 32,30,32 }, new byte[] { 24,23,25 }, new Point(0,0), new Point(0.9,1)),
            Wash(new byte[] { 30,28,31 }, new byte[] { 24,23,26 }, new Point(0.2,0), new Point(0.9,1)),
            Wash(new byte[] { 215,245,80 }, new byte[] { 182,224,62 }, new Point(0.2,0), new Point(0.83,1)),
            Ink(36,32,36),
            Stroke(Ink(189,221,70), 2.2),
            Stroke(Ink(39,37,38), 0.9),
            Stroke(Ink(172,169,162), 0.95),
            Stroke(Ink(92,89,91), 0.95),
            null);
        CatCoat siamese = Define(
            Wash(new byte[] { 240,232,218 }, new byte[] { 226,216,200 }, new Point(0,0), new Point(1,0.85)),
            Wash(new byte[] { 243,236,222 }, new byte[] { 228,218,202 }, new Point(0,0), new Point(0.9,1)),
            Wash(new byte[] { 241,234,220 }, new byte[] { 228,218,204 }, new Point(0.2,0), new Point(0.9,1)),
            Wash(new byte[] { 150,205,246 }, new byte[] { 62,132,216 }, new Point(0.2,0), new Point(0.83,1)),
            Ink(56,43,40),
            Stroke(Ink(168,212,248), 2.2),
            Stroke(Ink(216,206,190), 0.9),
            Stroke(Ink(202,190,172), 0.95),
            Stroke(Ink(168,156,144), 0.95),
            null);
        CatCoat golden = Define(
            Wash(new byte[] { 246,214,150 }, new byte[] { 198,152,90 }, new Point(0.9,0.15), new Point(0.05,0.8)),
            Wash(new byte[] { 228,186,118 }, new byte[] { 248,228,184 }, new Point(0.4,0), new Point(0.55,1)),
            Wash(new byte[] { 246,220,170 }, new byte[] { 216,178,118 }, new Point(0.2,0), new Point(0.9,1)),
            Wash(new byte[] { 168,230,158 }, new byte[] { 64,176,104 }, new Point(0.2,0), new Point(0.83,1)),
            Ink(224,164,128),
            Stroke(Ink(150,214,150), 2.2),
            Stroke(Ink(120,94,64), 0.9),
            Stroke(Ink(150,116,74), 0.95),
            Stroke(Ink(150,116,74), 0.95),
            Stroke(Ink(34,28,26), 1.6));
        return new CatCoat[] { black, black, siamese, golden };
    }

    private static CatCoat CoatOf(CatStyle style)
    {
        return Coats[(int)style];
    }

    private static readonly Geometry Body = Shape(
        "M 175,102 C 175,121 173,140 168,156 C 163,173 157,195 160,210 " +
        "C 161,219 163,225 167,228 C 165,234 169,239 175,239 " +
        "C 181,239 185,235 186,231 C 199,234 223,235 240,230 " +
        "C 241,235 245,238 251,238 C 258,238 262,232 260,227 " +
        "C 267,218 269,201 267,185 C 264,159 253,130 251,103 Z");

    private static readonly Geometry Head = Shape(
        "M -45,-21 C -45,-32 -43,-52 -39,-59 C -37,-63 -34,-63 -31,-61 " +
        "C -26,-58 -19,-48 -15,-44 C -5,-47 6,-47 16,-44 " +
        "C 22,-51 30,-62 34,-63 C 39,-64 41,-57 43,-47 C 45,-39 45,-27 45,-20 " +
        "C 50,-12 50,-1 47,9 C 44,19 36,27 25,30 C 11,35 -13,35 -28,29 " +
        "C -39,27 -47,17 -49,5 C -51,-4 -49,-14 -45,-21 Z");
    private static readonly Geometry EarLeft = Shape("M -40,-35 Q -40,-51 -36,-56 Q -32,-54 -23,-43 Q -32,-42 -40,-35 Z");
    private static readonly Geometry EarRight = Shape("M 24,-44 Q 33,-56 35,-57 Q 39,-52 40,-36 Q 35,-41 24,-44 Z");

    // Marking shapes shared by several coats: the paw sock, nose leather and smile.
    internal static readonly Geometry SockShape = Shape(
        "M -9.6,.5 C -10.4,-4.5 -10.2,-9.5 -7.6,-11.2 Q -5.2,-12.6 -2.2,-11.3 " +
        "Q .8,-9.8 3,-11.2 Q 5.9,-13 8.1,-10.9 Q 10.1,-7.5 9,-.5 " +
        "Q 7,7.4 0,9.4 Q -8.1,8.3 -9.6,.5 Z");
    internal static readonly Geometry NoseShape = Shape(
        "M -4.2,7.2 C -4.2,5.6 -2.6,4.9 0,5.3 C 2.6,4.9 4.2,5.6 4.2,7.2 " +
        "C 4.2,9 2,10.6 0,10.6 C -2,10.6 -4.2,9 -4.2,7.2 Z");
    internal static readonly Geometry SmileShape = Shape(
        "M 0,10.6 L 0,13.2 C -1.2,16 -4.6,16.4 -6.6,14.2 " +
        "M 0,13.2 C 1.3,15.8 4.8,16 6.6,13.8");

    internal static void PawSock(DrawingContext dc, Brush fill, Pen edge, Geometry sock, Geometry arm,
                                 Point paw, Vector direction, double radius, double activity)
    {
        if (direction.LengthSquared < .001) direction = new Vector(0,1);
        direction.Normalize();
        dc.PushClip(arm);
        dc.PushTransform(new MatrixTransform(direction.Y,-direction.X,direction.X,direction.Y,paw.X,paw.Y));
        dc.DrawGeometry(fill, null, sock);
        dc.Pop();
        dc.Pop();
        dc.DrawEllipse(fill, edge, paw, radius + .35, radius * (.94 - .06 * activity));
    }

    private static double Limit(double value, double low, double high)
    {
        return Math.Max(low, Math.Min(high, value));
    }

    private static double Smooth(double x)
    {
        x = Limit(x, 0, 1);
        return x * x * (3 - 2 * x);
    }

    private static void Tail(DrawingContext dc, double t, CatStyle style)
    {
        double wave = Math.Sin(t * 1.9);
        double tip = Math.Sin(t * 1.9 - 0.68);
        StreamGeometry tail = new StreamGeometry();
        using (StreamGeometryContext g = tail.Open())
        {
            g.BeginFigure(new Point(251,199), true, true);
            g.BezierTo(new Point(263,200),new Point(273,204),new Point(281,201 + wave * 1.3),true,true);
            g.BezierTo(new Point(290,198),new Point(292,186 + wave * 2),new Point(300,177 + wave * 2.5),true,true);
            g.BezierTo(new Point(307,169 + tip * 4),new Point(317,165 + tip * 5),new Point(325,166 + tip * 6),true,true);
            g.BezierTo(new Point(335,167 + tip * 6),new Point(334,181 + tip * 6),new Point(324,181 + tip * 6),true,true);
            g.BezierTo(new Point(311,180 + tip * 4),new Point(305,191),new Point(298,202 + wave * 2),true,true);
            g.BezierTo(new Point(289,217 + wave),new Point(268,220),new Point(251,214),true,true);
        }
        dc.DrawGeometry(CoatOf(style).Fur, null, tail);
        if (style == CatStyle.Tuxedo) Tuxedo.DrawTailTip(dc, tail, tip);
        else if (style == CatStyle.Siamese) Siamese.DrawTailTip(dc, tail, tip);
        else if (style == CatStyle.Golden) Golden.DrawTailTip(dc, tail, tip);
    }

    private static Point Cubic(Point a, Point b, Point c, Point d, double t)
    {
        double z = 1-t;
        return new Point(z*z*z*a.X+3*z*z*t*b.X+3*z*t*t*c.X+t*t*t*d.X,
                         z*z*z*a.Y+3*z*z*t*b.Y+3*z*t*t*c.Y+t*t*t*d.Y);
    }

    private static void Arm(DrawingContext dc, bool left, Point paw, double lean, double activity, CatStyle style)
    {
        CatCoat coat = CoatOf(style);
        double side = left ? -1 : 1;
        double raised = Smooth((221 - paw.Y)/32);
        Point a = new Point(214 + side*26 - 29*lean, 128);
        double dx = paw.X-a.X, dy=paw.Y-a.Y;
        Point b = new Point(a.X + dx*0.25 + side*(4+8*raised), a.Y+dy*0.37);
        Point c = new Point(a.X + dx*0.79 + side*(4+20*raised), paw.Y-15+raised*41);
        double pawRadius = 8.4 + Limit(activity,0,1)*1.0;
        StreamGeometry limb = new StreamGeometry { FillRule = FillRule.Nonzero };
        const int count = 18;
        Point[] secondSide = new Point[count+1];
        using (StreamGeometryContext g = limb.Open())
        {
            for (int i = 0; i <= count; ++i)
            {
                double u = i/(double)count;
                Point mid = Cubic(a,b,c,paw,u);
                Vector tangent = Cubic(a,b,c,paw,Math.Min(1,u+0.01)) - Cubic(a,b,c,paw,Math.Max(0,u-0.01));
                if (tangent.Length < 0.001) tangent = new Vector(0,1);
                tangent.Normalize();
                double width = (12.8 + (pawRadius - 12.8)*Smooth(u)) * (0.08 + 0.92 * Smooth(u / 0.19));
                Vector offset = new Vector(-tangent.Y*width,tangent.X*width);
                Point first = mid+offset;
                secondSide[i] = mid-offset;
                if (i==0) g.BeginFigure(first,true,true);
                else g.LineTo(first,true,true);
            }
            for (int i=count; i>=0; --i) g.LineTo(secondSide[i],true,true);
        }
        dc.DrawGeometry(coat.Foreleg, null, limb);
        if (style == CatStyle.Tuxedo)
        {
            Tuxedo.DrawSock(dc, limb, paw, paw-c, pawRadius, activity);
            return;
        }
        if (style == CatStyle.Siamese)
        {
            Siamese.DrawSock(dc, limb, paw, paw-c, pawRadius, activity);
            return;
        }
        // End the continuous foreleg with a soft mitten shape even at oblique key reaches.
        dc.DrawEllipse(coat.Foreleg,null,paw,pawRadius+0.35,pawRadius*(0.94-0.06*activity));
        if (raised > 0.1)
        {
            dc.PushOpacity(raised * 0.85);
            StreamGeometry rim = new StreamGeometry();
            using (StreamGeometryContext g = rim.Open())
            {
                g.BeginFigure(new Point(paw.X+side*7.5,paw.Y+0.7),false,false);
                g.BezierTo(new Point(paw.X+side*7.1,paw.Y+6.2),new Point(paw.X+side*2.7,paw.Y+8.2),new Point(paw.X-side*2.3,paw.Y+7.7),true,true);
            }
            dc.DrawGeometry(null,coat.PawRim,rim);
            dc.Pop();
        }
    }

    private static void Eye(DrawingContext dc, double side, double gx, double gy, double blink, CatCoat coat)
    {
        double angle = side*3 + gx*4;
        double x = side*19.1 + gx*2.6;
        double y = -15.8 + gy*1.7 - side*gx*1.1;
        double width = 13.0 * (1 - Math.Max(0, side*gx)*0.095);
        double height = 12.5 * (1 - blink);
        dc.PushTransform(new RotateTransform(angle,x,y));
        if (height < 1.6)
        {
            StreamGeometry slit = new StreamGeometry();
            using (StreamGeometryContext g = slit.Open())
            {
                g.BeginFigure(new Point(x-width*0.86,y),false,false);
                g.QuadraticBezierTo(new Point(x,y+3.0),new Point(x+width*0.86,y-0.2),true,true);
            }
            dc.DrawGeometry(null,coat.Lid,slit);
        }
        else
        {
            EllipseGeometry iris = new EllipseGeometry(new Point(x,y),width,height);
            dc.DrawGeometry(coat.Iris,null,iris);
            if (coat.EyeRim != null) dc.DrawGeometry(null,coat.EyeRim,iris);
            dc.PushClip(iris);
            dc.DrawEllipse(coat.Pupil,null,new Point(x+gx*7.8,y+gy*6.7),4.3+Math.Abs(gy)*0.8,8.1);
            dc.Pop();
        }
        dc.Pop();
    }

    private static void Whiskers(DrawingContext dc, double side, double gx, CatCoat coat)
    {
        double cheek = side*26 + gx*3;
        for (int k=0; k<3; ++k)
        {
            Point start = new Point(cheek+side*(0.8 - k*0.4),6 + k*5.0);
            Point end = new Point(cheek+side*(29-k*2.4), start.Y + (k-0.8)*5.6);
            StreamGeometry whisker = new StreamGeometry();
            using (StreamGeometryContext g = whisker.Open())
            {
                g.BeginFigure(start,false,false);
                g.QuadraticBezierTo(new Point((start.X+end.X)/2,start.Y-1.1+k*0.65),end,true,true);
            }
            dc.DrawGeometry(null,coat.Whisker,whisker);
            dc.DrawLine(coat.Root,start,new Point(start.X+side*(6.2-k*0.9),start.Y-0.45+k*0.65));
        }
    }

    public static void DrawStyled(DrawingContext dc, CatStyle style, double t, double gazeX, double gazeY,
                                  double blink, Point leftPaw, Point rightPaw, double leftActivity, double rightActivity)
    {
        CatCoat coat = CoatOf(style);
        double gx=Limit(gazeX,-1,1), gy=Limit(gazeY,-1,1);
        double lean=Smooth((157-leftPaw.X)/50);
        double breath=Math.Sin(t*1.7)*0.72;
        Tail(dc,t,style);

        // Keep the hips planted while the chest follows the outstretched mouse paw.
        dc.PushTransform(new MatrixTransform(1,0,lean*0.282,1, -233*lean*0.282,0));
        // Forelegs overlap one solid torso at every reach, including two-key chords.
        dc.DrawGeometry(coat.Fur, null, Body);
        DrawBodyMarkings(style, dc, Body);
        dc.Pop();
        Arm(dc,false,rightPaw,lean,rightActivity,style);
        Arm(dc,true,leftPaw,lean,leftActivity,style);

        dc.PushTransform(new TranslateTransform(214 - 45*lean + gx*3.2, 86 + breath + gy*2.0 + lean*1.2));
        dc.PushTransform(new RotateTransform(gx*2.8 - lean*3.2));
        DrawHead(dc, style, gx, gy, blink);
        dc.Pop();
        dc.Pop();
    }

    private static void DrawBodyMarkings(CatStyle style, DrawingContext dc, Geometry body)
    {
        if (style == CatStyle.Tuxedo) Tuxedo.DrawBodyMarkings(dc, body);
        else if (style == CatStyle.Siamese) Siamese.DrawBodyMarkings(dc, body);
        else if (style == CatStyle.Golden) Golden.DrawBodyMarkings(dc, body);
    }

    // Flat body render for the sprite exporter: torso plus coat markings, no pose.
    internal static void DrawBodySprite(DrawingContext dc, CatStyle style)
    {
        dc.DrawGeometry(CoatOf(style).Fur, null, Body);
        DrawBodyMarkings(style, dc, Body);
    }

    // Head-space bounds including whiskers are approximately (-57,-64) to (57,35).
    public static void DrawPortrait(DrawingContext dc, CatStyle style)
    {
        DrawHead(dc, style, 0, 0, 0);
    }

    public static bool ContainsHead(CatStyle style, Point pointer, double time,
                                    double gazeX, double gazeY, Point leftPaw)
    {
        return Math.Pow((pointer.X - 212) / 63, 2) + Math.Pow((pointer.Y - 85) / 53, 2) < 1;
    }

    private static void DrawHead(DrawingContext dc, CatStyle style, double gx, double gy, double blink)
    {
        CatCoat coat = CoatOf(style);
        dc.DrawGeometry(coat.Face, null, Head);
        dc.DrawGeometry(coat.Ear, null, EarLeft);
        dc.DrawGeometry(coat.Ear, null, EarRight);
        if (style == CatStyle.Tuxedo) Tuxedo.DrawFaceMarkings(dc, Head, gx);
        else if (style == CatStyle.Siamese) Siamese.DrawFaceMarkings(dc, Head, gx);
        else if (style == CatStyle.Golden) Golden.DrawFaceMarkings(dc, Head, gx);
        Eye(dc,-1,gx,gy,Limit(blink,0,1),coat);
        Eye(dc,1,gx,gy,Limit(blink,0,1),coat);
        Whiskers(dc,-1,gx,coat);
        Whiskers(dc,1,gx,coat);
    }
}
