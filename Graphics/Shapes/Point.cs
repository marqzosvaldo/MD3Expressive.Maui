using System;

namespace SkiaMD3Expressive.Maui.Graphics.Shapes
{
    public delegate (float X, float Y) PointTransformer(float x, float y);

    public struct Point
    {
        public float X { get; set; }
        public float Y { get; set; }

        public Point(float x, float y)
        {
            X = x;
            Y = y;
        }

        public readonly float GetDistance() => (float)Math.Sqrt(X * X + Y * Y);
        
        public readonly float GetDistanceSquared() => X * X + Y * Y;
        
        public readonly float DotProduct(Point other) => X * other.X + Y * other.Y;
        
        public readonly float DotProduct(float otherX, float otherY) => X * otherX + Y * otherY;
        
        public readonly bool Clockwise(Point other) => X * other.Y - Y * other.X > 0;
        
        public readonly Point GetDirection()
        {
            float d = GetDistance();
            if (d <= 0f) throw new InvalidOperationException("Can't get the direction of a 0-length vector");
            return this / d;
        }

        public readonly Point Rotate90() => new Point(-Y, X);

        public static Point operator -(Point p) => new Point(-p.X, -p.Y);
        
        public static Point operator -(Point p1, Point p2) => new Point(p1.X - p2.X, p1.Y - p2.Y);
        
        public static Point operator +(Point p1, Point p2) => new Point(p1.X + p2.X, p1.Y + p2.Y);
        
        public static Point operator *(Point p, float operand) => new Point(p.X * operand, p.Y * operand);
        
        public static Point operator /(Point p, float operand) => new Point(p.X / operand, p.Y / operand);
        
        public static Point operator %(Point p, float operand) => new Point(p.X % operand, p.Y % operand);

        public static Point Interpolate(Point start, Point stop, float fraction)
        {
            return new Point(
                Utils.Interpolate(start.X, stop.X, fraction),
                Utils.Interpolate(start.Y, stop.Y, fraction)
            );
        }

        public readonly Point Transformed(PointTransformer transformer)
        {
            var (x, y) = transformer(X, Y);
            return new Point(x, y);
        }

        public override readonly string ToString() => $"({X}, {Y})";
    }
}
