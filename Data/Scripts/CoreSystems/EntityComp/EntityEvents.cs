using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using CoreSystems.Platform;
using CoreSystems.Projectiles;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRageMath;
using static CoreSystems.Platform.CorePlatform;
using static CoreSystems.Session;

namespace CoreSystems.Support
{
    public partial class CoreComponent
    {
        internal void RegisterEvents(bool register = true)
        {
            if (register)
            {
                if (Registered)
                    Log.Line("BaseComp RegisterEvents error");
                //TODO change this
                Registered = true;
                if (IsBlock)
                {
                    if (Type == CompType.Weapon)
                        TerminalBlock.AppendingCustomInfo += AppendingCustomInfoWeapon;
                    else if (TypeSpecific == CompTypeSpecific.Support)
                        TerminalBlock.AppendingCustomInfo += AppendingCustomInfoSupport;
                    else if (TypeSpecific == CompTypeSpecific.Upgrade)
                        TerminalBlock.AppendingCustomInfo += AppendingCustomInfoUpgrade;
                    else if (TypeSpecific == CompTypeSpecific.Control)
                        TerminalBlock.AppendingCustomInfo += AppendingCustomInfoControl;

                    Cube.IsWorkingChanged += IsWorkingChanged;
                    IsWorkingChanged(Cube);
                }

                if (CoreInventory == null)
                {
                    if (TypeSpecific != CompTypeSpecific.Phantom && TypeSpecific != CompTypeSpecific.Control && TypeSpecific != CompTypeSpecific.SearchLight && !IsBomb)
                        Log.Line("BlockInventory is null");
                }
                else
                {
                    CoreInventory.InventoryContentChanged += OnContentsChanged;
                    I.CoreInventoryItems[CoreInventory] = new ConcurrentDictionary<uint, BetterInventoryItem>();
                    I.ConsumableItemList[CoreInventory] = I.BetterItemsListPool.Get();

                    var items = CoreInventory.GetItems();
                    for (int i = 0; i < items.Count; i++)
                    {
                        var bItem = I.BetterInventoryItems.Get();
                        var item = items[i];
                        bItem.Amount = (int)item.Amount;
                        bItem.Item = item;
                        bItem.Content = item.Content;

                        I.CoreInventoryItems[CoreInventory][items[i].ItemId] = bItem;
                    }
                }
            }
            else
            {
                if (!Registered)
                    Log.Line("BaseComp UnRegisterEvents error");

                if (Registered)
                {
                    //TODO change this
                    Registered = false;

                    if (IsBlock)
                    {

                        if (Type == CompType.Weapon)
                            TerminalBlock.AppendingCustomInfo -= AppendingCustomInfoWeapon;
                        else if (TypeSpecific == CompTypeSpecific.Support)
                            TerminalBlock.AppendingCustomInfo -= AppendingCustomInfoSupport;
                        else if (TypeSpecific == CompTypeSpecific.Upgrade)
                            TerminalBlock.AppendingCustomInfo -= AppendingCustomInfoUpgrade;
                        else if (TypeSpecific == CompTypeSpecific.Control)
                            TerminalBlock.AppendingCustomInfo -= AppendingCustomInfoControl;
                        Cube.IsWorkingChanged -= IsWorkingChanged;
                    }

                    if (CoreInventory == null)
                    {
                        if (TypeSpecific != CompTypeSpecific.Control && TypeSpecific != CompTypeSpecific.Phantom && TypeSpecific != CompTypeSpecific.SearchLight && !IsBomb)
                            Log.Line("BlockInventory is null");
                    }
                    else
                    {
                        CoreInventory.InventoryContentChanged -= OnContentsChanged;
                        ConcurrentDictionary<uint, BetterInventoryItem> removedItems;
                        MyConcurrentList<BetterInventoryItem> removedList;

                        if (I.CoreInventoryItems.TryRemove(CoreInventory, out removedItems))
                        {
                            foreach (var inventoryItems in removedItems)
                                I.BetterInventoryItems.Return(inventoryItems.Value);

                            removedItems.Clear();
                        }

                        if (I.ConsumableItemList.TryRemove(CoreInventory, out removedList))
                            I.BetterItemsListPool.Return(removedList);
                    }
                }
            }
        }

        private void OnContentsChanged(MyInventoryBase inv, MyPhysicalInventoryItem item, MyFixedPoint amount)
        {
            BetterInventoryItem cachedItem;
            if (!I.CoreInventoryItems[CoreInventory].TryGetValue(item.ItemId, out cachedItem))
            {
                cachedItem = I.BetterInventoryItems.Get();
                cachedItem.Amount = (int)amount;
                cachedItem.Content = item.Content;
                cachedItem.Item = item;
                I.CoreInventoryItems[CoreInventory].TryAdd(item.ItemId, cachedItem);
            }
            else if (cachedItem.Amount + amount > 0)
            {
                cachedItem.Amount += (int)amount;
            }
            else if (cachedItem.Amount + amount <= 0)
            {
                BetterInventoryItem removedItem;
                if (I.CoreInventoryItems[CoreInventory].TryRemove(item.ItemId, out removedItem))
                    I.BetterInventoryItems.Return(removedItem);
            }
            var collection = TypeSpecific != CompTypeSpecific.Phantom ? Platform.Weapons : Platform.Phantoms;
            if (I.IsServer && amount <= 0) {
                for (int i = 0; i < collection.Count; i++)
                    collection[i].CheckInventorySystem = true;
            }
        }

        private void IsWorkingChanged(MyCubeBlock myCubeBlock)
        {
            var wasFunctional = IsFunctional;
            IsFunctional = myCubeBlock.IsFunctional;

            if (Registered && Ai.Construct.RootAi !=null)
                Ai.Construct.RootAi.Construct.DirtyWeaponGroups = true;


            if (Platform.State == PlatformState.Incomplete) {
                Log.Line("Init on Incomplete");
                Init();
            }
            else {

                if (!wasFunctional && IsFunctional && IsWorkingChangedTick > 0)
                    Status = Start.ReInit;

                IsWorking = myCubeBlock.IsWorking;
                if (Type == CompType.Weapon)
                {
                    var wComp = (Weapon.WeaponComponent)this;
                    if (IsWorking)
                        I.FutureEvents.Schedule(wComp.ActivateWhileOn, null, 30);
                    else if (!IsWorking)
                        I.FutureEvents.Schedule(wComp.DeActivateWhileOn, null, 30);

                }
                if (Cube.ResourceSink.CurrentInputByType(GId) < 0) Log.Line($"IsWorking:{IsWorking}(was:{wasFunctional}) - Func:{IsFunctional} - GridAvailPow:{Ai.GridAvailablePower} - SinkPow:{SinkPower} - SinkReq:{Cube.ResourceSink.RequiredInputByType(GId)} - SinkCur:{Cube.ResourceSink.CurrentInputByType(GId)}");

                if (!IsWorking && Registered) {

                    var collection = TypeSpecific != CompTypeSpecific.Phantom ? Platform.Weapons : Platform.Phantoms;
                    foreach (var w in collection)
                            w.StopShooting();
                }
                IsWorkingChangedTick = I.Tick;

            }
            if (Platform.State == PlatformState.Ready) {

                if (Type == CompType.Weapon)
                {
                    var wComp = ((Weapon.WeaponComponent) this);
                    if (wasFunctional && !IsFunctional)
                        wComp.NotFunctional();


                    if (wasFunctional != IsFunctional && Ai.Construct.RootAi != null && wComp.Data.Repo.Values.Set.Overrides.WeaponGroupId > 0)
                        Ai.Construct.RootAi.Construct.DirtyWeaponGroups = true;
                }
            }
            
            if (I.MpActive && I.IsServer) {

                if (Type == CompType.Weapon)
                    ((Weapon.WeaponComponent)this).PowerLoss();
            }
        }

        internal string GetSystemStatus()
        {
            if (!Cube.IsFunctional) return Localization.GetText("SystemStatusFault");
            if (!Cube.IsWorking) return Localization.GetText("SystemStatusOffline");
            return Ai.AiOwner != 0 ? Localization.GetText("SystemStatusOnline") : Localization.GetText("SystemStatusRogueAi");
        }

        private void AppendingCustomInfoWeapon(IMyTerminalBlock block, StringBuilder stringBuilder)
        {
            var comp = ((Weapon.WeaponComponent)this);
            /*
            var r = "[color=#DDFF0000]"; //ARGB in hex values
            var y = "[color=#DDFFFF00]";
            var g = "[color=#DD00FF00]";               
            var e = "[/color]";
            stringBuilder.Append($"{r}Red{e} {y}Yellow{e} {g}Green{e}");
            */

            var collection = comp.HasAlternateUi ? SortAndGetTargetTypes() : TypeSpecific != CompTypeSpecific.Phantom ? Platform.Weapons : Platform.Phantoms;
            var debug = Debug || comp.Data.Repo.Values.Set.Overrides.Debug;
            var advanced = (I.Settings.ClientConfig.AdvancedMode || debug) && !comp.HasAlternateUi;

            if (I.Settings.Enforcement.ProhibitShooting)
            {
                stringBuilder.Append($"\n{Localization.GetText("WeaponInfoShootingDisabled")}");
            }

            if (HasServerOverrides)
                stringBuilder.Append($"\n{Localization.GetText("WeaponInfoServerModdedLine1")}\n")
                    .Append($"\n{Localization.GetText("WeaponInfoServerModdedLine2")}");

            if (comp.PrimaryWeapon.System.TrackProhibitLG)
                stringBuilder.Append($"\nCannot target large grids!");

            if (comp.PrimaryWeapon.System.TrackProhibitSG)
                stringBuilder.Append($"\nCannot target small grids!");

            if (comp.PrimaryWeapon.System.TargetGridCenter)
                stringBuilder.Append($"\nThis weapon aims at the center of grids");

            //Start of new formatting
            if (IdlePower > 0.01)
            {
                stringBuilder.Append($"\n{Localization.GetText("WeaponInfoIdlePower")}: {IdlePower:0.00} {Localization.GetText("WeaponInfoMWLabel")}");

                if (comp.Cube.IsWorking && comp.Cube.ResourceSink.CurrentInputByType(GId) < IdlePower)
                    stringBuilder.Append($"\n{Localization.GetText("WeaponInfoInsufficientPower")}");
            }

           

            for (int i = 0; i < collection.Count; i++)
            {
                var w = collection[i];
                string shots = "";
                if ((w.ActiveAmmoDef.AmmoDef.Const.EnergyAmmo || w.ActiveAmmoDef.AmmoDef.Const.IsHybrid) && !comp.HasAlternateUi)
                {
                    var chargeTime = w.AssignedPower > 0 ? (int)((w.MaxCharge - w.ProtoWeaponAmmo.CurrentCharge) / w.AssignedPower * MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS) : 0;
                    shots += $"\n{Localization.GetText("WeaponInfoDrawOverMax")}: {SinkPower - IdlePower:0.00}/ {w.ActiveAmmoDef.AmmoDef.Const.PowerPerTick:0.00} {Localization.GetText("WeaponInfoMWLabel")}" +
                    $"\n{(chargeTime == 0 ? Localization.GetText("WeaponInfoPowerCharged") : Localization.GetText("WeaponInfoPowerChargedIn") + " " + chargeTime + Localization.GetText("WeaponInfoSeconds"))}";
                }

                var endReturn = i + 1 != collection.Count ? "\n" : string.Empty;
                var timeToLoad = (int)(w.ReloadEndTick - Session.I.Tick) / 60;
                var showName = w.ActiveAmmoDef.AmmoDef.Const.TerminalName != w.ActiveAmmoDef.AmmoDef.Const.MagazineDef.DisplayNameText;
                var displayName = showName ? w.ActiveAmmoDef.AmmoDef.Const.TerminalName + " (" + w.ActiveAmmoDef.AmmoDef.Const.MagazineDef.DisplayNameText + ")" : w.ActiveAmmoDef.AmmoDef.Const.TerminalName;
                stringBuilder.Append($"\n\n" + w.System.PartName +
                    $" {(w.Comp.ProhibitSubsystemChanges ? "\nSubsystem selection disabled by mod" : "")}  " +
                    shots +
                    $" {(w.ActiveAmmoDef.AmmoDef.Const.EnergyAmmo ? string.Empty : $"\n{Localization.GetText("WeaponInfoAmmoLabel")}: " + (w.Loading ? timeToLoad < 0 ? Localization.GetText("WeaponInfoWaitingCharge") : Localization.GetText("WeaponInfoLoadedIn") + " " + timeToLoad + Localization.GetText("WeaponInfoSeconds") : w.ProtoWeaponAmmo.CurrentAmmo > 0 ? Localization.GetText("WeaponInfoLoaded") + " " + w.ProtoWeaponAmmo.CurrentAmmo + "x " + displayName : w.Comp.CurrentInventoryVolume > 0 ? Localization.GetText("WeaponInfoCheckAmmoType") : Localization.GetText("WeaponInfoNoammo")))}" +
                    $" {(w.ActiveAmmoDef.AmmoDef.Const.RequiresTarget ? "\n" + Localization.GetText("WeaponInfoHasTarget") + ": " + (w.Target.HasTarget ? Localization.GetText("WeaponTargTrue") : w.Comp.MasterAi.DetectionInfo.SomethingInRange && (w.Target.CurrentState == Target.States.NotSet || w.Target.CurrentState == Target.States.Expired) ? Localization.GetText("WeaponTargNeedSelection") : w.MinTargetDistanceSqr > 0 && (comp.MasterAi.DetectionInfo.OtherRangeSqr < w.MinTargetDistanceSqr || comp.MasterAi.DetectionInfo.PriorityRangeSqr < w.MinTargetDistanceSqr) ? Localization.GetText("WeaponTargTooClose") : w.BaseComp.MasterAi.DetectionInfo.SomethingInRange ? Localization.GetText("WeaponTargRange") : Localization.GetText("WeaponTargFalse")) : string.Empty)}" +
                    $" {(w.ActiveAmmoDef.AmmoDef.Const.RequiresTarget ? "\n" + Localization.GetText("WeaponInfoLoS") + ": " + (w.Target.HasTarget ? "" + !w.PauseShoot : Localization.GetText("WeaponInfoNoTarget")) : string.Empty)}");

                if (w.ActiveAmmoDef.AmmoDef.Const.RequiresTarget && w.ActiveAmmoDef.AmmoDef.Trajectory.TargetLossDegree > 0)
                {
                    stringBuilder.Append($"\nMax Tracking Angle: {w.ActiveAmmoDef.AmmoDef.Trajectory.TargetLossDegree}º");
                    if (w.Target.HasTarget)
                    {
                        var targetDir = Vector3D.Normalize(w.Target.TargetPos - w.GetScope.CachedPos);
                        if (!MathFuncs.IsDotProductWithinTolerance(ref targetDir, ref w.GetScope.CachedDir, w.ActiveAmmoDef.AmmoDef.Const.TargetLossDegree))
                            stringBuilder.Append($"\n  !! Target outside tracking arc !! \n  !! Projectile may not track !!");
                    }
                }
                stringBuilder.Append(endReturn);
            }
                
            if (HeatPerSecond > 0)
                stringBuilder.Append($"\n{Localization.GetText("WeaponInfoHeatPerSecOverMax")}: {HeatPerSecond}/{MaxHeat}" +
                    $"\n{Localization.GetText("WeaponInfoCurrentHeat")}: {CurrentHeat:0.} W ({(CurrentHeat / MaxHeat):P})");
                
            if (advanced)
            {
                stringBuilder.Append($"\n\n{Localization.GetText("WeaponInfoStatsHeader")}" +
                    $"\n{Localization.GetText("WeaponInfoDPSLabel")}: {comp.PeakDps:0.}");
                for (int i = 0; i < collection.Count; i++)
                {
                    var w = collection[i];
                    stringBuilder.Append($" {(collection.Count > 1 ? $"\n{w.FriendlyName}" : string.Empty)}" +
                        $"{(w.MinTargetDistance > 0 ? $"\n{Localization.GetText("WeaponInfoMinRange")}: {w.MinTargetDistance}{Localization.GetText("WeaponInfoMeter")}" : string.Empty)}" +
                        $"\n{Localization.GetText("WeaponInfoMaxRange")}: {w.MaxTargetDistance:0.}{Localization.GetText("WeaponInfoMeter")}" +
                        $"\n{Localization.GetText("WeaponInfoROF")}: {w.ActiveAmmoDef.AmmoDef.Const.RealShotsPerMin * comp.Data.Repo.Values.Set.RofModifier:0.}{Localization.GetText("WeaponInfoPerMin")}");
                    if(w.ActiveAmmoDef.AmmoDef.Const.RequiresTarget)
                    {
                        var targ = $"{Localization.GetText("WeaponInfoTargetLabel")}: ";
                        if (w.Target.HasTarget && w.Target.TargetObject != null)
                        {
                            var pTarg = w.Target.TargetObject as Projectile;
                            var eTarg = w.Target.TargetObject as MyEntity;
                            if(pTarg != null)
                            {
                                targ += Localization.GetText("WeaponInfoProjectileLabel");
                            }
                            else if (eTarg != null)
                            {
                                var topEnt = eTarg.GetTopMostParent();
                                var grid = topEnt as MyCubeGrid;
                                var suit = eTarg as IMyCharacter;
                                if (grid != null)
                                    targ += topEnt.DisplayName;
                                else if (suit != null)
                                    targ += suit.DisplayName;
                            }
                        }
                        else
                            targ += Localization.GetText("WeaponInfoNoneTarget");
                        stringBuilder.Append($"\n{targ}");
                    }

                    string otherAmmo = null;
                    if (!comp.HasAlternateUi)
                    {
                        for (int j = 0; j < w.System.AmmoTypes.Length; j++)
                        {
                            var ammo = w.System.AmmoTypes[j];
                            if (!ammo.AmmoDef.Const.IsTurretSelectable || string.IsNullOrEmpty(ammo.AmmoDef.AmmoRound) || ammo.AmmoDef.AmmoRound == "Energy")
                                continue;

                            if (otherAmmo == null)
                                otherAmmo = $"\n\n{Localization.GetText("WeaponInfoAmmoType")}:";
                            var showName =  ammo.AmmoDef.Const.TerminalName != ammo.AmmoDef.Const.MagazineDef.DisplayNameText && ammo.AmmoDef.Const.MagazineDef.DisplayNameText != "Energy";
                            otherAmmo += $"\n{ammo.AmmoDef.Const.TerminalName} {(showName ? "(" + ammo.AmmoDef.Const.MagazineDef.DisplayNameText + ")" : "")}";
                        }

                        if (otherAmmo != null)
                            stringBuilder.Append(otherAmmo);
                    }
                }
            }
        }

        private List<Weapon> SortAndGetTargetTypes()
        {
            var collection = TypeSpecific != CompTypeSpecific.Phantom ? Platform.Weapons : Platform.Phantoms;

            I.TmpWeaponEventSortingList.Clear();
            foreach (var w in collection)
            {
                if (!w.Target.HasTarget)
                    continue;

                w.Target.CurrentState = GetTargetState(w);
                I.TmpWeaponEventSortingList.Add(w);
            }
            var n = I.TmpWeaponEventSortingList.Count;
            for (int i = 1; i < n; ++i)
            {
                var key = I.TmpWeaponEventSortingList[i];
                var j = i - 1;

                while (j >= 0 && I.TmpWeaponEventSortingList[j].Target.CurrentState != key.Target.CurrentState)
                {
                    I.TmpWeaponEventSortingList[j + 1] = I.TmpWeaponEventSortingList[j];
                    j -= 1;
                }
                I.TmpWeaponEventSortingList[j + 1] = key;
            }
            return I.TmpWeaponEventSortingList;
        }

        private Target.States GetTargetState(Weapon w)
        {
            if (w.Target.TargetObject != null)
            {
                if (w.Target.TargetObject is MyPlanet)
                    return Target.States.Planet;

                if (w.Target.TargetObject is MyVoxelBase)
                    return Target.States.Roid;

                if (w.Target.TargetObject is Projectile)
                    return Target.States.Projectile;

                var entity = w.Target.TargetObject as MyEntity;
                if (entity == null)
                    return Target.States.Fake;

                Sandbox.ModAPI.Ingame.MyDetectedEntityInfo entInfo;
                Ai.CreateEntInfo(entity, w.Comp.Ai.AiOwner, out entInfo);
                if (entInfo.Relationship == MyRelationsBetweenPlayerAndBlock.Owner)
                    return Target.States.YourGrid;

                if (entInfo.Relationship == MyRelationsBetweenPlayerAndBlock.FactionShare || entInfo.Relationship == MyRelationsBetweenPlayerAndBlock.Friends)
                    return entity is IMyCharacter ? Target.States.FriendlyCharacter : Target.States.FriendlyGrid;

                if (entInfo.Relationship == MyRelationsBetweenPlayerAndBlock.Neutral)
                    return entity is IMyCharacter ? Target.States.NeutralCharacter : Target.States.NeutralGrid;

                if (entInfo.Relationship == MyRelationsBetweenPlayerAndBlock.NoOwnership)
                    return entity is IMyCharacter ? Target.States.NeutralCharacter : Target.States.UnOwnedGrid;

                if (entInfo.Relationship == MyRelationsBetweenPlayerAndBlock.Enemies)
                    return entity is IMyCharacter ? Target.States.EnemyCharacter : Target.States.EnemyGrid;
            }

            return Target.States.NotSet;
        }

        private void AppendingCustomInfoControl(IMyTerminalBlock block, StringBuilder stringBuilder)
        {
            try
            {
                var comp = ((ControlSys.ControlComponent)this);

                stringBuilder.Append("\n==== ControlSys ====\n");

                var ai = Platform.Control?.TopAi?.RootComp?.Ai;
                var initted = ai != null;

                if (comp.Controller.IsSunTrackerEnabled)
                    stringBuilder.Append($"Sun Tracking Mode");
                else
                {
                    stringBuilder.Append($"Ai Detected:{initted && !Platform.Control.TopAi.RootComp.Ai.MarkedForClose}\n\n");

                    if (initted)
                    {
                        stringBuilder.Append($"Weapons: {ai.WeaponComps.Count}\nTools: {ai.Tools.Count}\nCamera: {comp.Controller.Camera != null}\n");
                    }

                    if (Debug)
                    {
                        foreach (var support in Platform.Support)
                        {
                            stringBuilder.Append($"\n\nPart: {support.CoreSystem.PartName} - Enabled: {IsWorking}");
                            stringBuilder.Append($"\nManual: {support.BaseComp.UserControlled}");
                        }
                    }
                }
            }
            catch (Exception ex) { Log.Line($"Exception in AppendingCustomInfoSupport: {ex}", null, true); }
        }

        private void AppendingCustomInfoSupport(IMyTerminalBlock block, StringBuilder stringBuilder)
        {
            try
            {
                var status = GetSystemStatus();

                stringBuilder.Append(status + "\nCurrent: )");

                stringBuilder.Append("\n\n==== SupportSys ====");

                var weaponCnt = Platform.Support.Count;
                for (int i = 0; i < weaponCnt; i++)
                {
                    var a = Platform.Support[i];
                }

                if (Debug)
                {
                    foreach (var support in Platform.Support)
                    {
                        stringBuilder.Append($"\n\nPart: {support.CoreSystem.PartName} - Enabled: {IsWorking}");
                        stringBuilder.Append($"\nManual: {support.BaseComp.UserControlled}");
                    }
                }
            }
            catch (Exception ex) { Log.Line($"Exception in AppendingCustomInfoSupport: {ex}", null, true); }
        }

        private void AppendingCustomInfoUpgrade(IMyTerminalBlock block, StringBuilder stringBuilder)
        {
            try
            {
                var status = GetSystemStatus();

                stringBuilder.Append(status + "\nCurrent: )");

                stringBuilder.Append("\n\n==== Upgrade ====");

                var weaponCnt = Platform.Support.Count;
                for (int i = 0; i < weaponCnt; i++)
                {
                    var a = Platform.Upgrades[i];
                }

                if (Debug)
                {
                    foreach (var upgrade in Platform.Upgrades)
                    {
                        stringBuilder.Append($"\n\nPart: {upgrade.CoreSystem.PartName} - Enabled: {IsWorking}");
                        stringBuilder.Append($"\nManual: {upgrade.BaseComp.UserControlled}");
                    }
                }
            }
            catch (Exception ex) { Log.Line($"Exception in AppendingCustomInfoUpgrade: {ex}", null, true); }
        }
    }
}
