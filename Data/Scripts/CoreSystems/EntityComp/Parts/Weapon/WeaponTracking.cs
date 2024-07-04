using System;
using CoreSystems.Projectiles;
using CoreSystems.Support;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;
using static CoreSystems.Support.WeaponDefinition.HardPointDef;
using static CoreSystems.Support.WeaponDefinition.AmmoDef;
using static CoreSystems.Support.WeaponDefinition.AnimationDef.PartAnimationSetDef;
using CollisionLayers = Sandbox.Engine.Physics.MyPhysics.CollisionLayers;
using static CoreSystems.Support.MathFuncs;
using VRage.Game.ObjectBuilders.Components;

namespace CoreSystems.Platform
{
    public partial class Weapon
    {
        internal static bool CanShootTarget(Weapon weapon, ref Vector3D targetCenter, Vector3D targetLinVel, Vector3D targetAccel, out Vector3D targetPos, bool checkSelfHit = false, MyEntity target = null, DebugCaller caller = DebugCaller.CanShootTarget1)
        {
            if (weapon.PosChangedTick != Session.I.SimulationCount)
                weapon.UpdatePivotPos();

            var prediction = weapon.System.Values.HardPoint.AimLeadingPrediction;
            var trackingWeapon = weapon.TurretController ? weapon : weapon.Comp.PrimaryWeapon;
            if (Vector3D.IsZero(targetLinVel, 5E-03)) targetLinVel = Vector3.Zero;
            if (Vector3D.IsZero(targetAccel, 5E-03)) targetAccel = Vector3.Zero;

            var validEstimate = true;
            if (prediction != Prediction.Off && !weapon.ActiveAmmoDef.AmmoDef.Const.IsBeamWeapon && weapon.ActiveAmmoDef.AmmoDef.Const.DesiredProjectileSpeed * weapon.VelocityMult > 0)
                targetPos = TrajectoryEstimation(weapon, targetCenter, targetLinVel, targetAccel, weapon.MyPivotPos, out validEstimate, true);
            else
                targetPos = targetCenter;
            var targetDir = targetPos - weapon.MyPivotPos;

            double rangeToTarget;
            Vector3D.DistanceSquared(ref targetPos, ref weapon.MyPivotPos, out rangeToTarget);

            var inRange = rangeToTarget <= weapon.MaxTargetDistanceSqr && rangeToTarget >= weapon.MinTargetDistanceSqr;
            bool canTrack;
            bool isTracking;

            if (weapon.RotorTurretTracking)
                canTrack = validEstimate && weapon.Comp.Ai.ControlComp != null && RotorTurretLookAt(weapon.Comp.Ai.ControlComp.Platform.Control, ref targetDir, rangeToTarget);
            else if (weapon == trackingWeapon && weapon.TurretController)
                canTrack = validEstimate && WeaponLookAt(weapon, ref targetDir, rangeToTarget, false, true, caller, out isTracking);
            else
                canTrack = validEstimate && IsDotProductWithinTolerance(ref weapon.MyPivotFwd, ref targetDir, weapon.AimingTolerance);

            bool selfHit = false;
            weapon.LastHitInfo = null;
            if (checkSelfHit && target != null && !weapon.ActiveAmmoDef.AmmoDef.Const.SkipRayChecks)
            {
                var testLine = new LineD(targetCenter, weapon.BarrelOrigin);
                var predictedMuzzlePos = testLine.To + (-testLine.Direction * weapon.MuzzleDistToBarrelCenter);
                var ai = weapon.Comp.Ai;
                var clear = ai.AiType != Ai.AiTypes.Grid;

                if (!clear)
                {
                    var localPredictedPos = Vector3I.Round(Vector3D.Transform(predictedMuzzlePos, ai.GridEntity.PositionComp.WorldMatrixNormalizedInv) * ai.GridEntity.GridSizeR);

                    MyCube cube;
                    var noCubeAtPosition = !ai.GridEntity.TryGetCube(localPredictedPos, out cube);
                    if (noCubeAtPosition || cube.CubeBlock == weapon.Comp.Cube.SlimBlock)
                    {
                        var noCubeInLine = !ai.GridEntity.GetIntersectionWithLine(ref testLine, ref ai.GridHitInfo);
                        clear = noCubeInLine || ai.GridHitInfo.Position == weapon.Comp.Cube.Position;
                    }
                }

                if (clear)
                {
                    var oneHalfKmSqr = 2250000;
                    var lowFiVoxels = Vector3D.DistanceSquared(targetCenter, predictedMuzzlePos) > oneHalfKmSqr && (ai.PlanetSurfaceInRange || ai.ClosestVoxelSqr <= oneHalfKmSqr);
                    var filter = weapon.System.NoVoxelLosCheck ? CollisionLayers.NoVoxelCollisionLayer : lowFiVoxels ? CollisionLayers.DefaultCollisionLayer : CollisionLayers.VoxelLod1CollisionLayer;
                    Session.I.Physics.CastRay(predictedMuzzlePos, testLine.From, out weapon.LastHitInfo, filter);

                    if (ai.AiType == Ai.AiTypes.Grid && weapon.LastHitInfo != null && weapon.LastHitInfo.HitEntity == ai.GridEntity)
                        selfHit = true;
                }
                else selfHit = true;
            }

            return !selfHit && (inRange && canTrack || weapon.Comp.Data.Repo.Values.State.TrackingReticle);
        }


        internal static Vector3D LeadTargetAiBlock(Weapon weapon, MyEntity target)
        {
            var targetPos = target.PositionComp.WorldAABB.Center;
            var targParent = target.GetTopMostParent();
            if (targParent.Physics != null)
            {
                var vel = targParent.Physics.LinearVelocity;
                var accel = targParent.Physics.LinearAcceleration;
                bool validEstimate;
                if (!weapon.ActiveAmmoDef.AmmoDef.Const.IsBeamWeapon && weapon.ActiveAmmoDef.AmmoDef.Const.DesiredProjectileSpeed * weapon.VelocityMult > 0)
                    targetPos = TrajectoryEstimation(weapon, targetPos, vel, accel, weapon.Comp.Cube.PositionComp.WorldAABB.Center, out validEstimate, false, weapon.Comp.Data.Repo.Values.Set.Overrides.AngularTracking);
            }
            return targetPos;
        }


        internal static void LeadTarget(Weapon weapon, MyEntity target, out Vector3D targetPos, out bool couldHit, out bool willHit)
        {
            if (weapon.PosChangedTick != Session.I.SimulationCount)
                weapon.UpdatePivotPos();
            
            var vel = target.Physics.LinearVelocity;
            var accel = target.Physics.LinearAcceleration;
            
            var trackingWeapon = weapon.TurretController || weapon.Comp.PrimaryWeapon == null ? weapon : weapon.Comp.PrimaryWeapon;

            var box = target.PositionComp.LocalAABB;
            var obb = new MyOrientedBoundingBoxD(box, target.PositionComp.WorldMatrixRef);

            var validEstimate = true;

            if (!weapon.ActiveAmmoDef.AmmoDef.Const.IsBeamWeapon && weapon.ActiveAmmoDef.AmmoDef.Const.DesiredProjectileSpeed * weapon.VelocityMult > 0)
                targetPos = TrajectoryEstimation(weapon, obb.Center, vel, accel, weapon.MyPivotPos,  out validEstimate, false, weapon.Comp.Data.Repo.Values.Set.Overrides.AngularTracking);
            else
                targetPos = obb.Center;

            obb.Center = targetPos;
            weapon.TargetBox = obb;

            //var obbAbsMax = obb.HalfExtent.AbsMax();
            //var maxRangeSqr = obbAbsMax + weapon.MaxTargetDistance;
            //var minRangeSqr = obbAbsMax + weapon.MinTargetDistance;

            //maxRangeSqr *= maxRangeSqr;
            //minRangeSqr *= minRangeSqr;
            //double rangeToTarget;
            //Vector3D.DistanceSquared(ref targetPos, ref weapon.MyPivotPos, out rangeToTarget);
            //couldHit = validEstimate && rangeToTarget <= maxRangeSqr && rangeToTarget >= minRangeSqr;
            couldHit = validEstimate;

            bool canTrack = false;
            //if (validEstimate && rangeToTarget <= maxRangeSqr && rangeToTarget >= minRangeSqr)
            if (validEstimate)
            {
                var targetDir = targetPos - weapon.MyPivotPos;

                if (weapon == trackingWeapon)
                {
                    double checkAzimuth;
                    double checkElevation;

                    GetRotationAngles(ref targetDir, ref weapon.WeaponConstMatrix, out checkAzimuth, out checkElevation);

                    var azConstraint = Math.Min(weapon.MaxAzToleranceRadians, Math.Max(weapon.MinAzToleranceRadians, checkAzimuth));
                    var elConstraint = Math.Min(weapon.MaxElToleranceRadians, Math.Max(weapon.MinElToleranceRadians, checkElevation));

                    Vector3D constraintVector;
                    Vector3D.CreateFromAzimuthAndElevation(azConstraint, elConstraint, out constraintVector);
                    Vector3D.Rotate(ref constraintVector, ref weapon.WeaponConstMatrix, out constraintVector);

                    var testRay = new RayD(ref weapon.MyPivotPos, ref constraintVector);
                    if (obb.Intersects(ref testRay) != null)
                        canTrack = true;

                    if (weapon.Comp.Debug)
                        weapon.LimitLine = new LineD(weapon.MyPivotPos, weapon.MyPivotPos + (constraintVector * weapon.ActiveAmmoDef.AmmoDef.Const.MaxTrajectory));
                }
                else
                    canTrack = IsDotProductWithinTolerance(ref weapon.MyPivotFwd, ref targetDir, weapon.AimingTolerance);
            }
            willHit = canTrack;
        }

        internal static bool CanShootTargetObb(Weapon weapon, MyEntity entity, Vector3D targetLinVel, Vector3D targetAccel, out Vector3D targetPos)
        {   
            if (weapon.PosChangedTick != Session.I.SimulationCount)
                weapon.UpdatePivotPos();

            var prediction = weapon.System.Values.HardPoint.AimLeadingPrediction;
            var trackingWeapon = weapon.TurretController ? weapon : weapon.Comp.PrimaryWeapon;

            if (Vector3D.IsZero(targetLinVel, 5E-03)) targetLinVel = Vector3.Zero;
            if (Vector3D.IsZero(targetAccel, 5E-03)) targetAccel = Vector3.Zero;

            var box = entity.PositionComp.LocalAABB;
            var obb = new MyOrientedBoundingBoxD(box, entity.PositionComp.WorldMatrixRef);
            var tempObb = obb;
            var validEstimate = true;
            
            if (prediction != Prediction.Off && !weapon.ActiveAmmoDef.AmmoDef.Const.IsBeamWeapon && weapon.ActiveAmmoDef.AmmoDef.Const.DesiredProjectileSpeed * weapon.VelocityMult > 0)
                targetPos = TrajectoryEstimation(weapon, obb.Center, targetLinVel, targetAccel, weapon.MyPivotPos,  out validEstimate, true);
            else
                targetPos = obb.Center;

            obb.Center = targetPos;
            weapon.TargetBox = obb;

            var obbAbsMax = obb.HalfExtent.AbsMax();
            var maxRangeSqr = obbAbsMax + weapon.MaxTargetDistance;
            var minRangeSqr = obbAbsMax + weapon.MinTargetDistance;

            maxRangeSqr *= maxRangeSqr;
            minRangeSqr *= minRangeSqr;
            double rangeToTarget;
            if (weapon.ActiveAmmoDef.AmmoDef.Const.FeelsGravity) Vector3D.DistanceSquared(ref tempObb.Center, ref weapon.MyPivotPos, out rangeToTarget);
            else Vector3D.DistanceSquared(ref targetPos, ref weapon.MyPivotPos, out rangeToTarget);

            bool canTrack = false;
            if (validEstimate && rangeToTarget <= maxRangeSqr && rangeToTarget >= minRangeSqr)
            {
                var targetDir = targetPos - weapon.MyPivotPos;
                if (weapon.RotorTurretTracking)
                    canTrack = weapon.Comp.Ai.ControlComp != null && RotorTurretLookAt(weapon.Comp.Ai.ControlComp.Platform.Control, ref targetDir, rangeToTarget);
                else if (weapon == trackingWeapon)
                {
                    double checkAzimuth;
                    double checkElevation;

                    GetRotationAngles(ref targetDir, ref weapon.WeaponConstMatrix, out checkAzimuth, out checkElevation);
                    var azConstraint = Math.Min(weapon.MaxAzToleranceRadians, Math.Max(weapon.MinAzToleranceRadians, checkAzimuth));
                    var elConstraint = Math.Min(weapon.MaxElToleranceRadians, Math.Max(weapon.MinElToleranceRadians, checkElevation));

                    Vector3D constraintVector;
                    Vector3D.CreateFromAzimuthAndElevation(azConstraint, elConstraint, out constraintVector);
                    Vector3D.Rotate(ref constraintVector, ref weapon.WeaponConstMatrix, out constraintVector);

                    var testRay = new RayD(ref weapon.MyPivotPos, ref constraintVector);
                    if (obb.Intersects(ref testRay) != null) canTrack = true;

                    if (weapon.Comp.Debug)
                        weapon.LimitLine = new LineD(weapon.MyPivotPos, weapon.MyPivotPos + (constraintVector * weapon.ActiveAmmoDef.AmmoDef.Const.MaxTrajectory));
                }
                else
                    canTrack = IsDotProductWithinTolerance(ref weapon.MyPivotFwd, ref targetDir, weapon.AimingTolerance);
            }
            return canTrack;
        }

        internal static bool TargetAligned(Weapon weapon, Target target, out Vector3D targetPos)
        {

            if (weapon.PosChangedTick != Session.I.SimulationCount)
                weapon.UpdatePivotPos();

            Vector3 targetLinVel = Vector3.Zero;
            Vector3 targetAccel = Vector3.Zero;
            Vector3D targetCenter;

            Ai.FakeTarget.FakeWorldTargetInfo fakeTargetInfo = null;
            var pTarget = target.TargetObject as Projectile;
            var tEntity = target.TargetObject as MyEntity;
            var overrides = weapon.Comp.Data.Repo.Values.Set.Overrides;
            if (overrides.Control != ProtoWeaponOverrides.ControlModes.Auto && weapon.ValidFakeTargetInfo(weapon.Comp.Data.Repo.Values.State.PlayerId, out fakeTargetInfo))
            {
                targetCenter = fakeTargetInfo.WorldPosition;
            }
            else if (target.TargetState == Target.TargetStates.IsProjectile)
                targetCenter = pTarget?.Position ?? Vector3D.Zero;
            else if (target.TargetState != Target.TargetStates.IsFake)
                targetCenter = tEntity?.PositionComp.WorldAABB.Center ?? Vector3D.Zero;
            else
                targetCenter = Vector3D.Zero;

            var validEstimate = true;
            if (weapon.System.Prediction != Prediction.Off && (!weapon.ActiveAmmoDef.AmmoDef.Const.IsBeamWeapon && weapon.ActiveAmmoDef.AmmoDef.Const.DesiredProjectileSpeed * weapon.VelocityMult > 0))
            {

                if (fakeTargetInfo != null)
                {
                    targetLinVel = fakeTargetInfo.LinearVelocity;
                    targetAccel = fakeTargetInfo.Acceleration;
                }
                else
                {

                    var cube = tEntity as MyCubeBlock;
                    var topMostEnt = cube != null ? cube.CubeGrid : tEntity;

                    if (pTarget != null)
                    {
                        targetLinVel = pTarget.Velocity;
                        targetAccel = pTarget.Velocity - pTarget.PrevVelocity;
                    }
                    else if (topMostEnt?.Physics != null)
                    {
                        targetLinVel = topMostEnt.Physics.LinearVelocity;
                        targetAccel = topMostEnt.Physics.LinearAcceleration;
                    }
                }
                if (Vector3D.IsZero(targetLinVel, 5E-03)) targetLinVel = Vector3.Zero;
                if (Vector3D.IsZero(targetAccel, 5E-03)) targetAccel = Vector3.Zero;

                targetPos = TrajectoryEstimation(weapon, targetCenter, targetLinVel, targetAccel, weapon.MyPivotPos,  out validEstimate);
            }
            else
                targetPos = targetCenter;

            var targetDir = targetPos - weapon.MyPivotPos;

            double rangeToTarget;
            Vector3D.DistanceSquared(ref targetPos, ref weapon.MyPivotPos, out rangeToTarget);
            var inRange = rangeToTarget <= weapon.MaxTargetDistanceSqr && rangeToTarget >= weapon.MinTargetDistanceSqr;

            var isAligned = validEstimate && (inRange || weapon.Comp.Data.Repo.Values.State.TrackingReticle) && IsDotProductWithinTolerance(ref weapon.MyPivotFwd, ref targetDir, weapon.AimingTolerance);

            weapon.Target.TargetPos = targetPos;
            var wasAligned = weapon.Target.IsAligned;
            weapon.Target.IsAligned = isAligned;

            if (wasAligned != isAligned)
                weapon.EventTriggerStateChanged(EventTriggers.TargetAligned, isAligned);

            return isAligned;
        }

        internal static bool TrackingTarget(Weapon w, Target target, out bool targetLock)
        {
            Vector3D targetPos;
            Vector3 targetLinVel = Vector3.Zero;
            Vector3 targetAccel = Vector3.Zero;
            Vector3D targetCenter;
            targetLock = false;

            var baseData = w.Comp.Data.Repo.Values;
            var session = Session.I;
            var ai = w.Comp.MasterAi;
            var pTarget = target.TargetObject as Projectile;
            var tEntity = target.TargetObject as MyEntity;

            Ai.FakeTarget.FakeWorldTargetInfo fakeTargetInfo = null;
            if (w.Comp.FakeMode && w.ValidFakeTargetInfo(baseData.State.PlayerId, out fakeTargetInfo))
                targetCenter = fakeTargetInfo.WorldPosition;
            else if (target.TargetState == Target.TargetStates.IsProjectile)
                targetCenter = pTarget?.Position ?? Vector3D.Zero;
            else if (target.TargetState != Target.TargetStates.IsFake)
                targetCenter = tEntity?.PositionComp.WorldAABB.Center ?? Vector3D.Zero;
            else
                targetCenter = Vector3D.Zero;

            var validEstimate = true;
            if (w.System.Prediction != Prediction.Off && !w.ActiveAmmoDef.AmmoDef.Const.IsBeamWeapon && w.ActiveAmmoDef.AmmoDef.Const.DesiredProjectileSpeed * w.VelocityMult > 0)
            {

                if (fakeTargetInfo != null)
                {
                    targetLinVel = fakeTargetInfo.LinearVelocity;
                    targetAccel = fakeTargetInfo.Acceleration;
                }
                else
                {
                    var cube = tEntity as MyCubeBlock;
                    var topMostEnt = cube != null ? cube.CubeGrid : tEntity;

                    if (pTarget != null)
                    {
                        targetLinVel = pTarget.Velocity;
                        targetAccel = pTarget.Velocity - pTarget.PrevVelocity;
                    }
                    else if (topMostEnt?.Physics != null)
                    {
                        targetLinVel = topMostEnt.Physics.LinearVelocity;
                        targetAccel = topMostEnt.Physics.LinearAcceleration;
                    }
                }
                if (Vector3D.IsZero(targetLinVel, 5E-03)) targetLinVel = Vector3.Zero;
                if (Vector3D.IsZero(targetAccel, 5E-03)) targetAccel = Vector3.Zero;

                targetPos = TrajectoryEstimation(w, targetCenter, targetLinVel, targetAccel, w.MyPivotPos,  out validEstimate, false, baseData.Set.Overrides.AngularTracking);
            }
            else
                targetPos = targetCenter;

            w.Target.TargetPos = targetPos;

            double rangeToTargetSqr;
            Vector3D.DistanceSquared(ref targetPos, ref w.MyPivotPos, out rangeToTargetSqr);

            var r100 = rangeToTargetSqr >= w.MaxTargetDistance75Sqr;
            var r25 = rangeToTargetSqr <= w.MaxTargetDistance25Sqr;
            var r50 = !r25 && !r100  && rangeToTargetSqr < w.MaxTargetDistance50Sqr;
            var r75 = !r100 && !r50 && !r25;

            if (r100 && (w.PrevRangeEvent != EventTriggers.TargetRanged100 || !w.RangeEventActive))
                w.EventTriggerStateChanged(EventTriggers.TargetRanged100, true);
            else if (r75 && (w.PrevRangeEvent != EventTriggers.TargetRanged75 || !w.RangeEventActive))
                w.EventTriggerStateChanged(EventTriggers.TargetRanged75, true);
            else if (r50 && (w.PrevRangeEvent != EventTriggers.TargetRanged50 || !w.RangeEventActive))
                w.EventTriggerStateChanged(EventTriggers.TargetRanged50, true);
            else if (r25 && (w.PrevRangeEvent != EventTriggers.TargetRanged25 || !w.RangeEventActive))
                w.EventTriggerStateChanged(EventTriggers.TargetRanged25, true);


            var targetDir = targetPos - w.MyPivotPos;
            var readyToTrack = validEstimate && !w.Comp.ResettingSubparts && (w.Comp.ManualMode || w.Comp.PainterMode && rangeToTargetSqr <= w.ActiveAmmoDef.AmmoDef.Const.MaxTrajectorySqr && rangeToTargetSqr >= w.MinTargetDistanceSqr || rangeToTargetSqr <= w.MaxTargetDistanceSqr && rangeToTargetSqr >= w.MinTargetDistanceSqr);
            var locked = true;
            var isTracking = false;

            if (readyToTrack && w.PosChangedTick != Session.I.SimulationCount)
                w.UpdatePivotPos();

            if (readyToTrack && baseData.State.Control != ProtoWeaponState.ControlMode.Camera)
            {
                if (WeaponLookAt(w, ref targetDir, rangeToTargetSqr, true, false, DebugCaller.TrackingTarget, out isTracking))
                {

                    w.ReturingHome = false;
                    locked = false;
                    
                    w.AimBarrel();
                    if (isTracking) 
                        w.LookAtFailCount = 0;
                    if (w.LookAtFailCount++ < 1)
                        isTracking = true;
                }
            }

            w.Rotating = !locked;

            if (w.HasHardPointSound && w.PlayingHardPointSound && !w.Rotating)
                w.StopHardPointSound();

            var isAligned = false;

            if (isTracking)
                isAligned = IsDotProductWithinTolerance(ref w.MyPivotFwd, ref targetDir, w.AimingTolerance);

            var wasAligned = w.Target.IsAligned;
            w.Target.IsAligned = isAligned;

            var alignedChange = wasAligned != isAligned;
            if (w.System.DesignatorWeapon && session.IsServer && alignedChange)
            {
                for (int i = 0; i < w.Comp.Platform.Weapons.Count; i++)
                {
                    var weapon = w.Comp.Platform.Weapons[i];
                    var designator = weapon.System.DesignatorWeapon;
                    if (isAligned && !designator)
                        weapon.Target.Reset(session.Tick, Target.States.Designator);
                    else if (!isAligned && designator)
                        weapon.Target.Reset(session.Tick, Target.States.Designator);
                }
            }

            targetLock = isTracking && w.Target.IsAligned;


            if (baseData.State.Control == ProtoWeaponState.ControlMode.Camera || w.Comp.FakeMode || session.IsServer && baseData.Set.Overrides.Repel && ai.DetectionInfo.DroneInRange && target.IsDrone && (session.AwakeCount == w.Acquire.SlotId || ai.Construct.RootAi.Construct.LastDroneTick == session.Tick) && Ai.SwitchToDrone(w))
                return true;

            var rayCheckTest = isTracking && (isAligned || locked) && baseData.State.Control != ProtoWeaponState.ControlMode.Camera && (w.ActiveAmmoDef.AmmoDef.Trajectory.Guidance != TrajectoryDef.GuidanceType.Smart && w.ActiveAmmoDef.AmmoDef.Trajectory.Guidance != TrajectoryDef.GuidanceType.DroneAdvanced) && !w.System.DisableLosCheck && (!w.Casting && session.Tick - w.Comp.LastRayCastTick > 29 || w.System.Values.HardPoint.Other.MuzzleCheck && session.Tick - w.LastMuzzleCheck > 29);
            
            var trackingTimeLimit = w.System.MaxTrackingTime && session.Tick - w.Target.ChangeTick > w.System.MaxTrackingTicks;
            if (session.IsServer && (rayCheckTest && !w.RayCheckTest(rangeToTargetSqr) || trackingTimeLimit))
            {
                if (trackingTimeLimit)
                    w.FastTargetResetTick = session.Tick + 1;
                return false;
            }

            return isTracking;
        }

        private const int LosMax = 10;
        private int _losAngle = 11;
        private bool _increase;
        private int GetAngle()
        {
            if (_increase && _losAngle + 1 <= LosMax)
                ++_losAngle;
            else if (_increase)
            {
                _increase = false;
                _losAngle = 9;
            }
            else if (_losAngle - 1 > 0)
                --_losAngle;
            else
            {
                _increase = true;
                _losAngle = 2;
            }
            return _losAngle;
        }

        public bool TargetInRange(MyEntity target)
        {
            var worldVolume = target.PositionComp.WorldVolume;
            var targetPos = worldVolume.Center;
            var tRadius = worldVolume.Radius;
            var maxRangeSqr = tRadius + MaxTargetDistance;
            var minRangeSqr = tRadius + MinTargetDistance;

            maxRangeSqr *= maxRangeSqr;
            minRangeSqr *= minRangeSqr;

            double rangeToTarget;
            Vector3D.DistanceSquared(ref targetPos, ref MyPivotPos, out rangeToTarget);
            var inRange = rangeToTarget <= maxRangeSqr && rangeToTarget >= minRangeSqr;
            var block = target as MyCubeBlock;
            var overrides = Comp.Data.Repo.Values.Set.Overrides;
            return inRange && (block == null || !overrides.FocusSubSystem || overrides.SubSystem == WeaponDefinition.TargetingDef.BlockTypes.Any || ValidSubSystemTarget(block, overrides.SubSystem));
        }

        public bool SmartLos()
        {
            _losAngle = 11;
            Comp.Data.Repo.Values.Set.Overrides.Debug = false;
            PauseShoot = false;
            LastSmartLosCheck = Session.I.Tick;
            if (PosChangedTick != Session.I.SimulationCount)
                UpdatePivotPos();
            var info = GetScope.Info;

            var checkLevel = Comp.Ai.IsStatic ? 1 : 5;
            bool losBlocked = false;
            for (int j = 0; j < 10; j++)
            {

                if (losBlocked)
                    break;

                var angle = GetAngle();
                int blockedDir = 0;
                for (int i = 0; i < checkLevel; i++)
                {

                    var source = GetSmartLosPosition(i, ref info, angle);

                    IHitInfo hitInfo;
                    var filter = CollisionLayers.NoVoxelCollisionLayer;
                    Session.I.Physics.CastRay(source, info.Position, out hitInfo, (uint) filter, false);
                    var grid = hitInfo?.HitEntity?.GetTopMostParent() as MyCubeGrid;
                    if (grid != null && grid.IsInSameLogicalGroupAs(Comp.Ai.GridEntity) && grid.GetTargetedBlock(hitInfo.Position + (-info.Direction * 0.1f)) != Comp.Cube.SlimBlock)
                    {

                        if (i == 0)
                            blockedDir = 5;
                        else
                            ++blockedDir;

                        if (blockedDir >= 4 || i > 0 && i > blockedDir)
                            break;
                    }
                }
                losBlocked = blockedDir >= 4;
            }

            PauseShoot = losBlocked;

            if (!Session.I.DedicatedServer && PauseShoot && Session.I.Tick - Session.I.LosNotifyTick > 600 && Session.I.PlayerId == Comp.Data.Repo.Values.State.PlayerId)
            {
                Session.I.LosNotifyTick = Session.I.Tick;
                Session.I.ShowLocalNotify($"{System.ShortName} is a homing weapon and it has no line of sight", 10000);
            }

            return !PauseShoot;
        }

        private Vector3D GetSmartLosPosition(int i, ref Dummy.DummyInfo info, int degrees)
        {
            double angle = MathHelperD.ToRadians(degrees);
            var perpDir = Vector3D.CalculatePerpendicularVector(info.Direction);
            Vector3D up;
            Vector3D.Normalize(ref perpDir, out up);
            Vector3D right;
            Vector3D.Cross(ref info.Direction, ref up, out right);
            var offset = Math.Tan(angle); // angle better be in radians

            var destPos = info.Position;

            switch (i)
            {
                case 0:
                    return destPos + (info.Direction * Comp.Ai.TopEntityVolume.Radius);
                case 1:
                    return destPos + ((info.Direction + up * offset) * Comp.Ai.TopEntityVolume.Radius);
                case 2:
                    return destPos + ((info.Direction - up * offset) * Comp.Ai.TopEntityVolume.Radius);
                case 3:
                    return destPos + ((info.Direction + right * offset) * Comp.Ai.TopEntityVolume.Radius);
                case 4:
                    return destPos + ((info.Direction - right * offset) * Comp.Ai.TopEntityVolume.Radius);
            }

            return Vector3D.Zero;
        }

        internal void SmartLosDebug()
        {
            if (PosChangedTick != Session.I.SimulationCount)
                UpdatePivotPos();

            var info = GetScope.Info;

            var checkLevel = Comp.Ai.IsStatic ? 1 : 5;
            var angle = Session.I.Tick20 ? GetAngle() : _losAngle;
            for (int i = 0; i < checkLevel; i++)
            {
                var source = GetSmartLosPosition(i, ref info, angle);
                IHitInfo hitInfo;
                var filter = CollisionLayers.NoVoxelCollisionLayer;
                Session.I.Physics.CastRay(source, info.Position, out hitInfo, (uint) filter, false);
                var grid = hitInfo?.HitEntity?.GetTopMostParent() as MyCubeGrid;
                var hit = grid != null && grid.IsInSameLogicalGroupAs(Comp.Ai.GridEntity) && grid.GetTargetedBlock(hitInfo.Position + (-info.Direction * 0.1f)) != Comp.Cube.SlimBlock;

                var line = new LineD(source, info.Position);
                DsDebugDraw.DrawLine(line, hit ? Color.Red : Color.Blue, 0.05f);
            }
        }

        internal static Vector3D TrajectoryEstimationOld(Weapon weapon, Vector3D targetPos, Vector3D targetVel, Vector3D targetAcc, Vector3D shooterPos, bool trackAngular, out bool valid, bool overrideMode = false, bool setAdvOverride = false, bool skipAccel = false)
        {
            valid = true;
            var comp = weapon.Comp;
            var ai = comp.Ai;
            var session = Session.I;
            var ammoDef = weapon.ActiveAmmoDef.AmmoDef;
            var origTargetPos = targetPos; //Need these original values for debug draws later
            var origTargetVel = targetVel;

            if (ai.VelocityUpdateTick != session.Tick)
            {
                ai.TopEntityVolume.Center = comp.TopEntity.PositionComp.WorldVolume.Center;
                ai.TopEntityVel = comp.TopEntity.Physics?.LinearVelocity ?? Vector3D.Zero;
                ai.IsStatic = comp.TopEntity.Physics?.IsStatic ?? false;
                ai.VelocityUpdateTick = session.Tick;
            }

            var updateGravity = ammoDef.Const.FeelsGravity && ai.InPlanetGravity;

            if (updateGravity && session.Tick - weapon.GravityTick > 119)
            {
                weapon.GravityTick = session.Tick;
                float interference;
                weapon.GravityPoint = session.Physics.CalculateNaturalGravityAt(weapon.MyPivotPos, out interference);
                weapon.GravityUnitDir = weapon.GravityPoint;
                weapon.GravityLength = weapon.GravityUnitDir.Normalize();
            }
            else if (!updateGravity)
                weapon.GravityPoint = Vector3D.Zero;

            var gravityMultiplier = ammoDef.Const.FeelsGravity && !MyUtils.IsZero(weapon.GravityPoint) ? ammoDef.Const.GravityMultiplier : 0f;
            bool hasGravity = gravityMultiplier > 1e-6 && !MyUtils.IsZero(weapon.GravityPoint);

            var targetMaxSpeed = Session.I.MaxEntitySpeed;
            shooterPos = MyUtils.IsZero(shooterPos) ? weapon.MyPivotPos : shooterPos;

            var shooterVel = (Vector3D)weapon.Comp.Ai.TopEntityVel;
            var projectileMaxSpeed = ammoDef.Const.DesiredProjectileSpeed * weapon.VelocityMult;
            var projectileInitSpeed = ammoDef.Trajectory.AccelPerSec * MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
            var projectileAccMag = ammoDef.Trajectory.AccelPerSec;
            var basic = weapon.System.Prediction != Prediction.Advanced && !overrideMode || overrideMode && !setAdvOverride;
            
            if (basic && weapon.System.Prediction == Prediction.Accurate && hasGravity && ai.InPlanetGravity)
            {
                basic = false;
                skipAccel = true;
            }

            Vector3D deltaPos = targetPos - shooterPos;
            Vector3D deltaVel = targetVel - shooterVel;
            Vector3D deltaPosNorm;
            double deltaLength = 0;
            if (Vector3D.IsZero(deltaPos))
            {
                deltaPosNorm = Vector3D.Zero;
            }
            else if (Vector3D.IsUnit(ref deltaPos))
            {
                deltaPosNorm = deltaPos;
                deltaLength = 1;
            }
            else
            {
                deltaPosNorm = deltaPos;
                deltaLength = deltaPosNorm.Normalize();
            }

            double closingSpeed;
            Vector3D.Dot(ref deltaVel, ref deltaPosNorm, out closingSpeed);

            Vector3D closingVel = closingSpeed * deltaPosNorm;
            Vector3D lateralVel = deltaVel - closingVel;
            double projectileMaxSpeedSqr = projectileMaxSpeed * projectileMaxSpeed;
            double ttiDiff = projectileMaxSpeedSqr - lateralVel.LengthSquared();

            if (ttiDiff < 0)
            {
                valid = false;
                return targetPos;
            }

            double projectileClosingSpeed = Math.Sqrt(ttiDiff) - closingSpeed;

            double closingDistance;
            Vector3D.Dot(ref deltaPos, ref deltaPosNorm, out closingDistance);

            double timeToIntercept = ttiDiff < 0 ? 0 : closingDistance / projectileClosingSpeed;

            if (timeToIntercept < 0)
            {
                valid = false;
                return targetPos;
            }

            double maxSpeedSqr = targetMaxSpeed * targetMaxSpeed;
            double shooterVelScaleFactor = 1;
            bool projectileAccelerates = projectileAccMag > 1e-6;

            if (!basic && projectileAccelerates)
                shooterVelScaleFactor = Math.Min(1, (projectileMaxSpeed - projectileInitSpeed) / projectileAccMag);

            Vector3D estimatedImpactPoint = targetPos + timeToIntercept * (targetVel - shooterVel * shooterVelScaleFactor);
            
            if (basic)
                return estimatedImpactPoint;

            Vector3D aimDirection = estimatedImpactPoint - shooterPos;

            Vector3D projectileVel = shooterVel;
            Vector3D projectilePos = shooterPos;

            Vector3D aimDirectionNorm;
            if (projectileAccelerates)
            {

                if (Vector3D.IsZero(deltaPos)) aimDirectionNorm = Vector3D.Zero;
                else if (Vector3D.IsUnit(ref deltaPos)) aimDirectionNorm = aimDirection;
                else aimDirectionNorm = Vector3D.Normalize(aimDirection);
                projectileVel += aimDirectionNorm * projectileInitSpeed;
            }
            else
            {

                if (targetAcc.LengthSquared() < 1 && !hasGravity)
                    return estimatedImpactPoint;

                if (Vector3D.IsZero(deltaPos)) aimDirectionNorm = Vector3D.Zero;
                else if (Vector3D.IsUnit(ref deltaPos)) aimDirectionNorm = aimDirection;
                else Vector3D.Normalize(ref aimDirection, out aimDirectionNorm);
                projectileVel += aimDirectionNorm * projectileMaxSpeed;
            }

            var deepSim = projectileAccelerates || hasGravity;
            var count = deepSim ? 320 : 60;

            double dt = Math.Max(MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS, timeToIntercept / count); // This can be a const somewhere
            double dtSqr = dt * dt;
            Vector3D targetAccStep = targetAcc * dt;
            Vector3D projectileAccStep = aimDirectionNorm * projectileAccMag * dt;

            Vector3D aimOffset = Vector3D.Zero;

            //BD Todo:  Clamp this for projectiles OR targets that don't accelerate
            if (!skipAccel && (projectileAccelerates || targetVel.LengthSquared() >= 0.01))
            {
                for (int i = 0; i < count; ++i)
                {

                    targetVel += targetAccStep;

                    if (targetVel.LengthSquared() > maxSpeedSqr)
                    {
                        Vector3D targetNormVel;
                        Vector3D.Normalize(ref targetVel, out targetNormVel);
                        targetVel = targetNormVel * targetMaxSpeed;
                    }

                    targetPos += targetVel * dt;
                    if (projectileAccelerates)
                    {

                        projectileVel += projectileAccStep;
                        if (projectileVel.LengthSquared() > projectileMaxSpeedSqr)
                        {
                            Vector3D pNormVel;
                            Vector3D.Normalize(ref projectileVel, out pNormVel);
                            projectileVel = pNormVel * projectileMaxSpeed;
                        }
                    }

                    projectilePos += projectileVel * dt;
                    Vector3D diff = (targetPos - projectilePos);
                    double diffLenSq = diff.LengthSquared();
                    aimOffset = diff;
                    if (diffLenSq < projectileMaxSpeedSqr * dtSqr || Vector3D.Dot(diff, aimDirectionNorm) < 0)
                        break;
                }
            }

            Vector3D perpendicularAimOffset = !skipAccel ? aimOffset - Vector3D.Dot(aimOffset, aimDirectionNorm) * aimDirectionNorm : Vector3D.Zero;

            Vector3D gravityOffset = Vector3D.Zero;
            //gravity nonsense for differing elevations
            if (hasGravity && ai.InPlanetGravity)
            {
                var targetAngle = Math.Acos(Vector3D.Dot(weapon.GravityPoint, deltaPos) / (weapon.GravityLength * deltaLength));
                double elevationDifference;
                if (targetAngle >= 1.5708) //Target is above weapon
                {
                    targetAngle -= 1.5708; //angle-90
                    elevationDifference = -Math.Sin(targetAngle) * deltaLength;
                }
                else //Target is below weapon
                {
                    targetAngle = 1.5708 - targetAngle; //90-angle
                    elevationDifference = -Math.Sin(targetAngle) * deltaLength;
                }
                var horizontalDistance = Math.Sqrt(deltaLength * deltaLength - elevationDifference * elevationDifference);
                
                //Minimized for my sanity
                var g = -(weapon.GravityLength * gravityMultiplier);
                var v = projectileMaxSpeed;
                var h = elevationDifference;
                var d = horizontalDistance;

                var angleCheck = (v * v * v * v) - 2 * (v * v) * -h * g - (g * g) * (d * d);

                if (angleCheck <= 0)
                {
                    valid = false;
                    return estimatedImpactPoint + perpendicularAimOffset + gravityOffset;

                }

                //lord help me
                var angleSqrt = Math.Sqrt(angleCheck);
                var angle1 = -Math.Atan((v * v + angleSqrt) / (g * d));//Higher angle
                var angle2 = -Math.Atan((v * v - angleSqrt) / (g * d));//Lower angle                //Try angle 2 first (the lower one)
                
                var verticalDistance = Math.Tan(angle2) * horizontalDistance; //without below-the-horizon modifier
                gravityOffset = new Vector3D((verticalDistance + Math.Abs(elevationDifference)) * -weapon.GravityUnitDir);
                if (angle1 > 1.57)
                {
                    return estimatedImpactPoint + perpendicularAimOffset + gravityOffset;
                }

                var targetAimPoint = estimatedImpactPoint + perpendicularAimOffset + gravityOffset;
                var targetDirection = targetAimPoint - shooterPos;

                bool isTracking;
                if (!weapon.RotorTurretTracking && weapon.TurretController && !WeaponLookAt(weapon, ref targetDirection, deltaLength * deltaLength, false, true, DebugCaller.TrajectoryEstimation, out isTracking)) //Angle 2 obscured, switch to angle 1
                {
                    verticalDistance = Math.Tan(angle1) * horizontalDistance;
                    gravityOffset = new Vector3D((verticalDistance + Math.Abs(elevationDifference)) * -weapon.GravityUnitDir);
                }
                else if (weapon.RotorTurretTracking && weapon.Comp.Ai.ControlComp != null && !RotorTurretLookAt(weapon.Comp.Ai.ControlComp.Platform.Control, ref targetDirection, deltaLength * deltaLength))
                {
                    verticalDistance = Math.Tan(angle1) * horizontalDistance;
                    gravityOffset = new Vector3D((verticalDistance + Math.Abs(elevationDifference)) * -weapon.GravityUnitDir);
                }
            }

            if (false)
            {
                //OldAdvanced
                DsDebugDraw.DrawLine(new LineD(origTargetPos, estimatedImpactPoint + perpendicularAimOffset + gravityOffset), Color.Yellow, 2f);

                //OldBasic
                Vector3D estimatedImpactPointbasicOld = origTargetPos + (ttiDiff < 0 ? 0 : closingDistance / projectileClosingSpeed) * (origTargetVel - shooterVel);
                DsDebugDraw.DrawLine(new LineD(origTargetPos, estimatedImpactPointbasicOld), Color.Green, 2f);

                //New algo
                var tempCoord = TrajectoryEstimation(weapon, origTargetPos, origTargetVel, targetAcc, shooterPos, out valid, false, trackAngular);
                DsDebugDraw.DrawLine(new LineD(origTargetPos, tempCoord), Color.Red, 2);
                return tempCoord;
            }
            MyAPIGateway.Utilities.ShowNotification($"Old Mode: {(basic ? " basic" : " advanced")} {(updateGravity ? " w/ grav" : " no grav")} {(skipAccel ? "" : " w/ proj accel")} ", 16);

            return estimatedImpactPoint + perpendicularAimOffset + gravityOffset;
        }

        public static bool QuarticSolver(ref double timeToIntercept, Vector3D relativePosition, Vector3D relativeVelocity, Vector3D acceleration, double projectileSpeed, double[] coefficients, double tolerance = 1e-3, int maxIterations = 10)
        {
            var oneOverSpeedSq = projectileSpeed > 0 ? 1.0 / (projectileSpeed * projectileSpeed) : 0;
            coefficients[4] = acceleration.LengthSquared() * 0.25 * oneOverSpeedSq;
            coefficients[3] = Vector3D.Dot(relativeVelocity, acceleration) * oneOverSpeedSq;
            coefficients[2] = (Vector3D.Dot(relativePosition, acceleration) + relativeVelocity.LengthSquared()) * oneOverSpeedSq - 1.0;
            coefficients[1] = 2.0 * Vector3D.Dot(relativePosition, relativeVelocity) * oneOverSpeedSq;
            coefficients[0] = relativePosition.LengthSquared() * oneOverSpeedSq;

            for (int ii = 0; ii < maxIterations; ++ii)
            {
                // Evaluate
                double value = 0;
                double xn = 1;
                for (int n = 0; n <= 4; ++n)
                {
                    value += coefficients[n] * xn;
                    xn *= timeToIntercept;
                }

                if (Math.Abs(value) < tolerance)
                    return true;

                // Derivative
                double deriv = 0;
                double xn1 = 1;
                for (int n = 1; n <= 4; ++n)
                {
                    deriv += n * coefficients[n] * xn1;
                    xn1 *= timeToIntercept;
                }

                if (MyUtils.IsZero(deriv, 1e-10f)) 
                    break;

                timeToIntercept -= value / deriv;
            }
            return false;
        }

        public static Vector3D TrajectoryEstimation(Weapon weapon, Vector3D targetPos, Vector3D targetVel, Vector3D targetAcc, Vector3D shooterPos, out bool valid, bool basicPrediction = false, bool trackAngular = false)
        {
            valid = false;
            Vector3D aimPoint;

            if (weapon == null || weapon.Comp == null || weapon.ActiveAmmoDef == null || weapon.ActiveAmmoDef.AmmoDef == null)
                return Vector3D.Zero;

            var comp = weapon.Comp;
            var ai = comp.Ai;
            var session = Session.I;
            var ammoDef = weapon.ActiveAmmoDef.AmmoDef;
            var shooterVel = ai != null ? (Vector3D)ai.TopEntityVel : Vector3D.Zero;
            var projectileMaxSpeed = ammoDef.Const.DesiredProjectileSpeed * weapon.VelocityMult;
            var updateGravity = ammoDef.Const.FeelsGravity && ai != null && ai.InPlanetGravity;
            var useSimple = basicPrediction || ammoDef.Const.AmmoSkipAccel || targetAcc.LengthSquared() < 2.5;

            #region Must Have Updates
            if (ai != null && comp.TopEntity != null && comp.TopEntity.PositionComp != null && ai.VelocityUpdateTick != session.Tick)
            {
                ai.TopEntityVolume.Center = comp.TopEntity.PositionComp.WorldVolume.Center;
                ai.TopEntityVel = comp.TopEntity.Physics?.LinearVelocity ?? Vector3D.Zero;
                ai.IsStatic = comp.TopEntity.Physics?.IsStatic ?? false;
                ai.VelocityUpdateTick = session.Tick;
            }

            if (updateGravity && session.Tick - weapon.GravityTick > 119)
            {
                weapon.GravityTick = session.Tick;
                float interference;
                weapon.GravityPoint = session.Physics.CalculateNaturalGravityAt(weapon.MyPivotPos, out interference);
                weapon.GravityUnitDir = weapon.GravityPoint;
                weapon.GravityLength = weapon.GravityUnitDir.Normalize();
            }
            else if (!updateGravity)
                weapon.GravityPoint = Vector3D.Zero;

            #endregion

            Vector3D deltaVel = targetVel - shooterVel;

            double deltaLength;
            double initialTti;
            Vector3D deltaPos = targetPos - shooterPos;
            if (Vector3D.IsZero(deltaPos))
                return targetPos;

            Vector3D deltaPosNorm;

            var targCube = weapon.Target?.TargetObject as MyCubeBlock;
            if (!basicPrediction && trackAngular && targCube?.CubeGrid?.Physics != null && targCube.CubeGrid.Physics.AngularVelocity.LengthSquared() > 0.0014)
            {
                if (!ComputeAngular(targCube.CubeGrid, ai, weapon, ammoDef, ref targetPos, ref shooterPos, ref targetAcc, ref deltaVel, projectileMaxSpeed, out deltaLength, out initialTti, out deltaPos, out deltaPosNorm))
                    return targetPos;
            }
            else
            {
                if (Vector3D.IsUnit(ref deltaPos))
                {
                    deltaPosNorm = deltaPos;
                    deltaLength = 1;
                }
                else
                {
                    deltaPosNorm = deltaPos;
                    deltaLength = deltaPosNorm.Normalize();
                }

                double closingSpeed;
                Vector3D.Dot(ref deltaVel, ref deltaPosNorm, out closingSpeed);
                initialTti = (projectileMaxSpeed * projectileMaxSpeed) - (deltaVel - (closingSpeed * deltaPosNorm)).LengthSquared();

                if (initialTti <= 0)
                    return targetPos;

                double closingDistance;
                Vector3D.Dot(ref deltaPos, ref deltaPosNorm, out closingDistance);
                initialTti = closingDistance / (Math.Sqrt(initialTti) - closingSpeed);

                if (initialTti <= 0)
                    return targetPos;
            }

            valid = true;
            if (useSimple)
            {
                aimPoint = targetPos + (initialTti) * (targetVel - shooterVel);
            }
            else
            {
                var advTti = initialTti;
                var projAccelTime = ammoDef.Const.DesiredProjectileSpeed / ammoDef.Const.AccelInMetersPerSec;
                var usedTti = QuarticSolver(ref advTti, deltaPos, deltaVel, targetAcc, ammoDef.Const.DesiredProjectileSpeed * weapon.VelocityMult, ai?.QuadraticCoefficientsStorage) ? advTti : initialTti;
                aimPoint = targetPos + (usedTti + (ammoDef.Const.AmmoSkipAccel ? 0 : (projAccelTime / usedTti))) * (targetVel - shooterVel);
            }

            // Check if the time-to-intercept is greater than half of the maximum travel time
            double maxTravelTime = ammoDef.Const.MaxTrajectory / projectileMaxSpeed;
            double closingSpeedPercentage = Vector3D.Dot(deltaVel, deltaPosNorm) / projectileMaxSpeed;
            double interceptThreshold = maxTravelTime * 0.5 * (1 - closingSpeedPercentage);

            if (initialTti > interceptThreshold)
            {
                valid = false;
                weapon.Target.ImpossibleToHit = true; // Mark target as impossible to hit
                return targetPos;
            }

            Vector3D gravityOffset = Vector3D.Zero;
            if (updateGravity && !MyUtils.IsZero(weapon.GravityPoint))
            {
                var gravPointDot = Vector3D.Dot(weapon.GravityUnitDir, deltaPosNorm);
                var targetAngle = Math.Acos(gravPointDot);
                double elevationDifference;
                if (targetAngle >= 1.5708) //Target is above weapon
                {
                    targetAngle -= 1.5708; //angle-90
                    elevationDifference = -Math.Sin(targetAngle) * deltaLength;
                }
                else //Target is below weapon
                {
                    targetAngle = 1.5708 - targetAngle; //90-angle
                    elevationDifference = -Math.Sin(targetAngle) * deltaLength;
                }
                var horizontalDistance = Math.Sqrt(deltaLength * deltaLength - elevationDifference * elevationDifference);

                var g = -(weapon.GravityLength * ammoDef.Const.GravityMultiplier);
                var v = projectileMaxSpeed;
                var h = elevationDifference;
                var d = horizontalDistance;

                var angleCheck = (v * v * v * v) - 2 * (v * v) * -h * g - (g * g) * (d * d);

                if (angleCheck <= 0)
                    valid = false;
                else
                {
                    var angleSqrt = Math.Sqrt(angleCheck);
                    var angle1 = -Math.Atan((v * v + angleSqrt) / (g * d));//Higher angle
                    var angle2 = -Math.Atan((v * v - angleSqrt) / (g * d));//Lower angle                //Try angle 2 first (the lower one)

                    var verticalDistance = Math.Tan(angle2) * horizontalDistance; //without below-the-horizon modifier
                    gravityOffset = new Vector3D((verticalDistance + Math.Abs(elevationDifference)) * -weapon.GravityUnitDir);

                    if (angle1 < 1.57)
                    {
                        var targetAimPoint = aimPoint + gravityOffset;
                        var targetDirection = targetAimPoint - shooterPos;

                        bool isTracking;
                        if (!weapon.RotorTurretTracking && weapon.TurretController != null && !WeaponLookAt(weapon, ref targetDirection, deltaLength * deltaLength, false, true, DebugCaller.TrajectoryEstimation, out isTracking)) //Angle 2 obscured, switch to angle 1
                        {
                            verticalDistance = Math.Tan(angle1) * horizontalDistance;
                            gravityOffset = new Vector3D((verticalDistance + Math.Abs(elevationDifference)) * -weapon.GravityUnitDir);
                        }
                        else if (weapon.RotorTurretTracking && weapon.Comp?.Ai?.ControlComp?.Platform?.Control != null && !RotorTurretLookAt(weapon.Comp.Ai.ControlComp.Platform.Control, ref targetDirection, deltaLength * deltaLength))
                        {
                            verticalDistance = Math.Tan(angle1) * horizontalDistance;
                            gravityOffset = new Vector3D((verticalDistance + Math.Abs(elevationDifference)) * -weapon.GravityUnitDir);
                        }
                    }
                }
            }

            //DsDebugDraw.DrawLine(new LineD(targetPos, aimPoint + gravityOffset), Color.Red, 1);
            //MyAPIGateway.Utilities.ShowNotification($"New Mode: {(useSimple ? "Simple" : "Advanced")} {(updateGravity ? " w/ grav" : " no grav")} {(ammoDef.Const.AmmoSkipAccel ? " no proj accel" : "w/ proj accel")}",16);

            return aimPoint + gravityOffset;
        }

        private static bool ComputeAngular(MyCubeGrid grid, Ai ai, Weapon weapon, WeaponDefinition.AmmoDef ammoDef, ref Vector3D targetPos, ref Vector3D shooterPos, ref Vector3D targetAcc, ref Vector3D deltaVel, double projectileMaxSpeed, out double deltaLength, out double initialTti, out Vector3D deltaPos, out Vector3D deltaPosNorm)
        {
            // deltaPos computed twice, once before and once after Angular estimation.  We just return usedTti as initialTti since it should always be superior.
            deltaPos = targetPos - shooterPos;

            if (Vector3D.IsUnit(ref deltaPos))
            {
                deltaPosNorm = deltaPos;
                deltaLength = 1;
            }
            else
            {
                deltaPosNorm = deltaPos;
                deltaLength = deltaPosNorm.Normalize();
            }

            double closingSpeed;
            Vector3D.Dot(ref deltaVel, ref deltaPosNorm, out closingSpeed);
            initialTti = (projectileMaxSpeed * projectileMaxSpeed) - (deltaVel - (closingSpeed * deltaPosNorm)).LengthSquared();

            if (initialTti <= 0)
                return false;

            double closingDistance;
            Vector3D.Dot(ref deltaPos, ref deltaPosNorm, out closingDistance);
            initialTti = closingDistance / (Math.Sqrt(initialTti) - closingSpeed);

            if (initialTti <= 0)
                return false;

            var advTti = initialTti;
            var usedTti = QuarticSolver(ref advTti, deltaPos, deltaVel, targetAcc, ammoDef.Const.DesiredProjectileSpeed * weapon.VelocityMult, ai.QuadraticCoefficientsStorage) ? advTti : initialTti;
            var targCom = grid.Physics.CenterOfMassWorld;
            var targAngVel = grid.Physics.AngularVelocity; //Radians per second
            var targAngVelLen = targAngVel.Normalize();
            var angleTravelled = targAngVelLen * usedTti;
            var dirFromCom = targetPos - targCom;
            var distFromCom = dirFromCom.Normalize();
            var matrix = MatrixD.CreateFromAxisAngle(targAngVel, angleTravelled);
            var matrixRot = Vector3D.Rotate(dirFromCom, matrix);
            initialTti = usedTti;

            if (initialTti <= 0)
                return false;

            // re-run since we changed the targetPos
            targetPos = targCom + matrixRot * distFromCom;

            deltaPos = targetPos - shooterPos;
            if (Vector3D.IsZero(deltaPos))
                deltaLength = 0;
            else if (Vector3D.IsUnit(ref deltaPos))
                deltaLength = 1;
            else
                deltaLength = deltaPosNorm.Normalize();

            return true;
        }

        public void ManualShootRayCallBack(IHitInfo hitInfo)
        {
            Casting = false;
            var masterWeapon = System.TrackTargets ? this : Comp.PrimaryWeapon;

            var grid = hitInfo.HitEntity as MyCubeGrid;
            if (grid != null)
            {
                if (grid.IsSameConstructAs(Comp.Cube.CubeGrid))
                {
                    masterWeapon.Target.Reset(Session.I.Tick, Target.States.RayCheckFailed, false);
                    if (masterWeapon != this) Target.Reset(Session.I.Tick, Target.States.RayCheckFailed, false);
                }
            }
        }

        public bool HitFriendlyShield(Vector3D weaponPos, Vector3D targetPos, Vector3D dir)
        {
            var testRay = new RayD(weaponPos, dir);
            Comp.Ai.TestShields.Clear();
            var checkDistanceSqr = Vector3.DistanceSquared(targetPos, weaponPos);

            for (int i = 0; i < Comp.Ai.NearByFriendlyShields.Count; i++)
            {
                var shield = Comp.Ai.NearByFriendlyShields[i];
                var dist = testRay.Intersects(shield.PositionComp.WorldVolume);
                if (dist != null && dist.Value * dist.Value <= checkDistanceSqr)
                    Comp.Ai.TestShields.Add(shield);
            }

            if (Comp.Ai.TestShields.Count == 0)
                return false;

            var result = Session.I.SApi.IntersectEntToShieldFast(Comp.Ai.TestShields, testRay, true, false, Comp.Ai.AiOwner, checkDistanceSqr);

            return result.Item1 && result.Item2 > 0;
        }

        public bool MuzzleHitSelf()
        {
            for (int i = 0; i < Muzzles.Length; i++)
            {
                var m = Muzzles[i];
                var grid = Comp.Ai.GridEntity;
                var dummy = Dummies[i];
                var newInfo = dummy.Info;
                m.Direction = newInfo.Direction;
                m.Position = newInfo.Position;
                m.LastUpdateTick = Session.I.Tick;

                var start = m.Position;
                var end = m.Position + (m.Direction * grid.PositionComp.LocalVolume.Radius);

                Vector3D? hit;
                if (GridIntersection.BresenhamGridIntersection(grid, ref start, ref end, out hit, Comp.Cube, Comp.Ai))
                    return true;
            }
            return false;
        }
        private bool RayCheckTest(double rangeToTargetSqr)
        {
            if (PosChangedTick != Session.I.SimulationCount)
                UpdatePivotPos();

            var scopeInfo = GetScope.Info;
            var trackingCheckPosition = ScopeDistToCheckPos > 0 ? scopeInfo.Position - (scopeInfo.Direction * ScopeDistToCheckPos) : scopeInfo.Position;
            var overrides = Comp.Data.Repo.Values.Set.Overrides;
            var eTarget = Target.TargetObject as MyEntity;
            var pTarget = Target.TargetObject as Projectile;

            var oneHalfKmSqr = 2250000;
            var lowFiVoxels = rangeToTargetSqr > oneHalfKmSqr && (Comp.Ai.PlanetSurfaceInRange || Comp.Ai.ClosestVoxelSqr <= oneHalfKmSqr);
            var filter = System.NoVoxelLosCheck ? CollisionLayers.NoVoxelCollisionLayer : lowFiVoxels ? CollisionLayers.DefaultCollisionLayer : CollisionLayers.VoxelLod1CollisionLayer;

            if (Session.I.DebugLos && Target.TargetState == Target.TargetStates.IsEntity && eTarget != null)
            {
                var trackPos = BarrelOrigin + (MyPivotFwd * MuzzleDistToBarrelCenter);
                var targetTestPos = eTarget.PositionComp.WorldAABB.Center;
                var topEntity = eTarget.GetTopMostParent();
                IHitInfo hitInfo;
                if (Session.I.Physics.CastRay(trackPos, targetTestPos, out hitInfo, filter) && hitInfo.HitEntity == topEntity)
                {
                    var hitPos = hitInfo.Position;
                    double closestDist;
                    MyUtils.GetClosestPointOnLine(ref trackingCheckPosition, ref targetTestPos, ref hitPos, out closestDist);
                    var tDir = Vector3D.Normalize(targetTestPos - trackingCheckPosition);
                    var closestPos = trackingCheckPosition + (tDir * closestDist);

                    var missAmount = Vector3D.Distance(hitPos, closestPos);
                    Session.I.Rays++;
                    Session.I.RayMissAmounts += missAmount;

                }
            }

            var tick = Session.I.Tick;
            var masterWeapon = System.TrackTargets || Comp.PrimaryWeapon == null ? this : Comp.PrimaryWeapon;

            if (System.Values.HardPoint.Other.MuzzleCheck)
            {
                LastMuzzleCheck = tick;
                if (MuzzleHitSelf())
                {
                    masterWeapon.Target.Reset(Session.I.Tick, Target.States.RayCheckSelfHit, !Comp.FakeMode);
                    if (masterWeapon != this) Target.Reset(Session.I.Tick, Target.States.RayCheckSelfHit, !Comp.FakeMode);
                    return false;
                }
                if (tick - Comp.LastRayCastTick <= 29) return true;
            }

            if (Target.TargetObject is IMyCharacter && !overrides.Biologicals || Target.TargetObject is MyCubeBlock && !overrides.Grids)
            {
                masterWeapon.Target.Reset(Session.I.Tick, Target.States.RayCheckProjectile);
                if (masterWeapon != this) Target.Reset(Session.I.Tick, Target.States.RayCheckProjectile);
                return false;
            }

            Comp.LastRayCastTick = tick;

            if (Target.TargetState == Target.TargetStates.IsFake)
            {
                Casting = true;
                Session.I.Physics.CastRayParallel(ref trackingCheckPosition, ref Target.TargetPos, filter, ManualShootRayCallBack);
                return true;
            }

            if (Comp.FakeMode) return true;

            if (Target.TargetState == Target.TargetStates.IsProjectile)
            {
                if (pTarget != null && !Comp.Ai.LiveProjectile.ContainsKey(pTarget))
                {
                    masterWeapon.Target.Reset(Session.I.Tick, Target.States.RayCheckProjectile);
                    if (masterWeapon != this) Target.Reset(Session.I.Tick, Target.States.RayCheckProjectile);
                    return false;
                }
            }

            if (Target.TargetState != Target.TargetStates.IsProjectile)
            {
                var character = Target.TargetObject as IMyCharacter;
                if ((eTarget == null || eTarget.MarkedForClose) || character != null && (!overrides.Biologicals || character.IsDead || character.Integrity <= 0 || Session.I.AdminMap.ContainsKey(character) || ((uint)character.Flags & 0x20000000) > 0))
                {
                    masterWeapon.Target.Reset(Session.I.Tick, Target.States.RayCheckOther);
                    if (masterWeapon != this) Target.Reset(Session.I.Tick, Target.States.RayCheckOther);
                    return false;
                }

                var cube = Target.TargetObject as MyCubeBlock;
                if (cube != null)
                {
                    var rootAi = Comp.Ai.Construct.RootAi;
                    var gridSize = cube.CubeGrid.GridSizeEnum;
                    var invalidType = !overrides.Grids || !overrides.SmallGrid && gridSize == MyCubeSize.Small || !overrides.LargeGrid && gridSize == MyCubeSize.Large;
                    var focusGrid = rootAi.Construct.LastFocusEntity as MyCubeGrid;
                    var invalidCube = cube.MarkedForClose || !cube.IsWorking && (focusGrid == null || !focusGrid.IsSameConstructAs(cube.CubeGrid));
                    var focusFailed = overrides.FocusTargets && !rootAi.Construct.HadFocus;
                    var checkSubsystem = overrides.FocusSubSystem && overrides.SubSystem != WeaponDefinition.TargetingDef.BlockTypes.Any;
                    if (invalidType || invalidCube || focusFailed || ((uint)cube.CubeGrid.Flags & 0x20000000) > 0 || checkSubsystem && !ValidSubSystemTarget(cube, overrides.SubSystem))
                    {
                        masterWeapon.Target.Reset(Session.I.Tick, Target.States.RayCheckDeadBlock);
                        if (masterWeapon != this) Target.Reset(Session.I.Tick, Target.States.RayCheckDeadBlock);
                        FastTargetResetTick = Session.I.Tick;
                        return false;
                    }

                }
                var topMostEnt = eTarget.GetTopMostParent();
                if (Target.TopEntityId != topMostEnt.EntityId || !Comp.Ai.Targets.ContainsKey(topMostEnt) && (!System.ScanNonThreats || !Comp.Ai.ObstructionLookup.ContainsKey(topMostEnt)))
                {
                    masterWeapon.Target.Reset(Session.I.Tick, Target.States.RayCheckFailed);
                    if (masterWeapon != this) Target.Reset(Session.I.Tick, Target.States.RayCheckFailed);
                    return false;
                }
            }

            var targetPos = pTarget?.Position ?? eTarget?.PositionComp.WorldAABB.Center ?? Vector3D.Zero;
            var distToTargetSqr = Vector3D.DistanceSquared(targetPos, trackingCheckPosition);
            if (distToTargetSqr > MaxTargetDistanceSqr && distToTargetSqr < MinTargetDistanceSqr)
            {
                masterWeapon.Target.Reset(Session.I.Tick, Target.States.RayCheckDistExceeded);
                if (masterWeapon != this) Target.Reset(Session.I.Tick, Target.States.RayCheckDistExceeded);
                return false;
            }
            WaterData water = null;
            if (Session.I.WaterApiLoaded && !ActiveAmmoDef.AmmoDef.IgnoreWater && Comp.Ai.InPlanetGravity && Comp.Ai.MyPlanet != null && Session.I.WaterMap.TryGetValue(Comp.Ai.MyPlanet.EntityId, out water))
            {
                var waterSphere = new BoundingSphereD(Comp.Ai.MyPlanet.PositionComp.WorldAABB.Center, water.MinRadius);
                if (waterSphere.Contains(targetPos) != ContainmentType.Disjoint)
                {
                    masterWeapon.Target.Reset(Session.I.Tick, Target.States.RayCheckFailed);
                    if (masterWeapon != this) Target.Reset(Session.I.Tick, Target.States.RayCheckFailed);
                    return false;
                }
            }
            Casting = true;
            Session.I.Physics.CastRayParallel(ref trackingCheckPosition, ref targetPos, filter, RayCallBack.NormalShootRayCallBack);
            return true;
        }

        internal bool ValidSubSystemTarget(MyCubeBlock cube, WeaponDefinition.TargetingDef.BlockTypes subsystem)
        {
            switch (subsystem)
            {
                case WeaponDefinition.TargetingDef.BlockTypes.Jumping:
                    return cube is MyJumpDrive || cube is IMyDecoy;
                case WeaponDefinition.TargetingDef.BlockTypes.Offense:
                    return cube is IMyGunBaseUser || cube is MyConveyorSorter && Session.I.PartPlatforms.ContainsKey(cube.BlockDefinition.Id) || cube is IMyWarhead || cube is IMyDecoy;
                case WeaponDefinition.TargetingDef.BlockTypes.Power:
                    return cube is IMyPowerProducer || cube is IMyDecoy;
                case WeaponDefinition.TargetingDef.BlockTypes.Production:
                    return cube is IMyProductionBlock || cube is IMyUpgradeModule && Session.I.VanillaUpgradeModuleHashes.Contains(cube.BlockDefinition.Id.SubtypeName) || cube is IMyDecoy;
                case WeaponDefinition.TargetingDef.BlockTypes.Steering:
                    var cockpit = cube as MyCockpit;
                    return cube is MyGyro || cockpit != null && cockpit.EnableShipControl || cube is IMyDecoy;
                case WeaponDefinition.TargetingDef.BlockTypes.Thrust:
                    return cube is MyThrust || cube is IMyDecoy;
                case WeaponDefinition.TargetingDef.BlockTypes.Utility:
                    return !(cube is IMyProductionBlock) && cube is IMyUpgradeModule || cube is IMyRadioAntenna || cube is IMyLaserAntenna || cube is MyRemoteControl || cube is IMyShipToolBase || cube is IMyMedicalRoom || cube is IMyCameraBlock || cube is IMyDecoy; 
                default:
                    return false;
            }
        }

        internal void InitTracking()
        {
            var minAz = System.MinAzimuth;
            var maxAz = System.MaxAzimuth;
            var minEl = System.MinElevation;
            var maxEl = System.MaxElevation;
            var toleranceRads = System.WConst.AimingToleranceRads;

            MinElevationRadians = MinElToleranceRadians = MathHelperD.ToRadians(MathFuncs.NormalizeAngle(minEl));
            MaxElevationRadians = MaxElToleranceRadians = MathHelperD.ToRadians(MathFuncs.NormalizeAngle(maxEl));

            MinAzimuthRadians = MinAzToleranceRadians = MathHelperD.ToRadians(MathFuncs.NormalizeAngle(minAz));
            MaxAzimuthRadians = MaxAzToleranceRadians = MathHelperD.ToRadians(MathFuncs.NormalizeAngle(maxAz));

            if (System.TurretMovement == WeaponSystem.TurretType.AzimuthOnly || System.Values.HardPoint.AddToleranceToTracking)
            {
                MinElToleranceRadians -= toleranceRads;
                MaxElToleranceRadians += toleranceRads;
            }
            else if (System.TurretMovement == WeaponSystem.TurretType.ElevationOnly || System.Values.HardPoint.AddToleranceToTracking)
            {
                MinAzToleranceRadians -= toleranceRads;
                MaxAzToleranceRadians += toleranceRads;
            }

            if (MinElToleranceRadians > MaxElToleranceRadians)
                MinElToleranceRadians -= 6.283185f;

            if (MinAzToleranceRadians > MaxAzToleranceRadians)
                MinAzToleranceRadians -= 6.283185f;

            var dummyInfo = Dummies[MiddleMuzzleIndex].Info;

            MuzzleDistToBarrelCenter = Vector3D.Distance(dummyInfo.LocalPosition, dummyInfo.Entity.PositionComp.LocalAABB.Center);
        }
    }
}