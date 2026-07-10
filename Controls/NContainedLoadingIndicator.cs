using System;
using System.Collections.Generic;
using System.Numerics;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using SkiaMD3Expressive.Maui.Graphics.Shapes;

namespace SkiaMD3Expressive.Maui.Controls
{
    public class NContainedLoadingIndicator : Grid, IDisposable
    {
        // Bindable properties
        public static readonly BindableProperty ProgressProperty =
            BindableProperty.Create(nameof(Progress), typeof(double), typeof(NContainedLoadingIndicator), -1.0,
                propertyChanged: OnProgressChanged);

        public double Progress
        {
            get => (double)GetValue(ProgressProperty);
            set => SetValue(ProgressProperty, value);
        }

        public static readonly BindableProperty IndicatorColorProperty =
            BindableProperty.Create(nameof(IndicatorColor), typeof(Color), typeof(NContainedLoadingIndicator), Color.FromArgb("#0369A1"), // OnPrimaryContainer-ish dark sky blue
                propertyChanged: OnVisualPropertyChanged);

        public Color IndicatorColor
        {
            get => (Color)GetValue(IndicatorColorProperty);
            set => SetValue(IndicatorColorProperty, value);
        }

        public static readonly BindableProperty ContainerColorProperty =
            BindableProperty.Create(nameof(ContainerColor), typeof(Color), typeof(NContainedLoadingIndicator), Color.FromArgb("#E0F2FE"), // PrimaryContainer-ish light sky blue
                propertyChanged: OnVisualPropertyChanged);

        public Color ContainerColor
        {
            get => (Color)GetValue(ContainerColorProperty);
            set => SetValue(ContainerColorProperty, value);
        }

        public static readonly BindableProperty CornerRadiusProperty =
            BindableProperty.Create(nameof(CornerRadius), typeof(double?), typeof(NContainedLoadingIndicator), null, // null defaults to fully circular
                propertyChanged: OnVisualPropertyChanged);

        public double? CornerRadius
        {
            get => (double?)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        public static readonly BindableProperty DeterminateTargetProperty =
            BindableProperty.Create(nameof(DeterminateTarget), typeof(RoundedPolygon), typeof(NContainedLoadingIndicator), null,
                propertyChanged: OnDeterminateTargetChanged);

        public RoundedPolygon DeterminateTarget
        {
            get => (RoundedPolygon)GetValue(DeterminateTargetProperty);
            set => SetValue(DeterminateTargetProperty, value);
        }

        public static readonly BindableProperty IndicatorPolygonsProperty =
            BindableProperty.Create(nameof(IndicatorPolygons), typeof(IList<RoundedPolygon>), typeof(NContainedLoadingIndicator), null,
                propertyChanged: OnIndicatorPolygonsChanged);

        public IList<RoundedPolygon> IndicatorPolygons
        {
            get => (IList<RoundedPolygon>)GetValue(IndicatorPolygonsProperty);
            set => SetValue(IndicatorPolygonsProperty, value);
        }

        public static readonly BindableProperty GlobalRotationDurationProperty =
            BindableProperty.Create(nameof(GlobalRotationDuration), typeof(int), typeof(NContainedLoadingIndicator), 4666,
                propertyChanged: OnVisualPropertyChanged);

        public int GlobalRotationDuration
        {
            get => (int)GetValue(GlobalRotationDurationProperty);
            set => SetValue(GlobalRotationDurationProperty, value);
        }

        public static readonly BindableProperty MorphIntervalProperty =
            BindableProperty.Create(nameof(MorphInterval), typeof(int), typeof(NContainedLoadingIndicator), 650,
                propertyChanged: OnVisualPropertyChanged);

        public int MorphInterval
        {
            get => (int)GetValue(MorphIntervalProperty);
            set => SetValue(MorphIntervalProperty, value);
        }

        private Morph _customDeterminateMorph;
        private List<Morph> _customDeterminateMorphs;
        private List<Morph> _customIndeterminateMorphs;
        private float? _customDeterminateScaleFactor;
        private float? _customIndeterminateScaleFactor;

        // Predefined Morphs & Shapes
        private static readonly RoundedPolygon DeterminateStartShape;
        private static readonly Morph DeterminateMorph;
        private static readonly Morph[] IndeterminateMorphs;

        private static readonly float DeterminateScaleFactor;
        private static readonly float IndeterminateScaleFactor;

        private static readonly List<Morph> _defaultDeterminateMorphs;
        private static readonly List<Morph> _defaultIndeterminateMorphs;

        static NContainedLoadingIndicator()
        {
            // Determinate
            DeterminateStartShape = MaterialShapes.Circle.Transformed((x, y) => {
                var pt = RotateDegrees(new SkiaMD3Expressive.Maui.Graphics.Shapes.Point(x, y), 18f, Utils.Zero);
                return (pt.X, pt.Y);
            });
            DeterminateMorph = new Morph(DeterminateStartShape, MaterialShapes.SoftBurst);

            var determinatePolygons = new List<RoundedPolygon> { DeterminateStartShape, MaterialShapes.SoftBurst };
            DeterminateScaleFactor = CalculateScaleFactor(determinatePolygons);

            // Indeterminate
            var indeterminatePolygons = new List<RoundedPolygon>
            {
                MaterialShapes.SoftBurst,
                MaterialShapes.Cookie9Sided,
                MaterialShapes.Pentagon,
                MaterialShapes.Pill,
                MaterialShapes.Sunny,
                MaterialShapes.Cookie4Sided,
                MaterialShapes.Oval
            };

            IndeterminateMorphs = new Morph[]
            {
                new Morph(MaterialShapes.SoftBurst, MaterialShapes.Cookie9Sided),
                new Morph(MaterialShapes.Cookie9Sided, MaterialShapes.Pentagon),
                new Morph(MaterialShapes.Pentagon, MaterialShapes.Pill),
                new Morph(MaterialShapes.Pill, MaterialShapes.Sunny),
                new Morph(MaterialShapes.Sunny, MaterialShapes.Cookie4Sided),
                new Morph(MaterialShapes.Cookie4Sided, MaterialShapes.Oval),
                new Morph(MaterialShapes.Oval, MaterialShapes.SoftBurst)
            };

            IndeterminateScaleFactor = CalculateScaleFactor(indeterminatePolygons);

            // Defaults for Parity
            _defaultDeterminateMorphs = new List<Morph>
            {
                new Morph(DeterminateStartShape, MaterialShapes.SoftBurst)
            };

            _defaultIndeterminateMorphs = new List<Morph>(IndeterminateMorphs);
        }

        private static void OnDeterminateTargetChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is NContainedLoadingIndicator indicator)
            {
                if (newValue is RoundedPolygon target)
                {
                    indicator._customDeterminateMorph = new Morph(DeterminateStartShape, target);
                }
                else
                {
                    indicator._customDeterminateMorph = null;
                }
                indicator.UpdateNativeLayout();
            }
        }

        private static void OnIndicatorPolygonsChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is NContainedLoadingIndicator indicator)
            {
                indicator._customDeterminateMorphs = null;
                indicator._customIndeterminateMorphs = null;
                indicator._customDeterminateScaleFactor = null;
                indicator._customIndeterminateScaleFactor = null;
                indicator.UpdateNativeLayout();
            }
        }

        private static void OnProgressChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is NContainedLoadingIndicator indicator)
            {
                indicator.UpdateSemantics();
                indicator.StartAnimation();
            }
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is NContainedLoadingIndicator indicator)
            {
                indicator.UpdateBrushes();
                indicator.UpdateNativeLayout();
            }
        }

        // Native layout children
        private readonly RoundRectangle _containerShape;
        private readonly Microsoft.Maui.Controls.Shapes.Path _indicatorPath;

        private SolidColorBrush _indicatorBrush;
        private SolidColorBrush _containerBrush;
        private readonly PathF _reusableIndicatorPath = new PathF();

        private readonly System.Diagnostics.Stopwatch _stopwatch = new System.Diagnostics.Stopwatch();
        private bool _isAnimating = false;

        public NContainedLoadingIndicator()
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine("=== [CREATED] NContainedLoadingIndicator ===");
#endif
            WidthRequest = 48;
            HeightRequest = 48;
            BackgroundColor = Colors.Transparent;

            _containerShape = new RoundRectangle
            {
                StrokeThickness = 0,
                Stroke = null,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill,
                InputTransparent = true
            };

            _indicatorPath = new Microsoft.Maui.Controls.Shapes.Path
            {
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill,
                StrokeThickness = 0,
                Stroke = null,
                InputTransparent = true
            };

            Children.Add(_containerShape);
            Children.Add(_indicatorPath);

            UpdateBrushes();

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;

            UpdateSemantics();
        }

        private void OnLoaded(object? sender, System.EventArgs e)
        {
            StartAnimation();
        }

        private void OnUnloaded(object? sender, System.EventArgs e)
        {
            StopAnimation();
        }

#if DEBUG
        ~NContainedLoadingIndicator()
        {
            System.Diagnostics.Debug.WriteLine("=== [DESTROYED] NContainedLoadingIndicator ===");
        }
#endif

        protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();
            if (Handler != null)
            {
                Loaded -= OnLoaded;
                Unloaded -= OnUnloaded;
                Loaded += OnLoaded;
                Unloaded += OnUnloaded;

                StartAnimation();
            }
            else
            {
                Dispose();
            }
        }

        protected override void OnHandlerChanging(HandlerChangingEventArgs args)
        {
            base.OnHandlerChanging(args);
            if (args.NewHandler is null)
            {
                Dispose();
            }
        }

        private bool _isDisposed = false;

        public void Dispose()
        {
            if (_isDisposed) return;

            Loaded -= OnLoaded;
            Unloaded -= OnUnloaded;
            StopAnimation();

            if (Handler is Microsoft.Maui.IViewHandler platformHandler && platformHandler.PlatformView is IDisposable disposablePlatformView)
            {
                disposablePlatformView.Dispose();
            }
            Handler?.DisconnectHandler();

            _isDisposed = true;
            GC.SuppressFinalize(this);
        }

        private void UpdateBrushes()
        {
            _indicatorBrush = new SolidColorBrush(IndicatorColor);
            _containerBrush = new SolidColorBrush(ContainerColor);

            if (_indicatorPath != null) _indicatorPath.Fill = _indicatorBrush;
            if (_containerShape != null) _containerShape.Fill = _containerBrush;
        }

        private void StartAnimation()
        {
            if (Progress < 0)
            {
                if (!_isAnimating)
                {
                    _isAnimating = true;
                    _stopwatch.Restart();
                    this.Animate("NContainedLoadingIndicatorIndeterminateLoop",
                        callback: v => UpdateNativeLayout(),
                        start: 0,
                        end: 1,
                        rate: 16,
                        length: 1000,
                        easing: null,
                        finished: null,
                        repeat: () => _isAnimating);
                }
            }
            else
            {
                StopAnimation();
                UpdateNativeLayout();
            }
        }

        public void StopAnimation()
        {
            _isAnimating = false;
            this.AbortAnimation("NContainedLoadingIndicatorIndeterminateLoop");
            _stopwatch.Stop();
        }

        private void UpdateSemantics()
        {
            if (Progress < 0)
            {
                SemanticProperties.SetDescription(this, "Cargando");
            }
            else
            {
                int percentage = (int)Math.Round(Math.Clamp(Progress, 0.0, 1.0) * 100);
                SemanticProperties.SetDescription(this, $"Cargando, {percentage} por ciento");
            }
        }

        private List<Morph> GetIndeterminateMorphs()
        {
            if (IndicatorPolygons != null && IndicatorPolygons.Count >= 2)
            {
                if (_customIndeterminateMorphs == null)
                {
                    _customIndeterminateMorphs = BuildMorphSequence(IndicatorPolygons, circularSequence: true);
                }
                return _customIndeterminateMorphs;
            }
            return _defaultIndeterminateMorphs;
        }

        private float GetIndeterminateScaleFactor()
        {
            if (IndicatorPolygons != null && IndicatorPolygons.Count >= 2)
            {
                if (!_customIndeterminateScaleFactor.HasValue)
                {
                    _customIndeterminateScaleFactor = CalculateScaleFactor(new List<RoundedPolygon>(IndicatorPolygons));
                }
                return _customIndeterminateScaleFactor.Value;
            }
            return IndeterminateScaleFactor;
        }

        private float GetDeterminateScaleFactor()
        {
            if (IndicatorPolygons != null && IndicatorPolygons.Count >= 2)
            {
                if (!_customDeterminateScaleFactor.HasValue)
                {
                    _customDeterminateScaleFactor = CalculateScaleFactor(new List<RoundedPolygon>(IndicatorPolygons));
                }
                return _customDeterminateScaleFactor.Value;
            }
            return DeterminateScaleFactor;
        }

        private List<Morph> GetDeterminateMorphs()
        {
            if (IndicatorPolygons != null && IndicatorPolygons.Count >= 2)
            {
                if (_customDeterminateMorphs == null)
                {
                    _customDeterminateMorphs = BuildMorphSequence(IndicatorPolygons, circularSequence: false);
                }
                return _customDeterminateMorphs;
            }
            return _defaultDeterminateMorphs;
        }

        private static List<Morph> BuildMorphSequence(IList<RoundedPolygon> polygons, bool circularSequence)
        {
            var list = new List<Morph>();
            if (polygons == null || polygons.Count < 2) return list;

            for (int i = 0; i < polygons.Count; i++)
            {
                if (i + 1 < polygons.Count)
                {
                    list.Add(new Morph(polygons[i].Normalized(), polygons[i + 1].Normalized()));
                }
                else if (circularSequence)
                {
                    list.Add(new Morph(polygons[i].Normalized(), polygons[0].Normalized()));
                }
            }
            return list;
        }

        private void UpdateNativeLayout()
        {
            double width = Width > 0 ? Width : (WidthRequest > 0 ? WidthRequest : 48.0);
            double height = Height > 0 ? Height : (HeightRequest > 0 ? HeightRequest : 48.0);

            if (width <= 0 || height <= 0) return;

            float minDim = (float)Math.Min(width, height);

            // 1. Update Container Background corner radius
            float radius = CornerRadius.HasValue
                ? (float)CornerRadius.Value
                : minDim / 2f;

            if (_containerShape != null)
            {
                _containerShape.CornerRadius = radius;
            }

            // 2. Perform the path calculations
            float progress;
            float rotationAngle;
            Morph? activeMorph = null;
            bool isIndeterminate = Progress < 0;
            float scaleFactor = isIndeterminate ? GetIndeterminateScaleFactor() : GetDeterminateScaleFactor();

            if (isIndeterminate)
            {
                long elapsedMs = _stopwatch.ElapsedMilliseconds;

                float globalRotation = (elapsedMs % GlobalRotationDuration) / (float)GlobalRotationDuration * 360f;
                long cycle = elapsedMs / MorphInterval;

                var morphs = GetIndeterminateMorphs();
                if (morphs.Count > 0)
                {
                    int morphIndex = (int)(cycle % morphs.Count);
                    activeMorph = morphs[morphIndex];
                }

                float morphRotationTargetAngle = (cycle * 90f) % 360f;

                // Time t in seconds since the start of the current morph interval
                float t = (elapsedMs % MorphInterval) / 1000f;

                // Rest period (Snap) like Compose Material 3: the spring typically completes around 500ms
                if (t > 0.5f)
                {
                    progress = 1.0f;
                }
                else
                {
                    // Spring physics: dampingRatio = 0.6f, stiffness = 200f
                    float zeta = 0.6f;
                    float omega_n = (float)Math.Sqrt(200);
                    float omega_d = omega_n * (float)Math.Sqrt(1f - zeta * zeta);
                    float decay = zeta * omega_n;

                    progress = 1f - (float)Math.Exp(-decay * t) * ((float)Math.Cos(omega_d * t) + (decay / omega_d) * (float)Math.Sin(omega_d * t));

                    // Clamp progress to prevent glitches at boundary values
                    progress = Math.Clamp(progress, 0.0f, 2.0f);
                }

                // Add 90f initial offset (QuarterRotation) to match Compose
                rotationAngle = progress * 90f + morphRotationTargetAngle + globalRotation + 90f;
            }
            else
            {
                float progressValue = (float)Math.Clamp(Progress, 0.0, 1.0);

                // If custom multi-polygons sequence is supplied, use it
                if (IndicatorPolygons != null && IndicatorPolygons.Count >= 2)
                {
                    var morphs = GetDeterminateMorphs();
                    int activeMorphIndex = (int)(morphs.Count * progressValue);
                    activeMorphIndex = Math.Min(activeMorphIndex, morphs.Count - 1);

                    float adjustedProgressValue;
                    if (progressValue == 1f && activeMorphIndex == morphs.Count - 1)
                    {
                        adjustedProgressValue = 1f;
                    }
                    else
                    {
                        adjustedProgressValue = (progressValue * morphs.Count) % 1f;
                    }

                    activeMorph = morphs[activeMorphIndex];
                    progress = adjustedProgressValue;
                }
                else
                {
                    // Legacy single morph (respects DeterminateTarget selected by the picker)
                    activeMorph = _customDeterminateMorph ?? DeterminateMorph;
                    progress = progressValue;
                }
                rotationAngle = -progressValue * 180f;
            }

            if (activeMorph == null)
            {
                _indicatorPath.Data = null;
                return;
            }

            var path = ConvertToPathFOptimized(activeMorph, progress);

            float activeIndicatorScale = 38f / 48f; // Default Compose ratio
            float finalScale = scaleFactor * activeIndicatorScale;

            // 1. Get unscaled bounds to compute scaling center
            var bounds = path.Bounds;
            var unscaledCenter = new PointF(bounds.X + bounds.Width / 2f, bounds.Y + bounds.Height / 2f);

            // 2. Compute scale
            float scale = minDim * finalScale;
            var canvasCenter = new PointF((float)width / 2f, (float)height / 2f);
            float rotationAngleRad = (float)(rotationAngle * Math.PI / 180.0);

            // Combine scale, translation alignment, and rotation into one composite matrix
            Matrix3x2 combinedMatrix = Matrix3x2.CreateScale(scale, scale) *
                                       Matrix3x2.CreateTranslation(canvasCenter.X - unscaledCenter.X * scale, canvasCenter.Y - unscaledCenter.Y * scale) *
                                       Matrix3x2.CreateRotation(rotationAngleRad, new Vector2(canvasCenter.X, canvasCenter.Y));

            // 3. Convert to MAUI PathGeometry and assign using in-place zero-allocation mutation
            if (_indicatorPath.Data is PathGeometry indicatorGeom)
            {
                path.CopyToMauiGeometry(indicatorGeom, combinedMatrix);
            }
            else
            {
                var newGeom = new PathGeometry();
                path.CopyToMauiGeometry(newGeom, combinedMatrix);
                _indicatorPath.Data = newGeom;
            }
        }

        private PathF ConvertToPathFOptimized(Morph morph, float progress)
        {
            var path = new PathF();
            var match = morph.MorphMatch;
            if (match == null || match.Count == 0) return path;

            // Interpolate the first anchor point to start the path
            float firstAnchor0X = Utils.Interpolate(match[0].StartCubic.Points[0], match[0].EndCubic.Points[0], progress);
            float firstAnchor0Y = Utils.Interpolate(match[0].StartCubic.Points[1], match[0].EndCubic.Points[1], progress);
            path.MoveTo(firstAnchor0X, firstAnchor0Y);

            for (int i = 0; i < match.Count; i++)
            {
                var m = match[i];
                float c0x = Utils.Interpolate(m.StartCubic.Points[2], m.EndCubic.Points[2], progress);
                float c0y = Utils.Interpolate(m.StartCubic.Points[3], m.EndCubic.Points[3], progress);
                float c1x = Utils.Interpolate(m.StartCubic.Points[4], m.EndCubic.Points[4], progress);
                float c1y = Utils.Interpolate(m.StartCubic.Points[5], m.EndCubic.Points[5], progress);

                // Use the first anchor point coordinates on the final cubic segment to guarantee a perfect seam
                float a1x = (i == match.Count - 1) ? firstAnchor0X : Utils.Interpolate(m.StartCubic.Points[6], m.EndCubic.Points[6], progress);
                float a1y = (i == match.Count - 1) ? firstAnchor0Y : Utils.Interpolate(m.StartCubic.Points[7], m.EndCubic.Points[7], progress);

                path.CurveTo(c0x, c0y, c1x, c1y, a1x, a1y);
            }

            path.Close();
            return path;
        }

        private static float CalculateScaleFactor(List<RoundedPolygon> polygons)
        {
            float scaleFactor = 1f;
            var bounds = new float[4];
            var maxBounds = new float[4];

            foreach (var polygon in polygons)
            {
                polygon.CalculateBounds(bounds, approximate: false);
                polygon.CalculateMaxBounds(maxBounds);

                float boundsWidth = bounds[2] - bounds[0];
                float boundsHeight = bounds[3] - bounds[1];

                float maxBoundsWidth = maxBounds[2] - maxBounds[0];
                float maxBoundsHeight = maxBounds[3] - maxBounds[1];

                float scaleX = maxBoundsWidth == 0f ? 1f : boundsWidth / maxBoundsWidth;
                float scaleY = maxBoundsHeight == 0f ? 1f : boundsHeight / maxBoundsHeight;

                scaleFactor = Math.Min(scaleFactor, Math.Max(scaleX, scaleY));
            }

            return scaleFactor;
        }

        private static SkiaMD3Expressive.Maui.Graphics.Shapes.Point RotateDegrees(SkiaMD3Expressive.Maui.Graphics.Shapes.Point p, float angle, SkiaMD3Expressive.Maui.Graphics.Shapes.Point center)
        {
            float a = angle / 360f * 2f * (float)Math.PI;
            var off = p - center;
            return new SkiaMD3Expressive.Maui.Graphics.Shapes.Point(
                off.X * (float)Math.Cos(a) - off.Y * (float)Math.Sin(a),
                off.X * (float)Math.Sin(a) + off.Y * (float)Math.Cos(a)
            ) + center;
        }
    }
}
