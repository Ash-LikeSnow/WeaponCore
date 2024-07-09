using System;
using CoreSystems.Support;
using VRageMath;
using static CoreSystems.Support.CoreComponent;
using static CoreSystems.Support.WeaponDefinition.AnimationDef.PartAnimationSetDef;

namespace CoreSystems.Platform
{
    public partial class Weapon 
    {
        internal void ChangeActiveAmmoServer()
        {
            var proposed = ProposedAmmoId != -1;
            var ammoType = proposed ? System.AmmoTypes[ProposedAmmoId] : System.AmmoTypes[Reload.AmmoTypeId];
            ScheduleAmmoChange = false;

            if (ActiveAmmoDef == ammoType)
                return;
            if (proposed)
            {
                Reload.AmmoTypeId = ProposedAmmoId;
                ProposedAmmoId = -1;
                ProtoWeaponAmmo.CurrentAmmo = 0;
                Reload.CurrentMags = Comp.TypeSpecific != CompTypeSpecific.Phantom ? 0 : int.MaxValue;
            }

            ActiveAmmoDef = System.AmmoTypes[Reload.AmmoTypeId];
            if (string.IsNullOrEmpty(AmmoName)) 
                AmmoName = ActiveAmmoDef.AmmoDef.AmmoRound;
            PrepAmmoShuffle();

            if (!ActiveAmmoDef.AmmoDef.Const.EnergyAmmo)
                Reload.CurrentMags = Comp.TypeSpecific != CompTypeSpecific.Phantom ? Comp.CoreInventory.GetItemAmount(ActiveAmmoDef.AmmoDefinitionId).ToIntSafe() : int.MaxValue;

            AmmoName = ActiveAmmoDef.AmmoDef.AmmoRound;
            AmmoNameTerminal = ActiveAmmoDef.AmmoDef.Const.TerminalName;

            CheckInventorySystem = true;

            if (proposed && Comp.TypeSpecific == CompTypeSpecific.Rifle && Session.I.HandlesInput)
                Comp.HandReloadNotify(this);

            UpdateRof();
            SetWeaponDps();
            UpdateWeaponRange();
        }

        internal void ChangeActiveAmmoClient()
        {
            var ammoType = System.AmmoTypes[Reload.AmmoTypeId];

            if (ActiveAmmoDef == ammoType)
                return;

            ActiveAmmoDef = System.AmmoTypes[Reload.AmmoTypeId];
            PrepAmmoShuffle();

            if (Comp.TypeSpecific == CompTypeSpecific.Rifle)
                Comp.HandReloadNotify(this);

            UpdateRof();
            SetWeaponDps();
            UpdateWeaponRange();
        }

        internal void PrepAmmoShuffle()
        {
            if (AmmoShufflePattern.Length != ActiveAmmoDef.AmmoDef.Const.WeaponPatternCount) 
                Array.Resize(ref AmmoShufflePattern, ActiveAmmoDef.AmmoDef.Const.WeaponPatternCount);

            for (int i = 0; i < AmmoShufflePattern.Length; i++)
                AmmoShufflePattern[i] = i;
        }

        internal void QueueAmmoChange(int newAmmoId)
        {
            var serverAccept = Session.I.IsServer && (!Loading || DelayedCycleId < 0);
            var clientAccept = Session.I.IsClient && ClientMakeUpShots == 0 && !ServerQueuedAmmo && (!ClientReloading || ProtoWeaponAmmo.CurrentAmmo == 0);
            if (clientAccept || serverAccept)
            {
                DelayedCycleId = newAmmoId;
                AmmoName = System.AmmoTypes[newAmmoId].AmmoNameQueued;
                AmmoNameTerminal = "*" + System.AmmoTypes[newAmmoId].AmmoDef.Const.TerminalName;

                if (Session.I.IsClient && !System.DesignatorWeapon)
                    ChangeAmmo(newAmmoId);
            }
        }

        internal void ChangeAmmo(int newAmmoId)
        {
            if (Session.I.IsServer)
            {
                DelayedCycleId = -1;
                ProposedAmmoId = newAmmoId;
                var instantChange = Session.I.IsCreative || !ActiveAmmoDef.AmmoDef.Const.Reloadable;
                var canReload = ProtoWeaponAmmo.CurrentAmmo == 0;

                var proposedAmmo = System.AmmoTypes[ProposedAmmoId];

                if (instantChange)
                    ChangeActiveAmmoServer();
                else 
                    ScheduleAmmoChange = true;

                if (proposedAmmo.AmmoDef.Const.Reloadable && canReload)
                    ComputeServerStorage(true);
            }
            else 
                Session.I.SendAmmoCycleRequest(this, newAmmoId);
        }

        internal bool HasAmmo()
        {
            if (Session.I.IsCreative || !ActiveAmmoDef.AmmoDef.Const.Reloadable || Comp.InfiniteResource) {
                NoMagsToLoad = false;
                return true;
            }

            Reload.CurrentMags = Comp.TypeSpecific != CompTypeSpecific.Phantom ? Comp.CoreInventory.GetItemAmount(ActiveAmmoDef.AmmoDefinitionId).ToIntSafe() : Reload.CurrentMags;

            var energyDrainable = ActiveAmmoDef.AmmoDef.Const.EnergyAmmo && Comp.Ai.HasPower;
            var nothingToLoad = Reload.CurrentMags <= 0 && !energyDrainable;

            if (NoMagsToLoad) {
                if (nothingToLoad)
                    return false;

                EventTriggerStateChanged(EventTriggers.NoMagsToLoad, false);
                Comp.Ai.Construct.RootAi.Construct.OutOfAmmoWeapons.Remove(this);
                NoMagsToLoad = false;
                LastMagSeenTick = Session.I.Tick;
            }
            else if (nothingToLoad)
            {
                EventTriggerStateChanged(EventTriggers.NoMagsToLoad, true);
                Comp.Ai.Construct.RootAi.Construct.OutOfAmmoWeapons.Add(this);

                if (!NoMagsToLoad) 
                    CheckInventorySystem = true;

                NoMagsToLoad = true;
            }

            return !NoMagsToLoad;
        }

        internal bool ClientReload(bool networkCaller = false)
        {
            var syncUp = Reload.StartId > ClientStartId;

            if (!syncUp) {
                var energyDrainable = ActiveAmmoDef.AmmoDef.Const.EnergyAmmo && Comp.Ai.HasPower;
                if (Reload.CurrentMags <= 0 && !energyDrainable && ActiveAmmoDef.AmmoDef.Const.Reloadable && !Loading) {
                    
                    if (!Session.I.IsCreative) {

                        if (!NoMagsToLoad)
                            EventTriggerStateChanged(EventTriggers.NoMagsToLoad, true);
                        NoMagsToLoad = true;
                    }
                }
                
                if (Loading && ClientMakeUpShots < 1 && LoadingWait && Reload.EndId > ClientEndId)
                    Reloaded(1);

                return false;
            }
            ClientStartId = Reload.StartId;
            ClientMakeUpShots += ProtoWeaponAmmo.CurrentAmmo;


            ProtoWeaponAmmo.CurrentAmmo = 0;

            if (!Session.I.IsCreative) {

                if (NoMagsToLoad) {
                    EventTriggerStateChanged(EventTriggers.NoMagsToLoad, false);
                    NoMagsToLoad = false;
                }
            }

            ClientReloading = true;

            StartReload();
            return true;
        }

        //TODO account for float rounding errors to int here and in later inv pulling

        internal bool ComputeServerStorage(bool calledFromReload = false)
        {
            var s = Session.I;
            var isPhantom = Comp.TypeSpecific == CompTypeSpecific.Phantom;

            if (!Comp.IsWorking || !ActiveAmmoDef.AmmoDef.Const.Reloadable || !Comp.HasInventory && !isPhantom) return false;

            if (!ActiveAmmoDef.AmmoDef.Const.EnergyAmmo && !isPhantom)
            {
                if (!s.IsCreative)
                {
                    Comp.CurrentInventoryVolume = (float)Comp.CoreInventory.CurrentVolume;
                    var freeVolume = System.MaxAmmoVolume - Comp.CurrentInventoryVolume;
                    var spotsFree = (int)(freeVolume / ActiveAmmoDef.AmmoDef.Const.MagVolume + .0001f);
                    Reload.CurrentMags = Comp.CoreInventory.GetItemAmount(ActiveAmmoDef.AmmoDefinitionId).ToIntSafe();
                    CurrentAmmoVolume = Reload.CurrentMags * ActiveAmmoDef.AmmoDef.Const.MagVolume;

                    var magsRequested = (int)((System.FullAmmoVolume - CurrentAmmoVolume) / ActiveAmmoDef.AmmoDef.Const.MagVolume + .0001f);
                    var magsGranted = magsRequested > spotsFree ? spotsFree : magsRequested;
                    var requestedVolume = ActiveAmmoDef.AmmoDef.Const.MagVolume * magsGranted;
                    var spaceAvailable = freeVolume >= requestedVolume;
                    var pullAmmo = magsGranted > 0 && CurrentAmmoVolume < System.LowAmmoVolume && spaceAvailable;
                    
                    var failSafeTimer = s.Tick - LastInventoryTick > 600;
                    
                    if (pullAmmo && (CheckInventorySystem || failSafeTimer && Comp.Ai.Construct.RootAi.Construct.OutOfAmmoWeapons.Contains(this)) && s.PartToPullConsumable.TryAdd(this, byte.MaxValue)) {

                        CheckInventorySystem = false;
                        LastInventoryTick = s.Tick;
                        s.GridsToUpdateInventories.Add(Comp.Ai);
                    }
                    else if (CheckInventorySystem && failSafeTimer && !s.PartToPullConsumable.ContainsKey(this))
                        CheckInventorySystem = false;
                }
            }

            var outOfAmmo = ProtoWeaponAmmo.CurrentAmmo == 0;
            var sendHome = System.GoHomeToReload && !IsHome;

            if (outOfAmmo) {
                if (sendHome && !ReturingHome)
                    ScheduleWeaponHome(true);

                if (System.DropTargetUntilLoaded && Target.HasTarget)
                    Target.Reset(Session.I.Tick, Target.States.SendingHome);
            }

            var invalidStates = !outOfAmmo || sendHome || Loading || calledFromReload || Reload.WaitForClient || (System.MaxReloads > 0 && Reload.LifetimeLoads >= System.MaxReloads);
            return !invalidStates && ServerReload();
        }

        internal bool ServerReload()
        {
            if (DelayedCycleId >= 0)
                ChangeAmmo(DelayedCycleId);

            if (ScheduleAmmoChange) 
                ChangeActiveAmmoServer();

            var hasAmmo = HasAmmo();

            if (!hasAmmo) 
                return false;

            ++Reload.StartId;
            ++ClientStartId;
            ++Reload.LifetimeLoads;

            if (!ActiveAmmoDef.AmmoDef.Const.EnergyAmmo) {

                var isPhantom = Comp.TypeSpecific == CompTypeSpecific.Phantom;
                Reload.MagsLoaded = ActiveAmmoDef.AmmoDef.Const.MagsToLoad <= Reload.CurrentMags || Session.I.IsCreative ? ActiveAmmoDef.AmmoDef.Const.MagsToLoad : Reload.CurrentMags;
                
                if (!Session.I.IsCreative)
                {
                    if (!isPhantom && Comp.CoreInventory.ItemsCanBeRemoved(Reload.MagsLoaded, ActiveAmmoDef.AmmoDef.Const.AmmoItem))
                    {
                        if (System.HasAmmoSelection) {
                            var magItem = Comp.CoreInventory.FindItem(ActiveAmmoDef.AmmoDefinitionId) ?? ActiveAmmoDef.AmmoDef.Const.AmmoItem;
                            Comp.CoreInventory.RemoveItems(magItem.ItemId, Reload.MagsLoaded);
                        }
                        else
                        {
                            var magItem = Comp.TypeSpecific == CompTypeSpecific.Rifle ? Comp.CoreInventory.FindItem(ActiveAmmoDef.AmmoDefinitionId) ?? ActiveAmmoDef.AmmoDef.Const.AmmoItem : ActiveAmmoDef.AmmoDef.Const.AmmoItem;
                            Comp.CoreInventory.RemoveItems(magItem.ItemId, Reload.MagsLoaded);
                        }
                    }
                    else if (!isPhantom && Comp.CoreInventory.ItemCount > 0 && Comp.CoreInventory.ContainItems(Reload.MagsLoaded, ActiveAmmoDef.AmmoDef.Const.AmmoItem.Content))
                    {
                        Comp.CoreInventory.Remove(ActiveAmmoDef.AmmoDef.Const.AmmoItem, Reload.MagsLoaded);
                    }
                }

                Reload.CurrentMags = !isPhantom ? Comp.CoreInventory.GetItemAmount(ActiveAmmoDef.AmmoDefinitionId).ToIntSafe() : Reload.CurrentMags - Reload.MagsLoaded;
                if (Reload.CurrentMags == 0)
                    CheckInventorySystem = true;
            }

            StartReload();
            return true;
        }

        internal void StartReload()
        {
            Loading = true;
            if (Comp.TypeSpecific == CompTypeSpecific.Rifle)
                Comp.Rifle.GunBase.HasIronSightsActive = false;

            if (!ActiveAmmoDef.AmmoDef.Const.BurstMode && !ActiveAmmoDef.AmmoDef.Const.HasShotReloadDelay && System.Values.HardPoint.Loading.GiveUpAfter)
                GiveUpTarget();

            EventTriggerStateChanged(EventTriggers.Reloading, true);

            if (ActiveAmmoDef.AmmoDef.Const.MustCharge)
                ChargeReload();
            
            if (!ActiveAmmoDef.AmmoDef.Const.MustCharge || ActiveAmmoDef.AmmoDef.Const.IsHybrid) {

                var timeSinceShot = LastShootTick > 0 ? Session.I.Tick - LastShootTick : 0;
                var delayTime = timeSinceShot <= System.Values.HardPoint.Loading.DelayAfterBurst ? System.Values.HardPoint.Loading.DelayAfterBurst - timeSinceShot : 0;
                var delay = delayTime > 0 && ShotsFired == 0;
                if (System.WConst.ReloadTime > 0 || delay)
                {
                    ReloadEndTick = (uint)(Session.I.Tick + (!delay || System.WConst.ReloadTime > delayTime ? System.WConst.ReloadTime : delayTime));
                }
                else Reloaded(3);
            }

            if (Session.I.MpActive && Session.I.IsServer)
            {
                if (ActiveAmmoDef.AmmoDef.Const.SlowFireFixedWeapon && Comp.Data.Repo.Values.State.PlayerId > 0)
                    Reload.WaitForClient = !Session.I.IsHost;

                Session.I.SendWeaponReload(this);
            }

            if (ReloadEmitter == null || ReloadEmitter.IsPlaying) return;
            ReloadEmitter.PlaySound(System.ReloadSoundPairs, true, false, false, false, false, false);
        }

        internal void Reloaded(object o = null)
        {
            var input = o as int? ?? 0;
            var callBack = input == 1;
            var earlyExit = input == 2;
            using (Comp.CoreEntity.Pin()) {

                if (PartState == null || Comp.Data.Repo == null || Comp.Ai == null || Comp.CoreEntity.MarkedForClose) {
                    CancelReload();
                    return;
                }

                if (input == 4) {
                    ProtoWeaponAmmo.CurrentCharge = MaxCharge;
                    EstimatedCharge = MaxCharge;
                    return;
                }

                if (ActiveAmmoDef.AmmoDef.Const.MustCharge && !callBack && !earlyExit) {

                    ProtoWeaponAmmo.CurrentCharge = MaxCharge;
                    EstimatedCharge = MaxCharge;

                    if (ActiveAmmoDef.AmmoDef.Const.IsHybrid && LoadingWait)
                        return;
                }
                else if (ActiveAmmoDef.AmmoDef.Const.IsHybrid && Charging && ReloadEndTick != uint.MaxValue)
                {
                    ReloadEndTick = uint.MaxValue - 1;
                    return;
                }

                ProtoWeaponAmmo.CurrentAmmo = Reload.MagsLoaded * ActiveAmmoDef.AmmoDef.Const.MagazineSize;
                if (Session.I.IsServer) {

                    ++Reload.EndId;
                    ClientEndId = Reload.EndId;

                    if (Comp.TypeSpecific == CompTypeSpecific.Phantom && ActiveAmmoDef.AmmoDef.Const.EnergyAmmo)
                        --Reload.CurrentMags;

                    if (Session.I.MpActive) {

                        Session.I.SendWeaponReload(this);
                        if (Reload.EndId == 1)
                            Session.I.SendWeaponAmmoData(this);
                    }
                }
                else {
                    
                    ClientReloading = false;
                    ClientMakeUpShots = 0;
                    ClientEndId = Reload.EndId;
                    ServerQueuedAmmo = false;

                    if (DelayedCycleId == ActiveAmmoDef.AmmoDef.Const.AmmoIdxPos)
                    {
                        AmmoName = ActiveAmmoDef.AmmoDef.AmmoRound;
                        AmmoNameTerminal = ActiveAmmoDef.AmmoDef.Const.TerminalName;
                        DelayedCycleId = -1;
                    }

                    if (ActiveAmmoDef.AmmoDef.Const.SlowFireFixedWeapon && Session.I.PlayerId == Comp.Data.Repo.Values.State.PlayerId)
                        Session.I.SendClientReady(this);
                }

                TargetData.WeaponRandom.TurretRandom = new XorShiftRandomStruct((ulong)(TargetData.WeaponRandom.CurrentSeed + (Reload.EndId + 1000000)));
                EventTriggerStateChanged(EventTriggers.Reloading, false);
                LastLoadedTick = Session.I.Tick;

                if (!ActiveAmmoDef.AmmoDef.Const.HasShotReloadDelay)
                    ShotsFired = 0;

                Loading = false;
                ReloadEndTick = uint.MaxValue;
                ProjectileCounter = 0;
                NextMuzzle = 0;

                if (Comp.ShootManager.LastCycle != uint.MaxValue)
                    Comp.ShootManager.EndShootMode(ShootManager.EndReason.Reload);
            }
        }

        public void ChargeReload()
        {
            ProtoWeaponAmmo.CurrentCharge = 0;
            EstimatedCharge = 0;

            Comp.Ai.Charger.Add(this);
        }

        public void CancelReload()
        {
            if (ReloadEndTick == uint.MaxValue)
                return;
            NextMuzzle = 0;
            EventTriggerStateChanged(EventTriggers.Reloading, false);
            LastLoadedTick = Session.I.Tick;
            Loading = false;
            ReloadEndTick = uint.MaxValue;
            ProjectileCounter = 0;
        }
    }
}
