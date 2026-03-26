using System;
using CoreSystems.Projectiles;
using CoreSystems.Support;
using Sandbox.Definitions;
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
            if (prediction != Prediction.Off && !weapon.ActiveAmmoDef.AmmoDef.Const.IsBeamWeapon && weapon.ActiveAmmoDef.AmmoDef.Const.DesiredProjectileSpeed > 0)
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
            if (checkSelfHit && target != null && !weapon.ActiveAmmoDef.AmmoDef.Const.SkipRayChecks && !weapon.ActiveAmmoDef.AmmoDef.IgnoreGrids)
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
                if (!weapon.ActiveAmmoDef.AmmoDef.Const.IsBeamWeapon && weapon.ActiveAmmoDef.AmmoDef.Const.DesiredProjectileSpeed > 0)
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

            if (!weapon.ActiveAmmoDef.AmmoDef.Const.IsBeamWeapon && weapon.ActiveAmmoDef.AmmoDef.Const.DesiredProjectileSpeed > 0)
                targetPos = TrajectoryEstimation(weapon, obb.Center, vel, accel, weapon.MyPivotPos, out validEstimate, false, weapon.Comp.Data.Repo.Values.Set.Overrides.AngularTracking);
            else
                targetPos = obb.Center;

            obb.Center = targetPos;
            weapon.TargetBox = obb;
            couldHit = validEstimate;

            bool canTrack = false;
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
            weapon.Target.ValidEstimate = willHit;
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
            
            if (prediction != Prediction.Off && !weapon.ActiveAmmoDef.AmmoDef.Const.IsBeamWeapon && weapon.ActiveAmmoDef.AmmoDef.Const.DesiredProjectileSpeed > 0)
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
            if (weapon.System.Prediction != Prediction.Off && (!weapon.ActiveAmmoDef.AmmoDef.Const.IsBeamWeapon && weapon.ActiveAmmoDef.AmmoDef.Const.DesiredProjectileSpeed > 0))
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
                        targetLinVel = (Vector3)pTarget.Velocity;
                        targetAccel = (Vector3)(pTarget.Velocity - pTarget.PrevVelocity1);
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
            if (w.System.Prediction != Prediction.Off && !w.ActiveAmmoDef.AmmoDef.Const.IsBeamWeapon && w.ActiveAmmoDef.AmmoDef.Const.DesiredProjectileSpeed > 0)
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
                        targetLinVel = (Vector3)pTarget.Velocity;
                        targetAccel = (Vector3)(pTarget.Velocity - pTarget.PrevVelocity1);
                    }
                    else if (topMostEnt?.Physics != null)
                    {
                        targetLinVel = topMostEnt.Physics.LinearVelocity;
                        targetAccel = topMostEnt.Physics.LinearAcceleration;
                    }
                }
                if (Vector3D.IsZero(targetLinVel, 5E-03)) targetLinVel = Vector3.Zero;
                if (Vector3D.IsZero(targetAccel, 5E-03)) targetAccel = Vector3.Zero;
                if (w.PosChangedTick != Session.I.SimulationCount)
                    w.UpdatePivotPos();
                targetPos = TrajectoryEstimation(w, targetCenter, targetLinVel, targetAccel, w.MyPivotPos,  out validEstimate, false, baseData.Set.Overrides.AngularTracking);
                w.Target.ValidEstimate = validEstimate;
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

            var painterInRange = w.Comp.PainterMode && rangeToTargetSqr <= (w.System.PainterUseMaxTargeting ? w.MaxTargetDistanceSqr : w.ActiveAmmoDef.AmmoDef.Const.MaxTrajectorySqr) && rangeToTargetSqr >= w.MinTargetDistanceSqr;
            var readyToTrack = validEstimate && !w.Comp.ResettingSubparts && (w.Comp.ManualMode || painterInRange || rangeToTargetSqr <= w.MaxTargetDistanceSqr && rangeToTargetSqr >= w.MinTargetDistanceSqr);
            var locked = true;
            var isTracking = false;

            if (readyToTrack && w.PosChangedTick != Session.I.SimulationCount)
                w.UpdatePivotPos();
            var targetDir = targetPos - w.MyPivotPos;

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


            if (baseData.State.Control == ProtoWeaponState.ControlMode.Camera || w.Comp.ManualMode || painterInRange || session.IsServer && baseData.Set.Overrides.Repel && ai.DetectionInfo.DroneInRange && target.IsDrone && (session.AwakeCount == w.Acquire.SlotId || ai.Construct.RootAi.Construct.LastDroneTick == session.Tick) && Ai.SwitchToDrone(w))
                return true;

            var rayCheckTest = isTracking && (isAligned || locked) && baseData.State.Control != ProtoWeaponState.ControlMode.Camera && (w.ActiveAmmoDef.AmmoDef.Trajectory.Guidance != TrajectoryDef.GuidanceType.Smart && w.ActiveAmmoDef.AmmoDef.Trajectory.Guidance != TrajectoryDef.GuidanceType.DroneAdvanced) && !w.System.DisableLosCheck && (session.Tick - w.Comp.LastRayCastTick > 29 || w.System.Values.HardPoint.Other.MuzzleCheck && session.Tick - w.LastMuzzleCheck > 29);
            
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

        /// <summary>
        ///     Spatial information about a weapon and its target.
        /// </summary>
        private struct TrajectoryPredictionShootingFrame
        {
            public Vector3D TargetPos, TargetVel;
            public Vector3D ShooterPos, ShooterVel;

            public Vector3D Dr, Dv;
            public double Distance;
            public Vector3D Los;

            public static TrajectoryPredictionShootingFrame Calculate(
                ref Vector3D targetPos, ref Vector3D targetVel,
                ref Vector3D shooterPos, ref Vector3D shooterVel)
            {
                var dr = targetPos - shooterPos;
                var dv = targetVel - shooterVel;
                var distance = dr.Length();
                var los = dr / distance;

                return new TrajectoryPredictionShootingFrame
                {
                    TargetPos = targetPos, TargetVel = targetVel,
                    ShooterPos = shooterPos, ShooterVel = shooterVel,
                    Dr = dr, Dv = dv,
                    Distance = distance,
                    Los = los
                };
            }
            
            /// <summary>
            ///     Calculates a crude time-to-intercept using the first-order information.
            /// </summary>
            /// <param name="muzzleSpeed"></param>
            /// <param name="tti"></param>
            /// <returns></returns>
            public bool CalculateCrudeTti(double muzzleSpeed, out double tti)
            {
                double closingSpeed;
                Vector3D.Dot(ref Dv, ref Los, out closingSpeed);
                
                tti = muzzleSpeed * muzzleSpeed - (Dv - closingSpeed * Los).LengthSquared();

                if (tti <= 0.0)
                {
                    tti = double.PositiveInfinity;
                    return false;
                }

                double closingDistance;
                Vector3D.Dot(ref Dr, ref Los, out closingDistance);
                tti =  closingDistance / (Math.Sqrt(tti) - closingSpeed);
                
                if (tti <= 0.0)
                {
                    tti = double.PositiveInfinity;
                    return false;                
                }

                return true;
            }
        }

        /// <summary>
        ///     Extracted target information for a weapon.
        ///     Used to choose a target-specific prediction algorithm (curently only for grids). 
        /// </summary>
        private struct TrajectoryPredictionTargetDescription
        {
            /// <summary>
            ///     The top type of the target.
            /// </summary>
            public enum TargetType
            {
                /// <summary>
                ///     The type could not be determined.
                /// </summary>
                Unknown,
                /// <summary>
                ///     The target is a projectile.
                /// </summary>
                Projectile,
                /// <summary>
                ///     The target is either a painted grid or a targeted block on a grid.
                /// </summary>
                Grid
            }

            public enum PredictionAlgorithm
            {
                /// <summary>
                ///     The previous no-assumption targeting algorithm should be used.
                /// </summary>
                Crude,
                /// <summary>
                ///     The advanced anti-projectile-twisting algorithm should be used.
                /// </summary>
                AdvancedProjectile,
                /// <summary>
                ///     The advanced anti-dodging algorithm should be used.
                /// </summary>
                AdvancedGrid
            }
            
            public readonly TargetType Type;
            public readonly MyCubeGrid GridTarget;
            public readonly Projectile ProjectileTarget;

            public TrajectoryPredictionTargetDescription(Weapon weapon)
            {
                Type = TargetType.Unknown;
                GridTarget = null;
                ProjectileTarget = null;
                
                switch (weapon.Comp.Data.Repo.Values.Set.Overrides.Control)
                {
                    case ProtoWeaponOverrides.ControlModes.Auto:
                    {
                        ProjectileTarget = weapon.Target.TargetObject as Projectile;

                        if (ProjectileTarget != null)
                        {
                            Type = TargetType.Projectile;
                        }
                        else
                        {
                            var tEntity = weapon.Target.TargetObject as MyEntity;

                            if (tEntity is MyCubeGrid)
                            {
                                Type = TargetType.Grid;
                                GridTarget = (MyCubeGrid)tEntity;
                            }
                            else if (tEntity is MyCubeBlock)
                            {
                                GridTarget = ((MyCubeBlock)tEntity).CubeGrid;

                                if (GridTarget != null)
                                {
                                    Type = TargetType.Grid;
                                }
                            }
                        }
                        
                        break;
                    }
                    case ProtoWeaponOverrides.ControlModes.Painter:
                    {
                        Ai.FakeTargets fakeTargets;
                        if (Session.I.PlayerDummyTargets.TryGetValue(weapon.Comp.Data.Repo.Values.State.PlayerId, out fakeTargets))
                        {
                            if (!Session.I.Settings.Enforcement.ProhibitHUDPainter && fakeTargets.PaintedTarget.LocalPosition != Vector3D.Zero)
                            {
                                var entityId = fakeTargets.PaintedTarget.EntityId;
                        
                                if (entityId > 0 || entityId < -2)
                                {
                                    GridTarget = MyEntities.GetEntityById(entityId) as MyCubeGrid;

                                    if (GridTarget != null)
                                    {
                                        Type = TargetType.Grid;
                                    }
                                }
                            }
                        }

                        break;
                    }
                    case ProtoWeaponOverrides.ControlModes.Manual:
                    default:
                        // Ignored
                        break;
                }
            }

            public PredictionAlgorithm DecidePredictionAlgorithm(double targAccelSqr, double targVelSqr, bool allowAdvancedProjectileAlgorithm, bool allowAdvancedGridAlgorithm)
            {
                switch (Type)
                {
                    case TargetType.Projectile:
                    {
                        var attemptAdvancedProjectilePrediction =
                            allowAdvancedProjectileAlgorithm &&
                            ProjectileTarget != null &&
                            ProjectileTarget.PrevVelocity1.LengthSquared() > 100.0 &&
                            ProjectileTarget.PrevVelocity0.LengthSquared() > 100.0;

                        return attemptAdvancedProjectilePrediction
                            ? PredictionAlgorithm.AdvancedProjectile
                            : PredictionAlgorithm.Crude;
                    }
                    case TargetType.Grid:
                    {
                        var attemptAdvancedGridPrediction =
                            allowAdvancedGridAlgorithm &&
                            GridTarget?.Physics != null &&
                            !GridTarget.Closed &&
                            (
                                GridTarget.Physics.AngularVelocity.LengthSquared() > 0.0003 ||
                                (targAccelSqr > 100 && targVelSqr > 100)
                            );

                        return attemptAdvancedGridPrediction
                            ? PredictionAlgorithm.AdvancedGrid
                            : PredictionAlgorithm.Crude;
                    }
                    case TargetType.Unknown:
                    default:
                        return PredictionAlgorithm.Crude;
                }
            }
        }
        
        internal static Vector3D TrajectoryEstimation(
            Weapon weapon,
            Vector3D targetPos0,
            Vector3D targetVel0, 
            Vector3D targetAccel0,
            Vector3D shooterPos0,
            out bool valid,
            bool basicPrediction = false, 
            bool trackAngular = false)
        {
            valid = false;
            var weaponComp = weapon.Comp;
            var ai = weaponComp.Ai;
            var session = Session.I;
            var debug = weapon.System.DebugMode;
            #region Must Have Updates
            if (ai.VelocityUpdateTick != session.Tick)
            {
                ai.TopEntityVolume.Center = weaponComp.TopEntity.PositionComp.WorldVolume.Center;
                ai.TopEntityVel = weaponComp.TopEntity.Physics?.LinearVelocity ?? Vector3.Zero;
                ai.IsStatic = weaponComp.TopEntity.Physics?.IsStatic ?? false;
                ai.VelocityUpdateTick = session.Tick;
            }
            
            var shooterVel0 = (Vector3D)weapon.Comp.Ai.TopEntityVel;
            var ammoDef = weapon.ActiveAmmoDef.AmmoDef;
            var projectileMaxSpeed = ammoDef.Const.DesiredProjectileSpeed;
            var updateGravity = ammoDef.Const.FeelsGravity && ai.InPlanetGravity;
            var targAccelSqr = targetAccel0.LengthSquared();
            var targVelSqr = targetVel0.LengthSquared();
            var useSimple = basicPrediction || ammoDef.Const.AmmoSkipAccel || targAccelSqr < 2.5; //equal to approx 1.58 m/s

            if (updateGravity && session.Tick - weapon.GravityTick > 119)
            {
                weapon.GravityTick = session.Tick;
                float interference;
                weapon.GravityPoint = session.Physics.CalculateNaturalGravityAt(weapon.MyPivotPos, out interference);
                weapon.GravityUnitDir = weapon.GravityPoint;
                weapon.GravityLength = weapon.GravityUnitDir.Normalize();
            }
            else if (!updateGravity)
            {
                weapon.GravityPoint = Vector3D.Zero;
            }

            #endregion

            if (targetPos0.Equals(shooterPos0, 1e-3))
            {
                // Something invalid going on:
                return targetPos0;
            }
            
            var frame0 = TrajectoryPredictionShootingFrame.Calculate(
                ref targetPos0, ref targetVel0,
                ref shooterPos0, ref shooterVel0
            );
            
            double crudeTti; // Used for bounds estimation and basic prediction
            if (!frame0.CalculateCrudeTti(projectileMaxSpeed, out crudeTti))
            {
                // Probably best not to bother:
                return targetPos0;
            }

            // Extracts the target object from the weapon's state:
            var targetDescription = new TrajectoryPredictionTargetDescription(weapon);
            
            // Determines which prediction algorithm to run based on available information and weapon config:
            var algorithmToTry = targetDescription.DecidePredictionAlgorithm(
                targAccelSqr, targVelSqr,
                allowAdvancedGridAlgorithm: trackAngular, // Also, should basicPrediction be introduced here?
                allowAdvancedProjectileAlgorithm: weapon.System.UseLimitlessPDSolver
            );

            // The approximate frame at intercept time.
            // Used for the later gravity calculation.
            TrajectoryPredictionShootingFrame interceptFrame;
            
            // Kinetic state of the target point at intercept time, for the advanced algorithm.
            // For grids, the velocity is not exactly taken inside the target's frame (velocity due to rotation of target is discarded).
            KineticState targetPointStateTemp;
            double tti;
            
            if (
                (algorithmToTry == TrajectoryPredictionTargetDescription.PredictionAlgorithm.AdvancedGrid && 
                    CalculateAdvancedGridAimPrediction(
                        targetDescription.GridTarget, ref targetPos0, ref targetVel0,
                        ref shooterPos0, ref shooterVel0, crudeTti, projectileMaxSpeed, debug,
                        out targetPointStateTemp, out tti)
                    ) || 
                (algorithmToTry == TrajectoryPredictionTargetDescription.PredictionAlgorithm.AdvancedProjectile && 
                    CalculateAdvancedPdAimPrediction(
                        targetDescription.ProjectileTarget, ref targetPos0, ref targetVel0,
                        ref shooterPos0, ref shooterVel0, crudeTti, projectileMaxSpeed, debug,
                        out targetPointStateTemp, out tti)
                    )
                )
            {
                // The same approximation for accelerating projectiles that was used previously:
                if (!ammoDef.Const.AmmoSkipAccel && tti > 0.0)
                {
                    var projectileAccelTime = projectileMaxSpeed / ammoDef.Const.AccelInMetersPerSec;
                    var timePenalty = projectileAccelTime / tti;
                    
                    // ReSharper disable once RedundantAssignment
                    tti += timePenalty; 
                    
                    var dvIntercept = targetPointStateTemp.LinearVelocity - shooterVel0;
                    targetPointStateTemp.Translation += dvIntercept * timePenalty;
                }

                interceptFrame = TrajectoryPredictionShootingFrame.Calculate(
                    ref targetPointStateTemp.Translation, ref targetPointStateTemp.LinearVelocity,
                    ref shooterPos0, ref shooterVel0
                );            
            }
            else
            {
                // Fallback to old logic:
                tti = crudeTti;
                
                if (useSimple)
                {
                    var aimPoint = frame0.TargetPos + tti * frame0.Dv;
                    
                    interceptFrame = TrajectoryPredictionShootingFrame.Calculate(
                        ref aimPoint, ref frame0.TargetVel,
                        ref frame0.ShooterPos, ref frame0.ShooterVel
                    );
                }
                else
                {
                    var advTti = tti;
                 
                    var finalTti = QuarticSolver(
                        ref advTti, 
                        frame0.Dr, frame0.Dv,
                        targetAccel0,
                        projectileMaxSpeed, ai.QuadraticCoefficientsStorage
                    ) ? advTti : tti;
                    
                    var projectileAccelTime = ammoDef.Const.AmmoSkipAccel 
                        ? 0.0 
                        : projectileMaxSpeed / ammoDef.Const.AccelInMetersPerSec;
                    
                    var timePenalty = projectileAccelTime > 0
                        ? projectileAccelTime / finalTti 
                        : 0;

                    var aimPoint = frame0.TargetPos + (finalTti + timePenalty) * frame0.Dv;
                    
                    interceptFrame = TrajectoryPredictionShootingFrame.Calculate(
                        ref aimPoint, ref frame0.TargetVel,
                        ref frame0.ShooterPos, ref frame0.ShooterVel
                    );
                }   
            }

            valid = true;
            
            if (!updateGravity || MyUtils.IsZero(weapon.GravityPoint))
            {
                // No gravity to take into account:
                return interceptFrame.TargetPos;
            }
            
            var gravityOffset = Vector3D.Zero;
            var gravityDirDotLos = Vector3D.Dot(weapon.GravityUnitDir, interceptFrame.Los);
            var targetAngle = Math.Acos(MathHelperD.Clamp(gravityDirDotLos, -1.0, +1.0)); // Always clamp before calling arccos, the value can drift, BD! (speaking from experience, unfortunately)

            if (targetAngle >= MathHelperD.PiOver2) 
            {
                //Target is above weapon:
                targetAngle -= MathHelperD.PiOver2; // angle - 90deg
            }
            else
            {
                //Target is below weapon:
                targetAngle = MathHelperD.PiOver2 - targetAngle; // 90deg - angle
            }

            var elevationDifference = -Math.Sin(targetAngle) * interceptFrame.Distance;
            var horizontalDistance = Math.Sqrt(interceptFrame.Distance * interceptFrame.Distance - elevationDifference * elevationDifference);

            // ReSharper disable InlineTemporaryVariable
            var g = -(weapon.GravityLength * ammoDef.Const.GravityMultiplier);
            var v = projectileMaxSpeed;
            var h = elevationDifference;
            var d = horizontalDistance;
            // ReSharper restore InlineTemporaryVariable

            var angleCheck = v * v * v * v - 2.0 * (v * v) * -h * g - g * g * (d * d);

            if (angleCheck <= 0)
            {
                valid = false;
            }
            else
            {
                var angleSqrt = Math.Sqrt(angleCheck);
                var angle1 = -Math.Atan((v * v + angleSqrt) / (g * d)); //Higher angle
                var angle2 = -Math.Atan((v * v - angleSqrt) / (g * d)); //Lower angle. Try angle 2 first (the lower one)

                var verticalDistance = Math.Tan(angle2) * horizontalDistance; //without below-the-horizon modifier
                gravityOffset = new Vector3D((verticalDistance + Math.Abs(elevationDifference)) * -weapon.GravityUnitDir);

                if (angle1 < MathHelperD.PiOver2)
                {
                    var targetAimPoint = interceptFrame.TargetPos + gravityOffset;
                    var targetDirection = targetAimPoint - shooterPos0;

                    bool isTracking;
                    if (!weapon.RotorTurretTracking && weapon.TurretController && !WeaponLookAt(weapon, ref targetDirection, interceptFrame.Distance * interceptFrame.Distance, false, true, DebugCaller.TrajectoryEstimation, out isTracking)) //Angle 2 obscured, switch to angle 1
                    {
                        verticalDistance = Math.Tan(angle1) * horizontalDistance;
                        gravityOffset = new Vector3D((verticalDistance + Math.Abs(elevationDifference)) * -weapon.GravityUnitDir);
                    }
                    else if (weapon.RotorTurretTracking && weapon.Comp.Ai.ControlComp != null && !RotorTurretLookAt(weapon.Comp.Ai.ControlComp.Platform.Control, ref targetDirection, interceptFrame.Distance * interceptFrame.Distance))
                    {
                        verticalDistance = Math.Tan(angle1) * horizontalDistance;
                        gravityOffset = new Vector3D((verticalDistance + Math.Abs(elevationDifference)) * -weapon.GravityUnitDir);
                    }
                }
            }
            
            return interceptFrame.TargetPos + gravityOffset;
        }

        #region Advanced Trajectory Estimation Algorithms
        
        public struct KineticState
        {
            public Vector3D Translation;
            public Vector3D LinearVelocity;

            public KineticState(Vector3D translation, Vector3D linearVelocity)
            {
                Translation = translation;
                LinearVelocity = linearVelocity;
            }
        }

        private static bool CalculateAdvancedGridAimPrediction(MyCubeGrid targetGrid, ref Vector3D targetPos, ref Vector3D targetVel, ref Vector3D weaponPos, ref Vector3D weaponVel, double crudeTti, double muzzleSpeed, bool debug, out KineticState targetPointState, out double t)
        {
            const double dt = 1.0 / 60.0;
            
            double maxSpeed;
            bool applyMaxSpeedAfterStep;
            if (Session.I.TrajectoryPredictionShipVelocityConstraint != null)
            {
                var tuple = Session.I.TrajectoryPredictionShipVelocityConstraint.Invoke(targetGrid);
                maxSpeed = tuple.Item1;
                applyMaxSpeedAfterStep = tuple.Item2;
            }
            else
            {
                maxSpeed = targetGrid.GridSizeEnum == MyCubeSize.Large 
                    ? MyDefinitionManager.Static.EnvironmentDefinition.LargeShipMaxSpeed 
                    : MyDefinitionManager.Static.EnvironmentDefinition.SmallShipMaxSpeed;

                applyMaxSpeedAfterStep = true;
            }

            var maxSpeedSqr = maxSpeed * maxSpeed;
            
            var targetDriveAccelWorld = Session.I.TrajectoryPredictionShipAccelEstimator == null
                ? (Vector3D)targetGrid.Physics.LinearAcceleration
                : Session.I.TrajectoryPredictionShipAccelEstimator.Invoke(targetGrid);
            
            var targetFixedPoint = targetGrid.Physics.CenterOfMassWorld;
            var targetOffsetWorld = targetPos - targetFixedPoint;
            var previousTargetOffsetWorld = targetOffsetWorld;
            
            var w = (Vector3D)targetGrid.Physics.AngularVelocity;
            var wNorm = w.Length();

            var rotIncr = wNorm < 1e-8
                ? QuaternionD.Identity 
                : QuaternionD.CreateFromAxisAngle(w / wNorm, wNorm * dt);
            
            var currentX = new KineticState(targetFixedPoint, targetVel);
            var previousX = new KineticState();
            var externalForceFunction = Session.I.TrajectoryPredictionExternalForce;

            // Reasonable bounds for the solution:
            var start = Math.Max((int)(crudeTti * 60 * 0.8), 1);
            var budget = Math.Max((int)(crudeTti * 60 * 1.2), 5);

            for (var step = 0; step <= budget; step++) // Budget + 1 steps
            {
                if (step >= start)
                {
                    var a = previousX.Translation + previousTargetOffsetWorld;
                    var t0 = step * dt - dt;
                    var d = currentX.Translation + targetOffsetWorld - a;
                    var u = weaponVel - d / dt;
                    var w1 = weaponPos - a + d * t0 / dt;
                    // ReSharper disable InconsistentNaming
                    // Honestly it would be best if we made a linear solver if A ~= 0. Maybe later
                    var A = Vector3D.Dot(u, u) - muzzleSpeed * muzzleSpeed;
                    var B = 2 * Vector3D.Dot(u, w1);
                    var C = Vector3D.Dot(w1, w1);
                    // ReSharper restore InconsistentNaming
                    var delta = B * B - 4.0 * A * C;

                    bool hasRoot;
                    if (delta < 0.0)
                    {
                        hasRoot = false;
                    }
                    else
                    {
                        var t1Frame = t0 + dt;
                        var f0 = A * t0 * t0 + B * t0 + C;
                        var f1 = A * t1Frame * t1Frame + B * t1Frame + C;

                        // If the sign changed over this interval, the interval contains the root:
                        hasRoot = (f0 <= 0.0 && f1 >= 0.0) || (f0 >= 0.0 && f1 <= 0.0);

                        if (!hasRoot)
                        {
                            // If the sign didn't change, it's still possible for the function to have both roots inside this interval.
                            // To check this, we verify the signs of the function at the two ends against the sign of the function at the vertex:
                            var tVertex = -B / (2.0 * A);
                            if (tVertex > t0 && tVertex < t1Frame)
                            {
                                var fVertex = -delta / (4.0 * A);
                                hasRoot = (f0 <= 0.0 && fVertex >= 0.0) || (f0 >= 0.0 && fVertex <= 0.0);
                            }
                        }
                    }

                    if (hasRoot)
                    {
                        delta = Math.Sqrt(delta);
                        var t1 = (-B - delta) / (2.0 * A);
                        var t2 = (-B + delta) / (2.0 * A);

                        t = double.PositiveInfinity;

                        if (t1 > t0 && t1 <= t0 + dt)
                        {
                            t = Math.Min(t, t1);
                        }

                        if (t2 > t0 && t2 <= t0 + dt)
                        {
                            t = Math.Min(t, t2);
                        }

                        if (!double.IsPositiveInfinity(t))
                        {
                            // Target GRID position:
                            //var positionEstimate = a + d * (t - t0) / dt;

                            // The actual launch direction:
                            var directionEstimate = -(u * t + w1) / (muzzleSpeed * t);

                            targetPointState = new KineticState(
                                weaponPos + directionEstimate * (muzzleSpeed * t),
                                (currentX.Translation - previousX.Translation) / dt
                            );
                            
                            //MyAPIGateway.Utilities.ShowMessage("A", $"Found in {start}/{step}/{budget} used {step-start}");

                            return true;
                        }
                    }
                }

                if (debug && step > 0)
                {
                    DsDebugDraw.DrawLine(
                        new LineD(previousX.Translation + previousTargetOffsetWorld, currentX.Translation + targetOffsetWorld),
                        Color.Red.ToVector4(),
                        2.5f
                    );
                }
                
                previousX = currentX;
                previousTargetOffsetWorld = targetOffsetWorld;

                var vDot = targetDriveAccelWorld;

                if (externalForceFunction != null)
                {
                    vDot += externalForceFunction.Invoke(
                        targetGrid,
                        currentX.Translation,
                        currentX.LinearVelocity
                    );
                }

                if (!applyMaxSpeedAfterStep && currentX.LinearVelocity.LengthSquared() > maxSpeedSqr)
                {
                    currentX.LinearVelocity = currentX.LinearVelocity.Normalized() * maxSpeed;
                }
                
                currentX.LinearVelocity += vDot * dt;
                currentX.Translation += currentX.LinearVelocity * dt;

                if (applyMaxSpeedAfterStep && currentX.LinearVelocity.LengthSquared() > maxSpeedSqr)
                {
                    currentX.LinearVelocity = currentX.LinearVelocity.Normalized() * maxSpeed;
                }
                
                targetOffsetWorld = Vector3D.Transform(targetOffsetWorld, rotIncr);
                targetDriveAccelWorld = Vector3D.Transform(targetDriveAccelWorld, rotIncr);
            }

            targetPointState = new KineticState(targetPos, targetVel);
            t = double.PositiveInfinity;
            
            return false;
        }
        
        private static bool CalculateAdvancedPdAimPrediction(Projectile targetProjectile, ref Vector3D targetPos, ref Vector3D targetVel, ref Vector3D weaponPos, ref Vector3D weaponVel, double crudeTti, double muzzleSpeed, bool debug, out KineticState targetPointState, out double t)
        {
            const double dt = 1.0 / 60.0;

            var targetAccel0 = (targetProjectile.PrevVelocity1 - targetProjectile.PrevVelocity0) / dt;
            var targetAccel1 = (targetVel - targetProjectile.PrevVelocity1) / dt;

            var targetAccel0N = targetAccel0.Length();
            var targetAccel1N = targetAccel1.Length();
            
            if (targetAccel0N < 1.0 || targetAccel1N < 1.0)
            {
                targetPointState = new KineticState(targetPos, targetVel);
                t = double.PositiveInfinity;
                return false;
            }

            var e0 = targetAccel0 / targetAccel0N;
            var e1 = targetAccel1 / targetAccel1N;
            
            // Twist axis:
            var w = Vector3D.Cross(e0, e1);
            var sinTheta = MathHelperD.Clamp(w.Length(), 0.0, 1.0);

            if (sinTheta < 1e-6)
            {
                targetPointState = new KineticState(targetPos, targetVel);
                t = double.PositiveInfinity;
                return false;
            }

            w /= sinTheta;
            
            var cosTheta = MathHelperD.Clamp(Vector3D.Dot(e0, e1), -1.0, 1.0);
            
            var maxSpeed = targetProjectile.MaxSpeed;
            var maxSpeedSqr = maxSpeed * maxSpeed;
            
            var targetAccelWorld = targetAccel1;
            
            var currentX = new KineticState(targetPos, targetVel);
            var previousX = new KineticState();

            // Reasonable bounds for the solution:
            var start = Math.Max((int)(crudeTti * 60 * 0.8), 1);
            var budget = Math.Max((int)(crudeTti * 60 * 1.2), 5);
            
            for (var step = 0; step <= budget; step++) // Budget + 1 steps
            {
                if (step > start)
                {
                    var a = previousX.Translation;
                    var t0 = step * dt - dt;
                    var d = currentX.Translation - a;
                    var u = weaponVel - d / dt;
                    var w1 = weaponPos - a + d * t0 / dt;
                    // ReSharper disable InconsistentNaming
                    // Honestly it would be best if we made a linear solver if A ~= 0. Maybe later
                    var A = Vector3D.Dot(u, u) - muzzleSpeed * muzzleSpeed;
                    var B = 2 * Vector3D.Dot(u, w1);
                    var C = Vector3D.Dot(w1, w1);
                    // ReSharper restore InconsistentNaming
                    var delta = B * B - 4.0 * A * C;

                    bool hasRoot;
                    if (delta < 0.0)
                    {
                        hasRoot = false;
                    }
                    else
                    {
                        var t1Frame = t0 + dt;
                        var f0 = A * t0 * t0 + B * t0 + C;
                        var f1 = A * t1Frame * t1Frame + B * t1Frame + C;

                        // If the sign changed over this interval, the interval contains the root:
                        hasRoot = (f0 <= 0.0 && f1 >= 0.0) || (f0 >= 0.0 && f1 <= 0.0);

                        if (!hasRoot)
                        {
                            // If the sign didn't change, it's still possible for the function to have both roots inside this interval.
                            // To check this, we verify the signs of the function at the two ends against the sign of the function at the vertex:
                            var tVertex = -B / (2.0 * A);
                            if (tVertex > t0 && tVertex < t1Frame)
                            {
                                var fVertex = -delta / (4.0 * A);
                                hasRoot = (f0 <= 0.0 && fVertex >= 0.0) || (f0 >= 0.0 && fVertex <= 0.0);
                            }
                        }
                    }

                    if (hasRoot)
                    {
                        delta = Math.Sqrt(delta);
                        var t1 = (-B - delta) / (2.0 * A);
                        var t2 = (-B + delta) / (2.0 * A);

                        t = double.PositiveInfinity;

                        if (t1 > t0 && t1 <= t0 + dt)
                        {
                            t = Math.Min(t, t1);
                        }

                        if (t2 > t0 && t2 <= t0 + dt)
                        {
                            t = Math.Min(t, t2);
                        }

                        if (!double.IsPositiveInfinity(t))
                        {
                            // Target GRID position:
                            //var positionEstimate = a + d * (t - t0) / dt;

                            // The actual launch direction:
                            var directionEstimate = -(u * t + w1) / (muzzleSpeed * t);

                            targetPointState = new KineticState(
                                weaponPos + directionEstimate * (muzzleSpeed * t),
                                (currentX.Translation - previousX.Translation) / dt
                            );
                            
                            //MyAPIGateway.Utilities.ShowMessage("A", $"Found in {start}/{step}/{budget} used {step-start}");

                            return true;
                        }
                    }
                }

                if (debug && step > 0)
                {
                    DsDebugDraw.DrawLine(
                        new LineD(previousX.Translation, currentX.Translation),
                        Color.Red.ToVector4(),
                        2.5f
                    );
                }
                
                previousX = currentX;
                
                currentX.LinearVelocity += targetAccelWorld * dt;
                currentX.Translation += currentX.LinearVelocity * dt;

                if (currentX.LinearVelocity.LengthSquared() > maxSpeedSqr)
                {
                    currentX.LinearVelocity = currentX.LinearVelocity.Normalized() * maxSpeed;
                }
                
                // Rotates using the rotation formula. We can do this since it's a 2DOF rotation.
                var r1 = targetAccelWorld * cosTheta;
                var r2 = Vector3D.Cross(w, targetAccelWorld) * sinTheta;
                var r3 = w * (Vector3D.Dot(w, targetAccelWorld) * (1.0 - cosTheta));

                targetAccelWorld = r1 + r2 + r3;
            }

            targetPointState = new KineticState(targetPos, targetVel);
            t = double.PositiveInfinity;
            
            return false;
        }
        
        #endregion
        
        public void ManualShootRayCallBack(IHitInfo hitInfo)
        {
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
            var checkDistanceSqr = (float)Vector3D.DistanceSquared(targetPos, weaponPos);

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
            if (ActiveAmmoDef.AmmoDef.IgnoreGrids)
                return false;

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
                IHitInfo fakeHitInfo;
                Session.I.Physics.CastRay(trackingCheckPosition, Target.TargetPos, out fakeHitInfo, filter);
                ManualShootRayCallBack(fakeHitInfo);
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
            if (distToTargetSqr > MaxTargetDistanceSqr || distToTargetSqr < MinTargetDistanceSqr) //TODO this was &&, will that ever trip for a wep with min and max range?
            {
                masterWeapon.Target.Reset(Session.I.Tick, Target.States.RayCheckDistExceeded);
                if (masterWeapon != this) Target.Reset(Session.I.Tick, Target.States.RayCheckDistExceeded);
                return false;
            }
            WaterData water = null;
            if (Session.I.WaterApiLoaded && !(ActiveAmmoDef.AmmoDef.IgnoreWater || Comp.TargetSubmerged) && Comp.Ai.InPlanetGravity && Comp.Ai.MyPlanet != null && Session.I.WaterMap.TryGetValue(Comp.Ai.MyPlanet.EntityId, out water))
            {
                var waterSphere = new BoundingSphereD(water.Center, water.MinRadius);
                if (waterSphere.Contains(targetPos) != ContainmentType.Disjoint)
                {
                    masterWeapon.Target.Reset(Session.I.Tick, Target.States.RayCheckFailed);
                    if (masterWeapon != this) Target.Reset(Session.I.Tick, Target.States.RayCheckFailed);
                    return false;
                }
            }
            IHitInfo rayHitInfo;
            Session.I.Physics.CastRay(trackingCheckPosition, targetPos, out rayHitInfo);

            RayCallBack.NormalShootRayCallBack(rayHitInfo);

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