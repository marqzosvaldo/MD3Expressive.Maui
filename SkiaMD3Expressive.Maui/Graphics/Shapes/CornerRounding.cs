namespace SkiaMD3Expressive.Maui.Graphics.Shapes
{
    public class CornerRounding
    {
        public float Radius { get; }
        public float Smoothing { get; }

        public CornerRounding(float radius = 0f, float smoothing = 0f)
        {
            Radius = radius;
            Smoothing = smoothing;
        }

        public static readonly CornerRounding Unrounded = new CornerRounding(0f, 0f);
    }
}
