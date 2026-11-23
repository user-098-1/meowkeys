using System;
using System.Windows;
using System.Windows.Media;

public static partial class CatArt
{
    // Tuxedo coat: the black rig stays untouched; white bib, blaze, muzzle,
    // mittens and tail tip are clipped onto the same opaque shapes.
    private static class Tuxedo
    {
        private static readonly Brush White = Wash(new byte[] { 255,249,238 }, new byte[] { 226,217,203 }, new Point(.2,0), new Point(.8,1));
        private static readonly Brush NoseInk = Ink(38,34,42);
        private static readonly Pen Mouth = Stroke(Ink(58,54,62), 1.1);
        private static readonly Pen PawEdge = Stroke(Ink(150,141,130), .9);

        private static readonly Geometry Bib = Shape(
            "M 215,111 C 224,108 233,110 237,118 C 241,134 242,158 239,182 " +
            "C 236,202 231,217 222,222 C 213,225 207,217 205,199 " +
            "C 202,175 205,146 208,124 C 210,114 212,112 215,111 Z");
        private static readonly Geometry Blaze = Shape(
            "M -1,-48 C 3,-42 5,-32 5,-20 C 5,-8 4,2 3,8 L -3,8 " +
            "C -4,2 -5,-8 -5,-20 C -5,-32 -4,-40 -1,-48 Z");
        private static readonly Geometry Muzzle = Shape(
            "M -21,10 C -20,3 -13,-1 -6,-.5 C -2,-.2 2,-.2 6,-.5 " +
            "C 13,-1 20,3 21,10 C 22,17 18,26 10,29 C 4,31 -4,31 -10,29 " +
            "C -18,26 -22,17 -21,10 Z");
        private static readonly Geometry Nose = Shape(
            "M -4.2,7.2 C -4.2,5.6 -2.6,4.9 0,5.3 C 2.6,4.9 4.2,5.6 4.2,7.2 " +
            "C 4.2,9 2,10.6 0,10.6 C -2,10.6 -4.2,9 -4.2,7.2 Z");
        private static readonly Geometry Smile = Shape(
            "M 0,10.6 L 0,13.2 C -1.2,16 -4.6,16.4 -6.6,14.2 " +
            "M 0,13.2 C 1.3,15.8 4.8,16 6.6,13.8");
        private static readonly Geometry Sock = Shape(
            "M -9.6,.5 C -10.4,-4.5 -10.2,-9.5 -7.6,-11.2 Q -5.2,-12.6 -2.2,-11.3 " +
            "Q .8,-9.8 3,-11.2 Q 5.9,-13 8.1,-10.9 Q 10.1,-7.5 9,-.5 " +
            "Q 7,7.4 0,9.4 Q -8.1,8.3 -9.6,.5 Z");

        internal static void DrawBodyMarkings(DrawingContext dc, Geometry body)
        {
            dc.PushClip(body);
            dc.DrawGeometry(White, null, Bib);
            dc.Pop();
        }

        internal static void DrawFaceMarkings(DrawingContext dc, Geometry head, double gx)
        {
            dc.PushClip(head);
            dc.DrawGeometry(White, null, Blaze);
            dc.PushTransform(new TranslateTransform(gx * 2.9, 0));
            dc.DrawGeometry(White, null, Muzzle);
            dc.DrawGeometry(NoseInk, null, Nose);
            dc.DrawGeometry(null, Mouth, Smile);
            dc.Pop();
            dc.Pop();
        }

        internal static void DrawSock(DrawingContext dc, Geometry arm, Point paw, Vector direction,
                                      double radius, double activity)
        {
            if (direction.LengthSquared < .001) direction = new Vector(0,1);
            direction.Normalize();
            dc.PushClip(arm);
            dc.PushTransform(new MatrixTransform(direction.Y,-direction.X,direction.X,direction.Y,paw.X,paw.Y));
            dc.DrawGeometry(White, null, Sock);
            dc.Pop();
            dc.Pop();
            dc.DrawEllipse(White, PawEdge, paw, radius + .35, radius * (.94 - .06 * activity));
        }

        internal static void DrawTailTip(DrawingContext dc, Geometry tail, double tip)
        {
            dc.PushClip(tail);
            dc.DrawEllipse(White, null, new Point(326, 174 + tip * 6), 11.5, 10.5);
            dc.Pop();
        }
    }
}
