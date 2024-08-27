using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CoreSystems.Platform;
using CoreSystems.Projectiles;
using CoreSystems.Support;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;

namespace CoreSystems
{
    public partial class Session
    {
        private void StringReceived(byte[] rawData)
        {
            try
            {
                var message = Encoding.UTF8.GetString(rawData, 0, rawData.Length); 
                if (string.IsNullOrEmpty(message)) return;
                var firstChar = message[0];
                int logId;
                if (!int.TryParse(firstChar.ToString(), out logId))
                    return;
                message = message.Substring(1);

                switch (logId) {
                    case 0: {
                        Log.LineShortDate(message);
                        break;
                    }
                    case 1: {
                        Log.LineShortDate(message, "perf");
                        break;
                    }
                    case 2: {
                        Log.LineShortDate(message, "stats");
                        break;
                    }
                    case 3: { 
                        Log.LineShortDate(message, "net");
                        break;
                    }
                    case 4: {
                        Log.LineShortDate(message);
                        break;
                    }
                    default:
                        Log.LineShortDate(message);
                        break;
                }
            }
            catch (Exception ex) { Log.Line($"Exception in StringReceivedPacket: {ex}"); }
        }

        #region NewClientSwitch

        private void ClientReceivedPacket(byte[] rawData)
        {
            try
            {
                var packet = MyAPIGateway.Utilities.SerializeFromBinary<Packet>(rawData);
                if (packet == null)
                {
                    Log.Line("ClientReceivedPacket null packet");
                    return;
                }

                if (packet.PType == PacketType.PingPong)
                {
                    PingPong(((PingPacket)packet).RelativeTime);
                    return;
                }
                var packetSize = rawData.Length;
                var report = Reporter.ReportPool.Get();
                report.Receiver = NetworkReporter.Report.Received.Client;
                report.PacketSize = packetSize;
                Reporter.ReportData[packet.PType].Add(report);
                var packetObj = PacketObjPool.Get();
                packetObj.ErrorPacket.RecievedTick = Tick;
                packetObj.Packet = packet; packetObj.PacketSize = packetSize; packetObj.Report = report;
                ProccessClientPacket(packetObj);
            }
            catch (Exception ex) { Log.Line($"Exception in ClientReceivedPacket: {ex}", null, true); }
        }

        private bool ProccessClientPacket(PacketObj packetObj, bool firstRun = true)
        {
            try {
                var invalidType = false;
                switch (packetObj.Packet.PType) {

                    case PacketType.ShootSync:
                    {
                        ClientShootSyncs(packetObj);
                        break;
                    }
                    case PacketType.ProjectilePosSyncs:
                    {
                        ClientProjectilePosSyncs(packetObj);
                        break;
                    }
                    case PacketType.ProjectileTargetSyncs:
                    {
                        ClientProjectileTargetSyncs(packetObj);
                        break;
                    }
                    case PacketType.HandWeaponDebug:
                    {
                        ClientHandDebug(packetObj);
                        break;
                    }
                    case PacketType.AimTargetUpdate: 
                    {
                            ClientFakeTargetUpdate(packetObj);
                            break;
                    }
                    case PacketType.PaintedTargetUpdate:
                    {
                            ClientPaintedTargetUpdate(packetObj);
                            break;
                    }
                    case PacketType.PlayerIdUpdate: 
                    {
                            ClientPlayerIdUpdate(packetObj); 
                            break;
                    }
                    case PacketType.ServerData:
                    {
                        ClientServerData(packetObj);
                        break;
                    }
                    case PacketType.Construct:
                    {
                        ClientConstruct(packetObj);
                        break;
                    }
                    case PacketType.ConstructFoci:
                    {
                        ClientConstructFoci(packetObj);
                        break;
                    }
                    case PacketType.AiData: 
                    {
                        ClientAiDataUpdate(packetObj);
                        break;
                    }
                    case PacketType.WeaponComp:
                    {
                        ClientWeaponComp(packetObj);
                        break;
                    }
                    case PacketType.WeaponState:
                    {
                        ClientWeaponState(packetObj);
                        break;
                    }
                    case PacketType.UpgradeComp:
                    {
                        ClientUpgradeComp(packetObj);
                        break;
                    }
                    case PacketType.UpgradeState:
                    {
                        ClientUpgradeState(packetObj);
                        break;
                    }
                    case PacketType.SupportComp:
                    {
                        ClientSupportComp(packetObj);
                        break;
                    }
                    case PacketType.SupportState:
                    {
                        ClientSupportState(packetObj);
                        break;
                    }
                    case PacketType.ControlComp:
                    {
                        ClientControlComp(packetObj);
                        break;
                    }
                    case PacketType.ControlOnOff:
                    {
                        ClientControlOnOff(packetObj);
                        break;
                    }
                    case PacketType.ControlState:
                    {
                        ClientControlState(packetObj);
                        break;
                    }
                    case PacketType.WeaponReload:
                    {
                        ClientWeaponReloadUpdate(packetObj);
                        break;
                    }
                    case PacketType.WeaponAmmo:
                    {
                        ClientWeaponAmmoUpdate(packetObj);
                        break;
                    }
                    case PacketType.TargetChange:
                    {
                        ClientTargetUpdate(packetObj);
                        break;
                    }
                    case PacketType.ProblemReport: 
                    {
                        ClientSentReport(packetObj);
                        break;
                    }
                    case PacketType.ClientNotify:
                    {
                        ClientNotify(packetObj);
                        break;
                    }
                    case PacketType.EwaredBlocks:
                    {
                        ClientEwarBlocks(packetObj);
                        break;
                    }
                    case PacketType.Invalid:
                    {
                        Log.Line($"invalid packet: {packetObj.PacketSize} - {packetObj.Packet.PType}");
                        invalidType = true;
                        packetObj.Report.PacketValid = false;
                        break;
                    }
                    default:
                        Log.LineShortDate($"        [BadClientPacket] Type:{packetObj.Packet.PType} - Size:{packetObj.PacketSize}", "net");
                        Reporter.ReportData[PacketType.Invalid].Add(packetObj.Report);
                        invalidType = true;
                        packetObj.Report.PacketValid = false;
                        break;
                }
                if (firstRun && !packetObj.Report.PacketValid && !invalidType && !packetObj.ErrorPacket.Retry && !packetObj.ErrorPacket.NoReprocess)
                {
                    if (!ClientSideErrorPkt.Contains(packetObj))
                        ClientSideErrorPkt.Add(packetObj);
                    else
                        Log.Line($"ClientSideErrorPkt: this should be impossible: {packetObj.Packet.PType}");
                }

                if (firstRun)  {

                    ClientSideErrorPkt.ApplyChanges();
                    
                    if (!ClientSideErrorPkt.Contains(packetObj))  {
                        ClientPacketsToClean.Add(packetObj);
                        return true;
                    }
                }
            }
            catch (Exception ex) { Log.Line($"Exception in ProccessClientPacket: {ex} - packetSize:{packetObj?.PacketSize} - pObjNull:{packetObj == null} - packetNull:{packetObj?.Packet == null} - error:{packetObj?.ErrorPacket == null} - report:{packetObj?.Report == null}"); }
            return false;
        }
        #endregion

        #region NewServerSwitch
        internal void ProccessClientPacketsForServer()
        {
            if (!IsClient || !MpActive)
            {
                Log.Line("trying to process client packets on a non-client");
                PacketsToServer.Clear();
                return;
            }

            for (int i = 0; i < PacketsToServer.Count; i++)
                MyModAPIHelper.MyMultiplayer.Static.SendMessageToServer(ServerPacketId, MyAPIGateway.Utilities.SerializeToBinary(PacketsToServer[i]), true);

            PacketsToServer.Clear();
        }

        private void ProccessServerPacket(byte[] rawData)
        {
            var packet = MyAPIGateway.Utilities.SerializeFromBinary<Packet>(rawData);
            if (packet == null) return;

            if (packet.PType == PacketType.PingPong)
            {
                RecordClientLatency((PingPacket)packet);
                return;
            }

            var packetSize = rawData.Length;

            var report = Reporter.ReportPool.Get();
            report.Receiver = NetworkReporter.Report.Received.Server;
            report.PacketSize = packetSize;
            Reporter.ReportData[packet.PType].Add(report);

            var packetObj = PacketObjPool.Get();
            packetObj.ErrorPacket.RecievedTick = Tick;
            packetObj.Packet = packet; packetObj.PacketSize = packetSize; packetObj.Report = report;

            switch (packetObj.Packet.PType) {

                case PacketType.ForceReload:
                {
                    ServerForceReload(packetObj);
                    break;
                }
                case PacketType.BlackListRequest:
                {
                    ServerBlackList(packetObj);
                    break;
                }
                case PacketType.ShootSync:
                {
                    ServerShootSyncs(packetObj);
                    break;
                }
                case PacketType.ActiveControlUpdate: {
                    ServerActiveControlUpdate(packetObj);
                    break;
                }
                case PacketType.AimTargetUpdate: {
                    ServerAimTargetUpdate(packetObj);
                    break;
                }
                case PacketType.PaintedTargetUpdate:
                {
                    ServerPaintedTargetUpdate(packetObj);
                    break;
                }
                case PacketType.AmmoCycleRequest: {
                    ServerAmmoCycleRequest(packetObj);
                    break;
                }
                case PacketType.ReticleUpdate: {
                    ServerReticleUpdate(packetObj);
                    break;
                }
                case PacketType.CountingDownUpdate:
                {
                    ServerCountingDownUpdate(packetObj);
                    break;
                }
                case PacketType.CriticalReactionUpdate:
                {
                    ServerCriticalReactionUpdate(packetObj);
                    break;
                }
                case PacketType.PlayerControlRequest:
                {
                    ServerPlayerControlRequest(packetObj);
                    break;
                }
                case PacketType.ClientAiAdd:
                case PacketType.ClientAiRemove: {
                    ServerClientAiExists(packetObj);
                    break;
                }
                case PacketType.OverRidesUpdate: {
                    ServerOverRidesUpdate(packetObj);
                    break;
                }
                case PacketType.RequestDroneSet:
                {
                    ServerDroneUpdate(packetObj);
                    break;
                }
                case PacketType.ClientReady:
                {
                        ServerClientReady(packetObj);
                    break;
                }
                case PacketType.RequestShootUpdate: {
                    //ServerRequestShootUpdate(packetObj);
                    break;
                }
                case PacketType.FixedWeaponHitEvent: {
                    ServerFixedWeaponHitEvent(packetObj);
                    break;
                }
                case PacketType.RequestSetRof:
                case PacketType.RequestSetReportTarget:
                case PacketType.RequestSetOverload:
                case PacketType.RequestSetRange:
                case PacketType.RequestSetDps:
                case PacketType.RequestSetGravity:
                {
                    ServerUpdateSetting(packetObj);
                    break;
                }
                case PacketType.FocusUpdate:
                case PacketType.FocusLockUpdate:
                case PacketType.ReleaseActiveUpdate: {
                    ServerFocusUpdate(packetObj);
                    break;
                }
                case PacketType.ProblemReport: {
                    ServerRequestReport(packetObj);
                    break;
                }
                case PacketType.TerminalMonitor: {
                    ServerTerminalMonitor(packetObj);
                    break;
                }
                default:
                    packetObj.Report.PacketValid = false;
                    Reporter.ReportData[PacketType.Invalid].Add(packetObj.Report);
                    break;
            }

            if (!packetObj.Report.PacketValid)
                Log.LineShortDate(packetObj.ErrorPacket.Error, "net");

            PacketObjPool.Return(packetObj);
        }
        #endregion

        #region ProcessRequests
        private void ClientReceivedDeathPacket(byte[] rawData)
        {
            try
            {
                var deathSyncMonitor = MyAPIGateway.Utilities.SerializeFromBinary<ProtoDeathSyncMonitor>(rawData);
                if (deathSyncMonitor == null || deathSyncMonitor.Collection.Count == 0)
                {
                    Log.Line("ClientReceivedPdPacket null or empty packet");
                    return;
                }

                ++DeathSyncPackets;
                DeathSyncDataSize += rawData.Length;

                for (int i = 0; i < deathSyncMonitor.Collection.Count; i++)
                {
                    var pdInfo = deathSyncMonitor.Collection[i];
                    Projectile p = null;
                    Weapon w = null;
                    if (WeaponLookUp.TryGetValue(pdInfo.WeaponId, out w) && w.ProjectileSyncMonitor.TryGetValue(pdInfo.SyncId, out p) && (p.State == Projectile.ProjectileState.Alive || p.State == Projectile.ProjectileState.ClientPhantom))
                    {
                        p.State = Projectile.ProjectileState.Destroy;
                    }
                    //else
                    //    Log.Line($"pdSyncNotFound: syncId:{pdInfo.SyncId} - wId:{pdInfo.WeaponId} - i:{i} - wFound:{w != null} - pFound:{p != null} - pState:{p?.State}");
                }
                deathSyncMonitor.Collection.Clear();

            }
            catch (Exception ex) { Log.Line($"Exception in ClientReceivedDeathPacket: {ex}", null, true); }
        }


        internal void ProcessDeathSyncsForClients()
        {
            if (!AdvSyncClient)
            {
                var payLoad = MyAPIGateway.Utilities.SerializeToBinary(ProtoDeathSyncMonitor);
                var playerCount = Players.Values.Count;

                DeathSyncPackets += playerCount;
                DeathSyncDataSize += playerCount * payLoad.Length;

                foreach (var p in Players.Values)
                {
                    if (p.Player.SteamUserId != MultiplayerId)
                        MyModAPIHelper.MyMultiplayer.Static.SendMessageTo(ClientPdPacketId, payLoad, p.Player.SteamUserId, true);
                }
                ProtoDeathSyncMonitor.Collection.Clear();
            }
            else if (Tick60)
            {
                for (int i = 0; i < ProtoDeathSyncMonitor.Collection.Count; i++) {

                    var pdInfo = ProtoDeathSyncMonitor.Collection[i];
                    Projectile p;
                    Weapon w;
                    if (WeaponLookUp.TryGetValue(pdInfo.WeaponId, out w) && w.ProjectileSyncMonitor.TryGetValue(pdInfo.SyncId, out p) && (p.State == Projectile.ProjectileState.Alive || p.State == Projectile.ProjectileState.ClientPhantom))
                        p.State = Projectile.ProjectileState.Destroy;
                }
                ProtoDeathSyncMonitor.Collection.Clear();
            }
        }

        internal void ProccessServerPacketsForClients()
        {

            if ((!IsServer || !MpActive))
            {
                Log.Line("trying to process server packets on a non-server");
                return;
            }

            PacketsToClient.AddRange(PrunedPacketsToClient.Values);
            for (int i = 0; i < PacketsToClient.Count; i++)
            {
                var packetInfo = PacketsToClient[i];

                var sPlayerId = packetInfo.SpecialPlayerId;
                var hasRewritePlayer = sPlayerId > 0 && packetInfo.Function != null;
                var addOwl = sPlayerId == long.MinValue && packetInfo.Function != null;
                var hasSkipPlayer = !hasRewritePlayer && sPlayerId > 0;
                var packet = packetInfo.Packet;
                var reliable = !packetInfo.Unreliable;
                var bytes = MyAPIGateway.Utilities.SerializeToBinary(packet);
                if (packetInfo.SingleClient)
                    MyModAPIHelper.MyMultiplayer.Static.SendMessageTo(ClientPacketId, bytes, packet.SenderId, reliable);
                else
                {
                    long entityId = packetInfo.Entity?.GetTopMostParent().EntityId ?? -1;
                    foreach (var p in Players.Values)
                    {
                        var steamId = p.Player.SteamUserId;
                        var notSender = steamId != packet.SenderId;
                        
                        var specialPlayer = sPlayerId == p.PlayerId;
                        var skipPlayer = hasSkipPlayer && specialPlayer;

                        byte[] bytesRewrite = null;
                        var rewrite = specialPlayer && hasRewritePlayer || addOwl;
                        if (rewrite)
                            bytesRewrite = MyAPIGateway.Utilities.SerializeToBinary((Packet)packetInfo.Function(packet, steamId));


                        var sendPacket = notSender && packetInfo.Entity == null;
                        if (!sendPacket && !skipPlayer && notSender)
                        {
                            HashSet<long> entityIds;
                            if (PlayerEntityIdInRange.TryGetValue(steamId, out entityIds))
                            {
                                if (entityIds.Contains(entityId)) {
                                    sendPacket = true;
                                }
                                else  {
                                    Ai rootAi;
                                    CoreComponent comp;
                                    var notGrid = packetInfo.Entity != null && !(packetInfo.Entity is MyCubeBlock);
                                    var entity = notGrid && IdToCompMap.TryGetValue(packetInfo.Entity.EntityId, out comp) ? comp.TopEntity : packetInfo.Entity.GetTopMostParent();
                                    if (entity != null && EntityToMasterAi.TryGetValue(entity, out rootAi) && PlayerEntityIdInRange[p.Player.SteamUserId].Contains(rootAi.TopEntity.EntityId))
                                        sendPacket = true;
                                }
                            }
                        }

                        if (sendPacket)
                            MyModAPIHelper.MyMultiplayer.Static.SendMessageTo(ClientPacketId, !rewrite ? bytes : bytesRewrite, p.Player.SteamUserId, reliable);
                    }
                }
            }

            ServerPacketsForClientsClean();
        }

        private void ServerPacketsForClientsClean()
        {
            PacketsToClient.Clear();
            var prunedPackets = PrunedPacketsToClient.Values.ToArray();
            PrunedPacketsToClient.Clear();
            foreach (var pInfo in prunedPackets)
            {
                switch (pInfo.Packet.PType)
                {
                    case PacketType.ProjectilePosSyncs:
                    {
                        pInfo.Packet.CleanUp();
                        ProtoWeaponProPosPacketPool.Push((ProjectileSyncPositionPacket)pInfo.Packet);
                        break;
                    }
                    case PacketType.ProjectileTargetSyncs:
                    {
                        pInfo.Packet.CleanUp();
                        ProtoWeaponProTargetPacketPool.Push((ProjectileSyncTargetPacket)pInfo.Packet);
                        break;
                    }
                    case PacketType.AiData:
                    {
                        PacketAiPool.Return((AiDataPacket)pInfo.Packet);
                        break;
                    }
                    case PacketType.TargetChange:
                    {
                        PacketTargetPool.Return((TargetPacket)pInfo.Packet);
                        break;
                    }
                    case PacketType.WeaponReload:
                    {
                        PacketReloadPool.Return((WeaponReloadPacket)pInfo.Packet);
                        break;
                    }
                    case PacketType.Construct:
                    {
                        PacketConstructPool.Return((ConstructPacket)pInfo.Packet);
                        break;
                    }
                    case PacketType.ConstructFoci:
                    {
                        PacketConstructFociPool.Return((ConstructFociPacket)pInfo.Packet);
                        break;
                    }
                    case PacketType.WeaponAmmo:
                    {
                        PacketAmmoPool.Return((WeaponAmmoPacket)pInfo.Packet);
                        break;
                    }
                    case PacketType.WeaponComp:
                    {
                        PacketWeaponCompPool.Return((WeaponCompPacket)pInfo.Packet);
                        break;
                    }
                    case PacketType.WeaponState:
                    {
                        PacketWeaponStatePool.Return((WeaponStatePacket)pInfo.Packet);
                        break;
                    }
                    case PacketType.UpgradeComp:
                    {
                        PacketUpgradeCompPool.Return((UpgradeCompPacket)pInfo.Packet);
                        break;
                    }
                    case PacketType.UpgradeState:
                    {
                        PacketUpgradeStatePool.Return((UpgradeStatePacket)pInfo.Packet);
                        break;
                    }
                    case PacketType.SupportComp:
                    {
                        PacketSupportCompPool.Return((SupportCompPacket)pInfo.Packet);
                        break;
                    }
                    case PacketType.SupportState:
                    {
                        PacketSupportStatePool.Return((SupportStatePacket)pInfo.Packet);
                        break;
                    }
                    case PacketType.ControlComp:
                    {
                            PacketControlCompPool.Return((ControlCompPacket)pInfo.Packet);
                        break;
                    }
                    case PacketType.ControlState:
                    {
                        PacketControlStatePool.Return((ControlStatePacket)pInfo.Packet);
                        break;
                    }
                }
            }
        }
        #endregion
    }
}
