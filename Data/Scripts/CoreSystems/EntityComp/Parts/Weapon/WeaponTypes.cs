using System;
using System.Collections.Generic;
using CoreSystems.Projectiles;
using CoreSystems.Support;
using Sandbox.Game.Entities;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;

namespace CoreSystems.Platform
{
    public partial class Weapon 
    {
        internal class ParallelRayCallBack
        {
            internal Weapon Weapon;

            internal ParallelRayCallBack(Weapon weapon)
            {
                Weapon = weapon;
            }

            public void NormalShootRayCallBack(IHitInfo hitInfo)
            {
                var pTarget = Weapon.Target.TargetObject as Projectile;
                var eTarget = Weapon.Target.TargetObject as MyEntity;
                if (pTarget == null && eTarget == null)
                    return;
                Weapon.Casting = false;
                Weapon.PauseShoot = false;
                var masterWeapon = Weapon.System.TrackTargets ? Weapon : Weapon.Comp.PrimaryWeapon;
                var ignoreTargets = Weapon.Target.TargetState == Target.TargetStates.IsProjectile || Weapon.Target.TargetObject is IMyCharacter;
                var scope = Weapon.GetScope;
                var trackingCheckPosition = scope.CachedPos;
                double rayDist = 0;

                if (Session.I.DebugLos)
                {
                    var hitPos = hitInfo.Position;
                    if (rayDist <= 0) Vector3D.Distance(ref trackingCheckPosition, ref hitPos, out rayDist);

                    Session.I.AddLosCheck(new Session.LosDebug { Part = Weapon, HitTick = Session.I.Tick, Line = new LineD(trackingCheckPosition, hitPos) });
                }

                if (Weapon.Comp.Ai.ShieldNear)
                {
                    var targetPos = pTarget?.Position ?? eTarget.PositionComp.WorldMatrixRef.Translation;
                    var targetDir = targetPos - trackingCheckPosition;
                    if (Weapon.HitFriendlyShield(trackingCheckPosition, targetPos, targetDir))
                    {
                        masterWeapon.Target.Reset(Session.I.Tick, Target.States.RayCheckFriendly);
                        if (masterWeapon != Weapon) Weapon.Target.Reset(Session.I.Tick, Target.States.RayCheckFriendly);
                        return;
                    }
                }

                var hitTopEnt = (MyEntity)hitInfo?.HitEntity?.GetTopMostParent();
                if (hitTopEnt == null)
                {
                    if (Weapon.System.TargetGridCenter && eTarget != null)
                        hitTopEnt = eTarget;
                    else
                    {
                        if (ignoreTargets)
                            return;
                        masterWeapon.Target.Reset(Session.I.Tick, Target.States.RayCheckMiss);
                        if (masterWeapon != Weapon) Weapon.Target.Reset(Session.I.Tick, Target.States.RayCheckMiss);
                        return;
                    }
                }

                var targetTopEnt = eTarget?.GetTopMostParent();
                if (targetTopEnt == null)
                    return;

                var unexpectedHit = ignoreTargets || targetTopEnt != hitTopEnt;
                var topAsGrid = hitTopEnt as MyCubeGrid;

                if (unexpectedHit)
                {

                    if (hitTopEnt is MyVoxelBase && !Weapon.System.ScanNonThreats)
                    {
                        masterWeapon.Target.Reset(Session.I.Tick, Target.States.RayCheckVoxel);
                        if (masterWeapon != Weapon) Weapon.Target.Reset(Session.I.Tick, Target.States.RayCheckVoxel);
                        return;
                    }

                    if (topAsGrid == null)
                        return;
                    if (Weapon.Comp.Ai.AiType == Ai.AiTypes.Grid && topAsGrid.IsSameConstructAs(Weapon.Comp.Ai.GridEntity))
                    {
                        masterWeapon.Target.Reset(Session.I.Tick, Target.States.RayCheckSelfHit);
                        if (masterWeapon != Weapon) Weapon.Target.Reset(Session.I.Tick, Target.States.RayCheckSelfHit);
                        Weapon.PauseShoot = true;
                        return;
                    }
                    if (!Weapon.System.ScanNonThreats && (!topAsGrid.DestructibleBlocks || topAsGrid.Immune || topAsGrid.GridGeneralDamageModifier <= 0 || !Session.GridEnemy(Weapon.Comp.Ai.AiOwner, topAsGrid)))
                    {
                        masterWeapon.Target.Reset(Session.I.Tick, Target.States.RayCheckFriendly);
                        if (masterWeapon != Weapon) Weapon.Target.Reset(Session.I.Tick, Target.States.RayCheckFriendly);
                        return;
                    }
                    return;

                }
                if (Weapon.System.ClosestFirst && topAsGrid != null && topAsGrid == targetTopEnt)
                {
                    var halfExtMin = topAsGrid.PositionComp.LocalAABB.HalfExtents.Min();
                    var minSize = topAsGrid.GridSizeR * 8;
                    var maxChange = halfExtMin > minSize ? halfExtMin : minSize;
                    var targetPos = eTarget.PositionComp.WorldAABB.Center;
                    var weaponPos = trackingCheckPosition;

                    if (rayDist <= 0) Vector3D.Distance(ref weaponPos, ref targetPos, out rayDist);
                    var newHitShortDist = rayDist * (1 - hitInfo.Fraction);
                    var distanceToTarget = rayDist * hitInfo.Fraction;
                    var shortDistExceed = newHitShortDist - Weapon.Target.HitShortDist > maxChange;
                    var escapeDistExceed = distanceToTarget - Weapon.Target.OrigDistance > Weapon.Target.OrigDistance;
                    if (shortDistExceed || escapeDistExceed)
                    {
                        masterWeapon.Target.Reset(Session.I.Tick, Target.States.RayCheckDistOffset);
                        if (masterWeapon != Weapon) Weapon.Target.Reset(Session.I.Tick, Target.States.RayCheckDistOffset);
                    }
                }
            }

        }

        internal class Muzzle
        {
            internal Muzzle(Weapon weapon, int id)
            {
                MuzzleId = id;
                UniqueId = Session.I.UniqueMuzzleId.Id;
                Weapon = weapon;
            }

            internal Weapon Weapon;
            internal Vector3D Position;
            internal Vector3D Direction;
            internal Vector3D UpDirection;
            internal Vector3D DeviatedDir;
            internal uint LastUpdateTick;
            internal uint LastAv1Tick;
            internal uint LastAv2Tick;
            internal int MuzzleId;
            internal ulong UniqueId;
            internal bool Av1Looping;
            internal bool Av2Looping;

        }

        internal struct HiddenInfo
        {
            internal int SlotId;
            internal uint TickAdded;
        }

        internal struct TargetOwner
        {
            internal Weapon Weapon;
            internal uint ReleasedTick;
        }

        public class ApiShootRequest
        {
            public ApiShootRequest(Weapon weapon)
            {
                Weapon = weapon;
            }

            public enum TargetType
            {
                None,
                Position,
                MyEntity,
                Projectile,
            }

            public readonly Weapon Weapon;
            public object RawTarget;
            public bool Dirty;
            public bool AcquireTarget;
            public TargetType Type;
            public Vector3D Position = Vector3D.MaxValue;
            public ulong ProjectileId = ulong.MaxValue;
            public uint RequestTick = uint.MaxValue;
            public MyEntity TargetEntity;
            public double ExtraShotAngle;

            public bool Update(object target, double extraShotAngle)
            {
                var weaponBusy = Weapon.ProtoWeaponAmmo.CurrentAmmo == 0 || Weapon.Loading || Weapon.Reload.WaitForClient || (Weapon.System.MaxReloads > 0 && Weapon.Reload.LifetimeLoads >= Weapon.System.MaxReloads);

                if (Dirty || weaponBusy)
                    return false;

                Dirty = true;
                Weapon.Comp.ShootRequestDirty = true;
                RequestTick = Session.I.Tick + 1;
                RawTarget = target;

                var entity = target as MyEntity;
                var position = target as Vector3D?;
                var projectileId = target as ulong?;

                if (entity != null)
                {
                    TargetEntity = entity;
                    Type = TargetType.MyEntity;
                    ExtraShotAngle = extraShotAngle;
                    AcquireTarget = Weapon.TurretController;
                    Weapon.Comp.ShootManager.RequestShootSync(Session.I.PlayerId, ShootManager.RequestType.Once, ShootManager.Signals.Once);
                    return true;
                }

                if (position != null)
                {
                    Position = position.Value;
                    Type = TargetType.Position;
                    ExtraShotAngle = extraShotAngle;
                    Weapon.Comp.ShootManager.RequestShootSync(Session.I.PlayerId, ShootManager.RequestType.Once, ShootManager.Signals.Once);
                    return true;
                }

                if (projectileId != null)
                {
                    ProjectileId = projectileId.Value;
                    Type = TargetType.Projectile;
                    ExtraShotAngle = extraShotAngle;
                    AcquireTarget = Weapon.TurretController;
                    Weapon.Comp.ShootManager.RequestShootSync(Session.I.PlayerId, ShootManager.RequestType.Once, ShootManager.Signals.Once);
                    return true;
                }

                Clean();
                return false;
            }

            public void Clean()
            {
                Type = TargetType.None;
                TargetEntity = null;
                RawTarget = null;
                Position = Vector3D.MaxValue;
                ProjectileId = ulong.MaxValue;
                RequestTick = uint.MaxValue;
                ExtraShotAngle = 0;
                AcquireTarget = false;
                Dirty = false;
            }

        }

        public class ShootManager
        {
            public readonly WeaponComponent Comp;
            internal bool WaitingShootResponse;
            internal bool FreezeClientShoot;
            internal Signals Signal;
            internal uint CompletedCycles;
            internal uint LastCycle = uint.MaxValue;
            internal uint LastShootTick;
            internal uint PrevShootEventTick;
            internal uint WaitingTick;
            internal uint FreezeTick;
            internal int ShootDelay;
            internal uint ClientToggleCount;
            internal int WeaponsFired;

            public enum RequestType
            {
                On,
                Off,
                Once,
            }

            public enum Signals
            {
                None,
                Manual,
                MouseControl,
                On,
                Once,
                KeyToggle,
            }

            public enum ShootModes
            {
                AiShoot,
                MouseControl,
                KeyToggle,
                KeyFire,
            }

            internal enum ShootCodes
            {
                ServerResponse,
                ClientRequest,
                ServerRequest,
                ServerRelay,
                ToggleServerOff,
                ToggleClientOff,
                ClientRequestReject,
            }

            internal enum EndReason
            {
                Overheat,
                Reload,
                Toggle,
                ServerRequested,
                ServerAhead,
                Failed,
                ShootSync,
                Rejected,
            }

            public ShootManager(WeaponComponent comp)
            {
                Comp = comp;
            }


            #region InputManager
            internal bool RequestShootSync(long playerId, RequestType request, Signals signal = Signals.None) // this shoot method mixes client initiation with server delayed server confirmation in order to maintain sync while avoiding authoritative delays in the common case. 
            {
                var values = Comp.Data.Repo.Values;
                var state = values.State;
                var isRequestor = !Session.I.IsClient || playerId == Session.I.PlayerId;
                
                if (isRequestor && Session.I.IsClient && request == RequestType.Once && (WaitingShootResponse || FreezeClientShoot || CompletedCycles > 0 || ClientToggleCount > state.ToggleCount || state.Trigger != CoreComponent.Trigger.Off)) 
                    return false;

                if (isRequestor && !ProcessInput(playerId, request, signal) || !MakeReadyToShoot()) {
                    ChangeState(request, playerId, false);
                    return false;
                }

                Signal = request != RequestType.Off ? signal : Signals.None;

                if (Comp.IsBlock && Session.I.HandlesInput)
                    Session.I.TerminalMon.HandleInputUpdate(Comp);

                var sendRequest = !Session.I.IsClient || playerId == Session.I.PlayerId; // this method is used both by initiators and by receives. 
                
                if (Session.I.MpActive && sendRequest)
                {
                    WaitingShootResponse = Session.I.IsClient; // this will be set false on the client once the server responds to this packet
                    
                    if (WaitingShootResponse)
                        ClientToggleCount = state.ToggleCount + 1;

                    WaitingTick = Session.I.Tick;

                    var code = Session.I.IsServer ? playerId == 0 ? ShootCodes.ServerRequest : ShootCodes.ServerRelay : ShootCodes.ClientRequest;
                    ulong packagedMessage;
                    EncodeShootState((uint)request, (uint)signal, CompletedCycles, (uint)code, out packagedMessage);
                    if (playerId > 0) // if this is the server responding to a request, rewrite the packet sent to the origin client with a special response code.
                        Session.I.SendShootRequest(Comp, packagedMessage, PacketType.ShootSync, RewriteShootSyncToServerResponse, playerId);
                    else
                        Session.I.SendShootRequest(Comp, packagedMessage, PacketType.ShootSync, null, playerId);
                }

                ChangeState(request, playerId, true);

                return true;
            }

            internal bool ProcessInput(long playerId, RequestType request, Signals signal)
            {
                if (ShootRequestPending(request))
                    return false;

                var state = Comp.Data.Repo.Values.State;
                var wasToggled = ClientToggleCount > state.ToggleCount || state.Trigger == CoreComponent.Trigger.On;
                if (wasToggled && request != RequestType.On && !FreezeClientShoot) // toggle off
                {
                    if (Session.I.MpActive)
                    {
                        FreezeClientShoot = Session.I.IsClient; //if the initiators is a client pause future cycles until the server returns which cycle state to terminate on.
                        FreezeTick = Session.I.Tick;

                        ulong packagedMessage;
                        EncodeShootState((uint)request, (uint)signal, CompletedCycles, (uint)ShootCodes.ToggleServerOff, out packagedMessage);
                        Session.I.SendShootRequest(Comp, packagedMessage, PacketType.ShootSync, RewriteShootSyncToServerResponse, playerId);
                    }

                    if (Session.I.IsServer) 
                        EndShootMode(EndReason.ServerRequested);

                    Signal = request != RequestType.Off ? signal : Signals.None;
                }

                var pendingRequest = Comp.IsDisabled || wasToggled || Comp.IsBlock && !Comp.Cube.IsWorking;

                return !pendingRequest;
            }

            private bool ShootRequestPending(RequestType requestType)
            {
                if (FreezeClientShoot || WaitingShootResponse)
                {
                    return true;
                }
                return false;
            }

            private void ChangeState(RequestType request, long playerId, bool activated)
            {

                var state = Comp.Data.Repo.Values.State;

                // Pretty sus that this is allowed by client, possible race condition... likely needed by client side prediction
                state.PlayerId = playerId;
                if (Session.I.IsServer)
                {
                    switch (request)
                    {
                        case RequestType.Off:
                            state.Trigger = CoreComponent.Trigger.Off;
                            break;
                        case RequestType.On:
                            state.Trigger = CoreComponent.Trigger.On;
                            break;
                        case RequestType.Once:
                            state.Trigger = CoreComponent.Trigger.Once;
                            break;
                    }

                    if (activated)
                        ++state.ToggleCount;

                    if (Session.I.MpActive)
                        Session.I.SendState(Comp);
                }

                if (activated)
                    LastCycle = ClientToggleCount > state.ToggleCount && request != RequestType.Once || state.Trigger == CoreComponent.Trigger.On || Session.I.IsClient && request == RequestType.On && playerId == 0 ? uint.MaxValue : 1;

            }
            #endregion

            #region Main
            internal void RestoreWeaponShot()
            {
                for (int i = 0; i < Comp.Collection.Count; i++)
                {
                    var w = Comp.Collection[i];
                    var predicted = w.ActiveAmmoDef.AmmoDef.Const.ClientPredictedAmmo;
                    if (w.ActiveAmmoDef.AmmoDef.Const.MustCharge && !predicted)
                    {
                        Log.Line($"RestoreWeaponShot, recharge", Session.InputLog);
                        w.ProtoWeaponAmmo.CurrentCharge = w.MaxCharge;
                        w.EstimatedCharge = w.MaxCharge;
                    }
                    else if (!predicted)
                    {
                        w.ProtoWeaponAmmo.CurrentAmmo += (int)CompletedCycles;
                        Log.Line($"RestoreWeaponShot, return ammo:{CompletedCycles}", Session.InputLog);
                    }
                }
            }

            internal void UpdateShootSync(Weapon w)
            {
                if (--w.ShootCount == 0 && ++WeaponsFired >= Comp.TotalWeapons)
                {
                    var overrides = w.Comp.Data.Comp.Data.Repo.Values.Set.Overrides;
                    var state = w.Comp.Data.Comp.Data.Repo.Values.State;

                    ++CompletedCycles;
                    
                    var toggled = w.Comp.ShootManager.ClientToggleCount > state.ToggleCount || state.Trigger == CoreComponent.Trigger.On;
                    var overCount = CompletedCycles >= LastCycle;

                    if (!toggled || overCount)
                        EndShootMode(EndReason.ShootSync);
                    else
                    {
                        MakeReadyToShoot(true);
                        ShootDelay = overrides.BurstDelay;
                    }
                }
            }

            internal bool MakeReadyToShoot(bool skipReady = false)
            {
                var weaponsReady = 0;
                var totalWeapons = Comp.Collection.Count;
                var overrides = Comp.Data.Repo.Values.Set.Overrides;
                var burstTarget = overrides.BurstCount;
                var client = Session.I.IsClient;
                for (int i = 0; i < totalWeapons; i++)
                {
                    var w = Comp.Collection[i];
                    if (!w.System.DesignatorWeapon)
                    {
                        var aConst = w.ActiveAmmoDef.AmmoDef.Const;
                        var reloading = aConst.Reloadable && w.ClientMakeUpShots == 0 && (w.Loading || w.ProtoWeaponAmmo.CurrentAmmo == 0 || w.Reload.WaitForClient);

                        var reloadMinusAmmoCheck = aConst.Reloadable && w.ClientMakeUpShots == 0 && (w.Loading || w.Reload.WaitForClient);
                        var skipReload = client && reloading && !skipReady && !FreezeClientShoot && !WaitingShootResponse && !reloadMinusAmmoCheck && Session.I.Tick - LastShootTick > 30;
                        var overHeat = w.PartState.Overheated && w.OverHeatCountDown == 0;
                        var canShoot = !overHeat && (!reloading || skipReload);

                        if (canShoot && skipReload)
                            Log.Line($"ReadyToShoot succeeded on client but with CurrentAmmo > 0 - shooting:{w.IsShooting} - charging:{w.Charging} - charge:{w.ProtoWeaponAmmo.CurrentCharge}({w.MaxCharge})", Session.InputLog);

                        var weaponReady = canShoot && !w.IsShooting;

                        if (!weaponReady && !skipReady)
                        {
                            if (Session.I.IsServer) 
                                Log.Line($"MakeReadyToShoot: canShoot:{canShoot} - alreadyShooting:{w.IsShooting} - reloading:{reloading} - skipReload:{skipReload} - CurrentAmmo:{w.ProtoWeaponAmmo.CurrentAmmo} - wait:{w.Reload.WaitForClient}", Session.InputLog);
                            break;
                        }

                        weaponsReady += 1;

                        w.ShootCount += MathHelper.Clamp(burstTarget, 1, w.ProtoWeaponAmmo.CurrentAmmo + w.ClientMakeUpShots);
                    }
                    else
                        weaponsReady += 1;
                }

                var ready = weaponsReady == totalWeapons;

                if (!ready && weaponsReady > 0)
                {
                    Log.Line($"not ready to MakeReadyToShoot", Session.InputLog);
                    ResetShootRequest();
                }

                return ready;
            }

            internal void ResetShootRequest()
            {
                for (int i = 0; i < Comp.Collection.Count; i++)
                    Comp.Collection[i].ShootCount = 0;

                WeaponsFired = 0;
            }

            internal void EndShootMode(EndReason reason, bool skipNetwork = false)
            {
                var wValues = Comp.Data.Repo.Values;

                for (int i = 0; i < Comp.TotalWeapons; i++)
                {
                    var w = Comp.Collection[i];
                    if (Session.I.MpActive && reason != EndReason.Overheat) 
                        Log.Line($"[clear] Reason:{reason} - ammo:{w.ProtoWeaponAmmo.CurrentAmmo} - Trigger:{wValues.State.Trigger} - Signal:{Signal} - Cycles:{CompletedCycles}[{LastCycle}] - Count:{wValues.State.ToggleCount}[{ClientToggleCount}] - WeaponsFired:{WeaponsFired}", Session.InputLog);

                    if (w.ShootRequest.Dirty && reason != EndReason.ShootSync)
                        w.ShootRequest.Clean();

                    w.ShootCount = 0;
                }

                ShootDelay = 0;
                CompletedCycles = 0;
                WeaponsFired = 0;
                LastCycle = uint.MaxValue;

                ClientToggleCount = 0;
                FreezeClientShoot = false;
                WaitingShootResponse = false;
                Signal = Signals.None;
                if (Session.I.IsServer)
                {
                    wValues.State.Trigger = CoreComponent.Trigger.Off;
                    if (Session.I.MpActive && !skipNetwork)
                    {
                        Session.I.SendState(Comp);
                    }
                }
            }
            #endregion


            #region Network

            internal void ServerRejectResponse(ulong clientId, RequestType requestType)
            {
                Log.Line($"[server rejecting] Signal:{Signal} - CompletedCycles:{CompletedCycles} requestType:{requestType} - Trigger:{Comp.Data.Repo.Values.State.Trigger}", Session.InputLog);
                ulong packagedMessage;
                EncodeShootState(0, (uint)Signals.None, CompletedCycles, (uint)ShootCodes.ClientRequestReject, out packagedMessage);
                Session.I.SendShootReject(Comp, packagedMessage, PacketType.ShootSync, clientId);

                EndShootMode(EndReason.Rejected);
            }


            internal void ReceivedServerReject()
            {
                Log.Line($"[client rejection] message reset - wait:{WaitingShootResponse} - frozen:{FreezeClientShoot}", Session.InputLog);
                if (CompletedCycles > 0)
                    RestoreWeaponShot();

                EndShootMode(EndReason.Rejected);
            }

            internal void FailSafe()
            {
                Log.Line($"ShootMode failsafe triggered: LastCycle:{LastCycle} - CompletedCycles:{CompletedCycles} - WeaponsFired:{WeaponsFired} - wait:{WaitingShootResponse} - freeze:{FreezeClientShoot}", Session.InputLog);
                EndShootMode(EndReason.Failed);
            }

            internal void ServerToggleOffByClient(uint interval)
            {

                var clientMakeupRequest = interval > CompletedCycles && LastCycle == uint.MaxValue;
                var endCycle = !clientMakeupRequest ? CompletedCycles : interval;

                ulong packagedMessage;
                EncodeShootState(0, (uint)Signals.None, endCycle, (uint)ShootCodes.ToggleClientOff, out packagedMessage);
                Session.I.SendShootRequest(Comp, packagedMessage, PacketType.ShootSync, null, 0);

                if (!clientMakeupRequest)
                {
                    EndShootMode(EndReason.Toggle);
                }
                else
                {
                    Log.Line($"server catching up to client -- from:{CompletedCycles} to:{interval}", Session.InputLog);
                    LastCycle = endCycle;
                }
            }


            internal void ClientToggledOffByServer(uint interval, bool server = false)
            {
                if (server)
                    Log.Line($"server requested toggle off? - wait:{WaitingShootResponse} - mode:{Comp.Data.Repo.Values.Set.Overrides.ShootMode} - freeze:{FreezeClientShoot} - CompletedCycles:{CompletedCycles}({interval}) - LastCycle:{LastCycle}", Session.InputLog);

                if (interval > CompletedCycles)
                {
                    Log.Line($"[ClientToggledOffByServer] server interval {interval} > client: {CompletedCycles} - frozen:{FreezeClientShoot} - wait:{WaitingShootResponse}", Session.InputLog);
                }
                else if (interval < CompletedCycles) // look into adding a condition where the requesting client can cause the server to shoot for n burst to match client without exceeding reload, would need to freeze client.
                {
                    Log.Line($"[ClientToggledOffByServer] server interval {interval} < client:{CompletedCycles} - frozen:{FreezeClientShoot} - wait:{WaitingShootResponse}", Session.InputLog);
                }

                if (interval <= CompletedCycles)
                {
                    EndShootMode(EndReason.Toggle);
                }
                else if (interval > CompletedCycles)
                {
                    Log.Line($"[ClientToggleResponse] client is behind server: Current: {CompletedCycles} freeze:{FreezeClientShoot} - target:{interval} - LastCycle:{LastCycle}", Session.InputLog);

                    //LastCycle = interval;
                    EndShootMode(EndReason.ServerAhead);

                }
                FreezeClientShoot = false;
            }

            private static object RewriteShootSyncToServerResponse(object o1, object o2)
            {
                var ulongPacket = (ULongUpdatePacket)o1;

                RequestType type;
                Signals signal;
                ShootCodes code;
                uint internval;

                DecodeShootState(ulongPacket.Data, out type, out signal, out internval, out code);

                code = ShootCodes.ServerResponse;
                ulong packagedMessage;
                EncodeShootState((uint)type, (uint)signal, internval, (uint)code, out packagedMessage);

                ulongPacket.Data = packagedMessage;

                return ulongPacket;
            }


            internal static void DecodeShootState(ulong id, out RequestType type, out Signals shootState, out uint interval, out ShootCodes code)
            {
                type = (RequestType)(id >> 48);

                shootState = (Signals)((id << 16) >> 48);
                interval = (uint)((id << 32) >> 48);
                code = (ShootCodes)((id << 48) >> 48);
            }

            internal static void EncodeShootState(uint type, uint shootState, uint interval, uint code, out ulong id)
            {
                id = ((ulong)(type << 16 | shootState) << 32) | (interval << 16 | code);
            }

            #endregion
        }
    }
}
