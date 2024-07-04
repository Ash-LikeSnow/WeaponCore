using System;
using System.Collections.Generic;
using CoreSystems.Projectiles;
using CoreSystems.Support;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Lights;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using VRageRender.Lights;
using static CoreSystems.Session;
using static CoreSystems.Support.WeaponDefinition.AnimationDef.PartAnimationSetDef;
using static VRage.Game.ObjectBuilders.Definitions.MyObjectBuilder_GameDefinition;

namespace CoreSystems.Platform
{
    public partial class Weapon : Part
    {
        internal volatile bool Casting;
        private readonly int _numOfMuzzles;
        private readonly int _numModelBarrels;
        private readonly HashSet<string> _muzzlesToFire = new HashSet<string>();
        private readonly HashSet<string> _muzzlesFiring = new HashSet<string>();
        internal readonly Dictionary<int, string> MuzzleIdToName = new Dictionary<int, string>();
        internal readonly Dictionary<ulong, ClientProSync> WeaponProSyncs = new Dictionary<ulong, ClientProSync>();
        internal readonly Dictionary<string, PartAnimation> AnimationLookup = new Dictionary<string, PartAnimation>();
        internal readonly Dictionary<MyEntity, HiddenInfo> HiddenTargets = new Dictionary<MyEntity, HiddenInfo>();
        internal readonly Dictionary<ulong, Projectile> ProjectileSyncMonitor = new Dictionary<ulong, Projectile>();
        internal readonly List<MyCubeBlock> Top5 = new List<MyCubeBlock>();
        internal readonly HashSet<Weapon> Connections = new HashSet<Weapon>();
        internal readonly WeaponFrameCache WeaponCache = new WeaponFrameCache();
        internal readonly ProtoProPositionSync ProPositionSync = new ProtoProPositionSync();
        internal readonly ProtoProTargetSync ProTargetSync = new ProtoProTargetSync();
        internal readonly JerkRunningAverage JerkRunningAverage = new JerkRunningAverage(30);

        internal readonly ApiShootRequest ShootRequest;
        internal readonly WeaponSystem System;
        internal readonly Target Target;
        internal readonly Target NewTarget;
        internal readonly PartInfo MuzzlePart;
        internal readonly Dummy[] Dummies;
        internal readonly Dummy Ejector;
        internal readonly Dummy Scope;
        internal readonly Muzzle[] Muzzles;
        internal readonly PartInfo AzimuthPart;
        internal readonly PartInfo ElevationPart;
        internal readonly PartInfo SpinPart;
        internal readonly Dictionary<EventTriggers, ParticleEvent[]> ParticleEvents;
        internal readonly uint[] BeamSlot;
        internal readonly MyParticleEffect[] Effects1;
        internal readonly MyParticleEffect[] Effects2;
        internal readonly MyParticleEffect[] HitEffects;
        internal readonly WeaponComponent Comp;
        internal readonly MyEntity3DSoundEmitter ReloadEmitter;
        internal readonly MyEntity3DSoundEmitter PreFiringEmitter;
        internal readonly MyEntity3DSoundEmitter FiringEmitter;
        internal readonly MyEntity3DSoundEmitter BarrelRotateEmitter;
        internal readonly MyEntity3DSoundEmitter HardPointEmitter;

        internal readonly Dictionary<EventTriggers, PartAnimation[]> AnimationsSet;
        internal readonly bool PrimaryWeaponGroup;
        internal readonly bool AiOnlyWeapon;
        internal readonly bool HasHardPointSound;
        
        internal WeaponDefinition.TargetingDef.BlockTypes LastTop5BlockType;

        private int _nextVirtual;
        private uint _ticksUntilShoot;
        private uint _spinUpTick;
        private uint _ticksBeforeSpinUp;


        internal uint LastMagSeenTick;
        internal uint GravityTick;
        internal uint LastShootTick;
        internal uint TicksPerShot;
        internal uint ElevationTick;
        internal uint AzimuthTick;
        internal uint FastTargetResetTick;
        internal uint LastNanTick;
        internal uint OverHeatCountDown;
        internal uint DelayedTargetResetTick;
        internal int AcquireAttempts;
        internal float HeatPerc;
        internal int FailedAcquires;
        internal int BarrelRate;
        internal int ShotsFired;
        internal int NextMuzzle;
        internal int MiddleMuzzleIndex;
        internal int DelayedCycleId = -1;
        internal int PosChangedTick = -1;
        internal List<MyEntity> HeatingParts;
        internal Vector3D GravityPoint;
        internal Vector3D GravityUnitDir;
        internal Vector3D MyPivotPos;
        internal Vector3D BarrelOrigin;
        internal Vector3D MyPivotFwd;
        internal Vector3D MyPivotUp;
        internal Vector3D AimOffset;
        internal Vector3D AzimuthInitFwdDir;
        internal MatrixD WeaponConstMatrix;

        internal LineD MyCenterTestLine;
        internal LineD MyBarrelTestLine;
        internal LineD MyPivotTestLine;
        internal LineD MyAimTestLine;
        internal LineD MyShootAlignmentLine;
        internal LineD AzimuthFwdLine;
        internal XorShiftRandomStruct XorRnd;

        internal MyOrientedBoundingBoxD TargetBox;
        internal LineD LimitLine;


        internal MathFuncs.Cone AimCone;
        internal Matrix[] BarrelRotationPerShot = new Matrix[10];

        internal string FriendlyName = string.Empty;
        internal string FriendlyNameNoAmmo = string.Empty;
        internal string FriendlyNameNoTarget = string.Empty;
        internal string FriendlyNameNoSubsystem = string.Empty;
        internal string FriendlyNameImpossibleHit = string.Empty;

        internal string AmmoName = "";
        internal ProtoWeaponPartState PartState;
        internal ProtoWeaponReload Reload;
        internal ProtoWeaponTransferTarget TargetData;
        internal ProtoWeaponAmmo ProtoWeaponAmmo;
        internal WeaponSystem.AmmoType ActiveAmmoDef;
        internal int[] AmmoShufflePattern = {0};
        internal ParallelRayCallBack RayCallBack;
        
        internal IHitInfo LastHitInfo;
        internal EventTriggers LastEvent;
        internal EventTriggers PrevRangeEvent = EventTriggers.TargetRanged100;
        internal float ShotEnergyCost;
        internal float LastHeat;
        internal ushort ProjectileCounter;
        internal uint LastFriendlyNameTick;
        internal uint TargetAcquireTick = uint.MaxValue;
        internal uint ReloadEndTick = uint.MaxValue;
        internal uint CeaseFireDelayTick = uint.MaxValue / 2;
        internal uint LastTargetTick;
        internal uint LastTrackedTick;
        internal uint LastMuzzleCheck;
        internal uint LastSmartLosCheck;
        internal uint LastLoadedTick;
        internal uint OffDelay;
        internal uint AnimationDelayTick;
        internal uint TrackingDelayTick;
        internal uint LastHeatUpdateTick;
        internal uint LastInventoryTick;
        internal uint StopBarrelAvTick;
        internal int LiveSmarts;
        internal int ProposedAmmoId = -1;
        internal int ShootCount;
        internal int FireCounter;
        internal int RateOfFire;
        internal float BaseDamageMult = 1;
        internal float AreaDamageMult = 1;
        internal float AreaRadiusMult = 1;
        internal float VelocityMult = 1;
        internal bool FiringAllowed = true;
        internal int BarrelSpinRate;
        internal int EnergyPriority;
        internal int LastBlockCount;
        internal ushort ClientStartId;
        internal ushort ClientEndId;
        internal int ClientMakeUpShots;
        internal int ClientLastShotId;
        internal int LookAtFailCount;
        internal float HeatPShot;
        internal float HsRate;
        internal float CurrentAmmoVolume;
        internal double Azimuth;
        internal double Elevation;
        internal double AimingTolerance;
        internal double MaxAzToleranceRadians;
        internal double MinAzToleranceRadians;
        internal double MaxElToleranceRadians;
        internal double MinElToleranceRadians;
        internal double MaxAzimuthRadians;
        internal double MinAzimuthRadians;
        internal double MaxElevationRadians;
        internal double MinElevationRadians;
        internal double MaxTargetDistance;
        internal double MaxTargetDistanceSqr;
        internal double MaxTargetDistance75Sqr;
        internal double MaxTargetDistance50Sqr;
        internal double MaxTargetDistance25Sqr;
        internal double ShootTime;

        internal double MinTargetDistance;
        internal double MinTargetDistanceSqr;
        internal double MinTargetDistanceBufferSqr;
        internal double MuzzleDistToBarrelCenter;
        internal double ScopeDistToCheckPos;
        internal double GravityLength;
        internal bool AcquiredBlock;
        internal bool RangeEventActive;
        internal bool AlternateForward;
        internal bool BurstAvDelay;
        internal bool HeatLoopRunning;
        internal bool PreFired;
        internal bool FinishShots;
        internal bool ScheduleAmmoChange;
        internal bool CriticalReaction;
        internal bool FoundTopMostTarget;
        internal bool OutOfAmmo;
        internal bool TurretActive;
        internal bool TargetLock;
        internal bool ClientReloading;
        internal bool ServerQueuedAmmo;
        internal bool Rotating;
        internal bool TurretAttached;
        internal bool TurretController;
        internal bool AiShooting;
        internal bool IsShooting;
        internal bool PlayTurretAv;
        internal bool AvCapable;
        internal bool NoMagsToLoad;
        internal bool CurrentlyDegrading;
        internal bool FixedOffset;
        internal bool ProjectilesNear;
        internal bool BarrelSpinning;
        internal bool ReturingHome;
        internal bool IsHome = true;
        internal bool CanUseEnergyAmmo;
        internal bool CanUseHybridAmmo;
        internal bool CanUseChargeAmmo;
        internal bool CanUseBeams;
        internal bool PauseShoot;
        internal bool ShowReload;
        internal bool ParentIsSubpart;
        internal bool CheckInventorySystem = true;
        internal bool PlayingHardPointSound;
        internal bool VanillaTracking;
        internal bool RotorTurretTracking;

        internal bool ShotReady
        {
            get
            {
                var reloading = ActiveAmmoDef.AmmoDef.Const.Reloadable && ClientMakeUpShots == 0 && (Loading || ProtoWeaponAmmo.CurrentAmmo == 0 || Reload.WaitForClient);
                var overHeat = PartState.Overheated && OverHeatCountDown == 0;
                var canShoot = !overHeat && !reloading ;
                var shotReady = canShoot;
                return shotReady;
            }
        }

        internal bool LoadingWait => ReloadEndTick < uint.MaxValue - 1;
        internal Dummy GetScope => Scope ?? Dummies[MiddleMuzzleIndex];
        internal Weapon(MyEntity entity, WeaponSystem system, int partId, WeaponComponent comp, RecursiveSubparts parts, MyEntity elevationPart, MyEntity azimuthPart, MyEntity spinPart, string azimuthPartName, string elevationPartName)
        {
            Comp = comp;
            System = system;
            Init(comp, system, partId);
            MyStringHash subtype;
            if (Session.I.VanillaIds.TryGetValue(comp.Id, out subtype)) {
                if (subtype.String.Contains("Gatling"))
                    _numModelBarrels = 6;
                else
                    _numModelBarrels = System.Muzzles.Length;
            }
            else
                _numModelBarrels = System.Muzzles.Length;

            bool hitParticle = false;
            foreach (var ammoType in System.AmmoTypes)
            {
                var c = ammoType.AmmoDef.Const;
                if (c.EnergyAmmo) CanUseEnergyAmmo = true;
                if (c.IsHybrid) CanUseHybridAmmo = true;
                if (c.MustCharge) CanUseChargeAmmo = true;
                if (c.IsBeamWeapon) CanUseBeams = true;
                if (c.HitParticle) hitParticle = true;
            }

            comp.HasEnergyWeapon = comp.HasEnergyWeapon || CanUseEnergyAmmo || CanUseHybridAmmo;

            AvCapable = (System.HasBarrelShootAv || hitParticle) && !Session.I.DedicatedServer;

            if (AvCapable && system.FiringSound == WeaponSystem.FiringSoundState.WhenDone)
            {
                FiringEmitter = Session.I.Emitters.Count > 0 ? Session.I.Emitters.Pop() : new MyEntity3DSoundEmitter(null);
                FiringEmitter.CanPlayLoopSounds = true;
                FiringEmitter.Entity = Comp.CoreEntity;
            }

            if (AvCapable && system.PreFireSound)
            {
                PreFiringEmitter = Session.I.Emitters.Count > 0 ? Session.I.Emitters.Pop() : new MyEntity3DSoundEmitter(null);
                PreFiringEmitter.CanPlayLoopSounds = true;

                PreFiringEmitter.Entity = Comp.CoreEntity;
            }

            if (AvCapable && system.WeaponReloadSound)
            {
                ReloadEmitter = Session.I.Emitters.Count > 0 ? Session.I.Emitters.Pop() : new MyEntity3DSoundEmitter(null);
                ReloadEmitter.CanPlayLoopSounds = true;

                ReloadEmitter.Entity = Comp.CoreEntity;
            }

            if (AvCapable && system.BarrelRotateSound)
            {
                BarrelRotateEmitter = Session.I.Emitters.Count > 0 ? Session.I.Emitters.Pop() : new MyEntity3DSoundEmitter(null);
                BarrelRotateEmitter.CanPlayLoopSounds = true;

                BarrelRotateEmitter.Entity = Comp.CoreEntity;
            }

            if (AvCapable)
            {
                if (System.BarrelEffect1)
                    Effects1 = new MyParticleEffect[System.Values.Assignments.Muzzles.Length];
                if (System.BarrelEffect2)
                    Effects2 = new MyParticleEffect[System.Values.Assignments.Muzzles.Length];
                if (hitParticle && CanUseBeams)
                    HitEffects = new MyParticleEffect[System.Values.Assignments.Muzzles.Length];
            }

            if (System.TurretMovement != WeaponSystem.TurretType.Fixed)
                Comp.HasAim = true;

            PrimaryWeaponGroup = PartId % 2 == 0;
            TurretAttached = System.Values.HardPoint.Ai.TurretAttached;
            TurretController = System.Values.HardPoint.Ai.TurretController;

            AimOffset = System.Values.HardPoint.HardWare.Offset;
            FixedOffset = System.Values.HardPoint.HardWare.FixedOffset;

            HsRate = system.WConst.HeatSinkRate / 3;
            EnergyPriority = System.Values.HardPoint.Other.EnergyPriority;
            var toleranceInRadians = System.WConst.AimingToleranceRads;
            AimCone.ConeAngle = toleranceInRadians;
            AimingTolerance = Math.Cos(toleranceInRadians);

            if (Comp.Platform.Structure.PrimaryPart == partId)
                comp.PrimaryWeapon = this;

            if (TurretAttached && !System.TrackTargets)
                Target = comp.PrimaryWeapon.Target;
            else Target = new Target(this);

            _numOfMuzzles = System.Muzzles.Length;

            ShootRequest = new ApiShootRequest(this);
            BeamSlot = new uint[_numOfMuzzles];
            Muzzles = new Muzzle[_numOfMuzzles];
            Dummies = new Dummy[_numOfMuzzles];
            NewTarget = new Target();
            RayCallBack = new ParallelRayCallBack(this);
            Acquire = new PartAcquire(this);
            AzimuthPart = new PartInfo(azimuthPart, azimuthPart == Comp.CoreEntity, azimuthPart?.Parent == Comp.CoreEntity, PartInfo.PartTypes.Az);
            ElevationPart = new PartInfo(elevationPart, elevationPart == Comp.CoreEntity, elevationPart?.Parent == Comp.CoreEntity, PartInfo.PartTypes.El);
            SpinPart = System.HasBarrelRotation ? new PartInfo(spinPart, spinPart == Comp.CoreEntity, spinPart?.Parent == Comp.CoreEntity, PartInfo.PartTypes.Spin) : null;

            MuzzlePart = new PartInfo(entity, entity == Comp.CoreEntity, entity?.Parent == Comp.CoreEntity, PartInfo.PartTypes.Muzzle);
            MiddleMuzzleIndex = Muzzles.Length > 1 ? Muzzles.Length / 2 - 1 : 0;

            AnimationsSet = CreateWeaponAnimationSet(system, parts);

            try
            {
                foreach (var set in AnimationsSet)
                {
                    foreach (var pa in set.Value)
                    {
                        var modifiesCore = pa.Part == azimuthPart || pa.Part == elevationPart || pa.Part == spinPart ||
                                           pa.Part == entity;
                        if (modifiesCore && (pa.HasMovement || pa.MovesPivotPos))
                        {
                            Comp.AnimationsModifyCoreParts = true;
                            if (!Session.I.DedicatedServer && Session.I.PerformanceWarning.Add(Comp.SubTypeId))
                                Log.Line($"{Comp.SubtypeName} - {System.PartName} - Animation modifies core subparts, performance impact");
                        }

                        comp.AllAnimations.Add(pa);
                        AnimationLookup.Add(pa.AnimationId, pa);
                    }
                }
            }
            catch (Exception ex) { Log.Line($"Exception in {comp.SubtypeName} AnimationsSet: {ex}", null, true); }



            ParticleEvents = CreateWeaponParticleEvents(system, parts);

            var burstDelay = System.Values.HardPoint.Loading.DelayAfterBurst;
            ShowReload = Session.I.HandlesInput && (System.WConst.ReloadTime >= 60 || System.Values.HardPoint.Loading.ShotsInBurst > 0 && burstDelay >= 60);

            ParentIsSubpart = azimuthPart.Parent is MyEntitySubpart;
            AzimuthInitFwdDir = azimuthPart.PositionComp.LocalMatrixRef.Forward;
            
            FuckMyLife();
            
            AiOnlyWeapon = Comp.TypeSpecific != CoreComponent.CompTypeSpecific.VanillaTurret || (Comp.TypeSpecific == CoreComponent.CompTypeSpecific.VanillaTurret && (azimuthPartName != "MissileTurretBase1" && elevationPartName != "MissileTurretBarrels" && azimuthPartName != "InteriorTurretBase1" && elevationPartName != "InteriorTurretBase2" && azimuthPartName != "GatlingTurretBase1" && elevationPartName != "GatlingTurretBase2"));
            if (comp.TypeSpecific == CoreComponent.CompTypeSpecific.SearchLight) AiOnlyWeapon = false; //FML there's got to be a way to roll this into the line above
            VanillaTracking = TurretAttached && !AiOnlyWeapon;

            CriticalReaction = Comp.TypeSpecific != CoreComponent.CompTypeSpecific.Phantom && System.Values.HardPoint.HardWare.CriticalReaction.Enable;

            string ejectorMatch;
            MyEntity ejectorPart;
            if (System.HasEjector && Comp.Platform.Parts.FindFirstDummyByName(System.Values.Assignments.Ejector, System.AltEjectorName, out ejectorPart, out ejectorMatch))
                Ejector = new Dummy(ejectorPart, this, true, System.Values.Assignments.Ejector);

            string scopeMatch;
            MyEntity scopePart;
            if (System.HasScope && Comp.Platform.Parts.FindFirstDummyByName(System.Values.Assignments.Scope, System.AltScopeName, out scopePart, out scopeMatch))
                Scope = new Dummy(scopePart, this, false, scopeMatch);

            comp.Platform.SetupWeaponUi(this);

            if (!comp.Debug && System.Values.HardPoint.Other.Debug)
                comp.Debug = true;

            var hasHardPointSound = false;
            if (TurretController)
            {
                if (System.Values.HardPoint.Ai.PrimaryTracking && comp.PrimaryWeapon == null)
                    comp.PrimaryWeapon = this;

                if (AvCapable && System.HardPointRotationSound && (comp.PrimaryWeapon == this || !System.Values.HardPoint.Ai.PrimaryTracking))
                {
                    hasHardPointSound = true;
                    HardPointEmitter = Session.I.Emitters.Count > 0 ? Session.I.Emitters.Pop() : new MyEntity3DSoundEmitter(null);
                    HardPointEmitter.CanPlayLoopSounds = true;

                    HardPointEmitter.Entity = Comp.CoreEntity;
                }
            }

            HasHardPointSound = hasHardPointSound;

            if (System.HasAntiSmart)
                Session.I.AntiSmartActive = true;

            if (Session.I.HandlesInput && Comp.IsBlock)
            {
                //InitLight(Color.Red, 99, 1, out Light);
            }
        }

        private void InitLight(Vector4 color, float radius, float falloff, out MyLight light)
        {
            var cube = Comp.Cube;

            light = new MyLight();
            light.Start(color, cube.CubeGrid.GridScale * radius, cube.DisplayNameText);
            light.ReflectorOn = true;
            light.LightType = MyLightType.SPOTLIGHT;
            light.ReflectorTexture = @"Textures\Lights\reflector_large.dds"; ;
            light.Falloff = 0.3f;
            light.GlossFactor = 0.0f;
            light.ReflectorGlossFactor = 1f;
            light.ReflectorFalloff = 0.5f;
            light.GlareOn = light.LightOn;
            light.GlareQuerySize = GlareQuerySizeDef;
            light.GlareType = MyGlareTypeEnum.Directional;
            light.GlareSize = _flare.Size;
            light.SubGlares = _flare.SubGlares;

            //light.ReflectorIntensity = 10f;
            //light.ReflectorRange = 100; // how far the projected light goes
            //light.ReflectorConeDegrees = 90; // projected light angle in degrees, max 179.
            //light.CastShadows = true;
           

            cube.Render.NeedsDrawFromParent = true;
            light.Position = Scope.Info.Position + (Scope.Info.Direction * 1);
            light.UpdateLight();

            //light.GlareSize = new Vector2(1, 1); // glare size in X and Y.
            //light.GlareIntensity = 2;
            //light.GlareMaxDistance = 50;
            //light.SubGlares = GetFlareDefinition("InteriorLight").SubGlares; // subtype name from flares.sbc
            //light.GlareType = MyGlareTypeEnum.Normal; // usable values: MyGlareTypeEnum.Normal, MyGlareTypeEnum.Distant, MyGlareTypeEnum.Directional
            //light.GlareQuerySize = 0.5f; // glare "box" size, affects occlusion and fade occlussion
            //light.GlareQueryShift = 1f; // no idea
            //light.ParentID = cube.Render.GetRenderObjectID();
            //this.UpdateIntensity();
        }
        
        public static MyFlareDefinition GetFlareDefinition(string flareSubtypeId)
        {
            if (string.IsNullOrEmpty(flareSubtypeId))
                throw new ArgumentException("flareSubtypeId must not be null or empty!");

            var flareDefId = new MyDefinitionId(typeof(MyObjectBuilder_FlareDefinition), flareSubtypeId);
            var flareDef = MyDefinitionManager.Static.GetDefinition(flareDefId) as MyFlareDefinition;

            if (flareDef == null)
                throw new Exception($"Couldn't find flare subtype {flareSubtypeId}");

            return flareDef;
        }

        private readonly MyFlareDefinition _flare = new MyFlareDefinition() ;
        private float GlareQuerySizeDef => Comp.Cube.CubeGrid.GridScale * (true ? 0.5f : 0.1f);

        /*
        private void UpdateIntensity()
        {
            float num1 = this.m_lightingLogic.CurrentLightPower * this.m_lightingLogic.Intensity;
            foreach (MyLight light in this.m_lightingLogic.Lights)
            {
                light.ReflectorIntensity = num1 * 8f;
                light.Intensity = num1 * 0.3f;
                float num2 = num1 / this.m_lightingLogic.IntensityBounds.Max;
                float num3 = this.m_flare.Intensity * num1;
                if (num3 < (double)this.m_flare.Intensity)
                    num3 = this.m_flare.Intensity;
                light.GlareIntensity = num3;
                light.GlareSize = this.m_flare.Size * (float)((double)num2 / 2.0 + 0.5);
                this.m_lightingLogic.BulbColor = this.m_lightingLogic.ComputeBulbColor();
            }
        }
        */
        private void FuckMyLife()
        {
            var azPartMatrix = AzimuthPart.Entity.PositionComp.LocalMatrixRef;
            
            var fwdX = Math.Abs(azPartMatrix.Forward.X);
            var fwdY = Math.Abs(azPartMatrix.Forward.Y);
            var fwdZ = Math.Abs(azPartMatrix.Forward.Z);

            var fwdXAngle = !MyUtils.IsEqual(fwdX, 1f) && !MyUtils.IsZero(fwdX);
            var fwdYAngle = !MyUtils.IsEqual(fwdY, 1f) && !MyUtils.IsZero(fwdY);
            var fwdZAngle = !MyUtils.IsEqual(fwdZ, 1f) && !MyUtils.IsZero(fwdZ);

            var fwdAngled = fwdXAngle || fwdYAngle || fwdZAngle; 

            var upX = Math.Abs(azPartMatrix.Up.X);
            var upY = Math.Abs(azPartMatrix.Up.Y);
            var upZ = Math.Abs(azPartMatrix.Up.Z);

            var upXAngle = !MyUtils.IsEqual(upX, 1f) && !MyUtils.IsZero(upX);
            var upYAngle = !MyUtils.IsEqual(upY, 1f) && !MyUtils.IsZero(upY);
            var upZAngle = !MyUtils.IsEqual(upZ, 1f) && !MyUtils.IsZero(upZ);
           
            var upAngled = upXAngle || upYAngle || upZAngle;

            var leftX = Math.Abs(azPartMatrix.Up.X);
            var leftY = Math.Abs(azPartMatrix.Up.Y);
            var leftZ = Math.Abs(azPartMatrix.Up.Z);

            var leftXAngle = !MyUtils.IsEqual(leftX, 1f) && !MyUtils.IsZero(leftX);
            var leftYAngle = !MyUtils.IsEqual(leftY, 1f) && !MyUtils.IsZero(leftY);
            var leftZAngle = !MyUtils.IsEqual(leftZ, 1f) && !MyUtils.IsZero(leftZ);
            
            var leftAngled = leftXAngle || leftYAngle || leftZAngle;

            if (fwdAngled || upAngled || leftAngled)
                AlternateForward = true;
        }
    }
}
