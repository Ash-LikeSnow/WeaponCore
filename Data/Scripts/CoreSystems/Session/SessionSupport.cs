using System;
using System.Collections.Generic;
using System.Linq;
using CoreSystems.Platform;
using CoreSystems.Settings;
using CoreSystems.Support;
using ParallelTasks;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character.Components;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using Scripts;
using VRage;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Input;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;
using static CoreSystems.Support.Ai;
using static CoreSystems.Support.CoreComponent;
using static CoreSystems.Support.CoreComponent.Trigger;

namespace CoreSystems
{
    public partial class Session
    {
        internal void Timings()
        {
            _paused = false;
            Tick++;
            Tick5 = Tick % 5 == 0;
            Tick10 = Tick % 10 == 0;
            Tick20 = Tick % 20 == 0;
            Tick30 = Tick % 30 == 0;
            Tick60 = Tick % 60 == 0;
            Tick90 = Tick % 90 == 0;
            Tick120 = Tick % 120 == 0;
            Tick180 = Tick % 180 == 0;
            Tick300 = Tick % 300 == 0;
            Tick600 = Tick % 600 == 0;
            Tick1200 = Tick % 1200 == 0;
            Tick1800 = Tick % 1800 == 0;
            Tick3600 = Tick % 3600 == 0;

            var serverSim = MyAPIGateway.Physics.ServerSimulationRatio;
            var localSim = MyAPIGateway.Physics.SimulationRatio;
            var serverSimClamped = MathHelperD.Clamp(serverSim, 0.001d, 1);
            DeltaTimeRatio = IsServer ? 1 : serverSimClamped / MathHelperD.Clamp(localSim, 0.001d, serverSimClamped);
            DeltaStepConst = DeltaTimeRatio * StepConst;
            RelativeTime += DeltaStepConst;
            ServerSimulation += serverSim;
            LocalSimulation += localSim;

            SimStepsLastSecond += MyAPIGateway.Physics.StepsLastSecond;


            if (Tick60)
            {
                if (Av.ExplosionCounter - 5 >= 0) Av.ExplosionCounter -= 5;
                else Av.ExplosionCounter = 0;
            }
            
            if (++SCount == 60) SCount = 0;
            if (++QCount == 15) QCount = 0;

            if (++AwakeCount == AwakeBuckets) AwakeCount = 0;
            if (++AsleepCount == AsleepBuckets) AsleepCount = 0;

            if (Count++ == 119)
            {
                Count = 0;
                UiBkOpacity = MyAPIGateway.Session.Config.UIBkOpacity;
                UiOpacity = MyAPIGateway.Session.Config.UIOpacity;
                UiHudOpacity = MyAPIGateway.Session.Config.HUDBkOpacity;
                CheckAdminRights();
                if (IsServer && MpActive && (AuthLogging || ConnectedAuthors.Count > 0)) AuthorDebug();
                
                if (IsServer && PbActivate && !PbApiInited) Api.PbInit();

                if (HandlesInput && !ClientCheck && Tick > 1200)
                {
                    if (IsClient)
                    {
                        if (ServerVersion != ModContext.ModName)
                        {
                            var message = $"::CoreSystems Version Mismatch::    Server:{ServerVersion} - Client:{ModContext.ModName} -   Unexpected behavior may occur.";
                            Log.Line(message);
                            //MyAPIGateway.Utilities.ShowNotification(message, 10000, "Red");
                        }
                    }
                    ClientCheck = true;
                }

            }
            LCount++;
            if (LCount == 129)
                LCount = 0;

            if (Tick1200 && PlayerStartMessage && !string.IsNullOrEmpty(PlayerMessage)) {
                PlayerStartMessage = false;
                MyAPIGateway.Utilities.ShowNotification(PlayerMessage, 15000);
            }

            if (!GameLoaded)
            {
                if (FirstLoop)
                {
                    if (!MiscLoaded)
                        MiscLoaded = true;

                    InitRayCast();

                    GameLoaded = true;                   
                    if (LocalVersion)
                        Log.Line("Local CoreSystems Detected");
                }
                else if (!FirstLoop)
                {
                    FirstLoop = true;
                    foreach (var t in AllDefinitions)
                    {
                        var name = t.Id.SubtypeName;

                        if (name.Contains("Armor"))
                        {
                            var normalArmor = name.Contains("ArmorBlock") || name.Contains("HeavyArmor") || name.StartsWith("LargeRoundArmor") || name.Contains("BlockArmor");
                            var blast = !normalArmor && (name == "ArmorCenter" || name == "ArmorCorner" || name == "ArmorInvCorner" || name == "ArmorSide" || name.StartsWith("SmallArmor"));
                            if (normalArmor || blast)
                            {
                                AllArmorBaseDefinitions.Add(t);
                                if (blast || name.Contains("Heavy")) HeavyArmorBaseDefinitions.Add(t);
                            }
                        }
                        else if (name.StartsWith("DSControl") || name.StartsWith("NPCControl") || name.StartsWith("Emitter") || name.StartsWith("NPCEmitter"))
                        {
                            CoreShieldBlockTypes.Add(t);
                        }
                    }
                }
            }

            if (!PlayersLoaded && KeenFuckery())
                PlayersLoaded = true;

            //Api.PbHackDetection();
        }

        internal void AddLosCheck(LosDebug debug)
        {
            //if (!WeaponLosDebugActive.Add(debug.Part))
                //return;
            
            LosDebugList.Add(debug);
        }

        private void Test()
        {
            var endId = XorRnd.NextBoolean() ? XorRnd.NextUInt16() : (ushort)XorRnd.NextUInt64();

            var pCounter = XorRnd.NextUInt16();

            var spawnFrags = XorRnd.NextUInt16();
            var origFrags = spawnFrags;
            var spawnDepths = XorRnd.NextUInt16();
            var origDepths = spawnDepths;

            var testULong = ((ulong)endId << 48) | ((ulong)pCounter << 32) | ((ulong)spawnFrags << 16) | spawnDepths;
            var origLong = testULong;
            if (XorRnd.NextBoolean())
                spawnFrags++;

            if (XorRnd.NextBoolean())
                spawnDepths++;

            testULong = (testULong & 0xFFFFFFFF00000000) | ((ulong)spawnFrags << 16) | spawnDepths;

            var value1 = (ushort)((testULong >> 48) & 0x000000000000FFFF);
            var value2 = (ushort)((testULong >> 32) & 0x000000000000FFFF);
            var value3 = (ushort)((testULong >> 16) & 0x000000000000FFFF);
            var value4 = (ushort)(testULong & 0x000000000000FFFF);

            Log.Line($"{value1} - {endId}");
        }

        internal void VisualDebuging()
        {
            for (var i = LosDebugList.Count - 1; i >= 0; i--)
            {
                var info = LosDebugList[i];
                DsDebugDraw.DrawLine(info.Line, Color.Red, 0.15f);

                if (Tick - info.HitTick > 1200)
                {
                    LosDebugList.RemoveAtFast(i);
                    WeaponLosDebugActive.Remove(info.Part);
                }
            }

            foreach (var p in ProSyncLineDebug)
            {
                for (var i = p.Value.Count - 1; i >= 0; i--)
                {
                    var info = p.Value[i];
                    DsDebugDraw.DrawLine(info.Line, info.Color, 0.35f);
                    if (Tick - info.CreateTick > 3600)
                    {
                        p.Value.RemoveAt(i);
                    }
                }
            }


            foreach (var p in ApproachStageChangeDebug)
            {
                var draw = Tick - p.Value.CreateTick <= 180;
                ApproachStageDebug old;
                if (draw)
                {
                    DsDebugDraw.DrawX(p.Value.Position, CameraMatrix, 20);
                }
                else
                    ApproachStageChangeDebug.TryRemove(p.Key, out old);

            }


            if (ApproachDebug.LastTick == Tick && Tick != uint.MaxValue)
            {
                if (Tick10)
                {
                    if (ApproachDebug.TimeSinceSpawn <= double.MinValue)
                    {
                        ShowLocalNotify($"[Approach] Stage:{ApproachDebug.Stage} - Start1:{ApproachDebug.Approach.Definition.StartCondition1}:{ApproachDebug.Start1} - Start2:{ApproachDebug.Approach.Definition.StartCondition2}:{ApproachDebug.Start2} - End1:{ApproachDebug.Approach.Definition.EndCondition1}:{ApproachDebug.End1} - End2:{ApproachDebug.Approach.Definition.EndCondition2}:{ApproachDebug.End2} - End3:{ApproachDebug.Approach.Definition.EndCondition3}:{ApproachDebug.End3}", 160, "White");
                        ShowLocalNotify($"[AccelMulti:{ApproachDebug.Approach.Definition.AccelMulti} - SpeedCapMulti:{ApproachDebug.Approach.Definition.SpeedCapMulti} - LeadDist:{ApproachDebug.Approach.Definition.LeadDistance} - RestartType:{ApproachDebug.Approach.Definition.RestartCondition}]", 160, "White");
                        ShowLocalNotify($"[Forward:{ApproachDebug.Approach.Forward} - Up:{ApproachDebug.Approach.Up} - Source:{ApproachDebug.Approach.PositionB} Destination:{ApproachDebug.Approach.PositionC}", 160, "White");

                    }
                    else
                    {
                        ShowLocalNotify($"[Approach] Stage:{ApproachDebug.Stage} - Start1:{ApproachDebug.Approach.Definition.StartCondition1}:{ApproachDebug.Start1} - Start2:{ApproachDebug.Approach.Definition.StartCondition2}:{ApproachDebug.Start2} - End1:{ApproachDebug.Approach.Definition.EndCondition1}:{ApproachDebug.End1} - End2:{ApproachDebug.Approach.Definition.EndCondition2}:{ApproachDebug.End2} - End3:{ApproachDebug.Approach.Definition.EndCondition3}:{ApproachDebug.End3}", 160, "White");
                        ShowLocalNotify($"[AccelMulti:{ApproachDebug.Approach.Definition.AccelMulti} - SpeedCapMulti:{ApproachDebug.Approach.Definition.SpeedCapMulti} - LeadDist:{ApproachDebug.Approach.Definition.LeadDistance} - RestartType:{ApproachDebug.Approach.Definition.RestartCondition}", 160, "White");
                        ShowLocalNotify($"[Forward:{ApproachDebug.Approach.Forward} - Up:{ApproachDebug.Approach.Up} - Source:{ApproachDebug.Approach.PositionB} Destination:{ApproachDebug.Approach.PositionC}", 160, "White");
                        ShowLocalNotify($"[TimeSinceSpawn:{ApproachDebug.TimeSinceSpawn} - NextSpawn:{ApproachDebug.NextSpawn}", 160, "White");
                    }

                }
            }
            else if (ApproachDebug.LastTick == Tick - 1 && Tick != 1)
            {
                if (ApproachDebug.TimeSinceSpawn <= double.MinValue)
                    ShowLocalNotify($"[Approach] Completed on stage:{ApproachDebug.Stage} - {ApproachDebug.Start1}:{ApproachDebug.Start2}:{ApproachDebug.End1}:{ApproachDebug.End2}", 2000, "White");
                else
                    ShowLocalNotify($"[Approach] Completed on stage:{ApproachDebug.Stage} - {ApproachDebug.Start1}:{ApproachDebug.Start2}:{ApproachDebug.End1}:{ApproachDebug.End2} - {ApproachDebug.TimeSinceSpawn}:{ApproachDebug.NextSpawn}", 2000, "White");
            }

            /*
            if (Tick - _clientHandDebug.LastHitTick < 1200 || Tick - _clientHandDebug.LastShootTick < 1200)
            {
                if (_clientHandDebug.ShootStart != Vector3D.Zero)
                    DsDebugDraw.DrawLine(_clientHandDebug.ShootStart, _clientHandDebug.ShootEnd, Color.Blue, 0.2f);
                if (_clientHandDebug.HitStart != Vector3D.Zero)
                    DsDebugDraw.DrawLine(_clientHandDebug.HitStart, _clientHandDebug.HitEnd, Color.Red, 0.2f);
            }

            if (Tick - _clientHandDebug2.LastHitTick < 1200 || Tick - _clientHandDebug2.LastShootTick < 1200)
            {
                if (_clientHandDebug2.ShootStart != Vector3D.Zero)
                    DsDebugDraw.DrawLine(_clientHandDebug2.ShootStart, _clientHandDebug2.ShootEnd, Color.Green, 0.2f);
                if (_clientHandDebug2.HitStart != Vector3D.Zero)
                    DsDebugDraw.DrawLine(_clientHandDebug2.HitStart, _clientHandDebug2.HitEnd, Color.Yellow, 0.2f);
            }
            */
        }

        public void AddHandHitDebug(Vector3D start, Vector3D end, bool shoot)
        {
            var packet = HandlesInput ? _clientHandDebug2 : HandDebugPacketPacket;
            if (shoot)
            {
                packet.LastShootTick = Tick + 1;
                packet.LastHitTick = uint.MaxValue;
                packet.ShootStart = start;
                packet.ShootEnd = end;
                packet.HitStart = Vector3D.Zero;
                packet.HitEnd = Vector3D.Zero;
            }
            else
            {
                packet.LastHitTick = Tick + 1;
                packet.HitStart = start;
                packet.HitEnd = end;
            }

        }

        private double _drawCpuTime;
        private double _paCpuTime;
        private double _avCpuTime;

        private double _avTotal;
        private double _prevAvTotal;
        internal void ProfilePerformance()
        {
            var netTime1 = DsUtil.GetValue("network1");
            var psTime = DsUtil.GetValue("ps");
            var piTIme = DsUtil.GetValue("pi");
            var pdTime = DsUtil.GetValue("pd");
            var paTime = DsUtil.GetValue("pa");

            var updateTime = DsUtil.GetValue("shoot");
            var drawTime = DsUtil.GetValue("draw");
            var av = DsUtil.GetValue("av");
            var db = DsUtil.GetValue("db");
            var ai = DsUtil.GetValue("ai");
            var charge = DsUtil.GetValue("charge");
            var acquire = DsUtil.GetValue("acquire");

            var clientSim = LocalSimulation / 180;
            var serverSim = ServerSimulation / 180;
            var simSteps = SimStepsLastSecond / 180;

            Log.LineShortDate($"(CPU-T) --- <Steps>{simSteps} <S-Sim>{Math.Round(serverSim, 2)} <C-Sim>{Math.Round(clientSim, 2)} <AI>{ai.Median:0.0000}/{ai.Min:0.0000}/{ai.Max:0.0000} <Acq>{acquire.Median:0.0000}/{acquire.Min:0.0000}/{acquire.Max:0.0000} <SH>{updateTime.Median:0.0000}/{updateTime.Min:0.0000}/{updateTime.Max:0.0000} <CH>{charge.Median:0.0000}/{charge.Min:0.0000}/{charge.Max:0.0000} <PS>{psTime.Median:0.0000}/{psTime.Min:0.0000}/{psTime.Max:0.0000} <PI>{piTIme.Median:0.0000}/{piTIme.Min:0.0000}/{piTIme.Max:0.0000} <PD>{pdTime.Median:0.0000}/{pdTime.Min:0.0000}/{pdTime.Max:0.0000} <PA>{paTime.Median:0.0000}/{paTime.Min:0.0000}/{paTime.Max:0.0000} <DR>{drawTime.Median:0.0000}/{drawTime.Min:0.0000}/{drawTime.Max:0.0000} <AV>{av.Median:0.0000}/{av.Min:0.0000}/{av.Max:0.0000} <NET1>{netTime1.Median:0.0000}/{netTime1.Min:0.0000}/{netTime1.Max:0.0000}> <DB>{db.Median:0.0000}/{db.Min:0.0000}/{db.Max:0.0000}>", "perf");
            Log.LineShortDate($"(STATS) -------- AIs:[{EntityAIs.Count}] - WcBlocks:[{IdToCompMap.Count}] - AiReq:[{TargetRequests}] Targ:[{TargetChecks}] Bloc:[{BlockChecks}] Aim:[{CanShoot}] CCast:[{ClosestRayCasts}] RndCast[{RandomRayCasts}] TopCast[{TopRayCasts}]", "stats");

            var desyncLevel = serverSim - clientSim;
            if (IsClient && desyncLevel >= 0.1)
                Log.LineShortDate($"[Client Lagged] desync: {desyncLevel * 100}%", "perf");
            
            _drawCpuTime =  drawTime.Median;
            _avCpuTime = av.Median;
            _paCpuTime = paTime.Median;
            _prevAvTotal = _avTotal;
            _avTotal = _avCpuTime + _drawCpuTime + _paCpuTime;

            SimStepsLastSecond = 0;
            ServerSimulation = 0;
            LocalSimulation = 0;
            TargetRequests = 0;
            TargetChecks = 0;
            BlockChecks = 0;
            CanShoot = 0;
            ClosestRayCasts = 0;
            RandomRayCasts = 0;
            TopRayCasts = 0;
            TargetTransfers = 0;
            TargetSets = 0;
            TargetResets = 0;
            AmmoMoveTriggered = 0;
            Load = 0d;
            DsUtil.Clean();
        }


        internal void ClientMonitor()
        {
            if (ClientPerfHistory.Count > 19)
                ClientPerfHistory.Dequeue();

            var highestBillCount = Av.NearBillBoardLimit;
            var cpuHog = _avTotal > 8 && _prevAvTotal > 8;

            var forceEnable = highestBillCount > 25000 || cpuHog;

            Av.NearBillBoardLimit /= 2;
            ClientPerfHistory.Enqueue(_drawCpuTime);
            if (forceEnable || Settings.ClientConfig.ClientOptimizations || ClientAvDivisor > 1)
            {
                ClientAvLevel = GetClientPerfTarget(forceEnable);

                var oldDivisor = ClientAvDivisor;
                ClientAvDivisor = ClientAvLevel + 1;
                
                var change = ClientAvDivisor != oldDivisor;
                if (change)
                    Log.LineShortDate($"ClientAvScaler changed From:[{oldDivisor}] To:[{ClientAvDivisor}] Billbaords:[{highestBillCount}]", "perf");
            }
        }

        private int GetClientPerfTarget(bool forceIncrease)
        {
            var c = 0;
            var last = ClientPerfHistory.Count - 1;
            int lastValue = 0;
            var minValue = int.MaxValue;
            int maxValue = 0;

            foreach (var v in ClientPerfHistory)
            {
                var rawV = Math.Round(v);
                if (rawV < 4)
                    rawV = 0;
                else
                    rawV -= 2;

                var rV = MathHelper.Clamp((int)rawV, Settings.ClientConfig.AvLimit, int.MaxValue);
                
                if (rV > maxValue)
                    maxValue = rV;

                if (rV <= minValue)
                    minValue = rV;

                if (c++ == last && rV > ClientAvLevel)
                    lastValue = rV;
            }

            if (forceIncrease)
                lastValue = ClientAvLevel + 1;

            var newValue = lastValue > ClientAvLevel ? ClientAvLevel + 1 : maxValue < ClientAvLevel ? ClientAvLevel - 1 : ClientAvLevel;
            return MathHelper.Clamp(newValue, 0, 20);
        }

        internal void NetReport()
        {
            foreach (var reports in Reporter.ReportData)
            {
                var typeStr = reports.Key.ToString();
                var reportList = reports.Value;
                int clientReceivers = 0;
                int serverReceivers = 0;
                int noneReceivers = 0;
                int validPackets = 0;
                int invalidPackets = 0;
                ulong dataTransfer = 0;
                foreach (var report in reportList)
                {
                    if (report.PacketValid) validPackets++;
                    else invalidPackets++;

                    if (report.Receiver == NetworkReporter.Report.Received.None) noneReceivers++;
                    else if (report.Receiver == NetworkReporter.Report.Received.Server) serverReceivers++;
                    else clientReceivers++;

                    dataTransfer += (uint)report.PacketSize;
                    Reporter.ReportPool.Return(report);
                }
                var packetCount = reports.Value.Count;
                if (packetCount > 0) Log.LineShortDate($"(NINFO) - <{typeStr}> packets:[{packetCount}] dataTransfer:[{dataTransfer}] validPackets:[{validPackets}] invalidPackets:[{invalidPackets}] serverReceive:[{serverReceivers}({IsServer})] clientReceive:[{clientReceivers}] unknownReceive:[{noneReceivers}]", "net");
            }

            if (DeathSyncDataSize > 0)
            {
                var serverPackets = IsServer ? DeathSyncPackets : 0;
                var clientPackets = !IsServer ? DeathSyncPackets : 0;
                Log.LineShortDate($"(NINFO) - <PointDefenseSync> packets:[{DeathSyncPackets}] dataTransfer:[{DeathSyncDataSize}] validPackets:[{DeathSyncPackets}] invalidPackets:[0] serverReceive:[{serverPackets}({IsServer})] clientReceive:[{clientPackets}] unknownReceive:[0]", "net");
                DeathSyncDataSize = 0;
                DeathSyncPackets = 0;
            }

            foreach (var list in Reporter.ReportData.Values)
                list.Clear();
        }

        internal int ShortLoadAssigner()
        {
            if (_shortLoadCounter + 1 > 14) _shortLoadCounter = 0;
            else ++_shortLoadCounter;

            return _shortLoadCounter;
        }

        internal int LoadAssigner()
        {
            if (_loadCounter + 1 > 119) _loadCounter = 0;
            else ++_loadCounter;

            return _loadCounter;
        }

        internal List<MyLineSegmentOverlapResult<MyEntity>> AimRayEnts = new List<MyLineSegmentOverlapResult<MyEntity>>();
        internal bool GetAimedAtBlock(out MyCubeBlock cube)
        {
            cube = null;
            if (UiInput.AimRay.Length > 0)
            {
                AimRayEnts.Clear();
                MyGamePruningStructure.GetTopmostEntitiesOverlappingRay(ref UiInput.AimRay, AimRayEnts);
                foreach (var ent in AimRayEnts)
                {
                    var grid = ent.Element as MyCubeGrid;
                    if (grid?.Physics != null && grid.Physics.Enabled && !grid.Physics.IsPhantom && grid.InScene && !grid.IsPreview)
                    {
                        MyCube myCube;
                        var hitV3I = grid.RayCastBlocks(UiInput.AimRay.From, UiInput.AimRay.To);
                        if (hitV3I.HasValue && grid.TryGetCube(hitV3I.Value, out myCube)) {

                            var slim = (IMySlimBlock)myCube.CubeBlock;
                            if (slim.FatBlock != null) {
                                cube = (MyCubeBlock)slim.FatBlock;
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }


        private void CheckAdminRights()
        {
            const string spider = "Space_spider";
            const string wolf = "SpaceWolf";
            foreach (var item in Players) {

                var pLevel = item.Value.Player.PromoteLevel;
                var playerId = item.Key;
                var player = item.Value.Player;
                var wasAdmin = Admins.ContainsKey(playerId);

                if (pLevel == MyPromoteLevel.Admin || pLevel == MyPromoteLevel.Owner || pLevel == MyPromoteLevel.SpaceMaster) {

                    var character = player.Character;
                    var isAdmin = false;
                    if (character != null ) {
                        if (character.Definition.Id.SubtypeName.Equals(wolf) || character.Definition.Id.SubtypeName.StartsWith(spider)) continue;

                        if (MySafeZone.CheckAdminIgnoreSafezones(player.SteamUserId))
                            isAdmin = true;
                        else {

                            foreach (var gridAi in EntityAIs.Values) {

                                if (gridAi.Targets.ContainsKey((MyEntity)character) && gridAi.CompBase.Count > 0 && (gridAi.WeaponComps.Count > 0 || gridAi.UpgradeComps.Count > 0 || gridAi.SupportComps.Count > 0 || gridAi.ControlComps.Count > 0)) {
                                    
                                    var access = false;
                                    foreach (var comp in gridAi.CompBase) {

                                        if (!comp.Value.IsBlock)
                                            continue;

                                        access = ((IMyTerminalBlock)comp.Key).HasPlayerAccess(playerId);
                                        break;
                                    }

                                    if (access && MyIDModule.GetRelationPlayerBlock(playerId, gridAi.AiOwner) == MyRelationsBetweenPlayerAndBlock.Enemies) {
                                        isAdmin = true;
                                        break;
                                    }
                                }
                            }
                        }

                        if (isAdmin) {
                            Admins[playerId] = character;
                            AdminMap[character] = player;
                            continue;
                        }
                    }
                }

                if (wasAdmin)
                {
                    IMyCharacter removeCharacter;
                    IMyPlayer removePlayer;
                    Admins.TryRemove(playerId, out removeCharacter);
                    AdminMap.TryRemove(removeCharacter, out removePlayer);
                }
            }
        }

        public static bool GridEnemy(long gridOwner, MyCubeGrid grid, List<long> owners = null)
        {
            if (owners == null) owners = grid.BigOwners;
            if (owners.Count == 0) return true;
            var relationship = MyIDModule.GetRelationPlayerBlock(gridOwner, owners[0], MyOwnershipShareModeEnum.Faction);
            var enemy = relationship != MyRelationsBetweenPlayerAndBlock.Owner && relationship != MyRelationsBetweenPlayerAndBlock.FactionShare;
            return enemy;
        }


        internal void InitRayCast()
        {
            List<IHitInfo> tmpList = new List<IHitInfo>();
            MyAPIGateway.Physics.CastRay(new Vector3D { X = 10, Y = 10, Z = 10 }, new Vector3D { X = -10, Y = -10, Z = -10 }, tmpList);
        }


        internal void UpdateHomingWeapons()
        {
            for (int i = HomingWeapons.Count - 1; i >= 0; i--)
            {
                var w = HomingWeapons[i];
                var comp = w.BaseComp;
                if (w.BaseComp.Ai == null || comp.TopEntity.MarkedForClose || comp.Ai.Concealed || comp.CoreEntity.MarkedForClose || !comp.Cube.IsFunctional) {
                    HomingWeapons.RemoveAtFast(i);
                    continue;
                }

                w.TurretHomePosition();

                if (w.IsHome || !w.ReturingHome)
                {
                    HomingWeapons.RemoveAtFast(i);
                    w.EventTriggerStateChanged(WeaponDefinition.AnimationDef.PartAnimationSetDef.EventTriggers.Homing, false);
                }
            }
        }


        internal void CleanSounds(bool force = false)
        {
            for (int i = SoundsToClean.Count - 1; i >= 0; i--)
            {
                var sound = SoundsToClean[i];
                var age = Tick - sound.SpawnTick;
                var delayedClean = sound.DelayedReturn && age > 600;
                var justClean = sound.JustClean;
                if (justClean || force || !sound.Emitter.IsPlaying || (age > 4 && sound.Force || delayedClean))
                {
                    var loop = sound.Emitter.Loop;
                    if (!justClean && (sound.Force || loop)) {

                        if (loop && sound.Pair != null)
                        {
                            sound.Emitter.StopSound(true);
                            sound.Emitter.PlaySound(sound.Pair, stopPrevious: false, skipIntro: true, force2D: false, alwaysHearOnRealistic: false, skipToEnd: true);
                        }
                        else
                            sound.Emitter.StopSound(false);
                    }
                    sound.Emitter.Entity = null;
                    sound.EmitterPool.Push(sound.Emitter);
                    SoundsToClean.RemoveAtFast(i);
                }
            }
        }

        private void UpdateControlKeys()
        {
            if (ControlRequest == ControlQuery.Info)
            {

                MyAPIGateway.Input.GetListOfPressedKeys(_pressedKeys);
                if (_pressedKeys.Count > 0 && _pressedKeys[0] != MyKeys.Enter)
                {

                    var firstKey = _pressedKeys[0];
                    Settings.ClientConfig.InfoKey = firstKey.ToString();
                    UiInput.InfoKey = firstKey;
                    ControlRequest = ControlQuery.None;
                    Settings.VersionControl.UpdateClientCfgFile();
                    MyAPIGateway.Utilities.ShowNotification($"{firstKey.ToString()} is now the WeaponCore Info Key", 10000);
                }
            }
            else if (ControlRequest == ControlQuery.Action)
            {

                MyAPIGateway.Input.GetListOfPressedKeys(_pressedKeys);
                if (_pressedKeys.Count > 0 && _pressedKeys[0] != MyKeys.Enter)
                {

                    var firstKey = _pressedKeys[0];
                    Settings.ClientConfig.ActionKey = firstKey.ToString();
                    UiInput.ActionKey = firstKey;
                    ControlRequest = ControlQuery.None;
                    Settings.VersionControl.UpdateClientCfgFile();
                    MyAPIGateway.Utilities.ShowNotification($"{firstKey.ToString()} is now the WeaponCore Action Key", 10000);
                }
            }
            else if (ControlRequest == ControlQuery.Keyboard)
            {

                MyAPIGateway.Input.GetListOfPressedKeys(_pressedKeys);
                if (_pressedKeys.Count > 0 && _pressedKeys[0] != MyKeys.Enter)
                {

                    var firstKey = _pressedKeys[0];
                    Settings.ClientConfig.ControlKey = firstKey.ToString();
                    UiInput.ControlKey = firstKey;
                    ControlRequest = ControlQuery.None;
                    Settings.VersionControl.UpdateClientCfgFile();
                    MyAPIGateway.Utilities.ShowNotification($"{firstKey.ToString()} is now the WeaponCore Control Key", 10000);
                }
            }
            else if (ControlRequest == ControlQuery.Mouse)
            {

                MyAPIGateway.Input.GetListOfPressedMouseButtons(_pressedButtons);
                if (_pressedButtons.Count > 0)
                {

                    var firstButton = _pressedButtons[0];
                    var invalidButtons = firstButton == MyMouseButtonsEnum.Left || firstButton == MyMouseButtonsEnum.Right || firstButton == MyMouseButtonsEnum.None;

                    if (!invalidButtons)
                    {
                        Settings.ClientConfig.MenuButton = firstButton.ToString();
                        UiInput.MouseButtonMenu = firstButton;
                        Settings.VersionControl.UpdateClientCfgFile();
                        MyAPIGateway.Utilities.ShowNotification($"{firstButton.ToString()}MouseButton will now open and close the WeaponCore Menu", 10000);
                    }
                    else MyAPIGateway.Utilities.ShowNotification($"{firstButton.ToString()}Button is an invalid mouse button for this function", 10000);
                    ControlRequest = ControlQuery.None;
                }
            }
            else if (ControlRequest == ControlQuery.Next)
            {

                MyAPIGateway.Input.GetListOfPressedKeys(_pressedKeys);
                if (_pressedKeys.Count > 0 && _pressedKeys[0] != MyKeys.Enter)
                {

                    var firstKey = _pressedKeys[0];
                    Settings.ClientConfig.CycleNextKey = firstKey.ToString();
                    UiInput.CycleNextKey = firstKey;
                    ControlRequest = ControlQuery.None;
                    Settings.VersionControl.UpdateClientCfgFile();
                    MyAPIGateway.Utilities.ShowNotification($"{firstKey.ToString()} is now the Cycle Next Target Key", 10000);
                }
            }
            else if (ControlRequest == ControlQuery.Prev)
            {

                MyAPIGateway.Input.GetListOfPressedKeys(_pressedKeys);
                if (_pressedKeys.Count > 0 && _pressedKeys[0] != MyKeys.Enter)
                {

                    var firstKey = _pressedKeys[0];
                    Settings.ClientConfig.CyclePrevKey = firstKey.ToString();
                    UiInput.CyclePrevKey = firstKey;
                    ControlRequest = ControlQuery.None;
                    Settings.VersionControl.UpdateClientCfgFile();
                    MyAPIGateway.Utilities.ShowNotification($"{firstKey.ToString()} is now the Cycle Previous Target Key", 10000);
                }
            }
            _pressedKeys.Clear();
            _pressedButtons.Clear();
        }

        internal void CustomBlackListRequestBecauseKeenIsBrainDead(string key, long playerId, bool enable = false)
        {
            if (playerId == -1)
                return;

            if (IsServer)
                MyVisualScriptLogicProvider.SetPlayerInputBlacklistState(key, playerId, enable);
            else if (playerId > 0)
                SendBlackListRequest(key, enable);
        }

        private void ChatMessageSet(string message, ref bool sendToOthers)
        {
            var somethingUpdated = false;

            if (message == "/wc" || message.StartsWith("/wc "))
            {
                switch (message)
                {

                    case "/wc remap keyboard":
                        ControlRequest = ControlQuery.Keyboard;
                        somethingUpdated = true;
                        MyAPIGateway.Utilities.ShowNotification($"Press the key you want to use for the WeaponCore Control key", 10000);
                        break;
                    case "/wc remap mouse":
                        ControlRequest = ControlQuery.Mouse;
                        somethingUpdated = true;
                        MyAPIGateway.Utilities.ShowNotification($"Press the mouse button you want to use to open and close the WeaponCore Menu", 10000);
                        break;
                    case "/wc remap action":
                        ControlRequest = ControlQuery.Action;
                        somethingUpdated = true;
                        MyAPIGateway.Utilities.ShowNotification($"Press the key you want to use for the WeaponCore Action key", 10000);
                        break;
                    case "/wc remap info":
                        ControlRequest = ControlQuery.Info;
                        somethingUpdated = true;
                        MyAPIGateway.Utilities.ShowNotification($"Press the key you want to use for the WeaponCore Info key", 10000);
                        break;
                    case "/wc remap next":
                        ControlRequest = ControlQuery.Next;
                        somethingUpdated = true;
                        MyAPIGateway.Utilities.ShowNotification($"Press the key you want to use for Cycling Next Target Selection", 10000);
                        break;
                    case "/wc remap prev":
                        ControlRequest = ControlQuery.Prev;
                        somethingUpdated = true;
                        MyAPIGateway.Utilities.ShowNotification($"Press the key you want to use for Cycling Previous Target Selection", 10000);
                        break;
                }

                if (ControlRequest == ControlQuery.None)
                {

                    string[] tokens = message.Split(' ');

                    var tokenLength = tokens.Length;
                    if (tokenLength > 1)
                    {
                        switch (tokens[1])
                        {
                            case "avlimit":
                                {
                                    int avLimit;
                                    if (tokenLength > 2 && int.TryParse(tokens[2], out avLimit))
                                    {
                                        avLimit = MathHelper.Clamp(avLimit, 0, 20);
                                        Settings.ClientConfig.AvLimit = avLimit;
                                        var enabled = avLimit > 0;
                                        Settings.ClientConfig.ClientOptimizations = enabled;
                                        somethingUpdated = true;
                                        MyAPIGateway.Utilities.ShowNotification($"The audio visual quality limit is now set to {avLimit}", 10000);
                                        Settings.VersionControl.UpdateClientCfgFile();
                                    }

                                    break;
                                }
                            case "stickypainter":
                                Settings.ClientConfig.StikcyPainter = !Settings.ClientConfig.StikcyPainter;
                                somethingUpdated = true;
                                MyAPIGateway.Utilities.ShowNotification($"Sticky Painter set to: {Settings.ClientConfig.StikcyPainter}", 10000);
                                Settings.VersionControl.UpdateClientCfgFile();
                                break;
                            case "changehud":
                                CanChangeHud = !CanChangeHud;
                                somethingUpdated = true;
                                MyAPIGateway.Utilities.ShowNotification($"Modify Hud set to: {CanChangeHud}", 10000);
                                break;
                            case "advanced":
                                Settings.ClientConfig.AdvancedMode = !Settings.ClientConfig.AdvancedMode;
                                somethingUpdated = true;
                                MyAPIGateway.Utilities.ShowNotification($"Advanced UI set to: {Settings.ClientConfig.AdvancedMode}", 10000);
                                Settings.VersionControl.UpdateClientCfgFile();
                                break;
                            case "setdefaults":
                                Settings.ClientConfig = new CoreSettings.ClientSettings();
                                somethingUpdated = true;
                                MyAPIGateway.Utilities.ShowNotification($"Client configuration has been set to defaults", 10000);
                                Settings.VersionControl.UpdateClientCfgFile();
                                break;
                            case "debug":
                                somethingUpdated = true;
                                DebugMod = !DebugMod;
                                MyAPIGateway.Utilities.ShowNotification($"Debug has been toggled: {DebugMod}", 10000);
                                break;
                            case "unsupportedmode":
                                if (HandlesInput)
                                {
                                    somethingUpdated = true;
                                    Settings.Enforcement.UnsupportedMode = !Settings.Enforcement.UnsupportedMode;
                                    Settings.VersionControl.SaveServerCfg();
                                    if (Settings.Enforcement.UnsupportedMode)
                                        ShowLocalNotify("WeaponCore is running in [UnsupportedMode], certain features and blocks will not work as intended and may crash or become non-functional", 30000, "White");
                                    else
                                        ShowLocalNotify("WeaponCore is now running in [SupportedMode]", 30000, "White");
                                    if (Inited) Log.Line($"--Unsupported mode active: {Settings.Enforcement.UnsupportedMode}--");

                                }

                                break;
                        }
                    }
                }

                if (!somethingUpdated)
                {
                    if (message.Length <= 3)
                        MyAPIGateway.Utilities.ShowNotification("HELPFUL TIPS: https://github.com/Ash-LikeSnow/WeaponCore/wiki/Player-Tips\nValid WeaponCore Commands:\n'/wc advanced -- Toggle advanced UI features'\n'/wc remap -- Remap keys'\n'/wc avlimit 5' -- Hard limits visual effects (valid range: 0 - 20, 0 is unlimited)\n'/wc changehud' to enable moving/resizing of WC Hud\n'/wc setdefaults' -- Resets shield client configs to default values\n'/wc stickypainter' -- Disable Painter LoS checks\n", 10000);
                    else if (message.StartsWith("/wc remap"))
                        MyAPIGateway.Utilities.ShowNotification("'/wc remap keyboard' -- Remaps control key (default R)\n'/wc remap mouse' -- Remaps menu mouse key (default middle button)\n'/wc remap action' -- Remaps action key (default numpad0)\n'/wc remap info' -- Remaps info key (default decimal key, aka numpad period key)\n'/wc remap next' -- Remaps the Cycle Next Target key (default Page Down)\n'/wc remap prev' -- Remaps the Cycle Previous Target key (default Page Up)\n", 10000, "White");
                }
                sendToOthers = false;
            }
        }

        private uint _lastIncompatibleMessageTick = uint.MaxValue;
        internal void RemoveIncompatibleBlock(object o)
        {
            var cube = o as MyCubeBlock;
            if (cube != null)
            {

                var processCube = !cube.MarkedForClose && !cube.Closed && cube.SlimBlock != null && cube.BlockDefinition != null && cube.CubeGrid != null && !cube.CubeGrid.IsPreview && cube.CubeGrid.Physics != null && !cube.CubeGrid.MarkedForClose;
                if (processCube)
                {
                    if (BrokenMod(cube))
                        return;

                    if (_lastIncompatibleMessageTick == uint.MaxValue || Tick - _lastIncompatibleMessageTick > 600)
                    {
                        _lastIncompatibleMessageTick = Tick;
                        var skipMessage = IsClient && Settings.Enforcement.UnsupportedMode && Tick > 600;
                        if (!skipMessage)
                            FutureEvents.Schedule(ReportIncompatibleBlocks, null, 10);
                    }

                    if (cube.BlockDefinition?.Id.SubtypeName != null)
                        _unsupportedBlockNames.Add(cube.BlockDefinition.Id.SubtypeName);

                    if (DedicatedServer)
                    {
                        if (!Settings.Enforcement.UnsupportedMode)
                            cube.CubeGrid.RemoveBlock(cube.SlimBlock, true);
                    }
                    else if (!Settings.Enforcement.UnsupportedMode)
                    {
                        if (!_removeComplete)
                            _unsupportedBlocks.Add(cube);

                        if (_removeComplete || DedicatedServer)
                            cube.CubeGrid.RemoveBlock(cube.SlimBlock, true);

                        if (!_removeScheduled)
                        {
                            _removeScheduled = true;
                            FutureEvents.Schedule(RemoveIncompatibleBlocks, null, 3600);
                        }
                    }
                }
            }
        }

        private readonly HashSet<string> _unsupportedBlockNames = new HashSet<string>();
        private readonly HashSet<MyCubeBlock> _unsupportedBlocks = new HashSet<MyCubeBlock>();
        private bool _removeScheduled;
        private bool _removeComplete;
        private void ReportIncompatibleBlocks(object o)
        {
            string listOfNames = "Incompatible weapons: ";
            foreach (var s in _unsupportedBlockNames)
            {
                listOfNames += $"{s}, ";
            }

            if (DedicatedServer)
            {
                if (!Settings.Enforcement.UnsupportedMode)
                    Log.Line($"Removing incompatible weapon blocks, if you accept the risk you can modify the mods world config file to override for all clients");
                else
                    Log.Line($"Running in unsupported mode, certain features and blocks will not work as intended and may crash or become non-functional");

                Log.Line(listOfNames);

            }
            else
            {
                if (!Settings.Enforcement.UnsupportedMode)
                {
                    ShowLocalNotify("Sadly WeaponCore mods are not compatible with third party weapon mods, you must use one or the other", 30000, "White");
                    ShowLocalNotify(listOfNames, 30000, "White");
                    if (_removeComplete)
                    {
                        ShowLocalNotify("The incompatible weapons listed above have been [REMOVED FROM THE WORLD]", 30000, "Red");
                        ShowLocalNotify("If this is not acceptable quit [WITHOUT SAVING] and either [Enable UnsupportedMode] or uninstall all WC related mods", 30000, "Red");

                    }
                    else
                    {
                        ShowLocalNotify("The incompatible weapons listed above have been [SCHEDULED FOR REMOVAL IN 60 SECONDS]", 30000, "Red");
                        ShowLocalNotify("If this is not acceptable either type [/wc unsupportedmode] or quit [WITHOUT SAVING] and uninstall all WC related mods", 30000, "Red");
                    }

                }
                else
                {
                    ShowLocalNotify("WeaponCore is running in [UnsupportedMode], certain features and blocks will not work as intended and may crash or become non-functional", 30000, "White");
                    ShowLocalNotify(listOfNames, 30000, "White");
                    Log.Line($"Running in unsupported mode, certain features and blocks will not work as intended and may crash or become non-functional");
                    Log.Line(listOfNames);
                }

            }
            _unsupportedBlockNames.Clear();
        }

        private void RemoveIncompatibleBlocks(object o)
        {
            if (Settings.Enforcement.UnsupportedMode)
            {
                _unsupportedBlocks.Clear();
                return;
            }

            foreach (var cube in _unsupportedBlocks)
            {
                if (!cube.MarkedForClose)
                    cube.CubeGrid.RemoveBlock(cube.SlimBlock, true);
            }

            ShowLocalNotify("The incompatible weapons have been [REMOVED FROM THE WORLD]", 30000, "Red");
            ShowLocalNotify("If this is not acceptable quit [WITHOUT SAVING] and either [Enable UnsupportedMode] or uninstall all WC related mods", 30000, "Red");

            _unsupportedBlocks.Clear();
            _removeComplete = true;
        }

        internal bool BrokenMod(MyCubeBlock cube)
        {
            if (cube.Storage == null) return false;

            string rawData;
            if (!cube.Storage.TryGetValue(CompDataGuid, out rawData))
                return false;

            var base64 = Convert.FromBase64String(rawData);
            var brokeMod = MyAPIGateway.Utilities.SerializeFromBinary<ProtoWeaponRepo>(base64) != null;
            if (!brokeMod) return false;

            if (!_reportBrokenMods)
            {
                _reportBrokenMods = true;
                FutureEvents.Schedule(ReportBrokenBlocks, null, 300);
            }

            _brokenBlocks.Add(cube);
            return true;
        }

        private readonly HashSet<MyCubeBlock> _brokenBlocks = new HashSet<MyCubeBlock>();
        private bool _reportBrokenMods;
        private void ReportBrokenBlocks(object o)
        {
            string listOfNames = "The following WeaponCore blocks are orphaned by their mod, check the SE log to see if it failed to compile: ";
            
            foreach (var cube in _brokenBlocks)
            {                
                if (cube.BlockDefinition?.Id.SubtypeName != null)
                    listOfNames += $"{cube.BlockDefinition.Context.ModName} {cube.BlockDefinition.Id.SubtypeName}, ";
            }

            if (HandlesInput)
                ShowLocalNotify(listOfNames, 30000, "White");
            Log.Line($"{listOfNames}");

            _brokenBlocks.Clear();
        }

        internal bool KeenFuckery()
        {
            try
            {
                if (HandlesInput)
                {
                    if (Session?.Player == null) return false;
                    if (PlayerId == -1)
                        PlayerId = Session.Player.IdentityId;

                    MultiplayerId = MyAPIGateway.Multiplayer.MyId;

                    if (AuthorIds.Contains(MultiplayerId))
                        AuthorConnected = true;

                    List<IMyPlayer> players = new List<IMyPlayer>();
                    MyAPIGateway.Multiplayer.Players.GetPlayers(players);

                    for (int i = 0; i < players.Count; i++)
                        PlayerConnected(players[i].IdentityId);
                }

                _pingPongPacket.SenderId = MultiplayerId;
                _pingPongPacket.PType = PacketType.PingPong;

                return true;
            }
            catch (Exception ex) { Log.Line($"Exception in UpdatingStopped: {ex} - Session:{Session != null} - Player:{Session?.Player != null} - ClientMouseState:{UiInput.ClientInputState != null}"); }

            return false;
        }

        internal void ReallyStupidKeenShit() //aka block group removal of individual blocks
        {
            var categories = MyDefinitionManager.Static.GetCategories();
            MyGuiBlockCategoryDefinition shipWeapons;
            if (!categories.TryGetValue("ShipWeapons", out shipWeapons))
                return;

            var removeDefs = new HashSet<string>();

            foreach (var weaponDef in WeaponDefinitions)
            {
                foreach (var mount in weaponDef.Assignments.MountPoints)
                {
                    var subTypeId = mount.SubtypeId;
                    removeDefs.Add(subTypeId);
                }
            }

            foreach (var item in shipWeapons.ItemIds.ToList())
            {
                var index = item.IndexOf("/", StringComparison.Ordinal);
                var subtype = item.Substring(index + 1);
                if (string.IsNullOrEmpty(subtype) || string.Equals(subtype, "(null)"))
                    subtype = item.Remove(index);

                if (removeDefs.Contains(subtype))
                {
                    shipWeapons.ItemIds.Remove(item);
                }
            }
        }

        internal void CheckToolbarForVanilla(MyCubeBlock cube)
        {
            string message = null;
            if (cube is MyShipController)
            {
                var ob = (MyObjectBuilder_ShipController)cube.GetObjectBuilderCubeBlock();
                for (int i = 0; i < ob.Toolbar.Slots.Count; i++)
                {
                    var toolbarItem = ob.Toolbar.Slots[i].Data as MyObjectBuilder_ToolbarItemWeapon;
                    if (toolbarItem != null)
                    {
                        var defId = (MyDefinitionId)toolbarItem.defId;
                        if (VanillaIds.ContainsKey(defId) || PartPlatforms.ContainsKey(defId))
                        {
                            MyVisualScriptLogicProvider.ClearToolbarSlotLocal(i, PlayerId);
                            //var index = ob.Toolbar.Slots[i].Index;
                           // message += $"*Warning* Vanilla weapon toolbar action detected in slot {index + 1}, replace with WeaponCore Group toolbar action!\n";
                        }
                    }
                }

                if (message != null)
                    ShowLocalNotify(message, 10000, "Red");


            }
        }

        private readonly HandWeaponDebugPacket _clientHandDebug = new HandWeaponDebugPacket();
        private readonly HandWeaponDebugPacket _clientHandDebug2 = new HandWeaponDebugPacket();

        private void DrawHandDebug(HandWeaponDebugPacket hDebug)
        {
            _clientHandDebug.ShootStart = hDebug.ShootStart;
            _clientHandDebug.ShootEnd = hDebug.ShootEnd;
            _clientHandDebug.HitStart = hDebug.HitStart;
            _clientHandDebug.HitEnd = hDebug.HitEnd;
            _clientHandDebug.LastHitTick = Tick;
            _clientHandDebug.LastShootTick = Tick;
        }

        private static void CounterKeenLogMessage(bool console = true)
        {
            var message = "\n***\n    [CoreSystems] Ignore log messages from keen stating 'Mod CoreSystems is accessing physics from parallel threads'\n     CS is using a thread safe parallel.for, not a parallel task\n***";
            if (console) MyLog.Default.WriteLineAndConsole(message);
            else MyLog.Default.WriteLine(message);
        }

        internal static double ModRadius(double radius, bool largeBlock)
        {
            if (largeBlock && radius < 3) radius = 3;
            else if (largeBlock && radius > 25) radius = 25;
            else if (!largeBlock && radius > 5) radius = 5;

            radius = Math.Ceiling(radius);
            return radius;
        }

        public void WeaponDebug(Weapon w)
        {
            DsDebugDraw.DrawLine(w.MyPivotTestLine, Color.Red, 0.05f);
            DsDebugDraw.DrawLine(w.MyBarrelTestLine, Color.Blue, 0.05f);
            DsDebugDraw.DrawLine(w.MyAimTestLine, Color.Black, 0.07f);
            DsDebugDraw.DrawSingleVec(w.MyPivotPos, 1f, Color.White);
            DsDebugDraw.DrawLine(w.AzimuthFwdLine.From, w.AzimuthFwdLine.To, Color.Cyan, 0.05f);
            //DsDebugDraw.DrawLine(w.MyCenterTestLine, Color.Green, 0.05f);
            //DsDebugDraw.DrawBox(w.targetBox, Color.Plum);
            //DsDebugDraw.DrawLine(w.LimitLine.From, w.LimitLine.To, Color.Orange, 0.05f);
            //if (w.Target.HasTarget)
            //DsDebugDraw.DrawLine(w.MyShootAlignmentLine, Color.Yellow, 0.05f);

        }

        private void ModChecker()
        {
            LocalVersion = ModContext.ModId == "CoreSystems" || ModContext.ModId == "WeaponCore";

            if (LocalVersion)
            {
                DebugVersion = true;
                DebugMod = true;
            }

            foreach (var mod in Session.Mods)
            {
                var modPath = mod.GetPath();
                if (!string.IsNullOrEmpty(modPath))
                    ModInfo.TryAdd(mod.GetPath(), mod);
                if (mod.PublishedFileId == 1365616918 || mod.PublishedFileId == 2372872458 || mod.PublishedFileId == 3154379105) ShieldMod = true;
                else if (mod.GetPath().Contains("AppData\\Roaming\\SpaceEngineers\\Mods\\DefenseShields") || mod.Name.StartsWith("DefenseShields") || mod.FriendlyName.StartsWith("DefenseShields"))
                    ShieldMod = true;
                else if (mod.PublishedFileId == 1931509062 || mod.PublishedFileId == 1995197719 || mod.PublishedFileId == 2006751214 || mod.PublishedFileId == 2015560129)
                    ReplaceVanilla = true;
                else if (mod.GetPath().Contains("AppData\\Roaming\\SpaceEngineers\\Mods\\VanillaReplacement") || mod.Name.Contains("WCVanilla") || mod.FriendlyName.Contains("WCVanilla"))
                    ReplaceVanilla = true;
                else if (mod.PublishedFileId == 2189703321 || mod.PublishedFileId == 2496225055 || mod.PublishedFileId == 2726343161 || mod.PublishedFileId == 2734980390)
                {
                    DebugVersion = true;
                }
                else if (mod.PublishedFileId == 2200451495 || mod.PublishedFileId == 3283226082)
                    WaterMod = true;
            }

            //SuppressWc = !SUtils.ModActivate(ModContext, Session);

            if (!SuppressWc && !ReplaceVanilla)
            {
                ContainerDefinition baseDefs;
                Parts.GetBaseDefinitions(out baseDefs);
                if (baseDefs != null)
                {
                    Parts.SetModPath(baseDefs, ModContext.ModPath);
                    PickDef(baseDefs);
                }
            }

        }

        public string ModPath()
        {
            var modPath = ModContext.ModPath;
            return modPath;
        }

        private void Paused()
        {
            _paused = true;
        }

        public bool TaskHasErrors(ref Task task, string taskName)
        {
            if (task.Exceptions != null && task.Exceptions.Length > 0)
            {
                foreach (var e in task.Exceptions)
                {
                    Log.Line($"{taskName} thread!\n{e}");
                }

                return true;
            }

            return false;
        }

        internal bool GridHasPower(MyCubeGrid grid, TopMap map = null)
        {
            bool state = false;
            if (map != null || TopEntityToInfoMap.TryGetValue(grid, out map))
            {
                var dist = (MyResourceDistributorComponent)((IMyCubeGrid)grid).ResourceDistributor;


                if (dist != null)
                {
                    state = dist.ResourceState != MyResourceStateEnum.NoPower;
                }

                map.PowerCheckTick = Tick;
                map.Powered = state;
            }
            return state;
        }

        internal void UpdateGridPowerState()
        {
            foreach (var pair in DirtyPowerGrids)
                GridHasPower(pair.Key, pair.Value);

            DirtyPowerGrids.Clear();
        }

        internal void CheckGridPowerState(MyCubeGrid grid, TopMap map)
        {
            if ((!map.Powered && Tick - map.PowerCheckTick > 600 || map.Powered && Tick - map.PowerCheckTick > 1800))
                DirtyPowerGrids.TryAdd(grid, map);
        }

        internal void NewThreat(Weapon w)
        {
            try
            {
                var topmost = ((MyEntity)w.Target.TargetObject).GetTopMostParent();
                var ai = w.Comp.MasterAi;
                var ownerId = w.BaseComp.IsBlock ? w.BaseComp.Cube.OwnerId : ai.AiOwner;
                Ai.TargetInfo info;
                if (topmost != null && ai.Construct.RootAi.Construct.PreviousTargets.Add(topmost) && ai.Targets.TryGetValue(topmost, out info))
                {
                    PlayerMap weaponOwner;
                    Players.TryGetValue(ownerId, out weaponOwner);
                    var wOwner = weaponOwner != null && !string.IsNullOrEmpty(weaponOwner.Player.DisplayName) ? $"{weaponOwner.Player.DisplayName}({ownerId})" : $"{ownerId}";
                    var weaponFaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(ownerId);
                    var wFaction = weaponFaction != null && !string.IsNullOrEmpty(weaponFaction.Name) ? $"{weaponFaction.Name}({weaponFaction.FactionId})" : "NA";

                    PlayerMap aiOwner;
                    Players.TryGetValue(ai.AiOwner, out aiOwner);
                    var aOwner = aiOwner != null && !string.IsNullOrEmpty(aiOwner.Player.DisplayName) ? $"{aiOwner.Player.DisplayName}({ai.AiOwner})" : $"{ai.AiOwner}";
                    var aiFaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(ai.AiOwner);
                    var aFaction = aiFaction != null && !string.IsNullOrEmpty(aiFaction.Name) ? $"{aiFaction.Name}({aiFaction.FactionId})" : "NA";

                    Log.Line($"New Threat Detected:{topmost.DebugName}\n - by: {w.BaseComp.TopEntity.DebugName}" +
                             $"Attacking Weapon:{w.System.PartName} " + $"[Weapon] Owner:{wOwner} - Faction:{wFaction} - Neutrals:{w.Comp.Data.Repo.Values.Set.Overrides.Neutrals} - Friends:{w.Comp.Data.Repo.Values.Set.Overrides.Friendly} - Unowned:{w.Comp.Data.Repo.Values.Set.Overrides.Unowned}\n" +
                             $"[Ai] Owner:{aOwner} - Faction:{aFaction} - Relationship:{info.EntInfo.Relationship} - ThreatLevel:{info.OffenseRating} - isFocus:{ai.Construct.RootAi.Construct.Focus.OldHasFocus}\n", "combat");
                }
            }
            catch (Exception ex) { Log.Line($"NewThreatLogging in SessionDraw: {ex}", null, true); }
        }

        public void GetSortedConstructCollection(Ai ai, MyCubeGrid targetGrid)
        {
            var rootConstruct = ai.Construct.RootAi.Construct;
            var collection = rootConstruct.ThreatCacheCollection;
            collection.Clear();
            TopMap map;
            if (targetGrid != null && !targetGrid.MarkedForClose && TopEntityToInfoMap.TryGetValue(targetGrid, out map))
            {
                foreach (var myEntity in map.GroupMap.Construct.Keys) {
                    TargetInfo info;
                    if (ai.Targets.TryGetValue(myEntity, out info))
                        collection.Add(info);
                }

                var n = collection.Count;
                for (int i = 1; i < n; ++i)
                {
                    var key = collection[i];
                    var j = i - 1;

                    while (j >= 0 && (int)collection[j].OffenseRating > key.OffenseRating)
                    {
                        collection[j + 1] = collection[j];
                        j -= 1;
                    }
                    collection[j + 1] = key;
                }
            }
        }

        internal MyEntity CreatePhantomEntity(string phantomType, uint maxAge = 0, bool closeWhenOutOfAmmo = false, long defaultReloads = int.MaxValue, string ammoName = null, Trigger trigger = Off, float? modelScale = null, MyEntity parent = null, bool addToPrunning = false, bool shadows = false, long identity = 0, bool sync = false)
        {
            if (!Inited) lock (InitObj) Init();

            var ent = new MyEntity            {
                DefinitionId = CoreSystemsDefs[phantomType],
                Render = {CastShadows = shadows },
                IsPreview = !sync,
                Save = false,
                SyncFlag = sync,
                NeedsWorldMatrix = sync,
            };

            if (!addToPrunning)
                ent.Flags |= EntityFlags.IsNotGamePrunningStructureObject;

            var comp = (Weapon.WeaponComponent)InitComp(ent, ref ent.DefinitionId);

            if (comp == null)
            {
                Log.Line($"phantom comp failed to create");
                return null;
            }

            //Log.Line($"phantom entityId 1: {ent.EntityId} - flags:{ent.Flags}");

            string model = null;
            if (ModelMaps.TryGetValue(phantomType, out model) || parent != null || sync) 
                ent.Init(null, model, parent, modelScale, null);
            
            //Log.Line($"phantom entityId 2: {ent.EntityId} - flags:{ent.Flags}");

            if (sync)
                ent.CreateSync();

            //Log.Line($"phantom entityId 3: {ent.EntityId} - flags:{ent.Flags}");

            if (ent.EntityId == 0)
            {
                Log.Line($"invalid phantom entityId");
                return null;
            }

            MyEntities.Add(ent);

            Dictionary<long, Weapon.WeaponComponent> phantoms;
            //Log.Line($"phantom entityId 2: {ent.EntityId}");
            if (PhantomDatabase.TryGetValue(phantomType, out phantoms))
                phantoms[ent.EntityId] = comp;
            else
            {
                Log.Line($"phantom failed to be created");
                return null;
            }

            Dictionary<string, WeaponSystem.AmmoType> ammoMap;

            WeaponSystem.AmmoType ammoType;
            if (ammoName != null && AmmoMaps.TryGetValue(phantomType, out ammoMap) && ammoMap.TryGetValue(ammoName, out ammoType))
                comp.DefaultAmmoId = ammoType.AmmoDef.Const.AmmoIdxPos;

            comp.DefaultReloads = (int)defaultReloads;

            comp.DefaultTrigger = trigger;
            comp.HasCloseConsition = closeWhenOutOfAmmo;
            comp.CustomIdentity = identity;
            comp.PhantomType = phantomType;
            if (maxAge > 0)
                FutureEvents.Schedule(comp.ForceClose, phantomType, maxAge);

            return ent;
        }

        private void InitDelayedHandWeapons()
        {
            foreach (var rifle in DelayedHandWeaponsSpawn.Keys)
            {
                MyCharacterWeaponPositionComponent weaponPosComp;
                if (rifle.AmmoInventory?.Entity == null || rifle.Owner == null || !rifle.Owner.Components.TryGet(out weaponPosComp) || Vector3D.IsZero(weaponPosComp.LogicalPositionWorld))
                    continue;

                byte value;
                DelayedHandWeaponsSpawn.TryRemove(rifle, out value);

                MyDefinitionId? def = rifle.PhysicalItemId;
                InitComp((MyEntity)rifle, ref def);
            }
        }

        public enum CubeTypes
        {
            All,
            Slims,
            Fats,
        }

        internal void DeferredPlayerLocks()
        {
            foreach (var p in DeferredPlayerLock)
            {
                int retries;
                if (DeferredPlayerLock.TryGetValue(p.Key, out retries) && retries > 1)
                {
                    if (!Ai.Constructs.UpdatePlayerLockState(p.Key))
                        DeferredPlayerLock[p.Key] = retries - 1;
                    else
                        DeferredPlayerLock.TryRemove(p.Key, out retries);
                }
                else
                {
                    DeferredPlayerLock.TryRemove(p.Key, out retries);
                }
            }
        }

        internal void UpdateLocalCharacterInfo()
        {
            LocalCharacter = Session.Player?.Character;
            if (LocalCharacter != null)
            {
                PlayerPos = LocalCharacter.WorldAABB.Center;

                MyTargetFocusComponent tComp;
                if (Tick20 && (!DeferredPlayerLock.IsEmpty || LocalCharacter.Components.TryGet(out tComp) && tComp.FocusSearchMaxDistance > 0 && DeferredPlayerLock.TryAdd(PlayerId, 120)))
                    DeferredPlayerLocks();
            }
            else
                PlayerPos = Vector3D.Zero;
        }

        public static void GetCubesInRange(MyCubeGrid grid, MyCubeBlock rootBlock, int cubeDistance, HashSet<MyCube> resultSet, out Vector3I min, out Vector3I max, CubeTypes types = CubeTypes.All)
        {
            resultSet.Clear();
            min = rootBlock.Min - cubeDistance;
            max = rootBlock.Max + cubeDistance;
            var gridMin = grid.Min;
            var gridMax = grid.Max;

            Vector3I.Max(ref min, ref gridMin, out min);
            Vector3I.Min(ref max, ref gridMax, out max);

            var iter = new Vector3I_RangeIterator(ref min, ref max);

            var next = rootBlock.Position;
            while (iter.IsValid()) {

                MyCube myCube;
                if (grid.TryGetCube(next, out myCube) && myCube.CubeBlock != rootBlock.SlimBlock) {

                    var slim = (IMySlimBlock)myCube.CubeBlock;

                    if (next == slim.Position) {

                        if (types == CubeTypes.Slims && slim.FatBlock == null)
                            resultSet.Add(myCube);
                        else if (types == CubeTypes.Fats && slim.FatBlock != null)
                            resultSet.Add(myCube);
                        else if (types == CubeTypes.All)
                            resultSet.Add(myCube);
                    }
                }
                iter.GetNext(out next);
            }
        }

        private void ColorAreas()
        {
            var color = ColorArmorToggle ? SUtils.ColorToHSVOffset(Color.Black) : SUtils.ColorToHSVOffset(Color.OrangeRed);
            foreach (var enhancer in DisplayAffectedArmor)
            {
                var grid = enhancer.BaseComp.Ai.GridEntity;
                foreach (var pair in enhancer.BlockColorBackup)
                {
                    if (!pair.Key.IsDestroyed)
                        grid.ChangeColorAndSkin(pair.Value.MyCube.CubeBlock, color);
                }
            }
            ColorArmorToggle = !ColorArmorToggle;
        }

        private void ResetVisualAreas()
        {
            foreach (var enhancer in DisplayAffectedArmor)
            {
                var grid = enhancer.BaseComp.Ai.GridEntity;
                foreach (var pair in enhancer.BlockColorBackup)
                {
                    if (!pair.Key.IsDestroyed)
                        grid.ChangeColorAndSkin(pair.Value.MyCube.CubeBlock, pair.Value.OriginalColor, pair.Value.OriginalSkin);
                }
            }
        }

        public void CalculateRestrictedShapes(MyStringHash subtype, MyOrientedBoundingBoxD cubeBoundingBox, out MyOrientedBoundingBoxD restrictedBox, out BoundingSphereD restrictedSphere)
        {
            restrictedSphere = new BoundingSphereD();
            restrictedBox = new MyOrientedBoundingBoxD();

            if (!AreaRestrictions.ContainsKey(subtype))
                return;

            AreaRestriction restriction = AreaRestrictions[subtype];
            if (restriction.RestrictionBoxInflation < 0.1 && restriction.RestrictionRadius < 0.1)
                return;

            bool checkBox = restriction.RestrictionBoxInflation > 0;
            bool checkSphere = restriction.RestrictionRadius > 0;

            if (checkBox)
            {
                restrictedBox = new MyOrientedBoundingBoxD(cubeBoundingBox.Center, cubeBoundingBox.HalfExtent, cubeBoundingBox.Orientation);
                restrictedBox.HalfExtent = restrictedBox.HalfExtent + new Vector3D(Math.Sign(restrictedBox.HalfExtent.X) * restriction.RestrictionBoxInflation, Math.Sign(restrictedBox.HalfExtent.Y) * restriction.RestrictionBoxInflation, Math.Sign(restrictedBox.HalfExtent.Z) * restriction.RestrictionBoxInflation);
            }
            if (checkSphere)
            {
                restrictedSphere = new BoundingSphereD(cubeBoundingBox.Center, restriction.RestrictionRadius);
            }
        }

        public bool IsPartAreaRestricted(MyStringHash subtype, MyOrientedBoundingBoxD cubeBoundingBox, MyCubeGrid myGrid, long ignoredEntity, Ai newAi, out MyOrientedBoundingBoxD restrictedBox, out BoundingSphereD restrictedSphere)
        {
            _tmpNearByBlocks.Clear();
            Ai ai;
            if (newAi == null)
            {
                if (!EntityToMasterAi.ContainsKey(myGrid))
                {
                    restrictedSphere = new BoundingSphereD();
                    restrictedBox = new MyOrientedBoundingBoxD();
                    return false;
                }
                ai = EntityToMasterAi[myGrid];
            } else
            {
                ai = newAi;
            }

            CalculateRestrictedShapes(subtype, cubeBoundingBox, out restrictedBox, out restrictedSphere);
            AreaRestriction restriction;
            if (AreaRestrictions.ContainsKey(subtype))
                restriction = AreaRestrictions[subtype];
            else
                return false;
            var queryRadius = Math.Max(Math.Max(restrictedBox.HalfExtent.AbsMax(), restrictedSphere.Radius), cubeBoundingBox.HalfExtent.AbsMax());
            foreach (KeyValuePair<MyStringHash, AreaRestriction> otherRestrictions in AreaRestrictions)
            {
                if (otherRestrictions.Value.MaxSize > queryRadius)
                    queryRadius = otherRestrictions.Value.MaxSize;
            }
            
            if (queryRadius < 0.01)
                return false;
            
            var checkBox = restriction.RestrictionBoxInflation > 0;
            var checkSphere = restriction.RestrictionRadius > 0;
            var querySphere = new BoundingSphereD(cubeBoundingBox.Center, queryRadius);

            myGrid.Hierarchy.QuerySphere(ref querySphere, _tmpNearByBlocks);

            foreach (var grid in ai.SubGridCache) {
                if (grid == myGrid || !EntityAIs.ContainsKey(grid))
                    continue;
                grid.Hierarchy.QuerySphere(ref querySphere, _tmpNearByBlocks);
            }

            for (int l = 0; l < _tmpNearByBlocks.Count; l++) {

                var cube = _tmpNearByBlocks[l] as MyCubeBlock;
                var searchLight = cube as IMySearchlight;
                if (cube == null || cube.EntityId == ignoredEntity || searchLight != null || !CoreSystemsDefs.ContainsKey(cube.BlockDefinition.Id.SubtypeId.String))
                    continue;
                var cubeSubtype = cube.BlockDefinition.Id.SubtypeId;
                MyOrientedBoundingBoxD cubeBox;
                SUtils.GetBlockOrientedBoundingBox(cube, out cubeBox);
                if (AreaRestrictions.ContainsKey(cubeSubtype))
                {
                    AreaRestriction localrestriction = AreaRestrictions[cubeSubtype];
                    if(localrestriction.CheckForAnyPart || cube.BlockDefinition.Id.SubtypeId == subtype)
                    {
                        var localrestrictedSphere = new BoundingSphereD();
                        var localrestrictedBox = new MyOrientedBoundingBoxD();
                        CalculateRestrictedShapes(cubeSubtype, cubeBox, out localrestrictedBox, out localrestrictedSphere);
                        var localcheckBox = localrestriction.RestrictionBoxInflation > 0;
                        var localcheckSphere = localrestriction.RestrictionRadius > 0;
                        if (localcheckBox && localrestrictedBox.Contains(ref cubeBoundingBox) != ContainmentType.Disjoint)
                            return true;
                        if (localcheckSphere && cubeBoundingBox.Contains(ref localrestrictedSphere) != ContainmentType.Disjoint)
                            return true;
                    }
                }

                if (!restriction.CheckForAnyPart && cube.BlockDefinition.Id.SubtypeId != subtype)
                    continue;

                if (checkBox && restrictedBox.Contains(ref cubeBox) != ContainmentType.Disjoint)
                    return true;

                if (checkSphere && cubeBox.Contains(ref restrictedSphere) != ContainmentType.Disjoint)
                    return true;
            }
            return false;
        }

        internal MyEntity TriggerEntityActivator()
        {
            var ent = new MyEntity();
            ent.Init(null, TriggerEntityModel, null, null);
            ent.Render.CastShadows = false;
            ent.IsPreview = true;
            ent.Save = false;
            ent.SyncFlag = false;
            ent.NeedsWorldMatrix = false;
            ent.Flags |= EntityFlags.IsNotGamePrunningStructureObject;
            MyEntities.Add(ent);

            ent.PositionComp.SetWorldMatrix(ref MatrixD.Identity, null, false, false, false);
            ent.InScene = false;
            ent.Render.RemoveRenderObjects();
            return ent;
        }

        internal void TriggerEntityClear(MyEntity myEntity)
        {
            myEntity.PositionComp.SetWorldMatrix(ref MatrixD.Identity, null, false, false, false);
            myEntity.InScene = false;
            myEntity.Render.RemoveRenderObjects();
        }

        internal void LoadVanillaData()
        {
            var smallMissileId = new MyDefinitionId(typeof(MyObjectBuilder_SmallMissileLauncher), null);
            var smallGatId = new MyDefinitionId(typeof(MyObjectBuilder_SmallGatlingGun), null);
            var largeGatId = new MyDefinitionId(typeof(MyObjectBuilder_LargeGatlingTurret), null);
            var largeMissileId = new MyDefinitionId(typeof(MyObjectBuilder_LargeMissileTurret), null);

            var smallMissile = MyStringHash.GetOrCompute("SmallMissileLauncher");
            VanillaIds[smallMissileId] = smallMissile;
            VanillaCoreIds[smallMissile] = smallMissileId;

            var smallGat = MyStringHash.GetOrCompute("SmallGatlingGun");
            VanillaIds[smallGatId] = smallGat;
            VanillaCoreIds[smallGat] = smallGatId;

            var largeGat = MyStringHash.GetOrCompute("LargeGatlingTurret");
            VanillaIds[largeGatId] = largeGat;
            VanillaCoreIds[largeGat] = largeGatId;

            var largeMissile = MyStringHash.GetOrCompute("LargeMissileTurret");
            VanillaIds[largeMissileId] = largeMissile;
            VanillaCoreIds[largeMissile] = largeMissileId;

            ///
            ///
            /// 
            
            var intTurret = MyStringHash.GetOrCompute("LargeInteriorTurret");
            var intTurretId = new MyDefinitionId(typeof(MyObjectBuilder_InteriorTurret), "LargeInteriorTurret");
            VanillaIds[intTurretId] = intTurret;
            VanillaCoreIds[intTurret] = intTurretId;

            var largeMissileMedCal = MyStringHash.GetOrCompute("LargeBlockMediumCalibreTurret");
            var largeMissileMedCalId = new MyDefinitionId(typeof(MyObjectBuilder_LargeMissileTurret), "LargeBlockMediumCalibreTurret");
            VanillaIds[largeMissileMedCalId] = largeMissileMedCal;
            VanillaCoreIds[largeMissileMedCal] = largeMissileMedCalId;


            var smallLargeMissile = MyStringHash.GetOrCompute("LargeMissileLauncher");
            var smallLargeMissileId = new MyDefinitionId(typeof(MyObjectBuilder_SmallMissileLauncher), "LargeMissileLauncher");
            VanillaIds[smallLargeMissileId] = smallLargeMissile;
            VanillaCoreIds[smallLargeMissile] = smallLargeMissileId;


            var smallLargeMissileReload = MyStringHash.GetOrCompute("LargeRailgun");
            var smallLargeMissileReloadId = new MyDefinitionId(typeof(MyObjectBuilder_SmallMissileLauncherReload), "LargeRailgun");
            VanillaIds[smallLargeMissileReloadId] = smallLargeMissileReload;
            VanillaCoreIds[smallLargeMissileReload] = smallLargeMissileReloadId;

            var smallLargeMissileLargeCal = MyStringHash.GetOrCompute("LargeBlockLargeCalibreGun");
            var smallLargeMissileLargeCalId = new MyDefinitionId(typeof(MyObjectBuilder_SmallMissileLauncher), "LargeBlockLargeCalibreGun");
            VanillaIds[smallLargeMissileLargeCalId] = smallLargeMissileLargeCal;
            VanillaCoreIds[smallLargeMissileLargeCal] = smallLargeMissileLargeCalId;

            var largeCalTurret = MyStringHash.GetOrCompute("LargeCalibreTurret");
            var largeCalTurretId = new MyDefinitionId(typeof(MyObjectBuilder_LargeMissileTurret), "LargeCalibreTurret");
            VanillaIds[largeCalTurretId] = largeCalTurret;
            VanillaCoreIds[largeCalTurret] = largeCalTurretId;

            var smallLargeMissile2 = MyStringHash.GetOrCompute("SmallMissileLauncherWarfare2");
            var smallLargeMissile2Id = new MyDefinitionId(typeof(MyObjectBuilder_SmallMissileLauncher), "SmallMissileLauncherWarfare2");
            VanillaIds[smallLargeMissile2Id] = smallLargeMissile2;
            VanillaCoreIds[smallLargeMissile2] = smallLargeMissile2Id;

            var smallAutoTurret = MyStringHash.GetOrCompute("AutoCannonTurret");
            var smallAutoTurretId = new MyDefinitionId(typeof(MyObjectBuilder_LargeGatlingTurret), "AutoCannonTurret");
            VanillaIds[smallAutoTurretId] = smallAutoTurret;
            VanillaCoreIds[smallAutoTurret] = smallAutoTurretId;

            var smallMedCalGun = MyStringHash.GetOrCompute("SmallBlockMediumCalibreGun");
            var smallMedCalGunId = new MyDefinitionId(typeof(MyObjectBuilder_SmallMissileLauncherReload), "SmallBlockMediumCalibreGun");
            VanillaIds[smallMedCalGunId] = smallMedCalGun;
            VanillaCoreIds[smallMedCalGun] = smallMedCalGunId;

            var smallGatAuto = MyStringHash.GetOrCompute("SmallBlockAutocannon");
            var smallGatAutoId = new MyDefinitionId(typeof(MyObjectBuilder_SmallGatlingGun), "SmallBlockAutocannon");
            VanillaIds[smallGatAutoId] = smallGatAuto;
            VanillaCoreIds[smallGatAuto] = smallGatAutoId;

            var smallRocketReload = MyStringHash.GetOrCompute("SmallRocketLauncherReload");
            var smallRocketReloadId = new MyDefinitionId(typeof(MyObjectBuilder_SmallMissileLauncherReload), "SmallRocketLauncherReload");
            VanillaIds[smallRocketReloadId] = smallRocketReload;
            VanillaCoreIds[smallRocketReload] = smallRocketReloadId;

            var smallGat2 = MyStringHash.GetOrCompute("SmallGatlingGunWarfare2");
            var smallGat2Id = new MyDefinitionId(typeof(MyObjectBuilder_SmallGatlingGun), "SmallGatlingGunWarfare2");
            VanillaIds[smallGat2Id] = smallGat2;
            VanillaCoreIds[smallGat2] = smallGat2Id;

            //BDC Add crap here
            var largeSearch = MyStringHash.GetOrCompute("LargeSearchlight");
            var largeSearchId = new MyDefinitionId(typeof(MyObjectBuilder_SearchlightDefinition), "LargeSearchlight");
            VanillaIds[largeSearchId] = largeSearch;
            VanillaCoreIds[largeSearch] = largeSearchId;


            foreach (var pair in VanillaCoreIds)
                VanillaSubtypes.Add(pair.Key.String);

            VanillaSubpartNames.Add("InteriorTurretBase1");
            VanillaSubpartNames.Add("InteriorTurretBase2");
            VanillaSubpartNames.Add("MissileTurretBase1");
            VanillaSubpartNames.Add("MissileTurretBarrels");
            VanillaSubpartNames.Add("GatlingTurretBase1");
            VanillaSubpartNames.Add("GatlingTurretBase2");
            VanillaSubpartNames.Add("GatlingBarrel");
        }
        
        internal void UpdateEnforcement()
        {

            foreach (var platform in PartPlatforms)
            {
                var core = platform.Value;
                if (core.StructureType != CoreStructure.StructureTypes.Weapon) continue;

                foreach (var system in core.PartSystems)
                {
                    var part = (WeaponSystem)system.Value;

                    CoreSettings.ServerSettings.WeaponOverride wepOverride;
                    if (WeaponValuesMap.TryGetValue(part.Values, out wepOverride) && wepOverride != null)
                    {
                        part.WConst = new WeaponConstants(part.Values);
                    }

                    for (int i = 0; i < part.AmmoTypes.Length; i++)
                    {
                        var ammo = part.AmmoTypes[i];

                        CoreSettings.ServerSettings.AmmoOverride ammoOverride;
                        if (AmmoValuesMap.TryGetValue(ammo.AmmoDef, out ammoOverride) && ammoOverride != null)
                        {
                            ammo.AmmoDef.Const = new AmmoConstants(ammo, part.Values, part, i);
                            part.WConst.HasServerOverrides = true;
                        }
                    }
                }
            }

            var armors = Settings.Enforcement.DefinitionOverrides?.ArmorOverrides;
            if (ArmorCoreActive && armors != null && armors.Length > 0)
            {
                for (int j = 0; j < armors.Length; j++)
                {
                    var armor = armors[j];
                    for (int k = 0; k < armor.SubtypeIds.Length; k++)
                    {
                        var subtype = MyStringHash.GetOrCompute(armor.SubtypeIds[k]);
                        
                        ResistanceValues values;
                        if (!ArmorCoreBlockMap.TryGetValue(subtype, out values))
                            continue;

                        if (armor.KineticResistance.HasValue) values.KineticResistance = armor.KineticResistance.Value;
                        if (armor.EnergeticResistance.HasValue) values.EnergeticResistance = armor.EnergeticResistance.Value;

                        ArmorCoreBlockMap[subtype] = values;
                    }
                }
            }

            Log.Line($"WC Version: {ModVersion}");
            if (IsClient && Settings.Enforcement.Version > ModVersion)
                WarnClientAboutOldVersion();
        }

        private void WarnClientAboutOldVersion()
        {
            var message = "Your WeaponCore version is [older than the servers]!  This is likely due to a [corrupted download], please follow directions on [WC Steam Page] to correct";
            ShowLocalNotify(message, 30000);
            Log.Line(message);
        }
    }
}
