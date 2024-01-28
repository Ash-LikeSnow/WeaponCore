
using System;
using System.Collections.Generic;
using CoreSystems.Platform;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Weapons;
using SpaceEngineers.Game.ModAPI;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;

namespace CoreSystems.Support
{
    public partial class CoreComponent
    {
        internal readonly List<PartAnimation> AllAnimations = new List<PartAnimation>();
        internal readonly List<int> ConsumableSelectionPartIds = new List<int>();
        internal List<Action<long, int, ulong, long, Vector3D, bool>>[] ProjectileMonitors;
        internal List<Action<int, bool>>[] EventMonitors;

        internal bool InControlPanel => MyAPIGateway.Gui.GetCurrentScreen == MyTerminalPageEnum.ControlPanel;
        internal readonly RunningAverage DamageAverage = new RunningAverage(10);

        internal bool InventoryInited;
        internal CompType Type;
        internal CompTypeSpecific TypeSpecific;
        internal MyEntity CoreEntity;
        internal IMySlimBlock Slim;
        internal IMyTerminalBlock TerminalBlock;
        internal IMyFunctionalBlock FunctionalBlock;

        internal MyCubeBlock Cube;
        internal bool IsBlock;
        internal MyDefinitionId Id;
        internal MyStringHash SubTypeId;
        internal string SubtypeName;
        internal string PhantomType;
        internal bool LazyUpdate;
        internal bool IsBot;
        internal MyInventory CoreInventory;

        internal CompData BaseData;

        internal Ai Ai;
        internal Ai MasterAi;
        internal CorePlatform Platform;
        internal MyEntity TopEntity;
        internal MyEntity InventoryEntity;
        internal uint IsWorkingChangedTick;
        internal uint NextLazyUpdateStart;
        internal uint LastAddToScene;
        internal uint SpawnTick;
        internal uint LastRemoveFromScene;
        internal int PartTracking;
        internal double MaxDetectDistance = double.MinValue;
        internal double MaxDetectDistanceSqr = double.MinValue;
        internal double MinDetectDistance = double.MaxValue;
        internal double MinDetectDistanceSqr = double.MaxValue;

        internal double TotalEffect;
        internal long TotalPrimaryEffect;
        internal long TotalAOEEffect;
        internal long TotalShieldEffect;
        internal long TotalProjectileEffect;
        internal long LastControllingPlayerId;
        internal double PreviousTotalEffect;
        internal double AverageEffect;
        internal double AddEffect;
        internal double HeatLoss;

        internal float CurrentHeat;
        internal float MaxHeat;
        internal float HeatPerSecond;
        internal float HeatSinkRate;
        internal float SinkPower;
        internal float IdlePower;
        internal float MaxIntegrity;
        internal float CurrentInventoryVolume;
        internal int PowerGroupId;
        internal int SceneVersion;
        internal bool InfiniteResource;
        internal long CustomIdentity;
        internal bool DetectOtherSignals;
        internal bool IsAsleep;
        internal bool IsFunctional;
        internal bool IsWorking;
        internal bool WhileOnActive;
        internal bool IsDisabled;
        internal bool LastOnOffState;
        internal bool CanOverload;
        internal bool HasTurret;
        internal bool TurretController;
        internal bool HasArming;
        internal bool IsBomb;
        internal bool OverrideLeads;
        internal bool UpdatedState;
        internal bool UserControlled;
        internal bool Debug;
        internal bool ModOverride;
        internal bool Registered;
        internal bool ResettingSubparts;
        internal bool UiEnabled;
        internal bool HasDelayToFire;
        internal bool ManualMode;
        internal bool PainterMode;
        internal bool FakeMode;
        internal bool CloseCondition;
        internal bool HasCloseConsition;
        internal bool HasServerOverrides;
        internal bool HasInventory;
        internal bool NeedsWorldMatrix;
        internal bool WorldMatrixEnabled;
        internal bool NeedsWorldReset;
        internal bool AnimationsModifyCoreParts;
        internal bool HasAim;
        internal bool InReInit;
        internal string CustomIcon;
        internal bool ActivePlayer;

        internal MyDefinitionId GId = MyResourceDistributorComponent.ElectricityId;

        internal Start Status;

        internal enum Start
        {
            Started,
            Starting,
            Stopped,
            ReInit,
        }

        internal enum CompTypeSpecific
        {
            VanillaTurret,
            VanillaFixed,
            SorterWeapon,
            Support,
            Upgrade,
            Phantom,
            Rifle,
            Control,
            SearchLight,
        }

        internal enum CompType
        {
            Weapon,
            Support,
            Upgrade,
            Control
        }

        public enum Trigger
        {
            Off,
            On,
            Once,
        }

        internal bool FakeIsWorking => !IsBlock || IsWorking;

        public void Init(MyEntity coreEntity, bool isBlock, CompData compData, MyDefinitionId id)
        {
            CoreEntity = coreEntity;
            IsBlock = isBlock;
            Id = id;
            SubtypeName = id.SubtypeName;
            SubTypeId = id.SubtypeId;
            BaseData = compData;
            SpawnTick = Session.I.Tick;
            if (IsBlock) {

                Cube = (MyCubeBlock)CoreEntity;
                Slim = Cube.SlimBlock;
                MaxIntegrity = Slim.MaxIntegrity;
                TerminalBlock = coreEntity as IMyTerminalBlock;
                FunctionalBlock = coreEntity as IMyFunctionalBlock;

                var turret = CoreEntity as IMyLargeTurretBase;
                if (turret != null)
                {
                    TypeSpecific = CompTypeSpecific.VanillaTurret;
                    Type = CompType.Weapon;
                }
                else if (CoreEntity is IMyConveyorSorter)
                {
                    if (Session.I.CoreSystemsSupportDefs.Contains(Cube.BlockDefinition.Id))
                    {
                        TypeSpecific = CompTypeSpecific.Support;
                        Type = CompType.Support;
                    }
                    else if (Session.I.CoreSystemsUpgradeDefs.Contains(Cube.BlockDefinition.Id))
                    {
                        TypeSpecific = CompTypeSpecific.Upgrade;
                        Type = CompType.Upgrade;
                    }
                    else {

                        TypeSpecific = CompTypeSpecific.SorterWeapon;
                        Type = CompType.Weapon;
                    }
                }
                else if (CoreEntity is IMyTurretControlBlock) {
                    TypeSpecific = CompTypeSpecific.Control;
                    Type = CompType.Control;
                }
                else if (CoreEntity is IMySearchlight)
                {
                    TypeSpecific = CompTypeSpecific.SearchLight;
                    Type = CompType.Weapon;
                }
                else {
                    TypeSpecific = CompTypeSpecific.VanillaFixed;
                    Type = CompType.Weapon;
                }

            }
            else if (CoreEntity is IMyAutomaticRifleGun) {

                MaxIntegrity = 1;
                TypeSpecific = CompTypeSpecific.Rifle;
                Type = CompType.Weapon;
                var rifle = (IMyAutomaticRifleGun)CoreEntity;
                TopEntity = rifle.Owner;
                
                TopMap topMap;
                if (TopEntity != null && !Session.I.TopEntityToInfoMap.TryGetValue(TopEntity, out topMap))
                {
                    topMap = Session.I.GridMapPool.Get();
                    topMap.Trash = true;
                    Session.I.TopEntityToInfoMap.TryAdd(TopEntity, topMap);
                    var map = Session.I.GridGroupMapPool.Count > 0 ? Session.I.GridGroupMapPool.Pop() : new GridGroupMap();
                    map.OnTopEntityAdded(null, TopEntity, null);
                    TopEntity.OnClose += Session.I.RemoveOtherFromMap;
                }

            }
            else {
                TypeSpecific = CompTypeSpecific.Phantom;
                Type = CompType.Weapon;
            }

            LazyUpdate = Type == CompType.Support || Type == CompType.Upgrade;
            InventoryEntity = TypeSpecific != CompTypeSpecific.Rifle ? CoreEntity : (MyEntity)((IMyAutomaticRifleGun)CoreEntity).AmmoInventory.Entity;
            CoreInventory = (MyInventory)InventoryEntity.GetInventoryBase();
            
            HasInventory = InventoryEntity.HasInventory;
            Platform = Session.I.PlatFormPool.Get();
            Platform.Setup(this);
            IdlePower = Platform.Structure.CombinedIdlePower;
            SinkPower = IdlePower;

            ProjectileMonitors = new List<Action<long, int, ulong, long, Vector3D, bool>>[Platform.Structure.PartHashes.Length];
            for (int i = 0; i < ProjectileMonitors.Length; i++)
                ProjectileMonitors[i] = new List<Action<long, int, ulong, long, Vector3D, bool>>();

            EventMonitors = new List<Action<int, bool>>[Platform.Structure.PartHashes.Length];
            for (int i = 0; i < EventMonitors.Length; i++)
                EventMonitors[i] = new List<Action<int, bool>>();

            PowerGroupId = Session.I.PowerGroups[Platform.Structure];
            CoreEntity.OnClose += Session.I.CloseComps;
            CloseCondition = false;
        }        
    }
}
