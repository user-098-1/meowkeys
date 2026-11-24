using System;
using System.Windows;
using System.Windows.Media;

public static partial class CatArt
{
    // Siamese coat: seal points sit on the ears, a diamond mask that runs from
    // the forehead through the eyes to the chin, gloves and tail tip. Cheeks
    // stay cream and the whiskers are pale so the face reads as a cat.
    private static class Siamese
    {
        private static readonly Brush Seal = Wash(new byte[] { 74,56,50 }, new byte[] { 48,36,32 }, new Point(0,0), new Point(0.9,1));
        private static readonly Brush MaskSeal = Wash(new byte[] { 96,74,64 }, new byte[] { 56,42,37 }, new Point(0.3,0), new Point(0.6,1));
        private static readonly Brush InnerEar = Wash(new byte[] { 168,128,112 }, new byte[] { 132,96,84 }, new Point(0,0), new Point(0.6,1));
        private static readonly Brush NoseInk = Ink(30,22,20);
        private static readonly Pen Mouth = Stroke(Ink(206,190,172), 1.15);
        private static readonly Pen PawEdge = Stroke(Ink(138,122,112), .9);

        // Full outer ears, clipped to the head so the base edge follows the skull.
        private static readonly Geometry EarPointLeft = Shape(
            "M -46,-18 C -46,-34 -43,-54 -39,-60 C -37,-64 -33,-64 -30,-61 " +
            "C -25,-57 -18,-47 -13,-42 L -26,-33 C -35,-29 -42,-24 -46,-18 Z");
        private static readonly Geometry EarPointRight = Shape(
            "M 14,-42 C 21,-50 30,-62 34,-64 C 39,-65 42,-57 44,-47 " +
            "C 46,-37 46,-26 46,-18 C 41,-25 33,-32 26,-35 Z");

        // Diamond mask: narrow at the forehead, widest across the eyes, then
        // tapering over the nose and mouth to the chin.
        private static readonly Geometry Mask = Shape(
            "M 0,-45 " +
            "C -11,-45 -23,-39 -29,-31 " +
            "C -37,-25 -39,-16 -35,-8 " +
            "C -32,0 -25,4 -21,10 " +
            "C -19,20 -12,26 0,26 " +
            "C 12,26 19,20 21,10 " +
            "C 25,4 32,0 35,-8 " +
            "C 39,-16 37,-25 29,-31 " +
            "C 23,-39 11,-45 0,-45 Z");

        internal static void DrawBodyMarkings(DrawingContext dc, Geometry body)
        {
        }

        internal static void DrawFaceMarkings(DrawingContext dc, Geometry head, double gx)
        {
            dc.PushClip(head);
            dc.DrawGeometry(Seal, null, EarPointLeft);
            dc.DrawGeometry(Seal, null, EarPointRight);
            dc.DrawGeometry(InnerEar, null, EarLeft);
            dc.DrawGeometry(InnerEar, null, EarRight);
            dc.DrawGeometry(MaskSeal, null, Mask);
            dc.PushTransform(new TranslateTransform(gx * 2.9, 0));
            dc.DrawGeometry(NoseInk, null, NoseShape);
            dc.DrawGeometry(null, Mouth, SmileShape);
            dc.Pop();
            dc.Pop();
        }

        internal static void DrawSock(DrawingContext dc, Geometry arm, Point paw, Vector direction,
                                      double radius, double activity)
        {
            PawSock(dc, Seal, PawEdge, SockShape, arm, paw, direction, radius, activity);
        }

        internal static void DrawTailTip(DrawingContext dc, Geometry tail, double tip)
        {
            dc.PushClip(tail);
            dc.DrawEllipse(Seal, null, new Point(312, 172 + tip * 6), 19, 15);
            dc.DrawEllipse(Seal, null, new Point(325, 173 + tip * 6), 14, 13.5);
            dc.Pop();
        }
    }
}
