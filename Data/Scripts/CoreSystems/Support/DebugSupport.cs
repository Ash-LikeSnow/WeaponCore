using System;
using System.Text;
using CoreSystems;
using CoreSystems.Platform;
using CoreSystems.Support;
using ProtoBuf;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.Entity;
using VRage.ModAPI;

namespace WeaponCore.Data.Scripts.CoreSystems.Support
{
    internal static class DebugSupport
    {
        public const bool DebugWeaponSync = true;
        public const ushort WeaponSyncDebugId = 11223;
        
        [ProtoContract]
        private struct ProtoWeaponSyncDebug
        {
            [ProtoMember(1)] public long EntityId;
            [ProtoMember(2)] public int WeaponIndex;
            [ProtoMember(3)] public bool IsShooting;
            [ProtoMember(4)] public bool AiShooting;
            [ProtoMember(5)] public bool TurretActive;
            [ProtoMember(6)] public int CurrentAmmo;
            [ProtoMember(7)] public float CurrentCharge; 
            [ProtoMember(8)] public int CurrentMags;
            [ProtoMember(9)] public bool Loading;
            [ProtoMember(10)] public bool OutOfAmmo;
            [ProtoMember(11)] public long TargetId;
            [ProtoMember(12)] public byte TriggerState; 
            [ProtoMember(13)] public byte ControlMode;  
            [ProtoMember(14)] public int DelayedCycleId;
        }

        public static void ServerWeaponSyncDebug(Weapon w, int weaponIdx)
        {
            long targetId = 0;
            if (w.Target.HasTarget && w.Target.TargetObject is MyEntity)
            {
                targetId = ((MyEntity)w.Target.TargetObject).EntityId;
            }

            var packet = new ProtoWeaponSyncDebug
            {
                EntityId = w.Comp.Cube.EntityId,
                WeaponIndex = weaponIdx,
                IsShooting = w.IsShooting,
                AiShooting = w.AiShooting,
                TurretActive = w.TurretActive,
                CurrentAmmo = w.ProtoWeaponAmmo.CurrentAmmo,
                CurrentCharge = w.ProtoWeaponAmmo.CurrentCharge,
                CurrentMags = w.Reload.CurrentMags,
                Loading = w.Loading,
                OutOfAmmo = w.OutOfAmmo,
                TargetId = targetId,
                TriggerState = (byte)w.Comp.Data.Repo.Values.State.Trigger,
                ControlMode = (byte)w.Comp.Data.Repo.Values.State.Control,
                DelayedCycleId = w.DelayedCycleId
            };

            MyAPIGateway.Multiplayer.SendMessageToOthers(WeaponSyncDebugId, MyAPIGateway.Utilities.SerializeToBinary(packet));
        }
        
        public static void WeaponDesyncDebugHandler(ushort handlerId, byte[] data, ulong sender, bool reliable)
        {
            if (Session.I.IsServer)
            {
                MyAPIGateway.Utilities.ShowMessage("WeaponDesync", "Called server-side!");
                return;
            }

            try
            {
                var packet = MyAPIGateway.Utilities.SerializeFromBinary<ProtoWeaponSyncDebug>(data);

                IMyEntity entity;
                if (!MyAPIGateway.Entities.TryGetEntityById(packet.EntityId, out entity))
                {
                    MyAPIGateway.Utilities.ShowMessage("WeaponDesync", $"Invalid weapon debug entity {packet.EntityId}");
                    return;
                }
                
                var cube = entity as MyCubeBlock;

                if (cube == null)
                {
                    MyAPIGateway.Utilities.ShowMessage("WeaponDesync", "Cannot get cube");
                    return;
                }
                    
                Ai gridAi;
                if (!Session.I.EntityAIs.TryGetValue(cube.CubeGrid, out gridAi))
                {
                    MyAPIGateway.Utilities.ShowMessage("WeaponDesync", "Cannot get AI");
                    return;
                }
                 
                var foundWeapon = false;
                foreach (var wComp in gridAi.WeaponComps)
                {
                    if (wComp.CoreEntity.EntityId != packet.EntityId)
                    {
                        continue;
                    }

                    foundWeapon = true;
                    
                    if (packet.WeaponIndex >= wComp.Platform.Weapons.Count)
                    {
                        MyAPIGateway.Utilities.ShowMessage("WC-Debug", $"Invalid server weapon count");
                        break;
                    }
                        
                    var w = wComp.Platform.Weapons[packet.WeaponIndex];

                    long targetId = 0;
                    if (w.Target.HasTarget && w.Target.TargetObject is IMyEntity)
                    {
                        targetId = ((IMyEntity)w.Target.TargetObject).EntityId;
                    }

                    var sb = new StringBuilder();

                    if (w.IsShooting != packet.IsShooting)
                    {
                        sb.AppendLine($"IsShooting (S:{packet.IsShooting} C:{w.IsShooting})");
                    }

                    if (w.AiShooting != packet.AiShooting)
                    {
                        sb.AppendLine($"AiShooting (S:{packet.AiShooting} C:{w.AiShooting})");
                    }

                    if (w.TurretActive != packet.TurretActive)
                    {
                        sb.AppendLine($"TurretActive (S:{packet.TurretActive} C:{w.TurretActive})");
                    }

                    if (w.ProtoWeaponAmmo.CurrentAmmo != packet.CurrentAmmo)
                    {
                        sb.AppendLine($"CurrentAmmo (S:{packet.CurrentAmmo} C:{w.ProtoWeaponAmmo.CurrentAmmo})");
                    }
                            
                    if (Math.Abs(w.ProtoWeaponAmmo.CurrentCharge - packet.CurrentCharge) > 0.01)
                    {
                        sb.AppendLine($"CurrentCharge (S:{packet.CurrentCharge} C:{w.ProtoWeaponAmmo.CurrentCharge})");
                    }

                    if (w.Reload.CurrentMags != packet.CurrentMags)
                    {
                        sb.AppendLine($"CurrentMags (S:{packet.CurrentMags} C:{w.Reload.CurrentMags})");
                    }

                    if (w.Loading != packet.Loading)
                    {
                        sb.AppendLine($"Loading (S:{packet.Loading} C:{w.Loading})");
                    }

                    if (w.OutOfAmmo != packet.OutOfAmmo)
                    {
                        sb.AppendLine($"OutOfAmmo (S:{packet.OutOfAmmo} C:{w.OutOfAmmo})");
                    }

                    if (targetId != packet.TargetId)
                    {
                        sb.AppendLine($"TargetId (S:{packet.TargetId} C:{targetId})");
                    }

                    if ((byte)w.Comp.Data.Repo.Values.State.Trigger != packet.TriggerState)
                    {
                        sb.AppendLine($"Trigger (S:{packet.TriggerState} C:{(byte)w.Comp.Data.Repo.Values.State.Trigger})");
                    }

                    if ((byte)w.Comp.Data.Repo.Values.State.Control != packet.ControlMode)
                    {
                        sb.AppendLine($"Control (S:{packet.ControlMode} C:{(byte)w.Comp.Data.Repo.Values.State.Control})");
                    }

                    if (w.DelayedCycleId != packet.DelayedCycleId)
                    {
                        sb.AppendLine($"DelayedCycleId (S:{packet.DelayedCycleId} C:{w.DelayedCycleId})");
                    }

                    if (sb.Length > 0)
                    {
                        sb.AppendLine();
                        
                        var name = (cube as IMyTerminalBlock)?.CustomName ?? cube.BlockDefinition.Id.SubtypeName;
                               
                        MyAPIGateway.Utilities.ShowMessage("WeaponDesync", $"Block \"{name}\"#{packet.WeaponIndex}:\n{sb}");
                    }

                    break;
                }

                if (!foundWeapon)
                {
                    MyAPIGateway.Utilities.ShowMessage("WeaponDesync", $"Invalid weapon desync packet for {packet.EntityId}");
                }
            }
            catch(Exception ex)
            {
                MyAPIGateway.Utilities.ShowMessage("WeaponDesync", $"Weapon Desync Handler error: {ex.Message}");
            }
        }
    }
}