using System.Collections.Generic;
using CoreSystems.Platform;
using CoreSystems.Projectiles;
using CoreSystems.Support;
using Sandbox.ModAPI;
using VRageMath;
using static CoreSystems.Support.Target;
using static CoreSystems.Support.CoreComponent.Start;
using static CoreSystems.Support.CoreComponent.Trigger;
using static CoreSystems.Support.WeaponDefinition.AmmoDef.TrajectoryDef.GuidanceType;
using static CoreSystems.ProtoWeaponState;
using static CoreSystems.Support.WeaponDefinition.AnimationDef.PartAnimationSetDef;
using Sandbox.Game.Entities;
using Sandbox.ModAPI.Weapons;
using SpaceEngineers.Game.ModAPI;
using VRage.Game.Entity;
using VRage.Game;
using VRage.Game.ModAPI;

namespace CoreSystems
{
    public partial class Session
    {
        private void AiLoop()
        { //Fully Inlined due to keen's mod profiler

            foreach (var pair in EntityAIs)
            {
                ///
                /// GridAi update section
                ///

                var ai = pair.Value;

                ai.MyProjectiles = 0;
                var activeTurret = false;

                if (ai.MarkedForClose || !ai.AiInit || ai.TopEntity == null || ai.Construct.RootAi == null || ai.TopEntity.MarkedForClose)
                    continue;

                ai.Concealed = ((uint)ai.TopEntity.Flags & 4) > 0;

                if (ai.Concealed)
                    continue;

                if (!ai.ScanInProgress && Tick - ai.TargetsUpdatedTick > 100 && DbTask.IsComplete)
                    ai.RequestDbUpdate();

                if (ai.DeadProjectiles.Count > 0) 
                {
                    foreach (var dead in ai.DeadProjectiles)
                        ai.LiveProjectile.Remove(dead);
                    ai.DeadProjectiles.Clear();
                    ai.LiveProjectileTick = Tick;
                }
                ai.EnemyProjectiles = ai.LiveProjectile.Count > 0;
                ai.EnemyEntities = ai.SortedTargets.Count > 0;
                ai.EnemiesNear = ai.EnemyProjectiles || ai.EnemyEntities;

                ai.CheckProjectiles = Tick - ai.NewProjectileTick <= 1;

                if (ai.AiType == Ai.AiTypes.Grid && (ai.UpdatePowerSources || !ai.HadPower && ai.GridEntity.IsPowered || ai.HasPower && !ai.GridEntity.IsPowered || Tick10))
                    ai.UpdateGridPower();

                var enforcement = Settings.Enforcement;

                if (ai.QueuedSounds.Count > 0)
                    ai.ProcessQueuedSounds();

                ///
                /// Critical/warhead update section
                ///
                for (int i = 0; i < ai.CriticalComps.Count; i++)
                {
                    var wComp = ai.CriticalComps[i];
                    if (wComp.CloseCondition)
                    {
                        if (wComp.Slim != null && !wComp.Slim.IsDestroyed)
                            wComp.Slim.DoDamage(float.MaxValue, MyDamageType.Explosion, false);
                    }
                    var wValues = wComp.Data.Repo.Values;
                    var overrides = wValues.Set.Overrides;
                    for (int j = 0; j < wComp.Platform.Weapons.Count; j++)
                    {
                        var w = wComp.Platform.Weapons[j];
                        if (w.CriticalReaction && !wComp.CloseCondition && (overrides.Armed || wValues.State.CountingDown || wValues.State.CriticalReaction))
                            w.CriticalMonitor();
                    }
                }

                ///
                /// Phantom update section
                ///
                for (int i = 0; i < ai.PhantomComps.Count; i++)
                {
                    var pComp = ai.PhantomComps[i];

                    if (pComp.CloseCondition || pComp.HasCloseConsition && pComp.AllWeaponsOutOfAmmo())
                    {
                        if (!pComp.CloseCondition)
                            pComp.ForceClose(pComp.SubtypeName);
                        continue;
                    }
                    if (pComp.Status != Started)
                        pComp.HealthCheck();
                    if (pComp.Platform.State != CorePlatform.PlatformState.Ready || pComp.IsDisabled || pComp.IsAsleep || pComp.CoreEntity.MarkedForClose || pComp.LazyUpdate && !ai.DbUpdated && Tick > pComp.NextLazyUpdateStart)
                        continue;

                    if (ai.DbUpdated || !pComp.UpdatedState)
                    {
                        pComp.DetectStateChanges(false);
                    }

                    switch (pComp.Data.Repo.Values.State.Trigger)
                    {
                        case Once:
                            pComp.ShootManager.RequestShootSync(PlayerId, Weapon.ShootManager.RequestType.Once, Weapon.ShootManager.Signals.Once);
                            break;
                        case On:
                            pComp.ShootManager.RequestShootSync(PlayerId, Weapon.ShootManager.RequestType.On, Weapon.ShootManager.Signals.On);
                            break;
                    }

                    var pValues = pComp.Data.Repo.Values;
                    var overrides = pValues.Set.Overrides;
                    var cMode = overrides.Control;
                    var sMode = overrides.ShootMode;

                    var onConfrimed = pValues.State.Trigger == On && !pComp.ShootManager.FreezeClientShoot && !pComp.ShootManager.WaitingShootResponse && (sMode != Weapon.ShootManager.ShootModes.AiShoot || pComp.ShootManager.Signal == Weapon.ShootManager.Signals.Manual);
                    var noShootDelay = pComp.ShootManager.ShootDelay == 0 || pComp.ShootManager.ShootDelay != 0 && pComp.ShootManager.ShootDelay-- == 0;

                    ///
                    /// Phantom update section
                    /// 
                    for (int j = 0; j < pComp.Platform.Phantoms.Count; j++)
                    {
                        var p = pComp.Platform.Phantoms[j];
                        if (p.ActiveAmmoDef.AmmoDef.Const.Reloadable && !p.Loading)
                        {

                            if (IsServer && (p.ProtoWeaponAmmo.CurrentAmmo == 0 || p.CheckInventorySystem))
                                p.ComputeServerStorage();
                            else if (IsClient)
                            {

                                if (p.ClientReloading && p.Reload.EndId > p.ClientEndId && p.Reload.StartId == p.ClientStartId)
                                    p.Reloaded();
                                else
                                    p.ClientReload();
                            }
                        }
                        else if (p.Loading && Tick >= p.ReloadEndTick)
                            p.Reloaded(1);

                        var reloading = p.ActiveAmmoDef.AmmoDef.Const.Reloadable && p.ClientMakeUpShots == 0 && (p.Loading || p.ProtoWeaponAmmo.CurrentAmmo == 0);
                        var overHeat = p.PartState.Overheated && p.OverHeatCountDown == 0;
                        var canShoot = !overHeat && !reloading;

                        var autoShot = pComp.Data.Repo.Values.State.Trigger == On || p.AiShooting && pComp.Data.Repo.Values.State.Trigger == Off;
                        var anyShot = !pComp.ShootManager.FreezeClientShoot && (p.ShootCount > 0 || onConfrimed) && noShootDelay || autoShot && sMode == Weapon.ShootManager.ShootModes.AiShoot;

                        var delayedFire = p.System.DelayCeaseFire && !p.Target.IsAligned && Tick - p.CeaseFireDelayTick <= p.System.CeaseFireDelay;
                        var shoot = (anyShot || p.FinishShots || delayedFire);
                        var shotReady = canShoot && shoot;

                        if (shotReady)
                        {
                            p.Shoot();
                        }
                        else
                        {

                            if (p.IsShooting)
                                p.StopShooting();

                            if (p.BarrelSpinning)
                            {

                                var spinDown = !(shotReady && ai.CanShoot && p.System.Values.HardPoint.Loading.SpinFree);
                                p.SpinBarrel(spinDown);
                            }
                        }
                    }
                }

                if (ai.AiType == Ai.AiTypes.Grid && !ai.HasPower || enforcement.ServerSleepSupport && IsServer && ai.AwakeComps == 0 && ai.WeaponsTracking == 0 && ai.SleepingComps > 0 && !ai.CheckProjectiles && ai.AiSleep && !ai.DbUpdated) 
                    continue;

                var construct = ai.Construct;

                var rootAi = construct.RootAi;
                var rootConstruct = rootAi.Construct;
                var focus = rootConstruct.Focus;

                if (ai.AiType != Ai.AiTypes.Phantom) {
                    if (construct.LargestAi == null)
                        rootConstruct.Ai.TopEntityMap.GroupMap.UpdateAis();

                    if (ai.TopEntityMap.GroupMap.LastControllerTick == Tick || ai.TopEntityMap.LastControllerTick == Tick)
                        Ai.Constructs.UpdatePlayerStates(ai.TopEntityMap.GroupMap);

                    if (Tick60 && ai.AiType == Ai.AiTypes.Grid && ai.BlockChangeArea != BoundingBox.Invalid) {
                        ai.BlockChangeArea.Min *= ai.GridEntity.GridSize;
                        ai.BlockChangeArea.Max *= ai.GridEntity.GridSize;
                    }

                    if (rootConstruct.DirtyWeaponGroups)
                        Ai.Constructs.RebuildWeaponGroups(rootAi.TopEntityMap.GroupMap);

                    if (IsServer && rootConstruct.ActiveCombatBlock != null && rootConstruct.ActiveFlightBlock != null)
                        Ai.Constructs.CombatBlockUpdates(rootAi);

                    if (IsServer && rootConstruct.KeenDroneDirty)
                        Ai.Constructs.KeenDroneDirtyUpdate(rootAi);

                }
                if (Tick60 && Tick != rootConstruct.LastEffectUpdateTick && rootConstruct.TotalEffect > rootConstruct.PreviousTotalEffect)
                    rootConstruct.UpdateEffect(Tick);

                if (IsServer) {
                    if (rootConstruct.NewInventoryDetected)
                        rootConstruct.CheckForMissingAmmo();
                    else if (Tick60 && rootConstruct.RecentItems.Count > 0)
                        rootConstruct.CheckEmptyWeapons();
                }

                construct.HadFocus = rootConstruct.Data.Repo.FocusData.Target > 0 && MyEntities.TryGetEntityById(rootConstruct.Data.Repo.FocusData.Target, out rootConstruct.LastFocusEntity);
                var constructResetTick = rootConstruct.TargetResetTick == Tick;
                ///
                /// Upgrade update section
                ///
                for (int i = 0; i < ai.UpgradeComps.Count; i++)
                {
                    var uComp = ai.UpgradeComps[i];
                    if (uComp.Status != Started)
                        uComp.HealthCheck();

                    if (ai.DbUpdated || !uComp.UpdatedState)
                    {
                        uComp.DetectStateChanges();
                    }

                    if (uComp.Platform.State != CorePlatform.PlatformState.Ready || uComp.IsAsleep || !uComp.IsWorking || uComp.CoreEntity.MarkedForClose || uComp.IsDisabled || uComp.LazyUpdate && !ai.DbUpdated && Tick > uComp.NextLazyUpdateStart)
                        continue;

                    for (int j = 0; j < uComp.Platform.Upgrades.Count; j++)
                    {
                        var u = uComp.Platform.Upgrades[j];
                    }
                }

                ///
                /// Support update section
                ///
                for (int i = 0; i < ai.SupportComps.Count; i++)
                {
                    var sComp = ai.SupportComps[i];
                    if (sComp.Status != Started)
                        sComp.HealthCheck();

                    if (ai.DbUpdated || !sComp.UpdatedState)
                    {
                        sComp.DetectStateChanges();
                    }

                    if (sComp.Platform.State != CorePlatform.PlatformState.Ready || sComp.IsAsleep || !sComp.IsWorking || sComp.CoreEntity.MarkedForClose || sComp.IsDisabled || !Tick60)
                        continue;

                    for (int j = 0; j < sComp.Platform.Support.Count; j++)
                    {
                        var s = sComp.Platform.Support[j];
                        if (s.LastBlockRefreshTick < ai.LastBlockChangeTick && s.IsPrime || s.LastBlockRefreshTick < ai.LastBlockChangeTick && !sComp.Structure.CommonBlockRange)
                            s.RefreshBlocks();

                        if (s.ShowAffectedBlocks != sComp.Data.Repo.Values.Set.Overrides.ArmorShowArea)
                            s.ToggleAreaEffectDisplay();

                        if (s.Active)
                            s.Charge();
                    }
                }

                ///
                /// Control update section
                /// 
                for (int i = 0; i < ai.ControlComps.Count; i++)
                {
                    var cComp = ai.ControlComps[i];

                    if (cComp.Status != Started)
                        cComp.HealthCheck();

                    if (ai.DbUpdated || !cComp.UpdatedState)
                        cComp.DetectStateChanges();

                    if (cComp.Platform.State != CorePlatform.PlatformState.Ready || cComp.IsDisabled || cComp.IsAsleep || !cComp.IsWorking || cComp.CoreEntity.MarkedForClose || cComp.LazyUpdate && !ai.DbUpdated && Tick > cComp.NextLazyUpdateStart) {
                        if (cComp.RotorsMoving)
                            cComp.StopRotors();
                        continue;
                    }

                    var az = (IMyMotorStator)cComp.Controller.AzimuthRotor;
                    var el = (IMyMotorStator)cComp.Controller.ElevationRotor;

                    var cValues = cComp.Data.Repo.Values;

                    if (MpActive && IsClient)
                    {
                        MyEntity rotorEnt;
                        if (az == null && cValues.Other.Rotor1 > 0 && MyEntities.TryGetEntityById(cValues.Other.Rotor1, out rotorEnt))
                            az = (IMyMotorStator)rotorEnt;

                        if (el == null && cValues.Other.Rotor2 > 0 && MyEntities.TryGetEntityById(cValues.Other.Rotor2, out rotorEnt))
                            el = (IMyMotorStator)rotorEnt;
                    }

                    if (az == null || el == null)
                        continue;

                    if (MpActive && IsServer)
                    {
                        if (az.EntityId != cValues.Other.Rotor1 || el.EntityId != cValues.Other.Rotor2)
                        {
                            cValues.Other.Rotor1 = az.EntityId;
                            cComp.Controller.AzimuthRotor = az;
                            cValues.Other.Rotor2 = el.EntityId;
                            cComp.Controller.ElevationRotor = el;
                            SendComp(cComp);
                        }
                        else if (Tick1800)
                        {
                            cComp.Controller.AzimuthRotor = az;
                            cComp.Controller.ElevationRotor = el;
                        }

                    }

                    var cPart = cComp.Platform.Control;
                    cPart.IsAimed = false;
                    if (cPart.TopAi != null)
                        cPart.TopAi.ControlComp = null;
                    cPart.TopAi = null;
                    cPart.BaseMap = az.TopGrid == el.CubeGrid ? az : el;
                    cPart.OtherMap = cPart.BaseMap == az ? el : az;
                    var topGrid = cPart.BaseMap.TopGrid as MyCubeGrid;
                    var otherGrid = cPart.OtherMap.TopGrid as MyCubeGrid;

                    if (cPart.BaseMap == null || cPart.OtherMap == null  || topGrid == null || topGrid.MarkedForClose || otherGrid == null || otherGrid.MarkedForClose || !EntityAIs.TryGetValue(otherGrid, out cPart.TopAi))  {
                        if (cComp.RotorsMoving)
                            cComp.StopRotors();

                        if (cPart.TopAi != null)
                            cPart.CleanControl();
                        continue;
                    }

                    cPart.TopAi.ControlComp = cComp;
                    cPart.TopAi.RotorCommandTick = Tick;
                    

                    if (cPart.TopAi.MaxTargetingRange > ai.MaxTargetingRange)
                        cComp.ReCalculateMaxTargetingRange(cPart.TopAi.MaxTargetingRange);

                    if (Tick180)
                    {
                        cComp.ToolsAndWeapons.Clear();
                        foreach (var comp in cPart.TopAi.WeaponComps)
                            cComp.ToolsAndWeapons.Add(comp.CoreEntity);
                        foreach (var tool in cPart.TopAi.Tools)
                            cComp.ToolsAndWeapons.Add((MyEntity)tool);
                    }

                    var cPlayerId = cValues.State.PlayerId;
                    Ai.PlayerController pControl;
                    pControl.ControlEntity = null;
                    var playerControl = rootConstruct.ControllingPlayers.TryGetValue(cPlayerId, out pControl);
                    var activePlayer = PlayerId == cPlayerId && playerControl;

                    var hasControl = activePlayer && pControl.ControlEntity == cComp.CoreEntity;
                    cPart.TopAi.RotorManualControlId = hasControl ? PlayerId : cPart.TopAi.RotorManualControlId != -2 ? -1 : -2;
                    var cMode = cValues.Set.Overrides.Control;
                    if (HandlesInput && (cPlayerId == PlayerId || !cPart.Comp.HasAim && ai.RotorManualControlId == PlayerId))
                    {
                        var overrides = cValues.Set.Overrides;

                        var playerAim = activePlayer && cMode != ProtoWeaponOverrides.ControlModes.Auto || pControl.ControlEntity is IMyTurretControlBlock;
                        var track = !InMenu && (playerAim && !UiInput.CameraBlockView || pControl.ControlEntity is IMyTurretControlBlock || UiInput.CameraChannelId > 0 && UiInput.CameraChannelId == overrides.CameraChannel);

                        if (cValues.State.TrackingReticle != track)
                            TrackReticleUpdateCtc(cPart.Comp, track);
                    }

                    if (cComp.Controller.IsUnderControl)
                    {
                        cComp.RotorsMoving = true;
                        continue;
                    }

                    if (!cComp.Data.Repo.Values.Set.Overrides.AiEnabled || cComp.Controller.IsSunTrackerEnabled || !cPart.RefreshRootComp()) {
                        
                        if (cComp.RotorsMoving)
                            cComp.StopRotors();
                        continue;
                    }

                    var primaryWeapon = cPart.TopAi.RootComp.PrimaryWeapon;
                    primaryWeapon.RotorTurretTracking = true;

                    if (IsServer && cValues.Set.Range < 0 && primaryWeapon.MaxTargetDistance > 0)
                        BlockUi.RequestSetRangeControl(cComp.TerminalBlock, (float) primaryWeapon.MaxTargetDistance);

                    var validTarget = primaryWeapon.Target.TargetState == TargetStates.IsEntity || primaryWeapon.Target.TargetState == TargetStates.IsFake || primaryWeapon.Target.TargetState == TargetStates.IsProjectile;

                    var noTarget = false;
                    var desiredDirection = Vector3D.Zero;
                    
                    if (!validTarget)
                        noTarget = true;
                    else if (!ControlSys.TrajectoryEstimation(cPart, out desiredDirection))
                    {
                        noTarget = true;
                    }

                    if (noTarget) {

                        cPart.TopAi.RotorTargetPosition = Vector3D.MaxValue;

                        if (IsServer && primaryWeapon.Target.HasTarget)
                            primaryWeapon.Target.Reset(Tick, States.ServerReset);

                        if (cComp.RotorsMoving)
                            cComp.StopRotors();

                        continue;
                    }

                    if (!cComp.TrackTarget(cComp.Platform.Control.BaseMap,  cComp.Platform.Control.OtherMap,  ref desiredDirection))
                        continue;
                }


                ///
                /// WeaponComp update section
                ///
                for (int i = 0; i < ai.WeaponComps.Count; i++) {

                    var wComp = ai.WeaponComps[i];
                    if (wComp.Status != Started)
                        wComp.HealthCheck();

                    var wValues = wComp.Data.Repo.Values;
                    var overrides = wValues.Set.Overrides;
                    
                    var masterChange = false;
                    if (ai.ControlComp != null)
                        masterChange = wComp.UpdateControlInfo();

                    if (ai.DbUpdated || !wComp.UpdatedState || masterChange) 
                        wComp.DetectStateChanges(masterChange);

                    if (wComp.Platform.State != CorePlatform.PlatformState.Ready || wComp.IsDisabled || wComp.IsAsleep || !wComp.IsWorking || wComp.CoreEntity.MarkedForClose || wComp.LazyUpdate && !ai.DbUpdated && Tick > wComp.NextLazyUpdateStart)
                        continue;

                    var cMode = overrides.Control;

                    var sMode = overrides.ShootMode;
                    var focusTargets = wComp.MasterOverrides.FocusTargets;
                    var grids = wComp.MasterOverrides.Grids;
                    var overRide = wComp.MasterOverrides.Override;
                    var projectiles = wComp.MasterOverrides.Projectiles;

                    if (IsServer && ai.ControlComp != null)
                    {
                        var cValues = ai.ControlComp.Data.Repo.Values;
                        if (cValues.Set.Overrides.Control != overrides.Control)
                            BlockUi.RequestControlMode(wComp.TerminalBlock, (long)cValues.Set.Overrides.Control);

                        if (cValues.State.PlayerId != wValues.State.PlayerId)
                            wComp.TakeOwnerShip(cValues.State.PlayerId);
                    }

                    if (HandlesInput) {

                        if (IsClient && ai.TopEntityMap.LastControllerTick == Tick && wComp.ShootManager.Signal == Weapon.ShootManager.Signals.Manual && (wComp.ShootManager.ClientToggleCount > wValues.State.ToggleCount || wValues.State.Trigger == On) && wValues.State.PlayerId > 0) 
                            wComp.ShootManager.RequestShootSync(PlayerId, Weapon.ShootManager.RequestType.Off);

                        var isControllingPlayer = wValues.State.PlayerId == PlayerId || !wComp.HasAim && ai.RotorManualControlId == PlayerId;
                        if (isControllingPlayer) {

                            Ai.PlayerController pControl;
                            pControl.ControlEntity = null;
                            var playerControl = rootConstruct.ControllingPlayers.TryGetValue(wValues.State.PlayerId, out pControl);
                            
                            var activePlayer = PlayerId == wValues.State.PlayerId && playerControl;
                            var cManual = pControl.ControlEntity is IMyTurretControlBlock;
                            var customWeapon = cManual && ai.ControlComp != null && ai.ControlComp.Cube == pControl.ControlEntity;
                            var manualThisWeapon = pControl.ControlEntity == wComp.Cube && wComp.HasAim || pControl.ControlEntity is IMyAutomaticRifleGun;
                            var controllingWeapon = customWeapon || manualThisWeapon;
                            var validManualModes = (sMode == Weapon.ShootManager.ShootModes.MouseControl || cMode == ProtoWeaponOverrides.ControlModes.Manual);
                            var manual = (controllingWeapon || pControl.ShareControl && validManualModes && ((wComp.HasAim || ai.ControlComp != null) || !IdToCompMap.ContainsKey(pControl.EntityId)));
                            var playerAim = activePlayer && manual;
                            var track = !InMenu && (playerAim && (!UiInput.CameraBlockView || cManual || manualThisWeapon) || UiInput.CameraChannelId > 0 && UiInput.CameraChannelId == overrides.CameraChannel);
                            if (!activePlayer && wComp.ShootManager.Signal == Weapon.ShootManager.Signals.MouseControl)
                                wComp.ShootManager.RequestShootSync(PlayerId, Weapon.ShootManager.RequestType.Off);
                            
                            if (cMode == ProtoWeaponOverrides.ControlModes.Manual)
                                TargetUi.LastManualTick = Tick;

                            if (wValues.State.TrackingReticle != track && !ai.IsBot)
                                TrackReticleUpdate(wComp, track);

                            var active = wComp.ShootManager.ClientToggleCount > wValues.State.ToggleCount || wValues.State.Trigger == On;
                            var turnOn = !active && UiInput.ClientInputState.MouseButtonLeft && playerControl && !InMenu;
                            var turnOff = active && (!UiInput.ClientInputState.MouseButtonLeft || InMenu) && Tick5;

                            if (sMode == Weapon.ShootManager.ShootModes.AiShoot)
                            {
                                if (playerAim)
                                {
                                    if (turnOn || turnOff)
                                    {
                                        wComp.ShootManager.RequestShootSync(PlayerId, turnOn ? Weapon.ShootManager.RequestType.On : Weapon.ShootManager.RequestType.Off, turnOn ? Weapon.ShootManager.Signals.Manual : Weapon.ShootManager.Signals.None);
                                    }
                                }
                                else if (wComp.ShootManager.Signal == Weapon.ShootManager.Signals.Manual && active)
                                {
                                    wComp.ShootManager.RequestShootSync(PlayerId, Weapon.ShootManager.RequestType.Off);
                                }
                            }
                            else if (sMode == Weapon.ShootManager.ShootModes.MouseControl && (turnOn && playerAim || turnOff))
                            {
                                wComp.ShootManager.RequestShootSync(PlayerId, turnOn ? Weapon.ShootManager.RequestType.On : Weapon.ShootManager.RequestType.Off, manualThisWeapon ? Weapon.ShootManager.Signals.Manual : Weapon.ShootManager.Signals.MouseControl);
                            }
                        }
                    }

                    Ai.FakeTargets fakeTargets = null;
                    if (cMode == ProtoWeaponOverrides.ControlModes.Manual || cMode == ProtoWeaponOverrides.ControlModes.Painter)
                        PlayerDummyTargets.TryGetValue(wValues.State.PlayerId, out fakeTargets);

                    wComp.PainterMode = fakeTargets != null && cMode == ProtoWeaponOverrides.ControlModes.Painter && fakeTargets.PaintedTarget.EntityId != 0;
                    wComp.UserControlled = cMode != ProtoWeaponOverrides.ControlModes.Auto || wValues.State.Control == ControlMode.Camera || fakeTargets != null && fakeTargets.PaintedTarget.EntityId != 0;
                    wComp.FakeMode = wComp.ManualMode || wComp.PainterMode;

                    var onConfrimed = wValues.State.Trigger == On && !wComp.ShootManager.FreezeClientShoot && !wComp.ShootManager.WaitingShootResponse && (sMode != Weapon.ShootManager.ShootModes.AiShoot || wComp.ShootManager.Signal == Weapon.ShootManager.Signals.Manual);
                    var noShootDelay = wComp.ShootManager.ShootDelay == 0 || wComp.ShootManager.ShootDelay != 0 && wComp.ShootManager.ShootDelay-- == 0;
                    var sequenceReady = (overrides.WeaponGroupId == 0 || overrides.SequenceId == -1) || wComp.SequenceReady(rootConstruct);

                    if (Tick60) {
                        var add = wComp.TotalEffect - wComp.PreviousTotalEffect;
                        wComp.AddEffect = add > 0 ? add : wComp.AddEffect;
                        wComp.AverageEffect = wComp.DamageAverage.Add((int)add);
                        wComp.PreviousTotalEffect = wComp.TotalEffect;
                    }

                    ///
                    /// Weapon update section
                    ///
                    for (int j = 0; j < wComp.Platform.Weapons.Count; j++) {

                        var w = wComp.Platform.Weapons[j];
                        if (w.PartReadyTick > Tick)
                        {

                            if (w.Target.HasTarget && !IsClient)
                                w.Target.Reset(Tick, States.WeaponNotReady);
                            continue;
                        }

                        //if (DebugVersion && DedicatedServer && ai.AiType == Ai.AiTypes.Player && (HandDebugPacketPacket.LastHitTick == Tick || HandDebugPacketPacket.LastShootTick == Tick))
                        //    SendHandDebugInfo(w);

                        if (w.AvCapable && Tick20)
                        {
                            var avWasEnabled = w.PlayTurretAv;
                            double distSqr;
                            var pos = w.Comp.CoreEntity.PositionComp.WorldAABB.Center;
                            Vector3D.DistanceSquared(ref CameraPos, ref pos, out distSqr);
                            w.PlayTurretAv = distSqr < w.System.HardPointAvMaxDistSqr;
                            if (avWasEnabled != w.PlayTurretAv) w.StopBarrelAvTick = Tick;
                        }

                        ///
                        ///Check Reload
                        ///                        
                        var aConst = w.ActiveAmmoDef.AmmoDef.Const;
                        if (aConst.Reloadable && !w.System.DesignatorWeapon && !w.Loading)
                        { // does this need StayCharged?

                            if (IsServer)
                            {
                                if (w.ProtoWeaponAmmo.CurrentAmmo == 0 || w.CheckInventorySystem)
                                    w.ComputeServerStorage();
                            }
                            else if (IsClient)
                            {

                                if (w.ClientReloading && w.Reload.EndId > w.ClientEndId && w.Reload.StartId == w.ClientStartId)
                                    w.Reloaded(5);
                                else
                                    w.ClientReload();
                            }
                        }
                        else if (w.Loading && (IsServer && Tick >= w.ReloadEndTick || IsClient && !w.Charging && w.Reload.EndId > w.ClientEndId))
                            w.Reloaded(1);

                        if (DedicatedServer && w.Reload.WaitForClient && !w.Loading && (wValues.State.PlayerId <= 0 || Tick - w.LastLoadedTick > 60))
                            SendWeaponReload(w, true);

                        ///
                        /// Update Weapon Hud Info
                        /// 
                        var addWeaponToHud = HandlesInput && !w.System.DisableStatus && (w.HeatPerc >= 0.01 || (w.ShowReload && (w.Loading || w.Reload.WaitForClient)) || ((aConst.CanReportTargetStatus || ai.ControlComp != null) && wValues.Set.ReportTarget && !w.Target.HasTarget && grids && (wComp.DetectOtherSignals && ai.DetectionInfo.OtherInRange || ai.DetectionInfo.PriorityInRange) && ai.DetectionInfo.TargetInRange(w)));

                        if (addWeaponToHud && !Session.Config.MinimalHud && !enforcement.DisableHudReload && !Settings.ClientConfig.HideReload  && (ActiveControlBlock != null && ai.SubGridCache.Contains(ActiveControlBlock.CubeGrid) || PlayerHandWeapon != null && IdToCompMap.ContainsKey(((IMyGunBaseUser)PlayerHandWeapon).OwnerId)))
                        {
                            HudUi.TexturesToAdd++;
                            HudUi.WeaponsToDisplay.Add(w);
                        }

                        if (w.Target.ClientDirty)
                            w.Target.ClientUpdate(w, w.TargetData);

                        ///
                        /// Check target for expire states
                        /// 
                        var noAmmo = w.ProtoWeaponAmmo.CurrentAmmo == 0;
                        w.OutOfAmmo = w.NoMagsToLoad && noAmmo && aConst.Reloadable && !w.System.DesignatorWeapon && Tick - w.LastMagSeenTick > 600;

                        var weaponAcquires = ai.AcquireTargets && (aConst.RequiresTarget || w.RotorTurretTracking || w.ShootRequest.AcquireTarget);
                        var eTarget = w.Target.TargetObject as MyEntity;
                        var pTarget = w.Target.TargetObject as Projectile;
                        var cTarget = w.Target.TargetObject as IMyCharacter;
                        if (!IsClient)
                        {
                            if (w.Target.HasTarget)
                            {
                                if (w.OutOfAmmo)
                                {
                                    w.Target.Reset(Tick, States.Expired);
                                }
                                else if (w.Target.TargetObject == null && !wComp.FakeMode || wComp.ManualMode && (fakeTargets == null || Tick - fakeTargets.ManualTarget.LastUpdateTick > 120))
                                {
                                    w.Target.Reset(Tick, States.Expired, !wComp.ManualMode);
                                }
                                else if (eTarget != null && (eTarget.MarkedForClose || (cTarget!= null && (cTarget.IsDead || cTarget.Integrity <= 0)) || !rootConstruct.HadFocus && weaponAcquires && aConst.SkipAimChecks && !w.RotorTurretTracking || wComp.UserControlled && !w.System.SuppressFire))
                                {
                                    w.Target.Reset(Tick, States.Expired);
                                }
                                else if (eTarget != null && (Tick60 || constructResetTick) && (focusTargets || w.System.FocusOnly) && !focus.ValidFocusTarget(w))
                                {
                                    w.Target.Reset(Tick, States.Expired);
                                }
                                else if (eTarget != null && Tick60 && !focusTargets && !w.TurretController && weaponAcquires && !w.TargetInRange(eTarget))
                                {
                                    w.Target.Reset(Tick, States.Expired);
                                }
                                else if (pTarget != null && (!ai.LiveProjectile.ContainsKey(pTarget) || w.Target.TargetState == TargetStates.IsProjectile && pTarget.State != Projectile.ProjectileState.Alive))
                                {
                                    w.Target.Reset(Tick, States.Expired);
                                    w.FastTargetResetTick = Tick + 6;
                                }
                                else if (w.System.TargetSlaving && !w.System.TargetPersists && !rootConstruct.StillTrackingTarget(w))
                                {
                                    w.Target.Reset(Tick, States.Expired);
                                    w.FastTargetResetTick = Tick;
                                }
                                else if (!w.TurretController)
                                {
                                    Vector3D targetPos;
                                    if (w.TurretAttached)
                                    {
                                        if (!w.System.TrackTargets)
                                        {
                                            var trackingWeaponIsFake = wComp.PrimaryWeapon.Target.TargetState == TargetStates.IsFake;
                                            var thisWeaponIsFake = w.Target.TargetState == TargetStates.IsFake;
                                            if (w.System.MaxTrackingTime && Tick - w.Target.ChangeTick > w.System.MaxTrackingTicks || w.Target.TargetState == TargetStates.IsProjectile && (wComp.PrimaryWeapon.Target.TargetObject != w.Target.TargetObject || pTarget.State != Projectile.ProjectileState.Alive) || wComp.PrimaryWeapon.Target.TargetObject != w.Target.TargetObject || trackingWeaponIsFake != thisWeaponIsFake)
                                                w.Target.Reset(Tick, States.Expired);
                                            else
                                                w.TargetLock = true;
                                        }
                                        else if (w.System.MaxTrackingTime && Tick - w.Target.ChangeTick > w.System.MaxTrackingTicks || !Weapon.TargetAligned(w, w.Target, out targetPos))
                                            w.Target.Reset(Tick, States.Expired);
                                    }
                                    else if (w.System.TrackTargets)
                                    {
                                        if (w.System.MaxTrackingTime && Tick - w.Target.ChangeTick > w.System.MaxTrackingTicks || !Weapon.TargetAligned(w, w.Target, out targetPos))
                                            w.Target.Reset(Tick, States.Expired);
                                    }
                                    else if (w.System.MaxTrackingTime && Tick - w.Target.ChangeTick > w.System.MaxTrackingTicks)
                                        w.Target.Reset(Tick, States.Expired);
                                }
                            }
                        }
                        else if (eTarget != null && eTarget.MarkedForClose || w.Target.HasTarget && w.Target.TargetObject == null && w.TargetData.EntityId >= 0 || w.DelayedTargetResetTick == Tick && w.TargetData.EntityId == 0 && w.Target.TargetObject != null)
                        {
                            w.Target.Reset(Tick, States.ServerReset);
                        }

                        w.ProjectilesNear = ai.EnemyProjectiles && (w.System.TrackProjectile || ai.ControlComp != null) && !w.System.FocusOnly && projectiles && w.Target.TargetState != TargetStates.IsProjectile && (w.Target.TargetChanged || QCount == w.ShortLoadId);

                        if (wValues.State.Control == ControlMode.Camera && UiInput.MouseButtonPressed)
                            w.Target.TargetPos = Vector3D.Zero;

                        ///
                        /// Queue for target acquire or set to tracking weapon.
                        /// 
                        if (weaponAcquires && w.TargetAcquireTick == uint.MaxValue && (!w.System.DropTargetUntilLoaded || w.ProtoWeaponAmmo.CurrentAmmo > 0) && wValues.State.Control != ControlMode.Camera && (!wComp.UserControlled || wComp.FakeMode || wValues.State.Trigger == On))
                        {
                            var myTimeSlot = Tick == w.FastTargetResetTick || w.Acquire.IsSleeping && AsleepCount == w.Acquire.SlotId || !w.Acquire.IsSleeping && AwakeCount == w.Acquire.SlotId;

                            var focusRequest = rootConstruct.HadFocus && (aConst.SkipAimChecks || constructResetTick || Tick - w.ShootRequest.RequestTick <= 1);
                            var acquireReady = (!aConst.SkipAimChecks || w.RotorTurretTracking || w.ShootRequest.AcquireTarget) && myTimeSlot || focusRequest;

                            var somethingNearBy = wComp.DetectOtherSignals && wComp.MasterAi.DetectionInfo.OtherInRange || wComp.MasterAi.DetectionInfo.PriorityInRange;
                            var trackObstructions = w.System.ScanNonThreats && wComp.MasterAi.Obstructions.Count > 0;
                            var requiresHome = w.System.GoHomeToReload && !w.IsHome && noAmmo;
                            var weaponReady = !w.OutOfAmmo && !requiresHome && (!w.System.FocusOnly || rootConstruct.HadFocus) && (wComp.MasterAi.EnemiesNear && somethingNearBy || trackObstructions) && (!w.Target.HasTarget || rootConstruct.HadFocus && constructResetTick);
                            Dictionary<object, Weapon> masterTargets;
                            var seek = weaponReady && (acquireReady || w.ProjectilesNear) && (!w.System.TargetSlaving || rootConstruct.TrackedTargets.TryGetValue(w.System.StorageLocation, out masterTargets) && masterTargets.Count > 0);
                            var fakeRequest = wComp.FakeMode && w.Target.TargetState != TargetStates.IsFake && wComp.UserControlled;

                            if (seek || fakeRequest)
                            {
                                w.TargetAcquireTick = Tick;
                                AcquireTargets.Add(w);
                            }
                        }

                        if (w.Target.TargetChanged) // Target changed
                            w.TargetChanged();

                        ///
                        /// Determine if its time to shoot
                        ///
                        ///
                        w.AiShooting = !wComp.UserControlled && !w.System.SuppressFire && (w.TargetLock || w.Target.TargetState == TargetStates.IsProjectile && (aConst.IsSmart || aConst.IsDrone) || ai.ControlComp != null && ai.ControlComp.Platform.Control.IsAimed && Vector3D.DistanceSquared(wComp.CoreEntity.PositionComp.WorldAABB.Center, ai.RotorTargetPosition) <= wComp.MaxDetectDistanceSqr);

                        var reloading = aConst.Reloadable && w.ClientMakeUpShots == 0 && (w.Loading || noAmmo || w.Reload.WaitForClient);
                        var overHeat = w.PartState.Overheated && (w.OverHeatCountDown == 0 || w.OverHeatCountDown != 0 && w.OverHeatCountDown-- == 0);

                        var canShoot = !overHeat && !reloading && !w.System.DesignatorWeapon && sequenceReady;
                        var paintedTarget = wComp.PainterMode && w.Target.TargetState == TargetStates.IsFake && (w.Target.IsAligned || ai.ControlComp != null && ai.ControlComp.Platform.Control.IsAimed);
                        var autoShot = paintedTarget || w.AiShooting && wValues.State.Trigger == Off;
                        var anyShot = !wComp.ShootManager.FreezeClientShoot && (w.ShootCount > 0 || onConfrimed) && noShootDelay || autoShot && sMode == Weapon.ShootManager.ShootModes.AiShoot;

                        var delayedFire = w.System.DelayCeaseFire && !w.Target.IsAligned && Tick - w.CeaseFireDelayTick <= w.System.CeaseFireDelay;
                        var finish = w.FinishShots || delayedFire;
                        var shootRequest = (anyShot || finish);

                        var shotReady = canShoot && shootRequest;
                        var shoot = shotReady && ai.CanShoot && (!aConst.RequiresTarget || w.Target.HasTarget || finish || overRide || wComp.ShootManager.Signal == Weapon.ShootManager.Signals.Manual);

                        if (shoot) {
                            if (w.System.DelayCeaseFire && (autoShot || w.FinishShots))
                                w.CeaseFireDelayTick = Tick;
                            ShootingWeapons.Add(w);
                        }
                        else {
                            if (w.IsShooting || w.PreFired)
                                w.StopShooting();

                            if (w.BarrelSpinning)
                                w.SpinBarrel(!(shotReady && ai.CanShoot && w.System.WConst.SpinFree) && Tick - w.LastShootTick >= 60);
                        }

                        if (w.TurretController)
                        {
                            w.TurretActive = w.Target.HasTarget;
                            if (w.TurretActive)
                                activeTurret = true;
                        }

                        ///
                        /// Check weapon's turret to see if its time to go home
                        ///
                        if (w.TurretController && !w.IsHome && !w.ReturingHome && !w.Target.HasTarget && !shootRequest && Tick - w.Target.ChangeTick > 239 && !wComp.UserControlled && wValues.State.Trigger == Off)
                            w.ScheduleWeaponHome();

                        w.TargetLock = false;

                        if (wComp.Debug && !DedicatedServer)
                            WeaponDebug(w);

                    }
                }

                if (ai.AiType == Ai.AiTypes.Grid && Tick60 && ai.BlockChangeArea != BoundingBox.Invalid) {
                    ai.BlockChangeArea = BoundingBox.CreateInvalid();
                    ai.AddedBlockPositions.Clear();
                    ai.RemovedBlockPositions.Clear();
                }
                ai.DbUpdated = false;

                if (ai.RotorCommandTick > 0 && Tick - ai.RotorCommandTick > 1)
                    ai.ResetControlRotorState();


                if (activeTurret)
                    AimingAi.Add(ai);

                if (Tick - VanillaTurretTick < 3 && ai.TopEntityMap?.Targeting != null)
                    ai.TopEntityMap.Targeting.AllowScanning = false;

            }

            if (DbTask.IsComplete && DbsToUpdate.Count > 0 && !DbUpdating)
                UpdateDbsInQueue();
        }

        private void AimAi()
        {
            var aiCount = AimingAi.Count;
            var stride = aiCount < 32 ? 1 : 2;

            MyAPIGateway.Parallel.For(0, aiCount, i =>
            {
                var ai = AimingAi[i];
                for (int j = 0; j < ai.TrackingComps.Count; j++)
                {
                    var wComp = ai.TrackingComps[j];
                    for (int k = 0; k < wComp.Platform.Weapons.Count; k++)
                    {
                        var w = wComp.Platform.Weapons[k];
                        if (!w.TurretActive || !ai.AiInit || ai.MarkedForClose || ai.Concealed || w.Comp.Ai == null || ai.TopEntity == null || ai.Construct.RootAi == null || w.Comp.CoreEntity == null  || wComp.IsDisabled || wComp.IsAsleep || !wComp.IsWorking || ai.TopEntity.MarkedForClose || wComp.CoreEntity.MarkedForClose || w.Comp.Platform.State != CorePlatform.PlatformState.Ready) continue;
                        if (!Weapon.TrackingTarget(w, w.Target, out w.TargetLock) && !IsClient && w.Target.ChangeTick != Tick && w.Target.HasTarget)
                            w.Target.Reset(Tick, States.LostTracking);
                    }
                }

            }, stride);

            AimingAi.Clear();
        }

        private void CheckAcquire()
        {
            for (int i = AcquireTargets.Count - 1; i >= 0; i--)
            {
                var w = AcquireTargets[i];
                var comp = w.Comp;
                var ai = comp.MasterAi;
                if (comp.TopEntity.MarkedForClose || comp.CoreEntity.MarkedForClose || ai?.Construct.RootAi == null || w.ActiveAmmoDef == null || comp.IsAsleep || comp.IsBlock && !comp.Ai.HasPower || comp.Ai.Concealed || !comp.Ai.DbReady || !comp.IsWorking || w.OutOfAmmo) {
                    w.TargetAcquireTick = uint.MaxValue;
                    AcquireTargets.RemoveAtFast(i);
                    continue;
                }
                var rootConstruct = ai.Construct.RootAi.Construct;
                var recentApiRequest = Tick - w.ShootRequest.RequestTick <= 1;

                var pCheckOnly = w.ProjectilesNear && rootConstruct.TargetResetTick != Tick;

                var requiresFocus = w.System.FocusOnly || w.ActiveAmmoDef.AmmoDef.Const.SkipAimChecks && !pCheckOnly && !w.RotorTurretTracking && !comp.ManualMode || (rootConstruct.TargetResetTick == Tick || recentApiRequest) && !w.System.UniqueTargetPerWeapon;
                if (requiresFocus && !rootConstruct.HadFocus) {
                    w.TargetAcquireTick = uint.MaxValue;
                    AcquireTargets.RemoveAtFast(i);
                    continue;
                }

                if (!w.Acquire.Monitoring && IsServer && w.System.HasRequiresTarget)
                    AcqManager.Monitor(w.Acquire);

                var acquire = (w.Acquire.IsSleeping && AsleepCount == w.Acquire.SlotId || !w.Acquire.IsSleeping && AwakeCount == w.Acquire.SlotId);

                var seekProjectile = w.ProjectilesNear || recentApiRequest && w.ShootRequest.Type == Weapon.ApiShootRequest.TargetType.Position || (w.System.TrackProjectile || w.Comp.Ai.ControlComp != null) && comp.MasterOverrides.Projectiles && ai.CheckProjectiles;
                var checkTime = w.TargetAcquireTick == Tick || w.Target.TargetChanged || acquire || seekProjectile || w.FastTargetResetTick == Tick || recentApiRequest;

                if (checkTime || requiresFocus && w.Target.HasTarget) {


                    var checkObstructions = w.System.ScanNonThreats && ai.Obstructions.Count > 0;
                    var readyToAcquire = seekProjectile || comp.Data.Repo.Values.State.TrackingReticle || checkObstructions || (comp.DetectOtherSignals && ai.DetectionInfo.OtherInRange || ai.DetectionInfo.PriorityInRange) && ai.DetectionInfo.ValidSignalExists(w);

                    Dictionary<object, Weapon> masterTargets;

                    if (readyToAcquire && (!w.System.TargetSlaving || rootConstruct.TrackedTargets.TryGetValue(w.System.StorageLocation, out masterTargets) && masterTargets.Count > 0))
                    {
                        if (comp.PrimaryWeapon != null && comp.PrimaryWeapon.System.DesignatorWeapon && comp.PrimaryWeapon != w && comp.PrimaryWeapon.Target.HasTarget) {

                            var topMost = comp.PrimaryWeapon.Target.TargetObject as MyEntity;
                            topMost = topMost?.GetTopMostParent();
                            Ai.AcquireTarget(w, false, topMost);
                        }
                        else
                        {
                            var requestedTopEntity = w.ShootRequest.TargetEntity?.GetTopMostParent();
                            Ai.AcquireTarget(w, requiresFocus, requestedTopEntity);
                        }
                    }

                    if (w.Target.HasTarget || !checkObstructions && !(comp.DetectOtherSignals && ai.DetectionInfo.OtherInRange || ai.DetectionInfo.PriorityInRange) ) {

                        w.TargetAcquireTick = uint.MaxValue;
                        AcquireTargets.RemoveAtFast(i);
                        if (w.Target.HasTarget) {
                            w.EventTriggerStateChanged(EventTriggers.Tracking, true);

                            if (MpActive)
                                w.Target.PushTargetToClient(w);
                        }
                    }
                }
            }
        }

        private void ShootWeapons()
        {
            for (int i = ShootingWeapons.Count - 1; i >= 0; i--) {
                
                var w = ShootingWeapons[i];
                var invalidWeapon = w.Comp.CoreEntity.MarkedForClose || w.Comp.Ai == null || w.Comp.Ai.Concealed || w.Comp.Ai.MarkedForClose || w.Comp.TopEntity.MarkedForClose || w.Comp.Platform.State != CorePlatform.PlatformState.Ready;
                var smartTimer = w.ActiveAmmoDef.AmmoDef.Trajectory.Guidance == Smart && w.System.TurretMovement == WeaponSystem.TurretType.Fixed && (QCount == w.ShortLoadId && w.Target.HasTarget && Tick - w.LastSmartLosCheck > 240 || Tick - w.LastSmartLosCheck > 1200);
                var quickSkip = invalidWeapon || w.Comp.IsBlock && smartTimer && !w.SmartLos() || w.PauseShoot || w.LiveSmarts >= w.System.MaxActiveProjectiles || (w.ProtoWeaponAmmo.CurrentAmmo == 0 && w.ClientMakeUpShots == 0) && w.ActiveAmmoDef.AmmoDef.Const.Reloadable;
                if (quickSkip) continue;

                w.Shoot();
            }
            ShootingWeapons.Clear();
        }

        private void GroupUpdates()
        {
            for (int i = 0; i < GridGroupUpdates.Count; i++)
                GridGroupUpdates[i].UpdateAis();

            GridGroupUpdates.Clear();
        }
    }
}
