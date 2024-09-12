using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using CoreSystems.Platform;
using CoreSystems.Projectiles;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;
using WeaponCore.Data.Scripts.CoreSystems.Ui.Targeting;
using static CoreSystems.Platform.Weapon;

namespace CoreSystems.Support
{
    public partial class Ai
    {
        internal volatile bool AiInit;
        internal volatile uint AiSpawnTick;
        internal volatile uint AiCloseTick;
        internal volatile uint AiMarkedTick;
        internal volatile uint LastAiDataSave;
        internal readonly AiDetectionInfo DetectionInfo = new AiDetectionInfo();
        internal readonly Constructs Construct;
        internal readonly FastResourceLock DbLock = new FastResourceLock();

        internal readonly Dictionary<MyObjectBuilder_PhysicalGunObject, WeaponObStorage> WeaponAmmoCountStorage = new Dictionary<MyObjectBuilder_PhysicalGunObject, WeaponObStorage>();
        internal readonly Dictionary<MyEntity, CoreComponent> CompBase = new Dictionary<MyEntity, CoreComponent>();
        internal readonly Dictionary<WeaponComponent, int> WeaponIdx = new Dictionary<WeaponComponent, int>(32);
        internal readonly Dictionary<WeaponComponent, int> WeaponTrackIdx = new Dictionary<WeaponComponent, int>(32);

        internal readonly Dictionary<Upgrade.UpgradeComponent, int> UpgradeIdx = new Dictionary<Upgrade.UpgradeComponent, int>(32);
        internal readonly Dictionary<SupportSys.SupportComponent, int> SupportIdx = new Dictionary<SupportSys.SupportComponent, int>(32);
        internal readonly Dictionary<ControlSys.ControlComponent, int> ControlIdx = new Dictionary<ControlSys.ControlComponent, int>(32);
        internal readonly Dictionary<WeaponComponent, int> PhantomIdx = new Dictionary<WeaponComponent, int>(32);

        internal readonly Dictionary<Vector3I, IMySlimBlock> AddedBlockPositions = new Dictionary<Vector3I, IMySlimBlock>(Vector3I.Comparer);
        internal readonly Dictionary<Vector3I, IMySlimBlock> RemovedBlockPositions = new Dictionary<Vector3I, IMySlimBlock>(Vector3I.Comparer);

        internal readonly Dictionary<MyStringHash, PartCounter> PartCounting = new Dictionary<MyStringHash, PartCounter>(MyStringHash.Comparer);
        internal readonly Dictionary<MyEntity, TargetInfo> Targets = new Dictionary<MyEntity, TargetInfo>(32);
        internal readonly Dictionary<MyEntity, DetectInfo> ObstructionLookup = new Dictionary<MyEntity, DetectInfo>(32);

        internal readonly Dictionary<long, PlayerControllerEntity> PlayerControl = new Dictionary<long, PlayerControllerEntity>();
        internal readonly Dictionary<WeaponComponent, int> CompWeaponGroups = new Dictionary<WeaponComponent, int>();
        internal readonly ConcurrentDictionary<MyEntity, MyInventory> InventoryMonitor = new ConcurrentDictionary<MyEntity, MyInventory>();
        internal readonly HashSet<MyEntity> ValidGrids = new HashSet<MyEntity>();
        internal readonly HashSet<MyBatteryBlock> Batteries = new HashSet<MyBatteryBlock>();
        internal readonly HashSet<MyCubeGrid> SubGridCache = new HashSet<MyCubeGrid>();
        internal readonly Dictionary<Projectile, bool> LiveProjectile = new Dictionary<Projectile, bool>();
        internal readonly HashSet<IMyMotorStator> Stators = new HashSet<IMyMotorStator>();
        internal readonly HashSet<IMyOffensiveCombatBlock> AiOffense = new HashSet<IMyOffensiveCombatBlock>();
        internal readonly HashSet<IMyFlightMovementBlock> AiFlight = new HashSet<IMyFlightMovementBlock>();
        internal readonly HashSet<IMyShipToolBase> Tools = new HashSet<IMyShipToolBase>();
        internal readonly ConcurrentDictionary<MyCubeGrid, byte> SubGridsRegistered = new ConcurrentDictionary<MyCubeGrid, byte>();
        internal readonly double[] QuadraticCoefficientsStorage = new double[5];

        internal readonly ConcurrentDictionary<QueuedSoundEvent, byte> QueuedSounds = new ConcurrentDictionary<QueuedSoundEvent, byte>();
        internal readonly List<WeaponComponent> TrackingComps = new List<WeaponComponent>();
        internal readonly List<WeaponComponent> WeaponComps = new List<WeaponComponent>(32);
        internal readonly List<WeaponComponent> CriticalComps = new List<WeaponComponent>();
        internal readonly List<Upgrade.UpgradeComponent> UpgradeComps = new List<Upgrade.UpgradeComponent>(32);
        internal readonly List<SupportSys.SupportComponent> SupportComps = new List<SupportSys.SupportComponent>(32);
        internal readonly List<ControlSys.ControlComponent> ControlComps = new List<ControlSys.ControlComponent>(32);
        internal readonly List<WeaponComponent> PhantomComps = new List<WeaponComponent>(32);
        internal readonly HashSet<Projectile> DeadProjectiles = new HashSet<Projectile>();
        internal readonly List<Ai> TargetAisTmp = new List<Ai>();
        internal readonly List<Shields> NearByShieldsTmp = new List<Shields>();
        internal readonly List<MyEntity> NearByFriendlyShields = new List<MyEntity>();
        internal readonly List<MyEntity> NearByFriendlyShieldsCache = new List<MyEntity>();

        internal readonly List<MyEntity> TestShields = new List<MyEntity>();
        internal readonly List<MyEntity> EntitiesInRange = new List<MyEntity>();
        internal readonly List<DetectInfo> ObstructionsTmp = new List<DetectInfo>();
        internal readonly List<DetectInfo> Obstructions = new List<DetectInfo>();
        internal readonly List<MyEntity> StaticsInRangeTmp = new List<MyEntity>();
        internal readonly List<Projectile> ProjectileCache = new List<Projectile>();
        internal readonly List<Projectile> ProjectileLockedCache = new List<Projectile>();
        internal readonly List<Ai> TargetAis = new List<Ai>(32);
        internal readonly List<TargetInfo> SortedTargets = new List<TargetInfo>();
        internal readonly List<DetectInfo> NewEntities = new List<DetectInfo>();
        internal readonly List<TargetInfo> ThreatCollection = new List<TargetInfo>();
        internal readonly List<Projectile> ProjectileCollection = new List<Projectile>();
        internal readonly List<DetectInfo> NonThreatCollection = new List<DetectInfo>();

        internal readonly MyDefinitionId GId = MyResourceDistributorComponent.ElectricityId;
        internal readonly AiData Data = new AiData();
        internal readonly TargetStatus TargetState = new TargetStatus();
        internal readonly AiComponent AiComp;
        internal readonly AiCharger Charger;
        
        internal MyCubeGrid.MyCubeGridHitInfo GridHitInfo = new MyCubeGrid.MyCubeGridHitInfo();


        internal MyEntity TopEntity;
        internal MyCubeGrid GridEntity;
        internal TopMap TopEntityMap;
        internal IMyCubeGrid ImyGridEntity;
        internal WeaponComponent RootComp;
        internal ControlSys.ControlComponent ControlComp;
        internal WeaponComponent OnlyWeaponComp;
        internal IMyTerminalBlock LastTerminal;
        internal IMyTerminalBlock ShieldBlock;
        internal MyEntity MyShield;
        internal MyPlanet MyPlanetTmp;
        internal MyPlanet MyPlanet;

        internal Vector4 FgFactionColor;
        internal Vector4 BgFactionColor;
        internal Vector3 TopEntityVel;
        internal Vector3D PlanetClosestPoint;
        internal Vector3D ClosestPlanetCenter;
        internal BoundingSphereD TopEntityVolume;
        internal BoundingSphereD ScanVolume;
        internal BoundingSphereD WaterVolume;
        internal BoundingBox BlockChangeArea = BoundingBox.CreateInvalid();
        internal AiTypes AiType;
        internal long AiOwner;
        internal long AiOwnerFactionId;
        internal bool IsBot;
        internal bool EnemyProjectiles;
        internal bool EnemyEntities;
        internal bool EnemiesNear;
        internal bool BlockMonitoring;
        internal bool AiSleep;
        internal bool DbUpdated;
        internal bool DetectOtherSignals;
        internal bool PointDefense;
        internal bool IsStatic;
        internal bool DbReady;
        internal bool UpdatePowerSources;
        internal bool StaticEntitiesInRange;
        internal bool StaticEntityInRange;
        internal bool FriendlyShieldNear;
        internal bool ShieldNear;
        internal bool ShieldFortified;
        internal bool HasPower;
        internal bool HadPower;
        internal bool CheckProjectiles;
        internal bool FadeOut;
        internal bool Concealed;
        internal bool RamProtection = true; 
        internal bool PlanetSurfaceInRange;
        internal bool InPlanetGravity;
        internal bool FirstRun = true;
        internal bool CanShoot = true;
        internal bool Registered;
        internal bool MarkedForClose;
        internal bool Closed;
        internal bool ScanInProgress;
        internal bool TouchingWater;
        internal bool IsGrid;
        internal bool SmartHandheld;
        internal bool ModOverride;
        internal bool AcquireTargets;
        internal uint CreatedTick;
        internal uint RotorCommandTick;
        internal uint TargetsUpdatedTick;
        internal uint VelocityUpdateTick;
        internal uint NewProjectileTick;
        internal uint LiveProjectileTick;
        internal uint ProjectileTicker;
        internal uint LastDetectEvent;
        internal uint LastBlockChangeTick;
        internal uint LastAddToRotorTick;
        internal int SleepingComps;
        internal int AwakeComps;
        internal int SourceCount;
        internal int BlockCount;
        internal int PartCount;
        internal int Version;
        internal int MyProjectiles;
        internal int NearByEntities;
        internal int NearByEntitiesTmp;
        internal int WeaponsTracking;
        internal long RotorManualControlId = -2;
        internal double MaxTargetingRange;
        internal double MaxTargetingRangeSqr;
        internal double DeadSphereRadius;
        internal double ClosestStaticSqr = double.MaxValue;
        internal double ClosestVoxelSqr = double.MaxValue;
        internal double ClosestPlanetSqr = double.MaxValue;
        internal double ClosestFixedWeaponCompSqr = double.MaxValue;
        internal Vector3D RotorTargetPosition = Vector3D.MaxValue;
        internal float GridMaxPower;
        internal float GridCurrentPower;
        internal float GridAvailablePower;
        internal float GridAssignedPower;
        internal float BatteryMaxPower;
        internal float BatteryCurrentOutput;
        internal float BatteryCurrentInput;
        internal float OptimalDps;
        internal float EffectiveDps;
        internal float PerfectDps;

        internal enum AiTypes
        {
            Grid,
            Player,
            Phantom,
        }

        private readonly List<MyEntity> _possibleTargets = new List<MyEntity>();
        private uint _pCacheTick;

        public Ai()
        {
            AiComp = new AiComponent(this);
            Charger = new AiCharger(this);
            Construct = new Constructs(this);
        }

        internal void Init(MyEntity topEntity, CoreComponent.CompTypeSpecific type)
        {
            TopEntity = topEntity;
            GridEntity = topEntity as MyCubeGrid;
            ImyGridEntity = topEntity as IMyCubeGrid;
            AiType = GridEntity != null ? AiTypes.Grid : type == CoreComponent.CompTypeSpecific.Rifle ? AiTypes.Player : AiTypes.Phantom;
            IsGrid = AiType == AiTypes.Grid;
            DeadSphereRadius = GridEntity?.GridSizeHalf + 0.1 ?? 1.35;
            AcquireTargets = !Session.I.IsClient && !Session.I.Settings.Enforcement.DisableAi;

            if (AiType != AiTypes.Phantom)
            {
                if (Session.I.TopEntityToInfoMap.TryGetValue(topEntity, out TopEntityMap))
                    TopEntityMap.GroupMap.Construct[TopEntity] = this;
            }

            topEntity.Flags |= (EntityFlags)(1 << 31);
            Closed = false;
            MarkedForClose = false;

            MaxTargetingRange = Session.I.Settings.Enforcement.MinHudFocusDistance;
            MaxTargetingRangeSqr = MaxTargetingRange * MaxTargetingRange;

            if (CreatedTick == 0)
                CreatedTick = Session.I.Tick;

            AiMarkedTick = uint.MaxValue;
            RegisterMyGridEvents(true);
            AiSpawnTick = Session.I.Tick;

            topEntity.Components.Add(AiComp);


            Data.Init(this);
            Construct.Init(this);

            if (Session.I.IsClient)
                Session.I.SendUpdateRequest(TopEntity.EntityId, PacketType.ClientAiAdd);
        }
    }
}
