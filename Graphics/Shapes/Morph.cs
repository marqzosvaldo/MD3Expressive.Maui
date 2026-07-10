using System;
using System.Collections.Generic;

namespace SkiaMD3Expressive.Maui.Graphics.Shapes
{
    public class Morph
    {
        public RoundedPolygon Start { get; }
        public RoundedPolygon End { get; }

        public List<(Cubic StartCubic, Cubic EndCubic)> MorphMatch { get; }

        public Morph(RoundedPolygon start, RoundedPolygon end)
        {
            Start = start;
            End = end;
            MorphMatch = Match(start, end);
        }

        public float[] CalculateBounds(float[] bounds, bool approximate = true)
        {
            if (bounds.Length < 4)
                throw new ArgumentException("Required bounds size of 4");

            var startBounds = new float[4];
            var endBounds = new float[4];
            
            Start.CalculateBounds(startBounds, approximate);
            End.CalculateBounds(endBounds, approximate);

            bounds[0] = Math.Min(startBounds[0], endBounds[0]);
            bounds[1] = Math.Min(startBounds[1], endBounds[1]);
            bounds[2] = Math.Max(startBounds[2], endBounds[2]);
            bounds[3] = Math.Max(startBounds[3], endBounds[3]);
            return bounds;
        }

        public float[] CalculateMaxBounds(float[] bounds)
        {
            if (bounds.Length < 4)
                throw new ArgumentException("Required bounds size of 4");

            var startBounds = new float[4];
            var endBounds = new float[4];

            Start.CalculateMaxBounds(startBounds);
            End.CalculateMaxBounds(endBounds);

            bounds[0] = Math.Min(startBounds[0], endBounds[0]);
            bounds[1] = Math.Min(startBounds[1], endBounds[1]);
            bounds[2] = Math.Max(startBounds[2], endBounds[2]);
            bounds[3] = Math.Max(startBounds[3], endBounds[3]);
            return bounds;
        }

        public List<Cubic> AsCubics(float progress)
        {
            var list = new List<Cubic>();
            Cubic firstCubic = null;
            Cubic lastCubic = null;

            for (int i = 0; i < MorphMatch.Count; i++)
            {
                var points = new float[8];
                for (int j = 0; j < 8; j++)
                {
                    points[j] = Utils.Interpolate(
                        MorphMatch[i].StartCubic.Points[j],
                        MorphMatch[i].EndCubic.Points[j],
                        progress
                    );
                }

                var cubic = new Cubic(points);
                if (firstCubic == null) firstCubic = cubic;
                if (lastCubic != null) list.Add(lastCubic);
                lastCubic = cubic;
            }

            if (lastCubic != null && firstCubic != null)
            {
                list.Add(new Cubic(
                    lastCubic.Anchor0X, lastCubic.Anchor0Y,
                    lastCubic.Control0X, lastCubic.Control0Y,
                    lastCubic.Control1X, lastCubic.Control1Y,
                    firstCubic.Anchor0X, firstCubic.Anchor0Y
                ));
            }

            return list;
        }

        public void ForEachCubic(float progress, MutableCubic mutableCubic, Action<MutableCubic> callback)
        {
            for (int i = 0; i < MorphMatch.Count; i++)
            {
                mutableCubic.Interpolate(MorphMatch[i].StartCubic, MorphMatch[i].EndCubic, progress);
                callback(mutableCubic);
            }
        }

        private static List<(Cubic, Cubic)> Match(RoundedPolygon p1, RoundedPolygon p2)
        {
            var measurer = new LengthMeasurer();
            var measuredPolygon1 = MeasuredPolygon.MeasurePolygon(measurer, p1);
            var measuredPolygon2 = MeasuredPolygon.MeasurePolygon(measurer, p2);

            var doubleMapper = FeatureMapping.FeatureMapper(measuredPolygon1.Features, measuredPolygon2.Features);
            float polygon2CutPoint = doubleMapper.Map(0f);

            var bs1 = measuredPolygon1;
            var bs2 = measuredPolygon2.CutAndShift(polygon2CutPoint);

            var ret = new List<(Cubic, Cubic)>();
            int i1 = 0;
            int i2 = 0;

            var b1 = i1 < bs1.Count ? bs1.Cubics[i1++] : null;
            var b2 = i2 < bs2.Count ? bs2.Cubics[i2++] : null;

            while (b1 != null && b2 != null)
            {
                float b1a = i1 == bs1.Count ? 1f : b1.EndOutlineProgress;
                float b2a = i2 == bs2.Count 
                    ? 1f 
                    : doubleMapper.MapBack(Utils.PositiveModulo(b2.EndOutlineProgress + polygon2CutPoint, 1f));
                
                float minb = Math.Min(b1a, b2a);

                MeasuredPolygon.MeasuredCubic seg1, newb1;
                if (b1a > minb + Utils.AngleEpsilon)
                {
                    var cut = b1.CutAtProgress(minb);
                    seg1 = cut.Left;
                    newb1 = cut.Right;
                }
                else
                {
                    seg1 = b1;
                    newb1 = i1 < bs1.Count ? bs1.Cubics[i1++] : null;
                }

                MeasuredPolygon.MeasuredCubic seg2, newb2;
                if (b2a > minb + Utils.AngleEpsilon)
                {
                    var cut = b2.CutAtProgress(Utils.PositiveModulo(doubleMapper.Map(minb) - polygon2CutPoint, 1f));
                    seg2 = cut.Left;
                    newb2 = cut.Right;
                }
                else
                {
                    seg2 = b2;
                    newb2 = i2 < bs2.Count ? bs2.Cubics[i2++] : null;
                }

                ret.Add((seg1.Cubic, seg2.Cubic));
                b1 = newb1;
                b2 = newb2;
            }

            if (b1 != null || b2 != null)
                throw new InvalidOperationException("Expected both Polygon's Cubic to be fully matched");

            return ret;
        }
    }
}
