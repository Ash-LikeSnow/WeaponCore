using CoreSystems.Platform;
using CoreSystems.Projectiles;
using CoreSystems.Support;
using Sandbox.Game.Entities;
using VRageMath;
using WeaponCore.Data.Scripts.CoreSystems.Support;
using static CoreSystems.Support.Ai;
// ReSharper disable ForCanBeConvertedToForeach

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
                    errorPacket.MaxAttempts = 512;
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

            if (w.LastAuthoritativeSeqId >= weaponReloadPacket.SequenceId)
            {
                // Out-of-sequence:
                DebugLog.Warning($"ClientWeaponReloadUpdate out-of-sequence packet: {weaponReloadPacket.SequenceId}/{w.LastAuthoritativeSeqId}");
            }
            else
            {
                w.Reload.Sync(w, weaponReloadPacket.Data, false);
                w.LastAuthoritativeSeqId = weaponReloadPacket.SequenceId;
                DebugLog.Debug($"ClientWeaponReloadUpdate: APPLY {weaponReloadPacket.SequenceId}/{w.LastAuthoritativeSeqId}");
            }
            
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

            if (w.LastAuthoritativeSeqId >= ammoPacket.SequenceId)
            {
                // Out-of-sequence:
                DebugLog.Warning($"ClientWeaponAmmoUpdate out-of-sequence packet: {ammoPacket.SequenceId}/{w.LastAuthoritativeSeqId}");
            }
            else
            {
                w.ProtoWeaponAmmo.Sync(w, ammoPacket.Data);
                w.LastAuthoritativeSeqId = ammoPacket.SequenceId;
                DebugLog.Debug($"ClientWeaponReloadUpdate: APPLY {ammoPacket.SequenceId}/{w.LastAuthoritativeSeqId}");
            }
            
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

        private void ClientAdvProjectileSpawnSync(PacketObj data)
        {
            var packet = data.Packet;
            var spawn = (AdvProjectileSpawnPacket)packet;
            
            Weapon w;
            if (!WeaponLookUp.TryGetValue(spawn.WeaponId, out w) || w.Comp?.Ai == null || w.Comp.Platform.State != CorePlatform.PlatformState.Ready)
            {
                Log.Line($"ClientAdvProjectileSpawnSync: weapon {spawn.WeaponId} not found or not ready");
                return;
            }

            if (spawn.AmmoIndex < 0 || spawn.AmmoIndex >= w.System.AmmoTypes.Length)
            {
                Log.Line($"ClientAdvProjectileSpawnSync: ammoIndex {spawn.AmmoIndex} out of range");
                return;
            }

            if (spawn.MuzzleId < 0 || spawn.MuzzleId >= w.Muzzles.Length)
            {
                Log.Line($"ClientAdvProjectileSpawnSync: muzzleId {spawn.MuzzleId} out of range");
                return;
            }

            var ammoType = w.System.AmmoTypes[spawn.AmmoIndex];
            var muzzle = w.Muzzles[spawn.MuzzleId];
            
            Projectiles.NewProjectiles.Add(new NewProjectile
            {
                AmmoDef = ammoType.AmmoDef,
                Muzzle = muzzle,
                TargetEnt = null, // Recreated from the TargetInfo
                Origin = spawn.Position,
                OriginUp = muzzle.UpDirection,
                Direction = spawn.Direction,
                Velocity = spawn.Velocity,
                MaxTrajectory = ammoType.AmmoDef.Const.MaxTrajectory,
                Type = NewProjectile.Kind.AdvSync,
                AdvNetId = spawn.NetId,
                SpawnDepth = spawn.SpawnDepth,
                RandomState = spawn.RandomState,
                AdvTargetInfo = spawn.TargetInfo
            });
        }
        
        private void HandleClientAdvProjectileDeathSync(PacketObj data)
        {
            var packet = data.Packet;
            var death = (AdvProjectileDeathPacket)packet;

            Projectile p;
            if (ProjectilesByNetId.TryGetValue(death.NetId, out p))
            {
                if (p.State == Projectile.ProjectileState.Alive || p.State == Projectile.ProjectileState.ClientPhantom)
                {
                    p.State = Projectile.ProjectileState.Destroy;
                }
            }
            else
            {
                DebugLog.Warning($"ClientAdvProjectileDeathSync: Pro with NetID {death.NetId} not found");
            } 
        }

        private void HandleClientAdvProjectileTargetSync(PacketObj data)
        {
            var packet = (AdvProjectileUpdateTargetPacket)data.Packet;

            Projectile p;
            if (!ProjectilesByNetId.TryGetValue(packet.NetId, out p))
            {
                DebugLog.Warning($"ClientAdvProjectileTargetSync: Pro with NetID {packet.NetId} not found");
                return;
            }

            packet.Info.ApplyTo(p);
        }

        private void HandleClientAdvProjectilePositionSyncBatch(PacketObj data)
        {
            var batch = (AdvProjectilePositionBatchPacket)data.Packet;
            var frameCount = batch.Data.Count;

            for (var i = 0; i < frameCount; i++)
            {
                var frame = batch.Data[i];
                
                HandleClientAdvProjectilePositionSyncFrame(ref frame);
            }
        }

        private void HandleClientAdvProjectilePositionSyncFrame(ref AdvProjectilePositionFrame frame)
        {
            Projectile p;
            if (!ProjectilesByNetId.TryGetValue(frame.NetId, out p))
            {
                DebugLog.Warning($"ClientAdvProjectilePositionSync: Pro with NetId {frame.NetId} not found");
                return;
            }
            
            // Set independent of interpolation:
            p.Info.Storage.RandOffsetDir = frame.RandOffsetDir;
            p.OffsetTarget = frame.OffsetTarget;

            var position = frame.WorldPosition;
            var velocity = (Vector3D)frame.Velocity;
            var lastPosition = position - velocity * StepConst;
            var prevVelocity0 = (Vector3D)frame.PrevVelocity0;
            var prevVelocity1 = (Vector3D)frame.PrevVelocity1;
            var maxSpeed = p.MaxSpeed;
            
            const double imperceptibleFactor = 0.2;
            const double hardSnapFactor = 5.0;

            var torpedoSpeed = p.Velocity.Length();
            var distanceToServerHistory = Vector3D.Distance(p.Position, position);
            var window = p.Info.AmmoDef.Const.PositionPatchWindow;

            if (window <= 0 || distanceToServerHistory > torpedoSpeed * StepConst * hardSnapFactor || distanceToServerHistory < torpedoSpeed * StepConst * imperceptibleFactor)
            {
                // Hard snap. We do this if the difference is acceptably small, or the difference is so big, we fuck off:
                p.Position = position;
                p.LastPosition = lastPosition;
                p.Velocity = velocity;
                p.PrevVelocity0 = prevVelocity0;
                p.PrevVelocity1 = prevVelocity1;

                if (!Vector3D.IsZero(velocity))
                {
                    Vector3D.Normalize(ref velocity, out p.Direction);
                }

                Vector3D.Dot(ref p.Velocity, ref p.Velocity, out p.VelocityLengthSqr);
                p.TravelMagnitude = p.Velocity * StepConst;
            }
            else
            {
                // We run some interpolation:
                
                LimitlessPdAdvProjectilePositionSyncExtrapolate(
                    window,
                    ref position, ref lastPosition,
                    ref velocity,
                    ref prevVelocity1,
                    ref prevVelocity0,
                    maxSpeed
                );

                p.Info.AdvSyncInterpolator = new AdvSyncProjectileInterpolator
                {
                    IsSet = true,
                    Window = window,
                    Ticks = 0,
                    
                    InitialPosition = p.Position, 
                    InitialVelocity = p.Velocity,
                    FinalPosition = position,
                    FinalLastPosition =  lastPosition,
                    FinalVelocity =  velocity,
                    FinalPrevVelocity0 = prevVelocity0,
                    FinalPrevVelocity1 =  prevVelocity1,
                    VelPrev0 = p.PrevVelocity1,
                    VelPrev1 = p.Velocity
                };
            }
        }
        
        /// <summary>
        ///     Extrapolates the projectile forward in time, so it more closely matches up with the position on the server.
        ///     I don't know if the grids themselves are timed similarly to this, but BD wants extrapolation so here we go.
        /// </summary>
        /// <param name="extrapolateTicks"></param>
        /// <param name="position"></param>
        /// <param name="lastPosition"></param>
        /// <param name="velocity"></param>
        /// <param name="previousVelocity1"></param>
        /// <param name="previousVelocity0"></param>
        /// <param name="maxSpeed"></param>
        private static void LimitlessPdAdvProjectilePositionSyncExtrapolate(
            double extrapolateTicks,
            ref Vector3D position, ref Vector3D lastPosition,
            ref Vector3D velocity,
            ref Vector3D previousVelocity1,
            ref Vector3D previousVelocity0,
            double maxSpeed
            )
        {
            var fullSteps = (int)extrapolateTicks;
            var remainder = extrapolateTicks - fullSteps;
            var maxSpeedSqr = maxSpeed * maxSpeed;

            var targetAccel0 = (previousVelocity1 - previousVelocity0) / StepConst;
            var targetAccel1 = (velocity - previousVelocity1) / StepConst;

            var targetAccel0N = targetAccel0.Length();
            var targetAccel1N = targetAccel1.Length();

            if (targetAccel0N < 1.0 || targetAccel1N < 1.0)
            {
                goto fallback;
            }

            var e0 = targetAccel0 / targetAccel0N;
            var e1 = targetAccel1 / targetAccel1N;

            var w = Vector3D.Cross(e0, e1);
            var sinTheta = MathHelperD.Clamp(w.Length(), 0.0, 1.0);

            if (sinTheta < 1e-6)
            {
                goto fallback;
            }

            w /= sinTheta;

            var cosTheta = MathHelperD.Clamp(Vector3D.Dot(e0, e1), -1.0, 1.0);
            var targetAccelWorld = targetAccel1;

            // Only the integration loop taken from limitless PD:
            for (var step = 0; step < fullSteps; step++)
            {
                previousVelocity0 = previousVelocity1;
                previousVelocity1 = velocity;
                lastPosition = position;

                velocity += targetAccelWorld * StepConst;
           
                if (velocity.LengthSquared() > maxSpeedSqr)
                {
                    velocity = velocity.Normalized() * maxSpeed;
                }
                
                position += velocity * StepConst;

                var r1 = targetAccelWorld * cosTheta;
                var r2 = Vector3D.Cross(w, targetAccelWorld) * sinTheta;
                var r3 = w * (Vector3D.Dot(w, targetAccelWorld) * (1.0 - cosTheta));
                
                targetAccelWorld = r1 + r2 + r3;
            }

            if (remainder > 1e-6)
            {
                var frac = remainder * StepConst;
                previousVelocity0 = previousVelocity1;
                previousVelocity1 = velocity;
                lastPosition = position;

                velocity += targetAccelWorld * frac;
                if (velocity.LengthSquared() > maxSpeedSqr)
                {
                    velocity = velocity.Normalized() * maxSpeed;
                }
                
                position += velocity * frac;
            }

            return;

            fallback:
            for (var step = 0; step < fullSteps; step++)
            {
                previousVelocity0 = previousVelocity1;
                previousVelocity1 = velocity;
                lastPosition = position;
                position += velocity * StepConst;
                
                if (velocity.LengthSquared() > maxSpeedSqr)
                {
                    velocity = velocity.Normalized() * maxSpeed;
                }
            }

            if (remainder > 1e-6)
            {
                var frac = remainder * StepConst;
                previousVelocity0 = previousVelocity1;
                previousVelocity1 = velocity;
                lastPosition = position;
                position += velocity * frac;
                
                if (velocity.LengthSquared() > maxSpeedSqr)
                {
                    velocity = velocity.Normalized() * maxSpeed;
                }
            }
        }
    }
}
