using SixLabors.ImageSharp;

namespace Levels.Models;

public class Offset2D(int x = 0, int y = 0)
{
    public int X = x;
    public int Y = y;

    public static implicit operator Point(Offset2D o)
    {
        return new(o.X, o.Y);
    }

    public static explicit operator Offset2D(Point p)
    {
        return new(p.X, p.Y);
    }

    public static Offset2D operator +(Offset2D o, int n)
    {
        return new(o.X + n, o.Y + n);
    }

    public static Offset2D operator -(Offset2D o, int n)
    {
        return new(o.X - n, o.Y - n);
    }

    public override string ToString() => $"({X}, {Y})";
}
