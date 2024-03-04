using System;
using System.Collections.Generic;
using CoreSystems.Support;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Utils;
using VRageMath;
using static CoreSystems.Projectiles.Projectile;
using static CoreSystems.Support.AvShot;

namespace CoreSystems.Projectiles
{
    public partial class Projectiles
    {
        internal readonly Stack<List<NewVirtual>> VirtInfoPools = new Stack<List<NewVirtual>>(32);
        internal readonly Stack<ProInfo> VirtInfoPool = new Stack<ProInfo>(128);
        internal readonly Stack<HitEntity>[] HitEntityArrayPool = new Stack<HitEntity>[1025];
        internal readonly List<DeferedVoxels> DeferedVoxels = new List<DeferedVoxels>(128);
        internal readonly List<Projectile> AddTargets = new List<Projectile>();
        internal readonly List<Fragments> ShrapnelToSpawn = new List<Fragments>(128);
        internal readonly List<Projectile> ActiveProjetiles = new List<Projectile>(2048);
        internal readonly List<DeferedAv> DeferedAvDraw = new List<DeferedAv>(1024);
        internal readonly List<NewProjectile> NewProjectiles = new List<NewProjectile>(128);
        internal readonly Stack<Projectile> ProjectilePool = new Stack<Projectile>(2048);
        internal readonly Stack<Fragments> ShrapnelPool = new Stack<Fragments>(128);
        internal readonly Stack<Fragment> FragmentPool = new Stack<Fragment>(128);

        internal ulong CurrentProjectileId;
        internal Projectiles()
        {
            for (int i = 0; i < HitEntityArrayPool.Length; i++)
                HitEntityArrayPool[i] = new Stack<HitEntity>();
        }

        internal void Clean()
        {
            VirtInfoPools.Clear();
            VirtInfoPool.Clear();
            foreach (var a in HitEntityArrayPool)
                a?.Clear();
            DeferedVoxels.Clear();
            AddTargets.Clear();
            ShrapnelToSpawn.Clear();
            ActiveProjetiles.Clear();
            DeferedAvDraw.Clear();
            NewProjectiles.Clear();
            ProjectilePool.Clear();
            ShrapnelPool.Clear();
            FragmentPool.Clear();
        }

        internal void SpawnAndMove() // Methods highly inlined due to keen's mod profiler
        {
            Session.I.StallReporter.Start("GenProjectiles", 11);
            if (NewProjectiles.Count > 0) GenProjectiles();
            Session.I.StallReporter.End();

            Session.I.StallReporter.Start("AddTargets", 11);
            if (AddTargets.Count > 0)
                AddProjectileTargets();
            Session.I.StallReporter.End();

            Session.I.StallReporter.Start($"UpdateState: {ActiveProjetiles.Count}", 11);
            if (ActiveProjetiles.Count > 0) 
                UpdateState();
            Session.I.StallReporter.End();

            Session.I.StallReporter.Start($"Spawn: {ShrapnelToSpawn.Count}", 11);
            if (ShrapnelToSpawn.Count > 0)
                SpawnFragments();
            Session.I.StallReporter.End();
        }

        internal void Intersect() // Methods highly inlined due to keen's mod profiler
        {
            Session.I.StallReporter.Start($"CheckHits: {ActiveProjetiles.Count}", 11);
            if (ActiveProjetiles.Count > 0)
                CheckHits();
            Session.I.StallReporter.End();
        }

        internal void Damage()
        {
            if (Session.I.EffectedCubes.Count > 0)
                Session.I.ApplyGridEffect();

            if (Session.I.Tick60)
                Session.I.GridEffects();

            if (Session.I.IsClient && (Session.I.CurrentClientEwaredCubes.Count > 0 || Session.I.ActiveEwarCubes.Count > 0) && (Session.I.ClientEwarStale || Session.I.Tick120))
                Session.I.SyncClientEwarBlocks();

            if (Session.I.Hits.Count > 0)
            {
                Session.I.Api.ProjectileDamageEvents.Clear();

                Session.I.ProcessHits();

                if (Session.I.Api.ProjectileDamageEvents.Count > 0)
                    Session.I.ProcessDamageHandlerRequests();
            }
            if (Session.I.DeferredDestroy.Count > 0)
            {
                Session.I.DefferedDestroy();
            }
        }

        internal void AvUpdate()
        {
            if (!Session.I.DedicatedServer)
            {
                Session.I.StallReporter.Start($"AvUpdate: {ActiveProjetiles.Count}", 11);
                UpdateAv();
                DeferedAvStateUpdates();
                Session.I.StallReporter.End();
            }
        }

        private void UpdateState(int end = 0)
        {
            for (int i = ActiveProjetiles.Count - 1; i >= end; i--)
            {
                var p = ActiveProjetiles[i];
                var info = p.Info;
                var storage = info.Storage;
                var aConst = info.AmmoDef.Const;
                var target = info.Target;
                var ai = p.Info.Ai;
                ++info.Age;
                info.PrevRelativeAge = info.RelativeAge;
                info.RelativeAge += Session.I.DeltaTimeRatio;
                ++ai.MyProjectiles;
                ai.ProjectileTicker = Session.I.Tick;

                if (Session.I.AdvSync && aConst.FullSync)
                {
                    if (Session.I.IsClient) 
                    {
                        var posSlot = (int)Math.Round(info.RelativeAge) % 30;
                        storage.FullSyncInfo.PastProInfos[posSlot] =  p.Position;
                        if (info.Weapon.WeaponProSyncs.Count > 0)
                            p.SyncClientProjectile(posSlot);
                    }
                    else if (info.Age > 0 && info.Age % 29 == 0)
                        p.SyncPosServerProjectile(p.State != ProjectileState.Alive ? ProtoProPosition.ProSyncState.Dead : ProtoProPosition.ProSyncState.Alive);
                }

                if (storage.Sleep)
                {
                    var prevCheck = info.PrevRelativeAge % 100;
                    var currentCheck = info.RelativeAge % 100;
                    var check = prevCheck < 0 || prevCheck > currentCheck;
                    if (p.DeaccelRate > 300 && !check) {
                        p.DeaccelRate--;
                        continue;
                    }
                    storage.Sleep = false;
                }

                switch (p.State) {
                    case ProjectileState.Destroy:
                        p.DestroyProjectile();
                        continue;
                    case ProjectileState.Dead:
                        continue;
                    case ProjectileState.OneAndDone:
                    case ProjectileState.Depleted:
                    case ProjectileState.Detonate:
                        if (aConst.IsBeamWeapon && info.Age == 0)
                            break;

                        p.ProjectileClose();
                        ProjectilePool.Push(p);
                        ActiveProjetiles.RemoveAtFast(i);
                        continue;
                }

                if (p.EndState == EndStates.None) {

                    if (aConst.IsBeamWeapon) {

                        info.DistanceTraveled = info.MaxTrajectory;
                        p.LastPosition = p.Position;
                        
                        var beamEnd = p.Position + (p.Direction * info.MaxTrajectory);
                        p.TravelMagnitude = p.Position - beamEnd;
                        p.Position = beamEnd;
                        p.EndState = EndStates.AtMaxRange;
                    }
                    else 
                    {
                        var pTarget = target.TargetObject as Projectile;
                        if (pTarget != null && pTarget.State != ProjectileState.Alive) {
                            pTarget.Seekers.Remove(p);
                            target.Reset(Session.I.Tick, Target.States.ProjetileIntercept);
                        }

                        if (aConst.FeelsGravity && MyUtils.IsValid(p.Gravity) && !MyUtils.IsZero(ref p.Gravity)) 
                        {
                            var gravChange = (p.Gravity * aConst.GravityMultiplier) * (float)Session.I.DeltaStepConst;
                            p.Velocity += gravChange;
                            if (Vector3D.IsZero(p.Velocity))
                                p.Velocity = new Vector3D(float.Epsilon, float.Epsilon, float.Epsilon);

                            if (!aConst.IsSmart && !aConst.IsDrone)
                                p.Direction = Vector3D.Normalize(p.Direction + gravChange / (float)p.Velocity.Length());
                        }

                        var runSmart = aConst.IsSmart && (!aConst.IsMine || storage.RequestedStage == 1 && p.DistanceToTravelSqr < double.MaxValue);
                        if (runSmart)
                            p.RunSmart();
                        else if (aConst.IsDrone)
                            p.RunDrone();
                        else if (!aConst.AmmoSkipAccel && !info.ExpandingEwarField) {

                            var accel = true;
                            Vector3D newVel;
                            var accelThisTick = p.Direction * (aConst.DeltaVelocityPerTick * Session.I.DeltaTimeRatio);

                            var maxSpeedSqr = p.MaxSpeed * p.MaxSpeed;
                            if (p.DeaccelRate > 0) {

                                var distToMax = info.MaxTrajectory - info.DistanceTraveled;

                                var stopDist = p.VelocityLengthSqr / 2 / aConst.AccelInMetersPerSec;
                                if (distToMax <= stopDist)
                                    accel = false;

                                newVel = accel ? p.Velocity + accelThisTick : p.Velocity - accelThisTick;
                                p.VelocityLengthSqr = newVel.LengthSquared();

                                if (accel && p.VelocityLengthSqr > maxSpeedSqr) newVel = p.Direction * p.MaxSpeed;
                                else if (!accel && distToMax <= 0) {
                                    newVel = Vector3D.Zero;
                                    p.VelocityLengthSqr = 0;
                                }
                            }
                            else
                            {

                                newVel = p.Velocity + accelThisTick;

                                p.VelocityLengthSqr = newVel.LengthSquared();
                                if (p.VelocityLengthSqr > maxSpeedSqr)
                                    newVel = p.Direction * p.MaxSpeed;
                                else
                                    info.TotalAcceleration += (newVel - p.PrevVelocity);

                                if (info.TotalAcceleration.LengthSquared() > aConst.MaxAccelerationSqr)
                                    newVel = p.Velocity;
                            }

                            p.Velocity = newVel;
                        }

                        if (aConst.AmmoSkipAccel || p.VelocityLengthSqr > 0)
                            p.LastPosition = p.Position;

                        p.TravelMagnitude = info.Age != 0 ? p.Velocity * Session.I.DeltaStepConst : p.TravelMagnitude;
                        p.Position += p.TravelMagnitude;

                        info.PrevDistanceTraveled = info.DistanceTraveled;

                        double distChanged;
                        Vector3D.Dot(ref p.Direction, ref p.TravelMagnitude, out distChanged);
                        info.DistanceTraveled += Math.Abs(distChanged);

                        if (info.RelativeAge > aConst.MaxLifeTime) {
                            p.DistanceToTravelSqr = (info.DistanceTraveled * info.DistanceTraveled);
                            p.EndState = EndStates.EarlyEnd;
                        }

                        if (info.DistanceTraveled * info.DistanceTraveled >= p.DistanceToTravelSqr) {

                            if (!aConst.IsMine || storage.LastActivatedStage == -1)
                                p.EndState = p.EndState == EndStates.EarlyEnd ? EndStates.AtMaxEarly : EndStates.AtMaxRange;

                            if (p.DeaccelRate > 0) {

                                p.DeaccelRate--;
                                if (aConst.IsMine && storage.LastActivatedStage == -1 && info.Storage.RequestedStage != -2) {
                                    if (p.EnableAv) info.AvShot.Cloaked = info.AmmoDef.Trajectory.Mines.Cloak;
                                    storage.LastActivatedStage = -2;
                                }
                            }
                        }
                    }
                }
                else if (p.EndState == EndStates.AtMaxEarly) //Prevents projectiles that are AtMaxEarly from hanging infinitely
                    p.State = ProjectileState.Destroy;

                if (aConst.Ewar)
                    p.RunEwar();
            }
        }

        private int _beamCount;
        private void CheckHits()
        {
            _beamCount = 0;
            var apCount = ActiveProjetiles.Count;
            var minCount = Session.I.Settings.Enforcement.BaseOptimizations ? 96 : 99999;
            var targetStride = apCount / 20;
            var stride = apCount < minCount ? 100000 : targetStride > 48 ? targetStride : 48;

            MyAPIGateway.Parallel.For(0, apCount, i =>
            {
                var p = ActiveProjetiles[i];
                var info = p.Info;
                var storage = info.Storage;

                if ((int)p.State > 3 || storage.Sleep)
                    return;

                var ai = info.Ai;
                var aDef = info.AmmoDef;
                var aConst = aDef.Const;
                var target = info.Target;
                var primeModelUpdate = aConst.PrimeModel && p.EnableAv;
                if (primeModelUpdate || aConst.TriggerModel)
                {
                    if (primeModelUpdate) {

                        Vector3D modelDir;
                        var aInfo = storage.ApproachInfo;
                        if (aConst.HasApproaches && aInfo.Active && aInfo.ModelRotateMaxAge > 0 && aInfo.ModelRotateAge > 0 && !MyUtils.IsZero(aInfo.TargetPos)) 
                            modelDir =  Vector3D.Lerp(p.Direction, Vector3D.Normalize(aInfo.TargetPos - p.Position), aInfo.ModelRotateAge / (double)aInfo.ModelRotateMaxAge);
                        else
                            modelDir = p.Direction;

                        MatrixD.CreateWorld(ref p.Position, ref modelDir, ref info.OriginUp, out info.AvShot.PrimeMatrix);
                    }

                    if (aConst.TriggerModel)
                        info.TriggerMatrix.Translation = p.Position;
                }

                if (aConst.IsBeamWeapon)
                    ++_beamCount;

                var ewarTriggered = aConst.EwarFieldTrigger && info.ExpandingEwarField;
                var useEwarFieldSphere = (ewarTriggered || info.EwarActive) && aConst.EwarField && aConst.EwarType != WeaponDefinition.AmmoDef.EwarDef.EwarType.AntiSmart;
                var ewarRadius = !info.ExpandingEwarField ? aConst.EwarRadius : info.TriggerGrowthSteps < aConst.FieldGrowTime ? info.TriggerMatrix.Scale.AbsMax() : aConst.EwarRadius;
                p.Beam = useEwarFieldSphere ? new LineD(p.Position + (-p.Direction * ewarRadius), p.Position + (p.Direction * ewarRadius)) : new LineD(p.LastPosition, p.Position);
                var checkBeam = aConst.CheckFutureIntersection ? new LineD(p.Beam.From, p.Beam.From + p.Beam.Direction * (p.Beam.Length + aConst.FutureIntersectionRange), p.Beam.Length + aConst.FutureIntersectionRange) : p.Beam;
                var lineCheck = aConst.CollisionIsLine && !useEwarFieldSphere;

                if (p.DeaccelRate <= 0 && !aConst.IsBeamWeapon && (info.DistanceTraveled * info.DistanceTraveled >= p.DistanceToTravelSqr && !ewarTriggered || info.RelativeAge > aConst.MaxLifeTime)) {

                    p.PruneSphere.Center = p.Position;
                    p.PruneSphere.Radius = aConst.EndOfLifeRadius;

                    if (aConst.TravelTo && storage.RequestedStage == -2 || aConst.EndOfLifeAoe && info.RelativeAge >= aConst.MinArmingTime && (!aConst.ArmOnlyOnEolHit || info.ObjectsHit > 0))
                    {
                        MyGamePruningStructure.GetAllTopMostEntitiesInSphere(ref p.PruneSphere, p.MyEntityList, p.PruneQuery);

                        if (info.Weapon.System.TrackProjectile)
                        {
                            foreach (var lp in ai.LiveProjectile.Keys)
                            {
                                if (p.PruneSphere.Contains(lp.Position) != ContainmentType.Disjoint && lp != info.Target.TargetObject)
                                {
                                    ProjectileHit(p, lp, aConst.CollisionIsLine, ref p.Beam);
                                }
                            }
                        }


                        p.State = ProjectileState.Detonate;

                        if (p.EnableAv)
                            info.AvShot.ForceHitParticle = true;
                    }
                    else
                        p.State = ProjectileState.Detonate;

                    p.EndState = p.EndState == EndStates.AtMaxRange ? EndStates.AtMaxEarly : EndStates.EarlyEnd;
                    info.ProHit.LastHit = p.Position;
                }

                if (aConst.IsMine && storage.LastActivatedStage <= -2 && storage.RequestedStage != -3)
                    p.SeekEnemy();
                else if (useEwarFieldSphere)
                {
                    if (info.ExpandingEwarField)
                    {
                        p.PruneSphere.Center = p.Position;

                        if (p.PruneSphere.Radius < ewarRadius) {
                            p.PruneSphere.Center = p.Position;
                            p.PruneSphere.Radius = ewarRadius;
                        }
                        else
                        {
                            p.PruneSphere.Center = p.Position;
                            p.PruneSphere.Radius = aConst.EwarRadius;
                        }
                    }
                    else
                    {
                        p.PruneSphere = new BoundingSphereD(p.Position, aConst.EwarRadius);
                    }

                }
                else if (lineCheck)
                {
                    p.PruneSphere.Center = p.Position;
                    p.PruneSphere.Radius = aConst.CollisionSize;
                    if (aConst.IsBeamWeapon || info.DistanceTraveled > aConst.CollisionSize + 1.35f) {

                        if (aConst.DynamicGuidance && p.PruneQuery == MyEntityQueryType.Dynamic && Session.I.Tick60)
                            p.CheckForNearVoxel(60);
                        MyGamePruningStructure.GetTopmostEntitiesOverlappingRay(ref checkBeam, p.MySegmentList, p.PruneQuery);
                    }
                }
                else
                {
                    p.PruneSphere = new BoundingSphereD(p.Position, 0).Include(new BoundingSphereD(p.LastPosition, 0));
                    if (p.PruneSphere.Radius < aConst.CollisionSize)
                    {
                        p.PruneSphere.Center = p.Position;
                        p.PruneSphere.Radius = aConst.CollisionSize;
                    }
                }
                
                if (!lineCheck) {

                    if (aConst.DynamicGuidance && p.PruneQuery == MyEntityQueryType.Dynamic && Session.I.Tick60)
                        p.CheckForNearVoxel(60);
                    MyGamePruningStructure.GetAllTopMostEntitiesInSphere(ref p.PruneSphere, p.MyEntityList, p.PruneQuery);
                }

                info.ShieldBypassed = info.ShieldKeepBypass;
                info.ShieldKeepBypass = false;

                if (target.TargetState == Target.TargetStates.IsProjectile || lineCheck && p.MySegmentList.Count > 0 || !lineCheck && p.MyEntityList.Count > 0)
                {
                    InitialHitCheck(p, lineCheck);
                }
                else if (aConst.IsMine && storage.LastActivatedStage <= -2 && storage.RequestedStage != -3 && info.RelativeAge - storage.ChaseAge > 600)
                {
                    storage.Sleep = true;
                }
            },stride);
        }

        private void UpdateAv()
        {
            for (int x = ActiveProjetiles.Count - 1; x >= 0; x--) {

                var p = ActiveProjetiles[x];

                var info = p.Info;
                var aConst = info.AmmoDef.Const;
                var stepSize = info.DistanceTraveled - info.PrevDistanceTraveled;


                if (aConst.VirtualBeams && !p.Intersecting) {
                    AvShot.UpdateVirtualBeams(p, info, null, p.Beam.To, Vector3D.Zero, false);
                    continue;
                }

                if (!p.EnableAv) continue;

                if (p.Intersecting) {

                    if (aConst.DrawLine || aConst.PrimeModel || aConst.TriggerModel)
                    {
                        var useCollisionSize = !info.AvShot.HasModel && aConst.AmmoParticle && !aConst.DrawLine;
                        info.AvShot.TestSphere.Center = info.ProHit.LastHit;
                        info.AvShot.ShortStepAvUpdate(p, useCollisionSize, true, p.EndState == EndStates.EarlyEnd, p.Position);
                    }

                    if (info.BaseDamagePool <= 0 || p.State == ProjectileState.Depleted)
                        info.AvShot.ProEnded = true;

                    p.Intersecting = false;
                    continue;
                }

                if ((int)p.State > 3)
                    continue;

                if (aConst.DrawLine || !info.AvShot.HasModel && aConst.AmmoParticle)
                {
                    if (aConst.IsBeamWeapon)
                    {
                        info.AvShot.StepSize = info.MaxTrajectory;
                        info.AvShot.VisualLength = info.MaxTrajectory;

                        DeferedAvDraw.Add(new DeferedAv { AvShot = info.AvShot, Info = info, TracerFront = p.Position,  Direction = p.Direction });
                    }
                    else if (!info.AvShot.HasModel && aConst.AmmoParticle && !aConst.DrawLine)
                    {
                        if (p.EndState != EndStates.None)
                        {
                            var earlyEnd = p.EndState > (EndStates)1;
                            info.AvShot.ShortStepAvUpdate(p,true, false, earlyEnd, p.Position);
                        }
                        else
                        {
                            info.AvShot.StepSize = stepSize;
                            info.AvShot.VisualLength = aConst.CollisionSize;
                            DeferedAvDraw.Add(new DeferedAv { AvShot = info.AvShot, Info = info, TracerFront = p.Position,  Direction = p.Direction, });
                        }
                    }
                    else
                    {
                        var dir = (p.Velocity - info.ShooterVel) * Session.I.DeltaStepConst;
                        double distChanged;
                        Vector3D.Dot(ref p.Direction, ref dir, out distChanged);

                        info.ProjectileDisplacement += Math.Abs(distChanged);
                        var displaceDiff = info.ProjectileDisplacement - info.TracerLength;
                        if (info.ProjectileDisplacement < info.TracerLength && Math.Abs(displaceDiff) > 0.0001)
                        {
                            if (p.EndState != EndStates.None)
                            {
                                var earlyEnd = p.EndState > (EndStates) 1;
                                p.Info.AvShot.ShortStepAvUpdate(p,false, false, earlyEnd, p.Position);
                            }
                            else
                            {
                                info.AvShot.StepSize = stepSize;
                                info.AvShot.VisualLength = info.ProjectileDisplacement;
                                DeferedAvDraw.Add(new DeferedAv { AvShot = p.Info.AvShot, Info = info, TracerFront = p.Position,  Direction = p.Direction });
                            }
                        }
                        else
                        {
                            if (p.EndState != EndStates.None)
                            {
                                var earlyEnd = p.EndState > (EndStates)1;
                                info.AvShot.ShortStepAvUpdate(p, false, false, earlyEnd, p.Position);
                            }
                            else
                            {
                                info.AvShot.StepSize = stepSize;
                                info.AvShot.VisualLength = info.TracerLength;
                                DeferedAvDraw.Add(new DeferedAv { AvShot = info.AvShot, Info = info, TracerFront = p.Position,  Direction = p.Direction });
                            }
                        }
                    }
                }

                if (info.AvShot.ModelOnly)
                {
                    info.AvShot.StepSize = stepSize;
                    info.AvShot.VisualLength = info.TracerLength;
                    DeferedAvDraw.Add(new DeferedAv { AvShot = info.AvShot, Info = info, TracerFront = p.Position, Direction = p.Direction });
                }
            }
        }
    }
}
