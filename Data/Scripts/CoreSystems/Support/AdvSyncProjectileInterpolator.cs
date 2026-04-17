using CoreSystems;
using CoreSystems.Projectiles;
using VRageMath;

namespace WeaponCore.Data.Scripts.CoreSystems.Support
{
    internal struct AdvSyncProjectileInterpolator
    {
        public bool IsSet;

        public int Window, Ticks;
        
        // Kinematic state of the client projectile at receive time:
        public Vector3D InitialPosition;
        public Vector3D InitialVelocity;
        
        // Extrapolated server state (over the window) at receive time:
        public Vector3D FinalPosition, FinalLastPosition;
        public Vector3D FinalVelocity, FinalPrevVelocity1, FinalPrevVelocity0;
        
        // Rolling velocity history for consistent PrevVelocity0/1 during interpolation:
        public Vector3D VelPrev0, VelPrev1;
        
        public void Step(Projectile projectile)
        {
            var t = ++Ticks / (double)Window;

            if (Ticks >= Window)
            {
                IsSet = false;

                projectile.Position = FinalPosition;
                projectile.LastPosition = FinalLastPosition;
                projectile.Velocity = FinalVelocity;
                projectile.PrevVelocity0 = FinalPrevVelocity0;
                projectile.PrevVelocity1 = FinalPrevVelocity1;

                if (!Vector3D.IsZero(FinalVelocity))
                {
                    Vector3D.Normalize(ref FinalVelocity, out projectile.Direction);
                }

                Vector3D.Dot(ref projectile.Velocity, ref projectile.Velocity, out projectile.VelocityLengthSqr);
                
                return;
            }

            var u = Window * Session.StepConst;
            var v0 = InitialVelocity * u;
            var v1 = FinalVelocity * u;

            projectile.Position = HermiteCubic(
                ref InitialPosition, ref v0,
                ref v1, ref FinalPosition,
                t
            );

            var interpolatedVelocity = 1.0 / u * HermiteCubicDerivative(
                ref InitialPosition, ref v0,
                ref v1, ref FinalPosition,
                t
            );

            projectile.PrevVelocity0 = VelPrev0;
            projectile.PrevVelocity1 = VelPrev1;
            VelPrev0 = VelPrev1;
            VelPrev1 = interpolatedVelocity;

            projectile.Velocity = interpolatedVelocity;

            if (!Vector3D.IsZero(interpolatedVelocity))
            {
                Vector3D.Normalize(ref interpolatedVelocity, out projectile.Direction);
            }

            Vector3D.Dot(ref projectile.Velocity, ref projectile.Velocity, out projectile.VelocityLengthSqr);
        }
        
        private static Vector3D HermiteCubic(ref Vector3D p0, ref Vector3D v0, ref Vector3D v1, ref Vector3D p1, double t)
        {
            var h0 = 1.0 - 3.0 * (t * t) + 2.0 * (t * t * t);
            var h1 = t - 2.0 * (t * t) + t * t * t;
            var h2 = -(t * t) + t * t * t;
            var h3 = 3.0 * (t * t) - 2.0 * (t * t * t);

            return h0 * p0 + h1 * v0 + h2 * v1 + h3 * p1;
        }
        
        private static Vector3D HermiteCubicDerivative(ref Vector3D p0, ref Vector3D v0, ref Vector3D v1, ref Vector3D p1, double t)
        {
            var dh0 = -6.0 * t + 6.0 * t * t;
            var dh1 = 1.0 - 4.0 * t + 3.0 * t * t;
            var dh2 = -2.0 * t + 3.0 * t * t;
            var dh3 =  6.0 * t - 6.0 * t * t;

            return dh0 * p0 + dh1 * v0 + dh2 * v1 + dh3 * p1;
        }
    }
}