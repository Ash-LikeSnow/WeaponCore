using CoreSystems;
using CoreSystems.Projectiles;
using CoreSystems.Support;
using Sandbox.Game.Entities;
using VRageMath;

namespace WeaponCore.Data.Scripts.CoreSystems.Support
{
    internal struct AdvSyncProjectileFlightController
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

            projectile.Position = MathFuncs.HermiteCubic(
                ref InitialPosition, ref v0,
                ref v1, ref FinalPosition,
                t
            );

            var interpolatedVelocity = 1.0 / u * MathFuncs.HermiteCubicDerivative(
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
    }

    internal struct AdvSyncProjectileHitController
    {
        public bool IsSet;

        public int Window, Ticks;

        public MyCubeGrid TargetGrid;
        
        // Hit info, in the target's frame:
        public Vector3D HitPositionTarget;
        public Vector3D HitVelocityTarget;
        
        // Kinematic state of the client projectile at receive time:
        public Vector3D InitialPositionWorld;
        public Vector3D InitialVelocityWorld;
        
        public void Step(Projectile projectile)
        {
            if (TargetGrid.Closed || !TargetGrid.InScene)
            {
                IsSet = false;
                projectile.State = Projectile.ProjectileState.Destroy;
                return;
            }
            
            var t = ++Ticks / (double)Window;
            var txTargetWorld = TargetGrid.PositionComp.WorldMatrixRef;
            var hitPositionWorld = Vector3D.Transform(HitPositionTarget, txTargetWorld);
            var hitVelocityWorld = Vector3D.TransformNormal(HitVelocityTarget, txTargetWorld);
            
            if (Ticks >= Window)
            {
                IsSet = false;
                
                projectile.Position = hitPositionWorld;
                projectile.Velocity = hitVelocityWorld;

                if (!Vector3D.IsZero(hitVelocityWorld))
                {
                    Vector3D.Normalize(ref hitVelocityWorld, out projectile.Direction);
                }

                Vector3D.Dot(ref projectile.Velocity, ref projectile.Velocity, out projectile.VelocityLengthSqr);
                
                projectile.Intersecting = true;
                
                if (projectile.Info.AvShot != null)
                {
                    projectile.Info.AvShot.ForceHitParticle = true;
                    projectile.Info.AvShot.Hit = new Hit
                    {
                        Entity = TargetGrid,
                        EventType = HitEntity.Type.Grid,
                        SurfaceHit = hitPositionWorld,
                        LastHit = hitPositionWorld,
                        HitVelocity = hitVelocityWorld,
                        HitTick = Session.I.Tick
                    };
                }

                projectile.State = Projectile.ProjectileState.Depleted;
                
                return;
            }
            
            var u = Window * Session.StepConst;
            var v0 = InitialVelocityWorld * u;
            var v1 = hitVelocityWorld * u;

            projectile.Position = MathFuncs.HermiteCubic(
                ref InitialPositionWorld, ref v0,
                ref v1, ref hitPositionWorld,
                t
            );

            var interpolatedVelocity = 1.0 / u * MathFuncs.HermiteCubicDerivative(
                ref InitialPositionWorld, ref v0,
                ref v1, ref hitPositionWorld,
                t
            );
            
            projectile.Velocity = interpolatedVelocity;

            if (!Vector3D.IsZero(interpolatedVelocity))
            {
                Vector3D.Normalize(ref interpolatedVelocity, out projectile.Direction);
            }

            Vector3D.Dot(ref projectile.Velocity, ref projectile.Velocity, out projectile.VelocityLengthSqr);
        }
    }
}