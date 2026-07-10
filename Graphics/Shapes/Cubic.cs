using System;

namespace SkiaMD3Expressive.Maui.Graphics.Shapes
{
    public class Cubic
    {
        public float[] Points { get; } = new float[8];

        public float Anchor0X => Points[0];
        public float Anchor0Y => Points[1];
        public float Control0X => Points[2];
        public float Control0Y => Points[3];
        public float Control1X => Points[4];
        public float Control1Y => Points[5];
        public float Anchor1X => Points[6];
        public float Anchor1Y => Points[7];

        public Cubic()
        {
        }

        public Cubic(float[] points)
        {
            if (points.Length != 8)
                throw new ArgumentException("Points array size should be 8");
            Array.Copy(points, Points, 8);
        }

        public Cubic(Point anchor0, Point control0, Point control1, Point anchor1)
            : this(anchor0.X, anchor0.Y, control0.X, control0.Y, control1.X, control1.Y, anchor1.X, anchor1.Y)
        {
        }

        public Cubic(
            float anchor0X, float anchor0Y,
            float control0X, float control0Y,
            float control1X, float control1Y,
            float anchor1X, float anchor1Y)
        {
            Points[0] = anchor0X;
            Points[1] = anchor0Y;
            Points[2] = control0X;
            Points[3] = control0Y;
            Points[4] = control1X;
            Points[5] = control1Y;
            Points[6] = anchor1X;
            Points[7] = anchor1Y;
        }

        public Point PointOnCurve(float t)
        {
            float u = 1f - t;
            return new Point(
                Anchor0X * (u * u * u) +
                Control0X * (3f * t * u * u) +
                Control1X * (3f * t * t * u) +
                Anchor1X * (t * t * t),
                
                Anchor0Y * (u * u * u) +
                Control0Y * (3f * t * u * u) +
                Control1Y * (3f * t * t * u) +
                Anchor1Y * (t * t * t)
            );
        }

        public bool ZeroLength()
        {
            return Math.Abs(Anchor0X - Anchor1X) < Utils.DistanceEpsilon && 
                   Math.Abs(Anchor0Y - Anchor1Y) < Utils.DistanceEpsilon;
        }

        public bool ConvexTo(Cubic next)
        {
            var prevVertex = new Point(Anchor0X, Anchor0Y);
            var currVertex = new Point(Anchor1X, Anchor1Y);
            var nextVertex = new Point(next.Anchor1X, next.Anchor1Y);
            return Utils.Convex(prevVertex, currVertex, nextVertex);
        }

        private bool ZeroIsh(float value) => Math.Abs(value) < Utils.DistanceEpsilon;

        public void CalculateBounds(float[] bounds, bool approximate = false)
        {
            if (bounds.Length < 4)
                throw new ArgumentException("Bounds array size should be at least 4");

            if (ZeroLength())
            {
                bounds[0] = Anchor0X;
                bounds[1] = Anchor0Y;
                bounds[2] = Anchor0X;
                bounds[3] = Anchor0Y;
                return;
            }

            float minX = Math.Min(Anchor0X, Anchor1X);
            float minY = Math.Min(Anchor0Y, Anchor1Y);
            float maxX = Math.Max(Anchor0X, Anchor1X);
            float maxY = Math.Max(Anchor0Y, Anchor1Y);

            if (approximate)
            {
                bounds[0] = Math.Min(minX, Math.Min(Control0X, Control1X));
                bounds[1] = Math.Min(minY, Math.Min(Control0Y, Control1Y));
                bounds[2] = Math.Max(maxX, Math.Max(Control0X, Control1X));
                bounds[3] = Math.Max(maxY, Math.Max(Control0Y, Control1Y));
                return;
            }

            // Solve derivative: X(t) = a*t^3 + b*t^2 + c*t + d
            // Derivative is quadratic: X'(t) = 3*a*t^2 + 2*b*t + c
            // Let's call quadratic parameters: xa, xb, xc
            float xa = -Anchor0X + 3f * Control0X - 3f * Control1X + Anchor1X;
            float xb = 2f * Anchor0X - 4f * Control0X + 2f * Control1X;
            float xc = -Anchor0X + Control0X;

            if (ZeroIsh(xa))
            {
                if (xb != 0f)
                {
                    float t = 2f * xc / (-2f * xb);
                    if (t >= 0f && t <= 1f)
                    {
                        float x = PointOnCurve(t).X;
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                    }
                }
            }
            else
            {
                float xs = xb * xb - 4f * xa * xc;
                if (xs >= 0)
                {
                    float t1 = (-xb + (float)Math.Sqrt(xs)) / (2f * xa);
                    if (t1 >= 0f && t1 <= 1f)
                    {
                        float x = PointOnCurve(t1).X;
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                    }

                    float t2 = (-xb - (float)Math.Sqrt(xs)) / (2f * xa);
                    if (t2 >= 0f && t2 <= 1f)
                    {
                        float x = PointOnCurve(t2).X;
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                    }
                }
            }

            // Repeat for Y coordinate
            float ya = -Anchor0Y + 3f * Control0Y - 3f * Control1Y + Anchor1Y;
            float yb = 2f * Anchor0Y - 4f * Control0Y + 2f * Control1Y;
            float yc = -Anchor0Y + Control0Y;

            if (ZeroIsh(ya))
            {
                if (yb != 0f)
                {
                    float t = 2f * yc / (-2f * yb);
                    if (t >= 0f && t <= 1f)
                    {
                        float y = PointOnCurve(t).Y;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                }
            }
            else
            {
                float ys = yb * yb - 4f * ya * yc;
                if (ys >= 0)
                {
                    float t1 = (-yb + (float)Math.Sqrt(ys)) / (2f * ya);
                    if (t1 >= 0f && t1 <= 1f)
                    {
                        float y = PointOnCurve(t1).Y;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }

                    float t2 = (-yb - (float)Math.Sqrt(ys)) / (2f * ya);
                    if (t2 >= 0f && t2 <= 1f)
                    {
                        float y = PointOnCurve(t2).Y;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                }
            }

            bounds[0] = minX;
            bounds[1] = minY;
            bounds[2] = maxX;
            bounds[3] = maxY;
        }

        public (Cubic Left, Cubic Right) Split(float t)
        {
            float u = 1f - t;
            var pt = PointOnCurve(t);

            var left = new Cubic(
                Anchor0X, Anchor0Y,
                Anchor0X * u + Control0X * t, Anchor0Y * u + Control0Y * t,
                Anchor0X * (u * u) + Control0X * (2f * u * t) + Control1X * (t * t),
                Anchor0Y * (u * u) + Control0Y * (2f * u * t) + Control1Y * (t * t),
                pt.X, pt.Y
            );

            var right = new Cubic(
                pt.X, pt.Y,
                Control0X * (u * u) + Control1X * (2f * u * t) + Anchor1X * (t * t),
                Control0Y * (u * u) + Control1Y * (2f * u * t) + Anchor1Y * (t * t),
                Control1X * u + Anchor1X * t, Control1Y * u + Anchor1Y * t,
                Anchor1X, Anchor1Y
            );

            return (left, right);
        }

        public Cubic Reverse() => new Cubic(
            Anchor1X, Anchor1Y,
            Control1X, Control1Y,
            Control0X, Control0Y,
            Anchor0X, Anchor0Y
        );

        public static Cubic operator +(Cubic c1, Cubic c2)
        {
            var p = new float[8];
            for (int i = 0; i < 8; i++) p[i] = c1.Points[i] + c2.Points[i];
            return new Cubic(p);
        }

        public static Cubic operator *(Cubic c, float scalar)
        {
            var p = new float[8];
            for (int i = 0; i < 8; i++) p[i] = c.Points[i] * scalar;
            return new Cubic(p);
        }

        public static Cubic operator /(Cubic c, float scalar) => c * (1f / scalar);

        public Cubic Transformed(PointTransformer f)
        {
            var result = new MutableCubic();
            Array.Copy(Points, result.Points, 8);
            result.Transform(f);
            return result;
        }

        public override string ToString()
        {
            return $"anchor0: ({Anchor0X}, {Anchor0Y}) control0: ({Control0X}, {Control0Y}), control1: ({Control1X}, {Control1Y}), anchor1: ({Anchor1X}, {Anchor1Y})";
        }

        public static Cubic StraightLine(float x0, float y0, float x1, float y1)
        {
            return new Cubic(
                x0, y0,
                Utils.Interpolate(x0, x1, 1f / 3f), Utils.Interpolate(y0, y1, 1f / 3f),
                Utils.Interpolate(x0, x1, 2f / 3f), Utils.Interpolate(y0, y1, 2f / 3f),
                x1, y1
            );
        }

        public static Cubic CircularArc(
            float centerX, float centerY,
            float x0, float y0,
            float x1, float y1)
        {
            var p0d = Utils.DirectionVector(x0 - centerX, y0 - centerY);
            var p1d = Utils.DirectionVector(x1 - centerX, y1 - centerY);
            var rotatedP0 = p0d.Rotate90();
            var rotatedP1 = p1d.Rotate90();
            
            bool clockwise = rotatedP0.DotProduct(x1 - centerX, y1 - centerY) >= 0f;
            float cosa = p0d.DotProduct(p1d);
            
            if (cosa > 0.999f) 
                return StraightLine(x0, y0, x1, y1);

            float k = Utils.Distance(x0 - centerX, y0 - centerY) * 4f / 3f *
                      ((float)Math.Sqrt(2f * (1f - cosa)) - (float)Math.Sqrt(1f - cosa * cosa)) / (1f - cosa) *
                      (clockwise ? 1f : -1f);

            return new Cubic(
                x0, y0,
                x0 + rotatedP0.X * k, y0 + rotatedP0.Y * k,
                x1 - rotatedP1.X * k, y1 - rotatedP1.Y * k,
                x1, y1
            );
        }

        public static Cubic Empty(float x0, float y0) => new Cubic(x0, y0, x0, y0, x0, y0, x0, y0);
    }

    public class MutableCubic : Cubic
    {
        public MutableCubic() : base()
        {
        }

        public void Transform(PointTransformer f)
        {
            var r0 = f(Points[0], Points[1]);
            Points[0] = r0.X;
            Points[1] = r0.Y;

            var r1 = f(Points[2], Points[3]);
            Points[2] = r1.X;
            Points[3] = r1.Y;

            var r2 = f(Points[4], Points[5]);
            Points[4] = r2.X;
            Points[5] = r2.Y;

            var r3 = f(Points[6], Points[7]);
            Points[6] = r3.X;
            Points[7] = r3.Y;
        }

        public void Interpolate(Cubic c1, Cubic c2, float progress)
        {
            for (int i = 0; i < 8; i++)
            {
                Points[i] = Utils.Interpolate(c1.Points[i], c2.Points[i], progress);
            }
        }
    }
}
