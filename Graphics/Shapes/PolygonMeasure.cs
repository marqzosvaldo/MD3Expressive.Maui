using System;
using System.Collections.Generic;

namespace SkiaMD3Expressive.Maui.Graphics.Shapes
{
    public interface IMeasurer
    {
        float MeasureCubic(Cubic c);
        float FindCubicCutPoint(Cubic c, float m);
    }

    public class LengthMeasurer : IMeasurer
    {
        private const int Segments = 3;

        public float MeasureCubic(Cubic c)
        {
            return ClosestProgressTo(c, float.PositiveInfinity).Total;
        }

        public float FindCubicCutPoint(Cubic c, float m)
        {
            return ClosestProgressTo(c, m).Progress;
        }

        private (float Progress, float Total) ClosestProgressTo(Cubic cubic, float threshold)
        {
            float total = 0f;
            float remainder = threshold;
            var prev = new Point(cubic.Anchor0X, cubic.Anchor0Y);

            for (int i = 1; i <= Segments; i++)
            {
                float progress = (float)i / Segments;
                var point = cubic.PointOnCurve(progress);
                float segment = (point - prev).GetDistance();

                if (segment >= remainder)
                {
                    return (progress - (1f - remainder / segment) / Segments, threshold);
                }

                remainder -= segment;
                total += segment;
                prev = point;
            }

            return (1.0f, total);
        }
    }

    public class ProgressableFeature
    {
        public float Progress { get; }
        public Feature Feature { get; }

        public ProgressableFeature(float progress, Feature feature)
        {
            Progress = progress;
            Feature = feature;
        }
    }

    public class MeasuredPolygon
    {
        private readonly IMeasurer _measurer;
        public List<MeasuredCubic> Cubics { get; } = new List<MeasuredCubic>();
        public List<ProgressableFeature> Features { get; }

        private MeasuredPolygon(
            IMeasurer measurer,
            List<ProgressableFeature> features,
            List<Cubic> cubics,
            List<float> outlineProgress)
        {
            if (outlineProgress.Count != cubics.Count + 1)
                throw new ArgumentException("Outline progress size is expected to be cubics size + 1");
            if (outlineProgress[0] != 0f)
                throw new ArgumentException("First outline progress value is expected to be zero");
            if (Math.Abs(outlineProgress[outlineProgress.Count - 1] - 1f) > Utils.DistanceEpsilon)
                throw new ArgumentException("Last outline progress value is expected to be one");

            _measurer = measurer;
            Features = features;

            float startOutlineProgress = 0f;
            for (int index = 0; index < cubics.Count; index++)
            {
                if ((outlineProgress[index + 1] - outlineProgress[index]) > Utils.DistanceEpsilon)
                {
                    Cubics.Add(new MeasuredCubic(this, cubics[index], startOutlineProgress, outlineProgress[index + 1]));
                    startOutlineProgress = outlineProgress[index + 1];
                }
            }

            if (Cubics.Count > 0)
            {
                Cubics[Cubics.Count - 1].UpdateProgressRange(Cubics[Cubics.Count - 1].StartOutlineProgress, 1f);
            }
        }

        public int Count => Cubics.Count;

        public class MeasuredCubic
        {
            private readonly MeasuredPolygon _parent;
            public Cubic Cubic { get; }
            public float StartOutlineProgress { get; private set; }
            public float EndOutlineProgress { get; private set; }
            public float MeasuredSize { get; }

            public MeasuredCubic(MeasuredPolygon parent, Cubic cubic, float startOutlineProgress, float endOutlineProgress)
            {
                if (endOutlineProgress < startOutlineProgress)
                    throw new ArgumentException("endOutlineProgress is expected to be equal or greater than startOutlineProgress");

                _parent = parent;
                Cubic = cubic;
                StartOutlineProgress = startOutlineProgress;
                EndOutlineProgress = endOutlineProgress;
                MeasuredSize = _parent._measurer.MeasureCubic(cubic);
            }

            public void UpdateProgressRange(float startOutlineProgress, float endOutlineProgress)
            {
                if (endOutlineProgress < startOutlineProgress)
                    throw new ArgumentException("endOutlineProgress is expected to be equal or greater than startOutlineProgress");

                StartOutlineProgress = startOutlineProgress;
                EndOutlineProgress = endOutlineProgress;
            }

            public (MeasuredCubic Left, MeasuredCubic Right) CutAtProgress(float cutOutlineProgress)
            {
                float boundedCutOutlineProgress = Math.Clamp(cutOutlineProgress, StartOutlineProgress, EndOutlineProgress);
                float outlineProgressSize = EndOutlineProgress - StartOutlineProgress;
                float progressFromStart = boundedCutOutlineProgress - StartOutlineProgress;

                float relativeProgress = outlineProgressSize < 0.0001f ? 0.5f : progressFromStart / outlineProgressSize;
                float t = _parent._measurer.FindCubicCutPoint(Cubic, relativeProgress * MeasuredSize);
                t = Math.Clamp(t, 0f, 1f);

                var (c1, c2) = Cubic.Split(t);
                return (
                    new MeasuredCubic(_parent, c1, StartOutlineProgress, boundedCutOutlineProgress),
                    new MeasuredCubic(_parent, c2, boundedCutOutlineProgress, EndOutlineProgress)
                );
            }

            public override string ToString()
            {
                return $"MeasuredCubic(outlineProgress=[{StartOutlineProgress} .. {EndOutlineProgress}], size={MeasuredSize}, cubic={Cubic})";
            }
        }

        public MeasuredPolygon CutAndShift(float cuttingPoint)
        {
            if (cuttingPoint < 0f || cuttingPoint > 1f)
                throw new ArgumentException("Cutting point must be between 0 and 1");

            if (cuttingPoint < Utils.DistanceEpsilon) 
                return this;

            int targetIndex = -1;
            for (int i = 0; i < Cubics.Count; i++)
            {
                if (cuttingPoint >= Cubics[i].StartOutlineProgress && cuttingPoint <= Cubics[i].EndOutlineProgress)
                {
                    targetIndex = i;
                    break;
                }
            }

            if (targetIndex == -1)
                targetIndex = Cubics.Count - 1;

            var target = Cubics[targetIndex];
            var (b1, b2) = target.CutAtProgress(cuttingPoint);

            var retCubics = new List<Cubic> { b2.Cubic };
            for (int i = 1; i < Cubics.Count; i++)
            {
                retCubics.Add(Cubics[(i + targetIndex) % Cubics.Count].Cubic);
            }
            retCubics.Add(b1.Cubic);

            var retOutlineProgress = new List<float> { 0f };
            for (int i = 1; i < Cubics.Count + 1; i++)
            {
                int cubicIndex = (targetIndex + i - 1) % Cubics.Count;
                retOutlineProgress.Add(Utils.PositiveModulo(Cubics[cubicIndex].EndOutlineProgress - cuttingPoint, 1f));
            }
            retOutlineProgress.Add(1f);

            var newFeatures = new List<ProgressableFeature>();
            for (int i = 0; i < Features.Count; i++)
            {
                newFeatures.Add(new ProgressableFeature(
                    Utils.PositiveModulo(Features[i].Progress - cuttingPoint, 1f),
                    Features[i].Feature
                ));
            }

            return new MeasuredPolygon(_measurer, newFeatures, retCubics, retOutlineProgress);
        }

        public static MeasuredPolygon MeasurePolygon(IMeasurer measurer, RoundedPolygon polygon)
        {
            var cubics = new List<Cubic>();
            var featureToCubic = new List<(Feature Feature, int Index)>();

            for (int featureIndex = 0; featureIndex < polygon.Features.Count; featureIndex++)
            {
                var feature = polygon.Features[featureIndex];
                for (int cubicIndex = 0; cubicIndex < feature.Cubics.Count; cubicIndex++)
                {
                    if (feature is Feature.Corner && cubicIndex == feature.Cubics.Count / 2)
                    {
                        featureToCubic.Add((feature, cubics.Count));
                    }
                    cubics.Add(feature.Cubics[cubicIndex]);
                }
            }

            var measures = new List<float> { 0f };
            float accumulated = 0f;
            foreach (var cubic in cubics)
            {
                float size = measurer.MeasureCubic(cubic);
                accumulated += size;
                measures.Add(accumulated);
            }

            float totalMeasure = measures[measures.Count - 1];
            var outlineProgress = new List<float>();
            for (int i = 0; i < measures.Count; i++)
            {
                outlineProgress.Add(totalMeasure <= 0f ? 0f : measures[i] / totalMeasure);
            }
            if (outlineProgress.Count > 0)
            {
                outlineProgress[outlineProgress.Count - 1] = 1f; // Ensure exact 1.0f at end
            }

            var features = new List<ProgressableFeature>();
            for (int i = 0; i < featureToCubic.Count; i++)
            {
                int ix = featureToCubic[i].Index;
                features.Add(new ProgressableFeature(
                    Utils.PositiveModulo((outlineProgress[ix] + outlineProgress[ix + 1]) / 2f, 1f),
                    featureToCubic[i].Feature
                ));
            }

            return new MeasuredPolygon(measurer, features, cubics, outlineProgress);
        }
    }
}
