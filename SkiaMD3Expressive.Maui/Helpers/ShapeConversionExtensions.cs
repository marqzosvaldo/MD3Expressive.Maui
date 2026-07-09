using System;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using SkiaMD3Expressive.Maui.Graphics.Shapes;

namespace SkiaMD3Expressive.Maui.Helpers
{
    public static class ShapeConversionExtensions
    {
        /// <summary>
        /// Convierte un RoundedPolygon normalizado (0 a 1) en un PathGeometry nativo de MAUI,
        /// escalándolo a las dimensiones especificadas (ancho y alto).
        /// </summary>
        public static PathGeometry ToMauiPathGeometry(this RoundedPolygon polygon, double width, double height)
        {
            var pathGeometry = new PathGeometry();
            if (polygon == null) return pathGeometry;

            var cubics = polygon.Cubics;
            if (cubics == null || cubics.Count == 0)
                return pathGeometry;

            float scaleX = (float)width;
            float scaleY = (float)height;

            var pathFigure = new PathFigure
            {
                // El punto de inicio de la figura es el primer ancla (Anchor0) del primer segmento cúbico.
                StartPoint = new Microsoft.Maui.Graphics.Point(cubics[0].Anchor0X * scaleX, cubics[0].Anchor0Y * scaleY),
                IsClosed = true
            };

            foreach (var cubic in cubics)
            {
                var segment = new BezierSegment(
                    new Microsoft.Maui.Graphics.Point(cubic.Control0X * scaleX, cubic.Control0Y * scaleY),
                    new Microsoft.Maui.Graphics.Point(cubic.Control1X * scaleX, cubic.Control1Y * scaleY),
                    new Microsoft.Maui.Graphics.Point(cubic.Anchor1X * scaleX, cubic.Anchor1Y * scaleY)
                );
                pathFigure.Segments.Add(segment);
            }

            pathGeometry.Figures.Add(pathFigure);
            return pathGeometry;
        }

        /// <summary>
        /// Convierte un RoundedPolygon normalizado (0 a 1) en un PathGeometry nativo de MAUI sin escalar (mantiene rango de 0 a 1).
        /// </summary>
        public static PathGeometry ToMauiPathGeometry(this RoundedPolygon polygon)
        {
            return ToMauiPathGeometry(polygon, 1.0, 1.0);
        }
    }
}
