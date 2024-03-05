using System;
using System.Collections.Generic;
using CoreSystems.Support;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using VRage.Utils;
using VRageMath;
using static CoreSystems.Support.HitEntity.Type;
using static CoreSystems.Support.WeaponDefinition.AmmoDef.EwarDef.EwarType;
using static CoreSystems.Support.DeferedVoxels;
using CollisionLayers = Sandbox.Engine.Physics.MyPhysics.CollisionLayers;
using Jakaria.API;
using static CoreSystems.Projectiles.Projectile;
using static CoreSystems.Support.VoxelIntersect;
using System.Runtime.CompilerServices;

namespace CoreSystems.Projectiles
{
    public partial class Projectiles
    {
        internal void InitialHitCheck(Projectile p, bool lineCheck)
        {
            var info = p.Info;
            var s = info.Storage;
            var target = info.Target;
            var ai = info.Ai;
            var w = info.Weapon;
            var coreEntity = w.Comp.CoreEntity;
            var topEntity = w.Comp.TopEntity;
            var aDef = info.AmmoDef;
            var aConst = info.AmmoDef.Const;
            var shieldByPass = aConst.ShieldDamageBypassMod > 0;
            var genericFields = info.EwarActive && (aConst.EwarType == Dot || aConst.EwarType == Push || aConst.EwarType == Pull || aConst.EwarType == Tractor);
            var fieldActive = aConst.EwarField && (!aConst.EwarFieldTrigger || info.ExpandingEwarField);
            p.Info.ShieldInLine = false;
            var isBeam = aConst.IsBeamWeapon;
            var offensiveEwar = (info.EwarActive && aConst.NonAntiSmartEwar);
            bool projetileInShield = false;
            var tick = Session.I.Tick;
            var entityCollection = p.MyEntityList;
            var collectionCount = lineCheck ? p.MySegmentList.Count : entityCollection.Count;

            var beamLen = p.Beam.Length;
            var beamFrom = p.Beam.From;
            var beamTo = p.Beam.To;
            var direction = p.Beam.Direction;
            var beamLenSqr = beamLen * beamLen;

            var ray = new RayD(ref beamFrom, ref direction);
            var firingCube = coreEntity as MyCubeBlock;
            var goCritical = aConst.IsCriticalReaction;
            var selfDamage = aConst.SelfDamage;

            var aToggleVoxel = false;
            var aAvoidSelf = false;
            var aAvoidTarget = false;
            var aPhaseSelf = false;
            if (s.ApproachInfo != null && s.ApproachInfo.Active)
            {
                var approach = aConst.Approaches[s.RequestedStage];
                aToggleVoxel = approach.ToggleIngoreVoxels;
                aAvoidSelf = approach.SelfAvoidance;
                aAvoidTarget = approach.TargetAvoidance;
                aPhaseSelf = approach.SelfPhasing;
            }

            var ignoreVoxels = aDef.IgnoreVoxels && !aToggleVoxel || aToggleVoxel && !aDef.IgnoreVoxels;
            var isGrid = ai.AiType == Ai.AiTypes.Grid;
            var closestFutureDistSqr = double.MaxValue;

            var voxelCheck = aConst.FakeVoxelHitTicks > 0 || aConst.FakeVoxelHitTicks == 0 && !ignoreVoxels || ai.PlanetSurfaceInRange && ai.ClosestPlanetSqr <= info.MaxTrajectory * info.MaxTrajectory; 
            WaterData water = null;
            if (Session.I.WaterApiLoaded && info.MyPlanet != null)
                Session.I.WaterMap.TryGetValue(info.MyPlanet.EntityId, out water);
            MyEntity closestFutureEnt = null;
            IMyTerminalBlock iShield = null;
            for (int i = 0; i < collectionCount; i++)
            {
                var ent = lineCheck ? p.MySegmentList[i].Element : entityCollection[i];

                var grid = ent as MyCubeGrid;
                var entIsSelf = grid != null && firingCube != null && (grid == firingCube.CubeGrid || firingCube.CubeGrid.IsSameConstructAs(grid));

                if (entIsSelf && aConst.IsSmart && (!info.Storage.SmartReady || aPhaseSelf) || ent.MarkedForClose || !ent.InScene || !selfDamage && ent == ai.MyShield || !isGrid && ent == topEntity) continue;

                var character = ent as IMyCharacter;
                if (info.EwarActive && character != null && !genericFields) continue;

                var entSphere = ent.PositionComp.WorldVolume;
                var checkShield = Session.I.ShieldApiLoaded && Session.I.ShieldHash == ent.DefinitionId?.SubtypeId;
                MyTuple<IMyTerminalBlock, MyTuple<bool, bool, float, float, float, int>, MyTuple<MatrixD, MatrixD>, MyTuple<bool, bool, float, float>>? shieldInfo = null;
                if (aConst.CheckFutureIntersection && !entIsSelf && (!checkShield || ai.FriendlyShieldNear && ai.NearByFriendlyShieldsCache.Contains(ent)))
                {
                    var distSqrToSphere = Vector3D.DistanceSquared(beamFrom, entSphere.Center);
                    if (distSqrToSphere > beamLenSqr)
                    {
                        if (distSqrToSphere < closestFutureDistSqr)
                        {
                            closestFutureDistSqr = distSqrToSphere;
                            closestFutureEnt = ent;
                        }
                    }
                }

                if (grid != null || character != null)
                {
                    var extBeam = new LineD(beamFrom - direction * (entSphere.Radius * 2), beamTo);
                    var transform = ent.PositionComp.WorldMatrixRef;
                    var box = ent.PositionComp.LocalAABB;
                    var obb = new MyOrientedBoundingBoxD(box, transform);
                    if (lineCheck && obb.Intersects(ref extBeam) == null || !lineCheck && !obb.Intersects(ref p.PruneSphere)) continue;
                }

                var safeZone = ent as MySafeZone;
                if (safeZone != null && safeZone.Enabled)
                {
                    var action = (Session.SafeZoneAction)safeZone.AllowedActions;
                    if ((action & Session.SafeZoneAction.Damage) == 0 || (action & Session.SafeZoneAction.Shooting) == 0)
                    {

                        bool intersects;
                        if (safeZone.Shape == MySafeZoneShape.Sphere)
                        {
                            var sphere = new BoundingSphereD(safeZone.PositionComp.WorldVolume.Center, safeZone.Radius);
                            var dist = ray.Intersects(sphere);
                            intersects = dist != null && dist <= beamLen;
                        }
                        else
                            intersects = new MyOrientedBoundingBoxD(safeZone.PositionComp.LocalAABB, safeZone.PositionComp.WorldMatrixRef).Intersects(ref p.Beam) != null;

                        if (intersects)
                        {

                            p.State = ProjectileState.Destroy;
                            p.EndState = EndStates.EarlyEnd;

                            if (p.EnableAv)
                                info.AvShot.ForceHitParticle = true;
                            break;
                        }
                    }
                }

                HitEntity hitEntity = null;
                var poolId = Environment.CurrentManagedThreadId;
                var pool = HitEntityArrayPool[poolId];

                if (checkShield && ent.Render.Visible && !info.EwarActive || info.EwarActive && (aConst.EwarType == Dot || aConst.EwarType == Emp))
                {
                    if (shieldInfo == null)
                        shieldInfo = Session.I.SApi.MatchEntToShieldFastDetails(ent, true);

                    if (shieldInfo != null && (firingCube == null || (selfDamage || !firingCube.CubeGrid.IsSameConstructAs(shieldInfo.Value.Item1.CubeGrid)) && !goCritical))
                    {
                        if (shieldInfo.Value.Item2.Item1)
                        {
                            var shrapnelSpawn = p.Info.IsFragment && p.Info.PrevRelativeAge <= -1;
                            if (Vector3D.Transform(!shrapnelSpawn ? info.Origin : coreEntity.PositionComp.WorldMatrixRef.Translation, shieldInfo.Value.Item3.Item1).LengthSquared() > 1)
                            {

                                var dist = MathFuncs.IntersectEllipsoid(shieldInfo.Value.Item3.Item1, shieldInfo.Value.Item3.Item2, new RayD(beamFrom, direction));
                                if (target.TargetState == Target.TargetStates.IsProjectile && Vector3D.Transform(((Projectile)target.TargetObject).Position, shieldInfo.Value.Item3.Item1).LengthSquared() <= 1)
                                    projetileInShield = true;

                                var shieldIntersect = dist != null && (dist.Value < beamLen || info.EwarActive);
                                info.ShieldKeepBypass = shieldIntersect;
                                if (shieldIntersect && !info.ShieldBypassed)
                                {
                                    hitEntity = pool.Count > 0 ? pool.Pop() : new HitEntity();
                                    hitEntity.Pool = pool;

                                    hitEntity.EventType = Shield;
                                    var hitPos = beamFrom + (direction * dist.Value);
                                    hitEntity.HitPos = beamFrom + (direction * dist.Value);
                                    hitEntity.HitDist = dist;

                                    var weakendShield = shieldInfo.Value.Item4.Item2 || shieldInfo.Value.Item4.Item3 < shieldInfo.Value.Item4.Item4;

                                    if (weakendShield || shieldInfo.Value.Item2.Item2)
                                    {
                                        var faceInfo = Session.I.SApi.TAPI_GetFaceInfoAndPenChance(shieldInfo.Value.Item1, hitPos);
                                        var modifiedBypassMod = ((1 - aConst.ShieldDamageBypassMod) + faceInfo.Item5);
                                        var validBypassRange = modifiedBypassMod >= 0 && modifiedBypassMod <= 1 || faceInfo.Item1;
                                        var notSupressed = validBypassRange && modifiedBypassMod < 1 && faceInfo.Item5 < 1;
                                        var bypassAmmo = shieldByPass && notSupressed;
                                        var bypass = bypassAmmo || faceInfo.Item1;
                                        var hasPenChance = faceInfo.Item6 > 0;

                                        info.ShieldResistMod = faceInfo.Item4;
                                        if (bypass)
                                        {
                                            info.ShieldBypassed = true;
                                            modifiedBypassMod = bypassAmmo && faceInfo.Item1 ? 0f : modifiedBypassMod;
                                            info.ShieldBypassMod = bypassAmmo ? modifiedBypassMod : 0.15f;
                                        }
                                        else if (hasPenChance)
                                        {
                                            var normalized = info.ShieldProc.NextDouble();
                                            var threshold = (faceInfo.Item6 - aConst.ShieldAntiPenMod);

                                            if (normalized <= threshold)
                                            {
                                                //if (((MyCubeGrid)shieldInfo.Value.Item1.CubeGrid).DebugName.Contains("test"))
                                                //    Log.Line($"bypass: RNG:{normalized} <= penChance:{threshold} - ammo:{info.AmmoDef.AmmoRound} - sPerc:{shieldInfo.Value.Item2.Item5} - heat:{shieldInfo.Value.Item2.Item6} - threshold:{shieldInfo.Value.Item2.Item5 / (1 + (shieldInfo.Value.Item2.Item6 * 0.01))}");

                                                info.ShieldBypassed = true;
                                                info.ShieldBypassMod = 0.15f;
                                            }
                                            else
                                            {
                                                //if (((MyCubeGrid)shieldInfo.Value.Item1.CubeGrid).DebugName.Contains("test"))
                                                //    Log.Line($"resist: RNG:{normalized} <= penChance:{threshold} - ammo:{info.AmmoDef.AmmoRound} - sPerc:{shieldInfo.Value.Item2.Item5} - heat:{shieldInfo.Value.Item2.Item6} - threshold:{shieldInfo.Value.Item2.Item5 / (1 + (shieldInfo.Value.Item2.Item6 * 0.01))}");
                                                
                                                p.Info.ShieldBypassMod = 1f;
                                            }

                                        }
                                        else p.Info.ShieldBypassMod = 1f;
                                    }
                                    else if (shieldByPass)
                                    {
                                        info.ShieldBypassed = true;
                                        info.ShieldResistMod = 1f;
                                        info.ShieldBypassMod = aConst.ShieldDamageBypassMod;
                                    }
                                }
                                else continue;
                            }
                        }
                        else
                            iShield = shieldInfo.Value.Item1;
                    }
                }

                var voxel = ent as MyVoxelBase;
                var destroyable = ent as IMyDestroyableObject;

                if (voxel != null && voxel == voxel?.RootVoxel && !ignoreVoxels)
                {
                    VoxelIntersectBranch voxelState = VoxelIntersectBranch.None;

                    if ((ent == info.MyPlanet && !(voxelCheck || aConst.DynamicGuidance)) || !voxelCheck && isBeam)
                        continue;

                    Vector3D? voxelHit = null;
                    if (tick - info.VoxelCache.HitRefreshed < 60)
                    {
                        var hitSphere = info.VoxelCache.HitSphere;
                        var cacheDist = ray.Intersects(hitSphere);

                        if (cacheDist <= beamLen)
                        {
                            var sphereRadius = hitSphere.Radius;
                            var sphereRadiusSqr = sphereRadius * sphereRadius;

                            var overPenDist = beamLen - cacheDist.Value;
                            var proposedDist = overPenDist >= sphereRadius ? sphereRadius : overPenDist;
                            var testPos1 = beamFrom + (direction * (cacheDist.Value + proposedDist));
                            var testPos2 = beamFrom + (direction * (cacheDist.Value + (proposedDist * 0.5d)));

                            var testPos2DistSqr = Vector3D.DistanceSquared(hitSphere.Center, testPos2);
                            var testPos1DistSqr = Vector3D.DistanceSquared(hitSphere.Center, testPos1);
                            var hitPos = testPos2DistSqr < sphereRadiusSqr && testPos2DistSqr < testPos1DistSqr ? testPos2 : testPos1DistSqr < sphereRadiusSqr ? testPos1 : p.Beam.From + (p.Beam.Direction * cacheDist.Value);

                            voxelHit = hitPos;
                            voxelState = VoxelIntersectBranch.PseudoHit1;
                        }
                        else if (cacheDist.HasValue)
                            info.VoxelCache.MissSphere.Center = beamTo;
                    }

                    if (voxelState != VoxelIntersectBranch.PseudoHit1)
                    {

                        if (voxel == info.MyPlanet && info.VoxelCache.MissSphere.Contains(beamTo) == ContainmentType.Disjoint)
                        {

                            if (voxelCheck)
                            {
                                if (water != null && info.FirstWaterHitTick == 0)
                                {
                                    var waterOuterSphere = new BoundingSphereD(info.MyPlanet.PositionComp.WorldAABB.Center, water.MaxRadius);
                                    if (ray.Intersects(waterOuterSphere).HasValue || waterOuterSphere.Contains(beamFrom) == ContainmentType.Contains || waterOuterSphere.Contains(beamTo) == ContainmentType.Contains)
                                    {
                                        if (WaterModAPI.LineIntersectsWater(p.Beam, water.Planet) != 0)
                                        {
                                            voxelHit = WaterModAPI.GetClosestSurfacePoint(beamTo, water.Planet);
                                            voxelState = VoxelIntersectBranch.PseudoHit2;
                                            info.FirstWaterHitTick = tick;
                                        }
                                    }
                                }

                                if (voxelState != VoxelIntersectBranch.PseudoHit2)
                                {
                                    var surfacePos = info.MyPlanet.GetClosestSurfacePointGlobal(ref p.Position);
                                    var planetCenter = info.MyPlanet.PositionComp.WorldAABB.Center;
                                    var prevDistanceToSurface = p.DistanceToSurfaceSqr;
                                    Vector3D.DistanceSquared(ref surfacePos, ref p.Position, out p.DistanceToSurfaceSqr);

                                    double surfaceToCenter;
                                    Vector3D.DistanceSquared(ref surfacePos, ref planetCenter, out surfaceToCenter);
                                    double posToCenter;
                                    Vector3D.DistanceSquared(ref p.Position, ref planetCenter, out posToCenter);
                                    double startPosToCenter;
                                    Vector3D.DistanceSquared(ref info.Origin, ref planetCenter, out startPosToCenter);

                                    var distToSurfaceLessThanProLengthSqr = p.DistanceToSurfaceSqr <= beamLenSqr;
                                    var pastSurfaceDistMoreThanToTravel = prevDistanceToSurface > p.DistanceToTravelSqr;

                                    var surfacePosAboveEndpoint = surfaceToCenter > posToCenter;
                                    var posMovingCloserToCenter = posToCenter > startPosToCenter;

                                    var isThisRight = posMovingCloserToCenter && pastSurfaceDistMoreThanToTravel;

                                    if (surfacePosAboveEndpoint || distToSurfaceLessThanProLengthSqr || isThisRight || surfaceToCenter > Vector3D.DistanceSquared(planetCenter, p.LastPosition))
                                    {
                                        var estiamtedSurfaceDistance = ray.Intersects(info.VoxelCache.PlanetSphere);
                                        var fullCheck = info.VoxelCache.PlanetSphere.Contains(p.Info.Origin) != ContainmentType.Disjoint || !estiamtedSurfaceDistance.HasValue;

                                        if (!fullCheck && estiamtedSurfaceDistance.HasValue && (estiamtedSurfaceDistance.Value <= beamLen || info.VoxelCache.PlanetSphere.Radius < 1))
                                        {

                                            double distSqr;
                                            var estimatedHit = ray.Position + (ray.Direction * estiamtedSurfaceDistance.Value);
                                            Vector3D.DistanceSquared(ref info.VoxelCache.FirstPlanetHit, ref estimatedHit, out distSqr);

                                            if (distSqr > 625)
                                                fullCheck = true;
                                            else
                                            {
                                                voxelHit = estimatedHit;
                                                voxelState = VoxelIntersectBranch.PseudoHit2;
                                            }
                                        }

                                        if (fullCheck)
                                            voxelState = VoxelIntersectBranch.DeferFullCheck;

                                        if (voxelHit.HasValue && Vector3D.DistanceSquared(voxelHit.Value, info.VoxelCache.PlanetSphere.Center) > info.VoxelCache.PlanetSphere.Radius * info.VoxelCache.PlanetSphere.Radius)
                                            info.VoxelCache.GrowPlanetCache(voxelHit.Value);
                                    }

                                }

                            }
                        }
                        else if (voxelHit == null && info.VoxelCache.MissSphere.Contains(p.Beam.To) == ContainmentType.Disjoint)
                        {
                            voxelState = VoxelIntersectBranch.DeferedMissUpdate;
                        }
                    }

                    if (voxelState == VoxelIntersectBranch.PseudoHit1 || voxelState == VoxelIntersectBranch.PseudoHit2)
                    {
                        if (!voxelHit.HasValue)
                        {

                            if (info.VoxelCache.MissSphere.Contains(beamTo) == ContainmentType.Disjoint)
                                info.VoxelCache.MissSphere.Center = beamTo;
                            continue;
                        }

                        hitEntity = pool.Count > 0 ? pool.Pop() : new HitEntity();
                        hitEntity.Pool = pool;

                        var hitPos = voxelHit.Value;
                        hitEntity.HitPos = hitPos;

                        double dist;
                        Vector3D.Distance(ref beamFrom, ref hitPos, out dist);
                        hitEntity.HitDist = dist;
                        hitEntity.EventType = info.FirstWaterHitTick != tick ? Voxel : Water;
                    }
                    else if (voxelState == VoxelIntersectBranch.DeferedMissUpdate || voxelState == VoxelIntersectBranch.DeferFullCheck)
                    {
                        FullVoxelCheck(p, voxel, voxelState, lineCheck);
                        /*
                        lock (DeferedVoxels)
                        {
                            DeferedVoxels.Add(new DeferedVoxels { Projectile = p, Branch = voxelState, Voxel = voxel, LineCheck = lineCheck});
                        }
                        */
                    }
                }
                else if (ent.Physics != null && !ent.Physics.IsPhantom && !ent.IsPreview && grid != null)
                {
                    if (grid != null)
                    {
                        hitEntity = pool.Count > 0 ? pool.Pop() : new HitEntity();
                        hitEntity.Pool = pool;
                        if (entIsSelf && !selfDamage)
                        {
                            if (!isBeam && beamLen <= grid.GridSize * 2 && !goCritical)
                            {
                                MyCube cube;
                                if (!(grid.TryGetCube(grid.WorldToGridInteger(p.Position), out cube) && isGrid && cube.CubeBlock != firingCube.SlimBlock || grid.TryGetCube(grid.WorldToGridInteger(p.LastPosition), out cube) && isGrid && cube.CubeBlock != firingCube.SlimBlock)) {
                                    hitEntity.Clean();
                                    continue;
                                }
                            }

                            if (!fieldActive)
                            {
                                var forwardPos = p.Info.Age != 1 ? beamFrom : beamFrom + (direction * Math.Min(grid.GridSizeHalf, info.DistanceTraveled - info.PrevDistanceTraveled));
                                grid.RayCastCells(forwardPos, p.Beam.To, hitEntity.Vector3ICache, null, true, true);
                                var cacheSlot = 0;
                                if (hitEntity.Vector3ICache.Count > 0)
                                {

                                    bool hitself = false;
                                    for (int j = 0; j < hitEntity.Vector3ICache.Count; j++)
                                    {

                                        MyCube myCube;
                                        if (grid.TryGetCube(hitEntity.Vector3ICache[j], out myCube))
                                        {

                                            if (goCritical || isGrid && ((IMySlimBlock)myCube.CubeBlock).Position != firingCube.Position)
                                            {

                                                hitself = true;
                                                cacheSlot = j;
                                                break;
                                            }
                                        }
                                    }

                                    if (!hitself) {
                                        hitEntity.Clean();
                                        continue;
                                    }

                                    IHitInfo hitInfo = null;
                                    if (!goCritical)
                                    {

                                        Session.I.Physics.CastRay(forwardPos, beamTo, out hitInfo, 15);
                                        var hitGrid = hitInfo?.HitEntity?.GetTopMostParent() as MyCubeGrid;
                                        if (hitGrid == null || firingCube == null || !firingCube.CubeGrid.IsSameConstructAs(hitGrid)) {
                                            hitEntity.Clean();
                                            continue;
                                        }
                                    }

                                    hitEntity.HitPos = hitInfo?.Position ?? beamFrom;
                                    var posI = hitEntity.Vector3ICache[cacheSlot];
                                    var block = grid.GetCubeBlock(hitEntity.Vector3ICache[cacheSlot]) as IMySlimBlock;
                                    if (block != null) 
                                        hitEntity.Blocks.Add(new HitEntity.RootBlocks { Block = block, QueryPos = posI });
                                    else {
                                        hitEntity.Clean();
                                        continue;
                                    }
                                }
                                else {
                                    hitEntity.Clean();
                                    continue;
                                }
                            }
                        }
                        else
                            grid.RayCastCells(beamFrom, beamTo, hitEntity.Vector3ICache, null, true, true);

                        if (!offensiveEwar && !fieldActive)
                        {

                            if (iShield != null && grid != null && grid.IsSameConstructAs(iShield.CubeGrid))
                                hitEntity.DamageMulti = 16;

                            hitEntity.EventType = Grid;
                        }
                        else if (!fieldActive)
                            hitEntity.EventType = Effect;
                        else
                            hitEntity.EventType = Field;
                    }
                }
                else if (destroyable != null)
                {
                    hitEntity = pool.Count > 0 ? pool.Pop() : new HitEntity();
                    hitEntity.Pool = pool;
                    hitEntity.EventType = Destroyable;
                }

                if (hitEntity != null)
                {
                    var hitEnt = hitEntity.EventType != Shield ? ent : (MyEntity)shieldInfo.Value.Item1;
                    if (hitEnt != null)
                    {
                        hitEntity.Info = info;
                        hitEntity.Entity = hitEnt;
                        hitEntity.ShieldEntity = ent;
                        hitEntity.Intersection = p.Beam;
                        hitEntity.SphereCheck = !lineCheck;
                        hitEntity.PruneSphere = p.PruneSphere;
                        hitEntity.SelfHit = entIsSelf;
                        hitEntity.DamageOverTime = aConst.EwarType == Dot;
                        info.HitList.Add(hitEntity);
                    }
                    else
                    {
                        hitEntity.Clean();
                    }
                }
            }

            if (aConst.CheckFutureIntersection && closestFutureEnt != null && (aAvoidSelf || closestFutureEnt.EntityId != w.Target.TopEntityId && closestFutureEnt != topEntity))
            {
                var oGrid = closestFutureEnt as MyCubeGrid;
                var tGrid = target.TargetObject as MyCubeBlock;
                var isTarget = !aAvoidTarget && oGrid != null &&  tGrid != null && oGrid.IsSameConstructAs(tGrid.CubeGrid);
                if (!isTarget)
                {
                    s.Obstacle.Entity = closestFutureEnt;
                    s.Obstacle.LastSeenTick = Session.I.Tick;
                    s.Obstacle.AvoidSphere = new BoundingSphereD(closestFutureEnt.PositionComp.WorldAABB.Center, closestFutureEnt.PositionComp.LocalVolume.Radius + aConst.FutureIntersectionRange);
                }
            }

            if (target.TargetState == Target.TargetStates.IsProjectile && aConst.NonAntiSmartEwar && !projetileInShield)
            {
                var detonate = p.State == ProjectileState.Detonate;
                var hitTolerance = detonate ? aConst.EndOfLifeRadius : aConst.ByBlockHitRadius > aConst.CollisionSize ? aConst.ByBlockHitRadius : aConst.CollisionSize;
                var useLine = lineCheck && !detonate && aConst.ByBlockHitRadius <= 0;
                var projectile = (Projectile)target.TargetObject;
                var sphere = new BoundingSphereD(projectile.Position, aConst.CollisionSize);
                sphere.Include(new BoundingSphereD(projectile.LastPosition, 1));

                bool rayCheck = false;
                if (useLine)
                {
                    var dist = sphere.Intersects(new RayD(p.LastPosition, p.Direction));
                    if (dist <= hitTolerance || isBeam && dist <= beamLen)
                        rayCheck = true;
                }

                var testSphere = p.PruneSphere;
                testSphere.Radius = hitTolerance;

                if (rayCheck || sphere.Intersects(testSphere))
                {
                    ProjectileHit(p, projectile, lineCheck, ref p.Beam);
                }
            }
                
            if (lineCheck)
                p.MySegmentList.Clear();
            else
                entityCollection.Clear();

            if (info.HitList.Count > 0)
                FinalizeHits(p);
        }

        internal void FinalizeHits(Projectile p)
        {
            p.Intersecting = GenerateHitInfo(p);
            var info = p.Info;

            if (p.Intersecting)
            {
                var aConst = info.AmmoDef.Const;
                if (aConst.VirtualBeams)
                {

                    info.Weapon.WeaponCache.Hits = p.VrPros.Count;
                    info.Weapon.WeaponCache.HitDistance = Vector3D.Distance(p.LastPosition, info.ProHit.LastHit);
                }

                lock (Session.I.Hits)
                {
                    if (Session.I.IsClient && info.AimedShot && aConst.ClientPredictedAmmo && !info.IsFragment)
                    {
                        SendClientHit(p, true);
                    }
                    Session.I.Hits.Add(p);
                }
                return;
            }

            info.HitList.Clear();
        }

        internal static void SendClientHit(Projectile p, bool hit)
        {
            var info = p.Info;
            var w = info.Weapon;
            var comp = w.Comp;
            var aConst = p.Info.AmmoDef.Const;

            var isBeam = aConst.IsBeamWeapon;
            var vel = isBeam ? Vector3D.Zero : !MyUtils.IsZero(p.Velocity) ? p.Velocity : p.PrevVelocity;

            var firstHitEntity = hit ? info.HitList[0] : null;
            var hitDist = hit ? firstHitEntity?.HitDist ?? info.MaxTrajectory : info.MaxTrajectory;
            var distToTarget = aConst.IsBeamWeapon ? hitDist : info.MaxTrajectory - info.DistanceTraveled;

            var intersectOrigin = isBeam ? new Vector3D(p.Beam.From + (p.Direction * distToTarget)) : p.LastPosition;

            Session.I.SendFixedGunHitEvent(hit, comp.CoreEntity, info.ProHit.Entity, intersectOrigin, vel, info.OriginUp, info.MuzzleId, w.System.WeaponIdHash, aConst.AmmoIdxPos, (float)(isBeam ? info.MaxTrajectory : distToTarget));
            info.AimedShot = false; //to prevent hits on another grid from triggering again
        }

        internal void ProjectileHit(Projectile p, Projectile target, bool lineCheck, ref LineD beam)
        {
            var pool = HitEntityArrayPool[Environment.CurrentManagedThreadId];

            var hitEntity = pool.Count > 0 ? pool.Pop() : new HitEntity();
            hitEntity.Pool = pool;

            hitEntity.Info = p.Info;
            hitEntity.EventType = HitEntity.Type.Projectile;
            hitEntity.Hit = true;
            hitEntity.Projectile = target;
            hitEntity.SphereCheck = !lineCheck;
            hitEntity.PruneSphere = p.PruneSphere;
            double dist;
            Vector3D.Distance(ref beam.From, ref target.Position, out dist);
            hitEntity.HitDist = dist;

            hitEntity.Intersection = new LineD(p.LastPosition, p.LastPosition + (p.Direction * dist));
            hitEntity.HitPos = hitEntity.Intersection.To;

            p.Info.HitList.Add(hitEntity);
        }

        internal bool GenerateHitInfo(Projectile p)
        {
            try
            {
                var info = p.Info;
                var count = info.HitList.Count;
                if (count > 1)
                {
                    try { info.HitList.Sort((x, y) => GetEntityCompareDist(x, y, info)); } // Unable to sort because the IComparer.Compare() method returns inconsistent results
                    catch (Exception ex) { Log.Line($"p.Info.HitList.Sort failed: {ex} - weapon:{info.Weapon.System.PartName} - ammo:{info.AmmoDef.AmmoRound} - hitCount:{info.HitList.Count}", null, true); }
                }
                else GetEntityCompareDist(info.HitList[0], null, info);

                try
                {
                    var pulseTrigger = false;
                    var voxelFound = false;
                    for (int i = info.HitList.Count - 1; i >= 0; i--)
                    {
                        var ent = info.HitList[i];
                        if (ent.EventType == Voxel)
                            voxelFound = true;

                        if (!ent.Hit) {
                            if (ent.PulseTrigger) pulseTrigger = true;
                            info.HitList.RemoveAtFast(i);
                            ent.Clean();
                        }
                        else break;
                    }

                    if (pulseTrigger)
                    {

                        info.ExpandingEwarField = true;
                        p.DistanceToTravelSqr = info.DistanceTraveled * info.DistanceTraveled;
                        p.PrevVelocity = p.Velocity;
                        p.Velocity = Vector3D.Zero;
                        info.ProHit.LastHit = p.Position;
                        info.HitList.Clear();
                        return false;
                    }

                    var finalCount = info.HitList.Count;
                    try
                    {
                        if (finalCount > 0)
                        {
                            var aConst = info.AmmoDef.Const;
                            try
                            {
                                if (voxelFound && info.HitList[0].EventType != Voxel && aConst.IsBeamWeapon)
                                    info.VoxelCache.HitRefreshed = 0;
                            }
                            catch (Exception ex) { Log.Line($"Exception in HitRefreshed finalCount: {ex}", null, true); }

                            var checkHit = (!aConst.IsBeamWeapon || !info.ShieldBypassed || finalCount > 1);

                            var blockingEnt = !info.ShieldBypassed || finalCount == 1 ? 0 : 1;
                            var hitEntity = info.HitList[blockingEnt];
                            if (hitEntity == null)
                            {
                                Log.Line($"null hitEntity");
                                return false;
                            }

                            if (!checkHit)
                                hitEntity.HitPos = p.Beam.To;

                            info.ProHit.Entity = hitEntity.Entity;
                            info.ProHit.LastHit = hitEntity.HitPos ?? p.Beam.To;

                            if (aConst.OnHit && Session.I.Tick >= info.ProHit.EndTick)
                            {
                                info.ProHit.EndTick = Session.I.Tick + aConst.OnHitDuration;
                            }

                            if (p.EnableAv || aConst.VirtualBeams)
                            {
                                Vector3D lastHitVel = Vector3D.Zero;
                                if (hitEntity.EventType == Shield)
                                {
                                    var cube = hitEntity.Entity as MyCubeBlock;
                                    if (cube?.CubeGrid?.Physics != null)
                                        lastHitVel = cube.CubeGrid.Physics.LinearVelocity;
                                }
                                else if (hitEntity.Projectile != null)
                                    lastHitVel = hitEntity.Projectile?.Velocity ?? Vector3D.Zero;
                                else if (hitEntity.Entity?.Physics != null)
                                    lastHitVel = hitEntity.Entity?.Physics.LinearVelocity ?? Vector3D.Zero;
                                else lastHitVel = Vector3D.Zero;

                                Vector3D visualHitPos;
                                if (hitEntity.Entity is MyCubeGrid)
                                {
                                    IHitInfo hitInfo = null;
                                    if (Session.I.HandlesInput && hitEntity.HitPos.HasValue && Vector3D.DistanceSquared(hitEntity.HitPos.Value, Session.I.CameraPos) < 22500 && Session.I.CameraFrustrum.Contains(hitEntity.HitPos.Value) != ContainmentType.Disjoint)
                                    {
                                        var entSphere = hitEntity.Entity.PositionComp.WorldVolume;
                                        var from = hitEntity.Intersection.From + (hitEntity.Intersection.Direction * MyUtils.GetSmallestDistanceToSphereAlwaysPositive(ref hitEntity.Intersection.From, ref entSphere));
                                        var to = hitEntity.HitPos.Value + (hitEntity.Intersection.Direction * 3f);
                                        Session.I.Physics.CastRay(from, to, out hitInfo, CollisionLayers.NoVoxelCollisionLayer);
                                    }
                                    visualHitPos = hitInfo?.HitEntity != null ? hitInfo.Position : hitEntity.HitPos ?? p.Beam.To;
                                }
                                else visualHitPos = hitEntity.HitPos ?? p.Beam.To;

                                if (p.EnableAv) {
                                    info.AvShot.LastHitShield = hitEntity.EventType == Shield;
                                    info.AvShot.Hit = new Hit { Entity = hitEntity.Entity, EventType = hitEntity.EventType, HitTick = Session.I.Tick, HitVelocity = lastHitVel, LastHit = visualHitPos, SurfaceHit = visualHitPos };
                                }
                                else if (aConst.VirtualBeams)
                                    AvShot.UpdateVirtualBeams(p, info, hitEntity, visualHitPos, lastHitVel, true);

                                if (info.AimedShot && Session.I.TrackingAi != null && Session.I.TargetUi.HitIncrease < 0.1d && info.Ai.ControlComp == null && (aConst.FixedFireAmmo || info.Weapon.Comp.Data.Repo.Values.Set.Overrides.Control != ProtoWeaponOverrides.ControlModes.Auto))
                                    Session.I.TargetUi.SetHit(info);
                            }


                            return true;
                        }

                    }
                    catch (Exception ex) { Log.Line($"Exception in GenerateHitInfo1: {ex}", null, true); }
                }
                catch (Exception ex) { Log.Line($"Exception in GenerateHitInfo2: {ex}", null, true); }

            }
            catch (Exception ex) { Log.Line($"Exception in GenerateHitInfo3: {ex}", null, true); }

            return false;
        }

        internal int GetEntityCompareDist(HitEntity x, HitEntity y, ProInfo info)
        {
            var xDist = double.MaxValue;
            var yDist = double.MaxValue;
            var beam = x.Intersection;
            var count = y != null ? 2 : 1;
            var aConst = info.AmmoDef.Const;
            var eWarPulse = aConst.EwarField && info.EwarActive;
            var triggerEvent = aConst.EwarFieldTrigger && !info.ExpandingEwarField;
            for (int i = 0; i < count; i++)
            {
                var isX = i == 0;

                MyEntity ent;
                HitEntity hitEnt;
                if (isX)
                {
                    hitEnt = x;
                    ent = hitEnt.Entity;
                }
                else
                {
                    hitEnt = y;
                    ent = hitEnt.Entity;
                }
                var dist = double.MaxValue;
                var shield = ent as IMyTerminalBlock;
                var grid = ent as MyCubeGrid;
                var voxel = ent as MyVoxelBase;

                if (triggerEvent && ent != null && (info.Ai.Targets.ContainsKey(ent) || shield != null))
                    hitEnt.PulseTrigger = true;
                else if (hitEnt.Projectile != null)
                    dist = hitEnt.HitDist.Value;
                else if (shield != null)
                {
                    hitEnt.Hit = true;
                    dist = hitEnt.HitDist.Value;
                    info.ShieldInLine = true;
                }
                else if (grid != null)
                {

                    if (hitEnt.Miss)
                    {
                        hitEnt.HitDist = null;
                    }
                    else if (hitEnt.Hit)
                    {
                        dist = Vector3D.Distance(hitEnt.Intersection.From, hitEnt.HitPos.Value);
                        hitEnt.HitDist = dist;
                    }
                    else if (hitEnt.HitPos != null)
                    {
                        dist = Vector3D.Distance(hitEnt.Intersection.From, hitEnt.HitPos.Value);
                        hitEnt.HitDist = dist;
                        hitEnt.Hit = true;
                    }
                    else
                    {
                        if (hitEnt.SphereCheck || eWarPulse)
                        {
                            var ewarActive = hitEnt.EventType == Field || hitEnt.EventType == Effect;
                            var hitPos = !ewarActive ? hitEnt.PruneSphere.Center + (hitEnt.Intersection.Direction * hitEnt.PruneSphere.Radius) : hitEnt.PruneSphere.Center;
                            if (hitEnt.SelfHit && (Vector3D.DistanceSquared(hitPos, hitEnt.Info.Origin) <= grid.GridSize * grid.GridSize) && hitEnt.EventType != Field) 
                                continue;

                            if (!ewarActive)
                                GetAndSortBlocksInSphere(hitEnt.Info.AmmoDef, hitEnt.Info.Weapon.System, grid, hitEnt.PruneSphere, false, hitEnt.Blocks);

                            if (hitEnt.Blocks.Count > 0 || ewarActive)
                            {
                                dist = 0;
                                hitEnt.HitDist = dist;
                                hitEnt.Hit = true;
                                hitEnt.HitPos = hitPos;
                            }
                        }
                        else
                        {
                            var closestBlockFound = false;
                            IMySlimBlock lastBlockHit = null;
                            var ewarWeaponDamage = info.EwarActive && aConst.SelfDamage && hitEnt.EventType == Effect;

                            for (int j = 0; j < hitEnt.Vector3ICache.Count; j++)
                            {
                                var posI = hitEnt.Vector3ICache[j];
                                var firstBlock = grid.GetCubeBlock(posI) as IMySlimBlock;
                                if (firstBlock != null && firstBlock != lastBlockHit && !firstBlock.IsDestroyed && (hitEnt.Info.Ai.AiType != Ai.AiTypes.Grid || firstBlock != hitEnt.Info.Weapon.Comp.Cube?.SlimBlock || ewarWeaponDamage && firstBlock == hitEnt.Info.Weapon.Comp.Cube?.SlimBlock))
                                {
                                    lastBlockHit = firstBlock;
                                    hitEnt.Blocks.Add(new HitEntity.RootBlocks {Block = firstBlock, QueryPos = posI});
                                    if (closestBlockFound) continue;
                                    MyOrientedBoundingBoxD obb;
                                    var fat = firstBlock.FatBlock;
                                    if (fat != null)
                                    {
                                        obb = new MyOrientedBoundingBoxD(fat.Model.BoundingBox, fat.PositionComp.WorldMatrixRef);
                                    }
                                    else
                                    {
                                        Vector3 halfExt;
                                        firstBlock.ComputeScaledHalfExtents(out halfExt);
                                        var blockBox = new BoundingBoxD(-halfExt, halfExt);
                                        var gridMatrix = grid.PositionComp.WorldMatrixRef;
                                        gridMatrix.Translation = grid.GridIntegerToWorld(firstBlock.Position);
                                        obb = new MyOrientedBoundingBoxD(blockBox, gridMatrix);
                                    }

                                    var obbIntersects = obb.Intersects(ref beam);
                                    if (fat != null && (!hitEnt.SelfHit && obbIntersects.HasValue || hitEnt.Info.Ai.AiType == Ai.AiTypes.Player))
                                    {
                                        var door = fat as MyDoorBase;
                                        if (door != null && door.Open && !Session.I.HitDoor(hitEnt, door) || door == null && (hitEnt.Info.Ai.AiType == Ai.AiTypes.Player || fat is IMyMechanicalConnectionBlock) && !Session.I.RayAccuracyCheck(hitEnt, fat.SlimBlock))
                                        {
                                            hitEnt.Blocks.Clear();
                                            continue;
                                        }
                                    }

                                    var hitDist = obbIntersects ?? Vector3D.Distance(beam.From, obb.Center);
                                    var hitPos = beam.From + (beam.Direction * hitDist);

                                    if (hitEnt.SelfHit && !info.Storage.SmartReady)
                                    {
                                        if (Vector3D.DistanceSquared(hitPos, hitEnt.Info.Origin) <= grid.GridSize * 3)
                                        {
                                            hitEnt.Blocks.Clear();
                                        }
                                        else
                                        {
                                            dist = hitDist;
                                            hitEnt.HitDist = dist;
                                            hitEnt.Hit = true;
                                            hitEnt.HitPos = hitPos;
                                        }
                                        break;
                                    }

                                    dist = hitDist;
                                    hitEnt.HitDist = dist;
                                    hitEnt.Hit = true;
                                    hitEnt.HitPos = hitPos;
                                    closestBlockFound = true;
                                }
                            }

                            hitEnt.Miss = !closestBlockFound;
                        }
                    }
                }
                else if (voxel != null)
                {
                    hitEnt.Hit = true;
                    dist = hitEnt.HitDist.Value;
                    hitEnt.HitDist = dist;
                    dist += 1.25;
                }
                else if (ent is IMyDestroyableObject)
                {

                    if (hitEnt.Hit)
                    {
                        dist = Vector3D.Distance(hitEnt.Intersection.From, hitEnt.HitPos.Value);
                    }
                    else
                    {
                        if (hitEnt.SphereCheck || eWarPulse)
                        {

                            var ewarActive = hitEnt.EventType == Field || hitEnt.EventType == Effect;
                            dist = 0;
                            hitEnt.HitDist = dist;
                            hitEnt.Hit = true;
                            var hitPos = !ewarActive ? hitEnt.PruneSphere.Center + (hitEnt.Intersection.Direction * hitEnt.PruneSphere.Radius) : hitEnt.PruneSphere.Center;
                            hitEnt.HitPos = hitPos;
                        }
                        else
                        {

                            var transform = ent.PositionComp.WorldMatrixRef;
                            var box = ent.PositionComp.LocalAABB;
                            var obb = new MyOrientedBoundingBoxD(box, transform);
                            dist = obb.Intersects(ref beam) ?? double.MaxValue;
                            if (dist < double.MaxValue)
                            {
                                hitEnt.Hit = true;
                                hitEnt.HitPos = beam.From + (beam.Direction * dist);
                                hitEnt.HitDist = dist;
                            }
                        }
                    }
                }

                if (isX) xDist = dist;
                else yDist = dist;
            }
            return xDist.CompareTo(yDist);
        }

        internal void FullVoxelCheck(Projectile p, MyVoxelBase voxel, VoxelIntersectBranch branch, bool lineCheck)
        {
            var s = Session.I;
            var w = p.Info.Weapon;
            var info = p.Info;
            var aConst = info.AmmoDef.Const;

            Vector3D? voxelHit = null;

            if (branch == VoxelIntersectBranch.DeferFullCheck)
            {
                if (aConst.FakeVoxelHitTicks > 0 && s.Tick - w.WeaponCache.FakeCheckTick < aConst.FakeVoxelHitTicks && w.WeaponCache.FakeHitDistance > 0)
                {
                    voxelHit = p.Beam.From + p.Beam.Direction * w.WeaponCache.FakeHitDistance;
                }

                IHitInfo hit;
                if (s.Physics.CastRay(p.Beam.From, p.Beam.To, out hit, CollisionLayers.StaticCollisionLayer, false) && hit != null)
                    voxelHit = hit.Position;
                else if (PointInsideVoxel(voxel, s.TmpStorage, p.Beam.From))
                    voxelHit = p.Beam.From;

                if (info.PrevRelativeAge <= -1)
                {
                    if (voxelHit.HasValue)
                    {
                        if (info.IsFragment && !PointInsideVoxel(voxel, s.TmpStorage, voxelHit.Value + (p.Beam.Direction * 1.25f)))
                            voxelHit = null;
                    }
                    else if (PointInsideVoxel(voxel, s.TmpStorage, p.Beam.From))
                        voxelHit = p.Beam.From;
                }
            }
            else if (branch == VoxelIntersectBranch.DeferedMissUpdate)
            {

                if (aConst.FakeVoxelHitTicks > 0 && s.Tick - w.WeaponCache.FakeCheckTick < aConst.FakeVoxelHitTicks && w.WeaponCache.FakeHitDistance > 0)
                {
                    voxelHit = p.Beam.From + p.Beam.Direction * w.WeaponCache.FakeHitDistance;
                }
                else
                {
                    IHitInfo hit;
                    if (s.Physics.CastRay(p.Beam.From, p.Beam.To, out hit, CollisionLayers.StaticCollisionLayer, false) && hit != null)
                        voxelHit = hit.Position;
                    else if (PointInsideVoxel(voxel, s.TmpStorage, p.Beam.From))
                        voxelHit = p.Beam.From;
                }
            }

            if (!voxelHit.HasValue)
            {

                if (info.VoxelCache.MissSphere.Contains(p.Beam.To) == ContainmentType.Disjoint)
                    info.VoxelCache.MissSphere.Center = p.Beam.To;
                return;
            }

            info.VoxelCache.Update(voxel, ref voxelHit, Session.I.Tick);

            if (voxelHit == null)
                return;

            var pool = HitEntityArrayPool[Environment.CurrentManagedThreadId];
            var hitEntity = pool.Count > 0 ? pool.Pop() : new HitEntity();
            hitEntity.Pool = pool;
            hitEntity.Info = info;
            hitEntity.Entity = voxel;
            hitEntity.Intersection = p.Beam;
            hitEntity.SphereCheck = !lineCheck;
            hitEntity.PruneSphere = p.PruneSphere;
            hitEntity.DamageOverTime = aConst.EwarType == Dot;

            var hitPos = voxelHit.Value;
            hitEntity.HitPos = hitPos;

            double dist;
            Vector3D.Distance(ref p.Beam.From, ref hitPos, out dist);
            hitEntity.HitDist = dist;
            if (aConst.FakeVoxelHitTicks > 0 && s.Tick - w.WeaponCache.FakeCheckTick > aConst.FakeVoxelHitTicks || w.WeaponCache.FakeHitDistance == 0)
            {
                w.WeaponCache.FakeCheckTick = s.Tick;
                w.WeaponCache.FakeHitDistance = dist;
            }
            hitEntity.EventType = Voxel;
            p.Info.HitList.Add(hitEntity);
        }

        //TODO: In order to fix SphereShapes collisions with grids, this needs to be adjusted to take into account the Beam of the projectile
        internal static void GetAndSortBlocksInSphere(WeaponDefinition.AmmoDef ammoDef, WeaponSystem system, MyCubeGrid grid, BoundingSphereD sphere, bool fatOnly, List<HitEntity.RootBlocks> blocks)
        {
            var matrixNormalizedInv = grid.PositionComp.WorldMatrixNormalizedInv;
            Vector3D result;
            Vector3D.Transform(ref sphere.Center, ref matrixNormalizedInv, out result);
            var localSphere = new BoundingSphere(result, (float)sphere.Radius);
            var fieldType = ammoDef.Ewar.Type;
            var hitPos = sphere.Center;
            if (fatOnly)
            {
                TopMap map;
                if (Session.I.TopEntityToInfoMap.TryGetValue(grid, out map))
                {
                    foreach (var cube in map.MyCubeBocks)
                    {
                        switch (fieldType)
                        {
                            case JumpNull:
                                if (!(cube is MyJumpDrive)) continue;
                                break;
                            case EnergySink:
                                if (!(cube is IMyPowerProducer)) continue;
                                break;
                            case Anchor:
                                if (!(cube is MyThrust)) continue;
                                break;
                            case Nav:
                                if (!(cube is MyGyro)) continue;
                                break;
                            case Offense:
                                var valid = cube is IMyGunBaseUser || cube is MyConveyorSorter && Session.I.PartPlatforms.ContainsKey(cube.BlockDefinition.Id);
                                if (!valid) continue;
                                break;
                            case Emp:
                            case Dot:
                                if (fieldType == Emp && cube is IMyUpgradeModule && Session.I.CoreShieldBlockTypes.Contains(cube.BlockDefinition))
                                    continue;
                                break;
                            default: continue;
                        }
                        var block = cube.SlimBlock as IMySlimBlock;
                        if (!new BoundingBox(block.Min * grid.GridSize - grid.GridSizeHalf, block.Max * grid.GridSize + grid.GridSizeHalf).Intersects(localSphere))
                            continue;
                        blocks.Add(new HitEntity.RootBlocks {Block = block, QueryPos = block.Position});
                    }
                }
            }
            else
            {
                //usage:
                //var dict = (Dictionary<Vector3I, IMySlimBlock>)GetHackDict((IMySlimBlock) null);
                var tmpList = Session.I.SlimPool.Get();
                Session.GetBlocksInsideSphereFast(grid, ref sphere, true, tmpList);

                for (int i = 0; i < tmpList.Count; i++)
                {
                    var block = tmpList[i];
                    blocks.Add(new HitEntity.RootBlocks { Block = block, QueryPos = block.Position});
                }

                Session.I.SlimPool.Return(tmpList);
            }

            blocks.Sort((a, b) =>
            {
                var aPos = grid.GridIntegerToWorld(a.Block.Position);
                var bPos = grid.GridIntegerToWorld(b.Block.Position);
                return Vector3D.DistanceSquared(aPos, hitPos).CompareTo(Vector3D.DistanceSquared(bPos, hitPos));
            });
        }
        public static object GetHackDict<TVal>(TVal valueType) => new Dictionary<Vector3I, TVal>();

    }
}
