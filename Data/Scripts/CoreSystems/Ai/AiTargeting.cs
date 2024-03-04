using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Xml;
using CoreSystems.Platform;
using CoreSystems.Projectiles;
using Sandbox.Game.Entities;
using Sandbox.ModAPI.Ingame;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;
using static CoreSystems.Support.WeaponDefinition;
using static CoreSystems.Support.WeaponDefinition.TargetingDef;
using static CoreSystems.Support.WeaponDefinition.TargetingDef.BlockTypes;
using static CoreSystems.Support.WeaponDefinition.AmmoDef;
using static CoreSystems.Platform.Weapon.ApiShootRequest;
using IMyWarhead = Sandbox.ModAPI.IMyWarhead;
using CollisionLayers = Sandbox.Engine.Physics.MyPhysics.CollisionLayers;
using Sandbox.ModAPI;

namespace CoreSystems.Support
{
    public partial class Ai
    {
        internal static void AcquireTarget(Weapon w, bool forceFocus, MyEntity targetEntity = null)
        {
            var foundTarget = false;
            if (w.PosChangedTick != Session.I.SimulationCount) 
                w.UpdatePivotPos();

            var comp = w.Comp;
            var masterAi = w.Comp.MasterAi;
            var mOverrides = comp.MasterOverrides;
            var cMode = mOverrides.Control;
            FakeTarget.FakeWorldTargetInfo fakeInfo = null;
            if (cMode == ProtoWeaponOverrides.ControlModes.Auto || cMode == ProtoWeaponOverrides.ControlModes.Painter && !w.Comp.PainterMode)
            {
                w.AimCone.ConeDir = w.MyPivotFwd;
                w.AimCone.ConeTip = w.BarrelOrigin + (w.MyPivotFwd * w.MuzzleDistToBarrelCenter);
                var request = w.ShootRequest;
                var projectileRequest = request.Type == TargetType.Projectile;
                var pCount = masterAi.LiveProjectile.Count;
                var shootProjectile = pCount > 0 && (w.System.TrackProjectile || projectileRequest || w.Comp.Ai.ControlComp != null) && mOverrides.Projectiles && !w.System.FocusOnly;
                var projectilesFirst = !forceFocus && shootProjectile && w.System.ProjectilesFirst;
                var projectilesOnly =  w.System.ProjectilesOnly || projectileRequest || w.ProjectilesNear && !w.Target.TargetChanged && Session.I.Count != w.Acquire.SlotId && !forceFocus;
                var checkObstructions = w.System.ScanNonThreats && !w.System.FocusOnly && masterAi.Obstructions.Count > 0;
                
                if (!projectilesFirst && w.System.TrackTopMostEntities && !projectilesOnly && !w.System.NonThreatsOnly)
                    foundTarget = AcquireTopMostEntity(w, mOverrides, forceFocus, targetEntity);
                else if (!forceFocus && shootProjectile)
                    foundTarget = AcquireProjectile(w, request.ProjectileId);

                if (projectilesFirst && !foundTarget && !projectilesOnly && !w.System.NonThreatsOnly)
                    foundTarget = AcquireTopMostEntity(w, mOverrides, false, targetEntity);

                if (!foundTarget && checkObstructions)
                {
                    foundTarget = AcquireObstruction(w, mOverrides);
                }
            }
            else if (!w.System.ScanTrackOnly && w.ValidFakeTargetInfo(w.Comp.Data.Repo.Values.State.PlayerId, out fakeInfo))
            {
                Vector3D predictedPos;
                if (Weapon.CanShootTarget(w, ref fakeInfo.WorldPosition, fakeInfo.LinearVelocity, fakeInfo.Acceleration, out predictedPos, false, null, MathFuncs.DebugCaller.CanShootTarget1))
                {
                    w.Target.SetFake(Session.I.Tick, predictedPos, w.MyPivotPos);
                    if (w.ActiveAmmoDef.AmmoDef.Trajectory.Guidance != TrajectoryDef.GuidanceType.None || !w.MuzzleHitSelf())
                        foundTarget = true;
                }
            }

            if (!foundTarget)
            {
                if (w.Target.CurrentState == Target.States.Acquired && w.Acquire.IsSleeping && w.Acquire.Monitoring && Session.I.AcqManager.MonitorState.Remove(w.Acquire))
                    w.Acquire.Monitoring = false;

                if (w.NewTarget.CurrentState != Target.States.NoTargetsSeen) 
                    w.NewTarget.Reset(Session.I.Tick, Target.States.NoTargetsSeen);
                
                if (w.Target.CurrentState != Target.States.NoTargetsSeen)
                    w.Target.Reset(Session.I.Tick, Target.States.NoTargetsSeen, fakeInfo == null);

                w.LastBlockCount = masterAi.BlockCount;

                if (w.AcquiredBlock) {
                    ++w.FailedAcquires;
                    w.AcquiredBlock = false;
                }
            }
            else 
                w.WakeTargets();

            ++w.AcquireAttempts;
        }

        private static bool AcquireTopMostEntity(Weapon w, ProtoWeaponOverrides overRides, bool attemptReset = false, MyEntity targetEntity = null)
        {
            var s = w.System;
            var comp = w.Comp;
            var ai = comp.MasterAi;
            TargetInfo gridInfo = null;
            var forceTarget = false;
            if (targetEntity != null)
            {
                if (ai.Targets.TryGetValue(targetEntity, out gridInfo))
                {
                    forceTarget = true;
                }
            }

            var ammoDef = w.ActiveAmmoDef.AmmoDef;
            var focusOnly = overRides.FocusTargets || w.System.FocusOnly;

            var aConst = ammoDef.Const;
            var attackNeutrals = overRides.Neutrals || w.System.ScanTrackOnly;
            var attackFriends = overRides.Friendly || w.System.ScanTrackOnly;
            var attackNoOwner = overRides.Unowned || w.System.ScanTrackOnly;
            var forceFoci = focusOnly || w.System.ScanTrackOnly;
            var session = Session.I;
            session.TargetRequests++;
            var weaponPos = w.BarrelOrigin + (w.MyPivotFwd * w.MuzzleDistToBarrelCenter);
            var target = w.NewTarget;
            var accelPrediction = (int)s.Values.HardPoint.AimLeadingPrediction > 1;
            var minRadius = overRides.MinSize * 0.5f;
            var maxRadius = overRides.MaxSize * 0.5f;
            var minTargetRadius = minRadius > 0 ? minRadius : s.MinTargetRadius;
            var maxTargetRadius = maxRadius < s.MaxTargetRadius ? maxRadius : s.MaxTargetRadius;

            var moveMode = overRides.MoveMode;
            var movingMode = moveMode == ProtoWeaponOverrides.MoveModes.Moving;
            var fireOnStation = moveMode == ProtoWeaponOverrides.MoveModes.Any || moveMode == ProtoWeaponOverrides.MoveModes.Moored;
            var stationOnly = moveMode == ProtoWeaponOverrides.MoveModes.Moored;
            BoundingSphereD waterSphere = new BoundingSphereD(Vector3D.Zero, 1f);
            WaterData water = null;
            if (session.WaterApiLoaded && !ammoDef.IgnoreWater && ai.InPlanetGravity && ai.MyPlanet != null && session.WaterMap.TryGetValue(ai.MyPlanet.EntityId, out water))
                waterSphere = new BoundingSphereD(ai.MyPlanet.PositionComp.WorldAABB.Center, water.MinRadius);

            var rootConstruct = ai.Construct.RootAi.Construct;
            int offset = 0;
            w.FoundTopMostTarget = false;
            var focusGrid = rootConstruct.LastFocusEntity as MyCubeGrid;
            var lastFocusGrid = rootConstruct.LastFocusEntityChecked as MyCubeGrid;
            var cachedCollection = rootConstruct.ThreatCacheCollection;
            if (rootConstruct.HadFocus && (!w.System.ScanTrackOnly || w.System.FocusOnly) && !w.System.TargetSlaving) {
                if (focusGrid != null) {
                    if (cachedCollection.Count == 0 || session.Tick - rootConstruct.LastFocusConstructTick > 180 || lastFocusGrid == null || !focusGrid.IsSameConstructAs(lastFocusGrid)) {
                        rootConstruct.LastFocusEntityChecked = focusGrid;
                        rootConstruct.LastFocusConstructTick = session.Tick;
                        session.GetSortedConstructCollection(ai, focusGrid);
                    }
                    offset = cachedCollection.Count;
                }
                else if (rootConstruct.LastFocusEntity != null)
                    offset = 1;
            }
            else if (w.System.TargetSlaving && !rootConstruct.GetExportedCollection(w, Constructs.ScanType.Threats)) 
                    return false;

            var collection = !w.System.TargetSlaving ? ai.SortedTargets : ai.ThreatCollection;
            var numOfTargets = collection.Count;

            int checkSize;
            if (w.System.CycleTargets <= 0)
                checkSize = numOfTargets;
            else if (w.System.CycleTargets > numOfTargets)
                checkSize = w.System.CycleTargets - numOfTargets;
            else
                checkSize = w.System.CycleTargets;

            var chunk = numOfTargets > 0 ? checkSize * w.AcquireAttempts % numOfTargets : 0;

            if (chunk + checkSize >= numOfTargets)
                checkSize = numOfTargets - chunk;

            var deck = GetDeck(ref session.TargetDeck, chunk, checkSize, w.System.TopTargets, ref w.TargetData.WeaponRandom.AcquireRandom);

            var adjTargetCount = forceFoci && (offset > 0 || focusOnly) ? offset : (checkSize + offset);
            for (int x = 0; x < adjTargetCount; x++)
            {
                var focusTarget = offset > 0 && x < offset;
                var lastOffset = offset - 1;
                if (!focusTarget && (attemptReset || aConst.SkipAimChecks && !w.RotorTurretTracking || focusOnly)) 
                    break;

                TargetInfo info;
                if (focusTarget && cachedCollection.Count > 0)
                {
                    info = cachedCollection[x];
                }
                else if (focusTarget)
                {
                    ai.Targets.TryGetValue(rootConstruct.LastFocusEntity, out info);
                }
                else
                {
                    info = collection[deck[x - offset]];
                }

                if (info?.Target == null || info.Target.MarkedForClose)
                    continue;

                if (forceTarget && !focusTarget) 
                    info = gridInfo;
                else if (focusTarget && !attackFriends && info.EntInfo.Relationship == MyRelationsBetweenPlayerAndBlock.Friends)
                    continue;

                var grid = info.Target as MyCubeGrid;

                if (offset > 0 && x > lastOffset && (grid != null && focusGrid != null && grid.IsSameConstructAs(focusGrid)) || !attackNeutrals && info.EntInfo.Relationship == MyRelationsBetweenPlayerAndBlock.Neutral || !attackNoOwner && info.EntInfo.Relationship == MyRelationsBetweenPlayerAndBlock.NoOwnership)
                    continue;

                Weapon.TargetOwner tOwner;
                if (w.System.UniqueTargetPerWeapon && w.Comp.ActiveTargets.TryGetValue(info.Target, out tOwner) && tOwner.Weapon != w) {
                    
                    var evict = w.System.EvictUniqueTargets && !tOwner.Weapon.System.EvictUniqueTargets;
                    if (!evict)
                        continue;
                }

                if (w.System.ScanTrackOnly && !ValidScanEntity(w, info.EntInfo, info.Target, true))
                    continue;

                if (movingMode && info.VelLenSqr < 1 || !fireOnStation && info.IsStatic || stationOnly && !info.IsStatic)
                    continue;

                var character = info.Target as IMyCharacter;

                var targetRadius = character != null ? info.TargetRadius * 5 : info.TargetRadius;
                if (targetRadius < minTargetRadius || info.TargetRadius > maxTargetRadius && maxTargetRadius < 8192 || !focusTarget && info.OffenseRating <= 0) continue;
                
                var targetCenter = info.Target.PositionComp.WorldAABB.Center;
                var targetDistSqr = Vector3D.DistanceSquared(targetCenter, weaponPos);

                if (targetDistSqr > (w.MaxTargetDistance + info.TargetRadius) * (w.MaxTargetDistance + info.TargetRadius) || targetDistSqr < w.MinTargetDistanceSqr) continue;
                
                if (water != null) {
                    if (new BoundingSphereD(ai.MyPlanet.PositionComp.WorldAABB.Center, water.MinRadius).Contains(new BoundingSphereD(targetCenter, targetRadius)) == ContainmentType.Contains)
                        continue;
                }

                session.TargetChecks++;
                Vector3D targetLinVel = info.Target.Physics?.LinearVelocity ?? Vector3D.Zero;
                Vector3D targetAccel = accelPrediction ? info.Target.Physics?.LinearAcceleration ?? Vector3D.Zero : Vector3.Zero;
                Vector3D predictedPos;
                if (w.System.TargetGridCenter)
                {
                    if (!Weapon.CanShootTarget(w, ref targetCenter, targetLinVel, targetAccel, out predictedPos, false, null, MathFuncs.DebugCaller.CanShootTarget2)) continue;
                    double rayDist;
                    Vector3D.Distance(ref weaponPos, ref targetCenter, out rayDist);
                    var shortDist = rayDist;
                    var origDist = rayDist;
                    var topEntId = info.Target.GetTopMostParent().EntityId;
                    target.Set(info.Target, targetCenter, shortDist, origDist, topEntId);
                    target.TransferTo(w.Target, Session.I.Tick);

                    if (w.Target.TargetState == Target.TargetStates.IsEntity)
                        Session.I.NewThreat(w);
                    return true;
                }

                if (info.IsGrid)
                {
                    if (!s.TrackGrids || !overRides.Grids || (!overRides.LargeGrid && info.LargeGrid) || (!overRides.SmallGrid && !info.LargeGrid) || !focusTarget && info.FatCount < 2) continue;
                    session.CanShoot++;
                    Vector3D newCenter;
                    if (!w.TurretController && !w.RotorTurretTracking)
                    {

                        var validEstimate = true;
                        newCenter = w.System.Prediction != HardPointDef.Prediction.Off && (!aConst.IsBeamWeapon && aConst.DesiredProjectileSpeed * w.VelocityMult > 0) ? Weapon.TrajectoryEstimation(w, targetCenter, targetLinVel, targetAccel, weaponPos, out validEstimate, true) : targetCenter;
                        var targetSphere = info.Target.PositionComp.WorldVolume;
                        targetSphere.Center = newCenter;

                        if (!validEstimate || !aConst.SkipAimChecks && !MathFuncs.TargetSphereInCone(ref targetSphere, ref w.AimCone)) 
                            continue;
                    }
                    else if (!Weapon.CanShootTargetObb(w, info.Target, targetLinVel, targetAccel, out newCenter)) 
                        continue;

                    if (ai.FriendlyShieldNear)
                    {
                        var targetDir = newCenter - weaponPos;
                        if (w.HitFriendlyShield(weaponPos, newCenter, targetDir))
                            continue;
                    }

                    w.FoundTopMostTarget = true;

                    if (w.FailedAcquires > 9 && !w.DelayedAcquire(info))
                        continue;
                        
                    if (!AcquireBlock(w, target, info, ref waterSphere, ref w.XorRnd, null, !focusTarget))
                        continue;

                    target.TransferTo(w.Target, Session.I.Tick);
                    if (w.Target.TargetState == Target.TargetStates.IsEntity)
                        Session.I.NewThreat(w);

                    return true;
                }

                var meteor = info.Target as MyMeteor;
                if (meteor != null && (!s.TrackMeteors || !overRides.Meteors)) 
                    continue;
                
                if (character != null && (!overRides.Biologicals || character.IsDead || character.Integrity <= 0 || session.AdminMap.ContainsKey(character))) 
                    continue;


                if (!Weapon.CanShootTarget(w, ref targetCenter, targetLinVel, targetAccel, out predictedPos, true, info.Target, MathFuncs.DebugCaller.CanShootTarget3))
                    continue;

                if (ai.FriendlyShieldNear)
                {
                    var targetDir = predictedPos - weaponPos;
                    if (w.HitFriendlyShield(weaponPos, predictedPos, targetDir))
                        continue;
                }
                
                session.TopRayCasts++;

                if (w.LastHitInfo?.HitEntity != null && (!w.System.Values.HardPoint.Other.MuzzleCheck || !w.MuzzleHitSelf()))
                {
                    TargetInfo hitInfo;
                    if (w.LastHitInfo.HitEntity == info.Target || ai.Targets.TryGetValue((MyEntity)w.LastHitInfo.HitEntity, out hitInfo) && (hitInfo.EntInfo.Relationship == MyRelationsBetweenPlayerAndBlock.Enemies || hitInfo.EntInfo.Relationship == MyRelationsBetweenPlayerAndBlock.Neutral || hitInfo.EntInfo.Relationship == MyRelationsBetweenPlayerAndBlock.NoOwnership))
                    {
                        double rayDist;
                        Vector3D.Distance(ref weaponPos, ref targetCenter, out rayDist);
                        var shortDist = rayDist * (1 - w.LastHitInfo.Fraction);
                        var origDist = rayDist * w.LastHitInfo.Fraction;
                        var topEntId = info.Target.GetTopMostParent().EntityId;
                        target.Set(info.Target, w.LastHitInfo.Position, shortDist, origDist, topEntId);
                        target.TransferTo(w.Target, Session.I.Tick);
                        
                        w.FoundTopMostTarget = true;

                        if (w.Target.TargetState == Target.TargetStates.IsEntity)
                            Session.I.NewThreat(w);

                        return true;
                    }
                }
                else if (aConst.SkipRayChecks)
                {
                    double rayDist;
                    Vector3D.Distance(ref weaponPos, ref targetCenter, out rayDist);
                    var shortDist = rayDist;
                    var origDist = rayDist;
                    var topEntId = info.Target.GetTopMostParent().EntityId;
                    target.Set(info.Target, targetCenter, shortDist, origDist, topEntId);
                    target.TransferTo(w.Target, Session.I.Tick);

                    w.FoundTopMostTarget = true;

                    if (w.Target.TargetState == Target.TargetStates.IsEntity)
                        Session.I.NewThreat(w);

                    return true;
                }
                if (forceTarget) break;
            }

            return attemptReset && w.Target.HasTarget;
        }

        private static bool AcquireObstruction(Weapon w, ProtoWeaponOverrides overRides)
        {
            var s = w.System;
            var comp = w.Comp;
            var ai = comp.MasterAi;
            var ammoDef = w.ActiveAmmoDef.AmmoDef;
            var aConst = ammoDef.Const;
            var session = Session.I;
            session.TargetRequests++;

            var weaponPos = w.BarrelOrigin + (w.MyPivotFwd * w.MuzzleDistToBarrelCenter);

            var target = w.NewTarget;
            var accelPrediction = (int)s.Values.HardPoint.AimLeadingPrediction > 1;
            var minRadius = overRides.MinSize * 0.5f;
            var maxRadius = overRides.MaxSize * 0.5f;
            var minTargetRadius = minRadius > 0 ? minRadius : s.MinTargetRadius;
            var maxTargetRadius = maxRadius < s.MaxTargetRadius ? maxRadius : s.MaxTargetRadius;

            var moveMode = overRides.MoveMode;
            var movingMode = moveMode == ProtoWeaponOverrides.MoveModes.Moving;
            var fireOnStation = moveMode == ProtoWeaponOverrides.MoveModes.Any || moveMode == ProtoWeaponOverrides.MoveModes.Moored;
            var stationOnly = moveMode == ProtoWeaponOverrides.MoveModes.Moored;

            w.FoundTopMostTarget = false;

            if (w.System.TargetSlaving && !w.Comp.Ai.Construct.RootAi.Construct.GetExportedCollection(w, Constructs.ScanType.NonThreats))
                return false;

            var collection = !w.System.TargetSlaving ? ai.Obstructions : ai.NonThreatCollection;
            var numOfTargets = collection.Count;
            
            int checkSize;
            if (w.System.CycleTargets <= 0)
                checkSize = numOfTargets;
            else if (w.System.CycleTargets > numOfTargets)
                checkSize = w.System.CycleTargets - numOfTargets;
            else
                checkSize = w.System.CycleTargets;

            var chunk = numOfTargets > 0 ? checkSize * w.AcquireAttempts % numOfTargets : 0;

            if (chunk + checkSize >= numOfTargets)
                checkSize = numOfTargets - chunk;

            var deck = GetDeck(ref session.TargetDeck, chunk, checkSize, w.System.TopTargets, ref w.TargetData.WeaponRandom.AcquireRandom);
            for (int x = 0; x < checkSize; x++)
            {
                if (aConst.SkipAimChecks)
                    break;

                var info = collection[deck[x]];


                if (info.Target?.Physics == null || info.Target.MarkedForClose)
                    continue;

                if (!ValidScanEntity(w, info.EntInfo, info.Target))
                    continue;

                var grid = info.Target as MyCubeGrid;
                var character = info.Target as IMyCharacter;

                if (movingMode && !info.Target.Physics.IsMoving || !fireOnStation && info.Target.Physics.IsStatic || stationOnly && !info.Target.Physics.IsStatic)
                    continue;


                var targetRadius = character != null ? info.Target.PositionComp.LocalVolume.Radius * 5 : info.Target.PositionComp.LocalVolume.Radius;
                if (targetRadius < minTargetRadius || targetRadius > maxTargetRadius && maxTargetRadius < 8192) continue;

                var targetCenter = info.Target.PositionComp.WorldAABB.Center;
                var targetDistSqr = Vector3D.DistanceSquared(targetCenter, weaponPos);

                if (targetDistSqr > (w.MaxTargetDistance + targetRadius) * (w.MaxTargetDistance + targetRadius) || targetDistSqr < w.MinTargetDistanceSqr) continue;

                session.TargetChecks++;
                Vector3D targetLinVel = info.Target.Physics?.LinearVelocity ?? Vector3D.Zero;
                Vector3D targetAccel = accelPrediction ? info.Target.Physics?.LinearAcceleration ?? Vector3D.Zero : Vector3.Zero;
                double rayDist;

                if (grid != null)
                {
                    if (!overRides.Grids || (!overRides.LargeGrid && info.LargeGrid) || (!overRides.SmallGrid && !info.LargeGrid) || grid.CubeBlocks.Count == 0) continue;
                    session.CanShoot++;
                    Vector3D newCenter;

                    if (!w.TurretController && !w.RotorTurretTracking)
                    {

                        var validEstimate = true;
                        newCenter = w.System.Prediction != HardPointDef.Prediction.Off && (!aConst.IsBeamWeapon && aConst.DesiredProjectileSpeed * w.VelocityMult > 0) ? Weapon.TrajectoryEstimation(w, targetCenter, targetLinVel, targetAccel, weaponPos,  out validEstimate, true) : targetCenter;
                        var targetSphere = info.Target.PositionComp.WorldVolume;
                        targetSphere.Center = newCenter;

                        if (!validEstimate || !aConst.SkipAimChecks && !MathFuncs.TargetSphereInCone(ref targetSphere, ref w.AimCone)) continue;
                    }
                    else if (!Weapon.CanShootTargetObb(w, info.Target, targetLinVel, targetAccel,  out newCenter)) continue;

                    w.FoundTopMostTarget = true;
                    var pos = grid.PositionComp.WorldVolume.Center;
                    Vector3D.Distance(ref weaponPos, ref pos, out rayDist);
                    target.Set(grid, pos, rayDist, rayDist, grid.EntityId);

                    target.TransferTo(w.Target, Session.I.Tick);
                    if (w.Target.TargetState == Target.TargetStates.IsEntity)
                        Session.I.NewThreat(w);

                    return true;
                }

                var meteor = info.Target as MyMeteor;
                if (meteor != null && (!s.TrackMeteors || !overRides.Meteors)) continue;

                if (character != null && (false && !overRides.Biologicals || character.IsDead || character.Integrity <= 0)) continue;

                Vector3D predictedPos;
                if (!Weapon.CanShootTarget(w, ref targetCenter, targetLinVel, targetAccel, out predictedPos, true, info.Target, MathFuncs.DebugCaller.CanShootTarget4)) continue;

                session.TopRayCasts++;

                Vector3D.Distance(ref weaponPos, ref targetCenter, out rayDist);
                var shortDist = rayDist;
                var origDist = rayDist;
                var topEntId = info.Target.GetTopMostParent().EntityId;
                target.Set(info.Target, targetCenter, shortDist, origDist, topEntId);
                target.TransferTo(w.Target, Session.I.Tick);

                w.FoundTopMostTarget = true;

                if (w.Target.TargetState == Target.TargetStates.IsEntity)
                    Session.I.NewThreat(w);

                return true;
            }

            return w.Target.HasTarget;
        }

        internal static bool AcquireProjectile(Weapon w, ulong id = ulong.MaxValue)
        {
            var ai = w.Comp.MasterAi;
            var system = w.System;
            var s = Session.I;
            var physics = Session.I.Physics;
            var target = w.NewTarget;
            var weaponPos = w.BarrelOrigin;
            var aConst = w.ActiveAmmoDef.AmmoDef.Const;
            BoundingSphereD waterSphere = new BoundingSphereD(Vector3D.Zero, 1f);
            WaterData water = null;
            if (Session.I.WaterApiLoaded && !w.ActiveAmmoDef.AmmoDef.IgnoreWater && ai.InPlanetGravity && ai.MyPlanet != null && Session.I.WaterMap.TryGetValue(ai.MyPlanet.EntityId, out water))
                waterSphere = new BoundingSphereD(ai.MyPlanet.PositionComp.WorldAABB.Center, water.MinRadius);

            var wepAiOwnerFactionId = w.Comp.MasterAi.AiOwnerFactionId;
            var lockedOnly = w.System.Values.Targeting.LockedSmartOnly;
            var smartOnly = w.System.Values.Targeting.IgnoreDumbProjectiles;
            var comp = w.Comp;
            var mOverrides = comp.MasterOverrides;
            var collection = ai.GetProCache(w, mOverrides.SupportingPD);
            var minRadius = mOverrides.MinSize * 0.5f;
            var maxRadius = mOverrides.MaxSize * 0.5f;
            var minTargetRadius = minRadius > 0 ? minRadius : system.MinTargetRadius;
            var maxTargetRadius = maxRadius < system.MaxTargetRadius ? maxRadius : system.MaxTargetRadius;


            int index = int.MinValue;
            if (id != ulong.MaxValue) {
                if (!GetProjectileIndex(collection, id, out index)) 
                    return false;
            }
            else if (system.ClosestFirst)
            {
                int length = collection.Count;
                for (int h = length / 2; h > 0; h /= 2)
                {
                    for (int i = h; i < length; i += 1)
                    {
                        var tempValue = collection[i];
                        double temp;
                        Vector3D.DistanceSquared(ref collection[i].Position, ref weaponPos, out temp);

                        int j;
                        for (j = i; j >= h && Vector3D.DistanceSquared(collection[j - h].Position, weaponPos) > temp; j -= h)
                            collection[j] = collection[j - h];

                        collection[j] = tempValue;
                    }
                }
            }

            var numOfTargets = index < -1 ? collection.Count : index < 0 ? 0 : 1;

            int[] deck = null;
            var checkSize = numOfTargets;

            if (index < -1)
            {
                var numToRandomize = system.ClosestFirst ? w.System.TopTargets : numOfTargets;

                if (w.System.CycleTargets <= 0)
                    checkSize = numOfTargets;
                else if (w.System.CycleTargets > numOfTargets)
                    checkSize = w.System.CycleTargets - numOfTargets;
                else
                    checkSize = w.System.CycleTargets;

                var chunk = numOfTargets > 0 ? checkSize * w.AcquireAttempts % numOfTargets : 0;

                if (chunk + checkSize >= numOfTargets)
                    checkSize = numOfTargets - chunk;

                deck = GetDeck(ref s.TargetDeck, chunk, checkSize, numToRandomize, ref w.TargetData.WeaponRandom.AcquireRandom);
            }

            for (int x = 0; x < checkSize; x++)
            {
                var card = index < -1 ? deck[x] : index;
                var lp = collection[card];

                if (water != null && waterSphere.Contains(lp.Position) == ContainmentType.Contains)
                    continue;
                var lpAiOwnerFactionId = lp.Info.FactionId;
                if (!mOverrides.Neutrals && wepAiOwnerFactionId > 0 && lpAiOwnerFactionId > 0 && MyAPIGateway.Session.Factions.GetRelationBetweenFactions(lpAiOwnerFactionId, wepAiOwnerFactionId) == MyRelationsBetweenFactions.Neutral)
                    continue;
                var cube = lp.Info.Target.TargetObject as MyCubeBlock;
                Weapon.TargetOwner tOwner;
                var distSqr = Vector3D.DistanceSquared(lp.Position, weaponPos);
                if (lp.State != Projectile.ProjectileState.Alive || lp.MaxSpeed > system.MaxTargetSpeed || lp.MaxSpeed <= 0 || distSqr > w.MaxTargetDistanceSqr || distSqr < w.MinTargetDistanceBufferSqr || w.System.UniqueTargetPerWeapon && w.Comp.ActiveTargets.TryGetValue(lp, out tOwner) && tOwner.Weapon != w) continue;

                var lpaConst = lp.Info.AmmoDef.Const;

                var smart = lpaConst.IsDrone || lpaConst.IsSmart;
                if (smartOnly && !smart || lockedOnly && (!smart || cube != null && w.Comp.IsBlock && cube.CubeGrid.IsSameConstructAs(w.Comp.Ai.GridEntity)))
                    continue;

                var targetRadius = lpaConst.CollisionSize;
                if (targetRadius < minTargetRadius || targetRadius > maxTargetRadius && maxTargetRadius < 8192) continue;

                var lpAccel = lp.Velocity - lp.PrevVelocity;

                Vector3D predictedPos;
                if (Weapon.CanShootTarget(w, ref lp.Position, lp.Velocity, lpAccel, out predictedPos, false, null, MathFuncs.DebugCaller.CanShootTarget5))
                {

                    var needsCast = false;
                    if (!aConst.CheckFutureIntersection)
                    {
                        for (int i = 0; i < ai.Obstructions.Count; i++)
                        {
                            var ent = ai.Obstructions[i].Target;

                            if (ent == null)
                            {
                                Log.Line($"AcquireProjectile had null obstruction entity");
                                continue;
                            }
                            if (ent is MyPlanet)
                                continue;
                            var obsSphere = ent.PositionComp.WorldVolume;

                            var dir = lp.Position - weaponPos;
                            var beam = new RayD(ref weaponPos, ref dir);

                            if (beam.Intersects(obsSphere) != null)
                            {
                                var transform = ent.PositionComp.WorldMatrixRef;
                                var box = ent.PositionComp.LocalAABB;
                                var obb = new MyOrientedBoundingBoxD(box, transform);
                                if (obb.Intersects(ref beam) != null)
                                {
                                    needsCast = true;
                                    break;
                                }
                            }
                        }
                    }


                    if (needsCast)
                    {
                        IHitInfo hitInfo;
                        var oneHalfKmSqr = 2250000;
                        var lowFiVoxels = distSqr > oneHalfKmSqr && (ai.PlanetSurfaceInRange || ai.ClosestVoxelSqr <= oneHalfKmSqr);
                        var filter = w.System.NoVoxelLosCheck ? CollisionLayers.NoVoxelCollisionLayer : lowFiVoxels ? CollisionLayers.DefaultCollisionLayer : CollisionLayers.VoxelLod1CollisionLayer;
                       
                        physics.CastRay(weaponPos, lp.Position, out hitInfo, filter);
                        if (hitInfo?.HitEntity == null && (!w.System.Values.HardPoint.Other.MuzzleCheck || !w.MuzzleHitSelf()))
                        {
                            double hitDist;
                            Vector3D.Distance(ref weaponPos, ref lp.Position, out hitDist);
                            var shortDist = hitDist;
                            var origDist = hitDist;
                            target.Set(lp, lp.Position, shortDist, origDist, long.MaxValue);
                            target.TransferTo(w.Target, Session.I.Tick);
                            return true;
                        }
                    }
                    else
                    {
                        Vector3D? hitInfo;
                        if (ai.AiType == AiTypes.Grid && GridIntersection.BresenhamGridIntersection(ai.GridEntity, ref weaponPos, ref lp.Position, out hitInfo, w.Comp.Cube, ai))
                            continue;

                        double hitDist;
                        Vector3D.Distance(ref weaponPos, ref lp.Position, out hitDist);
                        var shortDist = hitDist;
                        var origDist = hitDist;
                        target.Set(lp, lp.Position, shortDist, origDist, long.MaxValue);
                        target.TransferTo(w.Target, Session.I.Tick);

                        return true;
                    }
                }
            }

            return false;
        }

        private static bool GetProjectileIndex(List<Projectile> collection, ulong id, out int index)
        {
            if (id != ulong.MaxValue && collection.Count > 0)
            {
                for (int i = 0; i < collection.Count; i++)
                {
                    if (collection[i].Info.Id == id)
                    {
                        index = i;
                        return true;
                    }
                }
            }

            index = -1;
            return false;
        }

        internal static bool ReacquireTarget(Projectile p)
        {
            var info = p.Info;
            if (info.CompSceneVersion != info.Weapon.Comp.SceneVersion)
                return false;

            var w = info.Weapon;
            var s = w.System;
            var target = info.Target;
            info.Storage.ChaseAge = (int) info.RelativeAge;
            var ai = info.Ai;
            var session = Session.I;

            var aConst = info.AmmoDef.Const;
            var overRides = w.Comp.Data.Repo.Values.Set.Overrides;
            var attackNeutrals = overRides.Neutrals;
            var attackFriends = overRides.Friendly;
            var attackNoOwner = overRides.Unowned;
            var forceFoci = overRides.FocusTargets || aConst.FocusOnly;
            var minRadius = overRides.MinSize * 0.5f;
            var maxRadius = overRides.MaxSize * 0.5f;
            var minTargetRadius = minRadius > 0 ? minRadius : s.MinTargetRadius;
            var maxTargetRadius = maxRadius < s.MaxTargetRadius ? maxRadius : s.MaxTargetRadius;
            var moveMode = overRides.MoveMode;
            var movingMode = moveMode == ProtoWeaponOverrides.MoveModes.Moving;
            var fireOnStation = moveMode == ProtoWeaponOverrides.MoveModes.Any || moveMode == ProtoWeaponOverrides.MoveModes.Moored;
            var stationOnly = moveMode == ProtoWeaponOverrides.MoveModes.Moored;
            var acquired = false;
            var previousEntity = info.AcquiredEntity;
            BoundingSphereD waterSphere = new BoundingSphereD(Vector3D.Zero, 1f);
            WaterData water = null;
            if (Session.I.WaterApiLoaded && !info.AmmoDef.IgnoreWater && ai.InPlanetGravity && ai.MyPlanet != null && Session.I.WaterMap.TryGetValue(ai.MyPlanet.EntityId, out water))
                waterSphere = new BoundingSphereD(ai.MyPlanet.PositionComp.WorldAABB.Center, water.MinRadius);
            TargetInfo alphaInfo = null;
            int offset = 0;
            MyEntity fTarget;
            if (!aConst.OverrideTarget && ai.Construct.Data.Repo.FocusData.Target > 0 && MyEntities.TryGetEntityById(ai.Construct.Data.Repo.FocusData.Target, out fTarget) && ai.Targets.TryGetValue(fTarget, out alphaInfo))
                offset++;

            if (aConst.FocusOnly && offset <= 0)
                return false;

            MyEntity topTarget = null;
            if (previousEntity && !aConst.OverrideTarget && target.TargetState == Target.TargetStates.IsEntity)
            {
                topTarget = ((MyEntity)target.TargetObject).GetTopMostParent() ?? alphaInfo?.Target;
                if (topTarget != null && topTarget.MarkedForClose)
                    topTarget = null;
            }

            var numOfTargets = ai.SortedTargets.Count;
            var hasOffset = offset > 0;

            int checkSize;
            if (w.System.CycleTargets <= 0)
                checkSize = numOfTargets;
            else if (w.System.CycleTargets > numOfTargets)
                checkSize = w.System.CycleTargets - numOfTargets;
            else
                checkSize = w.System.CycleTargets;

            var chunk = numOfTargets > 0 ? checkSize * w.AcquireAttempts % numOfTargets : 0;

            if (chunk + checkSize >= numOfTargets)
                checkSize = numOfTargets - chunk;

            var adjTargetCount = forceFoci && hasOffset ? offset : checkSize + offset;

            var deck = GetDeck(ref session.TargetDeck, chunk, checkSize, w.System.TopTargets, ref p.Info.Random);

            for (int i = 0; i < adjTargetCount; i++)
            {
                var focusTarget = hasOffset && i < offset;
                var lastOffset = offset - 1;

                if (aConst.FocusOnly && i > lastOffset)
                    break;

                TargetInfo tInfo;
                if (i == 0 && alphaInfo != null) tInfo = alphaInfo;
                else tInfo = ai.SortedTargets[deck[i - offset]];

                if (!focusTarget && tInfo.OffenseRating <= 0 || focusTarget && !attackFriends && tInfo.EntInfo.Relationship == MyRelationsBetweenPlayerAndBlock.Friends || tInfo.Target == null || tInfo.Target.MarkedForClose || hasOffset && i > lastOffset && (tInfo.Target == alphaInfo?.Target))
                {
                    continue;
                }

                if (!attackNeutrals && tInfo.EntInfo.Relationship == MyRelationsBetweenPlayerAndBlock.Neutral || !attackNoOwner && tInfo.EntInfo.Relationship == MyRelationsBetweenPlayerAndBlock.NoOwnership) continue;

                if (movingMode && tInfo.VelLenSqr < 1 || !fireOnStation && tInfo.IsStatic || stationOnly && !tInfo.IsStatic)
                    continue;

                var character = tInfo.Target as IMyCharacter;
                if (character != null && (!s.TrackCharacters || !overRides.Biologicals)) continue;

                var meteor = tInfo.Target as MyMeteor;
                if (meteor != null && (!s.TrackMeteors || !overRides.Meteors)) continue;

                var targetPos = tInfo.Target.PositionComp.WorldAABB.Center;

                double distSqr;
                Vector3D.DistanceSquared(ref targetPos, ref p.Position, out distSqr);

                if (distSqr > p.DistanceToTravelSqr)
                    continue;

                var targetRadius = tInfo.Target.PositionComp.LocalVolume.Radius;
                if (targetRadius < minTargetRadius || targetRadius > maxTargetRadius && maxTargetRadius < 8192 || topTarget != null && tInfo.Target != topTarget) continue;
                if (water != null)
                {
                    if (new BoundingSphereD(ai.MyPlanet.PositionComp.WorldAABB.Center, water.MinRadius).Contains(new BoundingSphereD(targetPos, targetRadius)) == ContainmentType.Contains)
                        continue;
                }

                if (tInfo.IsGrid)
                {

                    if (!s.TrackGrids || !overRides.Grids || !focusTarget && tInfo.FatCount < 2 || !aConst.CheckFutureIntersection && Obstruction(ref tInfo, ref targetPos, p) || (!overRides.LargeGrid && tInfo.LargeGrid) || (!overRides.SmallGrid && !tInfo.LargeGrid)) continue;

                    if (!AcquireBlock(w, target, tInfo, ref waterSphere, ref info.Random, p, !focusTarget)) continue;
                    acquired = true;
                    break;
                }

                if (!aConst.CheckFutureIntersection && Obstruction(ref tInfo, ref targetPos, p))
                    continue;

                var topEntId = tInfo.Target.GetTopMostParent().EntityId;
                target.Set(tInfo.Target, targetPos, 0, 0, topEntId);
                acquired = true;
                break;
            }
            if (!acquired && !previousEntity) target.Reset(Session.I.Tick, Target.States.NoTargetsSeen);
            return acquired;
        }

        internal static bool ReAcquireProjectile(Projectile p)
        {
            var info = p.Info;
            if (info.CompSceneVersion != info.Weapon.Comp.SceneVersion)
                return false;

            var w = info.Weapon;
            var comp = w.Comp;

            var s = w.System;
            var target = info.Target;
            info.Storage.ChaseAge = (int) info.RelativeAge;
            var ai = info.Ai;
            var overRides = comp.Data.Repo.Values.Set.Overrides;
            var session = Session.I;
            var physics = Session.I.Physics;
            var weaponPos = p.Position;
            var aConst = p.Info.AmmoDef.Const;
            var collection = ai.GetProCache(w, overRides.SupportingPD);
            var numOfTargets = collection.Count;
            var lockedOnly = s.Values.Targeting.LockedSmartOnly;
            var smartOnly = s.Values.Targeting.IgnoreDumbProjectiles;
            var found = false;

            var minRadius = overRides.MinSize * 0.5f;
            var maxRadius = overRides.MaxSize * 0.5f;
            var minTargetRadius = minRadius > 0 ? minRadius : s.MinTargetRadius;
            var maxTargetRadius = maxRadius < s.MaxTargetRadius ? maxRadius : s.MaxTargetRadius;

            if (s.ClosestFirst)
            {
                int length = collection.Count;
                for (int h = length / 2; h > 0; h /= 2)
                {
                    for (int i = h; i < length; i += 1)
                    {
                        var tempValue = collection[i];
                        double temp;
                        Vector3D.DistanceSquared(ref collection[i].Position, ref weaponPos, out temp);

                        int j;
                        for (j = i; j >= h && Vector3D.DistanceSquared(collection[j - h].Position, weaponPos) > temp; j -= h)
                            collection[j] = collection[j - h];

                        collection[j] = tempValue;
                    }
                }
            }

            var numToRandomize = s.ClosestFirst ? s.Values.Targeting.TopTargets : numOfTargets;
            if (session.TargetDeck.Length < numOfTargets)
            {
                session.TargetDeck = new int[numOfTargets];
            }

            for (int i = 0; i < numOfTargets; i++)
            {
                var j = i < numToRandomize ? info.Random.Range(0, i + 1) : i;
                session.TargetDeck[i] = session.TargetDeck[j];
                session.TargetDeck[j] = 0 + i;
            }

            var deck = session.TargetDeck;
            for (int x = 0; x < numOfTargets; x++)
            {
                var card = deck[x];
                var lp = collection[card];
                if (lp.State != Projectile.ProjectileState.Alive || lp.MaxSpeed > s.MaxTargetSpeed || lp.MaxSpeed <= 0) 
                    continue;
                
                var lpaConst = lp.Info.AmmoDef.Const;

                if (smartOnly && !(lpaConst.IsDrone || lpaConst.IsSmart) || lockedOnly && !(lpaConst.IsDrone || lpaConst.IsSmart))
                    continue;

                var targetRadius = lpaConst.CollisionSize;
                if (targetRadius < minTargetRadius || targetRadius > maxTargetRadius && maxTargetRadius < 8192) continue;

                var needsCast = false;

                if (!aConst.CheckFutureIntersection)
                {
                    for (int i = 0; i < ai.Obstructions.Count; i++)
                    {
                        var ent = ai.Obstructions[i].Target;
                        if (ent == null)
                        {
                            Log.Line($"ReAcquireProjectile had null obstruction entity");
                            continue;
                        }
                        if (ent is MyPlanet)
                            continue;

                        var obsSphere = ent.PositionComp.WorldVolume;

                        var dir = lp.Position - weaponPos;
                        var ray = new RayD(ref weaponPos, ref dir);

                        if (ray.Intersects(obsSphere) != null)
                        {
                            var transform = ent.PositionComp.WorldMatrixRef;
                            var box = ent.PositionComp.LocalAABB;
                            var obb = new MyOrientedBoundingBoxD(box, transform);
                            if (obb.Intersects(ref ray) != null)
                            {
                                needsCast = true;
                                break;
                            }
                        }
                    }
                }

                if (needsCast)
                {
                    IHitInfo hitInfo;

                    var oneHalfKmSqr = 2250000;
                    var lowFiVoxels = Vector3D.DistanceSquared(lp.Position, weaponPos) > oneHalfKmSqr && (ai.PlanetSurfaceInRange || ai.ClosestVoxelSqr <= oneHalfKmSqr);
                    var filter = w.System.NoVoxelLosCheck ? CollisionLayers.NoVoxelCollisionLayer : lowFiVoxels ? CollisionLayers.DefaultCollisionLayer : CollisionLayers.VoxelLod1CollisionLayer;
                    physics.CastRay(weaponPos, lp.Position, out hitInfo, filter);

                    if (hitInfo?.HitEntity == null)
                    {
                        target.Set(lp, lp.Position,  0, 0, long.MaxValue);
                        p.TargetPosition = lp.Position;
                        lp.Seekers.Add(p);
                        found = true;
                        break;
                    }
                }
                else
                {
                    Vector3D? hitInfo;
                    if (!aConst.CheckFutureIntersection && ai.AiType == AiTypes.Grid && GridIntersection.BresenhamGridIntersection(ai.GridEntity, ref weaponPos, ref lp.Position, out hitInfo, comp.CoreEntity, ai))
                        continue;

                    target.Set(lp, lp.Position, 0, 0, long.MaxValue);
                    p.TargetPosition = lp.Position;
                    lp.Seekers.Add(p);
                    found = true;
                    break;
                }
            }

            return found;
        }
        private static bool AcquireBlock(Weapon w, Target target, TargetInfo info, ref BoundingSphereD waterSphere, ref XorShiftRandomStruct xRnd, Projectile p, bool checkPower = true)
        {
            var system = w.System;
            if (system.TargetSubSystems)
            {
                var overRides = w.RotorTurretTracking ? w.Comp.MasterOverrides : w.Comp.Data.Repo.Values.Set.Overrides;
                var subSystems = system.Values.Targeting.SubSystems;
                var focusSubSystem = overRides.FocusSubSystem || overRides.FocusSubSystem;

                var targetLinVel = info.Target.Physics?.LinearVelocity ?? Vector3D.Zero;
                var targetAccel = (int)system.Values.HardPoint.AimLeadingPrediction > 1 ? info.Target.Physics?.LinearAcceleration ?? Vector3D.Zero : Vector3.Zero;
                var subSystem = overRides.SubSystem;

                foreach (var blockType in subSystems)
                {
                    var bt = focusSubSystem ? subSystem : blockType;

                    ConcurrentDictionary<BlockTypes, ConcurrentCachingList<MyCubeBlock>> blockTypeMap;
                    Session.I.GridToBlockTypeMap.TryGetValue((MyCubeGrid)info.Target, out blockTypeMap);
                    if (bt != Any && blockTypeMap != null && blockTypeMap[bt].Count > 0)
                    {
                        var subSystemList = blockTypeMap[bt];
                        if (system.ClosestFirst)
                        {
                            if (w.Top5.Count > 0 && (bt != w.LastTop5BlockType || w.Top5[0].CubeGrid != subSystemList[0].CubeGrid))
                                w.Top5.Clear();

                            w.LastTop5BlockType = bt;
                            if (GetClosestHitableBlockOfType(w, subSystemList, target, info, targetLinVel, targetAccel, ref waterSphere, p, checkPower))
                                return true;
                        }
                        else if (FindRandomBlock(w, target, info, subSystemList, ref waterSphere, ref xRnd, p, checkPower)) return true;
                    }

                    if (focusSubSystem) break;
                }

                if (system.OnlySubSystems || focusSubSystem && subSystem != Any) return false;
            }
            TopMap topMap;
            return Session.I.TopEntityToInfoMap.TryGetValue((MyCubeGrid)info.Target, out topMap) && topMap.MyCubeBocks != null && FindRandomBlock(w, target, info, topMap.MyCubeBocks, ref waterSphere, ref xRnd, p, checkPower);
        }

        private static bool FindRandomBlock(Weapon w, Target target, TargetInfo info, ConcurrentCachingList<MyCubeBlock> subSystemList, ref BoundingSphereD waterSphere, ref XorShiftRandomStruct xRnd, Projectile p, bool checkPower = true)
        {
            var totalBlocks = subSystemList.Count;
            var system = w.System;
            var ai = w.Comp.MasterAi;
            var s = Session.I;
            AmmoConstants aConst;
            Vector3D weaponPos;
            if (p != null)
            {
                aConst = p.Info.AmmoDef.Const;
                weaponPos = p.Position;
            }
            else
            {
                var barrelPos = w.BarrelOrigin;
                var targetNormDir = Vector3D.Normalize(info.Target.PositionComp.WorldAABB.Center - barrelPos);
                weaponPos = barrelPos + (targetNormDir * w.MuzzleDistToBarrelCenter);
                aConst = w.ActiveAmmoDef.AmmoDef.Const;
            }

            var topEnt = info.Target.GetTopMostParent();

            var entSphere = topEnt.PositionComp.WorldVolume;
            var distToEnt = MyUtils.GetSmallestDistanceToSphere(ref weaponPos, ref entSphere);
            var weaponCheck = p == null && (!w.ActiveAmmoDef.AmmoDef.Const.SkipAimChecks || w.RotorTurretTracking);
            var topBlocks = system.Values.Targeting.TopBlocks;
            var lastBlocks = topBlocks > 10 && distToEnt < 1000 ? topBlocks : 10;
            var isPriroity = false;

            if (lastBlocks < 250)
            {
                TargetInfo priorityInfo;
                MyEntity fTarget;
                if (ai.Construct.Data.Repo.FocusData.Target > 0 && MyEntities.TryGetEntityById(ai.Construct.Data.Repo.FocusData.Target, out fTarget) && ai.Targets.TryGetValue(fTarget, out priorityInfo) && priorityInfo.Target?.GetTopMostParent() == topEnt)
                {
                    isPriroity = true;
                    lastBlocks = totalBlocks < 250 ? totalBlocks : 250;
                }

            }
            if (totalBlocks < lastBlocks) lastBlocks = totalBlocks;

            int checkSize;
            if (w.System.CycleBlocks <= 0)
                checkSize = totalBlocks;
            else if (w.System.CycleBlocks > totalBlocks)
                checkSize = w.System.CycleBlocks - totalBlocks;
            else
                checkSize = w.System.CycleBlocks;

            var chunk = totalBlocks > 0 ? checkSize * w.AcquireAttempts % totalBlocks : 0;

            if (chunk + checkSize >= totalBlocks)
                checkSize = totalBlocks - chunk;

            var deck = GetDeck(ref s.BlockDeck, chunk, checkSize, topBlocks, ref xRnd);

            var physics = s.Physics;
            var iGrid = topEnt as IMyCubeGrid;
            var gridPhysics = iGrid?.Physics;
            Vector3D targetLinVel = gridPhysics?.LinearVelocity ?? Vector3D.Zero;
            Vector3D targetAccel = (int)system.Values.HardPoint.AimLeadingPrediction > 1 ? info.Target.Physics?.LinearAcceleration ?? Vector3D.Zero : Vector3.Zero;
            var foundBlock = false;
            var blocksChecked = 0;
            var blocksSighted = 0;
            var hitTmpList = s.HitInfoTmpList;

            var checkLimit = ai.PlanetSurfaceInRange ? 128 : 512;

            for (int i = 0; i < checkSize; i++)
            {
                if (weaponCheck && (blocksChecked > lastBlocks || isPriroity && (blocksSighted > 100 || blocksChecked > 50 && s.RandomRayCasts > checkLimit || blocksChecked > 25 && s.RandomRayCasts > checkLimit * 2)))
                    break;

                var card = deck[i];
                var block = subSystemList[card];

                if (block.MarkedForClose || checkPower && !(block is IMyWarhead) && !block.IsWorking) continue;

                s.BlockChecks++;
                var blockPos = block.PositionComp.WorldAABB.Center;

                double rayDist;
                if (weaponCheck)
                {
                    double distSqr;
                    Vector3D.DistanceSquared(ref blockPos, ref weaponPos, out distSqr);
                    if (distSqr > w.MaxTargetDistanceSqr || distSqr < w.MinTargetDistanceSqr)
                        continue;

                    blocksChecked++;
                    Session.I.CanShoot++;

                    Vector3D predictedPos;
                    if (!Weapon.CanShootTarget(w, ref blockPos, targetLinVel, targetAccel, out predictedPos, w.RotorTurretTracking, null, MathFuncs.DebugCaller.CanShootTarget6)) continue;

                    if (s.WaterApiLoaded && waterSphere.Radius > 2 && waterSphere.Contains(predictedPos) != ContainmentType.Disjoint)
                        continue;

                    blocksSighted++;
                    s.RandomRayCasts++;
                    w.AcquiredBlock = true;

                    var targetDirNorm = Vector3D.Normalize(blockPos - w.BarrelOrigin);
                    var testPos = w.BarrelOrigin + (targetDirNorm * w.MuzzleDistToBarrelCenter);
                    var targetDist = Vector3D.Distance(testPos, blockPos);

                    var fakeCheck = w.System.NoVoxelLosCheck;

                    bool acquire = false;
                    double closest = double.MaxValue;
                    if (!aConst.SkipRayChecks)
                    {
                        if (!fakeCheck)
                        {
                            hitTmpList.Clear();
                            var oneHalfKmSqr = 2250000;
                            var lowFiVoxels = distSqr > oneHalfKmSqr && (ai.PlanetSurfaceInRange || ai.ClosestVoxelSqr <= oneHalfKmSqr);
                            var filter = lowFiVoxels ? CollisionLayers.DefaultCollisionLayer : CollisionLayers.VoxelLod1CollisionLayer;

                            physics.CastRay(testPos, blockPos, hitTmpList, filter);
                            for (int j = 0; j < hitTmpList.Count; j++)
                            {
                                var hitInfo = hitTmpList[j];

                                var entity = hitInfo.HitEntity as MyEntity;
                                var hitGrid = entity as MyCubeGrid;
                                var voxel = entity as MyVoxelBase;
                                var character = entity as IMyCharacter;
                                var dist = hitInfo.Fraction * targetDist;

                                if (character == null && hitGrid == null && voxel == null || dist >= closest || hitGrid != null && (hitGrid.MarkedForClose || hitGrid.Physics == null || hitGrid.IsPreview))
                                    continue;

                                TargetInfo otherInfo;
                                var knownTarget = ai.Targets.TryGetValue(entity, out otherInfo) && (otherInfo.EntInfo.Relationship == MyRelationsBetweenPlayerAndBlock.Enemies || otherInfo.EntInfo.Relationship == MyRelationsBetweenPlayerAndBlock.Neutral || otherInfo.EntInfo.Relationship == MyRelationsBetweenPlayerAndBlock.NoOwnership);

                                var enemyCharacter = character != null && knownTarget;

                                if (character != null && !enemyCharacter)
                                {
                                    if (dist < closest)
                                    {
                                        closest = dist;
                                        acquire = false;
                                    }
                                }

                                if (voxel != null)
                                {
                                    if (dist < closest)
                                    {
                                        closest = dist;
                                        acquire = false;
                                    }
                                }
                                else if (hitGrid != null)
                                {
                                    var bigOwners = hitGrid.BigOwners;
                                    var noOwner = bigOwners.Count == 0;
                                    var validTarget = noOwner || knownTarget;

                                    if (dist < closest)
                                    {
                                        closest = dist;
                                        acquire = validTarget;
                                    }
                                }
                            }
                        }
                        else
                        {
                            IHitInfo iHitInfo;
                            if (ai.AiType == AiTypes.Grid && physics.CastRay(testPos, testPos + (targetDirNorm * (ai.TopEntityVolume.Radius * 2)), out iHitInfo, CollisionLayers.NoVoxelCollisionLayer))
                            {
                                var rayGrid = iHitInfo.HitEntity?.GetTopMostParent() as MyCubeGrid;
                                if (rayGrid != null && rayGrid.IsSameConstructAs(ai.GridEntity))
                                    continue;
                            }
                            var checkLine = new LineD(testPos, testPos + (targetDirNorm * w.MaxTargetDistance), w.MaxTargetDistance);

                            s.OverlapResultTmp.Clear();
                            var queryType = ai.StaticEntityInRange ? MyEntityQueryType.Both : MyEntityQueryType.Dynamic;
                            MyGamePruningStructure.GetTopmostEntitiesOverlappingRay(ref checkLine, s.OverlapResultTmp, queryType);
                            for (int j = 0; j < s.OverlapResultTmp.Count; j++)
                            {
                                var entity = s.OverlapResultTmp[j].Element;
                                var character = entity as IMyCharacter;
                                var hitGrid = entity as MyCubeGrid;

                                if (character == null && hitGrid == null || hitGrid != null && (hitGrid.MarkedForClose || hitGrid.Physics == null || hitGrid.IsPreview || ai.AiType == AiTypes.Grid && hitGrid.IsSameConstructAs(ai.GridEntity)))
                                    continue;

                                TargetInfo otherInfo;
                                var knownTarget = ai.Targets.TryGetValue(entity, out otherInfo) && (otherInfo.EntInfo.Relationship == MyRelationsBetweenPlayerAndBlock.Enemies || otherInfo.EntInfo.Relationship == MyRelationsBetweenPlayerAndBlock.Neutral || otherInfo.EntInfo.Relationship == MyRelationsBetweenPlayerAndBlock.NoOwnership);

                                var enemyCharacter = character != null && knownTarget;

                                double? hitDist;
                                if (character != null && !enemyCharacter)
                                {
                                    var obb = new MyOrientedBoundingBoxD(character.Model.BoundingBox, character.PositionComp.WorldMatrixRef);
                                    hitDist = obb.Intersects(ref checkLine);
                                    if (hitDist < closest)
                                    {
                                        closest = hitDist.Value;
                                        acquire = false;
                                    }
                                }

                                if (hitGrid != null)
                                {
                                    var bigOwners = hitGrid.BigOwners;
                                    var noOwner = bigOwners.Count == 0;
                                    var validTarget = noOwner || knownTarget;

                                    var hit = hitGrid.RayCastBlocks(checkLine.From, checkLine.To);

                                    MyCube cube;
                                    if (hit.HasValue && hitGrid.TryGetCube(hit.Value, out cube))
                                    {
                                        var slim = (IMySlimBlock)cube.CubeBlock;

                                        MyOrientedBoundingBoxD obb;
                                        var fat = slim.FatBlock;
                                        if (fat != null)
                                            obb = new MyOrientedBoundingBoxD(fat.Model.BoundingBox,
                                                fat.PositionComp.WorldMatrixRef);
                                        else
                                        {
                                            Vector3 halfExt;
                                            slim.ComputeScaledHalfExtents(out halfExt);
                                            var blockBox = new BoundingBoxD(-halfExt, halfExt);
                                            var gridMatrix = hitGrid.PositionComp.WorldMatrixRef;
                                            gridMatrix.Translation = hitGrid.GridIntegerToWorld(slim.Position);
                                            obb = new MyOrientedBoundingBoxD(blockBox, gridMatrix);
                                        }

                                        hitDist = obb.Intersects(ref checkLine);

                                        if (hitDist < closest)
                                        {
                                            closest = hitDist.Value;
                                            acquire = validTarget;
                                        }
                                    }
                                }
                            }

                            if (acquire)
                            {
                                var hitPos = checkLine.From + (checkLine.Direction * closest);
                                s.CustomHitInfo.Position = hitPos;
                                s.CustomHitInfo.HitEntity = block;
                                s.CustomHitInfo.Fraction = (float)(closest / targetDist);
                            }
                        }
                    }
                    else
                    {
                        acquire = true;
                        s.CustomHitInfo.Position = blockPos;
                        s.CustomHitInfo.HitEntity = block;
                        s.CustomHitInfo.Fraction = 1;
                    }


                    if (!acquire)
                        continue;

                    Vector3D.Distance(ref weaponPos, ref blockPos, out rayDist);
                    var shortDist = rayDist * (1 - s.CustomHitInfo.Fraction);
                    var origDist = rayDist * s.CustomHitInfo.Fraction;
                    target.Set(block, s.CustomHitInfo.Position, shortDist, origDist, block.GetTopMostParent().EntityId);
                    foundBlock = true;
                    break;
                }

                Vector3D.Distance(ref weaponPos, ref blockPos, out rayDist);
                target.Set(block, block.PositionComp.WorldAABB.Center, rayDist, rayDist, block.GetTopMostParent().EntityId);
                foundBlock = true;
                break;
            }
            return foundBlock;
        }


        internal static bool GetClosestHitableBlockOfType(Weapon w, ConcurrentCachingList<MyCubeBlock> cubes, Target target, TargetInfo info, Vector3D targetLinVel, Vector3D targetAccel, ref BoundingSphereD waterSphere, Projectile p, bool checkPower = true)
        {
            var minValue = double.MaxValue;
            var minValue0 = double.MaxValue;
            var minValue1 = double.MaxValue;
            var minValue2 = double.MaxValue;
            var minValue3 = double.MaxValue;

            MyCubeBlock newEntity = null;
            MyCubeBlock newEntity0 = null;
            MyCubeBlock newEntity1 = null;
            MyCubeBlock newEntity2 = null;
            MyCubeBlock newEntity3 = null;
            AmmoConstants aConst;
            Vector3D weaponPos;
            if (p != null)
            {
                weaponPos = p.Position;
                aConst = p.Info.AmmoDef.Const;
            }
            else
            {
                var barrelPos = w.BarrelOrigin;
                var targetNormDir = Vector3D.Normalize(info.Target.PositionComp.WorldAABB.Center - barrelPos);
                weaponPos = barrelPos + (targetNormDir * w.MuzzleDistToBarrelCenter);
                aConst = w.ActiveAmmoDef.AmmoDef.Const;
            }

            var ai = w.Comp.MasterAi;
            var s = Session.I;
            var bestCubePos = Vector3D.Zero;
            var top5Count = w.Top5.Count;
            var top5 = w.Top5;
            var physics = Session.I.Physics;
            var hitTmpList = Session.I.HitInfoTmpList;
            var weaponCheck = p == null && (!aConst.SkipAimChecks || w.RotorTurretTracking || aConst.SkipRayChecks);
            IHitInfo iHitInfo = null;

            for (int i = 0; i < cubes.Count + top5Count; i++)
            {

                Session.I.BlockChecks++;
                var index = i < top5Count ? i : i - top5Count;
                var cube = i < top5Count ? top5[index] : cubes[index];

                var grid = cube.CubeGrid;
                if (grid == null || grid.MarkedForClose) continue;
                if (cube.MarkedForClose || cube == newEntity || cube == newEntity0 || cube == newEntity1 || cube == newEntity2 || cube == newEntity3 || checkPower && !(cube is IMyWarhead) && !cube.IsWorking)
                    continue;

                var cubePos = grid.GridIntegerToWorld(cube.Position);
                var range = cubePos - weaponPos;
                var test = (range.X * range.X) + (range.Y * range.Y) + (range.Z * range.Z);

                if (Session.I.WaterApiLoaded && waterSphere.Radius > 2 && waterSphere.Contains(cubePos) != ContainmentType.Disjoint)
                    continue;

                if (test < minValue3)
                {

                    IHitInfo hit = null;

                    var best = test < minValue;
                    var bestTest = false;
                    if (best)
                    {

                        if (weaponCheck)
                        {
                            Session.I.CanShoot++;
                            Vector3D predictedPos;
                            if (Weapon.CanShootTarget(w, ref cubePos, targetLinVel, targetAccel, out predictedPos, false, null, MathFuncs.DebugCaller.CanShootTarget7))
                            {

                                Session.I.ClosestRayCasts++;
                                w.AcquiredBlock = true;
                                bool acquire = false;

                                if (!aConst.SkipRayChecks)
                                {
                                    var targetDirNorm = Vector3D.Normalize(cubePos - w.BarrelOrigin);
                                    var testPos = w.BarrelOrigin + (targetDirNorm * w.MuzzleDistToBarrelCenter);
                                    var targetDist = Vector3D.Distance(testPos, cubePos);

                                    hitTmpList.Clear();

                                    double closest = double.MaxValue;
                                    var oneHalfKmSqr = 2250000;
                                    var rayStart = w.BarrelOrigin + (targetDirNorm * w.MuzzleDistToBarrelCenter);
                                    var lowFiVoxels = Vector3D.DistanceSquared(rayStart, cubePos) > oneHalfKmSqr && (ai.PlanetSurfaceInRange || ai.ClosestVoxelSqr <= oneHalfKmSqr);
                                    var filter = lowFiVoxels ? CollisionLayers.DefaultCollisionLayer : CollisionLayers.VoxelLod1CollisionLayer;

                                    physics.CastRay(rayStart, cubePos, hitTmpList, filter);
                                    for (int j = 0; j < hitTmpList.Count; j++)
                                    {
                                        var hitInfo = hitTmpList[j];

                                        var entity = hitInfo.HitEntity as MyEntity;
                                        var hitGrid = entity as MyCubeGrid;
                                        var voxel = entity as MyVoxelBase;
                                        var character = entity as IMyCharacter;
                                        var dist = hitInfo.Fraction * targetDist;

                                        if (character == null && hitGrid == null && voxel == null || dist >= closest || hitGrid != null && (hitGrid.MarkedForClose || hitGrid.Physics == null || hitGrid.IsPreview))
                                            continue;

                                        TargetInfo otherInfo;
                                        var knownTarget = ai.Targets.TryGetValue(entity, out otherInfo) && (otherInfo.EntInfo.Relationship == MyRelationsBetweenPlayerAndBlock.Enemies || otherInfo.EntInfo.Relationship == MyRelationsBetweenPlayerAndBlock.Neutral || otherInfo.EntInfo.Relationship == MyRelationsBetweenPlayerAndBlock.NoOwnership);

                                        var enemyCharacter = character != null && knownTarget;

                                        if (character != null && !enemyCharacter)
                                        {
                                            if (dist < closest)
                                            {
                                                closest = dist;
                                                acquire = false;
                                            }
                                        }

                                        if (voxel != null)
                                        {
                                            if (dist < closest)
                                            {
                                                closest = dist;
                                                acquire = false;
                                            }
                                        }
                                        else if (hitGrid != null)
                                        {
                                            var bigOwners = hitGrid.BigOwners;
                                            var noOwner = bigOwners.Count == 0;
                                            var validTarget = noOwner || knownTarget;

                                            if (dist < closest)
                                            {
                                                closest = dist;
                                                acquire = validTarget;
                                            }
                                        }
                                    }

                                }

                                if (acquire || aConst.SkipRayChecks)
                                    bestTest = true;

                                if (!acquire)
                                    continue;
                            }
                        }
                        else bestTest = true;
                    }

                    if (best && bestTest)
                    {
                        minValue3 = minValue2;
                        newEntity3 = newEntity2;
                        minValue2 = minValue1;
                        newEntity2 = newEntity1;
                        minValue1 = minValue0;
                        newEntity1 = newEntity0;
                        minValue0 = minValue;
                        newEntity0 = newEntity;
                        minValue = test;

                        newEntity = cube;
                        bestCubePos = cubePos;
                        iHitInfo = hit;
                    }
                    else if (test < minValue0)
                    {
                        minValue3 = minValue2;
                        newEntity3 = newEntity2;
                        minValue2 = minValue1;
                        newEntity2 = newEntity1;
                        minValue1 = minValue0;
                        newEntity1 = newEntity0;
                        minValue0 = test;

                        newEntity0 = cube;
                    }
                    else if (test < minValue1)
                    {
                        minValue3 = minValue2;
                        newEntity3 = newEntity2;
                        minValue2 = minValue1;
                        newEntity2 = newEntity1;
                        minValue1 = test;

                        newEntity1 = cube;
                    }
                    else if (test < minValue2)
                    {
                        minValue3 = minValue2;
                        newEntity3 = newEntity2;
                        minValue2 = test;

                        newEntity2 = cube;
                    }
                    else
                    {
                        minValue3 = test;
                        newEntity3 = cube;
                    }
                }

            }
            top5.Clear();
            if (newEntity != null && iHitInfo != null)
            {

                double rayDist;
                Vector3D.Distance(ref weaponPos, ref bestCubePos, out rayDist);
                var shortDist = rayDist * (1 - iHitInfo.Fraction);
                var origDist = rayDist * iHitInfo.Fraction;
                target.Set(newEntity, iHitInfo.Position, shortDist, origDist, newEntity.GetTopMostParent().EntityId);
                top5.Add(newEntity);
            }
            else if (newEntity != null)
            {

                double rayDist;
                Vector3D.Distance(ref weaponPos, ref bestCubePos, out rayDist);
                var shortDist = rayDist;
                var origDist = rayDist;
                target.Set(newEntity, bestCubePos, shortDist, origDist, newEntity.GetTopMostParent().EntityId);
                top5.Add(newEntity);
            }
            else target.Reset(Session.I.Tick, Target.States.NoTargetsSeen, w == null);

            if (newEntity0 != null) top5.Add(newEntity0);
            if (newEntity1 != null) top5.Add(newEntity1);
            if (newEntity2 != null) top5.Add(newEntity2);
            if (newEntity3 != null) top5.Add(newEntity3);

            return top5.Count > 0;
        }
        private static bool Obstruction(ref TargetInfo info, ref Vector3D targetPos, Projectile p)
        {
            var ai = p.Info.Ai;
            var obstruction = false;
            var topEntity = p.Info.Weapon.Comp.TopEntity;
            for (int j = 0; j < ai.Obstructions.Count; j++)
            {
                var ent = ai.Obstructions[j].Target;
                if (ent == null || ent is MyPlanet)
                    continue;

                var voxel = ent as MyVoxelBase;
                var dir = (targetPos - p.Position);
                var entWorldVolume = ent.PositionComp.WorldVolume;
                if (voxel != null)
                {
                    if (!ai.PlanetSurfaceInRange && (entWorldVolume.Contains(p.Position) != ContainmentType.Disjoint || new RayD(ref p.Position, ref dir).Intersects(entWorldVolume) != null))
                    {
                        var dirNorm = Vector3D.Normalize(dir);
                        var targetDist = Vector3D.Distance(p.Position, targetPos);
                        var tRadius = info.Target.PositionComp.LocalVolume.Radius;
                        var testPos = p.Position + (dirNorm * (targetDist - tRadius));
                        var lineTest = new LineD(p.Position, testPos);
                        Vector3D? voxelHit;
                        using (voxel.Pin())
                            voxel.RootVoxel.GetIntersectionWithLine(ref lineTest, out voxelHit);

                        obstruction = voxelHit.HasValue;
                        if (obstruction)
                            break;
                    }
                }
                else
                {
                    if (new RayD(ref p.Position, ref dir).Intersects(entWorldVolume) != null)
                    {
                        var transform = ent.PositionComp.WorldMatrixRef;
                        var box = ent.PositionComp.LocalAABB;
                        var obb = new MyOrientedBoundingBoxD(box, transform);
                        var lineTest = new LineD(p.Position, targetPos);
                        if (obb.Intersects(ref lineTest) != null)
                        {
                            obstruction = true;
                            break;
                        }
                    }
                }
            }

            if (!obstruction)
            {
                var dir = (targetPos - p.Position);
                var ray = new RayD(ref p.Position, ref dir);
                foreach (var sub in ai.SubGridCache)
                {
                    var subDist = sub.PositionComp.WorldVolume.Intersects(ray);
                    if (subDist.HasValue)
                    {
                        var transform = topEntity.PositionComp.WorldMatrixRef;
                        var box = topEntity.PositionComp.LocalAABB;
                        var obb = new MyOrientedBoundingBoxD(box, transform);
                        if (obb.Intersects(ref ray) != null)
                            obstruction = sub.RayCastBlocks(p.Position, targetPos) != null;
                    }

                    if (obstruction) break;
                }

                if (!obstruction && ai.PlanetSurfaceInRange && ai.MyPlanet != null)
                {
                    double targetDist;
                    Vector3D.Distance(ref p.Position, ref targetPos, out targetDist);
                    var dirNorm = dir / targetDist;

                    var tRadius = info.Target.PositionComp.LocalVolume.Radius;
                    targetDist = targetDist > tRadius ? (targetDist - tRadius) : targetDist;

                    var targetEdgePos = targetPos + (-dirNorm * tRadius);

                    if (targetDist > 300)
                    {
                        var lineTest1 = new LineD(p.Position, p.Position + (dirNorm * 150), 150);
                        var lineTest2 = new LineD(targetEdgePos, targetEdgePos + (-dirNorm * 150), 150);
                        obstruction = VoxelIntersect.CheckSurfacePointsOnLine(ai.MyPlanet, ref lineTest1, 3);
                        if (!obstruction)
                            obstruction = VoxelIntersect.CheckSurfacePointsOnLine(ai.MyPlanet, ref lineTest2, 3);
                    }
                    else
                    {
                        var lineTest = new LineD(p.Position, targetEdgePos, targetDist);
                        obstruction = VoxelIntersect.CheckSurfacePointsOnLine(ai.MyPlanet, ref lineTest, 3);
                    }
                }
            }
            return obstruction;
        }

        internal static bool ValidScanEntity(Weapon w, MyDetectedEntityInfo info, MyEntity target, bool skipUnique = false)
        {
            Weapon.TargetOwner tOwner;
            if (!skipUnique && w.System.UniqueTargetPerWeapon && w.Comp.ActiveTargets.TryGetValue(target, out tOwner) && tOwner.Weapon != w)
                return false;

            var character = target as IMyCharacter;

            if (character != null)
            {
                switch (info.Relationship)
                {
                    case MyRelationsBetweenPlayerAndBlock.FactionShare:
                    case MyRelationsBetweenPlayerAndBlock.Friends:
                    case MyRelationsBetweenPlayerAndBlock.Owner:
                        if (!w.System.Threats.Contains((int) Threat.ScanFriendlyCharacter))
                            return false;
                        break;
                    case MyRelationsBetweenPlayerAndBlock.Neutral:
                        if (!w.System.Threats.Contains((int) Threat.ScanNeutralCharacter))
                            return false;
                        break;
                    case MyRelationsBetweenPlayerAndBlock.Enemies:
                        if (!w.System.Threats.Contains((int) Threat.ScanEnemyCharacter))
                            return false;
                        break;
                    default:
                        return false;
                }
            }

            var voxel = target as MyVoxelBase;
            if (voxel != null)
            {
                var planet = voxel as MyPlanet;
                if (planet != null && !w.System.Threats.Contains((int) Threat.ScanPlanet))
                    return false;
                if (!w.System.Threats.Contains((int) Threat.ScanRoid))
                    return false;
            }

            var grid = target as MyCubeGrid;
            if (grid != null)
            {
                switch (info.Relationship)
                {
                    case MyRelationsBetweenPlayerAndBlock.FactionShare:
                    case MyRelationsBetweenPlayerAndBlock.Friends:
                        if (!w.System.Threats.Contains((int) Threat.ScanFriendlyGrid))
                            return false;
                        break;
                    case MyRelationsBetweenPlayerAndBlock.Neutral:
                        if (!w.System.Threats.Contains((int) Threat.ScanNeutralGrid))
                            return false;
                        break;
                    case MyRelationsBetweenPlayerAndBlock.Enemies:
                        if (!w.System.Threats.Contains((int) Threat.ScanEnemyGrid))
                            return false;
                        break;
                    case MyRelationsBetweenPlayerAndBlock.NoOwnership:
                        if (!w.System.Threats.Contains((int) Threat.ScanUnOwnedGrid))
                            return false;
                        break;
                    case MyRelationsBetweenPlayerAndBlock.Owner:
                        if (!w.System.Threats.Contains((int) Threat.ScanOwnersGrid))
                            return false;
                        break;
                    default:
                        return false;
                }
            }

            return true;
        }

        internal static bool SwitchToDrone(Weapon w)
        {
            w.AimCone.ConeDir = w.MyPivotFwd;
            w.AimCone.ConeTip = w.BarrelOrigin + (w.MyPivotFwd * w.MuzzleDistToBarrelCenter);

            var comp = w.Comp;
            var overRides = comp.Data.Repo.Values.Set.Overrides;
            var attackNeutrals = overRides.Neutrals;
            var attackNoOwner = overRides.Unowned;
            var session = Session.I;
            var ai = comp.MasterAi;
            session.TargetRequests++;
            var ammoDef = w.ActiveAmmoDef.AmmoDef;
            var aConst = ammoDef.Const;
            var weaponPos = w.BarrelOrigin + (w.MyPivotFwd * w.MuzzleDistToBarrelCenter);
            var target = w.NewTarget;
            var s = w.System;
            var accelPrediction = (int)s.Values.HardPoint.AimLeadingPrediction > 1;
            var minRadius = overRides.MinSize * 0.5f;
            var maxRadius = overRides.MaxSize * 0.5f;
            var minTargetRadius = minRadius > 0 ? minRadius : s.MinTargetRadius;
            var maxTargetRadius = maxRadius < s.MaxTargetRadius ? maxRadius : s.MaxTargetRadius;

            var moveMode = overRides.MoveMode;
            var movingMode = moveMode == ProtoWeaponOverrides.MoveModes.Moving;
            var fireOnStation = moveMode == ProtoWeaponOverrides.MoveModes.Any || moveMode == ProtoWeaponOverrides.MoveModes.Moored;
            var stationOnly = moveMode == ProtoWeaponOverrides.MoveModes.Moored;
            BoundingSphereD waterSphere = new BoundingSphereD(Vector3D.Zero, 1f);
            WaterData water = null;
            if (session.WaterApiLoaded && !ammoDef.IgnoreWater && ai.InPlanetGravity && ai.MyPlanet != null && session.WaterMap.TryGetValue(ai.MyPlanet.EntityId, out water))
                waterSphere = new BoundingSphereD(ai.MyPlanet.PositionComp.WorldAABB.Center, water.MinRadius);
            var numOfTargets = ai.SortedTargets.Count;

            int checkSize;
            if (w.System.CycleTargets <= 0)
                checkSize = numOfTargets;
            else if (w.System.CycleTargets > numOfTargets)
                checkSize = w.System.CycleTargets - numOfTargets;
            else
                checkSize = w.System.CycleTargets;

            var chunk = numOfTargets > 0 ? checkSize * w.AcquireAttempts % numOfTargets : 0;

            if (chunk + checkSize >= numOfTargets)
                checkSize = numOfTargets - chunk;

            var deck = GetDeck(ref session.TargetDeck, chunk, checkSize, ai.DetectionInfo.DroneCount, ref w.TargetData.WeaponRandom.AcquireRandom);

            for (int i = 0; i < checkSize; i++)
            {
                var info = ai.SortedTargets[deck[i]];

                if (!info.Drone)
                    break;

                if (info.Target == null || info.Target.MarkedForClose || !attackNeutrals && info.EntInfo.Relationship == MyRelationsBetweenPlayerAndBlock.Neutral || !attackNoOwner && info.EntInfo.Relationship == MyRelationsBetweenPlayerAndBlock.NoOwnership) continue;

                if (movingMode && info.VelLenSqr < 1 || !fireOnStation && info.IsStatic || stationOnly && !info.IsStatic)
                    continue;

                var character = info.Target as IMyCharacter;
                var targetRadius = character != null ? info.TargetRadius * 5 : info.TargetRadius;
                if (targetRadius < minTargetRadius || info.TargetRadius > maxTargetRadius && maxTargetRadius < 8192) continue;

                var targetCenter = info.Target.PositionComp.WorldAABB.Center;
                var targetDistSqr = Vector3D.DistanceSquared(targetCenter, weaponPos);
                if (targetDistSqr > (w.MaxTargetDistance + info.TargetRadius) * (w.MaxTargetDistance + info.TargetRadius) || targetDistSqr < w.MinTargetDistanceSqr) continue;
                if (water != null)
                {
                    if (new BoundingSphereD(ai.MyPlanet.PositionComp.WorldAABB.Center, water.MinRadius).Contains(new BoundingSphereD(targetCenter, targetRadius)) == ContainmentType.Contains)
                        continue;
                }
                session.TargetChecks++;
                Vector3D targetLinVel = info.Target.Physics?.LinearVelocity ?? Vector3D.Zero;
                Vector3D targetAccel = accelPrediction ? info.Target.Physics?.LinearAcceleration ?? Vector3D.Zero : Vector3.Zero;

                if (info.IsGrid)
                {

                    if (!s.TrackGrids || !overRides.Grids || info.FatCount < 2 || (!overRides.LargeGrid && info.LargeGrid) || (!overRides.SmallGrid && !info.LargeGrid)) continue;
                    session.CanShoot++;
                    Vector3D newCenter;
                    if (!w.TurretController)
                    {

                        var validEstimate = true;
                        newCenter = w.System.Prediction != HardPointDef.Prediction.Off && (!aConst.IsBeamWeapon && aConst.DesiredProjectileSpeed * w.VelocityMult > 0) ? Weapon.TrajectoryEstimation(w, targetCenter, targetLinVel, targetAccel, weaponPos, out validEstimate, true) : targetCenter;
                        var targetSphere = info.Target.PositionComp.WorldVolume;
                        targetSphere.Center = newCenter;
                        if (!validEstimate || !aConst.SkipAimChecks && !MathFuncs.TargetSphereInCone(ref targetSphere, ref w.AimCone)) continue;
                    }
                    else if (!Weapon.CanShootTargetObb(w, info.Target, targetLinVel, targetAccel, out newCenter)) continue;

                    if (w.Comp.MasterAi.FriendlyShieldNear)
                    {
                        var targetDir = newCenter - weaponPos;
                        if (w.HitFriendlyShield(weaponPos, newCenter, targetDir))
                            continue;
                    }

                    if (!AcquireBlock(w, target, info, ref waterSphere, ref w.XorRnd, null, true)) continue;
                    target.TransferTo(w.Target, Session.I.Tick, true);

                    var validTarget = w.Target.TargetState == Target.TargetStates.IsEntity;

                    if (validTarget)
                    {

                        Session.I.NewThreat(w);

                        if (Session.I.MpActive && Session.I.IsServer)
                            w.Target.PushTargetToClient(w);
                    }

                    return validTarget;
                }
            }

            return false;
        }

    }
}
