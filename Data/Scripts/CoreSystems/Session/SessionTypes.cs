using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using CoreSystems.Platform;
using CoreSystems.Support;
using ProtoBuf;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Weapons;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;
using static CoreSystems.Platform.Part;
using static CoreSystems.Support.WeaponSystem;

namespace CoreSystems
{
    public partial class Session
    {
        internal struct DebugLine
        {
            internal enum LineType
            {
                MainTravel,
                MainHit,
                ShrapnelTravel,
                ShrapnelHit,
            }

            internal LineType Type;
            internal LineD Line;
            internal uint StartTick;

            internal bool Draw(uint tick)
            {
                Color color = new Color();
                switch (Type)
                {
                    case LineType.MainTravel:
                        color = Color.Blue;
                        break;
                    case LineType.MainHit:
                        color = Color.Red;
                        break;
                    case LineType.ShrapnelTravel:
                        color = Color.Green;
                        break;
                    case LineType.ShrapnelHit:
                        color = Color.Orange;
                        break;
                }

                DsDebugDraw.DrawLine(Line, color, 0.1f);

                return tick - StartTick < 1200;
            }
        }

        public struct RadiatedBlock
        {
            public IMySlimBlock Slim;
            public int Distance;
        }


        internal struct LosDebug
        {
            internal Part Part;
            internal LineD Line;
            internal uint HitTick;
        }

        internal class CubeCompare : IComparer<MyCubeBlock>
        {
            public int Compare(MyCubeBlock x, MyCubeBlock y)
            {
                return x.EntityId.CompareTo(y.EntityId);
            }
        }

        internal class ProblemReport
        {
            internal readonly Dictionary<string, Dictionary<string, Func<string>>> AllDicts = new Dictionary<string, Dictionary<string, Func<string>>>();
            internal readonly Dictionary<string, Func<string>> SessionFields;
            internal readonly Dictionary<string, Func<string>> AiFields;
            internal readonly Dictionary<string, Func<string>> CompFields;
            internal readonly Dictionary<string, Func<string>> PlatformFields;
            internal readonly Dictionary<string, Func<string>> WeaponFields;

            internal bool Generating;
            internal MyEntity TargetEntity;
            internal MyEntity TargetTopEntity;
            internal DataReport MyData;
            internal DataReport RemoteData;
            internal CorePlatform TmpPlatform;
            internal string Report;
            internal uint RequestTime = 1800;
            internal uint LastRequestTick = uint.MaxValue - 7200;

            internal ProblemReport()
            {
                SessionFields = InitSessionFields();
                AiFields = InitAiFields();
                CompFields = InitCompFields();
                PlatformFields = InitPlatformFields();
                WeaponFields = InitWeaponFields();

                AllDicts.Add("Session", SessionFields);
                AllDicts.Add("Ai", AiFields);
                AllDicts.Add("BaseComp", CompFields);
                AllDicts.Add("Platform", PlatformFields);
                AllDicts.Add("Weapon", WeaponFields);
            }

            internal void GenerateReport(MyEntity targetEntity)
            {
                var topMost = targetEntity.GetTopMostParent();
                var cube = targetEntity as MyCubeBlock;
                var rifle = targetEntity as IMyAutomaticRifleGun;
                var subName = cube != null ? cube.BlockDefinition.Id.SubtypeName : rifle != null ? rifle.DefinitionId.SubtypeName : "Unknown";

                Ai ai;
                if (Generating || I.Tick - LastRequestTick < RequestTime || topMost == null)
                {
                    return;
                }

                if (!I.EntityAIs.TryGetValue(topMost, out ai) || !ai.CompBase.ContainsKey(targetEntity))
                    Log.Line("Failed to generate user report, either grid does not have Weaponcore or this block this wc block is not initialized.");

                Log.Line("Generate User Weapon Report");
                Generating = true;
                LastRequestTick = I.Tick;
                TargetEntity = targetEntity;
                TargetTopEntity = topMost;
                
                MyData = new DataReport();

                if (!I.DedicatedServer)
                    MyAPIGateway.Utilities.ShowNotification($"Generating a error report for WC Block: {subName} - with id: {TargetEntity.EntityId}", 7000, "Red");

                if (I.IsServer)
                {

                    Compile();
                    if (I.MpActive)
                    {
                        foreach (var player in I.Players)
                            NetworkTransfer(false, player.Value.Player.SteamUserId);
                    }
                }
                else
                {
                    Compile();
                    NetworkTransfer(true);
                }
                I.FutureEvents.Schedule(CompleteReport, null, 300);
            }

            internal DataReport PullData(MyEntity targetEntity)
            {
                MyData = new DataReport();
                TargetEntity = targetEntity;
                TargetTopEntity = targetEntity.GetTopMostParent();

                Compile();

                return MyData;
            }

            internal void Compile()
            {
                try
                {
                    BuildData(MyData);
                }
                catch (Exception ex) { Log.Line($"Exception in ReportCompile: {ex}", null, true); }
            }

            internal void BuildData(DataReport data)
            {
                foreach (var d in AllDicts)
                {
                    foreach (var f in d.Value)
                    {
                        var value = f.Value.Invoke();
                        GetStorage(data, d.Key)[f.Key] = value;
                    }
                }
            }


            internal string[] IndexToString = { "Session", "Ai", "Platform", "BaseComp", "Weapon" };
            internal Dictionary<string, string> GetStorage(DataReport data, string storageName)
            {
                switch (storageName)
                {
                    case "Session":
                        return data.Session;
                    case "Ai":
                        return data.Ai;
                    case "BaseComp":
                        return data.Comp;
                    case "Platform":
                        return data.Platform;
                    case "Weapon":
                        return data.Weapon;
                    default:
                        return null;
                }
            }

            internal void NetworkTransfer(bool toServer, ulong clientId = 0, DataReport data = null)
            {
                if (toServer)
                {
                    I.PacketsToServer.Add(new ProblemReportPacket
                    {
                        SenderId = I.MultiplayerId,
                        PType = PacketType.ProblemReport,
                        EntityId = TargetEntity.EntityId,
                        Type = ProblemReportPacket.RequestType.RequestServerReport,
                    });
                }
                else
                {
                    I.PacketsToClient.Add(new PacketInfo
                    {
                        Packet = new ProblemReportPacket
                        {
                            SenderId = clientId,
                            PType = PacketType.ProblemReport,
                            Data = data,
                            Type = ProblemReportPacket.RequestType.SendReport,

                        },
                        SingleClient = true,
                    });
                }
            }

            internal void CompleteReport(object o)
            {
                if (I.MpActive && (RemoteData == null || MyData == null))
                {
                    Log.Line($"RemoteData:{RemoteData != null} - MyData:{MyData != null}, null data detected, waiting 10 second");
                    Clean();
                    return;
                }
                CompileReport();

                Log.CleanLine($"{Report}", "report");

                Clean();
            }

            internal void CompileReport()
            {
                Report = string.Empty;
                var myRole = !I.MpActive ? "" : I.IsClient ? "Client:" : "Server:";
                var otherRole = !I.MpActive ? "" : I.IsClient ? "Server:" : "Client:";
                var loopCnt = I.MpActive ? 2 : 1;
                var lastLoop = loopCnt > 1 ? 1 : 0;

                for (int x = 0; x < loopCnt; x++)
                {

                    if (x != lastLoop)
                        Report += "\n== Mismatched variables ==\n";
                    else if (x == lastLoop && lastLoop > 0)
                        Report += "== End of mismatch section ==\n\n";

                    for (int i = 0; i < 5; i++)
                    {
                        var indexString = IndexToString[i];
                        var myStorage = GetStorage(MyData, indexString);
                        var storageCnt = I.MpActive ? 2 : 1;
                        Report += $"Class: {indexString}\n";

                        foreach (var p in myStorage)
                        {
                            if (storageCnt > 1)
                            {
                                var remoteStorage = GetStorage(RemoteData, indexString);
                                var remoteValue = remoteStorage[p.Key];
                                if (x == lastLoop) Report += $"    [{p.Key}]\n      {myRole}{p.Value} - {otherRole}{remoteValue} - Matches:{p.Value == remoteValue}\n";
                                else if (p.Value != remoteValue && !MatchSkip.Contains(p.Key)) Report += $"    [{p.Key}]\n      {myRole}{p.Value} - {otherRole}{remoteValue}\n";
                            }
                            else
                            {
                                if (x == lastLoop) Report += $"    [{p.Key}]\n      {myRole}{p.Value}\n";
                            }
                        }
                    }
                }
            }

            internal HashSet<string> MatchSkip = new HashSet<string> { "AcquireEnabled", "AcquireAsleep", "WeaponReadyTick", "AwakeComps" };

            internal Dictionary<string, Func<string>> InitSessionFields()
            {
                var sessionFields = new Dictionary<string, Func<string>>
                {
                    {"HasGridMap", () => (GetComp() != null && I.TopEntityToInfoMap.ContainsKey(GetComp().TopEntity)).ToString()},
                    {"HasGridAi", () => (GetComp() != null && I.EntityAIs.ContainsKey(GetComp().TopEntity)).ToString()},
                };

                return sessionFields;
            }

            internal Dictionary<string, Func<string>> InitAiFields()
            {
                var aiFields = new Dictionary<string, Func<string>>
                {
                    {"Version", () => GetAi()?.Version.ToString() ?? string.Empty },
                    {"RootAiId", () => GetAi()?.Construct.RootAi?.TopEntity.EntityId.ToString() ?? string.Empty },
                    {"SubGrids", () => GetAi()?.SubGridCache.Count.ToString() ?? string.Empty },
                    {"AiSleep", () => GetAi()?.AiSleep.ToString() ?? string.Empty },
                    {"AiIsPowered", () => GetAi()?.HasPower.ToString() ?? string.Empty },
                    {"AiInit", () => GetAi()?.AiInit.ToString() ?? string.Empty },
                    {"ControllingPlayers", () => GetAi()?.Construct.ControllingPlayers.Count.ToString() ?? string.Empty },
                    {"Inventories", () => GetAi()?.InventoryMonitor.Count.ToString() ?? string.Empty },
                    {"SortedTargets", () => GetAi()?.SortedTargets.Count.ToString() ?? string.Empty },
                    {"Obstructions", () => GetAi()?.Obstructions.Count.ToString() ?? string.Empty },
                    {"NearByEntities", () => GetAi()?.NearByEntities.ToString() ?? string.Empty },
                    {"TargetAis", () => GetAi()?.TargetAis.Count.ToString() ?? string.Empty },
                    {"WeaponBase", () => GetAi()?.CompBase.Count.ToString() ?? string.Empty },
                    {"PriorityRangeSqr", () => GetAi()?.DetectionInfo.PriorityRangeSqr.ToString("0.####", CultureInfo.InvariantCulture) ?? string.Empty },
                    {"AiOwner", () => GetAi()?.AiOwner.ToString() ?? string.Empty },
                    {"AwakeComps", () => GetAi()?.AwakeComps.ToString() ?? string.Empty },
                    {"NumOfParts", () => GetAi()?.PartCount.ToString() ?? string.Empty },
                    {"PartTracking", () => GetAi()?.WeaponsTracking.ToString() ?? string.Empty },
                    {"GridAvailablePower", () => GetAi()?.GridAvailablePower.ToString(CultureInfo.InvariantCulture) ?? string.Empty },
                    {"MaxTargetingRange", () => GetAi()?.MaxTargetingRange.ToString(CultureInfo.InvariantCulture) ?? string.Empty },
                };

                return aiFields;
            }

            internal Dictionary<string, Func<string>> InitCompFields()
            {
                var compFields = new Dictionary<string, Func<string>>
                {
                    {"IsAsleep", () => GetComp()?.IsAsleep.ToString() ?? string.Empty },
                    {"GridId", () => GetComp()?.TopEntity.EntityId.ToString() ?? string.Empty },
                    {"TypeSpecific", () => GetComp()?.TypeSpecific.ToString() ?? string.Empty },
                    {"AiGridMatchCubeGrid", () => (GetComp()?.Ai?.TopEntity == GetComp()?.TopEntity).ToString() ?? string.Empty },
                    {"IsWorking", () => GetComp()?.IsWorking.ToString() ?? string.Empty },
                    {"entityIsWorking", () => GetComp()?.FakeIsWorking.ToString() ?? string.Empty },
                    {"MaxDetectDistance", () => GetComp()?.MaxDetectDistance.ToString(CultureInfo.InvariantCulture) ?? string.Empty },
                    {"Status", () => GetComp()?.Status.ToString() ?? string.Empty },
                    //{"ControlType", () => GetComp()?.Data.ProtoRepo.Values.PartState.Control.ToString() ?? string.Empty },
                    //{"PlayerId", () => GetComp()?.Data.ProtoRepo.Values.PartState.PlayerId.ToString() ?? string.Empty },
                    //{"FocusSubSystem", () => GetComp()?.BaseData.ProtoRepoBase.Values.Set.Overrides.FocusSubSystem.ToString() ?? string.Empty },
                    //{"FocusTargets", () => GetComp()?.BaseData.ProtoRepoBase.Values.Set.Overrides.FocusTargets.ToString() ?? string.Empty },
                    //{"MaxSize", () => GetComp()?.BaseData.ProtoRepoBase.Values.Set.Overrides.MaxSize.ToString() ?? string.Empty },
                   //{"MinSize", () => GetComp()?.BaseData.ProtoRepoBase.Values.Set.Overrides.MinSize.ToString() ?? string.Empty },
                   //{"CameraGroup", () => GetComp()?.BaseData.ProtoRepoBase.Values.Set.Overrides.CameraGroup.ToString() ?? string.Empty },
                };

                return compFields;
            }

            internal Dictionary<string, Func<string>> InitPlatformFields()
            {
                var platformFields = new Dictionary<string, Func<string>>
                {
                    {"PartState", () => GetPlatform()?.State.ToString() ?? string.Empty },
                };

                return platformFields;
            }

            internal Dictionary<string, Func<string>> InitWeaponFields()
            {
                var weaponFields = new Dictionary<string, Func<string>>
                {
                    {"TurretController", () => {
                        var message = string.Empty;
                        return !TryGetValidPlatform(out TmpPlatform) ? string.Empty : TmpPlatform.Weapons.Aggregate(message, (current, w) => current + $"{w.TurretController}"); }
                    },
                    {"AcquireEnabled", () => {
                            var message = string.Empty;
                            return !TryGetValidPlatform(out TmpPlatform) ? string.Empty : TmpPlatform.Weapons.Aggregate(message, (current, w) => current + $"{w.Acquire.Monitoring}"); }
                    },
                    {"AcquireAsleep", () => {
                            var message = string.Empty;
                            return !TryGetValidPlatform(out TmpPlatform) ? string.Empty : TmpPlatform.Weapons.Aggregate(message, (current, w) => current + $"{w.Acquire.IsSleeping}"); }
                    },
                    {"MaxDetectDistance", () => {
                            var message = string.Empty;
                            return !TryGetValidPlatform(out TmpPlatform) ? string.Empty : TmpPlatform.Weapons.Aggregate(message, (current, w) => current + $"{w.MaxTargetDistance}"); }
                    },
                    {"AmmoName", () => {
                            var message = string.Empty;
                            return !TryGetValidPlatform(out TmpPlatform) ? string.Empty : TmpPlatform.Weapons.Aggregate(message, (current, w) => current + $"{w.ActiveAmmoDef.AmmoDef.AmmoRound}"); }
                    },
                    {"CycleRate", () => {
                            var message = string.Empty;
                            return !TryGetValidPlatform(out TmpPlatform) ? string.Empty : TmpPlatform.Weapons.Aggregate(message, (current, w) => current + $"{w.RateOfFire}"); }
                    },
                    {"ShotReady", () => {
                            var message = string.Empty;
                            return !TryGetValidPlatform(out TmpPlatform) ? string.Empty : TmpPlatform.Weapons.Aggregate(message, (current, w) => current + $"{w.ShotReady}"); }
                    },
                    {"LastHeat", () => {
                            var message = string.Empty;
                            return !TryGetValidPlatform(out TmpPlatform) ? string.Empty : TmpPlatform.Weapons.Aggregate(message, (current, w) => current + $"{w.LastHeat}"); }
                    },
                    {"HasTarget", () => {
                            var message = string.Empty;
                            return !TryGetValidPlatform(out TmpPlatform) ? string.Empty : TmpPlatform.Weapons.Aggregate(message, (current, w) => current + $"{w.Target.HasTarget}"); }
                    },
                    {"TargetCurrentState", () => {
                            var message = string.Empty;
                            return !TryGetValidPlatform(out TmpPlatform) ? string.Empty : TmpPlatform.Weapons.Aggregate(message, (current, w) => current + $"{w.Target.CurrentState}"); }
                    },
                    {"TargetIsEntity", () => {
                            var message = string.Empty;
                            return !TryGetValidPlatform(out TmpPlatform) ? string.Empty : TmpPlatform.Weapons.Aggregate(message, (current, w) => current + $"{w.Target.TargetObject != null}"); }
                    },
                    {"TargetEntityId", () => {
                            var message = string.Empty;
                            return !TryGetValidPlatform(out TmpPlatform) ? string.Empty : TmpPlatform.Weapons.Aggregate(message, (current, w) => current + $"{w.TargetData.EntityId}"); }
                    },
                    {"IsShooting", () => {
                            var message = string.Empty;
                            return !TryGetValidPlatform(out TmpPlatform) ? string.Empty : TmpPlatform.Weapons.Aggregate(message, (current, w) => current + $"{w.IsShooting}"); }
                    },
                    {"NoMagsToLoad", () => {
                            var message = string.Empty;
                            return !TryGetValidPlatform(out TmpPlatform) ? string.Empty : TmpPlatform.Weapons.Aggregate(message, (current, w) => current + $"{w.NoMagsToLoad}"); }
                    },
                    {"Loading", () => {
                            var message = string.Empty;
                            return !TryGetValidPlatform(out TmpPlatform) ? string.Empty : TmpPlatform.Weapons.Aggregate(message, (current, w) => current + $"{w.Loading}"); }
                    },
                    {"StartId", () => {
                            var message = string.Empty;
                            return !TryGetValidPlatform(out TmpPlatform) ? string.Empty : TmpPlatform.Weapons.Aggregate(message, (current, w) => current + $"{w.Reload.StartId}"); }
                    },
                    {"ClientStartId", () => {
                            var message = string.Empty;
                            return !TryGetValidPlatform(out TmpPlatform) ? string.Empty : TmpPlatform.Weapons.Aggregate(message, (current, w) => current + $"{w.ClientStartId}"); }
                    },
                    {"WeaponReadyTick", () => {
                            var message = string.Empty;
                            return !TryGetValidPlatform(out TmpPlatform) ? string.Empty : TmpPlatform.Weapons.Aggregate(message, (current, w) => current + $"{I.Tick - w.PartReadyTick}"); }
                    },
                    {"AmmoTypeId", () => {
                            var message = string.Empty;
                            return !TryGetValidPlatform(out TmpPlatform) ? string.Empty : TmpPlatform.Weapons.Aggregate(message, (current, w) => current + $"{w.Reload.AmmoTypeId}"); }
                    },
                    {"ShotsFired", () => {
                            var message = string.Empty;
                            return !TryGetValidPlatform(out TmpPlatform) ? string.Empty : TmpPlatform.Weapons.Aggregate(message, (current, w) => current + $"{w.ShotsFired}"); }
                    },
                    {"Overheated", () => {
                            var message = string.Empty;
                            return !TryGetValidPlatform(out TmpPlatform) ? string.Empty : TmpPlatform.Weapons.Aggregate(message, (current, w) => current + $"{w.PartState.Overheated}"); }
                    },
                    {"Heat", () => {
                            var message = string.Empty;
                            return !TryGetValidPlatform(out TmpPlatform) ? string.Empty : TmpPlatform.Weapons.Aggregate(message, (current, w) => current + $"{w.PartState.Heat}"); }
                    },
                    {"CurrentAmmo", () => {
                            var message = string.Empty;
                            return !TryGetValidPlatform(out TmpPlatform) ? string.Empty : TmpPlatform.Weapons.Aggregate(message, (current, w) => current + $"{w.ProtoWeaponAmmo.CurrentAmmo}"); }
                    },
                    {"CurrentCharge", () => {
                            var message = string.Empty;
                            return !TryGetValidPlatform(out TmpPlatform) ? string.Empty : TmpPlatform.Weapons.Aggregate(message, (current, w) => current + $"{w.ProtoWeaponAmmo.CurrentCharge}"); }
                    },
                    {"CurrentMags", () => {
                            var message = string.Empty;
                            return !TryGetValidPlatform(out TmpPlatform) ? string.Empty : TmpPlatform.Weapons.Aggregate(message, (current, w) => current + $"{w.Reload.CurrentMags}"); }
                    },
                    {"LastEvent", () => {
                            var message = string.Empty;
                            return !TryGetValidPlatform(out TmpPlatform) ? string.Empty : TmpPlatform.Weapons.Aggregate(message, (current, w) => current + $"{w.LastEvent}"); }
                    },
                    {"ReloadEndTick", () => {
                            var message = string.Empty;
                            return !TryGetValidPlatform(out TmpPlatform) ? string.Empty : TmpPlatform.Weapons.Aggregate(message, (current, w) => current + $"{w.ReloadEndTick - I.Tick}"); }
                    },
                    {"CurrentlyDegrading", () => {
                            var message = string.Empty;
                            return !TryGetValidPlatform(out TmpPlatform) ? string.Empty : TmpPlatform.Weapons.Aggregate(message, (current, w) => current + $"{w.CurrentlyDegrading}"); }
                    },
                    {"ShootDelay", () => {
                            var message = string.Empty;
                            return !TryGetValidPlatform(out TmpPlatform) ? string.Empty : TmpPlatform.Weapons.Aggregate(message, (current, w) => current + $"{w.ShootTime <= I.RelativeTime}"); }
                    },
                    {"Charging", () => {
                            var message = string.Empty;
                            return !TryGetValidPlatform(out TmpPlatform) ? string.Empty : TmpPlatform.Weapons.Aggregate(message, (current, w) => current + $"{w.Charging}"); }
                    },
                    {"LastLoadedTick", () => {
                            var message = string.Empty;
                            return !TryGetValidPlatform(out TmpPlatform) ? string.Empty : TmpPlatform.Weapons.Aggregate(message, (current, w) => current + $"{I.Tick - w.LastLoadedTick}"); }
                    },
                };

                return weaponFields;
            }


            internal Ai GetAi()
            {
                Ai ai;
                if (I.EntityAIs.TryGetValue(TargetTopEntity, out ai))
                {
                    return ai;
                }
                return null;

            }

            internal CoreComponent GetComp()
            {
                Ai ai;
                if (I.EntityAIs.TryGetValue(TargetTopEntity, out ai))
                {
                    CoreComponent comp;
                    if (ai.CompBase.TryGetValue(TargetEntity, out comp))
                    {
                        return comp;
                    }
                }
                return null;

            }

            internal CorePlatform GetPlatform()
            {
                Ai ai;
                if (I.EntityAIs.TryGetValue(TargetTopEntity, out ai))
                {
                    CoreComponent comp;
                    if (ai.CompBase.TryGetValue(TargetEntity, out comp))
                    {
                        return comp.Platform;
                    }
                }
                return null;

            }

            internal bool TryGetValidPlatform(out CorePlatform platform)
            {
                platform = GetPlatform();
                return platform != null;
            }

            internal void Clean()
            {
                MyData = null;
                RemoteData = null;
                TargetEntity = null;
                Generating = false;
            }
        }

        internal class TerminalMonitor
        {
            internal readonly Dictionary<CoreComponent, long> ServerTerminalMaps = new Dictionary<CoreComponent, long>();
            internal CoreComponent Comp;
            internal int OriginalAiVersion;
            internal bool Active;

            internal void Monitor()
            {
                if (IsActive())
                {
                    if (Comp.IsBlock && I.Tick20)
                        Comp.TerminalRefresh();
                }
                else if (Active)
                    Clean();
            }
            internal bool IsActive()
            {
                if (Comp?.Ai == null) return false;

                var sameVersion = Comp.Ai.Version == OriginalAiVersion;
                var nothingMarked = !Comp.CoreEntity.MarkedForClose && !Comp.Ai.TopEntity.MarkedForClose && !Comp.Ai.TopEntity.MarkedForClose;
                var sameGrid = Comp.TopEntity == Comp.Ai.TopEntity;
                var inTerminalWindow = I.InMenu && MyAPIGateway.Gui.GetCurrentScreen == MyTerminalPageEnum.ControlPanel;
                var compReady = Comp.Platform.State == CorePlatform.PlatformState.Ready;
                var sameTerminalBlock = Comp.Ai.LastTerminal != null && (Comp.Ai.LastTerminal.EntityId == Comp.Ai.Data.Repo.ActiveTerminal || I.IsClient && (Comp.Ai.Data.Repo.ActiveTerminal == 0 || Comp.Ai.Data.Repo.ActiveTerminal == Comp.CoreEntity.EntityId));
                var isActive = (sameVersion && nothingMarked && sameGrid && compReady && inTerminalWindow && sameTerminalBlock);
                return isActive;
            }

            internal void HandleInputUpdate(CoreComponent comp)
            {
                if (Active && (Comp != comp || OriginalAiVersion != comp.Ai.Version))
                    Clean();

                Comp = comp;

                OriginalAiVersion = comp.Ai.Version;

                if (comp.IsAsleep)
                    comp.WakeupComp();

                if (I.IsClient && !Active)
                    I.SendActiveTerminal(comp);
                else if (I.IsServer)
                    ServerUpdate(Comp);

                Active = true;
            }

            internal void Clean(bool purge = false)
            {
                if (I.MpActive && I.IsClient && Comp != null && !purge)
                {
                    I.PacketsToServer.Add(new TerminalMonitorPacket
                    {
                        SenderId = I.MultiplayerId,
                        PType = PacketType.TerminalMonitor,
                        EntityId = Comp.CoreEntity.EntityId,
                        State = TerminalMonitorPacket.Change.Clean,
                    });
                }

                if (!purge && I.IsServer)
                    ServerClean(Comp);

                Comp = null;
                OriginalAiVersion = -1;
                Active = false;
            }


            internal void ServerUpdate(CoreComponent comp)
            {
                long aTermId;
                if (!ServerTerminalMaps.TryGetValue(comp, out aTermId))
                {
                    ServerTerminalMaps[comp] = comp.CoreEntity.EntityId;
                }
                else
                {

                    var entity = MyEntities.GetEntityByIdOrDefault(aTermId);
                    if (entity != null && entity.GetTopMostParent()?.EntityId != comp.Ai.TopEntity.EntityId)
                    {
                        ServerTerminalMaps[comp] = 0;
                    }
                }

                comp.Ai.Data.Repo.ActiveTerminal = comp.CoreEntity.EntityId;

                if (comp.IsAsleep)
                    comp.WakeupComp();

                if (I.MpActive)
                    I.SendAiData(comp.Ai);
            }

            internal void ServerClean(CoreComponent comp)
            {
                if (ServerTerminalMaps.ContainsKey(comp))
                {
                    ServerTerminalMaps[comp] = 0;
                    comp.Ai.Data.Repo.ActiveTerminal = 0;

                    if (I.MpActive)
                        I.SendAiData(comp.Ai);
                }
                else
                    Log.Line("ServerClean failed ");
            }

            internal void Purge()
            {
                Clean(true);
            }
        }

        internal class AcquireManager
        {
            internal readonly HashSet<PartAcquire> MonitorState = new HashSet<PartAcquire>();
            internal readonly HashSet<PartAcquire> Asleep = new HashSet<PartAcquire>();

            internal readonly List<PartAcquire> Collector = new List<PartAcquire>();
            internal readonly List<PartAcquire> ToRemove = new List<PartAcquire>();

            internal int LastSleepSlot = -1;
            internal int LastAwakeSlot = -1;
            internal int WasAwake;
            internal int WasAsleep;


            internal void Refresh(PartAcquire wa)
            {
                wa.CreatedTick = I.Tick;

                if (!wa.IsSleeping)
                    return;

                Monitor(wa);
            }

            internal void Monitor(PartAcquire wa)
            {
                wa.Monitoring = true;
                wa.IsSleeping = false;
                wa.CreatedTick = I.Tick;

                if (LastAwakeSlot < AwakeBuckets - 1)
                    wa.SlotId = ++LastAwakeSlot;
                else
                    wa.SlotId = LastAwakeSlot = 0;

                Asleep.Remove(wa);
                MonitorState.Add(wa);
            }

            internal void Observer()
            {
                foreach (var wa in MonitorState)
                {

                    var w = wa.Part as Weapon;

                    if (w != null && w.Target.HasTarget || w == null)
                    {
                        ToRemove.Add(wa);
                        continue;
                    }

                    if (I.Tick - wa.CreatedTick > 599)
                    {

                        if (LastSleepSlot < AsleepBuckets - 1)
                            wa.SlotId = ++LastSleepSlot;
                        else
                            wa.SlotId = LastSleepSlot = 0;

                        wa.IsSleeping = true;
                        Asleep.Add(wa);
                        ToRemove.Add(wa);
                    }
                }

                for (int i = 0; i < ToRemove.Count; i++)
                {
                    var wa = ToRemove[i];
                    wa.Monitoring = false;
                    MonitorState.Remove(wa);
                }

                ToRemove.Clear();
            }

            internal void ReorderSleep()
            {
                foreach (var wa in Asleep)
                {

                    var w = wa.Part as Weapon;

                    var remove = w != null && (w.Target.HasTarget || !w.System.HasRequiresTarget) || wa.Part.BaseComp.IsAsleep || !wa.Part.BaseComp.IsWorking || w == null;

                    if (remove)
                    {
                        ToRemove.Add(wa);
                        continue;
                    }
                    Collector.Add(wa);
                }

                Asleep.Clear();

                for (int i = 0; i < ToRemove.Count; i++)
                {
                    var wa = ToRemove[i];
                    wa.IsSleeping = false;
                    MonitorState.Remove(wa);
                }

                WasAsleep = Collector.Count;

                ShellSort(Collector);

                LastSleepSlot = -1;

                for (int i = 0; i < Collector.Count; i++)
                {

                    var wa = Collector[i];
                    if (LastSleepSlot < AsleepBuckets - 1)
                        wa.SlotId = ++LastSleepSlot;
                    else
                        wa.SlotId = LastSleepSlot = 0;

                    Asleep.Add(wa);
                }
                Collector.Clear();
                ToRemove.Clear();
            }

            static void ShellSort(List<PartAcquire> list)
            {
                int length = list.Count;

                for (int h = length / 2; h > 0; h /= 2)
                {
                    for (int i = h; i < length; i += 1)
                    {
                        var tempValue = list[i];
                        var temp = list[i].Part.UniquePartId;

                        int j;
                        for (j = i; j >= h && list[j - h].Part.UniquePartId > temp; j -= h)
                        {
                            list[j] = list[j - h];
                        }

                        list[j] = tempValue;
                    }
                }
            }

            internal void Clean()
            {
                MonitorState.Clear();
                Asleep.Clear();
                Collector.Clear();
                ToRemove.Clear();
            }

        }

        internal enum ControlQuery
        {
            None,
            Keyboard,
            Action,
            Info,
            Mouse,
            Next,
            Prev,
        }

        internal struct BlockDamage
        {
            internal float DirectModifer;
            internal float AreaModifer;
        }

        internal struct ClientProSync
        {
            internal ProtoProPosition ProPosition;

            internal float UpdateTick;
            internal float CurrentOwl;
        }

        internal struct ClientProSyncDebugLine
        {
            internal LineD Line;
            internal Color Color;
            internal uint CreateTick;
        }

        internal struct ApproachStageDebug
        {
            internal Vector3D Position;
            internal uint CreateTick;
        }

        internal struct TickLatency
        {
            internal float CurrentLatency;
            internal float PreviousLatency;
        }

        internal struct ResistanceValues
        {
            internal float EnergeticResistance;
            internal float KineticResistance;
        }

        internal struct CleanSound
        {
            internal Stack<MyEntity3DSoundEmitter> EmitterPool;
            internal MyEntity3DSoundEmitter Emitter;
            internal MySoundPair Pair;
            internal uint SpawnTick;
            internal bool Force;
            internal bool DelayedReturn;
            internal bool JustClean;
        }

        public class WeaponAmmoMoveRequest
        {
            public Weapon Weapon;
            public List<InventoryMags> Inventories = new List<InventoryMags>();

            public void Clean()
            {
                Weapon = null;
                Inventories.Clear();
            }
        }

        public struct WeaponMagMap
        {
            public int WeaponId;
            public AmmoType AmmoType;
        }

        public struct InventoryMags
        {
            public MyInventory Inventory;
            public BetterInventoryItem Item;
            public int Amount;
        }

        public class BetterInventoryItem
        {
            private int _amount;
            public MyObjectBuilder_PhysicalObject Content;
            public MyPhysicalInventoryItem Item;
            public MyDefinitionId DefId;

            public int Amount
            {
                get { return _amount; }
                set { Interlocked.Exchange(ref _amount, value); }
            }
        }

        public class AreaRestriction
        {
            public double MaxSize = 0;
            public double RestrictionRadius = 0;
            public double RestrictionBoxInflation = 0;
            public bool CheckForAnyPart = false;
        }
    }

    public class PlayerMap
    {
        public readonly MyTargetFocusComponentDefinition TargetFocusDef = new MyTargetFocusComponentDefinition();
        public IMyPlayer Player;
        public long PlayerId;
        public MyTargetFocusComponent TargetFocus;
        public MyTargetLockingComponent TargetLock;
    }

    public class GridGroupMap
    {
        public readonly ConcurrentDictionary<MyEntity, Ai> Construct = new ConcurrentDictionary<MyEntity, Ai>();
        public readonly List<Ai> Ais = new List<Ai>();
        public readonly Dictionary<long, Ai.PlayerController> ControlPlayerRequest = new Dictionary<long, Ai.PlayerController>();

        public bool Dirty;
        public GridLinkTypeEnum Type;
        public IMyGridGroupData GroupData;
        public uint LastChangeTick;
        public uint LastControllerTick;

        public GridGroupMap()
        {
            if (Session.I.MpActive && Session.I.IsClient)
                LastControllerTick = Session.I.Tick + 1;
        }

        public void OnTopEntityAdded(IMyGridGroupData group1, IMyCubeGrid topEntity, IMyGridGroupData group2) => OnTopEntityAdded(group1, (MyEntity)topEntity, group2);

        public void OnTopEntityAdded(IMyGridGroupData group1, MyEntity topEntity, IMyGridGroupData group2)
        {
            LastChangeTick = Session.I.Tick;
            TopMap topMap;
            if (Session.I.TopEntityToInfoMap.TryGetValue(topEntity, out topMap))
            {
                topMap.GroupMap = this;
                Construct.TryAdd(topEntity, null);
                if (!Dirty)
                {
                    Session.I.GridGroupUpdates.Add(this);
                    Dirty = true;
                }
            }
            else 
                Log.Line($"OnGridAdded could not find map");

        }

        public void OnTopEntityRemoved(IMyGridGroupData group1, IMyCubeGrid topEntity, IMyGridGroupData group2) => OnTopEntityRemoved(group1, (MyEntity)topEntity, group2);

        public void OnTopEntityRemoved(IMyGridGroupData group1, MyEntity topEntity, IMyGridGroupData group2)
        {
            LastChangeTick = Session.I.Tick;

            TopMap topMap;
            if (Session.I.TopEntityToInfoMap.TryGetValue(topEntity, out topMap))
            {
                topMap.GroupMap = this;
                Construct.Remove(topEntity);
                if (!Dirty)
                {
                    Session.I.GridGroupUpdates.Add(this);
                    Dirty = true;
                }
            }
            else
                Log.Line($"OnGridAdded could not find map");

        }

        public void UpdateAis()
        {
            Ais.Clear();
            foreach (var g in Construct) {
                Ai ai;
                if (Session.I.EntityAIs.TryGetValue(g.Key, out ai))
                    Ais.Add(ai);
            }

            var aiCount = Ais.Count;
            if (aiCount > 0)
            {
                for (int i = 0; i < aiCount; i++)
                    Ais[i].SubGridChanges();

                for (int i = 0; i < aiCount; i++)
                {
                    var ai = Ais[i];

                    ai.Construct.Refresh();
                }

                Ai.Constructs.UpdatePlayerStates(this);
                Ai.Constructs.BuildAiListAndCounters(this);
                Ai.Constructs.WeaponGroupsMarkDirty(this);
            }

            ControlPlayerRequest.Clear();
            Dirty = false;
        }

        internal void AddPlayerId()
        {

        }

        public void Clean()
        {
            GroupData = null;
            LastChangeTick = 0;
            LastControllerTick = 0;
            Dirty = false;
            Construct.Clear();
            Ais.Clear();
            ControlPlayerRequest.Clear();
        }

    }

    public class DeferredBlockDestroy
    {
        public readonly List<BlockDestroyInfo> DestroyBlocks = new List<BlockDestroyInfo>();
        public uint DestroyTick;
    }

    [ProtoContract]
    public struct ProjectileSync
    {
        [ProtoMember(1)] public uint WeaponId;
        [ProtoMember(2)] public ulong SyncId;
    }

    [ProtoContract]
    public class ProtoDeathSyncMonitor
    {
        [ProtoMember(1)] public readonly List<ProjectileSync> Collection = new List<ProjectileSync>(32);
    }

    public struct BlockDestroyInfo
    {
        public IMySlimBlock Block;
        public float ScaledDamage;
        public long AttackerId;
        public MyStringHash DamageType;
        public bool DetonateAmmo;
    }

    public class CustomHitInfo
    {
        public float Fraction;
        public MyEntity HitEntity;
        public Vector3D Position;
    }

    public class DamageHandlerRegistrant
    {
        public readonly Action<ListReader<MyTuple<ulong, long, int, MyEntity, MyEntity, ListReader<MyTuple<Vector3D, object, float>>>>> CallBack;

        public DamageHandlerRegistrant(Action<ListReader<MyTuple<ulong, long, int, MyEntity, MyEntity, ListReader<MyTuple<Vector3D, object, float>>>>> callback)
        {
            CallBack = callback;
        }
    }

    public class WaterData
    {
        public WaterData(MyPlanet planet)
        {
            Planet = planet;
            WaterId = planet.EntityId;
        }

        public MyPlanet Planet;
        public Vector3D Center;
        public long WaterId;
        public float Radius;
        public float MinRadius;
        public float MaxRadius;
        public float WaveHeight;
        public float WaveSpeed;
        public float TideHeight;
        public float TideSpeed;
    }
}
