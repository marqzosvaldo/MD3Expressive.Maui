using System;
using Microsoft.Maui.Graphics;

namespace SkiaMD3Expressive.Maui.Helpers
{
    public static class MaterialRippleHelper
    {
        /// <summary>
        /// Calculates the maximum distance from the touch point to the four corners of the control,
        /// accounting for potential press width expansion (e.g. in MaterialButtonGroup).
        /// </summary>
        public static double CalculateTargetRadius(double width, double height, Point touchPoint, double expansionFactor = 1.20)
        {
            double maxW = width * expansionFactor;
            double touchX = Math.Clamp(touchPoint.X, 0.0, width);
            double touchY = Math.Clamp(touchPoint.Y, 0.0, height);

            double distTopLeft = Math.Sqrt(touchX * touchX + touchY * touchY);
            double distTopRight = Math.Sqrt(Math.Pow(maxW - touchX, 2) + touchY * touchY);
            double distBottomLeft = Math.Sqrt(touchX * touchX + Math.Pow(height - touchY, 2));
            double distBottomRight = Math.Sqrt(Math.Pow(maxW - touchX, 2) + Math.Pow(height - touchY, 2));

            return Math.Max(Math.Max(distTopLeft, distTopRight), Math.Max(distBottomLeft, distBottomRight));
        }

#if ANDROID
        /// <summary>
        /// Creates a native Android RippleDrawable with a GradientDrawable mask matching the component shape.
        /// </summary>
        public static Android.Graphics.Drawables.RippleDrawable CreateNativeRipple(
            Color rippleColor,
            float cornerRadiusDp,
            float alphaPercentage,
            out Android.Graphics.Drawables.GradientDrawable maskDrawable)
        {
            int rippleArgb = Android.Graphics.Color.Argb(
                (int)(alphaPercentage * 255),
                (int)(rippleColor.Red * 255),
                (int)(rippleColor.Green * 255),
                (int)(rippleColor.Blue * 255));

            var colorStateList = Android.Content.Res.ColorStateList.ValueOf(
                new Android.Graphics.Color(rippleArgb));

            float density = Android.App.Application.Context.Resources?.DisplayMetrics?.Density ?? 2.625f;
            float cornerRadiusPx = cornerRadiusDp * density;

            maskDrawable = new Android.Graphics.Drawables.GradientDrawable();
            maskDrawable.SetShape(Android.Graphics.Drawables.ShapeType.Rectangle);
            maskDrawable.SetCornerRadius(cornerRadiusPx);
            maskDrawable.SetColor(Android.Graphics.Color.White);

            return new Android.Graphics.Drawables.RippleDrawable(colorStateList, null, maskDrawable);
        }

        /// <summary>
        /// Updates the corner radius of the Android RippleDrawable mask during dynamic view transformations.
        /// </summary>
        public static void UpdateRippleMaskRadius(
            Android.Graphics.Drawables.GradientDrawable? maskDrawable,
            double cornerRadiusDp,
            float density)
        {
            if (maskDrawable != null)
            {
                maskDrawable.SetCornerRadius((float)cornerRadiusDp * density);
            }
        }
#endif
    }

    internal class FabRippleAnimation
    {
        public Point StartCenter { get; }
        public Point TargetCenter { get; }
        public double StartRadius { get; }
        public double TargetRadius { get; }

        private readonly System.Diagnostics.Stopwatch _stopwatch = new();

        public enum AnimationState
        {
            FadingIn,
            AwaitingRelease,
            FadingOut,
            Completed
        }

        public AnimationState State { get; private set; } = AnimationState.FadingIn;
        public bool IsPressed { get; set; } = true;

        private const double FadeInDuration = 75.0;
        private const double RadiusDuration = 225.0;
        private const double FadeOutDuration = 150.0;

        private double _fadeOutStartAlpha = 0.0;
        private double _fadeOutStartMs = 0.0;

        public FabRippleAnimation(Point startCenter, Point targetCenter, double startRadius, double targetRadius)
        {
            StartCenter = startCenter;
            TargetCenter = targetCenter;
            StartRadius = startRadius;
            TargetRadius = targetRadius;
            _stopwatch.Start();
        }

        public void Release()
        {
            IsPressed = false;
            long elapsed = _stopwatch.ElapsedMilliseconds;
            if (State == AnimationState.FadingIn || State == AnimationState.AwaitingRelease)
            {
                _fadeOutStartAlpha = 1.0;
                _fadeOutStartMs = elapsed;
                State = AnimationState.FadingOut;
            }
        }

        public void Cancel()
        {
            IsPressed = false;
            long elapsed = _stopwatch.ElapsedMilliseconds;
            _fadeOutStartAlpha = GetCurrentAlpha();
            _fadeOutStartMs = elapsed;
            State = AnimationState.FadingOut;
        }

        public double GetCurrentAlpha()
        {
            long elapsed = _stopwatch.ElapsedMilliseconds;

            switch (State)
            {
                case AnimationState.FadingIn:
                    return Math.Clamp(elapsed / FadeInDuration, 0.0, 1.0);

                case AnimationState.AwaitingRelease:
                    return 1.0;

                case AnimationState.FadingOut:
                    double fadeOutElapsed = elapsed - _fadeOutStartMs;
                    double fadeOutProgress = Math.Clamp(fadeOutElapsed / FadeOutDuration, 0.0, 1.0);
                    return _fadeOutStartAlpha * (1.0 - fadeOutProgress);

                default:
                    return 0.0;
            }
        }

        public double GetCurrentRadiusPercent()
        {
            long elapsed = _stopwatch.ElapsedMilliseconds;
            double progress = Math.Clamp(elapsed / RadiusDuration, 0.0, 1.0);
            return EaseInOut(progress);
        }

        public void UpdateState()
        {
            long elapsed = _stopwatch.ElapsedMilliseconds;

            if (State == AnimationState.FadingIn)
            {
                if (elapsed >= RadiusDuration)
                {
                    if (IsPressed)
                    {
                        State = AnimationState.AwaitingRelease;
                    }
                    else
                    {
                        _fadeOutStartAlpha = 1.0;
                        _fadeOutStartMs = elapsed;
                        State = AnimationState.FadingOut;
                    }
                }
            }
            else if (State == AnimationState.FadingOut)
            {
                double fadeOutElapsed = elapsed - _fadeOutStartMs;
                if (fadeOutElapsed >= FadeOutDuration)
                {
                    State = AnimationState.Completed;
                    _stopwatch.Stop();
                }
            }
        }

        private static double EaseInOut(double t)
        {
            if (t <= 0) return 0.0;
            if (t >= 1) return 1.0;

            double start = 0.0, end = 1.0;
            for (int i = 0; i < 8; i++)
            {
                double mid = (start + end) / 2.0;
                double x = 3.0 * mid * (1.0 - mid) * (1.0 - mid) * 0.4 + 3.0 * mid * mid * (1.0 - mid) * 0.2 + mid * mid * mid;
                if (x < t) start = mid;
                else end = mid;
            }
            double u = (start + end) / 2.0;
            return 3.0 * u * (1.0 - u) * (1.0 - u) * 0.0 + 3.0 * u * u * (1.0 - u) * 1.0 + u * u * u;
        }
    }
}
