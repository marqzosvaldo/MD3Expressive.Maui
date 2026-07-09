using System;
using System.Diagnostics;
using Microsoft.Maui.Controls;

namespace SkiaMD3Expressive.Maui.Helpers
{
    public static class SpringAnimationExtensions
    {
        public static void AnimateScaleWithSpring(
            this VisualElement element,
            double targetScale,
            double stiffness = 400.0,
            double dampingRatio = 1.0)
        {
            if (element == null) return;

            string animName = "SpringScaleAnimation";
            element.AbortAnimation(animName);

            var solver = new SpringSolver(element.Scale, stiffness, dampingRatio)
            {
                TargetValue = targetScale
            };

            var stopwatch = Stopwatch.StartNew();

            element.Animate(animName,
                callback: _ =>
                {
                    double deltaTime = stopwatch.Elapsed.TotalSeconds;
                    stopwatch.Restart();

                    bool active = solver.Update(deltaTime);
                    element.Scale = solver.CurrentValue;

                    if (!active)
                    {
                        element.AbortAnimation(animName);
                    }
                },
                rate: 16,
                length: 5000,
                repeat: null);
        }

        public static void AnimateTranslationXWithSpring(
            this VisualElement element,
            double targetX,
            double stiffness = 400.0,
            double dampingRatio = 1.0)
        {
            if (element == null) return;

            string animName = "SpringTranslationXAnimation";
            element.AbortAnimation(animName);

            var solver = new SpringSolver(element.TranslationX, stiffness, dampingRatio)
            {
                TargetValue = targetX
            };

            var stopwatch = Stopwatch.StartNew();

            element.Animate(animName,
                callback: _ =>
                {
                    double deltaTime = stopwatch.Elapsed.TotalSeconds;
                    stopwatch.Restart();

                    bool active = solver.Update(deltaTime);
                    element.TranslationX = solver.CurrentValue;

                    if (!active)
                    {
                        element.AbortAnimation(animName);
                    }
                },
                rate: 16,
                length: 5000,
                repeat: null);
        }

        public static void AnimateTranslationYWithSpring(
            this VisualElement element,
            double targetY,
            double stiffness = 400.0,
            double dampingRatio = 1.0)
        {
            if (element == null) return;

            string animName = "SpringTranslationYAnimation";
            element.AbortAnimation(animName);

            var solver = new SpringSolver(element.TranslationY, stiffness, dampingRatio)
            {
                TargetValue = targetY
            };

            var stopwatch = Stopwatch.StartNew();

            element.Animate(animName,
                callback: _ =>
                {
                    double deltaTime = stopwatch.Elapsed.TotalSeconds;
                    stopwatch.Restart();

                    bool active = solver.Update(deltaTime);
                    element.TranslationY = solver.CurrentValue;

                    if (!active)
                    {
                        element.AbortAnimation(animName);
                    }
                },
                rate: 16,
                length: 5000,
                repeat: null);
        }

        public static void AnimateRotationWithSpring(
            this VisualElement element,
            double targetRotation,
            double stiffness = 400.0,
            double dampingRatio = 1.0)
        {
            if (element == null) return;

            string animName = "SpringRotationAnimation";
            element.AbortAnimation(animName);

            var solver = new SpringSolver(element.Rotation, stiffness, dampingRatio)
            {
                TargetValue = targetRotation
            };

            var stopwatch = Stopwatch.StartNew();

            element.Animate(animName,
                callback: _ =>
                {
                    double deltaTime = stopwatch.Elapsed.TotalSeconds;
                    stopwatch.Restart();

                    bool active = solver.Update(deltaTime);
                    element.Rotation = solver.CurrentValue;

                    if (!active)
                    {
                        element.AbortAnimation(animName);
                    }
                },
                rate: 16,
                length: 5000,
                repeat: null);
        }

        public static void AnimateOpacityWithSpring(
            this VisualElement element,
            double targetOpacity,
            double stiffness = 400.0,
            double dampingRatio = 1.0)
        {
            if (element == null) return;

            string animName = "SpringOpacityAnimation";
            element.AbortAnimation(animName);

            var solver = new SpringSolver(element.Opacity, stiffness, dampingRatio)
            {
                TargetValue = targetOpacity
            };

            var stopwatch = Stopwatch.StartNew();

            element.Animate(animName,
                callback: _ =>
                {
                    double deltaTime = stopwatch.Elapsed.TotalSeconds;
                    stopwatch.Restart();

                    bool active = solver.Update(deltaTime);
                    element.Opacity = Math.Clamp(solver.CurrentValue, 0.0, 1.0);

                    if (!active)
                    {
                        element.AbortAnimation(animName);
                    }
                },
                rate: 16,
                length: 5000,
                repeat: null);
        }
    }
}
