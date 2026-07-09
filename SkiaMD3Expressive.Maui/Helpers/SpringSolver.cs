using System;

namespace SkiaMD3Expressive.Maui.Helpers
{
    public class SpringSolver
    {
        public double Stiffness { get; set; } = 400.0; // k
        public double DampingRatio { get; set; } = 1.0; // zeta (1.0 = critically damped)
        
        public double TargetValue { get; set; }
        public double CurrentValue { get; set; }
        public double Velocity { get; set; }

        public SpringSolver(double initialValue, double stiffness = 400.0, double dampingRatio = 1.0)
        {
            CurrentValue = initialValue;
            TargetValue = initialValue;
            Stiffness = stiffness;
            DampingRatio = dampingRatio;
            Velocity = 0.0;
        }

        public void Reset(double value)
        {
            CurrentValue = value;
            TargetValue = value;
            Velocity = 0.0;
        }

        public bool Update(double deltaTime)
        {
            // Cap delta time to prevent massive jumps on long frame drops
            deltaTime = Math.Min(deltaTime, 0.05);

            if (deltaTime <= 0.0) return false;

            // Sanity check initial state to recover from any external NaN/Infinity values
            if (double.IsNaN(CurrentValue) || double.IsInfinity(CurrentValue))
            {
                CurrentValue = TargetValue;
                Velocity = 0.0;
            }

            double displacement = CurrentValue - TargetValue;

            // If we are extremely close to target with negligible velocity, snap to target
            if (Math.Abs(displacement) < 0.0001 && Math.Abs(Velocity) < 0.0001)
            {
                CurrentValue = TargetValue;
                Velocity = 0.0;
                return false;
            }

            double omega = Math.Sqrt(Stiffness);
            double c = 2.0 * DampingRatio * omega;

            // Sub-stepping to guarantee absolute numerical stability under Euler integration
            const double maxStep = 0.001; // 1 ms safe sub-steps
            double remainingTime = deltaTime;

            while (remainingTime > 0.0)
            {
                double dt = Math.Min(remainingTime, maxStep);
                remainingTime -= dt;

                displacement = CurrentValue - TargetValue;
                double force = -Stiffness * displacement - c * Velocity;
                Velocity += force * dt;
                CurrentValue += Velocity * dt;

                // Stop instantly if values explode
                if (double.IsNaN(CurrentValue) || double.IsInfinity(CurrentValue))
                {
                    CurrentValue = TargetValue;
                    Velocity = 0.0;
                    break;
                }
            }

            return Math.Abs(CurrentValue - TargetValue) >= 0.0001 || Math.Abs(Velocity) >= 0.0001;
        }
    }
}
