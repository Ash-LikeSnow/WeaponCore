using System.Collections.Generic;
using CoreSystems.Platform;
using CoreSystems.Projectiles;
using CoreSystems.Support;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.Entity;
using static CoreSystems.Session;
using static CoreSystems.Support.Ai;
namespace CoreSystems
{
    public partial class Session
    {
        public void CleanClientPackets()
        {
            foreach (var packet in ClientPacketsToClean)
                PacketObjPool.Return(packet);

            ClientPacketsToClean.Clear();
        }

        public void ReproccessClientErrorPackets()
        {
            foreach (var packetObj in ClientSideErrorPkt)
            {
                var errorPacket = packetObj.ErrorPacket;
                var packet = packetObj.Packet;

                if (errorPacket.MaxAttempts == 0)  {
                    Log.LineShortDate($"        [ClientReprocessing] Entity:{packet.EntityId} - Type:{packet.PType}", "net");
                    //set packet retry variables, based on type
                    errorPacket.MaxAttempts = 7;
                    errorPacket.RetryDelayTicks = 15;
                    errorPacket.RetryTick = Tick + errorPacket.RetryDelayTicks;
                }

                if (errorPacket.RetryTick > Tick) continue;
                errorPacket.RetryAttempt++;
                var success = ProccessClientPacket(packetObj, false) || packetObj.Report.PacketValid;

                if (success || errorPacket.RetryAttempt > errorPacket.MaxAttempts)  {

                    if (!success)  
                        Log.LineShortDate($"        [BadReprocess] Entity:{packet.EntityId} Cause:{errorPacket.Error ?? string.Empty} Type:{packet.PType}", "net");
                    else Log.LineShortDate($"        [ReprocessSuccess] Entity:{packet.EntityId} - Type:{packet.PType} - Retries:{errorPacket.RetryAttempt}", "net");

                    ClientSideErrorPkt.Remove(packetObj);
                    ClientPacketsToClean.Add(packetObj);
                }
                else
                    errorPacket.RetryTick = Tick + errorPacket.RetryDelayTicks;
            }
            ClientSideErrorPkt.ApplyChanges();
        }

        private bool ClientConstruct(PacketObj data)
        {
            var packet = data.Packet;
            var entity = MyEntities.GetEntityByIdOrDefault(packet.EntityId);
            var cgPacket = (ConstructPacket)packet;
            if (entity == null) return Error(data, Msg($"Entity: {packet.EntityId}"));

            Ai ai;
            if (EntityToMasterAi.TryGetValue(entity, out ai)) {
                var rootConstruct = ai.Construct.RootAi.Construct;

                rootConstruct.Data.Repo.Sync(rootConstruct, cgPacket.Data);
                rootConstruct.UpdateLeafs();

                data.Report.PacketValid = true;
            }
            else
                return Error(data, Msg($"Ai not found, is marked:{entity.MarkedForClose}, has root:{EntityToMasterAi.ContainsKey(entity)}"));

            return true;
        }

        private bool ClientConstructFoci(PacketObj data)
        {
            var packet = data.Packet;
            var entity = MyEntities.GetEntityByIdOrDefault(packet.EntityId);
            var fociPacket = (ConstructFociPacket)packet;
            if (entity == null) return Error(data, Msg($"Entity: {packet.EntityId}"));

            Ai ai;
            if (EntityToMasterAi.TryGetValue(entity, out ai))
            {
                var rootConstruct = ai.Construct.RootAi.Construct;
                rootConstruct.Data.Repo.FocusData.Sync(ai, fociPacket.Data);

                data.Report.PacketValid = true;
            }
            else
                return Error(data, Msg($"entity not found, is marked:{entity.MarkedForClose}, has root:{EntityToMasterAi.ContainsKey(entity)}"));

            return true;
        }

        private bool ClientAiDataUpdate(PacketObj data)
        {
            var packet = data.Packet;
            var entity = MyEntities.GetEntityByIdOrDefault(packet.EntityId);
            var aiSyncPacket = (AiDataPacket)packet;
            if (entity == null) return Error(data, Msg($"Entity: {packet.EntityId}"));

            Ai ai;
            if (EntityAIs.TryGetValue(entity, out ai)) {

                ai.Data.Repo.Sync(aiSyncPacket.Data);

                data.Report.PacketValid = true;
            }
            else
                return Error(data, Msg($"Ai not found, is marked:{entity.MarkedForClose}, has root:{EntityToMasterAi.ContainsKey(entity)}"));

            return true;
        }

        private bool ClientWeaponComp(PacketObj data)
        {
            var packet = data.Packet;
            var compDataPacket = (WeaponCompPacket)packet;
            var ent = MyEntities.GetEntityByIdOrDefault(packet.EntityId);
            var comp = ent?.Components.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp?.Ai == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return Error(data, Msg($"CompId: {packet.EntityId}", comp != null), Msg("Ai", comp?.Ai != null), Msg("Ai", comp?.Platform.State == CorePlatform.PlatformState.Ready));

            comp.Data.Repo.Values.Sync(comp, compDataPacket.Data);

            if (comp.IsBomb) comp.Cube.UpdateTerminal();
            data.Report.PacketValid = true;

            return true;
        }

        private bool ClientWeaponState(PacketObj data)
        {
            var packet = data.Packet;
            var compStatePacket = (WeaponStatePacket)packet;
            var ent = MyEntities.GetEntityByIdOrDefault(packet.EntityId);
            var comp = ent?.Components.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp?.Ai == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return Error(data, Msg($"CompId: {packet.EntityId}", comp != null), Msg("Ai", comp?.Ai != null), Msg("Ai", comp?.Platform.State == CorePlatform.PlatformState.Ready));

            comp.Data.Repo.Values.State.Sync(comp, compStatePacket.Data);

            if (comp.IsBomb) comp.Cube.UpdateTerminal();
            data.Report.PacketValid = true;

            return true;
        }

        private bool ClientUpgradeComp(PacketObj data)
        {
            var packet = data.Packet;
            var compDataPacket = (UpgradeCompPacket)packet;
            var ent = MyEntities.GetEntityByIdOrDefault(packet.EntityId);
            var comp = ent?.Components.Get<CoreComponent>() as Upgrade.UpgradeComponent;
            if (comp?.Ai == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return Error(data, Msg($"CompId: {packet.EntityId}", comp != null), Msg("Ai", comp?.Ai != null), Msg("Ai", comp?.Platform.State == CorePlatform.PlatformState.Ready));

            if (!comp.Data.Repo.Values.Sync(comp, compDataPacket.Data))
                Log.Line($"ClientUpgradeComp: version fail - senderId:{packet.SenderId} - version:{comp.Data.Repo.Values.Revision}({compDataPacket.Data.Revision})");

            data.Report.PacketValid = true;

            return true;
        }

        private bool ClientUpgradeState(PacketObj data)
        {
            var packet = data.Packet;
            var compStatePacket = (UpgradeStatePacket)packet;
            var ent = MyEntities.GetEntityByIdOrDefault(packet.EntityId);
            var comp = ent?.Components.Get<CoreComponent>() as Upgrade.UpgradeComponent;
            if (comp?.Ai == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return Error(data, Msg($"CompId: {packet.EntityId}", comp != null), Msg("Ai", comp?.Ai != null), Msg("Ai", comp?.Platform.State == CorePlatform.PlatformState.Ready));

            if (!comp.Data.Repo.Values.State.Sync(comp, compStatePacket.Data, ProtoUpgradeState.Caller.Direct))
                Log.Line($"ClientUpgradeState: version fail - senderId:{packet.SenderId} - version:{comp.Data.Repo.Values.Revision}({compStatePacket.Data.Revision})");

            data.Report.PacketValid = true;

            return true;
        }

        private bool ClientSupportComp(PacketObj data)
        {
            var packet = data.Packet;
            var compDataPacket = (SupportCompPacket)packet;
            var ent = MyEntities.GetEntityByIdOrDefault(packet.EntityId);
            var comp = ent?.Components.Get<CoreComponent>() as SupportSys.SupportComponent;
            if (comp?.Ai == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return Error(data, Msg($"CompId: {packet.EntityId}", comp != null), Msg("Ai", comp?.Ai != null), Msg("Ai", comp?.Platform.State == CorePlatform.PlatformState.Ready));

            if (!comp.Data.Repo.Values.Sync(comp, compDataPacket.Data))
                Log.Line($"ClientSupportComp: version fail - senderId:{packet.SenderId} - version:{comp.Data.Repo.Values.Revision}({compDataPacket.Data.Revision})");

            data.Report.PacketValid = true;

            return true;
        }

        private bool ClientSupportState(PacketObj data)
        {
            var packet = data.Packet;
            var compStatePacket = (SupportStatePacket)packet;
            var ent = MyEntities.GetEntityByIdOrDefault(packet.EntityId);
            var comp = ent?.Components.Get<CoreComponent>() as SupportSys.SupportComponent;
            if (comp?.Ai == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return Error(data, Msg($"CompId: {packet.EntityId}", comp != null), Msg("Ai", comp?.Ai != null), Msg("Ai", comp?.Platform.State == CorePlatform.PlatformState.Ready));

            if (!comp.Data.Repo.Values.State.Sync(comp, compStatePacket.Data, ProtoSupportState.Caller.Direct))
                Log.Line($"ClientSupportState: version fail - senderId:{packet.SenderId} - version:{comp.Data.Repo.Values.Revision}({compStatePacket.Data.Revision})");

            data.Report.PacketValid = true;

            return true;
        }

        private bool ClientControlComp(PacketObj data)
        {
            var packet = data.Packet;
            var compDataPacket = (ControlCompPacket)packet;
            var ent = MyEntities.GetEntityByIdOrDefault(packet.EntityId);
            var comp = ent?.Components.Get<CoreComponent>() as ControlSys.ControlComponent;
            if (comp?.Ai == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return Error(data, Msg($"CompId: {packet.EntityId}", comp != null), Msg("Ai", comp?.Ai != null), Msg("Ai", comp?.Platform.State == CorePlatform.PlatformState.Ready));

            if (!comp.Data.Repo.Values.Sync(comp, compDataPacket.Data))
                Log.Line($"ClientSupportComp: version fail - senderId:{packet.SenderId} - version:{comp.Data.Repo.Values.Revision}({compDataPacket.Data.Revision})");

            data.Report.PacketValid = true;

            return true;
        }

        private bool ClientControlOnOff(PacketObj data)
        {
            var packet = data.Packet;
            var boolPacket = (BoolUpdatePacket)packet;
            var ent = MyEntities.GetEntityByIdOrDefault(packet.EntityId);
            var comp = ent?.Components.Get<CoreComponent>() as ControlSys.ControlComponent;
            if (comp?.Ai == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return Error(data, Msg($"CompId: {packet.EntityId}", comp != null), Msg("Ai", comp?.Ai != null), Msg("Ai", comp?.Platform.State == CorePlatform.PlatformState.Ready));
            
            comp.Data.Repo.Values.State.Terminal = boolPacket.Data ? CoreComponent.Trigger.On : CoreComponent.Trigger.Off; 
            SendState(comp);

            data.Report.PacketValid = true;

            return true;
        }


        private bool ClientControlState(PacketObj data)
        {
            var packet = data.Packet;
            var compStatePacket = (ControlStatePacket)packet;
            var ent = MyEntities.GetEntityByIdOrDefault(packet.EntityId);
            var comp = ent?.Components.Get<CoreComponent>() as ControlSys.ControlComponent;
            if (comp?.Ai == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return Error(data, Msg($"CompId: {packet.EntityId}", comp != null), Msg("Ai", comp?.Ai != null), Msg("Ai", comp?.Platform.State == CorePlatform.PlatformState.Ready));

            if (!comp.Data.Repo.Values.State.Sync(comp, compStatePacket.Data, ProtoControlState.Caller.Direct))
                Log.Line($"ClientSupportState: version fail - senderId:{packet.SenderId} - version:{comp.Data.Repo.Values.Revision}({compStatePacket.Data.Revision})");

            data.Report.PacketValid = true;

            return true;
        }

        private bool ClientWeaponReloadUpdate(PacketObj data)
        {
            var packet = data.Packet;
            var weaponReloadPacket = (WeaponReloadPacket)packet;
            var ent = MyEntities.GetEntityByIdOrDefault(packet.EntityId);
            var comp = ent?.Components.Get<CoreComponent>();
            if (comp?.Ai == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return Error(data, Msg($"CompId: {packet.EntityId}", comp != null), Msg("Ai", comp?.Ai != null), Msg("Ai", comp?.Platform.State == CorePlatform.PlatformState.Ready));

            var collection = comp.TypeSpecific != CoreComponent.CompTypeSpecific.Phantom ? comp.Platform.Weapons : comp.Platform.Phantoms;
            var w = collection[weaponReloadPacket.PartId];
            w.Reload.Sync(w, weaponReloadPacket.Data, false);

            data.Report.PacketValid = true;

            return true;
        }

        private bool ClientTargetUpdate(PacketObj data)
        {
            var packet = data.Packet;
            var targetPacket = (TargetPacket)packet;
            var ent = MyEntities.GetEntityByIdOrDefault(packet.EntityId);
            var comp = ent?.Components.Get<CoreComponent>();

            if (comp?.Ai == null || comp.Platform.State != CorePlatform.PlatformState.Ready ) return Error(data, Msg($"CompId: {packet.EntityId}", comp != null), Msg("Ai", comp?.Ai != null), Msg("Ai", comp?.Platform.State == CorePlatform.PlatformState.Ready));
            var collection = comp.TypeSpecific != CoreComponent.CompTypeSpecific.Phantom ? comp.Platform.Weapons : comp.Platform.Phantoms;
            var w = collection[targetPacket.Target.PartId];
            targetPacket.Target.SyncTarget(w);

            data.Report.PacketValid = true;

            return true;
        }

        private bool ClientWeaponAmmoUpdate(PacketObj data)
        {
            var packet = data.Packet;
            var ammoPacket = (WeaponAmmoPacket)packet;
            var ent = MyEntities.GetEntityByIdOrDefault(packet.EntityId);
            var comp = ent?.Components.Get<CoreComponent>();

            if (comp?.Ai == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return Error(data, Msg($"CompId: {packet.EntityId}", comp != null), Msg("Ai", comp?.Ai != null), Msg("Ai", comp?.Platform.State == CorePlatform.PlatformState.Ready));
            var collection = comp.TypeSpecific != CoreComponent.CompTypeSpecific.Phantom ? comp.Platform.Weapons : comp.Platform.Phantoms;
            var w = collection[ammoPacket.PartId];
            w.ProtoWeaponAmmo.Sync(w, ammoPacket.Data);

            data.Report.PacketValid = true;

            return true;
        }

        private bool ClientFakeTargetUpdate(PacketObj data)
        {
            var packet = data.Packet;
            data.ErrorPacket.NoReprocess = true;
            var targetPacket = (FakeTargetPacket)packet;
            var entity = MyEntities.GetEntityByIdOrDefault(packet.EntityId);

            Ai ai;
            if (entity != null && EntityAIs.TryGetValue(entity, out ai)) {

                long playerId;
                if (SteamToPlayer.TryGetValue(packet.SenderId, out playerId)) {

                    FakeTargets dummyTargets;
                    if (PlayerDummyTargets.TryGetValue(playerId, out dummyTargets)) {
                        dummyTargets.ManualTarget.Sync(targetPacket, ai);
                    }
                    else
                        return Error(data, Msg("Player dummy target not found"));
                }
                else
                    return Error(data, Msg("SteamToPlayer missing Player"));

                data.Report.PacketValid = true;
            }
            else
                return Error(data, Msg($"EntityId: {packet.EntityId}", entity != null), Msg("Ai"));

            return true;
        }

        private bool ClientPaintedTargetUpdate(PacketObj data)
        {
            var packet = data.Packet;
            data.ErrorPacket.NoReprocess = true;
            var targetPacket = (PaintedTargetPacket)packet;
            var entity = MyEntities.GetEntityByIdOrDefault(packet.EntityId);

            Ai ai;
            if (entity != null && EntityAIs.TryGetValue(entity, out ai))
            {
                long playerId;
                if (SteamToPlayer.TryGetValue(packet.SenderId, out playerId))
                {
                    FakeTargets dummyTargets;
                    if (PlayerDummyTargets.TryGetValue(playerId, out dummyTargets))
                    {
                        dummyTargets.PaintedTarget.Sync(targetPacket, ai);
                    }
                    else
                        return Error(data, Msg("Player dummy target not found"));
                }
                else
                    return Error(data, Msg("SteamToPlayer missing Player"));

                data.Report.PacketValid = true;
            }
            else
                return Error(data, Msg($"EntityId: {packet.EntityId}", entity != null), Msg("Ai"));

            return true;
        }
        // no storge sync


        private bool ClientPlayerIdUpdate(PacketObj data)
        {
            var packet = data.Packet;
            var updatePacket = (BoolUpdatePacket)packet;

            if (updatePacket.Data)
                PlayerConnected(updatePacket.EntityId);
            else //remove
                PlayerDisconnected(updatePacket.EntityId);

            data.Report.PacketValid = true;
            return true;
        }

        private bool ClientServerData(PacketObj data)
        {
            var packet = data.Packet;
            var updatePacket = (ServerPacket)packet;

            ServerVersion = updatePacket.VersionString;
            Settings.VersionControl.UpdateClientEnforcements(updatePacket.Data);
            data.Report.PacketValid = true;
            Log.Line("Server enforcement received");

            UpdateEnforcement();

            return true;
        }

        private bool ClientNotify(PacketObj data)
        {
            var packet = data.Packet;
            var clientNotifyPacket = (ClientNotifyPacket)packet;

            if (clientNotifyPacket.Message == string.Empty || clientNotifyPacket.Color == string.Empty) return Error(data, Msg("BaseData"));

            ShowClientNotify(clientNotifyPacket);
            data.Report.PacketValid = true;

            return true;
        }

        private bool ClientEwarBlocks(PacketObj data)
        {
            var packet = data.Packet;
            var queueShot = (EwaredBlocksPacket)packet;
            if (queueShot?.Data == null)
            {
                return false;
            }

            CurrentClientEwaredCubes.Clear();

            for (int i = 0; i < queueShot.Data.Count; i++)
            {
                var values = queueShot.Data[i];
                CurrentClientEwaredCubes[values.EwaredBlockId] = values;
            }
            ClientEwarStale = true;

            data.Report.PacketValid = true;

            return true;
        }

        // Unmanaged state changes below this point
        private bool ClientSentReport(PacketObj data)
        {
            var packet = data.Packet;
            var sentReportPacket = (ProblemReportPacket)packet;
            if (sentReportPacket.Data == null) return Error(data, Msg("SentReport"));
            Log.Line("remote data received");
            ProblemRep.RemoteData = sentReportPacket.Data;
            data.Report.PacketValid = true;

            return true;

        }

        private bool ClientHandDebug(PacketObj data)
        {
            var packet = data.Packet;
            var debugPacket = (HandWeaponDebugPacket)packet;
            if (debugPacket == null) return Error(data, Msg("HandDebug"));
            {
                DrawHandDebug(debugPacket);
            }
            data.Report.PacketValid = true;
            return true;
        }

        private bool ClientProjectilePosSyncs(PacketObj data)
        {
            var packet = data.Packet;
            var proPacket = (ProjectileSyncPositionPacket)packet;
            if (proPacket.Data == null) return Error(data, Msg("ProSyncData"));

            for (int i = 0; i < proPacket.Data.Count; i++)
            {
                var syncPacket = proPacket.Data[i];
                Weapon w;
                if (WeaponLookUp.TryGetValue(syncPacket.WeaponSyncId, out w))
                {
                    if (w.Comp?.Ai == null || w.Comp.Platform.State != CorePlatform.PlatformState.Ready)
                        continue;

                    for (int j = 0; j < syncPacket.Collection.Count; j++)
                    {
                        var sync = syncPacket.Collection[j];
                        ClientProSync oldSync;
                        w.WeaponProSyncs.TryGetValue(sync.ProId, out oldSync);
                        w.WeaponProSyncs[sync.ProId] = new ClientProSync { ProPosition = sync, UpdateTick = (float) RelativeTime, CurrentOwl = proPacket.CurrentOwl };
                    }
                }
                else 
                    Log.Line($"ClientProjectilePosSyncs failed");
            }
            
            data.Report.PacketValid = true;

            proPacket.CleanUp();
            return true;
        }

        private bool ClientProjectileTargetSyncs(PacketObj data)
        {
            var packet = data.Packet;
            var proPacket = (ProjectileSyncTargetPacket)packet;
            if (proPacket.Data == null) return Error(data, Msg("ProSyncData"));

            for (int i = 0; i < proPacket.Data.Count; i++)
            {
                var syncPacket = proPacket.Data[i];
                Weapon w;
                if (WeaponLookUp.TryGetValue(syncPacket.WeaponSyncId, out w))
                {
                    if (w.Comp?.Ai == null || w.Comp.Platform.State != CorePlatform.PlatformState.Ready)
                        continue;

                    for (int j = 0; j < syncPacket.Collection.Count; j++)
                    {
                        var sync = syncPacket.Collection[j];
                        Projectile p;
                        MyEntity target;
                        if (w.ProjectileSyncMonitor.TryGetValue(sync.ProId, out p) && p.State == Projectile.ProjectileState.Alive && MyEntities.TryGetEntityById(sync.EntityId, out target) && target != p.Info.Target.TargetObject)
                        {
                            var topEntId = target.GetTopMostParent().EntityId;
                            var targetPos = target.PositionComp.WorldAABB.Center;
                            p.Info.Target.Set(target, targetPos, 0, 0, topEntId);
                        }
                    }
                }
                else
                    Log.Line($"ClientProjectileTargetSyncs failed");
            }

            data.Report.PacketValid = true;

            proPacket.CleanUp();
            return true;
        }

        private bool ClientShootSyncs(PacketObj data)
        {
            var packet = data.Packet;
            var dPacket = (ULongUpdatePacket)packet;
            var ent = MyEntities.GetEntityByIdOrDefault(packet.EntityId);
            var comp = ent?.Components.Get<CoreComponent>();

            if (comp?.Ai == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return Error(data, Msg($"CompId: {packet.EntityId}", comp != null), Msg("Ai", comp?.Ai != null), Msg("Ai", comp?.Platform.State == CorePlatform.PlatformState.Ready));

            Weapon.ShootManager.RequestType type;
            Weapon.ShootManager.Signals signal;
            Weapon.ShootManager.ShootCodes code;
            uint interval;

            Weapon.ShootManager.DecodeShootState(dPacket.Data, out type, out signal, out interval, out code);

            var wComp = comp as Weapon.WeaponComponent;
            if (wComp != null)
            {
                switch (code)
                {
                    case Weapon.ShootManager.ShootCodes.ClientRequestReject:
                        wComp.ShootManager.ReceivedServerReject();
                        break;
                    case Weapon.ShootManager.ShootCodes.ToggleClientOff:
                        wComp.ShootManager.ClientToggledOffByServer(interval);
                        break;
                    case Weapon.ShootManager.ShootCodes.ServerResponse:
                        wComp.ShootManager.WaitingShootResponse = false;
                        break;
                    case Weapon.ShootManager.ShootCodes.ToggleServerOff:
                        wComp.ShootManager.ClientToggledOffByServer(interval, true);
                        break;
                    default:
                        long playerId;
                        SteamToPlayer.TryGetValue(packet.SenderId, out playerId);
                        wComp.ShootManager.RequestShootSync(0, type, signal);
                        break;
                }
            }

            data.Report.PacketValid = true;
            return true;
        }
    }
}
