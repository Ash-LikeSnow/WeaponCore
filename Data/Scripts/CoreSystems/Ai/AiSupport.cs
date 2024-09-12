using System;
using System.Collections.Generic;
using System.Linq;
using CoreSystems.Platform;
using CoreSystems.Projectiles;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage;
using VRage.Game;
using VRageMath;

namespace CoreSystems.Support
{
    public partial class Ai
    {
        internal void CompChange(bool add, CoreComponent comp)
        {
            int idx;
            switch (comp.Type)
            {
                case CoreComponent.CompType.Weapon:
                    var wComp = (Weapon.WeaponComponent)comp;

                    if (comp.TypeSpecific != CoreComponent.CompTypeSpecific.Phantom)
                    {
                        if (add)
                        {
                            if (WeaponIdx.ContainsKey(wComp))
                            {
                                Log.Line($"CompAddFailed:<{wComp.CoreEntity.EntityId}> - comp({wComp.CoreEntity.DebugName}[{wComp.SubtypeName}]) already existed in {TopEntity.DebugName}");
                                return;
                            }

                            WeaponIdx.Add(wComp,  WeaponComps.Count);
                            WeaponComps.Add(wComp);
                            
                            if (wComp.HasArming || wComp.IsBomb)
                                CriticalComps.Add(wComp);

                            if (wComp.Data.Repo.Values.Set.Overrides.WeaponGroupId > 0)
                                CompWeaponGroups[wComp] = wComp.Data.Repo.Values.Set.Overrides.WeaponGroupId;

                            if (WeaponTrackIdx.ContainsKey(wComp))
                            {
                                Log.Line($"CompTrackAddFailed:<{wComp.CoreEntity.EntityId}> - comp({wComp.CoreEntity.DebugName}[{wComp.SubtypeName}]) already existed in {TopEntity.DebugName}");
                                return;
                            }

                            WeaponTrackIdx.Add(wComp, TrackingComps.Count);
                            TrackingComps.Add(wComp);
                        }
                        else
                        {
                            int weaponIdx;
                            if (!WeaponIdx.TryGetValue(wComp, out weaponIdx))
                            {
                                Log.Line($"CompRemoveFailed: <{wComp.CoreEntity.EntityId}> - {WeaponComps.Count}[{WeaponIdx.Count}]({CompBase.Count}) - {WeaponComps.Contains(wComp)}[{WeaponComps.Count}] - {Session.I.EntityAIs[wComp.TopEntity].CompBase.ContainsKey(wComp.CoreEntity)} - {Session.I.EntityAIs[wComp.TopEntity].CompBase.Count} ");
                                return;
                            }

                            var wCompMaxWepRange = wComp.MaxDetectDistance;
                            WeaponComps.RemoveAtFast(weaponIdx);

                            if (wComp.HasArming || wComp.IsBomb)
                                CriticalComps.Remove(wComp);

                            if (weaponIdx < WeaponComps.Count)
                                WeaponIdx[WeaponComps[weaponIdx]] = weaponIdx;
                            WeaponIdx.Remove(wComp);

                            if (wCompMaxWepRange >= (MaxTargetingRange - TopEntity.PositionComp.LocalVolume.Radius) * 0.95) //Filter so that only the longest ranged weps force a recalc
                                UpdateMaxTargetingRange();

                            if (wComp.Data.Repo.Values.Set.Overrides.WeaponGroupId > 0)
                                CompWeaponGroups.Remove(wComp);

                            int weaponTrackIdx;
                            if (!WeaponTrackIdx.TryGetValue(wComp, out weaponTrackIdx))
                            {
                                Log.Line($"CompRemoveFailed: <{wComp.CoreEntity.EntityId}> - {WeaponComps.Count}[{WeaponIdx.Count}]({CompBase.Count}) - {WeaponComps.Contains(wComp)}[{WeaponComps.Count}] - {Session.I.EntityAIs[wComp.TopEntity].CompBase.ContainsKey(wComp.CoreEntity)} - {Session.I.EntityAIs[wComp.TopEntity].CompBase.Count} ");
                                return;
                            }

                            TrackingComps.RemoveAtFast(weaponTrackIdx);
                            if (weaponTrackIdx < TrackingComps.Count)
                                WeaponTrackIdx[TrackingComps[weaponTrackIdx]] = weaponTrackIdx;
                            WeaponTrackIdx.Remove(wComp);
                        }
                    }
                    else
                    {
                        if (add)
                        {
                            if (PhantomIdx.ContainsKey(wComp))
                            {
                                Log.Line($"CompAddFailed:<{wComp.CoreEntity.EntityId}> - comp({wComp.CoreEntity.DebugName}[{wComp.SubtypeName}]) already existed in {TopEntity.DebugName}");
                                return;
                            }

                            PhantomIdx.Add(wComp, PhantomComps.Count);
                            PhantomComps.Add(wComp);
                        }
                        else
                        {
                            if (!PhantomIdx.TryGetValue(wComp, out idx))
                            {
                                Log.Line($"CompRemoveFailed: <{wComp.CoreEntity.EntityId}> - {WeaponComps.Count}[{PhantomIdx.Count}]({CompBase.Count}) - {PhantomComps.Contains(wComp)}[{PhantomComps.Count}] - {Session.I.EntityAIs[wComp.TopEntity].CompBase.ContainsKey(wComp.CoreEntity)} - {Session.I.EntityAIs[wComp.TopEntity].CompBase.Count} ");
                                return;
                            }

                            PhantomComps.RemoveAtFast(idx);
                            if (idx < PhantomComps.Count)
                                PhantomIdx[PhantomComps[idx]] = idx;
                            PhantomIdx.Remove(wComp);
                        }
                    }


                    break;
                case CoreComponent.CompType.Upgrade:
                    var uComp = (Upgrade.UpgradeComponent)comp;

                    if (add)
                    {
                        if (UpgradeIdx.ContainsKey(uComp))
                        {
                            Log.Line($"CompAddFailed:<{uComp.CoreEntity.EntityId}> - comp({uComp.CoreEntity.DebugName}[{uComp.SubtypeName}]) already existed in {TopEntity.DebugName}");
                            return;
                        }

                        UpgradeIdx.Add(uComp, UpgradeComps.Count);
                        UpgradeComps.Add(uComp);
                    }
                    else
                    {
                        if (!UpgradeIdx.TryGetValue(uComp, out idx))
                        {
                            Log.Line($"CompRemoveFailed: <{uComp.CoreEntity.EntityId}> - {WeaponComps.Count}[{UpgradeIdx.Count}]({CompBase.Count}) - {UpgradeComps.Contains(uComp)}[{WeaponComps.Count}] - {Session.I.EntityAIs[uComp.TopEntity].CompBase.ContainsKey(uComp.CoreEntity)} - {Session.I.EntityAIs[uComp.TopEntity].CompBase.Count} ");
                            return;
                        }

                        UpgradeComps.RemoveAtFast(idx);
                        if (idx < UpgradeComps.Count)
                            UpgradeIdx[UpgradeComps[idx]] = idx;
                        UpgradeIdx.Remove(uComp);
                    }


                    break;
                case CoreComponent.CompType.Support:

                    var sComp = (SupportSys.SupportComponent)comp;
                    if (add)
                    {
                        if (SupportIdx.ContainsKey(sComp))
                        {
                            Log.Line($"CompAddFailed:<{sComp.CoreEntity.EntityId}> - comp({sComp.CoreEntity.DebugName}[{sComp.SubtypeName}]) already existed in {TopEntity.DebugName}");
                            return;
                        }
                        SupportIdx.Add(sComp, SupportComps.Count);
                        SupportComps.Add(sComp);
                    }
                    else
                    {
                        if (!SupportIdx.TryGetValue(sComp, out idx))
                        {
                            Log.Line($"CompRemoveFailed: <{sComp.CoreEntity.EntityId}> - {WeaponComps.Count}[{SupportIdx.Count}]({CompBase.Count}) - {SupportComps.Contains(sComp)}[{SupportComps.Count}] - {Session.I.EntityAIs[sComp.TopEntity].CompBase.ContainsKey(sComp.CoreEntity)} - {Session.I.EntityAIs[sComp.TopEntity].CompBase.Count} ");
                            return;
                        }

                        SupportComps.RemoveAtFast(idx);
                        if (idx < SupportComps.Count)
                            SupportIdx[SupportComps[idx]] = idx;
                        SupportIdx.Remove(sComp);
                    }
                    break;
                case CoreComponent.CompType.Control:

                    var cComp = (ControlSys.ControlComponent)comp;
                    if (add)
                    {
                        if (ControlIdx.ContainsKey(cComp))
                        {
                            Log.Line($"CompAddFailed:<{cComp.CoreEntity.EntityId}> - comp({cComp.CoreEntity.DebugName}[{cComp.SubtypeName}]) already existed in {TopEntity.DebugName}");
                            return;
                        }
                        ControlIdx.Add(cComp, ControlComps.Count);
                        ControlComps.Add(cComp);
                    }
                    else
                    {
                        if (!ControlIdx.TryGetValue(cComp, out idx))
                        {
                            Log.Line($"CompRemoveFailed: <{cComp.CoreEntity.EntityId}> - {WeaponComps.Count}[{ControlIdx.Count}]({CompBase.Count}) - {ControlComps.Contains(cComp)}[{ControlComps.Count}] - {Session.I.EntityAIs[cComp.TopEntity].CompBase.ContainsKey(cComp.CoreEntity)} - {Session.I.EntityAIs[cComp.TopEntity].CompBase.Count} ");
                            return;
                        }

                        ControlComps.RemoveAtFast(idx);
                        if (idx < ControlComps.Count)
                            ControlIdx[ControlComps[idx]] = idx;
                        ControlIdx.Remove(cComp);
                    }
                    break;
            }
        }

        private void UpdateMaxTargetingRange()
        {
            var longestRange = 0d;
            foreach(var wComp in WeaponComps)
            {
                if (wComp.MaxDetectDistance > longestRange)
                {
                    longestRange = wComp.MaxDetectDistance;
                    if (longestRange >= Session.I.Settings.Enforcement.MaxHudFocusDistance)
                        break;
                }
            }
            var expandedMaxTrajectory = longestRange + TopEntity.PositionComp.LocalVolume.Radius;
            MaxTargetingRange = MathHelperD.Min(expandedMaxTrajectory, Session.I.Settings.Enforcement.MaxHudFocusDistance);
            MaxTargetingRangeSqr = MaxTargetingRange * MaxTargetingRange;            
        }

        private static int[] GetDeck(ref int[] deck, int firstCard, int cardsToSort, int cardsToShuffle, ref XorShiftRandomStruct rng)
        {
            if (deck.Length < cardsToSort)
                deck = new int[cardsToSort * 2];

            var shuffle = cardsToShuffle > 0;

            var splitSize = shuffle && cardsToShuffle <= cardsToSort ? cardsToSort / cardsToShuffle : 0;
            var startChunk = shuffle && splitSize > 0 ? rng.Range(1, splitSize + 1) : 0;

            var end = (startChunk > 0 ? startChunk * cardsToShuffle : cardsToShuffle);
            var start = (startChunk > 0 ? end - cardsToShuffle : 0) ;
            for (int i = 0; i < cardsToSort; i++)
            {
                int j;
                if (shuffle && i >= start && i < end)
                {
                    j = rng.Range(0, i + 1);
                }
                else
                {
                    j = i;
                }

                deck[i] = deck[j];
                deck[j] = i + firstCard;
            }
            return deck;
        }

        internal List<Projectile> GetProCache(Weapon w, bool supportingPD)
        {
            var collection = !w.System.TargetSlaving ? supportingPD ? ProjectileCache : ProjectileLockedCache : ProjectileCollection;
            if (!w.System.TargetSlaving)
            {
                if (LiveProjectileTick > _pCacheTick)
                {
                    ProjectileCache.Clear();
                    ProjectileLockedCache.Clear();
                    ProjectileCache.AddRange(LiveProjectile.Keys);
                    foreach(var proj in LiveProjectile)
                    {
                        if (proj.Value)
                            ProjectileLockedCache.Add(proj.Key);
                    }
                    _pCacheTick = LiveProjectileTick;
                }
            }
            else if (!Construct.RootAi.Construct.GetExportedCollection(w, Constructs.ScanType.Projectiles))
                Log.Line($"couldn't get exported projectile collection");

            return collection;
        }

        internal void ProcessQueuedSounds()
        {
            if (Session.I.HandlesInput && Environment.CurrentManagedThreadId == Session.I.MainThreadId)
            {
                foreach (var qs in QueuedSounds.Keys.ToArray())
                {
                    switch (qs.Type)
                    {
                        case QueuedSoundEvent.SoundTypes.HardPointStart:
                            qs.Weapon.StartHardPointSound();
                            break;
                        case QueuedSoundEvent.SoundTypes.HardPointStop:
                            qs.Weapon.StopHardPointSound();
                            break;
                    }
                    byte val;
                    QueuedSounds.TryRemove(qs, out val);
                }
                /*
                for (int i = 0; i < QueuedSounds.Count; i++)
                {
                    var qs = QueuedSounds[i];
                    switch (qs.Type)
                    {
                        case QueuedSoundEvent.SoundTypes.HardPointStart:
                            qs.Weapon.StartHardPointSound();
                            break;
                        case QueuedSoundEvent.SoundTypes.HardPointStop:
                            qs.Weapon.StopHardPointSound();
                            break;
                    }
                }
                */
            }

            //QueuedSounds.Clear();
        }

        private void WeaponShootOff()
        {
            for (int i = 0; i < WeaponComps.Count; i++) {

                var comp = WeaponComps[i];
                for (int x = 0; x < comp.Collection.Count; x++) {
                    var w = comp.Collection[x];
                    w.StopReloadSound();
                    w.StopShooting();
                }
            }
        }

        internal void ResetControlRotorState()
        {
            RotorManualControlId = -1;
            ClosestFixedWeaponCompSqr = double.MaxValue;
            RotorTargetPosition = Vector3D.MaxValue;
            RotorCommandTick = 0;
        }

        internal void UpdateGridPower()
        {
            bool powered = false;
            var powerDist = (MyResourceDistributorComponent)ImyGridEntity.ResourceDistributor;
            if (powerDist != null && powerDist.SourcesEnabled != MyMultipleEnabledEnum.NoObjects && powerDist.ResourceState != MyResourceStateEnum.NoPower)
            {
                GridMaxPower = powerDist.MaxAvailableResourceByType(GId, GridEntity);
                GridCurrentPower = powerDist.TotalRequiredInputByType(GId, GridEntity);
                if (Session.I.ShieldApiLoaded && ShieldBlock != null)
                {
                    var shieldPower = Session.I.SApi.GetPowerUsed(ShieldBlock);
                    GridCurrentPower -= shieldPower;
                }
                powered = true;
            }

            if (!powered)
            {

                if (HadPower)
                    WeaponShootOff();

                GridCurrentPower = 0;
                GridMaxPower = 0;
                GridAvailablePower = 0;

                HadPower = HasPower;
                HasPower = false;
                return;
            }

            if (Session.I.Tick60) {

                BatteryMaxPower = 0;
                BatteryCurrentOutput = 0;
                BatteryCurrentInput = 0;

                foreach (var battery in Batteries) {

                    if (!battery.IsWorking) continue;
                    var currentInput = battery.CurrentInput;
                    var currentOutput = battery.CurrentOutput;
                    var maxOutput = battery.MaxOutput;

                    if (currentInput > 0) {
                        BatteryCurrentInput += currentInput;
                        if (battery.IsCharging) BatteryCurrentOutput -= currentInput;
                        else BatteryCurrentOutput -= currentInput;
                    }
                    BatteryMaxPower += maxOutput;
                    BatteryCurrentOutput += currentOutput;
                }
            }

            GridAvailablePower = GridMaxPower - GridCurrentPower;

            GridCurrentPower += BatteryCurrentInput;
            GridAvailablePower -= BatteryCurrentInput;
            UpdatePowerSources = false;

            HadPower = HasPower;
            HasPower = GridMaxPower > 0;

            if (Session.I.Tick60 && HasPower) {
                var nearMax = GridMaxPower * 0.97;
                var halfMax = GridMaxPower * 0.5f;
                if (GridCurrentPower > nearMax && GridAssignedPower > halfMax)
                    Charger.Rebalance = true;
            }
            if (Session.I.Tick20 && HasPower)
            {
                if (Charger.TotalDesired > GridAssignedPower && GridAvailablePower > GridMaxPower * 0.1f)
                    Charger.Rebalance = true;
            }

            if (HasPower) return;
            if (HadPower)
                WeaponShootOff();
        }

        private void ForceCloseAiInventories()
        {
            foreach (var pair in InventoryMonitor)
                InventoryRemove(pair.Key, pair.Value);
            
            if (InventoryMonitor.Count > 0) {
                Log.Line($"Found stale inventories during AI close - failedToRemove:{InventoryMonitor.Count}");
                InventoryMonitor.Clear();
            }

        }
        
        internal void AiDelayedClose()
        {
            if (TopEntity == null || Closed) {
                Log.Line($"AiDelayedClose: Grid is null {TopEntity == null}  - Closed: {Closed}");
                return;
            }

            if (!ScanInProgress && Session.I.Tick - ProjectileTicker > 29 && AiMarkedTick != uint.MaxValue && Session.I.Tick - AiMarkedTick > 29) {

                using (DbLock.AcquireExclusiveUsing())
                {
                    if (ScanInProgress)
                        return;

                    CleanUp();
                    Session.I.AiPool.Push(this);
                }
            }
        }

        internal void AiForceClose()
        {
            if (TopEntity == null || Closed) {
                Log.Line($"AiDelayedClose: - Grid is null {TopEntity == null} - Closed: {Closed}");
                return;
            }

            RegisterMyGridEvents(false, true);
            
            CleanUp();
            Session.I.AiPool.Push(this);
        }

        internal void CleanSortedTargets()
        {
            for (int i = 0; i < SortedTargets.Count; i++)
            {
                var tInfo = SortedTargets[i];
                tInfo.Target = null;
                tInfo.MyAi = null;
                tInfo.TargetAi = null;
                Session.I.TargetInfoPool.Return(tInfo);
            }
            SortedTargets.Clear();
        }

        internal void UpdateFactionColors()
        {
            if (AiOwner != 0)
            {
                var aiFaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(AiOwner);
                if (aiFaction != null)
                {
                    BgFactionColor = MyColorPickerConstants.HSVOffsetToHSV(aiFaction.CustomColor).HSVtoColor().ToVector4().ToLinearRGB();
                    BgFactionColor *= 100;
                    BgFactionColor.W *= 0.01f;
                    FgFactionColor = MyColorPickerConstants.HSVOffsetToHSV(aiFaction.IconColor).HSVtoColor().ToVector4().ToLinearRGB();
                    FgFactionColor *= 100;
                    FgFactionColor.W *= 0.01f;
                    AiOwnerFactionId = aiFaction.FactionId;
                }
                else
                {
                    BgFactionColor = Vector4.Zero;
                    FgFactionColor = Vector4.Zero;
                    AiOwnerFactionId = 0;
                }
            }
            else
            {
                BgFactionColor = Vector4.Zero;
                FgFactionColor = Vector4.Zero;
                AiOwnerFactionId = 0;
            }
        }

        internal void CleanUp()
        {
            AiCloseTick = Session.I.Tick;

            TopEntity.Components.Remove<AiComponent>();

            if (Session.I.IsClient)
                Session.I.SendUpdateRequest(TopEntity.EntityId, PacketType.ClientAiRemove);

            Data.Repo.ActiveTerminal = 0;
            Charger.Clean();

            CleanSortedTargets();
            Construct.Clean();
            Obstructions.Clear();
            ObstructionsTmp.Clear();
            TargetAis.Clear();
            TargetAisTmp.Clear();
            EntitiesInRange.Clear();
            Batteries.Clear();
            Targets.Clear();
            WeaponAmmoCountStorage.Clear();
            PartCounting.Clear();
            TrackingComps.Clear();
            PlayerControl.Clear();
            WeaponComps.Clear();
            CriticalComps.Clear();
            UpgradeComps.Clear();
            SupportComps.Clear();
            ControlComps.Clear();
            PhantomComps.Clear();
            WeaponIdx.Clear();
            WeaponTrackIdx.Clear();
            SupportIdx.Clear();
            ControlIdx.Clear();
            UpgradeIdx.Clear();
            PhantomIdx.Clear();
            CompBase.Clear();
            Stators.Clear();
            Tools.Clear();
            AiOffense.Clear();
            AiFlight.Clear();
            QueuedSounds.Clear();
            ProjectileCache.Clear();
            ProjectileLockedCache.Clear();
            CompWeaponGroups.Clear();
            SortedTargets.Clear();
            LiveProjectile.Clear();
            DeadProjectiles.Clear();
            NearByShieldsTmp.Clear();
            NearByFriendlyShields.Clear();
            NearByFriendlyShieldsCache.Clear();
            StaticsInRangeTmp.Clear();
            TestShields.Clear();
            NewEntities.Clear();
            SubGridsRegistered.Clear();
            ObstructionLookup.Clear();
            ThreatCollection.Clear();
            ProjectileCollection.Clear();
            NonThreatCollection.Clear();
            SourceCount = 0;
            PartCount = 0;
            AiOwner = 0;
            AiOwnerFactionId = 0;
            LastAddToRotorTick = 0;
            ProjectileTicker = 0;
            NearByEntities = 0;
            NearByEntitiesTmp = 0;
            MyProjectiles = 0;
            ClosestFixedWeaponCompSqr = double.MaxValue;
            RotorTargetPosition = Vector3D.MaxValue;
            FgFactionColor = Vector4.Zero;
            BgFactionColor = Vector4.Zero;

            RotorManualControlId = -1;
            RotorCommandTick = 0;
            PointDefense = false;
            FadeOut = false;
            UpdatePowerSources = false;
            DbReady = false;
            AiInit = false;
            TouchingWater = false;
            BlockMonitoring = false;
            ShieldFortified = false;
            EnemiesNear = false;
            EnemyEntities = false;
            EnemyProjectiles = false;
            Data.Clean();
            RootComp = null;
            OnlyWeaponComp = null;
            GridEntity = null;
            ImyGridEntity = null;
            MyShield = null;
            MyPlanetTmp = null;
            MyPlanet = null;
            ShieldBlock = null;
            LastTerminal = null;
            TopEntity = null;
            TopEntityMap = null;
            ControlComp = null;
            Closed = true;
            CanShoot = true;
            Version++;
        }
    }
}
