using System;
using System.Collections.Generic;

namespace SkiaMD3Expressive.Maui.Graphics.Shapes
{
    public class RoundedPolygon
    {
        public List<Feature> Features { get; }
        public Point Center { get; }

        public float CenterX => Center.X;
        public float CenterY => Center.Y;

        public List<Cubic> Cubics { get; }

        public RoundedPolygon(List<Feature> features, Point center)
        {
            Features = features;
            Center = center;
            Cubics = BuildCubics(features);

            // Contiguous checks
            if (Cubics.Count > 0)
            {
                var prevCubic = Cubics[Cubics.Count - 1];
                for (int i = 0; i < Cubics.Count; i++)
                {
                    var cubic = Cubics[i];
                    if (Math.Abs(cubic.Anchor0X - prevCubic.Anchor1X) > Utils.DistanceEpsilon ||
                        Math.Abs(cubic.Anchor0Y - prevCubic.Anchor1Y) > Utils.DistanceEpsilon)
                    {
                        throw new ArgumentException("RoundedPolygon must be contiguous.");
                    }
                    prevCubic = cubic;
                }
            }
        }

        private List<Cubic> BuildCubics(List<Feature> features)
        {
            var list = new List<Cubic>();
            Cubic firstCubic = null;
            Cubic lastCubic = null;
            List<Cubic> firstFeatureSplitStart = null;
            List<Cubic> firstFeatureSplitEnd = null;

            if (features.Count > 0 && features[0].Cubics.Count == 3)
            {
                var centerCubic = features[0].Cubics[1];
                var (start, end) = centerCubic.Split(0.5f);
                firstFeatureSplitStart = new List<Cubic> { features[0].Cubics[0], start };
                firstFeatureSplitEnd = new List<Cubic> { end, features[0].Cubics[2] };
            }

            for (int i = 0; i <= features.Count; i++)
            {
                List<Cubic> featureCubics = null;
                if (i == 0 && firstFeatureSplitEnd != null)
                {
                    featureCubics = firstFeatureSplitEnd;
                }
                else if (i == features.Count)
                {
                    if (firstFeatureSplitStart != null)
                        featureCubics = firstFeatureSplitStart;
                    else
                        break;
                }
                else
                {
                    featureCubics = features[i].Cubics;
                }

                for (int j = 0; j < featureCubics.Count; j++)
                {
                    var cubic = featureCubics[j];
                    if (!cubic.ZeroLength())
                    {
                        if (lastCubic != null)
                            list.Add(lastCubic);
                        lastCubic = cubic;
                        if (firstCubic == null)
                            firstCubic = cubic;
                    }
                    else
                    {
                        if (lastCubic != null)
                        {
                            var pointsCopy = new float[8];
                            Array.Copy(lastCubic.Points, pointsCopy, 8);
                            lastCubic = new Cubic(pointsCopy);
                            lastCubic.Points[6] = cubic.Anchor1X;
                            lastCubic.Points[7] = cubic.Anchor1Y;
                        }
                    }
                }
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
            else
            {
                list.Add(new Cubic(CenterX, CenterY, CenterX, CenterY, CenterX, CenterY, CenterX, CenterY));
            }

            return list;
        }

        public RoundedPolygon Transformed(PointTransformer f)
        {
            var center = Center.Transformed(f);
            var features = new List<Feature>();
            for (int i = 0; i < Features.Count; i++)
            {
                features.Add(Features[i].Transformed(f));
            }
            return new RoundedPolygon(features, center);
        }

        public RoundedPolygon Normalized()
        {
            var bounds = new float[4];
            CalculateBounds(bounds);
            float width = bounds[2] - bounds[0];
            float height = bounds[3] - bounds[1];
            float side = Math.Max(width, height);
            
            float offsetX = (side - width) / 2f - bounds[0];
            float offsetY = (side - height) / 2f - bounds[1];

            return Transformed((x, y) => ((x + offsetX) / side, (y + offsetY) / side));
        }

        public float[] CalculateBounds(float[] bounds, bool approximate = true)
        {
            if (bounds.Length < 4)
                throw new ArgumentException("Required bounds size of 4");

            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;

            var tempBounds = new float[4];
            for (int i = 0; i < Cubics.Count; i++)
            {
                Cubics[i].CalculateBounds(tempBounds, approximate);
                minX = Math.Min(minX, tempBounds[0]);
                minY = Math.Min(minY, tempBounds[1]);
                maxX = Math.Max(maxX, tempBounds[2]);
                maxY = Math.Max(maxY, tempBounds[3]);
            }

            bounds[0] = minX;
            bounds[1] = minY;
            bounds[2] = maxX;
            bounds[3] = maxY;
            return bounds;
        }

        public float[] CalculateMaxBounds(float[] bounds)
        {
            if (bounds.Length < 4)
                throw new ArgumentException("Required bounds size of 4");

            float maxDistSquared = 0f;
            for (int i = 0; i < Cubics.Count; i++)
            {
                var cubic = Cubics[i];
                float anchorDistance = Utils.DistanceSquared(cubic.Anchor0X - CenterX, cubic.Anchor0Y - CenterY);
                var middlePoint = cubic.PointOnCurve(0.5f);
                float middleDistance = Utils.DistanceSquared(middlePoint.X - CenterX, middlePoint.Y - CenterY);
                maxDistSquared = Math.Max(maxDistSquared, Math.Max(anchorDistance, middleDistance));
            }

            float distance = (float)Math.Sqrt(maxDistSquared);
            bounds[0] = CenterX - distance;
            bounds[1] = CenterY - distance;
            bounds[2] = CenterX + distance;
            bounds[3] = CenterY + distance;
            return bounds;
        }

        public static RoundedPolygon Create(
            int numVertices,
            float radius = 1f,
            float centerX = 0f,
            float centerY = 0f,
            CornerRounding? rounding = null,
            List<CornerRounding>? perVertexRounding = null)
        {
            if (numVertices < 3)
                throw new ArgumentException("Polygon must have at least 3 vertices");

            return Create(
                VerticesFromNumVerts(numVertices, radius, centerX, centerY),
                rounding ?? CornerRounding.Unrounded,
                perVertexRounding,
                centerX,
                centerY
            );
        }

        public static RoundedPolygon Create(
            float[] vertices,
            CornerRounding? rounding = null,
            List<CornerRounding>? perVertexRounding = null,
            float centerX = float.MinValue,
            float centerY = float.MinValue)
        {
            rounding ??= CornerRounding.Unrounded;
            if (vertices.Length < 6)
                throw new ArgumentException("Polygons must have at least 3 vertices");
            if (vertices.Length % 2 == 1)
                throw new ArgumentException("The vertices array should have even size");
            if (perVertexRounding != null && perVertexRounding.Count * 2 != vertices.Length)
                throw new ArgumentException("perVertexRounding list size mismatch");

            int n = vertices.Length / 2;
            var roundedCorners = new List<RoundedCorner>();

            for (int i = 0; i < n; i++)
            {
                var vtxRounding = perVertexRounding?[i] ?? rounding;
                int prevIndex = ((i + n - 1) % n) * 2;
                int nextIndex = ((i + 1) % n) * 2;
                
                roundedCorners.Add(new RoundedCorner(
                    new Point(vertices[prevIndex], vertices[prevIndex + 1]),
                    new Point(vertices[i * 2], vertices[i * 2 + 1]),
                    new Point(vertices[nextIndex], vertices[nextIndex + 1]),
                    vtxRounding
                ));
            }

            var cutAdjusts = new List<(float RoundCutRatio, float CutRatio)>();
            for (int ix = 0; ix < n; ix++)
            {
                float expectedRoundCut = roundedCorners[ix].ExpectedRoundCut + roundedCorners[(ix + 1) % n].ExpectedRoundCut;
                float expectedCut = roundedCorners[ix].ExpectedCut + roundedCorners[(ix + 1) % n].ExpectedCut;
                
                float vtxX = vertices[ix * 2];
                float vtxY = vertices[ix * 2 + 1];
                float nextVtxX = vertices[((ix + 1) % n) * 2];
                float nextVtxY = vertices[((ix + 1) % n) * 2 + 1];
                float sideSize = Utils.Distance(vtxX - nextVtxX, vtxY - nextVtxY);

                if (expectedRoundCut > sideSize)
                {
                    cutAdjusts.Add((sideSize / expectedRoundCut, 0f));
                }
                else if (expectedCut > sideSize)
                {
                    cutAdjusts.Add((1f, (sideSize - expectedRoundCut) / (expectedCut - expectedRoundCut)));
                }
                else
                {
                    cutAdjusts.Add((1f, 1f));
                }
            }

            var corners = new List<List<Cubic>>();
            for (int i = 0; i < n; i++)
            {
                var allowedCuts = new List<float>();
                for (int delta = 0; delta <= 1; delta++)
                {
                    var (roundCutRatio, cutRatio) = cutAdjusts[(i + n - 1 + delta) % n];
                    allowedCuts.Add(
                        roundedCorners[i].ExpectedRoundCut * roundCutRatio +
                        (roundedCorners[i].ExpectedCut - roundedCorners[i].ExpectedRoundCut) * cutRatio
                    );
                }
                corners.Add(roundedCorners[i].GetCubics(allowedCuts[0], allowedCuts[1]));
            }

            var tempFeatures = new List<Feature>();
            for (int i = 0; i < n; i++)
            {
                int prevVtxIndex = (i + n - 1) % n;
                int nextVtxIndex = (i + 1) % n;
                var currVertex = new Point(vertices[i * 2], vertices[i * 2 + 1]);
                var prevVertex = new Point(vertices[prevVtxIndex * 2], vertices[prevVtxIndex * 2 + 1]);
                var nextVertex = new Point(vertices[nextVtxIndex * 2], vertices[nextVtxIndex * 2 + 1]);
                bool convex = Utils.Convex(prevVertex, currVertex, nextVertex);

                tempFeatures.Add(new Feature.Corner(corners[i], convex));
                tempFeatures.Add(new Feature.Edge(new List<Cubic>
                {
                    Cubic.StraightLine(
                        corners[i][corners[i].Count - 1].Anchor1X,
                        corners[i][corners[i].Count - 1].Anchor1Y,
                        corners[(i + 1) % n][0].Anchor0X,
                        corners[(i + 1) % n][0].Anchor0Y
                    )
                }));
            }

            var center = (centerX == float.MinValue || centerY == float.MinValue)
                ? CalculateCenter(vertices)
                : new Point(centerX, centerY);

            return new RoundedPolygon(tempFeatures, center);
        }

        private static Point CalculateCenter(float[] vertices)
        {
            float cumulativeX = 0f;
            float cumulativeY = 0f;
            int index = 0;
            while (index < vertices.Length)
            {
                cumulativeX += vertices[index++];
                cumulativeY += vertices[index++];
            }
            return new Point(cumulativeX / (vertices.Length / 2f), cumulativeY / (vertices.Length / 2f));
        }

        private class RoundedCorner
        {
            public Point P0 { get; }
            public Point P1 { get; }
            public Point P2 { get; }
            public CornerRounding Rounding { get; }

            public Point D1 { get; }
            public Point D2 { get; }
            public float CornerRadius { get; }
            public float Smoothing { get; }
            public float CosAngle { get; }
            public float SinAngle { get; }
            public float ExpectedRoundCut { get; }

            public float ExpectedCut => (1f + Smoothing) * ExpectedRoundCut;

            public Point Center { get; private set; }

            public RoundedCorner(Point p0, Point p1, Point p2, CornerRounding rounding)
            {
                P0 = p0;
                P1 = p1;
                P2 = p2;
                Rounding = rounding;

                var v01 = p0 - p1;
                var v21 = p2 - p1;
                float d01 = v01.GetDistance();
                float d21 = v21.GetDistance();

                if (d01 > 0f && d21 > 0f)
                {
                    D1 = v01 / d01;
                    D2 = v21 / d21;
                    CornerRadius = rounding?.Radius ?? 0f;
                    Smoothing = rounding?.Smoothing ?? 0f;
                    
                    CosAngle = D1.DotProduct(D2);
                    SinAngle = (float)Math.Sqrt(1f - Utils.Square(CosAngle));
                    
                    ExpectedRoundCut = SinAngle > 1e-3f 
                        ? CornerRadius * (CosAngle + 1f) / SinAngle 
                        : 0f;
                }
                else
                {
                    D1 = Utils.Zero;
                    D2 = Utils.Zero;
                    CornerRadius = 0f;
                    Smoothing = 0f;
                    CosAngle = 0f;
                    SinAngle = 0f;
                    ExpectedRoundCut = 0f;
                }
            }

            public List<Cubic> GetCubics(float allowedCut0, float allowedCut1)
            {
                float allowedCut = Math.Min(allowedCut0, allowedCut1);
                if (ExpectedRoundCut < Utils.DistanceEpsilon ||
                    allowedCut < Utils.DistanceEpsilon ||
                    CornerRadius < Utils.DistanceEpsilon)
                {
                    Center = P1;
                    return new List<Cubic> { Cubic.StraightLine(P1.X, P1.Y, P1.X, P1.Y) };
                }

                float actualRoundCut = Math.Min(allowedCut, ExpectedRoundCut);
                float actualSmoothing0 = CalculateActualSmoothingValue(allowedCut0);
                float actualSmoothing1 = CalculateActualSmoothingValue(allowedCut1);

                float actualR = CornerRadius * actualRoundCut / ExpectedRoundCut;
                float centerDistance = (float)Math.Sqrt(Utils.Square(actualR) + Utils.Square(actualRoundCut));
                
                Center = P1 + ((D1 + D2) / 2f).GetDirection() * centerDistance;
                var circleIntersection0 = P1 + D1 * actualRoundCut;
                var circleIntersection2 = P1 + D2 * actualRoundCut;

                var flanking0 = ComputeFlankingCurve(
                    actualRoundCut,
                    actualSmoothing0,
                    P1,
                    P0,
                    circleIntersection0,
                    circleIntersection2,
                    Center,
                    actualR
                );

                var flanking2 = ComputeFlankingCurve(
                    actualRoundCut,
                    actualSmoothing1,
                    P1,
                    P2,
                    circleIntersection2,
                    circleIntersection0,
                    Center,
                    actualR
                ).Reverse();

                return new List<Cubic>
                {
                    flanking0,
                    Cubic.CircularArc(
                        Center.X, Center.Y,
                        flanking0.Anchor1X, flanking0.Anchor1Y,
                        flanking2.Anchor0X, flanking2.Anchor0Y
                    ),
                    flanking2
                };
            }

            private float CalculateActualSmoothingValue(float allowedCut)
            {
                if (allowedCut > ExpectedCut) return Smoothing;
                if (allowedCut > ExpectedRoundCut) return Smoothing * (allowedCut - ExpectedRoundCut) / (ExpectedCut - ExpectedRoundCut);
                return 0f;
            }

            private Cubic ComputeFlankingCurve(
                float actualRoundCut,
                float actualSmoothingValues,
                Point corner,
                Point sideStart,
                Point circleSegmentIntersection,
                Point otherCircleSegmentIntersection,
                Point circleCenter,
                float actualR)
            {
                var sideDirection = (sideStart - corner).GetDirection();
                var curveStart = corner + sideDirection * actualRoundCut * (1f + actualSmoothingValues);
                
                var p = Point.Interpolate(
                    circleSegmentIntersection,
                    (circleSegmentIntersection + otherCircleSegmentIntersection) / 2f,
                    actualSmoothingValues
                );

                var curveEnd = circleCenter + Utils.DirectionVector(p.X - circleCenter.X, p.Y - circleCenter.Y) * actualR;
                var circleTangent = (curveEnd - circleCenter).Rotate90();
                
                var anchorEnd = LineIntersection(sideStart, sideDirection, curveEnd, circleTangent) 
                                ?? circleSegmentIntersection;

                var anchorStart = (curveStart + anchorEnd * 2f) / 3f;
                return new Cubic(curveStart, anchorStart, anchorEnd, curveEnd);
            }

            private Point? LineIntersection(Point p0, Point d0, Point p1, Point d1)
            {
                var rotatedD1 = d1.Rotate90();
                float den = d0.DotProduct(rotatedD1);
                if (Math.Abs(den) < Utils.DistanceEpsilon) return null;
                float num = (p1 - p0).DotProduct(rotatedD1);
                if (Math.Abs(den) < Utils.DistanceEpsilon * Math.Abs(num)) return null;
                float k = num / den;
                return p0 + d0 * k;
            }
        }

        private static float[] VerticesFromNumVerts(int numVertices, float radius, float centerX, float centerY)
        {
            var result = new float[numVertices * 2];
            int arrayIndex = 0;
            for (int i = 0; i < numVertices; i++)
            {
                var vertex = Utils.RadialToCartesian(radius, (Utils.FloatPi / numVertices * 2f * i)) + new Point(centerX, centerY);
                result[arrayIndex++] = vertex.X;
                result[arrayIndex++] = vertex.Y;
            }
            return result;
        }

        public static RoundedPolygon Circle(int numVertices = 8, float radius = 1f, float centerX = 0f, float centerY = 0f)
        {
            if (numVertices < 3) throw new ArgumentException("Circle must have at least three vertices");
            float theta = Utils.FloatPi / numVertices;
            float polygonRadius = radius / (float)Math.Cos(theta);
            return Create(
                numVertices,
                polygonRadius,
                centerX,
                centerY,
                new CornerRounding(radius)
            );
        }

        public static RoundedPolygon Rectangle(
            float width = 2f,
            float height = 2f,
            CornerRounding? rounding = null,
            List<CornerRounding>? perVertexRounding = null,
            float centerX = 0f,
            float centerY = 0f)
        {
            rounding ??= CornerRounding.Unrounded;
            float left = centerX - width / 2f;
            float top = centerY - height / 2f;
            float right = centerX + width / 2f;
            float bottom = centerY + height / 2f;

            var vertices = new float[] { right, bottom, left, bottom, left, top, right, top };
            return Create(vertices, rounding, perVertexRounding, centerX, centerY);
        }

        public static RoundedPolygon Star(
            int numVerticesPerRadius,
            float radius = 1f,
            float innerRadius = 0.5f,
            CornerRounding? rounding = null,
            CornerRounding? innerRounding = null,
            List<CornerRounding>? perVertexRounding = null,
            float centerX = 0f,
            float centerY = 0f)
        {
            rounding ??= CornerRounding.Unrounded;
            if (radius <= 0f || innerRadius <= 0f)
                throw new ArgumentException("Star radii must both be greater than 0");
            if (innerRadius >= radius)
                throw new ArgumentException("innerRadius must be less than radius");

            var pvRounding = perVertexRounding;
            if (pvRounding == null && innerRounding != null)
            {
                pvRounding = new List<CornerRounding>();
                for (int i = 0; i < numVerticesPerRadius; i++)
                {
                    pvRounding.Add(rounding);
                    pvRounding.Add(innerRounding);
                }
            }

            var vertices = StarVerticesFromNumVerts(numVerticesPerRadius, radius, innerRadius, centerX, centerY);
            return Create(vertices, rounding, pvRounding, centerX, centerY);
        }

        private static float[] StarVerticesFromNumVerts(
            int numVerticesPerRadius,
            float radius,
            float innerRadius,
            float centerX,
            float centerY)
        {
            var result = new float[numVerticesPerRadius * 4];
            int arrayIndex = 0;
            for (int i = 0; i < numVerticesPerRadius; i++)
            {
                var vertex = Utils.RadialToCartesian(radius, (Utils.FloatPi / numVerticesPerRadius * 2f * i));
                result[arrayIndex++] = vertex.X + centerX;
                result[arrayIndex++] = vertex.Y + centerY;
                
                vertex = Utils.RadialToCartesian(innerRadius, (Utils.FloatPi / numVerticesPerRadius * (2f * i + 1f)));
                result[arrayIndex++] = vertex.X + centerX;
                result[arrayIndex++] = vertex.Y + centerY;
            }
            return result;
        }

        public static RoundedPolygon Pill(
            float width = 2f,
            float height = 1f,
            float smoothing = 0f,
            float centerX = 0f,
            float centerY = 0f)
        {
            if (width <= 0f || height <= 0f)
                throw new ArgumentException("Pill shapes must have positive width and height");

            float wHalf = width / 2f;
            float hHalf = height / 2f;
            var vertices = new float[]
            {
                wHalf + centerX, hHalf + centerY,
                -wHalf + centerX, hHalf + centerY,
                -wHalf + centerX, -hHalf + centerY,
                wHalf + centerX, -hHalf + centerY
            };

            return Create(
                vertices,
                new CornerRounding(Math.Min(wHalf, hHalf), smoothing),
                null,
                centerX,
                centerY
            );
        }

        public static RoundedPolygon PillStar(
            float width = 2f,
            float height = 1f,
            int numVerticesPerRadius = 8,
            float innerRadiusRatio = 0.5f,
            CornerRounding rounding = null,
            CornerRounding innerRounding = null,
            List<CornerRounding> perVertexRounding = null,
            float vertexSpacing = 0.5f,
            float startLocation = 0f,
            float centerX = 0f,
            float centerY = 0f)
        {
            rounding ??= CornerRounding.Unrounded;
            if (width <= 0f || height <= 0f)
                throw new ArgumentException("Pill shapes must have positive width and height");
            if (innerRadiusRatio <= 0f || innerRadiusRatio > 1f)
                throw new ArgumentException("innerRadiusRatio must be between 0 and 1");

            var pvRounding = perVertexRounding;
            if (pvRounding == null && innerRounding != null)
            {
                pvRounding = new List<CornerRounding>();
                for (int i = 0; i < numVerticesPerRadius; i++)
                {
                    pvRounding.Add(rounding);
                    pvRounding.Add(innerRounding);
                }
            }

            var vertices = PillStarVerticesFromNumVerts(
                numVerticesPerRadius,
                width,
                height,
                innerRadiusRatio,
                vertexSpacing,
                startLocation,
                centerX,
                centerY
            );

            return Create(vertices, rounding, pvRounding, centerX, centerY);
        }

        private static float[] PillStarVerticesFromNumVerts(
            int numVerticesPerRadius,
            float width,
            float height,
            float innerRadius,
            float vertexSpacing,
            float startLocation,
            float centerX,
            float centerY)
        {
            float endcapRadius = Math.Min(width, height);
            float vSegLen = Math.Max(height - width, 0f);
            float hSegLen = Math.Max(width - height, 0f);
            float vSegHalf = vSegLen / 2f;
            float hSegHalf = hSegLen / 2f;

            float circlePerimeter = Utils.TwoPi * endcapRadius * Utils.Interpolate(innerRadius, 1f, vertexSpacing);
            float perimeter = 2f * hSegLen + 2f * vSegLen + circlePerimeter;

            var sections = new float[]
            {
                0f,
                vSegLen / 2f,
                vSegLen / 2f + circlePerimeter / 4f,
                vSegLen / 2f + circlePerimeter / 4f + hSegLen,
                vSegLen / 2f + circlePerimeter / 2f + hSegLen,
                vSegLen / 2f + circlePerimeter / 2f + hSegLen + vSegLen,
                vSegLen / 2f + circlePerimeter * 0.75f + hSegLen + vSegLen,
                vSegLen / 2f + circlePerimeter * 0.75f + hSegLen * 2f + vSegLen,
                vSegLen / 2f + circlePerimeter + hSegLen * 2f + vSegLen,
                vSegLen + circlePerimeter + hSegLen * 2f + vSegLen,
                perimeter
            };

            float tPerVertex = perimeter / (2f * numVerticesPerRadius);
            bool inner = false;
            int currSecIndex = 0;
            float secStart = 0f;
            float secEnd = sections[1];
            float t = startLocation * perimeter;

            var result = new float[numVerticesPerRadius * 4];
            int arrayIndex = 0;

            var rectBR = new Point(hSegHalf, vSegHalf);
            var rectBL = new Point(-hSegHalf, vSegHalf);
            var rectTL = new Point(-hSegHalf, -vSegHalf);
            var rectTR = new Point(hSegHalf, -vSegHalf);

            for (int i = 0; i < numVerticesPerRadius * 2; i++)
            {
                float boundedT = t % perimeter;
                if (boundedT < secStart) currSecIndex = 0;
                while (boundedT >= sections[(currSecIndex + 1) % sections.Length])
                {
                    currSecIndex = (currSecIndex + 1) % sections.Length;
                    secStart = sections[currSecIndex];
                    secEnd = sections[(currSecIndex + 1) % sections.Length];
                }

                float tInSection = boundedT - secStart;
                float tProportion = (secEnd - secStart) < 0.0001f ? 0.5f : tInSection / (secEnd - secStart);
                float currRadius = inner ? (endcapRadius * innerRadius) : endcapRadius;

                Point vertex;
                switch (currSecIndex)
                {
                    case 0:
                        vertex = new Point(currRadius, tProportion * vSegHalf);
                        break;
                    case 1:
                        vertex = Utils.RadialToCartesian(currRadius, tProportion * Utils.FloatPi / 2f) + rectBR;
                        break;
                    case 2:
                        vertex = new Point(hSegHalf - tProportion * hSegLen, currRadius);
                        break;
                    case 3:
                        vertex = Utils.RadialToCartesian(currRadius, Utils.FloatPi / 2f + (tProportion * Utils.FloatPi / 2f)) + rectBL;
                        break;
                    case 4:
                        vertex = new Point(-currRadius, vSegHalf - tProportion * vSegLen);
                        break;
                    case 5:
                        vertex = Utils.RadialToCartesian(currRadius, Utils.FloatPi + (tProportion * Utils.FloatPi / 2f)) + rectTL;
                        break;
                    case 6:
                        vertex = new Point(-hSegHalf + tProportion * hSegLen, -currRadius);
                        break;
                    case 7:
                        vertex = Utils.RadialToCartesian(currRadius, Utils.FloatPi * 1.5f + (tProportion * Utils.FloatPi / 2f)) + rectTR;
                        break;
                    default:
                        vertex = new Point(currRadius, -vSegHalf + tProportion * vSegHalf);
                        break;
                }

                result[arrayIndex++] = vertex.X + centerX;
                result[arrayIndex++] = vertex.Y + centerY;
                t += tPerVertex;
                inner = !inner;
            }

            return result;
        }
    }
}
