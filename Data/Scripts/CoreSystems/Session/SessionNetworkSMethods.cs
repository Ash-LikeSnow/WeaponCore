using CoreSystems.Platform;
using CoreSystems.Support;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.Entity;
using VRageMath;
using static CoreSystems.Platform.ControlSys;
using static CoreSystems.Support.Focus;

namespace CoreSystems
{
    public partial class Session
    {
        private bool ServerActiveControlUpdate(PacketObj data)
        {
            var packet = data.Packet;
            var dPacket = (BoolUpdatePacket)packet;
            var entity = MyEntities.GetEntityByIdOrDefault(packet.EntityId);
            var topEntity = entity?.GetTopMostParent();
            if (topEntity == null) return Error(data, Msg("TopEntity"));

            Ai ai;
            long playerId = 0;
            if (EntityToMasterAi.TryGetValue(topEntity, out ai) && SteamToPlayer.TryGetValue(packet.SenderId, out playerId))
            {
                ai.Construct.NetRefreshAi();
                data.Report.PacketValid = true;
            }
            else Log.Line($"ServerActiveControlUpdate: ai:{ai != null} - targetingAi:{EntityAIs.ContainsKey(topEntity)} - masterAi:{EntityToMasterAi.ContainsKey(topEntity)} - playerId:{playerId}({packet.SenderId}) - marked:{entity.MarkedForClose}({topEntity.MarkedForClose}) - active:{dPacket.Data} - inTopMap:{TopEntityToInfoMap.ContainsKey(topEntity)} - controlName:{entity.DebugName}");

            return true;
        }

        private bool ServerUpdateSetting(PacketObj data)
        {
            var packet = data.Packet;
            var ent = MyEntities.GetEntityByIdOrDefault(packet.EntityId);
            var comp = ent?.Components.Get<CoreComponent>();
            if (comp?.Ai == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return Error(data, Msg($"CompId: {packet.EntityId}", comp != null), Msg("Ai", comp?.Ai != null), Msg("Ai", comp?.Platform.State == CorePlatform.PlatformState.Ready));

            switch (packet.PType)
            {
                case PacketType.RequestSetRof:
                    {
                        BlockUi.RequestSetRof(comp.CoreEntity as IMyTerminalBlock, ((FloatUpdatePacket)packet).Data);
                        break;
                    }
                case PacketType.RequestSetRange:
                    {
                        if (comp is Weapon.WeaponComponent)
                            BlockUi.RequestSetRange(comp.CoreEntity as IMyTerminalBlock, ((FloatUpdatePacket)packet).Data);
                        else
                            BlockUi.RequestSetRangeControl(comp.CoreEntity as IMyTerminalBlock, ((FloatUpdatePacket)packet).Data);
                        break;
                    }
                case PacketType.RequestSetReportTarget:
                    {
                        if (comp is Weapon.WeaponComponent)
                            BlockUi.RequestSetReportTarget(comp.CoreEntity as IMyTerminalBlock, ((BoolUpdatePacket)packet).Data);
                        else
                            BlockUi.RequestSetReportTargetControl(comp.CoreEntity as IMyTerminalBlock, ((BoolUpdatePacket)packet).Data);
                        break;
                    }
                case PacketType.RequestSetOverload:
                    {
                        BlockUi.RequestSetOverload(comp.CoreEntity as IMyTerminalBlock, ((BoolUpdatePacket)packet).Data);
                        break;
                    }
            }

            data.Report.PacketValid = true;


            return true;
        }
        private bool ServerAimTargetUpdate(PacketObj data)
        {
            var packet = data.Packet;
            var targetPacket = (FakeTargetPacket)packet;
            var entity = MyEntities.GetEntityByIdOrDefault(packet.EntityId);

            if (entity == null) return Error(data, Msg($"EntityId:{packet.EntityId} - entityExists:{MyEntities.EntityExists(packet.EntityId)}"));


            Ai ai;
            long playerId;
            if (EntityAIs.TryGetValue(entity, out ai) && SteamToPlayer.TryGetValue(packet.SenderId, out playerId))
            {
                Ai.FakeTargets fakeTargets;
                if (PlayerDummyTargets.TryGetValue(playerId, out fakeTargets))
                {
                    fakeTargets.ManualTarget.Sync(targetPacket, ai);
                    PacketsToClient.Add(new PacketInfo { Entity = entity, Packet = targetPacket });

                    data.Report.PacketValid = true;
                }
            }
            else
                return Error(data, Msg($"Ai not found, is marked:{entity.MarkedForClose}, has root:{EntityToMasterAi.ContainsKey(entity)}"));

            return true;
        }

        private bool ServerPaintedTargetUpdate(PacketObj data)
        {
            var packet = data.Packet;
            var targetPacket = (PaintedTargetPacket)packet;
            var entity = MyEntities.GetEntityByIdOrDefault(packet.EntityId) ;

            if (entity == null) return Error(data, Msg($"EntityId:{packet.EntityId} - entityExists:{MyEntities.EntityExists(packet.EntityId)}"));


            Ai.FakeTargets fakeTargets;
            Ai ai;
            long playerId;
            if (EntityAIs.TryGetValue(entity, out ai) && SteamToPlayer.TryGetValue(packet.SenderId, out playerId) && PlayerDummyTargets.TryGetValue(playerId, out fakeTargets))
            {
                fakeTargets.PaintedTarget.Sync(targetPacket, ai);
                PacketsToClient.Add(new PacketInfo { Entity = entity, Packet = targetPacket });
                data.Report.PacketValid = true;
            }
            else
                return Error(data, Msg($"Ai not found, is marked:{entity.MarkedForClose}, has root:{EntityToMasterAi.ContainsKey(entity)}"));

            return true;
        }

        private bool ServerAmmoCycleRequest(PacketObj data)
        {
            var packet = data.Packet;
            var cyclePacket = (AmmoCycleRequestPacket)packet;
            var ent = MyEntities.GetEntityByIdOrDefault(packet.EntityId);
            var comp = ent?.Components.Get<CoreComponent>() as Weapon.WeaponComponent;

            if (comp?.Ai == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return Error(data, Msg("BaseComp", comp != null), Msg("Ai", comp?.Ai != null), Msg("Ai", comp?.Platform.State == CorePlatform.PlatformState.Ready));

            comp.Data.Repo.Values.State.PlayerId = cyclePacket.PlayerId;
            comp.Platform.Weapons[cyclePacket.PartId].QueueAmmoChange(cyclePacket.NewAmmoId);
            data.Report.PacketValid = true;

            return true;
        }

        private bool ServerPlayerControlRequest(PacketObj data)
        {
            var packet = data.Packet;
            var controlPacket = (PlayerControlRequestPacket)packet;
            var ent = MyEntities.GetEntityByIdOrDefault(packet.EntityId);
            var comp = ent?.Components.Get<CoreComponent>() as Weapon.WeaponComponent;

            if (comp?.Ai == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return Error(data, Msg("BaseComp", comp != null), Msg("Ai", comp?.Ai != null), Msg("Ai", comp?.Platform.State == CorePlatform.PlatformState.Ready));

            comp.Data.Repo.Values.State.PlayerId = controlPacket.PlayerId;
            comp.Data.Repo.Values.State.Control = controlPacket.Mode;
            SendComp(comp);
            data.Report.PacketValid = true;

            return true;
        }

        private bool ServerReticleUpdate(PacketObj data)
        {
            var packet = data.Packet;
            var reticlePacket = (BoolUpdatePacket)packet;
            var ent = MyEntities.GetEntityByIdOrDefault(packet.EntityId);
            var comp = ent?.Components.Get<CoreComponent>() as CoreComponent;

            if (comp?.Ai == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return Error(data, Msg("BaseComp", comp != null), Msg("Ai", comp?.Ai != null), Msg("Ai", comp?.Platform.State == CorePlatform.PlatformState.Ready));


            var wComp = comp as Weapon.WeaponComponent;
            var cComp = comp as ControlComponent;
            if (wComp != null)
            {
                var wValues = wComp.Data.Repo.Values;
                wValues.State.TrackingReticle = reticlePacket.Data;
                comp.ManualMode = wValues.State.TrackingReticle && wValues.Set.Overrides.Control == ProtoWeaponOverrides.ControlModes.Manual;

                SendState(wComp);
            }
            else if (cComp != null)
            {
                var wValues = cComp.Data.Repo.Values;
                wValues.State.TrackingReticle = reticlePacket.Data;
                comp.ManualMode = wValues.State.TrackingReticle && wValues.Set.Overrides.Control == ProtoWeaponOverrides.ControlModes.Manual;

                SendState(cComp);
            }

            data.Report.PacketValid = true;

            return true;
        }

        private bool ServerCountingDownUpdate(PacketObj data)
        {
            var packet = data.Packet;
            var countingDownPacket = (BoolUpdatePacket)packet;
            var ent = MyEntities.GetEntityByIdOrDefault(packet.EntityId);
            var comp = ent?.Components.Get<CoreComponent>() as Weapon.WeaponComponent;

            if (comp?.Ai == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return Error(data, Msg("BaseComp", comp != null), Msg("Ai", comp?.Ai != null), Msg("Ai", comp?.Platform.State == CorePlatform.PlatformState.Ready));

            comp.Data.Repo.Values.State.CountingDown = countingDownPacket.Data;
            SendState(comp);

            data.Report.PacketValid = true;

            return true;
        }

        private bool ServerCriticalReactionUpdate(PacketObj data)
        {
            var packet = data.Packet;
            var countingDownPacket = (BoolUpdatePacket)packet;
            var ent = MyEntities.GetEntityByIdOrDefault(packet.EntityId);
            var comp = ent?.Components.Get<CoreComponent>() as Weapon.WeaponComponent;

            if (comp?.Ai == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return Error(data, Msg("BaseComp", comp != null), Msg("Ai", comp?.Ai != null), Msg("Ai", comp?.Platform.State == CorePlatform.PlatformState.Ready));

            comp.Data.Repo.Values.State.CountingDown = countingDownPacket.Data;
            SendState(comp);

            data.Report.PacketValid = true;

            return true;
        }

        private bool ServerOverRidesUpdate(PacketObj data)
        {
            var packet = data.Packet;
            var overRidesPacket = (OverRidesPacket)packet;
            var ent = MyEntities.GetEntityByIdOrDefault(packet.EntityId, null, true);
            var comp = ent?.Components.Get<CoreComponent>();
            if (comp?.Ai == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return Error(data, Msg("BaseComp", comp != null), Msg("Ai", comp?.Ai != null), Msg("Ai", comp?.Platform.State == CorePlatform.PlatformState.Ready));

            var wComp = comp as Weapon.WeaponComponent;
            if (wComp != null) Weapon.WeaponComponent.RequestSetValue(wComp, overRidesPacket.Setting, overRidesPacket.Value, SteamToPlayer[overRidesPacket.SenderId]);
            var cComp = comp as ControlSys.ControlComponent;
            if (cComp != null) ControlSys.ControlComponent.RequestSetValue(cComp, overRidesPacket.Setting, overRidesPacket.Value, SteamToPlayer[overRidesPacket.SenderId]);

            data.Report.PacketValid = true;

            return true;
        }

        private bool ServerDroneUpdate(PacketObj data)
        {
            var packet = data.Packet;
            var dronePacket = (DronePacket)packet;
            var ent = MyEntities.GetEntityByIdOrDefault(packet.EntityId, null, true);
            var comp = ent?.Components.Get<CoreComponent>();
            if (comp?.Ai == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return Error(data, Msg("BaseComp", comp != null), Msg("Ai", comp?.Ai != null), Msg("Ai", comp?.Platform.State == CorePlatform.PlatformState.Ready));

            var wComp = comp as Weapon.WeaponComponent;
            if (wComp != null) Weapon.WeaponComponent.RequestDroneSetValue(wComp, dronePacket.Setting, dronePacket.Value, SteamToPlayer[dronePacket.SenderId]);

            data.Report.PacketValid = true;

            return true;
        }

        private bool ServerClientAiExists(PacketObj data)
        {
            var packet = data.Packet;

            if (PlayerEntityIdInRange.ContainsKey(packet.SenderId))
            {
                switch (packet.PType)
                {
                    case PacketType.ClientAiRemove:
                        PlayerEntityIdInRange[packet.SenderId].Remove(packet.EntityId);
                        break;
                    case PacketType.ClientAiAdd:
                        PlayerEntityIdInRange[packet.SenderId].Add(packet.EntityId);
                        break;
                }
            }
            else return Error(data, Msg("SenderId not found"));

            data.Report.PacketValid = true;

            return true;
        }

        private bool ServerRequestShootUpdate(PacketObj data)
        {
            var packet = data.Packet;
            var shootStatePacket = (ShootStatePacket)packet;
            var ent = MyEntities.GetEntityByIdOrDefault(packet.EntityId);
            var comp = ent?.Components.Get<CoreComponent>() as Weapon.WeaponComponent;

            if (comp?.Ai == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return Error(data, Msg("BaseComp", comp != null), Msg("Ai", comp?.Ai != null), Msg("Ai", comp?.Platform.State == CorePlatform.PlatformState.Ready));

            //comp.RequestShootUpdate(shootStatePacket.Action, shootStatePacket.PlayerId);
            data.Report.PacketValid = true;

            return true;
        }

        private bool ServerFocusUpdate(PacketObj data)
        {
            var packet = data.Packet;
            var focusPacket = (FocusPacket)packet;
            var entity = MyEntities.GetEntityByIdOrDefault(packet.EntityId);

            if (entity == null) return Error(data, Msg("Entity"));

            Ai ai;
            if (EntityToMasterAi.TryGetValue(entity, out ai))
            {
                var target = MyEntities.GetEntityByIdOrDefault(focusPacket.TargetId);
                long playerId;
                if (!SteamToPlayer.TryGetValue(packet.SenderId, out playerId))
                    Log.Line($"ServerFocusUpdate could not find playerId, but continuing anyway");
                switch (packet.PType)
                {
                    case PacketType.FocusUpdate:
                        if (target != null)
                            ai.Construct.Focus.ServerChangeFocus(target, ai, playerId, ChangeMode.Add); 
                        break;
                    case PacketType.ReleaseActiveUpdate:
                        ai.Construct.Focus.ServerChangeFocus(target, ai, playerId, ChangeMode.Release);
                        break;
                    case PacketType.FocusLockUpdate:
                        ai.Construct.Focus.ServerChangeFocus(target, ai, playerId, ChangeMode.Lock);
                        break;
                }

                data.Report.PacketValid = true;
            }
            else
                return Error(data, Msg($"Ai not found: ai:{ai != null}, is marked:{entity.MarkedForClose}, has root:{EntityToMasterAi.ContainsKey(entity)}"));

            return true;
        }

        private bool ServerForceReload(PacketObj data)
        {
            var packet = data.Packet;
            var ent = MyEntities.GetEntityByIdOrDefault(packet.EntityId);
            var comp = ent?.Components.Get<CoreComponent>() as Weapon.WeaponComponent;

            if (comp?.Ai == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return Error(data, Msg("BaseComp", comp != null), Msg("Ai", comp?.Ai != null), Msg("Ai", comp?.Platform.State == CorePlatform.PlatformState.Ready));

            comp.RequestForceReload();

            data.Report.PacketValid = true;

            return true;
        }


        private bool ServerTerminalMonitor(PacketObj data)
        {
            var packet = data.Packet;
            var terminalMonPacket = (TerminalMonitorPacket)packet;
            var ent = MyEntities.GetEntityByIdOrDefault(packet.EntityId);
            var comp = ent?.Components.Get<CoreComponent>();

            if (comp?.Ai == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return Error(data, Msg("BaseComp", comp != null), Msg("Ai", comp?.Ai != null), Msg("Ai", comp?.Platform.State == CorePlatform.PlatformState.Ready));

            if (terminalMonPacket.State == TerminalMonitorPacket.Change.Update)
                TerminalMon.ServerUpdate(comp);
            else if (terminalMonPacket.State == TerminalMonitorPacket.Change.Clean)
                TerminalMon.ServerClean(comp);

            data.Report.PacketValid = true;

            return true;
        }

        private bool ServerFixedWeaponHitEvent(PacketObj data)
        {
            var packet = data.Packet;
            var hitPacket = (FixedWeaponHitPacket)packet;

            var ent = MyEntities.GetEntityByIdOrDefault(packet.EntityId);
            var comp = ent?.Components.Get<CoreComponent>();

            if (comp?.Ai == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return Error(data, Msg("BaseComp", comp != null), Msg("Ai", comp?.Ai != null), Msg("Ai", comp?.Platform.State == CorePlatform.PlatformState.Ready));

            var collection = comp.TypeSpecific != CoreComponent.CompTypeSpecific.Phantom ? comp.Platform.Weapons : comp.Platform.Phantoms;
            var weapon = collection[hitPacket.WeaponId];
            var targetEnt = MyEntities.GetEntityByIdOrDefault(hitPacket.HitEnt);

            var ammoDef = weapon.System.AmmoTypes[hitPacket.AmmoIndex].AmmoDef;
            var muzzle = weapon.Muzzles[hitPacket.MuzzleId];
            var isBeam = ammoDef.Const.IsBeamWeapon;

            var hitOrigin = targetEnt != null ? targetEnt.PositionComp.WorldMatrixRef.Translation - hitPacket.HitOffset : hitPacket.HitOffset;
            var direction = isBeam ? (Vector3)(hitOrigin - muzzle.Position) : hitPacket.Velocity;
            direction.Normalize();

            Projectiles.NewProjectiles.Add(new NewProjectile
            {
                AmmoDef = ammoDef,
                Muzzle = muzzle,
                TargetEnt = targetEnt,
                Origin = isBeam ? muzzle.Position : hitOrigin,
                OriginUp = hitPacket.Up,
                Direction = direction,
                Velocity = hitPacket.Velocity,
                MaxTrajectory = hitPacket.MaxTrajectory,
                Type = NewProjectile.Kind.Client
            });

            data.Report.PacketValid = true;
            return true;
        }

        private bool ServerClientReady(PacketObj data)
        {
            var packet = data.Packet;
            var readyPacket = (ClientReadyPacket)packet;

            var ent = MyEntities.GetEntityByIdOrDefault(packet.EntityId);
            var comp = ent?.Components.Get<CoreComponent>();

            if (comp?.Ai == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return Error(data, Msg("BaseComp", comp != null), Msg("Ai", comp?.Ai != null), Msg("Ai", comp?.Platform.State == CorePlatform.PlatformState.Ready), Msg("WeaponId", readyPacket.WeaponId >= 0));

            var collection = comp.TypeSpecific != CoreComponent.CompTypeSpecific.Phantom ? comp.Platform.Weapons : comp.Platform.Phantoms;
            var weapon = collection[readyPacket.WeaponId];

            weapon.Reload.WaitForClient = false;
            SendWeaponReload(weapon);

            data.Report.PacketValid = true;

            return true;
        }

        private bool ServerRequestReport(PacketObj data)
        {
            var packet = data.Packet;

            var entity = MyEntities.GetEntityByIdOrDefault(packet.EntityId);
            if (entity == null) return Error(data, Msg("Cube"));

            var reportData = ProblemRep.PullData(entity);
            if (reportData == null) return Error(data, Msg("RequestReport"));

            ProblemRep.NetworkTransfer(false, packet.SenderId, reportData);
            data.Report.PacketValid = true;

            return true;
        }

        private bool ServerBlackList(PacketObj data)
        {
            var packet = data.Packet;
            var readyPacket = (BlackListPacket)packet;

            if (string.IsNullOrEmpty(readyPacket.Data)) return Error(data, Msg("null blacklist string", string.IsNullOrEmpty(readyPacket.Data)));

            long playerId;
            if (SteamToPlayer.TryGetValue(packet.SenderId, out playerId))
            {
                CustomBlackListRequestBecauseKeenIsBrainDead(readyPacket.Data, playerId, readyPacket.Enable);
                data.Report.PacketValid = true;
            }

            return true;
        }

        private bool ServerShootSyncs(PacketObj data)
        {

            var packet = data.Packet;
            var dPacket = (ULongUpdatePacket)packet;
            var ent = MyEntities.GetEntityByIdOrDefault(packet.EntityId);
            var comp = ent?.Components.Get<CoreComponent>();

            if (comp?.Ai == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return Error(data, Msg($"CompId: {packet.EntityId}", comp != null), Msg("Ai", comp?.Ai != null), Msg("Ai", comp?.Platform.State == CorePlatform.PlatformState.Ready));
            var wComp = comp as Weapon.WeaponComponent;

            Weapon.ShootManager.RequestType type;
            Weapon.ShootManager.Signals signal;
            Weapon.ShootManager.ShootCodes code;
            uint interval;

            Weapon.ShootManager.DecodeShootState(dPacket.Data, out type, out signal, out interval, out code);

            if (wComp != null)
            {
                long playerId;
                if (code == Weapon.ShootManager.ShootCodes.ToggleServerOff)
                {
                    wComp.ShootManager.ServerToggleOffByClient(interval);
                }
                else if (SteamToPlayer.TryGetValue(packet.SenderId, out playerId))
                {
                    var success = (wComp.Data.Repo.Values.State.Trigger != CoreComponent.Trigger.On && type != Weapon.ShootManager.RequestType.Off && wComp.ShootManager.RequestShootSync(playerId, type, signal));
                    if (!success || wComp.Data.Repo.Values.State.Trigger == CoreComponent.Trigger.Off)
                    {
                        wComp.ShootManager.ServerRejectResponse(packet.SenderId, type);
                    }
                }
                else
                {
                    Log.Line($"ServerShootSyncs failed: - mode:{signal} - {type}", InputLog);
                }
            }

            data.Report.PacketValid = true;
            return true;
        }
    }
}
