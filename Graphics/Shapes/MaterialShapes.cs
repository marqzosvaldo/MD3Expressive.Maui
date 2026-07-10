using System;
using System.Collections.Generic;

namespace SkiaMD3Expressive.Maui.Graphics.Shapes
{
    public static class MaterialShapes
    {
        private struct PointNRound
        {
            public Point O { get; }
            public CornerRounding R { get; }

            public PointNRound(Point o, CornerRounding? r = null)
            {
                O = o;
                R = r ?? CornerRounding.Unrounded;
            }
        }

        private static float AngleDegrees(Point p) => (float)(Math.Atan2(p.Y, p.X) * 180f / Math.PI);
        private static float ToRadians(float deg) => deg / 360f * 2f * (float)Math.PI;

        private static Point RotateDegrees(Point p, float angle, Point center)
        {
            float a = ToRadians(angle);
            var off = p - center;
            return new Point(
                off.X * (float)Math.Cos(a) - off.Y * (float)Math.Sin(a),
                off.X * (float)Math.Sin(a) + off.Y * (float)Math.Cos(a)
            ) + center;
        }

        private static List<PointNRound> DoRepeat(
            List<PointNRound> points,
            int reps,
            Point center,
            bool mirroring)
        {
            if (mirroring)
            {
                var list = new List<PointNRound>();
                var angles = new List<float>();
                var distances = new List<float>();

                foreach (var p in points)
                {
                    var offset = p.O - center;
                    angles.Add(AngleDegrees(offset));
                    distances.Add(offset.GetDistance());
                }

                int actualReps = reps * 2;
                float sectionAngle = 360f / actualReps;

                for (int it = 0; it < actualReps; it++)
                {
                    for (int index = 0; index < points.Count; index++)
                    {
                        int i = (it % 2 == 0) ? index : points.Count - 1 - index;
                        if (i > 0 || it % 2 == 0)
                        {
                            float a = sectionAngle * it + ((it % 2 == 0) ? angles[i] : sectionAngle - angles[i] + 2f * angles[0]);
                            float aRad = ToRadians(a);
                            var finalPoint = new Point((float)Math.Cos(aRad), (float)Math.Sin(aRad)) * distances[i] + center;
                            list.Add(new PointNRound(finalPoint, points[i].R));
                        }
                    }
                }
                return list;
            }
            else
            {
                var list = new List<PointNRound>();
                int np = points.Count;
                for (int it = 0; it < np * reps; it++)
                {
                    var point = RotateDegrees(points[it % np].O, (it / np) * 360f / reps, center);
                    list.Add(new PointNRound(point, points[it % np].R));
                }
                return list;
            }
        }

        private static RoundedPolygon CustomPolygon(
            List<PointNRound> pnr,
            int reps,
            Point? center = null,
            bool mirroring = false)
        {
            var actualCenter = center ?? new Point(0.5f, 0.5f);
            var actualPoints = DoRepeat(pnr, reps, actualCenter, mirroring);
            var vertices = new float[actualPoints.Count * 2];
            var perVertexRounding = new List<CornerRounding>();

            for (int i = 0; i < actualPoints.Count; i++)
            {
                vertices[i * 2] = actualPoints[i].O.X;
                vertices[i * 2 + 1] = actualPoints[i].O.Y;
                perVertexRounding.Add(actualPoints[i].R ?? CornerRounding.Unrounded);
            }

            return RoundedPolygon.Create(vertices, CornerRounding.Unrounded, perVertexRounding, actualCenter.X, actualCenter.Y);
        }

        // Cached Rounding parameters
        private static readonly CornerRounding CornerRound15 = new CornerRounding(0.15f);
        private static readonly CornerRounding CornerRound20 = new CornerRounding(0.2f);
        private static readonly CornerRounding CornerRound30 = new CornerRounding(0.3f);
        private static readonly CornerRounding CornerRound50 = new CornerRounding(0.5f);
        private static readonly CornerRounding CornerRound100 = new CornerRounding(1f);

        // Predefined Material Shapes Fields
        private static RoundedPolygon _circle;
        private static RoundedPolygon _square;
        private static RoundedPolygon _slanted;
        private static RoundedPolygon _arch;
        private static RoundedPolygon _fan;
        private static RoundedPolygon _arrow;
        private static RoundedPolygon _semiCircle;
        private static RoundedPolygon _oval;
        private static RoundedPolygon _pill;
        private static RoundedPolygon _triangle;
        private static RoundedPolygon _diamond;
        private static RoundedPolygon _clamShell;
        private static RoundedPolygon _pentagon;
        private static RoundedPolygon _gem;
        private static RoundedPolygon _sunny;
        private static RoundedPolygon _verySunny;
        private static RoundedPolygon _cookie4Sided;
        private static RoundedPolygon _cookie6Sided;
        private static RoundedPolygon _cookie7Sided;
        private static RoundedPolygon _cookie9Sided;
        private static RoundedPolygon _cookie12Sided;
        private static RoundedPolygon _ghostish;
        private static RoundedPolygon _clover4Leaf;
        private static RoundedPolygon _clover8Leaf;
        private static RoundedPolygon _burst;
        private static RoundedPolygon _softBurst;
        private static RoundedPolygon _boom;
        private static RoundedPolygon _softBoom;
        private static RoundedPolygon _flower;
        private static RoundedPolygon _puffy;
        private static RoundedPolygon _puffyDiamond;
        private static RoundedPolygon _pixelCircle;
        private static RoundedPolygon _pixelTriangle;
        private static RoundedPolygon _bun;
        private static RoundedPolygon _heart;

        // Public static accessors for the 35 shapes
        public static RoundedPolygon Circle => _circle ??= 
            RoundedPolygon.Circle(numVertices: 10).Normalized();

        public static RoundedPolygon Square => _square ??= 
            RoundedPolygon.Rectangle(width: 1f, height: 1f, rounding: CornerRound30).Normalized();

        public static RoundedPolygon Slanted => _slanted ??= 
            CustomPolygon(
                new List<PointNRound>
                {
                    new PointNRound(new Point(0.926f, 0.970f), new CornerRounding(0.189f, 0.811f)),
                    new PointNRound(new Point(-0.021f, 0.967f), new CornerRounding(0.187f, 0.057f))
                },
                reps: 2
            ).Normalized();

        public static RoundedPolygon Arch => _arch ??= 
            RoundedPolygon.Create(
                numVertices: 4,
                radius: 1f,
                perVertexRounding: new List<CornerRounding> { CornerRound100, CornerRound100, CornerRound20, CornerRound20 }
            ).Transformed((x, y) => { var pt = RotateDegrees(new Point(x, y), -135f, Utils.Zero); return (pt.X, pt.Y); }).Normalized();

        public static RoundedPolygon Fan => _fan ??= 
            CustomPolygon(
                new List<PointNRound>
                {
                    new PointNRound(new Point(1.004f, 1.000f), new CornerRounding(0.148f, 0.417f)),
                    new PointNRound(new Point(0.000f, 1.000f), new CornerRounding(0.151f)),
                    new PointNRound(new Point(0.000f, -0.003f), new CornerRounding(0.148f)),
                    new PointNRound(new Point(0.978f, 0.020f), new CornerRounding(0.803f))
                },
                reps: 1
            ).Normalized();

        public static RoundedPolygon Arrow => _arrow ??= 
            CustomPolygon(
                new List<PointNRound>
                {
                    new PointNRound(new Point(0.500f, 0.892f), new CornerRounding(0.313f)),
                    new PointNRound(new Point(-0.216f, 1.050f), new CornerRounding(0.207f)),
                    new PointNRound(new Point(0.499f, -0.160f), new CornerRounding(0.215f, 1.000f)),
                    new PointNRound(new Point(1.225f, 1.060f), new CornerRounding(0.211f))
                },
                reps: 1
            ).Normalized();

        public static RoundedPolygon SemiCircle => _semiCircle ??= 
            RoundedPolygon.Rectangle(
                width: 1.6f,
                height: 1f,
                perVertexRounding: new List<CornerRounding> { CornerRound20, CornerRound20, CornerRound100, CornerRound100 }
            ).Normalized();

        public static RoundedPolygon Oval => _oval ??= 
            RoundedPolygon.Circle(numVertices: 8)
                .Transformed((x, y) => (x, y * 0.64f))
                .Transformed((x, y) => { var pt = RotateDegrees(new Point(x, y), -45f, Utils.Zero); return (pt.X, pt.Y); })
                .Normalized();

        public static RoundedPolygon Pill => _pill ??= 
            CustomPolygon(
                new List<PointNRound>
                {
                    new PointNRound(new Point(0.961f, 0.039f), new CornerRounding(0.426f)),
                    new PointNRound(new Point(1.001f, 0.428f)),
                    new PointNRound(new Point(1.000f, 0.609f), CornerRound100)
                },
                reps: 2,
                mirroring: true
            ).Normalized();

        public static RoundedPolygon Triangle => _triangle ??= 
            RoundedPolygon.Create(numVertices: 3, rounding: CornerRound20)
                .Transformed((x, y) => { var pt = RotateDegrees(new Point(x, y), -90f, Utils.Zero); return (pt.X, pt.Y); }).Normalized();

        public static RoundedPolygon Diamond => _diamond ??= 
            CustomPolygon(
                new List<PointNRound>
                {
                    new PointNRound(new Point(0.500f, 1.096f), new CornerRounding(0.151f, 0.524f)),
                    new PointNRound(new Point(0.040f, 0.500f), new CornerRounding(0.159f))
                },
                reps: 2
            ).Normalized();

        public static RoundedPolygon ClamShell => _clamShell ??= 
            CustomPolygon(
                new List<PointNRound>
                {
                    new PointNRound(new Point(0.171f, 0.841f), new CornerRounding(0.159f)),
                    new PointNRound(new Point(-0.020f, 0.500f), new CornerRounding(0.140f)),
                    new PointNRound(new Point(0.170f, 0.159f), new CornerRounding(0.159f))
                },
                reps: 2
            ).Normalized();

        public static RoundedPolygon Pentagon => _pentagon ??= 
            CustomPolygon(
                new List<PointNRound>
                {
                    new PointNRound(new Point(0.500f, -0.009f), new CornerRounding(0.172f)),
                    new PointNRound(new Point(1.030f, 0.365f), new CornerRounding(0.164f)),
                    new PointNRound(new Point(0.828f, 0.970f), new CornerRounding(0.169f))
                },
                reps: 1,
                mirroring: true
            ).Normalized();

        public static RoundedPolygon Gem => _gem ??= 
            CustomPolygon(
                new List<PointNRound>
                {
                    new PointNRound(new Point(0.499f, 1.023f), new CornerRounding(0.241f, 0.778f)),
                    new PointNRound(new Point(-0.005f, 0.792f), new CornerRounding(0.208f)),
                    new PointNRound(new Point(0.073f, 0.258f), new CornerRounding(0.228f)),
                    new PointNRound(new Point(0.433f, -0.000f), new CornerRounding(0.491f))
                },
                reps: 1,
                mirroring: true
            ).Normalized();

        public static RoundedPolygon Sunny => _sunny ??= 
            RoundedPolygon.Star(
                numVerticesPerRadius: 8,
                radius: 1f,
                innerRadius: 0.8f,
                rounding: CornerRound15
            ).Normalized();

        public static RoundedPolygon VerySunny => _verySunny ??= 
            CustomPolygon(
                new List<PointNRound>
                {
                    new PointNRound(new Point(0.500f, 1.080f), new CornerRounding(0.085f)),
                    new PointNRound(new Point(0.358f, 0.843f), new CornerRounding(0.085f))
                },
                reps: 8
            ).Normalized();

        public static RoundedPolygon Cookie4Sided => _cookie4Sided ??= 
            CustomPolygon(
                new List<PointNRound>
                {
                    new PointNRound(new Point(1.237f, 1.236f), new CornerRounding(0.258f)),
                    new PointNRound(new Point(0.500f, 0.918f), new CornerRounding(0.233f))
                },
                reps: 4
            ).Normalized();

        public static RoundedPolygon Cookie6Sided => _cookie6Sided ??= 
            CustomPolygon(
                new List<PointNRound>
                {
                    new PointNRound(new Point(0.723f, 0.884f), new CornerRounding(0.394f)),
                    new PointNRound(new Point(0.500f, 1.099f), new CornerRounding(0.398f))
                },
                reps: 6
            ).Normalized();

        public static RoundedPolygon Cookie7Sided => _cookie7Sided ??= 
            RoundedPolygon.Star(
                numVerticesPerRadius: 7,
                radius: 1f,
                innerRadius: 0.75f,
                rounding: CornerRound50
            ).Transformed((x, y) => { var pt = RotateDegrees(new Point(x, y), -90f, Utils.Zero); return (pt.X, pt.Y); }).Normalized();

        public static RoundedPolygon Cookie9Sided => _cookie9Sided ??= 
            RoundedPolygon.Star(
                numVerticesPerRadius: 9,
                radius: 1f,
                innerRadius: 0.8f,
                rounding: CornerRound50
            ).Transformed((x, y) => { var pt = RotateDegrees(new Point(x, y), -90f, Utils.Zero); return (pt.X, pt.Y); }).Normalized();

        public static RoundedPolygon Cookie12Sided => _cookie12Sided ??= 
            RoundedPolygon.Star(
                numVerticesPerRadius: 12,
                radius: 1f,
                innerRadius: 0.8f,
                rounding: CornerRound50
            ).Transformed((x, y) => { var pt = RotateDegrees(new Point(x, y), -90f, Utils.Zero); return (pt.X, pt.Y); }).Normalized();

        public static RoundedPolygon Ghostish => _ghostish ??= 
            CustomPolygon(
                new List<PointNRound>
                {
                    new PointNRound(new Point(0.500f, 0f), CornerRound100),
                    new PointNRound(new Point(1f, 0f), CornerRound100),
                    new PointNRound(new Point(1f, 1.140f), new CornerRounding(0.254f, 0.106f)),
                    new PointNRound(new Point(0.575f, 0.906f), new CornerRounding(0.253f))
                },
                reps: 1,
                mirroring: true
            ).Normalized();

        public static RoundedPolygon Clover4Leaf => _clover4Leaf ??= 
            CustomPolygon(
                new List<PointNRound>
                {
                    new PointNRound(new Point(0.500f, 0.074f)),
                    new PointNRound(new Point(0.725f, -0.099f), new CornerRounding(0.476f))
                },
                reps: 4,
                mirroring: true
            ).Normalized();

        public static RoundedPolygon Clover8Leaf => _clover8Leaf ??= 
            CustomPolygon(
                new List<PointNRound>
                {
                    new PointNRound(new Point(0.500f, 0.036f)),
                    new PointNRound(new Point(0.758f, -0.101f), new CornerRounding(0.209f))
                },
                reps: 8
            ).Normalized();

        public static RoundedPolygon Burst => _burst ??= 
            CustomPolygon(
                new List<PointNRound>
                {
                    new PointNRound(new Point(0.500f, -0.006f), new CornerRounding(0.006f)),
                    new PointNRound(new Point(0.592f, 0.158f), new CornerRounding(0.006f))
                },
                reps: 12
            ).Normalized();

        public static RoundedPolygon SoftBurst => _softBurst ??= 
            CustomPolygon(
                new List<PointNRound>
                {
                    new PointNRound(new Point(0.193f, 0.277f), new CornerRounding(0.053f)),
                    new PointNRound(new Point(0.176f, 0.055f), new CornerRounding(0.053f))
                },
                reps: 10
            ).Normalized();

        public static RoundedPolygon Boom => _boom ??= 
            CustomPolygon(
                new List<PointNRound>
                {
                    new PointNRound(new Point(0.457f, 0.296f), new CornerRounding(0.007f)),
                    new PointNRound(new Point(0.500f, -0.051f), new CornerRounding(0.007f))
                },
                reps: 15
            ).Normalized();

        public static RoundedPolygon SoftBoom => _softBoom ??= 
            CustomPolygon(
                new List<PointNRound>
                {
                    new PointNRound(new Point(0.733f, 0.454f)),
                    new PointNRound(new Point(0.839f, 0.437f), new CornerRounding(0.532f)),
                    new PointNRound(new Point(0.949f, 0.449f), new CornerRounding(0.439f, 1.000f)),
                    new PointNRound(new Point(0.998f, 0.478f), new CornerRounding(0.174f))
                },
                reps: 16,
                mirroring: true
            ).Normalized();

        public static RoundedPolygon Flower => _flower ??= 
            CustomPolygon(
                new List<PointNRound>
                {
                    new PointNRound(new Point(0.370f, 0.187f)),
                    new PointNRound(new Point(0.416f, 0.049f), new CornerRounding(0.381f)),
                    new PointNRound(new Point(0.479f, 0.001f), new CornerRounding(0.095f))
                },
                reps: 8,
                mirroring: true
            ).Normalized();

        public static RoundedPolygon Puffy => _puffy ??= 
            CustomPolygon(
                new List<PointNRound>
                {
                    new PointNRound(new Point(0.500f, 0.053f)),
                    new PointNRound(new Point(0.545f, -0.040f), new CornerRounding(0.405f)),
                    new PointNRound(new Point(0.670f, -0.035f), new CornerRounding(0.426f)),
                    new PointNRound(new Point(0.717f, 0.066f), new CornerRounding(0.574f)),
                    new PointNRound(new Point(0.722f, 0.128f)),
                    new PointNRound(new Point(0.777f, 0.002f), new CornerRounding(0.360f)),
                    new PointNRound(new Point(0.914f, 0.149f), new CornerRounding(0.660f)),
                    new PointNRound(new Point(0.926f, 0.289f), new CornerRounding(0.660f)),
                    new PointNRound(new Point(0.881f, 0.346f)),
                    new PointNRound(new Point(0.940f, 0.344f), new CornerRounding(0.126f)),
                    new PointNRound(new Point(1.003f, 0.437f), new CornerRounding(0.255f))
                },
                reps: 2,
                mirroring: true
            ).Transformed((x, y) => (x, y * 0.742f)).Normalized();

        public static RoundedPolygon PuffyDiamond => _puffyDiamond ??= 
            CustomPolygon(
                new List<PointNRound>
                {
                    new PointNRound(new Point(0.870f, 0.130f), new CornerRounding(0.146f)),
                    new PointNRound(new Point(0.818f, 0.357f)),
                    new PointNRound(new Point(1.000f, 0.332f), new CornerRounding(0.853f))
                },
                reps: 4,
                mirroring: true
            ).Normalized();

        public static RoundedPolygon PixelCircle => _pixelCircle ??= 
            RoundedPolygon.Create(new float[] {
                253.556f, 34f,
                253.556f, 54.2217f,
                296.889f, 54.2217f,
                296.889f, 80.2222f,
                322.889f, 80.2222f,
                322.889f, 126.444f,
                346f, 126.444f,
                346f, 253.556f,
                322.889f, 253.556f,
                322.889f, 299.778f,
                296.889f, 299.778f,
                296.889f, 325.777f,
                253.556f, 325.777f,
                253.556f, 346f,
                126.444f, 346f,
                126.444f, 325.777f,
                83.1111f, 325.777f,
                83.1111f, 299.778f,
                57.1111f, 299.778f,
                57.1111f, 253.556f,
                34f, 253.556f,
                34f, 126.444f,
                57.1111f, 126.444f,
                57.1111f, 80.2222f,
                83.1111f, 80.2222f,
                83.1111f, 54.2217f,
                126.444f, 54.2217f,
                126.444f, 34f
            }).Normalized();

        public static RoundedPolygon PixelTriangle => _pixelTriangle ??= 
            RoundedPolygon.Create(new float[] {
                121.641f, 30f,
                121.641f, 57.8262f,
                164.564f, 57.8262f,
                164.564f, 84.2609f,
                209.077f, 84.2609f,
                209.077f, 114.869f,
                245.641f, 114.869f,
                245.641f, 139.912f,
                282.205f, 139.912f,
                282.205f, 170.522f,
                314f, 170.522f,
                314f, 209.478f,
                282.205f, 209.478f,
                282.205f, 240.086f,
                245.641f, 240.086f,
                245.641f, 265.13f,
                209.077f, 265.13f,
                209.077f, 295.739f,
                164.564f, 295.739f,
                164.564f, 322.174f,
                121.641f, 322.174f,
                121.641f, 350f,
                66f, 350f,
                66f, 30f
            }).Normalized();

        public static RoundedPolygon Bun => _bun ??= 
            CustomPolygon(
                new List<PointNRound>
                {
                    new PointNRound(new Point(0.796f, 0.500f)),
                    new PointNRound(new Point(0.853f, 0.518f), CornerRound100),
                    new PointNRound(new Point(0.992f, 0.631f), CornerRound100),
                    new PointNRound(new Point(0.968f, 1.000f), CornerRound100)
                },
                reps: 2,
                mirroring: true
            ).Normalized();

        public static RoundedPolygon Heart => _heart ??= 
            CustomPolygon(
                new List<PointNRound>
                {
                    new PointNRound(new Point(0.500f, 0.268f), new CornerRounding(0.016f)),
                    new PointNRound(new Point(0.792f, -0.066f), new CornerRounding(0.958f)),
                    new PointNRound(new Point(1.064f, 0.276f), CornerRound100),
                    new PointNRound(new Point(0.501f, 0.946f), new CornerRounding(0.129f))
                },
                reps: 1,
                mirroring: true
            ).Normalized();
    }
}
