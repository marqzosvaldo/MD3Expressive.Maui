using System;
using System.Collections.Generic;

namespace SkiaMD3Expressive.Maui.Graphics.Shapes
{
    public abstract class Feature
    {
        public List<Cubic> Cubics { get; }

        protected Feature(List<Cubic> cubics)
        {
            Cubics = cubics;
        }

        public abstract Feature Transformed(PointTransformer f);
        public abstract Feature Reversed();

        public abstract bool IsIgnorableFeature { get; }
        public abstract bool IsEdge { get; }
        public abstract bool IsConvexCorner { get; }
        public abstract bool IsConcaveCorner { get; }

        public static Feature BuildIgnorableFeature(List<Cubic> cubics) => Validated(new Edge(cubics));
        public static Feature BuildEdge(Cubic cubic) => new Edge(new List<Cubic> { cubic });
        public static Feature BuildConvexCorner(List<Cubic> cubics) => Validated(new Corner(cubics, true));
        public static Feature BuildConcaveCorner(List<Cubic> cubics) => Validated(new Corner(cubics, false));

        private static Feature Validated(Feature feature)
        {
            if (feature.Cubics.Count == 0)
                throw new ArgumentException("Features need at least one cubic.");

            if (!IsContinuous(feature))
                throw new ArgumentException("Feature must be continuous.");

            return feature;
        }

        private static bool IsContinuous(Feature feature)
        {
            var prevCubic = feature.Cubics[0];
            for (int i = 1; i < feature.Cubics.Count; i++)
            {
                var cubic = feature.Cubics[i];
                if (Math.Abs(cubic.Anchor0X - prevCubic.Anchor1X) > Utils.DistanceEpsilon ||
                    Math.Abs(cubic.Anchor0Y - prevCubic.Anchor1Y) > Utils.DistanceEpsilon)
                {
                    return false;
                }
                prevCubic = cubic;
            }
            return true;
        }

        public class Edge : Feature
        {
            public Edge(List<Cubic> cubics) : base(cubics)
            {
            }

            public override Feature Transformed(PointTransformer f)
            {
                var list = new List<Cubic>();
                for (int i = 0; i < Cubics.Count; i++)
                {
                    list.Add(Cubics[i].Transformed(f));
                }
                return new Edge(list);
            }

            public override Feature Reversed()
            {
                var list = new List<Cubic>();
                for (int i = Cubics.Count - 1; i >= 0; i--)
                {
                    list.Add(Cubics[i].Reverse());
                }
                return new Edge(list);
            }

            public override string ToString() => "Edge";

            public override bool IsIgnorableFeature => true;
            public override bool IsEdge => true;
            public override bool IsConvexCorner => false;
            public override bool IsConcaveCorner => false;
        }

        public class Corner : Feature
        {
            public bool Convex { get; }

            public Corner(List<Cubic> cubics, bool convex = true) : base(cubics)
            {
                Convex = convex;
            }

            public override Feature Transformed(PointTransformer f)
            {
                var list = new List<Cubic>();
                for (int i = 0; i < Cubics.Count; i++)
                {
                    list.Add(Cubics[i].Transformed(f));
                }
                return new Corner(list, Convex);
            }

            public override Feature Reversed()
            {
                var list = new List<Cubic>();
                for (int i = Cubics.Count - 1; i >= 0; i--)
                {
                    list.Add(Cubics[i].Reverse());
                }
                return new Corner(list, !Convex);
            }

            public override string ToString() => $"Corner: convex={Convex}";

            public override bool IsIgnorableFeature => false;
            public override bool IsEdge => false;
            public override bool IsConvexCorner => Convex;
            public override bool IsConcaveCorner => !Convex;
        }
    }
}
