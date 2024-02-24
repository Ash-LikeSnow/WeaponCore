using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using CoreSystems.Api;
using CoreSystems.Platform;
using CoreSystems.Projectiles;
using CoreSystems.Settings;
using CoreSystems.Support;
using Jakaria.API;
using ParallelTasks;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using Sandbox.ModAPI.Weapons;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Input;
using VRage.Library.Threading;
using VRage.ModAPI;
using VRage.Utils;
using VRage.Voxels;
using VRageMath;
using WeaponCore.Data.Scripts.CoreSystems.Comms;
using WeaponCore.Data.Scripts.CoreSystems.Ui;
using static CoreSystems.Support.Ai;
using static CoreSystems.Support.WeaponSystem;

namespace CoreSystems
{
    public partial class Session
    {
        internal const double StepConst = MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS;
        internal const ushort ClientPdPacketId = 62516;
        internal const ushort StringPacketId = 62517;
        internal const ushort ServerPacketId = 62518;
        internal const ushort ClientPacketId = 62519;
        internal const double TickTimeDiv = 0.0625;
        internal const double VisDirToleranceAngle = 2; //in degrees
        internal const double AimDirToleranceAngle = 5; //in degrees
        internal const int VersionControl = 34;
        internal const int AwakeBuckets = 60;
        internal const int AsleepBuckets = 180;
        internal const int ModVersion = 28;
        internal const int ClientCfgVersion = 9;
        internal const string ServerCfgName = "CoreSystemsServer.cfg";
        internal const string ClientCfgName = "CoreSystemsClient.cfg";
        internal static Session I;
        internal volatile bool Inited;
        internal volatile bool TurretControls;
        internal volatile bool FixedMissileControls;
        internal volatile bool FixedMissileReloadControls;
        internal volatile bool FixedGunControls;
        internal volatile bool TurretControllerControls;
        internal volatile bool SorterControls;
        internal volatile bool SearchLightControls;
        internal volatile uint LastDeform;
        internal volatile bool DecoyControls;
        internal volatile bool CombatControls;

        internal double DeltaStepConst;
        internal double RelativeTime;
        internal double DeltaTimeRatio;

        internal readonly TargetCompare TargetCompare = new TargetCompare();
        internal readonly WaterModAPI WApi = new WaterModAPI();
        internal readonly CustomHitInfo CustomHitInfo = new CustomHitInfo();

        internal readonly MyStringHash ShieldBypassDamageType = MyStringHash.GetOrCompute("bypass");
        internal readonly MyConcurrentPool<ConcurrentDictionary<WeaponDefinition.TargetingDef.BlockTypes, ConcurrentCachingList<MyCubeBlock>>> BlockTypePool = new MyConcurrentPool<ConcurrentDictionary<WeaponDefinition.TargetingDef.BlockTypes, ConcurrentCachingList<MyCubeBlock>>>(64);
        internal readonly MyConcurrentPool<TargetInfo> TargetInfoPool = new MyConcurrentPool<TargetInfo>(256, info => info.Clean());
        internal readonly MyConcurrentPool<WeaponAmmoMoveRequest> InventoryMoveRequestPool = new MyConcurrentPool<WeaponAmmoMoveRequest>(128, invMove => invMove.Clean());
        internal readonly MyConcurrentPool<ConcurrentCachingList<MyCubeBlock>> ConcurrentListPool = new MyConcurrentPool<ConcurrentCachingList<MyCubeBlock>>(100, cList => cList.ClearImmediate());
        internal readonly MyConcurrentPool<TopMap> GridMapPool = new MyConcurrentPool<TopMap>(128, fatMap => fatMap.Clean());
        internal readonly MyConcurrentPool<PartCounter> PartCountPool = new MyConcurrentPool<PartCounter>(64, count => count.Current = 0);
        internal readonly MyConcurrentPool<List<IMySlimBlock>> SlimPool = new MyConcurrentPool<List<IMySlimBlock>>(128, slim => slim.Clear());
        internal readonly MyConcurrentPool<CorePlatform> PlatFormPool = new MyConcurrentPool<CorePlatform>(256, platform => platform.Clean());
        internal readonly MyConcurrentPool<PacketObj> PacketObjPool = new MyConcurrentPool<PacketObj>(128, packet => packet.Clean());
        internal readonly MyConcurrentPool<ConstructPacket> PacketConstructPool = new MyConcurrentPool<ConstructPacket>(64, packet => packet.CleanUp());
        internal readonly MyConcurrentPool<ConstructFociPacket> PacketConstructFociPool = new MyConcurrentPool<ConstructFociPacket>(64, packet => packet.CleanUp());
        internal readonly MyConcurrentPool<AiDataPacket> PacketAiPool = new MyConcurrentPool<AiDataPacket>(64, packet => packet.CleanUp());
        internal readonly MyConcurrentPool<WeaponCompPacket> PacketWeaponCompPool = new MyConcurrentPool<WeaponCompPacket>(64, packet => packet.CleanUp());
        internal readonly MyConcurrentPool<UpgradeCompPacket> PacketUpgradeCompPool = new MyConcurrentPool<UpgradeCompPacket>(64, packet => packet.CleanUp());
        internal readonly MyConcurrentPool<SupportCompPacket> PacketSupportCompPool = new MyConcurrentPool<SupportCompPacket>(64, packet => packet.CleanUp());
        internal readonly MyConcurrentPool<ControlCompPacket> PacketControlCompPool = new MyConcurrentPool<ControlCompPacket>(64, packet => packet.CleanUp());

        internal readonly MyConcurrentPool<WeaponStatePacket> PacketWeaponStatePool = new MyConcurrentPool<WeaponStatePacket>(64, packet => packet.CleanUp());
        internal readonly MyConcurrentPool<UpgradeStatePacket> PacketUpgradeStatePool = new MyConcurrentPool<UpgradeStatePacket>(64, packet => packet.CleanUp());
        internal readonly MyConcurrentPool<SupportStatePacket> PacketSupportStatePool = new MyConcurrentPool<SupportStatePacket>(64, packet => packet.CleanUp());
        internal readonly MyConcurrentPool<ControlStatePacket> PacketControlStatePool = new MyConcurrentPool<ControlStatePacket>(64, packet => packet.CleanUp());
        internal readonly MyConcurrentPool<EwarValues> EwarDataPool = new MyConcurrentPool<EwarValues>(64);

        internal readonly MyConcurrentPool<WeaponReloadPacket> PacketReloadPool = new MyConcurrentPool<WeaponReloadPacket>(64, packet => packet.CleanUp());
        internal readonly MyConcurrentPool<WeaponAmmoPacket> PacketAmmoPool = new MyConcurrentPool<WeaponAmmoPacket>(64, packet => packet.CleanUp());
        internal readonly MyConcurrentPool<TargetPacket> PacketTargetPool = new MyConcurrentPool<TargetPacket>(64, packet => packet.CleanUp());
        internal readonly MyConcurrentPool<BetterInventoryItem> BetterInventoryItems = new MyConcurrentPool<BetterInventoryItem>(128);
        internal readonly MyConcurrentPool<MyConcurrentList<MyPhysicalInventoryItem>> PhysicalItemListPool = new MyConcurrentPool<MyConcurrentList<MyPhysicalInventoryItem>>(256, list => list.Clear());
        internal readonly MyConcurrentPool<MyConcurrentList<BetterInventoryItem>> BetterItemsListPool = new MyConcurrentPool<MyConcurrentList<BetterInventoryItem>>(256, list => list.Clear());
        internal readonly Stack<GridGroupMap> GridGroupMapPool = new Stack<GridGroupMap>(64);

        internal readonly Stack<Dictionary<object, Weapon>> TrackingDictPool = new Stack<Dictionary<object, Weapon>>();
        internal readonly Stack<Ai> AiPool = new Stack<Ai>(128);
        internal readonly Stack<MyEntity3DSoundEmitter> Emitters = new Stack<MyEntity3DSoundEmitter>(256);
        internal readonly Stack<VoxelCache> VoxelCachePool = new Stack<VoxelCache>(256);
        internal readonly Stack<DeferredBlockDestroy> DefferedDestroyPool = new Stack<DeferredBlockDestroy>(128);
        internal readonly Stack<ProtoProPosition> ProtoWeaponProSyncPosPool = new Stack<ProtoProPosition>(128);
        internal readonly Stack<ProtoProTarget> ProtoWeaponProSyncTargetPool = new Stack<ProtoProTarget>(32);

        internal readonly Stack<ProjectileSyncPositionPacket> ProtoWeaponProPosPacketPool = new Stack<ProjectileSyncPositionPacket>(128);
        internal readonly Stack<ProjectileSyncTargetPacket> ProtoWeaponProTargetPacketPool = new Stack<ProjectileSyncTargetPacket>(32);

        internal readonly Stack<List<MyTuple<Vector3D, object, float>>> ProHitPool = new Stack<List<MyTuple<Vector3D, object, float>>>(128);
        internal readonly Stack<WeaponSequence> SequencePool = new Stack<WeaponSequence>(32);
        internal readonly Stack<WeaponGroup> GroupPool = new Stack<WeaponGroup>(32);
        internal readonly Stack<DroneInfo> DroneInfoPool = new Stack<DroneInfo>(128);
        internal readonly Stack<ClosestObstacles> ClosestObstaclesPool = new Stack<ClosestObstacles>(64);
        internal readonly Stack<FullSyncInfo> FullSyncInfoPool = new Stack<FullSyncInfo>(32);

        internal readonly HashSet<MyCubeGrid> DirtyGridInfos = new HashSet<MyCubeGrid>();
        internal readonly HashSet<ulong> AuthorIds = new HashSet<ulong> { 76561197969691953 };

        internal readonly ConcurrentDictionary<Weapon, byte> PartToPullConsumable = new ConcurrentDictionary<Weapon, byte>();

        internal readonly ConcurrentCachingList<CoreComponent> CompsToStart = new ConcurrentCachingList<CoreComponent>();
        internal readonly ConcurrentCachingList<Ai> DelayedAiClean = new ConcurrentCachingList<Ai>();

        internal readonly CachingHashSet<PacketObj> ClientSideErrorPkt = new CachingHashSet<PacketObj>();
        internal readonly CachingHashSet<AiCharger> ChargingParts = new CachingHashSet<AiCharger>();

        internal readonly ConcurrentQueue<DeferedTypeCleaning> BlockTypeCleanUp = new ConcurrentQueue<DeferedTypeCleaning>();
        internal readonly ConcurrentQueue<Type> ControlQueue = new ConcurrentQueue<Type>();

        internal readonly ConcurrentDictionary<IMyAutomaticRifleGun, byte> DelayedHandWeaponsSpawn = new ConcurrentDictionary<IMyAutomaticRifleGun, byte>();
        internal readonly ConcurrentDictionary<MyEntity, Ai> EntityToMasterAi = new ConcurrentDictionary<MyEntity, Ai>();
        internal readonly ConcurrentDictionary<MyEntity, Ai> EntityAIs = new ConcurrentDictionary<MyEntity, Ai>();
        internal readonly ConcurrentDictionary<long, PlayerMap> Players = new ConcurrentDictionary<long, PlayerMap>();
        internal readonly ConcurrentDictionary<long, IMyCharacter> Admins = new ConcurrentDictionary<long, IMyCharacter>();
        internal readonly ConcurrentDictionary<IMyCharacter, IMyPlayer> AdminMap = new ConcurrentDictionary<IMyCharacter, IMyPlayer>();
        internal readonly ConcurrentDictionary<MyCubeGrid, ConcurrentDictionary<WeaponDefinition.TargetingDef.BlockTypes, ConcurrentCachingList<MyCubeBlock>>> GridToBlockTypeMap = new ConcurrentDictionary<MyCubeGrid, ConcurrentDictionary<WeaponDefinition.TargetingDef.BlockTypes, ConcurrentCachingList<MyCubeBlock>>>();
        internal readonly ConcurrentDictionary<MyInventory, MyConcurrentList<MyPhysicalInventoryItem>> InventoryItems = new ConcurrentDictionary<MyInventory, MyConcurrentList<MyPhysicalInventoryItem>>();
        internal readonly ConcurrentDictionary<MyInventory, ConcurrentDictionary<uint, BetterInventoryItem>> CoreInventoryItems = new ConcurrentDictionary<MyInventory, ConcurrentDictionary<uint, BetterInventoryItem>>();
        internal readonly ConcurrentDictionary<MyEntity, TopMap> TopEntityToInfoMap = new ConcurrentDictionary<MyEntity, TopMap>();
        internal readonly ConcurrentDictionary<MyInventory, MyConcurrentList<BetterInventoryItem>> ConsumableItemList = new ConcurrentDictionary<MyInventory, MyConcurrentList<BetterInventoryItem>>();
        internal readonly ConcurrentDictionary<MyInventory, int> InventoryMonitors = new ConcurrentDictionary<MyInventory, int>();
        internal readonly ConcurrentDictionary<IMySlimBlock, SupportSys> ProtSupports = new ConcurrentDictionary<IMySlimBlock, SupportSys>();
        internal readonly ConcurrentDictionary<IMySlimBlock, SupportSys> RegenSupports = new ConcurrentDictionary<IMySlimBlock, SupportSys>();
        internal readonly ConcurrentDictionary<IMySlimBlock, SupportSys> StructalSupports = new ConcurrentDictionary<IMySlimBlock, SupportSys>();
        internal readonly ConcurrentDictionary<MyEntity, WeaponDefinition.TargetingDef.BlockTypes> DecoyMap = new ConcurrentDictionary<MyEntity, WeaponDefinition.TargetingDef.BlockTypes>();
        internal readonly ConcurrentDictionary<MyCubeBlock, long> CameraChannelMappings = new ConcurrentDictionary<MyCubeBlock, long>();
        internal readonly ConcurrentDictionary<long, WaterData> WaterMap = new ConcurrentDictionary<long, WaterData>();
        internal readonly ConcurrentDictionary<long, MyPlanet> PlanetMap = new ConcurrentDictionary<long, MyPlanet>();
        internal readonly ConcurrentDictionary<MyPlanet, long> PlanetTemp = new ConcurrentDictionary<MyPlanet, long>();
        internal readonly ConcurrentDictionary<MyCubeGrid, TopMap> DirtyPowerGrids = new ConcurrentDictionary<MyCubeGrid, TopMap>();
        internal readonly ConcurrentDictionary<string, MyObjectBuilder_Checkpoint.ModItem> ModInfo = new ConcurrentDictionary<string, MyObjectBuilder_Checkpoint.ModItem>();
        
        internal readonly Dictionary<ulong, long> SteamToPlayer = new Dictionary<ulong, long>();
        internal readonly Dictionary<MyStringHash, DamageInfoLog> DmgLog = new Dictionary<MyStringHash, DamageInfoLog>(MyStringHash.Comparer);
        internal readonly Dictionary<IMyGridGroupData, GridGroupMap> GridGroupMap = new Dictionary<IMyGridGroupData, GridGroupMap>();
        internal readonly Dictionary<string, Dictionary<string, AmmoType>> AmmoMaps = new Dictionary<string, Dictionary<string, WeaponSystem.AmmoType>>();
        internal readonly Dictionary<string, string> ModelMaps = new Dictionary<string, string>();
        internal readonly Dictionary<string, Dictionary<long, Weapon.WeaponComponent>> PhantomDatabase = new Dictionary<string, Dictionary<long, Weapon.WeaponComponent>>();
        internal readonly Dictionary<CoreStructure, int> PowerGroups = new Dictionary<CoreStructure, int>();
        internal readonly Dictionary<MyDefinitionBase, BlockDamage> BlockDamageMap = new Dictionary<MyDefinitionBase, BlockDamage>();
        internal readonly Dictionary<MyDefinitionId, CoreStructure> PartPlatforms = new Dictionary<MyDefinitionId, CoreStructure>(MyDefinitionId.Comparer);
        internal readonly Dictionary<string, MyDefinitionId> CoreSystemsDefs = new Dictionary<string, MyDefinitionId>();
        internal readonly Dictionary<string, MyDefinitionId> NpcSafeWeaponDefs = new Dictionary<string, MyDefinitionId>();
        internal readonly Dictionary<uint, Weapon> WeaponLookUp = new Dictionary<uint, Weapon>();
        internal readonly Dictionary<string, MyStringHash> SubTypeIdHashMap = new Dictionary<string, MyStringHash>();
        internal readonly Dictionary<MyDefinitionId, MyStringHash> VanillaIds = new Dictionary<MyDefinitionId, MyStringHash>(MyDefinitionId.Comparer);
        internal readonly Dictionary<MyStringHash, MyDefinitionId> VanillaCoreIds = new Dictionary<MyStringHash, MyDefinitionId>(MyStringHash.Comparer);
        internal readonly Dictionary<MyStringHash, AreaRestriction> AreaRestrictions = new Dictionary<MyStringHash, AreaRestriction>(MyStringHash.Comparer);
        internal readonly Dictionary<long, FakeTargets> PlayerDummyTargets = new Dictionary<long, FakeTargets> { [-1] = new FakeTargets() };
        internal readonly Dictionary<ulong, HashSet<long>> PlayerEntityIdInRange = new Dictionary<ulong, HashSet<long>>();
        internal readonly Dictionary<long, ulong> ConnectedAuthors = new Dictionary<long, ulong>();
        internal readonly Dictionary<ulong, AvInfoCache> AvShotCache = new Dictionary<ulong, AvInfoCache>();
        internal readonly Dictionary<ulong, VoxelCache> VoxelCaches = new Dictionary<ulong, VoxelCache>();
        internal readonly Dictionary<MyEntity, CoreComponent> ArmorCubes = new Dictionary<MyEntity, CoreComponent>();
        internal readonly Dictionary<object, PacketInfo> PrunedPacketsToClient = new Dictionary<object, PacketInfo>();
        internal readonly Dictionary<long, CoreComponent> IdToCompMap = new Dictionary<long, CoreComponent>();
        internal readonly Dictionary<uint, MyPhysicalInventoryItem> AmmoItems = new Dictionary<uint, MyPhysicalInventoryItem>();
        internal readonly Dictionary<string, MyKeys> KeyMap = new Dictionary<string, MyKeys>();
        internal readonly Dictionary<string, MyMouseButtonsEnum> MouseMap = new Dictionary<string, MyMouseButtonsEnum>();
        internal readonly Dictionary<WeaponDefinition.AmmoDef, CoreSettings.ServerSettings.AmmoOverride> AmmoValuesMap = new Dictionary<WeaponDefinition.AmmoDef, CoreSettings.ServerSettings.AmmoOverride>();
        internal readonly Dictionary<WeaponDefinition, CoreSettings.ServerSettings.WeaponOverride> WeaponValuesMap = new Dictionary<WeaponDefinition, CoreSettings.ServerSettings.WeaponOverride>();
        internal readonly Dictionary<ulong, Projectile> MonitoredProjectiles = new Dictionary<ulong, Projectile>();
        internal readonly Dictionary<uint, ProtoProPositionSync> GlobalProPosSyncs = new Dictionary<uint, ProtoProPositionSync>();
        internal readonly Dictionary<uint, ProtoProTargetSync> GlobalProTargetSyncs = new Dictionary<uint, ProtoProTargetSync>();

        internal readonly Dictionary<ulong, TickLatency> PlayerTickLatency = new Dictionary<ulong, TickLatency>();
        internal readonly Dictionary<long, DamageHandlerRegistrant> DamageHandlerRegistrants = new Dictionary<long, DamageHandlerRegistrant>();
        internal readonly Dictionary<long, Func<MyEntity, IMyCharacter, long, int, bool>> TargetFocusHandlers = new Dictionary<long, Func<MyEntity, IMyCharacter, long, int, bool>>();
        internal readonly Dictionary<long, Func<IMyCharacter, long, int, bool>> HudHandlers = new Dictionary<long, Func<IMyCharacter, long, int, bool>>();
        internal readonly Dictionary<long, Func<Vector3D, Vector3D, int, bool, object, int, int, int, bool>> ShootHandlers = new Dictionary<long, Func<Vector3D, Vector3D, int, bool, object, int, int, int, bool>>();
        internal readonly Dictionary<MyStringHash, ResistanceValues> ArmorCoreBlockMap = new Dictionary<MyStringHash, ResistanceValues>();
        internal readonly Dictionary<MyDefinitionId, AmmoType> AmmoDefIds = new Dictionary<MyDefinitionId, AmmoType>(MyDefinitionId.Comparer);
        internal readonly Dictionary<MyDefinitionId, List<WeaponMagMap>> SubTypeIdToWeaponMagMap = new Dictionary<MyDefinitionId, List<WeaponMagMap>>(MyDefinitionId.Comparer);
        internal readonly Dictionary<MyDefinitionId, List<WeaponMagMap>> SubTypeIdToNpcSafeWeaponMagMap = new Dictionary<MyDefinitionId, List<WeaponMagMap>>(MyDefinitionId.Comparer);
        internal readonly Dictionary<MyCubeGrid, DeferredBlockDestroy> DeferredDestroy = new Dictionary<MyCubeGrid, DeferredBlockDestroy>();
        internal readonly Dictionary<long, DamageHandlerRegistrant> SystemWideDamageRegistrants = new Dictionary<long, DamageHandlerRegistrant>();


        internal readonly ConcurrentDictionary<long, int> DeferredPlayerLock = new ConcurrentDictionary<long, int>();
        internal readonly HashSet<MyDefinitionId> DefIdsComparer = new HashSet<MyDefinitionId>(MyDefinitionId.Comparer);
        internal readonly HashSet<string> VanillaSubpartNames = new HashSet<string>();
        internal readonly HashSet<MyDefinitionBase> AllArmorBaseDefinitions = new HashSet<MyDefinitionBase>();
        internal readonly HashSet<MyDefinitionBase> HeavyArmorBaseDefinitions = new HashSet<MyDefinitionBase>();
        internal readonly HashSet<MyDefinitionBase> CoreShieldBlockTypes = new HashSet<MyDefinitionBase>();
        internal readonly HashSet<MyStringHash> CustomArmorSubtypes = new HashSet<MyStringHash>();
        internal readonly HashSet<MyStringHash> CustomHeavyArmorSubtypes = new HashSet<MyStringHash>();


        internal readonly HashSet<MyCubeGrid> DeformProtection = new HashSet<MyCubeGrid>();
        internal readonly HashSet<IMyTerminalAction> CustomActions = new HashSet<IMyTerminalAction>();
        internal readonly HashSet<IMyTerminalAction> AlteredActions = new HashSet<IMyTerminalAction>();
        internal readonly HashSet<IMyTerminalControl> CustomControls = new HashSet<IMyTerminalControl>();
        internal readonly HashSet<IMyTerminalControl> AlteredControls = new HashSet<IMyTerminalControl>();
        internal readonly HashSet<Part> WeaponLosDebugActive = new HashSet<Part>();
        internal readonly HashSet<SupportSys> DisplayAffectedArmor = new HashSet<SupportSys>();
        internal readonly HashSet<Type> ControlTypeActivated = new HashSet<Type>();
        internal readonly HashSet<IMyPlayer> PlayerControllerMonitor = new HashSet<IMyPlayer>();
        internal readonly List<int> PointDefenseSyncs = new List<int>();
        internal readonly List<GridGroupMap> GridGroupUpdates = new List<GridGroupMap>();
        internal readonly List<Weapon> InvPullClean = new List<Weapon>();
        internal readonly List<Weapon> InvRemoveClean = new List<Weapon>();
        internal readonly List<CoreComponent> CompsDelayedInit = new List<CoreComponent>();
        internal readonly List<CoreComponent> CompsDelayedReInit = new List<CoreComponent>();
        internal readonly Dictionary<ulong, List<ClientProSyncDebugLine>> ProSyncLineDebug = new Dictionary<ulong, List<ClientProSyncDebugLine>>();
        internal readonly ConcurrentDictionary<ulong, ApproachStageDebug> ApproachStageChangeDebug = new ConcurrentDictionary<ulong, ApproachStageDebug>();
        internal readonly List<CompReAdd> CompReAdds = new List<CompReAdd>();
        internal readonly List<MyLineSegmentOverlapResult<MyEntity>> OverlapResultTmp = new List<MyLineSegmentOverlapResult<MyEntity>>();
        internal readonly List<Projectile> Hits = new List<Projectile>(16);
        internal readonly List<Weapon> AcquireTargets = new List<Weapon>(128);
        internal readonly List<Weapon> TmpWeaponEventSortingList = new List<Weapon>();
        internal readonly List<Weapon> HomingWeapons = new List<Weapon>(128);
        internal readonly List<Ai> AimingAi = new List<Ai>(128);
        internal readonly List<IHitInfo> HitInfoTmpList = new List<IHitInfo>();
        internal readonly HashSet<MyDefinitionId> CoreSystemsFixedBlockDefs = new HashSet<MyDefinitionId>();
        internal readonly HashSet<MyDefinitionId> CoreSystemsTurretBlockDefs = new HashSet<MyDefinitionId>();
        internal readonly HashSet<MyDefinitionId> CoreSystemsSupportDefs = new HashSet<MyDefinitionId>();
        internal readonly HashSet<MyDefinitionId> CoreSystemsUpgradeDefs = new HashSet<MyDefinitionId>();
        internal readonly HashSet<MyDefinitionId> CoreSystemsRifleDefs = new HashSet<MyDefinitionId>();
        internal readonly HashSet<MyDefinitionId> CoreSystemsPhantomDefs = new HashSet<MyDefinitionId>();
        internal readonly HashSet<ArmorDefinition> CoreSystemsArmorDefs = new HashSet<ArmorDefinition>();
        internal readonly HashSet<string> VanillaSubtypes = new HashSet<string>();
        internal readonly HashSet<MyStringHash> PerformanceWarning = new HashSet<MyStringHash>();
        internal readonly HashSet<Ai> GridsToUpdateInventories = new HashSet<Ai>();

        internal readonly List<MyCubeGrid> DirtyGridsTmp = new List<MyCubeGrid>(10);
        internal readonly List<DbScan> DbsToUpdate = new List<DbScan>(32);
        internal readonly List<Weapon> ShootingWeapons = new List<Weapon>(128);
        internal readonly List<PacketInfo> PacketsToClient = new List<PacketInfo>(128);
        internal readonly List<Packet> PacketsToServer = new List<Packet>(128);
        internal readonly List<WeaponAmmoMoveRequest> ConsumableToPullQueue = new List<WeaponAmmoMoveRequest>(128);
        internal readonly List<PacketObj> ClientPacketsToClean = new List<PacketObj>(64);
        internal readonly List<CleanSound> SoundsToClean = new List<CleanSound>(128);
        internal readonly List<LosDebug> LosDebugList = new List<LosDebug>(128);
        internal readonly List<MyTuple<IMyPlayer, Vector4, FakeTarget>> ActiveMarks = new List<MyTuple<IMyPlayer, Vector4, FakeTarget>>();
        
        internal readonly List<Weapon>[] LeadGroups = new List<Weapon>[4];
        internal readonly Queue<double> ClientPerfHistory = new Queue<double>(20);
        internal readonly int[] AuthorSettings = new int[6];
        internal readonly List<Projectile> EwaredProjectiles = new List<Projectile>();
        internal readonly List<IMySlimBlock>[] DamageBlockCache = new List<IMySlimBlock>[512];

        ///
        ///
        ///

        internal readonly double ApproachDegrees = Math.Cos(MathHelper.ToRadians(50));
        internal readonly CubeCompare CubeComparer = new CubeCompare();
        internal readonly FutureEvents FutureEvents = new FutureEvents();
        internal readonly BoundingFrustumD CameraFrustrum = new BoundingFrustumD();
        internal readonly Guid CompDataGuid = new Guid("75BBB4F5-4FB9-4230-BEEF-BB79C9811501");
        internal readonly Guid AiDataGuid = new Guid("75BBB4F5-4FB9-4230-BEEF-BB79C9811502");
        internal readonly Guid ConstructDataGuid = new Guid("75BBB4F5-4FB9-4230-BEEF-BB79C9811503");

        internal readonly double VisDirToleranceCosine;
        internal readonly double AimDirToleranceCosine;

        private readonly HashSet<IMySlimBlock> _destroyedSlims = new HashSet<IMySlimBlock>();
        private readonly HashSet<IMySlimBlock> _destroyedSlimsClient = new HashSet<IMySlimBlock>();
        private readonly Dictionary<IMySlimBlock, float> _slimHealthClient = new Dictionary<IMySlimBlock, float>();
        private readonly Dictionary<IMySlimBlock, uint> _slimLastDeformTick = new Dictionary<IMySlimBlock, uint>();

        private readonly Dictionary<string, Dictionary<string, MyTuple<string, string, string, string>>> _subTypeMaps = new Dictionary<string, Dictionary<string, MyTuple<string, string, string, string>>>();
        private readonly Dictionary<string, List<WeaponDefinition>> _subTypeIdWeaponDefs = new Dictionary<string, List<WeaponDefinition>>();
        private readonly Dictionary<string, List<UpgradeDefinition>> _subTypeIdUpgradeDefs = new Dictionary<string, List<UpgradeDefinition>>();
        private readonly Dictionary<string, List<SupportDefinition>> _subTypeIdSupportDefs = new Dictionary<string, List<SupportDefinition>>();
        private readonly List<MyKeys> _pressedKeys = new List<MyKeys>();
        private readonly List<MyMouseButtonsEnum> _pressedButtons = new List<MyMouseButtonsEnum>();
        private readonly List<MyEntity> _tmpNearByBlocks = new List<MyEntity>();


        internal readonly Spectrum Spectrum;

        internal readonly ProtoDeathSyncMonitor ProtoDeathSyncMonitor = new ProtoDeathSyncMonitor();
        private readonly EwaredBlocksPacket _cachedEwarPacket = new EwaredBlocksPacket();
        private readonly SpinLockRef _dityGridLock = new SpinLockRef();

        internal int[] TargetDeck = new int[1000];
        internal int[] BlockDeck = new int[5000];

        internal List<RadiatedBlock> SlimsSortedList = new List<RadiatedBlock>(1024);
        internal MyConcurrentPool<MyEntity> TriggerEntityPool;
        internal MyDynamicAABBTreeD ProjectileTree = new MyDynamicAABBTreeD(Vector3D.One * 10.0, 10.0);

        internal List<PartAnimation> AnimationsToProcess = new List<PartAnimation>(128);
        internal List<WeaponDefinition> WeaponDefinitions = new List<WeaponDefinition>();
        internal List<UpgradeDefinition> UpgradeDefinitions = new List<UpgradeDefinition>();
        internal List<SupportDefinition> SupportDefinitions = new List<SupportDefinition>();
        internal DictionaryValuesReader<MyDefinitionId, MyDefinitionBase> AllDefinitions;
        internal DictionaryValuesReader<MyDefinitionId, MyAudioDefinition> SoundDefinitions;
        internal Color[] HeatEmissives;

        internal ControlQuery ControlRequest;
        internal IMyPhysics Physics;
        internal IMyCamera Camera;
        internal IMyGps TargetGps;
        internal IMyBlockPlacerBase Placer;
        internal IMyTerminalBlock LastTerminal;
        internal IMyCharacter LocalCharacter;
        internal Ai TrackingAi;

        internal ApiServer ApiServer;
        internal MyCockpit ActiveCockPit;
        internal MyCubeBlock ActiveControlBlock;
        internal MyCameraBlock ActiveCameraBlock;
        internal IMyAutomaticRifleGun PlayerHandWeapon;
        internal MyEntity ControlledEntity;
        internal Projectiles.Projectiles Projectiles;
        internal ApiBackend Api;
        internal Action<Vector3, float> ProjectileAddedCallback = (location, health) => { };
        internal ShieldApi SApi = new ShieldApi();
        internal NetworkReporter Reporter = new NetworkReporter();
        internal MyStorageData TmpStorage = new MyStorageData();
        internal AcquireManager AcqManager;
        internal RunAv Av;
        internal DSUtils DsUtil;
        internal DSUtils DsUtil2;
        internal StallReporter StallReporter;
        internal StallReporter InnerStallReporter;
        internal UiInput UiInput;
        internal WeaponCore.Data.Scripts.CoreSystems.Ui.Targeting.TargetUi TargetUi;
        internal WeaponCore.Data.Scripts.CoreSystems.Ui.Hud.Hud HudUi;
        internal CoreSettings Settings;
        internal TerminalMonitor TerminalMon;
        internal ProblemReport ProblemRep;

        internal XorShiftRandomStruct XorRnd;
        internal ApproachDebug ApproachDebug = new ApproachDebug {LastTick = uint.MaxValue};
        internal MatrixD CameraMatrix;
        internal Vector3D CameraPos;
        internal Vector3D PlayerPos;
        internal Task PTask = new Task();
        internal Task GridTask = new Task();
        internal Task DbTask = new Task();
        internal Task ITask = new Task();
        internal Task CTask = new Task();
        internal MyStringHash ShieldHash;
        internal MyStringHash WaterHash;
        internal MyStringHash CustomEntityHash;

        internal string TriggerEntityModel;
        internal string ServerVersion;
        internal string PlayerMessage;
        internal object InitObj = new object();

        internal uint AdvancedToggleTick;
        internal uint Tick;
        internal uint ClientDestroyBlockTick;
        internal uint ReInitTick;
        internal uint TargetLastDrawTick;
        internal uint LastProSyncSendTick;
        internal uint LastPongTick;
        internal uint WeaponSyncId;
        internal uint LosNotifyTick;
        internal int TargetDrawAge;
        internal int WeaponIdCounter;

        internal int ActiveAntiSmarts;
        internal int SimulationCount;
        internal int PlayerEventId;
        internal int TargetRequests;
        internal int TargetChecks;
        internal int BlockChecks;
        internal int ClosestRayCasts;
        internal int RandomRayCasts;
        internal int TopRayCasts;
        internal int CanShoot;
        internal int TargetTransfers;
        internal int TargetSets;
        internal int TargetResets;
        internal int AmmoMoveTriggered;
        internal int Count = -1;
        internal int LCount;
        internal int SCount;
        internal int QCount;
        internal int LogLevel;
        internal int AwakeCount = -1;
        internal int AsleepCount = -1;
        internal int Rays;
        internal int ClientAvLevel;
        internal int SimStepsLastSecond;
        internal int MenuDepth;
        internal int MainThreadId = 1;
        internal int DeathSyncPackets;
        internal int DeathSyncDataSize;
        internal ulong MultiplayerId;
        internal ulong MuzzleIdCounter;
        internal ulong PhantomIdCounter;

        internal long PlayerId;
        internal int ClientAvDivisor = 1;
        internal double SyncDistSqr;
        internal double SyncBufferedDistSqr;
        internal double SyncDist;
        internal double MaxEntitySpeed;
        internal double Load;
        internal double ScaleFov;
        internal double RayMissAmounts;

        internal float AspectRatio;
        internal float AspectRatioInv;
        internal float UiBkOpacity;
        internal float UiOpacity;
        internal float UiHudOpacity;
        internal float CurrentFovWithZoom;
        internal float LastOptimalDps;
        internal float ServerSimulation;
        internal float LocalSimulation;
        internal bool GlobalDamageHandlerActive;
        internal bool TargetInfoKeyLock;
        internal bool MinimalHudOverride;
        internal bool PurgedAll;
        internal bool InMenu;
        internal bool PlayerStartMessage;
        internal bool GunnerBlackList;
        internal bool MpActive;
        internal bool AdvSyncClient;
        internal bool AdvSyncServer;
        internal bool AdvSync;
        internal bool IsServer;
        internal bool BaseControlsActions;
        internal bool EarlyInitOver;
        internal bool IsHost;
        internal bool MpServer;
        internal bool DedicatedServer;
        internal bool FirstLoop;
        internal bool GameLoaded;
        internal bool PlayersLoaded;
        internal bool MiscLoaded;
        internal bool Tick5;
        internal bool Tick10;
        internal bool Tick20;
        internal bool Tick30;
        internal bool Tick60;
        internal bool Tick90;
        internal bool Tick120;
        internal bool Tick180;
        internal bool Tick300;
        internal bool Tick600;
        internal bool Tick1200;
        internal bool Tick1800;
        internal bool Tick3600;
        internal bool ShieldMod;
        internal bool ReplaceVanilla;
        internal bool ShieldApiLoaded;
        internal bool WaterApiLoaded;
        internal bool InGridAiBlock;
        internal bool IsCreative;
        internal bool IsClient;
        internal bool HandlesInput;
        internal bool AuthLogging;
        internal bool DamageHandler;
        internal bool LocalVersion;
        internal bool SuppressWc;
        internal bool PbApiInited;
        internal bool PbActivate;
        internal bool ClientCheck;
        internal bool DbUpdating;
        internal bool InventoryUpdate;
        internal bool GlobalDamageModifed;
        internal bool WaterMod;
        internal bool DebugLos = false;
        internal bool QuickDisableGunsCheck;
        internal bool ColorArmorToggle;
        internal bool EwarNetDataDirty;
        internal bool CanChangeHud;
        internal bool LeadGroupsDirty;
        internal bool CameraDetected;
        internal bool LeadGroupActive;
        internal bool ArmorCoreActive;
        internal bool DebugMod;
        internal bool DebugVersion;
        internal bool AntiSmartActive;
        internal bool DirtyGrid;
        internal bool AuthorConnected;

        internal readonly HashSet<ulong> BlessedPlayers = new HashSet<ulong>()
        {
            //76561198339035377 -- Most cherished of humanity
        };

        internal readonly HashSet<ulong> JokePlayerList = new HashSet<ulong>()
        {
        };


        internal readonly HashSet<string> VanillaUpgradeModuleHashes = new HashSet<string>()
        {
            "LargeProductivityModule", "LargeEffectivenessModule", "LargeEnergyModule",
        };

        internal readonly HashSet<MyDefinitionId> SearchLightHashes = new HashSet<MyDefinitionId>(MyDefinitionId.Comparer)
        {
            new MyDefinitionId(typeof(MyObjectBuilder_Searchlight), "LargeSearchlight"),
            new MyDefinitionId(typeof(MyObjectBuilder_Searchlight), "SmallSearchlight"),
        };


        internal readonly HashSet<string> VanillaWeaponCompatible = new HashSet<string>()
        {
            "Large_SC_LaserDrillTurret", 
        };

        internal readonly Dictionary<string, int> VanillaLeadGroupMatch = new Dictionary<string, int>()
        {
            ["Small Gatling Gun"] = 1,
            ["Large Missile Launcher"] = 2,
            ["Small Missile Launcher"] = 2,
            ["Reloadable Missile Launcher"] = 2,
            ["Autocannon"] = 3,
            ["Assault Cannon"] = 3,
            ["Artillery"] = 3,
            ["Large Railgun"] = 4,
        };

        [Flags]
        internal enum SafeZoneAction
        {
            Damage = 1,
            Shooting = 2,
            Drilling = 4,
            Welding = 8,
            Grinding = 16, // 0x00000010
            VoxelHand = 32, // 0x00000020
            Building = 64, // 0x00000040
            LandingGearLock = 128, // 0x00000080
            ConvertToStation = 256, // 0x00000100
            All = ConvertToStation | LandingGearLock | Building | VoxelHand | Grinding | Welding | Drilling | Shooting | Damage, // 0x000001FF
            AdminIgnore = ConvertToStation | Building | VoxelHand | Grinding | Welding | Drilling | Shooting, // 0x0000017E
        }

        internal enum AnimationType
        {
            Movement,
            ShowInstant,
            HideInstant,
            ShowFade,
            HideFade,
            Delay,
            EmissiveOnly
        }

        private int _loadCounter = 1;
        private int _shortLoadCounter = 1;
        private uint _lastDrawTick;
        internal uint VanillaTurretTick;

        private bool _paused;

        internal class HackEqualityComparer : IEqualityComparer
        {
            internal MyObjectBuilder_Definitions Def;
            public bool Equals(object a, object b) => false;

            public int GetHashCode(object o)
            {
                var definitions = o as MyObjectBuilder_Definitions;
                if (definitions != null)
                    Def = definitions;
                return 0;
            }
        }

        internal VoxelCache UniqueMuzzleId
        {
            get {
                if (VoxelCachePool.Count > 0)
                    return VoxelCachePool.Pop();

                var cache = new VoxelCache { Id = MuzzleIdCounter++ };
                VoxelCaches.Add(cache.Id, cache);
                return cache;
            }   
            set { VoxelCachePool.Push(value); } 
        }

        internal int UniquePartId => WeaponIdCounter+=5;
        internal uint SyncWeaponId => WeaponSyncId++;

        internal ulong UniquePhantomId => PhantomIdCounter++;

        public static T CastProhibit<T>(T ptr, object val) => (T) val;

        public Session()
        {
            I = this;

            XorRnd = new XorShiftRandomStruct(235211389686413);
            UiInput = new UiInput();
            HudUi = new WeaponCore.Data.Scripts.CoreSystems.Ui.Hud.Hud();
            TargetUi = new WeaponCore.Data.Scripts.CoreSystems.Ui.Targeting.TargetUi();
            DsUtil = new DSUtils();
            DsUtil2 = new DSUtils();
            StallReporter = new StallReporter();
            InnerStallReporter = new StallReporter();
            Av = new RunAv();
            Api = new ApiBackend();
            ApiServer = new ApiServer();
            Projectiles = new Projectiles.Projectiles();
            AcqManager = new AcquireManager();
            TerminalMon = new TerminalMonitor();
            Spectrum = new Spectrum();
            _cachedEwarPacket.Data = new List<EwarValues>(32);

            ProblemRep = new ProblemReport();
            VisDirToleranceCosine = Math.Cos(MathHelper.ToRadians(VisDirToleranceAngle));
            AimDirToleranceCosine = Math.Cos(MathHelper.ToRadians(AimDirToleranceAngle));

            VoxelCaches[ulong.MaxValue] = new VoxelCache();

            HeatEmissives = CreateHeatEmissive();
            LoadVanillaData();
            CustomEntityHash = MyStringHash.GetOrCompute("CustomEntity");
            for (int i = 0; i < AuthorSettings.Length; i++)
                AuthorSettings[i] = -1;

            for (int i = 0; i < LeadGroups.Length; i++)
                LeadGroups[i] = new List<Weapon>();

            for (int i = 0; i < DamageBlockCache.Length; i++)
               DamageBlockCache[i] = new List<IMySlimBlock>();

        }
    }

    internal class DamageInfoLog
    {
        public string TerminalName = "";
        public long Primary = 0;
        public long Shield = 0;
        public long AOE = 0;
        public long Projectile = 0;
        public int WepCount = 0;
    }
}