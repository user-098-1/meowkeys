using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

// All positions are expressed on a 480 x 320 drawing surface.
internal sealed class CatScene : FrameworkElement
{
    private sealed class Cap
    {
        public string Label;
        public Point Start;
        public double Time, Angle;
    }

    private static readonly Point IdleLeft = new Point(192, 233);
    private static readonly Point IdleRight = new Point(229, 233);
    private readonly List<Cap> caps = new List<Cap>();
    private readonly Dictionary<int, double> strikes = new Dictionary<int, double>();
    private readonly Dictionary<int, double> highlights = new Dictionary<int, double>();
    private readonly List<int> expired = new List<int>();
    private readonly Random random = new Random();
    private Point left = IdleLeft, freeLeft = IdleLeft, right = IdleRight, leftKey, rightKey;
    private Point pointer, lastPointer;
    private Vector mouseOffset, localTravel, monitorPosition;
    private double grip;
    private bool pointerKnown, leftDown, rightDown, headHovered;
    private double time, lastKey = -10, leftAt = -10, rightAt = -10;
    private double lastMove = -10, leftClickAt = -10, rightClickAt = -10, lastPet = -10, nextBlink = 2.7;
    private double gazeX, gazeY, blink, leftActivity, rightActivity;
    public bool LabelsEnabled = true;
    public CatStyle Cat = CatStyle.Black;
    public KeyboardStyle Keyboard = KeyboardStyle.Classic;
    public MouseStyle Mouse = MouseStyle.Classic;
    public const double CanvasWidth = 480, CanvasHeight = 320;

    public void ClearLabels() { caps.Clear(); InvalidateVisual(); }

    public void Strike(int vk, double now)
    {
        Point target = DeskArt.KeyPoint(vk);
        lastKey = now;
        strikes[vk] = now;
        if (!UsingMouse(now) && (target.X < 218 || (vk == 32 && rightAt > leftAt)))
        {
            leftAt = now;
            leftKey = target;
        }
        else
        {
            rightAt = now;
            rightKey = target;
        }
        if (!LabelsEnabled) return;
        string label = KeyLabels.Get(vk);
        if (label == null) return;
        if (caps.Count >= 9) caps.RemoveAt(0);
        caps.Add(new Cap { Label = label, Time = now,
            Start = new Point(target.X + random.Next(-5, 6), target.Y - 14),
            Angle = random.Next(-7, 8) });
    }

    public void Pointer(Point p, bool buttonDown, double now)
    {
        Pointer(p, buttonDown, false, now, new Vector(Math.Tanh((p.X - 214) / 600), Math.Tanh((p.Y - 150) / 400)));
    }

    public void Pointer(Point p, bool buttonDown, double now, Vector positionOnMonitor)
    {
        Pointer(p, buttonDown, false, now, positionOnMonitor);
    }

    public void Pointer(Point p, bool physicalLeft, bool physicalRight, double now, Vector positionOnMonitor)
    {
        Vector delta = p - lastPointer;
        if (pointerKnown && delta.LengthSquared > .000001)
        {
            localTravel.X = Clamp(localTravel.X + delta.X * .13, -5.5, 5.5);
            localTravel.Y = Clamp(localTravel.Y + delta.Y * .13, -6, 6);
            MarkMouseActivity(now);
        }
        monitorPosition = positionOnMonitor;
        bool onHead = CatArt.ContainsHead(Cat,p,time,gazeX,gazeY,left);
        if (onHead && !headHovered) lastPet = now;
        headHovered = onHead;
        pointerKnown = true;
        pointer = lastPointer = p;
        MouseButton(1, physicalLeft, now);
        MouseButton(2, physicalRight, now);
    }

    public void MouseButton(int virtualKey, bool down, double now)
    {
        if (virtualKey == 1)
        {
            if (leftDown == down) return;
            MarkMouseActivity(now);
            leftDown = down;
            if (down) leftClickAt = now;
        }
        else if (virtualKey == 2)
        {
            if (rightDown == down) return;
            MarkMouseActivity(now);
            rightDown = down;
            if (down) rightClickAt = now;
        }
        if (down && headHovered) lastPet = now;
    }

    private void MarkMouseActivity(double now)
    {
        // A key-down can arrive just before the next cursor sample.
        if (!UsingMouse(now) && leftAt > rightAt && now - leftAt < .24)
        {
            rightKey = leftKey; rightAt = leftAt; leftAt = -10;
        }
        lastMove = now;
    }

    private bool UsingMouse(double now) { return leftDown || rightDown || now - lastMove < .83; }
    private double ClickGlow(bool down, double onset)
    {
        // A tap that starts and ends between animation frames still gets a visible press.
        return down ? 1 : 1 - Smooth((time - onset - .10) / .14);
    }
    private static double Clamp(double value, double low, double high) { return Math.Max(low, Math.Min(high, value)); }

    public void Advance(double now)
    {
        double dt = Math.Max(0.001, Math.Min(0.1, now - time));
        time = now;
        if (now > nextBlink + 0.30) nextBlink = now + 2.6 + random.NextDouble() * 3.7;
        blink = Math.Max(Pulse(now - nextBlink, .23), Pulse(now - lastPet, .59));
        double typing = 1 - Smooth((now - lastKey - .95) / .48);
        double hold = leftDown || rightDown ? 1 : 1 - Smooth((now - lastMove - .55) / .28);
        grip += Clamp(hold - grip, -dt * 10, dt * 12);
        if (hold == 0) localTravel *= Math.Exp(-dt * 8);
        // Slow sweeps follow the position on the monitor; small reversals also
        // move the miniature mouse without requiring a sweep across the screen.
        Vector travel = new Vector(monitorPosition.X * 5 + localTravel.X, monitorPosition.Y * 5 + localTravel.Y);
        mouseOffset += (travel * grip - mouseOffset) * (1 - Math.Exp(-dt * 32));
        Point leftRest = Mix(IdleLeft, new Point(188, 189), typing);
        Point rightRest = Mix(IdleRight, new Point(233, 193), typing);
        double la = Beat(now - leftAt), ra = Beat(now - rightAt);
        leftRest = Mix(leftRest, leftKey, la);
        rightRest = Mix(rightRest, rightKey, ra);
        Point mousePaw = MouseArt.PawTarget + mouseOffset;
        double leftClick = ClickGlow(leftDown, leftClickAt), rightClick = ClickGlow(rightDown, rightClickAt);
        mousePaw.X += (leftClick - rightClick) * 1.6;
        mousePaw.Y += 2.6 * Math.Max(leftClick, rightClick);
        double armEase = 1 - Math.Exp(-dt * 46);
        freeLeft = Mix(freeLeft, leftRest, armEase);
        left = Mix(freeLeft, mousePaw, grip);
        right = Mix(right, rightRest, armEase);
        leftActivity = Math.Max(typing, grip);
        rightActivity = typing;
        double tx = pointerKnown ? Math.Tanh((pointer.X - 214) / 155) : 0;
        double ty = pointerKnown ? Math.Tanh((pointer.Y - 85) / 155) : 0;
        double watchingKeys = typing * (1 - grip * .55);
        double currentKeyX = leftAt >= rightAt ? leftKey.X : rightKey.X;
        tx += (Math.Tanh((currentKeyX - 214) / 130) - tx) * watchingKeys;
        ty += (1 - ty) * watchingKeys;
        double eyeEase = 1 - Math.Exp(-dt * 14);
        gazeX += (tx - gazeX) * eyeEase;
        gazeY += (ty - gazeY) * eyeEase;
        for (int i = caps.Count - 1; i >= 0; i--)
        {
            // The newest key drifts over to the tail and lingers like the reference.
            double lifetime = i == caps.Count - 1 ? 5.8 : .89;
            if (now - caps[i].Time > lifetime) caps.RemoveAt(i);
        }
        highlights.Clear();
        expired.Clear();
        foreach (var pair in strikes)
        {
            double age = now - pair.Value;
            if (age > .43) expired.Add(pair.Key);
            else highlights[pair.Key] = 1 - Smooth(age / .43);
        }
        foreach (int key in expired) strikes.Remove(key);
        InvalidateVisual();
    }

    private static double Beat(double age)
    {
        if (age < 0 || age > .24) return 0;
        return age < .065 ? 1 : 1 - Smooth((age - .065) / .175);
    }
    private static double Pulse(double age, double duration)
    {
        if (age < 0 || age > duration) return 0;
        return Math.Min(1, Math.Sin(Math.PI * age / duration) * 1.4);
    }
    private static double Smooth(double a)
    {
        a = Math.Max(0, Math.Min(1, a));
        return a * a * (3 - 2 * a);
    }
    private static Point Mix(Point a, Point b, double k) { return a + (b - a) * k; }

    protected override void OnRender(DrawingContext dc)
    {
        dc.PushTransform(new ScaleTransform(ActualWidth / CanvasWidth, ActualHeight / CanvasHeight));
        DeskArt.DrawDesk(dc, highlights, ClickGlow(leftDown, leftClickAt), ClickGlow(rightDown, rightClickAt),
            Keyboard, Mouse, mouseOffset);
        CatArt.DrawStyled(dc, Cat, time, gazeX, gazeY, blink, left, right, leftActivity, rightActivity);
        for (int i = 0; i < caps.Count; i++)
        {
            Cap cap = caps[i];
            double age = Math.Max(0, time - cap.Time);
            double progress = age / .89;
            double travel = 1 - Math.Pow(1 - Math.Min(1, Math.Max(0, progress)), 2);
            Point center = new Point(cap.Start.X + cap.Angle * travel, cap.Start.Y - 53 * travel);
            bool newest = i == caps.Count - 1;
            double settle = newest ? Smooth((age - .60) / .42) : 0;
            center = Mix(center, new Point(298, 214), settle);
            double opacity = newest ? 1 - Smooth((age - 5.4) / .4) : 1 - Smooth((progress - .55) / .45);
            DeskArt.DrawCap(dc, cap.Label, center, opacity, cap.Angle * travel * (1 - settle));
        }
        dc.Pop();
    }
}

internal static class KeyLabels
{
    public static string Get(int vk)
    {
        if ((vk >= 48 && vk <= 57) || (vk >= 65 && vk <= 90)) return ((char)vk).ToString();
        if (vk >= 96 && vk <= 105) return (vk - 96).ToString();
        if (vk >= 112 && vk <= 135) return "F" + (vk - 111);
        switch (vk)
        {
            case 8: return "⌫"; case 9: return "Tab"; case 13: return "↵";
            case 16: case 160: case 161: return "⇧";
            case 17: case 162: case 163: return "Ctrl";
            case 18: case 164: case 165: return "Alt";
            case 19: return "Pause"; case 20: return "Caps"; case 27: return "Esc";
            case 32: return "␣"; case 33: return "PgUp"; case 34: return "PgDn";
            case 35: return "End"; case 36: return "Home"; case 37: return "←";
            case 38: return "↑"; case 39: return "→"; case 40: return "↓";
            case 44: return "Prt"; case 45: return "Ins"; case 46: return "Del";
            case 91: case 92: return "Win"; case 93: return "Menu";
            case 106: return "×"; case 107: return "+"; case 109: return "−";
            case 110: case 190: return "."; case 111: case 191: return "/";
            case 144: return "Num"; case 145: return "Scr";
            case 186: return ";"; case 187: return "="; case 188: return ",";
            case 189: return "−"; case 192: return "`"; case 219: return "[";
            case 220: case 226: return "\\"; case 221: return "]"; case 222: return "'";
            default: return null;
        }
    }
}
