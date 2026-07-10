using System;

namespace SkiaMD3Expressive.Maui.Graphics.Shapes
{
    public static class Utils
    {
        public const float DistanceEpsilon = 1e-4f;
        public const float AngleEpsilon = 1e-6f;
        public const float RelaxedDistanceEpsilon = 5e-3f;

        public static readonly Point Zero = new Point(0f, 0f);
        public const float FloatPi = (float)Math.PI;
        public const float TwoPi = (float)(2 * Math.PI);

        public static float Distance(float x, float y) => (float)Math.Sqrt(x * x + y * y);
        
        public static float DistanceSquared(float x, float y) => x * x + y * y;

        public static Point DirectionVector(float x, float y)
        {
            float d = Distance(x, y);
            if (d <= 0f) throw new ArgumentException("Required distance greater than zero");
            return new Point(x / d, y / d);
        }

        public static Point DirectionVector(float angleRadians) => 
            new Point((float)Math.Cos(angleRadians), (float)Math.Sin(angleRadians));

        public static Point RadialToCartesian(float radius, float angleRadians) =>
            DirectionVector(angleRadians) * radius;

        public static Point RadialToCartesian(float radius, float angleRadians, Point center) =>
            DirectionVector(angleRadians) * radius + center;

        public static float Square(float x) => x * x;

        public static float Interpolate(float start, float stop, float fraction)
        {
            return (1f - fraction) * start + fraction * stop;
        }

        public static float PositiveModulo(float num, float mod)
        {
            return (num % mod + mod) % mod;
        }

        public static bool CollinearIsh(float aX, float aY, float bX, float bY, float cX, float cY, float tolerance = DistanceEpsilon)
        {
            var ab = new Point(bX - aX, bY - aY).Rotate90();
            var ac = new Point(cX - aX, cY - aY);
            float dotProduct = Math.Abs(ab.DotProduct(ac));
            float relativeTolerance = tolerance * ab.GetDistance() * ac.GetDistance();

            return dotProduct < tolerance || dotProduct < relativeTolerance;
        }

        public static bool Convex(Point previous, Point current, Point next)
        {
            return (current - previous).Clockwise(next - current);
        }
    }
}
