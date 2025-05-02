using System;
using CoreSystems.Platform;
using VRage.Utils;
using VRageMath;

namespace CoreSystems.Support
{
    internal static class MathFuncs
    {
        internal struct Cone
        {
            internal Vector3D ConeDir;
            internal Vector3D ConeTip;
            internal double ConeAngle;
        }

        internal static bool TargetSphereInCone(ref BoundingSphereD targetSphere, ref Cone cone)
        {
            Vector3D toSphere = targetSphere.Center - cone.ConeTip;
            double angRad = Math.Asin(targetSphere.Radius / toSphere.Length());
            
            var angPos = AngleBetween(cone.ConeDir, toSphere);

            var ang1 = angPos + angRad;
            var ang2 = angPos - angRad;

            if (ang1 < -cone.ConeAngle)
                return false;

            if (ang2 > cone.ConeAngle)
                return false;

            return true;
        }

        internal static Vector3D ShieldHitAngle(MatrixD ellipsoidMatrixInv, MatrixD ellipsoidMatrix, RayD ray)
        {
            var dirMat = MatrixD.CreateWorld(ray.Position, ray.Direction, Vector3D.CalculatePerpendicularVector(ray.Direction));
            var ray1 = new RayD(ray.Position + dirMat.Up * 0.05 + dirMat.Right * 0.05, ray.Direction);
            var ray2 = new RayD(ray.Position + dirMat.Down * 0.05 + dirMat.Right * 0.05, ray.Direction);
            var ray3 = new RayD(ray.Position + dirMat.Left * 0.0707106781186548, ray.Direction);

            var dist1 = IntersectEllipsoid(ellipsoidMatrixInv, ellipsoidMatrix, ray1);
            var dist2 = IntersectEllipsoid(ellipsoidMatrixInv, ellipsoidMatrix, ray2);
            var dist3 = IntersectEllipsoid(ellipsoidMatrixInv, ellipsoidMatrix, ray3);
            if (!dist1.HasValue || !dist2.HasValue || !dist3.HasValue)
                return Vector3D.Zero;

            var hitPos1World = ray1.Position + (ray.Direction * dist1.Value);
            var hitPos2World = ray2.Position + (ray.Direction * dist2.Value);
            var hitPos3World = ray3.Position + (ray.Direction * dist3.Value);
            var plane = new PlaneD(hitPos1World, hitPos2World, hitPos3World);

            if (!plane.Normal.IsValid() || plane.Normal.AbsMax() == 1)
                return Vector3D.Zero;

            return plane.Normal;
        }

        internal static double? IntersectEllipsoid(MatrixD ellipsoidMatrixInv, MatrixD ellipsoidMatrix, RayD ray)
        {
            var normSphere = new BoundingSphereD(Vector3.Zero, 1f);
            var kRay = new RayD(Vector3D.Zero, Vector3D.Forward);

            Vector3D krayPos;
            Vector3D.Transform(ref ray.Position, ref ellipsoidMatrixInv, out krayPos);

            Vector3D nDir;
            Vector3D.TransformNormal(ref ray.Direction, ref ellipsoidMatrixInv, out nDir);

            Vector3D krayDir;
            Vector3D.Normalize(ref nDir, out krayDir);

            kRay.Direction = krayDir;
            kRay.Position = krayPos;
            var nullDist = normSphere.Intersects(kRay);
            if (!nullDist.HasValue) return null;

            var hitPos = krayPos + (krayDir * -nullDist.Value);
            Vector3D worldHitPos;
            Vector3D.Transform(ref hitPos, ref ellipsoidMatrix, out worldHitPos);

            double dist;
            Vector3D.Distance(ref worldHitPos, ref ray.Position, out dist);
            return (double.IsNaN(dist) ? (double?) null : dist);
        }

        internal static bool IsDotProductWithinTolerance(ref Vector3D targetDir, ref Vector3D refDir, double tolerance)
        {
            double dot = Vector3D.Dot(targetDir, refDir);
            double num = targetDir.LengthSquared() * refDir.LengthSquared() * tolerance * Math.Abs(tolerance);
            return Math.Abs(dot) * dot > num;
        }

        public enum DebugCaller
        {
            TrajectoryEstimation,
            CanShootTarget1,
            CanShootTarget2,
            CanShootTarget3,
            CanShootTarget4,
            CanShootTarget5,
            CanShootTarget6,
            CanShootTarget7,
            TrackingTarget,
        }

        internal static bool WeaponLookAt(Weapon weapon, ref Vector3D targetDir, double targetDistSqr, bool setWeapon, bool canSeeOnly, DebugCaller caller, out bool isTracking)
        {
            isTracking = false;
            try
            {
                var system = weapon.System;
                var target = weapon.Target;
                //Get weapon direction and orientation
                Vector3D currentVector;
                Vector3D.CreateFromAzimuthAndElevation(weapon.Azimuth, weapon.Elevation, out currentVector);
                Vector3D.Rotate(ref currentVector, ref weapon.WeaponConstMatrix, out currentVector);

                var up = weapon.MyPivotUp;
                Vector3D left;
                Vector3D.Cross(ref up, ref currentVector, out left);
                if (!Vector3D.IsUnit(ref left) && !Vector3D.IsZero(left)) left.Normalize();
                Vector3D forward;
                Vector3D.Cross(ref left, ref up, out forward);
                var constraintMatrix = new MatrixD { Forward = forward, Left = left, Up = up, };

                // ugly as sin inlined compute GetRotationAngles + AngleBetween, returning the desired az/el doubles;
                MatrixD transposeMatrix;
                MatrixD.Transpose(ref constraintMatrix, out transposeMatrix);

                Vector3D localTargetVector;
                Vector3D.TransformNormal(ref targetDir, ref transposeMatrix, out localTargetVector);

                if (!MyUtils.IsValid(localTargetVector))
                {
                    if (Session.I.Tick - weapon.LastNanTick > 60)
                    {
                        weapon.LastNanTick = Session.I.Tick;
                        Log.Line($"WeaponLookAt:{weapon.System.ShortName} - ammo:{weapon.ActiveAmmoDef.AmmoDef.AmmoRound} - caller:{caller} - targetDir:{targetDir} - MyPivotPos:{weapon.MyPivotPos} - transPoseMatrix:{transposeMatrix} - up:{up} - left:{left} - forward:{forward} - currentVector:{currentVector}");
                    }
                    return false;
                }

                var flattenedTargetVector = new Vector3D(localTargetVector.X, 0, localTargetVector.Z);
                var azVecIsZero = Vector3D.IsZero(flattenedTargetVector);
                var flatSqr = flattenedTargetVector.LengthSquared();

                var desiredAzimuth = azVecIsZero ? 0 : Math.Acos(MathHelperD.Clamp(-flattenedTargetVector.Z / Math.Sqrt(flatSqr), -1, 1)) * -Math.Sign(localTargetVector.X); //right is positive;

                if (Math.Abs(desiredAzimuth) < 1E-6 && localTargetVector.Z > 0) //check for straight back case
                    desiredAzimuth = Math.PI;

                double desiredElevation;
                if (Vector3D.IsZero(flattenedTargetVector)) //check for straight up case
                    desiredElevation = MathHelper.PiOver2 * Math.Sign(localTargetVector.Y);
                else
                {
                    var elVecIsZero = Vector3D.IsZero(localTargetVector) || Vector3D.IsZero(flattenedTargetVector);
                    desiredElevation = elVecIsZero ? 0 : Math.Acos(MathHelperD.Clamp(localTargetVector.Dot(flattenedTargetVector) / Math.Sqrt(localTargetVector.LengthSquared() * flatSqr), -1, 1)) * Math.Sign(localTargetVector.Y); //up is positive
                }

                // return result of desired values being in tolerances
                if (canSeeOnly)
                {
                    if (weapon.Azimuth + desiredAzimuth > weapon.MaxAzToleranceRadians && weapon.MaxAzToleranceRadians < Math.PI)
                        return false;

                    if (weapon.Azimuth + desiredAzimuth < weapon.MinAzToleranceRadians && weapon.MinAzToleranceRadians > -Math.PI)
                        return false;

                    if (desiredElevation < weapon.MinElToleranceRadians || desiredElevation > weapon.MaxElToleranceRadians)
                        return false;

                    return true;
                }

                // check for backAround constraint
                double azToTraverse;
                if (weapon.MaxAzToleranceRadians < Math.PI && weapon.MinAzToleranceRadians > -Math.PI)
                {

                    var azAngle = weapon.Azimuth + desiredAzimuth;
                    if (azAngle > Math.PI)
                    {
                        azAngle -= MathHelperD.TwoPi;
                    }
                    else if (azAngle < -Math.PI)
                    {
                        azAngle = MathHelperD.TwoPi + azAngle;
                    }
                    azToTraverse = azAngle - weapon.Azimuth;
                }
                else
                    azToTraverse = desiredAzimuth;

                // Clamp step within limits.
                var simAzStep = system.AzStep * Session.I.DeltaTimeRatio;
                var simElStep = system.ElStep * Session.I.DeltaTimeRatio;

                var azStep = MathHelperD.Clamp(azToTraverse, -simAzStep, simAzStep);
                var elStep = MathHelperD.Clamp(desiredElevation - weapon.Elevation, -simElStep, simElStep);

                // epsilon based on target type and distance
                var epsilon = target.TargetState == Target.TargetStates.IsProjectile || Session.I.Tick120 ? 1E-06d : targetDistSqr <= 640000 ? 1E-03d : targetDistSqr <= 3240000 ? 1E-04d : 1E-05d;

                // check if step is within epsilon of zero;
                var azLocked = MyUtils.IsZero(azStep, (float)epsilon);
                var elLocked = MyUtils.IsZero(elStep, (float)epsilon);

                // are az and el both within tolerance of target
                var locked = azLocked && elLocked;

                // Compute actual angle to rotate subparts
                var az = weapon.Azimuth + azStep;
                var el = weapon.Elevation + elStep;

                // This is where we should clamp. az and el are measured relative the WorldMatrix.Forward.
                // desiredAzimuth is measured off of the CURRENT heading of the barrel. The limits are based off of
                // WorldMatrix.Forward as well.
                var azHitLimit = false;
                var elHitLimit = false;

                // Check azimuth angles
                if (az > weapon.MaxAzToleranceRadians && weapon.MaxAzToleranceRadians < Math.PI)
                {
                    // Hit upper azimuth limit
                    az = weapon.MaxAzToleranceRadians;
                    azHitLimit = true;
                }
                else if (az < weapon.MinAzToleranceRadians && weapon.MinAzToleranceRadians > -Math.PI)
                {
                    // Hit lower azimuth limit
                    az = weapon.MinAzToleranceRadians;
                    azHitLimit = true;
                }

                // Check elevation angles
                if (el > weapon.MaxElToleranceRadians)
                {
                    // Hit upper elevation limit
                    el = weapon.MaxElToleranceRadians;
                    elHitLimit = true;
                }
                else if (el < weapon.MinElToleranceRadians)
                {
                    // Hit lower elevation limit
                    el = weapon.MinElToleranceRadians;
                    elHitLimit = true;
                }


                // Weapon has a degree of freedom to move towards target
                var tracking = !azHitLimit && !elHitLimit;
                if (setWeapon)
                {
                    isTracking = tracking;

                    if (!azLocked)
                    {
                        weapon.Azimuth = az;
                        weapon.AzimuthTick = Session.I.Tick;
                    }

                    if (!elLocked)
                    {
                        weapon.Elevation = el;
                        weapon.ElevationTick = Session.I.Tick;
                    }
                }
                return !locked;
            }
            catch (Exception ex) { Log.Line($"Exception in WeaponLookAt: {ex}", null, true); }

            return false;
        }

        internal static bool RotorTurretLookAt(ControlSys controlPart, ref Vector3D desiredDirection, double targetDistSqr)
        {
            var root = controlPart.BaseMap;
            var other = controlPart.OtherMap;
            if (root == null || other == null)
                return false;

            //var epsilon = targetDistSqr <= 640000 ? 1E-03d : targetDistSqr <= 3240000 ? 1E-04d : 1E-05d;

            var currentDirection = controlPart.TopAi.RootComp.PrimaryWeapon.GetScope.Info.Direction;
            var axis = Vector3D.Cross(desiredDirection, currentDirection);

            Vector3D up = root.PositionComp.WorldMatrixRef.Up;
            bool upZero = Vector3D.IsZero(up);
            Vector3D desiredFlat = upZero || Vector3D.IsZero(desiredDirection) ? Vector3D.Zero : desiredDirection - desiredDirection.Dot(up) * up;
            Vector3D currentFlat = upZero || Vector3D.IsZero(currentDirection) ? Vector3D.Zero : currentDirection - currentDirection.Dot(up) * up;
            double rootAngle = Vector3D.IsZero(desiredFlat) || Vector3D.IsZero(currentFlat) ? 0 : Math.Acos(MathHelper.Clamp(desiredFlat.Dot(currentFlat) / Math.Sqrt(desiredFlat.LengthSquared() * currentFlat.LengthSquared()), -1, 1));

            rootAngle *= Math.Sign(Vector3D.Dot(axis, up));
            var desiredAngle = root.Angle + rootAngle;
            var rootOutsideLimits = desiredAngle < root.LowerLimitRad && desiredAngle + MathHelper.TwoPi > root.UpperLimitRad;

            up = other.PositionComp.WorldMatrixRef.Up;
            upZero = Vector3D.IsZero(up);
            desiredFlat = upZero || Vector3D.IsZero(desiredDirection) ? Vector3D.Zero : desiredDirection - desiredDirection.Dot(up) * up;
            currentFlat = upZero || Vector3D.IsZero(currentDirection) ? Vector3D.Zero : currentDirection - currentDirection.Dot(up) * up;
            double secondaryAngle = Vector3D.IsZero(desiredFlat) || Vector3D.IsZero(currentFlat) ? 0 : Math.Acos(MathHelper.Clamp(desiredFlat.Dot(currentFlat) / Math.Sqrt(desiredFlat.LengthSquared() * currentFlat.LengthSquared()), -1, 1));

            secondaryAngle *= Math.Sign(Vector3D.Dot(axis, up));
            desiredAngle = other.Angle + secondaryAngle;
            var secondaryOutsideLimits = desiredAngle < other.LowerLimitRad && desiredAngle + MathHelper.TwoPi > other.UpperLimitRad;
            if (rootOutsideLimits && secondaryOutsideLimits)
            {
                return false;
            }

            return true;
        }

        /*
        /// Whip's Get Rotation Angles Method v14 - 9/25/18 ///
        Dependencies: AngleBetween
        */

        internal static double AngleBetween(Vector3D a, Vector3D b) //returns radians
        {
            if (Vector3D.IsZero(a) || Vector3D.IsZero(b))
                return 0;

            return Math.Acos(MathHelperD.Clamp(a.Dot(b) / Math.Sqrt(a.LengthSquared() * b.LengthSquared()), -1, 1));
        }

        internal static void GetRotationAngles(ref Vector3D targetVector, ref MatrixD matrix, out double yaw, out double pitch)
        {
            yaw = 0;
            pitch = 0;

            var localTargetVector = Vector3D.TransformNormal(targetVector, MatrixD.Transpose(matrix));

            if (Vector3D.IsZero(localTargetVector) || !MyUtils.IsValid(localTargetVector)) return;

            var flattenedTargetVector = new Vector3D(localTargetVector.X, 0, localTargetVector.Z);

            if (Vector3D.IsZero(flattenedTargetVector)) {
                pitch = MathHelper.PiOver2 * Math.Sign(localTargetVector.Y);
                return;
            }

            yaw = AngleBetween(Vector3D.Forward, flattenedTargetVector) * -Math.Sign(localTargetVector.X);
            yaw = Math.Abs(yaw) < 1E-6 && localTargetVector.Z > 0 ? Math.PI : yaw;

            pitch = AngleBetween(localTargetVector, flattenedTargetVector) * Math.Sign(localTargetVector.Y);
        }

        internal static float NormalizeAngle(int angle)
        {
            int num = angle % 360;
            if (num == 0 && angle != 0)
                return 360f;
            return num;
        }

        internal static double Intercept(Vector3D deltaPos, Vector3D deltaVel, double projectileVel)
        {
            var num1 = Vector3D.Dot(deltaVel, deltaVel) - projectileVel * projectileVel;
            var num2 = 2.0 * Vector3D.Dot(deltaVel, deltaPos);
            var num3 = Vector3D.Dot(deltaPos, deltaPos);
            var d = num2 * num2 - 4.0 * num1 * num3;
            if (d <= 0.0)
                return -1.0;
            return 2.0 * num3 / (Math.Sqrt(d) - num2);
        }

        public static double LargestCubeInSphere(double r)
        {

            // radius cannot be negative  
            if (r < 0)
                return -1;

            // side of the cube  
            var a = (2 * r) / Math.Sqrt(3);
            return a;
        }

        public static double VolumeCube(double len)
        {
            return Math.Pow(len, 3);
        }

        public static double GetMedian(int[] array)
        {
            int[] tempArray = array;
            int count = tempArray.Length;

            Array.Sort(tempArray);

            double medianValue = 0;

            if (count % 2 == 0)
            {
                // count is even, need to get the middle two elements, add them together, then divide by 2
                int middleElement1 = tempArray[(count / 2) - 1];
                int middleElement2 = tempArray[(count / 2)];
                medianValue = (middleElement1 + middleElement2) / 2;
            }
            else
            {
                // count is odd, simply get the middle element.
                medianValue = tempArray[(count / 2)];
            }

            return medianValue;
        }
    }

}
