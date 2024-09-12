using System.Collections.Generic;
using CoreSystems.Support;
using Sandbox.Game.Entities;
using VRage.Game.ModAPI;

namespace CoreSystems
{
    public partial class Session
    {

        private void DeferedUpBlockTypeCleanUp(bool force = false)
        {
            foreach (var clean in BlockTypeCleanUp)
            {
                if (force || Tick - clean.RequestTick > 120)
                {
                    foreach (var item in clean.Collection)
                        ConcurrentListPool.Return(item.Value);
                    clean.Collection.Clear();

                    BlockTypePool.Return(clean.Collection);

                    DeferedTypeCleaning removed;
                    BlockTypeCleanUp.TryDequeue(out removed);
                }
            }
        }

        internal void PurgeAll()
        {
            PurgedAll = true;
            FutureEvents.Purge((int)Tick);

            var purgeGroupList = new List<IMyGridGroupData>(GridGroupMap.Keys);
            foreach (var data in purgeGroupList)
                GridGroupsOnOnGridGroupDestroyed(data);

            foreach (var comp in CompsToStart)
                if (comp?.Platform != null)
                    CloseComps(comp.CoreEntity);

            foreach (var readd in CompReAdds)
            {
                if (!readd.Ai.Closed) readd.Ai.AiForceClose();
                if (readd.Comp?.Platform != null)
                {
                    CloseComps(readd.Comp.CoreEntity);
                }
            }

            foreach (var comp in CompsDelayedInit)
            {
                if (comp?.Platform != null)
                    CloseComps(comp.CoreEntity);
            }

            foreach (var comp in CompsDelayedReInit)
            {
                if (comp?.Platform != null)
                    CloseComps(comp.CoreEntity);
            }

            foreach (var gridAi in DelayedAiClean)
            {
                if (!gridAi.Closed)
                    gridAi.AiForceClose();
            }

            PlatFormPool.Clean();
            CompsToStart.ClearImmediate();
            DelayedAiClean.ClearImmediate();

            CompsDelayedInit.Clear();
            CompsDelayedReInit.Clear();

            CompReAdds.Clear();

            foreach (var a in AiPool)
            {
                if (a.Closed)
                    continue;
                a.CleanUp();
            }
            AiPool.Clear();

            for (int i = 0; i < DamageBlockCache.Length; i++)
            {
                DamageBlockCache[i].Clear();
                DamageBlockCache[i] = null;
            }


            PurgeTerminalSystem(this);
            HudUi.Purge();
            TerminalMon.Purge();
            foreach (var reports in Reporter.ReportData.Values)
            {
                foreach (var report in reports)
                {
                    report.Clean();
                    Reporter.ReportPool.Return(report);
                }
                reports.Clear();
            }
            Reporter.ReportData.Clear();
            Reporter.ReportPool.Clean();

            PacketsToClient.Clear();
            PacketsToServer.Clear();

            AcqManager.Clean();

            CleanSounds(true);

            foreach (var e in Emitters)
                e.StopSound(true);
            foreach (var e in Av.PersistentEmitters)
                e.StopSound(true);
            foreach (var e in Av.FireEmitters)
                e.StopSound(true);
            foreach (var e in Av.TravelEmitters)
                e.StopSound(true);

            Emitters.Clear();

            foreach (var item in EffectedCubes)
            {
                var cubeid = item.Key;
                var blockInfo = item.Value;
                var functBlock = blockInfo.FunctBlock;
                var cube = (MyCubeBlock)blockInfo.FunctBlock;
                if (functBlock?.SlimBlock == null || functBlock.SlimBlock.IsDestroyed || cube == null || cube.MarkedForClose || cube.Closed || cube.CubeGrid.MarkedForClose || !cube.IsFunctional || !cube.InScene) { // keen is failing to check for null when they null out functional block types
                    _effectPurge.Enqueue(cubeid);
                    continue;
                }

                functBlock.EnabledChanged -= ForceDisable;
                functBlock.Enabled = blockInfo.FirstState;
                functBlock.SetDamageEffect(false);
                _effectPurge.Enqueue(cubeid);
            }

            while (_effectPurge.Count != 0)
            {
                EffectedCubes.Remove(_effectPurge.Dequeue());
            }

            DeferedUpBlockTypeCleanUp(true);
            BlockTypeCleanUp.Clear();


            GridGroupMap.Clear();
            GridGroupMapPool.Clear();

            foreach (var p in PlayerControllerMonitor) {
                var controller = p.Controller;
                controller.ControlledEntityChanged -= OnPlayerController;
            }

            foreach (var ent in TopEntityToInfoMap.Keys)
                RemoveFromMap(ent);

            TopEntityToInfoMap.Clear();
            GridMapPool.Clean();

            DirtyGridsTmp.Clear();
            PlayersToAdd.Clear();
            WeaponValuesMap.Clear();
            AmmoValuesMap.Clear();

            foreach (var structure in PartPlatforms.Values) {
                foreach (var pair in structure.PartSystems) {
                    var system = pair.Value as WeaponSystem;
                    if (system != null) {
                        foreach (var ammo in system.AmmoTypes) {
                            ammo.AmmoDef.Const.Purge();
                        }
                    }
                }
                structure.PartSystems.Clear();
            }

            TriggerEntityPool.Clean();
            PartPlatforms.Clear();

            foreach (var gridToMap in GridToBlockTypeMap)
            {
                foreach (var map in gridToMap.Value)
                {
                    ConcurrentListPool.Return(map.Value);
                }
                gridToMap.Value.Clear();
                BlockTypePool.Return(gridToMap.Value);
            }
            GridToBlockTypeMap.Clear();

            foreach(var playerGrids in PlayerEntityIdInRange)
                playerGrids.Value.Clear();

            foreach (var phantomType in PhantomDatabase.Values)
                phantomType.Clear();
            PhantomDatabase.Clear();

            foreach (var ammoMaps in AmmoMaps.Values)
                ammoMaps.Clear();
            AmmoMaps.Clear();

            ModelMaps.Clear();

            PlayerEntityIdInRange.Clear();
            using (_dityGridLock.Acquire())
                DirtyGridInfos.Clear();

            foreach (var s in SoundsToClean)
                s.EmitterPool?.Clear();
            SoundsToClean.Clear();

            foreach (var c in CameraChannelMappings) {
                CameraOnClose(c.Key);
                CameraOnMarkForClose(c.Key);
            }
            CameraChannelMappings.Clear();

            DsUtil.Purge();
            DsUtil2.Purge();
            ProblemRep.Clean();

            PhysicalItemListPool.Clean();
            BetterItemsListPool.Clean();
            BetterInventoryItems.Clean();
            PowerGroups.Clear();
            KeyMap.Clear();
            LosDebugList.Clear();
            _gridsNearCamera.Clear();
            PartPlatforms.Clear();
            DelayedAiClean.ClearImmediate();
            ShootingWeapons.Clear();
            PartToPullConsumable.Clear();
            ConsumableToPullQueue.Clear();
            AimingAi.Clear();
            ChargingParts.Clear();
            Hits.Clear();
            HomingWeapons.Clear();
            EntityToMasterAi.Clear();
            Players.Clear();
            IdToCompMap.Clear();
            AllArmorBaseDefinitions.Clear();
            HeavyArmorBaseDefinitions.Clear();
            AllArmorBaseDefinitions.Clear();
            AcquireTargets.Clear();
            AnimationsToProcess.Clear();
            _subTypeIdWeaponDefs.Clear();
            WeaponDefinitions.Clear();
            SlimsSortedList.Clear();
            _destroyedSlims.Clear();
            _destroyedSlimsClient.Clear();
            _slimHealthClient.Clear();
            _subTypeMaps.Clear();
            _tmpNearByBlocks.Clear();



            foreach (var errorpkt in ClientSideErrorPkt)
                errorpkt.Packet.CleanUp();
            ClientSideErrorPkt.Clear();

            GridEffectPool.Clean();
            GridEffectsPool.Clean();
            BlockTypePool.Clean();
            ConcurrentListPool.Clean();

            TargetInfoPool.Clean();
            PacketObjPool.Clean();

            InventoryMoveRequestPool.Clean();
            CoreSystemsDefs.Clear();
            VanillaIds.Clear();
            VanillaCoreIds.Clear();
            CoreSystemsFixedBlockDefs.Clear();
            CoreSystemsTurretBlockDefs.Clear();
            CoreSystemsUpgradeDefs.Clear();
            CoreSystemsSupportDefs.Clear();
            CoreSystemsRifleDefs.Clear();
            CoreSystemsPhantomDefs.Clear();

            VoxelCaches.Clear();
            ArmorCubes.Clear();

            Av.Clean();

            Projectiles.Clean();

            DbsToUpdate.Clear();
            EntityAIs.Clear();

            DsUtil = null;
            DsUtil2 = null;
            SlimsSortedList = null;
            Settings = null;
            StallReporter = null;
            TerminalMon = null;
            Physics = null;
            Camera = null;
            Projectiles = null;
            TrackingAi = null;
            UiInput = null;
            TargetUi = null;
            Placer = null;
            SApi.Unload();
            if (WaterApiLoaded)
                WApi.Unregister();
            SApi = null;
            Api = null;
            ApiServer = null;
            Reporter = null;
            WeaponDefinitions = null;
            AnimationsToProcess = null;
            ProjectileTree.Clear();
            ProjectileTree = null;
            Av = null;
            HudUi = null;
            AllDefinitions = null;
            SoundDefinitions = null;
            ActiveCockPit = null;
            ActiveControlBlock = null;
            PlayerHandWeapon = null;
            ControlledEntity = null;
            TmpStorage = null;
            ApiServer = null;
        }
    }
}
