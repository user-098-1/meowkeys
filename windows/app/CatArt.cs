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

    private static Brush Wash(byte[] light, byte[] dark, Point from, Point to)
    {
        LinearGradientBrush brush = new LinearGradientBrush(
            Color.FromRgb(light[0], light[1], light[2]),
            Color.FromRgb(dark[0], dark[1], dark[2]), from, to);
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

    private static readonly Brush Fur = Wash(new byte[] { 31,29,32 }, new byte[] { 22,21,24 }, new Point(0,0), new Point(1,0.85));
    private static readonly Brush Face = Wash(new byte[] { 32,30,32 }, new byte[] { 24,23,25 }, new Point(0,0), new Point(0.9,1));
    private static readonly Brush Foreleg = Wash(new byte[] { 30,28,31 }, new byte[] { 24,23,26 }, new Point(0.2,0), new Point(0.9,1));
    private static readonly Brush Iris = Wash(new byte[] { 215,245,80 }, new byte[] { 182,224,62 }, new Point(0.2,0), new Point(0.83,1));
    private static readonly Brush Pupil = Ink(22,24,16);
    private static readonly Brush Ear = Ink(36,32,36);
    private static readonly Brush WhiskerRoot = Ink(172,169,162);
    private static readonly Pen Whisker = Stroke(Ink(39,37,38), 0.9);
    private static readonly Pen RootPen = Stroke(WhiskerRoot, 0.95);
    private static readonly Pen Lid = Stroke(Ink(189,221,70), 2.2);
    private static readonly Pen PawLight = Stroke(Ink(92,89,91), 0.95);

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

    private static double Limit(double value, double low, double high)
    {
        return Math.Max(low, Math.Min(high, value));
    }

    private static double Smooth(double x)
    {
        x = Limit(x, 0, 1);
        return x * x * (3 - 2 * x);
    }

    private static void Tail(DrawingContext dc, double t, bool tuxedo)
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
        dc.DrawGeometry(Fur, null, tail);
        if (tuxedo) Tuxedo.DrawTailTip(dc, tail, tip);
    }

    private static Point Cubic(Point a, Point b, Point c, Point d, double t)
    {
        double z = 1-t;
        return new Point(z*z*z*a.X+3*z*z*t*b.X+3*z*t*t*c.X+t*t*t*d.X,
                         z*z*z*a.Y+3*z*z*t*b.Y+3*z*t*t*c.Y+t*t*t*d.Y);
    }

    private static void Arm(DrawingContext dc, bool left, Point paw, double lean, double activity, bool tuxedo)
    {
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
        dc.DrawGeometry(Foreleg, null, limb);
        if (tuxedo)
        {
            Tuxedo.DrawSock(dc, limb, paw, paw-c, pawRadius, activity);
            return;
        }
        // End the continuous foreleg with a soft mitten shape even at oblique key reaches.
        dc.DrawEllipse(Foreleg,null,paw,pawRadius+0.35,pawRadius*(0.94-0.06*activity));
        if (raised > 0.1)
        {
            dc.PushOpacity(raised * 0.85);
            StreamGeometry rim = new StreamGeometry();
            using (StreamGeometryContext g = rim.Open())
            {
                g.BeginFigure(new Point(paw.X+side*7.5,paw.Y+0.7),false,false);
                g.BezierTo(new Point(paw.X+side*7.1,paw.Y+6.2),new Point(paw.X+side*2.7,paw.Y+8.2),new Point(paw.X-side*2.3,paw.Y+7.7),true,true);
            }
            dc.DrawGeometry(null,PawLight,rim);
            dc.Pop();
        }
    }

    private static void Eye(DrawingContext dc, double side, double gx, double gy, double blink)
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
            dc.DrawGeometry(null,Lid,slit);
        }
        else
        {
            EllipseGeometry iris = new EllipseGeometry(new Point(x,y),width,height);
            dc.DrawGeometry(Iris,null,iris);
            dc.PushClip(iris);
            dc.DrawEllipse(Pupil,null,new Point(x+gx*7.8,y+gy*6.7),4.3+Math.Abs(gy)*0.8,8.1);
            dc.Pop();
        }
        dc.Pop();
    }

    private static void Whiskers(DrawingContext dc, double side, double gx)
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
            dc.DrawGeometry(null,Whisker,whisker);
            dc.DrawLine(RootPen,start,new Point(start.X+side*(6.2-k*0.9),start.Y-0.45+k*0.65));
        }
    }

    public static void DrawStyled(DrawingContext dc, CatStyle style, double t, double gazeX, double gazeY,
                                  double blink, Point leftPaw, Point rightPaw, double leftActivity, double rightActivity)
    {
        bool tuxedo = style == CatStyle.Tuxedo;
        double gx=Limit(gazeX,-1,1), gy=Limit(gazeY,-1,1);
        double lean=Smooth((157-leftPaw.X)/50);
        double breath=Math.Sin(t*1.7)*0.72;
        Tail(dc,t,tuxedo);

        // Keep the hips planted while the chest follows the outstretched mouse paw.
        dc.PushTransform(new MatrixTransform(1,0,lean*0.282,1, -233*lean*0.282,0));
        // Forelegs overlap one solid torso at every reach, including two-key chords.
        dc.DrawGeometry(Fur, null, Body);
        if (tuxedo) Tuxedo.DrawBodyMarkings(dc, Body);
        dc.Pop();
        Arm(dc,false,rightPaw,lean,rightActivity,tuxedo);
        Arm(dc,true,leftPaw,lean,leftActivity,tuxedo);

        dc.PushTransform(new TranslateTransform(214 - 45*lean + gx*3.2, 86 + breath + gy*2.0 + lean*1.2));
        dc.PushTransform(new RotateTransform(gx*2.8 - lean*3.2));
        DrawHead(dc, tuxedo, gx, gy, blink);
        dc.Pop();
        dc.Pop();
    }

    // Head-space bounds including whiskers are approximately (-57,-64) to (57,35).
    public static void DrawPortrait(DrawingContext dc, CatStyle style)
    {
        DrawHead(dc, style == CatStyle.Tuxedo, 0, 0, 0);
    }

    public static bool ContainsHead(CatStyle style, Point pointer, double time,
                                    double gazeX, double gazeY, Point leftPaw)
    {
        return Math.Pow((pointer.X - 212) / 63, 2) + Math.Pow((pointer.Y - 85) / 53, 2) < 1;
    }

    private static void DrawHead(DrawingContext dc, bool tuxedo, double gx, double gy, double blink)
    {
        dc.DrawGeometry(Face, null, Head);
        dc.DrawGeometry(Ear, null, EarLeft);
        dc.DrawGeometry(Ear, null, EarRight);
        if (tuxedo) Tuxedo.DrawFaceMarkings(dc, Head, gx);
        Eye(dc,-1,gx,gy,Limit(blink,0,1));
        Eye(dc,1,gx,gy,Limit(blink,0,1));
        Whiskers(dc,-1,gx);
        Whiskers(dc,1,gx);
    }
}
