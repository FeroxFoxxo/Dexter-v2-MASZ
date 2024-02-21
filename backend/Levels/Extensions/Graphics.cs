using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;
using System.Numerics;

namespace Levels.Extensions;

public static class Graphics
{
    public static Color ColorFromArgb(this uint code)
    {
        var b = (byte)(code % 256);
        code >>= 8;
        var g = (byte)(code % 256);
        code >>= 8;
        var r = (byte)(code % 256);
        code >>= 8;
        var a = (byte)code;
        return Color.FromRgba(r, g, b, a);
    }

    public static IImageProcessingContext DrawTextInRect(this IImageProcessingContext context, string text,
        RectangleF rect, Font font, Color color = default, HorizontalAlignment horAlignment = HorizontalAlignment.Left,
        VerticalAlignment verAlignment = VerticalAlignment.Top)
    {
        var opts = new RichTextOptions(font)
        {
            HorizontalAlignment = horAlignment,
            VerticalAlignment = verAlignment,
            Origin = new PointF(
                horAlignment switch
                {
                    HorizontalAlignment.Left => rect.Left,
                    HorizontalAlignment.Right => rect.Right,
                    _ => (rect.Left + rect.Right) / 2
                },
                verAlignment switch
                {
                    VerticalAlignment.Top => rect.Top,
                    VerticalAlignment.Bottom => rect.Bottom,
                    _ => (rect.Top + rect.Bottom) / 2
                }
            )
        };
        return context.DrawText(opts, text, color);
    }

    public static IPath RoundedRect(Rectangle bounds, int radius)
    {
        var p1 = new PointF(bounds.Right - radius, bounds.Top);
        var c1 = new PointF(bounds.Right, bounds.Top);
        var p2 = new PointF(bounds.Right, bounds.Top + radius);
        var p3 = new PointF(bounds.Right, bounds.Bottom - radius);
        var c2 = new PointF(bounds.Right, bounds.Bottom);
        var p4 = new PointF(bounds.Right - radius, bounds.Bottom);
        var p5 = new PointF(bounds.Left + radius, bounds.Bottom);
        var c3 = new PointF(bounds.Left, bounds.Bottom);
        var p6 = new PointF(bounds.Left, bounds.Bottom - radius);
        var p7 = new PointF(bounds.Left, bounds.Top + radius);
        var c4 = new PointF(bounds.Left, bounds.Top);
        var p8 = new PointF(bounds.Left + radius, bounds.Top);

        var path = new PathBuilder();

        path.SetOrigin(Point.Empty)
            .AddQuadraticBezier(p1, c1, p2)
            .AddQuadraticBezier(p3, c2, p4)
            .AddQuadraticBezier(p5, c3, p6)
            .AddQuadraticBezier(p7, c4, p8)
            .CloseFigure();

        return path.Build();
    }

    public static float GetBrightness(this Color color)
    {
        var vec4 = (Vector4)color;
        return (vec4.X + vec4.Y + vec4.Z) / 3;
    }
}
