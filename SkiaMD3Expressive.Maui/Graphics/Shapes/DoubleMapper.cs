using System;
using System.Collections.Generic;

namespace SkiaMD3Expressive.Maui.Graphics.Shapes
{
    public class DoubleMapper
    {
        private readonly List<float> _sourceValues = new List<float>();
        private readonly List<float> _targetValues = new List<float>();

        public DoubleMapper(params (float First, float Second)[] mappings)
        {
            foreach (var mapping in mappings)
            {
                _sourceValues.Add(mapping.First);
                _targetValues.Add(mapping.Second);
            }
            ValidateProgress(_sourceValues);
            ValidateProgress(_targetValues);
        }

        public float Map(float x) => LinearMap(_sourceValues, _targetValues, x);

        public float MapBack(float x) => LinearMap(_targetValues, _sourceValues, x);

        public static readonly DoubleMapper Identity = new DoubleMapper((0f, 0f), (0.5f, 0.5f));

        public static bool ProgressInRange(float progress, float progressFrom, float progressTo)
        {
            if (progressTo >= progressFrom)
            {
                return progress >= progressFrom && progress <= progressTo;
            }
            else
            {
                return progress >= progressFrom || progress <= progressTo;
            }
        }

        public static float LinearMap(List<float> xValues, List<float> yValues, float x)
        {
            if (x < 0f || x > 1f)
                throw new ArgumentException($"Invalid progress: {x}");

            int segmentStartIndex = -1;
            for (int i = 0; i < xValues.Count; i++)
            {
                if (ProgressInRange(x, xValues[i], xValues[(i + 1) % xValues.Count]))
                {
                    segmentStartIndex = i;
                    break;
                }
            }

            if (segmentStartIndex == -1)
                segmentStartIndex = 0;

            int segmentEndIndex = (segmentStartIndex + 1) % xValues.Count;
            float segmentSizeX = Utils.PositiveModulo(xValues[segmentEndIndex] - xValues[segmentStartIndex], 1f);
            float segmentSizeY = Utils.PositiveModulo(yValues[segmentEndIndex] - yValues[segmentStartIndex], 1f);
            
            float positionInSegment = segmentSizeX < 0.001f 
                ? 0.5f 
                : Utils.PositiveModulo(x - xValues[segmentStartIndex], 1f) / segmentSizeX;

            return Utils.PositiveModulo(yValues[segmentStartIndex] + segmentSizeY * positionInSegment, 1f);
        }

        public static void ValidateProgress(List<float> p)
        {
            if (p.Count == 0) return;
            float prev = p[p.Count - 1];
            int wraps = 0;
            for (int i = 0; i < p.Count; i++)
            {
                float curr = p[i];
                if (curr < 0f || curr >= 1f)
                    throw new ArgumentException("Progress outside of range [0, 1)");

                if (ProgressDistance(curr, prev) <= Utils.DistanceEpsilon)
                    throw new ArgumentException("Progress repeats a value");

                if (curr < prev)
                {
                    wraps++;
                    if (wraps > 1)
                        throw new ArgumentException("Progress wraps more than once");
                }
                prev = curr;
            }
        }

        public static float ProgressDistance(float p1, float p2)
        {
            float d = Math.Abs(p1 - p2);
            return Math.Min(d, 1f - d);
        }
    }
}
