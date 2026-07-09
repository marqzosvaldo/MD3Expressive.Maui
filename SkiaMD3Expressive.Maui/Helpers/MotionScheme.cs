namespace SkiaMD3Expressive.Maui.Helpers
{
    public class SpringSpec
    {
        public double DampingRatio { get; }
        public double Stiffness { get; }

        public SpringSpec(double dampingRatio, double stiffness)
        {
            DampingRatio = dampingRatio;
            Stiffness = stiffness;
        }
    }

    public interface IMotionScheme
    {
        SpringSpec DefaultSpatialSpec { get; }
        SpringSpec FastSpatialSpec { get; }
        SpringSpec SlowSpatialSpec { get; }

        SpringSpec DefaultEffectsSpec { get; }
        SpringSpec FastEffectsSpec { get; }
        SpringSpec SlowEffectsSpec { get; }
    }

    public class ExpressiveMotionScheme : IMotionScheme
    {
        public SpringSpec DefaultSpatialSpec { get; } = new(ExpressiveMotionTokens.SpringDefaultSpatialDamping, ExpressiveMotionTokens.SpringDefaultSpatialStiffness);
        public SpringSpec FastSpatialSpec { get; } = new(ExpressiveMotionTokens.SpringFastSpatialDamping, ExpressiveMotionTokens.SpringFastSpatialStiffness);
        public SpringSpec SlowSpatialSpec { get; } = new(ExpressiveMotionTokens.SpringSlowSpatialDamping, ExpressiveMotionTokens.SpringSlowSpatialStiffness);

        public SpringSpec DefaultEffectsSpec { get; } = new(ExpressiveMotionTokens.SpringDefaultEffectsDamping, ExpressiveMotionTokens.SpringDefaultEffectsStiffness);
        public SpringSpec FastEffectsSpec { get; } = new(ExpressiveMotionTokens.SpringFastEffectsDamping, ExpressiveMotionTokens.SpringFastEffectsStiffness);
        public SpringSpec SlowEffectsSpec { get; } = new(ExpressiveMotionTokens.SpringSlowEffectsDamping, ExpressiveMotionTokens.SpringSlowEffectsStiffness);
    }

    public class StandardMotionScheme : IMotionScheme
    {
        public SpringSpec DefaultSpatialSpec { get; } = new(StandardMotionTokens.SpringDefaultSpatialDamping, StandardMotionTokens.SpringDefaultSpatialStiffness);
        public SpringSpec FastSpatialSpec { get; } = new(StandardMotionTokens.SpringFastSpatialDamping, StandardMotionTokens.SpringFastSpatialStiffness);
        public SpringSpec SlowSpatialSpec { get; } = new(StandardMotionTokens.SpringSlowSpatialDamping, StandardMotionTokens.SpringSlowSpatialStiffness);

        public SpringSpec DefaultEffectsSpec { get; } = new(StandardMotionTokens.SpringDefaultEffectsDamping, StandardMotionTokens.SpringDefaultEffectsStiffness);
        public SpringSpec FastEffectsSpec { get; } = new(StandardMotionTokens.SpringFastEffectsDamping, StandardMotionTokens.SpringFastEffectsStiffness);
        public SpringSpec SlowEffectsSpec { get; } = new(StandardMotionTokens.SpringSlowEffectsDamping, StandardMotionTokens.SpringSlowEffectsStiffness);
    }

    /// <summary>
    /// Motion Scheme provider to fetch standard or expressive specs, matching Material Design 3 guidelines.
    /// </summary>
    public static class MotionScheme
    {
        public static IMotionScheme Standard { get; } = new StandardMotionScheme();
        public static IMotionScheme Expressive { get; } = new ExpressiveMotionScheme();
    }
}
