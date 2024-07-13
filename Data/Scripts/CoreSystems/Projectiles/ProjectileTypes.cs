using System;
using System.Collections.Generic;
using CoreSystems.Platform;
using CoreSystems.Projectiles;
using Sandbox.Game.Entities;
using VRage;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRageMath;
using static CoreSystems.Support.HitEntity.Type;
using static CoreSystems.Support.WeaponDefinition;
using static CoreSystems.Support.Ai;

namespace CoreSystems.Support
{
    public class ProInfo
    {
        internal readonly Target Target = new Target();
        internal readonly SmartStorage Storage = new SmartStorage();
        internal readonly List<HitEntity> HitList = new List<HitEntity>();
        internal readonly ProHit ProHit = new ProHit();
        internal List<MyTuple<Vector3D, object, float>> ProHits;
        internal int[] PatternShuffle;
        internal object LastTarget;
        internal AvShot AvShot;
        internal Weapon Weapon;
        internal Ai Ai;
        internal AmmoDef AmmoDef;
        internal MyPlanet MyPlanet;
        internal VoxelCache VoxelCache;
        internal Vector3D ShooterVel;
        internal Vector3D Origin;
        internal Vector3D OriginUp;
        internal Vector3D OriginFwd;
        internal Vector3D TotalAcceleration;
        internal XorShiftRandomStruct Random;
        internal XorShiftRandomStruct ShieldProc;
        internal int Age = -1;
        internal int TriggerGrowthSteps;
        internal int MuzzleId;
        internal int ObjectsHit;
        internal int Frags;
        internal int CompSceneVersion;
        internal ulong UniqueMuzzleId;
        internal ulong Id;
        internal ulong SyncId = ulong.MaxValue;

        internal double DistanceTraveled;
        internal double PrevDistanceTraveled;
        internal double ProjectileDisplacement;
        internal double TracerLength;
        internal double MaxTrajectory;
        internal double LastFragTime;
        internal double ShotFade;
        internal double RelativeAge = -1;
        internal double PrevRelativeAge = -1;
        internal long DamageDonePri;
        internal long DamageDoneAoe;
        internal long DamageDoneShld;
        internal long DamageDoneProj;
        internal long LastTopTargetId;
        internal long FactionId;
        internal float BaseDamagePool;
        internal float BaseHealthPool;
        internal float BaseEwarPool;
        internal bool IsFragment;
        internal bool ExpandingEwarField;
        internal bool EwarActive;
        internal bool AcquiredEntity;
        internal bool AimedShot;
        internal bool DoDamage;
        internal bool ShieldBypassed;
        internal bool ShieldKeepBypass;
        internal bool ShieldInLine;
        internal uint FirstWaterHitTick;
        internal float ShieldResistMod = 1f;
        internal float ShieldBypassMod = 1f;
        internal ushort SyncedFrags;
        internal ushort SpawnDepth;
        internal MatrixD TriggerMatrix = MatrixD.Identity;

        internal void InitVirtual(Weapon weapon, AmmoDef ammodef,  Weapon.Muzzle muzzle, double maxTrajectory, double shotFade)
        {
            Weapon = weapon;
            Ai = weapon.BaseComp.MasterAi;
            MyPlanet = Ai.MyPlanet;
            AmmoDef = ammodef;
            Target.TargetObject = weapon.Target.TargetObject;
            MuzzleId = muzzle.MuzzleId;
            UniqueMuzzleId = muzzle.UniqueId;
            Origin = muzzle.Position;
            OriginFwd = muzzle.DeviatedDir;
            MaxTrajectory = maxTrajectory;
            ShotFade = shotFade;
        }

        internal void Clean()
        {
            var aConst = AmmoDef.Const;

            var monitor = Weapon.Comp.ProjectileMonitors[Weapon.PartId];
            if (monitor?.Count > 0) {
                for (int i = 0; i < monitor.Count; i++)
                    monitor[i].Invoke(Weapon.Comp.CoreEntity.EntityId, Weapon.PartId, Id, Target.TargetId, ProHit.LastHit, false);

                Session.I.MonitoredProjectiles.Remove(Id);
            }

            if (ProHits != null) {
                ProHits.Clear();
                Session.I.ProHitPool.Push(ProHits);
            }

            Target.Reset(Session.I.Tick, Target.States.ProjectileClean);
            HitList.Clear();
            
            if(aConst.IsGuided)
                Storage.Clean(this);


            SyncId = ulong.MaxValue;


            if (IsFragment)
            {
                if (VoxelCache != null)
                {
                    Session.I.UniqueMuzzleId = VoxelCache;
                }
            }

            if (PatternShuffle != null)
            {
                for (int i = 0; i < PatternShuffle.Length; i++)
                    PatternShuffle[i] = i;

                aConst.PatternShuffleArray.Push(PatternShuffle);
                PatternShuffle = null;
            }

            AvShot = null;
            Ai = null;
            MyPlanet = null;
            AmmoDef = null;
            VoxelCache = null;
            LastTarget = null;
            Weapon = null;
            IsFragment = false;
            ExpandingEwarField = false;
            EwarActive = false;
            AcquiredEntity = false;
            AimedShot = false;
            DoDamage = false;
            ShieldBypassed = false;
            ShieldInLine = false;
            ShieldKeepBypass = false;
            FirstWaterHitTick = 0;
            TriggerGrowthSteps = 0;
            SpawnDepth = 0;
            Frags = 0;
            MuzzleId = 0;
            LastTopTargetId = 0;
            FactionId = 0;
            Age = -1;
            RelativeAge = -1;
            PrevRelativeAge = -1;
            DamageDonePri = 0;
            DamageDoneAoe = 0;
            DamageDoneShld = 0;
            DamageDoneProj = 0;
            SyncedFrags = 0;
            ProjectileDisplacement = 0;
            MaxTrajectory = 0;
            ShotFade = 0;
            TracerLength = 0;
            UniqueMuzzleId = 0;
            LastFragTime = 0;
            ShieldResistMod = 1f;
            ShieldBypassMod = 1f;
            ProHit.LastHit = Vector3D.Zero;
            ProHit.Entity = null;
            Origin = Vector3D.Zero;
            OriginFwd = Vector3D.Zero;
            ShooterVel = Vector3D.Zero;
            TriggerMatrix = MatrixD.Identity;
            TotalAcceleration = Vector3D.Zero;

        }
    }

    internal class SmartStorage
    {
        internal ClosestObstacles Obstacle;
        internal FullSyncInfo FullSyncInfo;
        internal FakeTargets DummyTargets;
        internal ApproachInfo ApproachInfo;
        internal DroneInfo DroneInfo;
        internal Vector3D RandOffsetDir;
        internal Vector3D? LastVelocity;
        internal bool SmartReady;
        internal bool WasTracking;
        internal bool Sleep;
        internal bool PickTarget;
        internal int ChaseAge;
        internal int LastOffsetTime;
        internal int SmartSlot;
        internal int LastActivatedStage = -1;
        internal int RequestedStage = -1;
        internal double ZombieLifeTime;
        internal double PrevZombieLifeTime;

        internal void Clean(ProInfo info)
        {
            LastActivatedStage = -1;
            RequestedStage = -1;

            ChaseAge = 0;
            ZombieLifeTime = 0;
            PrevZombieLifeTime = 0;
            LastOffsetTime = 0;
            SmartSlot = 0;

            RandOffsetDir = Vector3D.Zero;

            DummyTargets = null;
            LastVelocity = null;
            SmartReady = false;
            WasTracking = false;
            PickTarget = false;

            Sleep = false;

            if (!info.AmmoDef.Const.FullSync && info.SyncId != ulong.MaxValue)
                info.Weapon.ProjectileSyncMonitor.Remove(info.SyncId);

            if (ApproachInfo != null)
            {
                ApproachInfo.Clean(info);
                ApproachInfo = null;
            }
            else if (DroneInfo != null)
            {
                DroneInfo.Clean();
                DroneInfo = null;
            }

            if (FullSyncInfo != null)
            {
                FullSyncInfo.Clean(info);
                FullSyncInfo = null;
            }

            if (Obstacle != null)
            {
                Obstacle.Entity = null;
                Obstacle.LastSeenTick = uint.MaxValue;
            }
        }
    }

    internal class ClosestObstacles
    {
        internal MyEntity Entity;
        internal uint LastSeenTick = uint.MaxValue;
        internal BoundingSphereD AvoidSphere;
    }

    internal class FullSyncInfo
    {
        internal readonly Vector3D[] PastProInfos = new Vector3D[30];
        internal int ProSyncPosMissCount;

        internal void Clean(ProInfo info)
        {
            for (int i = 0; i < PastProInfos.Length; i++)
                PastProInfos[i] = Vector3D.Zero;

            info.Weapon.ProjectileSyncMonitor.Remove(info.SyncId);

            ProSyncPosMissCount = 0;

            Session.I.FullSyncInfoPool.Push(this);
        }
    }

    public class ApproachInfo
    {
        internal ApproachInfo(AmmoConstants aConst)
        {
            Storage = new ApproachStorage[aConst.ApproachesCount * 2];
            for (int i = 0; i < Storage.Length; i++)
                Storage[i] = new ApproachStorage();
        }
        
        internal readonly ApproachStorage[] Storage;
        internal BoundingSphereD NavTargetBound;
        internal Vector3D TargetPos;
        internal Vector3D PositionC;
        internal Vector3D PositionB;
        internal Vector3D OffsetUpDir;
        internal Vector3D OffsetFwdDir;
        internal double RelativeAgeStart;
        internal double StartDistanceTraveled;
        internal double StartHealth;
        internal int RelativeSpawnsStart;
        internal double TargetLossTime;
        internal double AngleVariance;
        internal int ModelRotateAge;
        internal int ModelRotateMaxAge;
        internal bool Active;
        internal void Clean(ProInfo info)
        {
            for (int i = 0; i < Storage.Length; i++)
            {
                var s = Storage[i];
                s.RunCount = 0;
                s.StoredPosition = Vector3D.Zero;
            }

            TargetPos = Vector3D.Zero;
            PositionC = Vector3D.Zero;
            PositionB = Vector3D.Zero;
            OffsetUpDir = Vector3D.Zero;
            OffsetFwdDir = Vector3D.Zero;
            StartDistanceTraveled = 0;
            RelativeAgeStart = 0;
            RelativeSpawnsStart = 0;
            TargetLossTime = 0;
            AngleVariance = 0;
            ModelRotateMaxAge = 0;
            ModelRotateAge = 0;
            StartHealth = 0;
            Active = false;
            NavTargetBound = new BoundingSphereD(Vector3D.Zero, 0);
            info.AmmoDef.Const.ApproachInfoPool.Push(this);
        }

        internal class ApproachStorage
        {
            internal int RunCount;
            internal Vector3D StoredPosition;
        }
    }

    internal class DroneInfo
    {

        internal enum DroneStatus
        {
            Launch,
            Transit, //Movement from/to target area
            Approach, //Final transition between transit and orbit
            Orbit, //Orbit & shoot
            Strafe, //Nose at target movement, for PointType = direct and PointAtTarget = false
            Escape, //Move away from imminent collision
            Kamikaze,
            Return, //Return to "base"
            Dock,
        }

        internal enum DroneMission
        {
            Attack,
            Defend,
            Rtb,
        }

        internal MyEntity NavTargetEnt;
        internal BoundingSphereD NavTargetBound;
        internal Vector3D DestinationPos;

        internal DroneStatus DroneStat;
        internal DroneMission DroneMsn;

        internal void Clean()
        {
            DestinationPos = Vector3D.Zero;
            NavTargetBound = new BoundingSphereD(Vector3D.Zero, 0);
            NavTargetEnt = null;
            DroneStat = DroneStatus.Launch;
            DroneMsn = DroneMission.Attack;

            Session.I.DroneInfoPool.Push(this);
        }
    }


    internal struct DeferedVoxels
    {
        internal enum VoxelIntersectBranch
        {
            None,
            DeferedMissUpdate,
            DeferFullCheck,
            PseudoHit1,
            PseudoHit2,
        }

        internal Projectile Projectile;
        internal MyVoxelBase Voxel;
        internal VoxelIntersectBranch Branch;
        internal bool LineCheck;
    }

    public class HitEntity
    {
        public enum Type
        {
            Shield,
            Grid,
            Voxel,
            Destroyable,
            Stale,
            Projectile,
            Field,
            Effect,
            Water,
        }

        public readonly List<RootBlocks> Blocks = new List<RootBlocks>(16);
        public readonly List<Vector3I> Vector3ICache = new List<Vector3I>(16);
        public MyEntity Entity;
        public MyEntity ShieldEntity;
        internal Projectile Projectile;
        public ProInfo Info;
        public LineD Intersection;
        public bool Hit;
        public bool Miss;
        public bool SphereCheck;
        public bool DamageOverTime;
        public bool PulseTrigger;
        public bool SelfHit;
        public BoundingSphereD PruneSphere;
        public Vector3D? HitPos;
        public double? HitDist;
        public Type EventType;
        public int DamageMulti = 1;
        public Stack<HitEntity> Pool;
        public void Clean()
        {
            Vector3ICache.Clear();
            Entity = null;
            ShieldEntity = null;
            Projectile = null;
            Blocks.Clear();
            Hit = false;
            HitPos = null;
            HitDist = null;
            Info = null;
            EventType = Stale;
            SphereCheck = false;
            DamageOverTime = false;
            PulseTrigger = false;
            SelfHit = false;
            Miss = false;
            DamageMulti = 1;
            Pool.Push(this);
            Pool = null;
        }

        public struct RootBlocks
        {
            public IMySlimBlock Block;
            public Vector3I QueryPos;
        }
    }

    internal struct Hit
    {
        internal MyEntity Entity;
        internal HitEntity.Type EventType;
        internal Vector3D SurfaceHit;
        internal Vector3D LastHit;
        internal Vector3D HitVelocity;
        internal uint HitTick;
    }

    internal class ProHit
    {
        internal MyEntity Entity;
        internal Vector3D LastHit;
        internal uint EndTick;
        internal int ProcInterval;
        internal double ProcAmount;
        internal bool ProcOnVoxels;
        internal bool FragOnProc;
        internal bool DieOnEnd;
        internal bool StickOnHit;
        internal bool AlignFragtoImpactAngle;
    }

    internal class WeaponFrameCache
    {
        internal int Hits;
        internal double HitDistance;
        internal int VirutalId = -1;
        internal uint FakeCheckTick;
        internal double FakeHitDistance;
    }

    internal struct NewVirtual
    {
        internal ProInfo Info;
        internal Weapon.Muzzle Muzzle;
        internal bool Rotate;
        internal int VirtualId;
    }

    internal struct NewProjectile
    {
        internal enum Kind
        {
            Normal,
            Virtual,
            Frag,
            Client
        }

        internal Weapon.Muzzle Muzzle;
        internal AmmoDef AmmoDef;
        internal MyEntity TargetEnt;
        internal List<NewVirtual> NewVirts;
        internal Vector3D Origin;
        internal Vector3D OriginUp;
        internal Vector3D Direction;
        internal Vector3D Velocity;
        internal long PatternCycle;
        internal float MaxTrajectory;
        internal Kind Type;
    }

    internal class Fragments
    {
        internal List<Fragment> Sharpnel = new List<Fragment>();
        internal void Init(Projectile p, Stack<Fragment> fragPool, AmmoDef ammoDef, ref Vector3D newOrigin, ref Vector3D pointDir)
        {
            var info = p.Info;
            var target = info.Target;
            var aConst = info.AmmoDef.Const;
            var fragCount = p.Info.AmmoDef.Fragment.Fragments;
            var guidance = aConst.IsDrone || aConst.IsSmart;
            if (Session.I.IsClient && fragCount > 0 && info.AimedShot && aConst.ClientPredictedAmmo && !info.IsFragment)
            {
                Projectiles.Projectiles.SendClientHit(p, false);
            }

            var state = target.TargetState == Target.TargetStates.IsFake ? Target.TargetStates.IsFake :  Target.TargetStates.None;
            var targetState = info.LastTarget == null || state != Target.TargetStates.None ? state : info.LastTarget is MyEntity ? Target.TargetStates.IsEntity : Target.TargetStates.IsProjectile;

            for (int i = 0; i < fragCount; i++)
            {
                var frag = fragPool.Count > 0 ? fragPool.Pop() : new Fragment();

                frag.Weapon = info.Weapon;
                frag.Ai = info.Ai;
                frag.AmmoDef = ammoDef;
                if (guidance)
                    frag.DummyTargets = info.Storage.DummyTargets;

                frag.SyncId = info.SyncId;
                frag.SyncedFrags = ++info.SyncedFrags;

                frag.Depth = (ushort) (info.SpawnDepth + 1);

                frag.FactionId = info.FactionId;
                frag.TargetState = targetState;
                frag.TargetEntity = info.LastTarget;
                frag.TopEntityId = info.LastTopTargetId;
                frag.TargetPos = target.TargetPos;
                frag.Gravity = p.Gravity;
                frag.MuzzleId = info.MuzzleId;
                frag.Radial = aConst.FragRadial;
                frag.SceneVersion = info.CompSceneVersion;
                frag.Origin = newOrigin;
                frag.OriginUp = info.OriginUp;
                frag.Random = new XorShiftRandomStruct(info.Random.NextUInt64());
                frag.DoDamage = info.DoDamage;
                frag.PrevTargetPos = state == Target.TargetStates.IsFake ? target.TargetPos : p.TargetPosition;
                frag.Velocity = !aConst.FragDropVelocity ? p.Velocity : Vector3D.Zero;
                frag.AcquiredEntity = info.AcquiredEntity;
                frag.IgnoreShield = info.ShieldBypassed && aConst.ShieldDamageBypassMod > 0;
                var posValue = aConst.FragDegrees;
                posValue *= 0.5f;
                var randomFloat1 = (float)(frag.Random.NextDouble() * posValue) + (frag.Radial);
                var randomFloat2 = (float)(frag.Random.NextDouble() * MathHelper.TwoPi);
                var mutli = aConst.FragReverse ? -1 : 1;

                var r1Sin = Math.Sin(randomFloat1);
                var r2Sin = Math.Sin(randomFloat2);
                var r1Cos = Math.Cos(randomFloat1);
                var r2Cos = Math.Cos(randomFloat2);

                var shrapnelDir = Vector3.TransformNormal(mutli  * -new Vector3(r1Sin * r2Cos, r1Sin * r2Sin, r1Cos), Matrix.CreateFromDir(pointDir));

                frag.Direction = shrapnelDir;
                Sharpnel.Add(frag);
            }
        }

        internal void Spawn(out int spawned)
        {
            Session session = null;
            spawned = Sharpnel.Count;
            for (int i = 0; i < spawned; i++)
            {
                var frag = Sharpnel[i];
                session = Session.I;
                var p = session.Projectiles.ProjectilePool.Count > 0 ? session.Projectiles.ProjectilePool.Pop() : new Projectile();
                var info = p.Info;
                info.Weapon = frag.Weapon;

                info.Ai = frag.Ai;
                info.Id = session.Projectiles.CurrentProjectileId++;

                var aDef = frag.AmmoDef;
                var aConst = aDef.Const;
                info.AmmoDef = aDef;
                var target = info.Target;
                
                target.TargetObject = frag.TargetEntity;
                target.TargetState = frag.TargetState;
                target.TopEntityId = frag.TopEntityId;
                target.TargetPos = frag.TargetPos;

                info.FactionId = frag.FactionId;
                info.ShotFade = 0;
                info.IsFragment = true;
                info.Target.TargetPos = frag.PrevTargetPos;
                info.MuzzleId = frag.MuzzleId;
                info.UniqueMuzzleId = session.UniqueMuzzleId.Id;
                info.Origin = frag.Origin;
                info.OriginUp = frag.OriginUp;
                info.OriginFwd = frag.Direction;
                info.ShooterVel = frag.Velocity;
                info.Random = frag.Random;
                info.DoDamage = frag.DoDamage;
                info.SpawnDepth = frag.Depth;
                info.SyncedFrags = frag.SyncedFrags;
                info.BaseDamagePool = aConst.BaseDamage;
                info.AcquiredEntity = frag.AcquiredEntity;
                info.MaxTrajectory = aConst.MaxTrajectory;
                info.ShieldBypassed = frag.IgnoreShield;
                info.CompSceneVersion = frag.SceneVersion;

                p.TargetPosition = frag.PrevTargetPos;
                p.Gravity = frag.Gravity;

                if (aConst.IsDrone || aConst.IsSmart)
                    info.Storage.DummyTargets = frag.DummyTargets;

                if (session.AdvSync)
                {
                    var syncPart1 = (ushort)((frag.SyncId >> 48) & 0x000000000000FFFF);
                    var syncPart2 = (ushort)((frag.SyncId >> 32) & 0x000000000000FFFF);
                    info.SyncId = ((ulong)syncPart1 << 48) | ((ulong)syncPart2 << 32) | ((ulong)info.SyncedFrags << 16) | info.SpawnDepth;

                    if (aConst.PdDeathSync || aConst.OnHitDeathSync || aConst.FullSync)
                        p.Info.Weapon.ProjectileSyncMonitor[info.SyncId] = p;
                }

                session.Projectiles.ActiveProjetiles.Add(p);
                p.Start();
                if (aConst.Health > 0 && !aConst.IsBeamWeapon)
                    session.Projectiles.AddTargets.Add(p);

                session.Projectiles.FragmentPool.Push(frag);
            }
            session?.Projectiles.ShrapnelPool.Push(this);
            Sharpnel.Clear();
        }
    }

    internal class Fragment
    {
        public Weapon Weapon;
        public Ai Ai;
        public AmmoDef AmmoDef;
        public object TargetEntity;
        public FakeTargets DummyTargets;
        public Vector3D Origin;
        public Vector3D OriginUp;
        public Vector3D Direction;
        public Vector3D Velocity;
        public Vector3D PrevTargetPos;
        public Vector3D TargetPos;
        public Vector3 Gravity;
        public int MuzzleId;
        public ushort Depth;
        public long FactionId;

        public XorShiftRandomStruct Random;
        public bool DoDamage;
        public bool AcquiredEntity;
        public bool IgnoreShield;
        public Target.TargetStates TargetState;
        public float Radial;
        internal int SceneVersion;
        internal ulong SyncId;
        internal ushort SyncedFrags;
        internal long TopEntityId;

    }

    public struct ApproachDebug
    {
        public ApproachConstants Approach;
        public ulong ProId;
        public uint LastTick;
        public int Stage;
        public bool Start1;
        public bool Start2;
        public bool End1;
        public bool End2;
        public bool End3;
        public double TimeSinceSpawn;
        public double NextSpawn;
    }

    public class VoxelCache
    {
        internal BoundingSphereD HitSphere = new BoundingSphereD(Vector3D.Zero, 2f);
        internal BoundingSphereD MissSphere = new BoundingSphereD(Vector3D.Zero, 1.5f);
        internal BoundingSphereD PlanetSphere = new BoundingSphereD(Vector3D.Zero, 0.1f);
        internal Vector3D FirstPlanetHit;

        internal uint HitRefreshed;
        internal ulong Id;

        internal void Update(MyVoxelBase voxel, ref Vector3D? hitPos, uint tick)
        {
            var hit = hitPos ?? Vector3D.Zero;
            HitSphere.Center = hit;
            HitRefreshed = tick;
            if (voxel is MyPlanet)
            {
                double dist;
                Vector3D.DistanceSquared(ref hit, ref FirstPlanetHit, out dist);
                if (dist > 625)
                {
                    FirstPlanetHit = hit;
                    PlanetSphere.Radius = 0.1f;
                }
            }
        }

        internal void GrowPlanetCache(Vector3D hitPos)
        {
            double dist;
            Vector3D.Distance(ref PlanetSphere.Center, ref hitPos, out dist);
            PlanetSphere = new BoundingSphereD(PlanetSphere.Center, dist);
        }

        internal void DebugDraw()
        {
            DsDebugDraw.DrawSphere(HitSphere, Color.Red);
        }
    }
}
