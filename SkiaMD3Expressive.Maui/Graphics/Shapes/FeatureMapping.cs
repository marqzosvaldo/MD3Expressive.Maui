using System;
using System.Collections.Generic;

namespace SkiaMD3Expressive.Maui.Graphics.Shapes
{
    public static class FeatureMapping
    {
        public static DoubleMapper FeatureMapper(List<ProgressableFeature> features1, List<ProgressableFeature> features2)
        {
            var filteredFeatures1 = new List<ProgressableFeature>();
            for (int i = 0; i < features1.Count; i++)
            {
                if (features1[i].Feature is Feature.Corner)
                {
                    filteredFeatures1.Add(features1[i]);
                }
            }

            var filteredFeatures2 = new List<ProgressableFeature>();
            for (int i = 0; i < features2.Count; i++)
            {
                if (features2[i].Feature is Feature.Corner)
                {
                    filteredFeatures2.Add(features2[i]);
                }
            }

            var featureProgressMapping = DoMapping(filteredFeatures1, filteredFeatures2);

            var pairs = new (float First, float Second)[featureProgressMapping.Count];
            for (int i = 0; i < featureProgressMapping.Count; i++)
            {
                pairs[i] = (featureProgressMapping[i].First, featureProgressMapping[i].Second);
            }

            return new DoubleMapper(pairs);
        }

        private class DistanceVertex : IComparable<DistanceVertex>
        {
            public float Distance { get; }
            public ProgressableFeature F1 { get; }
            public ProgressableFeature F2 { get; }

            public DistanceVertex(float distance, ProgressableFeature f1, ProgressableFeature f2)
            {
                Distance = distance;
                F1 = f1;
                F2 = f2;
            }

            public int CompareTo(DistanceVertex? other)
            {
                if (other == null) return 1;
                return Distance.CompareTo(other.Distance);
            }
        }

        private static readonly List<(float First, float Second)> IdentityMapping = 
            new List<(float, float)> { (0f, 0f), (0.5f, 0.5f) };

        public static List<(float First, float Second)> DoMapping(
            List<ProgressableFeature> features1,
            List<ProgressableFeature> features2)
        {
            var distanceVertexList = new List<DistanceVertex>();
            for (int i = 0; i < features1.Count; i++)
            {
                var f1 = features1[i];
                for (int j = 0; j < features2.Count; j++)
                {
                    var f2 = features2[j];
                    float d = FeatureDistSquared(f1.Feature, f2.Feature);
                    if (d != float.MaxValue)
                    {
                        distanceVertexList.Add(new DistanceVertex(d, f1, f2));
                    }
                }
            }

            distanceVertexList.Sort();

            if (distanceVertexList.Count == 0) return IdentityMapping;
            if (distanceVertexList.Count == 1)
            {
                var first = distanceVertexList[0];
                float f1 = first.F1.Progress;
                float f2 = first.F2.Progress;
                return new List<(float, float)>
                {
                    (f1, f2),
                    (Utils.PositiveModulo(f1 + 0.5f, 1f), Utils.PositiveModulo(f2 + 0.5f, 1f))
                };
            }

            var helper = new MappingHelper();
            foreach (var dv in distanceVertexList)
            {
                helper.AddMapping(dv.F1, dv.F2);
            }
            return helper.Mapping;
        }

        private class MappingHelper
        {
            public List<(float First, float Second)> Mapping { get; } = new List<(float, float)>();
            private readonly HashSet<ProgressableFeature> _usedF1 = new HashSet<ProgressableFeature>();
            private readonly HashSet<ProgressableFeature> _usedF2 = new HashSet<ProgressableFeature>();

            public void AddMapping(ProgressableFeature f1, ProgressableFeature f2)
            {
                if (_usedF1.Contains(f1) || _usedF2.Contains(f2)) return;

                int index = Mapping.BinarySearch((f1.Progress, 0f), 
                    Comparer<(float First, float Second)>.Create((x, y) => x.First.CompareTo(y.First)));
                
                if (index >= 0) return;

                int insertionIndex = ~index;
                int n = Mapping.Count;

                if (n >= 1)
                {
                    var (before1, before2) = Mapping[(insertionIndex + n - 1) % n];
                    var (after1, after2) = Mapping[insertionIndex % n];

                    if (DoubleMapper.ProgressDistance(f1.Progress, before1) < Utils.DistanceEpsilon ||
                        DoubleMapper.ProgressDistance(f1.Progress, after1) < Utils.DistanceEpsilon ||
                        DoubleMapper.ProgressDistance(f2.Progress, before2) < Utils.DistanceEpsilon ||
                        DoubleMapper.ProgressDistance(f2.Progress, after2) < Utils.DistanceEpsilon)
                    {
                        return;
                    }

                    if (n > 1 && !DoubleMapper.ProgressInRange(f2.Progress, before2, after2))
                    {
                        return;
                    }
                }

                Mapping.Insert(insertionIndex, (f1.Progress, f2.Progress));
                _usedF1.Add(f1);
                _usedF2.Add(f2);
            }
        }

        public static float FeatureDistSquared(Feature f1, Feature f2)
        {
            if (f1 is Feature.Corner c1 && f2 is Feature.Corner c2 && c1.Convex != c2.Convex)
            {
                return float.MaxValue;
            }
            return (FeatureRepresentativePoint(f1) - FeatureRepresentativePoint(f2)).GetDistanceSquared();
        }

        public static Point FeatureRepresentativePoint(Feature feature)
        {
            float x = (feature.Cubics[0].Anchor0X + feature.Cubics[feature.Cubics.Count - 1].Anchor1X) / 2f;
            float y = (feature.Cubics[0].Anchor0Y + feature.Cubics[feature.Cubics.Count - 1].Anchor1Y) / 2f;
            return new Point(x, y);
        }
    }
}
