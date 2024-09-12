using System;
using CoreSystems.Platform;
using CoreSystems.Support;
using Sandbox.ModAPI;
using VRage;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRageMath;
using static CoreSystems.Platform.ControlSys;
using static CoreSystems.Platform.Weapon.ShootManager;
using static CoreSystems.Support.CoreComponent;

namespace CoreSystems
{
    public partial class Session
    {

        #region Packet Creation Methods
        internal class PacketObj
        {
            internal readonly ErrorPacket ErrorPacket = new ErrorPacket();
            internal Packet Packet;
            internal NetworkReporter.Report Report;
            internal int PacketSize;

            internal void Clean()
            {
                Packet = null;
                Report = null;
                PacketSize = 0;
                ErrorPacket.CleanUp();
            }
        }

        public struct NetResult
        {
            public string Message;
            public bool Valid;
        }

        private NetResult Msg(string message, bool valid = false)
        {
            return new NetResult { Message = message, Valid = valid };
        }

        private long _lastFakeTargetUpdateErrorId = long.MinValue;
        private bool Error(PacketObj data, params NetResult[] messages)
        {
            var fakeTargetUpdateError = data.Packet.PType == PacketType.AimTargetUpdate;

            if (fakeTargetUpdateError)
            {
                if (data.Packet.EntityId == _lastFakeTargetUpdateErrorId)
                    return false;
                _lastFakeTargetUpdateErrorId = data.Packet.EntityId;
            }

            var message = $"[{data.Packet.PType.ToString()} - PacketError] - ";

            for (int i = 0; i < messages.Length; i++)
            {
                var resultPair = messages[i];
                message += $"{resultPair.Message}: {resultPair.Valid} - ";
            }
            data.ErrorPacket.Error = message;
            Log.LineShortDate(data.ErrorPacket.Error, "net");
            return false;
        }

        internal struct PacketInfo
        {
            internal MyEntity Entity;
            internal Packet Packet;
            internal Func<object, object, object> Function;
            internal bool SingleClient;
            internal long SpecialPlayerId;
            internal bool Unreliable;
        }

        internal class ErrorPacket
        {
            internal uint RecievedTick;
            internal uint RetryTick;
            internal uint RetryDelayTicks;
            internal int RetryAttempt;
            internal int MaxAttempts;
            internal bool NoReprocess;
            internal bool Retry;
            internal string Error;

            public void CleanUp()
            {
                RecievedTick = 0;
                RetryTick = 0;
                RetryDelayTicks = 0;
                RetryAttempt = 0;
                MaxAttempts = 0;
                NoReprocess = false;
                Retry = false;
                Error = string.Empty;
            }
        }
        #endregion

        #region ServerOnly

        internal readonly HandWeaponDebugPacket HandDebugPacketPacket = new HandWeaponDebugPacket {PType = PacketType.HandWeaponDebug};
        private void SendHandDebugInfo(Weapon weapon)
        {
            PlayerMap player;
            if (Players.TryGetValue(weapon.Comp.Data.Repo.Values.State.PlayerId, out player))
            {
                HandDebugPacketPacket.SenderId = player.Player.SteamUserId;

                PacketsToClient.Add(new PacketInfo
                {
                    SingleClient = true,
                    Unreliable = true,
                    Packet = HandDebugPacketPacket
                });
            }
        }

        private void SendProjectilePosSyncs()
        {
            var packet = ProtoWeaponProPosPacketPool.Count > 0 ? ProtoWeaponProPosPacketPool.Pop() : new ProjectileSyncPositionPacket ();
            
            var latencyMonActive = Tick - LastPongTick < 120;
            LastProSyncSendTick = Tick;

            foreach (var pSync in GlobalProPosSyncs)
            {
                var sync = pSync.Value;
                if (latencyMonActive)
                    packet.Data.Add(sync);

                foreach (var p in sync.Collection)
                    ProtoWeaponProSyncPosPool.Push(p);
            }

            GlobalProPosSyncs.Clear();

            if (!latencyMonActive)
            {
                Log.Line($"PingPong not active");
                ProtoWeaponProPosPacketPool.Push(packet);
                return;
            }

            packet.PType = PacketType.ProjectilePosSyncs;
            PrunedPacketsToClient[packet] = new PacketInfo
            {
                Function = RewriteAddClientLatency,
                SpecialPlayerId = long.MinValue,
                Packet = packet,
                Entity = null,
            };
        }

        private void SendProjectileTargetSyncs()
        {
            var packet = ProtoWeaponProTargetPacketPool.Count > 0 ? ProtoWeaponProTargetPacketPool.Pop() : new ProjectileSyncTargetPacket();

            var latencyMonActive = Tick - LastPongTick < 120;
            LastProSyncSendTick = Tick;

            foreach (var pSync in GlobalProTargetSyncs)
            {
                var sync = pSync.Value;
                if (latencyMonActive)
                    packet.Data.Add(sync);

                foreach (var p in sync.Collection)
                    ProtoWeaponProSyncTargetPool.Push(p);
            }

            GlobalProPosSyncs.Clear();

            if (!latencyMonActive)
            {
                Log.Line($"PingPong not active");
                ProtoWeaponProTargetPacketPool.Push(packet);
                return;
            }

            packet.PType = PacketType.ProjectileTargetSyncs;
            PrunedPacketsToClient[packet] = new PacketInfo
            {
                Function = null,
                Packet = packet,
                Entity = null,
            };
        }

        private object RewriteAddClientLatency(object o1, object o2)
        {
            var proSync = (ProjectileSyncPositionPacket)o1;
            var targetSteamId = (ulong)o2;

            TickLatency tickLatency;
            PlayerTickLatency.TryGetValue(targetSteamId, out tickLatency);
            proSync.CurrentOwl = tickLatency.CurrentLatency;
            return proSync;
        }

        internal void SendConstruct(Ai ai)
        {
            if (IsServer)
            {

                PrunedPacketsToClient.Remove(ai.Construct.Data.Repo.FocusData);
                ++ai.Construct.Data.Repo.FocusData.Revision;

                PacketInfo oldInfo;
                ConstructPacket iPacket;
                if (PrunedPacketsToClient.TryGetValue(ai.Construct.Data.Repo, out oldInfo))
                {
                    iPacket = (ConstructPacket)oldInfo.Packet;
                    iPacket.EntityId = ai.TopEntity.EntityId;
                    iPacket.Data = ai.Construct.Data.Repo;
                }
                else
                {
                    iPacket = PacketConstructPool.Get();
                    iPacket.EntityId = ai.TopEntity.EntityId;
                    iPacket.SenderId = MultiplayerId;
                    iPacket.PType = PacketType.Construct;
                    iPacket.Data = ai.Construct.Data.Repo;
                }

                PrunedPacketsToClient[ai.Construct.Data.Repo] = new PacketInfo
                {
                    Entity = ai.TopEntity,
                    Packet = iPacket,
                };
            }
            else Log.Line("SendConstruct should never be called on Client");
        }

        internal void SendConstructFoci(Ai ai)
        {
            if (IsServer)
            {

                ++ai.Construct.Data.Repo.FocusData.Revision;

                if (!PrunedPacketsToClient.ContainsKey(ai.Construct.Data.Repo))
                {
                    PacketInfo oldInfo;
                    ConstructFociPacket iPacket;
                    if (PrunedPacketsToClient.TryGetValue(ai.Construct.Data.Repo.FocusData, out oldInfo))
                    {
                        iPacket = (ConstructFociPacket)oldInfo.Packet;
                        iPacket.EntityId = ai.TopEntity.EntityId;
                        iPacket.Data = ai.Construct.Data.Repo.FocusData;
                    }
                    else
                    {
                        iPacket = PacketConstructFociPool.Get();
                        iPacket.EntityId = ai.TopEntity.EntityId;
                        iPacket.SenderId = MultiplayerId;
                        iPacket.PType = PacketType.ConstructFoci;
                        iPacket.Data = ai.Construct.Data.Repo.FocusData;
                    }

                    PrunedPacketsToClient[ai.Construct.Data.Repo.FocusData] = new PacketInfo
                    {
                        Entity = ai.TopEntity,
                        Packet = iPacket,
                    };
                }
                else SendConstruct(ai);

            }
            else Log.Line("SendConstructGroups should never be called on Client");
        }

        internal void SendAiData(Ai ai)
        {
            if (IsServer)
            {
                PacketInfo oldInfo;
                AiDataPacket iPacket;
                if (PrunedPacketsToClient.TryGetValue(ai.Data.Repo, out oldInfo))
                {
                    iPacket = (AiDataPacket)oldInfo.Packet;
                    iPacket.EntityId = ai.TopEntity.EntityId;
                    iPacket.Data = ai.Data.Repo;
                }
                else
                {

                    iPacket = PacketAiPool.Get();
                    iPacket.EntityId = ai.TopEntity.EntityId;
                    iPacket.SenderId = MultiplayerId;
                    iPacket.PType = PacketType.AiData;
                    iPacket.Data = ai.Data.Repo;
                }

                PrunedPacketsToClient[ai.Data.Repo] = new PacketInfo
                {
                    Entity = ai.TopEntity,
                    Packet = iPacket,
                };
            }
            else Log.Line("SendAiData should never be called on Client");
        }

        internal void SendWeaponAmmoData(Weapon w)
        {
            if (IsServer)
            {

                const PacketType type = PacketType.WeaponAmmo;
                ++w.ProtoWeaponAmmo.Revision;

                PacketInfo oldInfo;
                WeaponAmmoPacket iPacket;
                if (PrunedPacketsToClient.TryGetValue(w.ProtoWeaponAmmo, out oldInfo))
                {
                    iPacket = (WeaponAmmoPacket)oldInfo.Packet;
                    iPacket.EntityId = w.BaseComp.CoreEntity.EntityId;
                    iPacket.Data = w.ProtoWeaponAmmo;
                }
                else
                {

                    iPacket = PacketAmmoPool.Get();
                    iPacket.EntityId = w.BaseComp.CoreEntity.EntityId;
                    iPacket.SenderId = MultiplayerId;
                    iPacket.PType = type;
                    iPacket.Data = w.ProtoWeaponAmmo;
                    iPacket.PartId = w.PartId;
                }


                PrunedPacketsToClient[w.ProtoWeaponAmmo] = new PacketInfo
                {
                    Entity = w.BaseComp.CoreEntity,
                    Packet = iPacket,
                };
            }
            else Log.Line("SendWeaponAmmoData should never be called on Client");
        }

        internal void SendComp(Weapon.WeaponComponent comp)
        {
            if (IsServer)
            {
                const PacketType type = PacketType.WeaponComp;
                comp.Data.Repo.Values.UpdateCompPacketInfo(comp, true, true);

                PacketInfo oldInfo;
                WeaponCompPacket iPacket;
                if (PrunedPacketsToClient.TryGetValue(comp.Data.Repo.Values, out oldInfo))
                {
                    iPacket = (WeaponCompPacket)oldInfo.Packet;
                    iPacket.EntityId = comp.CoreEntity.EntityId;
                    iPacket.Data = comp.Data.Repo.Values;
                }
                else
                {

                    iPacket = PacketWeaponCompPool.Get();
                    iPacket.EntityId = comp.CoreEntity.EntityId;
                    iPacket.SenderId = MultiplayerId;
                    iPacket.PType = type;
                    iPacket.Data = comp.Data.Repo.Values;
                }
                PrunedPacketsToClient[comp.Data.Repo.Values] = new PacketInfo
                {
                    Entity = comp.CoreEntity,
                    Packet = iPacket,
                };
            }
            else Log.Line("SendComp should never be called on Client");
        }

        internal void SendComp(Upgrade.UpgradeComponent comp)
        {
            if (IsServer)
            {

                const PacketType type = PacketType.UpgradeComp;
                comp.Data.Repo.Values.UpdateCompPacketInfo(comp, true);

                PacketInfo oldInfo;
                UpgradeCompPacket iPacket;
                if (PrunedPacketsToClient.TryGetValue(comp.Data.Repo.Values, out oldInfo))
                {
                    iPacket = (UpgradeCompPacket)oldInfo.Packet;
                    iPacket.EntityId = comp.CoreEntity.EntityId;
                    iPacket.Data = comp.Data.Repo.Values;
                }
                else
                {

                    iPacket = PacketUpgradeCompPool.Get();
                    iPacket.EntityId = comp.CoreEntity.EntityId;
                    iPacket.SenderId = MultiplayerId;
                    iPacket.PType = type;
                    iPacket.Data = comp.Data.Repo.Values;
                }

                PrunedPacketsToClient[comp.Data.Repo.Values] = new PacketInfo
                {
                    Entity = comp.CoreEntity,
                    Packet = iPacket,
                };
            }
            else Log.Line("SendComp should never be called on Client");
        }

        internal void SendComp(SupportSys.SupportComponent comp)
        {
            if (IsServer)
            {

                const PacketType type = PacketType.SupportComp;
                comp.Data.Repo.Values.UpdateCompPacketInfo(comp, true);

                PacketInfo oldInfo;
                SupportCompPacket iPacket;
                if (PrunedPacketsToClient.TryGetValue(comp.Data.Repo.Values, out oldInfo))
                {
                    iPacket = (SupportCompPacket)oldInfo.Packet;
                    iPacket.EntityId = comp.CoreEntity.EntityId;
                    iPacket.Data = comp.Data.Repo.Values;
                }
                else
                {

                    iPacket = PacketSupportCompPool.Get();
                    iPacket.EntityId = comp.CoreEntity.EntityId;
                    iPacket.SenderId = MultiplayerId;
                    iPacket.PType = type;
                    iPacket.Data = comp.Data.Repo.Values;
                }

                PrunedPacketsToClient[comp.Data.Repo.Values] = new PacketInfo
                {
                    Entity = comp.CoreEntity,
                    Packet = iPacket,
                };
            }
            else Log.Line("SendComp should never be called on Client");
        }

        internal void SendComp(ControlComponent comp)
        {
            if (IsServer)
            {

                const PacketType type = PacketType.ControlComp;
                comp.Data.Repo.Values.UpdateCompPacketInfo(comp, true);

                PacketInfo oldInfo;
                ControlCompPacket iPacket;
                if (PrunedPacketsToClient.TryGetValue(comp.Data.Repo.Values, out oldInfo))
                {
                    iPacket = (ControlCompPacket)oldInfo.Packet;
                    iPacket.EntityId = comp.CoreEntity.EntityId;
                    iPacket.Data = comp.Data.Repo.Values;
                }
                else
                {

                    iPacket = PacketControlCompPool.Get();
                    iPacket.EntityId = comp.CoreEntity.EntityId;
                    iPacket.SenderId = MultiplayerId;
                    iPacket.PType = type;
                    iPacket.Data = comp.Data.Repo.Values;
                }

                PrunedPacketsToClient[comp.Data.Repo.Values] = new PacketInfo
                {
                    Entity = comp.CoreEntity,
                    Packet = iPacket,
                };
            }
            else Log.Line("SendComp should never be called on Client");
        }

        internal void SendState(Weapon.WeaponComponent comp)
        {
            if (IsServer)
            {

                if (!PrunedPacketsToClient.ContainsKey(comp.Data.Repo.Values))
                {

                    const PacketType type = PacketType.WeaponState;
                    comp.Data.Repo.Values.UpdateCompPacketInfo(comp);

                    PacketInfo oldInfo;
                    WeaponStatePacket iPacket;
                    if (PrunedPacketsToClient.TryGetValue(comp.Data.Repo.Values.State, out oldInfo))
                    {
                        iPacket = (WeaponStatePacket)oldInfo.Packet;
                        iPacket.EntityId = comp.CoreEntity.EntityId;
                        iPacket.Data = comp.Data.Repo.Values.State;
                    }
                    else
                    {
                        iPacket = PacketWeaponStatePool.Get();
                        iPacket.EntityId = comp.CoreEntity.EntityId;
                        iPacket.SenderId = MultiplayerId;
                        iPacket.PType = type;
                        iPacket.Data = comp.Data.Repo.Values.State;
                    }

                    PrunedPacketsToClient[comp.Data.Repo.Values.State] = new PacketInfo
                    {
                        Entity = comp.CoreEntity,
                        Packet = iPacket,
                    };
                }
                else
                    SendComp(comp);

            }
            else Log.Line("SendState should never be called on Client");
        }

        internal void SendState(SupportSys.SupportComponent comp)
        {
            if (IsServer)
            {

                if (!PrunedPacketsToClient.ContainsKey(comp.Data.Repo.Values))
                {

                    const PacketType type = PacketType.SupportState;
                    comp.Data.Repo.Values.UpdateCompPacketInfo(comp);

                    PacketInfo oldInfo;
                    SupportStatePacket iPacket;
                    if (PrunedPacketsToClient.TryGetValue(comp.Data.Repo.Values.State, out oldInfo))
                    {
                        iPacket = (SupportStatePacket)oldInfo.Packet;
                        iPacket.EntityId = comp.CoreEntity.EntityId;
                        iPacket.Data = comp.Data.Repo.Values.State;
                    }
                    else
                    {
                        iPacket = PacketSupportStatePool.Get();
                        iPacket.EntityId = comp.CoreEntity.EntityId;
                        iPacket.SenderId = MultiplayerId;
                        iPacket.PType = type;
                        iPacket.Data = comp.Data.Repo.Values.State;
                    }

                    PrunedPacketsToClient[comp.Data.Repo.Values.State] = new PacketInfo
                    {
                        Entity = comp.CoreEntity,
                        Packet = iPacket,
                    };
                }
                else
                    SendComp(comp);

            }
            else Log.Line("SendState should never be called on Client");
        }

        internal void SendState(Upgrade.UpgradeComponent comp)
        {
            if (IsServer)
            {

                if (!PrunedPacketsToClient.ContainsKey(comp.Data.Repo.Values))
                {

                    const PacketType type = PacketType.UpgradeState;
                    comp.Data.Repo.Values.UpdateCompPacketInfo(comp);

                    PacketInfo oldInfo;
                    UpgradeStatePacket iPacket;
                    if (PrunedPacketsToClient.TryGetValue(comp.Data.Repo.Values.State, out oldInfo))
                    {
                        iPacket = (UpgradeStatePacket)oldInfo.Packet;
                        iPacket.EntityId = comp.CoreEntity.EntityId;
                        iPacket.Data = comp.Data.Repo.Values.State;
                    }
                    else
                    {
                        iPacket = PacketUpgradeStatePool.Get();
                        iPacket.EntityId = comp.CoreEntity.EntityId;
                        iPacket.SenderId = MultiplayerId;
                        iPacket.PType = type;
                        iPacket.Data = comp.Data.Repo.Values.State;
                    }

                    PrunedPacketsToClient[comp.Data.Repo.Values.State] = new PacketInfo
                    {
                        Entity = comp.CoreEntity,
                        Packet = iPacket,
                    };
                }
                else
                    SendComp(comp);

            }
            else Log.Line("SendState should never be called on Client");
        }

        internal void SendState(ControlSys.ControlComponent comp)
        {
            if (IsServer)
            {

                if (!PrunedPacketsToClient.ContainsKey(comp.Data.Repo.Values))
                {

                    const PacketType type = PacketType.ControlState;
                    comp.Data.Repo.Values.UpdateCompPacketInfo(comp);

                    PacketInfo oldInfo;
                    ControlStatePacket iPacket;
                    if (PrunedPacketsToClient.TryGetValue(comp.Data.Repo.Values.State, out oldInfo))
                    {
                        iPacket = (ControlStatePacket)oldInfo.Packet;
                        iPacket.EntityId = comp.CoreEntity.EntityId;
                        iPacket.Data = comp.Data.Repo.Values.State;
                    }
                    else
                    {
                        iPacket = PacketControlStatePool.Get();
                        iPacket.EntityId = comp.CoreEntity.EntityId;
                        iPacket.SenderId = MultiplayerId;
                        iPacket.PType = type;
                        iPacket.Data = comp.Data.Repo.Values.State;
                    }

                    PrunedPacketsToClient[comp.Data.Repo.Values.State] = new PacketInfo
                    {
                        Entity = comp.CoreEntity,
                        Packet = iPacket,
                    };
                }
                else
                    SendComp(comp);

            }
            else Log.Line("SendState should never be called on Client");
        }

        internal void SendTargetChange(Weapon.WeaponComponent comp, int partId)
        {
            if (IsServer)
            {
                if (!PrunedPacketsToClient.ContainsKey(comp.Data.Repo.Values))
                {
                    const PacketType type = PacketType.TargetChange;
                    comp.Data.Repo.Values.UpdateCompPacketInfo(comp, true, false, partId);

                    var collection = comp.TypeSpecific != CompTypeSpecific.Phantom ? comp.Platform.Weapons : comp.Platform.Phantoms;
                    var w = collection[partId];

                    PacketInfo oldInfo;
                    TargetPacket iPacket;
                    if (PrunedPacketsToClient.TryGetValue(w.TargetData, out oldInfo))
                    {
                        iPacket = (TargetPacket)oldInfo.Packet;
                        iPacket.EntityId = comp.CoreEntity.EntityId;
                        iPacket.Target = w.TargetData;
                    }
                    else
                    {
                        iPacket = PacketTargetPool.Get();
                        iPacket.EntityId = comp.CoreEntity.EntityId;
                        iPacket.SenderId = MultiplayerId;
                        iPacket.PType = type;
                        iPacket.Target = w.TargetData;
                    }

                    PrunedPacketsToClient[w.TargetData] = new PacketInfo
                    {
                        Entity = comp.CoreEntity,
                        Packet = iPacket,
                    };
                }
                else
                    SendComp(comp);
            }
            else Log.Line("SendTargetChange should never be called on Client");
        }

        internal void SendWeaponReload(Weapon w, bool resetWait = false)
        {
            if (IsServer)
            {
                if (resetWait)
                    w.Reload.WaitForClient = false;

                if (!PrunedPacketsToClient.ContainsKey(w.Comp.Data.Repo.Values))
                {

                    const PacketType type = PacketType.WeaponReload;
                    w.Comp.Data.Repo.Values.UpdateCompPacketInfo(w.Comp);

                    PacketInfo oldInfo;
                    WeaponReloadPacket iPacket;
                    if (PrunedPacketsToClient.TryGetValue(w.Reload, out oldInfo))
                    {
                        iPacket = (WeaponReloadPacket)oldInfo.Packet;
                        iPacket.EntityId = w.Comp.CoreEntity.EntityId;
                        iPacket.Data = w.Reload;
                    }
                    else
                    {
                        iPacket = PacketReloadPool.Get();
                        iPacket.EntityId = w.Comp.CoreEntity.EntityId;
                        iPacket.SenderId = MultiplayerId;
                        iPacket.PType = type;
                        iPacket.Data = w.Reload;
                        iPacket.PartId = w.PartId;
                    }

                    PrunedPacketsToClient[w.Reload] = new PacketInfo
                    {
                        Entity = w.Comp.CoreEntity,
                        Packet = iPacket,
                    };
                }
                else
                    SendComp(w.Comp);
            }
            else Log.Line("SendWeaponReload should never be called on Client");
        }

        internal void SendClientNotify(long id, string message, bool singleClient = false, string color = null, int duration = 0, bool soundClick = false)
        {
            ulong senderId = 0;
            PlayerMap player = null;
            if (singleClient && Players.TryGetValue(id, out player))
                senderId = player.Player.SteamUserId;

            PacketsToClient.Add(new PacketInfo
            {
                Entity = null,
                SingleClient = singleClient,
                Packet = new ClientNotifyPacket
                {
                    EntityId = id,
                    SenderId = senderId,
                    PType = PacketType.ClientNotify,
                    Message = message,
                    Color = color,
                    Duration = duration,
                    SoundClick = soundClick,
                }
            });
        }

        internal void SendPlayerConnectionUpdate(long id, bool connected)
        {
            if (IsServer)
            {
                PacketsToClient.Add(new PacketInfo
                {
                    Entity = null,
                    Packet = new BoolUpdatePacket
                    {
                        EntityId = id,
                        SenderId = MultiplayerId,
                        PType = PacketType.PlayerIdUpdate,
                        Data = connected
                    }
                });
            }
            else Log.Line("SendPlayerConnectionUpdate should only be called on server");
        }

        internal void SendServerStartup(ulong id)
        {
            if (IsServer)
            {
                PacketsToClient.Add(new PacketInfo
                {
                    Entity = null,
                    SingleClient = true,
                    Packet = new ServerPacket
                    {
                        EntityId = 0,
                        SenderId = id,
                        PType = PacketType.ServerData,
                        VersionString = ModContext.ModName,
                        Data = Settings.Enforcement,
                    }
                });
            }
            else Log.Line("SendServerVersion should only be called on server");
        }
        #endregion

        #region ClientOnly
        internal void SendUpdateRequest(long entityId, PacketType ptype)
        {
            if (IsClient)
            {
                PacketsToServer.Add(new Packet
                {
                    EntityId = entityId,
                    SenderId = MultiplayerId,
                    PType = ptype
                });
            }
            else Log.Line("SendUpdateRequest should only be called on clients");
        }

        internal void SendOverRidesClientComp(CoreComponent comp, string settings, int value)
        {
            if (IsClient)
            {
                PacketsToServer.Add(new OverRidesPacket
                {
                    PType = PacketType.OverRidesUpdate,
                    EntityId = comp.CoreEntity.EntityId,
                    SenderId = MultiplayerId,
                    Setting = settings,
                    Value = value,
                });
            }
            else Log.Line("SendOverRidesClientComp should only be called on clients");
        }

        internal void SendDroneClientComp(CoreComponent comp, string settings, long value)
        {
            if (IsClient)
            {
                PacketsToServer.Add(new DronePacket
                {
                    PType = PacketType.RequestDroneSet,
                    EntityId = comp.CoreEntity.EntityId,
                    SenderId = MultiplayerId,
                    Setting = settings,
                    Value = value,
                });
            }
            else Log.Line("SendOverRidesClientComp should only be called on clients");
        }

        internal void SendFixedGunHitEvent(bool hit, MyEntity triggerEntity, MyEntity hitEnt, Vector3D origin, Vector3 velocity, Vector3 up, int muzzleId, int systemId, int ammoIndex, float maxTrajectory)
        {
            if (triggerEntity == null || hitEnt == null && hit) return;

            var comp = triggerEntity.Components.Get<CoreComponent>();

            int weaponId;
            if (comp?.Ai?.TopEntity != null && comp.Platform.State == CorePlatform.PlatformState.Ready && comp.Platform.Structure.HashToId.TryGetValue(systemId, out weaponId))
            {
                var hitEntId = hitEnt?.EntityId ?? 0;
                var hitOffset = hitEnt != null ? hitEnt.PositionComp.WorldMatrixRef.Translation - origin : origin;

                PacketsToServer.Add(new FixedWeaponHitPacket
                {
                    EntityId = triggerEntity.EntityId,
                    SenderId = MultiplayerId,
                    PType = PacketType.FixedWeaponHitEvent,
                    HitEnt = hitEntId,
                    HitOffset = hitOffset,
                    Up = up,
                    MuzzleId = muzzleId,
                    WeaponId = weaponId,
                    Velocity = velocity,
                    AmmoIndex = ammoIndex,
                    MaxTrajectory = maxTrajectory,
                });
            }
        }

        #endregion

        #region AIFocus packets
        internal void SendFocusTargetUpdate(Ai ai, long targetId)
        {
            if (IsClient)
            {
                PacketsToServer.Add(new FocusPacket
                {
                    EntityId = ai.TopEntity.EntityId,
                    SenderId = MultiplayerId,
                    PType = PacketType.FocusUpdate,
                    TargetId = targetId
                });

            }
            else if (HandlesInput)
            {
                PacketsToClient.Add(new PacketInfo
                {
                    Entity = ai.TopEntity,
                    Packet = new FocusPacket
                    {
                        EntityId = ai.TopEntity.EntityId,
                        SenderId = MultiplayerId,
                        PType = PacketType.FocusUpdate,
                        TargetId = targetId
                    }
                });
            }
        }

        internal void SendFocusLockUpdate(Ai ai)
        {
            if (IsClient)
            {
                PacketsToServer.Add(new FocusPacket
                {
                    EntityId = ai.TopEntity.EntityId,
                    SenderId = MultiplayerId,
                    PType = PacketType.FocusLockUpdate,
                });

            }
            else if (HandlesInput)
            {
                PacketsToClient.Add(new PacketInfo
                {
                    Entity = ai.TopEntity,
                    Packet = new FocusPacket
                    {
                        EntityId = ai.TopEntity.EntityId,
                        SenderId = MultiplayerId,
                        PType = PacketType.FocusLockUpdate,
                    }
                });
            }
        }

        internal void SendReleaseActiveUpdate(Ai ai)
        {
            if (IsClient)
            {
                PacketsToServer.Add(new FocusPacket
                {
                    EntityId = ai.TopEntity.EntityId,
                    SenderId = MultiplayerId,
                    PType = PacketType.ReleaseActiveUpdate
                });
            }
            else if (HandlesInput)
            {
                PacketsToClient.Add(new PacketInfo
                {
                    Entity = ai.TopEntity,
                    Packet = new FocusPacket
                    {
                        EntityId = ai.TopEntity.EntityId,
                        SenderId = MultiplayerId,
                        PType = PacketType.ReleaseActiveUpdate
                    }
                });
            }
        }
        #endregion

        internal void SendClientReady(Weapon w)
        {
            if (IsClient)
            {
                PacketsToServer.Add(new ClientReadyPacket
                {
                    EntityId = w.Comp.CoreEntity.EntityId,
                    SenderId = MultiplayerId,
                    PType = PacketType.ClientReady,
                    WeaponId = w.PartId
                });
            }
            else Log.Line("SendClientReady on anything but client");
        }

        internal void SendActiveControlUpdate(Ai ai, MyEntity entity, bool active)
        {
            if (IsClient)
            {
                PacketsToServer.Add(new BoolUpdatePacket
                {
                    EntityId = entity.EntityId,
                    SenderId = MultiplayerId,
                    PType = PacketType.ActiveControlUpdate,
                    Data = active
                });
            }
            else if (HandlesInput)
            {
                ai.Construct.NetRefreshAi();
            }
            else Log.Line("SendActiveControlUpdate should never be called on Dedicated");
        }

        internal void SendActionShootUpdate(CoreComponent comp, Trigger action)
        {
            if (IsClient)
            {
                PacketsToServer.Add(new ShootStatePacket
                {
                    EntityId = comp.CoreEntity.EntityId,
                    SenderId = MultiplayerId,
                    PType = PacketType.RequestShootUpdate,
                    Action = action,
                    PlayerId = PlayerId,
                });
            }
            else if (HandlesInput)
            {
                PacketsToClient.Add(new PacketInfo
                {
                    Entity = comp.CoreEntity,
                    Packet = new ShootStatePacket
                    {
                        EntityId = comp.CoreEntity.EntityId,
                        SenderId = MultiplayerId,
                        PType = PacketType.RequestShootUpdate,
                        Action = action,
                        PlayerId = PlayerId,
                    }
                });
            }
            else Log.Line("SendActionShootUpdate should never be called on Dedicated");
        }

        internal void SendActiveTerminal(CoreComponent comp)
        {
            if (IsClient)
            {
                PacketsToServer.Add(new TerminalMonitorPacket
                {
                    SenderId = MultiplayerId,
                    PType = PacketType.TerminalMonitor,
                    EntityId = comp.CoreEntity.EntityId,
                    State = TerminalMonitorPacket.Change.Update,
                });
            }
            else if (HandlesInput)
            {
                PacketsToClient.Add(new PacketInfo
                {
                    Entity = comp.CoreEntity,
                    Packet = new TerminalMonitorPacket
                    {
                        SenderId = MultiplayerId,
                        PType = PacketType.TerminalMonitor,
                        EntityId = comp.CoreEntity.EntityId,
                        State = TerminalMonitorPacket.Change.Update,
                    }
                });
            }
            else Log.Line("SendActiveTerminal should never be called on Dedicated");
        }

        internal void SendAimTargetUpdate(Ai ai, Ai.FakeTarget fake)
        {
            if (IsClient)
            {
                PacketsToServer.Add(new FakeTargetPacket
                {
                    EntityId = ai.TopEntity.EntityId,
                    SenderId = MultiplayerId,
                    PType = PacketType.AimTargetUpdate,
                    Pos = fake.EntityId != 0 ? fake.LocalPosition : fake.FakeInfo.WorldPosition,
                    TargetId = fake.EntityId,
                });
            }
            else if (HandlesInput)
            {
                PacketsToClient.Add(new PacketInfo
                {
                    Entity = ai.TopEntity,
                    Packet = new FakeTargetPacket
                    {
                        EntityId = ai.TopEntity.EntityId,
                        SenderId = MultiplayerId,
                        PType = PacketType.AimTargetUpdate,
                        Pos = fake.EntityId != 0 ? fake.LocalPosition : fake.FakeInfo.WorldPosition,
                        TargetId = fake.EntityId,
                    }
                });
            }
            else Log.Line($"SendAimTargetUpdate should never be called on Dedicated");
        }

        internal void SendPaintedTargetUpdate(Ai ai, Ai.FakeTarget fake)
        {
            if (IsClient)
            {
                PacketsToServer.Add(new PaintedTargetPacket
                {
                    EntityId = ai.TopEntity.EntityId,
                    SenderId = MultiplayerId,
                    PType = PacketType.PaintedTargetUpdate,
                    Pos = fake.EntityId != 0 ? fake.LocalPosition : fake.FakeInfo.WorldPosition,
                    TargetId = fake.EntityId,
                });
            }
            else if (HandlesInput)
            {
                PacketsToClient.Add(new PacketInfo
                {
                    Entity = ai.TopEntity,
                    Packet = new PaintedTargetPacket
                    {
                        EntityId = ai.TopEntity.EntityId,
                        SenderId = MultiplayerId,
                        PType = PacketType.PaintedTargetUpdate,
                        Pos = fake.EntityId != 0 ? fake.LocalPosition : fake.FakeInfo.WorldPosition,
                        TargetId = fake.EntityId,
                    }
                });
            }
            else Log.Line($"SendPaintedTargetUpdate should never be called on Dedicated");
        }


        internal void SendPlayerControlRequest(CoreComponent comp, long playerId, ProtoWeaponState.ControlMode mode)
        {
            if (IsClient)
            {
                PacketsToServer.Add(new PlayerControlRequestPacket
                {
                    EntityId = comp.CoreEntity.EntityId,
                    SenderId = MultiplayerId,
                    PType = PacketType.PlayerControlRequest,
                    PlayerId = playerId,
                    Mode = mode,
                });
            }
            else if (HandlesInput)
            {
                SendComp((Weapon.WeaponComponent)comp);
            }
            else Log.Line("SendPlayerControlRequest should never be called on Server");
        }

        internal void SendEwaredBlocks()
        {
            if (IsServer)
            {
                _cachedEwarPacket.CleanUp();
                _cachedEwarPacket.SenderId = MultiplayerId;
                _cachedEwarPacket.PType = PacketType.EwaredBlocks;
                _cachedEwarPacket.Data.AddRange(DirtyEwarData.Values);

                DirtyEwarData.Clear();
                EwarNetDataDirty = false;

                PacketsToClient.Add(new PacketInfo { Packet = _cachedEwarPacket });
            }
            else Log.Line($"SendEwaredBlocks should never be called on Client");
        }

        internal void SendAmmoCycleRequest(Weapon w, int newAmmoId)
        {
            if (IsClient)
            {
                PacketsToServer.Add(new AmmoCycleRequestPacket
                {
                    EntityId = w.BaseComp.CoreEntity.EntityId,
                    SenderId = MultiplayerId,
                    PType = PacketType.AmmoCycleRequest,
                    PartId = w.PartId,
                    NewAmmoId = newAmmoId,
                    PlayerId = PlayerId,
                });
            }
            else Log.Line("SendAmmoCycleRequest should never be called on Non-Client");
        }

        internal void SendSetCompFloatRequest(CoreComponent comp, float newDps, PacketType type)
        {
            if (IsClient)
            {
                PacketsToServer.Add(new FloatUpdatePacket
                {
                    EntityId = comp.CoreEntity.EntityId,
                    SenderId = MultiplayerId,
                    PType = type,
                    Data = newDps,
                });
            }
            else if (HandlesInput)
            {
                PacketsToClient.Add(new PacketInfo
                {
                    Entity = comp.CoreEntity,
                    Packet = new FloatUpdatePacket
                    {
                        EntityId = comp.CoreEntity.EntityId,
                        SenderId = MultiplayerId,
                        PType = type,
                        Data = newDps,
                    }
                });
            }
            else Log.Line("SendSetFloatRequest should never be called on Non-HandlesInput");
        }

        internal void SendSetCompBoolRequest(CoreComponent comp, bool newBool, PacketType type)
        {
            if (IsClient)
            {
                PacketsToServer.Add(new BoolUpdatePacket
                {
                    EntityId = comp.CoreEntity.EntityId,
                    SenderId = MultiplayerId,
                    PType = type,
                    Data = newBool,
                });
            }
            else if (HandlesInput)
            {
                PacketsToClient.Add(new PacketInfo
                {
                    Entity = comp.CoreEntity,
                    Packet = new BoolUpdatePacket
                    {
                        EntityId = comp.CoreEntity.EntityId,
                        SenderId = MultiplayerId,
                        PType = type,
                        Data = newBool,
                    }
                });
            }
            else Log.Line("SendSetCompBoolRequest should never be called on Non-HandlesInput");
        }

        internal void SendSetCompIntRequest(CoreComponent comp, int newInt, PacketType type)
        {
            if (IsClient)
            {
                PacketsToServer.Add(new IntUpdatePacket
                {
                    EntityId = comp.CoreEntity.EntityId,
                    SenderId = MultiplayerId,
                    PType = type,
                    Data = newInt,
                });
            }
            else if (MpServer)
            {
                PacketsToClient.Add(new PacketInfo
                {
                    Entity = comp.CoreEntity,
                    Packet = new IntUpdatePacket
                    {
                        EntityId = comp.CoreEntity.EntityId,
                        SenderId = MultiplayerId,
                        PType = type,
                        Data = newInt,
                    }
                });
            }
            else Log.Line("SendSetFloatRequest should never be called on Non-HandlesInput");
        }

        internal void SendSetCompLongRequest(CoreComponent comp, ulong newLong, PacketType type)
        {
            if (IsClient)
            {
                PacketsToServer.Add(new ULongUpdatePacket
                {
                    EntityId = comp.CoreEntity.EntityId,
                    SenderId = MultiplayerId,
                    PType = type,
                    Data = newLong,
                });
            }
            else if (MpServer)
            {
                PacketsToClient.Add(new PacketInfo
                {
                    Entity = comp.CoreEntity,
                    Packet = new ULongUpdatePacket
                    {
                        EntityId = comp.CoreEntity.EntityId,
                        SenderId = MultiplayerId,
                        PType = type,
                        Data = newLong,
                    }
                });
            }
            else Log.Line("SendSetFloatRequest should never be called on Non-HandlesInput");
        }

        internal void SendShootRequest(CoreComponent comp, ulong newLong, PacketType type, Func<object, object, object> function, long requestingPlayerId)
        {
            if (IsClient)
            {
                PacketsToServer.Add(new ULongUpdatePacket
                {
                    EntityId = comp.CoreEntity.EntityId,
                    SenderId = MultiplayerId,
                    PType = type,
                    Data = newLong,
                });
            }
            else if (MpServer)
            {
                PacketsToClient.Add(new PacketInfo
                {
                    Entity = comp.CoreEntity,
                    Function = function,
                    SpecialPlayerId = requestingPlayerId,

                    Packet = new ULongUpdatePacket
                    {
                        EntityId = comp.CoreEntity.EntityId,
                        SenderId = MultiplayerId,
                        PType = type,
                        Data = newLong,
                    }
                });
            }
            else Log.Line("SendSetFloatRequest should never be called on Non-HandlesInput");
        }

        internal void SendShootReject(CoreComponent comp, ulong newLong, PacketType type, ulong clientId)
        {
            if (IsClient)
            {
                PacketsToServer.Add(new ULongUpdatePacket
                {
                    EntityId = comp.CoreEntity.EntityId,
                    SenderId = MultiplayerId,
                    PType = type,
                    Data = newLong,
                });
            }
            else if (MpServer)
            {
                PacketsToClient.Add(new PacketInfo
                {
                    Entity = comp.CoreEntity,
                    SingleClient = true,
                    Packet = new ULongUpdatePacket
                    {
                        EntityId = comp.CoreEntity.EntityId,
                        SenderId = clientId,
                        PType = type,
                        Data = newLong,
                    }
                });
            }
            else Log.Line("SendSetFloatRequest should never be called on Non-HandlesInput");
        }

        internal void TrackReticleUpdate(Weapon.WeaponComponent comp, bool track)
        {
            if (IsClient)
            {
                PacketsToServer.Add(new BoolUpdatePacket
                {
                    EntityId = comp.CoreEntity.EntityId,
                    SenderId = MultiplayerId,
                    PType = PacketType.ReticleUpdate,
                    Data = track
                });
            }
            else if (HandlesInput)
            {

                comp.Data.Repo.Values.State.TrackingReticle = track;
                var wValues = comp.Data.Repo.Values;

                comp.ManualMode = wValues.State.TrackingReticle && wValues.Set.Overrides.Control == ProtoWeaponOverrides.ControlModes.Manual; // needs to be set everywhere dedicated and non-tracking clients receive TrackingReticle or Control updates.

                if (MpActive)
                    SendComp(comp);
            }
        }

        internal void TrackReticleUpdateCtc(ControlComponent comp, bool track)
        {
            if (IsClient)
            {
                PacketsToServer.Add(new BoolUpdatePacket
                {
                    EntityId = comp.CoreEntity.EntityId,
                    SenderId = MultiplayerId,
                    PType = PacketType.ReticleUpdate,
                    Data = track
                });
            }
            else if (HandlesInput)
            {

                comp.Data.Repo.Values.State.TrackingReticle = track;
                var wValues = comp.Data.Repo.Values;

                comp.ManualMode = wValues.State.TrackingReticle && wValues.Set.Overrides.Control == ProtoWeaponOverrides.ControlModes.Manual; // needs to be set everywhere dedicated and non-tracking clients receive TrackingReticle or Control updates.
                if (MpActive)
                    SendComp(comp);
            }
        }

        internal void SendCountingDownUpdate(Weapon.WeaponComponent comp, bool countingDown)
        {
            if (IsClient)
            {
                PacketsToServer.Add(new BoolUpdatePacket
                {
                    EntityId = comp.CoreEntity.EntityId,
                    SenderId = MultiplayerId,
                    PType = PacketType.CountingDownUpdate,
                    Data = countingDown
                });
            }
            else if (IsServer)
            {
                comp.Data.Repo.Values.State.CountingDown = countingDown;
                if (MpActive) SendComp(comp);
            }
        }

        internal void SendTriggerCriticalReaction(Weapon.WeaponComponent comp)
        {
            if (IsClient)
            {
                PacketsToServer.Add(new BoolUpdatePacket
                {
                    EntityId = comp.CoreEntity.EntityId,
                    SenderId = MultiplayerId,
                    PType = PacketType.CountingDownUpdate,
                    Data = true
                });
            }
            else if (IsServer)
            {
                comp.Data.Repo.Values.State.CriticalReaction = true;
                if (MpActive) SendComp(comp);
            }
        }

        internal void SendBlackListRequest(string key, bool enable)
        {
            if (IsClient)
            {
                PacketsToServer.Add(new BlackListPacket
                {
                    EntityId = 0,
                    SenderId = MultiplayerId,
                    PType = PacketType.BlackListRequest,
                    Data = key,
                    Enable = enable,
                });
            }
        }

        internal void RequestToggle(CoreComponent comp, PacketType type)
        {
            if (IsClient)
            {
                PacketsToServer.Add(new Packet
                {
                    EntityId = comp.CoreEntity.EntityId,
                    SenderId = MultiplayerId,
                    PType = type,
                });
            }
            else Log.Line("SendToggle not called on client");
        }

        private readonly PingPacket _pingPongPacket = new PingPacket();
        internal void PingPong(float relativeTime)
        {
            _pingPongPacket.RelativeTime = relativeTime;

            if (IsClient)
            {
                PacketsToServer.Add(_pingPongPacket);
            }
            else if (MpServer)
            {
                PacketsToClient.Add(new PacketInfo
                {
                    Entity = null,
                    Packet = _pingPongPacket,
                });
            }
        }

        internal void RecordClientLatency(PingPacket clientPong)
        {
            var rtt = RelativeTime - clientPong.RelativeTime;
            var owl = (float)Math.Max(Math.Round(rtt + 1d / 2d), 2d);

            TickLatency oldLatency;
            PlayerTickLatency.TryGetValue(clientPong.SenderId, out oldLatency);

            PlayerTickLatency[clientPong.SenderId] = new TickLatency { CurrentLatency = owl, PreviousLatency = oldLatency.CurrentLatency };
            LastPongTick = Tick;
        }
    }
}
