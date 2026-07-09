using System;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using SkiaMD3Expressive.Maui.Graphics.Shapes;

namespace SkiaMD3Expressive.Maui.Helpers
{
    public static class MorphAnimationExtensions
    {
        /// <summary>
        /// Obtiene un PathGeometry intermedio entre las dos formas del Morph según el progreso (0.0 a 1.0)
        /// y escalado al ancho y alto indicados.
        /// </summary>
        public static PathGeometry InterpolatePathGeometry(this Morph morph, double progress, double width, double height)
        {
            var pathGeometry = new PathGeometry();
            if (morph == null) return pathGeometry;

            var match = morph.MorphMatch;
            if (match == null || match.Count == 0) return pathGeometry;

            float scaleX = (float)width;
            float scaleY = (float)height;
            float p = (float)Math.Clamp(progress, 0.0, 1.0);

            // Interpolamos el primer punto de ancla para iniciar la figura
            float firstAnchor0X = Utils.Interpolate(match[0].StartCubic.Points[0], match[0].EndCubic.Points[0], p);
            float firstAnchor0Y = Utils.Interpolate(match[0].StartCubic.Points[1], match[0].EndCubic.Points[1], p);

            var pathFigure = new PathFigure
            {
                StartPoint = new Microsoft.Maui.Graphics.Point(firstAnchor0X * scaleX, firstAnchor0Y * scaleY),
                IsClosed = true
            };

            for (int i = 0; i < match.Count; i++)
            {
                var m = match[i];
                float c0x = Utils.Interpolate(m.StartCubic.Points[2], m.EndCubic.Points[2], p);
                float c0y = Utils.Interpolate(m.StartCubic.Points[3], m.EndCubic.Points[3], p);
                float c1x = Utils.Interpolate(m.StartCubic.Points[4], m.EndCubic.Points[4], p);
                float c1y = Utils.Interpolate(m.StartCubic.Points[5], m.EndCubic.Points[5], p);
                float a1x = Utils.Interpolate(m.StartCubic.Points[6], m.EndCubic.Points[6], p);
                float a1y = Utils.Interpolate(m.StartCubic.Points[7], m.EndCubic.Points[7], p);

                var segment = new BezierSegment(
                    new Microsoft.Maui.Graphics.Point(c0x * scaleX, c0y * scaleY),
                    new Microsoft.Maui.Graphics.Point(c1x * scaleX, c1y * scaleY),
                    new Microsoft.Maui.Graphics.Point(a1x * scaleX, a1y * scaleY)
                );
                pathFigure.Segments.Add(segment);
            }

            pathGeometry.Figures.Add(pathFigure);
            return pathGeometry;
        }

        /// <summary>
        /// Anima el contorno de un Border nativo de MAUI realizando morphing desde un polígono inicial a uno final.
        /// </summary>
        public static void AnimateBorderMorph(
            this Border border,
            RoundedPolygon startShape,
            RoundedPolygon endShape,
            uint durationMs = 300,
            Easing easing = null)
        {
            if (border == null || startShape == null || endShape == null) return;

            string animName = "BorderMorphAnimation";
            border.AbortAnimation(animName);

            var morph = new Morph(startShape, endShape);

            var animation = new Animation(
                callback: progress =>
                {
                    double w = border.Width > 0 ? border.Width : border.WidthRequest > 0 ? border.WidthRequest : 100.0;
                    double h = border.Height > 0 ? border.Height : border.HeightRequest > 0 ? border.HeightRequest : 100.0;

                    border.StrokeShape = morph.InterpolatePathGeometry(progress, w, h);
                },
                start: 0.0,
                end: 1.0
            );

            animation.Commit(border, animName, length: durationMs, easing: easing ?? Easing.CubicInOut);
        }

        /// <summary>
        /// Anima la máscara de recorte (Clip) de cualquier VisualElement de MAUI realizando morphing de siluetas.
        /// </summary>
        public static void AnimateClipMorph(
            this VisualElement element,
            RoundedPolygon startShape,
            RoundedPolygon endShape,
            uint durationMs = 300,
            Easing easing = null)
        {
            if (element == null || startShape == null || endShape == null) return;

            string animName = "ClipMorphAnimation";
            element.AbortAnimation(animName);

            var morph = new Morph(startShape, endShape);

            var animation = new Animation(
                callback: progress =>
                {
                    double w = element.Width > 0 ? element.Width : element.WidthRequest > 0 ? element.WidthRequest : 100.0;
                    double h = element.Height > 0 ? element.Height : element.HeightRequest > 0 ? element.HeightRequest : 100.0;

                    element.Clip = morph.InterpolatePathGeometry(progress, w, h);
                },
                start: 0.0,
                end: 1.0
            );

            animation.Commit(element, animName, length: durationMs, easing: easing ?? Easing.CubicInOut);
        }
    }
}
