using System;
using System.Collections.Generic;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Interfaces;
using Sandbox.ModAPI;
using VRage;
using VRage.Collections;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;

namespace CoreSystems.Support
{
    public partial class Ai
    {
        internal void RegisterMyGridEvents(bool register, bool force = false)
        {
            if (register) {

                if (Registered)
                    Log.Line("Ai RegisterMyGridEvents error");

                Registered = true;
                if (AiType == AiTypes.Grid)
                {
                    GridEntity.OnFatBlockAdded += FatBlockAdded;
                    GridEntity.OnFatBlockRemoved += FatBlockRemoved;

                    if (SubGridsRegistered.ContainsKey(GridEntity))
                        Log.Line("Main Grid Already Registered");

                    SubGridsRegistered[GridEntity] = byte.MaxValue;

                }

                TopEntity.OnMarkForClose += GridClose;


            }
            else {

                if (Registered) {

                    Registered = false;

                    if (AiType == AiTypes.Grid)
                    {
                        GridEntity.OnFatBlockAdded -= FatBlockAdded;
                        GridEntity.OnFatBlockRemoved -= FatBlockRemoved;
                        
                        if (BlockMonitoring)
                            DelayedEventRegistration();

                        if (!SubGridsRegistered.ContainsKey(GridEntity))
                            Log.Line("Main Grid Already UnRegistered");
                        SubGridsRegistered.Remove(GridEntity);
                    }

                    TopEntity.OnMarkForClose -= GridClose;


                }
                else if (!force) Log.Line($"NotRegistered:- Aimarked:{MarkedForClose} - aiClosed:{Closed} - Ticks:{Session.I.Tick - AiCloseTick} - topMarked:{TopEntity.MarkedForClose}");
            }
        }

        internal void DelayedEventRegistration(bool register = false)
        {
            if (AiType == AiTypes.Grid)
            {
                if (register && Registered)
                {
                    BlockMonitoring = true;
                    GridEntity.OnBlockAdded += OnBlockAdded;
                    GridEntity.OnBlockRemoved += OnBlockRemoved;
                    GridEntity.OnBlockIntegrityChanged += OnBlockIntegrityChanged;
                    LastBlockChangeTick = Session.I.Tick > 0 ? Session.I.Tick : 1;

                }
                else if (!register && Registered)
                {
                    GridEntity.OnBlockAdded -= OnBlockAdded;
                    GridEntity.OnBlockRemoved -= OnBlockRemoved;
                    GridEntity.OnBlockIntegrityChanged -= OnBlockIntegrityChanged;
                }
                else
                {
                    Log.Line($"DelayedEventRegistration failed: {register} - {Registered}");
                }
            }
            else
                Log.Line("DelayedEventRegistration failed no grid");
        }

        internal void OnBlockAdded(IMySlimBlock slim)
        {
            BlockChangeArea.Min = Vector3.Min(BlockChangeArea.Min, slim.Min);
            BlockChangeArea.Max = Vector3.Max(BlockChangeArea.Max, slim.Max);
            AddedBlockPositions[slim.Position] = slim;

            LastBlockChangeTick = Session.I.Tick;
        }

        internal void OnBlockRemoved(IMySlimBlock slim)
        {

            BlockChangeArea.Min = Vector3.Min(BlockChangeArea.Min, slim.Min);
            BlockChangeArea.Max = Vector3.Max(BlockChangeArea.Max, slim.Max);
            RemovedBlockPositions[slim.Position] = slim;

            LastBlockChangeTick = Session.I.Tick;
        }

        internal void OnBlockIntegrityChanged(IMySlimBlock mySlimBlock)
        {

        }

        internal void FatBlockAdded(MyCubeBlock cube)
        {
            var stator = cube as IMyMotorStator;
            var tool = cube as IMyShipToolBase;
            var offense = cube as IMyOffensiveCombatBlock;
            var flight = cube as IMyFlightMovementBlock;
            if (stator != null || tool != null || offense != null || flight != null)
            {
                if (stator != null)
                    Stators.Add(stator);

                if (tool != null)
                    Tools.Add(tool);

                if (offense != null)
                    AiOffense.Add(offense);

                if (flight != null)
                    AiFlight.Add(flight);

                return;
            }

            var battery = cube as MyBatteryBlock;
            var weaponType = (cube is MyConveyorSorter || cube is IMyUserControllableGun);
            var isWeaponBase = weaponType && cube.BlockDefinition != null && (Session.I.VanillaIds.ContainsKey(cube.BlockDefinition.Id) || Session.I.PartPlatforms.ContainsKey(cube.BlockDefinition.Id));

            if (!isWeaponBase && (cube is MyConveyor || cube is IMyConveyorTube || cube is MyConveyorSorter || cube is MyCargoContainer || cube is MyCockpit || cube is IMyAssembler || cube is IMyShipConnector) && cube.CubeGrid.IsSameConstructAs(GridEntity)) { 
                
                MyInventory inventory;
                if (cube.HasInventory && cube.TryGetInventory(out inventory))
                {
                    var assembler = cube as IMyAssembler;
                    if (assembler != null)
                        inventory = assembler.GetInventory(1) as MyInventory;

                    if (inventory != null && InventoryMonitor.TryAdd(cube, inventory))
                    {
                        inventory.InventoryContentChanged += CheckAmmoInventory;
                        Construct.RootAi.Construct.NewInventoryDetected = true;

                        int monitors;
                        if (!Session.I.InventoryMonitors.TryGetValue(inventory, out monitors))
                        {

                            Session.I.InventoryMonitors[inventory] = 0;
                            Session.I.InventoryItems[inventory] = Session.I.PhysicalItemListPool.Get();
                            Session.I.ConsumableItemList[inventory] = Session.I.BetterItemsListPool.Get();
                        }
                        else
                            Session.I.InventoryMonitors[inventory] = monitors + 1;
                    }
                }
            }
            else if (battery != null) {
                if (Batteries.Add(battery)) SourceCount++;
                UpdatePowerSources = true;
            } 
            else if (isWeaponBase)
            {
                MyOrientedBoundingBoxD b;
                BoundingSphereD s;
                MyOrientedBoundingBoxD blockBox;
                SUtils.GetBlockOrientedBoundingBox(cube, out blockBox);
                if (!ModOverride && Session.I.IsPartAreaRestricted(cube.BlockDefinition.Id.SubtypeId, blockBox, cube.CubeGrid, cube.EntityId, null, out b, out s))
                {
                    if (Session.I.IsServer)
                    {
                        Session.I.FutureEvents.Schedule(QueuedBlockRemoval, cube, 10);
                        //cube.CubeGrid.RemoveBlock(cube.SlimBlock, true);
                    }
                }

                //Projected block ammo removal
                var slim = cube.SlimBlock as IMySlimBlock;
                if (slim.BuildLevelRatio < 1 && cube.Storage != null && cube.Storage.ContainsKey(Session.I.CompDataGuid) && slim.ComponentStack.GetComponentStackInfo(0).MountedCount == 1)
                {
                    ProtoWeaponRepo load = null;
                    string rawData;
                    if (cube.Storage.TryGetValue(Session.I.CompDataGuid, out rawData))
                    {
                        try
                        {
                            var base64 = Convert.FromBase64String(rawData);
                            load = MyAPIGateway.Utilities.SerializeFromBinary<ProtoWeaponRepo>(base64);
                            foreach (var ammo in load.Ammos)
                            {
                                ammo.CurrentAmmo = 0;
                                ammo.CurrentCharge = 0;
                            }
                            var save = MyAPIGateway.Utilities.SerializeToBinary<ProtoWeaponRepo>(load);
                            var base64Out = Convert.ToBase64String(save);
                            cube.Storage[Session.I.CompDataGuid] = base64Out;
                        }
                        catch (Exception e)
                        {
                            Log.Line("WeaponCore failed to strip ammo from projector placed block\n" + e);
                        }
                    }
                }
            }
        }

        private void QueuedBlockRemoval(object o)
        {
            var cube = o as MyCubeBlock;
            if (cube != null)
                cube.CubeGrid.RemoveBlock(cube.SlimBlock, true);
        }

        private void FatBlockRemoved(MyCubeBlock cube)
        {
            var stator = cube as IMyMotorStator;
            var tool = cube as IMyShipToolBase;
            var offense = cube as IMyOffensiveCombatBlock;
            var flight = cube as IMyFlightMovementBlock;

            if (stator != null || tool != null || offense != null || flight != null)
            {
                LastAddToRotorTick = Session.I.Tick;

                if (stator != null)
                    Stators.Remove(stator);

                if (tool != null)
                    Tools.Remove(tool);

                if (offense != null)
                    AiOffense.Remove(offense);

                if (flight != null)
                    AiFlight.Remove(flight);

                return;
            }

            var weaponType = (cube is MyConveyorSorter || cube is IMyUserControllableGun);
            var cubeDef = cube.BlockDefinition;
            var isWeaponBase = weaponType && cubeDef != null && (Session.I.VanillaIds.ContainsKey(cubeDef.Id) || Session.I.PartPlatforms.ContainsKey(cubeDef.Id));
            var battery = cube as MyBatteryBlock;
            MyInventory inventory;

            if (!isWeaponBase && cube.HasInventory && cube.TryGetInventory(out inventory)) {

                var assembler = cube as IMyAssembler;
                if (assembler != null)
                    inventory = assembler.GetInventory(1) as MyInventory;

                if (inventory != null && !InventoryRemove(cube, inventory))
                    Log.Line($"FatBlock inventory remove failed: {cube.BlockDefinition?.Id.SubtypeName} - gridMatch:{cube.CubeGrid == TopEntity} - aiMarked:{MarkedForClose} - {cube.CubeGrid.DebugName} - {TopEntity?.DebugName}");
            }
            else if (battery != null)
            {

                if (Batteries.Remove(battery))
                    SourceCount--;

                UpdatePowerSources = true;
            }

        }
        
        
        private bool InventoryRemove(MyEntity entity, MyInventory inventory)
        {
            MyInventory oldInventory;
            if (InventoryMonitor.TryRemove(entity, out oldInventory)) {

                inventory.InventoryContentChanged -= CheckAmmoInventory;

                int monitors;
                if (Session.I.InventoryMonitors.TryGetValue(inventory, out monitors)) {

                    if (--monitors < 0) {

                        MyConcurrentList<MyPhysicalInventoryItem> removedPhysical;
                        MyConcurrentList<Session.BetterInventoryItem> removedBetter;

                        if (Session.I.InventoryItems.TryRemove(inventory, out removedPhysical))
                            Session.I.PhysicalItemListPool.Return(removedPhysical);

                        if (Session.I.ConsumableItemList.TryRemove(inventory, out removedBetter))
                            Session.I.BetterItemsListPool.Return(removedBetter);

                        Session.I.InventoryMonitors.Remove(inventory);
                    }
                    else Session.I.InventoryMonitors[inventory] = monitors;
                }
                else return false;
            }
            return true;
        }

        internal void CheckAmmoInventory(MyInventoryBase inventory, MyPhysicalInventoryItem item, MyFixedPoint amount)
        {
            if (amount <= 0 || item.Content == null || inventory == null) return;
            var itemDef = item.Content.GetObjectId();
            if (Session.I.AmmoDefIds.ContainsKey(itemDef))
            {
                Construct.RootAi?.Construct.RecentItems.Add(itemDef);
            }
        }

        internal void GridClose(MyEntity myEntity)
        {
            if (TopEntity == null || Closed)
            {
                Log.Line($"[GridClose]  MyGrid:{TopEntity != null} - Closed:{Closed} - myEntity:{myEntity != null}");
                return;
            }

            MarkedForClose = true;
            AiMarkedTick = Session.I.Tick;

            RegisterMyGridEvents(false);

            CleanSubGrids();
            ForceCloseAiInventories();

            Session.I.DelayedAiClean.Add(this);
            Session.I.DelayedAiClean.ApplyAdditions();
        }
    }
}
