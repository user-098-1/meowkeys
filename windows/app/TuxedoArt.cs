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
            dc.DrawGeometry(NoseInk, null, NoseShape);
            dc.DrawGeometry(null, Mouth, SmileShape);
            dc.Pop();
            dc.Pop();
        }

        internal static void DrawSock(DrawingContext dc, Geometry arm, Point paw, Vector direction,
                                      double radius, double activity)
        {
            PawSock(dc, White, PawEdge, SockShape, arm, paw, direction, radius, activity);
        }

        internal static void DrawTailTip(DrawingContext dc, Geometry tail, double tip)
        {
            dc.PushClip(tail);
            dc.DrawEllipse(White, null, new Point(326, 174 + tip * 6), 11.5, 10.5);
            dc.Pop();
        }
    }
}
