using System;
using Microsoft.Maui.Controls;

namespace SkiaMD3Expressive.Maui.Helpers
{
    public class SpringHoverScaleBehavior : Behavior<View>
    {
        public static readonly BindableProperty TargetScaleProperty =
            BindableProperty.Create(nameof(TargetScale), typeof(double), typeof(SpringHoverScaleBehavior), 1.1);

        public double TargetScale
        {
            get => (double)GetValue(TargetScaleProperty);
            set => SetValue(TargetScaleProperty, value);
        }

        public static readonly BindableProperty StiffnessProperty =
            BindableProperty.Create(nameof(Stiffness), typeof(double), typeof(SpringHoverScaleBehavior), 400.0);

        public double Stiffness
        {
            get => (double)GetValue(StiffnessProperty);
            set => SetValue(StiffnessProperty, value);
        }

        public static readonly BindableProperty DampingRatioProperty =
            BindableProperty.Create(nameof(DampingRatio), typeof(double), typeof(SpringHoverScaleBehavior), 1.0);

        public double DampingRatio
        {
            get => (double)GetValue(DampingRatioProperty);
            set => SetValue(DampingRatioProperty, value);
        }

        private PointerGestureRecognizer _pointerGestureRecognizer;
        private View _associatedObject;

        protected override void OnAttachedTo(View bindable)
        {
            base.OnAttachedTo(bindable);
            _associatedObject = bindable;

            _pointerGestureRecognizer = new PointerGestureRecognizer();
            _pointerGestureRecognizer.PointerEntered += OnPointerEntered;
            _pointerGestureRecognizer.PointerExited += OnPointerExited;

            bindable.GestureRecognizers.Add(_pointerGestureRecognizer);
        }

        protected override void OnDetachingFrom(View bindable)
        {
            if (_pointerGestureRecognizer != null)
            {
                _pointerGestureRecognizer.PointerEntered -= OnPointerEntered;
                _pointerGestureRecognizer.PointerExited -= OnPointerExited;
                bindable.GestureRecognizers.Remove(_pointerGestureRecognizer);
                _pointerGestureRecognizer = null;
            }
            _associatedObject = null;

            base.OnDetachingFrom(bindable);
        }

        private void OnPointerEntered(object sender, PointerEventArgs e)
        {
            if (_associatedObject != null)
            {
                _associatedObject.AnimateScaleWithSpring(TargetScale, Stiffness, DampingRatio);
            }
        }

        private void OnPointerExited(object sender, PointerEventArgs e)
        {
            if (_associatedObject != null)
            {
                _associatedObject.AnimateScaleWithSpring(1.0, Stiffness, DampingRatio);
            }
        }
    }
}
