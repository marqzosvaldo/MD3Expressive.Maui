using System;
using System.Numerics;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace SkiaMD3Expressive.Maui.Controls
{
    public readonly struct CubicSubdivision
    {
        public readonly PointF P0;
        public readonly PointF P1;
        public readonly PointF P2;
        public readonly PointF P3;

        public CubicSubdivision(PointF p0, PointF p1, PointF p2, PointF p3)
        {
            P0 = p0;
            P1 = p1;
            P2 = p2;
            P3 = p3;
        }
    }

    public readonly struct QuadSubdivision
    {
        public readonly PointF P0;
        public readonly PointF P1;
        public readonly PointF P2;

        public QuadSubdivision(PointF p0, PointF p1, PointF p2)
        {
            P0 = p0;
            P1 = p1;
            P2 = p2;
        }
    }

    public static class OptimizedSkiaPathExtensions
    {
        public static Microsoft.Maui.Controls.Shapes.Geometry ToMauiGeometry(this Microsoft.Maui.Graphics.PathF pathF)
        {
            if (pathF == null || pathF.OperationCount == 0)
            {
                return new Microsoft.Maui.Controls.Shapes.PathGeometry();
            }

            var pathGeometry = new Microsoft.Maui.Controls.Shapes.PathGeometry();
            var figure = new Microsoft.Maui.Controls.Shapes.PathFigure();

            for (int i = 0; i < pathF.OperationCount; i++)
            {
                var op = pathF.GetSegmentType(i);
                var pts = pathF.GetPointsForSegment(i);

                switch (op)
                {
                    case PathOperation.Move:
                        if (figure.Segments.Count > 0)
                        {
                            pathGeometry.Figures.Add(figure);
                            figure = new Microsoft.Maui.Controls.Shapes.PathFigure();
                        }
                        if (pts != null && pts.Length > 0)
                        {
                            figure.StartPoint = new Microsoft.Maui.Graphics.Point(pts[0].X, pts[0].Y);
                        }
                        break;

                    case PathOperation.Line:
                        if (pts != null && pts.Length > 0)
                        {
                            figure.Segments.Add(new Microsoft.Maui.Controls.Shapes.LineSegment(
                                new Microsoft.Maui.Graphics.Point(pts[0].X, pts[0].Y)
                            ));
                        }
                        break;

                    case PathOperation.Quad:
                        if (pts != null && pts.Length > 1)
                        {
                            figure.Segments.Add(new Microsoft.Maui.Controls.Shapes.QuadraticBezierSegment(
                                new Microsoft.Maui.Graphics.Point(pts[0].X, pts[0].Y),
                                new Microsoft.Maui.Graphics.Point(pts[1].X, pts[1].Y)
                            ));
                        }
                        break;

                    case PathOperation.Cubic:
                        if (pts != null && pts.Length > 2)
                        {
                            figure.Segments.Add(new Microsoft.Maui.Controls.Shapes.BezierSegment(
                                new Microsoft.Maui.Graphics.Point(pts[0].X, pts[0].Y),
                                new Microsoft.Maui.Graphics.Point(pts[1].X, pts[1].Y),
                                new Microsoft.Maui.Graphics.Point(pts[2].X, pts[2].Y)
                            ));
                        }
                        break;

                    case PathOperation.Close:
                        figure.IsClosed = true;
                        break;
                }
            }

            if (figure.Segments.Count > 0 || figure.IsClosed)
            {
                pathGeometry.Figures.Add(figure);
            }

            return pathGeometry;
        }

        public static void CopyToMauiGeometry(this Microsoft.Maui.Graphics.PathF pathF, Microsoft.Maui.Controls.Shapes.PathGeometry pathGeometry, Matrix3x2? transform = null)
        {
            if (pathGeometry == null) return;

            if (pathF == null || pathF.OperationCount == 0)
            {
                if (pathGeometry.Figures.Count > 0)
                    pathGeometry.Figures.Clear();
                return;
            }

            bool applyTransform = transform.HasValue && !transform.Value.IsIdentity;
            Matrix3x2 matrix = applyTransform ? transform!.Value : Matrix3x2.Identity;

            int figureIndex = -1;
            Microsoft.Maui.Controls.Shapes.PathFigure? currentFigure = null;
            int segmentIndex = 0;

            for (int i = 0; i < pathF.OperationCount; i++)
            {
                var op = pathF.GetSegmentType(i);
                var pts = pathF.GetPointsForSegment(i);

                if (op == PathOperation.Move)
                {
                    if (currentFigure != null && segmentIndex < currentFigure.Segments.Count)
                    {
                        while (currentFigure.Segments.Count > segmentIndex)
                        {
                            currentFigure.Segments.RemoveAt(currentFigure.Segments.Count - 1);
                        }
                    }

                    figureIndex++;
                    segmentIndex = 0;

                    if (figureIndex < pathGeometry.Figures.Count)
                    {
                        currentFigure = pathGeometry.Figures[figureIndex];
                    }
                    else
                    {
                        currentFigure = new Microsoft.Maui.Controls.Shapes.PathFigure();
                        pathGeometry.Figures.Add(currentFigure);
                    }

                    if (pts != null && pts.Length > 0)
                    {
                        var pt = pts[0];
                        if (applyTransform)
                        {
                            var v = Vector2.Transform(new Vector2(pt.X, pt.Y), matrix);
                            pt = new PointF(v.X, v.Y);
                        }
                        var newStart = new Microsoft.Maui.Graphics.Point(pt.X, pt.Y);
                        if (currentFigure.StartPoint != newStart)
                        {
                            currentFigure.StartPoint = newStart;
                        }
                    }
                    currentFigure.IsClosed = false;
                }
                else
                {
                    if (currentFigure == null)
                    {
                        figureIndex++;
                        segmentIndex = 0;
                        if (figureIndex < pathGeometry.Figures.Count)
                        {
                            currentFigure = pathGeometry.Figures[figureIndex];
                        }
                        else
                        {
                            currentFigure = new Microsoft.Maui.Controls.Shapes.PathFigure();
                            pathGeometry.Figures.Add(currentFigure);
                        }
                        currentFigure.IsClosed = false;
                    }

                    switch (op)
                    {
                        case PathOperation.Line:
                            if (pts != null && pts.Length > 0)
                            {
                                var pt = pts[0];
                                if (applyTransform)
                                {
                                    var v = Vector2.Transform(new Vector2(pt.X, pt.Y), matrix);
                                    pt = new PointF(v.X, v.Y);
                                }
                                var point = new Microsoft.Maui.Graphics.Point(pt.X, pt.Y);
                                if (segmentIndex < currentFigure.Segments.Count && currentFigure.Segments[segmentIndex] is Microsoft.Maui.Controls.Shapes.LineSegment lineSeg)
                                {
                                    if (lineSeg.Point != point) lineSeg.Point = point;
                                }
                                else
                                {
                                    var newSeg = new Microsoft.Maui.Controls.Shapes.LineSegment(point);
                                    if (segmentIndex < currentFigure.Segments.Count)
                                        currentFigure.Segments[segmentIndex] = newSeg;
                                    else
                                        currentFigure.Segments.Add(newSeg);
                                }
                                segmentIndex++;
                            }
                            break;

                        case PathOperation.Quad:
                            if (pts != null && pts.Length > 1)
                            {
                                var pt0 = pts[0];
                                var pt1 = pts[1];
                                if (applyTransform)
                                {
                                    var v0 = Vector2.Transform(new Vector2(pt0.X, pt0.Y), matrix);
                                    pt0 = new PointF(v0.X, v0.Y);
                                    var v1 = Vector2.Transform(new Vector2(pt1.X, pt1.Y), matrix);
                                    pt1 = new PointF(v1.X, v1.Y);
                                }
                                var p1 = new Microsoft.Maui.Graphics.Point(pt0.X, pt0.Y);
                                var p2 = new Microsoft.Maui.Graphics.Point(pt1.X, pt1.Y);
                                if (segmentIndex < currentFigure.Segments.Count && currentFigure.Segments[segmentIndex] is Microsoft.Maui.Controls.Shapes.QuadraticBezierSegment quadSeg)
                                {
                                    if (quadSeg.Point1 != p1) quadSeg.Point1 = p1;
                                    if (quadSeg.Point2 != p2) quadSeg.Point2 = p2;
                                }
                                else
                                {
                                    var newSeg = new Microsoft.Maui.Controls.Shapes.QuadraticBezierSegment(p1, p2);
                                    if (segmentIndex < currentFigure.Segments.Count)
                                        currentFigure.Segments[segmentIndex] = newSeg;
                                    else
                                        currentFigure.Segments.Add(newSeg);
                                }
                                segmentIndex++;
                            }
                            break;

                        case PathOperation.Cubic:
                            if (pts != null && pts.Length > 2)
                            {
                                var pt0 = pts[0];
                                var pt1 = pts[1];
                                var pt2 = pts[2];
                                if (applyTransform)
                                {
                                    var v0 = Vector2.Transform(new Vector2(pt0.X, pt0.Y), matrix);
                                    pt0 = new PointF(v0.X, v0.Y);
                                    var v1 = Vector2.Transform(new Vector2(pt1.X, pt1.Y), matrix);
                                    pt1 = new PointF(v1.X, v1.Y);
                                    var v2 = Vector2.Transform(new Vector2(pt2.X, pt2.Y), matrix);
                                    pt2 = new PointF(v2.X, v2.Y);
                                }
                                var p1 = new Microsoft.Maui.Graphics.Point(pt0.X, pt0.Y);
                                var p2 = new Microsoft.Maui.Graphics.Point(pt1.X, pt1.Y);
                                var p3 = new Microsoft.Maui.Graphics.Point(pt2.X, pt2.Y);
                                if (segmentIndex < currentFigure.Segments.Count && currentFigure.Segments[segmentIndex] is Microsoft.Maui.Controls.Shapes.BezierSegment cubicSeg)
                                {
                                    if (cubicSeg.Point1 != p1) cubicSeg.Point1 = p1;
                                    if (cubicSeg.Point2 != p2) cubicSeg.Point2 = p2;
                                    if (cubicSeg.Point3 != p3) cubicSeg.Point3 = p3;
                                }
                                else
                                {
                                    var newSeg = new Microsoft.Maui.Controls.Shapes.BezierSegment(p1, p2, p3);
                                    if (segmentIndex < currentFigure.Segments.Count)
                                        currentFigure.Segments[segmentIndex] = newSeg;
                                    else
                                        currentFigure.Segments.Add(newSeg);
                                }
                                segmentIndex++;
                            }
                            break;

                        case PathOperation.Close:
                            currentFigure.IsClosed = true;
                            break;
                    }
                }
            }

            if (currentFigure != null && segmentIndex < currentFigure.Segments.Count)
            {
                while (currentFigure.Segments.Count > segmentIndex)
                {
                    currentFigure.Segments.RemoveAt(currentFigure.Segments.Count - 1);
                }
            }

            int expectedFiguresCount = figureIndex + 1;
            if (pathGeometry.Figures.Count > expectedFiguresCount)
            {
                while (pathGeometry.Figures.Count > expectedFiguresCount)
                {
                    pathGeometry.Figures.RemoveAt(pathGeometry.Figures.Count - 1);
                }
            }
        }

        public static void AddArcCustom(this Microsoft.Maui.Graphics.PathF path, float cx, float cy, float radius, float startAngleDegrees, float sweepAngleDegrees)
        {
            if (Math.Abs(sweepAngleDegrees) < 0.0001f) return;

            float startAngle = (float)(startAngleDegrees * Math.PI / 180.0);
            float sweepAngle = (float)(sweepAngleDegrees * Math.PI / 180.0);

            int segments = (int)Math.Ceiling(Math.Abs(sweepAngle) / (Math.PI / 2.0));
            float segmentSweep = sweepAngle / segments;

            float currentAngle = startAngle;

            if (path.OperationCount == 0)
            {
                float sx = cx + radius * (float)Math.Cos(currentAngle);
                float sy = cy + radius * (float)Math.Sin(currentAngle);
                path.MoveTo(sx, sy);
            }

            for (int i = 0; i < segments; i++)
            {
                float nextAngle = currentAngle + segmentSweep;

                float p0x = cx + radius * (float)Math.Cos(currentAngle);
                float p0y = cy + radius * (float)Math.Sin(currentAngle);

                float p3x = cx + radius * (float)Math.Cos(nextAngle);
                float p3y = cy + radius * (float)Math.Sin(nextAngle);

                float theta = segmentSweep;
                float L = (4f / 3f) * (float)Math.Tan(theta / 4f);

                float p1x = p0x - L * radius * (float)Math.Sin(currentAngle);
                float p1y = p0y + L * radius * (float)Math.Cos(currentAngle);

                float p2x = p3x + L * radius * (float)Math.Sin(nextAngle);
                float p2y = p3y - L * radius * (float)Math.Cos(nextAngle);

                path.CurveTo(p1x, p1y, p2x, p2y, p3x, p3y);

                currentAngle = nextAngle;
            }
        }

        public static void AddCircleCustom(this Microsoft.Maui.Graphics.PathF path, float cx, float cy, float radius)
        {
            path.AddArcCustom(cx, cy, radius, 0f, 360f);
        }

        public static PointF GetStartPoint(this Microsoft.Maui.Graphics.PathF path)
        {
            if (path == null || path.OperationCount == 0) return PointF.Zero;
            var pts = path.GetPointsForSegment(0);
            return pts != null && pts.Length > 0 ? pts[0] : PointF.Zero;
        }

        public static PointF GetEndPoint(this Microsoft.Maui.Graphics.PathF path)
        {
            if (path == null || path.OperationCount == 0) return PointF.Zero;
            for (int i = path.OperationCount - 1; i >= 0; i--)
            {
                var pts = path.GetPointsForSegment(i);
                if (pts != null && pts.Length > 0)
                {
                    return pts[pts.Length - 1];
                }
            }
            return PointF.Zero;
        }
    }

    public class PathFMeasure
    {
        private PathF _path;
        private float[] _segmentLengths;
        private float _totalLength;

        public float Length => _totalLength;

        public void SetPath(PathF path, bool forceClosed = false)
        {
            _path = path;
            if (path == null || path.OperationCount == 0)
            {
                _segmentLengths = Array.Empty<float>();
                _totalLength = 0f;
                return;
            }

            _segmentLengths = new float[path.OperationCount];
            _totalLength = 0f;

            PointF currentPoint = PointF.Zero;
            PointF startPoint = PointF.Zero;

            for (int i = 0; i < path.OperationCount; i++)
            {
                var op = path.GetSegmentType(i);
                var pts = path.GetPointsForSegment(i);
                float len = 0f;

                switch (op)
                {
                    case PathOperation.Move:
                        if (pts != null && pts.Length > 0)
                        {
                            currentPoint = pts[0];
                            startPoint = pts[0];
                        }
                        break;

                    case PathOperation.Line:
                        if (pts != null && pts.Length > 0)
                        {
                            len = Distance(currentPoint, pts[0]);
                            currentPoint = pts[0];
                        }
                        break;

                    case PathOperation.Quad:
                        if (pts != null && pts.Length > 1)
                        {
                            len = QuadBezierLength(currentPoint, pts[0], pts[1]);
                            currentPoint = pts[1];
                        }
                        break;

                    case PathOperation.Cubic:
                        if (pts != null && pts.Length > 2)
                        {
                            len = CubicBezierLength(currentPoint, pts[0], pts[1], pts[2]);
                            currentPoint = pts[2];
                        }
                        break;

                    case PathOperation.Close:
                        len = Distance(currentPoint, startPoint);
                        currentPoint = startPoint;
                        break;
                }

                _segmentLengths[i] = len;
                _totalLength += len;
            }
        }

        public bool GetSegment(float start, float stop, PathF destination, bool startWithMoveTo)
        {
            if (_path == null || _path.OperationCount == 0 || start >= stop || start > _totalLength)
            {
                return false;
            }

            start = Math.Max(0f, start);
            stop = Math.Min(_totalLength, stop);

            float currentDist = 0f;
            PointF currentPoint = PointF.Zero;
            PointF startPoint = PointF.Zero;
            bool firstSegment = true;

            for (int i = 0; i < _path.OperationCount; i++)
            {
                var op = _path.GetSegmentType(i);
                var pts = _path.GetPointsForSegment(i);
                float len = _segmentLengths[i];
                float nextDist = currentDist + len;

                PointF nextPoint = currentPoint;
                if (op == PathOperation.Move && pts != null && pts.Length > 0)
                {
                    nextPoint = pts[0];
                    startPoint = pts[0];
                }
                else if (op == PathOperation.Line && pts != null && pts.Length > 0)
                {
                    nextPoint = pts[0];
                }
                else if (op == PathOperation.Quad && pts != null && pts.Length > 1)
                {
                    nextPoint = pts[1];
                }
                else if (op == PathOperation.Cubic && pts != null && pts.Length > 2)
                {
                    nextPoint = pts[2];
                }
                else if (op == PathOperation.Close)
                {
                    nextPoint = startPoint;
                }

                if (nextDist > start && currentDist < stop)
                {
                    float tStart = 0f;
                    if (len > 0f && start > currentDist)
                    {
                        tStart = (start - currentDist) / len;
                    }

                    float tEnd = 1f;
                    if (len > 0f && stop < nextDist)
                    {
                        tEnd = (stop - currentDist) / len;
                    }

                    if (op == PathOperation.Move)
                     {
                         if (firstSegment && startWithMoveTo)
                         {
                             destination.MoveTo(nextPoint.X, nextPoint.Y);
                             firstSegment = false;
                         }
                     }
                    else if (op == PathOperation.Line)
                    {
                        PointF pStart = Interpolate(currentPoint, nextPoint, tStart);
                        PointF pEnd = Interpolate(currentPoint, nextPoint, tEnd);

                        if (firstSegment && startWithMoveTo)
                        {
                            destination.MoveTo(pStart.X, pStart.Y);
                            firstSegment = false;
                        }
                        destination.LineTo(pEnd.X, pEnd.Y);
                    }
                    else if (op == PathOperation.Quad && pts != null && pts.Length > 1)
                    {
                        QuadSubdivision sub = SubdivideQuad(currentPoint, pts[0], pts[1], tStart, tEnd);
                        if (firstSegment && startWithMoveTo)
                        {
                            destination.MoveTo(sub.P0.X, sub.P0.Y);
                            firstSegment = false;
                        }
                        destination.QuadTo(sub.P1.X, sub.P1.Y, sub.P2.X, sub.P2.Y);
                    }
                    else if (op == PathOperation.Cubic && pts != null && pts.Length > 2)
                    {
                        CubicSubdivision sub = SubdivideCubic(currentPoint, pts[0], pts[1], pts[2], tStart, tEnd);
                        if (firstSegment && startWithMoveTo)
                        {
                            destination.MoveTo(sub.P0.X, sub.P0.Y);
                            firstSegment = false;
                        }
                        destination.CurveTo(sub.P1.X, sub.P1.Y, sub.P2.X, sub.P2.Y, sub.P3.X, sub.P3.Y);
                    }
                    else if (op == PathOperation.Close)
                    {
                        PointF pStart = Interpolate(currentPoint, startPoint, tStart);
                        PointF pEnd = Interpolate(currentPoint, startPoint, tEnd);

                        if (firstSegment && startWithMoveTo)
                        {
                            destination.MoveTo(pStart.X, pStart.Y);
                            firstSegment = false;
                        }
                        destination.LineTo(pEnd.X, pEnd.Y);
                        if (tEnd >= 0.999f)
                        {
                            destination.Close();
                        }
                    }
                }

                currentDist = nextDist;
                currentPoint = nextPoint;
            }

            return true;
        }

        private static float Distance(PointF p0, PointF p1)
        {
            float dx = p1.X - p0.X;
            float dy = p1.Y - p0.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        private static float QuadBezierLength(PointF p0, PointF p1, PointF p2)
        {
            float length = 0f;
            PointF prev = p0;
            int steps = 10;
            for (int i = 1; i <= steps; i++)
            {
                float t = (float)i / steps;
                float u = 1f - t;
                float uu = u * u;
                float tt = t * t;

                float x = uu * p0.X + 2f * u * t * p1.X + tt * p2.X;
                float y = uu * p0.Y + 2f * u * t * p1.Y + tt * p2.Y;

                PointF current = new PointF(x, y);
                length += Distance(prev, current);
                prev = current;
            }
            return length;
        }

        private static float CubicBezierLength(PointF p0, PointF p1, PointF p2, PointF p3)
        {
            float length = 0f;
            PointF prev = p0;
            int steps = 12;
            for (int i = 1; i <= steps; i++)
            {
                float t = (float)i / steps;
                float u = 1f - t;
                float tt = t * t;
                float uu = u * u;
                float uuu = uu * u;
                float ttt = tt * t;

                float x = uuu * p0.X + 3f * uu * t * p1.X + 3f * u * tt * p2.X + ttt * p3.X;
                float y = uuu * p0.Y + 3f * uu * t * p1.Y + 3f * u * tt * p2.Y + ttt * p3.Y;

                PointF current = new PointF(x, y);
                length += Distance(prev, current);
                prev = current;
            }
            return length;
        }

        private static PointF Interpolate(PointF p0, PointF p1, float t)
        {
            return new PointF(p0.X + t * (p1.X - p0.X), p0.Y + t * (p1.Y - p0.Y));
        }

        private static QuadSubdivision SubdivideQuad(PointF p0, PointF p1, PointF p2, float tStart, float tEnd)
        {
            var p01 = Interpolate(p0, p1, tEnd);
            var p12 = Interpolate(p1, p2, tEnd);
            var p012 = Interpolate(p01, p12, tEnd);

            float tAdjusted = tEnd > 0.0001f ? Math.Clamp(tStart / tEnd, 0f, 1f) : 0f;

            var q01 = Interpolate(p0, p01, tAdjusted);
            var q12 = Interpolate(p01, p012, tAdjusted);
            var q012 = Interpolate(q01, q12, tAdjusted);

            return new QuadSubdivision(q012, q12, p012);
        }

        private static CubicSubdivision SubdivideCubic(PointF p0, PointF p1, PointF p2, PointF p3, float tStart, float tEnd)
        {
            var p01 = Interpolate(p0, p1, tEnd);
            var p12 = Interpolate(p1, p2, tEnd);
            var p23 = Interpolate(p2, p3, tEnd);

            var p012 = Interpolate(p01, p12, tEnd);
            var p123 = Interpolate(p12, p23, tEnd);

            var p0123 = Interpolate(p012, p123, tEnd);

            float tAdjusted = tEnd > 0.0001f ? Math.Clamp(tStart / tEnd, 0f, 1f) : 0f;

            var q01 = Interpolate(p0, p01, tAdjusted);
            var q12 = Interpolate(p01, p012, tAdjusted);
            var q23 = Interpolate(p012, p0123, tAdjusted);

            var q012 = Interpolate(q01, q12, tAdjusted);
            var q123 = Interpolate(q12, q23, tAdjusted);

            var q0123 = Interpolate(q012, q123, tAdjusted);

            return new CubicSubdivision(q0123, q123, q23, p0123);
        }
    }
}
