using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CoreSystems.Platform;
using CoreSystems.Support;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Weapons;
using SpaceEngineers.Game.ModAPI;
using VRage.Collections;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRageMath;
using static CoreSystems.Support.Ai;
using IMyControllableEntity = VRage.Game.ModAPI.Interfaces.IMyControllableEntity;

namespace CoreSystems
{
    public partial class Session
    {
        internal void OnEntityCreate(MyEntity entity)
        {
            if (!Inited) lock (InitObj) Init();
            
            var planet = entity as MyPlanet;
            if (planet != null)
                PlanetTemp.TryAdd(planet, byte.MaxValue); //More keen jank workarounds

            var grid = entity as MyCubeGrid;
            if (grid != null)
            {
                var gridMap = GridMapPool.Get();
                gridMap.Trash = true;
                TopEntityToInfoMap.TryAdd(grid, gridMap);
                grid.AddedToScene += AddGridToMap;
                grid.OnClose += RemoveFromMap;
            }

            if (!PbApiInited && entity is IMyProgrammableBlock) PbActivate = true;
            var placer = entity as IMyBlockPlacerBase;
            if (placer != null && Placer == null) Placer = placer;

            var cube = entity as MyCubeBlock;
            var sorter = entity as MyConveyorSorter;
            var turret = entity as IMyLargeTurretBase;
            var controllableGun = entity as IMyUserControllableGun;

            var rifle = entity as IMyAutomaticRifleGun;

            var decoy = cube as IMyDecoy;
            var camera = cube as MyCameraBlock;
            var turretController = cube as IMyTurretControlBlock;
            var searchLight = cube as IMySearchlight;
            var flight = cube as IMyFlightMovementBlock;
            var combat = cube as IMyOffensiveCombatBlock;

            if (sorter != null || turret != null || controllableGun != null || rifle != null || turretController != null || searchLight != null)
            {
                lock (InitObj)
                {
                    if (rifle != null) {
                        DelayedHandWeaponsSpawn.TryAdd(rifle, byte.MinValue);
                        return;
                    }

                    var validVanilla = cube != null && VanillaIds.ContainsKey(cube.BlockDefinition.Id);
                    if (validVanilla && cube.BlockDefinition.Id.SubtypeId.String == "AutoCannonTurret" && ((IMyModel)cube.Model).AssetName.StartsWith("Models"))
                        return;

                    var validType = cube != null && (validVanilla || PartPlatforms.ContainsKey(cube.BlockDefinition.Id)) || turretController != null;

                    if (!validType)
                    {
                        if (turret != null)
                            VanillaTurretTick = Tick;
                        if (cube != null && (turret != null || controllableGun != null) && (cube.BlockDefinition?.Id == null || !VanillaWeaponCompatible.Contains(cube.BlockDefinition.Id.SubtypeName)))
                            FutureEvents.Schedule(RemoveIncompatibleBlock, cube, 10);
                        return;
                    }

                    if (!SorterControls && entity is MyConveyorSorter) {
                        MyAPIGateway.Utilities.InvokeOnGameThread(() => CreateTerminalUi<IMyConveyorSorter>(this));
                        SorterControls = true;
                        if (!EarlyInitOver) ControlQueue.Enqueue(typeof(IMyConveyorSorter));
                    }
                    else if (!TurretControls && turret != null) {
                        MyAPIGateway.Utilities.InvokeOnGameThread(() => CreateTerminalUi<IMyLargeTurretBase>(this));
                        TurretControls = true;
                        if (!EarlyInitOver) ControlQueue.Enqueue(typeof(IMyLargeTurretBase));
                    }
                    else if (!FixedMissileReloadControls && controllableGun is IMySmallMissileLauncherReload) {
                        MyAPIGateway.Utilities.InvokeOnGameThread(() => CreateTerminalUi<IMySmallMissileLauncherReload>(this));
                        FixedMissileReloadControls = true;
                        if (!EarlyInitOver) ControlQueue.Enqueue(typeof(IMySmallMissileLauncherReload));
                    }
                    else if (!FixedMissileControls && controllableGun is IMySmallMissileLauncher) {
                        MyAPIGateway.Utilities.InvokeOnGameThread(() => CreateTerminalUi<IMySmallMissileLauncher>(this));
                        FixedMissileControls = true;

                        if (!EarlyInitOver) ControlQueue.Enqueue(typeof(IMySmallMissileLauncher));
                    }
                    else if (!FixedGunControls && controllableGun is IMySmallGatlingGun) {
                        MyAPIGateway.Utilities.InvokeOnGameThread(() => CreateTerminalUi<IMySmallGatlingGun>(this));
                        FixedGunControls = true;
                        if (!EarlyInitOver) ControlQueue.Enqueue(typeof(IMySmallGatlingGun));
                    }
                    else if (!TurretControllerControls && turretController != null) {
                        MyAPIGateway.Utilities.InvokeOnGameThread(() => CreateTerminalUi<IMyTurretControlBlock>(this));
                        TurretControllerControls = true;
                        if (!EarlyInitOver) ControlQueue.Enqueue(typeof(IMyTurretControlBlock));
                    }                       
                    else if (!SearchLightControls && searchLight != null)
                    {
                        MyAPIGateway.Utilities.InvokeOnGameThread(() => CreateTerminalUi<IMySearchlight>(this));
                        SearchLightControls = true;
                        if (!EarlyInitOver) ControlQueue.Enqueue(typeof(IMySearchlight));
                    }
                }

                var def = cube?.BlockDefinition.Id ?? entity.DefinitionId;
                InitComp(entity, ref def);
            }
            else if (decoy != null || camera != null || flight != null || combat != null)
            {
                lock (InitObj)
                {
                    if (decoy != null)
                    {
                        if (!DecoyControls)
                        {
                            MyAPIGateway.Utilities.InvokeOnGameThread(() => CreateDecoyTerminalUi<IMyDecoy>(this));
                            DecoyControls = true;
                            if (!EarlyInitOver) ControlQueue.Enqueue(typeof(IMyDecoy));
                        }

                        cube.AddedToScene += DecoyAddedToScene;
                        cube.OnClose += DecoyOnClose;

                    }
                    else if (camera != null)
                    {
                        if (!CameraDetected)
                        {
                            CameraDetected = true;
                            MyAPIGateway.Utilities.InvokeOnGameThread(() => CreateCameraTerminalUi<IMyCameraBlock>(this));
                            if (!EarlyInitOver) ControlQueue.Enqueue(typeof(IMyCameraBlock));
                        }

                        cube.AddedToScene += CameraAddedToScene;
                        cube.OnClose += CameraOnClose;
                    }
                    else if (flight != null)
                    {
                        flight.IsWorkingChanged += FlightBlockDirty;
                        cube.OnClose += FlightBlockOnClose;
                    }
                    else if (combat != null)
                    {
                        if (!CombatControls)
                        {
                            CombatControls = true;
                            MyAPIGateway.Utilities.InvokeOnGameThread(() => CombatBlockUi<IMyOffensiveCombatBlock>(this));
                            if (!EarlyInitOver) ControlQueue.Enqueue(typeof(IMyOffensiveCombatBlock));
                        }
                        combat.OnTargetChanged += CombatBlockTargetDirty;
                        combat.IsWorkingChanged += CombatBlockDirty;
                        combat.OnSelectedAttackPatternChanged += CombatBlockUiDirty;
                        cube.OnClose += CombatBlockOnClose;
                    }
                }
            }
        }


        private void GridGroupsOnOnGridGroupCreated(IMyGridGroupData groupData)
        {
            if (groupData.LinkType != GridLinkTypeEnum.Mechanical)
                return;

            var map = GridGroupMapPool.Count > 0 ? GridGroupMapPool.Pop() : new GridGroupMap();
            map.Type = groupData.LinkType;
            map.GroupData = groupData;
            //groupData.OnReleased += map.OnReleased;
            groupData.OnGridAdded += map.OnTopEntityAdded;
            groupData.OnGridRemoved += map.OnTopEntityRemoved;
            GridGroupMap[groupData] = map;
        }

        private void GridGroupsOnOnGridGroupDestroyed(IMyGridGroupData groupData)
        {
            if (groupData.LinkType != GridLinkTypeEnum.Mechanical)
                return;

            GridGroupMap map;
            if (GridGroupMap.TryGetValue(groupData, out map))
            {
                //groupData.OnReleased -= map.OnReleased;
                groupData.OnGridAdded -= map.OnTopEntityAdded;
                groupData.OnGridRemoved -= map.OnTopEntityRemoved;
                
                GridGroupMap.Remove(groupData);
                map.Clean();
                GridGroupMapPool.Push(map);
            }
            else 
                Log.Line($"GridGroupsOnOnGridGroupDestroyed could not find map");
        }

        private void DecoyAddedToScene(MyEntity myEntity)
        {
            var term = (IMyTerminalBlock)myEntity;
            term.CustomDataChanged += DecoyCustomDataChanged;
            term.AppendingCustomInfo += DecoyAppendingCustomInfo;
            myEntity.OnMarkForClose += DecoyOnMarkForClose;

            long value = -1;
            long.TryParse(term.CustomData, out value);
            if (value < 1 || value > 7)
                value = 1;
            DecoyMap[myEntity] = (WeaponDefinition.TargetingDef.BlockTypes)value;
        }

        private void DecoyAppendingCustomInfo(IMyTerminalBlock term, StringBuilder stringBuilder)
        {
            if (term.CustomData.Length == 1)
                DecoyCustomDataChanged(term);
        }

        private void DecoyOnMarkForClose(MyEntity myEntity)
        {
            var term = (IMyTerminalBlock)myEntity;
            term.CustomDataChanged -= DecoyCustomDataChanged;
            term.AppendingCustomInfo -= DecoyAppendingCustomInfo;
            myEntity.OnMarkForClose -= DecoyOnMarkForClose;
        }

        private void DecoyCustomDataChanged(IMyTerminalBlock term)
        {
            long value = -1;
            long.TryParse(term.CustomData, out value);

            var entity = (MyEntity)term;
            var cube = (MyCubeBlock)entity;
            if (value > 0 && value <= 7)
            {
                var newType = (WeaponDefinition.TargetingDef.BlockTypes)value;
                WeaponDefinition.TargetingDef.BlockTypes type;
                ConcurrentDictionary<WeaponDefinition.TargetingDef.BlockTypes, ConcurrentCachingList<MyCubeBlock>> blockTypes;
                if (GridToBlockTypeMap.TryGetValue(cube.CubeGrid, out blockTypes) && DecoyMap.TryGetValue(entity, out type) && type != newType)
                {
                    blockTypes[type].Remove(cube, true);
                    var addColletion = blockTypes[newType];
                    addColletion.Add(cube);
                    addColletion.ApplyAdditions();
                    DecoyMap[entity] = newType;
                }
            }
        }

        private void CameraAddedToScene(MyEntity myEntity)
        {
            var term = (IMyTerminalBlock)myEntity;
            term.CustomDataChanged += CameraCustomDataChanged;
            term.AppendingCustomInfo += CameraAppendingCustomInfo;
            myEntity.OnMarkForClose += CameraOnMarkForClose;
            CameraCustomDataChanged(term);
        }

        private void CameraOnClose(MyEntity myEntity)
        {
            myEntity.OnClose -= CameraOnClose;
            myEntity.AddedToScene -= CameraAddedToScene;
        }

        private void FlightBlockOnClose(MyEntity myEntity)
        {
            myEntity.OnClose -= FlightBlockOnClose;
            var flight = myEntity as IMyFlightMovementBlock;
            flight.IsWorkingChanged -= FlightBlockDirty;
        }

        private void CombatBlockOnClose(MyEntity myEntity)
        {
            myEntity.OnClose -= CombatBlockOnClose;
            var combat = myEntity as IMyOffensiveCombatBlock;
            combat.IsWorkingChanged -= CombatBlockDirty;
            combat.OnTargetChanged -= CombatBlockTargetDirty;
            combat.OnSelectedAttackPatternChanged -= CombatBlockUiDirty;
        }

        private void DecoyOnClose(MyEntity myEntity)
        {
            myEntity.OnClose -= DecoyOnClose;
            myEntity.AddedToScene -= DecoyAddedToScene;
        }

        private void CameraAppendingCustomInfo(IMyTerminalBlock term, StringBuilder stringBuilder)
        {
            if (term.CustomData.Length == 1)
                CameraCustomDataChanged(term);
        }

        private void CameraOnMarkForClose(MyEntity myEntity)
        {
            var term = (IMyTerminalBlock)myEntity;
            term.CustomDataChanged -= CameraCustomDataChanged;
            term.AppendingCustomInfo -= CameraAppendingCustomInfo;
            myEntity.OnMarkForClose -= CameraOnMarkForClose;
        }

        private void CameraCustomDataChanged(IMyTerminalBlock term)
        {
            var entity = (MyEntity)term;
            var cube = (MyCubeBlock)entity;
            long value = -1;
            if (long.TryParse(term.CustomData, out value))
            {
                CameraChannelMappings[cube] = value;
            }
            else
            {
                CameraChannelMappings[cube] = -1;
            }
        }

        private void AddGridToMap(MyEntity myEntity)
        {
            var grid = myEntity as MyCubeGrid;

            if (grid != null)
            {
                TopMap topMap;
                if (TopEntityToInfoMap.TryGetValue(grid, out topMap))
                {
                    var allFat = ConcurrentListPool.Get();

                    var gridFat = grid.GetFatBlocks();
                    for (int i = 0; i < gridFat.Count; i++)
                    {
                        var term = gridFat[i] as IMyTerminalBlock;
                        if (term == null) continue;

                        allFat.Add(gridFat[i]);
                    }
                    allFat.ApplyAdditions();

                    if (grid.Components.TryGet(out topMap.Targeting))
                        topMap.Targeting.AllowScanning = false;

                    topMap.MyCubeBocks = allFat;

                    grid.OnFatBlockAdded += ToGridMap;
                    grid.OnFatBlockRemoved += FromGridMap;
                    using (_dityGridLock.Acquire())
                    {
                        DirtyGridInfos.Add(grid);
                        DirtyGrid = true;
                    }
                }
                else Log.Line($"AddGridToMap could not find gridmap");
            }
        }

        private void RemoveFromMap(MyEntity myEntity)
        {
            var grid = myEntity as MyCubeGrid;
            if (grid != null)
                RemoveGridFromMap(myEntity);
            else
                RemoveOtherFromMap(myEntity);
        }

        private void RemoveGridFromMap(MyEntity myEntity)
        {
            var grid = (MyCubeGrid)myEntity;
            TopMap topMap;
            if (TopEntityToInfoMap.TryRemove(grid, out topMap))
            {
                topMap.Trash = true;
                grid.OnClose -= RemoveFromMap;
                grid.AddedToScene -= AddGridToMap;

                if (topMap.MyCubeBocks != null)
                {
                    ConcurrentListPool.Return(topMap.MyCubeBocks);
                    grid.OnFatBlockAdded -= ToGridMap;
                    grid.OnFatBlockRemoved -= FromGridMap;
                }

                topMap.GroupMap = null;
                GridMapPool.Return(topMap);

                using (_dityGridLock.Acquire())
                {
                    DirtyGridInfos.Add(grid);
                    DirtyGrid = true;
                }
            }
            else Log.Line($"grid not removed and list not cleaned: marked:{grid.MarkedForClose}({grid.Closed}) - inScene:{grid.InScene}");
        }

        internal void RemoveOtherFromMap(MyEntity myEntity)
        {
            TopMap topMap;
            if (TopEntityToInfoMap.TryRemove(myEntity, out topMap))
            {
                topMap.Trash = true;
                myEntity.OnClose -= RemoveOtherFromMap;

                topMap.GroupMap.Clean();
                GridGroupMapPool.Push(topMap.GroupMap);
                topMap.GroupMap = null;
                
                GridMapPool.Return(topMap);

            }
            else Log.Line($"RemoveOtherFromMap not removed and list not cleaned: {myEntity.DebugName}");
        }

        private void ToGridMap(MyCubeBlock myCubeBlock)
        {
            var term = myCubeBlock as IMyTerminalBlock;
            TopMap topMap;
            if (term != null && TopEntityToInfoMap.TryGetValue(myCubeBlock.CubeGrid, out topMap))
            {
                topMap.MyCubeBocks.Add(myCubeBlock);
                using (_dityGridLock.Acquire())
                {
                    DirtyGridInfos.Add(myCubeBlock.CubeGrid);
                    DirtyGrid = true;
                }
            }
            else if (term != null) Log.Line($"ToGridMap missing grid: cubeMark:{myCubeBlock.MarkedForClose} - gridMark:{myCubeBlock.CubeGrid.MarkedForClose} - name:{myCubeBlock.DebugName}");
        }

        private void FromGridMap(MyCubeBlock myCubeBlock)
        {
            var term = myCubeBlock as IMyTerminalBlock;
            TopMap topMap;
            if (term != null && TopEntityToInfoMap.TryGetValue(myCubeBlock.CubeGrid, out topMap))
            {
                topMap.MyCubeBocks.Remove(myCubeBlock);
                using (_dityGridLock.Acquire())
                {
                    DirtyGridInfos.Add(myCubeBlock.CubeGrid);
                    DirtyGrid = true;
                }
            }
            else if (term != null) Log.Line($"ToGridMap missing grid: cubeMark:{myCubeBlock.MarkedForClose} - gridMark:{myCubeBlock.CubeGrid.MarkedForClose} - name:{myCubeBlock.DebugName}");
        }
        internal void CombatBlockTargetDirty(IMyOffensiveCombatBlock block, VRage.Game.ModAPI.Ingame.IMyEntity oldTarg, VRage.Game.ModAPI.Ingame.IMyEntity newTarg, bool forced)
        {
            if (Session.IsServer)
            {
                Ai curAi;
                var cubeBlock = (MyCubeBlock)block;
                if (EntityToMasterAi.TryGetValue(cubeBlock.CubeGrid, out curAi))
                    curAi.Construct.ConstructKeenDroneCombatTargetDirty(cubeBlock, (VRage.ModAPI.IMyEntity)newTarg, (VRage.ModAPI.IMyEntity)oldTarg);
            }
        }
        internal void CombatBlockDirty(IMyCubeBlock block)
        {
            if (Session.IsServer)
            {
                Ai curAi;
                var cubeBlock = (MyCubeBlock)block;
                if (EntityToMasterAi.TryGetValue(cubeBlock.CubeGrid, out curAi))
                    curAi.Construct.ConstructKeenDroneCombatDirty(cubeBlock);
            }
        }
        internal void FlightBlockDirty(IMyCubeBlock block)
        {
            if (Session.IsServer)
            {
                Ai curAi;
                var cubeBlock = (MyCubeBlock)block;
                if (EntityToMasterAi.TryGetValue(cubeBlock.CubeGrid, out curAi))
                    curAi.Construct.ConstructKeenDroneFlightDirty(cubeBlock);
            }
        }

        public void OnCloseAll()
        {
            var list = new List<IMyGridGroupData>(GridGroupMap.Keys);
            foreach (var value in list)
                GridGroupsOnOnGridGroupDestroyed(value);
           
            MyAPIGateway.GridGroups.OnGridGroupDestroyed -= GridGroupsOnOnGridGroupDestroyed;
            MyAPIGateway.GridGroups.OnGridGroupCreated -= GridGroupsOnOnGridGroupCreated;

            GridGroupMap.Clear();
        }


        private void MenuOpened(object obj)
        {
            InMenu = true;
            MenuDepth++;
        }

        private void MenuClosed(object obj)
        {
            MenuDepth--;
            if (MenuDepth <= 0)
            {
                InMenu = false;
                HudUi.NeedsUpdate = true;
                MenuDepth = 0;
            }
        }


        private void PlayerControlNotify(MyEntity entity)
        {
            var topMost = entity.GetTopMostParent();
            Ai ai;
            if (topMost != null && EntityAIs.TryGetValue(topMost, out ai))
            {
                if (HandlesInput && ai.AiOwner == 0)
                {
                    MyAPIGateway.Utilities.ShowNotification($"Ai computer is not owned, take ownership of grid weapons! - current ownerId is: {ai.AiOwner}", 10000);
                }
            }
        }
        
        private void PlayerConnected(long id)
        {
            if (Players.ContainsKey(id)) return;
            MyAPIGateway.Multiplayer.Players.GetPlayers(null, myPlayer => FindPlayer(myPlayer, id));
        }

        private void PlayerDisconnected(long l)
        {
            PlayerEventId++;
            PlayerMap removedPlayer;
            if (Players.TryRemove(l, out removedPlayer))
            {
                long playerId;

                if (SteamToPlayer.TryGetValue(removedPlayer.Player.SteamUserId, out playerId))
                    SteamToPlayer.Remove(removedPlayer.Player.SteamUserId);

                PlayerEntityIdInRange.Remove(removedPlayer.Player.SteamUserId);
                PlayerDummyTargets.Remove(playerId);
                if (PlayerControllerMonitor.Remove(removedPlayer.Player))
                    removedPlayer.Player.Controller.ControlledEntityChanged -= OnPlayerController;

                if (IsServer && MpActive)
                    SendPlayerConnectionUpdate(l, false);

                if (AuthorIds.Contains(removedPlayer.Player.SteamUserId))
                {
                    ConnectedAuthors.Remove(playerId);
                }
            }
        }

        private bool FindPlayer(IMyPlayer player, long id)
        {
            if (player.IdentityId == id)
            {
                if (!Players.ContainsKey(id))
                    BuildPlayerMap(player, id);

                SteamToPlayer[player.SteamUserId] = id;
                PlayerDummyTargets[id] = new FakeTargets();
                PlayerEntityIdInRange[player.SteamUserId] = new HashSet<long>();

                var controller = player.Controller;
                if (controller != null && PlayerControllerMonitor.Add(player))
                {
                    controller.ControlledEntityChanged += OnPlayerController;
                    OnPlayerController(null, controller.ControlledEntity);
                }

                PlayerEventId++;
                if (AuthorIds.Contains(player.SteamUserId))
                {
                    //if (MpActive && DedicatedServer)
                    //    FutureEvents.Schedule(MovePlayer, player, 600);

                    ConnectedAuthors.Add(id, player.SteamUserId);
                }

                if (IsServer && MpActive)
                {
                    SendPlayerConnectionUpdate(id, true);
                    SendServerStartup(player.SteamUserId);
                }
                return false;
            }
            return false;
        }

        private void BuildPlayerMap(IMyPlayer player, long id)
        {
            MyTargetFocusComponent targetFocus = null;
            MyTargetLockingComponent targetLock = null;
            if (player.Character != null) {
                player.Character.Components.TryGet(out targetFocus);
                player.Character.Components.TryGet(out targetLock);
            }
            Players[id] = new PlayerMap { Player = player, PlayerId = id, TargetFocus = targetFocus, TargetLock = targetLock};
        }

        internal void OnPlayerControl(MyEntity exitEntity, MyEntity enterEntity)
        {
            try
            {
                TopMap topMap;
                var otherExitControl = exitEntity as IMyAutomaticRifleGun;
                var exitController = exitEntity as IMyControllableEntity;
                CoreComponent exitComp = null;
                if (exitController?.ControllerInfo != null || otherExitControl != null && IdToCompMap.TryGetValue(otherExitControl.EntityId, out exitComp))
                {
                    var cube = exitEntity as MyCubeBlock;
                    var topEnt = cube?.CubeGrid ?? exitComp?.TopEntity;
                    if (topEnt != null && TopEntityToInfoMap.TryGetValue(topEnt, out topMap))
                    {
                        var controlledEnt = cube ?? exitComp.CoreEntity;
                        topMap.LastControllerTick = Tick + 1;
                        long playerId;
                        if (topMap.ControlEntityPlayerMap.TryGetValue(controlledEnt, out playerId))
                        {
                            topMap.ControlEntityPlayerMap.Remove(controlledEnt);
                            var topMapPlayerRemoved = topMap.PlayerControllers.Remove(playerId);
                            if (topMap.GroupMap != null)
                            {
                                topMap.GroupMap.LastControllerTick = Tick + 1;
                                if (cube != null)
                                {
                                    var pController = new PlayerController { ControlEntity = controlledEnt, Id = playerId, EntityId = controlledEnt.EntityId, ChangeTick = Tick, LastChangeReason = PlayerController.ChangeType.Remove };
                                    topMap.GroupMap.ControlPlayerRequest[playerId] = pController;
                                }
                            }
                            else if (IsServer)
                                Log.Line($"OnPlayerController exit gridmap null");
                        }

                        Ai ai;
                        if (EntityAIs.TryGetValue(topEnt, out ai))
                        {
                            CoreComponent comp;
                            if (ai.CompBase.TryGetValue(controlledEnt, out comp))
                            {
                                var lastPlayerId = comp.LastControllingPlayerId;
                                var wComp = comp as Weapon.WeaponComponent;
                                var cComp = comp as ControlSys.ControlComponent;
                                if (wComp != null)
                                    wComp.ReleaseControl(lastPlayerId);
                                else if (cComp != null)
                                    cComp.ReleaseControl(lastPlayerId);
                            }
                        }
                    }
                }


                var otherEnterControl = enterEntity as IMyAutomaticRifleGun;
                var enterController = enterEntity as IMyControllableEntity;

                CoreComponent enterComp = null;
                if (enterController?.ControllerInfo != null || otherEnterControl != null && IdToCompMap.TryGetValue(otherEnterControl.EntityId, out enterComp))
                {
                    var cube = enterEntity as MyCubeBlock;
                    var topEnt = cube?.CubeGrid ?? enterComp?.TopEntity;
                    var playerId = enterController?.ControllerInfo?.ControllingIdentityId ?? enterComp?.Ai?.AiOwner ?? 0;
                    if (topEnt != null && playerId != 0 && TopEntityToInfoMap.TryGetValue(topEnt, out topMap))
                    {
                        var shareControl = true;
                        var controlledEnt = cube ?? enterEntity;
                        
                        Ai ai;
                        if (EntityAIs.TryGetValue(topEnt, out ai))
                        {
                            CoreComponent comp;
                            if (ai.CompBase.TryGetValue(controlledEnt, out comp))
                            {
                                var wComp = comp as Weapon.WeaponComponent;
                                var cComp = comp as ControlSys.ControlComponent;
                                if (wComp != null)
                                {
                                    shareControl = wComp.Data.Repo.Values.Set.Overrides.ShareFireControl;
                                    wComp.TookControl(playerId);
                                }
                                else if (cComp != null)
                                {
                                    shareControl = cComp.Data.Repo.Values.Set.Overrides.ShareFireControl;
                                    cComp.TookControl(playerId);
                                }
                            }
                        }

                        var pController = new PlayerController { ControlEntity = controlledEnt, Id = playerId, EntityId = controlledEnt.EntityId, ChangeTick = Tick, ShareControl = shareControl, LastChangeReason = PlayerController.ChangeType.Add};
                        topMap.LastControllerTick = Tick + 1;
                        topMap.PlayerControllers[playerId] = pController;
                        topMap.ControlEntityPlayerMap[controlledEnt] = playerId;

                        if (topMap.GroupMap != null && cube != null)
                        {
                            topMap.GroupMap.LastControllerTick = Tick + 1;
                            topMap.GroupMap.ControlPlayerRequest[playerId] = pController;
                        }
                    }
                }
            }
            catch (Exception ex) { Log.Line($"Exception in OnPlayerController: {ex}"); }
        }

        private void OnPlayerController(IMyControllableEntity exitController, IMyControllableEntity enterController)
        {
            OnPlayerControl((MyEntity) exitController, (MyEntity) enterController);
        }
    }
}
